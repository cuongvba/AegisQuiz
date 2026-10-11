using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Symposium Secretariat Council]
    /// Hợp đồng Hội Đồng Thư Ký Hợp Nhất Đại Hội Thảo:
    /// Chịu trách nhiệm tuyển chọn danh sách các mô hình Thư Ký từ CSDL (xếp hạng ELO)
    /// và thực thi cơ chế chuyển tiếp dự phòng (Cascade Failover) khi mô hình chính gặp sự cố.
    /// </summary>
    public interface ISymposiumSecretariat
    {
        Task<SymposiumDialogueTurn> SynthesizeMetatheoryAsync(
            string topicTitle,
            string coreProblemStatement,
            SymposiumTeleologyMode teleologyMode,
            int roundNumber,
            string roundContextPrompt,
            EmpiricalEvidenceDossierDto? empiricalDossier,
            List<SymposiumDialogueTurn> dialogueLog,
            List<UniversalAiCouncil> participatingCouncils,
            List<CouncilAcademicContributionDto> councilContributions,
            Guid? userId = null,
            Guid? tenantId = null,
            string locale = "vi-VN",
            CancellationToken ct = default);
    }
}
