using System;
using System.Security.Cryptography;
using System.Text;

namespace AegisQuiz.Infrastructure.Security
{
    /// <summary>
    /// [AegisAI Security Sovereign]
    /// Mã hóa và giải mã khóa API cá nhân sử dụng thuật toán chuẩn quân sự AES-256-GCM (Authenticated Encryption).
    /// </summary>
    public static class ApiKeyEncryptionHelper
    {
        // 12 bytes nonce khuyến nghị cho AES-GCM
        private const int NonceSize = 12;
        // 16 bytes auth tag
        private const int TagSize = 16;

        /// <summary>
        /// Tạo key 256-bit chuẩn từ chuỗi cấu hình master secret
        /// </summary>
        private static byte[] DeriveKey(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                secret = "AegisQuiz-Master-Secret-Quantum-Resistant-2026-Vault";
            }
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
        }

        /// <summary>
        /// Mã hóa khóa API thành chuỗi Base64: [Nonce (12B)] + [Tag (16B)] + [Ciphertext]
        /// </summary>
        public static string Encrypt(string plainText, string masterSecret)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var keyBytes = DeriveKey(masterSecret);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);

            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            var tag = new byte[TagSize];
            var cipherBytes = new byte[plainBytes.Length];

            using var aesGcm = new AesGcm(keyBytes, TagSize);
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

            // Gộp Nonce + Tag + Ciphertext thành 1 mảng kết quả
            var combined = new byte[NonceSize + TagSize + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
            Buffer.BlockCopy(cipherBytes, 0, combined, NonceSize + TagSize, cipherBytes.Length);

            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// Giải mã chuỗi Base64 về plain text
        /// </summary>
        public static string Decrypt(string encryptedBase64, string masterSecret)
        {
            if (string.IsNullOrWhiteSpace(encryptedBase64))
                return string.Empty;

            try
            {
                var combined = Convert.FromBase64String(encryptedBase64);
                if (combined.Length < NonceSize + TagSize)
                    return string.Empty;

                var nonce = new byte[NonceSize];
                var tag = new byte[TagSize];
                var cipherLength = combined.Length - NonceSize - TagSize;
                var cipherBytes = new byte[cipherLength];

                Buffer.BlockCopy(combined, 0, nonce, 0, NonceSize);
                Buffer.BlockCopy(combined, NonceSize, tag, 0, TagSize);
                Buffer.BlockCopy(combined, NonceSize + TagSize, cipherBytes, 0, cipherLength);

                var plainBytes = new byte[cipherLength];
                var keyBytes = DeriveKey(masterSecret);

                using var aesGcm = new AesGcm(keyBytes, TagSize);
                aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return string.Empty; // Tránh vỡ flow nếu key invalid/tampered
            }
        }

        /// <summary>
        /// Tạo vân tay an toàn để hiển thị trên UI (vd: 'sk-d...7f3a')
        /// </summary>
        public static string GenerateFingerprint(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return "••••••••";

            apiKey = apiKey.Trim();
            if (apiKey.Length <= 8)
                return "••••" + apiKey[^2..];

            var prefix = apiKey[..Math.Min(4, apiKey.Length)];
            var suffix = apiKey[^Math.Min(4, apiKey.Length)..];
            return $"{prefix}••••{suffix}";
        }
    }
}
