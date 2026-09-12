using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// Phiên thi nghiêm ngặt — học từ quiz-service ExamSession.
    /// Tách biệt với QuestionAttempt (luyện tập) để đảm bảo tính nghiêm ngặt và proctoring.
    /// </summary>
    public class ExamSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // 1. Định danh
        public Guid ExamContestId { get; set; }     // FK → ExamContest (kỳ thi)
        public Guid ApplicantId { get; set; }        // UserId thí sinh
        public string ApplicantIdentifier { get; set; } = string.Empty; // SBD

        // 2. Lifecycle
        public DateTimeOffset TimeStarted { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? TimeSubmitted { get; set; }
        public ExamSessionStatus Status { get; set; } = ExamSessionStatus.InProgress;

        // 3. Anti-Cheat Tracking (học từ quiz-service)
        public int NumberOfTabSwitches { get; set; } = 0;
        public string? EndReason { get; set; }

        // 4. Snapshot câu hỏi (pre-generated để đảm bảo fairness khi resume)
        public List<Guid>? SnapshotQuestionIds { get; set; }

        // 5. Kết quả chấm
        public decimal EarnedScore { get; set; }
        public int CorrectItemCount { get; set; }
        public int TotalItemCount { get; set; }
        public bool Passed { get; set; }

        // 6. Đáp án cuối cùng (JSONB map: questionId → answerJson)
        public Dictionary<Guid, JsonElement> FinalAnswers { get; set; } = new();
    }

    public enum ExamSessionStatus
    {
        InProgress = 1,
        SubmittedByUser = 2,
        AutoSubmittedBySystem = 3,  // Server tự nộp khi hết giờ
        VoidedByAdmin = 99          // Bị giám thị hủy vì gian lận
    }
}
