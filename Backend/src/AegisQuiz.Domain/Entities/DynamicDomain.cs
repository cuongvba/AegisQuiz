using System;
using System.Collections.Generic;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// Thực thể Danh mục Ngành Động (Dynamic Domain Taxonomy).
    /// Cho phép mở rộng danh mục ngành từ chuẩn hệ thống (SystemStandard)
    /// đến các ngành/tiểu ngành riêng do Tenant tự định nghĩa.
    /// </summary>
    public class DynamicDomain
    {
        public string Code { get; set; } = string.Empty; // PK e.g., "BANKING", "HEALTHCARE", "AGRI_CREDIT"
        public string Name { get; set; } = string.Empty; // "Tài chính - Ngân hàng"
        public string? Description { get; set; }
        public string Icon { get; set; } = "AccountBalance"; // Material/Lucide Icon identifier
        public string ColorBadge { get; set; } = "#1976d2"; // Hex color or Tailwind color class
        
        /// <summary>
        /// True: Thuộc chuẩn hóa toàn cầu hệ thống; False: Do Tenant tự định nghĩa
        /// </summary>
        public bool IsSystemStandard { get; set; } = true;

        /// <summary>
        /// Tenant sở hữu (null nếu là chuẩn hệ sinh thái chung)
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// Mã ngành cha (dành cho các Sub-domain / Tiểu ngành chuyên sâu)
        /// </summary>
        public string? ParentDomainCode { get; set; }

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation for sub-domains
        public virtual DynamicDomain? ParentDomain { get; set; }
        public virtual ICollection<DynamicDomain> SubDomains { get; set; } = new List<DynamicDomain>();
    }
}
