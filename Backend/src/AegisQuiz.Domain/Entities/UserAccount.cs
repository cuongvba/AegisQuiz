using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [Enterprise Security] Quản lý tài khoản người dùng, băm mật khẩu bảo mật và thông tin thuê bao.
    /// </summary>
    public class UserAccount : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Email định danh (Unique, chữ thường, dùng để đăng nhập).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Mật khẩu đã băm bằng Salted PBKDF2/SHA-256 (NIST standard).</summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>Họ và tên đầy đủ của người dùng.</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Số điện thoại liên hệ (tùy chọn).</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>Ảnh đại diện (URL).</summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Vai trò chính (SystemAdmin, TenantAdmin, TeamLeader, Instructor, ContentManager, Learner).
        /// Tham chiếu hằng số từ AppRoles.
        /// </summary>
        public string Role { get; set; } = AppRoles.Learner;

        /// <summary>Tenant / Tổ chức người dùng trực thuộc.</summary>
        public Guid TenantId { get; set; }

        /// <summary>Đơn vị phòng ban / Squad người dùng thuộc về (đặc biệt quan trọng với TeamLeader / Learner).</summary>
        public Guid? OrgUnitId { get; set; }

        /// <summary>Cờ đánh dấu tài khoản VIP / Doanh nghiệp có trả phí.</summary>
        public bool IsPremium { get; set; } = false;

        /// <summary>Gói thuê bao hiện tại: "FREE", "VIP", "ENTERPRISE".</summary>
        public string SubscriptionTier { get; set; } = "FREE";

        /// <summary>Thời điểm hết hạn gói thuê bao (null = vĩnh viễn hoặc gói Free).</summary>
        public DateTime? SubscriptionExpiresAt { get; set; }

        /// <summary>Trạng thái tài khoản (true = hoạt động bình thường, false = bị vô hiệu hóa).</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Đếm số lần đăng nhập sai mật khẩu liên tiếp (chống Brute-force).</summary>
        public int FailedLoginAttempts { get; set; } = 0;

        /// <summary>Thời điểm mở khóa tài khoản nếu bị khóa tạm thời do nhập sai quá 5 lần.</summary>
        public DateTime? LockoutEnd { get; set; }

        /// <summary>Thời điểm đăng nhập gần nhất.</summary>
        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual OrganizationUnit? OrgUnit { get; set; }
    }
}
