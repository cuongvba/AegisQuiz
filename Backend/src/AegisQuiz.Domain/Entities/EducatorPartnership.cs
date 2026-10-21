using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [Creator Economy & Educator Partner Engine]
    /// Quản lý quan hệ đối tác chiến lược giáo viên: 90 ngày dùng thử toàn năng,
    /// mã giới thiệu affiliate, tỷ lệ chia sẻ doanh thu (80/20) và điểm Karma đóng góp.
    /// </summary>
    public class EducatorPartnership : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }

        /// <summary>Mã giới thiệu định danh cá nhân của giáo viên (ví dụ: THAYCUONG2026).</summary>
        public string ReferralCode { get; set; } = string.Empty;

        /// <summary>ID giáo viên đã giới thiệu (nếu có).</summary>
        public Guid? ReferredByUserId { get; set; }

        /// <summary>Thời điểm hết hạn 90 ngày dùng thử toàn năng (mặc định UtcNow + 90 ngày).</summary>
        public DateTime TrialExpiresAt { get; set; } = DateTime.UtcNow.AddDays(90);

        /// <summary>Tỷ lệ chia sẻ doanh thu bán đề / khóa học cho giáo viên (mặc định 80% = 0.80).</summary>
        public decimal RevenueShareRate { get; set; } = 0.80m;

        /// <summary>Điểm cống hiến tri thức Karma (1 đề chuẩn cộng đồng >= 20 lượt 5 sao = 1 tháng Pro miễn phí).</summary>
        public int KarmaPoints { get; set; } = 0;

        /// <summary>Số tài khoản ngân hàng nhận tiền quyết toán định kỳ.</summary>
        public string? PayoutBankAccount { get; set; }

        /// <summary>Tên ngân hàng (ví dụ: Vietcombank, Techcombank, MB, BIDV...).</summary>
        public string? PayoutBankName { get; set; }

        /// <summary>Tên chủ tài khoản ngân hàng.</summary>
        public string? PayoutAccountHolder { get; set; }

        /// <summary>Tổng doanh thu giáo viên đã tích lũy (VNĐ).</summary>
        public decimal TotalEarnings { get; set; } = 0m;

        /// <summary>Số dư khả dụng chờ quyết toán (VNĐ).</summary>
        public decimal PendingPayoutBalance { get; set; } = 0m;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
