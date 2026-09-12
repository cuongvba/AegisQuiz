using System;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Services;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [Admin Cognitive Ingestion Rule Studio Controller]
    /// Cung cấp API quản lý danh mục quy tắc bóc tách câu hỏi và Live Sandbox.
    /// </summary>
    [ApiController]
    [Route("api/admin/parsing-rules")]
    public class AdminParsingRulesController : ControllerBase
    {
        private readonly IAiRuleAutopilotService _aiRuleAutopilotService;

        public AdminParsingRulesController(IAiRuleAutopilotService aiRuleAutopilotService)
        {
            _aiRuleAutopilotService = aiRuleAutopilotService;
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
    }

    public class ReloadRulesRequest
    {
        public string? CustomJsonConfig { get; set; }
    }
}
