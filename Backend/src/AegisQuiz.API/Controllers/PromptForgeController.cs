using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [AegisAI Cognitive Prompt Forge Studio API]
    /// Cung cấp bộ công cụ tối cao quản trị Prompt phân tầng:
    /// 1. Đề xuất sinh Prompt Tầng 0 (Meta Architect)
    /// 2. Khởi sinh Thác đổ (Cascading 6 Layers)
    /// 3. Kiểm định tính nhất quán Tiên đề (Axiomatic Audit)
    /// 4. Phê chuẩn và Khắc ghi (Enact & Hot-Reload 0ms)
    /// </summary>
    [ApiController]
    [Route("api/ai/symposium/prompts")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.TenantAdmin},Admin")]
    public class PromptForgeController : ControllerBase
    {
        private readonly IPromptForgeAssistantService _forgeService;
        private readonly ITenantContext _tenantContext;

        public PromptForgeController(
            IPromptForgeAssistantService forgeService,
            ITenantContext tenantContext)
        {
            _forgeService = forgeService;
            _tenantContext = tenantContext;
        }

        private Guid? GetTenantId()
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (tenantId != Guid.Empty) return tenantId;

            var claim = User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(claim, out var tid)) return tid;

            return null;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách System Prompt Templates phân tầng trong CSDL
        /// </summary>
        [HttpGet("templates")]
        public async Task<IActionResult> GetAllTemplates(
            [FromQuery] string locale = "vi-VN",
            CancellationToken ct = default)
        {
            var tenantId = GetTenantId();
            var templates = await _forgeService.GetAllTemplatesAsync(tenantId, locale, ct);
            return Ok(templates);
        }

        /// <summary>
        /// Trợ lý AI đề xuất sinh Prompt Tầng 0 (Meta Architect) dựa trên ngành nghề & chuẩn biện chứng
        /// </summary>
        [HttpPost("propose-meta")]
        public async Task<IActionResult> ProposeMetaPrompt(
            [FromBody] PromptProposalRequest request,
            CancellationToken ct = default)
        {
            if (request == null)
                return BadRequest(new { message = "Yêu cầu đề xuất prompt không hợp lệ." });

            request.TenantId ??= GetTenantId();
            var proposal = await _forgeService.ProposeMetaPromptAsync(request, ct);
            return Ok(proposal);
        }

        /// <summary>
        /// Khởi sinh Thác đổ (Cascading): Tự động sinh đồng bộ các Tầng 1..5 từ Tầng 0
        /// </summary>
        [HttpPost("generate-stack")]
        public async Task<IActionResult> GenerateCascadingStack(
            [FromBody] GenerateStackRequest request,
            CancellationToken ct = default)
        {
            if (request == null)
                return BadRequest(new { message = "Yêu cầu sinh stack prompt không hợp lệ." });

            request.TenantId ??= GetTenantId();
            var stack = await _forgeService.GenerateCascadingStackAsync(request, ct);
            return Ok(stack);
        }

        /// <summary>
        /// Thanh tra tính nhất quán Tiên đề (Axiomatic Consistency Audit) giữa các tầng
        /// </summary>
        [HttpGet("audit-consistency")]
        public async Task<IActionResult> AuditConsistency(
            [FromQuery] string locale = "vi-VN",
            CancellationToken ct = default)
        {
            var tenantId = GetTenantId();
            var audit = await _forgeService.AuditConsistencyAsync(tenantId, locale, ct);
            return Ok(audit);
        }

        /// <summary>
        /// Phê chuẩn và Khắc ghi bộ Prompt vào CSDL PostgreSQL kèm 0ms Hot-Reload
        /// </summary>
        [HttpPost("enact-stack")]
        public async Task<IActionResult> EnactPromptStack(
            [FromBody] EnactPromptStackRequest request,
            CancellationToken ct = default)
        {
            if (request == null)
                return BadRequest(new { message = "Dữ liệu phê chuẩn không hợp lệ." });

            request.TenantId ??= GetTenantId();
            var success = await _forgeService.EnactPromptStackAsync(request, ct);
            return Ok(new
            {
                success,
                message = "Đã phê chuẩn, lưu phiên bản mới và kích hoạt 0ms Hot-Reload thành công trên toàn hệ sinh thái!"
            });
        }
    }
}
