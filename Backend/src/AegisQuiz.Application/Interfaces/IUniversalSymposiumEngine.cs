using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Sovereign] Nhạc trưởng Đại Hội Thảo Bách Khoa Vạn Vật Tri Thức
    /// Điều phối các Viện Hàn Lâm chuyên môn tranh biện 6 pha: Đặt vấn đề -> Vấn đáp -> Phản biện -> Hợp nhất -> Chấm điểm -> Tiến hóa.
    /// Hỗ trợ quản trị linh động Viện Hàn Lâm (OCP) và Kết nối Trực tiếp vào Hồ Sơ Tri Thức Hub (AegisNotebook).
    /// </summary>
    public interface IUniversalSymposiumEngine
    {
        Task<List<UniversalAiCouncil>> GetActiveCouncilsAsync(CancellationToken ct = default);
        Task<SymposiumProceedingsResult> ConveneSymposiumAsync(ConveneSymposiumRequest request, CancellationToken ct = default);
        Task<Guid> ExportSymposiumToNotebookAsync(Guid sessionId, Guid? targetNotebookId, Guid userId, Guid tenantId, CancellationToken ct = default);
        Task<UniversalAiCouncil> CreateOrUpdateCouncilAsync(UniversalAiCouncil council, CancellationToken ct = default);
        Task<bool> DeleteCouncilAsync(Guid councilId, CancellationToken ct = default);
        Task<UniversalCouncilSeat> AddCouncilSeatAsync(Guid councilId, UniversalCouncilSeat seat, CancellationToken ct = default);
        Task<bool> RemoveCouncilSeatAsync(Guid seatId, CancellationToken ct = default);
        Task<UniversalCouncilSeat?> UpdateCouncilSeatSwarmAsync(
            Guid seatId,
            string alphaModelCode,
            string swarmModelCodesJson,
            string swarmPolicy,
            string? personaPromptDirective = null,
            string? seatTitle = null,
            CouncilSeatRole? role = null,
            double? decisionWeight = null,
            CancellationToken ct = default);
    }
}
