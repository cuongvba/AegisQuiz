using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.Services
{
    // ── Essay Grading Job ──────────────────────────────────────────────────────
    /// <summary>Message đưa vào queue khi học viên nộp bài tự luận.</summary>
    public record EssayGradingJob(
        Guid   AttemptId,
        Guid   QuestionId,
        Guid   UserId,
        string StudentAnswer,
        string QuestionContent,
        string Rubric
    );

    // ── Essay Grading Channel ──────────────────────────────────────────────────
    /// <summary>
    /// [Phase A - FIX] Channel&lt;T&gt; singleton — bridge giữa Controller và Worker.
    /// Thread-safe, backpressure-aware, zero-allocation per message.
    /// </summary>
    public class EssayGradingChannel
    {
        private readonly Channel<EssayGradingJob> _channel;

        public EssayGradingChannel()
        {
            _channel = Channel.CreateBounded<EssayGradingJob>(new BoundedChannelOptions(500)
            {
                FullMode          = BoundedChannelFullMode.Wait,
                SingleReader      = true,
                SingleWriter      = false
            });
        }

        public ChannelWriter<EssayGradingJob>  Writer => _channel.Writer;
        public ChannelReader<EssayGradingJob>  Reader => _channel.Reader;
    }

    // ── Essay Grading Background Worker ───────────────────────────────────────
    /// <summary>
    /// [Phase A - FIX] Thay thế EssayQuestion.EvaluateAnswer() throws NotImplementedException.
    ///
    /// Luồng xử lý:
    ///   1. AttemptController nhận nộp bài essay → enqueue EssayGradingJob
    ///   2. EssayGradingWorker (BackgroundService) nhận job từ Channel
    ///   3. Gọi IGeminiGradingService.GradeEssayAsync() (thật)
    ///   4. Lưu điểm vào QuestionAttemptAnswer (IsCorrect + Score)
    ///   5. [TODO Phase 1] Gửi SignalR notification tới user: "Bài essay đã được chấm"
    /// </summary>
    public class EssayGradingWorker : BackgroundService
    {
        private readonly EssayGradingChannel  _channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EssayGradingWorker> _logger;

        public EssayGradingWorker(
            EssayGradingChannel  channel,
            IServiceScopeFactory scopeFactory,
            ILogger<EssayGradingWorker> logger)
        {
            _channel      = channel;
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[EssayGradingWorker] Started — listening for essay grading jobs...");

            await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessJobAsync(job, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[EssayGradingWorker] Failed to grade essay for AttemptId={AttemptId}",
                        job.AttemptId);
                }
            }

            _logger.LogInformation("[EssayGradingWorker] Stopped.");
        }

        private async Task ProcessJobAsync(EssayGradingJob job, CancellationToken ct)
        {
            _logger.LogDebug("[EssayGradingWorker] Grading essay AttemptId={AttemptId}", job.AttemptId);

            using var scope   = _scopeFactory.CreateScope();
            var gradingService = scope.ServiceProvider.GetRequiredService<IGeminiGradingService>();
            var db             = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            // Gọi Gemini thật để chấm điểm
            var result = await gradingService.GradeEssayAsync(
                job.QuestionContent,
                job.Rubric,
                job.StudentAnswer);

            // Lưu kết quả vào database
            var attemptAnswer = await db.QuestionAttemptAnswers
                .FindAsync(new object[] { job.AttemptId }, ct);

            if (attemptAnswer != null)
            {
                attemptAnswer.AiScore       = result.Score;
                attemptAnswer.AiExplanation = result.Explanation;
                attemptAnswer.GradedAt      = DateTime.UtcNow;
                attemptAnswer.IsCorrect     = result.Score >= 5.0; // Pass nếu >= 5/10

                await db.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "[EssayGradingWorker] ✅ Graded AttemptId={AttemptId} Score={Score}",
                job.AttemptId, result.Score);

            // TODO Phase 1: SignalR notification
            // await _hubContext.Clients.User(job.UserId.ToString())
            //     .SendAsync("EssayGraded", new { attemptId = job.AttemptId, score = result.Score });
        }
    }
}
