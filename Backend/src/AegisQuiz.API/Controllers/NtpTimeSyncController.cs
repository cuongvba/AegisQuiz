using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [AegisGeoNet NTP Synchronization Controller]
    /// Cổng kết nối siêu tốc hỗ trợ thí sinh đo đạc độ trễ mạng và độ lệch đồng hồ máy chủ trước trận đấu Gameshow.
    /// </summary>
    [ApiController]
    [Route("api/v1/arena/time-sync")]
    public class NtpTimeSyncController : ControllerBase
    {
        private readonly INtpTimeSyncService _ntpService;

        public NtpTimeSyncController(INtpTimeSyncService ntpService)
        {
            _ntpService = ntpService;
        }

        /// <summary>
        /// Điểm chạm đồng bộ thời gian NTP 4-timestamps với độ trễ thấp nhất.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("ping")]
        public async Task<IActionResult> PingTime(
            [FromBody] NtpSyncRequestDto request,
            CancellationToken ct = default)
        {
            if (request == null)
                return BadRequest("Yêu cầu đồng bộ không hợp lệ.");

            var syncResult = await _ntpService.SynchronizeClockAsync(request, ct);
            return Ok(syncResult);
        }
    }
}
