using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// Cấu hình kích hoạt và tùy biến hiển thị ngành cho từng Tenant cụ thể.
    /// Cho phép Tenant bật/tắt ngành và đổi nhãn hiển thị theo từ ngữ nội bộ.
    /// </summary>
    public class TenantDomainConfig : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }

        public string DomainCode { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Tên hiển thị tùy biến riêng tại Tenant này (nếu khác với Name mặc định của DynamicDomain)
        /// </summary>
        public string? CustomDisplayName { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual DynamicDomain? Domain { get; set; }
    }
}
