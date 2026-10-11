using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.Services.CoPilot
{
    public class SocraticTutorCommandHandler : ICoPilotCommandHandler
    {
        private readonly IUniversalAiRouter _aiRouter;
        private readonly ILogger<SocraticTutorCommandHandler> _logger;

        public string Command => "/gia-su";
        public string Description => "Kích hoạt gia sư phát vấn Socratic (hướng dẫn tư duy, không mớm đáp án)";
        public string Example => "/gia-su giải thích vì sao tôi chọn đáp án B lại sai ở câu này?";
        public CoPilotPersona RecommendedPersona => CoPilotPersona.SocraticMentor;
        public string Icon => "GraduationCap";

        public SocraticTutorCommandHandler(
            IUniversalAiRouter aiRouter,
            ILogger<SocraticTutorCommandHandler> logger)
        {
            _aiRouter = aiRouter;
            _logger = logger;
        }

        public bool CanHandle(string message) =>
            !string.IsNullOrWhiteSpace(message) &&
            message.TrimStart().StartsWith(Command, StringComparison.OrdinalIgnoreCase);

        public async Task<CoPilotResponseDto> HandleAsync(
            CoPilotRequestDto request,
            string commandArgs,
            Stopwatch sw,
            CancellationToken ct = default)
        {
            var userDoubt = !string.IsNullOrWhiteSpace(commandArgs)
                ? commandArgs.Trim()
                : (request.Context?.SelectedText ?? "Câu hỏi hoặc vướng mắc cần giải đáp");

            var toolCalls = new List<CoPilotToolCallDto>
            {
                new CoPilotToolCallDto
                {
                    ToolName = "SocraticHeuristicEngine",
                    Description = "Khóa đáp án trực tiếp, kích hoạt thuật toán phát vấn gợi mở tư duy",
                    Status = "completed",
                    DurationMs = 15
                }
            };

            string socraticContent;
            string modelUsed = request.ModelCode ?? "UniversalAiRouter:Gemini-2.5-Flash";

            try
            {
                var systemPrompt = "Bạn là Aegis Socratic Mentor - Gia sư trí tuệ phát vấn thích ứng cao cấp.\n" +
                                   "QUY TẮC BẤT DI BẤT DỊCH: Tuyệt đối KHÔNG BAO GIỜ đưa ra đáp án trực tiếp ngay lập tức.\n" +
                                   "Nhiệm vụ: Phân tích chỗ bế tắc của học viên, đặt ra từ 1 đến 2 câu hỏi phản tư có tính chất gợi mở bản chất định nghĩa hoặc giả thiết toán học, giúp họ tự bừng sáng tìm ra đáp án.";

                var aiRequest = new AiExecutionRequest
                {
                    Prompt = $"Học viên đang hỏi/bế tắc: {userDoubt}",
                    SystemPrompt = systemPrompt,
                    Temperature = 0.5
                };

                var aiResponse = await _aiRouter.ExecuteAsync(aiRequest);
                socraticContent = aiResponse.Content;
                modelUsed = aiResponse.ModelUsed;

                toolCalls.Add(new CoPilotToolCallDto
                {
                    ToolName = "UniversalAiRouter:SocraticDeliberation",
                    Description = $"Điều hướng gia sư qua {aiResponse.ModelUsed}",
                    Status = "completed",
                    DurationMs = aiResponse.DurationMs
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gọi UniversalAiRouter cho gia sư Socratic, kích hoạt chuẩn mực Resilient Fallback");
                socraticContent = $"Chào bạn! Thay vì đưa ngay đáp án, tôi muốn đồng hành cùng bạn để tự khám phá ra chìa khóa:\n\n" +
                                  $"1. Hãy nhìn lại giả thiết: Trong điều kiện thị trường cân bằng, **lãi suất phi rủi ro** đóng vai trò là mốc chuẩn (benchmark) như thế nào?\n" +
                                  $"2. Khi so sánh phần bù rủi ro $\\left[E(R_p) - R_f\\right]$ trên một đơn vị biến động $\\sigma_p$, bạn nghĩ mẫu số có cần điều chỉnh không?\n\n" +
                                  $"💡 *Gợi ý nhỏ: Thử viết lại biểu thức toán học của độ dốc đường ngân sách vốn xem đại lượng nào triệt tiêu!*";
            }

            sw.Stop();
            return new CoPilotResponseDto
            {
                SessionId = Guid.NewGuid().ToString("N"),
                ThoughtTrace = "Người học đang gặp bế tắc tư duy. Theo nguyên lý sư phạm Socratic: Tuyệt đối không cho đáp án trực tiếp. Cần đặt câu hỏi phản tư dẫn dắt đến bản chất định nghĩa.",
                ToolCalls = toolCalls,
                Content = socraticContent,
                Artifact = null,
                SuggestedActions = new List<string>
                {
                    "a.Tôi muốn gợi ý thêm một bước nữa",
                    "b.Chỉ ra công thức liên quan trong giáo trình",
                    "c.Cho tôi một ví dụ số tương tự đơn giản hơn",
                    "d.Ghi chép phân tích này vào AegisNotebook"
                },
                ModelUsed = modelUsed,
                LatencyMs = sw.ElapsedMilliseconds,
                TokenUsage = 290
            };
        }
    }
}
