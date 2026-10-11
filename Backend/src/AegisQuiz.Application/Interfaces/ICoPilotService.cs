using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Application.Interfaces
{
    public interface ICoPilotService
    {
        /// <summary>
        /// Xử lý yêu cầu tương tác Agentic Co-Pilot:
        /// - Tự động bóc tách Slash Command (/soan, /hoithao, /ma-tran, /kiem-dinh, /gia-su)
        /// - Phân giải ngữ cảnh Persona (Assessment Architect, Socratic Mentor, Governance & FinOps)
        /// - Điều phối Tool Calling qua UniversalAiRouter và xuất vết suy luận CoT + Artifact Card sống
        /// </summary>
        Task<CoPilotResponseDto> ProcessPromptAsync(CoPilotRequestDto request, CancellationToken ct = default);

        /// <summary>
        /// Danh sách Slash Commands được hỗ trợ kèm gợi ý thông minh
        /// </summary>
        Task<IReadOnlyList<CoPilotSlashCommandDto>> GetAvailableCommandsAsync(CoPilotPersona? persona = null, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh mục Model AI gom nhóm theo Nhà cung cấp (Provider),
        /// tự động phân giải nguồn khóa theo Thác lũ User -> Tenant -> System,
        /// và đề xuất thông minh mô hình tối ưu theo câu hỏi/chủ đề hiện tại.
        /// </summary>
        Task<IReadOnlyList<CoPilotModelGroupDto>> GetGroupedModelsAsync(
            string? prompt = null,
            System.Guid? userId = null,
            System.Guid? tenantId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Bảo toàn và tinh thể hóa tri thức cuộc hội thoại vào AegisNotebook,
        /// biến tri thức trao đổi thành tài sản sống tái sử dụng lâu dài cho toàn hệ thống.
        /// </summary>
        Task<ExportCoPilotToNotebookResponse> ExportToNotebookAsync(
            ExportCoPilotToNotebookRequest request,
            CancellationToken ct = default);
    }
}
