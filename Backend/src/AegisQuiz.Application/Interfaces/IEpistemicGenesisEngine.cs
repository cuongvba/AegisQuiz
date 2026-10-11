using System;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Epistemic Genesis Engine - OCP Supreme Protocol]
    /// Động cơ Khởi Sinh Viện Hàn Lâm Tự Động của Hội Đồng Đại Kiến Trúc Sư Tối Cao (Tầng 0).
    /// Khi gặp một vùng tri thức hoàn toàn mới lạ chưa có Hội đồng chuyên trách,
    /// động cơ này tự động thiết kế, phân vai ghế phi đối xứng, xác lập tuyên ngôn nhận thức luận
    /// và khắc ghi vĩnh cửu vào CSDL mà không cần con người can thiệp hay sửa mã nguồn.
    /// </summary>
    public interface IEpistemicGenesisEngine
    {
        Task<UniversalAiCouncil?> GenesisCouncilForTopicAsync(
            string topicTitle,
            string coreProblemStatement,
            AegisQuiz.Application.Models.AI.SuggestedGenesisMetadata? suggestedGenesis = null,
            Guid? tenantId = null,
            CancellationToken ct = default);
    }
}
