using System;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Sovereign] Bộ nhớ làm việc nhận thức tạm thời (Ephemeral Cognitive Working Memory)
    /// Đảm bảo hệ thống duy trì tính liền mạch trong suy luận, chống lại chứng mất trí nhớ khi xoay khóa (CoT Amnesia).
    /// </summary>
    public interface ICognitiveWorkingMemory
    {
        Task SaveTraceAsync(ThinkingTraceContext trace, TimeSpan? ttl = null, CancellationToken ct = default);
        Task<ThinkingTraceContext?> GetTraceAsync(string sessionId, CancellationToken ct = default);
        Task AppendScratchpadAsync(string sessionId, string thoughtChunk, CancellationToken ct = default);
        Task RecordFailoverEventAsync(string sessionId, KeyFailoverEvent failoverEvent, CancellationToken ct = default);
        Task InvalidateTraceAsync(string sessionId, CancellationToken ct = default);
    }
}
