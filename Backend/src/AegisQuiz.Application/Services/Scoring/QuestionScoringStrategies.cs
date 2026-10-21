using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Application.Services.Scoring
{
    public static class ScoringNormalizer
    {
        private static readonly string[] Letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };

        public static List<string> ParseIndexList(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            return raw.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(x => x.Trim())
                      .Where(x => x.Length > 0)
                      .ToList();
        }

        public static string NormalizeIndexToken(string token)
        {
            var t = token.Trim();
            if (t.Length == 1 && char.IsLetter(t[0]))
            {
                var upper = char.ToUpperInvariant(t[0]);
                if (upper >= 'A' && upper <= 'J')
                    return (upper - 'A' + 1).ToString();
            }
            return t;
        }

        public static bool IsOptionCorrect(string answerRaw, string optId, string optLabel, string optValue)
        {
            if (string.IsNullOrWhiteSpace(answerRaw)) return false;
            var tokens = ParseIndexList(answerRaw);
            var cleanId = optId.Trim();
            var cleanLabel = optLabel.Trim().ToUpperInvariant();
            var cleanVal = optValue.Trim().ToLowerInvariant();

            foreach (var token in tokens)
            {
                var t = token.Trim();
                if (string.IsNullOrEmpty(t)) continue;

                if (string.Equals(t, cleanId, StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(t, cleanLabel, StringComparison.OrdinalIgnoreCase)) return true;

                if (t.Length == 1 && char.IsLetter(t[0]))
                {
                    var numFromLetter = (char.ToUpperInvariant(t[0]) - 'A' + 1).ToString();
                    if (numFromLetter == cleanId) return true;
                }

                if (int.TryParse(t, out var num) && num >= 1 && num <= Letters.Length)
                {
                    var letterFromNum = Letters[num - 1];
                    if (string.Equals(letterFromNum, cleanLabel, StringComparison.OrdinalIgnoreCase)) return true;
                }

                if (string.Equals(t.ToLowerInvariant(), cleanVal, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public static Dictionary<string, bool> ParseMultiTfAnswer(string? answerStr)
        {
            var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(answerStr)) return map;

            var raw = answerStr.Trim();
            var regex = new Regex(@"(?:\[|\(|^|\b|\;|\,)\s*([a-z0-9]+)\s*[\:\=\)\.\-]\s*([01]|đúng|sai|true|false|đ|s|t|f)\b", RegexOptions.IgnoreCase);
            var matches = regex.Matches(raw);

            foreach (Match m in matches)
            {
                var rawKey = m.Groups[1].Value.ToLowerInvariant();
                var key = rawKey;
                if (int.TryParse(rawKey, out var num) && num >= 1 && num <= 26)
                {
                    key = ((char)('a' + num - 1)).ToString();
                }

                var valStr = m.Groups[2].Value.ToLowerInvariant();
                var isTrue = valStr == "1" || valStr == "đúng" || valStr == "đ" || valStr == "true" || valStr == "t";
                map[key] = isTrue;
            }

            if (map.Count == 0 && !raw.Contains(':'))
            {
                var tokens = Regex.Split(raw, @"[;, \t\[\]]+").Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                for (int idx = 0; idx < tokens.Count; idx++)
                {
                    var key = ((char)('a' + idx)).ToString();
                    var t = tokens[idx].ToLowerInvariant();
                    map[key] = t == "1" || t == "đ" || t == "đúng" || t == "t" || t == "true";
                }
            }

            return map;
        }

        public static string CanonicalPair(string left, string right)
        {
            var l = Regex.Replace(left.Trim(), @"\s+", "").ToLowerInvariant();
            var r = Regex.Replace(right.Trim(), @"\s+", "").ToLowerInvariant();
            return $"{l}:{r}";
        }

        public static int? ResolveIndex(string raw, int size)
        {
            if (!int.TryParse(raw.Trim(), out var n)) return null;
            // Ưu tiên 1-based index (chuẩn khảo thí: 1..size -> 0..size-1)
            if (n >= 1 && n <= size) return n - 1;
            // Fallback 0-based
            if (n >= 0 && n < size) return n;
            return null;
        }
    }

    public class SingleChoiceEvaluatorStrategy : IQuestionEvaluatorStrategy
    {
        public bool CanHandle(string questionType) =>
            questionType.Equals("SINGLE", StringComparison.OrdinalIgnoreCase) ||
            questionType.Equals("TRUE_FALSE", StringComparison.OrdinalIgnoreCase);

        public EvaluationResult Evaluate(QuestionEvaluationContext ctx)
        {
            var pts = ctx.Points <= 0 ? 1 : ctx.Points;
            var sub = ctx.Submission;
            var isAnswered = !string.IsNullOrWhiteSpace(sub?.SelectedOption);
            if (!isAnswered || sub?.SelectedOption == null)
                return new EvaluationResult(0, pts, EvaluationStatus.Scored, false);

            var options = ctx.Options ?? new List<string>();
            var selId = sub.SelectedOption.Trim();
            var optIndex = int.TryParse(selId, out var parsedIndex) ? parsedIndex - 1 : -1;
            var optVal = optIndex >= 0 && optIndex < options.Count ? options[optIndex] : string.Empty;
            var optLabel = optIndex >= 0 && optIndex < 10 ? ((char)('A' + optIndex)).ToString() : selId;

            var isCorrect = ScoringNormalizer.IsOptionCorrect(ctx.AnswerRaw, selId, optLabel, optVal);
            return new EvaluationResult(isCorrect ? pts : 0, pts, EvaluationStatus.Scored, true);
        }
    }

    public class MultiChoiceEvaluatorStrategy : IQuestionEvaluatorStrategy
    {
        public bool CanHandle(string questionType) =>
            questionType.Equals("MULTI", StringComparison.OrdinalIgnoreCase);

        public EvaluationResult Evaluate(QuestionEvaluationContext ctx)
        {
            var pts = ctx.Points <= 0 ? 1 : ctx.Points;
            var sub = ctx.Submission;
            var selected = sub?.SelectedOptions ?? new List<string>();
            var isAnswered = selected.Count > 0;
            if (!isAnswered)
                return new EvaluationResult(0, pts, EvaluationStatus.Scored, false);

            var options = ctx.Options ?? new List<string>();
            var correctIds = new List<string>();
            for (int i = 0; i < options.Count; i++)
            {
                var optId = (i + 1).ToString();
                var optLabel = ((char)('A' + i)).ToString();
                var optVal = options[i];
                if (ScoringNormalizer.IsOptionCorrect(ctx.AnswerRaw, optId, optLabel, optVal))
                {
                    correctIds.Add(optId);
                }
            }

            var selSet = new HashSet<string>(selected.Select(s => s.Trim()));
            var correctSet = new HashSet<string>(correctIds);
            var isCorrect = correctSet.Count > 0 && selSet.SetEquals(correctSet);

            return new EvaluationResult(isCorrect ? pts : 0, pts, EvaluationStatus.Scored, true);
        }
    }

    public class MultiTrueFalseEvaluatorStrategy : IQuestionEvaluatorStrategy
    {
        public bool CanHandle(string questionType) =>
            questionType.Equals("MULTI_TRUE_FALSE", StringComparison.OrdinalIgnoreCase);

        public EvaluationResult Evaluate(QuestionEvaluationContext ctx)
        {
            var pts = ctx.Points <= 0 ? 1 : ctx.Points;
            var sub = ctx.Submission;
            var expectedMap = ScoringNormalizer.ParseMultiTfAnswer(ctx.AnswerRaw);

            Dictionary<string, bool> userMap;
            if (sub?.MatrixAnswer != null && sub.MatrixAnswer.Count > 0)
                userMap = new Dictionary<string, bool>(sub.MatrixAnswer, StringComparer.OrdinalIgnoreCase);
            else if (!string.IsNullOrWhiteSpace(sub?.ShortText))
                userMap = ScoringNormalizer.ParseMultiTfAnswer(sub.ShortText);
            else
                userMap = new Dictionary<string, bool>();

            var isAnswered = userMap.Count > 0 || !string.IsNullOrWhiteSpace(sub?.ShortText);
            if (!isAnswered || expectedMap.Count == 0)
                return new EvaluationResult(0, pts, EvaluationStatus.Scored, isAnswered);

            int matched = 0;
            foreach (var kvp in expectedMap)
            {
                if (userMap.TryGetValue(kvp.Key, out var val) && val == kvp.Value)
                    matched++;
            }

            int total = expectedMap.Count;
            double score = 0;
            // Chuẩn Bộ GD&ĐT 2025: 4 ý (1->0.1đ, 2->0.25đ, 3->0.5đ, 4->1.0đ)
            if (total == 4)
            {
                if (matched == 4) score = pts;
                else if (matched == 3) score = Math.Round(pts * 0.5, 2);
                else if (matched == 2) score = Math.Round(pts * 0.25, 2);
                else if (matched == 1) score = Math.Round(pts * 0.1, 2);
                else score = 0;
            }
            else
            {
                score = Math.Round(((double)matched / total) * pts, 2);
            }

            return new EvaluationResult(score, pts, EvaluationStatus.Scored, true);
        }
    }

    public class ShortAnswerEvaluatorStrategy : IQuestionEvaluatorStrategy
    {
        public bool CanHandle(string questionType) =>
            questionType.Equals("SHORT_ANSWER", StringComparison.OrdinalIgnoreCase) ||
            questionType.Equals("FILL_BLANK", StringComparison.OrdinalIgnoreCase);

        public EvaluationResult Evaluate(QuestionEvaluationContext ctx)
        {
            var pts = ctx.Points <= 0 ? 1 : ctx.Points;
            var sub = ctx.Submission;
            var actual = (sub?.ShortText ?? string.Empty).Trim().ToLowerInvariant();
            var expected = (ctx.AnswerRaw ?? string.Empty).Trim().ToLowerInvariant();
            var isAnswered = actual.Length > 0;

            if (!isAnswered)
                return new EvaluationResult(0, pts, EvaluationStatus.Scored, false);

            var isCorrect = expected.Length > 0 && actual == expected;
            return new EvaluationResult(isCorrect ? pts : 0, pts, EvaluationStatus.Scored, true);
        }
    }

    public class OrderingEvaluatorStrategy : IQuestionEvaluatorStrategy
    {
        public bool CanHandle(string questionType) =>
            questionType.Equals("ORDERING", StringComparison.OrdinalIgnoreCase);

        public EvaluationResult Evaluate(QuestionEvaluationContext ctx)
        {
            var pts = ctx.Points <= 0 ? 1 : ctx.Points;
            var sub = ctx.Submission;
            var options = ctx.Options ?? new List<string>();
            var defaultOrder = Enumerable.Range(1, options.Count).Select(i => i.ToString()).ToList();

            var selected = (sub?.Ordering != null && sub.Ordering.Count > 0)
                ? sub.Ordering.Select(x => x.Trim()).ToList()
                : defaultOrder;

            var isAnswered = (sub?.Ordering != null && sub.Ordering.Count > 0) || options.Count > 0;
            var expected = ScoringNormalizer.ParseIndexList(ctx.AnswerRaw)
                                            .Select(ScoringNormalizer.NormalizeIndexToken)
                                            .ToList();

            var isCorrect = selected.Count > 0 && selected.SequenceEqual(expected);
            return new EvaluationResult(isCorrect ? pts : 0, pts, EvaluationStatus.Scored, isAnswered);
        }
    }

    public class MatchingEvaluatorStrategy : IQuestionEvaluatorStrategy
    {
        public bool CanHandle(string questionType) =>
            questionType.Equals("MATCHING", StringComparison.OrdinalIgnoreCase);

        public EvaluationResult Evaluate(QuestionEvaluationContext ctx)
        {
            var pts = ctx.Points <= 0 ? 1 : ctx.Points;
            var sub = ctx.Submission;
            var mapping = sub?.Matching ?? new Dictionary<string, string>();
            var isAnswered = mapping.Count > 0;
            if (!isAnswered)
                return new EvaluationResult(0, pts, EvaluationStatus.Scored, false);

            var expectedPairs = ParseExpectedPairs(ctx);
            if (expectedPairs.Count == 0)
                return new EvaluationResult(0, pts, EvaluationStatus.Scored, true);

            var actualPairs = new HashSet<string>(
                mapping.Select(kv => ScoringNormalizer.CanonicalPair(kv.Key, kv.Value)));

            var isCorrect = actualPairs.SetEquals(expectedPairs);
            return new EvaluationResult(isCorrect ? pts : 0, pts, EvaluationStatus.Scored, true);
        }

        private HashSet<string> ParseExpectedPairs(QuestionEvaluationContext ctx)
        {
            var set = new HashSet<string>();

            // 1. Kiểm tra payloadJson
            if (!string.IsNullOrWhiteSpace(ctx.PayloadJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(ctx.PayloadJson);
                    if (doc.RootElement.TryGetProperty("correctPairs", out var cpEl) && cpEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in cpEl.EnumerateArray())
                        {
                            var l = item.TryGetProperty("left", out var lProp) ? lProp.GetString() ?? "" : "";
                            var r = item.TryGetProperty("right", out var rProp) ? rProp.GetString() ?? "" : "";
                            if (!string.IsNullOrWhiteSpace(l) && !string.IsNullOrWhiteSpace(r))
                                set.Add(ScoringNormalizer.CanonicalPair(l, r));
                        }
                        if (set.Count > 0) return set;
                    }
                }
                catch { }
            }

            // 2. Parse từ options
            var options = ctx.Options ?? new List<string>();
            var leftPool = new List<string>();
            var rightPool = new List<string>();

            foreach (var opt in options)
            {
                string[] parts = null!;
                if (opt.Contains("=>")) parts = opt.Split(new[] { "=>" }, StringSplitOptions.None);
                else if (opt.Contains('|')) parts = opt.Split('|');
                else if (Regex.IsMatch(opt, @"\s+[-–—]\s+")) parts = Regex.Split(opt, @"\s+[-–—]\s+");
                else if (opt.Contains(':') && opt.Length > 3) parts = opt.Split(':');

                if (parts != null && parts.Length == 2)
                {
                    var l = parts[0].Trim();
                    var r = parts[1].Trim();
                    if (!string.IsNullOrEmpty(l) && !string.IsNullOrEmpty(r))
                    {
                        leftPool.Add(l);
                        rightPool.Add(r);
                    }
                }
            }

            if (leftPool.Count > 0 && string.IsNullOrWhiteSpace(ctx.AnswerRaw))
            {
                for (int i = 0; i < leftPool.Count; i++)
                    set.Add(ScoringNormalizer.CanonicalPair(leftPool[i], rightPool[i]));
                return set;
            }

            // 3. Parse từ answerRaw (chỉ số 1:1; 2:2)
            if (!string.IsNullOrWhiteSpace(ctx.AnswerRaw))
            {
                var entries = ctx.AnswerRaw.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var entry in entries)
                {
                    var pair = entry.Split(':');
                    if (pair.Length != 2) continue;

                    var rawL = pair[0].Trim();
                    var rawR = pair[1].Trim();

                    var lIdx = ScoringNormalizer.ResolveIndex(rawL, leftPool.Count);
                    var rIdx = ScoringNormalizer.ResolveIndex(rawR, rightPool.Count);

                    if (lIdx.HasValue && rIdx.HasValue && lIdx.Value < leftPool.Count && rIdx.Value < rightPool.Count)
                    {
                        set.Add(ScoringNormalizer.CanonicalPair(leftPool[lIdx.Value], rightPool[rIdx.Value]));
                    }
                    else
                    {
                        set.Add(ScoringNormalizer.CanonicalPair(rawL, rawR));
                    }
                }
            }

            return set;
        }
    }

    public class EssayEvaluatorStrategy : IQuestionEvaluatorStrategy
    {
        public bool CanHandle(string questionType) =>
            questionType.Equals("ESSAY", StringComparison.OrdinalIgnoreCase);

        public EvaluationResult Evaluate(QuestionEvaluationContext ctx)
        {
            var pts = ctx.Points <= 0 ? 1 : ctx.Points;
            var sub = ctx.Submission;
            var isAnswered = !string.IsNullOrWhiteSpace(sub?.Essay);
            return new EvaluationResult(0, pts, EvaluationStatus.Manual, isAnswered);
        }
    }

    public class QuestionEvaluationDispatcher : IQuestionEvaluationDispatcher
    {
        private readonly IEnumerable<IQuestionEvaluatorStrategy> _strategies;

        public QuestionEvaluationDispatcher(IEnumerable<IQuestionEvaluatorStrategy> strategies)
        {
            _strategies = strategies;
        }

        public EvaluationResult Evaluate(QuestionEvaluationContext context)
        {
            var strategy = _strategies.FirstOrDefault(s => s.CanHandle(context.QuestionType));
            if (strategy == null)
            {
                return new EvaluationResult(0, context.Points, EvaluationStatus.Scored, false);
            }
            return strategy.Evaluate(context);
        }
    }
}
