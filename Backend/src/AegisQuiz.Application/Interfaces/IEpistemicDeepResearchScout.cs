using System;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Epistemic Deep Research Scout]
    /// Hợp đồng điều tra, trinh sát thực địa và tổng hợp Bằng chứng Thực nghiệm (Empirical Evidence):
    /// 1. Tự động quét và tổng hợp dữ liệu thực tế, số liệu thống kê, bài báo khoa học.
    /// 2. Soạn thảo Hồ sơ Bằng chứng Thực nghiệm (Empirical Dossier) trước khi Đại Hội Thảo bước vào Pha 1.
    /// 3. Tiếp sức cho Ghế Thư Ký Dữ Liệu & Thực Nghiệm (Empirical Auditor) phản bác ảo giác và neo giữ chân lý.
    /// </summary>
    public interface IEpistemicDeepResearchScout
    {
        Task<EmpiricalEvidenceDossierDto> ConductPreSymposiumInvestigationAsync(
            string topicTitle,
            string coreProblemStatement,
            CancellationToken ct = default);
    }
}
