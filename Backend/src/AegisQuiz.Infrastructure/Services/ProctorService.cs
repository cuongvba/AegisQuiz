using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Enterprise Fortress — Epoch 3] Triển khai ProctorSignal Engine phân tích vi phạm và tự động liên kết kiểm toán.
    /// </summary>
    public class ProctorService : IProctorService
    {
        private readonly AegisQuizDbContext _db;
        private readonly IExamAuditService _audit;
        private readonly ILogger<ProctorService> _logger;

        private static readonly ConcurrentQueue<ProctorSignalEventDto> _recentViolations = new();
        private static int _todayViolationsCount = 0;
        private static DateTime _todayTrackingDate = DateTime.UtcNow.Date;

        public ProctorService(
            AegisQuizDbContext db,
            IExamAuditService audit,
            ILogger<ProctorService> logger)
        {
            _db = db;
            _audit = audit;
            _logger = logger;
        }

        public async Task<ProctorSignalEventDto> ProcessProctorEventAsync(ProctorSignalEventDto evt, CancellationToken ct = default)
        {
            // Reset đếm hàng ngày nếu sang ngày mới
            if (DateTime.UtcNow.Date > _todayTrackingDate)
            {
                _todayTrackingDate = DateTime.UtcNow.Date;
                Interlocked.Exchange(ref _todayViolationsCount, 0);
            }

            // 1. Phân loại mức độ nghiêm trọng
            string normalizedType = (evt.EventType ?? "UNKNOWN").ToUpperInvariant();
            string severity = "WARNING";

            switch (normalizedType)
            {
                case "MULTI_MONITOR":
                case "RAPID_ANSWER_ANOMALY":
                case "DEVTOOLS_OPEN":
                    severity = "CRITICAL";
                    break;
                case "TAB_SWITCH":
                case "WINDOW_BLUR":
                case "CLIPBOARD":
                case "CLIPBOARD_COPY":
                case "CLIPBOARD_PASTE":
                    severity = "WARNING";
                    break;
                case "HEARTBEAT":
                    severity = "INFO";
                    break;
                default:
                    severity = "WARNING";
                    break;
            }
            evt.Severity = severity;

            if (severity != "INFO")
            {
                Interlocked.Increment(ref _todayViolationsCount);

                // Thêm vào hàng đợi sự kiện gần đây (tối đa 50 bản ghi)
                _recentViolations.Enqueue(evt);
                while (_recentViolations.Count > 50 && _recentViolations.TryDequeue(out _)) { }

                // 2. Cập nhật số lần vi phạm vào ExamSession nếu có
                if (evt.ExamSessionId != Guid.Empty)
                {
                    var session = await _db.ExamSessions.FirstOrDefaultAsync(s => s.Id == evt.ExamSessionId, ct);
                    if (session != null)
                    {
                        session.NumberOfTabSwitches++;
                        if (session.NumberOfTabSwitches >= 5 && string.IsNullOrEmpty(session.EndReason))
                        {
                            session.EndReason = $"⚠️ CẦN GIÁM THỊ XEM XÉT: {session.NumberOfTabSwitches} vi phạm ProctorSignal ({normalizedType}).";
                        }
                        await _db.SaveChangesAsync(ct);
                    }
                }

                // 3. Ghi nhận tức thì vào Chuỗi băm kiểm toán bất biến (Immutable Audit Hash Chain)
                try
                {
                    await _audit.AppendAuditLogAsync(
                        tenantId: Tenant.CommunityTenantId,
                        examId: evt.ExamId != Guid.Empty ? evt.ExamId : evt.ExamSessionId,
                        userId: evt.UserId,
                        action: $"VIOLATION_{normalizedType}",
                        severity: severity,
                        details: $"{{\"candidate\": \"{evt.CandidateIdentifier}\", \"details\": \"{evt.Details}\", \"ip\": \"{evt.ClientIp}\"}}",
                        clientIp: evt.ClientIp,
                        userAgent: evt.UserAgent,
                        ct: ct);
                }
                catch (Exception exAudit)
                {
                    _logger.LogWarning(exAudit, "[ProctorSignal] Không thể ghi audit log cho sự kiện {Type}", evt.EventType);
                }

                _logger.LogWarning(
                    "[ProctorSignal] ⚠️ Phát hiện vi phạm [{Severity}] loại {Type} của thí sinh {Candidate} trên Session {SessionId}",
                    severity, normalizedType, evt.CandidateIdentifier, evt.ExamSessionId);
            }

            return evt;
        }

        public Task<int> GetTodayViolationCountAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_todayViolationsCount);
        }

        public Task<List<ProctorSignalEventDto>> GetRecentViolationsFeedAsync(int limit = 20, CancellationToken ct = default)
        {
            var list = _recentViolations.Reverse().Take(limit).ToList();
            return Task.FromResult(list);
        }
    }
}
