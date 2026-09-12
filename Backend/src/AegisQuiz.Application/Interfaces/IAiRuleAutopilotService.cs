using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// AI Autopilot Service tự động học định dạng đề thi lạ và suy diễn quy tắc CIG 4 chiều (Gemini 1.5 Flash + Local Heuristic Sniffer)
    /// </summary>
    public interface IAiRuleAutopilotService
    {
        Task<AiRuleAutopilotResponseDto> SynthesizeRuleAsync(AiRuleAutopilotRequestDto request);
    }
}
