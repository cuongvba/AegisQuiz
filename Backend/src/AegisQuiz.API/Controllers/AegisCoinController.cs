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
    /// [Epoch 4: Community Ecosystem] Bộ điều khiển Kinh tế mã hóa AegisCoin Proof-of-Learning.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AegisCoinController : ControllerBase
    {
        private readonly IAegisCoinService _coinService;

        public AegisCoinController(IAegisCoinService coinService)
        {
            _coinService = coinService;
        }

        private Guid GetCurrentUserId()
        {
            var claimVal = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
            if (Guid.TryParse(claimVal, out var uid)) return uid;
            return Guid.Parse("00000000-0000-0000-0000-000000000002"); // Fallback demo user
        }

        [HttpGet("wallet")]
        public async Task<IActionResult> GetWallet(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            var wallet = await _coinService.GetWalletAsync(userId, ct);
            return Ok(wallet);
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            long balance = await _coinService.GetBalanceAsync(userId, ct);
            return Ok(new { userId, balance });
        }

        [HttpPost("reward")]
        public async Task<IActionResult> RewardCoins([FromBody] AegisCoinRewardRequestDto req, CancellationToken ct)
        {
            if (req.UserId == Guid.Empty)
            {
                req.UserId = GetCurrentUserId();
            }
            var wallet = await _coinService.RewardCoinsAsync(req, ct);
            return Ok(new { success = true, wallet });
        }

        [HttpPost("spend")]
        public async Task<IActionResult> SpendCoins([FromBody] AegisCoinSpendRequestDto req, CancellationToken ct)
        {
            if (req.UserId == Guid.Empty)
            {
                req.UserId = GetCurrentUserId();
            }
            bool success = await _coinService.SpendCoinsAsync(req, ct);
            if (!success)
            {
                return BadRequest(new { success = false, message = "Số dư AegisCoin không đủ để mở khóa nội dung này." });
            }
            var wallet = await _coinService.GetWalletAsync(req.UserId, ct);
            return Ok(new { success = true, message = "Mở khóa thành công!", balance = wallet.Balance });
        }
    }
}
