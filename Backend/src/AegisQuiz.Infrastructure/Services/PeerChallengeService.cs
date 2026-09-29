using System;
using System.Collections.Concurrent;
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
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Epoch 4: Community Ecosystem] Triển khai động cơ thách đấu đồng đẳng và link lan tỏa Peer Challenge.
    /// </summary>
    public class PeerChallengeService : IPeerChallengeService
    {
        private readonly AegisQuizDbContext _db;
        private readonly ICacheService _cache;
        private readonly ILogger<PeerChallengeService> _logger;

        private static readonly ConcurrentDictionary<string, PeerChallengeDto> _inMemoryChallenges = new();

        public PeerChallengeService(
            AegisQuizDbContext db,
            ICacheService cache,
            ILogger<PeerChallengeService> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PeerChallengeDto> CreateChallengeAsync(
            Guid examId,
            Guid creatorUserId,
            string creatorName,
            double creatorScore,
            CancellationToken ct = default)
        {
            var topic = await _db.BankTopics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == examId || t.Code == "TOAN_GDPT_2025", ct);

            string title = topic?.Name ?? "Thử Thách Toán Học GDPT 2025";
            int totalQ = 22; // 22 câu chuẩn đề tốt nghiệp

            // Tạo mã short code độc bản 8 ký tự
            string rawToken = $"{examId}:{creatorUserId}:{DateTime.UtcNow.Ticks}";
            using var sha = SHA256.Create();
            string hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken)));
            string shareCode = $"AEGIS-{hash[..6]}";

            var challenge = new PeerChallengeDto
            {
                ShareCode = shareCode,
                ExamId = examId,
                ExamTitle = title,
                CreatorName = string.IsNullOrWhiteSpace(creatorName) ? "Thí sinh ẩn danh" : creatorName,
                CreatorScore = creatorScore,
                TotalQuestions = totalQ,
                DurationMinutes = 50,
                CreatedAtUtc = DateTime.UtcNow,
                Leaderboard = new List<PeerChallengeParticipantDto>
                {
                    new()
                    {
                        CandidateIdentifier = creatorName,
                        Score = creatorScore,
                        TimeSpentSeconds = 1200,
                        CompletedAtUtc = DateTime.UtcNow
                    }
                }
            };

            _inMemoryChallenges[shareCode] = challenge;
            await _cache.SetAsync($"challenge:{shareCode}", challenge, TimeSpan.FromDays(7), ct);

            _logger.LogInformation("[PeerChallenge] Đã tạo mã thách đấu {Code} cho đề thi {Title}", shareCode, title);
            return challenge;
        }

        public async Task<PeerChallengeDto?> GetChallengeByCodeAsync(string shareCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(shareCode)) return null;

            string normalized = shareCode.Trim().ToUpperInvariant();
            var cached = await _cache.GetAsync<PeerChallengeDto>($"challenge:{normalized}", ct);
            if (cached != null) return cached;

            _inMemoryChallenges.TryGetValue(normalized, out var challenge);
            return challenge;
        }

        public async Task<PeerChallengeDto> SubmitChallengeResultAsync(
            string shareCode,
            string candidateIdentifier,
            double score,
            int timeSpentSec,
            CancellationToken ct = default)
        {
            string normalized = shareCode.Trim().ToUpperInvariant();
            var challenge = await GetChallengeByCodeAsync(normalized, ct);

            if (challenge == null)
            {
                challenge = await CreateChallengeAsync(Guid.Empty, Guid.NewGuid(), "Host", 8.0, ct);
            }

            challenge.Leaderboard.Add(new PeerChallengeParticipantDto
            {
                CandidateIdentifier = candidateIdentifier,
                Score = score,
                TimeSpentSeconds = timeSpentSec,
                CompletedAtUtc = DateTime.UtcNow
            });

            // Sắp xếp bảng xếp hạng thách đấu: Điểm cao nhất trước, cùng điểm thì thời gian nhanh hơn xếp trên
            challenge.Leaderboard = challenge.Leaderboard
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.TimeSpentSeconds)
                .ToList();

            _inMemoryChallenges[normalized] = challenge;
            await _cache.SetAsync($"challenge:{normalized}", challenge, TimeSpan.FromDays(7), ct);

            _logger.LogInformation("[PeerChallenge] Thí sinh {Candidate} nộp điểm thách đấu {Score} vào mã {Code}", candidateIdentifier, score, normalized);
            return challenge;
        }
    }
}
