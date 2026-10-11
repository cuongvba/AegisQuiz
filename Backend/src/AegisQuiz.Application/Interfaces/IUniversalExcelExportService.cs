using System.Collections.Generic;
using System.IO;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// Tùy chọn xuất bản Excel linh động phục vụ mọi yêu cầu khách hàng.
    /// </summary>
    public class ExcelExportOptionsDto
    {
        public string? ExamTitle { get; set; }
        public string? IssuingOrg { get; set; }
        public int? BenchmarkYear { get; set; }
        public string? BenchmarkStandard { get; set; }
        public string? TargetLevel { get; set; }
        public string? AssessmentPurpose { get; set; }
        public string? DomainCode { get; set; }
        public string? DefaultTopicCode { get; set; }
        public string MathFormat { get; set; } = "latex"; // "latex", "unicode", "image"
        public bool IncludeExplanation { get; set; } = true;
        public bool IncludeImages { get; set; } = true;
        public byte[]? CustomTemplateBytes { get; set; }
    }

    /// <summary>
    /// Thông tin mô tả một mẫu xuất Excel sẵn có trong hệ thống.
    /// </summary>
    public class ExcelExportTemplateInfoDto
    {
        public string TemplateCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool SupportsMultiSheet { get; set; }
        public bool IsNativeIsomorphic { get; set; }
    }

    /// <summary>
    /// Chiến lược xuất bản câu hỏi sang bảng tính Excel (Open/Closed Principle).
    /// </summary>
    public interface IExcelExportStrategy
    {
        string TemplateCode { get; }
        string DisplayName { get; }
        string Description { get; }
        bool SupportsMultiSheet { get; }
        bool IsNativeIsomorphic { get; }

        byte[] Export(IReadOnlyList<DocxImportPreviewDto> questions, ExcelExportOptionsDto options);
    }

    /// <summary>
    /// [Universal Excel Export Engine — DTaaS]
    /// Dịch vụ chuyển đổi và xuất bản danh sách câu hỏi trung gian sang bảng tính Excel đa định dạng.
    /// </summary>
    public interface IUniversalExcelExportService
    {
        /// <summary>
        /// Xuất danh sách câu hỏi sang bảng tính Excel dạng byte array theo mẫu chỉ định.
        /// </summary>
        byte[] Export(IReadOnlyList<DocxImportPreviewDto> questions, string templateCode = "NATIVE_AEGIS", ExcelExportOptionsDto? options = null);

        /// <summary>
        /// Lấy danh sách tất cả các mẫu xuất bản đang được hệ thống hỗ trợ.
        /// </summary>
        IReadOnlyList<ExcelExportTemplateInfoDto> GetAvailableTemplates();
    }
}
