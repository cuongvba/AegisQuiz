using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Domain.Entities;
using AegisQuiz.API.Hubs;

namespace AegisQuiz.API.Services
{
    /// <summary>
    /// Server-authoritative exam timer — học từ quiz-service ExamTimerService.
    /// 
    /// Nguyên tắc anti-cheat: KHÔNG bao giờ tin vào đồng hồ của client.
    /// Service này broadcast "TimerTick" mỗi giây đến tất cả phiên thi đang diễn ra.
    /// Refresh danh sách active exams mỗi 30 giây.
    /// </summary>
    public class ExamTimerService : BackgroundService
    {
        private readonly IHubContext<ExamProctoringHub> _hub;
        private readonly IServiceProvider _sp;
        private readonly ILogger<ExamTimerService> _logger;

        private IReadOnlySet<Guid> _activeExamIds = new HashSet<Guid>();
        private DateTimeOffset _lastRefreshed = DateTimeOffset.MinValue;

        public ExamTimerService(
            IHubContext<ExamProctoringHub> hub,
            IServiceProvider sp,
            ILogger<ExamTimerService> logger)
        {
            _hub = hub;
            _sp = sp;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("[ExamTimer] Server-side exam timer khởi động.");

            while (!ct.IsCancellationRequested)
            {
                // Refresh danh sách phiên thi đang diễn ra mỗi 30 giây
                if (DateTimeOffset.UtcNow - _lastRefreshed > TimeSpan.FromSeconds(30))
                    await RefreshActiveExamsAsync(ct);

                if (_activeExamIds.Count > 0)
                {
                    var serverTime = DateTimeOffset.UtcNow;

                    foreach (var examId in _activeExamIds)
                    {
                        try
                        {
                            await _hub.Clients
                                .Group($"exam_{examId}")
                                .SendAsync("TimerTick", new { serverTime }, ct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "[ExamTimer] Không gửi được tick đến exam {ExamId}", examId);
                        }
                    }
                }

                await Task.Delay(1_000, ct);
            }
        }

        private async Task RefreshActiveExamsAsync(CancellationToken ct)
        {
            try
            {
                await using var scope = _sp.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

                var ids = await db.ExamSessions
                    .Where(s => s.Status == ExamSessionStatus.InProgress)
                    .Select(s => s.Id)
                    .ToListAsync(ct);

                _activeExamIds = ids.ToHashSet();
                _lastRefreshed = DateTimeOffset.UtcNow;

                _logger.LogDebug("[ExamTimer] Refreshed: {Count} phiên thi đang diễn ra", ids.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExamTimer] Lỗi khi refresh danh sách phiên thi");
            }
        }
    }
}
