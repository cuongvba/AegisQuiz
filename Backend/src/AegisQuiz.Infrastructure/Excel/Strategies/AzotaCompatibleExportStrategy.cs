using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Excel.Strategies
{
    /// <summary>
    /// [Azota & LMS Generic Compatible Export Strategy]
    /// Chiến lược xuất bản tương thích rộng với các nền tảng thi trắc nghiệm phổ biến (Azota, Subiz, 789, OLM, K12Online).
    /// </summary>
    public class AzotaCompatibleExportStrategy : IExcelExportStrategy
    {
        public string TemplateCode => "AZOTA";
        public string DisplayName => "Azota / LMS Việt Nam (Tối Giản Phổ Thông)";
        public string Description => "Mẫu bảng tính tương thích trực tiếp các hệ thống thi trắc nghiệm Azota, Subiz, OLM, K12Online.";
        public bool SupportsMultiSheet => false;
        public bool IsNativeIsomorphic => false;

        public byte[] Export(IReadOnlyList<DocxImportPreviewDto> questions, ExcelExportOptionsDto options)
        {
            using var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("DeThiAzota");
            sheet.DisplayGridlines = true;

            var fHeader = workbook.CreateFont();
            fHeader.FontName = "Arial";
            fHeader.FontHeightInPoints = 10;
            fHeader.IsBold = true;
            fHeader.Color = IndexedColors.White.Index;

            var sHeader = workbook.CreateCellStyle();
            sHeader.SetFont(fHeader);
            sHeader.FillForegroundColor = IndexedColors.Orange.Index;
            sHeader.FillPattern = FillPattern.SolidForeground;
            sHeader.Alignment = HorizontalAlignment.Center;
            sHeader.VerticalAlignment = VerticalAlignment.Center;

            var sNormal = workbook.CreateCellStyle();
            sNormal.WrapText = true;
            sNormal.VerticalAlignment = VerticalAlignment.Center;

            var sCenter = workbook.CreateCellStyle();
            sCenter.Alignment = HorizontalAlignment.Center;
            sCenter.VerticalAlignment = VerticalAlignment.Center;

            var r0 = sheet.CreateRow(0);
            r0.HeightInPoints = 26;
            string[] headers = { "STT", "Câu hỏi", "Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D", "Đáp án đúng", "Giải thích" };
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
                row.CreateCell(0).SetCellValue(!string.IsNullOrWhiteSpace(q.SourceQuestionNo) ? q.SourceQuestionNo : (i + 1).ToString());
                row.CreateCell(1).SetCellValue(ExcelCellSanitizer.Sanitize(q.Content));
                row.CreateCell(2).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 0)));
                row.CreateCell(3).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 1)));
                row.CreateCell(4).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 2)));
                row.CreateCell(5).SetCellValue(ExcelCellSanitizer.Sanitize(GetOption(q, 3)));
                row.CreateCell(6).SetCellValue(FormatAns(q.SuggestedAnswer));
                row.CreateCell(7).SetCellValue(options.IncludeExplanation ? ExcelCellSanitizer.Sanitize(q.AiExplanation) : "");

                row.GetCell(0).CellStyle = sCenter;
                row.GetCell(1).CellStyle = sNormal;
                row.GetCell(2).CellStyle = sNormal;
                row.GetCell(3).CellStyle = sNormal;
                row.GetCell(4).CellStyle = sNormal;
                row.GetCell(5).CellStyle = sNormal;
                row.GetCell(6).CellStyle = sCenter;
                row.GetCell(7).CellStyle = sNormal;
            }

            sheet.SetColumnWidth(0, 8 * 256);
            sheet.SetColumnWidth(1, 45 * 256);
            sheet.SetColumnWidth(2, 22 * 256);
            sheet.SetColumnWidth(3, 22 * 256);
            sheet.SetColumnWidth(4, 22 * 256);
            sheet.SetColumnWidth(5, 22 * 256);
            sheet.SetColumnWidth(6, 14 * 256);
            sheet.SetColumnWidth(7, 35 * 256);

            using var ms = new MemoryStream();
            workbook.Write(ms);
            return ms.ToArray();
        }

        private static string GetOption(DocxImportPreviewDto q, int idx)
        {
            if (q.Options == null || idx >= q.Options.Count) return "";
            return Regex.Replace(q.Options[idx], @"^(?:\[?[A-Ha-h0-9]\]?[\.\)\:]|\([A-Ha-h0-9]\))\s*", "").Trim();
        }

        private static string FormatAns(string? a)
        {
            if (string.IsNullOrWhiteSpace(a)) return "";
            if (a == "1") return "A";
            if (a == "2") return "B";
            if (a == "3") return "C";
            if (a == "4") return "D";
            return a.ToUpperInvariant();
        }
    }
}
