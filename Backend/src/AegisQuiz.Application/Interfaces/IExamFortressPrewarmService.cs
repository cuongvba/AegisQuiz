using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Enterprise Fortress — Epoch 3] Dịch vụ nạp sẵn dữ liệu kỳ thi vào tầng nhớ Cache đa tầng (L1 Memory + L2 Redis).
    /// Đảm bảo phân phát đề thi cho 50,000+ thí sinh đồng thời với độ trễ nano/micro-second (< 5ms).
    /// </summary>
    public interface IExamFortressPrewarmService
    {
        /// <summary>
        /// Nạp sẵn toàn bộ đề thi, câu hỏi, tài nguyên SVG và cấu hình kiểm toán vào bộ đệm lai.
        /// </summary>
        Task<ExamPrewarmPackageDto> PrewarmExamAsync(Guid examId, CancellationToken ct = default);

        /// <summary>
        /// Truy xuất gói đề thi nạp sẵn với độ trễ cực thấp (Sub-5ms latency).
        /// </summary>
        Task<ExamPrewarmPackageDto?> GetPrewarmedExamPackageAsync(Guid examId, CancellationToken ct = default);

        /// <summary>
        /// Kiểm tra xem kỳ thi đã được nạp sẵn trong bộ nhớ hay chưa.
        /// </summary>
        Task<bool> IsPrewarmedAsync(Guid examId, CancellationToken ct = default);

        /// <summary>
        /// Hủy bộ đệm nạp sẵn (khi ban tổ chức chỉnh sửa đề thi trước giờ G).
        /// </summary>
        Task<bool> InvalidatePrewarmAsync(Guid examId, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách ID các kỳ thi đang được nạp sẵn trong bộ đệm.
        /// </summary>
        Task<List<Guid>> GetPrewarmedExamIdsAsync(CancellationToken ct = default);
    }
}
