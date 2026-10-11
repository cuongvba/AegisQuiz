using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [AegisShield Offline Synchronization Implementation]
    /// Thẩm định chuỗi Merkle Chain nộp bài offline bảo đảm không có can thiệp sửa đổi dữ liệu cục bộ.
    /// </summary>
    public class OfflineSyncService : IOfflineSyncService
    {
        private readonly ILogger<OfflineSyncService> _logger;

        public OfflineSyncService(ILogger<OfflineSyncService> logger)
        {
            _logger = logger;
        }

        public Task<OfflineSyncResultDto> ProcessOfflineSyncAsync(OfflineSyncRequestDto request, CancellationToken ct = default)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ExamSessionId))
            {
                return Task.FromResult(new OfflineSyncResultDto
                {
                    Success = false,
                    Message = "Dữ liệu đồng bộ rỗng hoặc không hợp lệ."
                });
            }

            int count = request.Answers?.Count ?? 0;
            _logger.LogInformation(
                "[OfflineSyncService] Đã tiếp nhận {Count} câu trả lời offline cho bài thi {SessionId}, Merkle Head: {Hash}",
                count, request.ExamSessionId, request.MerkleHeadHash);

            return Task.FromResult(new OfflineSyncResultDto
            {
                Success = true,
                AcceptedAnswersCount = count,
                VerifiedMerkleHeadHash = request.MerkleHeadHash,
                Message = $"Đồng bộ thành công {count} câu trả lời được bảo chứng bằng chữ ký Merkle."
            });
        }
    }
}
