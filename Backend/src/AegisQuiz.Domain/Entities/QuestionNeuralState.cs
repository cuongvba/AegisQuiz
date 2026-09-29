using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [Cognitive Neuroscience — Epoch 2] Thực thể theo dõi trạng thái mạng nơ-ron nhận thức của người học cho từng câu hỏi.
    /// Dựa trên mô hình DSR (Difficulty - Stability - Retrievability) của thuật toán FSRS v4/v5 (Free Spaced Repetition Scheduler).
    /// </summary>
    public class QuestionNeuralState : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Người học</summary>
        public Guid UserId { get; set; }

        /// <summary>Câu hỏi</summary>
        public Guid QuestionId { get; set; }

        /// <summary>Tenant không gian người học đang thuộc về</summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// [D - Difficulty]: Độ khó tương đối đối với người học này (1.0 = Cực dễ, 10.0 = Cực khó). Mặc định: 5.0.
        /// </summary>
        public double Difficulty { get; set; } = 5.0;

        /// <summary>
        /// [S - Stability]: Độ bền vững trí nhớ (tính bằng ngày).
        /// Là khoảng thời gian cần thiết để khả năng nhớ lại (Retrievability) suy giảm từ 100% về 90%.
        /// </summary>
        public double Stability { get; set; } = 0.5;

        /// <summary>
        /// [R - Retrievability]: Xác suất nhớ lại kiến thức tại thời điểm hiện tại (0.0 đến 1.0).
        /// Tính theo công thức quên Ebbinghaus / FSRS: R = (1 + factor * t / S)^-decay.
        /// </summary>
        public double Retrievability { get; set; } = 1.0;

        /// <summary>Số ngày đã trôi qua kể từ lần ôn tập gần nhất</summary>
        public int ElapsedDays { get; set; } = 0;

        /// <summary>Số ngày được lên lịch cho chu kỳ ôn tập tiếp theo</summary>
        public int ScheduledDays { get; set; } = 1;

        /// <summary>Tổng số lần người học đã tương tác / làm câu hỏi này</summary>
        public int Reps { get; set; } = 0;

        /// <summary>Số lần người học bị quên hoặc làm sai câu hỏi này (Lapse count)</summary>
        public int Lapses { get; set; } = 0;

        /// <summary>
        /// Trạng thái học tập FSRS:
        /// 0 = New (Mới)
        /// 1 = Learning (Đang học bước đầu)
        /// 2 = Review (Đang trong chu kỳ lặp lại ngắt quãng)
        /// 3 = Relearning (Học lại do vừa quên/làm sai)
        /// </summary>
        public int State { get; set; } = 0;

        /// <summary>Đánh giá gần nhất (1=Again, 2=Hard, 3=Good, 4=Easy)</summary>
        public int LastRating { get; set; } = 0;

        /// <summary>Thời điểm ôn tập gần nhất</summary>
        public DateTime? LastReviewAt { get; set; }

        /// <summary>Thời điểm cần ôn tập tiếp theo (Due timestamp để query các câu đến hạn học)</summary>
        public DateTime DueAt { get; set; } = DateTime.UtcNow;

        /// <summary>Thời gian phản xạ trung bình (mili giây) cho câu hỏi này</summary>
        public int AverageResponseMs { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual QuestionBase? Question { get; set; }
        public virtual UserAccount? User { get; set; }
    }
}
