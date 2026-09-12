using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [World-Class Refactor] LeaderboardController — Bảng xếp hạng.
    /// Route base: api/quiz/leaderboard
    /// </summary>
    [ApiController]
    [Route("api/quiz")]
    public class LeaderboardController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        public LeaderboardController(AegisQuizDbContext db) => _db = db;

        // ── GET api/quiz/leaderboard ───────────────────────────────────────────
        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard(
            [FromQuery] string period = "weekly",
            [FromQuery] int    top    = 20,
            [FromQuery] Guid?  userId = null)
        {
            try
            {
                var validPeriods = new[] { "daily", "weekly", "monthly", "all_time" };
                if (!validPeriods.Contains(period)) period = "weekly";
                top = Math.Clamp(top, 1, 100);

                // Ưu tiên pre-computed leaderboard (nhanh hơn)
                var preComputed = await _db.LeaderboardEntries
                    .Where(e => e.Period == period)
                    .OrderBy(e => e.Rank)
                    .Take(top)
                    .ToListAsync();

                if (preComputed.Any())
                {
                    var result = preComputed.Select((e, i) => new
                    {
                        rank          = e.Rank,
                        name          = e.DisplayName,
                        avatar        = e.AvatarInitial,
                        score         = Math.Round(e.AverageScore),
                        totalAttempts = e.TotalAttempts,
                        xp            = e.XpTotal,
                        isCurrentUser = userId.HasValue && e.UserId == userId.Value
                    });
                    return Ok(new { period, source = "pre_computed", data = result });
                }

                // Fallback: tính real-time từ QuestionAttempts
                var since = period switch
                {
                    "daily"   => DateTime.UtcNow.AddDays(-1),
                    "weekly"  => DateTime.UtcNow.AddDays(-7),
                    "monthly" => DateTime.UtcNow.AddDays(-30),
                    _         => DateTime.MinValue
                };

                var stats = await _db.QuestionAttempts
                    .Where(a => a.AttemptDate >= since)
                    .GroupBy(a => a.UserId)
                    .Select(g => new
                    {
                        UserId        = g.Key,
                        TotalScore    = g.Count(x => x.IsCorrect) * 100.0 / g.Count(),
                        TotalAttempts = g.Count(),
                        CorrectCount  = g.Count(x => x.IsCorrect)
                    })
                    .OrderByDescending(s => s.TotalScore)
                    .ThenByDescending(s => s.TotalAttempts)
                    .Take(top)
                    .ToListAsync();

                var dynamic = stats.Select((s, i) => new
                {
                    rank          = i + 1,
                    userId        = s.UserId,
                    score         = Math.Round(s.TotalScore, 1),
                    totalAttempts = s.TotalAttempts,
                    correctCount  = s.CorrectCount,
                    isCurrentUser = userId.HasValue && s.UserId == userId.Value
                });

                return Ok(new { period, source = "real_time", data = dynamic });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy bảng xếp hạng: {ex.Message}" });
            }
        }
    }
}
