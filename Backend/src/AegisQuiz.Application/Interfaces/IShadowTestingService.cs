using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Phase 4] Shadow Testing Pipeline cho AI Rule Autopilot & CIG Evolution
    /// Chạy ngầm các quy tắc mới trên bộ dữ liệu kiểm chuẩn (Ground-truth & Regression Suite)
    /// trước khi tự động merge vào Production Rule Registry.
    /// </summary>
    public interface IShadowTestingService
    {
        /// <summary>
        /// Thực hiện Shadow Run đánh giá mức độ an toàn và tỷ lệ Regression của quy tắc đề xuất.
        /// </summary>
        Task<ShadowTestResultDto> RunShadowTestAsync(ShadowTestRequestDto request, CancellationToken ct = default);
    }
}
