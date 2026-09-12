using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AegisQuiz.Infrastructure.Excel
{
    /// <summary>
    /// [Smart Semantic Abbreviation Engine - SSAE]
    /// Động cơ siêu vi nhận thức rút gọn mã chủ đề thông minh.
    /// Áp dụng các quy tắc viết tắt chuẩn mực trong Ngân hàng, Tài chính, Giáo dục và Công nghệ.
    /// Giảm độ dài mã từ 50-70 ký tự xuống còn 6-18 ký tự siêu gọn gàng, trực quan và dễ nhớ.
    /// </summary>
    public static class SmartTopicAbbreviationEngine
    {
        // ── 1. TỪ ĐIỂN ÁNH XẠ CỤM TỪ NGHIỆP VỤ CHUYÊN NGÀNH ─────────────────
        private static readonly Dictionary<string, string> IndustryPhrases = new(StringComparer.OrdinalIgnoreCase)
        {
            // === NGÂN HÀNG & TÀI CHÍNH (BANKING) ===
            { "khach hang doanh nghiep vua va nho", "KHDN_SME" },
            { "khach hang doanh nghiep", "KHDN" },
            { "khach hang ca nhan", "KHCN" },
            { "khdn", "KHDN" },
            { "khcn", "KHCN" },
            { "sme", "SME" },
            { "tham dinh tai san bao dam", "TDTS_TSBD" },
            { "tai san bao dam", "TSBD" },
            { "tsbd", "TSBD" },
            { "tham dinh tai san", "TDTS" },
            { "tham dinh gia", "TDG" },
            { "ke toan giao dich noi bo", "KT_GDNB" },
            { "ke toan giao dich", "KTGD" },
            { "ke toan noi bo", "KTNB" },
            { "ke toan tong hop", "KTTH" },
            { "ke toan ngan hang", "KTNH" },
            { "ke toan", "KT" },
            { "kiem toan noi bo", "KTNB" },
            { "giao dich vien", "GDV" },
            { "kiem ngan vien", "KNV" },
            { "kiem ngan", "KN" },
            { "thu quy", "TQ" },
            { "tien te kho quy", "TTKQ" },
            { "quan ly kho quy", "QLKQ" },
            { "thanh toan quoc te", "TTQT" },
            { "the tin dung quoc te", "THE_TDQT" },
            { "the tin dung", "THE_TD" },
            { "the ghi no", "THE_GN" },
            { "tin dung quoc te", "TDQT" },
            { "tin dung tieu dung", "TDTD" },
            { "tin dung ban le", "TDBL" },
            { "tin dung trung dai han", "TD_TDH" },
            { "tin dung ngan han", "TD_NH" },
            { "tin dung", "TD" },
            { "cap tin dung", "TD" },
            { "cho vay tieu dung", "CVTD" },
            { "cho vay", "CV" },
            { "quan tri rui ro tin dung", "QTRR_TD" },
            { "quan tri rui ro hoat dong", "QTRR_HD" },
            { "quan tri rui ro thi truong", "QTRR_TT" },
            { "quan tri rui ro", "QTRR" },
            { "quan ly rui ro", "QLRR" },
            { "phong chong rua tien", "PCRT" },
            { "chong rua tien", "AML" },
            { "rua tien", "AML" },
            { "aml", "AML" },
            { "phong chong tham nhung", "PCTN" },
            { "tai tro thuong mai", "TTTM" },
            { "nguon von va kinh doanh ngoai te", "NV_KDNT" },
            { "kinh doanh ngoai te", "KDNT" },
            { "nguon von", "NV" },
            { "xu ly no co van de", "XLN" },
            { "xu ly no xau", "XLN" },
            { "xu ly no", "XLN" },
            { "thu hoi no", "THN" },
            { "dich vu the", "DV_THE" },
            { "phat hanh the", "PH_THE" },
            { "ngan hang dien tu", "E_BANK" },
            { "ngan hang so", "DIGITAL_BANK" },
            { "cong nghe thong tin", "CNTT" },
            { "an toan thong tin", "ATTT" },
            { "bao mat he thong", "SEC" },
            { "an ninh mang", "ANM" },
            { "chuyen doi so", "CDS" },
            { "quy trinh nghiep vu", "QTNV" },
            { "dao duc nghe nghiep", "DDNN" },
            { "van hoa doanh nghiep", "VHDN" },
            { "noi quy lao dong", "NQLD" },
            { "ky nang ban hang", "KNBH" },
            { "ky nang giao tiep", "KNGT" },
            { "cham soc khach hang", "CSKH" },
            { "xu ly khieu nai", "XLKN" },
            { "khieu nai", "KN" },
            { "khach hang", "KH" },
            { "dich vu khach hang", "DVKH" },
            { "bao hiem nhan tho", "BHNT" },
            { "bao hiem phi nhan tho", "BHPNT" },
            { "bao hiem", "BH" },
            { "bancassurance", "BANCA" },

            // === GIÁO DỤC, ĐÀO TẠO & THI CỬ (EDUCATION) ===
            { "tot nghiep trung hoc pho thong", "TN_THPT" },
            { "tot nghiep thpt", "TN_THPT" },
            { "giao duc pho thong 2018", "GDPT18" },
            { "giao duc pho thong", "GDPT" },
            { "trung hoc pho thong", "THPT" },
            { "trung hoc co so", "THCS" },
            { "tieu hoc", "TH" },
            { "lop 12", "12" },
            { "lop 11", "11" },
            { "lop 10", "10" },
            { "toan hoc", "TOAN" },
            { "hinh hoc khong gian", "HH_KG" },
            { "hinh hoc", "HINH" },
            { "dai so va giai tich", "DAI_SO" },
            { "giai tich", "GT" },
            { "vat ly", "LY" },
            { "hoa hoc", "HOA" },
            { "sinh hoc", "SINH" },
            { "lich su", "SU" },
            { "dia ly", "DIA" },
            { "giao duc kinh te va phap luat", "GDKT_PL" },
            { "tieng anh", "ENG" },
            { "ngoai ngu", "NN" },
            { "ngu van", "VAN" },
            { "tin hoc", "TIN" },

            // === SÁT HẠCH LÁI XE & GIAO THÔNG (GOV_DRIVING) ===
            { "sat hach lai xe", "SHLX" },
            { "giay phep lai xe", "GPLX" },
            { "gplx", "GPLX" },
            { "luat giao thong duong bo", "GTDB" },
            { "giao thong duong bo", "GTDB" },
            { "cau hoi diem liet", "DIEM_LIET" },
            { "diem liet", "DL" },
            { "sa hinh", "SA_HINH" },
            { "bien bao duong bo", "BIEN_BAO" },
            { "bien bao", "BB" },
            { "ky thuat lai xe", "KTLX" },
            { "quy tac giao thong", "QTGT" },
            { "hang b2", "B2" },
            { "hang b1", "B1" },
            { "hang c", "C" },
            { "hang d", "D" },
            { "hang e", "E" },
            { "hang fc", "FC" },
        };

        // Danh sách cụm từ nghiệp vụ sắp xếp ưu tiên từ dài đến ngắn (Longest Match First)
        private static readonly List<KeyValuePair<string, string>> SortedPhrases =
            IndustryPhrases.OrderByDescending(p => p.Key.Length).ToList();

        // ── 2. DANH TỪ DỪNG PHỤ TRỢ (STOPWORDS LOẠI BỎ) ───────────────────────
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "ve", "va", "cua", "cac", "nhung", "cho", "trong", "theo", "voi", "boi", "tai",
            "quy", "dinh", "huong", "dan", "nghiep", "vu", "ngan", "hang", "cau", "hoi",
            "bo", "de", "thi", "tuyen", "dung", "sat", "hach", "kiem", "tra", "danh", "gia",
            "tai", "lieu", "on", "tap", "chuong", "phan", "bai", "muc", "chuyen", "de",
            "so", "stt", "tong", "hop", "chuan", "nam", "dot", "danh", "cho"
        };

        private record MatchedToken(int Index, string Value);

        /// <summary>
        /// Rút gọn thông minh tên cụm từ (File, Sheet, Tên Nghiệp vụ) thành Mã ngắn gọn 4-16 ký tự
        /// </summary>
        public static string Abbreviate(string? rawText, int maxLen = 16)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return "GEN";

            // 1. Chuẩn hóa & loại bỏ số thứ tự đầu dòng ("01.", "1 -", "Bài 2: "...)
            var clean = CleanPrefixNumbers(rawText);

            // 2. Chuyển sang chuỗi không dấu dạng lower
            var nonAccent = RemoveDiacritics(clean).ToLowerInvariant();

            // 3. Tách nhận diện đợt thi & năm ("Đợt 2", "Năm 2026", "2026")
            string? periodSuffix = ExtractPeriodTag(nonAccent);
            // Xóa phần đợt/năm khỏi text để không bị trùng lặp
            nonAccent = CleanPeriodText(nonAccent);

            // 4. Quét từ điển cụm từ nghiệp vụ (Longest Match First) kèm vị trí xuất hiện
            var matchedTokens = new List<MatchedToken>();
            var maskSb = new StringBuilder(nonAccent);

            foreach (var kvp in SortedPhrases)
            {
                var phrase = kvp.Key;
                int idx = 0;
                while ((idx = maskSb.ToString().IndexOf(phrase, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    matchedTokens.Add(new MatchedToken(idx, kvp.Value));
                    // Đè khoảng trắng để không bị match lại cụm từ con
                    maskSb.Remove(idx, phrase.Length);
                    maskSb.Insert(idx, new string(' ', phrase.Length));
                    idx += phrase.Length;
                }
            }

            // 5. Với phần văn bản còn lại: lọc stopwords và lấy chữ cái đầu (Acronym)
            var remaining = maskSb.ToString();
            var rawWords = remaining.Split(new[] { ' ', '-', '_', '.', ',', ':', ';', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            var meaningfulWords = rawWords.Where(w => !StopWords.Contains(w) && w.Length > 1).ToList();

            if (meaningfulWords.Count > 0)
            {
                if (matchedTokens.Count == 0)
                {
                    if (meaningfulWords.Count == 1)
                    {
                        matchedTokens.Add(new MatchedToken(0, meaningfulWords[0].ToUpperInvariant()));
                    }
                    else if (meaningfulWords.Count == 2)
                    {
                        matchedTokens.Add(new MatchedToken(0, meaningfulWords[0].Length <= 4 ? meaningfulWords[0].ToUpperInvariant() : meaningfulWords[0].Substring(0, 3).ToUpperInvariant()));
                        matchedTokens.Add(new MatchedToken(1, meaningfulWords[1].Length <= 4 ? meaningfulWords[1].ToUpperInvariant() : meaningfulWords[1].Substring(0, 3).ToUpperInvariant()));
                    }
                    else
                    {
                        var acronym = string.Concat(meaningfulWords.Select(w => char.ToUpperInvariant(w[0])));
                        matchedTokens.Add(new MatchedToken(0, acronym));
                    }
                }
                else if (matchedTokens.Count <= 2 && meaningfulWords.Count <= 3)
                {
                    var subAcronym = string.Concat(meaningfulWords.Select(w => char.ToUpperInvariant(w[0])));
                    if (subAcronym.Length >= 2) matchedTokens.Add(new MatchedToken(999, subAcronym));
                }
            }

            // 6. Ghép các token theo đúng thứ tự xuất hiện ban đầu
            var sortedTokens = matchedTokens
                .OrderBy(t => t.Index)
                .Select(t => t.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // 7. Ghép thêm tag đợt/năm nếu có
            if (!string.IsNullOrWhiteSpace(periodSuffix))
            {
                sortedTokens.Add(periodSuffix);
            }

            if (sortedTokens.Count == 0)
            {
                sortedTokens.Add("TOPIC");
            }

            var result = string.Join("_", sortedTokens);
            result = Regex.Replace(result, @"_+", "_").Trim('_');

            if (result.Length > maxLen)
            {
                var parts = result.Split('_');
                var builder = new StringBuilder();
                foreach (var p in parts)
                {
                    if (builder.Length + p.Length + 1 <= maxLen)
                    {
                        if (builder.Length > 0) builder.Append('_');
                        builder.Append(p);
                    }
                    else if (builder.Length == 0)
                    {
                        return p.Substring(0, Math.Min(p.Length, maxLen));
                    }
                    else break;
                }
                return builder.ToString();
            }

            return result;
        }

        /// <summary>
        /// Xây dựng bộ thông tin Chủ đề Cha & Con chuẩn phân cấp Hierarchical Topic
        /// Đảm bảo Tên giữ nguyên 100% tiếng Việt có dấu, Mã siêu ngắn gọn, súc tích.
        /// </summary>
        public static (string ParentCode, string ParentName, string ChildCode, string ChildName, string Description)
            BuildHierarchicalTopicInfo(string? folderName, string? fileName, string? sheetName, int estimatedCount = 0)
        {
            var cleanSheet = !string.IsNullOrWhiteSpace(sheetName)
                ? Regex.Replace(sheetName, @"\s*\d+\s*(câu|cau)\s*.*$", "", RegexOptions.IgnoreCase).Trim()
                : string.Empty;

            // 1. Tên file gốc (giữ nguyên có dấu)
            string rawFileTitle = !string.IsNullOrWhiteSpace(fileName)
                ? Path.GetFileNameWithoutExtension(fileName).Trim()
                : (!string.IsNullOrWhiteSpace(cleanSheet) ? cleanSheet : "Chủ đề tổng hợp");

            string cleanFileTitle = CleanPrefixNumbers(rawFileTitle);

            // 2. Thư mục cha (Parent Topic)
            string folder = !string.IsNullOrWhiteSpace(folderName) ? folderName.Trim() : "CHUNG";
            bool isSpecificBatchFolder = Regex.IsMatch(folder, @"\b20\d\d[-_]DOT\d+\b", RegexOptions.IgnoreCase);

            string parentCode;
            string parentName;

            if (isSpecificBatchFolder)
            {
                // Thư mục đợt thi chuẩn như 2026-DOT2 -> 2026_DOT2
                parentCode = folder.Replace("-", "_").ToUpperInvariant();
                parentName = folder;
            }
            else if (folder.Equals("CHUNG", StringComparison.OrdinalIgnoreCase) || folder.Length <= 1)
            {
                parentCode = "CHUNG";
                parentName = "Chủ đề chung";
            }
            else
            {
                parentCode = Abbreviate(folder, 10);
                parentName = folder;
            }

            // 3. Mã & Tên Chủ đề con (Child Topic)
            string fileAbbr = Abbreviate(cleanFileTitle, 14);

            string childCode;
            string childName;

            bool isGenericSheet = string.IsNullOrWhiteSpace(cleanSheet)
                || cleanSheet.Equals(rawFileTitle, StringComparison.OrdinalIgnoreCase)
                || cleanSheet.Equals(cleanFileTitle, StringComparison.OrdinalIgnoreCase)
                || Regex.IsMatch(cleanSheet, @"^(sheet|trang|data|dulieu)\s*\d*$", RegexOptions.IgnoreCase);

            if (!isGenericSheet)
            {
                string sheetAbbr = Abbreviate(cleanSheet, 10);
                if (fileAbbr.Contains(sheetAbbr, StringComparison.OrdinalIgnoreCase))
                {
                    childCode = fileAbbr;
                }
                else
                {
                    childCode = $"{fileAbbr}_{sheetAbbr}";
                    if (childCode.Length > 22)
                    {
                        childCode = $"{fileAbbr.Substring(0, Math.Min(fileAbbr.Length, 10))}_{sheetAbbr}";
                    }
                }
                childName = $"{rawFileTitle} — {cleanSheet}";
            }
            else
            {
                childCode = fileAbbr;
                childName = rawFileTitle;
            }

            // Nếu nằm trong thư mục đợt thi cụ thể (như 2026_DOT2), gắn tiền tố đợt thi vào childCode để định tuyến chuẩn
            if (isSpecificBatchFolder && !childCode.StartsWith(parentCode, StringComparison.OrdinalIgnoreCase))
            {
                childCode = $"{parentCode}_{childCode}";
            }

            // Mô tả chuẩn
            string countStr = estimatedCount > 0 ? $"{estimatedCount} câu hỏi" : "Tự động trích xuất";
            string desc = $"{childName} | Thư mục: {parentName} | {countStr}";

            return (parentCode, parentName, childCode, childName, desc);
        }

        // ── HELPER UTILITIES ─────────────────────────────────────────────────

        private static string CleanPrefixNumbers(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var cleaned = Regex.Replace(text.Trim(), @"^(?:bài|phần|chương|đề|câu|stt)?\s*\d+[\.\:\-\s_]+\s*", "", RegexOptions.IgnoreCase);
            return cleaned.Trim();
        }

        private static string? ExtractPeriodTag(string text)
        {
            var mDot = Regex.Match(text, @"(?:dot|d)\s*0?(\d+)\b", RegexOptions.IgnoreCase);
            var mYear = Regex.Match(text, @"\b20(2[0-9])\b");

            string? dotStr = mDot.Success ? $"D{mDot.Groups[1].Value}" : null;
            string? yearStr = mYear.Success ? mYear.Groups[1].Value : null;

            if (dotStr != null && yearStr != null) return $"{dotStr}_{yearStr}";
            if (dotStr != null) return dotStr;
            if (yearStr != null) return yearStr;
            return null;
        }

        private static string CleanPeriodText(string text)
        {
            var res = Regex.Replace(text, @"(?:dot|d)\s*0?\d+\b", " ", RegexOptions.IgnoreCase);
            res = Regex.Replace(res, @"\b202[0-9]\b", " ");
            res = Regex.Replace(res, @"(?:nam|năm)\s*", " ", RegexOptions.IgnoreCase);
            return res;
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat == UnicodeCategory.NonSpacingMark) continue;
                if (ch == 'đ' || ch == 'Đ') sb.Append('d');
                else sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
