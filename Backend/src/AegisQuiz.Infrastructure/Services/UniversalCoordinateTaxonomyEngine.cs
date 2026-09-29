using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>Kết quả nhận diện Hệ 5 Trục Tọa Độ Tri Thức + Smart Tags.</summary>
    public class CognitiveCoordinateTaxonomyResult
    {
        public string DomainCode { get; set; } = "GENERAL";
        public string? TargetLevel { get; set; }
        public string? AssessmentPurpose { get; set; }
        public string? IssuingOrg { get; set; }
        public int? BenchmarkYear { get; set; }
        public string? BenchmarkStandard { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// [Universal Cognitive Coordinate & Taxonomy Engine]
    /// Động cơ nhận thức tự động nhận diện Hệ 5 Trục Tọa Độ (DomainCode, TargetLevel, AssessmentPurpose, IssuingOrg,
    /// BenchmarkYear/BenchmarkStandard) và Thẻ Thông Minh từ tên file, hàng banner đầu bảng, nội dung câu hỏi và căn cứ pháp lý.
    /// Ưu tiên tuyệt đối dữ liệu động của Tenant (<see cref="DynamicDomainContext"/>: Domains, CoordinatePresets, Topics);
    /// bộ từ khóa chuẩn hệ thống chỉ là lớp dự phòng khi Tenant chưa cấu hình.
    /// </summary>
    public static class UniversalCoordinateTaxonomyEngine
    {
        public static CognitiveCoordinateTaxonomyResult SniffCoordinates(
            string? fileName,
            string? sheetName,
            IEnumerable<string>? headerBannerLines,
            IEnumerable<string>? sampleContents = null,
            IEnumerable<string>? sampleReferences = null,
            DynamicDomainContext? dynamicContext = null)
        {
            var result = new CognitiveCoordinateTaxonomyResult();

            var bannerList = (headerBannerLines ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            var combinedHeader = string.Join("\n", bannerList);
            var combinedAll = $"{fileName} {sheetName} {combinedHeader} " +
                              $"{string.Join(" ", sampleContents?.Take(15) ?? Enumerable.Empty<string>())} " +
                              $"{string.Join(" ", sampleReferences?.Take(15) ?? Enumerable.Empty<string>())}";

            result.BenchmarkYear = DetectYear(bannerList, fileName, sheetName);
            result.TargetLevel = DetectTargetLevel(bannerList, fileName, sheetName, combinedAll, dynamicContext);
            result.AssessmentPurpose = DetectAssessmentPurpose(bannerList, fileName, sheetName, combinedAll, dynamicContext);
            result.IssuingOrg = DetectIssuingOrg(bannerList, fileName, combinedAll, dynamicContext);
            result.DomainCode = DetectDomain(combinedAll, result.TargetLevel, result.AssessmentPurpose, result.IssuingOrg, dynamicContext);
            result.BenchmarkStandard = DetectStandard(combinedAll, result.DomainCode, result.AssessmentPurpose, result.BenchmarkYear, dynamicContext);
            result.Tags = GenerateSmartTags(result, fileName, sheetName, dynamicContext);
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Trục 1: Năm chuẩn mực
        // ─────────────────────────────────────────────────────────────────────

        private static int? DetectYear(List<string> banners, string? fileName, string? sheetName)
        {
            var text = $"{string.Join(" ", banners)} {fileName} {sheetName}";

            var labeled = Regex.Match(text, @"(?:năm|nam|đợt\s*\d+\s*năm|niên\s*khóa|khoá|year)\s*[:\-–—]?\s*(20\d{2})", RegexOptions.IgnoreCase);
            if (labeled.Success && int.TryParse(labeled.Groups[1].Value, out var year) && year >= 2000 && year <= 2050)
            {
                return year;
            }

            var bare = Regex.Match(text, @"\b(202[0-9]|201[8-9])\b");
            return bare.Success && int.TryParse(bare.Groups[1].Value, out var bareYear) ? bareYear : null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Trục 2: Vị trí / Cấp bậc / Khối lớp
        // ─────────────────────────────────────────────────────────────────────

        private static string? DetectTargetLevel(List<string> banners, string? fileName, string? sheetName, string combinedAll, DynamicDomainContext? dynamicContext)
        {
            var preset = MatchPreset(dynamicContext, "TARGET_LEVEL", combinedAll);
            if (preset != null) return preset;

            foreach (var banner in banners)
            {
                var m = Regex.Match(banner, @"(?:vị\s*trí|chức\s*danh|ngạch|đối\s*tượng|cấp\s*bậc|khối\s*lớp|hạng\s*bằng|dành\s*cho)\s*[:\-–—]\s*([^\r\n|;]+)", RegexOptions.IgnoreCase);
                if (!m.Success) continue;
                var extracted = CleanExtractedText(m.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(extracted) && extracted.Length > 2)
                {
                    return NormalizeLevelName(extracted);
                }
            }

            var fileAndSheet = $"{fileName} {sheetName}";
            if (Regex.IsMatch(fileAndSheet, @"\btín\s+dụng\s+khdn\b|\bkhdn\b", RegexOptions.IgnoreCase)) return "Tín dụng Khách hàng Doanh nghiệp";
            if (Regex.IsMatch(fileAndSheet, @"\btín\s+dụng\s+khcn\b|\bkhcn\b", RegexOptions.IgnoreCase)) return "Tín dụng Khách hàng Cá nhân";
            if (Regex.IsMatch(fileAndSheet, @"\bkế\s+toán\s+ngân\s+quỹ\b|\bngân\s+quỹ\b", RegexOptions.IgnoreCase)) return "Kế toán & Ngân quỹ";
            if (Regex.IsMatch(fileAndSheet, @"\bkiểm\s+soát\s+viên\b", RegexOptions.IgnoreCase)) return "Kiểm soát viên";
            if (Regex.IsMatch(fileAndSheet, @"\bgiao\s+dịch\s+viên\b", RegexOptions.IgnoreCase)) return "Giao dịch viên";
            if (Regex.IsMatch(fileAndSheet, @"\blớp\s*12\b|\bkhối\s*12\b", RegexOptions.IgnoreCase)) return "Lớp 12";
            if (Regex.IsMatch(fileAndSheet, @"\blớp\s*11\b|\bkhối\s*11\b", RegexOptions.IgnoreCase)) return "Lớp 11";
            if (Regex.IsMatch(fileAndSheet, @"\blớp\s*10\b|\bkhối\s*10\b", RegexOptions.IgnoreCase)) return "Lớp 10";

            var licenseClass = Regex.Match(fileAndSheet, @"\bhạng\s*(b2|b1|c|d|e|f)\b", RegexOptions.IgnoreCase);
            if (licenseClass.Success) return "GPLX Hạng " + licenseClass.Groups[1].Value.ToUpperInvariant();

            if (Regex.IsMatch(combinedAll, @"\btín\s+dụng\s+khách\s+hàng\s+doanh\s+nghiệp\b", RegexOptions.IgnoreCase)) return "Tín dụng Khách hàng Doanh nghiệp";
            if (Regex.IsMatch(combinedAll, @"\btín\s+dụng\s+khách\s+hàng\s+cá\s+nhân\b", RegexOptions.IgnoreCase)) return "Tín dụng Khách hàng Cá nhân";
            return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Trục 3: Mục đích sát hạch / Loại kỳ thi
        // ─────────────────────────────────────────────────────────────────────

        private static string? DetectAssessmentPurpose(List<string> banners, string? fileName, string? sheetName, string combinedAll, DynamicDomainContext? dynamicContext)
        {
            var preset = MatchPreset(dynamicContext, "ASSESSMENT_PURPOSE", combinedAll);
            if (preset != null) return preset;

            foreach (var banner in banners)
            {
                var stripped = Regex.Replace(banner, @"^(?:bộ\s*câu\s*hỏi(?:\s*,\s*đáp\s*án)?\s*(?:ôn\s*tập\s*)?[-–—:]?\s*)", "", RegexOptions.IgnoreCase).Trim();
                var m = Regex.Match(stripped, @"(kỳ\s*kiểm\s*tra[^\r\n|;]+|kỳ\s*thi[^\r\n|;]+|sát\s*hạch[^\r\n|;]+|đánh\s*giá[^\r\n|;]+|ôn\s*tập\s*nghiệp\s*vụ[^\r\n|;]+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var purpose = Regex.Replace(m.Groups[1].Value.Trim(), @"\s+năm\s+\d{4}$", "", RegexOptions.IgnoreCase).Trim();
                    return NormalizeTitleCase(purpose);
                }
            }

            if (Regex.IsMatch(combinedAll, @"\btốt\s+nghiệp\s+thpt\b|\bthpt\s+quốc\s+gia\b", RegexOptions.IgnoreCase)) return "Kỳ thi Tốt nghiệp THPT Quốc Gia";
            if (Regex.IsMatch(combinedAll, @"\bsát\s+hạch\s+lái\s+xe\b|\bgplx\b", RegexOptions.IgnoreCase)) return "Sát hạch Giấy phép Lái xe (GPLX)";
            if (Regex.IsMatch(combinedAll, @"\bkiểm\s+tra\s+(?:chuyên\s*môn\s*)?nghiệp\s+vụ\s+định\s+kỳ\b", RegexOptions.IgnoreCase)) return "Kiểm tra chuyên môn nghiệp vụ định kỳ";
            if (Regex.IsMatch(combinedAll, @"\bphòng\s+chống\s+rửa\s+tiền\b|\baml\b", RegexOptions.IgnoreCase)) return "Sát hạch Nghiệp vụ Phòng chống Rửa tiền (AML)";
            if (Regex.IsMatch(combinedAll, @"\ban\s+toàn\s+lao\s+động\b|\batvsld\b", RegexOptions.IgnoreCase)) return "Huấn luyện An toàn Vệ sinh Lao động (ATVSLĐ)";
            return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Trục 4: Đơn vị / Cơ quan ban hành
        // ─────────────────────────────────────────────────────────────────────

        private static string? DetectIssuingOrg(List<string> banners, string? fileName, string combinedAll, DynamicDomainContext? dynamicContext)
        {
            var preset = MatchPreset(dynamicContext, "ISSUING_ORG", combinedAll);
            if (preset != null) return preset;

            if (Regex.IsMatch(combinedAll, @"\bagribank\b|\bnhno\b|\bngân\s+hàng\s+nông\s+nghiệp\b", RegexOptions.IgnoreCase))
            {
                return Regex.IsMatch(combinedAll, @"\bchi\s+nhánh\b", RegexOptions.IgnoreCase) ? "Agribank (Chi nhánh)" : "Agribank";
            }
            if (Regex.IsMatch(combinedAll, @"\bvietcombank\b|\bvcb\b|\bngoại\s+thương\b", RegexOptions.IgnoreCase)) return "Vietcombank";
            if (Regex.IsMatch(combinedAll, @"\bbidv\b|\bđầu\s+tư\s+và\s+phát\s+triển\b", RegexOptions.IgnoreCase)) return "BIDV";
            if (Regex.IsMatch(combinedAll, @"\bvietinbank\b|\bctg\b|\bcông\s+thương\b", RegexOptions.IgnoreCase)) return "VietinBank";
            if (Regex.IsMatch(combinedAll, @"\bcục\s+đường\s+bộ\b", RegexOptions.IgnoreCase)) return "Cục Đường bộ Việt Nam";
            if (Regex.IsMatch(combinedAll, @"\bbộ\s+y\s+tế\b", RegexOptions.IgnoreCase)) return "Bộ Y tế";
            if (Regex.IsMatch(combinedAll, @"\bbộ\s+giáo\s+dục\b", RegexOptions.IgnoreCase)) return "Bộ Giáo dục và Đào tạo";
            if (Regex.IsMatch(combinedAll, @"\bchuyên\s+amsterdam\b|\bamsterdam\b", RegexOptions.IgnoreCase)) return "THPT Chuyên Hà Nội - Amsterdam";

            foreach (var banner in banners)
            {
                var m = Regex.Match(banner, @"(?:đơn\s*vị|cơ\s*quan|trường|ngân\s*hàng|chi\s*nhánh)\s*[:\-–—]\s*([^\r\n|;]+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var extracted = CleanExtractedText(m.Groups[1].Value);
                    if (!string.IsNullOrWhiteSpace(extracted)) return NormalizeTitleCase(extracted);
                }
                if (Regex.IsMatch(banner, @"\btại\s+chi\s+nhánh\b", RegexOptions.IgnoreCase)) return "Chi nhánh";
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Trục 5: Miền ngành nghề (Dynamic Domains → chuẩn hệ thống dự phòng)
        // ─────────────────────────────────────────────────────────────────────

        private static string DetectDomain(string combinedAll, string? targetLevel, string? purpose, string? org, DynamicDomainContext? dynamicContext)
        {
            if (dynamicContext?.Domains != null && dynamicContext.Domains.Count > 0)
            {
                var cleanAll = CleanToTag(combinedAll);
                var scores = dynamicContext.Domains.ToDictionary(d => d.Code, _ => 0, StringComparer.OrdinalIgnoreCase);

                foreach (var domain in dynamicContext.Domains)
                {
                    if (domain.Code.Equals("GENERAL", StringComparison.OrdinalIgnoreCase)) continue;

                    if (!string.IsNullOrWhiteSpace(domain.Code) && combinedAll.Contains(domain.Code, StringComparison.OrdinalIgnoreCase)) scores[domain.Code] += 5;
                    if (ContainsPhrase(combinedAll, cleanAll, domain.Name)) scores[domain.Code] += 5;
                    scores[domain.Code] += 3 * domain.Synonyms.Count(s => ContainsPhrase(combinedAll, cleanAll, s));

                    if (dynamicContext.CoordinatePresets != null)
                    {
                        foreach (var preset in dynamicContext.CoordinatePresets.Where(p => p.DomainCode.Equals(domain.Code, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (ContainsPhrase(combinedAll, cleanAll, preset.PresetLabel)) scores[domain.Code] += 4;
                            scores[domain.Code] += 2 * preset.Synonyms.Count(s => ContainsPhrase(combinedAll, cleanAll, s));
                        }
                    }

                    if (dynamicContext.Topics != null)
                    {
                        foreach (var topic in dynamicContext.Topics)
                        {
                            bool topicMentioned = combinedAll.Contains(topic.Code, StringComparison.OrdinalIgnoreCase) || ContainsPhrase(combinedAll, cleanAll, topic.Name);
                            if (!topicMentioned) continue;

                            var cleanTopicName = CleanToTag(topic.Name);
                            bool topicBelongsToDomain = domain.Synonyms.Any(s => cleanTopicName.Contains(CleanToTag(s)))
                                                        || (!string.IsNullOrWhiteSpace(domain.Name) && cleanTopicName.Contains(CleanToTag(domain.Name)));
                            if (topicBelongsToDomain) scores[domain.Code] += 4;
                        }
                    }
                }

                var best = scores.OrderByDescending(kv => kv.Value).FirstOrDefault();
                if (best.Value > 0) return best.Key;
            }

            var fallbackScores = new Dictionary<string, int>
            {
                ["BANKING"] = 0, ["EDUCATION"] = 0, ["GOV_DRIVING"] = 0, ["HEALTHCARE"] = 0, ["HSE"] = 0, ["IT_SECURITY"] = 0
            };

            foreach (var (domain, keywords, weight) in SystemDomainKeywords)
            {
                fallbackScores[domain] += weight * keywords.Count(k => combinedAll.Contains(k, StringComparison.OrdinalIgnoreCase));
            }

            var winner = fallbackScores.OrderByDescending(kv => kv.Value).First();
            return winner.Value > 0 ? winner.Key : "GENERAL";
        }

        /// <summary>Bộ từ khóa chuẩn hệ thống (lớp dự phòng khi Tenant chưa cấu hình Dynamic Domains).</summary>
        private static readonly (string Domain, string[] Keywords, int Weight)[] SystemDomainKeywords =
        {
            ("BANKING", new[] { "tín dụng", "khdn", "khcn", "tiền gửi", "ngân hàng", "chi nhánh", "rửa tiền", "aml", "thế chấp", "tài sản bảo đảm",
                                "e-banking", "ebanking", "agribank", "vietcombank", "bidv", "vietinbank", "cic", "kho tiền", "ngân quỹ", "thẩm định",
                                "cho vay", "bảo lãnh", "lãi suất", "sổ tiết kiệm", "thẻ tín dụng", "quy định số 1686", "nhno", "hạn mức" }, 2),
            ("EDUCATION", new[] { "toán", "đạo hàm", "tích phân", "hàm số", "ngữ văn", "tiếng anh", "vật lý", "hóa học", "gdpt", "thpt",
                                  "lớp 12", "lớp 11", "lớp 10", "k-12", "reading passage", "pronunciation", "chuyên amsterdam" }, 2),
            ("GOV_DRIVING", new[] { "gplx", "sa hình", "biển báo", "lái xe", "vượt ẩu", "xe cơ giới", "tốc độ tối đa", "điểm liệt", "cục đường bộ" }, 3),
            ("HEALTHCARE", new[] { "bác sĩ", "dược lâm sàng", "phác đồ", "điều trị", "bệnh nhân", "bệnh viện", "bộ y tế", "y khoa" }, 3),
            ("HSE", new[] { "an toàn lao động", "bảo hộ lao động", "pccc", "phòng cháy chữa cháy", "vslđ", "iso 45001", "hóa chất", "an toàn điện" }, 3),
            ("IT_SECURITY", new[] { "an toàn thông tin", "iso 27001", "owasp", "an ninh mạng", "phishing", "mật khẩu", "cybersecurity", "devsecops" }, 3)
        };

        // ─────────────────────────────────────────────────────────────────────
        // Trục 5b: Chuẩn mực / Văn bản pháp quy
        // ─────────────────────────────────────────────────────────────────────

        private static string? DetectStandard(string combinedAll, string domain, string? purpose, int? year, DynamicDomainContext? dynamicContext)
        {
            if (dynamicContext?.CoordinatePresets != null && dynamicContext.CoordinatePresets.Count > 0)
            {
                var candidates = dynamicContext.CoordinatePresets.Where(p =>
                    p.CoordinateType.Equals("BENCHMARK_STANDARD", StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrWhiteSpace(domain)
                        || p.DomainCode.Equals(domain, StringComparison.OrdinalIgnoreCase)
                        || p.DomainCode.Equals("GENERAL", StringComparison.OrdinalIgnoreCase)));

                foreach (var preset in candidates)
                {
                    if (PresetMentioned(preset, combinedAll)) return preset.PresetLabel;
                }
            }

            var decision = Regex.Match(combinedAll, @"(quy\s*định\s*số\s*\d+/[A-Za-z0-9\-_]+)", RegexOptions.IgnoreCase);
            if (decision.Success) return NormalizeTitleCase(decision.Groups[1].Value);

            var circular = Regex.Match(combinedAll, @"(thông\s*tư\s*\d+(?:/\d+)?/TT-[A-Za-z0-9]+)", RegexOptions.IgnoreCase);
            if (circular.Success) return circular.Groups[1].Value.ToUpperInvariant();

            return domain switch
            {
                "BANKING" => year.HasValue ? $"Quy chế kiểm tra nghiệp vụ định kỳ Đợt {ExtractDotNumber(combinedAll)}/{year}" : "Chuẩn mực Nghiệp vụ Ngân hàng",
                "EDUCATION" => "Chương trình GDPT 2018",
                "GOV_DRIVING" => "Nghị định 100/2019/NĐ-CP & QC 41:2019",
                "HEALTHCARE" => "Phác đồ Điều trị Bộ Y tế 2025",
                "HSE" => "Nghị định 44/2016/NĐ-CP & ISO 45001",
                "IT_SECURITY" => "ISO/IEC 27001:2022",
                _ => null
            };
        }

        private static string ExtractDotNumber(string text)
        {
            var m = Regex.Match(text, @"đợt\s*(\d+)", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : "2";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Smart Tags
        // ─────────────────────────────────────────────────────────────────────

        private static List<string> GenerateSmartTags(CognitiveCoordinateTaxonomyResult res, string? fileName, string? sheetName, DynamicDomainContext? dynamicContext)
        {
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var dynamicDomain = dynamicContext?.Domains?.FirstOrDefault(d => d.Code.Equals(res.DomainCode, StringComparison.OrdinalIgnoreCase));
            if (dynamicDomain != null)
            {
                AddTag(tags, CleanToTag(dynamicDomain.Name));
                AddTag(tags, CleanToTag(dynamicDomain.Code));
            }

            if (dynamicContext?.CoordinatePresets != null)
            {
                foreach (var preset in dynamicContext.CoordinatePresets)
                {
                    bool presetSelected = string.Equals(preset.PresetLabel, res.TargetLevel, StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(preset.PresetLabel, res.IssuingOrg, StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(preset.PresetLabel, res.BenchmarkStandard, StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(preset.PresetLabel, res.AssessmentPurpose, StringComparison.OrdinalIgnoreCase);
                    if (presetSelected) AddTag(tags, CleanToTag(preset.PresetCode));
                }
            }

            switch (res.DomainCode)
            {
                case "BANKING": tags.Add("ngan-hang"); break;
                case "EDUCATION": tags.Add("giao-duc"); break;
                case "GOV_DRIVING": tags.Add("gplx"); tags.Add("sat-hach-lai-xe"); break;
                case "HEALTHCARE": tags.Add("y-te"); break;
                case "HSE": tags.Add("an-toan-lao-dong"); tags.Add("hse"); break;
                case "IT_SECURITY": tags.Add("an-toan-thong-tin"); break;
            }

            if (!string.IsNullOrWhiteSpace(res.TargetLevel))
            {
                AddTag(tags, CleanToTag(res.TargetLevel));
                if (res.TargetLevel.Contains("Khách hàng Doanh nghiệp", StringComparison.OrdinalIgnoreCase)) { tags.Add("tin-dung-khdn"); tags.Add("khdn"); }
                if (res.TargetLevel.Contains("Khách hàng Cá nhân", StringComparison.OrdinalIgnoreCase)) { tags.Add("tin-dung-khcn"); tags.Add("khcn"); }
            }

            if (!string.IsNullOrWhiteSpace(res.AssessmentPurpose))
            {
                if (res.AssessmentPurpose.Contains("định kỳ", StringComparison.OrdinalIgnoreCase)) tags.Add("kiem-tra-dinh-ky");
                if (res.BenchmarkYear.HasValue)
                {
                    if (res.AssessmentPurpose.Contains("Đợt 2", StringComparison.OrdinalIgnoreCase)) tags.Add($"dot-2-{res.BenchmarkYear}");
                    else if (res.AssessmentPurpose.Contains("Đợt 1", StringComparison.OrdinalIgnoreCase)) tags.Add($"dot-1-{res.BenchmarkYear}");
                }
            }

            if (res.BenchmarkYear.HasValue) tags.Add($"nam-{res.BenchmarkYear}");

            if (!string.IsNullOrWhiteSpace(res.IssuingOrg))
            {
                AddTag(tags, CleanToTag(res.IssuingOrg));
                if (res.IssuingOrg.Contains("Agribank", StringComparison.OrdinalIgnoreCase)) tags.Add("agribank");
                if (res.IssuingOrg.Contains("Vietcombank", StringComparison.OrdinalIgnoreCase)) tags.Add("vietcombank");
                if (res.IssuingOrg.Contains("BIDV", StringComparison.OrdinalIgnoreCase)) tags.Add("bidv");
                if (res.IssuingOrg.Contains("Chi nhánh", StringComparison.OrdinalIgnoreCase)) tags.Add("chi-nhanh");
            }

            if (!string.IsNullOrWhiteSpace(res.BenchmarkStandard)) AddTag(tags, CleanToTag(res.BenchmarkStandard));

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var lowerFile = fileName.ToLowerInvariant();
                if (lowerFile.Contains("tin dung") || lowerFile.Contains("tín dụng")) tags.Add("tin-dung");
            }

            return tags.Where(t => !string.IsNullOrWhiteSpace(t) && t.Length >= 2).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Tiện ích dùng chung
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Tìm preset của Tenant theo loại tọa độ có nhãn hoặc từ đồng nghĩa xuất hiện trong văn bản.</summary>
        private static string? MatchPreset(DynamicDomainContext? dynamicContext, string coordinateType, string combinedAll)
        {
            if (dynamicContext?.CoordinatePresets == null || dynamicContext.CoordinatePresets.Count == 0) return null;

            foreach (var preset in dynamicContext.CoordinatePresets.Where(p => p.CoordinateType.Equals(coordinateType, StringComparison.OrdinalIgnoreCase)))
            {
                if (PresetMentioned(preset, combinedAll)) return preset.PresetLabel;
            }
            return null;
        }

        private static bool PresetMentioned(TenantCoordinatePresetDto preset, string combinedAll)
        {
            if (!string.IsNullOrWhiteSpace(preset.PresetLabel) && combinedAll.Contains(preset.PresetLabel, StringComparison.OrdinalIgnoreCase)) return true;
            return preset.Synonyms.Any(s => !string.IsNullOrWhiteSpace(s) && combinedAll.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Cụm từ xuất hiện trong văn bản gốc (không phân biệt hoa/thường) hoặc trong dạng slug đã bỏ dấu.</summary>
        private static bool ContainsPhrase(string combinedAll, string cleanAll, string? phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase)) return false;
            return combinedAll.Contains(phrase, StringComparison.OrdinalIgnoreCase) || cleanAll.Contains(CleanToTag(phrase));
        }

        private static void AddTag(HashSet<string> tags, string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag)) tags.Add(tag);
        }

        private static string CleanExtractedText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return Regex.Replace(input, @"[|\t]+", " ").Trim(' ', ':', '-', '–', '—');
        }

        private static string NormalizeLevelName(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.Equals("TÍN DỤNG KHÁCH HÀNG DOANH NGHIỆP", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("TÍN DỤNG KHDN", StringComparison.OrdinalIgnoreCase))
                return "Tín dụng Khách hàng Doanh nghiệp";
            if (trimmed.Equals("TÍN DỤNG KHÁCH HÀNG CÁ NHÂN", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("TÍN DỤNG KHCN", StringComparison.OrdinalIgnoreCase))
                return "Tín dụng Khách hàng Cá nhân";
            return NormalizeTitleCase(trimmed);
        }

        private static readonly HashSet<string> UpperCaseAcronyms = new(StringComparer.OrdinalIgnoreCase)
            { "KHDN", "KHCN", "GPLX", "THPT", "GDPT", "AML", "HSE", "PCCC", "CNTT", "ISO" };

        private static readonly HashSet<string> LowerCaseConnectors = new(StringComparer.OrdinalIgnoreCase)
            { "và", "với", "tại", "cho", "của" };

        /// <summary>Viết hoa chữ cái đầu mỗi từ; giữ nguyên viết tắt chuyên ngành; hạ thường liên từ tiếng Việt.</summary>
        public static string NormalizeTitleCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var words = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if (word.Length == 1) sb.Append(char.ToUpper(word[0]));
                else if (UpperCaseAcronyms.Contains(word)) sb.Append(word.ToUpperInvariant());
                else if (i > 0 && LowerCaseConnectors.Contains(word)) sb.Append(word.ToLowerInvariant());
                else sb.Append(char.ToUpper(word[0])).Append(word.Substring(1).ToLowerInvariant());

                if (i < words.Length - 1) sb.Append(' ');
            }
            return sb.ToString();
        }

        /// <summary>Chuyển cụm từ thành slug không dấu, chữ thường, phân cách bằng "-" (ví dụ "Tín dụng KHDN" → "tin-dung-khdn").</summary>
        private static string CleanToTag(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var sb = new StringBuilder();
            foreach (var c in input.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
                else if (char.IsWhiteSpace(c) || c == '-' || c == '_') sb.Append('-');
            }
            return Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
        }
    }
}
