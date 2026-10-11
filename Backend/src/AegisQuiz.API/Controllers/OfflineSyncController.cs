using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [AegisShield Offline Synchronization Controller]
    /// Tiếp nhận dữ liệu bài thi đồng bộ từ ứng dụng Client/PWA sau khi kết nối mạng được khôi phục.
    /// </summary>
    [ApiController]
    [Route("api/v1/exams/offline-sync")]
    public class OfflineSyncController : ControllerBase
    {
        private readonly IOfflineSyncService _syncService;

        public OfflineSyncController(IOfflineSyncService syncService)
        {
            _syncService = syncService;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SyncExamData(
            [FromBody] OfflineSyncRequestDto request,
            CancellationToken ct = default)
        {
            var result = await _syncService.ProcessOfflineSyncAsync(request, ct);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
