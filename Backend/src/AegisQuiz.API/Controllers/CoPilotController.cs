using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [Aegis Sovereign Co-Pilot Studio - Antigravity Agentic Sidecar]
    /// Bộ điều phối trung tâm cho giao diện Agentic Co-Pilot Sidecar:
    /// - Nhận thức sâu ngữ cảnh màn hình và vai trò người dùng (Tri-Persona)
    /// - Điều phối Slash Commands (/soan, /hoithao, /ma-tran, /gia-su, /kiem-dinh)
    /// - Xuất vết suy luận ngầm (Thought Trace), Action Pills và Interactive Artifacts
    /// </summary>
    [ApiController]
    [Route("api/v1/copilot")]
    public class CoPilotController : ControllerBase
    {
        private readonly ICoPilotService _coPilotService;
        private readonly IUniversalAiRouter _aiRouter;
        private readonly ITenantContext _tenantContext;

        public CoPilotController(
            ICoPilotService coPilotService,
            IUniversalAiRouter aiRouter,
            ITenantContext tenantContext)
        {
            _coPilotService = coPilotService;
            _aiRouter = aiRouter;
            _tenantContext = tenantContext;
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(claim, out var uid) && uid != Guid.Empty)
                return uid;

            return Guid.Parse("00000000-0000-0000-0000-000000000001");
        }

        private Guid GetCurrentTenantId()
        {
            return _tenantContext.CurrentTenantId != Guid.Empty
                ? _tenantContext.CurrentTenantId
                : Guid.Empty;
        }

        /// <summary>
        /// Gửi yêu cầu đàm thoại hoặc lệnh Slash Command tới Aegis Co-Pilot
        /// </summary>
        [AllowAnonymous]
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] CoPilotRequestDto request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Nội dung yêu cầu (Message) không được để trống." });
            }

            request.UserId ??= GetCurrentUserId();
            request.TenantId ??= GetCurrentTenantId();

            var response = await _coPilotService.ProcessPromptAsync(request, ct);
            return Ok(response);
        }

        /// <summary>
        /// Lấy danh mục Slash Commands khả dụng kèm mô tả và ví dụ
        /// </summary>
        [AllowAnonymous]
        [HttpGet("commands")]
        public async Task<IActionResult> GetCommands([FromQuery] CoPilotPersona? persona = null, CancellationToken ct = default)
        {
            var commands = await _coPilotService.GetAvailableCommandsAsync(persona, ct);
            return Ok(commands);
        }

        /// <summary>
        /// Lấy danh sách các Model AI đang kích hoạt trong hệ thống từ UniversalAiRouter
        /// </summary>
        [AllowAnonymous]
        [HttpGet("models")]
        public async Task<IActionResult> GetActiveModels(CancellationToken ct = default)
        {
            try
            {
                var models = await _aiRouter.GetActiveNormalizedModelsAsync(null, ct);
                return Ok(models);
            }
            catch
            {
                // Fallback nếu chưa khởi tạo database đầy đủ
                var fallbackList = new[]
                {
                    new { ModelCode = "claude-3-5-sonnet", DisplayName = "Claude 3.5 Sonnet (Chuyên gia Khảo thí)", Archetype = "DeepReasoner" },
                    new { ModelCode = "gemini-2-5-flash", DisplayName = "Gemini 2.5 Flash (Siêu tốc 0ms)", Archetype = "UltraFast" },
                    new { ModelCode = "gpt-4o", DisplayName = "GPT-4o (Đa phương thức)", Archetype = "MassiveContext" },
                    new { ModelCode = "deepseek-r1-local", DisplayName = "DeepSeek-R1 (Toán học & Logic)", Archetype = "DeepReasoner" }
                };
                return Ok(fallbackList);
            }
        }

        /// <summary>
        /// Lấy danh mục các Model AI gom nhóm theo Nhà cung cấp (Provider),
        /// tự động phân giải nguồn khóa (User -> Tenant -> System)
        /// và đánh dấu Model khuyên dùng thông minh theo câu hỏi/chủ đề hiện tại.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("models/grouped")]
        public async Task<IActionResult> GetGroupedModels([FromQuery] string? prompt = null, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            var tenantId = GetCurrentTenantId();
            var groups = await _coPilotService.GetGroupedModelsAsync(prompt, userId, tenantId, ct);
            return Ok(groups);
        }

        /// <summary>
        /// Bảo toàn và kết tinh tri thức từ hội thoại hoặc Artifact vào AegisNotebook
        /// </summary>
        [AllowAnonymous]
        [HttpPost("export-notebook")]
        public async Task<IActionResult> ExportToNotebook(
            [FromBody] ExportCoPilotToNotebookRequest request,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Content) && request.ArtifactData == null)
            {
                return BadRequest(new { message = "Nội dung cần lưu vào Notebook không được để trống." });
            }

            request.UserId ??= GetCurrentUserId();
            request.TenantId ??= GetCurrentTenantId();

            var result = await _coPilotService.ExportToNotebookAsync(request, ct);
            return Ok(result);
        }
    }
}
