using System;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Aegis UPMP v2.0] Chuẩn hóa ký hiệu toán học đa ngôn ngữ & đơn vị đo lường khoa học.
    /// Tuân thủ Clean Architecture (Application interface) và Open/Closed Principle (OCP).
    /// </summary>
    public interface IMathLocaleNormalizer
    {
        /// <summary>
        /// Chuẩn hóa toàn diện chuỗi LaTeX KaTeX theo ngôn ngữ/văn hóa toán học
        /// (Đơn vị đo lường, hàm lượng giác địa phương, dấu phẩy thập phân, hình học SGK).
        /// </summary>
        string Normalize(string latex, string locale = "vi");

        /// <summary>
        /// Bọc các đơn vị đo lường khoa học đứng sau số hạng vào \text{...}
        /// để tránh KaTeX hiển thị dạng in nghiêng biến số toán học.
        /// </summary>
        string NormalizeScientificUnits(string latex, string locale = "vi");

        /// <summary>
        /// Chuẩn hóa hàm lượng giác đa ngôn ngữ (tg -> \tan, cotg -> \cot, sh -> \sinh...)
        /// </summary>
        string NormalizeTrigonometryAndFunctions(string latex, string locale = "vi");

        /// <summary>
        /// Chuẩn hóa dấu phẩy số thập phân trong tiếng Việt / Pháp (ví dụ: 3,14 -> 3{,}14)
        /// để KaTeX không tạo khoảng trắng rộng sai quy cách như dấu phẩy danh sách.
        /// </summary>
        string NormalizeDecimalCommas(string latex, string locale = "vi");
    }
}
