using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Epoch 4: Community Ecosystem] Pipeline kiểm duyệt nội dung câu hỏi do cộng đồng đóng góp (AI Guardrails).
    /// </summary>
    public class ContentModerationService : IContentModerationService
    {
        private readonly ILogger<ContentModerationService> _logger;

        private static readonly HashSet<string> _bannedKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "lừa đảo", "hack", "cheat", "cờ bạc", "phản động", "vi phạm bản quyền"
        };

        public ContentModerationService(ILogger<ContentModerationService> logger)
        {
            _logger = logger;
        }

        public Task<ContentModerationResultDto> ModerateQuestionAsync(string content, string? explanation, CancellationToken ct = default)
        {
            var result = new ContentModerationResultDto
            {
                IsApproved = true,
                RiskScore = 0.05,
                FlaggedCategories = new List<string>(),
                QualityAssessment = "MEETS_PEDAGOGICAL_STANDARDS",
                RemediationSuggestions = new List<string>()
            };

            if (string.IsNullOrWhiteSpace(content) || content.Trim().Length < 10)
            {
                result.IsApproved = false;
                result.RiskScore = 0.9;
                result.FlaggedCategories.Add("INSUFFICIENT_CONTENT");
                result.RemediationSuggestions.Add("Nội dung câu hỏi quá ngắn, tối thiểu phải từ 10 ký tự trở lên.");
                return Task.FromResult(result);
            }

            // 1. Kiểm tra từ khóa nhạy cảm / độc hại
            foreach (var kw in _bannedKeywords)
            {
                if (content.Contains(kw, StringComparison.OrdinalIgnoreCase))
                {
                    result.IsApproved = false;
                    result.RiskScore = 0.95;
                    result.FlaggedCategories.Add("TOXIC_OR_PROHIBITED_KEYWORD");
                    result.RemediationSuggestions.Add($"Phát hiện từ khóa không phù hợp: '{kw}'. Vui lòng chỉnh sửa lại văn phong học thuật.");
                }
            }

            // 2. Kiểm tra cú pháp công thức toán học
            int openBrackets = Regex.Matches(content, @"\\\(|\\\[|\$").Count;
            if (openBrackets % 2 != 0)
            {
                result.RiskScore += 0.2;
                result.FlaggedCategories.Add("UNBALANCED_MATH_DELIMITER");
                result.RemediationSuggestions.Add("Ký hiệu công thức toán $...$ hoặc \\(...\\) có dấu mở mà chưa đóng. Cần kiểm tra lại KaTeX.");
            }

            // 3. Đánh giá lời giải thích chi tiết
            if (string.IsNullOrWhiteSpace(explanation) || explanation.Length < 15)
            {
                result.RemediationSuggestions.Add("Nên bổ sung thêm lời giải thích chi tiết hoặc định lý áp dụng để được duyệt nhanh hơn.");
            }

            _logger.LogInformation("[ContentModeration] Kiểm duyệt câu hỏi (Độ dài: {Len}): Approved={Approved}, Risk={Risk}",
                content.Length, result.IsApproved, result.RiskScore);

            return Task.FromResult(result);
        }
    }
}
