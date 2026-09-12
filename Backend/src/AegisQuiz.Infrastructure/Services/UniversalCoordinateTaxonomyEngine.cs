using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AegisQuiz.Infrastructure.Services
{
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
    /// Động cơ siêu vi nhận thức tự động nhận diện Hệ 5 Trục Tọa Độ và Thẻ Thông Minh
    /// từ tiêu đề file, các hàng banner/tiêu đề đầu bảng và nội dung câu hỏi/căn cứ pháp lý.
    /// </summary>
    public static class UniversalCoordinateTaxonomyEngine
    {
        public static CognitiveCoordinateTaxonomyResult SniffCoordinates(
            string? fileName,
            string? sheetName,
            IEnumerable<string>? headerBannerLines,
            IEnumerable<string>? sampleContents = null,
            IEnumerable<string>? sampleReferences = null)
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

            // 1. Nhận diện Năm chuẩn mực (BenchmarkYear)
            result.BenchmarkYear = DetectYear(bannerList, fileName, sheetName);

            // 2. Nhận diện Vị trí / Cấp bậc / Khối lớp (TargetLevel)
            result.TargetLevel = DetectTargetLevel(bannerList, fileName, sheetName, combinedAll);

            // 3. Nhận diện Mục đích sát hạch / Loại kỳ thi (AssessmentPurpose)
            result.AssessmentPurpose = DetectAssessmentPurpose(bannerList, fileName, sheetName, combinedAll);

            // 4. Nhận diện Đơn vị / Cơ quan ban hành (IssuingOrg)
            result.IssuingOrg = DetectIssuingOrg(bannerList, fileName, combinedAll);

            // 5. Nhận diện Miền ngành nghề đào tạo (DomainCode)
            result.DomainCode = DetectDomain(combinedAll, result.TargetLevel, result.AssessmentPurpose, result.IssuingOrg);

            // 6. Nhận diện Chuẩn mực / Thể thức quy chiếu (BenchmarkStandard)
            result.BenchmarkStandard = DetectStandard(combinedAll, result.DomainCode, result.AssessmentPurpose, result.BenchmarkYear);

            // 7. Tự động sinh danh sách Thẻ thông minh (Smart Tags)
            result.Tags = GenerateSmartTags(result, fileName, sheetName);

            return result;
        }

        private static int? DetectYear(List<string> banners, string? fileName, string? sheetName)
        {
            var textToScan = $"{string.Join(" ", banners)} {fileName} {sheetName}";

            // Ưu tiên khớp "NĂM 2026", "ĐỢT 2 NĂM 2026", "NIÊN KHÓA 2025-2026"
            var mYearPrefixed = Regex.Match(textToScan, @"(?:năm|nam|đợt\s*\d+\s*năm|niên\s*khóa|khoá|year)\s*[:\-–—]?\s*(20\d{2})", RegexOptions.IgnoreCase);
            if (mYearPrefixed.Success && int.TryParse(mYearPrefixed.Groups[1].Value, out int y1) && y1 >= 2000 && y1 <= 2050)
            {
                return y1;
            }

            // Quét năm dạng 202x đứng riêng
            var mYearStandalone = Regex.Match(textToScan, @"\b(202[0-9]|201[8-9])\b");
            if (mYearStandalone.Success && int.TryParse(mYearStandalone.Groups[1].Value, out int y2))
            {
                return y2;
            }

            return null;
        }

        private static string? DetectTargetLevel(List<string> banners, string? fileName, string? sheetName, string combinedAll)
        {
            // 1. Quét các dòng banner tìm tiền tố "VỊ TRÍ:", "CHỨC DANH:", "ĐỐI TƯỢNG:", "KHỐI LỚP:", "CẤP BẬC:"
            foreach (var line in banners)
            {
                var mPrefix = Regex.Match(line, @"(?:vị\s*trí|chức\s*danh|ngạch|đối\s*tượng|cấp\s*bậc|khối\s*lớp|hạng\s*bằng|dành\s*cho)\s*[:\-–—]\s*([^\r\n|;]+)", RegexOptions.IgnoreCase);
                if (mPrefix.Success)
                {
                    var val = CleanExtractedText(mPrefix.Groups[1].Value);
                    if (!string.IsNullOrWhiteSpace(val) && val.Length > 2)
                    {
                        return NormalizeLevelName(val);
                    }
                }
            }

            // 2. Nhận diện từ file name / sheet name (Ví dụ: "1. Tín dụng KHDN", "Tín dụng KHCN", "Lớp 12", "Hạng B2")
            var nameSource = $"{fileName} {sheetName}";
            if (Regex.IsMatch(nameSource, @"\btín\s+dụng\s+khdn\b|\bkhdn\b", RegexOptions.IgnoreCase))
                return "Tín dụng Khách hàng Doanh nghiệp";
            if (Regex.IsMatch(nameSource, @"\btín\s+dụng\s+khcn\b|\bkhcn\b", RegexOptions.IgnoreCase))
                return "Tín dụng Khách hàng Cá nhân";
            if (Regex.IsMatch(nameSource, @"\bkế\s+toán\s+ngân\s+quỹ\b|\bngân\s+quỹ\b", RegexOptions.IgnoreCase))
                return "Kế toán & Ngân quỹ";
            if (Regex.IsMatch(nameSource, @"\bkiểm\s+soát\s+viên\b", RegexOptions.IgnoreCase))
                return "Kiểm soát viên";
            if (Regex.IsMatch(nameSource, @"\bgiao\s+dịch\s+viên\b", RegexOptions.IgnoreCase))
                return "Giao dịch viên";
            if (Regex.IsMatch(nameSource, @"\blớp\s*12\b|\bkhối\s*12\b", RegexOptions.IgnoreCase))
                return "Lớp 12";
            if (Regex.IsMatch(nameSource, @"\blớp\s*11\b|\bkhối\s*11\b", RegexOptions.IgnoreCase))
                return "Lớp 11";
            if (Regex.IsMatch(nameSource, @"\blớp\s*10\b|\bkhối\s*10\b", RegexOptions.IgnoreCase))
                return "Lớp 10";
            if (Regex.IsMatch(nameSource, @"\bhạng\s*(?:b2|b1|c|d|e|f)\b", RegexOptions.IgnoreCase))
            {
                var hm = Regex.Match(nameSource, @"hạng\s*(b2|b1|c|d|e|f)", RegexOptions.IgnoreCase);
                return $"GPLX Hạng {hm.Groups[1].Value.ToUpperInvariant()}";
            }

            // 3. Quét toàn văn
            if (Regex.IsMatch(combinedAll, @"\btín\s+dụng\s+khách\s+hàng\s+doanh\s+nghiệp\b", RegexOptions.IgnoreCase))
                return "Tín dụng Khách hàng Doanh nghiệp";
            if (Regex.IsMatch(combinedAll, @"\btín\s+dụng\s+khách\s+hàng\s+cá\s+nhân\b", RegexOptions.IgnoreCase))
                return "Tín dụng Khách hàng Cá nhân";

            return null;
        }

        private static string? DetectAssessmentPurpose(List<string> banners, string? fileName, string? sheetName, string combinedAll)
        {
            // 1. Quét banner tìm cấu trúc kỳ kiểm tra / kỳ thi
            foreach (var line in banners)
            {
                // Bỏ qua cụm "BỘ CÂU HỎI, ĐÁP ÁN ÔN TẬP"
                var cleaned = Regex.Replace(line, @"^(?:bộ\s*câu\s*hỏi(?:\s*,\s*đáp\s*án)?\s*(?:ôn\s*tập\s*)?[-–—:]?\s*)", "", RegexOptions.IgnoreCase).Trim();

                var m = Regex.Match(cleaned, @"(kỳ\s*kiểm\s*tra[^\r\n|;]+|kỳ\s*thi[^\r\n|;]+|sát\s*hạch[^\r\n|;]+|đánh\s*giá[^\r\n|;]+|ôn\s*tập\s*nghiệp\s*vụ[^\r\n|;]+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var purpose = m.Groups[1].Value.Trim();
                    // Loại bỏ phần "năm 20xx" ở đuôi nếu có
                    purpose = Regex.Replace(purpose, @"\s+năm\s+\d{4}$", "", RegexOptions.IgnoreCase).Trim();
                    return NormalizeTitleCase(purpose);
                }
            }

            // 2. Nhận diện từ file / sheet / nội dung
            if (Regex.IsMatch(combinedAll, @"\btốt\s+nghiệp\s+thpt\b|\bthpt\s+quốc\s+gia\b", RegexOptions.IgnoreCase))
                return "Kỳ thi Tốt nghiệp THPT Quốc Gia";
            if (Regex.IsMatch(combinedAll, @"\bsát\s+hạch\s+lái\s+xe\b|\bgplx\b", RegexOptions.IgnoreCase))
                return "Sát hạch Giấy phép Lái xe (GPLX)";
            if (Regex.IsMatch(combinedAll, @"\bkiểm\s+tra\s+(?:chuyên\s*môn\s*)?nghiệp\s+vụ\s+định\s+kỳ\b", RegexOptions.IgnoreCase))
                return "Kiểm tra chuyên môn nghiệp vụ định kỳ";
            if (Regex.IsMatch(combinedAll, @"\bphòng\s+chống\s+rửa\s+tiền\b|\baml\b", RegexOptions.IgnoreCase))
                return "Sát hạch Nghiệp vụ Phòng chống Rửa tiền (AML)";
            if (Regex.IsMatch(combinedAll, @"\ban\s+toàn\s+lao\s+động\b|\batvsld\b", RegexOptions.IgnoreCase))
                return "Huấn luyện An toàn Vệ sinh Lao động (ATVSLĐ)";

            return null;
        }

        private static string? DetectIssuingOrg(List<string> banners, string? fileName, string combinedAll)
        {
            // 1. Quét tìm tên ngân hàng / tổ chức lớn
            if (Regex.IsMatch(combinedAll, @"\bagribank\b|\bnhno\b|\bngân\s+hàng\s+nông\s+nghiệp\b", RegexOptions.IgnoreCase))
            {
                if (Regex.IsMatch(combinedAll, @"\bchi\s+nhánh\b", RegexOptions.IgnoreCase))
                    return "Agribank (Chi nhánh)";
                return "Agribank";
            }
            if (Regex.IsMatch(combinedAll, @"\bvietcombank\b|\bvcb\b|\bngoại\s+thương\b", RegexOptions.IgnoreCase))
                return "Vietcombank";
            if (Regex.IsMatch(combinedAll, @"\bbidv\b|\bđầu\s+tư\s+và\s+phát\s+triển\b", RegexOptions.IgnoreCase))
                return "BIDV";
            if (Regex.IsMatch(combinedAll, @"\bvietinbank\b|\bctg\b|\bcông\s+thương\b", RegexOptions.IgnoreCase))
                return "VietinBank";
            if (Regex.IsMatch(combinedAll, @"\bcục\s+đường\s+bộ\b", RegexOptions.IgnoreCase))
                return "Cục Đường bộ Việt Nam";
            if (Regex.IsMatch(combinedAll, @"\bbộ\s+y\s+tế\b", RegexOptions.IgnoreCase))
                return "Bộ Y tế";
            if (Regex.IsMatch(combinedAll, @"\bbộ\s+giáo\s+dục\b", RegexOptions.IgnoreCase))
                return "Bộ Giáo dục và Đào tạo";
            if (Regex.IsMatch(combinedAll, @"\bchuyên\s+amsterdam\b|\bamsterdam\b", RegexOptions.IgnoreCase))
                return "THPT Chuyên Hà Nội - Amsterdam";

            // 2. Tìm tiền tố "ĐƠN VỊ:", "CƠ QUAN:", "TẠI CHI NHÁNH"
            foreach (var line in banners)
            {
                var m = Regex.Match(line, @"(?:đơn\s*vị|cơ\s*quan|trường|ngân\s*hàng|chi\s*nhánh)\s*[:\-–—]\s*([^\r\n|;]+)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var val = CleanExtractedText(m.Groups[1].Value);
                    if (!string.IsNullOrWhiteSpace(val)) return NormalizeTitleCase(val);
                }

                if (Regex.IsMatch(line, @"\btại\s+chi\s+nhánh\b", RegexOptions.IgnoreCase))
                    return "Chi nhánh";
            }

            return null;
        }

        private static string DetectDomain(string combinedAll, string? targetLevel, string? purpose, string? org)
        {
            var scores = new Dictionary<string, int>
            {
                ["BANKING"] = 0,
                ["EDUCATION"] = 0,
                ["GOV_DRIVING"] = 0,
                ["HEALTHCARE"] = 0,
                ["HSE"] = 0,
                ["IT_SECURITY"] = 0
            };

            // Điểm tín hiệu Ngân hàng
            var bankingTerms = new[] { "tín dụng", "khdn", "khcn", "tiền gửi", "ngân hàng", "chi nhánh", "rửa tiền", "aml", "thế chấp",
                "tài sản bảo đảm", "e-banking", "ebanking", "agribank", "vietcombank", "bidv", "vietinbank", "cic", "kho tiền", "ngân quỹ",
                "thẩm định", "cho vay", "bảo lãnh", "lãi suất", "sổ tiết kiệm", "thẻ tín dụng", "quy định số 1686", "nhno", "hạn mức" };
            foreach (var term in bankingTerms)
            {
                if (combinedAll.Contains(term, StringComparison.OrdinalIgnoreCase)) scores["BANKING"] += 2;
            }

            // Điểm tín hiệu Giáo dục
            var eduTerms = new[] { "toán", "đạo hàm", "tích phân", "hàm số", "ngữ văn", "tiếng anh", "vật lý", "hóa học", "gdpt", "thpt",
                "lớp 12", "lớp 11", "lớp 10", "k-12", "reading passage", "pronunciation", "chuyên amsterdam" };
            foreach (var term in eduTerms)
            {
                if (combinedAll.Contains(term, StringComparison.OrdinalIgnoreCase)) scores["EDUCATION"] += 2;
            }

            // Điểm tín hiệu Sát hạch lái xe
            var drivingTerms = new[] { "gplx", "sa hình", "biển báo", "lái xe", "vượt ẩu", "xe cơ giới", "tốc độ tối đa", "điểm liệt", "cục đường bộ" };
            foreach (var term in drivingTerms)
            {
                if (combinedAll.Contains(term, StringComparison.OrdinalIgnoreCase)) scores["GOV_DRIVING"] += 3;
            }

            // Điểm tín hiệu Y tế
            var healthTerms = new[] { "bác sĩ", "dược lâm sàng", "phác đồ", "điều trị", "bệnh nhân", "bệnh viện", "bộ y tế", "y khoa" };
            foreach (var term in healthTerms)
            {
                if (combinedAll.Contains(term, StringComparison.OrdinalIgnoreCase)) scores["HEALTHCARE"] += 3;
            }

            // Điểm tín hiệu HSE
            var hseTerms = new[] { "an toàn lao động", "bảo hộ lao động", "pccc", "phòng cháy chữa cháy", "vslđ", "iso 45001", "hóa chất", "an toàn điện" };
            foreach (var term in hseTerms)
            {
                if (combinedAll.Contains(term, StringComparison.OrdinalIgnoreCase)) scores["HSE"] += 3;
            }

            // Điểm tín hiệu CNTT
            var itTerms = new[] { "an toàn thông tin", "iso 27001", "owasp", "an ninh mạng", "phishing", "mật khẩu", "cybersecurity", "devsecops" };
            foreach (var term in itTerms)
            {
                if (combinedAll.Contains(term, StringComparison.OrdinalIgnoreCase)) scores["IT_SECURITY"] += 3;
            }

            var best = scores.OrderByDescending(kv => kv.Value).First();
            return best.Value > 0 ? best.Key : "GENERAL";
        }

        private static string? DetectStandard(string combinedAll, string domain, string? purpose, int? year)
        {
            // Trích xuất số quy định nếu có (Ví dụ: "Quy định số 1686/QyĐ-NHNo-KHDN")
            var mQd = Regex.Match(combinedAll, @"(quy\s*định\s*số\s*\d+/[A-Za-z0-9\-_]+)", RegexOptions.IgnoreCase);
            if (mQd.Success)
            {
                return NormalizeTitleCase(mQd.Groups[1].Value);
            }

            // Trích xuất Thông tư nếu có (Ví dụ: "Thông tư 41/2016/TT-NHNN")
            var mTt = Regex.Match(combinedAll, @"(thông\s*tư\s*\d+(?:/\d+)?/TT-[A-Za-z0-9]+)", RegexOptions.IgnoreCase);
            if (mTt.Success)
            {
                return mTt.Groups[1].Value.ToUpperInvariant();
            }

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

        private static List<string> GenerateSmartTags(CognitiveCoordinateTaxonomyResult res, string? fileName, string? sheetName)
        {
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Thẻ từ Domain
            switch (res.DomainCode)
            {
                case "BANKING":
                    tags.Add("ngan-hang");
                    break;
                case "EDUCATION":
                    tags.Add("giao-duc");
                    break;
                case "GOV_DRIVING":
                    tags.Add("gplx");
                    tags.Add("sat-hach-lai-xe");
                    break;
                case "HEALTHCARE":
                    tags.Add("y-te");
                    break;
                case "HSE":
                    tags.Add("an-toan-lao-dong");
                    tags.Add("hse");
                    break;
                case "IT_SECURITY":
                    tags.Add("an-toan-thong-tin");
                    break;
            }

            // 2. Thẻ từ TargetLevel
            if (!string.IsNullOrWhiteSpace(res.TargetLevel))
            {
                var cleanLvl = CleanToTag(res.TargetLevel);
                if (!string.IsNullOrWhiteSpace(cleanLvl)) tags.Add(cleanLvl);

                // Viết tắt phổ biến
                if (res.TargetLevel.Contains("Khách hàng Doanh nghiệp", StringComparison.OrdinalIgnoreCase))
                {
                    tags.Add("tin-dung-khdn");
                    tags.Add("khdn");
                }
                if (res.TargetLevel.Contains("Khách hàng Cá nhân", StringComparison.OrdinalIgnoreCase))
                {
                    tags.Add("tin-dung-khcn");
                    tags.Add("khcn");
                }
            }

            // 3. Thẻ từ Purpose
            if (!string.IsNullOrWhiteSpace(res.AssessmentPurpose))
            {
                if (res.AssessmentPurpose.Contains("định kỳ", StringComparison.OrdinalIgnoreCase))
                    tags.Add("kiem-tra-dinh-ky");
                if (res.AssessmentPurpose.Contains("Đợt 2", StringComparison.OrdinalIgnoreCase) && res.BenchmarkYear.HasValue)
                    tags.Add($"dot-2-{res.BenchmarkYear}");
                else if (res.AssessmentPurpose.Contains("Đợt 1", StringComparison.OrdinalIgnoreCase) && res.BenchmarkYear.HasValue)
                    tags.Add($"dot-1-{res.BenchmarkYear}");
            }

            // 4. Thẻ từ Year
            if (res.BenchmarkYear.HasValue)
            {
                tags.Add($"nam-{res.BenchmarkYear}");
            }

            // 5. Thẻ từ Org
            if (!string.IsNullOrWhiteSpace(res.IssuingOrg))
            {
                if (res.IssuingOrg.Contains("Agribank", StringComparison.OrdinalIgnoreCase)) tags.Add("agribank");
                if (res.IssuingOrg.Contains("Vietcombank", StringComparison.OrdinalIgnoreCase)) tags.Add("vietcombank");
                if (res.IssuingOrg.Contains("BIDV", StringComparison.OrdinalIgnoreCase)) tags.Add("bidv");
                if (res.IssuingOrg.Contains("Chi nhánh", StringComparison.OrdinalIgnoreCase)) tags.Add("chi-nhanh");
            }

            // 6. Thẻ từ File name
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var lowerFile = fileName.ToLowerInvariant();
                if (lowerFile.Contains("tin dung") || lowerFile.Contains("tín dụng")) tags.Add("tin-dung");
            }

            return tags.Where(t => !string.IsNullOrWhiteSpace(t) && t.Length >= 2).ToList();
        }

        private static string CleanExtractedText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return Regex.Replace(input, @"[|\t]+", " ").Trim(' ', ':', '-', '–', '—');
        }

        private static string NormalizeLevelName(string text)
        {
            var t = text.Trim();
            if (t.Equals("TÍN DỤNG KHÁCH HÀNG DOANH NGHIỆP", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("TÍN DỤNG KHDN", StringComparison.OrdinalIgnoreCase))
            {
                return "Tín dụng Khách hàng Doanh nghiệp";
            }
            if (t.Equals("TÍN DỤNG KHÁCH HÀNG CÁ NHÂN", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("TÍN DỤNG KHCN", StringComparison.OrdinalIgnoreCase))
            {
                return "Tín dụng Khách hàng Cá nhân";
            }
            return NormalizeTitleCase(t);
        }

        public static string NormalizeTitleCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var words = input.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            for (int i = 0; i < words.Length; i++)
            {
                var w = words[i];
                if (w.Length == 1)
                {
                    sb.Append(char.ToUpper(w[0]));
                }
                else if (w.Equals("KHDN", StringComparison.OrdinalIgnoreCase) ||
                         w.Equals("KHCN", StringComparison.OrdinalIgnoreCase) ||
                         w.Equals("GPLX", StringComparison.OrdinalIgnoreCase) ||
                         w.Equals("THPT", StringComparison.OrdinalIgnoreCase) ||
                         w.Equals("GDPT", StringComparison.OrdinalIgnoreCase) ||
                         w.Equals("AML", StringComparison.OrdinalIgnoreCase) ||
                         w.Equals("HSE", StringComparison.OrdinalIgnoreCase) ||
                         w.Equals("PCCC", StringComparison.OrdinalIgnoreCase) ||
                         w.Equals("CNTT", StringComparison.OrdinalIgnoreCase) ||
                         w.Equals("ISO", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append(w.ToUpperInvariant());
                }
                else if (i > 0 && (w.Equals("và", StringComparison.OrdinalIgnoreCase) ||
                                  w.Equals("với", StringComparison.OrdinalIgnoreCase) ||
                                  w.Equals("tại", StringComparison.OrdinalIgnoreCase) ||
                                  w.Equals("cho", StringComparison.OrdinalIgnoreCase) ||
                                  w.Equals("của", StringComparison.OrdinalIgnoreCase)))
                {
                    sb.Append(w.ToLowerInvariant());
                }
                else
                {
                    sb.Append(char.ToUpper(w[0]) + w.Substring(1).ToLowerInvariant());
                }
                if (i < words.Length - 1) sb.Append(' ');
            }
            return sb.ToString();
        }

        private static string CleanToTag(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            // Chuyển tiếng Việt có dấu thành không dấu
            string normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != UnicodeCategory.NonSpacingMark)
                {
                    if (char.IsLetterOrDigit(c))
                        sb.Append(char.ToLowerInvariant(c));
                    else if (char.IsWhiteSpace(c) || c == '-' || c == '_')
                        sb.Append('-');
                }
            }
            var tag = Regex.Replace(sb.ToString(), @"-+", "-").Trim('-');
            return tag;
        }
    }
}
