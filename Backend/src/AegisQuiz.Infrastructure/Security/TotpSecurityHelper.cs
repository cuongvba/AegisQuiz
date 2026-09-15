using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AegisQuiz.Infrastructure.Security
{
    /// <summary>
    /// [Enterprise Security] Công cụ mật mã học TOTP (Time-Based One-Time Password - RFC 6238)
    /// và Base32 (RFC 4648) cho Google Authenticator / Microsoft Authenticator / Apple Keychain.
    /// Thiết kế Zero-Dependency, hiệu năng cao, thread-safe.
    /// </summary>
    public static class TotpSecurityHelper
    {
        private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public class RecoveryCodeModel
        {
            public string Hash { get; set; } = string.Empty;
            public bool Used { get; set; } = false;
            public DateTime? UsedAt { get; set; }
        }

        // ── BASE32 ENCODE / DECODE (RFC 4648) ──────────────────────────────────
        public static string ToBase32String(byte[] data)
        {
            if (data == null || data.Length == 0) return string.Empty;

            var sb = new StringBuilder((data.Length * 8 + 4) / 5);
            int buffer = data[0];
            int next = 1;
            int bitsLeft = 8;

            while (bitsLeft > 0 || next < data.Length)
            {
                if (bitsLeft < 5)
                {
                    if (next < data.Length)
                    {
                        buffer = (buffer << 8) | (data[next++] & 0xFF);
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = 5 - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }

                int index = (buffer >> (bitsLeft - 5)) & 0x1F;
                bitsLeft -= 5;
                sb.Append(Base32Chars[index]);
            }

            return sb.ToString();
        }

        public static byte[] FromBase32String(string base32)
        {
            if (string.IsNullOrWhiteSpace(base32)) return Array.Empty<byte>();

            var clean = base32.Trim().Replace(" ", "").Replace("-", "").ToUpperInvariant();
            var output = new List<byte>((clean.Length * 5) / 8);

            int buffer = 0;
            int bitsLeft = 0;

            foreach (char c in clean)
            {
                int val = Base32Chars.IndexOf(c);
                if (val < 0) continue; // Bỏ qua ký tự không hợp lệ

                buffer = (buffer << 5) | val;
                bitsLeft += 5;

                if (bitsLeft >= 8)
                {
                    output.Add((byte)((buffer >> (bitsLeft - 8)) & 0xFF));
                    bitsLeft -= 8;
                }
            }

            return output.ToArray();
        }

        // ── TOTP GENERATION & VALIDATION (RFC 6238 / RFC 4226) ─────────────────
        /// <summary>
        /// Sinh khóa bí mật ngẫu nhiên (mặc định 20 bytes = 160 bits entropy theo chuẩn NIST).
        /// </summary>
        public static string GenerateSecretKey(int byteLength = 20)
        {
            var bytes = new byte[byteLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return ToBase32String(bytes);
        }

        /// <summary>
        /// Sinh mã xác thực 6 số TOTP cho một thời điểm cụ thể.
        /// </summary>
        public static string GenerateTotpCode(string base32Secret, long? unixTimestampSeconds = null, int stepSeconds = 30, int digits = 6)
        {
            var key = FromBase32String(base32Secret);
            if (key.Length == 0) return string.Empty;

            long timestamp = unixTimestampSeconds ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long counter = timestamp / stepSeconds;

            // Biểu diễn counter dưới dạng 8-byte Big-Endian
            byte[] counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counterBytes);
            }

            // Tính toán HMAC-SHA1
            using var hmac = new HMACSHA1(key);
            byte[] hash = hmac.ComputeHash(counterBytes);

            // Dynamic Truncation (RFC 4226)
            int offset = hash[^1] & 0x0F;
            int binaryCode = ((hash[offset] & 0x7F) << 24)
                           | ((hash[offset + 1] & 0xFF) << 16)
                           | ((hash[offset + 2] & 0xFF) << 8)
                           | (hash[offset + 3] & 0xFF);

            int modulo = (int)Math.Pow(10, digits);
            int otp = binaryCode % modulo;

            return otp.ToString(new string('0', digits));
        }

        /// <summary>
        /// Xác thực mã TOTP với khả năng bù trừ sai lệch đồng hồ (Clock Skew) +- tolerance chu kỳ (mỗi chu kỳ 30s).
        /// </summary>
        public static bool ValidateTotpCode(string base32Secret, string code, int toleranceSteps = 1, int stepSeconds = 30)
        {
            if (string.IsNullOrWhiteSpace(base32Secret) || string.IsNullOrWhiteSpace(code)) return false;

            var cleanCode = code.Trim().Replace(" ", "");
            if (cleanCode.Length != 6) return false;

            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            for (int i = -toleranceSteps; i <= toleranceSteps; i++)
            {
                long testTimestamp = currentTimestamp + (i * stepSeconds);
                var expectedCode = GenerateTotpCode(base32Secret, testTimestamp, stepSeconds);
                if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expectedCode),
                    Encoding.UTF8.GetBytes(cleanCode)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tạo URI chuẩn otpauth:// để render mã QR cho Google Authenticator, Microsoft Authenticator.
        /// </summary>
        public static string GenerateOtpAuthUri(string issuer, string userEmail, string base32Secret)
        {
            var encodedIssuer = Uri.EscapeDataString(issuer.Trim());
            var encodedEmail = Uri.EscapeDataString(userEmail.Trim().ToLowerInvariant());
            return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={base32Secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
        }

        // ── RECOVERY / BACKUP CODES ────────────────────────────────────────────
        /// <summary>
        /// Sinh danh sách mã khôi phục khẩn cấp (mặc định 8 mã, định dạng XXXX-XXXX).
        /// </summary>
        public static List<string> GenerateRecoveryCodes(int count = 8)
        {
            const string chars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"; // Bỏ ký tự dễ nhầm lẫn như 0, O, 1, I
            var codes = new List<string>(count);

            for (int i = 0; i < count; i++)
            {
                var bytes = new byte[8];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(bytes);
                }

                var sb = new StringBuilder(9);
                for (int j = 0; j < 8; j++)
                {
                    if (j == 4) sb.Append('-');
                    sb.Append(chars[bytes[j] % chars.Length]);
                }
                codes.Add(sb.ToString());
            }

            return codes;
        }

        public static string HashRecoveryCode(string rawCode)
        {
            var clean = rawCode.Trim().Replace("-", "").ToUpperInvariant();
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(clean));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        public static string SerializeRecoveryCodes(IEnumerable<string> rawCodes)
        {
            var models = new List<RecoveryCodeModel>();
            foreach (var code in rawCodes)
            {
                models.Add(new RecoveryCodeModel
                {
                    Hash = HashRecoveryCode(code),
                    Used = false,
                    UsedAt = null
                });
            }
            return JsonSerializer.Serialize(models);
        }

        /// <summary>
        /// Xác thực và tiêu thụ (Burn) một mã khôi phục dự phòng (chỉ dùng 1 lần duy nhất).
        /// </summary>
        public static bool ValidateAndConsumeRecoveryCode(string inputCode, ref string? recoveryCodesJson)
        {
            if (string.IsNullOrWhiteSpace(inputCode) || string.IsNullOrWhiteSpace(recoveryCodesJson))
                return false;

            try
            {
                var models = JsonSerializer.Deserialize<List<RecoveryCodeModel>>(recoveryCodesJson);
                if (models == null || models.Count == 0) return false;

                var inputHash = HashRecoveryCode(inputCode);
                var matched = models.Find(m => !m.Used && m.Hash == inputHash);
                if (matched != null)
                {
                    matched.Used = true;
                    matched.UsedAt = DateTime.UtcNow;
                    recoveryCodesJson = JsonSerializer.Serialize(models);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
