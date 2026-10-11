using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Sovereign - OCP] Điều phối viên Bàn giao Nhận thức
    /// Tự động lựa chọn Chiến lược (ICognitiveHandoffStrategy) phù hợp nhất theo độ ưu tiên.
    /// </summary>
    public interface ICognitiveHandoffDispatcher
    {
        (string AdaptedSystemPrompt, string AdaptedUserPrompt) DispatchHandoff(
            ThinkingTraceContext trace, 
            CognitiveArchetype targetArchetype, 
            string baseSystemPrompt, 
            string baseUserPrompt);
    }
}
