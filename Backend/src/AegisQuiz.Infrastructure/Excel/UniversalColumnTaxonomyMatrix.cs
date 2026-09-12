using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AegisQuiz.Infrastructure.Excel
{
    /// <summary>
    /// [UCTE v4.0] Universal Column Taxonomy Matrix
    /// Bảng quy tắc ánh xạ tên cột nhận thức toàn cầu đa ngành, đa ngôn ngữ, hỗ trợ tự làm giàu (Self-Enriching).
    /// </summary>
    public class ColumnTaxonomyRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Trường đích của hệ thống: ANSWER_KEY, OPTION, QUESTION_CONTENT, EXPLANATION, TOPIC_CODE, TOPIC_NAME, STT, DIFFICULTY, CRITICAL, TYPE, CONTEXT, DURATION, SUB_CATEGORY
        /// </summary>
        public string TargetField { get; set; } = string.Empty;

        /// <summary>
        /// Độ ưu tiên khớp (10: Cao nhất -> 1: Thấp nhất). Giúp ANSWER_KEY luôn nhận trước OPTION.
        /// </summary>
        public int Priority { get; set; } = 5;

        /// <summary>
        /// Điểm số tin cậy cộng thêm khi khớp quy tắc này.
        /// </summary>
        public int ConfidenceScore { get; set; } = 3;

        /// <summary>
        /// Mẫu Regex nhận diện tích cực.
        /// </summary>
        public string PositivePattern { get; set; } = string.Empty;

        /// <summary>
        /// Mẫu Regex loại trừ (nếu khớp mẫu này thì hủy nhận diện để tránh false positive).
        /// </summary>
        public string? NegativePattern { get; set; }

        /// <summary>
        /// Phạm vi ngành nghề: "ALL", "BANKING", "HEALTHCARE", "AVIATION", "K12_GDPT2025", "LEGAL"
        /// </summary>
        public string DomainScope { get; set; } = "ALL";

        /// <summary>
        /// Mã ngôn ngữ: "any", "vi", "en", "zh", "ja", "fr"
        /// </summary>
        public string LanguageCode { get; set; } = "any";

        /// <summary>
        /// Cờ nhận biết quy tắc do hệ thống tự làm giàu khi quét file hoặc do người dùng gán trên UI
        /// </summary>
        public bool IsSelfEnriched { get; set; } = false;

        /// <summary>
        /// Số lần quy tắc đã khớp thành công
        /// </summary>
        public int HitCount { get; set; } = 0;

        /// <summary>
        /// Đối tượng Regex biên dịch sẵn tối ưu tốc độ
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public Regex? CompiledRegex { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public Regex? CompiledNegativeRegex { get; set; }
    }

    public class ColumnMatchResult
    {
        public string TargetField { get; set; } = "UNKNOWN";
        public int OptionIndex { get; set; } = -1; // 1..8 nếu là OPTION
        public int Confidence { get; set; } = 0;
        public ColumnTaxonomyRule? MatchedRule { get; set; }
    }

    public class UniversalColumnTaxonomyMatrix
    {
        private static readonly Lazy<UniversalColumnTaxonomyMatrix> _instance =
            new(() => new UniversalColumnTaxonomyMatrix());

        public static UniversalColumnTaxonomyMatrix Instance => _instance.Value;

        private readonly ConcurrentBag<ColumnTaxonomyRule> _rules = new();

        public UniversalColumnTaxonomyMatrix()
        {
            SeedDefaultGlobalRules();
        }

        public IReadOnlyCollection<ColumnTaxonomyRule> Rules => _rules.ToArray();

        /// <summary>
        /// Khớp tiêu đề cột với ma trận phân loại toàn cầu
        /// </summary>
        public ColumnMatchResult MatchColumn(string rawHeader, string domain = "ALL", string lang = "any")
        {
            if (string.IsNullOrWhiteSpace(rawHeader))
                return new ColumnMatchResult();

            var normalized = NormalizeText(rawHeader);

            // Duyệt theo thứ tự Priority giảm dần
            var candidates = _rules
                .Where(r => (r.DomainScope == "ALL" || domain == "ALL" || r.DomainScope.Equals(domain, StringComparison.OrdinalIgnoreCase))
                         && (r.LanguageCode == "any" || lang == "any" || r.LanguageCode.Equals(lang, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.ConfidenceScore);

            foreach (var rule in candidates)
            {
                if (rule.CompiledRegex == null)
                {
                    try
                    {
                        rule.CompiledRegex = new Regex(rule.PositivePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(rule.NegativePattern) && rule.CompiledNegativeRegex == null)
                {
                    try
                    {
                        rule.CompiledNegativeRegex = new Regex(rule.NegativePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    }
                    catch
                    {
                        // Bỏ qua lỗi negative regex
                    }
                }

                // Kiểm tra Negative Regex trước
                if (rule.CompiledNegativeRegex != null && rule.CompiledNegativeRegex.IsMatch(normalized))
                {
                    continue;
                }

                var match = rule.CompiledRegex.IsMatch(normalized);
                if (match)
                {
                    rule.HitCount++;

                    int optIdx = -1;
                    if (rule.TargetField == "OPTION")
                    {
                        var m = rule.CompiledRegex.Match(normalized);
                        if (m.Groups.Count > 1)
                        {
                            var token = m.Groups[m.Groups.Count - 1].Value.Trim();
                            if (int.TryParse(token, out int n)) optIdx = n;
                            else if (!string.IsNullOrEmpty(token) && char.IsLetter(token[0]))
                            {
                                optIdx = char.ToUpperInvariant(token[0]) - 'A' + 1;
                            }
                        }
                    }

                    return new ColumnMatchResult
                    {
                        TargetField = rule.TargetField,
                        OptionIndex = optIdx,
                        Confidence = rule.ConfidenceScore,
                        MatchedRule = rule
                    };
                }
            }

            return new ColumnMatchResult { TargetField = "UNKNOWN", Confidence = 0 };
        }

        /// <summary>
        /// Cơ chế Tự Động Làm Giàu Tri Thức (Self-Enrichment Loop)
        /// Khi người dùng ánh xạ cột mới trên UI hoặc hệ thống phát hiện từ khóa lạ có độ tin cậy cao
        /// </summary>
        public bool EnrichRule(string targetField, string rawHeader, string domain = "ALL", string lang = "any")
        {
            if (string.IsNullOrWhiteSpace(rawHeader) || string.IsNullOrWhiteSpace(targetField))
                return false;

            var normalized = NormalizeText(rawHeader);
            if (string.IsNullOrWhiteSpace(normalized) || normalized.Length < 2)
                return false;

            // Kiểm tra xem đã có rule nào khớp chưa
            var existingMatch = MatchColumn(rawHeader, domain, lang);
            if (existingMatch.TargetField == targetField)
                return false; // Đã nhận diện đúng, không cần làm giàu thêm

            // Sinh Regex linh hoạt từ chuỗi header
            var tokens = normalized.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var patternBody = string.Join(@"\s*", tokens.Select(Regex.Escape));
            var positivePattern = $@"\b{patternBody}\b";

            int priority = targetField switch
            {
                "ANSWER_KEY" => 10,
                "OPTION" => 8,
                "QUESTION_CONTENT" => 7,
                "EXPLANATION" => 6,
                "CONTEXT" => 6,
                "CONTEXT_TITLE" => 5,
                "GRADING_RUBRIC" => 5,
                "TOPIC_CODE" => 5,
                "TOPIC_NAME" => 5,
                "STT" => 4,
                _ => 3
            };

            var newRule = new ColumnTaxonomyRule
            {
                TargetField = targetField,
                Priority = priority,
                ConfidenceScore = 5,
                PositivePattern = positivePattern,
                DomainScope = domain,
                LanguageCode = lang,
                IsSelfEnriched = true,
                HitCount = 1,
                CompiledRegex = new Regex(positivePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase)
            };

            _rules.Add(newRule);
            return true;
        }

        private static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var lower = text.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in lower)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                var mapped = ch == 'đ' ? 'd' : ch;
                if (char.IsLetterOrDigit(mapped) || mapped == ' ') sb.Append(mapped);
            }
            return Regex.Replace(sb.ToString().Normalize(System.Text.NormalizationForm.FormC), @"\s+", " ").Trim();
        }

        private void SeedDefaultGlobalRules()
        {
            // ═════════════════════════════════════════════════════════════════════
            // 1. PRIORITY 10: ANSWER KEY (Tuyệt đối ưu tiên để không bị Option nuốt)
            // ═════════════════════════════════════════════════════════════════════
            // Tiếng Việt
            AddRule("ANSWER_KEY", 10, 5,
                @"(?:da[pn]\s*an\s*(?:dung|chinh\s*xac|nhat|chon|chuan|cau\s*hoi|1\s*2\s*3\s*4|a\s*b\s*c\s*d)|phuong\s*an\s*(?:dung|chinh\s*xac|nhat|chon|chuan|1\s*2\s*3\s*4|a\s*b\s*c\s*d)|pa\s*(?:dung)|d\s*a\s*(?:dung)|cau\s*tra\s*loi\s*(?:dung|chinh\s*xac)|ket\s*qua|key|dap\s*so|nghiem|^da[pn]\s*an$|^phuong\s*an$|^tra\s*loi$)",
                negativePattern: @"(sai|khong\s*dung)", lang: "vi");

            // Tiếng Anh (English)
            AddRule("ANSWER_KEY", 10, 5,
                @"(?:correct\s*(?:answer|option|choice)?|answer\s*key|^key$|^solution$|^correct$|^ans$|^correct\s*key$)",
                negativePattern: @"(incorrect|wrong)", lang: "en");

            // Tiếng Trung (Chinese) & Tiếng Nhật (Japanese)
            AddRule("ANSWER_KEY", 10, 5, @"(?:正确答案|答案|参考答案|标准答案|正解|答え|解答|正答)", lang: "any");

            // Tiếng Pháp (French)
            AddRule("ANSWER_KEY", 10, 5, @"(?:bonne\s*reponse|reponse\s*correcte|cle|solution)", lang: "fr");

            // ═════════════════════════════════════════════════════════════════════
            // 2. PRIORITY 8: OPTION COLUMNS (A..H, 1..8, Distractors)
            // ═════════════════════════════════════════════════════════════════════
            // Tiếng Việt & Anh: Phương án 1..8 hoặc Đáp án A..H
            AddRule("OPTION", 8, 3,
                @"((?:da[pn]\s*an|phuong\s*an|lua\s*chon|option|pa|choice|tra\s*loi|distractor))\s*([0-9]+|[a-hA-H])\b",
                negativePattern: @"(dung|chinh\s*xac|nhat|correct|key)", lang: "any");

            // Chữ cái đơn A..H
            AddRule("OPTION", 8, 3,
                @"^(?:\[?|\(?)([a-hA-H])(?:\]?|\)?|\.?)$",
                negativePattern: null, lang: "any");

            // Tiếng Trung / Nhật: 选项A, 選択肢1
            AddRule("OPTION", 8, 3,
                @"(?:选项|选择|選択肢|オプション)\s*([0-9]+|[a-hA-H])\b", lang: "any");

            // ═════════════════════════════════════════════════════════════════════
            // 3. PRIORITY 7: QUESTION CONTENT
            // ═════════════════════════════════════════════════════════════════════
            // Tiếng Việt
            AddRule("QUESTION_CONTENT", 7, 5,
                @"(cau\s*hoi|noi\s*dung|de\s*bai|yeu\s*cau|tinh\s*huong|van\s*de|menh\s*de|khang\s*dinh)",
                negativePattern: @"(trich\s*dan|can\s*cu|giai\s*thich|dap\s*an)", lang: "vi");

            // Tiếng Anh
            AddRule("QUESTION_CONTENT", 7, 5,
                @"(question|content|prompt|stem|problem|statement|task|title|item\s*text)",
                negativePattern: @"(answer|explanation|reference)", lang: "en");

            // Tiếng Trung / Nhật
            AddRule("QUESTION_CONTENT", 7, 5, @"(题目|问题|题干|内容|問題|設問|問題文|質問)", lang: "any");

            // ═════════════════════════════════════════════════════════════════════
            // 4. PRIORITY 6: EXPLANATION / REFERENCE / CITATION / RUBRIC
            // ═════════════════════════════════════════════════════════════════════
            AddRule("EXPLANATION", 6, 3,
                @"(trich\s*dan|nguon|giai\s*thich|can\s*cu|can\s*cu\s*phap\s*ly|huong\s*dan|ghi\s*chu|luu\s*y|barem|rubric|dieu\s*khoan)",
                negativePattern: null, lang: "vi");

            AddRule("EXPLANATION", 6, 3,
                @"(reference|citation|explanation|rationale|note|rubric|grading|source|justification)",
                negativePattern: null, lang: "en");

            AddRule("EXPLANATION", 6, 3, @"(解析|解释|依据|参考|出处|解説|理由|根拠|参考文献)", lang: "any");

            // ═════════════════════════════════════════════════════════════════════
            // 5. PRIORITY 5: TOPIC CODE & NAME
            // ═════════════════════════════════════════════════════════════════════
            AddRule("TOPIC_CODE", 5, 2,
                @"(ma\s*cd|ma\s*chu\s*de|topic\s*code|topiccode|mcd|chu\s*de|chuyen\s*de|ma\s*chuyen\s*de|category|nghiep\s*vu|phan\b)",
                negativePattern: @"(ten\s*chu\s*de|ten\s*cd)", lang: "any");

            AddRule("TOPIC_NAME", 5, 2,
                @"(ten\s*chu\s*de|ten\s*cd|topic\s*name|ten\s*chuyen\s*de|category\s*name)",
                negativePattern: null, lang: "any");

            // ═════════════════════════════════════════════════════════════════════
            // 6. PRIORITY 4: STT / ORDER / INDEX
            // ═════════════════════════════════════════════════════════════════════
            AddRule("STT", 4, 1,
                @"^(?:stt|so\s*thu\s*tu|tt|no|order|index|item\s*no|item|#|序号|番号)$",
                negativePattern: null, lang: "any");

            // ═════════════════════════════════════════════════════════════════════
            // 7. UNIVERSAL EXTENDED FIELDS (Context, Rubric, Duration, Tags, Media...)
            // ═════════════════════════════════════════════════════════════════════
            AddRule("CONTEXT_TITLE", 6, 5,
                @"(ten\s*bai\s*doc|tieu\s*de\s*ngu\s*canh|context\s*title|passage\s*title|tieu\s*de\s*tinh\s*huong|ten\s*tinh\s*huong|tieu\s*de)", lang: "any");

            AddRule("CONTEXT", 6, 4,
                @"(bai\s*doc|ngu\s*canh|doan\s*van|reading\s*passage|passage|context|tinh\s*huong|stimulus|case\s*study|tai\s*lieu\s*chung|reading)",
                negativePattern: @"(tieu\s*de|ten\s*bai|title)", lang: "any");

            AddRule("GRADING_RUBRIC", 5, 3,
                @"(tieu\s*chi\s*cham|rubric|grading\s*rubric|barem|huong\s*dan\s*cham|thang\s*diem|tieu\s*chuan\s*cham)", lang: "any");

            AddRule("TARGET_LEVEL", 5, 3,
                @"(trinh\s*do|cap\s*bac|khoi\s*lop|target\s*level|doi\s*tuong)", lang: "any");

            AddRule("DOMAIN_CODE", 5, 3,
                @"(linh\s*vuc|nganh\s*nghe|domain|domain\s*code|khoi\s*nganh)", lang: "any");

            AddRule("DIFFICULTY", 3, 2,
                @"(do\s*kho|muc\s*do|bac\s*nhan\s*thuc|bloom|difficulty|hardness|level|难度|難易度)", lang: "any");

            AddRule("CRITICAL", 3, 2,
                @"(diem\s*liet|liet|tu\s*than|fatal|critical|bat\s*buoc\s*dung|cau\s*cot\s*loi)", lang: "any");

            AddRule("TYPE", 3, 2,
                @"(loai|loai\s*cau\s*hoi|hinh\s*thuc|type|question\s*type|kieu|题型|設問形式)", lang: "any");

            AddRule("DURATION", 3, 2,
                @"(thoi\s*gian(?:\s*lam\s*bai)?|duration|time\s*limit|thoi\s*luong|so\s*giay|so\s*phut|seconds|minutes)", lang: "any");

            AddRule("SUB_CATEGORY", 3, 2,
                @"(phan\s*loai|nhom|sub\s*category|subcategory|chuyen\s*de\s*phu|sa\s*hinh|bien\s*bao|luat)", lang: "any");

            AddRule("TAGS", 3, 2,
                @"(the\s*tag|tag|tags|nhan|tu\s*khoa|keyword|keywords|hashtag)", lang: "any");

            AddRule("MEDIA_URL", 3, 2,
                @"(hinh\s*anh|anh|media|image|photo|audio|video|link\s*anh|media\s*url|image\s*url|duong\s*dan\s*anh)", lang: "any");
        }

        private void AddRule(string targetField, int priority, int score, string pattern, string? negativePattern = null, string domain = "ALL", string lang = "any")
        {
            _rules.Add(new ColumnTaxonomyRule
            {
                TargetField = targetField,
                Priority = priority,
                ConfidenceScore = score,
                PositivePattern = pattern,
                NegativePattern = negativePattern,
                DomainScope = domain,
                LanguageCode = lang,
                IsSelfEnriched = false,
                CompiledRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase),
                CompiledNegativeRegex = !string.IsNullOrWhiteSpace(negativePattern)
                    ? new Regex(negativePattern, RegexOptions.Compiled | RegexOptions.IgnoreCase)
                    : null
            });
        }
    }
}
