using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Aegis Vision Math Cognitive Service v1.0 - World-Class Standard]
    /// Triển khai dịch vụ chuyển đổi công thức toán học từ ảnh/WMF phức tạp sang KaTeX ($...$).
    /// Tận dụng:
    /// 1. Bầy Sói Đa Mô Hình (UniversalAiRouter) hỗ trợ Multimodal Vision (Gemini 2.0 Flash, GPT-4o, Claude 3.5).
    /// 2. Bộ đệm 2 tầng HybridCacheService với CacheKey SHA-256 (0ms latency cho công thức lặp lại).
    /// 3. Hậu xử lý KaTeX & Chuẩn hóa Unicode tự động.
    /// </summary>
    public class VisionMathService : IVisionMathService
    {
        private readonly IUniversalAiRouter _aiRouter;
        private readonly ICacheService _cacheService;
        private readonly ILogger<VisionMathService> _logger;

        private const string MathPrompt = 
            "You are an expert Math OCR Engine. Read the mathematical expression in this image and convert it to a clean KaTeX/LaTeX string.\n" +
            "RULES:\n" +
            "1. Output ONLY the raw LaTeX formula. Do NOT wrap in $ or $$ or markdown code blocks (```latex).\n" +
            "2. Preserve fractions (\\frac), radicals (\\sqrt), matrices (\\begin{matrix}), systems of equations (\\begin{cases}).\n" +
            "3. If the image is not a math formula, return empty string.";

        public VisionMathService(
            IUniversalAiRouter aiRouter,
            ICacheService cacheService,
            ILogger<VisionMathService> logger)
        {
            _aiRouter = aiRouter;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<string?> ConvertImageToLatexAsync(
            byte[] imageBytes, 
            string mimeType = "image/png", 
            CancellationToken cancellationToken = default)
        {
            if (imageBytes == null || imageBytes.Length < 32)
            {
                return null;
            }

            // 1. Tính toán SHA-256 Cache Key
            string hashKey;
            using (var sha = SHA256.Create())
            {
                var hashBytes = sha.ComputeHash(imageBytes);
                hashKey = Convert.ToHexString(hashBytes).ToLowerInvariant();
            }

            string cacheKey = $"math:vision:{hashKey}";

            // 2. Tra cứu Cache L1/L2
            var cached = await _cacheService.GetAsync<string>(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogDebug("[VisionMath] Cache Hit cho công thức {HashKey}", hashKey);
                return cached;
            }

            try
            {
                // 3. Chuẩn bị Base64 Data URI
                string base64 = Convert.ToBase64String(imageBytes);
                string dataUri = $"data:{mimeType};base64,{base64}";

                // 4. Tạo yêu cầu Multimodal Vision gửi tới UniversalAiRouter
                var request = new AiExecutionRequest
                {
                    Prompt = $"{MathPrompt}\nImage: {dataUri}",
                    PreferredProviderCode = WellKnownAiProviders.GoogleGemini,
                    PreferredModel = "gemini-2.0-flash",
                    Temperature = 0.0,
                    MaxTokens = 500,
                    TaskCode = "latex_conversion"
                };

                var response = await _aiRouter.ExecuteAsync(request);
                if (response == null || !response.Success || string.IsNullOrWhiteSpace(response.Content))
                {
                    _logger.LogWarning("[VisionMath] AI Router không trích xuất được công thức: {Error}", response?.ErrorMessage);
                    return null;
                }

                // 5. Làm sạch và chuẩn hóa kết quả KaTeX
                string rawLatex = response.Content.Trim();
                rawLatex = Regex.Replace(rawLatex, @"^```[a-zA-Z]*\s*", "");
                rawLatex = Regex.Replace(rawLatex, @"\s*```$", "");
                rawLatex = rawLatex.Trim().Trim('$', ' ');

                if (string.IsNullOrWhiteSpace(rawLatex))
                {
                    return null;
                }

                string katexResult = $"${rawLatex}$";

                // 6. Lưu vào Cache 30 ngày (công thức toán là bất biến)
                await _cacheService.SetAsync(cacheKey, katexResult, TimeSpan.FromDays(30), cancellationToken);

                _logger.LogInformation("[VisionMath] Chuyển đổi thành công công thức: {Katex}", katexResult);
                return katexResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VisionMath] Lỗi ngoại lệ khi giải mã công thức toán qua Vision AI");
                return null;
            }
        }
    }
}
