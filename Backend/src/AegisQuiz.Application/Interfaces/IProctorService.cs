using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Enterprise Fortress — Epoch 3] Dịch vụ phát hiện và xử lý vi phạm giám sát khảo thí (ProctorSignal Engine).
    /// </summary>
    public interface IProctorService
    {
        /// <summary>
        /// Ghi nhận và phân tích sự kiện vi phạm giám sát từ thí sinh (Tab switch, Blur, Clipboard, Fast answer...).
        /// Tự động cập nhật số lần vi phạm, kích hoạt kiểm toán chuỗi băm bất biến và trả về chỉ thị xử lý.
        /// </summary>
        Task<ProctorSignalEventDto> ProcessProctorEventAsync(ProctorSignalEventDto evt, CancellationToken ct = default);

        /// <summary>
        /// Lấy thống kê tổng số vi phạm trong ngày trên toàn hệ thống.
        /// </summary>
        Task<int> GetTodayViolationCountAsync(CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách các sự kiện vi phạm gần nhất cho live feed.
        /// </summary>
        Task<List<ProctorSignalEventDto>> GetRecentViolationsFeedAsync(int limit = 20, CancellationToken ct = default);
    }
}
