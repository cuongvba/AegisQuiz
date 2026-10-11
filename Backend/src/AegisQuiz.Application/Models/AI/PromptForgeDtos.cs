using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.Models.AI
{
    public enum DialecticalRigorLevel
    {
        StrictPopperian = 1,       // Khắt khe phản nghiệm Karl Popper, đào sâu phản chứng
        PragmaticEmpirical = 2,    // Thực chứng thực dụng, ưu tiên dữ liệu quan trắc và ứng dụng
        ConstructiveDialectical = 3,// Biện chứng xây dựng, tìm kiếm điểm cân bằng luận điểm - phản đề
        SystemicHolistic = 4       // Tư duy hệ thống tổng thể, xem xét mối tương quan phi tuyến tính
    }

    public class PromptProposalRequest
    {
        public string OrganizationDomain { get; set; } = string.Empty; // VD: "Y Khoa Lâm Sàng", "Fintech Ngân Hàng", "Hàng Không Vũ Trụ"
        public string CoreAxioms { get; set; } = string.Empty;         // Tôn chỉ hoặc tiêu chuẩn cần tuân thủ
        public DialecticalRigorLevel RigorLevel { get; set; } = DialecticalRigorLevel.StrictPopperian;
        public string Locale { get; set; } = "vi-VN";
        public Guid? TenantId { get; set; }
    }

    public class MetaPromptProposalDto
    {
        public string TemplateCode { get; set; } = "ARCHITECT_META";
        public string Body { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public List<string> ExtractedAxioms { get; set; } = new();
        public int EstimatedTokens { get; set; }
        public int Version { get; set; } = 1;
    }

    public class GenerateStackRequest
    {
        public string MetaPromptBody { get; set; } = string.Empty;
        public string DomainFocus { get; set; } = string.Empty;
        public string Locale { get; set; } = "vi-VN";
        public Guid? TenantId { get; set; }
    }

    public class CouncilCharterProposalDto
    {
        public string CouncilCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DisciplineArea { get; set; } = string.Empty;
        public string TaxonomyPath { get; set; } = string.Empty;
        public string CharterPromptBody { get; set; } = string.Empty;
    }

    public class SeatRoleProposalDto
    {
        public string SeatTitle { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PreferredModel { get; set; } = string.Empty;
        public string DirectivePromptBody { get; set; } = string.Empty;
    }

    public class CascadingPromptStackDto
    {
        public string MetaPromptLayer0 { get; set; } = string.Empty;
        public string ConstitutionLayer1 { get; set; } = string.Empty;
        public List<CouncilCharterProposalDto> CouncilChartersLayer2 { get; set; } = new();
        public List<SeatRoleProposalDto> SeatRolesLayer3 { get; set; } = new();
        public string PhaseDirectivesLayer4 { get; set; } = string.Empty;
        public string OutputContractLayer5 { get; set; } = string.Empty;
        public int TotalEstimatedTokens { get; set; }
    }

    public class AxiomaticAuditReportDto
    {
        public bool IsFullyConsistent { get; set; }
        public double ConsistencyScore { get; set; } = 1.0; // 0.0 -> 1.0
        public List<AxiomaticInconsistencyDto> Inconsistencies { get; set; } = new();
        public List<string> AuditChecklist { get; set; } = new();
    }

    public class AxiomaticInconsistencyDto
    {
        public string LayerName { get; set; } = string.Empty;
        public string TargetKey { get; set; } = string.Empty;
        public string ConflictSummary { get; set; } = string.Empty;
        public string Severity { get; set; } = "WARNING"; // WARNING, CRITICAL
        public string RemediationSuggestion { get; set; } = string.Empty;
    }

    public class EnactPromptStackRequest
    {
        public string MetaPromptLayer0 { get; set; } = string.Empty;
        public string ConstitutionLayer1 { get; set; } = string.Empty;
        public List<CouncilCharterProposalDto> Charters { get; set; } = new();
        public string OutputContractLayer5 { get; set; } = string.Empty;
        public string Locale { get; set; } = "vi-VN";
        public Guid? TenantId { get; set; }
        public string ChangeSummary { get; set; } = "Cập nhật qua Cognitive Prompt Forge Studio";
    }

    public class PromptTemplateSummaryDto
    {
        public Guid Id { get; set; }
        public string TemplateCode { get; set; } = string.Empty;
        public int Layer { get; set; }
        public string LayerName { get; set; } = string.Empty;
        public string TargetKey { get; set; } = "*";
        public int Version { get; set; }
        public string Locale { get; set; } = "vi-VN";
        public string Body { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
