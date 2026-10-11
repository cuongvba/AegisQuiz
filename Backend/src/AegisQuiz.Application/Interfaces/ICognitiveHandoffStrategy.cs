using System;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Sovereign - OCP Compliant]
    /// Chiến lược Bàn giao Nhận thức (Cognitive Handoff Strategy)
    /// Khi hệ thống buộc phải xoay sang mô hình khác hoặc archetype khác do cạn kiệt tài nguyên,
    /// Chiến lược này chịu trách nhiệm biên dịch và tái cấu trúc vết suy luận trung gian (Scratchpad),
    /// biến nó thành tiền đề vững chắc cho mô hình tiếp quản, triệt tiêu hoàn toàn sự "ngơ ngẩn" hay mất ngữ cảnh.
    /// </summary>
    public interface ICognitiveHandoffStrategy
    {
        int Priority { get; }
        bool CanHandle(CognitiveArchetype fromArchetype, CognitiveArchetype toArchetype);
        (string AdaptedSystemPrompt, string AdaptedUserPrompt) AdaptContext(
            ThinkingTraceContext trace, 
            CognitiveArchetype targetArchetype, 
            string baseSystemPrompt, 
            string baseUserPrompt);
    }
}
