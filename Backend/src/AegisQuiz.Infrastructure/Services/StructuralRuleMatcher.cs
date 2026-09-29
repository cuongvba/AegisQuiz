using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// Giao diện đại diện cho một quy tắc cấu trúc toán học đã được biên dịch trong bộ nhớ RAM O(1)
    /// </summary>
    public interface ICompiledStructuralMatcher
    {
        string RuleId { get; }
        int Priority { get; }
        bool Enabled { get; }

        bool TryMatch(
            IReadOnlyList<(int x1, int y1, int x2, int y2)> lines,
            List<WmfSimpleTextItem> textItems,
            int polyCount,
            int complexShapes,
            int baseFs,
            out List<WmfSimpleTextItem> modifiedItems);
    }

    /// <summary>
    /// [Enterprise Spatial Structural Matcher]
    /// Bộ so khớp cấu trúc không gian toán học động (Declarative Structural Rule Engine):
    /// Bóc tách các cấu trúc 2 chiều (phân số, căn thức, hệ phương trình, giới hạn, vector...)
    /// dựa trên tọa độ hình học Bounding Box và mỏ neo (Anchor) được cấu hình 100% qua JSON/DB.
    /// </summary>
    public class CompiledStructuralMatcher : ICompiledStructuralMatcher
    {
        public MathStructuralRuleDto Rule { get; }
        public string RuleId => Rule.Id;
        public int Priority => Rule.Priority;
        public bool Enabled => Rule.Enabled;

        private readonly Dictionary<string, Regex?> _compiledSlotRegexes = new(StringComparer.OrdinalIgnoreCase);

        public CompiledStructuralMatcher(MathStructuralRuleDto rule)
        {
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));

            if (Rule.Slots != null)
            {
                foreach (var slot in Rule.Slots)
                {
                    if (!string.IsNullOrWhiteSpace(slot.AllowedPattern))
                    {
                        try
                        {
                            _compiledSlotRegexes[slot.Name] = new Regex(slot.AllowedPattern, RegexOptions.Compiled);
                        }
                        catch
                        {
                            _compiledSlotRegexes[slot.Name] = null;
                        }
                    }
                }
            }
        }

        public bool TryMatch(
            IReadOnlyList<(int x1, int y1, int x2, int y2)> lines,
            List<WmfSimpleTextItem> textItems,
            int polyCount,
            int complexShapes,
            int baseFs,
            out List<WmfSimpleTextItem> modifiedItems)
        {
            modifiedItems = textItems;
            if (!Rule.Enabled || textItems == null || textItems.Count == 0)
            {
                return false;
            }

            // 1. Kiểm tra các ràng buộc an toàn (Constraints Guard)
            if (Rule.Constraints != null)
            {
                if (Rule.Constraints.DisallowPolygons && (polyCount > 0 || complexShapes > 0))
                {
                    return false;
                }

                if (lines != null && lines.Count > Rule.Constraints.MaxLines)
                {
                    return false;
                }
            }

            if (baseFs <= 0) baseFs = 14;
            var anchorType = (Rule.Anchor?.Type ?? "GEOMETRIC_LINE").ToUpperInvariant();

            switch (anchorType)
            {
                case "GEOMETRIC_LINE":
                    return MatchGeometricLineFraction(lines, textItems, baseFs, out modifiedItems);

                case "COMPOUND":
                    return MatchCompoundRadical(lines, textItems, baseFs, out modifiedItems);

                case "DELIMITER_SYMBOL":
                    return MatchDelimiterSystemOfEquations(lines, textItems, baseFs, out modifiedItems);

                case "TEXT_ANCHOR":
                    return MatchTextAnchor(lines, textItems, baseFs, out modifiedItems);

                case "VECTOR_OVERLINE":
                    return MatchVectorOverline(lines, textItems, baseFs, out modifiedItems);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Khớp cấu trúc phân số qua đường kẻ ngang (GEOMETRIC_LINE)
        /// </summary>
        private bool MatchGeometricLineFraction(
            IReadOnlyList<(int x1, int y1, int x2, int y2)>? lines,
            List<WmfSimpleTextItem> textItems,
            int baseFs,
            out List<WmfSimpleTextItem> modifiedItems)
        {
            modifiedItems = textItems;
            if (lines == null || lines.Count != (Rule.Anchor?.Count ?? 1))
            {
                return false;
            }

            var line = lines[0];
            int dx = Math.Abs(line.x2 - line.x1);
            int dy = Math.Abs(line.y2 - line.y1);
            if (dx == 0) return false;

            float slope = (float)dy / dx;
            float maxSlope = Rule.Anchor?.MaxSlope > 0 ? Rule.Anchor.MaxSlope : 0.08f;
            if (slope > maxSlope) return false;

            double lineY = (line.y1 + line.y2) / 2.0;
            int lineMinX = Math.Min(line.x1, line.x2);
            int lineMaxX = Math.Max(line.x1, line.x2);

            var numSlot = Rule.Slots?.FirstOrDefault(s => s.RelativePosition.Equals("ABOVE_ANCHOR", StringComparison.OrdinalIgnoreCase));
            var denSlot = Rule.Slots?.FirstOrDefault(s => s.RelativePosition.Equals("BELOW_ANCHOR", StringComparison.OrdinalIgnoreCase));

            if (numSlot == null || denSlot == null) return false;

            int padX = Math.Max(numSlot.PaddingX, denSlot.PaddingX);
            if (padX < 40) padX = 40;
            padX = Math.Max(padX, (int)(baseFs * 0.12f));

            bool IsInFractionSpan(WmfSimpleTextItem t)
            {
                int effectiveW = t.Width > 0 ? Math.Min(t.Width, (int)(t.FontSize * 0.75f)) : 10;
                int centerX = t.X + effectiveW / 2;
                return t.X >= lineMinX - padX && centerX <= lineMaxX + padX;
            }

            var numItems = textItems.Where(t => t.Y < lineY && IsInFractionSpan(t)).OrderBy(t => t.X).ToList();
            var denItems = textItems.Where(t => t.Y > lineY && IsInFractionSpan(t)).OrderBy(t => t.X).ToList();

            if (!ValidateSlot(numSlot, numItems, baseFs) || !ValidateSlot(denSlot, denItems, baseFs))
            {
                return false;
            }

            string numText = string.Join("", numItems.Select(t => t.Text)).Trim();
            string denText = string.Join("", denItems.Select(t => t.Text)).Trim();

            // Sinh mã KaTeX từ Template
            string fracLatex = RenderTemplate(new Dictionary<string, string>
            {
                [numSlot.Name] = numText,
                [denSlot.Name] = denText
            });

            if (string.IsNullOrEmpty(fracLatex))
            {
                fracLatex = $"\\frac{{{numText}}}{{{denText}}}";
            }

            var consumedItems = new HashSet<WmfSimpleTextItem>(numItems.Concat(denItems));
            var outerItems = textItems.Where(t => !consumedItems.Contains(t)).ToList();

            int baseFractionY = outerItems.Count > 0 ? outerItems[0].Y : (int)lineY;
            int baseFractionFs = outerItems.Count > 0 ? outerItems[0].FontSize : baseFs;

            var synthesizedItem = new WmfSimpleTextItem
            {
                Text = fracLatex,
                X = lineMinX,
                Y = baseFractionY,
                Width = Math.Max(10, lineMaxX - lineMinX),
                FontSize = baseFractionFs,
                IsSymbol = false,
                IsMtExtra = false
            };

            outerItems.Add(synthesizedItem);
            modifiedItems = outerItems.OrderBy(t => t.X).ToList();
            return true;
        }

        /// <summary>
        /// Khớp cấu trúc căn thức có gạch nóc (COMPOUND)
        /// </summary>
        private bool MatchCompoundRadical(
            IReadOnlyList<(int x1, int y1, int x2, int y2)>? lines,
            List<WmfSimpleTextItem> textItems,
            int baseFs,
            out List<WmfSimpleTextItem> modifiedItems)
        {
            modifiedItems = textItems;
            if (lines == null || lines.Count != 1) return false;

            var radSymbol = Rule.Anchor?.Symbol ?? "\\sqrt";
            var radSignItem = textItems.FirstOrDefault(t =>
                t.Text.Contains(radSymbol, StringComparison.OrdinalIgnoreCase) ||
                t.Text == "√" || t.Text == "\u00D6");

            if (radSignItem == null) return false;

            var line = lines[0];
            double lineY = (line.y1 + line.y2) / 2.0;
            int lineMinX = Math.Min(line.x1, line.x2);
            int lineMaxX = Math.Max(line.x1, line.x2);

            if (Math.Abs(radSignItem.X + radSignItem.Width - lineMinX) > 40) return false;

            var radicandSlot = Rule.Slots?.FirstOrDefault(s => s.RelativePosition.Equals("UNDER_OVERLINE", StringComparison.OrdinalIgnoreCase) || s.Name.Equals("radicand", StringComparison.OrdinalIgnoreCase));
            var degreeSlot = Rule.Slots?.FirstOrDefault(s => s.RelativePosition.Equals("TOP_LEFT_CORNER", StringComparison.OrdinalIgnoreCase) || s.Name.Equals("degree", StringComparison.OrdinalIgnoreCase));

            if (radicandSlot == null) return false;

            var underItems = textItems.Where(t => t != radSignItem && t.Y > lineY &&
                (t.X + t.Width >= lineMinX - 15) && (t.X <= lineMaxX + 15)).OrderBy(t => t.X).ToList();

            if (!ValidateSlot(radicandSlot, underItems, baseFs)) return false;

            List<WmfSimpleTextItem> degreeItems = new();
            if (degreeSlot != null)
            {
                degreeItems = textItems.Where(t => t != radSignItem && t.X < radSignItem.X &&
                    t.Y < radSignItem.Y && t.FontSize < baseFs * 0.85f).OrderBy(t => t.X).ToList();
            }

            string radText = string.Join("", underItems.Select(t => t.Text)).Trim();
            string degText = string.Join("", degreeItems.Select(t => t.Text)).Trim();

            var placeholders = new Dictionary<string, string>
            {
                [radicandSlot.Name] = radText
            };
            if (degreeSlot != null)
            {
                placeholders[degreeSlot.Name] = degText;
            }

            string renderedLatex = RenderTemplate(placeholders);
            if (string.IsNullOrEmpty(renderedLatex))
            {
                renderedLatex = !string.IsNullOrEmpty(degText) ? $"\\sqrt[{degText}]{{{radText}}}" : $"\\sqrt{{{radText}}}";
            }

            var consumedItems = new HashSet<WmfSimpleTextItem>(underItems.Concat(degreeItems));
            consumedItems.Add(radSignItem);
            var outerItems = textItems.Where(t => !consumedItems.Contains(t)).ToList();

            var synthesizedItem = new WmfSimpleTextItem
            {
                Text = renderedLatex,
                X = radSignItem.X,
                Y = (int)lineY,
                Width = Math.Max(15, lineMaxX - radSignItem.X),
                FontSize = baseFs,
                IsSymbol = false,
                IsMtExtra = false
            };

            outerItems.Add(synthesizedItem);
            modifiedItems = outerItems.OrderBy(t => t.X).ToList();
            return true;
        }

        /// <summary>
        /// Khớp cấu trúc hệ phương trình ngoặc nhọn 2 dòng (DELIMITER_SYMBOL)
        /// </summary>
        private bool MatchDelimiterSystemOfEquations(
            IReadOnlyList<(int x1, int y1, int x2, int y2)>? lines,
            List<WmfSimpleTextItem> textItems,
            int baseFs,
            out List<WmfSimpleTextItem> modifiedItems)
        {
            modifiedItems = textItems;
            if (lines != null && lines.Count > 0) return false;

            var braceSymbol = Rule.Anchor?.Symbol ?? "{";
            var braceItem = textItems.FirstOrDefault(t => t.Text == braceSymbol && (t.FontSize >= baseFs * 1.5f || t.IsSymbol));
            if (braceItem == null) return false;

            var rightItems = textItems.Where(t => t != braceItem && t.X > braceItem.X).OrderBy(t => t.X).ToList();
            if (rightItems.Count == 0) return false;

            int minY = rightItems.Min(t => t.Y);
            int maxY = rightItems.Max(t => t.Y);
            if (maxY - minY < baseFs * 0.8f) return false;

            double midY = (minY + maxY) / 2.0;
            var row1Slot = Rule.Slots?.FirstOrDefault(s => s.Name.Equals("row1", StringComparison.OrdinalIgnoreCase) || s.RelativePosition.Equals("RIGHT_UPPER", StringComparison.OrdinalIgnoreCase));
            var row2Slot = Rule.Slots?.FirstOrDefault(s => s.Name.Equals("row2", StringComparison.OrdinalIgnoreCase) || s.RelativePosition.Equals("RIGHT_LOWER", StringComparison.OrdinalIgnoreCase));

            if (row1Slot == null || row2Slot == null) return false;

            var row1Items = rightItems.Where(t => t.Y <= midY).OrderBy(t => t.X).ToList();
            var row2Items = rightItems.Where(t => t.Y > midY).OrderBy(t => t.X).ToList();

            if (!ValidateSlot(row1Slot, row1Items, baseFs) || !ValidateSlot(row2Slot, row2Items, baseFs))
            {
                return false;
            }

            string row1Text = string.Join("", row1Items.Select(t => t.Text)).Trim();
            string row2Text = string.Join("", row2Items.Select(t => t.Text)).Trim();

            string latex = RenderTemplate(new Dictionary<string, string>
            {
                [row1Slot.Name] = row1Text,
                [row2Slot.Name] = row2Text
            });

            if (string.IsNullOrEmpty(latex))
            {
                latex = $"\\begin{{cases}} {row1Text} \\\\ {row2Text} \\end{{cases}}";
            }

            var synthesizedItem = new WmfSimpleTextItem
            {
                Text = latex,
                X = braceItem.X,
                Y = (int)midY,
                Width = rightItems.Max(t => t.X + t.Width) - braceItem.X,
                FontSize = baseFs,
                IsSymbol = false,
                IsMtExtra = false
            };

            modifiedItems = new List<WmfSimpleTextItem> { synthesizedItem };
            return true;
        }

        /// <summary>
        /// Khớp mỏ neo ký hiệu chữ như giới hạn lim (TEXT_ANCHOR)
        /// </summary>
        private bool MatchTextAnchor(
            IReadOnlyList<(int x1, int y1, int x2, int y2)>? lines,
            List<WmfSimpleTextItem> textItems,
            int baseFs,
            out List<WmfSimpleTextItem> modifiedItems)
        {
            modifiedItems = textItems;
            var anchorToken = Rule.Anchor?.Symbol ?? "\\lim";
            var anchorItem = textItems.FirstOrDefault(t => t.Text.Equals(anchorToken, StringComparison.OrdinalIgnoreCase) || t.Text.Equals("lim", StringComparison.OrdinalIgnoreCase));
            if (anchorItem == null) return false;

            var subSlot = Rule.Slots?.FirstOrDefault(s => s.RelativePosition.Equals("DIRECTLY_BELOW", StringComparison.OrdinalIgnoreCase) || s.Name.Equals("subscript", StringComparison.OrdinalIgnoreCase));
            var argSlot = Rule.Slots?.FirstOrDefault(s => s.RelativePosition.Equals("RIGHT_ADJACENT", StringComparison.OrdinalIgnoreCase) || s.Name.Equals("argument", StringComparison.OrdinalIgnoreCase));

            if (subSlot == null || argSlot == null) return false;

            // Ký tự dưới lim (subscript): X nằm trong vùng anchor, Y > anchor.Y
            var subItems = textItems.Where(t => t != anchorItem && t.Y > anchorItem.Y &&
                (t.X + t.Width >= anchorItem.X - 10) && (t.X <= anchorItem.X + anchorItem.Width + 20)).OrderBy(t => t.X).ToList();

            var argItems = textItems.Where(t => t != anchorItem && !subItems.Contains(t) && t.X > anchorItem.X + anchorItem.Width - 10).OrderBy(t => t.X).ToList();

            if (!ValidateSlot(subSlot, subItems, baseFs) || !ValidateSlot(argSlot, argItems, baseFs))
            {
                return false;
            }

            string subText = string.Join("", subItems.Select(t => t.Text)).Trim();
            string argText = string.Join("", argItems.Select(t => t.Text)).Trim();

            string latex = RenderTemplate(new Dictionary<string, string>
            {
                [subSlot.Name] = subText,
                [argSlot.Name] = argText
            });

            if (string.IsNullOrEmpty(latex))
            {
                latex = $"\\lim_{{{subText}}} {argText}";
            }

            var consumedItems = new HashSet<WmfSimpleTextItem>(subItems.Concat(argItems));
            consumedItems.Add(anchorItem);
            var outerItems = textItems.Where(t => !consumedItems.Contains(t)).ToList();

            var synthesizedItem = new WmfSimpleTextItem
            {
                Text = latex,
                X = anchorItem.X,
                Y = anchorItem.Y,
                Width = Math.Max(20, (argItems.LastOrDefault()?.X ?? anchorItem.X) - anchorItem.X),
                FontSize = baseFs,
                IsSymbol = false,
                IsMtExtra = false
            };

            outerItems.Add(synthesizedItem);
            modifiedItems = outerItems.OrderBy(t => t.X).ToList();
            return true;
        }

        /// <summary>
        /// Khớp vector mũi tên MT Extra (VECTOR_OVERLINE)
        /// </summary>
        private bool MatchVectorOverline(
            IReadOnlyList<(int x1, int y1, int x2, int y2)>? lines,
            List<WmfSimpleTextItem> textItems,
            int baseFs,
            out List<WmfSimpleTextItem> modifiedItems)
        {
            modifiedItems = textItems;
            var vectorItems = textItems.Where(t => t.IsMtExtra && (t.Text == "u" || t.Text == "r")).ToList();
            var normalItems = textItems.Where(t => !t.IsMtExtra).ToList();

            if (vectorItems.Count == 0 || normalItems.Count == 0) return false;

            var arrowGroups = new List<List<WmfSimpleTextItem>>();
            var currentGroup = new List<WmfSimpleTextItem>();
            foreach (var v in vectorItems.OrderBy(t => t.X))
            {
                currentGroup.Add(v);
                if (v.Text == "r")
                {
                    arrowGroups.Add(new List<WmfSimpleTextItem>(currentGroup));
                    currentGroup.Clear();
                }
            }

            if (arrowGroups.Count == 0) return false;

            var resolvedVectors = new List<(List<WmfSimpleTextItem> Under, WmfSimpleTextItem Replacement)>();
            foreach (var grp in arrowGroups)
            {
                int minVx = grp.Min(t => t.X);
                int maxVx = grp.Max(t => t.X + t.Width);
                double avgVy = grp.Average(t => t.Y);

                var under = normalItems.Where(t => t.Y > avgVy && (t.X + t.Width >= minVx - 15) && (t.X <= maxVx + 15)).OrderBy(t => t.X).ToList();
                if (under.Count > 0 && under.Count <= 3 && under.All(u => Regex.IsMatch(u.Text, @"^[a-zA-Z\']+$")))
                {
                    string underText = string.Join("", under.Select(u => u.Text));
                    string vecLatex = under.Count > 1 ? $"\\overrightarrow{{{underText}}}" : $"\\vec{{{underText}}}";
                    resolvedVectors.Add((under, new WmfSimpleTextItem
                    {
                        Text = vecLatex,
                        X = under[0].X,
                        Y = under[0].Y,
                        Width = under.Sum(u => u.Width),
                        FontSize = under[0].FontSize,
                        IsSymbol = false,
                        IsMtExtra = false
                    }));
                }
            }

            if (resolvedVectors.Count > 0)
            {
                var usedUnder = new HashSet<WmfSimpleTextItem>();
                var newNormal = new List<WmfSimpleTextItem>();
                foreach (var (underList, rep) in resolvedVectors)
                {
                    foreach (var u in underList) usedUnder.Add(u);
                    newNormal.Add(rep);
                }
                foreach (var n in normalItems)
                {
                    if (!usedUnder.Contains(n))
                        newNormal.Add(n);
                }
                modifiedItems = newNormal.OrderBy(t => t.X).ToList();
                return true;
            }

            return false;
        }

        private bool ValidateSlot(SpatialSlotDto slot, List<WmfSimpleTextItem> items, int baseFs)
        {
            if (items.Count == 0)
            {
                return slot.Optional;
            }

            if (slot.MaxTokens > 0 && items.Count > slot.MaxTokens)
            {
                return false;
            }

            if (_compiledSlotRegexes.TryGetValue(slot.Name, out var regex) && regex != null)
            {
                foreach (var item in items)
                {
                    if (!regex.IsMatch(item.Text))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private string RenderTemplate(Dictionary<string, string> placeholders)
        {
            if (string.IsNullOrWhiteSpace(Rule.Template)) return string.Empty;

            var sb = new StringBuilder(Rule.Template);
            foreach (var kvp in placeholders)
            {
                sb.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
            }
            return sb.ToString();
        }
    }
}
