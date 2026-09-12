using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [World-Class Refactor] AdaptiveController — Sinh đề thích ứng & luyện tập.
    /// Tích hợp đầy đủ mô hình toán học Item Response Theory (3PL CAT Engine).
    /// Route base: api/quiz
    /// </summary>
    [ApiController]
    [Route("api/quiz")]
    public class AdaptiveController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        private readonly IIrtClientService _irtClient;

        public AdaptiveController(AegisQuizDbContext db, IIrtClientService irtClient)
        {
            _db = db;
            _irtClient = irtClient;
        }

        // ── POST api/quiz/cat/start ────────────────────────────────────────────
        /// <summary>Khởi tạo phiên thi thích ứng CAT/IRT 3-PL</summary>
        [HttpPost("cat/start")]
        public async Task<IActionResult> StartCat([FromBody] StartCatSessionRequest request)
        {
            try
            {
                // Truy vấn pool câu hỏi từ cơ sở dữ liệu
                var query = _db.Questions.AsQueryable();
                if (!string.IsNullOrWhiteSpace(request.TopicCode) && request.TopicCode != "ALL")
                {
                    var descendantCodes = await GetDescendantCodesAsync(request.TopicCode);
                    query = query.Where(q => descendantCodes.Contains(q.CategoryCode));
                }

                var dbQuestions = await query.ToListAsync();
                var eligibleQuestions = dbQuestions.Where(IsQuestionEnabled).ToList();

                if (eligibleQuestions.Count == 0)
                {
                    return BadRequest(new { message = "Không tìm thấy câu hỏi phù hợp cho chủ đề đã chọn." });
                }

                // Chuyển đổi thành tham số IRT 3PL chuẩn
                var irtBank = eligibleQuestions.Select(q =>
                {
                    double diffNorm = ((q.Difficulty - 3) * 1.0); // map 1..5 thành -2.0 .. +2.0
                    int optCount = q.Options?.Count ?? 0;
                    double guessRate = optCount >= 2 ? (1.0 / optCount) : 0.25;
                    guessRate = Math.Clamp(guessRate, 0.05, 0.50); // Pydantic 3PL IRT: c in [0.0, 0.5]

                    return new QuestionIrtDto
                    {
                        QuestionId = q.Id.ToString(),
                        A = 1.2, // Độ phân biệt mặc định
                        B = diffNorm, // Độ khó chuẩn hóa
                        C = guessRate, // Xác suất đoán mò
                        TopicCode = q.CategoryCode,
                        Difficulty = q.Difficulty
                    };
                }).ToList();

                var config = new CatConfigDto
                {
                    MinItems = Math.Clamp(request.MinItems ?? 5, 3, 10),
                    MaxItems = Math.Clamp(request.MaxItems ?? 20, 5, 50),
                    SeThreshold = Math.Clamp(request.SeThreshold ?? 0.3, 0.1, 1.0),
                    InitialTheta = Math.Clamp(request.InitialTheta ?? 0.0, -4.0, 4.0),
                    TopicCode = request.TopicCode
                };

                var startResult = await _irtClient.StartCatSessionAsync(
                    request.UserId == Guid.Empty ? Guid.NewGuid().ToString() : request.UserId.ToString(),
                    irtBank,
                    config);

                if (startResult == null)
                {
                    // Fallback sang Naive nếu IRT microservice tạm thời chưa phản hồi
                    return await FallbackStartCatAsync(eligibleQuestions, config);
                }

                // Gắn đầy đủ nội dung câu hỏi thật vào response
                if (startResult.Question != null)
                {
                    var qElem = (System.Text.Json.JsonElement)startResult.Question;
                    if (qElem.TryGetProperty("question_id", out var qIdProp) && Guid.TryParse(qIdProp.GetString(), out var targetQId))
                    {
                        var fullQ = eligibleQuestions.FirstOrDefault(x => x.Id == targetQId);
                        if (fullQ != null)
                        {
                            return Ok(new
                            {
                                sessionId = startResult.SessionId,
                                currentTheta = startResult.CurrentTheta,
                                administeredCount = startResult.AdministeredCount,
                                maxItems = startResult.MaxItems,
                                finished = false,
                                question = fullQ
                            });
                        }
                    }
                }

                return Ok(startResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khởi tạo phiên thi CAT: {ex.Message}" });
            }
        }

        // ── POST api/quiz/cat/{sessionId}/respond ──────────────────────────────
        /// <summary>Nộp câu trả lời cho câu hiện tại và nhận câu hỏi thích ứng tiếp theo</summary>
        [HttpPost("cat/{sessionId}/respond")]
        public async Task<IActionResult> RespondCat([FromRoute] string sessionId, [FromBody] SubmitCatAnswerRequest request)
        {
            try
            {
                var question = await _db.Questions.FirstOrDefaultAsync(q => q.Id == request.QuestionId);
                if (question == null)
                {
                    return NotFound(new { message = "Không tìm thấy câu hỏi tương ứng." });
                }

                // Đánh giá đúng sai đa hình
                double score = question.EvaluateAnswer(request.SelectedAnswer);
                bool isCorrect = score >= 0.99;

                // Lưu QuestionAttempt để lưu vết học tập
                var attempt = new QuestionAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.Empty, // Có thể gán từ Claims nếu có auth
                    QuestionId = question.Id,
                    Category = question.CategoryCode,
                    Difficulty = question.Difficulty,
                    SelectedAnswer = request.SelectedAnswer,
                    IsCorrect = isCorrect,
                    TimeSpentSeconds = request.TimeSpentSeconds,
                    AttemptDate = DateTime.UtcNow
                };
                _db.QuestionAttempts.Add(attempt);
                await _db.SaveChangesAsync();

                var nextStep = await _irtClient.SubmitAnswerAndGetNextAsync(sessionId, isCorrect, request.TimeSpentSeconds);
                if (nextStep == null)
                {
                    return StatusCode(502, new { message = "Không nhận được phản hồi từ động cơ IRT." });
                }

                nextStep.LastIsCorrect = isCorrect;
                nextStep.LastExplanation = question.Explanation;

                // Nếu có câu hỏi tiếp theo, đính kèm đầy đủ thực thể câu hỏi
                if (!nextStep.Finished && nextStep.NextQuestion != null)
                {
                    var qElem = (System.Text.Json.JsonElement)nextStep.NextQuestion;
                    if (qElem.TryGetProperty("question_id", out var qIdProp) && Guid.TryParse(qIdProp.GetString(), out var targetQId))
                    {
                        var fullQ = await _db.Questions.FirstOrDefaultAsync(x => x.Id == targetQId);
                        return Ok(new
                        {
                            finished = false,
                            currentTheta = nextStep.CurrentTheta,
                            se = nextStep.Se,
                            administeredCount = nextStep.AdministeredCount,
                            lastIsCorrect = isCorrect,
                            lastExplanation = question.Explanation,
                            nextQuestion = fullQ
                        });
                    }
                }

                return Ok(nextStep);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi xử lý câu trả lời CAT: {ex.Message}" });
            }
        }

        // ── GET api/quiz/cat/{sessionId}/result ────────────────────────────────
        /// <summary>Lấy báo cáo kết quả đánh giá năng lực thích ứng CAT</summary>
        [HttpGet("cat/{sessionId}/result")]
        public async Task<IActionResult> GetCatResult([FromRoute] string sessionId)
        {
            try
            {
                var result = await _irtClient.GetSessionResultAsync(sessionId);
                if (result == null)
                {
                    return NotFound(new { message = "Không tìm thấy kết quả hoặc phiên thi đã hết hạn." });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy kết quả CAT: {ex.Message}" });
            }
        }

        private async Task<IActionResult> FallbackStartCatAsync(List<QuestionBase> questions, CatConfigDto config)
        {
            var firstQ = questions.OrderBy(q => Math.Abs(q.Difficulty - 3)).FirstOrDefault() ?? questions[0];
            return Ok(new
            {
                sessionId = Guid.NewGuid().ToString(),
                currentTheta = 0.0,
                administeredCount = 0,
                maxItems = config.MaxItems,
                finished = false,
                question = firstQ,
                isFallback = true
            });
        }

        // ── POST api/quiz/adaptive ─────────────────────────────────────────────
        /// <summary>
        /// Sinh đề thích ứng dựa trên lịch sử người học.
        /// Phase C sẽ thay bằng IRT/CAT engine.
        /// </summary>
        [HttpPost("adaptive")]
        public async Task<IActionResult> GenerateAdaptiveQuiz([FromBody] AdaptiveQuizRequest request)
        {
            try
            {
                // Bước 1: Phân tích lịch sử — tìm chủ đề yếu nhất
                var history = await _db.QuestionAttempts
                    .Where(q => q.UserId == request.UserId)
                    .ToListAsync();

                string weakestCategory = "GENERAL"; // Default nếu chưa có lịch sử

                if (history.Any())
                {
                    var weakStat = history
                        .GroupBy(h => h.Category)
                        .Select(g => new
                        {
                            Category = g.Key,
                            Accuracy = (double)g.Count(x => x.IsCorrect) / g.Count(),
                            Total    = g.Count()
                        })
                        .Where(s => s.Total >= 2)          // Cần tối thiểu 2 lần thử để đánh giá
                        .OrderBy(s => s.Accuracy)
                        .FirstOrDefault();

                    if (weakStat != null) weakestCategory = weakStat.Category;
                }

                // Bước 2: Lấy câu hỏi theo chủ đề yếu, loại bỏ câu đã làm gần đây
                var recentQuestionIds = history
                    .Where(h => h.AttemptDate >= DateTime.UtcNow.AddDays(-7))
                    .Select(h => h.QuestionId)
                    .ToHashSet();

                var count = request.Count > 0 ? Math.Min(request.Count, 20) : 10;

                var weakQuestions = await _db.Questions
                    .Where(q => q.CategoryCode == weakestCategory
                             && !recentQuestionIds.Contains(q.Id))
                    .ToListAsync();

                var adaptiveQuestions = weakQuestions
                    .Where(IsQuestionEnabled)
                    .OrderBy(_ => Guid.NewGuid())   // Shuffle
                    .Take(count)
                    .ToList();

                // Bổ sung từ chủ đề khác nếu không đủ
                if (adaptiveQuestions.Count < count)
                {
                    var remaining = count - adaptiveQuestions.Count;
                    var extraIds  = adaptiveQuestions.Select(q => q.Id).ToHashSet();

                    var extras = await _db.Questions
                        .Where(q => q.CategoryCode != weakestCategory
                                 && !recentQuestionIds.Contains(q.Id)
                                 && !extraIds.Contains(q.Id))
                        .ToListAsync();

                    adaptiveQuestions.AddRange(
                        extras.Where(IsQuestionEnabled)
                              .OrderBy(_ => Guid.NewGuid())
                              .Take(remaining));
                }

                return Ok(new
                {
                    weakestCategory,
                    totalHistory   = history.Count,
                    questions      = adaptiveQuestions,
                    engineVersion  = "1.0-naive" // Phase C: sẽ đổi thành "2.0-irt-cat"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi tạo đề thích ứng: {ex.Message}" });
            }
        }

        // ── POST api/quiz/practice ─────────────────────────────────────────────
        /// <summary>Sinh đề luyện tập theo cấu hình (chủ đề + số lượng + chế độ).</summary>
        [HttpPost("practice")]
        public async Task<IActionResult> GeneratePractice([FromBody] GeneratePracticeRequest request)
        {
            try
            {
                var questionsList = new List<QuestionBase>();

                foreach (var topicConf in request.TopicConfigs)
                {
                    bool isFatalFilter = request.OnlyCritical || (topicConf.OnlyCritical ?? false) || 
                                          topicConf.TopicKey.Equals("CRITICAL_ONLY", StringComparison.OrdinalIgnoreCase) ||
                                          topicConf.TopicKey.Equals("GPLX_FATAL", StringComparison.OrdinalIgnoreCase);

                    List<QuestionBase> all;
                    if (topicConf.TopicKey.Equals("ALL", StringComparison.OrdinalIgnoreCase) ||
                        topicConf.TopicKey.Equals("CRITICAL_ONLY", StringComparison.OrdinalIgnoreCase) ||
                        topicConf.TopicKey.Equals("GPLX_FATAL", StringComparison.OrdinalIgnoreCase))
                    {
                        all = await _db.Questions.ToListAsync();
                    }
                    else
                    {
                        var descendants = await GetDescendantCodesAsync(topicConf.TopicKey);
                        all = await _db.Questions
                            .Where(q => descendants.Contains(q.CategoryCode))
                            .ToListAsync();
                    }

                    var active = all.Where(IsQuestionEnabled);
                    if (isFatalFilter)
                    {
                        var fatalOnly = active.Where(q => q.IsCritical).ToList();
                        if (fatalOnly.Count > 0)
                        {
                            active = fatalOnly;
                        }
                    }

                    var activeList = active.ToList();

                    List<QuestionBase> topicQuestions;
                    if (topicConf.IsRandom)
                    {
                        topicQuestions = activeList
                            .OrderBy(_ => Guid.NewGuid())
                            .Take(topicConf.NumberOfQuestions)
                            .ToList();
                    }
                    else
                    {
                        int skip = Math.Max(0, topicConf.StartIndex - 1);
                        topicQuestions = activeList
                            .OrderBy(q => q.Id)
                            .Skip(skip)
                            .Take(topicConf.NumberOfQuestions)
                            .ToList();
                    }

                    questionsList.AddRange(topicQuestions);
                }

                if (request.ShuffleQuestions)
                    questionsList = questionsList.OrderBy(_ => Guid.NewGuid()).ToList();

                return Ok(questionsList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi sinh đề luyện tập: {ex.Message}" });
            }
        }

        // ── GET api/quiz/paths ─────────────────────────────────────────────────
        [HttpGet("paths")]
        public async Task<IActionResult> GetLearningPaths()
        {
            try
            {
                var paths = await _db.LearningPaths
                    .Include(p => p.Steps)
                    .Where(p => p.Enabled && p.IsPublic)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
                return Ok(paths);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy lộ trình: {ex.Message}" });
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────
        private static bool IsQuestionEnabled(QuestionBase q)
        {
            if (q.Payload == null) return true;
            try
            {
                if (q.Payload.RootElement.TryGetProperty("enabled", out var prop))
                {
                    if (prop.ValueKind == System.Text.Json.JsonValueKind.False) return false;
                    if (prop.ValueKind == System.Text.Json.JsonValueKind.String)
                        return !string.Equals(prop.GetString(), "false", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { /* fallback */ }
            return true;
        }

        private async Task<List<string>> GetDescendantCodesAsync(string topicCode)
        {
            var codes     = new List<string> { topicCode };
            var allTopics = await _db.BankTopics.Where(t => t.Enabled).ToListAsync();
            var start     = allTopics.FirstOrDefault(t => t.Code == topicCode);
            if (start == null) return codes;

            var queue = new Queue<Guid>();
            queue.Enqueue(start.Id);

            while (queue.Count > 0)
            {
                var parentId = queue.Dequeue();
                foreach (var child in allTopics.Where(t => t.ParentId == parentId))
                {
                    codes.Add(child.Code);
                    queue.Enqueue(child.Id);
                }
            }
            return codes;
        }
    }
}
