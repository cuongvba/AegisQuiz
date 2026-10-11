using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Spatio-Temporal Role Allocation]
    /// Bộ Phân Vai Biện Chứng Động Theo Không - Thời Gian & Đánh Giá Tính Sẵn Sàng Hội Đồng (Quorum Readiness).
    /// </summary>
    public interface ISpatioTemporalRoleAllocator
    {
        /// <summary>
        /// Đánh giá tình trạng sẵn sàng hoạt động (Readiness & Quorum) của từng Viện Hàn Lâm
        /// dựa trên CSDL Model Registry, API Keys và BYOK đang có.
        /// </summary>
        Task EvaluateCouncilsReadinessAsync(List<UniversalAiCouncil> councils, Guid tenantId, CancellationToken ct = default);

        /// <summary>
        /// Phân vai động mô hình (Dynamic Spatio-Temporal Binding) cho từng ghế học giả của các Viện tham gia,
        /// dựa trên đặc thù văn hóa - không gian của đề tài và ELO/năng lực hiện thời của mô hình.
        /// </summary>
        Task AllocateCouncilRolesAsync(
            string topicTitle,
            string problemStatement,
            List<UniversalAiCouncil> participatingCouncils,
            Guid tenantId,
            CancellationToken ct = default);
    }
}
