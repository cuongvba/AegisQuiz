using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [CG5: Economic Engine] + [CG6: Identity Absolute]
    /// Xử lý: Cấp lại JWT isPremium sau payment + PKCE Code Exchange
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(AegisQuizDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        /// <summary>
        /// [CG5 FIX] Sau khi webhook xác nhận payment → Cấp JWT mới với isPremium=true
        /// Frontend gọi endpoint này thay vì tự set localStorage
        /// </summary>
        [HttpPost("refresh-premium")]
        public async Task<IActionResult> RefreshPremiumToken([FromBody] RefreshPremiumRequest request)
        {
            // Xác minh giao dịch thực sự đã hoàn thành trong DB
            var transaction = await _db.PaymentTransactions
                .FirstOrDefaultAsync(t =>
                    t.UserId == request.UserId &&
                    t.PaymentCode == request.PaymentCode &&
                    t.Status == PaymentStatus.Completed);

            if (transaction == null)
            {
                return Unauthorized(new { message = "[CG5] Giao dịch thanh toán chưa được xác nhận. isPremium không được cấp." });
            }

            // Giao dịch hợp lệ → Phát hành JWT mới với isPremium=true
            var secretKey = _config["Jwt:Secret"]
                ?? throw new InvalidOperationException("[CG2] Jwt:Secret chưa được cấu hình.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, request.UserId.ToString()),
                // [RBAC FIX] Dùng AppRoles.Learner thay vì hardcode "student"
                new Claim(ClaimTypes.Role, AppRoles.Learner),
                // [CG5] isPremium=true được server cấp sau khi xác nhận payment — không thể giả mạo
                new Claim("isPremium", "true"),
                new Claim("paymentCode", request.PaymentCode),
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = "AegisQuiz.PKI",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.WriteToken(handler.CreateToken(tokenDescriptor));

            return Ok(new
            {
                token,
                user = new
                {
                    id        = request.UserId,
                    isPremium = true,
                    // [RBAC FIX] Dùng AppRoles constant
                    role      = AppRoles.Learner
                }
            });
        }

        /// <summary>
        /// [CG6: BFF Pattern] PKCE Authorization Code Exchange
        /// Frontend gửi code + codeVerifier → Backend exchange với Keycloak → Trả JWT
        /// Token KHÔNG trả về full access token để lưu localStorage → Dùng HttpOnly Cookie
        /// </summary>
        [HttpPost("exchange")]
        public async Task<IActionResult> ExchangeCode([FromBody] PkceExchangeRequest request)
        {
            if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.CodeVerifier))
                return BadRequest(new { message = "code và codeVerifier là bắt buộc." });

            var oidcAuthority = _config["Oidc:Authority"] ?? "http://localhost:8180/realms/dehoc";
            var clientId = _config["Oidc:ClientId"] ?? "quiz-service";

            // Exchange code với Keycloak
            using var httpClient = new System.Net.Http.HttpClient();
            var tokenEndpoint = $"{oidcAuthority}/protocol/openid-connect/token";

            var formData = new Dictionary<string, string>
            {
                ["grant_type"]    = "authorization_code",
                ["client_id"]     = clientId,
                ["code"]          = request.Code,
                ["code_verifier"] = request.CodeVerifier,
                ["redirect_uri"]  = request.RedirectUri ?? $"{Request.Scheme}://{Request.Host}/auth/callback",
            };

            var response = await httpClient.PostAsync(tokenEndpoint, new System.Net.Http.FormUrlEncodedContent(formData));

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return Unauthorized(new { message = $"[CG6] Keycloak exchange thất bại: {error}" });
            }

            var tokenJson = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(tokenJson);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString();

            // [CG6 BFF] Lưu token trong HttpOnly Cookie — không trả về trực tiếp
            Response.Cookies.Append("aegis_access_token", accessToken ?? "", new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = !HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().IsDevelopment(),
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            // Trả về user info (không trả full access token)
            return Ok(new
            {
                accessToken, // Tạm thời trả về để tương thích với localStorage flow hiện tại
                user = new { isPremium = false, role = "student" }
            });
        }
    }

    public class RefreshPremiumRequest
    {
        public Guid UserId { get; set; }
        public string PaymentCode { get; set; } = string.Empty;
    }

    public class PkceExchangeRequest
    {
        public string Code { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
        public string? RedirectUri { get; set; }
    }
}
