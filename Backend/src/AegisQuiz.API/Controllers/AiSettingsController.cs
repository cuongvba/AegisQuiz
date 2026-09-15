using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Infrastructure.AI;

namespace AegisQuiz.API.Controllers
{
    [ApiController]
    [Route("api/admin/ai")]
    public class AiSettingsController : ControllerBase
    {
        private readonly IUniversalAiRouter _aiRouter;
        private readonly MultiAgentCommitteeEngine _committeeEngine;

        public AiSettingsController(
            IUniversalAiRouter aiRouter,
            MultiAgentCommitteeEngine committeeEngine)
        {
            _aiRouter = aiRouter;
            _committeeEngine = committeeEngine;
        }

        // GET api/admin/ai/config
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            try
            {
                var config = _aiRouter.GetSystemConfig();
                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy cấu hình AI: {ex.Message}" });
            }
        }

        // POST api/admin/ai/config
        [HttpPost("config")]
        public IActionResult UpdateConfig([FromBody] AiSystemConfigDto newConfig)
        {
            try
            {
                if (newConfig == null) return BadRequest(new { message = "Dữ liệu cấu hình không hợp lệ." });
                _aiRouter.UpdateSystemConfig(newConfig);
                return Ok(new { message = "Đã cập nhật cấu hình hệ sinh thái AI thành công!", config = _aiRouter.GetSystemConfig() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi cập nhật cấu hình AI: {ex.Message}" });
            }
        }

        // POST api/admin/ai/test
        [HttpPost("test")]
        public async Task<IActionResult> TestProviders()
        {
            try
            {
                var results = await _aiRouter.TestProvidersAsync();
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi kiểm thử nhà cung cấp AI: {ex.Message}" });
            }
        }

        // POST api/admin/ai/tenant-byok
        [HttpPost("tenant-byok")]
        public IActionResult RegisterTenantByok([FromBody] TenantByokConfigDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.TenantId) || string.IsNullOrWhiteSpace(dto.ApiKey))
                {
                    return BadRequest(new { message = "TenantId và ApiKey là bắt buộc." });
                }

                _aiRouter.RegisterTenantByok(dto);
                return Ok(new { message = $"Đã cấu hình BYOK thành công cho Khách thuê {dto.TenantId}!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi cấu hình BYOK: {ex.Message}" });
            }
        }

        // POST api/admin/ai/walkthrough
        [HttpPost("walkthrough")]
        public async Task<IActionResult> GenerateWalkthrough([FromBody] WalkthroughRequestDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.QuestionContent))
                {
                    return BadRequest(new { message = "Nội dung câu hỏi không được để trống." });
                }

                var persona = dto.Persona.HasValue ? (AgentPersonaType)dto.Persona.Value : AgentPersonaType.SupremeArbiter;
                var walkthrough = await _aiRouter.GenerateWalkthroughAsync(
                    dto.QuestionContent,
                    dto.Options ?? new List<string>(),
                    dto.CorrectAnswer ?? "A",
                    dto.DomainCode ?? "GENERAL",
                    persona,
                    dto.TenantId);

                return Ok(walkthrough);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi sinh bản đồ tư duy Walkthrough: {ex.Message}" });
            }
        }

        // POST api/admin/ai/audit-question
        [HttpPost("audit-question")]
        public async Task<IActionResult> AuditQuestion([FromBody] AuditQuestionRequestDto dto)
        {
            try
            {
                var auditResult = await _committeeEngine.AuditQuestionAsync(
                    dto.QuestionContent,
                    dto.Options ?? new List<string>(),
                    dto.CorrectAnswer ?? "A",
                    dto.DomainCode ?? "GENERAL");

                return Ok(auditResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi thẩm định đa tác tử: {ex.Message}" });
            }
        }
    }

    public class WalkthroughRequestDto
    {
        public string QuestionContent { get; set; } = string.Empty;
        public List<string>? Options { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? DomainCode { get; set; }
        public int? Persona { get; set; }
        public string? TenantId { get; set; }
    }

    public class AuditQuestionRequestDto
    {
        public string QuestionContent { get; set; } = string.Empty;
        public List<string>? Options { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? DomainCode { get; set; }
    }
}
