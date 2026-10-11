using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisShield Offline Synchronization Service]
    /// Tiếp nhận và thẩm định tính hợp lệ của chuỗi Merkle Chain nộp bài offline khi thí sinh kết nối lại mạng.
    /// </summary>
    public interface IOfflineSyncService
    {
        Task<OfflineSyncResultDto> ProcessOfflineSyncAsync(OfflineSyncRequestDto request, CancellationToken ct = default);
    }

    public class OfflineSyncRequestDto
    {
        public string ExamSessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public long StartedAt { get; set; }
        public long LastUpdatedAt { get; set; }
        public Dictionary<string, OfflineAnswerItemDto> Answers { get; set; } = new();
        public string MerkleHeadHash { get; set; } = string.Empty;
    }

    public class OfflineAnswerItemDto
    {
        public string SelectedOption { get; set; } = string.Empty;
        public long AnsweredAt { get; set; }
        public string ProofHash { get; set; } = string.Empty;
    }

    public class OfflineSyncResultDto
    {
        public bool Success { get; set; }
        public int AcceptedAnswersCount { get; set; }
        public string VerifiedMerkleHeadHash { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
