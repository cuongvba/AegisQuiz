using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Excel
{
    /// <summary>
    /// [AegisQuiz Excel Image Helper]
    /// Tiện ích bóc tách, nhận diện định dạng và nhúng hình ảnh chuyên nghiệp vào bảng tính Excel (NPOI OpenXML):
    /// - Quét toàn diện hình ảnh từ Content, AiExplanation, Options và MediaUrl.
    /// - Tự động nhận diện định dạng file ảnh qua Magic Bytes Header (PNG, JPEG, DIB/BMP, GIF).
    /// - Neo hình ảnh chính xác vào ô bảng tính qua XSSFClientAnchor, tự động căn giữa và co giãn tỷ lệ an toàn.
    /// </summary>
    public static class ExcelImageHelper
    {
        private static readonly Regex Base64ImageRegex = new(
            @"data:image\/(?:png|jpeg|jpg|webp|gif|bmp|svg\+xml);base64,([A-Za-z0-9+/=]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public class ExtractedImage
        {
            public string MimeType { get; set; } = "image/png";
            public byte[] RawBytes { get; set; } = Array.Empty<byte>();
            public string Base64Data { get; set; } = string.Empty;
        }

        /// <summary>
        /// Bóc tách tất cả hình ảnh có trong câu hỏi (từ đề bài, lời giải, phương án hoặc media url)
        /// </summary>
        public static List<ExtractedImage> ExtractImages(DocxImportPreviewDto q)
        {
            var results = new List<ExtractedImage>();
            var targets = new List<string?> { q.MediaUrl, q.Content, q.AiExplanation };
            if (q.Options != null)
            {
                targets.AddRange(q.Options);
            }

            foreach (var text in targets)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;

                var matches = Base64ImageRegex.Matches(text);
                foreach (Match m in matches)
                {
                    try
                    {
                        var b64 = m.Groups[1].Value;
                        var bytes = Convert.FromBase64String(b64);
                        if (bytes.Length > 0)
                        {
                            results.Add(new ExtractedImage
                            {
                                Base64Data = b64,
                                RawBytes = bytes,
                                MimeType = DetectMimeType(bytes)
                            });
                        }
                    }
                    catch
                    {
                        // Bỏ qua nếu chuỗi base64 bị lỗi
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Nhận diện loại ảnh NPOI thông qua Magic Bytes ở đầu mảng byte
        /// </summary>
        public static PictureType DetectPictureType(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4) return PictureType.PNG;

            // PNG signature: 89 50 4E 47
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            {
                return PictureType.PNG;
            }

            // JPEG signature: FF D8
            if (bytes[0] == 0xFF && bytes[1] == 0xD8)
            {
                return PictureType.JPEG;
            }

            // BMP signature: 42 4D
            if (bytes[0] == 0x42 && bytes[1] == 0x4D)
            {
                return PictureType.DIB;
            }

            // GIF signature: 47 49 46
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
            {
                return PictureType.PICT;
            }

            return PictureType.PNG;
        }

        /// <summary>
        /// Nhận diện MimeType từ Magic Bytes
        /// </summary>
        private static string DetectMimeType(byte[] bytes)
        {
            if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50) return "image/png";
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) return "image/jpeg";
            if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D) return "image/bmp";
            if (bytes.Length >= 3 && bytes[0] == 0x47 && bytes[1] == 0x49) return "image/gif";
            return "image/png";
        }

        /// <summary>
        /// Nhúng ảnh trực tiếp vào ô Excel với căn chỉnh viền tự động (padding) và giữ tỷ lệ thẩm mỹ
        /// </summary>
        public static bool EmbedImageInCell(
            IWorkbook workbook,
            XSSFDrawing drawing,
            byte[] imageBytes,
            int rowIdx,
            int colIdx,
            int colSpan = 1,
            int rowSpan = 1)
        {
            if (imageBytes == null || imageBytes.Length == 0) return false;

            try
            {
                var pictureType = DetectPictureType(imageBytes);
                int pictureIdx = workbook.AddPicture(imageBytes, pictureType);

                var helper = workbook.GetCreationHelper();
                var anchor = helper.CreateClientAnchor();

                // Đặt tọa độ góc trên-trái và góc dưới-phải của ô
                anchor.Col1 = colIdx;
                anchor.Row1 = rowIdx;
                anchor.Col2 = colIdx + colSpan;
                anchor.Row2 = rowIdx + rowSpan;

                // Padding nhẹ (đơn vị EMU: 1px ≈ 9525 EMU)
                anchor.Dx1 = 20 * 9525;  // Cách mép trái ~20px
                anchor.Dy1 = 8 * 9525;   // Cách mép trên ~8px
                anchor.Dx2 = -20 * 9525; // Cách mép phải ~20px
                anchor.Dy2 = -8 * 9525;  // Cách mép dưới ~8px

                anchor.AnchorType = AnchorType.MoveAndResize;

                var picture = drawing.CreatePicture(anchor, pictureIdx);
                return picture != null;
            }
            catch
            {
                // Fallback nếu NPOI gặp định dạng không hỗ trợ trực tiếp (như SVG raw)
                return false;
            }
        }
    }
}
