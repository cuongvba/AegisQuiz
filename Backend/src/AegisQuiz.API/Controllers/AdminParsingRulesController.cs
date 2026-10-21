using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.API.Security;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Services;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [Admin Cognitive Ingestion Rule Studio Controller]
    /// Cung cấp API quản lý danh mục quy tắc bóc tách câu hỏi và Live Sandbox.
    /// [CRITICAL SECURITY FIX] Trước đây route mang tên "/api/admin/..." nhưng KHÔNG có
    /// [Authorize] nào — nay yêu cầu Role-Weight ≥ TenantAdmin.
    /// </summary>
    [ApiController]
    [Route("api/admin/parsing-rules")]
    [Authorize(Policy = RoleWeightPolicies.TenantAdmin)]
    public class AdminParsingRulesController : ControllerBase
    {
        private readonly IAiRuleAutopilotService _aiRuleAutopilotService;
        private readonly IShadowTestingService _shadowTestingService;

        public AdminParsingRulesController(
            IAiRuleAutopilotService aiRuleAutopilotService,
            IShadowTestingService shadowTestingService)
        {
            _aiRuleAutopilotService = aiRuleAutopilotService;
            _shadowTestingService = shadowTestingService;
        }

        [HttpGet]
        public IActionResult GetAllRules()
        {
            var rules = DynamicQuestionIngestionEngine.Instance.GetAllRules();
            return Ok(new
            {
                Total = rules.Count,
                Rules = rules
            });
        }

        [HttpPost("reload")]
        public IActionResult ReloadRules([FromBody] ReloadRulesRequest? request)
        {
            bool success;
            if (request != null && !string.IsNullOrWhiteSpace(request.CustomJsonConfig))
            {
                success = DynamicQuestionIngestionEngine.Instance.HotReloadRules(request.CustomJsonConfig);
            }
            else
            {
                DynamicQuestionIngestionEngine.Instance.LoadDefaultRules();
                success = true;
            }

            var currentRules = DynamicQuestionIngestionEngine.Instance.GetAllRules();
            return Ok(new
            {
                Success = success,
                Message = success ? $"Đã nạp thành công {currentRules.Count} quy tắc." : "Nạp quy tắc thất bại.",
                CurrentRulesCount = currentRules.Count
            });
        }

        /// <summary>
        /// [Maintenance / Admin] Xóa bỏ toàn bộ quy tắc cũ trong DB và thiết lập lại bộ quy tắc chuẩn mới
        /// </summary>
        [HttpPost("reset-and-seed")]
        public async System.Threading.Tasks.Task<IActionResult> ResetAndReseedRules()
        {
            int count = await DynamicQuestionIngestionEngine.Instance.ResetAndReseedRulesAsync();
            return Ok(new
            {
                Success = count > 0,
                Message = $"Đã xóa sạch quy tắc cũ và tái thiết lập thành công {count} quy tắc chuẩn mới vào CSDL!",
                RulesCount = count
            });
        }

        [HttpPost("test-snippet")]
        public IActionResult TestSnippet([FromBody] ParseSnippetRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.SnippetText))
            {
                return BadRequest(new { Message = "SnippetText không được để trống." });
            }

            var response = DynamicQuestionIngestionEngine.Instance.ParseSnippet(
                request.SnippetText, 
                request.SpecificRuleCode);

            return Ok(response);
        }

        /// <summary>
        /// [Phase 4] AI Rule Autopilot: Tự động học ngữ pháp đề thi lạ và sinh quy tắc CIG 4 chiều
        /// </summary>
        [HttpPost("ai-generate-rule")]
        public async System.Threading.Tasks.Task<IActionResult> AiGenerateRule([FromBody] AiRuleAutopilotRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.SnippetText))
            {
                return BadRequest(new { Message = "SnippetText không được để trống." });
            }

            var result = await _aiRuleAutopilotService.SynthesizeRuleAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// [Phase 4] Lưu và kích hoạt ngay quy tắc tùy biến vào Engine (Realtime Zero-Downtime)
        /// </summary>
        [HttpPost("save-custom-rule")]
        public IActionResult SaveCustomRule([FromBody] QuestionParsingRuleDto rule)
        {
            if (string.IsNullOrWhiteSpace(rule.RuleCode) || string.IsNullOrWhiteSpace(rule.Structure?.QuestionHeaderRegexPattern))
            {
                return BadRequest(new { Message = "Quy tắc không hợp lệ (RuleCode hoặc QuestionHeaderRegexPattern trống)." });
            }

            DynamicQuestionIngestionEngine.Instance.AddOrUpdateRule(rule, persistToFile: true);

            return Ok(new
            {
                Success = true,
                Message = $"Đã lưu và kích hoạt quy tắc [{rule.RuleCode}] thành công!",
                Rule = rule
            });
        }

        /// <summary>
        /// [Phase 4] Shadow Testing Pipeline: Chạy ngầm quy tắc qua bộ dữ liệu chuẩn chống thoái lui (Zero Regression)
        /// </summary>
        [HttpPost("shadow-test")]
        public async System.Threading.Tasks.Task<IActionResult> RunShadowTest([FromBody] ShadowTestRequestDto request)
        {
            if (request?.ProposedRule == null || string.IsNullOrWhiteSpace(request.ProposedRule.RuleCode))
            {
                return BadRequest(new { Message = "Yêu cầu kiểm thử ngầm không hợp lệ (RuleCode trống)." });
            }

            var result = await _shadowTestingService.RunShadowTestAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// [Universal Taxonomy] Lấy toàn bộ từ điển cấu trúc tài liệu đang kích hoạt
        /// </summary>
        [HttpGet("structure-dictionary")]
        public IActionResult GetStructureDictionary()
        {
            var dict = DynamicQuestionIngestionEngine.Instance.GetStructureDictionary();
            return Ok(dict);
        }

        /// <summary>
        /// [Universal Taxonomy] Cập nhật và lưu từ điển cấu trúc tài liệu (0ms Hot-Reload)
        /// </summary>
        [HttpPut("structure-dictionary")]
        public IActionResult UpdateStructureDictionary([FromBody] DocumentStructureDictionaryDto dictionary)
        {
            if (dictionary == null)
            {
                return BadRequest(new { Message = "Dữ liệu từ điển cấu trúc không hợp lệ." });
            }

            DynamicQuestionIngestionEngine.Instance.UpdateStructureDictionary(dictionary, persistToFile: true);
            return Ok(new
            {
                Success = true,
                Message = "Đã lưu và cập nhật Từ điển Cấu trúc Tài liệu thành công (0ms Hot-Reload)!"
            });
        }

        /// <summary>
        /// Xóa một quy tắc khỏi hệ thống theo RuleCode
        /// </summary>
        [HttpDelete("{ruleCode}")]
        public IActionResult DeleteRule(string ruleCode)
        {
            bool removed = DynamicQuestionIngestionEngine.Instance.RemoveRule(ruleCode, persistToFile: true);
            if (!removed)
            {
                return NotFound(new { Message = $"Không tìm thấy quy tắc với mã [{ruleCode}]." });
            }

            return Ok(new
            {
                Success = true,
                Message = $"Đã xóa quy tắc [{ruleCode}] thành công!"
            });
        }
    }

    public class ReloadRulesRequest
    {
        public string? CustomJsonConfig { get; set; }
    }
}
