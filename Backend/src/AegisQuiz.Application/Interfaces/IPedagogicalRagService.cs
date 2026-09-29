using System;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Epoch 5: CognitiveOS] Động cơ RAG sư phạm dẫn xuất giáo trình chuẩn (Curriculum-Grounded Pedagogical RAG).
    /// </summary>
    public interface IPedagogicalRagService
    {
        Task<PedagogicalRagResponseDto> GenerateCurriculumGroundedExplanationAsync(
            Guid questionId,
            string questionContent,
            string? studentAnswer,
            string domainCode = "EDUCATION",
            CancellationToken ct = default);
    }
}
