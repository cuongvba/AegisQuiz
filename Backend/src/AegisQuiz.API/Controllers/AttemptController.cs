using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.AI;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [World-Class Refactor] AttemptController — Xử lý nộp bài, phân tích kết quả.
    /// Route base: api/quiz
    /// </summary>
    [ApiController]
    [Route("api/quiz")]
    public class AttemptController : ControllerBase
    {
        private readonly AegisQuizDbContext   _db;
        private readonly IGeminiMentorService _geminiMentor;
        private readonly IHttpClientFactory   _httpClientFactory;
        private readonly IConfiguration       _config;

        public AttemptController(
            AegisQuizDbContext   db,
            IGeminiMentorService geminiMentor,
            IHttpClientFactory   httpClientFactory,
            IConfiguration       config)
        {
            _db                = db;
            _geminiMentor      = geminiMentor;
            _httpClientFactory = httpClientFactory;
            _config            = config;
        }

        // ── POST api/quiz/attempt ──────────────────────────────────────────────
        /// <summary>Nộp kết quả 1 câu hỏi — lưu vào QuestionAttempts.</summary>
        [HttpPost("attempt")]
        public async Task<IActionResult> SubmitAttempt([FromBody] QuestionAttemptDto dto)
        {
            try
            {
                var attempt = new QuestionAttempt
                {
                    Id               = Guid.NewGuid(),
                    UserId           = dto.UserId,
                    QuestionId       = dto.QuestionId,
                    Category         = dto.Category,
                    Difficulty       = dto.Difficulty,
                    SelectedAnswer   = dto.SelectedAnswer,
                    IsCorrect        = dto.IsCorrect,
                    TimeSpentSeconds = dto.TimeSpentSeconds,
                    AttemptDate      = DateTime.UtcNow
                };

                _db.QuestionAttempts.Add(attempt);
                await _db.SaveChangesAsync();

                return Ok(new { success = true, attemptId = attempt.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lưu lịch sử làm bài: {ex.Message}" });
            }
        }

        // ── POST api/quiz/{questionId}/explain ─────────────────────────────────
        /// <summary>
        /// [FIX] Gọi Gemini thật để giải thích câu hỏi — thay vì hardcoded template.
        /// </summary>
        [HttpPost("{questionId:guid}/explain")]
        public async Task<IActionResult> ExplainQuestion(Guid questionId, [FromBody] ExplainRequest request)
        {
            try
            {
                var question = await _db.Questions.FindAsync(questionId);
                if (question == null)
                    return NotFound(new { message = "Không tìm thấy câu hỏi" });

                string correctOpt = question.CorrectOption ?? "";

                // [FIX] Gọi Gemini API thật — không còn hardcoded template
                var apiKey = _config["Gemini:ApiKey"] ?? "";
                string explanation;

                if (!string.IsNullOrEmpty(apiKey))
                {
                    var prompt = $@"Bạn là giảng viên tài chính ngân hàng xuất sắc. 
Câu hỏi: ""{question.Content}""
Học viên chọn: ""{request.SelectedAnswer}""
Đáp án đúng: ""{correctOpt}""

Hãy giải thích ngắn gọn (3-5 câu, tiếng Việt), súc tích tại sao đáp án đúng là như vậy và tại sao phương án học viên chọn chưa đúng (nếu sai). Trả lời bằng plain text, không dùng markdown.";

                    explanation = await CallGeminiFlashAsync(apiKey, prompt);
                }
                else
                {
                    explanation = $"Đáp án đúng là \"{correctOpt}\". Hãy xem lại tài liệu về chủ đề {question.CategoryCode}.";
                }

                return Ok(new { explanation });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi sinh giải thích AI: {ex.Message}" });
            }
        }

        // ── POST api/quiz/attempt/analyze ──────────────────────────────────────
        /// <summary>
        /// [FIX] Gọi Gemini thật để phân tích toàn bộ kết quả thi — không Task.Delay().
        /// </summary>
        [HttpPost("attempt/analyze")]
        public async Task<IActionResult> AnalyzeAttempt([FromBody] AnalyzeAttemptRequest request)
        {
            try
            {
                int total    = request.Attempts.Count;
                int correct  = request.Attempts.Count(a => a.IsCorrect);
                double pct   = total > 0 ? (double)correct * 100 / total : 0;

                var apiKey = _config["Gemini:ApiKey"] ?? "";
                string analysis;

                if (!string.IsNullOrEmpty(apiKey) && request.Attempts.Any())
                {
                    // Tổng hợp câu hỏi sai để Gemini phân tích
                    var wrongOnes = request.Attempts
                        .Where(a => !a.IsCorrect)
                        .Take(5)
                        .Select(a => $"- Câu: \"{a.Content}\" | Bạn chọn: \"{a.UserAnswer}\" | Đúng: \"{a.CorrectAnswer}\"")
                        .ToList();

                    var wrongSummary = wrongOnes.Any()
                        ? "Các câu sai:\n" + string.Join("\n", wrongOnes)
                        : "Bạn đã trả lời đúng tất cả câu hỏi!";

                    var prompt = $@"Bạn là cố vấn đào tạo tài chính ngân hàng. Học viên vừa hoàn thành bài kiểm tra:
Kết quả: {correct}/{total} câu đúng ({pct:F1}%).
{wrongSummary}

Hãy viết bản phân tích ngắn gọn (5-8 câu): điểm mạnh, điểm yếu cụ thể, và 3 gợi ý cải thiện thực tế. Tiếng Việt, plain text.";

                    analysis = await CallGeminiFlashAsync(apiKey, prompt);
                }
                else
                {
                    // Fallback khi không có API key
                    analysis = $"Kết quả: {correct}/{total} câu đúng ({pct:F1}%). " +
                               $"{(pct >= 80 ? "Xuất sắc! Tiếp tục duy trì." : pct >= 60 ? "Khá tốt, cần ôn thêm phần yếu." : "Cần ôn tập kỹ hơn trước khi thi chính thức.")}";
                }

                return Ok(new { analysis, score = correct, total, accuracy = Math.Round(pct, 1) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi phân tích AI: {ex.Message}" });
            }
        }

        // ── GET api/quiz/weak-topics ───────────────────────────────────────────
        [HttpGet("weak-topics")]
        public async Task<IActionResult> GetWeakTopics([FromQuery] Guid userId)
        {
            try
            {
                var stats = await _db.QuestionAttempts
                    .Where(a => a.UserId == userId)
                    .GroupBy(a => a.Category)
                    .Select(g => new
                    {
                        topicCode    = g.Key,
                        total        = g.Count(),
                        correct      = g.Count(x => x.IsCorrect),
                        accuracy     = (double)g.Count(x => x.IsCorrect) / g.Count(),
                        lastAttempt  = g.Max(x => x.AttemptDate)
                    })
                    .OrderBy(s => s.accuracy)
                    .Take(5)
                    .ToListAsync();

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy chủ đề yếu: {ex.Message}" });
            }
        }

        // ── GET api/quiz/attempts/history [NEW P3] ────────────────────────────
        /// <summary>Lấy lịch sử làm bài của người dùng theo thứ tự mới nhất.</summary>
        [HttpGet("attempts/history")]
        public async Task<IActionResult> GetAttemptHistory(
            [FromQuery] Guid userId,
            [FromQuery] int limit = 20)
        {
            try
            {
                if (userId == Guid.Empty)
                    return BadRequest(new { message = "userId không hợp lệ." });

                limit = Math.Clamp(limit, 1, 100);

                var history = await _db.QuestionAttempts
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.AttemptDate)
                    .Take(limit)
                    .Select(a => new
                    {
                        id             = a.Id,
                        questionId     = a.QuestionId,
                        category       = a.Category,
                        difficulty     = a.Difficulty,
                        selectedAnswer = a.SelectedAnswer,
                        isCorrect      = a.IsCorrect,
                        timeSpentSec   = a.TimeSpentSeconds,
                        attemptDate    = a.AttemptDate
                    })
                    .ToListAsync();

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy lịch sử làm bài: {ex.Message}" });
            }
        }

        // ── Private: Gemini Flash call (IHttpClientFactory — fix memory leak) ──
        private async Task<string> CallGeminiFlashAsync(string apiKey, string prompt)
        {
            // [FIX CRITICAL] Dùng IHttpClientFactory thay new HttpClient()
            // → Tránh Socket Exhaustion và Memory Leak trong production
            using var client = _httpClientFactory.CreateClient("gemini");

            var url  = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
            var body = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };
            var json    = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc    = JsonDocument.Parse(responseJson);

            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;
        }
    }
}
