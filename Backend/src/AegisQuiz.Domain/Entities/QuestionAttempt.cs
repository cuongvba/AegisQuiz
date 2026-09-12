using System;

namespace AegisQuiz.Domain.Entities
{
    // [Cảnh giới 7: Phân tích Dữ liệu Hành vi]
    // Lưu lại từng "nhịp thở" của người dùng khi làm bài
    public class QuestionAttempt
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid QuestionId { get; set; }
        
        // Metadata để AI phân tích
        public string Category { get; set; } = string.Empty; // VD: "Toán lớp 1", "IT Agribank"
        public int Difficulty { get; set; }
        
        // Hành vi cốt lõi
        public string SelectedAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int TimeSpentSeconds { get; set; } // Học sinh suy nghĩ bao lâu?

        public DateTime AttemptDate { get; set; } = DateTime.UtcNow;
    }
}
