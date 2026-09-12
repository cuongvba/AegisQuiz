using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [World-Class Refactor] AchievementsController — Gamification: Badges, XP.
    /// Route base: api/quiz
    /// </summary>
    [ApiController]
    [Route("api/quiz")]
    public class AchievementsController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        public AchievementsController(AegisQuizDbContext db) => _db = db;

        // ── GET api/quiz/achievements ──────────────────────────────────────────
        [HttpGet("achievements")]
        public async Task<IActionResult> GetAchievements([FromQuery] Guid? userId)
        {
            try
            {
                var achievements = await _db.LearningAchievements
                    .Where(a => a.Enabled)
                    .OrderBy(a => a.XpReward)
                    .ToListAsync();

                if (!achievements.Any())
                    return Ok(GetDefaultAchievements());

                var earnedIds = new HashSet<Guid>();
                if (userId.HasValue)
                {
                    var earned = await _db.PersonAchievements
                        .Where(p => p.UserId == userId.Value)
                        .Select(p => p.AchievementId)
                        .ToListAsync();
                    earnedIds = earned.ToHashSet();
                }

                var response = achievements.Select(a => new
                {
                    id          = a.Code,
                    title       = a.Name,
                    description = a.Description,
                    iconEmoji   = a.IconEmoji,
                    badgeColor  = a.BadgeColor,
                    xpReward    = a.XpReward,
                    triggerType = a.TriggerType,
                    isUnlocked  = earnedIds.Contains(a.Id)
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy thành tích: {ex.Message}" });
            }
        }

        // ── POST api/quiz/achievements/check ───────────────────────────────────
        /// <summary>Kiểm tra và trao thành tích cho user sau khi nộp bài.</summary>
        [HttpPost("achievements/check")]
        public async Task<IActionResult> CheckAndAwardAchievements([FromQuery] Guid userId)
        {
            try
            {
                var newlyEarned = new List<object>();
                var attempts    = await _db.QuestionAttempts
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.AttemptDate)
                    .Take(100)
                    .ToListAsync();

                var allAchievements = await _db.LearningAchievements
                    .Where(a => a.Enabled)
                    .ToListAsync();

                var earned = await _db.PersonAchievements
                    .Where(p => p.UserId == userId)
                    .Select(p => p.AchievementId)
                    .ToHashSetAsync();

                foreach (var achievement in allAchievements)
                {
                    if (earned.Contains(achievement.Id)) continue;

                    bool triggered = achievement.TriggerType switch
                    {
                        "first_attempt" => attempts.Count >= 1,
                        "quiz_score"    => EvalScoreCondition(achievement.TriggerCondition, attempts),
                        "quiz_streak"   => EvalStreakCondition(achievement.TriggerCondition, attempts),
                        _               => false
                    };

                    if (triggered)
                    {
                        _db.PersonAchievements.Add(new PersonAchievement
                        {
                            Id            = Guid.NewGuid(),
                            UserId        = userId,
                            AchievementId = achievement.Id,
                            EarnedAt      = DateTime.UtcNow,
                            XpEarned      = achievement.XpReward
                        });
                        newlyEarned.Add(new
                        {
                            code      = achievement.Code,
                            name      = achievement.Name,
                            iconEmoji = achievement.IconEmoji,
                            xpReward  = achievement.XpReward
                        });
                    }
                }

                if (newlyEarned.Any())
                    await _db.SaveChangesAsync();

                return Ok(new { newlyEarned, count = newlyEarned.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi kiểm tra thành tích: {ex.Message}" });
            }
        }

        // ── POST api/quiz/achievements/seed-defaults ───────────────────────────
        [HttpPost("achievements/seed-defaults")]
        public async Task<IActionResult> SeedDefaults()
        {
            if (await _db.LearningAchievements.AnyAsync())
                return Ok(new { message = "Thành tích đã được seed trước đó." });

            var defaults = new[]
            {
                new LearningAchievement { Code = "first_attempt",  Name = "Khởi đầu rực rỡ",      Description = "Hoàn thành bài thi đầu tiên",               IconEmoji = "🌟", BadgeColor = "#F59E0B", TriggerType = "first_attempt", TriggerCondition = "{}",                XpReward = 50  },
                new LearningAchievement { Code = "streak_10",      Name = "Chiến binh bất bại",     Description = "10 câu đúng liên tiếp",                      IconEmoji = "⚔️", BadgeColor = "#10B981", TriggerType = "quiz_streak",   TriggerCondition = "{\"streak\":10}",   XpReward = 100 },
                new LearningAchievement { Code = "high_scorer",    Name = "Xuất sắc",               Description = "Đạt trên 90% trong một lần kiểm tra",        IconEmoji = "🏆", BadgeColor = "#F59E0B", TriggerType = "quiz_score",    TriggerCondition = "{\"minScore\":90}", XpReward = 200 },
                new LearningAchievement { Code = "night_owl",      Name = "Cú đêm chăm chỉ",        Description = "Học vào khoảng 0:00 – 4:00 sáng",            IconEmoji = "🦉", BadgeColor = "#8B5CF6", TriggerType = "first_attempt", TriggerCondition = "{}",                XpReward = 30  },
            };

            _db.LearningAchievements.AddRange(defaults);
            await _db.SaveChangesAsync();
            return Ok(new { message = $"Đã seed {defaults.Length} thành tích.", count = defaults.Length });
        }

        // ── Private helpers ────────────────────────────────────────────────────
        private static bool EvalScoreCondition(string condJson, List<QuestionAttempt> attempts)
        {
            try
            {
                using var doc = JsonDocument.Parse(condJson);
                if (!doc.RootElement.TryGetProperty("minScore", out var minEl)) return false;
                double minScore = minEl.GetDouble();
                if (!attempts.Any()) return false;
                double accuracy = (double)attempts.Count(a => a.IsCorrect) / attempts.Count * 100;
                return accuracy >= minScore;
            }
            catch { return false; }
        }

        private static bool EvalStreakCondition(string condJson, List<QuestionAttempt> attempts)
        {
            try
            {
                using var doc = JsonDocument.Parse(condJson);
                if (!doc.RootElement.TryGetProperty("streak", out var streakEl)) return false;
                int required = streakEl.GetInt32();
                int current  = 0;
                foreach (var attempt in attempts.OrderBy(a => a.AttemptDate))
                {
                    if (attempt.IsCorrect) { current++; if (current >= required) return true; }
                    else current = 0;
                }
                return false;
            }
            catch { return false; }
        }

        private static object[] GetDefaultAchievements() => new object[]
        {
            new { id = "first_attempt", title = "Khởi đầu rực rỡ",  description = "Hoàn thành bài thi đầu tiên", isUnlocked = false, xpReward = 50  },
            new { id = "streak_10",     title = "Chiến binh bất bại", description = "10 câu đúng liên tiếp",       isUnlocked = false, xpReward = 100 },
        };
    }
}
