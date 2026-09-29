using System;
using System.Collections.Generic;

namespace AegisQuiz.Domain.Entities
{
    public enum OrgUnitType
    {
        Holding     = 0, // Tập đoàn mẹ / Holding (Sovereign Mode)
        HeadOffice  = 1, // Hội sở chính / Ban Lãnh đạo
        Region      = 2, // Văn phòng đại diện / Khu vực / Khối
        Branch      = 3, // Chi nhánh / Phân hiệu / Trường thành viên
        Department  = 4, // Phòng ban / Khoa / Bộ môn
        Classroom   = 5, // Lớp học / Tổ nhóm nghiệp vụ
        Sector      = 6, // Ngành dọc / Lĩnh vực
        Division    = 7, // Bộ phận / Phân ban
        Section     = 8, // Tổ / Mảng nghiệp vụ
        Squad       = 9, // Nhóm làm việc nhỏ (Express / Team Mode)
        Custom      = 99 // Nhãn tùy biến theo Dynamic Taxonomy Preset của Tenant
    }

    /// <summary>
    /// Thực thể Quản trị Cây Tổ chức Đệ quy Mềm (Hierarchical Organization Unit).
    /// Phục vụ phân tầng quản trị đa cấp 5 tầng (HQ -> Region -> Branch -> Dept -> User)
    /// </summary>
    public class OrganizationUnit : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }

        public Guid? ParentId { get; set; }
        public string Code { get; set; } = string.Empty; // e.g., "HO", "MB_HN", "CN_HOANKIEM"
        public string Name { get; set; } = string.Empty; // e.g., "Hội sở chính", "Chi nhánh Hoàn Kiếm"
        public OrgUnitType UnitType { get; set; } = OrgUnitType.Branch;

        /// <summary>
        /// Đường dẫn phả hệ để tối ưu hóa truy vấn dạng cây (e.g. "/ROOT/MB_HN/CN_HOANKIEM")
        /// </summary>
        public string HierarchyPath { get; set; } = "/";

        /// <summary>Độ sâu trong cây phân cấp (1 = gốc HeadOffice/Trường, 5 = Classroom/Squad).</summary>
        public int DepthLevel { get; set; } = 1;

        /// <summary>Nhãn phân loại động theo Tenant (ví dụ "Khối", "Chi nhánh", "Bộ môn") — phục vụ Dynamic Taxonomy Presets.</summary>
        public string? TaxonomyCategory { get; set; }

        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual OrganizationUnit? Parent { get; set; }
        public virtual ICollection<OrganizationUnit> Children { get; set; } = new List<OrganizationUnit>();
    }
}
