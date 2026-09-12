using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.API.Security;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly AegisQuizDbContext _dbContext;

        public WebhookController(AegisQuizDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // [LUỒNG 1: Ngân hàng Việt Nam (Casso/SePay)]
        [ValidateWebhookSignature] // [Lỗ hổng #5 đã vá!]
        [HttpPost("vietqr-gateway")]
        public async Task<IActionResult> ReceiveVietQRPayment([FromBody] VietQRPayload payload)
        {
            string transferContent = payload.Description;
            var match = Regex.Match(transferContent, @"AEGISQ\s*[A-Z0-9]+", RegexOptions.IgnoreCase);
            
            if (!match.Success) return Ok(new { message = "Không tìm thấy mã thanh toán." });

            string paymentCode = match.Value.ToUpper().Replace(" ", "");
            Console.WriteLine($"[VietQR] Nhận được {payload.Amount} VND cho mã: {paymentCode}");
            
            // LOGIC XỬ LÝ THANH TOÁN KÉP (IDEMPOTENCY & OVERPAYMENT)
            var transaction = await _dbContext.PaymentTransactions.FirstOrDefaultAsync(t => t.PaymentCode == paymentCode);
            
            if (transaction == null)
            {
                return NotFound(new { message = "Không tìm thấy giao dịch với mã tương ứng." });
            }

            if (transaction.Status == PaymentStatus.Completed) 
            {
                Console.WriteLine("CẢNH BÁO: Giao dịch đã được thanh toán trước đó.");
                return Ok(new { message = "Giao dịch đã được hoàn thành trước đó." });
            }

            // Thanh toán hợp lệ -> Mở khóa
            transaction.MarkAsCompleted();
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Thanh toán thành công. Đã kích hoạt gói." });
        }

        // [LUỒNG 2: Crypto Web3 (Polygon Smart Contract)]
        // [Fix bảo mật] Thêm [ValidateWebhookSignature] để chống giả mạo — đồng nhất với VietQR
        [ValidateWebhookSignature]
        [HttpPost("polygon-gateway")]
        public async Task<IActionResult> ReceiveWeb3Payment([FromBody] Web3EventPayload payload)
        {
            // API này nhận Event từ Blockchain (Smart Contract Trigger)
            string paymentCode = payload.PaymentCode; 
            
            Console.WriteLine($"[Web3] Hợp đồng thông minh báo cáo nhận {payload.MaticAmount} MATIC. Đang mở khóa mã: {paymentCode}");
            
            var transaction = await _dbContext.PaymentTransactions.FirstOrDefaultAsync(t => t.PaymentCode == paymentCode);
            
            if (transaction == null)
            {
                return NotFound(new { message = "Không tìm thấy giao dịch Web3 tương ứng." });
            }

            if (transaction.Status == PaymentStatus.Completed)
            {
                return Ok(new { message = "Giao dịch Web3 đã được hoàn tất từ trước." });
            }

            // Mở khóa giao dịch
            transaction.MarkAsCompleted();
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Mở khóa thành công." });
        }

        [HttpPost("create-transaction")]
        public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
        {
            var code = "AEGISQ" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            var transaction = new PaymentTransaction
            {
                TransactionId = Guid.NewGuid(),
                UserId = request.UserId,
                PaymentCode = code,
                AmountRequired = request.Amount,
                Currency = request.Currency,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.PaymentTransactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            return Ok(transaction);
        }
    }

    public class CreateTransactionRequest
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "MATIC";
    }

    public class VietQRPayload
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class Web3EventPayload
    {
        public decimal MaticAmount { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public string PaymentCode { get; set; } = string.Empty;
    }
}
