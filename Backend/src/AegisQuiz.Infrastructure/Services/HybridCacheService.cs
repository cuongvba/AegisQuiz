using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [World-Class Architecture] Hybrid Two-Tier Cache Service.
    /// L1: Ultra-fast In-Memory Cache (microseconds latency).
    /// L2: Distributed Redis Cache (shared state across cluster nodes).
    /// Graceful Degradation: If Redis is offline or unstable, smoothly degrades to L1 without failing requests.
    /// </summary>
    public class HybridCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache? _distributedCache;
        private readonly ILogger<HybridCacheService> _logger;
        private readonly ITenantContext? _tenantContext;
        private bool _redisWarningLogged = false;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        public HybridCacheService(
            IMemoryCache memoryCache,
            ILogger<HybridCacheService> logger,
            IDistributedCache? distributedCache = null,
            ITenantContext? tenantContext = null)
        {
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _logger = logger;
            _tenantContext = tenantContext;
        }

        private string BuildKey(string rawKey)
        {
            var tenantId = _tenantContext?.CurrentTenantId.ToString() ?? "global";
            return $"aegis:{tenantId}:{rawKey}";
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            var scopedKey = BuildKey(key);

            // 1. Kiểm tra L1 Memory Cache
            if (_memoryCache.TryGetValue(scopedKey, out T? l1Value))
            {
                AegisQuiz.Application.Common.AegisObservability.CacheHitCounter.Add(1);
                return l1Value;
            }

            // 2. Kiểm tra L2 Distributed Cache (Redis)
            if (_distributedCache != null)
            {
                try
                {
                    var bytes = await _distributedCache.GetAsync(scopedKey, ct);
                    if (bytes != null && bytes.Length > 0)
                    {
                        var deserialized = JsonSerializer.Deserialize<T>(bytes, JsonOpts);
                        if (deserialized != null)
                        {
                            // Backfill vào L1 với TTL ngắn hạn (5 phút)
                            _memoryCache.Set(scopedKey, deserialized, TimeSpan.FromMinutes(5));
                            AegisQuiz.Application.Common.AegisObservability.CacheHitCounter.Add(1);
                            return deserialized;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogRedisDegradation(ex, "GetAsync");
                }
            }

            AegisQuiz.Application.Common.AegisObservability.CacheMissCounter.Add(1);
            return default;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
        {
            if (value == null) return;
            var scopedKey = BuildKey(key);
            var ttl = expiration ?? TimeSpan.FromHours(1);

            // 1. Ghi vào L1 Memory Cache
            _memoryCache.Set(scopedKey, value, ttl);

            // 2. Ghi vào L2 Distributed Cache (Redis)
            if (_distributedCache != null)
            {
                try
                {
                    var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOpts);
                    var entryOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = ttl
                    };
                    await _distributedCache.SetAsync(scopedKey, bytes, entryOptions, ct);
                }
                catch (Exception ex)
                {
                    LogRedisDegradation(ex, "SetAsync");
                }
            }
        }

        public async Task RemoveAsync(string key, CancellationToken ct = default)
        {
            var scopedKey = BuildKey(key);

            // Xóa L1
            _memoryCache.Remove(scopedKey);

            // Xóa L2
            if (_distributedCache != null)
            {
                try
                {
                    await _distributedCache.RemoveAsync(scopedKey, ct);
                }
                catch (Exception ex)
                {
                    LogRedisDegradation(ex, "RemoveAsync");
                }
            }
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        {
            // MemoryCache không hỗ trợ wildcard xóa theo prefix theo mặc định,
            // ta chỉ cần invalidate key cụ thể hoặc reset vùng cache khi cần.
            // Có thể mở rộng bằng Redis SCAN pattern.
            return Task.CompletedTask;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default)
        {
            var cached = await GetAsync<T>(key, ct);
            if (cached != null)
            {
                return cached;
            }

            var computed = await factory();
            if (computed != null)
            {
                await SetAsync(key, computed, expiration, ct);
            }
            return computed;
        }

        private void LogRedisDegradation(Exception ex, string op)
        {
            if (!_redisWarningLogged)
            {
                _logger.LogWarning(ex, "[HybridCacheService] L2 Redis Cache không khả dụng tại {Op}. Tự động chuyển tuyến an toàn (Degrade) sang L1 In-Memory Cache. Chi tiết: {Message}", op, ex.Message);
                _redisWarningLogged = true;
            }
            else
            {
                _logger.LogDebug("[HybridCacheService] Redis {Op} skipped: {Message}", op, ex.Message);
            }
        }
    }
}
