using System;
using System.Threading;
using System.Threading.Tasks;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Enterprise Caching Architecture] Dịch vụ bộ đệm đa tầng (L1 In-Memory + L2 Distributed Redis).
    /// Hỗ trợ Multi-Tenant Isolation Keying và Graceful Fallback khi Redis gián đoạn.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>Lấy dữ liệu từ Cache (L1 -> L2)</summary>
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

        /// <summary>Ghi dữ liệu vào Cache (đồng bộ cả L1 và L2)</summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);

        /// <summary>Xóa khóa khỏi Cache</summary>
        Task RemoveAsync(string key, CancellationToken ct = default);

        /// <summary>Xóa tập hợp khóa theo tiền tố (ví dụ theo Tenant hoặc Category)</summary>
        Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);

        /// <summary>Cache-Aside pattern: Lấy từ Cache nếu có, nếu miss thì gọi factory để tính toán và lưu Cache</summary>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken ct = default);
    }
}
