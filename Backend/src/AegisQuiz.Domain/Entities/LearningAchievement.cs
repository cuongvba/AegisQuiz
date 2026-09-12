using System;
using System.Collections.Generic;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// Định nghĩa thành tích/huy chương — template.
    /// Học từ quiz-service LearningAchievement.
    /// </summary>
    public class LearningAchievement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string BadgeColor { get; set; } = "#4CAF50";
        public string? IconEmoji { get; set; }

        /// <summary>
        /// Loại điều kiện kích hoạt:
        ///   quiz_score | quiz_streak | topic_complete | first_attempt | xp_milestone
        /// </summary>
        public string TriggerType { get; set; } = string.Empty;

        /// <summary>
        /// JSON điều kiện: { "minScore": 90 } | { "streakDays": 7 } | { "xpRequired": 500 }
        /// </summary>
        public string TriggerCondition { get; set; } = "{}";

        public int XpReward { get; set; } = 0;
        public string? TitleReward { get; set; }
        public bool Enabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PersonAchievement> PersonAchievements { get; set; } = new List<PersonAchievement>();
    }

    /// <summary>Thành tích đã đạt được của một người học.</summary>
    public class PersonAchievement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid AchievementId { get; set; }
        public LearningAchievement Achievement { get; set; } = null!;
        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
        public int XpEarned { get; set; }
        /// <summary>Thông tin liên quan khi đạt thành tích (quizId, attemptId…).</summary>
        public string? ContextJson { get; set; }
    }
}
