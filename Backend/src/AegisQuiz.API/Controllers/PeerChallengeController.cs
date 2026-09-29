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
    /// [Epoch 4: Community Ecosystem] Bộ điều khiển Thách đấu đồng đẳng và Liên kết lan tỏa (Peer Challenge).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PeerChallengeController : ControllerBase
    {
        private readonly IPeerChallengeService _challengeService;

        public PeerChallengeController(IPeerChallengeService challengeService)
        {
            _challengeService = challengeService;
        }

        public class CreateChallengeRequest
        {
            public Guid ExamId { get; set; }
            public string CreatorName { get; set; } = string.Empty;
            public double Score { get; set; }
        }

        public class SubmitChallengeRequest
        {
            public string ShareCode { get; set; } = string.Empty;
            public string CandidateIdentifier { get; set; } = string.Empty;
            public double Score { get; set; }
            public int TimeSpentSeconds { get; set; }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateChallenge([FromBody] CreateChallengeRequest req, CancellationToken ct)
        {
            var userId = Guid.NewGuid();
            var challenge = await _challengeService.CreateChallengeAsync(req.ExamId, userId, req.CreatorName, req.Score, ct);
            return Ok(new
            {
                success = true,
                shareCode = challenge.ShareCode,
                shareUrl = $"/challenge/{challenge.ShareCode}",
                challenge
            });
        }

        [HttpGet("{shareCode}")]
        public async Task<IActionResult> GetChallenge([FromRoute] string shareCode, CancellationToken ct)
        {
            var challenge = await _challengeService.GetChallengeByCodeAsync(shareCode, ct);
            if (challenge == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy thử thách hoặc mã đã hết hạn." });
            }
            return Ok(challenge);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitResult([FromBody] SubmitChallengeRequest req, CancellationToken ct)
        {
            var challenge = await _challengeService.SubmitChallengeResultAsync(
                req.ShareCode, req.CandidateIdentifier, req.Score, req.TimeSpentSeconds, ct);
            return Ok(new { success = true, leaderboard = challenge.Leaderboard });
        }
    }
}
