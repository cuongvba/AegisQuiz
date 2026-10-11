using System;
using System.Threading;
using System.Threading.Tasks;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisGeoNet NTP Synchronization Service]
    /// Cung cấp giải thuật chuẩn hóa đồng hồ thời gian thực vi mô giữa Thí sinh và Máy chủ Đấu trường.
    /// Khắc phục hoàn toàn chênh lệch độ trễ ánh sáng xuyên lục địa (Cross-Pacific Latency Compensation).
    /// </summary>
    public interface INtpTimeSyncService
    {
        /// <summary>
        /// Tiếp nhận dấu thời gian T1 (Client Send) và phản hồi T2 (Server Receive) + T3 (Server Transmit).
        /// </summary>
        Task<NtpSyncResponseDto> SynchronizeClockAsync(NtpSyncRequestDto request, CancellationToken ct = default);

        /// <summary>
        /// Chuẩn hóa dấu thời gian cướp chuông của thí sinh dựa trên độ lệch đồng hồ đã được xác minh.
        /// </summary>
        long NormalizeFingerDownTimestamp(long clientFingerDownTimestamp, double clockSkewMs);

        /// <summary>
        /// Xác thực tính toàn vẹn chữ ký HMAC của gói tin cướp chuông bảo vệ chống gian lận Client.
        /// </summary>
        bool VerifyBuzzerSignature(string roomId, string playerId, long clientFingerDownTimestamp, string signatureHex, string sessionSecret);
    }

    public class NtpSyncRequestDto
    {
        public string ClientSessionId { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
        /// <summary>Dấu thời gian Client phát lệnh đồng bộ (T1 - Unix Epoch Microseconds)</summary>
        public long ClientSendTimestampUs { get; set; }
    }

    public class NtpSyncResponseDto
    {
        /// <summary>Dấu thời gian Client phát lệnh đồng bộ ban đầu (T1)</summary>
        public long ClientSendTimestampUs { get; set; }

        /// <summary>Dấu thời gian Server nhận gói tin (T2 - Unix Epoch Microseconds)</summary>
        public long ServerReceiveTimestampUs { get; set; }

        /// <summary>Dấu thời gian Server phản hồi gói tin (T3 - Unix Epoch Microseconds)</summary>
        public long ServerTransmitTimestampUs { get; set; }

        /// <summary>Khóa phiên dùng cho chữ ký số HMAC của lượt cướp chuông (Ephemeral Token)</summary>
        public string SessionSecretToken { get; set; } = string.Empty;
    }
}
