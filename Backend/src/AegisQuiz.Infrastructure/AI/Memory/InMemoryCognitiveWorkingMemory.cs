using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.AI.Memory
{
    /// <summary>
    /// [AegisAI Sovereign] Bộ nhớ làm việc nhận thức In-Memory với cơ chế tự hủy an toàn (Safe Ephemeral Memory)
    /// Lưu trữ các vết tư duy (ThinkingTraces) của các phiên làm việc đang chạy để hỗ trợ xoay khóa không mất dữ liệu.
    /// </summary>
    public class InMemoryCognitiveWorkingMemory : ICognitiveWorkingMemory
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<InMemoryCognitiveWorkingMemory> _logger;
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(20);

        public InMemoryCognitiveWorkingMemory(
            IMemoryCache cache,
            ILogger<InMemoryCognitiveWorkingMemory> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        private static string GetCacheKey(string sessionId) => $"cog_trace_{sessionId}";

        public Task SaveTraceAsync(ThinkingTraceContext trace, TimeSpan? ttl = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(trace.SessionId))
            {
                trace.SessionId = Guid.NewGuid().ToString("N");
            }

            trace.LastUpdatedAt = DateTime.UtcNow;
            var key = GetCacheKey(trace.SessionId);
            _cache.Set(key, trace, ttl ?? DefaultTtl);

            _logger.LogDebug("[CognitiveMemory] Đã lưu ThinkingTrace cho Session {SessionId}, Steps: {Steps}, Scratchpad: {Length} chars",
                trace.SessionId, trace.CompletedSteps, trace.AccumulatedScratchpad.Length);

            return Task.CompletedTask;
        }

        public Task<ThinkingTraceContext?> GetTraceAsync(string sessionId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return Task.FromResult<ThinkingTraceContext?>(null);

            var key = GetCacheKey(sessionId);
            _cache.TryGetValue<ThinkingTraceContext>(key, out var trace);
            return Task.FromResult(trace);
        }

        public Task AppendScratchpadAsync(string sessionId, string thoughtChunk, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(thoughtChunk))
                return Task.CompletedTask;

            var key = GetCacheKey(sessionId);
            if (_cache.TryGetValue<ThinkingTraceContext>(key, out var trace) && trace != null)
            {
                trace.AccumulatedScratchpad += "\n" + thoughtChunk;
                trace.LastUpdatedAt = DateTime.UtcNow;
                _cache.Set(key, trace, DefaultTtl);
            }
            return Task.CompletedTask;
        }

        public Task RecordFailoverEventAsync(string sessionId, KeyFailoverEvent failoverEvent, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return Task.CompletedTask;

            var key = GetCacheKey(sessionId);
            if (_cache.TryGetValue<ThinkingTraceContext>(key, out var trace) && trace != null)
            {
                trace.RotationHistory.Add(failoverEvent);
                trace.LastUpdatedAt = DateTime.UtcNow;
                _cache.Set(key, trace, DefaultTtl);
            }
            return Task.CompletedTask;
        }

        public Task InvalidateTraceAsync(string sessionId, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                _cache.Remove(GetCacheKey(sessionId));
            }
            return Task.CompletedTask;
        }
    }
}
