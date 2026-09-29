using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [Cognitive Dual-Space Architecture]
    /// Lưu trữ tư cách thành viên của Người dùng trong từng Không gian làm việc (Tenant/Workspace).
    /// Một người dùng có thể là 'Learner' trong Community Commons, nhưng là 'Instructor' hoặc 'TenantAdmin' trong Trường học/Ngân hàng.
    /// </summary>
    public class TenantMembership
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Khóa ngoại tới tài khoản người dùng</summary>
        public Guid UserId { get; set; }
        public virtual UserAccount? User { get; set; }

        /// <summary>Khóa ngoại tới Tenant / Không gian làm việc</summary>
        public Guid TenantId { get; set; }
        public virtual Tenant? Tenant { get; set; }

        /// <summary>
        /// Vai trò cụ thể trong Không gian làm việc này (AppRoles: SystemAdmin, TenantAdmin, TeamLeader, Instructor, Learner...).
        /// </summary>
        public string RoleInTenant { get; set; } = AppRoles.Learner;

        /// <summary>Đơn vị phòng ban (OU) trực thuộc trong Tenant này (nếu là Enterprise Tenant)</summary>
        public Guid? OrgUnitId { get; set; }
        public virtual OrganizationUnit? OrgUnit { get; set; }

        /// <summary>Đánh dấu đây là workspace mặc định khi đăng nhập</summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>Trạng thái hoạt động trong workspace này</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Thời điểm gia nhập</summary>
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Thời điểm cập nhật quyền gần nhất</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
