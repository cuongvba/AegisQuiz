using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Enterprise Fortress — Epoch 3] Triển khai dịch vụ kiểm toán chuỗi băm bất biến (Tamper-Evident Hash Chaining).
    /// </summary>
    public class ExamAuditService : IExamAuditService
    {
        private readonly AegisQuizDbContext _db;
        private readonly ILogger<ExamAuditService> _logger;
        private static readonly SemaphoreSlim _chainLock = new(1, 1);

        public ExamAuditService(AegisQuizDbContext db, ILogger<ExamAuditService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ExamAuditLog> AppendAuditLogAsync(
            Guid tenantId,
            Guid examId,
            Guid userId,
            string action,
            string severity = "INFO",
            string details = "{}",
            string clientIp = "127.0.0.1",
            string userAgent = "",
            CancellationToken ct = default)
        {
            await _chainLock.WaitAsync(ct);
            try
            {
                // 1. Tự động kiểm tra và tạo bảng nếu chưa có (Self-healing DDL)
                await EnsureTableExistsAsync(ct);

                // 2. Lấy block kiểm toán mới nhất của ExamId này
                var lastLog = await _db.ExamAuditLogs
                    .AsNoTracking()
                    .Where(a => a.ExamId == examId)
                    .OrderByDescending(a => a.SequenceNumber)
                    .FirstOrDefaultAsync(ct);

                long nextSequence = (lastLog != null) ? lastLog.SequenceNumber + 1 : 1;
                string prevHash = (lastLog != null) ? lastLog.CurrentHash : "GENESIS_0000000000000000";
                var now = DateTime.UtcNow;

                // 3. Tính toán SHA-256 Hash Chaining
                string payloadToHash = $"{tenantId}|{examId}|{userId}|{action}|{now:O}|{clientIp}|{nextSequence}|{prevHash}|{details}";
                string currentHash = ComputeSha256Hex(payloadToHash);

                var logEntry = new ExamAuditLog
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ExamId = examId,
                    UserId = userId,
                    Action = action,
                    Severity = severity,
                    Details = details,
                    ClientIp = string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp,
                    UserAgent = userAgent ?? string.Empty,
                    SequenceNumber = nextSequence,
                    PreviousLogHash = prevHash,
                    CurrentHash = currentHash,
                    TimestampUtc = now
                };

                _db.ExamAuditLogs.Add(logEntry);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "[AuditFortress] Ghi nhận khối kiểm toán #{Seq} (Hash: {Hash}) cho Kỳ thi {ExamId}, Hành động: {Action}",
                    nextSequence, currentHash[..8], examId, action);

                return logEntry;
            }
            finally
            {
                _chainLock.Release();
            }
        }

        public async Task<AuditTrailVerificationResultDto> VerifyAuditIntegrityAsync(Guid examId, CancellationToken ct = default)
        {
            await EnsureTableExistsAsync(ct);

            var logs = await _db.ExamAuditLogs
                .AsNoTracking()
                .Where(a => a.ExamId == examId)
                .OrderBy(a => a.SequenceNumber)
                .ToListAsync(ct);

            var result = new AuditTrailVerificationResultDto
            {
                ExamId = examId,
                TotalBlocksChecked = logs.Count,
                IsChainValid = true,
                VerifiedAtUtc = DateTime.UtcNow
            };

            if (logs.Count == 0)
            {
                result.LatestBlockHash = "EMPTY_NO_LOGS";
                return result;
            }

            string expectedPrevHash = "GENESIS_0000000000000000";

            for (int i = 0; i < logs.Count; i++)
            {
                var block = logs[i];

                // Kiểm tra liên kết với khối trước
                if (block.PreviousLogHash != expectedPrevHash)
                {
                    result.IsChainValid = false;
                    result.BrokenAtBlockId = block.Id.ToString();
                    result.BrokenSequenceNumber = block.SequenceNumber;
                    result.AnomalyDescription = $"Mắt xích bị đứt tại khối #{block.SequenceNumber}! PreviousLogHash lưu trong DB không khớp với CurrentHash của khối trước đó.";
                    _logger.LogCritical("[AuditFortress] 🚨 PHÁT HIỆN GIAN LẬN KIỂM TOÁN: {Description}", result.AnomalyDescription);
                    return result;
                }

                // Tính toán lại mã băm thực tế của khối hiện tại để phát hiện việc chỉnh sửa nội dung
                string payloadToHash = $"{block.TenantId}|{block.ExamId}|{block.UserId}|{block.Action}|{block.TimestampUtc:O}|{block.ClientIp}|{block.SequenceNumber}|{block.PreviousLogHash}|{block.Details}";
                string recomputedHash = ComputeSha256Hex(payloadToHash);

                if (recomputedHash != block.CurrentHash)
                {
                    result.IsChainValid = false;
                    result.BrokenAtBlockId = block.Id.ToString();
                    result.BrokenSequenceNumber = block.SequenceNumber;
                    result.AnomalyDescription = $"Dữ liệu khối #{block.SequenceNumber} đã bị chỉnh sửa trái phép! Băm thực tế {recomputedHash[..8]} != Băm lưu trữ {block.CurrentHash[..8]}.";
                    _logger.LogCritical("[AuditFortress] 🚨 PHÁT HIỆN DỮ LIỆU BỊ CAN THIỆP: {Description}", result.AnomalyDescription);
                    return result;
                }

                expectedPrevHash = block.CurrentHash;
            }

            result.LatestBlockHash = expectedPrevHash;
            _logger.LogInformation("[AuditFortress] ✅ Xác minh thành công tính toàn vẹn 100% của {Count} khối kiểm toán cho Kỳ thi {ExamId}", logs.Count, examId);
            return result;
        }

        public async Task<List<ExamAuditLog>> GetAuditTrailAsync(Guid examId, Guid? userId = null, int limit = 100, CancellationToken ct = default)
        {
            await EnsureTableExistsAsync(ct);

            var query = _db.ExamAuditLogs
                .AsNoTracking()
                .Where(a => a.ExamId == examId);

            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                query = query.Where(a => a.UserId == userId.Value);
            }

            return await query
                .OrderByDescending(a => a.SequenceNumber)
                .Take(limit)
                .ToListAsync(ct);
        }

        private static string ComputeSha256Hex(string input)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        private static bool _tableEnsured = false;
        private async Task EnsureTableExistsAsync(CancellationToken ct)
        {
            if (_tableEnsured) return;

            try
            {
                var sql = @"
                    CREATE TABLE IF NOT EXISTS ""ExamAuditLogs"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""TenantId"" uuid NOT NULL,
                        ""ExamId"" uuid NOT NULL,
                        ""UserId"" uuid NOT NULL,
                        ""Action"" text NOT NULL,
                        ""Severity"" text NOT NULL DEFAULT 'INFO',
                        ""Details"" text NOT NULL DEFAULT '{}',
                        ""ClientIp"" text NOT NULL DEFAULT '127.0.0.1',
                        ""UserAgent"" text NOT NULL DEFAULT '',
                        ""SequenceNumber"" bigint NOT NULL,
                        ""PreviousLogHash"" text NOT NULL,
                        ""CurrentHash"" text NOT NULL,
                        ""TimestampUtc"" timestamp with time zone NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_ExamAuditLogs_ExamId_SequenceNumber"" ON ""ExamAuditLogs"" (""ExamId"", ""SequenceNumber"");
                    CREATE INDEX IF NOT EXISTS ""IX_ExamAuditLogs_ExamId_UserId"" ON ""ExamAuditLogs"" (""ExamId"", ""UserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_ExamAuditLogs_TimestampUtc"" ON ""ExamAuditLogs"" (""TimestampUtc"");
                ";
                await _db.Database.ExecuteSqlRawAsync(sql, ct);
                _tableEnsured = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AuditFortress] Self-healing DDL check for ExamAuditLogs skipped or table already exists.");
                _tableEnsured = true;
            }
        }
    }
}
