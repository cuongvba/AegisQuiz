using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Council Selection Strategy - OCP Compliant]
    /// Chiến lược lựa chọn và sắp xếp các Viện Hàn Lâm tham gia Đại Hội Thảo.
    /// Cho phép mở rộng thuật toán (Semantic, Taxonomy, Embedding, LLM Router) mà không sửa core engine.
    /// </summary>
    public interface ICouncilSelectionStrategy
    {
        Task<List<UniversalAiCouncil>> SelectCouncilsAsync(
            CouncilSelectionContext context,
            List<UniversalAiCouncil> availableCouncils,
            CancellationToken ct = default);
    }

    public class CouncilSelectionContext
    {
        public string TopicTitle { get; set; } = string.Empty;
        public string CoreProblemStatement { get; set; } = string.Empty;
        public List<string>? RequestedCouncilCodes { get; set; }
        public int MinCouncils { get; set; } = 2;
        public int MaxCouncils { get; set; } = 4;
        public bool IncludeContrarianPerspective { get; set; } = true;
        public bool AllowDynamicGenesis { get; set; } = true;
        public Guid? TenantId { get; set; }
    }
}
