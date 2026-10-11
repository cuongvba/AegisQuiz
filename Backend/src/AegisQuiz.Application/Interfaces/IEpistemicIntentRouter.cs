using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Epistemic Intent Router - AIER v2.0]
    /// Định tuyến nhận thức đa chiều và phân giải bản thể học chủ đề.
    /// Giúp hệ thống thấu hiểu sâu sắc đề tài, vượt qua bẫy đa nghĩa từ vựng,
    /// tự động lựa chọn Viện Chủ Trì và Viện Đối Trọng Biện Chứng động,
    /// đồng thời nhận diện chuẩn xác khi nào xuất hiện Miền Tri Thức Mới cần Khởi Sinh Viện.
    /// </summary>
    public interface IEpistemicIntentRouter
    {
        Task<EpistemicRoutingIntent> RouteEpistemicIntentAsync(
            string topicTitle,
            string coreProblem,
            List<UniversalAiCouncil> existingCouncils,
            Guid? tenantId = null,
            CancellationToken ct = default);
    }
}
