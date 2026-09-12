using System;
using System.Collections.Generic;

namespace AegisQuiz.Domain.Entities
{
    public enum OrgUnitType
    {
        HeadOffice  = 1, // Hội sở chính / Ban Lãnh đạo
        Region      = 2, // Văn phòng đại diện / Khu vực / Khối
        Branch      = 3, // Chi nhánh / Phân hiệu / Trường thành viên
        Department  = 4, // Phòng ban / Khoa / Bộ môn
        Classroom   = 5  // Lớp học / Tổ nhóm nghiệp vụ
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
