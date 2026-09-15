using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.AI
{
    public class ManagedKeyInfo
    {
        public string Key { get; set; } = string.Empty;
        public DateTime? QuarantinedUntilUtc { get; set; }
        public long TotalCalls;
        public long FailedCalls;
        public bool IsQuarantined => QuarantinedUntilUtc.HasValue && QuarantinedUntilUtc.Value > DateTime.UtcNow;
    }

    public class AiKeyPoolManager
    {
        private readonly ConcurrentDictionary<AiProviderType, List<ManagedKeyInfo>> _keyPools = new();
        private readonly ConcurrentDictionary<AiProviderType, int> _roundRobinIndices = new();
        private readonly int _quarantineDurationSeconds;

        public AiKeyPoolManager(int quarantineDurationSeconds = 60)
        {
            _quarantineDurationSeconds = quarantineDurationSeconds;
        }

        public void RegisterKeys(AiProviderType provider, IEnumerable<string> keys)
        {
            var validKeys = keys
                .Where(k => !string.IsNullOrWhiteSpace(k) && !k.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .Select(k => new ManagedKeyInfo { Key = k.Trim() })
                .ToList();

            if (validKeys.Count > 0)
            {
                _keyPools[provider] = validKeys;
                _roundRobinIndices[provider] = 0;
            }
        }

        public string? GetAvailableKey(AiProviderType provider)
        {
            if (!_keyPools.TryGetValue(provider, out var pool) || pool.Count == 0)
            {
                return null;
            }

            lock (pool)
            {
                var availableKeys = pool.Where(k => !k.IsQuarantined).ToList();
                if (availableKeys.Count == 0)
                {
                    // Nếu tất cả các key đều bị cách ly, thử lấy key có thời gian cách ly ngắn nhất sắp hết hạn
                    var earliestExpiring = pool.OrderBy(k => k.QuarantinedUntilUtc).FirstOrDefault();
                    if (earliestExpiring != null)
                    {
                        earliestExpiring.QuarantinedUntilUtc = null; // Phục hồi cưỡng bức để thử lại
                        return earliestExpiring.Key;
                    }
                    return null;
                }

                int currentIndex = _roundRobinIndices.GetOrAdd(provider, 0);
                int selectedIndex = Math.Abs(currentIndex) % availableKeys.Count;
                _roundRobinIndices[provider] = currentIndex + 1;

                var selectedKeyInfo = availableKeys[selectedIndex];
                Interlocked.Increment(ref selectedKeyInfo.TotalCalls);
                return selectedKeyInfo.Key;
            }
        }

        public void ReportRateLimitOrFailure(AiProviderType provider, string key, bool isRateLimit = true)
        {
            if (_keyPools.TryGetValue(provider, out var pool))
            {
                var keyInfo = pool.FirstOrDefault(k => k.Key == key);
                if (keyInfo != null)
                {
                    Interlocked.Increment(ref keyInfo.FailedCalls);
                    // Cách ly key trong khoảng thời gian quy định
                    keyInfo.QuarantinedUntilUtc = DateTime.UtcNow.AddSeconds(_quarantineDurationSeconds);
                    Console.WriteLine($"[AiKeyPoolManager] Key cho nhà cung cấp {provider} đã bị cách ly trong {_quarantineDurationSeconds}s do lỗi {(isRateLimit ? "429 Rate Limit" : "kết nối")}.");
                }
            }
        }

        public Dictionary<AiProviderType, List<ManagedKeyInfo>> GetAllPoolStatus()
        {
            return _keyPools.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
        }
    }
}
