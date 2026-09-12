using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// Bảng xếp hạng học viên — học từ quiz-service LearnerLeaderboardEntry.
    /// Được tính lại định kỳ (event hoặc cron).
    /// </summary>
    public class LearnerLeaderboardEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }

        /// <summary>Tên hiển thị (denormalized).</summary>
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarInitial { get; set; }

        /// <summary>daily | weekly | monthly | all_time</summary>
        public string Period { get; set; } = "weekly";
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public double TotalScore { get; set; }
        public double AverageScore { get; set; }
        public int TotalAttempts { get; set; }
        public int CorrectCount { get; set; }

        /// <summary>Tổng XP trong kỳ.</summary>
        public int XpTotal { get; set; }

        /// <summary>Hạng trong kỳ (1-based).</summary>
        public int Rank { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
