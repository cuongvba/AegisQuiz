using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [AegisGeoNet NTP Synchronization Service Implementation]
    /// Đo đạc và chuẩn hóa độ lệch đồng hồ máy khách - máy chủ theo tiêu chuẩn RFC 5905 (Network Time Protocol v4).
    /// </summary>
    public class NtpTimeSyncService : INtpTimeSyncService
    {
        private readonly ILogger<NtpTimeSyncService> _logger;
        private static readonly long UnixEpochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

        public NtpTimeSyncService(ILogger<NtpTimeSyncService> logger)
        {
            _logger = logger;
        }

        private static long GetCurrentMicroseconds()
        {
            // Tính toán Microseconds chuẩn UTC
            var ticksSinceEpoch = DateTime.UtcNow.Ticks - UnixEpochTicks;
            return ticksSinceEpoch / 10;
        }

        public Task<NtpSyncResponseDto> SynchronizeClockAsync(NtpSyncRequestDto request, CancellationToken ct = default)
        {
            var serverReceiveUs = GetCurrentMicroseconds();
            
            // Sinh mã bí mật phiên (Ephemeral Session Secret) bảo vệ lượt cướp chuông
            var secretBytes = RandomNumberGenerator.GetBytes(16);
            var sessionSecret = Convert.ToHexString(secretBytes).ToLowerInvariant();

            var serverTransmitUs = GetCurrentMicroseconds();

            var response = new NtpSyncResponseDto
            {
                ClientSendTimestampUs = request.ClientSendTimestampUs,
                ServerReceiveTimestampUs = serverReceiveUs,
                ServerTransmitTimestampUs = serverTransmitUs,
                SessionSecretToken = sessionSecret
            };

            return Task.FromResult(response);
        }

        public long NormalizeFingerDownTimestamp(long clientFingerDownTimestamp, double clockSkewMs)
        {
            // Chuyển clockSkewMs thành microseconds
            long skewUs = (long)(clockSkewMs * 1000.0);
            return clientFingerDownTimestamp - skewUs;
        }

        public bool VerifyBuzzerSignature(string roomId, string playerId, long clientFingerDownTimestamp, string signatureHex, string sessionSecret)
        {
            if (string.IsNullOrWhiteSpace(signatureHex) || string.IsNullOrWhiteSpace(sessionSecret))
                return false;

            try
            {
                var payload = $"{roomId}:{playerId}:{clientFingerDownTimestamp}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sessionSecret));
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var computedHex = Convert.ToHexString(computedHash).ToLowerInvariant();

                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedHex),
                    Encoding.UTF8.GetBytes(signatureHex.ToLowerInvariant()));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[NtpTimeSyncService] Xác thực chữ ký chuông thất bại.");
                return false;
            }
        }
    }
}
