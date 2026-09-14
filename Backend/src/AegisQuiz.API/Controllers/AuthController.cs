using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Application.Common.Security;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [Enterprise Security] Hệ thống xác thực định danh và phân luồng vai trò & thuê bao.
    /// Hỗ trợ: Đăng nhập/Đăng ký tài khoản mật khẩu (Salted PBKDF2/SHA-256), PKCE Code Exchange, Refresh Premium.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        private readonly IConfiguration _config;
        private readonly Microsoft.Extensions.Logging.ILogger<AuthController> _logger;
        private static bool _tableEnsured = false;
        private static readonly object _tableLock = new object();

        public AuthController(
            AegisQuizDbContext db, 
            IConfiguration config, 
            Microsoft.Extensions.Logging.ILogger<AuthController> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        private void EnsureUserAccountsTable()
        {
            if (_tableEnsured) return;
            lock (_tableLock)
            {
                if (_tableEnsured) return;
                try
                {
                    if (_db.Database.IsRelational())
                    {
                        _db.Database.ExecuteSqlRaw(@"
                            CREATE TABLE IF NOT EXISTS ""UserAccounts"" (
                                ""Id"" uuid NOT NULL,
                                ""Email"" text NOT NULL,
                                ""PasswordHash"" text NOT NULL,
                                ""FullName"" text NOT NULL,
                                ""PhoneNumber"" text,
                                ""AvatarUrl"" text,
                                ""Role"" text NOT NULL DEFAULT 'Learner',
                                ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                                ""OrgUnitId"" uuid,
                                ""IsPremium"" boolean NOT NULL DEFAULT false,
                                ""SubscriptionTier"" text NOT NULL DEFAULT 'FREE',
                                ""SubscriptionExpiresAt"" timestamp with time zone,
                                ""IsActive"" boolean NOT NULL DEFAULT true,
                                ""FailedLoginAttempts"" integer NOT NULL DEFAULT 0,
                                ""LockoutEnd"" timestamp with time zone,
                                ""LastLoginAt"" timestamp with time zone,
                                ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                                ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                                CONSTRAINT ""PK_UserAccounts"" PRIMARY KEY (""Id"")
                            );
                            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserAccounts_Email"" ON ""UserAccounts"" (""Email"");
                        ");
                    }
                    _tableEnsured = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[AuthController] Table self-healing warning: {Msg}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Đăng ký tài khoản học viên mới với bảo mật mật khẩu PBKDF2 và tự động phân luồng.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            EnsureUserAccountsTable();

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Email và mật khẩu không được để trống." });

            var emailClean = request.Email.Trim().ToLowerInvariant();
            if (!emailClean.Contains('@') || !emailClean.Contains('.'))
                return BadRequest(new { message = "Định dạng email không hợp lệ." });

            if (request.Password.Length < 6)
                return BadRequest(new { message = "Mật khẩu phải có độ dài tối thiểu 6 ký tự." });

            try
            {
                var existingUser = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Email == emailClean);
                if (existingUser != null)
                    return Conflict(new { message = "Email này đã được đăng ký tài khoản trong hệ thống." });

                // Lấy hoặc tạo Tenant mặc định
                var defaultTenant = await _db.Tenants.FirstOrDefaultAsync();
                if (defaultTenant == null)
                {
                    defaultTenant = new Tenant
                    {
                        Id = Guid.NewGuid(),
                        Code = "dehoc",
                        Name = "Hệ sinh thái Giáo dục Dehoc",
                        Plan = TenantPlan.Starter,
                        ScaleType = TenantScaleType.Individual,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Tenants.Add(defaultTenant);
                    await _db.SaveChangesAsync();
                }

                // Băm mật khẩu chuẩn NIST PBKDF2
                var passwordHash = PasswordSecurityHelper.HashPassword(request.Password);

                var newUser = new UserAccount
                {
                    Id = Guid.NewGuid(),
                    Email = emailClean,
                    FullName = string.IsNullOrWhiteSpace(request.Name) ? emailClean.Split('@')[0] : request.Name.Trim(),
                    PasswordHash = passwordHash,
                    Role = AppRoles.Learner,
                    TenantId = defaultTenant.Id,
                    IsPremium = false,
                    SubscriptionTier = "FREE",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.UserAccounts.Add(newUser);

                // Ghi nhận bản ghi UserRole
                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = newUser.Id.ToString(),
                    TenantId = defaultTenant.Id,
                    Role = AppRoles.Learner,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow
                };
                _db.UserRoles.Add(userRole);

                await _db.SaveChangesAsync();

                // Phát hành JWT Token
                var token = GenerateJwtToken(newUser);

                _logger.LogInformation("[AuthController] Đăng ký thành công tài khoản mới: {Email}", emailClean);

                return Ok(new
                {
                    token,
                    user = BuildUserProfileDto(newUser)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthController] Lỗi khi xử lý đăng ký tài khoản cho {Email}: {Msg}", emailClean, ex.Message);
                return StatusCode(500, new { message = $"Đăng ký không thành công do máy chủ gặp sự cố: {ex.Message}" });
            }
        }

        /// <summary>
        /// Đăng nhập tài khoản với cơ chế kiểm tra mật khẩu an toàn và chống tấn công Brute-force.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            EnsureUserAccountsTable();

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Vui lòng điền đầy đủ email và mật khẩu." });

            var emailClean = request.Email.Trim().ToLowerInvariant();

            try
            {
                var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Email == emailClean);
                if (user == null)
                {
                    // Giảm thiểu rò rỉ thông tin qua phân tích timing
                    PasswordSecurityHelper.VerifyPassword("dummy-password", "pbkdf2_sha256:100000:c2FsdHNhbHQ=:aGFzaGhhc2g=");
                    return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác." });
                }

                if (!user.IsActive)
                    return Unauthorized(new { message = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ ban quản trị." });

                // Kiểm tra trạng thái khóa tạm thời (Lockout)
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    var remainingMinutes = Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
                    return StatusCode(423, new
                    {
                        message = $"Tài khoản tạm thời bị khóa do nhập sai quá 5 lần liên tiếp. Vui lòng thử lại sau {remainingMinutes} phút."
                    });
                }

                // Kiểm tra mật khẩu
                var isPasswordValid = PasswordSecurityHelper.VerifyPassword(request.Password, user.PasswordHash);
                if (!isPasswordValid)
                {
                    user.FailedLoginAttempts++;
                    if (user.FailedLoginAttempts >= 5)
                    {
                        user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                        user.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                        _logger.LogWarning("[Security] Khóa tài khoản {Email} 15 phút do nhập sai mật khẩu 5 lần.", emailClean);
                        return StatusCode(423, new
                        {
                            message = "Bạn đã nhập sai mật khẩu 5 lần liên tiếp. Tài khoản bị tạm khóa 15 phút để đảm bảo an toàn."
                        });
                    }

                    user.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();

                    var remainingAttempts = 5 - user.FailedLoginAttempts;
                    return Unauthorized(new
                    {
                        message = $"Mật khẩu không chính xác. Bạn còn {remainingAttempts} lần thử trước khi tài khoản bị tạm khóa."
                    });
                }

                // Đăng nhập thành công -> Reset lockout và đếm sai
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                user.LastLoginAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                _logger.LogInformation("[AuthController] Đăng nhập thành công: {Email} ({Role})", emailClean, user.Role);

                return Ok(new
                {
                    token,
                    user = BuildUserProfileDto(user)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AuthController] Lỗi hệ thống khi đăng nhập cho {Email}: {Msg}", emailClean, ex.Message);
                return StatusCode(500, new { message = $"Đăng nhập không thành công do lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Lấy thông tin phiên làm việc hiện tại của người dùng.
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });

            var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || !user.IsActive)
                return Unauthorized(new { message = "Tài khoản không tồn tại hoặc đã bị khóa." });

            return Ok(new
            {
                user = BuildUserProfileDto(user)
            });
        }

        /// <summary>
        /// Đổi mật khẩu tài khoản an toàn với xác thực mật khẩu cũ (PBKDF2/SHA-256).
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new { message = "Vui lòng nhập đầy đủ mật khẩu cũ và mật khẩu mới." });

            if (request.NewPassword.Length < 6)
                return BadRequest(new { message = "Mật khẩu mới phải có tối thiểu 6 ký tự." });

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });

            var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || !user.IsActive)
                return Unauthorized(new { message = "Tài khoản không tồn tại hoặc đã bị khóa." });

            if (!PasswordSecurityHelper.VerifyPassword(request.OldPassword, user.PasswordHash))
                return BadRequest(new { message = "Mật khẩu hiện tại không chính xác." });

            user.PasswordHash = PasswordSecurityHelper.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("[Security] Người dùng {Email} ({Role}) đã đổi mật khẩu thành công.", user.Email, user.Role);
            return Ok(new { message = "Đổi mật khẩu thành công. Mật khẩu mới có hiệu lực ngay lập tức." });
        }

        /// <summary>
        /// [CG5 FIX] Sau khi webhook xác nhận payment → Cấp JWT mới với isPremium=true
        /// </summary>
        [HttpPost("refresh-premium")]
        public async Task<IActionResult> RefreshPremiumToken([FromBody] RefreshPremiumRequest request)
        {
            var transaction = await _db.PaymentTransactions
                .FirstOrDefaultAsync(t =>
                    t.UserId == request.UserId &&
                    t.PaymentCode == request.PaymentCode &&
                    t.Status == PaymentStatus.Completed);

            if (transaction == null)
            {
                return Unauthorized(new { message = "[CG5] Giao dịch thanh toán chưa được xác nhận. isPremium không được cấp." });
            }

            var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user != null)
            {
                user.IsPremium = true;
                user.SubscriptionTier = "VIP";
                user.SubscriptionExpiresAt = DateTime.UtcNow.AddMonths(1);
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            var secretKey = _config["Jwt:Secret"]
                ?? _config["Jwt__Secret"]
                ?? _config["JWT_SECRET"]
                ?? "SuperSecretJwtKeyForAegisQuizPlatform2026DehocVn!";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, request.UserId.ToString()),
                new Claim(ClaimTypes.Role, user?.Role ?? AppRoles.Learner),
                new Claim("isPremium", "true"),
                new Claim("subscriptionTier", "VIP"),
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
                    id = request.UserId,
                    isPremium = true,
                    subscriptionTier = "VIP",
                    role = user?.Role ?? AppRoles.Learner
                }
            });
        }

        /// <summary>
        /// [CG6: BFF Pattern] PKCE Authorization Code Exchange
        /// </summary>
        [HttpPost("exchange")]
        public async Task<IActionResult> ExchangeCode([FromBody] PkceExchangeRequest request)
        {
            if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.CodeVerifier))
                return BadRequest(new { message = "code và codeVerifier là bắt buộc." });

            var oidcAuthority = _config["Oidc:Authority"] ?? "http://localhost:8180/realms/dehoc";
            var clientId = _config["Oidc:ClientId"] ?? "quiz-service";

            using var httpClient = new System.Net.Http.HttpClient();
            var tokenEndpoint = $"{oidcAuthority}/protocol/openid-connect/token";

            var formData = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId,
                ["code"] = request.Code,
                ["code_verifier"] = request.CodeVerifier,
                ["redirect_uri"] = request.RedirectUri ?? $"{Request.Scheme}://{Request.Host}/auth/callback",
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

            Response.Cookies.Append("aegis_access_token", accessToken ?? "", new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = !HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().IsDevelopment(),
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            return Ok(new
            {
                accessToken,
                user = new { isPremium = false, role = AppRoles.Learner, subscriptionTier = "FREE" }
            });
        }

        // ── Helper Methods ──────────────────────────────────────────────────

        private string GenerateJwtToken(UserAccount user)
        {
            var secretKey = _config["Jwt:Secret"]
                ?? _config["Jwt__Secret"]
                ?? _config["JWT_SECRET"]
                ?? "SuperSecretJwtKeyForAegisQuizPlatform2026DehocVn!";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("tenant_id", user.TenantId.ToString()),
                new Claim("isPremium", user.IsPremium ? "true" : "false"),
                new Claim("subscriptionTier", user.SubscriptionTier ?? "FREE")
            };

            if (user.OrgUnitId.HasValue)
            {
                claims.Add(new Claim("org_unit_id", user.OrgUnitId.Value.ToString()));
            }

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
            return handler.WriteToken(handler.CreateToken(tokenDescriptor));
        }

        private static object BuildUserProfileDto(UserAccount user)
        {
            return new
            {
                id = user.Id.ToString(),
                name = user.FullName,
                email = user.Email,
                role = user.Role,
                tenantId = user.TenantId.ToString(),
                orgUnitId = user.OrgUnitId?.ToString(),
                isPremium = user.IsPremium,
                subscriptionTier = user.SubscriptionTier,
                avatar = user.AvatarUrl
            };
        }
    }

    public class RegisterRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
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

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
