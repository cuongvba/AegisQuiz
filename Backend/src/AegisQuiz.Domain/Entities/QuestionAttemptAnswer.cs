using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// Lưu câu trả lời chi tiết từng câu hỏi trong một lần làm bài.
    /// Học từ quiz-service: nền tảng dữ liệu cho phân tích câu yếu, lộ trình học tập, AI calibration.
    /// </summary>
    public class QuestionAttemptAnswer
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid AttemptId { get; set; }    // FK → QuestionAttempt (aggregate)
        public Guid UserId { get; set; }        // Denormalized để query nhanh
        public Guid QuestionId { get; set; }

        /// <summary>Câu trả lời học viên gửi lên (raw string hoặc JSON array).</summary>
        public string SubmittedAnswer { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        /// <summary>Điểm đạt được cho câu này.</summary>
        public double PointsEarned { get; set; }

        /// <summary>Điểm tối đa của câu này.</summary>
        public double PointsMax { get; set; } = 1.0;

        /// <summary>Thời gian trả lời (giây). Null nếu không đo được.</summary>
        public int? TimeTakenSec { get; set; }

        /// <summary>Chủ đề câu hỏi — denormalized để query nhanh không cần JOIN.</summary>
        public string? CategoryCode { get; set; }

        public int Difficulty { get; set; }

        public DateTime AttemptDate { get; set; } = DateTime.UtcNow;

        // ── Essay AI Grading fields ────────────────────────────────────────────
        /// <summary>Điểm do Gemini AI chấm (dành cho câu tự luận).</summary>
        public double? AiScore { get; set; }

        /// <summary>Nhận xét chi tiết của Gemini AI.</summary>
        public string? AiExplanation { get; set; }

        /// <summary>Thời điểm Gemini hoàn thành chấm điểm.</summary>
        public DateTime? GradedAt { get; set; }
    }
}
