using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.API.Controllers
{
    [ApiController]
    [Route("api/auth/pki")]
    public class PkiAuthController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _env;

        public PkiAuthController(IDistributedCache cache, IConfiguration configuration, IHostEnvironment env)
        {
            _cache = cache;
            _configuration = configuration;
            _env = env;
        }

        // Bước 1: Sinh Challenge (Nonce) gửi cho Client
        [HttpPost("challenge")]
        public async Task<IActionResult> GetChallenge()
        {
            // Sinh chuỗi ngẫu nhiên 32 bytes dưới dạng Base64
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var challenge = Convert.ToBase64String(bytes);

            // Lưu challenge vào Cache với TTL 2 phút
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
            };
            await _cache.SetStringAsync($"pki_nonce:{challenge}", "active", options);

            return Ok(new { challenge = challenge });
        }

        // Bước 2: Nhận Chữ ký số và xác thực đăng nhập
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] PkiLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Challenge) || 
                string.IsNullOrEmpty(request.Signature) || 
                string.IsNullOrEmpty(request.CertificateBase64))
            {
                return BadRequest(new { message = "Dữ liệu yêu cầu không đầy đủ." });
            }

            // 1. Kiểm tra Challenge có tồn tại trong Redis không
            var cachedStatus = await _cache.GetStringAsync($"pki_nonce:{request.Challenge}");
            if (string.IsNullOrEmpty(cachedStatus))
            {
                return Unauthorized(new { message = "Thử thách (Challenge) đã hết hạn hoặc không hợp lệ." });
            }

            try
            {
                // 2. Load Chứng thư số từ Base64 client gửi lên
                byte[] certBytes = Convert.FromBase64String(request.CertificateBase64);
                using var cert = X509CertificateLoader.LoadCertificate(certBytes);

                // 3. Kiểm tra tính hợp lệ của Chứng thư số (Trust Chain & Revocation)
                if (!_env.IsDevelopment())
                {
                    using (var chain = new X509Chain())
                    {
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                        bool isChainValid = chain.Build(cert);
                        if (!isChainValid)
                        {
                            return Unauthorized(new { message = "Chứng thư số không hợp lệ hoặc đã bị thu hồi bởi CA." });
                        }
                    }
                }

                // 4. Xác thực chữ ký số bằng Khóa công khai (Public Key) từ Cert
                byte[] challengeBytes = Convert.FromBase64String(request.Challenge);
                byte[] signatureBytes = Convert.FromBase64String(request.Signature);

                using var rsa = cert.GetRSAPublicKey();
                if (rsa == null)
                {
                    return BadRequest(new { message = "Chứng thư số không hỗ trợ thuật toán RSA." });
                }

                // Kiểm tra chữ ký số
                bool isSignatureValid = rsa.VerifyData(
                    challengeBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );

                if (!isSignatureValid)
                {
                    return Unauthorized(new { message = "Chữ ký số không hợp lệ." });
                }

                // 5. Đọc định danh người dùng từ Cert (Common Name - CN)
                string employeeId = cert.GetNameInfo(X509NameType.SimpleName, false);
                if (string.IsNullOrEmpty(employeeId))
                {
                    employeeId = cert.Subject;
                }

                // Xóa Challenge khỏi cache để tránh Replay Attack
                await _cache.RemoveAsync($"pki_nonce:{request.Challenge}");

                // 6. Phát hành JWT token nội bộ
                // [CG2] Không có fallback hardcode — fail fast nếu secret chưa cấu hình
                var tokenHandler = new JwtSecurityTokenHandler();
                var secretKey = _configuration["Jwt:Secret"]
                    ?? throw new InvalidOperationException("[CG2] Jwt:Secret chưa được cấu hình.");
                var key = Encoding.UTF8.GetBytes(secretKey);

                // [CG2+CG5] isPremium phải là server-side claim trong JWT
                // KHÔNG để frontend tự set isPremium trong localStorage
                // TODO Sprint 2: Query DB để lấy trạng thái isPremium thực của nhân viên
                var isPremiumClaim = "false"; // Mặc định false — chỉ true sau khi thanh toán thành công

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, employeeId),
                    new Claim(ClaimTypes.Name, employeeId),
                    new Claim(ClaimTypes.Email, $"{employeeId}@mybank.com.vn"),
                    // [RBAC FIX] Dùng AppRoles.Learner — PKI login mặc định là Learner
                    // TODO Sprint 2: Tra DB lấy role thật của nhân viên
                    new Claim(ClaimTypes.Role, AppRoles.Learner),
                    // [CG5: Economic Engine] isPremium trong JWT claim — không thể giả mạo từ client
                    new Claim("isPremium", isPremiumClaim)
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(8),
                    Issuer = "AegisQuiz.PKI",
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                // [CG2 FIX] isPremium đọc từ JWT claim — không hardcode true
                // Frontend chỉ decode JWT để lấy isPremium, không tự set localStorage
                var userProfile = new
                {
                    id        = "pki_" + employeeId,
                    name      = employeeId + " (PKI)",
                    email     = $"{employeeId}@mybank.com.vn",
                    // [RBAC FIX] Dùng AppRoles.Learner
                    role      = AppRoles.Learner,
                    isPremium = false  // Server sẽ trả token mới isPremium=true sau khi thanh toán
                };

                return Ok(new
                {
                    token = tokenString,
                    user = userProfile
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = $"Lỗi xác thực chữ ký số: {ex.Message}" });
            }
        }
    }

    public class PkiLoginRequest
    {
        public string Challenge { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string CertificateBase64 { get; set; } = string.Empty;
    }
}
