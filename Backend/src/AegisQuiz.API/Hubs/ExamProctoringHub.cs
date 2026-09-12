using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.API.Hubs
{
    /// <summary>
    /// Real-time exam proctoring hub — học từ quiz-service ExamProctoringHub.
    /// Group key: "exam_{examSessionId}" | "invigilator_{examSessionId}"
    /// 
    /// Chức năng:
    ///   - JoinExam: thí sinh vào phòng thi
    ///   - ReportSuspiciousActivity: báo hành vi vi phạm (tab_switch, copy_paste...)
    ///   - JoinInvigilator: giám thị theo dõi
    ///   - ConfirmVoidByInvigilator: giám thị xác nhận hủy bài
    /// </summary>
    public class ExamProctoringHub : Hub
    {
        private readonly AegisQuizDbContext _db;
        private readonly ILogger<ExamProctoringHub> _logger;

        public ExamProctoringHub(AegisQuizDbContext db, ILogger<ExamProctoringHub> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ── Thí sinh vào phòng thi ────────────────────────────────────────────
        public async Task JoinExam(Guid examSessionId)
        {
            var session = await _db.ExamSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == examSessionId);

            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Không tìm thấy phiên thi.");
                return;
            }

            bool isEnded = session.Status is ExamSessionStatus.SubmittedByUser
                                          or ExamSessionStatus.AutoSubmittedBySystem
                                          or ExamSessionStatus.VoidedByAdmin;
            if (isEnded)
            {
                await Clients.Caller.SendAsync("Error", "Phiên thi này đã kết thúc.");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, ExamGroup(examSessionId));
            _logger.LogInformation("[Proctoring] {ConnectionId} joined exam {ExamSessionId}", Context.ConnectionId, examSessionId);

            var now = DateTimeOffset.UtcNow;
            var elapsedSeconds = (int)(now - session.TimeStarted).TotalSeconds;

            await Clients.Caller.SendAsync("ExamState", new
            {
                examSessionId,
                status = session.Status.ToString(),
                elapsedSeconds,
                tabSwitchCount = session.NumberOfTabSwitches,
                serverTime = now
            });
        }

        // ── Báo hành vi vi phạm (tab_switch, copy_paste, devtools_open...) ────
        public async Task ReportSuspiciousActivity(Guid examSessionId, string activityType, string details = "")
        {
            var allowed = new[] { "tab_switch", "copy_paste", "context_menu", "devtools_open", "window_blur" };
            if (!allowed.Contains(activityType)) return;

            var session = await _db.ExamSessions.FirstOrDefaultAsync(s => s.Id == examSessionId);
            if (session == null) return;

            session.NumberOfTabSwitches++;
            await _db.SaveChangesAsync();

            _logger.LogWarning("[Proctoring] Vi phạm trên session {ExamSessionId}: {ActivityType} (tổng: {Count})",
                examSessionId, activityType, session.NumberOfTabSwitches);

            // Thông báo cho giám thị
            await Clients.Group(InvigilatorGroup(examSessionId))
                .SendAsync("ViolationDetected", new
                {
                    examSessionId,
                    activityType,
                    details,
                    violationCount = session.NumberOfTabSwitches,
                    timestamp = DateTimeOffset.UtcNow
                });

            // Cảnh báo thí sinh từ lần thứ 3
            if (session.NumberOfTabSwitches >= 3)
            {
                await Clients.Caller.SendAsync("Warning", new
                {
                    message = $"⚠️ Cảnh báo: Hành vi vi phạm đã được ghi nhận ({session.NumberOfTabSwitches} lần). Tiếp tục có thể bị thu bài.",
                    violationCount = session.NumberOfTabSwitches
                });
            }

            // Flag cho giám thị xem xét tại lần thứ 5 (không tự hủy — cần human review)
            if (session.NumberOfTabSwitches >= 5 && session.EndReason?.StartsWith("⚠️") != true)
            {
                session.EndReason = $"⚠️ CẦN GIÁM THỊ XEM XÉT: {session.NumberOfTabSwitches} vi phạm chống gian lận.";
                await _db.SaveChangesAsync();

                await Clients.Group(InvigilatorGroup(examSessionId))
                    .SendAsync("FlagForReview", new
                    {
                        examSessionId,
                        violationCount = session.NumberOfTabSwitches,
                        reason = session.EndReason,
                        timestamp = DateTimeOffset.UtcNow
                    });
            }
        }

        // ── Giám thị vào phòng giám sát ───────────────────────────────────────
        public async Task JoinInvigilator(Guid examSessionId)
        {
            // Trong thực tế cần check role từ JWT claim
            await Groups.AddToGroupAsync(Context.ConnectionId, InvigilatorGroup(examSessionId));
            _logger.LogInformation("[Proctoring] Giám thị {ConnectionId} đang giám sát kỳ thi {ExamSessionId}", Context.ConnectionId, examSessionId);

            var session = await _db.ExamSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == examSessionId);
            if (session != null)
            {
                await Clients.Caller.SendAsync("InvigilatorState", new
                {
                    examSessionId,
                    status = session.Status.ToString(),
                    violationCount = session.NumberOfTabSwitches,
                    serverTime = DateTimeOffset.UtcNow
                });
            }
        }

        // ── Giám thị xác nhận hủy bài sau khi xem xét ────────────────────────
        public async Task ConfirmVoidByInvigilator(Guid examSessionId)
        {
            var session = await _db.ExamSessions.FirstOrDefaultAsync(s => s.Id == examSessionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Không tìm thấy phiên thi.");
                return;
            }

            session.Status = ExamSessionStatus.VoidedByAdmin;
            session.TimeSubmitted = DateTimeOffset.UtcNow;
            session.EndReason = (session.EndReason ?? "") +
                $" | Bị hủy bởi giám thị lúc {DateTimeOffset.UtcNow:O}";
            await _db.SaveChangesAsync();

            await Clients.Group(ExamGroup(examSessionId)).SendAsync("ExamTerminated", new
            {
                reason = "Bài thi đã bị hủy bởi giám thị do vi phạm liêm chính học thuật.",
                violationCount = session.NumberOfTabSwitches
            });

            _logger.LogWarning("[Proctoring] Kỳ thi {ExamSessionId} bị hủy bởi giám thị", examSessionId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("[Proctoring] {ConnectionId} đã ngắt kết nối", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        private static string ExamGroup(Guid id) => $"exam_{id}";
        private static string InvigilatorGroup(Guid id) => $"invigilator_{id}";
    }
}
