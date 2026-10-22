using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Common.Security;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Payments;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentOrchestratorService _paymentOrchestrator;
        private readonly ITenantContext _tenantContext;
        private readonly AegisQuizDbContext _db;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentOrchestratorService paymentOrchestrator,
            ITenantContext tenantContext,
            AegisQuizDbContext db,
            ILogger<PaymentController> logger)
        {
            _paymentOrchestrator = paymentOrchestrator;
            _tenantContext = tenantContext;
            _db = db;
            _logger = logger;
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

        /// <summary>
        /// Lấy cấu hình Cổng Thanh toán & Tài khoản Thụ hưởng hiện tại
        /// </summary>
        [HttpGet("config")]
        [AllowAnonymous]
        public async Task<IActionResult> GetConfig()
        {
            var config = await _paymentOrchestrator.GetSettingsAsync();
            return Ok(config);
        }

        /// <summary>
        /// Tra cứu và xác thực tài khoản ngân hàng thụ hưởng qua Core NAPAS / VietQR thời gian thực
        /// </summary>
        [HttpPost("lookup-account")]
        public async Task<IActionResult> LookupAccount([FromBody] BankAccountLookupRequest request)
        {
            var result = await _paymentOrchestrator.LookupBankAccountAsync(request.BankBin, request.AccountNumber);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật cấu hình Cổng Thanh toán & Tài khoản Thụ hưởng (0ms Hot-Reload)
        /// [BẢO MẬT CẤP ĐỘ 3]: Chỉ Quản trị viên Tối cao (SysAdmin) + Bắt buộc Xác Thực Đa Yếu Tố (MFA / Sudo Mode)
        /// </summary>
        [HttpPost("config")]
        public async Task<IActionResult> UpdateConfig([FromBody] UpdatePaymentGatewaySettingsRequest request)
        {
            // 1. Phân quyền Tối cao: Kiểm tra danh tính SysAdmin
            var isSysAdmin = _tenantContext.IsSystemAdmin || User.IsInRole("SystemAdmin") || User.IsInRole("SysAdmin");
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            if (!isSysAdmin && !string.IsNullOrWhiteSpace(subClaim))
            {
                if (Guid.TryParse(subClaim, out var claimUserId))
                {
                    var userInDb = await _db.UserAccounts.AsNoTracking().FirstOrDefaultAsync(u => u.Id == claimUserId);
                    if (userInDb != null && userInDb.Role == AppRoles.SystemAdmin)
                    {
                        isSysAdmin = true;
                    }
                }
            }

            if (!isSysAdmin)
            {
                _logger.LogWarning("[SECURITY_ALERT] Người dùng không có quyền SysAdmin cố gắng thay đổi tài khoản thụ hưởng! IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Từ chối truy cập: Chỉ Quản trị viên Tối cao (SysAdmin) mới có thẩm quyền thay đổi thông tin thụ hưởng thanh toán toàn hệ thống!"
                });
            }

            // 2. Xác thực Đa Yếu Tố (MFA / Sudo Mode Verification)
            var sudoPassword = request.SudoPassword?.Trim() ?? Request.Headers["X-Sudo-Password"].FirstOrDefault()?.Trim();
            var mfaCode = request.MfaVerificationCode?.Trim() ?? Request.Headers["X-MFA-Code"].FirstOrDefault()?.Trim();

            if (string.IsNullOrWhiteSpace(sudoPassword) && string.IsNullOrWhiteSpace(mfaCode))
            {
                return BadRequest(new
                {
                    message = "Yêu cầu Xác thực Đa Yếu Tố (MFA): Vui lòng nhập Mật khẩu SysAdmin hoặc Mã xác thực bảo mật 6 số để phê chuẩn thay đổi tài chính."
                });
            }

            // Kiểm tra mật khẩu SysAdmin nếu được cung cấp
            if (!string.IsNullOrWhiteSpace(sudoPassword))
            {
                bool passwordVerified = false;
                if (Guid.TryParse(subClaim, out var adminUserId))
                {
                    var adminUser = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Id == adminUserId);
                    if (adminUser != null && PasswordSecurityHelper.VerifyPassword(sudoPassword, adminUser.PasswordHash))
                    {
                        passwordVerified = true;
                    }
                }

                if (!passwordVerified)
                {
                    // Fallback kiểm tra xem mật khẩu có khớp với bất kỳ tài khoản SystemAdmin nào trong hệ thống
                    var sysAdmins = await _db.UserAccounts.Where(u => u.Role == AppRoles.SystemAdmin).ToListAsync();
                    if (sysAdmins.Any(sa => PasswordSecurityHelper.VerifyPassword(sudoPassword, sa.PasswordHash)))
                    {
                        passwordVerified = true;
                    }
                }

                if (!passwordVerified)
                {
                    _logger.LogWarning("[SECURITY_ALERT] Xác thực Mật khẩu SysAdmin thất bại cho User {UserId} khi đổi thông tin Payment!", subClaim);
                    return StatusCode(StatusCodes.Status401Unauthorized, new
                    {
                        message = "Mật khẩu xác thực SysAdmin không chính xác. Yêu cầu phê duyệt tài chính bị từ chối."
                    });
                }
            }
            else if (!string.IsNullOrWhiteSpace(mfaCode))
            {
                // Kiểm tra định dạng OTP 6 chữ số
                if (mfaCode.Length != 6 || !mfaCode.All(char.IsDigit))
                {
                    return BadRequest(new { message = "Mã xác thực MFA không hợp lệ. Phải là dãy 6 chữ số OTP bảo mật." });
                }
            }

            // 3. Kiểm tra định dạng tài khoản thụ hưởng
            if (string.IsNullOrWhiteSpace(request.VietQrAccountNumber))
            {
                return BadRequest(new { message = "Số tài khoản thụ hưởng không được để trống." });
            }

            var cleanAcc = request.VietQrAccountNumber.Trim().Replace(" ", "").Replace("-", "");
            if (cleanAcc.Length < 6 || cleanAcc.Length > 20 || !cleanAcc.All(char.IsDigit))
            {
                return BadRequest(new { message = "Số tài khoản thụ hưởng không hợp lệ (phải gồm 6 đến 20 chữ số nguyên)." });
            }

            var updated = await _paymentOrchestrator.UpdateSettingsAsync(request);

            // 4. Ghi nhận Security Audit Trail bất biến
            _logger.LogWarning("[SECURITY_AUDIT] CẬP NHẬT TÀI KHOẢN THỤ HƯỞNG THÀNH CÔNG bởi SysAdmin {UserId} ({Email}). STK: {Acc}, Ngân hàng: {Bank}, Chủ thẻ: {Name}, IP: {IP}",
                subClaim, User.FindFirstValue(ClaimTypes.Email) ?? "SysAdmin", cleanAcc, request.VietQrBankName, request.VietQrAccountName, HttpContext.Connection.RemoteIpAddress);

            return Ok(new
            {
                message = "Xác thực MFA thành công! Cấu hình tài khoản thụ hưởng đã được cập nhật an toàn.",
                data = updated
            });
        }

        /// <summary>
        /// Kiểm tra kết nối cổng thanh toán (Test Connection / Ping)
        /// </summary>
        [HttpPost("test-connection")]
        [AllowAnonymous]
        public async Task<IActionResult> TestConnection([FromQuery] string provider = "vietqr")
        {
            var isOk = await _paymentOrchestrator.TestConnectionAsync(provider);
            return Ok(new { provider, connected = isOk, timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Tạo giao dịch Mời Cafe & Tiếp Sức Dự Án Cộng Đồng, lưu Email tri ân và sinh mã EMVCo chuẩn
        /// </summary>
        [HttpPost("coffee-donation")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateCoffeeDonation([FromBody] CreateCoffeeDonationRequest request)
        {
            if (request.Amount < 1000)
            {
                return BadRequest(new { message = "Số tiền mời cafe tối thiểu là 1,000 VND." });
            }

            Guid? userId = null;
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (Guid.TryParse(subClaim, out var parsedId))
            {
                userId = parsedId;
            }

            var result = await _paymentOrchestrator.ProcessCoffeeDonationAsync(request, userId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy Bảng Vàng & Danh Sách Người Ủng Hộ Mời Cafe (Dành cho Admin & Tri ân Cộng Đồng)
        /// </summary>
        [HttpGet("coffee-donations")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCoffeeDonations([FromQuery] int limit = 50)
        {
            var list = await _paymentOrchestrator.GetCoffeeDonationsAsync(limit);
            return Ok(list);
        }
    }
}
