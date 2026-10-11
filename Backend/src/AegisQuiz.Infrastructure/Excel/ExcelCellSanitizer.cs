using System;
using System.Text.RegularExpressions;

namespace AegisQuiz.Infrastructure.Excel
{
    /// <summary>
    /// [AegisQuiz Excel Cell Sanitizer]
    /// Bộ làm sạch và bảo vệ an toàn cho ô dữ liệu Excel OpenXML (NPOI):
    /// 1. Thay thế chuỗi hình ảnh Base64 siêu dài (hàng trăm nghìn ký tự) thành nhãn văn bản súc tích.
    /// 2. Cắt ngắn an toàn ở ngưỡng 32,000 ký tự (trần giới hạn kỹ thuật của Excel là 32,767 ký tự),
    ///    triệt tiêu hoàn toàn ngoại lệ System.ArgumentException: The maximum length of cell contents is 32,767 characters.
    /// </summary>
    public static class ExcelCellSanitizer
    {
        public const int MaxExcelCellChars = 32000;

        public static string Sanitize(string? text, bool replaceBase64 = true)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string clean = text;

            if (replaceBase64 && clean.Contains("data:image/"))
            {
                // Thay thế thẻ <img src="data:image/..."/> thành nhãn gọn gàng
                clean = Regex.Replace(clean, @"<img\s+[^>]*src=[""']data:image\/[^""']+[""'][^>]*>", "[Hình ảnh minh họa]", RegexOptions.IgnoreCase);
                // Thay thế chuỗi raw data:image/... còn sót lại
                clean = Regex.Replace(clean, @"data:image\/(?:png|jpeg|jpg|gif|bmp);base64,[A-Za-z0-9+/=]+", "[Hình ảnh Base64]", RegexOptions.IgnoreCase);
            }

            if (clean.Length > MaxExcelCellChars)
            {
                clean = clean.Substring(0, MaxExcelCellChars) + "... [Đã rút gọn văn bản do vượt trần Excel 32K ký tự]";
            }

            return clean;
        }
    }
}
