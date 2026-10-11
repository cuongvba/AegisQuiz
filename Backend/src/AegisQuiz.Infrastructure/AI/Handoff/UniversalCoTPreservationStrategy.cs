using System;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Infrastructure.AI.Handoff
{
    /// <summary>
    /// [AegisAI Sovereign - OCP]
    /// Chiến lược Bàn giao Nhận thức Phổ quát (Universal CoT Preservation)
    /// Đóng vai trò là Fallback Strategy cho mọi cặp chuyển đổi Archetype,
    /// bảo toàn nguyên vẹn dòng suy luận vào Working Memory của prompt mới.
    /// </summary>
    public class UniversalCoTPreservationStrategy : ICognitiveHandoffStrategy
    {
        public int Priority => 10;

        public bool CanHandle(CognitiveArchetype fromArchetype, CognitiveArchetype toArchetype)
        {
            return true; // Xử lý mọi trường hợp mặc định
        }

        public (string AdaptedSystemPrompt, string AdaptedUserPrompt) AdaptContext(
            ThinkingTraceContext trace, 
            CognitiveArchetype targetArchetype, 
            string baseSystemPrompt, 
            string baseUserPrompt)
        {
            if (string.IsNullOrWhiteSpace(trace.AccumulatedScratchpad))
            {
                return (baseSystemPrompt, baseUserPrompt);
            }

            var snippet = trace.AccumulatedScratchpad;
            if (snippet.Length > 2500)
            {
                snippet = "..." + snippet[^2500..];
            }

            var adaptedSystem = $@"{baseSystemPrompt}

=== [BẢNG GIAO TRẠNG THÁI NHẬN THỨC (COGNITIVE WORKING MEMORY)] ===
Chuỗi suy luận trước đó từ phiên làm việc ({trace.OriginArchetype}):
{snippet}
Hãy tiếp tục hoàn thiện nhiệm vụ dựa trên ngữ cảnh suy luận này.";

            return (adaptedSystem, baseUserPrompt);
        }
    }
}
