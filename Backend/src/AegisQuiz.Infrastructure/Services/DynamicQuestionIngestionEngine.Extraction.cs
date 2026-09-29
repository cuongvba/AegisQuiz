using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [CIG Engine — Extraction] Trích xuất phương án cùng dòng, đáp án nằm trong lời giải, phân nhóm chuyên đề con,
    /// nhận diện dòng lời giải / đáp án cục bộ và Live Sandbox phân tích snippet cho Admin Rule Studio.
    /// </summary>
    public partial class DynamicQuestionIngestionEngine
    {
        private static readonly InlineAnswerExtractSpecDto BuiltInInlineAnswerSpec = new();
        private static readonly string BuiltInInlineOptionPattern = new InteractionSpecDto().InlineOptionDelimiterPattern;
        private const string BuiltInExplanationLinePattern = @"^(?:Lời\s*giải|L[\.\?\s]*i\s*gi[\.\?\s]*i|Hướng\s*dẫn\s*giải|Hu?ng\s*d?n\s*gi?i|HƯỚNG\s*DẪN|LỜI\s*GIẢI|HD|Giải\s*thích|Căn\s*cứ|Explanation|Rationale)\b";
        private const string BuiltInAnswerLinePattern = @"^(?:Đáp\s*(?:án|số)|Đa['\s]*p\s*a['\s]*n|Đ\/a|Key|Answer|Kết\s*quả)(?:\s*bằng)?[\.\:\s]+(.*)";
        private static readonly HashSet<string> TruthTokens = new(StringComparer.OrdinalIgnoreCase) { "đúng", "d", "true", "t", "正确", "对" };

        // ─────────────────────────────────────────────────────────────────────
        // Đáp án nằm trong lời giải (Inline Answer Extraction)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Trích xuất đáp án nằm lẫn trong dòng lời giải theo loại câu hỏi:
        /// SINGLE/MULTI → chỉ số phương án (A=1, B=2...), SHORT_ANSWER → giá trị số (dấu phẩy → chấm), TRUE_FALSE → "a:Đ" / "b:S".
        /// </summary>
        public bool TryExtractInlineAnswer(string line, string questionType, QuestionParsingRuleDto? matchedRule, out string extractedAnswer)
        {
            extractedAnswer = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var ruleSpec = matchedRule?.EvaluationSpec?.InlineExtraction;
            var docSpec = _structureDictionary.InlineAnswerSpecs;

            switch ((questionType ?? string.Empty).ToUpperInvariant())
            {
                case "SINGLE":
                case "MULTI":
                {
                    var patterns = PickNonEmpty(ruleSpec?.SingleChoiceExtractionPatterns, docSpec?.SingleChoiceExtractionPatterns, BuiltInInlineAnswerSpec.SingleChoiceExtractionPatterns);
                    var letter = FirstCapture(line, patterns);
                    if (string.IsNullOrEmpty(letter)) return false;
                    extractedAnswer = (char.ToUpperInvariant(letter[0]) - 'A' + 1).ToString();
                    return true;
                }
                case "SHORT_ANSWER":
                {
                    var patterns = PickNonEmpty(ruleSpec?.NumericValueExtractionPatterns, docSpec?.NumericValueExtractionPatterns, BuiltInInlineAnswerSpec.NumericValueExtractionPatterns);
                    var value = FirstCapture(line, patterns);
                    if (string.IsNullOrEmpty(value)) return false;
                    extractedAnswer = value.Replace(",", ".");
                    return true;
                }
                case "TRUE_FALSE":
                {
                    var patterns = PickNonEmpty(ruleSpec?.TrueFalseExtractionPatterns, docSpec?.TrueFalseExtractionPatterns, BuiltInInlineAnswerSpec.TrueFalseExtractionPatterns);
                    foreach (var pattern in patterns)
                    {
                        var match = GetOrAddRegex(pattern).Match(line);
                        if (!match.Success || match.Groups.Count < 3) continue;
                        var label = match.Groups[1].Value.ToLowerInvariant();
                        bool isTrue = TruthTokens.Contains(match.Groups[2].Value);
                        extractedAnswer = $"{label}:{(isTrue ? "Đ" : "S")}";
                        return true;
                    }
                    return false;
                }
                default:
                    return false;
            }
        }

        private static List<string> PickNonEmpty(params List<string>?[] candidates)
            => candidates.FirstOrDefault(c => c != null && c.Count > 0) ?? new List<string>();

        // ─────────────────────────────────────────────────────────────────────
        // Phương án cùng dòng & phân nhóm chuyên đề con
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Bóc tách các phương án nằm cùng một dòng ("A. ... B. ... C. ... D. ...", hỗ trợ đánh dấu đúng bằng dấu *).
        /// Mẫu phân tách lấy từ <c>InteractionSpec.InlineOptionDelimiterPattern</c> của quy tắc (fallback mẫu lõi).
        /// </summary>
        public List<(char Letter, string Content, bool IsMarkedCorrect)> ExtractOptions(string line, QuestionParsingRuleDto? matchedRule = null)
        {
            var result = new List<(char Letter, string Content, bool IsMarkedCorrect)>();
            if (string.IsNullOrWhiteSpace(line)) return result;

            var pattern = matchedRule?.InteractionSpec?.InlineOptionDelimiterPattern;
            if (string.IsNullOrWhiteSpace(pattern)) pattern = BuiltInInlineOptionPattern;

            var matches = GetOrAddRegex(pattern).Matches(line);
            if (matches.Count == 0) return result;

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                if (match.Groups.Count < 3 || string.IsNullOrEmpty(match.Groups[2].Value)) continue;

                char letter = match.Groups[2].Value[0];
                bool isMarked = !string.IsNullOrEmpty(match.Groups[1].Value) || (match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value));

                int contentStart = match.Index + match.Length;
                int contentEnd = i + 1 < matches.Count ? matches[i + 1].Index : line.Length;
                string content = contentEnd > contentStart ? line.Substring(contentStart, contentEnd - contentStart).Trim() : string.Empty;
                content = Regex.Replace(content, @"[\.\s\t]+$", "").Trim();

                result.Add((letter, content, isMarked));
            }
            return result;
        }

        /// <summary>Phân nhóm chuyên đề con theo từ khóa nội dung (ví dụ GPLX: SA_HINH, BIEN_BAO). Trả về null nếu không thuộc nhóm nào.</summary>
        public string? DetectSubCategory(string text, QuestionParsingRuleDto? matchedRule = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            var fromRule = MatchSubCategory(text, matchedRule?.InteractionSpec?.SubCategoryPatterns);
            if (fromRule != null) return fromRule;

            foreach (var rule in SnapshotRules())
            {
                var key = MatchSubCategory(text, rule.InteractionSpec?.SubCategoryPatterns);
                if (key != null) return key;
            }

            var builtIn = new InteractionSpecDto().SubCategoryPatterns;
            return MatchSubCategory(text, builtIn);
        }

        private string? MatchSubCategory(string text, Dictionary<string, List<string>>? mapping)
        {
            if (mapping == null || mapping.Count == 0) return null;
            foreach (var (key, patterns) in mapping)
            {
                if (MatchesAny(text, patterns)) return key;
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Dòng lời giải / đáp án cục bộ ngay dưới câu hỏi
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Nhận diện dòng mở đầu lời giải cục bộ ("Lời giải:", "Hướng dẫn giải:", "Explanation:", "解析:"...).</summary>
        public bool IsExplanationLine(string line, QuestionParsingRuleDto? matchedRule = null)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            if (MatchesAny(line, matchedRule?.EvaluationSpec?.ExplanationLinePrefixPatterns)) return true;
            if (SnapshotRules().Any(rule => MatchesAny(line, rule.EvaluationSpec?.ExplanationLinePrefixPatterns))) return true;
            return GetOrAddRegex(BuiltInExplanationLinePattern).IsMatch(line);
        }

        /// <summary>Nhận diện dòng đáp án cục bộ ("Đáp án: B", "Đáp số: 12", "Key: C"...) và trích phần giá trị đáp án.</summary>
        public bool IsAnswerLine(string line, QuestionParsingRuleDto? matchedRule, out string answerText)
        {
            answerText = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var rulePattern = matchedRule?.EvaluationSpec?.AnswerRegexPattern;
            if (!string.IsNullOrWhiteSpace(rulePattern))
            {
                var match = GetOrAddRegex(rulePattern).Match(line);
                if (match.Success)
                {
                    int groupIdx = matchedRule!.EvaluationSpec.AnswerGroupIndex <= 0 ? 1 : matchedRule.EvaluationSpec.AnswerGroupIndex;
                    answerText = match.Groups.Count > groupIdx ? match.Groups[groupIdx].Value.Trim() : match.Value.Trim();
                    return true;
                }
            }

            var generic = GetOrAddRegex(BuiltInAnswerLinePattern).Match(line);
            if (!generic.Success) return false;
            answerText = generic.Groups.Count > 1 ? generic.Groups[1].Value.Trim() : generic.Value.Trim();
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Live Document Sandbox (Admin Rule Studio & AI Autopilot)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>[Admin Sandbox] Phân tích một đoạn snippet văn bản thô bằng toàn bộ quy tắc hoặc một quy tắc chỉ định.</summary>
        public ParseSnippetResponseDto ParseSnippet(string snippetText, string? specificRuleCode = null)
            => ParseSnippetInternal(snippetText, null, specificRuleCode);

        /// <summary>[AI Autopilot Sandbox] Phân tích snippet bằng một quy tắc tùy biến truyền trực tiếp (chưa cần lưu).</summary>
        public ParseSnippetResponseDto ParseSnippet(string snippetText, QuestionParsingRuleDto customRule)
            => ParseSnippetInternal(snippetText, customRule, null);

        private ParseSnippetResponseDto ParseSnippetInternal(string snippetText, QuestionParsingRuleDto? customRule, string? specificRuleCode)
        {
            var response = new ParseSnippetResponseDto();
            if (string.IsNullOrWhiteSpace(snippetText))
            {
                response.Success = false;
                response.ParsingLogs.Add("Văn bản thử nghiệm rỗng.");
                return response;
            }

            var lines = snippetText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var rules = SnapshotRules();
            DocxImportPreviewDto? currentQ = null;
            QuestionParsingRuleDto? activeRule = customRule;
            var promptBuffer = new List<string>();

            if (activeRule == null && !string.IsNullOrEmpty(specificRuleCode))
            {
                activeRule = rules.FirstOrDefault(r => string.Equals(r.RuleCode, specificRuleCode, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 1. Điểm neo câu hỏi (Anchor)
                bool isAnchor;
                int qNum = 0;
                QuestionParsingRuleDto? matchedRule = null;
                if (activeRule != null)
                {
                    var m = GetOrAddRegex(activeRule.AnchorSpec.RegexPattern).Match(line);
                    isAnchor = m.Success;
                    if (isAnchor)
                    {
                        matchedRule = activeRule;
                        if (activeRule.AnchorSpec.QuestionNumberGroupIndex <= m.Groups.Count - 1)
                        {
                            int.TryParse(m.Groups[activeRule.AnchorSpec.QuestionNumberGroupIndex].Value, out qNum);
                        }
                    }
                }
                else
                {
                    isAnchor = TryMatchQuestionAnchor(line, out qNum, out matchedRule, rules);
                }

                if (isAnchor && matchedRule != null)
                {
                    if (currentQ != null) response.Questions.Add(currentQ);

                    activeRule ??= matchedRule;
                    response.MatchedRuleCode = matchedRule.RuleCode;
                    currentQ = new DocxImportPreviewDto
                    {
                        Content = line,
                        QuestionType = matchedRule.InteractionSpec.DefaultQuestionType,
                        TopicCode = matchedRule.TargetExamType,
                        IsCritical = IsCriticalQuestion(line, matchedRule)
                    };
                    promptBuffer.Clear();
                    response.ParsingLogs.Add($"[Anchor Match] Nhận diện Câu #{qNum} bằng quy tắc {matchedRule.RuleCode}");
                    continue;
                }

                var ruleToUse = activeRule ?? rules.FirstOrDefault() ?? GetDefaultFallbackRules().First();

                // 2. Chưa có câu hỏi được khởi tạo (không có tiền tố "Câu 1:")
                if (currentQ == null)
                {
                    var inlineOpts = ExtractInlineOptions(line);
                    if (inlineOpts.Count >= 2)
                    {
                        var promptPart = line.Substring(0, inlineOpts[0].StartIndex).Trim();
                        var fullPrompt = promptBuffer.Count > 0 ? string.Join("\n", promptBuffer) + "\n" + promptPart : promptPart;
                        currentQ = new DocxImportPreviewDto
                        {
                            Content = string.IsNullOrWhiteSpace(fullPrompt) ? "Câu hỏi" : fullPrompt,
                            QuestionType = ruleToUse.InteractionSpec.DefaultQuestionType,
                            TopicCode = ruleToUse.TargetExamType,
                            IsCritical = IsCriticalQuestion(fullPrompt, ruleToUse)
                        };
                        foreach (var io in inlineOpts)
                        {
                            currentQ.Options.Add($"{io.Letter}. {io.Content}");
                            if (io.IsMarkedCorrect) currentQ.SuggestedAnswer = (io.Letter - 'A' + 1).ToString();
                        }
                        promptBuffer.Clear();
                        response.ParsingLogs.Add($"[Inline Options] Nhận diện {inlineOpts.Count} phương án inline");
                        continue;
                    }

                    if (TryMatchOption(line, ruleToUse, out string implicitLabel, out string implicitContent))
                    {
                        var implicitPrompt = promptBuffer.Count > 0 ? string.Join("\n", promptBuffer) : "Câu hỏi";
                        currentQ = new DocxImportPreviewDto
                        {
                            Content = implicitPrompt,
                            QuestionType = ruleToUse.InteractionSpec.DefaultQuestionType,
                            TopicCode = ruleToUse.TargetExamType,
                            IsCritical = IsCriticalQuestion(implicitPrompt, ruleToUse)
                        };
                        currentQ.Options.Add($"{implicitLabel}. {implicitContent}");
                        promptBuffer.Clear();
                        response.ParsingLogs.Add($"[Implicit Anchor] Khởi tạo câu hỏi tự động từ phương án {implicitLabel}");
                        continue;
                    }

                    promptBuffer.Add(line);
                    continue;
                }

                // 3. Đáp án đúng
                if (TryMatchAnswer(line, ruleToUse, out string answerKey))
                {
                    currentQ.SuggestedAnswer = answerKey;
                    response.ParsingLogs.Add($"[Answer Match] Câu có đáp án: {answerKey}");
                    continue;
                }

                // 4. Lời giải
                if (TryMatchExplanation(line, ruleToUse))
                {
                    currentQ.AiExplanation = line;
                    response.ParsingLogs.Add($"[Explanation Match] Nhận diện lời giải: {line.Substring(0, Math.Min(30, line.Length))}...");
                    continue;
                }

                // 5. Phương án (Option)
                if (TryMatchOption(line, ruleToUse, out string optLabel, out string optContent))
                {
                    currentQ.Options.Add($"{optLabel}. {optContent}");
                    response.ParsingLogs.Add($"[Option Match] Thêm phương án {optLabel}");
                    continue;
                }

                // 6. Tiểu mục tự luận phân cấp (a), b), Bước 1...)
                var subItemPattern = ruleToUse.InteractionSpec.SubItemRegexPattern;
                if (!string.IsNullOrEmpty(subItemPattern))
                {
                    var subMatch = GetOrAddRegex(subItemPattern).Match(line);
                    if (subMatch.Success)
                    {
                        int labelIdx = ruleToUse.InteractionSpec.SubItemLabelGroupIndex;
                        int contentIdx = ruleToUse.InteractionSpec.SubItemContentGroupIndex;
                        string subLabel = labelIdx <= subMatch.Groups.Count - 1 ? subMatch.Groups[labelIdx].Value : string.Empty;
                        string subContent = contentIdx <= subMatch.Groups.Count - 1 ? subMatch.Groups[contentIdx].Value : string.Empty;
                        currentQ.Options.Add($"{subLabel}. {subContent}".Trim());
                        response.ParsingLogs.Add($"[Sub-Item Match] Thêm tiểu mục tự luận {subLabel}");
                        continue;
                    }
                }

                // 7. Nội dung bổ sung cho đề bài hoặc lời giải
                if (!string.IsNullOrEmpty(currentQ.AiExplanation))
                {
                    currentQ.AiExplanation += "\n" + line;
                }
                else if (currentQ.Options.Count == 0
                         || ruleToUse.InteractionSpec.DefaultQuestionType == "SHORT_ANSWER"
                         || ruleToUse.InteractionSpec.DefaultQuestionType == "ESSAY")
                {
                    currentQ.Content += "\n" + line;
                    if (IsCriticalQuestion(line, ruleToUse))
                    {
                        currentQ.IsCritical = true;
                    }
                }
            }

            if (currentQ != null) response.Questions.Add(currentQ);

            response.ParsedCount = response.Questions.Count;
            response.Success = response.ParsedCount > 0;
            return response;
        }

        /// <summary>
        /// Bóc tách các phương án nằm cùng một dòng (A. ... B. ... hoặc 1. ... 2. ...) — API tĩnh dùng cho Excel/PDF pipelines.
        /// Chữ số 1-4 được quy đổi sang chữ cái A-D.
        /// </summary>
        public static List<InlineOptionMatch> ExtractInlineOptions(string text)
        {
            var options = new List<InlineOptionMatch>();
            if (string.IsNullOrWhiteSpace(text)) return options;

            var regex = new Regex(@"(?:^|[\s\t\.\,\;\–\—]+|(?<=[a-z0-9\)]))(\*?)([A-G]|[1-4])(\*?)[\.\:\)]+\s*", RegexOptions.IgnoreCase);
            var matches = regex.Matches(text);
            if (matches.Count < 2) return options;

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                char rawChar = match.Groups[2].Value[0];
                char letter = char.IsDigit(rawChar) ? (char)('A' + (rawChar - '1')) : char.ToUpperInvariant(rawChar);
                bool isCorrect = match.Groups[1].Value == "*" || match.Groups[3].Value == "*";

                int contentStart = match.Index + match.Length;
                int contentEnd = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
                string content = text.Substring(contentStart, contentEnd - contentStart).Trim();
                content = Regex.Replace(content, @"[\.\s\t]+$", "").Trim();

                options.Add(new InlineOptionMatch
                {
                    Letter = letter,
                    Content = content,
                    IsMarkedCorrect = isCorrect,
                    StartIndex = match.Index
                });
            }
            return options;
        }
    }
}
