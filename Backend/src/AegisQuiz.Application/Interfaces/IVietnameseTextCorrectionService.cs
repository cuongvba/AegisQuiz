using System.Collections.Generic;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisQuiz Universal Text Engine] Dịch vụ sửa lỗi chính tả, chuẩn hóa văn bản tiếng Việt
    /// và chuẩn hóa logic tham chiếu phương án từ kho tri thức maprepl.db.
    /// </summary>
    public interface IVietnameseTextCorrectionService
    {
        /// <summary>
        /// Làm sạch và sửa lỗi chuỗi ký tự tiếng Việt theo 670+ quy tắc thực chiến.
        /// </summary>
        string CorrectText(string input);

        /// <summary>
        /// Chuẩn hóa toàn bộ câu hỏi (thân câu, các phương án, lời giải thích).
        /// </summary>
        DocxImportPreviewDto CorrectQuestionDto(DocxImportPreviewDto dto);

        /// <summary>
        /// Chuẩn hóa hàng loạt câu hỏi trong luồng bóc tách DOCX/PDF/Excel.
        /// </summary>
        List<DocxImportPreviewDto> CorrectQuestionsBatch(List<DocxImportPreviewDto> dtos);

        /// <summary>
        /// Kiểm tra xem phương án có chứa tham chiếu vị trí/tổng hợp hay không
        /// (ví dụ: "Cả A và B", "Tất cả các phương án trên", "Cả (1) và (2)", "Không có phương án nào đúng").
        /// </summary>
        bool ContainsPositionalReference(string optionText);

        /// <summary>
        /// Kiểm tra xem phương án có phải là phương án neo (Anchor Option)
        /// cần được cố định ở vị trí cuối cùng khi đảo đề hay không.
        /// </summary>
        bool IsAnchorOption(string optionText);
    }
}
