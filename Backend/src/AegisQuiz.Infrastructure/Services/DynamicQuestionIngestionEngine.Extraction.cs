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
            if (TryExtractInlineAnswerStructured(line, questionType, matchedRule, null, out var res))
            {
                if ((questionType == "TRUE_FALSE" || questionType == "MULTI_TRUE_FALSE") && !string.IsNullOrEmpty(res.SubItemKey))
                {
                    bool isTruth = res.SubItemValue.StartsWith("đ", StringComparison.OrdinalIgnoreCase)
                                   || res.SubItemValue.StartsWith("d", StringComparison.OrdinalIgnoreCase)
                                   || res.SubItemValue.StartsWith("t", StringComparison.OrdinalIgnoreCase);
                    extractedAnswer = $"{res.SubItemKey}:{(isTruth ? "Đ" : "S")}";
                    return true;
                }
                extractedAnswer = res.AnswerValue;
                return true;
            }
            extractedAnswer = string.Empty;
            return false;
        }

        /// <summary>
        /// [CIG v4.0 MultiLang] Trích xuất đáp án nằm trong lời giải có cấu trúc, hỗ trợ đa ngôn ngữ (VI, EN, FR, ZH, DE, JA, KO)
        /// Cấu hình động 100% (Zero Hard-code) từ RuleSpec, DocumentStructureDictionary và BuiltInInlineAnswerSpec.
        /// </summary>
        public bool TryExtractInlineAnswerStructured(
            string line,
            string questionType,
            QuestionParsingRuleDto? matchedRule,
            string? language,
            out InlineAnswerResult result)
        {
            result = new InlineAnswerResult();
            if (string.IsNullOrWhiteSpace(line)) return false;

            var ruleSpec = matchedRule?.EvaluationSpec?.InlineExtraction;
            var docSpec = _structureDictionary.InlineAnswerSpecs;
            string qType = (questionType ?? string.Empty).ToUpperInvariant();

            switch (qType)
            {
                case "SINGLE":
                case "MULTI":
                {
                    var patterns = new List<string>();
                    if (ruleSpec?.SingleChoiceExtractionPatterns?.Count > 0) patterns.AddRange(ruleSpec.SingleChoiceExtractionPatterns);
                    if (docSpec?.SingleChoiceExtractionPatterns?.Count > 0) patterns.AddRange(docSpec.SingleChoiceExtractionPatterns);
                    if (BuiltInInlineAnswerSpec.SingleChoiceExtractionPatterns?.Count > 0) patterns.AddRange(BuiltInInlineAnswerSpec.SingleChoiceExtractionPatterns);

                    var letter = FirstCapture(line, patterns);
                    if (string.IsNullOrEmpty(letter)) return false;

                    char letChar = char.ToUpperInvariant(letter[0]);
                    result.Success = true;
                    result.AnswerValue = (letChar - 'A' + 1).ToString();
                    return true;
                }
                case "SHORT_ANSWER":
                {
                    var patterns = new List<string>();
                    if (ruleSpec?.NumericValueExtractionPatterns?.Count > 0) patterns.AddRange(ruleSpec.NumericValueExtractionPatterns);
                    if (docSpec?.NumericValueExtractionPatterns?.Count > 0) patterns.AddRange(docSpec.NumericValueExtractionPatterns);
                    if (BuiltInInlineAnswerSpec.NumericValueExtractionPatterns?.Count > 0) patterns.AddRange(BuiltInInlineAnswerSpec.NumericValueExtractionPatterns);

                    var value = FirstCapture(line, patterns);
                    if (string.IsNullOrEmpty(value)) return false;

                    result.Success = true;
                    result.AnswerValue = value.Trim().Trim('$').Replace(",", ".");
                    return true;
                }
                case "TRUE_FALSE":
                case "MULTI_TRUE_FALSE":
                {
                    var patterns = new List<string>();
                    if (ruleSpec?.TrueFalseExtractionPatterns?.Count > 0) patterns.AddRange(ruleSpec.TrueFalseExtractionPatterns);
                    if (docSpec?.TrueFalseExtractionPatterns?.Count > 0) patterns.AddRange(docSpec.TrueFalseExtractionPatterns);
                    if (BuiltInInlineAnswerSpec.TrueFalseExtractionPatterns?.Count > 0) patterns.AddRange(BuiltInInlineAnswerSpec.TrueFalseExtractionPatterns);

                    foreach (var pattern in patterns)
                    {
                        var match = GetOrAddRegex(pattern).Match(line);
                        if (!match.Success || match.Groups.Count < 3) continue;

                        char rawChar = match.Groups[1].Value[0];
                        string subKey = (char.IsDigit(rawChar) ? (char)('a' + (rawChar - '1')) : char.ToLowerInvariant(rawChar)).ToString();
                        string valToken = match.Groups[2].Value.Trim().ToLowerInvariant();
                        bool isTrue = TruthTokens.Contains(valToken) || valToken.StartsWith("đ") || valToken.StartsWith("d") || valToken.StartsWith("t") || valToken.StartsWith("v") || valToken.StartsWith("r");

                        result.Success = true;
                        result.SubItemKey = subKey;
                        result.SubItemValue = isTrue ? "Đúng" : "Sai";
                        result.AnswerValue = $"{subKey}) {result.SubItemValue}";
                        return true;
                    }
                    return false;
                }
                default:
                    return false;
            }
        }

        /// <summary>
        /// [CIG v4.0] Nhận diện dòng mở đầu Lời giải chi tiết và trích xuất nội dung giải sạch (bỏ nhãn tiêu đề)
        /// Nạp 100% từ Rule EvaluationSpec hoặc DocumentStructureDictionary (Zero Hard-code).
        /// </summary>
        public bool TryMatchExplanationStart(string line, QuestionParsingRuleDto? matchedRule, out string cleanRemainder)
        {
            cleanRemainder = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var patterns = new List<string>();
            if (!string.IsNullOrWhiteSpace(matchedRule?.EvaluationSpec?.ExplanationStartRegex))
                patterns.Add(matchedRule.EvaluationSpec.ExplanationStartRegex);
            if (_structureDictionary.InlineAnswerSpecs?.ExplanationStartPatterns?.Count > 0)
                patterns.AddRange(_structureDictionary.InlineAnswerSpecs.ExplanationStartPatterns);
            if (_structureDictionary.ExplanationMarkers?.Count > 0)
                patterns.AddRange(_structureDictionary.ExplanationMarkers);
            if (BuiltInInlineAnswerSpec.ExplanationStartPatterns?.Count > 0)
                patterns.AddRange(BuiltInInlineAnswerSpec.ExplanationStartPatterns);

            foreach (var pattern in patterns)
            {
                var m = GetOrAddRegex(pattern).Match(line);
                if (m.Success)
                {
                    cleanRemainder = m.Groups.Count > 1 ? m.Groups[1].Value.Trim() : line;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// [CIG v4.0] Nhận diện nhãn Ý con / Mệnh đề Đúng-Sai (a, b, c, d hoặc 1, 2, 3, 4)
        /// Nạp 100% từ DocumentStructureDictionary / Rule Spec (Zero Hard-code).
        /// </summary>
        public bool TryMatchSubItemMarker(
            string line,
            QuestionParsingRuleDto? matchedRule,
            out string subKey,
            out string content,
            out string? inlineAnswer)
        {
            subKey = string.Empty;
            content = string.Empty;
            inlineAnswer = null;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var patterns = new List<string>();
            if (_structureDictionary.InlineAnswerSpecs?.SubItemMarkerPatterns?.Count > 0)
                patterns.AddRange(_structureDictionary.InlineAnswerSpecs.SubItemMarkerPatterns);
            if (BuiltInInlineAnswerSpec.SubItemMarkerPatterns?.Count > 0)
                patterns.AddRange(BuiltInInlineAnswerSpec.SubItemMarkerPatterns);

            foreach (var pattern in patterns)
            {
                var m = GetOrAddRegex(pattern).Match(line);
                if (!m.Success) continue;

                char rawChar = m.Groups[1].Value[0];
                subKey = (char.IsDigit(rawChar) ? (char)('a' + (rawChar - '1')) : char.ToLowerInvariant(rawChar)).ToString();
                content = m.Groups.Count > 2 ? m.Groups[2].Value.Trim() : string.Empty;

                // Kiểm tra xem dòng có chứa đáp án inline không qua TruthTokens
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var tfPatterns = new List<string>();
                    if (_structureDictionary.InlineAnswerSpecs?.TrueFalseExtractionPatterns?.Count > 0)
                        tfPatterns.AddRange(_structureDictionary.InlineAnswerSpecs.TrueFalseExtractionPatterns);
                    if (BuiltInInlineAnswerSpec.TrueFalseExtractionPatterns?.Count > 0)
                        tfPatterns.AddRange(BuiltInInlineAnswerSpec.TrueFalseExtractionPatterns);

                    foreach (var tfp in tfPatterns)
                    {
                        var mAns = GetOrAddRegex(tfp).Match(line);
                        if (mAns.Success && mAns.Groups.Count >= 3)
                        {
                            string token = mAns.Groups[2].Value.Trim().ToLowerInvariant();
                            bool isTrue = TruthTokens.Contains(token) || token.StartsWith("đ") || token.StartsWith("d") || token.StartsWith("t") || token.StartsWith("v") || token.StartsWith("r");
                            inlineAnswer = isTrue ? "Đúng" : "Sai";
                            break;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// [CIG v4.0] Trích xuất số thứ tự câu hỏi từ văn bản đa ngôn ngữ
        /// Nạp 100% từ Rule AnchorSpec hoặc DocumentStructureDictionary (Zero Hard-code).
        /// </summary>
        public bool TryExtractQuestionNumber(string line, QuestionParsingRuleDto? matchedRule, out int questionNumber)
        {
            questionNumber = 0;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var patterns = new List<string>();
            if (!string.IsNullOrWhiteSpace(matchedRule?.AnchorSpec?.RegexPattern))
                patterns.Add(matchedRule.AnchorSpec.RegexPattern);
            if (_structureDictionary.InlineAnswerSpecs?.QuestionNumberPatterns?.Count > 0)
                patterns.AddRange(_structureDictionary.InlineAnswerSpecs.QuestionNumberPatterns);
            if (BuiltInInlineAnswerSpec.QuestionNumberPatterns?.Count > 0)
                patterns.AddRange(BuiltInInlineAnswerSpec.QuestionNumberPatterns);

            foreach (var pattern in patterns)
            {
                var m = GetOrAddRegex(pattern).Match(line.Trim());
                if (m.Success)
                {
                    int groupIdx = matchedRule?.AnchorSpec?.QuestionNumberGroupIndex ?? 1;
                    if (m.Groups.Count > groupIdx && int.TryParse(m.Groups[groupIdx].Value, out int qNum))
                    {
                        questionNumber = qNum;
                        return true;
                    }
                    if (int.TryParse(m.Groups[1].Value, out int fallbackNum))
                    {
                        questionNumber = fallbackNum;
                        return true;
                    }
                }
            }
            return false;
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

            // Nếu chỉ có 1 nhãn số trên dòng (1., 2...) và quy tắc không phải là GPLX,
            // thì đây là mệnh đề liệt kê trong đề bài (ví dụ: "1. Nước sôi...", "2. Kim loại..."), không phải phương án trắc nghiệm
            if (matches.Count == 1 && matches[0].Groups.Count >= 3 && !string.IsNullOrEmpty(matches[0].Groups[2].Value) && char.IsDigit(matches[0].Groups[2].Value[0]))
            {
                bool isGplx = matchedRule?.RuleCode == "VN_GPLX_600_STANDARD" || matchedRule?.TargetExamType == "GPLX";
                if (!isGplx) return result;
            }

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                if (match.Groups.Count < 3 || string.IsNullOrEmpty(match.Groups[2].Value)) continue;

                char letter = match.Groups[2].Value[0];
                bool isMarked = match.Value.Contains("<u>") || match.Value.Contains("<b>") || match.Value.Contains("*")
                    || !string.IsNullOrEmpty(match.Groups[1].Value) 
                    || (match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value));

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

        /// <summary>Nhận diện dòng mở đầu lời giải cục bộ ("Lời giải:", "Hướng dẫn giải:", "Ý tưởng giải:", "Explanation:", "解析:"...).</summary>
        public bool IsExplanationLine(string line, QuestionParsingRuleDto? matchedRule = null)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            if (MatchesAny(line, _structureDictionary.ExplanationMarkers)) return true;
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

            if (activeRule != null)
            {
                response.MatchedRuleCode = activeRule.RuleCode;
            }

            string currentSection = string.Empty;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 0a. Nhận diện tiêu đề phân đoạn (Section Header: PHẦN I, PHẦN II, PHẦN III, PART A, etc.)
                if (TryMatchSectionHeader(line, out string detectedSection))
                {
                    if (currentQ != null)
                    {
                        response.Questions.Add(currentQ);
                        currentQ = null;
                    }

                    currentSection = detectedSection;
                    response.ParsingLogs.Add($"[Section Match] Phát hiện phân đoạn {detectedSection}");

                    // Tự động định tuyến sang quy tắc tương ứng nếu không bị chỉ định cứng
                    if (customRule == null && string.IsNullOrEmpty(specificRuleCode))
                    {
                        var sectionRule = rules.FirstOrDefault(r =>
                            string.Equals(r.InteractionSpec?.DefaultQuestionType, detectedSection, StringComparison.OrdinalIgnoreCase));

                        if (sectionRule != null)
                        {
                            activeRule = sectionRule;
                            response.MatchedRuleCode = sectionRule.RuleCode;
                            response.ParsingLogs.Add($"[Section Routing] Kích hoạt quy tắc {sectionRule.RuleCode} cho phân đoạn {detectedSection}");
                        }
                    }

                    promptBuffer.Clear();
                    continue;
                }

                // 0b. Bỏ qua các dòng chỉ dẫn khảo thí / thông tin kỳ thi
                if (IsExamInstructionOrMeta(line))
                {
                    response.ParsingLogs.Add($"[Instruction Skip] Bỏ qua dòng chỉ dẫn: {line}");
                    continue;
                }

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

                    string determinedType = matchedRule.InteractionSpec.DefaultQuestionType;

                    currentQ = new DocxImportPreviewDto
                    {
                        Content = line,
                        QuestionType = determinedType,
                        TopicCode = matchedRule.TargetExamType,
                        IsCritical = IsCriticalQuestion(line, matchedRule)
                    };
                    promptBuffer.Clear();
                    response.ParsingLogs.Add($"[Anchor Match] Nhận diện Câu #{qNum} ({determinedType}) bằng quy tắc {matchedRule.RuleCode}");
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

                // 3. Đáp án đúng cho Đúng/Sai đa mệnh đề GDPT 2025
                if (currentQ.QuestionType == "MULTI_TRUE_FALSE" || currentQ.QuestionType == "TRUE_FALSE")
                {
                    var matchMultiTfAns = Regex.Match(line, @"^(?:ĐÁP\s*ÁN|ANSWER|KEY|HƯỚNG\s*DẪN)[\.\:\s\-]+(.*)", RegexOptions.IgnoreCase);
                    if (matchMultiTfAns.Success)
                    {
                        var remainder = matchMultiTfAns.Groups[1].Value.Trim();
                        var tfMatches = Regex.Matches(remainder, @"(?:^|\b|\,)([a-d])[\)\:\.\s\-]+(đúng|sai|true|false|đ|s|t|f)\b", RegexOptions.IgnoreCase);
                        if (tfMatches.Count > 0)
                        {
                            var parts = new List<string>();
                            foreach (Match tm in tfMatches)
                            {
                                string ltr = tm.Groups[1].Value.ToLowerInvariant();
                                string vt = tm.Groups[2].Value.ToLowerInvariant();
                                string val = (vt.StartsWith("đ") || vt.StartsWith("d") || vt.StartsWith("t")) ? "Đúng" : "Sai";
                                parts.Add($"{ltr}) {val}");
                            }
                            parts.Sort();
                            currentQ.SuggestedAnswer = string.Join(", ", parts);
                            response.ParsingLogs.Add($"[Multi-TF Answer] Nhận diện ma trận đáp án: {currentQ.SuggestedAnswer}");
                            continue;
                        }
                    }
                }

                // 3b. Đáp án đúng tiêu chuẩn
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

                // 5a. Nhận diện tiểu mục Đúng / Sai đa mệnh đề GDPT 2025 (a, b, c, d...)
                bool isTfQuestion = currentQ.QuestionType == "TRUE_FALSE" || currentQ.QuestionType == "MULTI_TRUE_FALSE"
                    || Regex.IsMatch(currentQ.Content ?? "", @"(?:đúng\s*[-–\/\s]*sai|chọn\s+đúng\s+hoặc\s+sai|mệnh\s+đề\s+đúng\s+hay\s+sai|true\s*[-–\/\s]*false)", RegexOptions.IgnoreCase);

                if (isTfQuestion)
                {
                    var mSub = Regex.Match(line, @"^(?:([a-d])|[\(]([a-d])[\)])[\)\.\:\s\-]+(.*)", RegexOptions.IgnoreCase);
                    if (mSub.Success)
                    {
                        string subLetter = (!string.IsNullOrEmpty(mSub.Groups[1].Value) ? mSub.Groups[1].Value : mSub.Groups[2].Value).ToLowerInvariant();
                        string subContent = mSub.Groups[3].Value.Trim();
                        currentQ.QuestionType = "TRUE_FALSE";
                        currentQ.Options.Add($"{subLetter}) {subContent}");
                        response.ParsingLogs.Add($"[Multi-TF Sub-Item] Thêm mệnh đề {subLetter})");

                        // Trích xuất đáp án nếu có dấu hiệu Đúng / Sai kèm theo
                        var matchTf = Regex.Match(subContent, @"(?:\[|\(|\b)(đúng|sai|true|false|đ|s|t|f)(?:\]|\)|\b)", RegexOptions.IgnoreCase);
                        if (matchTf.Success)
                        {
                            string valToken = matchTf.Groups[1].Value.ToLowerInvariant();
                            string val = (valToken.StartsWith("đ") || valToken.StartsWith("d") || valToken.StartsWith("t")) ? "Đúng" : "Sai";
                            var parts = (currentQ.SuggestedAnswer ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => p.Trim())
                                .Where(p => !p.StartsWith(subLetter + ":", StringComparison.OrdinalIgnoreCase) && !p.StartsWith(subLetter + ")", StringComparison.OrdinalIgnoreCase))
                                .ToList();
                            parts.Add($"{subLetter}) {val}");
                            parts.Sort();
                            currentQ.SuggestedAnswer = string.Join(", ", parts);
                        }
                        continue;
                    }
                }

                // 5b. Phương án (Option) tiêu chuẩn A/B/C/D
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
        /// Kiểm tra dòng văn bản có phải là chỉ dẫn khảo thí, tiêu đề biểu mẫu kỳ thi hoặc thông tin thí sinh không.
        /// </summary>
        public static bool IsExamInstructionOrMeta(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            var trimmed = line.Trim();
            return Regex.IsMatch(trimmed, @"^(?:Thí\s*sinh\s*trả\s*lời|Trong\s*mỗi\s*ý|Mỗi\s*câu\s*hỏi\s*thí\s*sinh|Thời\s*gian\s*làm\s*bài|Đề\s*thi\s*gồm|Mã\s*đề\s*thi|Họ(?:\s*,|\s*và)?\s*tên\s*thí\s*sinh|Số\s*báo\s*danh)\b", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Bóc tách các phương án nằm cùng một dòng (A. ... B. ... hoặc 1. ... 2. ...) — API tĩnh dùng cho Excel/PDF pipelines.
        /// Chữ số 1-4 được quy đổi sang chữ cái A-D.
        /// </summary>
        public static List<InlineOptionMatch> ExtractInlineOptions(string text)
        {
            var options = new List<InlineOptionMatch>();
            if (string.IsNullOrWhiteSpace(text) || IsExamInstructionOrMeta(text)) return options;

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

            // Loại bỏ trường hợp chuỗi chỉ dẫn (vd: Trong mỗi ý a), b), c), d)... các phương án không có nội dung thực)
            if (options.Count(o => !string.IsNullOrWhiteSpace(o.Content) && !Regex.IsMatch(o.Content, @"^[,\.\s\)]+$")) < 2)
            {
                return new List<InlineOptionMatch>();
            }

            return options;
        }

        /// <summary>
        /// Trích xuất danh sách phương án trắc nghiệm dạng chữ hoa (A, B, C, D) từ dòng.
        /// </summary>
        public List<ParsedOptionDto> ExtractUppercaseOptionsFromLine(string line, QuestionParsingRuleDto? rule = null)
        {
            var options = new List<ParsedOptionDto>();
            if (string.IsNullOrWhiteSpace(line)) return options;

            var cigOpts = ExtractOptions(line, rule);
            if (cigOpts.Count > 0)
            {
                foreach (var opt in cigOpts)
                {
                    char rawChar = opt.Letter;
                    if (char.IsUpper(rawChar) || char.IsDigit(rawChar))
                    {
                        char letter = char.IsDigit(rawChar) ? (char)('A' + (rawChar - '1')) : rawChar;
                        options.Add(new ParsedOptionDto
                        {
                            Letter = letter,
                            Content = opt.Content,
                            IsMarkedCorrect = opt.IsMarkedCorrect
                        });
                    }
                }
                if (options.Count >= 2) return options;
                options.Clear();
            }

            bool isGplx = rule?.RuleCode == "VN_GPLX_600_STANDARD" || rule?.TargetExamType == "GPLX";
            string pattern = isGplx
                ? @"(?:^|[\s\t\–\—]+|(?<=[\.\:\)\;\!\?]))(?:<u>|\*|\*\*|\<b\>)?(\*?)((?-i:[A-H])|[1-4])(\*?)(?:</u>|\*|\*\*|\</b\>)?[\.\:\)](?:</u>|\</b\>)?[\s\t]+"
                : @"(?:^|[\s\t\–\—]+|(?<=[\.\:\)\;\!\?]))(?:<u>|\*|\*\*|\<b\>)?(\*?)(?-i:([A-H]))(\*?)(?:</u>|\*|\*\*|\</b\>)?[\.\:\)](?:</u>|\</b\>)?[\s\t]+";

            var regex = GetOrAddRegex(pattern);
            var matches = regex.Matches(line);
            if (matches.Count == 0) return options;

            if (matches.Count == 1 && char.IsDigit(matches[0].Groups[2].Value[0]) && !isGplx)
            {
                return options;
            }

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                char rawChar = match.Groups[2].Value[0];
                char letter = char.IsDigit(rawChar) ? (char)('A' + (rawChar - '1')) : char.ToUpperInvariant(rawChar);
                bool isCorrect = match.Value.Contains("<u>") || match.Value.Contains("<b>") || match.Groups[1].Value == "*" || match.Groups[3].Value == "*";

                int contentStart = match.Index + match.Length;
                int contentEnd = (i + 1 < matches.Count) ? matches[i + 1].Index : line.Length;

                string content = line.Substring(contentStart, contentEnd - contentStart).Trim();
                content = Regex.Replace(content, @"[\.\s\t]+$", "").Trim();

                options.Add(new ParsedOptionDto
                {
                    Letter = letter,
                    Content = content,
                    IsMarkedCorrect = isCorrect
                });
            }

            return options;
        }

        /// <summary>
        /// Trích xuất phương án trắc nghiệm từ một dòng văn bản.
        /// </summary>
        public List<ParsedOptionDto> ExtractOptionsFromLine(string line, QuestionParsingRuleDto? rule = null)
        {
            var options = new List<ParsedOptionDto>();
            if (string.IsNullOrWhiteSpace(line)) return options;

            var cigOpts = ExtractOptions(line, rule);
            if (cigOpts.Count > 0)
            {
                foreach (var opt in cigOpts)
                {
                    char rawChar = opt.Letter;
                    char letter = char.IsDigit(rawChar) ? (char)('A' + (rawChar - '1')) : char.ToUpperInvariant(rawChar);
                    options.Add(new ParsedOptionDto
                    {
                        Letter = letter,
                        Content = opt.Content,
                        IsMarkedCorrect = opt.IsMarkedCorrect
                    });
                }
                return options;
            }

            var regex = GetOrAddRegex(@"(?:^|[\s\t\–\—]+)(?:<u>|\*|\*\*|\<b\>)?(\*?)((?-i:[A-H]|[a-h])|[1-4])(\*?)(?:</u>|\*|\*\*|\</b\>)?[\.\:\)](?:</u>|\</b\>)?[\s\t]+");
            var matches = regex.Matches(line);

            if (matches.Count == 0) return options;

            if (matches.Count == 1 && char.IsDigit(matches[0].Groups[2].Value[0]))
            {
                bool isGplx = rule?.RuleCode == "VN_GPLX_600_STANDARD" || rule?.TargetExamType == "GPLX";
                if (!isGplx) return options;
            }

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                char rawChar = match.Groups[2].Value[0];
                char letter = char.IsDigit(rawChar) ? (char)('A' + (rawChar - '1')) : char.ToUpperInvariant(rawChar);
                bool isCorrect = match.Value.Contains("<u>") || match.Value.Contains("<b>") || match.Groups[1].Value == "*" || match.Groups[3].Value == "*";

                int contentStart = match.Index + match.Length;
                int contentEnd = (i + 1 < matches.Count) ? matches[i + 1].Index : line.Length;

                string content = line.Substring(contentStart, contentEnd - contentStart).Trim();
                content = Regex.Replace(content, @"[\.\s\t]+$", "").Trim();

                options.Add(new ParsedOptionDto
                {
                    Letter = letter,
                    Content = content,
                    IsMarkedCorrect = isCorrect
                });
            }

            return options;
        }

        /// <summary>
        /// Trích xuất phương án đúng/sai (a, b, c, d) trên nhiều dòng liên tiếp.
        /// </summary>
        public bool TryExtractTrueFalseOptions(
            List<string> lines,
            out List<ParsedOptionDto> options,
            out int startIndex)
        {
            options = new List<ParsedOptionDto>();
            startIndex = lines.Count;

            var tfSubItems = new List<ParsedOptionDto>();
            int tfStart = -1;
            char expectedLetter = 'a';
            ParsedOptionDto? currentTfOpt = null;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var matchTf = Regex.Match(line, @"^(?:<u>|\*|\*\*|\<b\>|\<strong\>|\<em\>)*[\(\[\s]*((?-i:[a-d])|[1-4])(?:\</[a-z0-9]+\>)*[\)\]\.\:\-]+(?:\</[a-z0-9]+\>)*\s*(.*)");
                if (matchTf.Success)
                {
                    char rawChar = matchTf.Groups[1].Value[0];
                    char letterChar = char.IsDigit(rawChar) ? (char)('a' + (rawChar - '1')) : char.ToLowerInvariant(rawChar);

                    if (letterChar == expectedLetter)
                    {
                        if (tfStart == -1) tfStart = i;
                        string content = matchTf.Groups[2].Value.Trim();
                        bool isCorrect = line.Contains("<u>") || line.Contains("<b>") || line.Contains("*");
                        currentTfOpt = new ParsedOptionDto
                        {
                            Letter = letterChar,
                            Content = $"{letterChar}) {content}",
                            IsMarkedCorrect = isCorrect
                        };
                        tfSubItems.Add(currentTfOpt);
                        expectedLetter = (char)(expectedLetter + 1);
                        continue;
                    }
                }

                if (currentTfOpt != null)
                {
                    currentTfOpt.Content = string.IsNullOrEmpty(currentTfOpt.Content) ? line : $"{currentTfOpt.Content}\n{line}";
                    if (line.Contains("<u>") || line.Contains("<b>") || line.Contains("*"))
                    {
                        currentTfOpt.IsMarkedCorrect = true;
                    }
                }
            }

            if (tfSubItems.Count >= 2)
            {
                options = tfSubItems;
                startIndex = tfStart;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Trích xuất phương án trắc nghiệm chữ hoa trên nhiều dòng liên tiếp.
        /// </summary>
        public bool TryExtractUppercaseMultilineOptions(
            List<string> lines,
            QuestionParsingRuleDto? rule,
            out List<ParsedOptionDto> options,
            out int startIndex)
        {
            options = new List<ParsedOptionDto>();
            startIndex = lines.Count;

            var subItems = new List<ParsedOptionDto>();
            int singleStart = -1;
            char expectedUpper = 'A';
            ParsedOptionDto? currentUpperOpt = null;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                bool isGplx = rule?.RuleCode == "VN_GPLX_600_STANDARD" || rule?.TargetExamType == "GPLX";
                var matchUpper = isGplx
                    ? Regex.Match(line, @"^(?:<u>|\*|\*\*|\<b\>|\<strong\>|\<em\>)*[\(\[\s]*((?-i:[A-H])|[1-4])(?:\</[a-z0-9]+\>)*[\)\]\.\:\-]+(?:\</[a-z0-9]+\>)*\s*(.*)")
                    : Regex.Match(line, @"^(?:<u>|\*|\*\*|\<b\>|\<strong\>|\<em\>)*[\(\[\s]*(?-i:([A-H]))(?:\</[a-z0-9]+\>)*[\)\]\.\:\-]+(?:\</[a-z0-9]+\>)*\s*(.*)");
                if (matchUpper.Success)
                {
                    char rawChar = matchUpper.Groups[1].Value[0];
                    char letterChar = char.IsDigit(rawChar) ? (char)('A' + (rawChar - '1')) : rawChar;

                    if (letterChar == expectedUpper)
                    {
                        if (singleStart == -1) singleStart = i;
                        string content = matchUpper.Groups[2].Value.Trim();
                        bool isCorrect = line.Contains("<u>") || line.Contains("<b>") || line.Contains("*");
                        currentUpperOpt = new ParsedOptionDto
                        {
                            Letter = letterChar,
                            Content = content,
                            IsMarkedCorrect = isCorrect
                        };
                        subItems.Add(currentUpperOpt);
                        expectedUpper = (char)(expectedUpper + 1);
                        continue;
                    }
                }

                if (currentUpperOpt != null)
                {
                    currentUpperOpt.Content = string.IsNullOrEmpty(currentUpperOpt.Content) ? line : $"{currentUpperOpt.Content}\n{line}";
                    if (line.Contains("<u>") || line.Contains("<b>") || line.Contains("*"))
                    {
                        currentUpperOpt.IsMarkedCorrect = true;
                    }
                }
            }

            if (subItems.Count >= 2 && subItems[0].Letter == 'A')
            {
                options = subItems;
                startIndex = singleStart;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Trích xuất phương án trắc nghiệm từ dưới lên (Bottom-Up) kẹp giữa câu hỏi.
        /// </summary>
        public bool TryExtractOptionsBottomUp(
            List<string> lines,
            string currentSection,
            QuestionParsingRuleDto? rule,
            out List<ParsedOptionDto> options,
            out int optionStartIndex,
            out string questionType)
        {
            options = new List<ParsedOptionDto>();
            optionStartIndex = lines.Count;
            questionType = currentSection;

            if (lines.Count == 0) return false;

            bool isTfSection = HasTypeCapability(currentSection, QuestionTypeProfileDto.CapTrueFalseFamily);
            bool hasExplicitTfPrompt = lines.Any(l => Regex.IsMatch(l, @"(?:đúng\s*[-–\/\s]*sai|chọn\s+đúng\s+hoặc\s+sai|mệnh\s+đề\s+đúng\s+hay\s+sai|true\s*[-–\/\s]*false)", RegexOptions.IgnoreCase));

            string combinedQuestionText = string.Join("\n", lines);
            bool isEssayQuestion = currentSection == "ESSAY" || IsEssayQuestion(combinedQuestionText, rule);

            if (isEssayQuestion)
            {
                questionType = "ESSAY";
            }

            // 1. Ưu tiên Section Đúng/Sai nếu có chỉ thị rõ ràng
            if (isTfSection || (hasExplicitTfPrompt && !isEssayQuestion))
            {
                if (TryExtractTrueFalseOptions(lines, out var tfOptions, out int tfStartIdx))
                {
                    options = tfOptions;
                    optionStartIndex = tfStartIdx;
                    questionType = "MULTI_TRUE_FALSE";
                    return true;
                }
            }

            // 2. Phương án in HOA cùng dòng (Inline Options A., B., C., D.)
            int inlineStart = lines.Count;
            var inlineOpts = new List<ParsedOptionDto>();
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (inlineOpts.Count == 0 && (string.IsNullOrWhiteSpace(lines[i]) || lines[i].StartsWith("![") || lines[i].Contains("data:image/")))
                {
                    continue; // Bỏ qua ảnh hoặc dòng trống ở đuôi câu hỏi
                }
                var lineOpts = ExtractUppercaseOptionsFromLine(lines[i], rule);
                if (lineOpts.Count >= 2)
                {
                    inlineOpts.InsertRange(0, lineOpts);
                    inlineStart = i;
                    if (lineOpts[0].Letter == 'A') break;
                }
                else if (lineOpts.Count == 1 && inlineOpts.Count > 0)
                {
                    inlineOpts.InsertRange(0, lineOpts);
                    inlineStart = i;
                    if (lineOpts[0].Letter == 'A') break;
                }
                else
                {
                    break;
                }
            }

            bool inlineOptsValid = isEssayQuestion
                ? (inlineOpts.Count >= 3 && inlineOpts[0].Letter == 'A')
                : (inlineOpts.Count >= 2 && (inlineOpts[0].Letter == 'A' || inlineOpts[0].Letter == '1'));

            if (inlineOptsValid)
            {
                options = inlineOpts;
                optionStartIndex = inlineStart;
                questionType = "SINGLE";
                return true;
            }

            // 3. Phương án in HOA nhiều dòng (Multiline Options A., B., C., D.)
            if (TryExtractUppercaseMultilineOptions(lines, rule, out var multilineOpts, out int multilineStart))
            {
                if (!isEssayQuestion || multilineOpts.Count >= 3)
                {
                    options = multilineOpts;
                    optionStartIndex = multilineStart;
                    questionType = "SINGLE";
                    return true;
                }
            }

            // 4. Phương án chữ thường cá biệt (a., b., c., d.) khi không có chữ hoa
            if (currentSection == "SINGLE" && !isEssayQuestion)
            {
                var lowerOpts = new List<ParsedOptionDto>();
                int lowerStart = lines.Count;
                char expectedLower = '\0';
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    var match = Regex.Match(lines[i], @"^(\*?)(?:([a-d])|[\(]([a-d])[\)])[\.\:\)][\s\t]+(.*)", RegexOptions.None);
                    if (match.Success)
                    {
                        char ltr = (!string.IsNullOrEmpty(match.Groups[2].Value) ? match.Groups[2].Value : match.Groups[3].Value)[0];
                        bool isCorrect = match.Groups[1].Value == "*";
                        string cnt = match.Groups[4].Value.Trim();

                        if (expectedLower == '\0')
                        {
                            if (ltr >= 'b' && ltr <= 'd')
                            {
                                expectedLower = (char)(ltr - 1);
                                lowerOpts.Insert(0, new ParsedOptionDto { Letter = char.ToUpperInvariant(ltr), Content = cnt, IsMarkedCorrect = isCorrect });
                                lowerStart = i;
                            }
                            else break;
                        }
                        else if (ltr == expectedLower)
                        {
                            lowerOpts.Insert(0, new ParsedOptionDto { Letter = char.ToUpperInvariant(ltr), Content = cnt, IsMarkedCorrect = isCorrect });
                            lowerStart = i;
                            if (ltr == 'a') break;
                            expectedLower = (char)(expectedLower - 1);
                        }
                        else break;
                    }
                    else break;
                }

                if (lowerOpts.Count >= 2 && lowerOpts[0].Letter == 'A')
                {
                    bool hasMoreItemsAbove = lowerStart > 0 && Regex.IsMatch(lines[lowerStart - 1], @"^[a-z][\.\)]", RegexOptions.IgnoreCase);
                    if (!hasMoreItemsAbove)
                    {
                        options = lowerOpts;
                        optionStartIndex = lowerStart;
                        questionType = "SINGLE";
                        return true;
                    }
                }
            }

            // 5. Fallback Đúng/Sai nếu Section chưa xác định (và không phải SINGLE rõ ràng)
            if (!isTfSection && !hasExplicitTfPrompt && !isEssayQuestion && currentSection != "SINGLE")
            {
                if (TryExtractTrueFalseOptions(lines, out var fallbackTfOpts, out int fallbackTfStart))
                {
                    options = fallbackTfOpts;
                    optionStartIndex = fallbackTfStart;
                    questionType = "MULTI_TRUE_FALSE";
                    return true;
                }
            }

            return false;
        }

        /// <summary>Gắn dòng giải thích vào câu hỏi đang xử lý.</summary>
        public void AppendExplanationLine(DocxImportPreviewDto q, string line, QuestionParsingRuleDto? rule = null)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            if (TryMatchLinguisticPattern(LinguisticKeys.BareExplanationHeader, line, out _))
                return;

            if (TryMatchEndOfExam(line) || IsProctorDisclaimerLine(line))
                return;

            if (string.IsNullOrWhiteSpace(q.AiExplanation))
            {
                q.AiExplanation = line;
            }
            else
            {
                q.AiExplanation += "\n" + line;
            }
        }

        /// <summary>Cập nhật đáp án đúng sai cho từng ý con.</summary>
        public void UpdateTrueFalseAnswer(DocxImportPreviewDto q, string itemLetter, string val)
        {
            var validLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "b", "c", "d" };
            if (!validLetters.Contains(itemLetter)) return;

            var parts = (q.SuggestedAnswer ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => {
                    var m = Regex.Match(p, @"^([a-d])[\:\)]", RegexOptions.IgnoreCase);
                    return m.Success && !m.Groups[1].Value.Equals(itemLetter, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
            parts.Add($"{itemLetter.ToLowerInvariant()}) {val}");
            parts.Sort();
            q.SuggestedAnswer = string.Join(", ", parts);

            var sub = q.SubItems.FirstOrDefault(s => string.Equals(s.Key, itemLetter, StringComparison.OrdinalIgnoreCase));
            if (sub == null)
            {
                sub = new QuestionSubItemDto
                {
                    Key = itemLetter.ToLowerInvariant(),
                    Content = $"Mệnh đề {itemLetter})",
                    ExpectedAnswer = val,
                    OrderIndex = q.SubItems.Count + 1
                };
                q.SubItems.Add(sub);
            }
            else
            {
                sub.ExpectedAnswer = val;
            }
        }

        /// <summary>Gắn lời giải chi tiết cho từng ý con.</summary>
        public void AppendSubItemExplanation(DocxImportPreviewDto q, string subKey, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var sub = q.SubItems.FirstOrDefault(s => string.Equals(s.Key, subKey, StringComparison.OrdinalIgnoreCase));
            if (sub == null)
            {
                sub = new QuestionSubItemDto
                {
                    Key = subKey.ToLowerInvariant(),
                    Content = $"Mệnh đề {subKey})",
                    Explanation = text,
                    OrderIndex = q.SubItems.Count + 1
                };
                q.SubItems.Add(sub);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(sub.Explanation))
                {
                    sub.Explanation = text;
                }
                else
                {
                    sub.Explanation += "\n" + text;
                }
            }
        }

        /// <summary>Thử trích xuất đáp án từ dòng văn bản.</summary>
        public void TryExtractAnswer(DocxImportPreviewDto q, string line, QuestionParsingRuleDto? rule = null)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            if (TryExtractInlineAnswerStructured(line, q.QuestionType, rule, null, out var res))
            {
                if (HasTypeCapability(q.QuestionType, QuestionTypeProfileDto.CapTrueFalseFamily))
                {
                    if (!string.IsNullOrEmpty(res.SubItemKey))
                    {
                        UpdateTrueFalseAnswer(q, res.SubItemKey, !string.IsNullOrWhiteSpace(res.SubItemValue) ? res.SubItemValue : res.AnswerValue);
                        return;
                    }
                }

                if (HasTypeCapability(q.QuestionType, QuestionTypeProfileDto.CapSingleAnswer) && !string.IsNullOrWhiteSpace(res.AnswerValue))
                {
                    string trimmed = res.AnswerValue.Trim();
                    var matchLet = Regex.Match(trimmed, @"\b([A-G])\b", RegexOptions.IgnoreCase);
                    if (matchLet.Success)
                    {
                        q.SuggestedAnswer = (char.ToUpperInvariant(matchLet.Value[0]) - 'A' + 1).ToString();
                        return;
                    }
                    if (int.TryParse(trimmed, out int numVal) && numVal >= 1 && numVal <= 7)
                    {
                        q.SuggestedAnswer = numVal.ToString();
                        return;
                    }
                }

                q.SuggestedAnswer = res.AnswerValue;
            }
        }

        /// <summary>Chuẩn hóa khóa phân đoạn (SINGLE, TRUE_FALSE, SHORT_ANSWER, ESSAY...).</summary>
        public string NormalizeSectionKey(string? rawSec, string? questionType = null)
        {
            if (!string.IsNullOrWhiteSpace(rawSec))
            {
                var resolved = TryResolveSectionKeyByAlias(rawSec);
                if (resolved != null) return resolved;
            }
            if (!string.IsNullOrWhiteSpace(questionType))
            {
                var resolved = TryResolveSectionKeyByAlias(questionType);
                if (resolved != null) return resolved;
            }
            return "SINGLE";
        }

        /// <summary>Gỡ các dòng HẾT / chỉ thị coi thi còn sót ở đuôi nội dung.</summary>
        public string StripTrailingExamNoise(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            var lines = content.Replace("\r\n", "\n").Split('\n').ToList();
            while (lines.Count > 0)
            {
                var last = lines[^1];
                if (string.IsNullOrWhiteSpace(last) || TryMatchEndOfExam(last) || IsProctorDisclaimerLine(last))
                    lines.RemoveAt(lines.Count - 1);
                else break;
            }
            return string.Join("\n", lines).Trim();
        }

        /// <summary>Phân giải khối câu hỏi và các phương án từ dưới lên (Bottom-Up Resolution).</summary>
        public void ResolveQuestionBlockBottomUp(
            DocxImportPreviewDto currentQuestion,
            List<string> rawLines,
            string currentSection,
            QuestionParsingRuleDto? matchedRule)
        {
            var lines = new List<string>(rawLines.Where(l => !string.IsNullOrWhiteSpace(l)));
            while (lines.Count > 0)
            {
                var bottom = lines[^1].Trim();
                if (TryMatchEndOfExam(bottom) || IsProctorDisclaimerLine(bottom))
                {
                    lines.RemoveAt(lines.Count - 1);
                    continue;
                }
                break;
            }
            if (lines.Count == 0) return;

            if (currentQuestion.Options.Count > 0)
            {
                while (lines.Count > 0)
                {
                    var lastLine = lines[^1].Trim();
                    if (IsAnswerLine(lastLine, matchedRule, out string ansToken))
                    {
                        currentQuestion.SuggestedAnswer = ansToken;
                        lines.RemoveAt(lines.Count - 1);
                        continue;
                    }
                    if (IsExplanationLine(lastLine, matchedRule))
                    {
                        AppendExplanationLine(currentQuestion, lastLine, matchedRule);
                        lines.RemoveAt(lines.Count - 1);
                        continue;
                    }
                    break;
                }
                foreach (var line in lines)
                {
                    currentQuestion.Content += "\n" + line;
                }
                return;
            }

            // 1. Tách dòng Lời giải hoặc Đáp án ở cuối
            int expStart = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (IsExplanationLine(lines[i], matchedRule))
                {
                    expStart = i;
                    break;
                }
            }
            if (expStart >= 0)
            {
                var expLines = lines.Skip(expStart).ToList();
                foreach (var exp in expLines)
                {
                    AppendExplanationLine(currentQuestion, exp, matchedRule);
                    TryExtractAnswer(currentQuestion, exp, matchedRule);
                }
                lines = lines.Take(expStart).ToList();
            }

            while (lines.Count > 0)
            {
                var lastLine = lines[^1].Trim();
                if (IsAnswerLine(lastLine, matchedRule, out string ansToken))
                {
                    if (ansToken.Length == 1 && char.IsLetter(ansToken[0]))
                    {
                        int optIndex = char.ToUpperInvariant(ansToken[0]) - 'A' + 1;
                        currentQuestion.SuggestedAnswer = optIndex.ToString();
                    }
                    else
                    {
                        currentQuestion.SuggestedAnswer = ansToken;
                    }
                    lines.RemoveAt(lines.Count - 1);
                    continue;
                }

                if (TryMatchLinguisticPattern(LinguisticKeys.InlineExplanationNote, lastLine, out var matchExp) && matchExp.Success)
                {
                    currentQuestion.AiExplanation = matchExp.Groups[1].Value.Trim();
                    lines.RemoveAt(lines.Count - 1);
                    continue;
                }

                if (IsExplanationLine(lastLine, matchedRule))
                {
                    AppendExplanationLine(currentQuestion, lastLine, matchedRule);
                    lines.RemoveAt(lines.Count - 1);
                    continue;
                }

                break;
            }

            // 2. Trích xuất phương án trắc nghiệm Bottom-Up
            bool isShortOrEssay = HasTypeCapability(currentSection, QuestionTypeProfileDto.CapFreeText)
                                  || HasTypeCapability(currentQuestion.QuestionType, QuestionTypeProfileDto.CapFreeText)
                                  || (currentQuestion.SourceSectionName?.Contains("III") == true);

            string detectedType = null;
            if (!isShortOrEssay && TryExtractOptionsBottomUp(lines, currentSection, matchedRule, out var extractedOptions, out int optStartIdx, out detectedType))
            {
                currentQuestion.QuestionType = detectedType;
                foreach (var opt in extractedOptions)
                {
                    currentQuestion.Options.Add(opt.Content);
                    if (opt.IsMarkedCorrect && string.IsNullOrEmpty(currentQuestion.SuggestedAnswer))
                    {
                        currentQuestion.SuggestedAnswer = currentQuestion.Options.Count.ToString();
                    }
                    if (opt.Content.Contains("data:image/"))
                    {
                        currentQuestion.OptionType = "image";
                    }
                }

                lines = lines.Take(optStartIdx).ToList();
            }
            else
            {
                if (detectedType == "ESSAY")
                {
                    currentQuestion.QuestionType = "ESSAY";
                }
            }

            // 3. Toàn bộ dòng còn lại là thân câu hỏi
            var contentBuilder = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(currentQuestion.Content))
            {
                contentBuilder.Append(currentQuestion.Content);
            }

            foreach (var line in lines)
            {
                if (line.StartsWith("data:image/") || line.StartsWith("![Hình vẽ]") || line.Contains("data:image/"))
                {
                    if (!contentBuilder.ToString().Contains("data:image/"))
                    {
                        contentBuilder.Append("\n\n" + (line.StartsWith("![") ? line : $"![Hình minh họa]({line})"));
                        currentQuestion.ContentType = "image";
                    }
                    else
                    {
                        contentBuilder.Append("\n\n" + line);
                    }
                }
                else
                {
                    if (contentBuilder.Length > 0) contentBuilder.Append("\n");
                    contentBuilder.Append(line);
                }
            }

            currentQuestion.Content = contentBuilder.ToString().Trim();
            currentQuestion.Content = StripTrailingExamNoise(currentQuestion.Content);

            if (string.IsNullOrWhiteSpace(currentQuestion.Content))
            {
                currentQuestion.Content = !string.IsNullOrWhiteSpace(currentQuestion.SourceSectionName) && currentQuestion.SourceSectionName != "SINGLE" && currentQuestion.SourceSectionName != "MULTI"
                    ? currentQuestion.SourceSectionName
                    : (!string.IsNullOrWhiteSpace(currentQuestion.ContextContent) && currentQuestion.ContextContent.Length <= 250
                        ? currentQuestion.ContextContent
                        : $"Chọn phương án đúng nhất điền vào vị trí ({currentQuestion.SourceQuestionNo}):");
            }

            if (IsCriticalQuestion(currentQuestion.Content, matchedRule))
            {
                currentQuestion.IsCritical = true;
                currentQuestion.Difficulty = 5;
            }

            var subCat = DetectSubCategory(currentQuestion.Content, matchedRule);
            if (!string.IsNullOrEmpty(subCat))
            {
                currentQuestion.SubCategory = subCat;
            }
        }

        /// <summary>
        /// Danh sách các phân đoạn tương đương khi tra cứu theo SectionKey.
        /// </summary>
        public List<string> GetEquivalentSections(string section)
        {
            return GetEquivalentTypes(section ?? "SINGLE").ToList();
        }

        /// <summary>
        /// Tra cứu câu hỏi 3 tầng theo phân cấp (Map -> Danh sách -> Fallback an toàn).
        /// </summary>
        public DocxImportPreviewDto? FindQuestionByHierarchy(
            Dictionary<string, DocxImportPreviewDto> questionMap,
            List<DocxImportPreviewDto> results,
            string examCode,
            string section,
            int qNum)
        {
            string normSec = NormalizeSectionKey(section);
            var candidateSections = GetEquivalentSections(normSec);
            if (!candidateSections.Contains(normSec)) candidateSections.Insert(0, normSec);
            if (!string.IsNullOrWhiteSpace(section) && !candidateSections.Contains(section)) candidateSections.Add(section);

            // 1. Tra cứu theo questionMap với các candidate sections
            foreach (var sec in candidateSections)
            {
                string key = $"{examCode}_{sec}_{qNum}";
                if (questionMap.TryGetValue(key, out var found))
                {
                    return found;
                }
            }

            // 2. Tra cứu trong danh sách kết quả đã bóc tách
            var matchedInResults = results.FirstOrDefault(r =>
                (string.Equals(r.SourceExamName, examCode, StringComparison.OrdinalIgnoreCase) || examCode == "DEFAULT") &&
                candidateSections.Any(s => string.Equals(NormalizeSectionKey(r.SourceSectionName), s, StringComparison.OrdinalIgnoreCase) 
                                        || string.Equals(NormalizeSectionKey(r.QuestionType), s, StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(r.SourceSectionName, s, StringComparison.OrdinalIgnoreCase)) &&
                r.SourceQuestionNo == qNum.ToString());

            if (matchedInResults != null)
            {
                return matchedInResults;
            }

            // 3. Fallback an toàn: CHỈ KHI toàn bộ đề này có DUY NHẤT một câu mang số qNum đó
            var allWithQNum = results.Where(r =>
                (string.Equals(r.SourceExamName, examCode, StringComparison.OrdinalIgnoreCase) || examCode == "DEFAULT") &&
                r.SourceQuestionNo == qNum.ToString()).ToList();

            if (allWithQNum.Count == 1)
            {
                return allWithQNum[0];
            }

            return null;
        }

        /// <summary>
        /// Đóng gói và làm giàu câu hỏi hoàn chỉnh (Tự luận, Trắc nghiệm, Độ khó, Mã băm, Tọa độ).
        /// </summary>
        public void FinalizeQuestion(
            DocxImportPreviewDto q, 
            DynamicDomainContext? ctx = null,
            QuestionParsingRuleDto? rule = null)
        {
            if (q.TopicCode == "GENERAL")
            {
                q.TopicCode = UniversalCoordinateTaxonomyEngine.InferTopicFromContent(q.Content, ctx);
            }

            if (q.Options.Any(o => o.Contains("data:image/")))
            {
                q.OptionType = "image";
            }

            // [Tự Luận & Barem Điểm Safeguard - 100% CIG Data-Driven]
            bool isEssay = q.QuestionType == "ESSAY" || IsEssayQuestion(q.Content ?? "", rule, q.TopicCode);

            if (isEssay)
            {
                if (q.Options.Count > 0 && (q.Options.Count < 4 || !q.Options.Any(o => Regex.IsMatch(o, @"^[A-D]\.\s*"))))
                {
                    foreach (var opt in q.Options)
                    {
                        q.Content += "\n" + opt;
                    }
                    q.Options.Clear();
                }

                if (q.Options.Count == 0 || HasTypeCapability(q.QuestionType, QuestionTypeProfileDto.CapSingleAnswer) || HasTypeCapability(q.QuestionType, QuestionTypeProfileDto.CapNumericAnswer))
                {
                    q.QuestionType = "ESSAY";
                    q.Difficulty = Math.Max(q.Difficulty, 4);
                    if (string.IsNullOrWhiteSpace(q.SuggestedAnswer) || q.SuggestedAnswer.Contains("Điền đáp số"))
                    {
                        q.SuggestedAnswer = "Tự luận chấm theo barem điểm từng bước.";
                    }
                }
            }
            else if (HasTypeCapability(q.QuestionType, QuestionTypeProfileDto.CapNumericAnswer))
            {
                q.Difficulty = Math.Max(q.Difficulty, 4);
                if (string.IsNullOrWhiteSpace(q.SuggestedAnswer))
                {
                    q.SuggestedAnswer = "Điền đáp số / giá trị số.";
                }
            }
            else if (HasTypeCapability(q.QuestionType, QuestionTypeProfileDto.CapTrueFalseFamily))
            {
                q.Difficulty = Math.Max(q.Difficulty, 3);

                if (q.SubItems.Count == 0 && q.Options.Count > 0)
                {
                    for (int i = 0; i < q.Options.Count; i++)
                    {
                        var optText = q.Options[i];
                        var matchSub = Regex.Match(optText, @"^(?:<u>|\*|\*\*|\<b\>)?[\(\s]*([a-d1-4])[\)\.\:]\s*(?:</u>|\*|\*\*|\</b\>)?(.*)", RegexOptions.IgnoreCase);
                        string key = matchSub.Success ? matchSub.Groups[1].Value.ToLowerInvariant() : ((char)('a' + i)).ToString();
                        string content = matchSub.Success ? matchSub.Groups[2].Value.Trim() : optText;
                        q.SubItems.Add(new QuestionSubItemDto
                        {
                            Key = key,
                            Content = content,
                            OrderIndex = i + 1
                        });
                    }
                }

                if (!string.IsNullOrWhiteSpace(q.SuggestedAnswer) && q.SubItems.Count > 0)
                {
                    var subMatches = Regex.Matches(q.SuggestedAnswer, @"([a-d])[\:\)]\s*(Đúng|Sai|True|False)", RegexOptions.IgnoreCase);
                    foreach (Match m in subMatches)
                    {
                        string sKey = m.Groups[1].Value.ToLowerInvariant();
                        string sVal = m.Groups[2].Value.StartsWith("đ", StringComparison.OrdinalIgnoreCase) || m.Groups[2].Value.StartsWith("d", StringComparison.OrdinalIgnoreCase) ? "Đúng" : "Sai";
                        var sub = q.SubItems.FirstOrDefault(s => string.Equals(s.Key, sKey, StringComparison.OrdinalIgnoreCase));
                        if (sub != null && string.IsNullOrEmpty(sub.ExpectedAnswer))
                        {
                            sub.ExpectedAnswer = sVal;
                        }
                    }
                }
            }
            else if (HasTypeCapability(q.QuestionType, QuestionTypeProfileDto.CapSingleAnswer) && q.Options.Count <= 1)
            {
                if (q.Options.Count == 1)
                {
                    q.Content = string.IsNullOrWhiteSpace(q.Content) ? q.Options[0] : $"{q.Content}\n{q.Options[0]}";
                    q.Options.Clear();
                }

                if (IsEssayQuestion(q.Content ?? "", rule, q.TopicCode))
                {
                    q.QuestionType = "ESSAY";
                    q.Difficulty = Math.Max(q.Difficulty, 4);
                    if (string.IsNullOrWhiteSpace(q.SuggestedAnswer) || q.SuggestedAnswer.Contains("Điền đáp số"))
                    {
                        q.SuggestedAnswer = "Tự luận chấm theo barem điểm từng bước.";
                    }
                }
                else
                {
                    q.QuestionType = "SHORT_ANSWER";
                    q.Difficulty = Math.Max(q.Difficulty, 4);
                    if (string.IsNullOrWhiteSpace(q.SuggestedAnswer))
                    {
                        q.SuggestedAnswer = "Điền đáp số / giá trị số.";
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(q.CanonicalContentHash) && !string.IsNullOrWhiteSpace(q.Content))
            {
                q.CanonicalContentHash = ExamDeduplicationEngine.GenerateCanonicalFingerprint(q.Content, q.Options);
            }

            UniversalCoordinateTaxonomyEngine.AutoEnrichTaxonomyAndTags(q, ctx);

            bool isAcademicTopic = UniversalCoordinateTaxonomyEngine.IsAcademicTopic(q.TopicCode, ctx);
            if (isAcademicTopic)
            {
                q.DomainCode = "EDUCATION";
            }
            else if (string.IsNullOrWhiteSpace(q.DomainCode) || q.DomainCode == "GENERAL")
            {
                q.DomainCode = !string.IsNullOrWhiteSpace(q.TopicCode) && q.TopicCode != "GENERAL" ? q.TopicCode : "GENERAL";
            }

            if (string.IsNullOrWhiteSpace(q.TopicName) || q.TopicName == "GENERAL" || q.TopicName == "EDUCATION")
            {
                q.TopicName = ctx?.Topics?.FirstOrDefault(t => t.Code.Equals(q.TopicCode, StringComparison.OrdinalIgnoreCase))?.Name 
                              ?? DocumentStructureDictionaryDto.GetDefaultTopicTaxonomy()
                                  .FirstOrDefault(t => t.TopicCode.Equals(q.TopicCode, StringComparison.OrdinalIgnoreCase))?.TopicName;
            }

            if (string.IsNullOrWhiteSpace(q.OrgUnitName) && !string.IsNullOrWhiteSpace(q.IssuingOrg) && q.IssuingOrg.Contains("Trường", StringComparison.OrdinalIgnoreCase))
            {
                q.OrgUnitName = q.IssuingOrg;
            }

            if (string.IsNullOrWhiteSpace(q.CanonicalExamUrn) && !string.IsNullOrWhiteSpace(q.SourceExamName))
            {
                q.CanonicalExamUrn = $"urn:aegis:exam:{q.DomainCode?.ToLowerInvariant() ?? "general"}:{UniversalCoordinateTaxonomyEngine.NormalizeToCodeSlug(q.TopicCode).ToLowerInvariant()}:{UniversalCoordinateTaxonomyEngine.NormalizeToCodeSlug(q.SourceExamName).ToLowerInvariant()}:q{(q.SourceQuestionNo ?? "0")}";
            }
        }

        /// <summary>
        /// Đóng gói và hợp nhất câu hỏi vào bộ nhớ đệm đề thi (tránh trùng lặp / merge lời giải chi tiết).
        /// </summary>
        public void FinalizeAndMergeQuestion(
            List<DocxImportPreviewDto> results,
            Dictionary<string, DocxImportPreviewDto> questionMap,
            DocxImportPreviewDto q,
            string examCode,
            int qNum,
            DynamicDomainContext? ctx = null,
            QuestionParsingRuleDto? rule = null)
        {
            FinalizeQuestion(q, ctx, rule);

            string secKey = NormalizeSectionKey(q.SourceSectionName, q.QuestionType);
            string key = $"{examCode}_{secKey}_{qNum}";

            if (questionMap.TryGetValue(key, out var existing))
            {
                if (string.IsNullOrWhiteSpace(existing.AiExplanation) && !string.IsNullOrWhiteSpace(q.AiExplanation))
                {
                    existing.AiExplanation = q.AiExplanation;
                }
                else if (!string.IsNullOrWhiteSpace(q.AiExplanation) && q.AiExplanation.Length > existing.AiExplanation.Length)
                {
                    existing.AiExplanation = q.AiExplanation;
                }

                if ((string.IsNullOrWhiteSpace(existing.SuggestedAnswer) || existing.SuggestedAnswer.Contains("Điền đáp số"))
                    && !string.IsNullOrWhiteSpace(q.SuggestedAnswer))
                {
                    existing.SuggestedAnswer = q.SuggestedAnswer;
                }

                if (existing.SubItems.Count > 0 && q.SubItems.Count > 0)
                {
                    foreach (var sub in q.SubItems)
                    {
                        var exSub = existing.SubItems.FirstOrDefault(s => string.Equals(s.Key, sub.Key, StringComparison.OrdinalIgnoreCase));
                        if (exSub != null)
                        {
                            if (string.IsNullOrWhiteSpace(exSub.ExpectedAnswer) && !string.IsNullOrWhiteSpace(sub.ExpectedAnswer))
                            {
                                exSub.ExpectedAnswer = sub.ExpectedAnswer;
                            }
                            if (string.IsNullOrWhiteSpace(exSub.Explanation) && !string.IsNullOrWhiteSpace(sub.Explanation))
                            {
                                exSub.Explanation = sub.Explanation;
                            }
                        }
                    }
                }
                return;
            }

            questionMap[key] = q;
            results.Add(q);
        }
    }
}
