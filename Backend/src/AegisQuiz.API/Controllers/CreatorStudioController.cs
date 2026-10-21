using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.API.Controllers
{
    [ApiController]
    [Route("api/creator/studio")]
    public class CreatorStudioController : ControllerBase
    {
        private readonly ITaxonomySnifferDispatcher _taxonomyDispatcher;
        private readonly IDocxParserService _docxParser;
        private readonly IExamDeduplicationEngine _dedupEngine;
        private readonly AegisQuizDbContext _db;
        private readonly ILogger<CreatorStudioController> _logger;

        public CreatorStudioController(
            ITaxonomySnifferDispatcher taxonomyDispatcher,
            IDocxParserService docxParser,
            IExamDeduplicationEngine dedupEngine,
            AegisQuizDbContext db,
            ILogger<CreatorStudioController> logger)
        {
            _taxonomyDispatcher = taxonomyDispatcher;
            _docxParser = docxParser;
            _dedupEngine = dedupEngine;
            _db = db;
            _logger = logger;
        }

        [HttpPost("sniff")]
        [AllowAnonymous]
        public IActionResult SniffTaxonomy([FromBody] SniffTaxonomyRequest request)
        {
            var result = _taxonomyDispatcher.SniffTaxonomy(request?.ContentPreview ?? string.Empty);
            return Ok(new { success = true, data = result });
        }

        [HttpPost("upload")]
        [AllowAnonymous]
        [RequestSizeLimit(30_000_000)]
        public async Task<IActionResult> UploadAndPreview(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File trống hoặc không hợp lệ." });

            try
            {
                using var stream = file.OpenReadStream();
                var parsedQuestions = _docxParser.ParseDocxFile(stream, null);
                if (parsedQuestions == null || parsedQuestions.Count == 0)
                    return BadRequest(new { success = false, message = "Không trích xuất được câu hỏi từ tệp." });

                // Dùng Deduplication Engine để phân tích trùng lặp CAS đa tầng
                var dedupReport = await _dedupEngine.AnalyzeAndTagDuplicatesAsync(parsedQuestions, Guid.Empty);

                var previewItems = parsedQuestions.Select((q, idx) => new ZenQuestionPreviewDto
                {
                    Order = idx + 1,
                    Content = q.Content ?? "",
                    CanonicalHash = q.CanonicalContentHash ?? _dedupEngine.ComputeCanonicalContentHash(q.Content ?? "", q.Options),
                    IsDuplicate = q.IsDuplicate,
                    QuestionType = q.QuestionType,
                    Options = q.Options?.ToList() ?? new(),
                    CorrectAnswer = q.SuggestedAnswer,
                    Explanation = q.AiExplanation
                }).ToList();

                var sampleContent = string.Join("\n", previewItems.Take(3).Select(x => x.Content));
                var taxonomy = _taxonomyDispatcher.SniffTaxonomy(sampleContent);

                return Ok(new ZenUploadResponseDto
                {
                    Success = true,
                    ExamTitle = Path.GetFileNameWithoutExtension(file.FileName),
                    TotalQuestions = parsedQuestions.Count,
                    DuplicateCount = dedupReport.DuplicateCount,
                    UniqueCount = dedupReport.UniqueCount,
                    DeduplicationRatePercentage = dedupReport.DeduplicationRatePercentage,
                    SuggestedTaxonomy = taxonomy,
                    Questions = previewItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi bóc tách Zen Studio upload");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("launch")]
        [AllowAnonymous]
        public IActionResult LaunchRoom([FromBody] LaunchZenRoomRequest request)
        {
            var pin = Random.Shared.Next(100000, 999999).ToString();
            var roomId = Guid.NewGuid().ToString("N")[..8];
            return Ok(new
            {
                success = true,
                pinCode = pin,
                roomId = roomId,
                title = request.Title,
                joinUrl = $"/arena/join?pin={pin}",
                qrCodeData = $"aegisquiz://arena/{pin}",
                launchedAt = DateTime.UtcNow
            });
        }

        [HttpGet("partnership")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPartnership()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.TryParse(userIdStr, out var parsed) ? parsed : Guid.Empty;

            var partner = await _db.EducatorPartnerships.FirstOrDefaultAsync(p => p.UserId == userId);
            if (partner == null)
            {
                partner = new EducatorPartnership
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ReferralCode = "TEACH-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
                    TrialExpiresAt = DateTime.UtcNow.AddDays(90),
                    IsActive = true,
                    RevenueShareRate = 0.80m,
                    KarmaPoints = 100
                };
                _db.EducatorPartnerships.Add(partner);
                await _db.SaveChangesAsync();
            }

            var daysRemaining = Math.Max(0, (int)(partner.TrialExpiresAt - DateTime.UtcNow).TotalDays);
            return Ok(new CreatorPartnershipDto
            {
                Id = partner.Id,
                UserId = partner.UserId,
                ReferralCode = partner.ReferralCode,
                TrialExpiresAt = partner.TrialExpiresAt,
                DaysRemaining = daysRemaining,
                IsActive = partner.IsActive,
                RevenueShareRate = partner.RevenueShareRate,
                KarmaPoints = partner.KarmaPoints,
                PayoutBankAccount = partner.PayoutBankAccount
            });
        }
    }
}
