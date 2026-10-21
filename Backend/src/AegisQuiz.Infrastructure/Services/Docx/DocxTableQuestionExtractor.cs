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
        /// Hỗ trợ cả bảng có tiêu đề "Câu"/"Chọn" lẫn bảng lưới số thuần túy (GDPT 2025/2026: 1 | 2 | 3...).
        /// </summary>
        /// <summary>
        /// Bóc tách bảng đáp án trong OpenXML sang map 3 chiều [examCode][section][qNum].
        /// Hỗ trợ cả bảng có tiêu đề "Câu"/"Chọn" lẫn bảng lưới số thuần túy (GDPT 2025/2026: 1 | 2 | 3...).
        /// </summary>
        public static Dictionary<string, Dictionary<string, Dictionary<int, string>>> ExtractAnswerTables(
            XDocument doc,
            string defaultExamPrefix = "DE",
            DynamicQuestionIngestionEngine? engineOverride = null)
        {
            return ExtractAnswerTables(doc, out _, defaultExamPrefix, engineOverride);
        }

        /// <summary>
        /// Bóc tách bảng đáp án và đồng thời đánh dấu các phần tử bảng đáp án để không bị trôi vào văn bản thuần.
        /// Tuân thủ nguyên lý COD (Configuration-Over-Data):
        /// - Nhận diện biên giới đề thi, phân đoạn (Phần I/II/III), tiêu đề bảng qua DynamicQuestionIngestionEngine & JSON Dictionary.
        /// - Loại bỏ hoàn toàn magic numbers (12, 16) bằng thuật toán theo dõi số lượng câu tích lũy động.
        /// </summary>
        public static Dictionary<string, Dictionary<string, Dictionary<int, string>>> ExtractAnswerTables(
            XDocument doc,
            out HashSet<XElement> answerTableElements,
            string defaultExamPrefix = "DE",
            DynamicQuestionIngestionEngine? engineOverride = null)
        {
            answerTableElements = new HashSet<XElement>();
            var map = new Dictionary<string, Dictionary<string, Dictionary<int, string>>>(StringComparer.OrdinalIgnoreCase);
            var body = doc.Descendants(W + "body").FirstOrDefault();
            if (body == null) return map;

            var cig = engineOverride ?? DynamicQuestionIngestionEngine.Instance;
            var specs = cig.StructureDictionary.AnswerTableSpecs ?? new AegisQuiz.Application.DTOs.AnswerTableDictionaryDto();
            var qHeaderRegex = new Regex(specs.QuestionHeaderPattern ?? @"^(?:Câu|C[\.\?\s]*u|CÂU|TT|Q|Question)$", RegexOptions.IgnoreCase);
            var aHeaderRegex = new Regex(specs.AnswerHeaderPattern ?? @"^(?:Chọn|Ch[\.\?\s]*n|Đáp\s*án|D[\.\?\s]*p\s*[\.\?\s]*n|Answer|Key|Đ[\/\.]?A|Phương\s*án)$", RegexOptions.IgnoreCase);
            var tfLabels = specs.TrueFalseColumnLabels?.Count > 0 ? specs.TrueFalseColumnLabels : new List<string> { "a", "b", "c", "d" };

            string prefix = string.IsNullOrWhiteSpace(defaultExamPrefix) ? "DE" : defaultExamPrefix.Trim().TrimEnd('_');
            string currentExam = "DEFAULT";
            string currentSection = "SINGLE";
            bool inAnswerKeyPhase = false;

            // Theo dõi số câu hỏi tích lũy theo từng đề để ánh xạ động số thứ tự toàn đề (thay thế hoàn toàn magic numbers 12, 16)
            var examCumulativeOffsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            void EnsureMap(string exam, string sec)
            {
                if (!map.ContainsKey(exam)) map[exam] = new(StringComparer.OrdinalIgnoreCase);
                if (!map[exam].ContainsKey(sec)) map[exam][sec] = new();
                if (!map.ContainsKey("DEFAULT")) map["DEFAULT"] = new(StringComparer.OrdinalIgnoreCase);
                if (!map["DEFAULT"].ContainsKey(sec)) map["DEFAULT"][sec] = new();
            }

            void StoreAnswer(string exam, string sec, int qNum, string ans)
            {
                EnsureMap(exam, sec);
                map[exam][sec][qNum] = ans;
                if (exam.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase) || !map["DEFAULT"][sec].ContainsKey(qNum))
                {
                    map["DEFAULT"][sec][qNum] = ans;
                }
            }

            foreach (var el in body.Elements())
            {
                if (el.Name == W + "p")
                {
                    var pText = string.Concat(el.Descendants(W + "t").Select(t => t.Value)).Trim();
                    if (string.IsNullOrWhiteSpace(pText)) continue;

                    // 1. COD: Nhận diện biên giới đề thi từ DocumentStructureDictionary / Parsing Rules
                    if (cig.TryMatchExamHeading(pText, out string examCode))
                    {
                        if (int.TryParse(examCode, out int examNum))
                        {
                            if (examNum >= 1900 && examNum <= 2099)
                            {
                                continue; // Bỏ qua năm học (2024..2026), không ghi đè currentExam!
                            }
                            currentExam = $"{prefix}_{examNum:D2}";
                        }
                        else
                        {
                            currentExam = $"{prefix}_{examCode}";
                        }
                    }

                    // 2. COD: Nhận diện tiêu đề bảng đáp án ("BẢNG ĐÁP ÁN", "ANSWER KEY"...)
                    if (cig.TryMatchAnswerTablePhase(pText))
                    {
                        inAnswerKeyPhase = true;
                    }

                    // 3. COD: Nhận diện tiêu đề phân đoạn ("PHẦN I", "PHẦN II", "PHẦN III", "PART A/B/C"...)
                    if (cig.TryMatchSectionHeader(pText, out string detectedSec))
                    {
                        currentSection = detectedSec;
                    }
                }
                else if (el.Name == W + "tbl")
                {
                    var rows = el.Descendants(W + "tr").ToList();
                    if (rows.Count < 2) continue;

                    // [Table Exam Heading Synchronization]
                    // Nếu bảng này là bảng tiêu đề chứa thông tin Đề thi (ví dụ ĐỀ 1, EXAM 02, SỞ GD&ĐT...):
                    foreach (var tp in el.Descendants(W + "p"))
                    {
                        var tpText = string.Concat(tp.Descendants(W + "t").Select(t => t.Value)).Trim();
                        if (!string.IsNullOrWhiteSpace(tpText) && cig.TryMatchExamHeading(tpText, out string tExamCode))
                        {
                            if (int.TryParse(tExamCode, out int tExamNum))
                            {
                                if (tExamNum < 1900 || tExamNum > 2099)
                                {
                                    currentExam = $"{prefix}_{tExamNum:D2}";
                                }
                            }
                            else
                            {
                                currentExam = $"{prefix}_{tExamCode}";
                            }
                        }
                    }

                    int offset = examCumulativeOffsets.GetValueOrDefault(currentExam, 0);

                    // ── [CHIP v3.1 / SCIE v4.0 - Trụ cột 1: Nhận diện Merged Header & Trích xuất Section] ──
                    int startRow = 0;
                    var r0Cells = rows[0].Descendants(W + "tc").ToList();
                    var r0Text = string.Concat(r0Cells.SelectMany(tc => tc.Descendants(W + "t").Select(t => t.Value))).Trim();

                    if (r0Cells.Count <= 2 && rows.Count >= 3)
                    {
                        var r1Cells = rows[1].Descendants(W + "tc").ToList();
                        if (r1Cells.Count >= 3 && (r0Text.Contains("ĐÁP ÁN", StringComparison.OrdinalIgnoreCase) ||
                                                   r0Text.Contains("PHẦN", StringComparison.OrdinalIgnoreCase) ||
                                                   r0Text.Contains("HƯỚNG DẪN", StringComparison.OrdinalIgnoreCase) ||
                                                   r0Text.Contains("ANSWER", StringComparison.OrdinalIgnoreCase) ||
                                                   r0Text.Contains("KEY", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (cig.TryMatchSectionHeader(r0Text, out string secFromHeader))
                            {
                                currentSection = secFromHeader;
                            }
                            startRow = 1;
                        }
                    }

                    var row0 = ExtractRowCellsNormalized(rows[startRow], W);
                    var row1 = ExtractRowCellsNormalized(rows[startRow + 1], W);
                    if (row0.Count == 0 || row1.Count == 0) continue;

                    // ── DẠNG 1: Bảng lưới số thuần túy đa tầng (Row 2k: 1 | 2 | 3... không có cột "Câu" / "Chọn") ──
                    bool isNumberedGrid = row0.Count >= 2 && row0.All(c => int.TryParse(c, out _));
                    if (!isNumberedGrid && row0.Count >= 2 && int.TryParse(row0[0], out int fst) && fst == 1)
                    {
                        if (row0.Count > 1 && int.TryParse(row0[1], out int snd) && snd == 2)
                        {
                            isNumberedGrid = true;
                        }
                    }

                    if (isNumberedGrid)
                    {
                        answerTableElements.Add(el);
                        int tableQCount = 0;

                        // Duyệt qua tất cả các cặp hàng (startRow, startRow + 1), (startRow + 2, startRow + 3)...
                        for (int rIdx = startRow; rIdx < rows.Count - 1; rIdx += 2)
                        {
                            var pairRow0 = rows[rIdx].Descendants(W + "tc").Select(tc => string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim()).ToList();
                            var pairRow1 = rows[rIdx + 1].Descendants(W + "tc").Select(tc => string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim()).ToList();
                            if (pairRow0.Count < 2 || pairRow1.Count == 0) continue;

                            // Bỏ qua nếu dòng này không phải số câu
                            if (!int.TryParse(pairRow0[0], out _)) continue;

                            // Suy luận phân đoạn dựa trên dữ liệu các ô hàng đáp án
                            string sec = currentSection;
                            if (pairRow1.All(v => Regex.IsMatch(v, @"^[SĐTFsdtf]{2,5}$")))
                            {
                                sec = "TRUE_FALSE";
                            }
                            else if (pairRow1.All(v => Regex.IsMatch(v, @"^[A-Da-d]$")))
                            {
                                sec = "SINGLE";
                            }
                            else if (currentSection == "SHORT_ANSWER" || pairRow1.Any(v => double.TryParse(v.Replace(',', '.'), out _)))
                            {
                                sec = "SHORT_ANSWER";
                            }

                            for (int c = 0; c < Math.Min(pairRow0.Count, pairRow1.Count); c++)
                            {
                                if (int.TryParse(pairRow0[c], out int qNum))
                                {
                                    tableQCount++;
                                    string rawAns = pairRow1[c].Trim();
                                    int globalQ = (qNum <= offset) ? (offset + qNum) : qNum;
                                    int localQ = (qNum > offset) ? (qNum - offset) : qNum;

                                    if (sec == "SINGLE")
                                    {
                                        string ansIndex = Regex.IsMatch(rawAns, @"^[A-Da-d]$")
                                            ? (char.ToUpperInvariant(rawAns[0]) - 'A' + 1).ToString()
                                            : rawAns;

                                        StoreAnswer(currentExam, "SINGLE", localQ, ansIndex);
                                        if (globalQ != localQ) StoreAnswer(currentExam, "SINGLE", globalQ, ansIndex);
                                    }
                                    else if (sec == "TRUE_FALSE")
                                    {
                                        var tfList = new List<string>();
                                        for (int i = 0; i < rawAns.Length && i < tfLabels.Count; i++)
                                        {
                                            string sub = tfLabels[i];
                                            char ch = char.ToUpperInvariant(rawAns[i]);
                                            string isTrue = (ch == 'Đ' || ch == 'D' || ch == 'T') ? "Đúng" : "Sai";
                                            tfList.Add($"{sub}) {isTrue}");
                                        }
                                        string formattedTf = tfList.Count > 0 ? string.Join(", ", tfList) : rawAns;

                                        StoreAnswer(currentExam, "TRUE_FALSE", localQ, formattedTf);
                                        StoreAnswer(currentExam, "MULTI_TRUE_FALSE", localQ, formattedTf);
                                        if (globalQ != localQ)
                                        {
                                            StoreAnswer(currentExam, "TRUE_FALSE", globalQ, formattedTf);
                                            StoreAnswer(currentExam, "MULTI_TRUE_FALSE", globalQ, formattedTf);
                                        }
                                    }
                                    else if (sec == "SHORT_ANSWER")
                                    {
                                        StoreAnswer(currentExam, "SHORT_ANSWER", localQ, rawAns);
                                        if (globalQ != localQ) StoreAnswer(currentExam, "SHORT_ANSWER", globalQ, rawAns);
                                    }
                                }
                            }
                        }

                        // Cập nhật số lượng câu tích lũy động cho đề thi hiện tại
                        examCumulativeOffsets[currentExam] = offset + tableQCount;
                        continue;
                    }

                    // ── DẠNG 2A: Bảng trắc nghiệm có cột tiêu đề "Câu" / "Chọn" đa tầng (COD AnswerTableSpecs) ──
                    if (row0.Count >= 2 && qHeaderRegex.IsMatch(row0[0]) && aHeaderRegex.IsMatch(row1[0]))
                    {
                        answerTableElements.Add(el);
                        int tableQCount = 0;

                        for (int rIdx = startRow; rIdx < rows.Count - 1; rIdx += 2)
                        {
                            var pairRow0 = rows[rIdx].Descendants(W + "tc").Select(tc => string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim()).ToList();
                            var pairRow1 = rows[rIdx + 1].Descendants(W + "tc").Select(tc => string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim()).ToList();
                            if (pairRow0.Count < 2 || pairRow1.Count < 2) continue;
                            if (!qHeaderRegex.IsMatch(pairRow0[0]) && !int.TryParse(pairRow0[1], out _)) continue;

                            for (int c = 1; c < Math.Min(pairRow0.Count, pairRow1.Count); c++)
                            {
                                if (int.TryParse(pairRow0[c], out int qNum))
                                {
                                    tableQCount++;
                                    string ans = pairRow1[c].Trim();
                                    string sec = currentSection;
                                    string finalAns = ans;

                                    if (Regex.IsMatch(ans, @"^[A-Da-d]$"))
                                    {
                                        sec = "SINGLE";
                                        finalAns = (char.ToUpperInvariant(ans[0]) - 'A' + 1).ToString();
                                    }
                                    else if (Regex.IsMatch(ans, @"(?:[a-d][\:\)]\s*[ĐSdtsTF]|[ĐS]{2,4})", RegexOptions.IgnoreCase)
                                             || sec == "TRUE_FALSE" || sec == "MULTI_TRUE_FALSE")
                                    {
                                        sec = "MULTI_TRUE_FALSE";
                                        var subM = Regex.Matches(ans, @"([a-d])[\:\)]\s*(Đ|S|Đúng|Sai|True|False|D|T)", RegexOptions.IgnoreCase);
                                        if (subM.Count > 0)
                                        {
                                            var tfList = new List<string>();
                                            foreach (Match sm in subM)
                                            {
                                                string sk = sm.Groups[1].Value.ToLowerInvariant();
                                                string sv = sm.Groups[2].Value.StartsWith("đ", StringComparison.OrdinalIgnoreCase) || sm.Groups[2].Value.StartsWith("d", StringComparison.OrdinalIgnoreCase) || sm.Groups[2].Value.StartsWith("t", StringComparison.OrdinalIgnoreCase) ? "Đúng" : "Sai";
                                                tfList.Add($"{sk}) {sv}");
                                            }
                                            finalAns = string.Join(", ", tfList);
                                        }
                                    }
                                    else
                                    {
                                        sec = "SHORT_ANSWER";
                                    }

                                    int localQ = qNum;
                                    StoreAnswer(currentExam, sec, localQ, finalAns);
                                    if (sec == "MULTI_TRUE_FALSE")
                                    {
                                        StoreAnswer(currentExam, "TRUE_FALSE", localQ, finalAns);
                                    }
                                }
                            }
                        }

                        examCumulativeOffsets[currentExam] = offset + tableQCount;
                        continue;
                    }

                    // ── DẠNG 2B: Bảng kết quả mini của 1 câu hỏi Đúng/Sai (Ảnh 2: Row 0: Câu 1 | a) | b) | c) | d) - Row 1: ý | Đ | S | Đ | Đ) ──
                    var matchSingleQ = Regex.Match(row0[0], @"^(?:Câu|Question|Q)\s*(\d+)", RegexOptions.IgnoreCase);
                    bool isSingleQTfTable = row0.Count >= 3 && matchSingleQ.Success
                        && row0.Skip(1).Take(4).All(c => Regex.IsMatch(c.Trim(), @"^[a-dA-D][\)\.]?$"));

                    if (isSingleQTfTable)
                    {
                        answerTableElements.Add(el);
                        int qNum = int.Parse(matchSingleQ.Groups[1].Value);
                        var tfList = new List<string>();

                        for (int c = 1; c < Math.Min(row0.Count, row1.Count); c++)
                        {
                            var matchSub = Regex.Match(row0[c], @"[a-dA-D]");
                            string sub = matchSub.Success ? matchSub.Value.ToLowerInvariant() : tfLabels.ElementAtOrDefault(c - 1) ?? "a";
                            string cellVal = row1[c].Trim();
                            bool isTrue = Regex.IsMatch(cellVal, @"^(?:Đ|D|T|Đúng|True)$", RegexOptions.IgnoreCase);
                            tfList.Add($"{sub}) {(isTrue ? "Đúng" : "Sai")}");
                        }

                        if (tfList.Count > 0)
                        {
                            string formattedTf = string.Join(", ", tfList);
                            int globalQ = (qNum <= offset) ? (offset + qNum) : qNum;
                            int localQ = (qNum > offset) ? (qNum - offset) : qNum;

                            StoreAnswer(currentExam, "TRUE_FALSE", localQ, formattedTf);
                            StoreAnswer(currentExam, "MULTI_TRUE_FALSE", localQ, formattedTf);
                            if (globalQ != localQ)
                            {
                                StoreAnswer(currentExam, "TRUE_FALSE", globalQ, formattedTf);
                                StoreAnswer(currentExam, "MULTI_TRUE_FALSE", globalQ, formattedTf);
                            }
                        }
                        continue;
                    }

                    // ── Dạng 3: Bảng trắc nghiệm Đúng / Sai ma trận cột đa câu (Ảnh 1: Row 0: Câu 1 | Câu 2 | Câu 3 | Câu 4) ──
                    string row0Prefix = Regex.Replace(row0[0], @"\s*\d+.*$", "").Trim();
                    bool isTfMatrixMulti = row0.Count >= 2 && !isSingleQTfTable &&
                        (row0.Take(2).All(c => Regex.IsMatch(c, @"^(?:Câu|Question|Q)\s*\d+", RegexOptions.IgnoreCase)) ||
                         (qHeaderRegex.IsMatch(row0Prefix) && row0.Skip(1).All(c => Regex.IsMatch(c, @"\d+"))));

                    if (isTfMatrixMulti)
                    {
                        answerTableElements.Add(el);
                        int tableQCount = 0;
                        for (int col = 0; col < row0.Count; col++)
                        {
                            var matchColNum = Regex.Match(row0[col], @"\d+");
                            int qNum = matchColNum.Success ? int.Parse(matchColNum.Value) : (col + 1);
                            tableQCount++;

                            var tfParts = new List<string>();
                            for (int r = 1; r < rows.Count; r++)
                            {
                                var cells = rows[r].Descendants(W + "tc").Select(tc => string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim()).ToList();
                                if (col < cells.Count && !string.IsNullOrEmpty(cells[col]))
                                {
                                    string rawCell = cells[col].Trim();
                                    // Chuẩn hóa dạng "a) Đ" -> "a) Đúng", "b) S" -> "b) Sai"
                                    var matchSubAns = Regex.Match(rawCell, @"^([a-dA-D])[\)\.]?\s*(.+)$");
                                    if (matchSubAns.Success)
                                    {
                                        string sub = matchSubAns.Groups[1].Value.ToLowerInvariant();
                                        string val = matchSubAns.Groups[2].Value.Trim();
                                        bool isTrue = Regex.IsMatch(val, @"^(?:Đ|D|T|Đúng|True)$", RegexOptions.IgnoreCase);
                                        tfParts.Add($"{sub}) {(isTrue ? "Đúng" : "Sai")}");
                                    }
                                    else
                                    {
                                        string sub = tfLabels.ElementAtOrDefault(r - 1) ?? "a";
                                        bool isTrue = Regex.IsMatch(rawCell, @"^(?:Đ|D|T|Đúng|True)$", RegexOptions.IgnoreCase);
                                        tfParts.Add($"{sub}) {(isTrue ? "Đúng" : "Sai")}");
                                    }
                                }
                            }
                            if (tfParts.Count > 0)
                            {
                                string formattedTf = string.Join(", ", tfParts);
                                int globalQ = (qNum <= offset) ? (offset + qNum) : qNum;
                                int localQ = (qNum > offset) ? (qNum - offset) : qNum;

                                StoreAnswer(currentExam, "TRUE_FALSE", localQ, formattedTf);
                                StoreAnswer(currentExam, "MULTI_TRUE_FALSE", localQ, formattedTf);
                                if (globalQ != localQ)
                                {
                                    StoreAnswer(currentExam, "TRUE_FALSE", globalQ, formattedTf);
                                    StoreAnswer(currentExam, "MULTI_TRUE_FALSE", globalQ, formattedTf);
                                }
                            }
                        }

                        examCumulativeOffsets[currentExam] = offset + tableQCount;
                        continue;
                    }

                    // ── DẠNG 3B: Bảng Đúng / Sai Hàng-Câu Cột-Ý (GDPT 2025: Row 0: Câu | a | b | c | d - Row 1..N: Câu 1 | Đ | S | Đ | S) ──
                    bool isRowBasedTf = row0.Count >= 5 && qHeaderRegex.IsMatch(row0[0])
                        && row0.Skip(1).Take(4).All(c => Regex.IsMatch(c.Trim(), @"^(?:ý\s*)?[a-dA-D][\)\.]?$", RegexOptions.IgnoreCase));

                    if (isRowBasedTf)
                    {
                        answerTableElements.Add(el);
                        int tableQCount = 0;
                        for (int r = startRow + 1; r < rows.Count; r++)
                        {
                            var rCells = ExtractRowCellsNormalized(rows[r], W);
                            if (rCells.Count < 5) continue;

                            var mQ = Regex.Match(rCells[0], @"\d+");
                            if (!mQ.Success) continue;
                            int qNum = int.Parse(mQ.Value);
                            tableQCount++;

                            var tfParts = new List<string>();
                            for (int c = 1; c <= 4 && c < rCells.Count; c++)
                            {
                                string sub = tfLabels.ElementAtOrDefault(c - 1) ?? ((char)('a' + c - 1)).ToString();
                                string val = rCells[c].Trim();
                                bool isTrue = Regex.IsMatch(val, @"^(?:Đ|D|T|Đúng|True)$", RegexOptions.IgnoreCase);
                                tfParts.Add($"{sub}) {(isTrue ? "Đúng" : "Sai")}");
                            }

                            if (tfParts.Count > 0)
                            {
                                string formattedTf = string.Join(", ", tfParts);
                                int globalQ = (qNum <= offset) ? (offset + qNum) : qNum;
                                int localQ = (qNum > offset) ? (qNum - offset) : qNum;

                                StoreAnswer(currentExam, "TRUE_FALSE", localQ, formattedTf);
                                StoreAnswer(currentExam, "MULTI_TRUE_FALSE", localQ, formattedTf);
                                if (globalQ != localQ)
                                {
                                    StoreAnswer(currentExam, "TRUE_FALSE", globalQ, formattedTf);
                                    StoreAnswer(currentExam, "MULTI_TRUE_FALSE", globalQ, formattedTf);
                                }
                            }
                        }

                        examCumulativeOffsets[currentExam] = offset + tableQCount;
                        continue;
                    }

                    // ── Dạng 4: Bảng lưới đa cột dọc (Câu | Đáp án | Câu | Đáp án ...) theo COD AnswerTableSpecs ──
                    if (row0.Count >= 2 && qHeaderRegex.IsMatch(row0[0]) && aHeaderRegex.IsMatch(row0[1]))
                    {
                        answerTableElements.Add(el);
                        int tableQCount = 0;

                        // Thu thập tất cả các ô đáp án để suy luận Section chính xác (Zero Hard-code)
                        var sampleAnswers = new List<string>();
                        for (int r = 1; r < rows.Count; r++)
                        {
                            var cells = ExtractRowCellsNormalized(rows[r], W);
                            for (int c = 0; c < cells.Count - 1; c += 2)
                            {
                                if (int.TryParse(cells[c], out _) && !string.IsNullOrWhiteSpace(cells[c + 1]))
                                {
                                    sampleAnswers.Add(cells[c + 1].Trim());
                                }
                            }
                        }

                        // Suy luận target section:
                        // 1. Nếu currentSection là SHORT_ANSWER hoặc có mẫu đáp số (chứa số, dấu phẩy, đơn vị đo mA, mW, %, kJ...) -> SHORT_ANSWER
                        // 2. Nếu tất cả là [A-G] -> SINGLE
                        // 3. Nếu là Đúng/Sai -> MULTI_TRUE_FALSE
                        string targetSec = currentSection;
                        if (sampleAnswers.Count > 0)
                        {
                            if (sampleAnswers.All(a => Regex.IsMatch(a, @"^[A-Ga-g]$")))
                            {
                                targetSec = "SINGLE";
                            }
                            else if (currentSection == "SHORT_ANSWER" || sampleAnswers.Any(a => Regex.IsMatch(a, @"[\d,\.\%\+\-]")))
                            {
                                targetSec = "SHORT_ANSWER";
                            }
                        }

                        for (int r = 1; r < rows.Count; r++)
                        {
                            var cells = ExtractRowCellsNormalized(rows[r], W);
                            for (int c = 0; c < cells.Count - 1; c += 2)
                            {
                                if (int.TryParse(cells[c], out int qNum))
                                {
                                    tableQCount++;
                                    string rawAns = cells[c + 1].Trim();
                                    string finalAns = rawAns;

                                    if (targetSec == "SINGLE")
                                    {
                                        var upperA = rawAns.ToUpperInvariant();
                                        finalAns = Regex.IsMatch(upperA, @"^[A-G]$")
                                            ? (upperA[0] - 'A' + 1).ToString()
                                            : rawAns;
                                    }

                                    if (!string.IsNullOrWhiteSpace(finalAns))
                                    {
                                        int globalQ = (qNum <= offset) ? (offset + qNum) : qNum;
                                        int localQ = (qNum > offset) ? (qNum - offset) : qNum;

                                        StoreAnswer(currentExam, targetSec, localQ, finalAns);
                                        if (globalQ != localQ) StoreAnswer(currentExam, targetSec, globalQ, finalAns);
                                    }
                                }
                            }
                        }

                        examCumulativeOffsets[currentExam] = offset + tableQCount;
                        continue;
                    }

                    // ── DẠNG 6: Bảng Ma Trận Đa Mã Đề (Multi-Exam Code Cross-Matrix: Row 0: Câu | 101 | 102 | 103...) ──
                    bool isMultiExamMatrix = row0.Count >= 3 && qHeaderRegex.IsMatch(row0[0])
                        && row0.Skip(1).All(c => Regex.IsMatch(c.Trim(), @"^(?:(?:mã\s*đề|đề|exam|code)\s*)?\d{2,4}$", RegexOptions.IgnoreCase));

                    if (isMultiExamMatrix)
                    {
                        answerTableElements.Add(el);
                        var colExamMap = new Dictionary<int, string>();
                        for (int c = 1; c < row0.Count; c++)
                        {
                            var matchCode = Regex.Match(row0[c].Trim(), @"\d{2,4}");
                            if (matchCode.Success)
                            {
                                colExamMap[c] = $"{prefix}_{matchCode.Value}";
                            }
                        }

                        if (colExamMap.Count > 0)
                        {
                            int tableQCount = 0;
                            for (int r = startRow + 1; r < rows.Count; r++)
                            {
                                var rCells = ExtractRowCellsNormalized(rows[r], W);
                                if (rCells.Count < 2) continue;

                                var mQ = Regex.Match(rCells[0], @"\d+");
                                if (!mQ.Success) continue;
                                int qNum = int.Parse(mQ.Value);
                                tableQCount++;

                                foreach (var (colIdx, examTarget) in colExamMap)
                                {
                                    if (colIdx < rCells.Count)
                                    {
                                        string rawVal = rCells[colIdx].Trim().ToUpperInvariant();
                                        string finalAns = Regex.IsMatch(rawVal, @"^[A-D]$")
                                            ? (rawVal[0] - 'A' + 1).ToString()
                                            : rawVal;

                                        StoreAnswer(examTarget, "SINGLE", qNum, finalAns);
                                    }
                                }
                            }
                            examCumulativeOffsets[currentExam] = offset + tableQCount;
                            continue;
                        }
                    }

                    // ── Dạng 5: Ô tổ hợp (Composite Cell Pattern dạng "1A 2B", "1.A 2.B" hoặc "Câu 1: A  Câu 2: B") ──
                    var compositeMatches = new List<(int QNum, string Ans)>();
                    var compositeRegex = new Regex(@"(?:^|\b|\s)(?:(?:Câu|C[\.\?\s]*u|Q)\s*)?([1-9]\d?)\s*[\.\:\-\)]?\s*([A-D]|Đúng|Sai|[ĐS])\b", RegexOptions.IgnoreCase);
                    foreach (var r in rows)
                    {
                        foreach (var tc in r.Descendants(W + "tc"))
                        {
                            var cellText = string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim();
                            if (string.IsNullOrWhiteSpace(cellText)) continue;
                            var matches = compositeRegex.Matches(cellText);
                            foreach (Match m in matches)
                            {
                                if (int.TryParse(m.Groups[1].Value, out int qNum))
                                {
                                    compositeMatches.Add((qNum, m.Groups[2].Value));
                                }
                            }
                        }
                    }

                    if (compositeMatches.Count >= 3)
                    {
                        answerTableElements.Add(el);
                        int tableQCount = 0;
                        foreach (var (qNum, rawAns) in compositeMatches)
                        {
                            tableQCount++;
                            string finalAns = rawAns.ToUpperInvariant();
                            if (finalAns == "Đ" || finalAns == "ĐÚNG") finalAns = "Đúng";
                            else if (finalAns == "S" || finalAns == "SAI") finalAns = "Sai";
                            else if (Regex.IsMatch(finalAns, @"^[A-D]$")) finalAns = (finalAns[0] - 'A' + 1).ToString();

                            string sec = (finalAns == "Đúng" || finalAns == "Sai") ? "TRUE_FALSE" : (currentSection == "TRUE_FALSE" || currentSection == "MULTI_TRUE_FALSE" ? "MULTI_TRUE_FALSE" : "SINGLE");
                            int localQ = qNum;

                            StoreAnswer(currentExam, sec, localQ, finalAns);
                            if (sec == "TRUE_FALSE") StoreAnswer(currentExam, "MULTI_TRUE_FALSE", localQ, finalAns);
                        }
                        examCumulativeOffsets[currentExam] = offset + tableQCount;
                        continue;
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

        /// <summary>
        /// Chuẩn hóa các ô trong hàng bảng OpenXML, giải nén ô gộp w:gridSpan thành các cột logic ảo.
        /// Triệt tiêu hoàn toàn hiện tượng lệch chỉ số cột (Column Index Drift).
        /// </summary>
        public static List<string> ExtractRowCellsNormalized(XElement row, XNamespace W)
        {
            var result = new List<string>();
            foreach (var tc in row.Elements(W + "tc"))
            {
                var text = string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim();
                int span = 1;
                var gridSpanEl = tc.Element(W + "tcPr")?.Element(W + "gridSpan");
                if (gridSpanEl != null)
                {
                    var valAttr = gridSpanEl.Attribute(W + "val");
                    if (valAttr != null && int.TryParse(valAttr.Value, out int sVal) && sVal > 1)
                    {
                        span = sVal;
                    }
                }
                for (int i = 0; i < span; i++)
                {
                    result.Add(text);
                }
            }
            return result;
        }
    }
}
