using System;
using System.Collections.Generic;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// Chủ đề ngân hàng câu hỏi — học từ quiz-service BankTopic.
    /// Hỗ trợ phân quyền hiển thị đa cấp.
    /// </summary>
    public static class TopicVisibility
    {
        public const string Private    = "PRIVATE";     // Chỉ admin sở hữu thấy
        public const string Public     = "PUBLIC";      // Tất cả thấy
        public const string TenantGroup = "TENANT_GROUP"; // Chia sẻ với danh sách chỉ định
    }

    public class BankTopic : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CategoryCode { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;

        /// <summary>Mức hiển thị: PRIVATE | PUBLIC | TENANT_GROUP.</summary>
        public string VisibilityScope { get; set; } = TopicVisibility.Private;

        /// <summary>Danh sách userId/orgId được phép xem (dùng cho TENANT_GROUP).</summary>
        public List<string> AllowedTenantIds { get; set; } = new();

        // [Phase B — Multi-Tenancy] Chủ đề thuộc về Tenant nào.
        // Guid.Empty = chủ đề dùng chung (system-wide)
        public Guid TenantId { get; set; } = Guid.Empty;

        /// <summary>Phạm vi phân quyền tri thức: "COMMUNITY" (Kho mở công cộng) | "TENANT" (Nội bộ doanh nghiệp)</summary>
        public string Scope { get; set; } = "COMMUNITY";

        /// <summary>Mã lĩnh vực nhận thức (VD: BANKING, HEALTHCARE, IT, LEGAL, EDUCATION, GPLX)</summary>
        public string DomainCode { get; set; } = "GENERAL";

        /// <summary>Đường dẫn phân cấp phả hệ cụ thể hóa (Materialized Path, VD: "/BANKING/2026_DOT2/1_TIN_DUNG/")</summary>
        public string? MaterializedPath { get; set; }

        /// <summary>Độ sâu trong cây phả hệ (0: Root/Domain, 1: Parent Folder, 2: Child Topic, 3: Micro-concept)</summary>
        public int DepthLevel { get; set; } = 0;

        /// <summary>Thứ tự sắp xếp hiển thị</summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>Bộ đệm số lượng câu hỏi thuộc chủ đề (tối ưu hóa tốc độ tải hàng vạn chủ đề)</summary>
        public int QuestionCountCached { get; set; } = 0;

        /// <summary>Định danh tri thức toàn cầu vĩnh cửu (URN)</summary>
        public string? Urn { get; set; }

        public Guid? ParentId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public BankTopic? Parent { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<BankTopic> Children { get; set; } = new List<BankTopic>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
    }
}
