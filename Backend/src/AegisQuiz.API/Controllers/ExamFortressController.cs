using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [Enterprise Fortress — Epoch 3] Bộ điều khiển Pháo đài Khảo thí Doanh nghiệp.
    /// Cung cấp endpoints nạp sẵn đề thi (Pre-warm Cache), phân phát đề thi độ trễ thấp (<5ms),
    /// và kiểm toán chuỗi băm bất biến (Cryptographic Audit Verification).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ExamFortressController : ControllerBase
    {
        private readonly IExamFortressPrewarmService _prewarmService;
        private readonly IExamAuditService _auditService;
        private readonly ILogger<ExamFortressController> _logger;

        public ExamFortressController(
            IExamFortressPrewarmService prewarmService,
            IExamAuditService auditService,
            ILogger<ExamFortressController> logger)
        {
            _prewarmService = prewarmService;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Kích hoạt nạp sẵn dữ liệu đề thi vào L1/L2 Redis trước giờ thi (Ban tổ chức / SysAdmin gọi lúc T-15 phút).
        /// </summary>
        [HttpPost("prewarm/{examId}")]
        public async Task<IActionResult> PrewarmExam([FromRoute] Guid examId, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var package = await _prewarmService.PrewarmExamAsync(examId, ct);
            sw.Stop();

            return Ok(new
            {
                success = true,
                message = $"Đã nạp sẵn thành công kỳ thi vào bộ đệm lai L1+L2 trong {sw.ElapsedMilliseconds}ms.",
                examId = package.ExamId,
                title = package.Title,
                totalQuestions = package.TotalQuestions,
                checksum = package.ChecksumSha256,
                prewarmedAt = package.PrewarmedAtUtc,
                latencyMs = sw.ElapsedMilliseconds
            });
        }

        /// <summary>
        /// Phân phát gói đề thi nạp sẵn với độ trễ cực thấp (< 5ms) cho thí sinh bước vào phòng thi.
        /// </summary>
        [HttpGet("package/{examId}")]
        public async Task<IActionResult> GetExamPackage([FromRoute] Guid examId, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var package = await _prewarmService.GetPrewarmedExamPackageAsync(examId, ct);
            sw.Stop();

            if (package == null)
            {
                return NotFound(new { error = "Không tìm thấy gói đề thi nạp sẵn cho kỳ thi này." });
            }

            Response.Headers.Append("X-Fortress-Dispatch-Time-Ms", sw.ElapsedMilliseconds.ToString());
            Response.Headers.Append("X-Fortress-Cache-Hit", "True");

            return Ok(new
            {
                success = true,
                dispatchLatencyMs = sw.ElapsedMilliseconds,
                package
            });
        }

        /// <summary>
        /// Kiểm tra trạng thái nạp sẵn của kỳ thi.
        /// </summary>
        [HttpGet("status/{examId}")]
        public async Task<IActionResult> GetPrewarmStatus([FromRoute] Guid examId, CancellationToken ct)
        {
            bool isPrewarmed = await _prewarmService.IsPrewarmedAsync(examId, ct);
            return Ok(new { examId, isPrewarmed });
        }

        /// <summary>
        /// Hủy bộ đệm nạp sẵn (khi cần sửa đề thi trước giờ thi).
        /// </summary>
        [HttpDelete("prewarm/{examId}")]
        public async Task<IActionResult> InvalidatePrewarm([FromRoute] Guid examId, CancellationToken ct)
        {
            await _prewarmService.InvalidatePrewarmAsync(examId, ct);
            return Ok(new { success = true, message = "Đã giải phóng bộ đệm nạp sẵn." });
        }

        /// <summary>
        /// Xác minh tính toàn vẹn chuỗi băm kiểm toán mật mã học (Cryptographic Audit Trail Verification).
        /// Trả về kết quả IsChainValid: true nếu toàn bộ dữ liệu kỳ thi nguyên vẹn 100%, không bị sửa/xóa/chèn.
        /// </summary>
        [HttpGet("audit/verify/{examId}")]
        public async Task<IActionResult> VerifyAuditIntegrity([FromRoute] Guid examId, CancellationToken ct)
        {
            var result = await _auditService.VerifyAuditIntegrityAsync(examId, ct);
            return Ok(result);
        }

        /// <summary>
        /// Lấy nhật ký kiểm toán bất biến của kỳ thi.
        /// </summary>
        [HttpGet("audit/logs/{examId}")]
        public async Task<IActionResult> GetAuditLogs([FromRoute] Guid examId, [FromQuery] Guid? userId, [FromQuery] int limit = 100, CancellationToken ct = default)
        {
            var logs = await _auditService.GetAuditTrailAsync(examId, userId, limit, ct);
            return Ok(new
            {
                examId,
                totalLogs = logs.Count,
                logs
            });
        }
    }
}
