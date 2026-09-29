using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Epoch 5: CognitiveOS] Động cơ mạng nơ-ron mô phỏng tri thức sâu (Deep Knowledge Tracing Engine).
    /// </summary>
    public interface IDktEngine
    {
        Task<DktStateDto> GetStudentKnowledgeStateAsync(Guid userId, CancellationToken ct = default);
        Task<DktStateDto> RecordInteractionAndTraceAsync(Guid userId, string conceptCode, bool isCorrect, int responseTimeMs, CancellationToken ct = default);
        Task<double> PredictSuccessProbabilityAsync(Guid userId, string conceptCode, CancellationToken ct = default);
    }
}
