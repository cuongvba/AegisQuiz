using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AegisQuiz.Infrastructure.Services.Docx
{
    /// <summary>
    /// [Docx Table Question Extractor]
    /// Chuyên trách phân giải bảng biểu trong OpenXML:
    /// - Bóc tách Bảng đáp án tổng hợp (Phần I: Trắc nghiệm, Phần II: Đúng/Sai, Phần III: Trả lời ngắn, Lưới đa cột).
    /// - Chuyển đổi bảng số liệu, bảng biến thiên trong đề thi sang bảng Markdown chuẩn.
    /// </summary>
    public static class DocxTableQuestionExtractor
    {
        private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        /// <summary>
        /// Bóc tách bảng đáp án trong OpenXML sang map 3 chiều [examCode][section][qNum].
        /// [Sprint 1 — 7.2] Tham số defaultExamPrefix được truyền vào để thay thế hardcode "TOAN_DE_".
        /// </summary>
        public static Dictionary<string, Dictionary<string, Dictionary<int, string>>> ExtractAnswerTables(
            XDocument doc,
            string defaultExamPrefix = "DE")
        {
            var map = new Dictionary<string, Dictionary<string, Dictionary<int, string>>>(StringComparer.OrdinalIgnoreCase);
            var body = doc.Descendants(W + "body").FirstOrDefault();
            if (body == null) return map;

            // Chuẩn hóa prefix: đảm bảo kết thúc bằng "_"
            string prefix = string.IsNullOrWhiteSpace(defaultExamPrefix) ? "DE" : defaultExamPrefix.Trim().TrimEnd('_');
            string currentExam = "DEFAULT";

            foreach (var el in body.Elements())
            {
                if (el.Name == W + "p")
                {
                    var pText = string.Concat(el.Descendants(W + "t").Select(t => t.Value)).Trim();
                    var matchExam = Regex.Match(pText, @"^(?:ĐỀ|D[E|Ê])\s*(?:SỐ\s*|THI\s*|ÔN\s*THI\s*)?(\d+)\b", RegexOptions.IgnoreCase);
                    if (matchExam.Success)
                    {
                        int examNum = int.Parse(matchExam.Groups[1].Value);
                        currentExam = $"{prefix}_{examNum:D2}"; // [Sprint 1 - 7.2] Dùng prefix động
                    }
                }
                else if (el.Name == W + "tbl")
                {
                    var rows = el.Descendants(W + "tr").ToList();
                    if (rows.Count < 2) continue;

                    var row0 = rows[0].Descendants(W + "tc").Select(tc => string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim()).ToList();
                    var row1 = rows[1].Descendants(W + "tc").Select(tc => string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim()).ToList();

                    // Dạng 1: Bảng trắc nghiệm 1 đáp án hoặc trả lời ngắn
                    if (row0.Count >= 2 && Regex.IsMatch(row0[0], @"^(?:Câu|C.u)$", RegexOptions.IgnoreCase)
                        && Regex.IsMatch(row1[0], @"^(?:Chọn|Ch.n)$", RegexOptions.IgnoreCase))
                    {
                        if (!map.ContainsKey(currentExam)) map[currentExam] = new();
                        for (int c = 1; c < Math.Min(row0.Count, row1.Count); c++)
                        {
                            if (int.TryParse(row0[c], out int qNum))
                            {
                                string ans = row1[c].Trim();
                                string sec = Regex.IsMatch(ans, @"^[A-D]$", RegexOptions.IgnoreCase) ? "SINGLE" : "SHORT_ANSWER";
                                if (!map[currentExam].ContainsKey(sec)) map[currentExam][sec] = new();

                                if (sec == "SINGLE")
                                {
                                    char ansChar = char.ToUpperInvariant(ans[0]);
                                    map[currentExam][sec][qNum] = (ansChar - 'A' + 1).ToString();
                                }
                                else
                                {
                                    map[currentExam][sec][qNum] = ans;
                                }
                            }
                        }
                    }
                    // Dạng 2: Bảng trắc nghiệm Đúng / Sai (Row 0: Câu 1 | Câu 2 | Câu 3 | Câu 4)
                    else if (row0.Count >= 4 && Regex.IsMatch(row0[0], @"^(?:Câu|C.u)\s*1", RegexOptions.IgnoreCase))
                    {
                        if (!map.ContainsKey(currentExam)) map[currentExam] = new();
                        if (!map[currentExam].ContainsKey("TRUE_FALSE")) map[currentExam]["TRUE_FALSE"] = new();

                        for (int col = 0; col < row0.Count; col++)
                        {
                            int qNum = col + 1;
                            var tfParts = new List<string>();
                            for (int r = 1; r < rows.Count; r++)
                            {
                                var cells = rows[r].Descendants(W + "tc").Select(tc => string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim()).ToList();
                                if (col < cells.Count && !string.IsNullOrEmpty(cells[col]))
                                {
                                    tfParts.Add(cells[col]);
                                }
                            }
                            if (tfParts.Count > 0)
                            {
                                map[currentExam]["TRUE_FALSE"][qNum] = string.Join(", ", tfParts);
                            }
                        }
                    }
                    // Dạng 3: Bảng lưới đa cột dọc (Câu | Đáp án | Câu | Đáp án ...)
                    else if (row0.Count >= 2 && Regex.IsMatch(row0[0], @"^(?:Câu|C[\.\?\s]*u)$", RegexOptions.IgnoreCase)
                             && Regex.IsMatch(row0[1], @"^(?:Đáp\s*án|D[\.\?\s]*p\s*[\.\?\s]*n|Answer|Key)$", RegexOptions.IgnoreCase))
                    {
                        if (!map.ContainsKey(currentExam)) map[currentExam] = new();
                        if (!map[currentExam].ContainsKey("SINGLE")) map[currentExam]["SINGLE"] = new();

                        for (int r = 1; r < rows.Count; r++)
                        {
                            var cells = rows[r].Descendants(W + "tc").Select(tc => string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim()).ToList();
                            for (int c = 0; c < cells.Count - 1; c += 2)
                            {
                                if (int.TryParse(cells[c], out int qNum))
                                {
                                    string ans = cells[c + 1].Trim().ToUpperInvariant();
                                    if (Regex.IsMatch(ans, @"^[A-D]$"))
                                    {
                                        char ansChar = ans[0];
                                        map[currentExam]["SINGLE"][qNum] = (ansChar - 'A' + 1).ToString();
                                    }
                                    else if (!string.IsNullOrWhiteSpace(ans))
                                    {
                                        map[currentExam]["SINGLE"][qNum] = ans;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return map;
        }

        public static string ConvertTableToMarkdown(
            XElement tbl,
            Func<XElement, string> paragraphTextExtractor)
        {
            var rows = tbl.Descendants(W + "tr").ToList();
            if (rows.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            int maxCols = 0;
            var rowData = new List<List<string>>();

            foreach (var row in rows)
            {
                var cells = row.Elements(W + "tc").ToList();
                var cellTexts = new List<string>();
                foreach (var tc in cells)
                {
                    var cellPTexts = new List<string>();
                    foreach (var p in tc.Elements(W + "p"))
                    {
                        var pt = paragraphTextExtractor(p);
                        if (!string.IsNullOrWhiteSpace(pt)) cellPTexts.Add(pt);
                    }
                    var cellCombined = string.Join(" ", cellPTexts).Replace("|", "\\|").Trim();
                    cellTexts.Add(cellCombined);
                }
                if (cellTexts.Count > maxCols) maxCols = cellTexts.Count;
                rowData.Add(cellTexts);
            }

            if (maxCols == 0 || rowData.Count == 0) return string.Empty;

            var headerCells = rowData[0];
            while (headerCells.Count < maxCols) headerCells.Add("");
            sb.AppendLine("| " + string.Join(" | ", headerCells) + " |");
            sb.AppendLine("| " + string.Join(" | ", Enumerable.Repeat("---", maxCols)) + " |");

            for (int r = 1; r < rowData.Count; r++)
            {
                var rCells = rowData[r];
                while (rCells.Count < maxCols) rCells.Add("");
                sb.AppendLine("| " + string.Join(" | ", rCells) + " |");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
