using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Cognitive Prompt Forge Assistant]
    /// Dịch vụ Trợ lý Kiến trúc sư Nhận thức Luận:
    /// 1. Tự động đề xuất nội dung Prompt Tầng 0 dựa trên hồ sơ ngành nghề và mức độ khắt khe biện chứng.
    /// 2. Khởi sinh Thác đổ (Cascading): Từ Tầng 0 tự động sinh và cân chỉnh các Tầng 1 (Constitution), 2 (Charters), 3 (Seats), 4 (Phases), 5 (Output).
    /// 3. Thanh tra tính nhất quán Tiên đề (Axiomatic Consistency Audit) giữa các tầng để phát hiện mâu thuẫn nhận thức.
    /// 4. Phê chuẩn và Khắc ghi (Enact & Hot-Reload) vào CSDL PostgreSQL với 0ms gián đoạn.
    /// </summary>
    public interface IPromptForgeAssistantService
    {
        Task<MetaPromptProposalDto> ProposeMetaPromptAsync(PromptProposalRequest request, CancellationToken ct = default);

        Task<CascadingPromptStackDto> GenerateCascadingStackAsync(GenerateStackRequest request, CancellationToken ct = default);

        Task<AxiomaticAuditReportDto> AuditConsistencyAsync(Guid? tenantId, string locale = "vi-VN", CancellationToken ct = default);

        Task<bool> EnactPromptStackAsync(EnactPromptStackRequest request, CancellationToken ct = default);

        Task<List<PromptTemplateSummaryDto>> GetAllTemplatesAsync(Guid? tenantId, string locale = "vi-VN", CancellationToken ct = default);
    }
}
