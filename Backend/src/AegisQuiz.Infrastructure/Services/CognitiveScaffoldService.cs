using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Epoch 2: Intelligent Feedback Scaffold]
    /// Triển khai dịch vụ cung cấp giàn giáo nhận thức bậc thang (Vygotsky Scaffolding).
    /// </summary>
    public class CognitiveScaffoldService : ICognitiveScaffoldService
    {
        private readonly AegisQuizDbContext _db;
        private readonly ICacheService _cache;
        private readonly IUniversalAiRouter _aiRouter;
        private readonly ILogger<CognitiveScaffoldService> _logger;

        public CognitiveScaffoldService(
            AegisQuizDbContext db,
            ICacheService cache,
            IUniversalAiRouter aiRouter,
            ILogger<CognitiveScaffoldService> logger)
        {
            _db = db;
            _cache = cache;
            _aiRouter = aiRouter;
            _logger = logger;
        }

        public async Task<ScaffoldHintDto> GetScaffoldHintAsync(
            Guid questionId,
            ScaffoldLevel level,
            string? studentAnswer = null,
            CancellationToken ct = default)
        {
            var cacheKey = $"scaffold:{questionId}:lvl_{(int)level}";
            var cached = await _cache.GetAsync<ScaffoldHintDto>(cacheKey, ct);
            if (cached != null)
            {
                return cached;
            }

            var question = await _db.Questions.FindAsync(new object[] { questionId }, ct);
            if (question == null)
            {
                return new ScaffoldHintDto
                {
                    QuestionId = questionId,
                    Level = level,
                    LevelTitle = "Không tìm thấy câu hỏi",
                    HintContent = "Không có thông tin chi tiết cho câu hỏi này.",
                    HasNextLevel = false
                };
            }

            // Trích xuất lời giải gốc từ Payload nếu có
            string? baseExplanation = null;
            if (question.Payload != null)
            {
                try
                {
                    if (question.Payload.RootElement.TryGetProperty("explanation", out var expElem))
                    {
                        baseExplanation = expElem.GetString();
                    }
                }
                catch { /* Payload có thể ở định dạng khác */ }
            }

            var hintDto = new ScaffoldHintDto
            {
                QuestionId = questionId,
                Level = level,
                HasNextLevel = (int)level < 4
            };

            switch (level)
            {
                case ScaffoldLevel.ConceptAnchor:
                    hintDto.LevelTitle = "Bậc 1: Khái Niệm Cốt Lõi & Định Lý Nền Tảng";
                    hintDto.HintContent = ExtractConceptAnchor(question, baseExplanation);
                    hintDto.SocraticQuestion = "Bạn có thể viết lại công thức tổng quát trước khi thay số không?";
                    break;

                case ScaffoldLevel.MethodStrategy:
                    hintDto.LevelTitle = "Bậc 2: Chiến Lược Giải Quyết & Bước Đi Đầu Tiên";
                    hintDto.HintContent = ExtractMethodStrategy(question, baseExplanation);
                    hintDto.SocraticQuestion = "Điều kiện xác định của bài toán là gì và đại lượng nào cần tìm trước tiên?";
                    break;

                case ScaffoldLevel.CalculationStep:
                    hintDto.LevelTitle = "Bậc 3: Hướng Dẫn Từng Phép Biến Đổi";
                    hintDto.HintContent = ExtractCalculationSteps(question, baseExplanation);
                    hintDto.SocraticQuestion = "Hãy thử thực hiện phép tính rút gọn ở bước này xem ra kết quả bao nhiêu?";
                    break;

                case ScaffoldLevel.FullWalkthrough:
                default:
                    hintDto.LevelTitle = "Bậc 4: Toàn Bộ Lời Giải Socratic & Mổ Xẻ Chi Tiết";
                    hintDto.HintContent = string.IsNullOrWhiteSpace(baseExplanation)
                        ? "Xem đáp án đúng là: " + (question.CorrectOption ?? "A")
                        : baseExplanation;
                    hintDto.SocraticQuestion = "Điểm bạn dễ nhầm lẫn nhất ở bài này là ở bước nào?";
                    break;
            }

            // Lưu vào Cache với TTL 24 giờ
            await _cache.SetAsync(cacheKey, hintDto, TimeSpan.FromHours(24), ct);

            return hintDto;
        }

        private static string ExtractConceptAnchor(QuestionBase q, string? explanation)
        {
            if (!string.IsNullOrWhiteSpace(explanation))
            {
                var lines = explanation.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0 && lines[0].Length > 10)
                {
                    return lines[0];
                }
            }

            return $"💡 **Mẹo nhận thức:** Xác định dạng toán của câu hỏi: {q.CategoryCode ?? "Toán học"}. Hãy liên hệ tới các công thức cơ bản và tính chất hình học/đại số liên quan.";
        }

        private static string ExtractMethodStrategy(QuestionBase q, string? explanation)
        {
            if (!string.IsNullOrWhiteSpace(explanation) && explanation.Contains("Bước 1"))
            {
                var idx = explanation.IndexOf("Bước 1", StringComparison.OrdinalIgnoreCase);
                var snippet = explanation.Substring(idx);
                var endIdx = snippet.IndexOf("Bước 2", StringComparison.OrdinalIgnoreCase);
                if (endIdx > 0)
                {
                    return snippet.Substring(0, endIdx).Trim();
                }
            }

            return "🎯 **Chiến lược:**\n1. Lập phương trình hoặc vẽ hình minh họa sơ bộ.\n2. Đặt ẩn phụ hoặc sử dụng phương pháp tọa độ hóa nếu là hình học không gian Oxyz.";
        }

        private static string ExtractCalculationSteps(QuestionBase q, string? explanation)
        {
            if (!string.IsNullOrWhiteSpace(explanation))
            {
                return explanation;
            }

            return "⚙️ **Phép biến đổi:** Thực hiện thay các tọa độ điểm vào phương trình để tìm nghiệm tối ưu.";
        }
    }
}
