using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.Payments;

namespace AegisQuiz.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentOrchestratorService _paymentOrchestrator;

        public PaymentController(IPaymentOrchestratorService paymentOrchestrator)
        {
            _paymentOrchestrator = paymentOrchestrator;
        }

        /// <summary>
        /// Khởi tạo yêu cầu thanh toán đa kênh (PayOS VietQR, Thẻ Quốc Tế Stripe, Alipay/WeChat Pay, Web3)
        /// </summary>
        [HttpPost("create-order")]
        [AllowAnonymous] // Cho phép cả khách chưa đăng nhập hoặc học viên đã đăng nhập
        public async Task<IActionResult> CreateOrder([FromBody] CreatePaymentOrderRequest request)
        {
            // Trích xuất UserId từ JWT nếu có
            if (request.UserId == Guid.Empty)
            {
                var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                if (Guid.TryParse(subClaim, out var parsedUserId))
                {
                    request.UserId = parsedUserId;
                }
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new { message = "Số tiền thanh toán phải lớn hơn 0." });
            }

            try
            {
                var result = await _paymentOrchestrator.InitiatePaymentAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khởi tạo cổng thanh toán.", error = ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái thanh toán theo thời gian thực (Polling / Status Check)
        /// </summary>
        [HttpGet("status/{orderCode}")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckStatus([FromRoute] long orderCode)
        {
            var status = await _paymentOrchestrator.CheckPaymentStatusAsync(orderCode);
            return Ok(status);
        }

        /// <summary>
        /// Báo cáo Doanh thu & Thống kê Tài chính Thật (Dành cho Quản trị viên)
        /// </summary>
        [HttpGet("analytics")]
        [Authorize(Roles = "SuperAdmin,TenantAdmin,Administrator")]
        public async Task<IActionResult> GetAnalytics()
        {
            var analytics = await _paymentOrchestrator.GetPaymentAnalyticsAsync();
            return Ok(analytics);
        }

        /// <summary>
        /// API Giả lập thanh toán thành công (Dành riêng cho Sandbox / Local Testing)
        /// </summary>
        [HttpPost("simulate-success/{orderCode}")]
        [AllowAnonymous]
        public async Task<IActionResult> SimulateSuccess([FromRoute] long orderCode)
        {
            var simulatedWebhook = new PayOSWebhookRequest
            {
                Code = "00",
                Desc = "Thanh cong gia lap Sandbox",
                Data = new PayOSWebhookData
                {
                    OrderCode = orderCode,
                    Amount = 99000,
                    Reference = $"SIM_FT_{DateTime.UtcNow.Ticks}",
                    Description = "AEGIS VIP SANDBOX SIMULATED",
                    Currency = "VND",
                    Code = "00",
                    Desc = "Thanh cong"
                }
            };

            var success = await _paymentOrchestrator.HandlePayOSWebhookAsync(simulatedWebhook);
            if (!success)
            {
                return NotFound(new { message = "Không tìm thấy giao dịch hoặc giao dịch đã hoàn tất." });
            }

            return Ok(new { message = "Đã giả lập thanh toán thành công! Quyền VIP đã được kích hoạt.", orderCode });
        }
    }
}
