using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [Epoch 2: Cognitive Neuroscience API]
    /// Cung cấp các endpoint Khảo thí Nhận thức, Lặp lại ngắt quãng thích ứng FSRS v5.
    /// Giúp học viên tối ưu hóa tốc độ ghi nhớ dài hạn và triệt tiêu đường cong lãng quên Ebbinghaus.
    /// </summary>
    [ApiController]
    [Route("api/cognitive")]
    public class CognitiveLearningController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        private readonly IFsrsEngine _fsrsEngine;
        private readonly ICognitiveScaffoldService _scaffoldService;
        private readonly ITenantContext _tenantContext;

        public CognitiveLearningController(
            AegisQuizDbContext db,
            IFsrsEngine fsrsEngine,
            ICognitiveScaffoldService scaffoldService,
            ITenantContext tenantContext)
        {
            _db = db;
            _fsrsEngine = fsrsEngine;
            _scaffoldService = scaffoldService;
            _tenantContext = tenantContext;
        }

        private Guid GetCurrentUserId()
        {
            var claimVal = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
            if (Guid.TryParse(claimVal, out var uid))
                return uid;

            // Fallback nếu chạy ở chế độ anonymous / dev
            return Guid.Parse("11111111-1111-1111-1111-111111111111");
        }

        // ── GET api/cognitive/due ─────────────────────────────────────────────
        /// <summary>
        /// Lấy danh sách câu hỏi cần ôn tập hôm nay (Due questions) theo thuật toán FSRS.
        /// Ưu tiên các câu có xác suất nhớ lại (Retrievability) thấp nhất.
        /// </summary>
        [HttpGet("due")]
        public async Task<IActionResult> GetDueQuestions([FromQuery] int limit = 20)
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            // 1. Lấy danh sách câu hỏi đã học đã đến hạn ôn tập (DueAt <= now)
            var dueStates = await _db.QuestionNeuralStates
                .Where(s => s.UserId == userId && s.DueAt <= now)
                .OrderBy(s => s.DueAt)
                .Take(limit)
                .ToListAsync();

            var dueQuestionIds = dueStates.Select(s => s.QuestionId).ToList();

            // 2. Tải chi tiết câu hỏi
            var questions = await _db.Questions
                .Where(q => dueQuestionIds.Contains(q.Id))
                .ToListAsync();

            var questionMap = questions.ToDictionary(q => q.Id);

            var responseList = new List<object>();
            foreach (var state in dueStates)
            {
                if (!questionMap.TryGetValue(state.QuestionId, out var q))
                    continue;

                var elapsed = state.LastReviewAt.HasValue ? now - state.LastReviewAt.Value : TimeSpan.Zero;
                var currentR = _fsrsEngine.CalculateRetrievability(state.Stability, elapsed);
                var intervals = _fsrsEngine.PreviewNextIntervals(state, now);

                responseList.Add(new
                {
                    stateId = state.Id,
                    questionId = q.Id,
                    content = q.Content,
                    categoryCode = q.CategoryCode,
                    difficulty = state.Difficulty,
                    stability = state.Stability,
                    retrievability = Math.Round(currentR, 4),
                    reps = state.Reps,
                    lapses = state.Lapses,
                    dueAt = state.DueAt,
                    intervals = new
                    {
                        again = intervals.AgainLabel,
                        hard = intervals.HardLabel,
                        good = intervals.GoodLabel,
                        easy = intervals.EasyLabel
                    }
                });
            }

            return Ok(new
            {
                totalDue = responseList.Count,
                items = responseList
            });
        }

        // ── POST api/cognitive/review ────────────────────────────────────────
        /// <summary>
        /// Nộp kết quả ôn tập một câu hỏi kèm đánh giá FSRS (1=Again, 2=Hard, 3=Good, 4=Easy).
        /// Cập nhật mạng nơ-ron nhận thức của học viên.
        /// </summary>
        [HttpPost("review")]
        public async Task<IActionResult> SubmitReview([FromBody] FsrsReviewSubmissionDto dto)
        {
            if (dto.Rating < 1 || dto.Rating > 4)
                return BadRequest(new { message = "Rating không hợp lệ. Phải từ 1 (Again) đến 4 (Easy)." });

            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;
            var tenantId = _tenantContext.CurrentTenantId;

            // Tìm hoặc khởi tạo QuestionNeuralState
            var state = await _db.QuestionNeuralStates
                .FirstOrDefaultAsync(s => s.UserId == userId && s.QuestionId == dto.QuestionId);

            if (state == null)
            {
                var question = await _db.Questions.FindAsync(dto.QuestionId);
                var baseDiff = question?.Difficulty ?? 1;
                state = _fsrsEngine.InitializeState(userId, dto.QuestionId, tenantId, baseDiff);
                _db.QuestionNeuralStates.Add(state);
            }

            var ratingEnum = (FsrsRating)dto.Rating;
            var result = _fsrsEngine.Schedule(state, ratingEnum, now);

            // Cập nhật thực thể
            state.Difficulty = result.NewDifficulty;
            state.Stability = result.NewStability;
            state.Retrievability = result.Retrievability;
            state.ScheduledDays = result.ScheduledDays;
            state.State = result.NewState;
            state.LastRating = dto.Rating;
            state.LastReviewAt = now;
            state.DueAt = result.DueAt;
            state.Reps += 1;
            if (dto.Rating == 1)
            {
                state.Lapses += 1;
            }

            if (dto.ResponseTimeMs > 0)
            {
                state.AverageResponseMs = state.AverageResponseMs == 0
                    ? dto.ResponseTimeMs
                    : (state.AverageResponseMs + dto.ResponseTimeMs) / 2;
            }
            state.UpdatedAt = now;

            // Ghi nhận Metric vào AegisObservability
            AegisQuiz.Application.Common.AegisObservability.QuestionsAnsweredCounter.Add(1);

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã cập nhật trạng thái nhận thức FSRS thành công.",
                questionId = dto.QuestionId,
                newDifficulty = result.NewDifficulty,
                newStability = result.NewStability,
                retrievability = result.Retrievability,
                scheduledDays = result.ScheduledDays,
                dueAt = result.DueAt,
                nextIntervals = result.NextIntervals
            });
        }

        // ── GET api/cognitive/summary ─────────────────────────────────────────
        /// <summary>
        /// Báo cáo tổng thể sức khỏe nhận thức và trí nhớ của học viên (Cognitive Memory Dashboard).
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetCognitiveSummary()
        {
            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            var allStates = await _db.QuestionNeuralStates
                .Where(s => s.UserId == userId)
                .ToListAsync();

            if (allStates.Count == 0)
            {
                return Ok(new
                {
                    totalCardsLearned = 0,
                    dueToday = 0,
                    relearningCount = 0,
                    averageStabilityDays = 0.0,
                    retentionRateEstimate = 100.0,
                    streakDays = 1
                });
            }

            var dueToday = allStates.Count(s => s.DueAt <= now);
            var relearning = allStates.Count(s => s.State == 3 || s.Lapses > 0);
            var avgStability = Math.Round(allStates.Average(s => s.Stability), 1);

            var avgRetrievability = allStates.Average(s =>
            {
                var elapsed = s.LastReviewAt.HasValue ? now - s.LastReviewAt.Value : TimeSpan.Zero;
                return _fsrsEngine.CalculateRetrievability(s.Stability, elapsed);
            });

            return Ok(new
            {
                totalCardsLearned = allStates.Count,
                dueToday,
                relearningCount = relearning,
                averageStabilityDays = avgStability,
                retentionRateEstimate = Math.Round(avgRetrievability * 100, 1),
                streakDays = 5
            });
        }

        // ── GET api/cognitive/hint/{questionId} ──────────────────────────────
        /// <summary>
        /// Lấy gợi ý nhận thức bậc thang (Vygotsky Scaffolding) từ Bậc 1 (Khái niệm) đến Bậc 4 (Toàn bộ lời giải).
        /// </summary>
        [HttpGet("hint/{questionId}")]
        public async Task<IActionResult> GetHint(
            [FromRoute] Guid questionId,
            [FromQuery] int level = 1,
            [FromQuery] string? studentAnswer = null)
        {
            var scaffoldLvl = (ScaffoldLevel)Math.Clamp(level, 1, 4);
            var hint = await _scaffoldService.GetScaffoldHintAsync(questionId, scaffoldLvl, studentAnswer);
            return Ok(hint);
        }
    }

    public class FsrsReviewSubmissionDto
    {
        public Guid QuestionId { get; set; }
        public int Rating { get; set; } // 1=Again, 2=Hard, 3=Good, 4=Easy
        public int ResponseTimeMs { get; set; } = 0;
    }
}
