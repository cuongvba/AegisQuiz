using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Excel.Strategies
{
    /// <summary>
    /// [Bộ GD&ĐT GDPT 2025 Multi-Sheet Export Strategy]
    /// Chiến lược xuất bản cấu trúc đề thi theo quy chuẩn Chương trình Giáo dục Phổ thông 2025:
    /// - Sheet 1: PHẦN I — Câu hỏi trắc nghiệm nhiều phương án lựa chọn (SINGLE).
    ///   + Hỗ trợ đầy đủ Cột Bài đọc hiểu / Ngữ cảnh cho đề Tiếng Anh (Merge dòng liên quan, chỉ xuất 1 lần).
    ///   + Hỗ trợ Cột Hình ảnh minh họa nhúng trực tiếp cho đề Toán & KHTN.
    /// - Sheet 2: PHẦN II — Câu hỏi trắc nghiệm Đúng/Sai (TRUE_FALSE - 4 ý a, b, c, d).
    /// - Sheet 3: PHẦN III — Câu hỏi trắc nghiệm dạng trả lời ngắn (SHORT_ANSWER).
    /// - Sheet 4: PHẦN IV — Câu hỏi tự luận hoặc dạng khác (ESSAY / OTHERS nếu có).
    /// </summary>
    public class MoetGdpt2025ExportStrategy : IExcelExportStrategy
    {
        public string TemplateCode => "GDPT_2025";
        public string DisplayName => "Bộ GD&ĐT GDPT 2025 (Chuẩn 3 Phần Chuyên Biệt)";
        public string Description => "Tự động phân tách câu hỏi thành 3 sheet: Phần I (Nhiều lựa chọn), Phần II (Đúng/Sai 4 ý), Phần III (Trả lời ngắn) kèm hình ảnh & bài đọc hiểu merge chuẩn mực.";
        public bool SupportsMultiSheet => true;
        public bool IsNativeIsomorphic => false;

        public byte[] Export(IReadOnlyList<DocxImportPreviewDto> questions, ExcelExportOptionsDto options)
        {
            using var workbook = new XSSFWorkbook();
            var styles = CreateStyles(workbook);

            // Gom nhóm câu hỏi theo phân đoạn chuẩn GDPT 2025
            var part1 = questions.Where(q => (q.QuestionType ?? "SINGLE").Equals("SINGLE", StringComparison.OrdinalIgnoreCase)).ToList();
            var part2 = questions.Where(q => (q.QuestionType ?? "").Equals("TRUE_FALSE", StringComparison.OrdinalIgnoreCase) || (q.QuestionType ?? "").Equals("MULTI_TRUE_FALSE", StringComparison.OrdinalIgnoreCase)).ToList();
            var part3 = questions.Where(q => (q.QuestionType ?? "").Equals("SHORT_ANSWER", StringComparison.OrdinalIgnoreCase)).ToList();
            var partOthers = questions.Except(part1).Except(part2).Except(part3).ToList();

            // Render Sheet 1: Phần I
            RenderPart1Sheet(workbook, workbook.CreateSheet("Phan_I_TracNghiem"), part1, styles, options);

            // Render Sheet 2: Phần II
            RenderPart2Sheet(workbook, workbook.CreateSheet("Phan_II_DungSai"), part2, styles, options);

            // Render Sheet 3: Phần III
            RenderPart3Sheet(workbook, workbook.CreateSheet("Phan_III_TraLoiNgan"), part3, styles, options);

            // Render Sheet 4: Phần Khác (nếu có)
            if (partOthers.Count > 0)
            {
                RenderPart1Sheet(workbook, workbook.CreateSheet("Phan_Khac"), partOthers, styles, options);
            }

            using var ms = new MemoryStream();
            workbook.Write(ms);
            return ms.ToArray();
        }

        private static void RenderPart1Sheet(XSSFWorkbook workbook, ISheet sheet, List<DocxImportPreviewDto> list, StyleSet styles, ExcelExportOptionsDto options)
        {
            sheet.DisplayGridlines = true;
            CreateBanner(sheet, "PHẦN I. CÂU HỎI TRẮC NGHIỆM NHIỀU PHƯƠNG ÁN LỰA CHỌN", list.Count, styles);

            var rHeader = sheet.CreateRow(2);
            rHeader.HeightInPoints = 28;
            string[] headers = {
                "STT",                          // 0
                "Bài đọc hiểu / Ngữ cảnh",      // 1
                "Nội dung câu hỏi",             // 2
                "Hình ảnh minh họa",            // 3
                "Phương án A",                  // 4
                "Phương án B",                  // 5
                "Phương án C",                  // 6
                "Phương án D",                  // 7
                "Đáp án",                       // 8
                "Lời giải chi tiết"             // 9
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var c = rHeader.CreateCell(i);
                c.SetCellValue(headers[i]);
                c.CellStyle = styles.HeaderStyle;
            }

            var contextGroups = BuildContextGroups(list);
            var groupFirstMap = contextGroups.ToDictionary(g => g.StartIdx, g => (g.Title, g.Content));
            var groupContinuationSet = new HashSet<int>(contextGroups.SelectMany(g => Enumerable.Range(g.StartIdx + 1, Math.Max(0, g.EndIdx - g.StartIdx))));

            var drawing = (XSSFDrawing)sheet.CreateDrawingPatriarch();
            int r = 3;

            for (int i = 0; i < list.Count; i++)
            {
                var q = list[i];
                var row = sheet.CreateRow(r);
                row.HeightInPoints = 28;

                // 0: STT
                var c0 = row.CreateCell(0);
                c0.SetCellValue(!string.IsNullOrWhiteSpace(q.SourceQuestionNo) ? q.SourceQuestionNo : (i + 1).ToString());
                c0.CellStyle = styles.CenterStyle;

                // 1: Bài đọc hiểu / Ngữ cảnh
                var c1 = row.CreateCell(1);
                c1.CellStyle = styles.ContextWrapStyle;
                if (groupFirstMap.TryGetValue(i, out var gInfo))
                {
                    string fullPassage = !string.IsNullOrWhiteSpace(gInfo.Title)
                        ? $"📖 【{gInfo.Title.Trim()}】\r\n\r\n{gInfo.Content.Trim()}"
                        : gInfo.Content.Trim();
                    c1.SetCellValue(ExcelCellSanitizer.Sanitize(fullPassage));
                }
                else if (groupContinuationSet.Contains(i))
                {
                    c1.SetCellValue(string.Empty);
                }
                else if (!string.IsNullOrWhiteSpace(q.ContextContent))
                {
                    string fullPassage = !string.IsNullOrWhiteSpace(q.ContextTitle)
                        ? $"📖 【{q.ContextTitle.Trim()}】\r\n\r\n{q.ContextContent.Trim()}"
                        : q.ContextContent.Trim();
                    c1.SetCellValue(ExcelCellSanitizer.Sanitize(fullPassage));
                }
                else
                {
                    c1.SetCellValue(string.Empty);
                }

                // 2: Nội dung câu hỏi
                var c2 = row.CreateCell(2);
                c2.SetCellValue(ExcelCellSanitizer.Sanitize(q.Content));
                c2.CellStyle = styles.WrapStyle;

                // 3: Hình ảnh minh họa
                var c3 = row.CreateCell(3);
                c3.CellStyle = styles.CenterStyle;
                var images = ExcelImageHelper.ExtractImages(q);
                if (options.IncludeImages && images.Count > 0)
                {
                    bool embedded = ExcelImageHelper.EmbedImageInCell(workbook, drawing, images[0].RawBytes, r, 3);
                    if (embedded)
                    {
                        row.HeightInPoints = Math.Max(row.HeightInPoints, 85);
                        c3.SetCellValue(string.Empty);
                    }
                    else
                    {
                        c3.SetCellValue("[Hình ảnh]");
                    }
                }
                else
                {
                    c3.SetCellValue(string.Empty);
                }

                // 4-7: Phương án A - D
                row.CreateCell(4).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 0)));
                row.CreateCell(5).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 1)));
                row.CreateCell(6).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 2)));
                row.CreateCell(7).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 3)));
                for (int col = 4; col <= 7; col++) row.GetCell(col).CellStyle = styles.WrapStyle;

                // 8: Đáp án
                var c8 = row.CreateCell(8);
                c8.SetCellValue(FormatSingleAnswer(q.SuggestedAnswer));
                c8.CellStyle = styles.CenterBoldStyle;

                // 9: Lời giải chi tiết
                var c9 = row.CreateCell(9);
                c9.SetCellValue(options.IncludeExplanation ? ExcelCellSanitizer.Sanitize(q.AiExplanation) : string.Empty);
                c9.CellStyle = styles.WrapStyle;

                r++;
            }

            // Merge các dòng liên quan của Cột 1 (Bài đọc hiểu)
            foreach (var g in contextGroups)
            {
                if (g.EndIdx > g.StartIdx)
                {
                    sheet.AddMergedRegion(new CellRangeAddress(3 + g.StartIdx, 3 + g.EndIdx, 1, 1));
                }
            }

            sheet.SetColumnWidth(0, 8 * 256);   // STT
            sheet.SetColumnWidth(1, 35 * 256);  // Bài đọc hiểu / Ngữ cảnh
            sheet.SetColumnWidth(2, 42 * 256);  // Nội dung câu hỏi
            sheet.SetColumnWidth(3, 26 * 256);  // Hình ảnh minh họa
            sheet.SetColumnWidth(4, 20 * 256);  // Phương án A
            sheet.SetColumnWidth(5, 20 * 256);  // Phương án B
            sheet.SetColumnWidth(6, 20 * 256);  // Phương án C
            sheet.SetColumnWidth(7, 20 * 256);  // Phương án D
            sheet.SetColumnWidth(8, 12 * 256);  // Đáp án
            sheet.SetColumnWidth(9, 35 * 256);  // Lời giải chi tiết
        }

        private static void RenderPart2Sheet(XSSFWorkbook workbook, ISheet sheet, List<DocxImportPreviewDto> list, StyleSet styles, ExcelExportOptionsDto options)
        {
            sheet.DisplayGridlines = true;
            CreateBanner(sheet, "PHẦN II. CÂU HỎI TRẮC NGHIỆM ĐÚNG / SAI (4 MỆNH ĐỀ)", list.Count, styles);

            var rHeader = sheet.CreateRow(2);
            rHeader.HeightInPoints = 28;
            string[] headers = {
                "STT",                              // 0
                "Ngữ cảnh / Tình huống",            // 1
                "Nội dung câu hỏi",                 // 2
                "Hình ảnh minh họa",                // 3
                "Mệnh đề a)",                       // 4
                "Mệnh đề b)",                       // 5
                "Mệnh đề c)",                       // 6
                "Mệnh đề d)",                       // 7
                "Đáp án (a,b,c,d: Đ/S)",            // 8
                "Lời giải chi tiết"                 // 9
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var c = rHeader.CreateCell(i);
                c.SetCellValue(headers[i]);
                c.CellStyle = styles.HeaderStyle;
            }

            var contextGroups = BuildContextGroups(list);
            var groupFirstMap = contextGroups.ToDictionary(g => g.StartIdx, g => (g.Title, g.Content));
            var groupContinuationSet = new HashSet<int>(contextGroups.SelectMany(g => Enumerable.Range(g.StartIdx + 1, Math.Max(0, g.EndIdx - g.StartIdx))));

            var drawing = (XSSFDrawing)sheet.CreateDrawingPatriarch();
            int r = 3;

            for (int i = 0; i < list.Count; i++)
            {
                var q = list[i];
                var row = sheet.CreateRow(r);
                row.HeightInPoints = 28;

                // 0: STT
                var c0 = row.CreateCell(0);
                c0.SetCellValue(!string.IsNullOrWhiteSpace(q.SourceQuestionNo) ? q.SourceQuestionNo : (i + 1).ToString());
                c0.CellStyle = styles.CenterStyle;

                // 1: Ngữ cảnh
                var c1 = row.CreateCell(1);
                c1.CellStyle = styles.ContextWrapStyle;
                if (groupFirstMap.TryGetValue(i, out var gInfo))
                {
                    string fullPassage = !string.IsNullOrWhiteSpace(gInfo.Title) ? $"📖 【{gInfo.Title.Trim()}】\r\n\r\n{gInfo.Content.Trim()}" : gInfo.Content.Trim();
                    c1.SetCellValue(ExcelCellSanitizer.Sanitize(fullPassage));
                }
                else if (groupContinuationSet.Contains(i))
                {
                    c1.SetCellValue(string.Empty);
                }
                else if (!string.IsNullOrWhiteSpace(q.ContextContent))
                {
                    string fullPassage = !string.IsNullOrWhiteSpace(q.ContextTitle) ? $"📖 【{q.ContextTitle.Trim()}】\r\n\r\n{q.ContextContent.Trim()}" : q.ContextContent.Trim();
                    c1.SetCellValue(ExcelCellSanitizer.Sanitize(fullPassage));
                }
                else
                {
                    c1.SetCellValue(string.Empty);
                }

                // 2: Nội dung câu hỏi
                var c2 = row.CreateCell(2);
                c2.SetCellValue(ExcelCellSanitizer.Sanitize(q.Content));
                c2.CellStyle = styles.WrapStyle;

                // 3: Hình ảnh minh họa
                var c3 = row.CreateCell(3);
                c3.CellStyle = styles.CenterStyle;
                var images = ExcelImageHelper.ExtractImages(q);
                if (options.IncludeImages && images.Count > 0)
                {
                    bool embedded = ExcelImageHelper.EmbedImageInCell(workbook, drawing, images[0].RawBytes, r, 3);
                    if (embedded)
                    {
                        row.HeightInPoints = Math.Max(row.HeightInPoints, 85);
                        c3.SetCellValue(string.Empty);
                    }
                    else
                    {
                        c3.SetCellValue("[Hình ảnh]");
                    }
                }
                else
                {
                    c3.SetCellValue(string.Empty);
                }

                // 4-7: Mệnh đề a-d
                row.CreateCell(4).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 0)));
                row.CreateCell(5).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 1)));
                row.CreateCell(6).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 2)));
                row.CreateCell(7).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 3)));
                for (int col = 4; col <= 7; col++) row.GetCell(col).CellStyle = styles.WrapStyle;

                // 8: Đáp án
                var c8 = row.CreateCell(8);
                c8.SetCellValue(q.SuggestedAnswer ?? string.Empty);
                c8.CellStyle = styles.CenterBoldStyle;

                // 9: Lời giải chi tiết
                var c9 = row.CreateCell(9);
                c9.SetCellValue(options.IncludeExplanation ? ExcelCellSanitizer.Sanitize(q.AiExplanation) : string.Empty);
                c9.CellStyle = styles.WrapStyle;

                r++;
            }

            foreach (var g in contextGroups)
            {
                if (g.EndIdx > g.StartIdx)
                {
                    sheet.AddMergedRegion(new CellRangeAddress(3 + g.StartIdx, 3 + g.EndIdx, 1, 1));
                }
            }

            sheet.SetColumnWidth(0, 8 * 256);
            sheet.SetColumnWidth(1, 35 * 256);
            sheet.SetColumnWidth(2, 42 * 256);
            sheet.SetColumnWidth(3, 26 * 256);
            sheet.SetColumnWidth(4, 20 * 256);
            sheet.SetColumnWidth(5, 20 * 256);
            sheet.SetColumnWidth(6, 20 * 256);
            sheet.SetColumnWidth(7, 20 * 256);
            sheet.SetColumnWidth(8, 20 * 256);
            sheet.SetColumnWidth(9, 35 * 256);
        }

        private static void RenderPart3Sheet(XSSFWorkbook workbook, ISheet sheet, List<DocxImportPreviewDto> list, StyleSet styles, ExcelExportOptionsDto options)
        {
            sheet.DisplayGridlines = true;
            CreateBanner(sheet, "PHẦN III. CÂU HỎI TRẮC NGHIỆM DẠNG TRẢ LỜI NGẮN", list.Count, styles);

            var rHeader = sheet.CreateRow(2);
            rHeader.HeightInPoints = 28;
            string[] headers = {
                "STT",                          // 0
                "Ngữ cảnh / Dữ liệu",           // 1
                "Nội dung câu hỏi / Bài toán",  // 2
                "Hình ảnh minh họa",            // 3
                "Đáp số / Giá trị số",          // 4
                "Lời giải & Phương pháp giải"   // 5
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var c = rHeader.CreateCell(i);
                c.SetCellValue(headers[i]);
                c.CellStyle = styles.HeaderStyle;
            }

            var contextGroups = BuildContextGroups(list);
            var groupFirstMap = contextGroups.ToDictionary(g => g.StartIdx, g => (g.Title, g.Content));
            var groupContinuationSet = new HashSet<int>(contextGroups.SelectMany(g => Enumerable.Range(g.StartIdx + 1, Math.Max(0, g.EndIdx - g.StartIdx))));

            var drawing = (XSSFDrawing)sheet.CreateDrawingPatriarch();
            int r = 3;

            for (int i = 0; i < list.Count; i++)
            {
                var q = list[i];
                var row = sheet.CreateRow(r);
                row.HeightInPoints = 28;

                // 0: STT
                var c0 = row.CreateCell(0);
                c0.SetCellValue(!string.IsNullOrWhiteSpace(q.SourceQuestionNo) ? q.SourceQuestionNo : (i + 1).ToString());
                c0.CellStyle = styles.CenterStyle;

                // 1: Ngữ cảnh
                var c1 = row.CreateCell(1);
                c1.CellStyle = styles.ContextWrapStyle;
                if (groupFirstMap.TryGetValue(i, out var gInfo))
                {
                    string fullPassage = !string.IsNullOrWhiteSpace(gInfo.Title) ? $"📖 【{gInfo.Title.Trim()}】\r\n\r\n{gInfo.Content.Trim()}" : gInfo.Content.Trim();
                    c1.SetCellValue(ExcelCellSanitizer.Sanitize(fullPassage));
                }
                else if (groupContinuationSet.Contains(i))
                {
                    c1.SetCellValue(string.Empty);
                }
                else if (!string.IsNullOrWhiteSpace(q.ContextContent))
                {
                    string fullPassage = !string.IsNullOrWhiteSpace(q.ContextTitle) ? $"📖 【{q.ContextTitle.Trim()}】\r\n\r\n{q.ContextContent.Trim()}" : q.ContextContent.Trim();
                    c1.SetCellValue(ExcelCellSanitizer.Sanitize(fullPassage));
                }
                else
                {
                    c1.SetCellValue(string.Empty);
                }

                // 2: Nội dung câu hỏi
                var c2 = row.CreateCell(2);
                c2.SetCellValue(ExcelCellSanitizer.Sanitize(q.Content));
                c2.CellStyle = styles.WrapStyle;

                // 3: Hình ảnh minh họa
                var c3 = row.CreateCell(3);
                c3.CellStyle = styles.CenterStyle;
                var images = ExcelImageHelper.ExtractImages(q);
                if (options.IncludeImages && images.Count > 0)
                {
                    bool embedded = ExcelImageHelper.EmbedImageInCell(workbook, drawing, images[0].RawBytes, r, 3);
                    if (embedded)
                    {
                        row.HeightInPoints = Math.Max(row.HeightInPoints, 85);
                        c3.SetCellValue(string.Empty);
                    }
                    else
                    {
                        c3.SetCellValue("[Hình ảnh]");
                    }
                }
                else
                {
                    c3.SetCellValue(string.Empty);
                }

                // 4: Đáp số
                var c4 = row.CreateCell(4);
                c4.SetCellValue(q.SuggestedAnswer ?? string.Empty);
                c4.CellStyle = styles.CenterBoldStyle;

                // 5: Lời giải
                var c5 = row.CreateCell(5);
                c5.SetCellValue(options.IncludeExplanation ? ExcelCellSanitizer.Sanitize(q.AiExplanation) : string.Empty);
                c5.CellStyle = styles.WrapStyle;

                r++;
            }

            foreach (var g in contextGroups)
            {
                if (g.EndIdx > g.StartIdx)
                {
                    sheet.AddMergedRegion(new CellRangeAddress(3 + g.StartIdx, 3 + g.EndIdx, 1, 1));
                }
            }

            sheet.SetColumnWidth(0, 8 * 256);
            sheet.SetColumnWidth(1, 35 * 256);
            sheet.SetColumnWidth(2, 45 * 256);
            sheet.SetColumnWidth(3, 26 * 256);
            sheet.SetColumnWidth(4, 20 * 256);
            sheet.SetColumnWidth(5, 40 * 256);
        }

        private static List<(int StartIdx, int EndIdx, string Title, string Content)> BuildContextGroups(List<DocxImportPreviewDto> list)
        {
            var groups = new List<(int StartIdx, int EndIdx, string Title, string Content)>();
            int start = -1;
            string key = string.Empty;
            string title = string.Empty;
            string content = string.Empty;

            for (int i = 0; i < list.Count; i++)
            {
                var q = list[i];
                string cKey = !string.IsNullOrWhiteSpace(q.ContextId)
                    ? q.ContextId.Trim()
                    : (!string.IsNullOrWhiteSpace(q.ContextContent) ? q.ContextContent.Trim() : string.Empty);

                if (!string.IsNullOrWhiteSpace(cKey))
                {
                    if (start == -1)
                    {
                        start = i;
                        key = cKey;
                        title = q.ContextTitle ?? string.Empty;
                        content = q.ContextContent ?? string.Empty;
                    }
                    else if (cKey == key)
                    {
                        if (string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(q.ContextTitle))
                        {
                            title = q.ContextTitle;
                        }
                    }
                    else
                    {
                        groups.Add((start, i - 1, title, content));
                        start = i;
                        key = cKey;
                        title = q.ContextTitle ?? string.Empty;
                        content = q.ContextContent ?? string.Empty;
                    }
                }
                else
                {
                    if (start != -1)
                    {
                        groups.Add((start, i - 1, title, content));
                        start = -1;
                        key = string.Empty;
                    }
                }
            }
            if (start != -1)
            {
                groups.Add((start, list.Count - 1, title, content));
            }

            return groups;
        }

        private static void CreateBanner(ISheet sheet, string title, int count, StyleSet styles)
        {
            var r0 = sheet.CreateRow(0);
            r0.HeightInPoints = 24;
            var c0 = r0.CreateCell(0);
            c0.SetCellValue(title);
            c0.CellStyle = styles.BannerStyle;

            var r1 = sheet.CreateRow(1);
            r1.HeightInPoints = 18;
            var c1 = r1.CreateCell(0);
            c1.SetCellValue($"Số lượng câu hỏi: {count} câu | Quy chuẩn: Bộ Giáo Dục & Đào Tạo 2025");
            c1.CellStyle = styles.MetaStyle;
        }

        private static string GetOption(DocxImportPreviewDto q, int idx)
        {
            if (q.Options == null || idx >= q.Options.Count) return string.Empty;
            var raw = q.Options[idx];
            return Regex.Replace(raw, @"^(?:\[?[A-Ha-h0-9]\]?[\.\)\:]|\([A-Ha-h0-9]\))\s*", "").Trim();
        }

        private static string FormatSingleAnswer(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var trimmed = raw.Trim().ToUpperInvariant();
            if (trimmed == "1") return "A";
            if (trimmed == "2") return "B";
            if (trimmed == "3") return "C";
            if (trimmed == "4") return "D";
            return trimmed;
        }

        private class StyleSet
        {
            public ICellStyle BannerStyle { get; set; } = null!;
            public ICellStyle MetaStyle { get; set; } = null!;
            public ICellStyle HeaderStyle { get; set; } = null!;
            public ICellStyle WrapStyle { get; set; } = null!;
            public ICellStyle ContextWrapStyle { get; set; } = null!;
            public ICellStyle CenterStyle { get; set; } = null!;
            public ICellStyle CenterBoldStyle { get; set; } = null!;
        }

        private static StyleSet CreateStyles(IWorkbook workbook)
        {
            var set = new StyleSet();

            var fBanner = workbook.CreateFont();
            fBanner.FontName = "Segoe UI";
            fBanner.FontHeightInPoints = 12;
            fBanner.IsBold = true;
            fBanner.Color = IndexedColors.DarkBlue.Index;

            var fMeta = workbook.CreateFont();
            fMeta.FontName = "Segoe UI";
            fMeta.FontHeightInPoints = 9;
            fMeta.IsItalic = true;
            fMeta.Color = IndexedColors.Grey50Percent.Index;

            var fHeader = workbook.CreateFont();
            fHeader.FontName = "Segoe UI";
            fHeader.FontHeightInPoints = 10;
            fHeader.IsBold = true;
            fHeader.Color = IndexedColors.White.Index;

            var fBody = workbook.CreateFont();
            fBody.FontName = "Segoe UI";
            fBody.FontHeightInPoints = 10;

            var fContext = workbook.CreateFont();
            fContext.FontName = "Segoe UI";
            fContext.FontHeightInPoints = 9;

            var fBold = workbook.CreateFont();
            fBold.FontName = "Segoe UI";
            fBold.FontHeightInPoints = 10;
            fBold.IsBold = true;
            fBold.Color = IndexedColors.DarkBlue.Index;

            var sBanner = workbook.CreateCellStyle();
            sBanner.SetFont(fBanner);
            sBanner.VerticalAlignment = VerticalAlignment.Center;
            set.BannerStyle = sBanner;

            var sMeta = workbook.CreateCellStyle();
            sMeta.SetFont(fMeta);
            sMeta.VerticalAlignment = VerticalAlignment.Center;
            set.MetaStyle = sMeta;

            var sHeader = workbook.CreateCellStyle();
            sHeader.SetFont(fHeader);
            sHeader.FillForegroundColor = IndexedColors.RoyalBlue.Index;
            sHeader.FillPattern = FillPattern.SolidForeground;
            sHeader.Alignment = HorizontalAlignment.Center;
            sHeader.VerticalAlignment = VerticalAlignment.Center;
            SetBorders(sHeader, BorderStyle.Thin, IndexedColors.Grey25Percent.Index);
            set.HeaderStyle = sHeader;

            var sWrap = workbook.CreateCellStyle();
            sWrap.SetFont(fBody);
            sWrap.WrapText = true;
            sWrap.VerticalAlignment = VerticalAlignment.Center;
            SetBorders(sWrap, BorderStyle.Thin, IndexedColors.Grey25Percent.Index);
            set.WrapStyle = sWrap;

            var sContext = workbook.CreateCellStyle();
            sContext.SetFont(fContext);
            sContext.WrapText = true;
            sContext.VerticalAlignment = VerticalAlignment.Top;
            SetBorders(sContext, BorderStyle.Thin, IndexedColors.Grey25Percent.Index);
            set.ContextWrapStyle = sContext;

            var sCenter = workbook.CreateCellStyle();
            sCenter.SetFont(fBody);
            sCenter.Alignment = HorizontalAlignment.Center;
            sCenter.VerticalAlignment = VerticalAlignment.Center;
            SetBorders(sCenter, BorderStyle.Thin, IndexedColors.Grey25Percent.Index);
            set.CenterStyle = sCenter;

            var sCenterBold = workbook.CreateCellStyle();
            sCenterBold.CloneStyleFrom(sCenter);
            sCenterBold.SetFont(fBold);
            sCenterBold.FillForegroundColor = IndexedColors.LightTurquoise.Index;
            sCenterBold.FillPattern = FillPattern.SolidForeground;
            set.CenterBoldStyle = sCenterBold;

            return set;
        }

        private static void SetBorders(ICellStyle style, BorderStyle border, short color)
        {
            style.BorderTop = border;
            style.BorderBottom = border;
            style.BorderLeft = border;
            style.BorderRight = border;
            style.TopBorderColor = color;
            style.BottomBorderColor = color;
            style.LeftBorderColor = color;
            style.RightBorderColor = color;
        }
    }
}
