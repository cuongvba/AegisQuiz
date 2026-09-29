using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Enterprise Fortress — Epoch 3] Dịch vụ kiểm toán bất biến với cơ chế xâu chuỗi băm mật mã học (Cryptographic Hash Chaining).
    /// </summary>
    public interface IExamAuditService
    {
        /// <summary>
        /// Ghi nhận một sự kiện kiểm toán mới vào chuỗi băm của kỳ thi (tự động tính PreviousLogHash và CurrentHash).
        /// </summary>
        Task<ExamAuditLog> AppendAuditLogAsync(
            Guid tenantId,
            Guid examId,
            Guid userId,
            string action,
            string severity = "INFO",
            string details = "{}",
            string clientIp = "127.0.0.1",
            string userAgent = "",
            CancellationToken ct = default);

        /// <summary>
        /// Duyệt toàn bộ chuỗi băm của kỳ thi từ Genesis Block đến bản ghi mới nhất để kiểm tra tính toàn vẹn.
        /// Phát hiện chính xác nếu bất kỳ ai sửa đổi, chèn hoặc xóa bản ghi trực tiếp trong cơ sở dữ liệu.
        /// </summary>
        Task<AuditTrailVerificationResultDto> VerifyAuditIntegrityAsync(Guid examId, CancellationToken ct = default);

        /// <summary>
        /// Lấy lịch sử kiểm toán của kỳ thi hoặc thí sinh cụ thể.
        /// </summary>
        Task<List<ExamAuditLog>> GetAuditTrailAsync(Guid examId, Guid? userId = null, int limit = 100, CancellationToken ct = default);
    }
}
