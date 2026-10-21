using System.Threading;
using System.Threading.Tasks;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Aegis Vision Math Cognitive Service - World-Class Standard]
    /// Dịch vụ AI Vision Math & Multimodal Math OCR chuyên biệt:
    /// Đọc trực tiếp các hình ảnh công thức toán học vector/raster (WMF, SVG, PNG)
    /// có cấu trúc cực kỳ phức tạp (phân số đa tầng, ma trận, hệ phương trình, tích phân có cận)
    /// và chuyển đổi thành chuỗi KaTeX/LaTeX ($...$) thuần túy có thể chỉnh sửa tự do.
    /// </summary>
    public interface IVisionMathService
    {
        /// <summary>
        /// Chuyển đổi tệp ảnh công thức toán học hoặc WMF sang chuỗi KaTeX/LaTeX.
        /// </summary>
        /// <param name="imageBytes">Dữ liệu nhị phân của tệp ảnh/WMF</param>
        /// <param name="mimeType">Loại MIME (ví dụ: image/png, image/wmf, image/svg+xml)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Chuỗi KaTeX ($...$) hoặc null nếu không thể giải mã</returns>
        Task<string?> ConvertImageToLatexAsync(
            byte[] imageBytes, 
            string mimeType = "image/png", 
            CancellationToken cancellationToken = default);
    }
}
