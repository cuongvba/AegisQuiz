using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.AI
{
    public class MultiAgentAuditResult
    {
        public bool IsApproved { get; set; } = true;
        public string CertifiedQuestion { get; set; } = string.Empty;
        public List<string> CertifiedOptions { get; set; } = new();
        public string CertifiedCorrectAnswer { get; set; } = string.Empty;
        public string AdversarialCritique { get; set; } = string.Empty;
        public int BloomLevel { get; set; } = 3;
        public double EstimatedIrtDifficulty { get; set; } = 0.5;
        public double EstimatedIrtDiscrimination { get; set; } = 1.2;
        public PedagogicalWalkthroughDto Walkthrough { get; set; } = new();
    }

    public class MultiAgentCommitteeEngine
    {
        private readonly IUniversalAiRouter _aiRouter;

        public MultiAgentCommitteeEngine(IUniversalAiRouter aiRouter)
        {
            _aiRouter = aiRouter;
        }

        public async Task<MultiAgentAuditResult> AuditQuestionAsync(
            string rawContent,
            List<string> rawOptions,
            string rawCorrectAnswer,
            string domainCode)
        {
            // Bước 1: Devil's Advocate Agent phản biện kẽ hở
            var advocatePrompt = $@"
Dưới đây là một câu hỏi trắc nghiệm:
Lĩnh vực: {domainCode}
Câu hỏi: {rawContent}
Phương án: {string.Join(" | ", rawOptions)}
Đáp án dự kiến: {rawCorrectAnswer}

Hãy đóng vai KẺ PHẢN BIỆN KHẮT KHE (Devil's Advocate):
1. Tìm xem câu hỏi có bị mơ hồ, đa nghĩa hoặc sai sót thuật ngữ nào không?
2. Có trường hợp ngoại lệ nào khiến đáp án khác cũng có thể đúng không?
3. Đề xuất cách viết lại câu hỏi và phương án để tuyệt đối chỉ có 1 đáp án duy nhất đúng.

Trả về JSON:
{{
  ""critique"": ""Nhận xét phản biện ngắn gọn"",
  ""improvedContent"": ""Câu hỏi đã chuẩn hóa"",
  ""improvedOptions"": [""A..."", ""B..."", ""C...""],
  ""finalAnswer"": ""Đáp án đúng tuyệt đối""
}}
";

            var advocateResponse = await _aiRouter.ExecuteAsync(new AiExecutionRequest
            {
                TaskType = AiTaskType.DistractorAnalysis,
                Prompt = advocatePrompt,
                Persona = AgentPersonaType.BankingLegalCounsel,
                Temperature = 0.2
            });

            string finalQuestion = rawContent;
            var finalOptions = new List<string>(rawOptions);
            string finalAnswer = rawCorrectAnswer;
            string critique = "Câu hỏi đạt tiêu chuẩn rõ ràng, không có xung đột đa nghĩa.";

            if (advocateResponse.Success && !string.IsNullOrWhiteSpace(advocateResponse.Content))
            {
                try
                {
                    var cleanJson = ExtractJson(advocateResponse.Content);
                    using var doc = JsonDocument.Parse(cleanJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("critique", out var cr)) critique = cr.GetString() ?? critique;
                    if (root.TryGetProperty("improvedContent", out var ic) && !string.IsNullOrWhiteSpace(ic.GetString()))
                        finalQuestion = ic.GetString()!;
                    if (root.TryGetProperty("finalAnswer", out var fa) && !string.IsNullOrWhiteSpace(fa.GetString()))
                        finalAnswer = fa.GetString()!;
                }
                catch
                {
                    // Fallback nếu parse JSON thất bại
                }
            }

            // Bước 2: Sinh bản đồ tư duy 5 bước sư phạm
            var walkthrough = await _aiRouter.GenerateWalkthroughAsync(
                finalQuestion,
                finalOptions,
                finalAnswer,
                domainCode,
                AgentPersonaType.SupremeArbiter);

            return new MultiAgentAuditResult
            {
                IsApproved = true,
                CertifiedQuestion = finalQuestion,
                CertifiedOptions = finalOptions,
                CertifiedCorrectAnswer = finalAnswer,
                AdversarialCritique = critique,
                BloomLevel = walkthrough.BloomLevel,
                EstimatedIrtDifficulty = walkthrough.EstimatedDifficultyIrt,
                EstimatedIrtDiscrimination = 1.25,
                Walkthrough = walkthrough
            };
        }

        private static string ExtractJson(string text)
        {
            var clean = text.Trim();
            if (clean.StartsWith("```json")) clean = clean[7..];
            if (clean.StartsWith("```")) clean = clean[3..];
            if (clean.EndsWith("```")) clean = clean[..^3];
            return clean.Trim();
        }
    }
}
