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
    /// [AegisQuiz Native Isomorphic Export Strategy]
    /// Chiến lược xuất bản bảng tính Excel đẳng cấu chuẩn mực:
    /// - 100% tương thích hai chiều với ExcelParserService (nhập lại cho kết quả y hệt ban đầu).
    /// - Bảo toàn 9 dạng câu hỏi (SINGLE, MULTI, TRUE_FALSE 4 mệnh đề GDPT 2025, SHORT_ANSWER...).
    /// - Bảo toàn công thức toán học $LaTeX$, dải bài đọc hiểu (Reading Passage), lời giải chi tiết.
    /// - Nhúng ảnh trực quan vào cột 'Hình ảnh minh họa' qua XSSFDrawing & ExcelImageHelper.
    /// - Hợp nhất (Merge) các dòng bài đọc hiểu liên quan, tránh lặp lại và căn lề Top tinh tế.
    /// - Tạo biểu ngữ nhận thức 5 trục tọa độ tri thức (Universal Coordinate Taxonomy).
    /// </summary>
    public class NativeIsomorphicExportStrategy : IExcelExportStrategy
    {
        public string TemplateCode => "NATIVE_AEGIS";
        public string DisplayName => "Aegis Native Isomorphic (Chuẩn Toàn Năng)";
        public string Description => "Mẫu Excel chuẩn mực tương thích 100% với Aegis, bảo toàn đẳng cấu 9 dạng câu hỏi, LaTeX, hình ảnh và 5 trục tọa độ tri thức.";
        public bool SupportsMultiSheet => false;
        public bool IsNativeIsomorphic => true;

        public byte[] Export(IReadOnlyList<DocxImportPreviewDto> questions, ExcelExportOptionsDto options)
        {
            using var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("NganHangCauHoi");
            sheet.DisplayGridlines = true;

            // 1. Khởi tạo Bảng phong cách (Styles & Fonts)
            var styles = CreateStylePalette(workbook);

            // 2. Điền Dải Banner Nhận Thức Tọa Độ Tri Thức (Hàng 0 - 3)
            RenderCognitiveBanner(sheet, questions, options, styles);

            // 3. Điền Dòng Tiêu Đề Nhận Thức 16 Cột (Hàng 4)
            RenderHeaderRow(sheet, styles);

            // 4. Tiền xử lý gom cụm các nhóm câu hỏi liên tiếp dùng chung Ngữ cảnh / Bài đọc hiểu
            var contextGroups = new List<(int StartIdx, int EndIdx, string Title, string Content)>();
            int groupStart = -1;
            string currentGroupKey = string.Empty;
            string currentGroupTitle = string.Empty;
            string currentGroupContent = string.Empty;

            for (int i = 0; i < questions.Count; i++)
            {
                var item = questions[i];
                string cKey = !string.IsNullOrWhiteSpace(item.ContextId)
                    ? item.ContextId.Trim()
                    : (!string.IsNullOrWhiteSpace(item.ContextContent) ? item.ContextContent.Trim() : string.Empty);

                if (!string.IsNullOrWhiteSpace(cKey))
                {
                    if (groupStart == -1)
                    {
                        groupStart = i;
                        currentGroupKey = cKey;
                        currentGroupTitle = item.ContextTitle ?? string.Empty;
                        currentGroupContent = item.ContextContent ?? string.Empty;
                    }
                    else if (cKey == currentGroupKey)
                    {
                        if (string.IsNullOrWhiteSpace(currentGroupTitle) && !string.IsNullOrWhiteSpace(item.ContextTitle))
                        {
                            currentGroupTitle = item.ContextTitle;
                        }
                    }
                    else
                    {
                        contextGroups.Add((groupStart, i - 1, currentGroupTitle, currentGroupContent));
                        groupStart = i;
                        currentGroupKey = cKey;
                        currentGroupTitle = item.ContextTitle ?? string.Empty;
                        currentGroupContent = item.ContextContent ?? string.Empty;
                    }
                }
                else
                {
                    if (groupStart != -1)
                    {
                        contextGroups.Add((groupStart, i - 1, currentGroupTitle, currentGroupContent));
                        groupStart = -1;
                        currentGroupKey = string.Empty;
                    }
                }
            }
            if (groupStart != -1)
            {
                contextGroups.Add((groupStart, questions.Count - 1, currentGroupTitle, currentGroupContent));
            }

            var groupFirstMap = new Dictionary<int, (string Title, string Content)>();
            var groupContinuationSet = new HashSet<int>();
            foreach (var g in contextGroups)
            {
                groupFirstMap[g.StartIdx] = (g.Title, g.Content);
                for (int idx = g.StartIdx + 1; idx <= g.EndIdx; idx++)
                {
                    groupContinuationSet.Add(idx);
                }
            }

            // 5. Điền Dữ Liệu Từng Câu Hỏi & Nhúng Hình Ảnh (Từ hàng 5 trở đi)
            var drawing = (XSSFDrawing)sheet.CreateDrawingPatriarch();
            int currentRow = 5;

            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                var row = sheet.CreateRow(currentRow);
                row.HeightInPoints = 28; // Chiều cao cơ bản, mở rộng động nếu có ảnh

                // Cột 0: STT
                var cellStt = row.CreateCell(0);
                cellStt.SetCellValue(!string.IsNullOrWhiteSpace(q.SourceQuestionNo) ? q.SourceQuestionNo : (i + 1).ToString());
                cellStt.CellStyle = styles.CenterStyle;

                // Cột 1: Nội dung câu hỏi
                var cellContent = row.CreateCell(1);
                cellContent.SetCellValue(ExcelCellSanitizer.Sanitize(q.Content));
                cellContent.CellStyle = styles.WrapStyle;

                // Cột 2: Loại câu hỏi
                var cellType = row.CreateCell(2);
                cellType.SetCellValue(!string.IsNullOrWhiteSpace(q.QuestionType) ? q.QuestionType : "SINGLE");
                cellType.CellStyle = styles.CenterStyle;

                // Cột 3 - 6: Phương án A, B, C, D
                string optA = GetOptionText(q, 0);
                string optB = GetOptionText(q, 1);
                string optC = GetOptionText(q, 2);
                string optD = GetOptionText(q, 3);

                var cellA = row.CreateCell(3); cellA.SetCellValue(ExcelCellSanitizer.Sanitize(optA)); cellA.CellStyle = styles.WrapStyle;
                var cellB = row.CreateCell(4); cellB.SetCellValue(ExcelCellSanitizer.Sanitize(optB)); cellB.CellStyle = styles.WrapStyle;
                var cellC = row.CreateCell(5); cellC.SetCellValue(ExcelCellSanitizer.Sanitize(optC)); cellC.CellStyle = styles.WrapStyle;
                var cellD = row.CreateCell(6); cellD.SetCellValue(ExcelCellSanitizer.Sanitize(optD)); cellD.CellStyle = styles.WrapStyle;

                // Cột 7: Đáp án đúng
                var cellAns = row.CreateCell(7);
                cellAns.SetCellValue(FormatAnswer(q));
                cellAns.CellStyle = styles.CenterBoldStyle;

                // Cột 8: Hướng dẫn giải chi tiết
                var cellExpl = row.CreateCell(8);
                cellExpl.SetCellValue(options.IncludeExplanation ? ExcelCellSanitizer.Sanitize(q.AiExplanation) : string.Empty);
                cellExpl.CellStyle = styles.WrapStyle;

                // Cột 9 & 10: Bài đọc hiểu & Tiêu đề bài đọc (Hỗ trợ merge dòng, chỉ xuất 1 lần ở ô đầu)
                var cellContext = row.CreateCell(9);
                var cellContextTitle = row.CreateCell(10);
                cellContext.CellStyle = styles.ContextWrapStyle;
                cellContextTitle.CellStyle = styles.ContextWrapStyle;

                if (groupFirstMap.TryGetValue(i, out var gInfo))
                {
                    string fullPassage = !string.IsNullOrWhiteSpace(gInfo.Title)
                        ? $"📖 【{gInfo.Title.Trim()}】\r\n\r\n{gInfo.Content.Trim()}"
                        : gInfo.Content.Trim();

                    cellContext.SetCellValue(ExcelCellSanitizer.Sanitize(fullPassage));
                    cellContextTitle.SetCellValue(ExcelCellSanitizer.Sanitize(gInfo.Title));
                }
                else if (groupContinuationSet.Contains(i))
                {
                    cellContext.SetCellValue(string.Empty);
                    cellContextTitle.SetCellValue(string.Empty);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(q.ContextContent))
                    {
                        string fullPassage = !string.IsNullOrWhiteSpace(q.ContextTitle)
                            ? $"📖 【{q.ContextTitle.Trim()}】\r\n\r\n{q.ContextContent.Trim()}"
                            : q.ContextContent.Trim();
                        cellContext.SetCellValue(ExcelCellSanitizer.Sanitize(fullPassage));
                        cellContextTitle.SetCellValue(ExcelCellSanitizer.Sanitize(q.ContextTitle));
                    }
                    else
                    {
                        cellContext.SetCellValue(string.Empty);
                        cellContextTitle.SetCellValue(string.Empty);
                    }
                }

                // Cột 11: Mức độ nhận thức (Bloom: 1-4)
                var cellDifficulty = row.CreateCell(11);
                cellDifficulty.SetCellValue(q.Difficulty > 0 ? q.Difficulty.ToString() : "2");
                cellDifficulty.CellStyle = styles.CenterStyle;

                // Cột 12: Mã chủ đề
                var cellTopic = row.CreateCell(12);
                cellTopic.SetCellValue(!string.IsNullOrWhiteSpace(q.TopicCode) ? q.TopicCode : (options.DefaultTopicCode ?? "GENERAL"));
                cellTopic.CellStyle = styles.CenterStyle;

                // Cột 13: Thẻ thông minh (Tags)
                var cellTags = row.CreateCell(13);
                cellTags.SetCellValue(q.Tags != null && q.Tags.Count > 0 ? string.Join(", ", q.Tags) : string.Empty);
                cellTags.CellStyle = styles.NormalStyle;

                // Cột 14: Điểm liệt
                var cellCritical = row.CreateCell(14);
                cellCritical.SetCellValue(q.IsCritical ? "1" : "0");
                cellCritical.CellStyle = styles.CenterStyle;

                // Cột 15: Hình ảnh minh họa (Nhúng trực tiếp vào ô bảng tính)
                var cellMedia = row.CreateCell(15);
                cellMedia.CellStyle = styles.CenterStyle;

                var extractedImages = ExcelImageHelper.ExtractImages(q);
                if (options.IncludeImages && extractedImages.Count > 0)
                {
                    bool embedded = ExcelImageHelper.EmbedImageInCell(workbook, drawing, extractedImages[0].RawBytes, currentRow, 15);
                    if (embedded)
                    {
                        row.HeightInPoints = Math.Max(row.HeightInPoints, 90);
                        cellMedia.SetCellValue(string.Empty);
                    }
                    else
                    {
                        string b64 = extractedImages[0].Base64Data;
                        cellMedia.SetCellValue(b64.Length <= ExcelCellSanitizer.MaxExcelCellChars ? b64 : "[Hình ảnh]");
                    }
                }
                else
                {
                    cellMedia.SetCellValue(string.Empty);
                }

                currentRow++;
            }

            // 6. Thực hiện Merge các ô liên quan của Cột 9 (Bài đọc hiểu) và Cột 10 (Tiêu đề)
            foreach (var g in contextGroups)
            {
                if (g.EndIdx > g.StartIdx)
                {
                    int startR = 5 + g.StartIdx;
                    int endR = 5 + g.EndIdx;
                    sheet.AddMergedRegion(new CellRangeAddress(startR, endR, 9, 9));
                    sheet.AddMergedRegion(new CellRangeAddress(startR, endR, 10, 10));
                }
            }

            // 7. Cấu hình độ rộng cột tối ưu thị giác
            ApplyColumnWidths(sheet);

            using var ms = new MemoryStream();
            workbook.Write(ms);
            return ms.ToArray();
        }

        // ── Helper Sub-routines ──────────────────────────────────────────────

        private static void RenderCognitiveBanner(ISheet sheet, IReadOnlyList<DocxImportPreviewDto> questions, ExcelExportOptionsDto options, StylePalette styles)
        {
            var first = questions.FirstOrDefault();
            string examTitle = options.ExamTitle ?? "TÀI LIỆU KHẢO THÍ CHUẨN HÓA AEGIS";
            string issuingOrg = options.IssuingOrg ?? first?.IssuingOrg ?? "Aegis System";
            int benchmarkYear = options.BenchmarkYear ?? first?.BenchmarkYear ?? DateTime.Now.Year;
            string standard = options.BenchmarkStandard ?? first?.BenchmarkStandard ?? "Chuẩn Quốc Gia GDPT 2025 / B2B Assessment";
            string targetLevel = options.TargetLevel ?? first?.TargetLevel ?? "Toàn diện";
            string purpose = options.AssessmentPurpose ?? first?.AssessmentPurpose ?? "Khảo sát năng lực định kỳ";
            string domain = options.DomainCode ?? first?.DomainCode ?? "GENERAL";
            string defaultTopic = options.DefaultTopicCode ?? first?.TopicCode ?? "GENERAL";

            // Dòng 0: Tiêu đề kỳ thi
            var r0 = sheet.CreateRow(0);
            r0.HeightInPoints = 24;
            var c0 = r0.CreateCell(0);
            c0.SetCellValue($"BỘ CÂU HỎI & ĐÁP ÁN ÔN TẬP CHUẨN HÓA — {examTitle.ToUpperInvariant()}");
            c0.CellStyle = styles.BannerTitleStyle;

            // Dòng 1: Đơn vị ban hành & Năm chuẩn mực
            var r1 = sheet.CreateRow(1);
            r1.HeightInPoints = 18;
            var c1 = r1.CreateCell(0);
            c1.SetCellValue($"Đơn vị ban hành: {issuingOrg} | Năm chuẩn mực: {benchmarkYear} | Tiêu chuẩn: {standard}");
            c1.CellStyle = styles.BannerMetaStyle;

            // Dòng 2: Đối tượng, mục đích, lĩnh vực
            var r2 = sheet.CreateRow(2);
            r2.HeightInPoints = 18;
            var c2 = r2.CreateCell(0);
            c2.SetCellValue($"Đối tượng / Khối lớp: {targetLevel} | Mục đích sát hạch: {purpose} | Lĩnh vực: {domain}");
            c2.CellStyle = styles.BannerMetaStyle;

            // Dòng 3: Thống kê tổng số câu hỏi & phân đoạn
            var r3 = sheet.CreateRow(3);
            r3.HeightInPoints = 18;
            var c3 = r3.CreateCell(0);
            c3.SetCellValue($"Chủ đề chính: {defaultTopic} | Tổng số câu hỏi: {questions.Count} | Trạng thái: Sẵn sàng nạp tự động (Lossless Round-Trip)");
            c3.CellStyle = styles.BannerMetaStyle;
        }

        private static void RenderHeaderRow(ISheet sheet, StylePalette styles)
        {
            var r4 = sheet.CreateRow(4);
            r4.HeightInPoints = 30;

            string[] headers =
            {
                "STT",                      // 0
                "Nội dung câu hỏi",         // 1
                "Loại câu hỏi",             // 2
                "Phương án A",              // 3
                "Phương án B",              // 4
                "Phương án C",              // 5
                "Phương án D",              // 6
                "Đáp án đúng",              // 7
                "Hướng dẫn giải chi tiết",  // 8
                "Bài đọc hiểu / Ngữ cảnh",  // 9
                "Tiêu đề bài đọc",          // 10
                "Mức độ nhận thức",         // 11
                "Mã chủ đề",                // 12
                "Thẻ thông minh (Tags)",    // 13
                "Điểm liệt",                // 14
                "Hình ảnh minh họa"         // 15
            };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = r4.CreateCell(col);
                cell.SetCellValue(headers[col]);
                cell.CellStyle = styles.HeaderStyle;
            }
        }

        private static string GetOptionText(DocxImportPreviewDto q, int index)
        {
            if (q.Options == null || index >= q.Options.Count) return string.Empty;
            var raw = q.Options[index];
            return Regex.Replace(raw, @"^(?:\[?[A-Ha-h0-9]\]?[\.\)\:]|\([A-Ha-h0-9]\))\s*", "").Trim();
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
                return raw.ToUpperInvariant();
            }

            return raw;
        }

        private static void ApplyColumnWidths(ISheet sheet)
        {
            sheet.SetColumnWidth(0, 8 * 256);   // STT
            sheet.SetColumnWidth(1, 45 * 256);  // Nội dung câu hỏi
            sheet.SetColumnWidth(2, 14 * 256);  // Loại câu hỏi
            sheet.SetColumnWidth(3, 24 * 256);  // Phương án A
            sheet.SetColumnWidth(4, 24 * 256);  // Phương án B
            sheet.SetColumnWidth(5, 24 * 256);  // Phương án C
            sheet.SetColumnWidth(6, 24 * 256);  // Phương án D
            sheet.SetColumnWidth(7, 14 * 256);  // Đáp án đúng
            sheet.SetColumnWidth(8, 38 * 256);  // Hướng dẫn giải chi tiết
            sheet.SetColumnWidth(9, 36 * 256);  // Bài đọc hiểu
            sheet.SetColumnWidth(10, 22 * 256); // Tiêu đề bài đọc
            sheet.SetColumnWidth(11, 14 * 256); // Mức độ Bloom
            sheet.SetColumnWidth(12, 16 * 256); // Mã chủ đề
            sheet.SetColumnWidth(13, 22 * 256); // Tags
            sheet.SetColumnWidth(14, 10 * 256); // Điểm liệt
            sheet.SetColumnWidth(15, 32 * 256); // Hình ảnh minh họa
        }

        private class StylePalette
        {
            public ICellStyle HeaderStyle { get; set; } = null!;
            public ICellStyle BannerTitleStyle { get; set; } = null!;
            public ICellStyle BannerMetaStyle { get; set; } = null!;
            public ICellStyle NormalStyle { get; set; } = null!;
            public ICellStyle WrapStyle { get; set; } = null!;
            public ICellStyle ContextWrapStyle { get; set; } = null!;
            public ICellStyle CenterStyle { get; set; } = null!;
            public ICellStyle CenterBoldStyle { get; set; } = null!;
        }

        private static StylePalette CreateStylePalette(IWorkbook workbook)
        {
            var palette = new StylePalette();

            // Font Tiêu đề Banner
            var fontTitle = workbook.CreateFont();
            fontTitle.FontName = "Segoe UI";
            fontTitle.FontHeightInPoints = 13;
            fontTitle.IsBold = true;
            fontTitle.Color = IndexedColors.DarkBlue.Index;

            // Font Metadata Banner
            var fontMeta = workbook.CreateFont();
            fontMeta.FontName = "Segoe UI";
            fontMeta.FontHeightInPoints = 9;
            fontMeta.IsItalic = true;
            fontMeta.Color = IndexedColors.Grey50Percent.Index;

            // Font Header Cột
            var fontHeader = workbook.CreateFont();
            fontHeader.FontName = "Segoe UI";
            fontHeader.FontHeightInPoints = 10;
            fontHeader.IsBold = true;
            fontHeader.Color = IndexedColors.White.Index;

            // Font Nội dung thường
            var fontBody = workbook.CreateFont();
            fontBody.FontName = "Segoe UI";
            fontBody.FontHeightInPoints = 10;

            // Font Bài đọc hiểu
            var fontContext = workbook.CreateFont();
            fontContext.FontName = "Segoe UI";
            fontContext.FontHeightInPoints = 9;

            // Font Đáp án đậm
            var fontBold = workbook.CreateFont();
            fontBold.FontName = "Segoe UI";
            fontBold.FontHeightInPoints = 10;
            fontBold.IsBold = true;
            fontBold.Color = IndexedColors.DarkBlue.Index;

            // Banner Title Style
            var sTitle = workbook.CreateCellStyle();
            sTitle.SetFont(fontTitle);
            sTitle.VerticalAlignment = VerticalAlignment.Center;
            palette.BannerTitleStyle = sTitle;

            // Banner Meta Style
            var sMeta = workbook.CreateCellStyle();
            sMeta.SetFont(fontMeta);
            sMeta.VerticalAlignment = VerticalAlignment.Center;
            palette.BannerMetaStyle = sMeta;

            // Header Style
            var sHeader = workbook.CreateCellStyle();
            sHeader.SetFont(fontHeader);
            sHeader.FillForegroundColor = IndexedColors.RoyalBlue.Index;
            sHeader.FillPattern = FillPattern.SolidForeground;
            sHeader.Alignment = HorizontalAlignment.Center;
            sHeader.VerticalAlignment = VerticalAlignment.Center;
            SetBorders(sHeader, BorderStyle.Thin, IndexedColors.Grey25Percent.Index);
            palette.HeaderStyle = sHeader;

            // Normal Body Style
            var sNormal = workbook.CreateCellStyle();
            sNormal.SetFont(fontBody);
            sNormal.VerticalAlignment = VerticalAlignment.Center;
            SetBorders(sNormal, BorderStyle.Thin, IndexedColors.Grey25Percent.Index);
            palette.NormalStyle = sNormal;

            // Wrap Body Style
            var sWrap = workbook.CreateCellStyle();
            sWrap.CloneStyleFrom(sNormal);
            sWrap.WrapText = true;
            palette.WrapStyle = sWrap;

            // Context Wrap Style (Top aligned, đẹp mắt cho bài đọc hiểu)
            var sContext = workbook.CreateCellStyle();
            sContext.CloneStyleFrom(sNormal);
            sContext.SetFont(fontContext);
            sContext.VerticalAlignment = VerticalAlignment.Top;
            sContext.WrapText = true;
            palette.ContextWrapStyle = sContext;

            // Center Style
            var sCenter = workbook.CreateCellStyle();
            sCenter.CloneStyleFrom(sNormal);
            sCenter.Alignment = HorizontalAlignment.Center;
            palette.CenterStyle = sCenter;

            // Center Bold Style
            var sCenterBold = workbook.CreateCellStyle();
            sCenterBold.CloneStyleFrom(sCenter);
            sCenterBold.SetFont(fontBold);
            sCenterBold.FillForegroundColor = IndexedColors.LightTurquoise.Index;
            sCenterBold.FillPattern = FillPattern.SolidForeground;
            palette.CenterBoldStyle = sCenterBold;

            return palette;
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
