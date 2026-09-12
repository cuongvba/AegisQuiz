using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services
{
    public class SmartOptionShufflerService : ISmartOptionShufflerService
    {
        private readonly IVietnameseTextCorrectionService _correctionService;

        public SmartOptionShufflerService(IVietnameseTextCorrectionService correctionService)
        {
            _correctionService = correctionService;
        }

        public ShuffledOptionsResult ShuffleOptions(
            List<string> options, 
            string? currentCorrectAnswer, 
            string? currentCorrectOptionText = null)
        {
            var result = new ShuffledOptionsResult();

            if (options == null || options.Count <= 1)
            {
                result.Options = options != null ? new List<string>(options) : new List<string>();
                result.CorrectAnswer = currentCorrectAnswer;
                result.CorrectOptionText = currentCorrectOptionText;
                result.WasShuffled = false;
                result.Message = "Không đủ số lượng phương án để xáo trộn.";
                return result;
            }

            // Bước 1: Chuẩn hóa phương án bằng maprepl.db và dịch các cụm từ neo (Anchor Phrases) theo luật VN Correct
            var cleanedOptions = options.Select(opt => 
            {
                var corrected = _correctionService.CorrectText(opt?.Trim() ?? string.Empty);
                return TranslateAnchorOption(corrected);
            }).ToList();

            // Xác định vị trí ban đầu của đáp án đúng (1-based index) đa hình (Số, Chữ cái, hoặc Chuỗi văn bản)
            int originalCorrect1BasedIndex = ResolveOriginalCorrectIndex(currentCorrectAnswer, currentCorrectOptionText, cleanedOptions);

            // Bước 2: Phát hiện phương án neo (Anchor / Synthesis Options)
            // Ví dụ: "Tất cả các phương án trên đều đúng", "Không có phương án nào đúng", "Cả (1) và (2)"
            int anchorIndex = -1;
            for (int i = 0; i < cleanedOptions.Count; i++)
            {
                if (_correctionService.IsAnchorOption(cleanedOptions[i]) || 
                    _correctionService.ContainsPositionalReference(cleanedOptions[i]) ||
                    IsAnchorPhrase(cleanedOptions[i]))
                {
                    anchorIndex = i;
                    break;
                }
            }

            // Danh sách các cặp (Nội dung, Vị trí 1-based ban đầu)
            var indexedOptions = cleanedOptions
                .Select((opt, idx) => new OptionItem { Text = opt, OriginalIndex1Based = idx + 1, IsAnchor = (idx == anchorIndex) })
                .ToList();

            var finalShuffled = new List<OptionItem>();

            if (anchorIndex >= 0)
            {
                // Có phương án neo: Tách phương án neo ra để GHIM CỐ ĐỊNH Ở CUỐI CÙNG (Last position)
                var anchorItem = indexedOptions[anchorIndex];
                var independentItems = indexedOptions.Where((_, idx) => idx != anchorIndex).ToList();

                // Xáo trộn ngẫu nhiên các phương án độc lập bằng Fisher-Yates
                FisherYatesShuffle(independentItems);

                finalShuffled.AddRange(independentItems);
                finalShuffled.Add(anchorItem); // Luôn ở cuối cùng
                result.HasAnchorPinned = true;
            }
            else
            {
                // Không có phương án neo: Xáo trộn 100% ngẫu nhiên toàn bộ danh sách
                FisherYatesShuffle(indexedOptions);
                finalShuffled.AddRange(indexedOptions);
                result.HasAnchorPinned = false;
            }

            // Bước 3: Tạo bản đồ ánh xạ chỉ số cũ -> mới (1-based)
            var mappingOldToNew = new Dictionary<int, int>();
            for (int newIdx0 = 0; newIdx0 < finalShuffled.Count; newIdx0++)
            {
                var item = finalShuffled[newIdx0];
                mappingOldToNew[item.OriginalIndex1Based] = newIdx0 + 1;
            }
            result.IndexMapping = mappingOldToNew;

            // Bước 4: Dynamic Re-mapping nội dung cho phương án neo nếu có dạng "Cả (1) và (2)"
            for (int i = 0; i < finalShuffled.Count; i++)
            {
                var item = finalShuffled[i];
                if (item.IsAnchor)
                {
                    // Tự động viết lại chỉ số tham chiếu theo vị trí mới
                    string rewritten = Regex.Replace(item.Text, @"\(([1-4])\)", m =>
                    {
                        if (int.TryParse(m.Groups[1].Value, out int refOld) && mappingOldToNew.TryGetValue(refOld, out int refNew))
                        {
                            return $"({refNew})";
                        }
                        return m.Value;
                    });

                    // Sắp xếp lại chỉ số tăng dần cho thẩm mỹ biên tập chuẩn mực: "Cả (2) và (1)" hoặc "(2) và (1)" -> "Cả (1) và (2)"
                    rewritten = Regex.Replace(rewritten, @"(?:Cả\s+)?\(([1-4])\)\s+và\s+\(([1-4])\)", m =>
                    {
                        int a = int.Parse(m.Groups[1].Value);
                        int b = int.Parse(m.Groups[2].Value);
                        int min = Math.Min(a, b);
                        int max = Math.Max(a, b);
                        return $"Cả ({min}) và ({max})";
                    }, RegexOptions.IgnoreCase);

                    // Sắp xếp lại 3 chỉ số nếu có: "Cả (3), (1) và (2)" -> "Cả (1), (2) và (3)"
                    rewritten = Regex.Replace(rewritten, @"(?:Cả\s+)?\(([1-4])\)[,\s]+\(([1-4])\)\s+và\s+\(([1-4])\)", m =>
                    {
                        var list = new List<int>
                        {
                            int.Parse(m.Groups[1].Value),
                            int.Parse(m.Groups[2].Value),
                            int.Parse(m.Groups[3].Value)
                        };
                        list.Sort();
                        return $"Cả ({list[0]}), ({list[1]}) và ({list[2]})";
                    }, RegexOptions.IgnoreCase);

                    // Sắp xếp lại dạng hoặc nếu có: "(2) hoặc (1)" -> "(1) hoặc (2)"
                    rewritten = Regex.Replace(rewritten, @"\(([1-4])\)\s+hoặc\s+\(([1-4])\)", m =>
                    {
                        int a = int.Parse(m.Groups[1].Value);
                        int b = int.Parse(m.Groups[2].Value);
                        int min = Math.Min(a, b);
                        int max = Math.Max(a, b);
                        return $"({min}) hoặc ({max})";
                    }, RegexOptions.IgnoreCase);

                    item.Text = rewritten;
                }
            }

            // Bước 5: Cập nhật đáp án đúng mới chuẩn xác 100%
            result.Options = finalShuffled.Select(x => x.Text).ToList();
            if (originalCorrect1BasedIndex >= 1 && mappingOldToNew.TryGetValue(originalCorrect1BasedIndex, out int newCorrect1Based))
            {
                result.CorrectAnswer = newCorrect1Based.ToString();
                result.CorrectOptionText = result.Options[newCorrect1Based - 1];
            }
            else
            {
                result.CorrectAnswer = currentCorrectAnswer;
                result.CorrectOptionText = currentCorrectOptionText;
            }

            result.WasShuffled = true;
            result.Message = result.HasAnchorPinned
                ? "Đã xáo trộn phương án độc lập và ghim cố định phương án tổng hợp ở vị trí cuối cùng."
                : "Đã xáo trộn ngẫu nhiên toàn bộ phương án thành công.";

            return result;
        }

        /// <summary>
        /// Bóc tách các nhãn tiền tố của phương án (ví dụ "A.", "B.", "1.", "[A]", "(A)") nếu người dùng nhập trong ô Excel.
        /// Bảo toàn các biểu thức logic tham chiếu như "(1) hoặc (2)", "Cả (1) và (2)", "1 và 2", "1, 2".
        /// </summary>
        public static string StripOptionPrefix(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var t = text.Trim();

            // Nếu chuỗi là biểu thức logic như "(1) hoặc (2)", "Cả (1) và (2)", "1 và 2", "1, 2", "1,2", không bóc tách
            if (Regex.IsMatch(t, @"^(?:cả\s+)?(?:\([1-4]\)|[1-4]|[a-d]|i{1,4})\s*(?:và|hoặc|,)\s*(?:\([1-4]\)|[1-4]|[a-d]|i{1,4})", RegexOptions.IgnoreCase))
            {
                return t;
            }

            // Bóc tách nhãn tiền tố: "A.", "B.", "C.", "D.", "a)", "b)", "1.", "2.", "(A)", "[A]"
            var stripped = Regex.Replace(t, @"^(?:\[[A-Ha-h]\]|\([A-Ha-h]\)|[A-Ha-h][\.\:\)\-]|[0-9]+[\.\:\)\-])\s*", "", RegexOptions.IgnoreCase).Trim();
            return string.IsNullOrWhiteSpace(stripped) ? t : stripped;
        }

        private static int ParseIndexToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return -1;
            var t = token.Trim().Trim('(', ')', '[', ']');
            if (int.TryParse(t, out int num) && num >= 1 && num <= 4) return num;
            if (t.Length == 1)
            {
                char c = char.ToUpperInvariant(t[0]);
                if (c >= 'A' && c <= 'D') return c - 'A' + 1;
            }
            var upper = t.ToUpperInvariant();
            if (upper == "I") return 1;
            if (upper == "II") return 2;
            if (upper == "III") return 3;
            if (upper == "IV") return 4;
            return -1;
        }

        /// <summary>
        /// Dịch và chuẩn hóa các cụm từ tham chiếu vị trí/phương án tổng hợp sang định dạng chuẩn Smart Option Shuffler theo luật VN Correct.
        /// Bao phủ triệt để: "1 và 2", "1,2", "1, 2", "1 hoặc 2", "1 và 3", "2 và 3", "1, 3", "1,3", "2, 3", "2,3",
        /// "1 và 4", "1, 4", "1,4", "1 hoặc 4", "2 và 4", "3 và 4", "A và B", "A, B", "A,B", "A hoặc B", "I và II", "I, II", "I,II",
        /// "1, 2 và 3", "1, 2, 3", "1,2,3", "A, B, C", "I, II, III", "(1) và (2)", "(1) hoặc (2)", "(1), (2)", v.v.
        /// </summary>
        public static string TranslateAnchorOption(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var t = text.Trim();

            // 0. Bóc tách tiền tố nhãn (A., B., 1., [A], (A)...) và dấu kết thúc (. : ;) để so khớp ngữ nghĩa thực tế
            var stripped = StripOptionPrefix(t).TrimEnd('.', ':', ';').Trim();

            // 1. Chuẩn hóa nhóm Tất cả các phương án trên đều đúng
            if (Regex.IsMatch(stripped, @"^(?:" +
                @"tất\s+cả\s+(?:các\s+)?(?:phương|đáp)\s+án\s+trên(?:\s+đều\s+đúng|\s+đều\s+chính\s+xác)?" +
                @"|tất\s+cả\s+(?:các\s+)?(?:phương|đáp)\s+án(?:\s+đều\s+đúng|\s+đều\s+chính\s+xác)" +
                @"|tất\s+cả\s+(?:các\s+)?(?:ý|câu|lựa\s+chọn)\s+trên(?:\s+đều\s+đúng|\s+đều\s+chính\s+xác)?" +
                @"|tất\s+cả\s+(?:các\s+)?(?:ý|câu|lựa\s+chọn)(?:\s+đều\s+đúng|\s+đều\s+chính\s+xác)" +
                @"|tất\s+cả\s+đều\s+(?:đúng|chính\s+xác)" +
                @"|các\s+(?:phương|đáp)\s+án\s+trên\s+đều\s+đúng" +
                @"|các\s+(?:ý|lựa\s+chọn)\s+trên\s+đều\s+đúng" +
                @"|cả\s+(?:3|4|ba|bốn)\s+(?:phương|đáp)\s+án\s+trên(?:\s+đều\s+đúng|\s+đều\s+chính\s+xác)?" +
                @"|cả\s+(?:3|4|ba|bốn)\s+ý\s+trên(?:\s+đều\s+đúng|\s+đều\s+chính\s+xác)?" +
                @"|cả\s+[a-d][,\s]+[a-d][,\s]+[a-d](?:[,\s]+[a-d])?\s+đều\s+đúng" +
                @"|cả\s+[1-4][,\s]+[1-4][,\s]+[1-4](?:[,\s]+[1-4])?\s+đều\s+đúng" +
                @"|cả\s+[a-d][,\s]+[a-d]\s+và\s+[a-d]\s+đều\s+đúng" +
                @"|cả\s+[1-4][,\s]+[1-4]\s+và\s+[1-4]\s+đều\s+đúng" +
                @")$", RegexOptions.IgnoreCase))
            {
                return "Tất cả các phương án trên đều đúng";
            }

            // 2. Chuẩn hóa nhóm Không có phương án nào đúng
            if (Regex.IsMatch(stripped, @"^(?:" +
                @"không\s+(?:có\s+)?(?:phương|đáp)\s+án\s+nào\s+(?:đúng|chính\s+xác)" +
                @"|không\s+(?:có\s+)?(?:ý|câu|lựa\s+chọn)\s+nào\s+(?:đúng|chính\s+xác)" +
                @"|không\s+(?:có\s+)?câu\s+trả\s+lời\s+nào\s+đúng" +
                @"|không\s+(?:phương|đáp)\s+án\s+nào\s+(?:đúng|chính\s+xác)" +
                @"|tất\s+cả\s+(?:các\s+)?(?:phương|đáp)\s+án\s+trên\s+đều\s+sai" +
                @"|tất\s+cả\s+(?:các\s+)?(?:ý|câu|lựa\s+chọn)\s+trên\s+đều\s+sai" +
                @"|tất\s+cả\s+đều\s+sai" +
                @"|cả\s+(?:3|4|ba|bốn)\s+(?:phương|đáp)\s+án\s+trên\s+đều\s+sai" +
                @"|cả\s+[a-d][,\s]+[a-d][,\s]+[a-d]\s+đều\s+sai" +
                @"|cả\s+[1-4][,\s]+[1-4][,\s]+[1-4]\s+đều\s+sai" +
                @")$", RegexOptions.IgnoreCase))
            {
                return "Không có phương án nào đúng";
            }

            // 3. Chuẩn hóa nhóm kết hợp hai phương án trên: "Cả hai phương án trên", "Cả 2 đáp án trên"
            if (Regex.IsMatch(stripped, @"^(?:cả\s+)?(?:hai|2)\s+(?:phương\s+án|đáp\s+án|ý)\s+trên(?:\s+đều\s+đúng)?$", RegexOptions.IgnoreCase))
            {
                return "Cả (1) và (2)";
            }

            // 4. Chuẩn hóa nhóm tổ hợp 3 phương án: "1, 2 và 3", "1, 2, 3", "1,2,3", "A, B, C", "I, II, III", "1, 2 và 4", v.v.
            const string tokPat = @"(?:\(?([1-4]|[a-d]|i{1,4})\)?)";
            const string pfxPat = @"^(?:cả\s+|đáp\s+án\s+|phương\s+án\s+|ý\s+|câu\s+|cả\s+(?:2|hai)\s+(?:ý|đáp\s+án|phương\s+án)\s+)?";
            const string sfxPat = @"(?:\s+đều\s+đúng|\s+đều\s+chính\s+xác|\s+đúng|\s+chính\s+xác)?$";

            var m3 = Regex.Match(stripped, pfxPat + tokPat + @"[,\s]+" + tokPat + @"(?:[,\s]+|(?:\s+và\s+))" + tokPat + sfxPat, RegexOptions.IgnoreCase);
            if (m3.Success)
            {
                int v1 = ParseIndexToken(m3.Groups[1].Value);
                int v2 = ParseIndexToken(m3.Groups[2].Value);
                int v3 = ParseIndexToken(m3.Groups[3].Value);
                if (v1 > 0 && v2 > 0 && v3 > 0 && v1 != v2 && v2 != v3 && v1 != v3)
                {
                    var sorted = new List<int> { v1, v2, v3 };
                    sorted.Sort();
                    return $"Cả ({sorted[0]}), ({sorted[1]}) và ({sorted[2]})";
                }
            }

            // 5. Chuẩn hóa nhóm tổ hợp 2 phương án toàn diện:
            // Phục vụ triệt để tất cả dạng: "1 và 2", "1,2", "1, 2", "1 hoặc 2", "1 và 3", "1,3", "1, 3", "2 và 3", "2,3", "2, 3",
            // "1 và 4", "1, 4", "1,4", "1 hoặc 4", "2 và 4", "3 và 4", "A và B", "A, B", "A,B", "A hoặc B", "I và II", "I, II", "I,II",
            // "(1) và (2)", "(1) hoặc (2)", "(1), (2)", "Cả 1 và 2", "Đáp án 1, 2", "Ý 1 và 2", v.v.
            var m2 = Regex.Match(stripped, pfxPat + tokPat + @"\s*(và|hoặc|,)\s*" + tokPat + sfxPat, RegexOptions.IgnoreCase);
            if (m2.Success)
            {
                int v1 = ParseIndexToken(m2.Groups[1].Value);
                string op = m2.Groups[2].Value.ToLowerInvariant();
                int v2 = ParseIndexToken(m2.Groups[3].Value);
                if (v1 > 0 && v2 > 0 && v1 != v2)
                {
                    int min = Math.Min(v1, v2);
                    int max = Math.Max(v1, v2);
                    if (op.Contains("hoặc"))
                    {
                        return $"({min}) hoặc ({max})";
                    }
                    else
                    {
                        return $"Cả ({min}) và ({max})";
                    }
                }
            }

            // 6. Chuẩn hóa nhóm tham chiếu đơn lẻ: "Chỉ có ý 1", "Chỉ có đáp án A", "Chỉ có 1"
            var mSingle = Regex.Match(stripped, @"^chỉ\s+có\s+(?:ý\s+|đáp\s+án\s+|phương\s+án\s+)?(?:\(?([1-4]|[a-d]|i{1,4})\)?)$", RegexOptions.IgnoreCase);
            if (mSingle.Success)
            {
                int v = ParseIndexToken(mSingle.Groups[1].Value);
                if (v > 0)
                {
                    return $"Chỉ có ({v})";
                }
            }

            return t;
        }

        public static bool IsAnchorPhrase(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var t = StripOptionPrefix(text).Trim();
            return t.StartsWith("Tất cả các phương án", StringComparison.OrdinalIgnoreCase) ||
                   t.StartsWith("Không có phương án nào", StringComparison.OrdinalIgnoreCase) ||
                   t.StartsWith("Cả (", StringComparison.OrdinalIgnoreCase) ||
                   t.StartsWith("Chỉ có (", StringComparison.OrdinalIgnoreCase) ||
                   Regex.IsMatch(t, @"^\([1-4]\)\s+(?:và|hoặc|,)\s+\([1-4]\)", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(t, @"^(?:Cả\s+)?\([1-4]\)", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(t, @"^(?:Cả\s+|Ý\s+|Đáp\s+án\s+|Phương\s+án\s+|Câu\s+)?(?:\(?([1-4]|[a-d]|i{1,4})\)?)\s*(?:và|hoặc|,)\s*(?:\(?([1-4]|[a-d]|i{1,4})\)?)", RegexOptions.IgnoreCase);
        }

        private int ResolveOriginalCorrectIndex(string? currentCorrectAnswer, string? currentCorrectOptionText, List<string> cleanedOptions)
        {
            if (int.TryParse(currentCorrectAnswer, out int parsedIdx) && parsedIdx >= 1 && parsedIdx <= cleanedOptions.Count)
            {
                return parsedIdx;
            }

            if (!string.IsNullOrWhiteSpace(currentCorrectAnswer))
            {
                var trimmed = currentCorrectAnswer.Trim();

                // 1. Nhận diện chữ cái đơn lẻ 'A', 'B', 'C', 'D' hoặc "A." / "C)"
                var letterMatch = Regex.Match(trimmed, @"^\(?([A-Ha-h])[\.\)]?$");
                if (letterMatch.Success)
                {
                    int lIdx = char.ToUpperInvariant(letterMatch.Groups[1].Value[0]) - 'A' + 1;
                    if (lIdx >= 1 && lIdx <= cleanedOptions.Count)
                    {
                        return lIdx;
                    }
                }

                // 2. Khớp chuỗi văn bản của đáp án
                var normAnswer = TranslateAnchorOption(_correctionService.CorrectText(trimmed));
                var strippedNormAnswer = StripOptionPrefix(normAnswer);

                for (int i = 0; i < cleanedOptions.Count; i++)
                {
                    var cleanOpt = cleanedOptions[i];
                    var strippedOpt = StripOptionPrefix(cleanOpt);

                    if (string.Equals(cleanOpt, normAnswer, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(cleanOpt, trimmed, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(strippedOpt, strippedNormAnswer, StringComparison.OrdinalIgnoreCase))
                    {
                        return i + 1;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(currentCorrectOptionText))
            {
                var normText = TranslateAnchorOption(_correctionService.CorrectText(currentCorrectOptionText.Trim()));
                var strippedNormText = StripOptionPrefix(normText);

                for (int i = 0; i < cleanedOptions.Count; i++)
                {
                    var cleanOpt = cleanedOptions[i];
                    var strippedOpt = StripOptionPrefix(cleanOpt);

                    if (string.Equals(cleanOpt, normText, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(cleanOpt, currentCorrectOptionText.Trim(), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(strippedOpt, strippedNormText, StringComparison.OrdinalIgnoreCase))
                    {
                        return i + 1;
                    }
                }
            }

            return -1;
        }

        private static void FisherYatesShuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private class OptionItem
        {
            public string Text { get; set; } = string.Empty;
            public int OriginalIndex1Based { get; set; }
            public bool IsAnchor { get; set; }
        }
    }
}
