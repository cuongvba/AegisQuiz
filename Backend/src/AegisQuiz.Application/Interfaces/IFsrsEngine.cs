using System;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// Đánh giá của người học sau khi giải câu hỏi (FSRS Rating scale).
    /// </summary>
    public enum FsrsRating
    {
        /// <summary>Quên / Làm sai / Không hiểu</summary>
        Again = 1,
        /// <summary>Đúng nhưng khó khăn / ngập ngừng / mất nhiều thời gian</summary>
        Hard = 2,
        /// <summary>Đúng với nỗ lực nhận thức vừa phải</summary>
        Good = 3,
        /// <summary>Nắm vững hoàn toàn / trả lời ngay tức thì</summary>
        Easy = 4
    }

    /// <summary>
    /// Dự đoán khoảng thời gian cho 4 phương án đánh giá tiếp theo (hiển thị trên nút bấm UI).
    /// </summary>
    public class FsrsIntervalPreview
    {
        public TimeSpan AgainInterval { get; set; }
        public TimeSpan HardInterval { get; set; }
        public TimeSpan GoodInterval { get; set; }
        public TimeSpan EasyInterval { get; set; }

        public string AgainLabel { get; set; } = string.Empty;
        public string HardLabel { get; set; } = string.Empty;
        public string GoodLabel { get; set; } = string.Empty;
        public string EasyLabel { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kết quả tính toán chu kỳ ôn tập tiếp theo từ FSRS Engine.
    /// </summary>
    public class FsrsScheduleResult
    {
        public double NewDifficulty { get; set; }
        public double NewStability { get; set; }
        public double Retrievability { get; set; }
        public int ScheduledDays { get; set; }
        public int NewState { get; set; }
        public DateTime DueAt { get; set; }
        public FsrsIntervalPreview NextIntervals { get; set; } = new();
    }

    /// <summary>
    /// [Cognitive Intelligence — Epoch 2]
    /// Bộ máy lập lịch lặp lại ngắt quãng thích ứng tối tân FSRS v5 (Free Spaced Repetition Scheduler).
    /// Thay thế thuật toán SM-2 cổ điển (1987) bằng mô hình DSR thần kinh học thế kỷ 21.
    /// </summary>
    public interface IFsrsEngine
    {
        /// <summary>
        /// Tính toán xác suất nhớ lại hiện tại (Retrievability) dựa trên độ bền trí nhớ S và thời gian trôi qua.
        /// </summary>
        double CalculateRetrievability(double stability, TimeSpan elapsed);

        /// <summary>
        /// Lập lịch và cập nhật trạng thái nhận thức cho câu hỏi sau một lượt ôn tập/kiểm tra.
        /// </summary>
        FsrsScheduleResult Schedule(QuestionNeuralState currentState, FsrsRating rating, DateTime reviewTime, double requestRetention = 0.9);

        /// <summary>
        /// Dự đoán trước các khoảng thời gian cho 4 nút (Again, Hard, Good, Easy) để hiển thị trực quan cho học viên.
        /// </summary>
        FsrsIntervalPreview PreviewNextIntervals(QuestionNeuralState currentState, DateTime reviewTime, double requestRetention = 0.9);

        /// <summary>
        /// Khởi tạo trạng thái nhận thức mới cho một câu hỏi lần đầu tiên người học tiếp cận.
        /// </summary>
        QuestionNeuralState InitializeState(Guid userId, Guid questionId, Guid tenantId, int questionBaseDifficulty = 1);
    }
}
