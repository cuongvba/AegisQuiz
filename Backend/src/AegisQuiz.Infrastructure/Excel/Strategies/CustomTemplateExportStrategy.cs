using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Excel.Strategies
{
    /// <summary>
    /// [Custom Bring-Your-Own-Template (BYOT) Export Strategy]
    /// Chiến lược xuất bản bảng tính Excel thông minh theo tệp mẫu tùy biến do người dùng/khách hàng cung cấp:
    /// - Nhận file mẫu .xlsx bất kỳ từ người dùng (qua options.CustomTemplateBytes).
    /// - Tự động quét và nhận diện dòng tiêu đề (Header Row Detection) & lập bản đồ cột (Column Mapping)
    ///   theo từ khóa linh hoạt: STT, Nội dung câu hỏi, Phương án A-D, Đáp án đúng, Lời giải, Chủ đề...
    /// - Bảo toàn nguyên vẹn định dạng, font chữ, màu sắc và logo có sẵn trong template mẫu.
    /// - Điền dữ liệu an toàn 100% qua ExcelCellSanitizer (chống tràn 32k ký tự).
    /// - Nếu chưa tải file mẫu, tự động xuất template khung chuẩn đa năng để người dùng làm mẫu nạp.
    /// </summary>
    public class CustomTemplateExportStrategy : IExcelExportStrategy
    {
        public string TemplateCode => "CUSTOM_BYOT";
        public string DisplayName => "Tùy biến theo Mẫu tải lên (Custom BYOT Template)";
        public string Description => "Tự động ánh xạ và điền danh sách câu hỏi vào đúng mẫu bảng tính Excel (.xlsx) do tổ chức hoặc cá nhân tự cung cấp.";
        public bool SupportsMultiSheet => false;
        public bool IsNativeIsomorphic => false;

        public byte[] Export(IReadOnlyList<DocxImportPreviewDto> questions, ExcelExportOptionsDto options)
        {
            if (options.CustomTemplateBytes != null && options.CustomTemplateBytes.Length > 0)
            {
                return ExportWithUploadedTemplate(questions, options);
            }

            return ExportDefaultStarterTemplate(questions, options);
        }

        private static byte[] ExportWithUploadedTemplate(IReadOnlyList<DocxImportPreviewDto> questions, ExcelExportOptionsDto options)
        {
            using var inputStream = new MemoryStream(options.CustomTemplateBytes!);
            using var workbook = new XSSFWorkbook(inputStream);

            var sheet = workbook.GetSheetAt(0);
            if (sheet == null)
            {
                sheet = workbook.CreateSheet("Quiz_Export");
            }

            // 1. Quét tìm dòng header và bản đồ cột trong 15 dòng đầu tiên
            var (headerRowIdx, colMap) = DetectHeaderAndMapColumns(sheet);

            int startRow = headerRowIdx >= 0 ? headerRowIdx + 1 : sheet.LastRowNum + 1;

            // 2. Lấy mẫu style từ dòng dữ liệu đầu tiên (nếu có) để kế thừa định dạng đẹp mắt
            var sampleRow = sheet.GetRow(startRow);
            var styles = new Dictionary<int, ICellStyle>();
            if (sampleRow != null)
            {
                for (int c = 0; c < sampleRow.LastCellNum; c++)
                {
                    var cell = sampleRow.GetCell(c);
                    if (cell?.CellStyle != null)
                    {
                        styles[c] = cell.CellStyle;
                    }
                }
            }

            // 3. Điền từng câu hỏi vào sheet
            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                int currentR = startRow + i;
                var row = sheet.GetRow(currentR) ?? sheet.CreateRow(currentR);
                if (row.HeightInPoints < 24) row.HeightInPoints = 26;

                // Gán STT
                if (colMap.TryGetValue("STT", out int cStt))
                {
                    SetCell(row, cStt, !string.IsNullOrWhiteSpace(q.SourceQuestionNo) ? q.SourceQuestionNo : (i + 1).ToString(), styles);
                }

                // Gán Nội dung câu hỏi
                if (colMap.TryGetValue("CONTENT", out int cContent))
                {
                    SetCell(row, cContent, ExcelCellSanitizer.Sanitize(q.Content), styles);
                }

                // Gán Loại câu hỏi
                if (colMap.TryGetValue("TYPE", out int cType))
                {
                    SetCell(row, cType, q.QuestionType ?? "SINGLE", styles);
                }

                // Gán Phương án A - D
                if (colMap.TryGetValue("OPTA", out int cOptA)) SetCell(row, cOptA, ExcelCellSanitizer.Sanitize(GetOption(q, 0)), styles);
                if (colMap.TryGetValue("OPTB", out int cOptB)) SetCell(row, cOptB, ExcelCellSanitizer.Sanitize(GetOption(q, 1)), styles);
                if (colMap.TryGetValue("OPTC", out int cOptC)) SetCell(row, cOptC, ExcelCellSanitizer.Sanitize(GetOption(q, 2)), styles);
                if (colMap.TryGetValue("OPTD", out int cOptD)) SetCell(row, cOptD, ExcelCellSanitizer.Sanitize(GetOption(q, 3)), styles);

                // Gán Đáp án đúng
                if (colMap.TryGetValue("ANSWER", out int cAns))
                {
                    SetCell(row, cAns, FormatAnswer(q), styles);
                }

                // Gán Hướng dẫn giải
                if (colMap.TryGetValue("EXPLANATION", out int cExpl))
                {
                    SetCell(row, cExpl, options.IncludeExplanation ? ExcelCellSanitizer.Sanitize(q.AiExplanation) : string.Empty, styles);
                }

                // Gán Chủ đề
                if (colMap.TryGetValue("TOPIC", out int cTopic))
                {
                    SetCell(row, cTopic, !string.IsNullOrWhiteSpace(q.TopicCode) ? q.TopicCode : (options.DefaultTopicCode ?? "GENERAL"), styles);
                }

                // Gán Độ khó
                if (colMap.TryGetValue("DIFFICULTY", out int cDiff))
                {
                    SetCell(row, cDiff, q.Difficulty > 0 ? q.Difficulty.ToString() : "2", styles);
                }

                // Gán Bài đọc hiểu / Ngữ cảnh
                if (colMap.TryGetValue("CONTEXT", out int cContext))
                {
                    string fullPassage = !string.IsNullOrWhiteSpace(q.ContextTitle)
                        ? $"📖 【{q.ContextTitle.Trim()}】\r\n\r\n{q.ContextContent?.Trim()}"
                        : (q.ContextContent?.Trim() ?? string.Empty);
                    SetCell(row, cContext, ExcelCellSanitizer.Sanitize(fullPassage), styles);
                }

                // Gán / Nhúng Hình ảnh minh họa
                if (colMap.TryGetValue("IMAGE", out int cImg))
                {
                    var images = ExcelImageHelper.ExtractImages(q);
                    if (options.IncludeImages && images.Count > 0)
                    {
                        var drawing = (XSSFDrawing)(sheet.DrawingPatriarch ?? sheet.CreateDrawingPatriarch());
                        bool embedded = ExcelImageHelper.EmbedImageInCell(workbook, drawing, images[0].RawBytes, currentR, cImg);
                        if (embedded)
                        {
                            row.HeightInPoints = Math.Max(row.HeightInPoints, 85);
                            SetCell(row, cImg, string.Empty, styles);
                        }
                        else
                        {
                            SetCell(row, cImg, "[Hình ảnh]", styles);
                        }
                    }
                    else
                    {
                        SetCell(row, cImg, string.Empty, styles);
                    }
                }
            }

            using var outMs = new MemoryStream();
            workbook.Write(outMs);
            return outMs.ToArray();
        }

        private static byte[] ExportDefaultStarterTemplate(IReadOnlyList<DocxImportPreviewDto> questions, ExcelExportOptionsDto options)
        {
            using var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Custom_Quiz_Template");
            sheet.DisplayGridlines = true;

            var fHeader = workbook.CreateFont();
            fHeader.FontName = "Calibri";
            fHeader.FontHeightInPoints = 11;
            fHeader.IsBold = true;
            fHeader.Color = IndexedColors.White.Index;

            var sHeader = workbook.CreateCellStyle();
            sHeader.SetFont(fHeader);
            sHeader.FillForegroundColor = IndexedColors.RoyalBlue.Index;
            sHeader.FillPattern = FillPattern.SolidForeground;
            sHeader.Alignment = HorizontalAlignment.Center;
            sHeader.VerticalAlignment = VerticalAlignment.Center;

            var sWrap = workbook.CreateCellStyle();
            sWrap.WrapText = true;
            sWrap.VerticalAlignment = VerticalAlignment.Center;

            var sCenter = workbook.CreateCellStyle();
            sCenter.Alignment = HorizontalAlignment.Center;
            sCenter.VerticalAlignment = VerticalAlignment.Center;

            string[] headers =
            {
                "STT", "Nội dung câu hỏi", "Loại câu hỏi",
                "Phương án A", "Phương án B", "Phương án C", "Phương án D",
                "Đáp án đúng", "Hướng dẫn giải chi tiết", "Chủ đề", "Mức độ"
            };

            var r0 = sheet.CreateRow(0);
            r0.HeightInPoints = 28;
            for (int i = 0; i < headers.Length; i++)
            {
                var c = r0.CreateCell(i);
                c.SetCellValue(headers[i]);
                c.CellStyle = sHeader;
            }

            int r = 1;
            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                var row = sheet.CreateRow(r++);
                row.HeightInPoints = 26;

                row.CreateCell(0).SetCellValue(!string.IsNullOrWhiteSpace(q.SourceQuestionNo) ? q.SourceQuestionNo : (i + 1).ToString());
                row.CreateCell(1).SetCellValue(ExcelCellSanitizer.Sanitize(q.Content));
                row.CreateCell(2).SetCellValue(q.QuestionType ?? "SINGLE");
                row.CreateCell(3).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 0)));
                row.CreateCell(4).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 1)));
                row.CreateCell(5).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 2)));
                row.CreateCell(6).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 3)));
                row.CreateCell(7).SetCellValue(FormatAnswer(q));
                row.CreateCell(8).SetCellValue(options.IncludeExplanation ? ExcelCellSanitizer.Sanitize(q.AiExplanation) : "");
                row.CreateCell(9).SetCellValue(!string.IsNullOrWhiteSpace(q.TopicCode) ? q.TopicCode : (options.DefaultTopicCode ?? "GENERAL"));
                row.CreateCell(10).SetCellValue(q.Difficulty > 0 ? q.Difficulty.ToString() : "2");

                row.GetCell(0).CellStyle = sCenter;
                row.GetCell(1).CellStyle = sWrap;
                row.GetCell(2).CellStyle = sCenter;
                for (int c = 3; c <= 6; c++) row.GetCell(c).CellStyle = sWrap;
                row.GetCell(7).CellStyle = sCenter;
                row.GetCell(8).CellStyle = sWrap;
                row.GetCell(9).CellStyle = sCenter;
                row.GetCell(10).CellStyle = sCenter;
            }

            sheet.SetColumnWidth(0, 8 * 256);
            sheet.SetColumnWidth(1, 45 * 256);
            sheet.SetColumnWidth(2, 14 * 256);
            sheet.SetColumnWidth(3, 22 * 256);
            sheet.SetColumnWidth(4, 22 * 256);
            sheet.SetColumnWidth(5, 22 * 256);
            sheet.SetColumnWidth(6, 22 * 256);
            sheet.SetColumnWidth(7, 14 * 256);
            sheet.SetColumnWidth(8, 35 * 256);
            sheet.SetColumnWidth(9, 16 * 256);
            sheet.SetColumnWidth(10, 10 * 256);

            using var ms = new MemoryStream();
            workbook.Write(ms);
            return ms.ToArray();
        }

        private static void SetCell(IRow row, int colIdx, string value, Dictionary<int, ICellStyle> styles)
        {
            var cell = row.GetCell(colIdx) ?? row.CreateCell(colIdx);
            cell.SetCellValue(value);
            if (styles.TryGetValue(colIdx, out var style))
            {
                cell.CellStyle = style;
            }
        }

        private static (int HeaderRowIdx, Dictionary<string, int> ColMap) DetectHeaderAndMapColumns(ISheet sheet)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int maxSearchRow = Math.Min(15, sheet.LastRowNum);

            for (int r = 0; r <= maxSearchRow; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                var candidateMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int c = 0; c < row.LastCellNum; c++)
                {
                    var cell = row.GetCell(c);
                    if (cell == null) continue;

                    string text = cell.ToString()?.Trim().ToLowerInvariant() ?? "";
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    // Match roles
                    if (MatchesKeyword(text, "stt", "câu số", "no", "item", "id", "{{stt}}", "{{id}}")) candidateMap["STT"] = c;
                    else if (MatchesKeyword(text, "câu hỏi", "nội dung", "đề bài", "question", "content", "{{content}}", "{{cau_hoi}}")) candidateMap["CONTENT"] = c;
                    else if (MatchesKeyword(text, "phương án a", "đáp án a", "lựa chọn a", "option a", "a)", "{{opta}}", "{{a}}")) candidateMap["OPTA"] = c;
                    else if (MatchesKeyword(text, "phương án b", "đáp án b", "lựa chọn b", "option b", "b)", "{{optb}}", "{{b}}")) candidateMap["OPTB"] = c;
                    else if (MatchesKeyword(text, "phương án c", "đáp án c", "lựa chọn c", "option c", "c)", "{{optc}}", "{{c}}")) candidateMap["OPTC"] = c;
                    else if (MatchesKeyword(text, "phương án d", "đáp án d", "lựa chọn d", "option d", "d)", "{{optd}}", "{{d}}")) candidateMap["OPTD"] = c;
                    else if (MatchesKeyword(text, "đáp án đúng", "đáp án", "kết quả", "key", "answer", "correct", "{{answer}}", "{{key}}")) candidateMap["ANSWER"] = c;
                    else if (MatchesKeyword(text, "lời giải", "hướng dẫn", "giải thích", "explanation", "solution", "{{explanation}}")) candidateMap["EXPLANATION"] = c;
                    else if (MatchesKeyword(text, "chủ đề", "chương", "topic", "category", "{{topic}}")) candidateMap["TOPIC"] = c;
                    else if (MatchesKeyword(text, "độ khó", "mức độ", "difficulty", "bloom", "{{difficulty}}")) candidateMap["DIFFICULTY"] = c;
                    else if (MatchesKeyword(text, "loại câu hỏi", "loại", "type", "{{type}}")) candidateMap["TYPE"] = c;
                    else if (MatchesKeyword(text, "bài đọc", "đọc hiểu", "ngữ cảnh", "passage", "context", "{{context}}")) candidateMap["CONTEXT"] = c;
                    else if (MatchesKeyword(text, "hình ảnh", "ảnh", "hình vẽ", "sơ đồ", "image", "picture", "{{image}}")) candidateMap["IMAGE"] = c;
                }

                // Nếu tìm thấy ít nhất 2 cột cốt lõi (CONTENT + ANSWER hoặc CONTENT + OPTA)
                if (candidateMap.ContainsKey("CONTENT") || (candidateMap.ContainsKey("OPTA") && candidateMap.ContainsKey("OPTB")))
                {
                    return (r, candidateMap);
                }
            }

            // Fallback mặc định
            map["STT"] = 0;
            map["CONTENT"] = 1;
            map["OPTA"] = 2;
            map["OPTB"] = 3;
            map["OPTC"] = 4;
            map["OPTD"] = 5;
            map["ANSWER"] = 6;
            map["EXPLANATION"] = 7;
            return (0, map);
        }

        private static bool MatchesKeyword(string text, params string[] keywords)
        {
            return keywords.Any(k => text.Equals(k, StringComparison.OrdinalIgnoreCase) || text.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetOption(DocxImportPreviewDto q, int idx)
        {
            if (q.Options == null || idx >= q.Options.Count) return string.Empty;
            return Regex.Replace(q.Options[idx], @"^(?:\[?[A-Ha-h0-9]\]?[\.\)\:]|\([A-Ha-h0-9]\))\s*", "").Trim();
        }

        private static string FormatAnswer(DocxImportPreviewDto q)
        {
            var raw = q.SuggestedAnswer?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var qType = (q.QuestionType ?? "SINGLE").ToUpperInvariant();
            if (qType == "SINGLE")
            {
                if (raw == "1") return "A";
                if (raw == "2") return "B";
                if (raw == "3") return "C";
                if (raw == "4") return "D";
            }
            return raw.ToUpperInvariant();
        }
    }
}
