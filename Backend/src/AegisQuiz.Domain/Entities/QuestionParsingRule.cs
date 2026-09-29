using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [Cognitive Ingestion Grammar] Thực thể lưu trữ quy tắc bóc tách câu hỏi động trong PostgreSQL.
    /// Hỗ trợ Multi-Tenancy (tùy biến quy tắc theo trường/tổ chức) và Hot-reload qua Redis.
    /// </summary>
    public class QuestionParsingRule : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }

        public string RuleCode { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; } = 100;
        public bool IsActive { get; set; } = true;
        public string Scope { get; set; } = "GLOBAL"; // "GLOBAL", "EDUCATION", "BANKING", "TENANT_CUSTOM"

        // JSON serialized 4-dimensional specifications
        public string AnchorSpecJson { get; set; } = "{}";
        public string InteractionSpecJson { get; set; } = "{}";
        public string EvaluationSpecJson { get; set; } = "{}";
        public string SemanticsSpecJson { get; set; } = "{}";

        public long HitCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
