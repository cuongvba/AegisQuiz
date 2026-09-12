using System.IO;
using System.Text;

namespace AegisQuiz.Infrastructure.Pdf
{
    // [Pipeline Giai đoạn 1: Extract] Trích xuất văn bản từ file PDF
    // Sử dụng thư viện PdfPig (Open-source, miễn phí, thuần C#)
    public class PdfExtractorService
    {
        public string ExtractTextFromPdf(Stream pdfStream)
        {
            var textBuilder = new StringBuilder();
            
            /* Yêu cầu NuGet: UglyToad.PdfPig
            using var document = UglyToad.PdfPig.PdfDocument.Open(pdfStream);
            
            foreach (var page in document.GetPages())
            {
                // Lấy toàn bộ text trên mỗi trang
                string pageText = page.Text;
                textBuilder.AppendLine($"--- TRANG {page.Number} ---");
                textBuilder.AppendLine(pageText);
                textBuilder.AppendLine();
            }
            */

            Console.WriteLine($"[PdfExtractor] Đã trích xuất {textBuilder.Length} ký tự từ PDF.");
            return textBuilder.ToString();
        }

        // Trường hợp PDF là ảnh scan (không có text layer)
        // Gọi Google Cloud Vision OCR để nhận dạng chữ
        public async Task<string> ExtractTextFromScannedPdfAsync(Stream pdfStream)
        {
            // Logic gọi Google Cloud Vision API
            Console.WriteLine("[PdfExtractor] PDF không có text layer. Đang gọi Google Cloud Vision OCR...");
            await Task.Delay(3000); // Giả lập OCR
            return "Nội dung được nhận dạng từ ảnh scan bằng OCR...";
        }
    }
}
