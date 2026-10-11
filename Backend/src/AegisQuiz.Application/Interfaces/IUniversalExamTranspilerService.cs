using System.Collections.Generic;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// Các tùy chọn xuất bản và chuyển dịch đề thi đa nền tảng (Universal Transpiler Options).
    /// </summary>
    public class TranspileOptionsDto
    {
        public string? ExamTitle { get; set; }
        public string? CategoryName { get; set; } = "AegisQuiz Imported Questions";
        public string? CourseCode { get; set; }
        public string? DefaultPoints { get; set; } = "1.0";
        public string MathFormat { get; set; } = "latex"; // "latex", "mathml", "unicode"
        public bool IncludeExplanation { get; set; } = true;
        public bool IncludeImages { get; set; } = true;
        public bool ShuffleAnswers { get; set; } = true;
    }

    /// <summary>
    /// Thông tin mô tả một định dạng xuất bản chuẩn công nghiệp được hệ thống hỗ trợ.
    /// </summary>
    public class TranspileFormatInfoDto
    {
        public string FormatCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TargetPlatforms { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public bool SupportsSelfContainedMedia { get; set; }
        public bool IsPackageZip { get; set; }
    }

    /// <summary>
    /// Chiến lược xuất bản câu hỏi sang định dạng chuẩn công nghiệp (Open/Closed Principle).
    /// </summary>
    public interface ITranspilerExportStrategy
    {
        string FormatCode { get; }
        string DisplayName { get; }
        string Description { get; }
        string TargetPlatforms { get; }
        string MimeType { get; }
        string FileExtension { get; }
        bool SupportsSelfContainedMedia { get; }
        bool IsPackageZip { get; }

        byte[] Transpile(IReadOnlyList<DocxImportPreviewDto> questions, TranspileOptionsDto options);
    }

    /// <summary>
    /// [Universal Exam Transpiler Engine — DTaaS]
    /// Dịch vụ điều phối chuyển dịch ngân hàng câu hỏi sang các định dạng chuẩn quốc tế (Moodle, Canvas QTI, GIFT, SCORM...).
    /// </summary>
    public interface IUniversalExamTranspilerService
    {
        /// <summary>
        /// Chuyển dịch danh sách câu hỏi sang định dạng đích dưới dạng mảng byte.
        /// </summary>
        byte[] Transpile(IReadOnlyList<DocxImportPreviewDto> questions, string formatCode, TranspileOptionsDto? options = null);

        /// <summary>
        /// Lấy danh sách tất cả các định dạng xuất bản chuẩn quốc tế đang sẵn sàng trong hệ thống.
        /// </summary>
        IReadOnlyList<TranspileFormatInfoDto> GetAvailableFormats();
    }
}
