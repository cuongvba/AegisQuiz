using System;
using System.Security.Cryptography;

namespace AegisQuiz.Application.Common.Security
{
    /// <summary>
    /// [Enterprise Security] Quản lý băm và kiểm tra mật khẩu tuân thủ NIST SP 800-63B.
    /// Sử dụng Salted PBKDF2 với SHA-256, 100.000 vòng lặp và FixedTimeEquals chống timing attack.
    /// </summary>
    public static class PasswordSecurityHelper
    {
        private const int SaltByteSize = 16;
        private const int HashByteSize = 32;
        private const int DefaultIterations = 100_000;
        private const string VersionPrefix = "pbkdf2_sha256";

        /// <summary>
        /// Băm mật khẩu người dùng với muối ngẫu nhiên 16 bytes.
        /// Định dạng chuỗi kết quả: pbkdf2_sha256:100000:{saltBase64}:{hashBase64}
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Mật khẩu không được để trống.", nameof(password));

            var salt = RandomNumberGenerator.GetBytes(SaltByteSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                DefaultIterations,
                HashAlgorithmName.SHA256,
                HashByteSize);

            return $"{VersionPrefix}:{DefaultIterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Xác thực mật khẩu người dùng nhập vào so với chuỗi băm lưu trong DB.
        /// Chống tấn công kênh kề qua FixedTimeEquals.
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            try
            {
                var parts = storedHash.Split(':');
                if (parts.Length != 4 || parts[0] != VersionPrefix)
                    return false;

                if (!int.TryParse(parts[1], out int iterations) || iterations <= 0)
                    return false;

                var salt = Convert.FromBase64String(parts[2]);
                var expectedHash = Convert.FromBase64String(parts[3]);

                var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
