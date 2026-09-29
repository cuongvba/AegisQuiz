using System;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Epoch 4: Community Ecosystem] Dịch vụ ví & kinh tế học tập AegisCoin (Proof-of-Learning).
    /// </summary>
    public interface IAegisCoinService
    {
        Task<AegisCoinWalletDto> GetWalletAsync(Guid userId, CancellationToken ct = default);
        Task<AegisCoinWalletDto> RewardCoinsAsync(AegisCoinRewardRequestDto req, CancellationToken ct = default);
        Task<bool> SpendCoinsAsync(AegisCoinSpendRequestDto req, CancellationToken ct = default);
        Task<long> GetBalanceAsync(Guid userId, CancellationToken ct = default);
    }
}
