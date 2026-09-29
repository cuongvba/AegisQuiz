using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [Epoch 4: Community Ecosystem] Ví tiền tệ số học tập AegisCoin (Proof-of-Learning Token Wallet).
    /// Ghi nhận nỗ lực học tập, giải bài tập khó và đóng góp tri thức của người học.
    /// </summary>
    public class AegisCoinWallet : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public long Balance { get; set; } = 100; // Khởi tạo tặng 100 AegisCoin chào mừng
        public long LifetimeEarned { get; set; } = 100;
        public long LifetimeSpent { get; set; } = 0;
        public DateTime? LastEarnedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual UserAccount? User { get; set; }
    }

    /// <summary>
    /// Bản ghi giao dịch sổ cái kép (Double-entry Ledger) của AegisCoin.
    /// </summary>
    public class AegisCoinTransaction : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WalletId { get; set; }
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }

        /// <summary>
        /// Số tiền biến động: Dương (+) là cộng thưởng, Âm (-) là chi tiêu
        /// </summary>
        public long Amount { get; set; }

        /// <summary>Số dư ví sau khi giao dịch hoàn tất</summary>
        public long BalanceAfter { get; set; }

        /// <summary>
        /// Loại giao dịch: WELCOME_BONUS, PRACTICE_PERFECT, STREAK_MILESTONE,
        /// HARD_QUESTION_SOLVED, COMMUNITY_CONTRIBUTION, UNLOCK_PREMIUM_EXAM, AI_MENTOR_DEEP_DIVE
        /// </summary>
        public string TransactionType { get; set; } = "WELCOME_BONUS";

        public string Description { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual AegisCoinWallet? Wallet { get; set; }
    }
}
