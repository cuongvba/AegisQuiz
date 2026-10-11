using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Symposium Architecture] Bộ kiến tạo 5 Khối Tri Thức Cấu Trúc Toàn Diện (5-Pillar Outcome Builder).
    /// Bóc tách và kết tinh kết quả hội thảo thành Thực trạng [E#] -> Căn nguyên -> Tiến bộ -> Nan đề mở -> Giải pháp & Tiên đề.
    /// </summary>
    public interface ISymposiumOutcomeBuilder
    {
        /// <summary>
        /// Xây dựng cấu trúc kết quả tri thức toàn diện từ tiến trình biện chứng và hồ sơ thực nghiệm.
        /// </summary>
        Task<SymposiumStructuredOutcomeDto> BuildStructuredOutcomeAsync(
            SymposiumTeleologyMode mode,
            string topicTitle,
            string problemStatement,
            EmpiricalEvidenceDossierDto? dossier,
            List<SymposiumDialogueTurn> dialogueLog,
            List<CouncilAcademicContributionDto> contributions,
            List<string> citedEvidenceIds,
            bool hasRealTurns,
            Guid? userId,
            Guid? tenantId,
            CancellationToken ct = default);
    }
}
