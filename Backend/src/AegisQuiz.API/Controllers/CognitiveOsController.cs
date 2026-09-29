using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [Epoch 5: CognitiveOS & AI Moderation] Bộ điều khiển Hệ Điều Hành Nhận Thức Tối Thượng.
    /// Cung cấp endpoints cho Deep Knowledge Tracing (DKT), Đồ thị Tri thức Nhận thức,
    /// Trợ giảng Socratic dẫn xuất giáo trình chuẩn RAG, và Kiểm duyệt nội dung cộng đồng.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CognitiveOsController : ControllerBase
    {
        private readonly IDktEngine _dktEngine;
        private readonly IKnowledgeGraphService _knowledgeGraph;
        private readonly IPedagogicalRagService _ragService;
        private readonly IContentModerationService _moderationService;

        public CognitiveOsController(
            IDktEngine dktEngine,
            IKnowledgeGraphService knowledgeGraph,
            IPedagogicalRagService ragService,
            IContentModerationService moderationService)
        {
            _dktEngine = dktEngine;
            _knowledgeGraph = knowledgeGraph;
            _ragService = ragService;
            _moderationService = moderationService;
        }

        private Guid GetCurrentUserId()
        {
            var claimVal = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
            if (Guid.TryParse(claimVal, out var uid)) return uid;
            return Guid.Parse("00000000-0000-0000-0000-000000000002");
        }

        public class DktInteractionRequest
        {
            public string ConceptCode { get; set; } = string.Empty;
            public bool IsCorrect { get; set; }
            public int ResponseTimeMs { get; set; }
        }

        public class RagExplanationRequest
        {
            public Guid QuestionId { get; set; }
            public string QuestionContent { get; set; } = string.Empty;
            public string? StudentAnswer { get; set; }
            public string DomainCode { get; set; } = "EDUCATION";
        }

        public class ModerationAuditRequest
        {
            public string Content { get; set; } = string.Empty;
            public string? Explanation { get; set; }
        }

        // ── 1. Deep Knowledge Tracing (DKT) ──────────────────────────────────
        [HttpGet("dkt/state")]
        public async Task<IActionResult> GetDktState(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            var state = await _dktEngine.GetStudentKnowledgeStateAsync(userId, ct);
            return Ok(state);
        }

        [HttpPost("dkt/interaction")]
        public async Task<IActionResult> RecordInteraction([FromBody] DktInteractionRequest req, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            var state = await _dktEngine.RecordInteractionAndTraceAsync(
                userId, req.ConceptCode, req.IsCorrect, req.ResponseTimeMs, ct);
            return Ok(state);
        }

        // ── 2. Metacognitive Knowledge Graph ─────────────────────────────────
        [HttpGet("graph/concepts")]
        public async Task<IActionResult> GetConcepts([FromQuery] string domainCode = "EDUCATION", CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            var concepts = await _knowledgeGraph.GetDomainConceptsAsync(domainCode, userId, ct);
            return Ok(new { domainCode, totalConcepts = concepts.Count, concepts });
        }

        [HttpGet("graph/root-gaps/{conceptCode}")]
        public async Task<IActionResult> TraceRootGaps([FromRoute] string conceptCode, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            var gaps = await _knowledgeGraph.TraceRootCauseGapsAsync(conceptCode, userId, ct);
            return Ok(new { targetConcept = conceptCode, rootGaps = gaps });
        }

        [HttpGet("graph/remediation/{conceptCode}")]
        public async Task<IActionResult> GetRemediationPath([FromRoute] string conceptCode, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            var path = await _knowledgeGraph.GetRemediationLearningPathAsync(conceptCode, userId, ct);
            return Ok(new { targetConcept = conceptCode, stepsCount = path.Count, remediationPath = path });
        }

        // ── 3. Curriculum-Grounded Pedagogical RAG ────────────────────────────
        [HttpPost("rag/socratic-explanation")]
        public async Task<IActionResult> GenerateExplanation([FromBody] RagExplanationRequest req, CancellationToken ct)
        {
            var response = await _ragService.GenerateCurriculumGroundedExplanationAsync(
                req.QuestionId, req.QuestionContent, req.StudentAnswer, req.DomainCode, ct);
            return Ok(response);
        }

        // ── 4. Content Moderation Guardrail ──────────────────────────────────
        [HttpPost("moderation/audit")]
        public async Task<IActionResult> AuditQuestionContent([FromBody] ModerationAuditRequest req, CancellationToken ct)
        {
            var result = await _moderationService.ModerateQuestionAsync(req.Content, req.Explanation, ct);
            return Ok(result);
        }
    }
}
