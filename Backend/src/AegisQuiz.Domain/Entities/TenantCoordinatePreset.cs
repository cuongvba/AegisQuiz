using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// Bộ tọa độ tri thức đặc thù của Tenant (TARGET_LEVEL, ISSUING_ORG, BENCHMARK_STANDARD, ASSESSMENT_PURPOSE).
    /// Hỗ trợ từ đồng nghĩa (SynonymsJson) giúp AI Cognitive Banner Sniffer nhận diện tự động từ khóa của từng đơn vị.
    /// </summary>
    public class TenantCoordinatePreset : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }

        public string DomainCode { get; set; } = string.Empty; // e.g. "BANKING", "IT_SECURITY"
        
        /// <summary>
        /// Loại tọa độ: "TARGET_LEVEL", "ISSUING_ORG", "BENCHMARK_STANDARD", "ASSESSMENT_PURPOSE"
        /// </summary>
        public string CoordinateType { get; set; } = string.Empty;

        public string PresetCode { get; set; } = string.Empty; // e.g., "KHDN", "GDV", "ISO_27001"
        public string PresetLabel { get; set; } = string.Empty; // e.g., "Tín dụng Khách hàng Doanh nghiệp"

        /// <summary>
        /// Danh sách từ đồng nghĩa dạng JSON array: ["Tín dụng doanh nghiệp", "KHDN", "Corporate Credit"]
        /// </summary>
        public string? SynonymsJson { get; set; }

        public bool IsDefault { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
