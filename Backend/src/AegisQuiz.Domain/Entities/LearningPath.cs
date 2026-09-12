using System;
using System.Collections.Generic;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// Lộ trình đào tạo — học từ quiz-service LearningPath.
    /// Tập hợp có thứ tự các chủ đề/kỹ năng cần hoàn thành.
    /// </summary>
    public class LearningPath
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = true;
        public bool Enabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<LearningPathStep> Steps { get; set; } = new List<LearningPathStep>();
    }

    /// <summary>Một bước trong lộ trình — gắn với BankTopic và thứ tự.</summary>
    public class LearningPathStep
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearningPathId { get; set; }
        public LearningPath LearningPath { get; set; } = null!;

        /// <summary>Thứ tự bước (1-based).</summary>
        public int StepOrder { get; set; }
        public string CategoryCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>Điểm tối thiểu để coi là vượt qua bước này (0-100).</summary>
        public double PassScore { get; set; } = 70.0;
        public string? LearningObjective { get; set; }
    }

    /// <summary>Tiến độ của một người học theo từng lộ trình.</summary>
    public class LearningPathProgress
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LearningPathId { get; set; }
        public LearningPath LearningPath { get; set; } = null!;
        public Guid UserId { get; set; }

        /// <summary>Bước hiện tại đang học (1-based).</summary>
        public int CurrentStep { get; set; } = 1;

        /// <summary>Phần trăm hoàn thành lộ trình (0-100).</summary>
        public double CompletionPercent { get; set; } = 0;
        public double AverageScore { get; set; } = 0;
        public DateTime? CompletedAt { get; set; }

        /// <summary>in_progress | completed | paused</summary>
        public string Status { get; set; } = "in_progress";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
