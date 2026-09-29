using System;
using System.Linq;
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
    /// [Epoch 4: Community Ecosystem] Triển khai kinh tế mã hóa AegisCoin Proof-of-Learning với Sổ cái kép.
    /// </summary>
    public class AegisCoinService : IAegisCoinService
    {
        private readonly AegisQuizDbContext _db;
        private readonly ILogger<AegisCoinService> _logger;
        private static bool _tableEnsured = false;

        public AegisCoinService(AegisQuizDbContext db, ILogger<AegisCoinService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<AegisCoinWalletDto> GetWalletAsync(Guid userId, CancellationToken ct = default)
        {
            await EnsureTablesAsync(ct);

            var wallet = await _db.AegisCoinWallets
                .FirstOrDefaultAsync(w => w.UserId == userId, ct);

            if (wallet == null)
            {
                wallet = new AegisCoinWallet
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TenantId = Tenant.CommunityTenantId,
                    Balance = 100,
                    LifetimeEarned = 100,
                    LifetimeSpent = 0,
                    LastEarnedAt = DateTime.UtcNow
                };
                _db.AegisCoinWallets.Add(wallet);

                _db.AegisCoinTransactions.Add(new AegisCoinTransaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    UserId = userId,
                    TenantId = Tenant.CommunityTenantId,
                    Amount = 100,
                    BalanceAfter = 100,
                    TransactionType = "WELCOME_BONUS",
                    Description = "Quà tặng chào mừng thành viên mới gia nhập AegisOS Cộng Đồng",
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync(ct);
            }

            var recentTx = await _db.AegisCoinTransactions
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => new AegisCoinTransactionItemDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    BalanceAfter = t.BalanceAfter,
                    TransactionType = t.TransactionType,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync(ct);

            return new AegisCoinWalletDto
            {
                WalletId = wallet.Id,
                UserId = wallet.UserId,
                Balance = wallet.Balance,
                LifetimeEarned = wallet.LifetimeEarned,
                LifetimeSpent = wallet.LifetimeSpent,
                LastEarnedAt = wallet.LastEarnedAt,
                RecentTransactions = recentTx
            };
        }

        public async Task<AegisCoinWalletDto> RewardCoinsAsync(AegisCoinRewardRequestDto req, CancellationToken ct = default)
        {
            await EnsureTablesAsync(ct);

            var wallet = await _db.AegisCoinWallets
                .FirstOrDefaultAsync(w => w.UserId == req.UserId, ct);

            if (wallet == null)
            {
                await GetWalletAsync(req.UserId, ct);
                wallet = await _db.AegisCoinWallets.FirstAsync(w => w.UserId == req.UserId, ct);
            }

            long rewardAmount = Math.Max(1, req.Amount);
            wallet.Balance += rewardAmount;
            wallet.LifetimeEarned += rewardAmount;
            wallet.LastEarnedAt = DateTime.UtcNow;
            wallet.UpdatedAt = DateTime.UtcNow;

            var tx = new AegisCoinTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                UserId = req.UserId,
                TenantId = wallet.TenantId,
                Amount = rewardAmount,
                BalanceAfter = wallet.Balance,
                TransactionType = req.TransactionType,
                Description = req.Description,
                ReferenceId = req.ReferenceId,
                CreatedAt = DateTime.UtcNow
            };
            _db.AegisCoinTransactions.Add(tx);

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[AegisCoin] Cộng thưởng +{Amount} Coin cho User {UserId} ({Type})", rewardAmount, req.UserId, req.TransactionType);

            return await GetWalletAsync(req.UserId, ct);
        }

        public async Task<bool> SpendCoinsAsync(AegisCoinSpendRequestDto req, CancellationToken ct = default)
        {
            await EnsureTablesAsync(ct);

            var wallet = await _db.AegisCoinWallets
                .FirstOrDefaultAsync(w => w.UserId == req.UserId, ct);

            if (wallet == null || wallet.Balance < req.Amount)
            {
                return false; // Không đủ số dư
            }

            long spendAmount = Math.Abs(req.Amount);
            wallet.Balance -= spendAmount;
            wallet.LifetimeSpent += spendAmount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var tx = new AegisCoinTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                UserId = req.UserId,
                TenantId = wallet.TenantId,
                Amount = -spendAmount,
                BalanceAfter = wallet.Balance,
                TransactionType = req.ItemCode,
                Description = req.Description,
                CreatedAt = DateTime.UtcNow
            };
            _db.AegisCoinTransactions.Add(tx);

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("[AegisCoin] Tiêu dùng -{Amount} Coin cho User {UserId} ({Item})", spendAmount, req.UserId, req.ItemCode);
            return true;
        }

        public async Task<long> GetBalanceAsync(Guid userId, CancellationToken ct = default)
        {
            var wallet = await GetWalletAsync(userId, ct);
            return wallet.Balance;
        }

        private async Task EnsureTablesAsync(CancellationToken ct)
        {
            if (_tableEnsured) return;
            try
            {
                var sql = @"
                    CREATE TABLE IF NOT EXISTS ""AegisCoinWallets"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""UserId"" uuid NOT NULL,
                        ""TenantId"" uuid NOT NULL,
                        ""Balance"" bigint NOT NULL DEFAULT 100,
                        ""LifetimeEarned"" bigint NOT NULL DEFAULT 100,
                        ""LifetimeSpent"" bigint NOT NULL DEFAULT 0,
                        ""LastEarnedAt"" timestamp with time zone,
                        ""CreatedAt"" timestamp with time zone NOT NULL,
                        ""UpdatedAt"" timestamp with time zone NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_AegisCoinWallets_UserId"" ON ""AegisCoinWallets"" (""UserId"");

                    CREATE TABLE IF NOT EXISTS ""AegisCoinTransactions"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""WalletId"" uuid NOT NULL,
                        ""UserId"" uuid NOT NULL,
                        ""TenantId"" uuid NOT NULL,
                        ""Amount"" bigint NOT NULL,
                        ""BalanceAfter"" bigint NOT NULL,
                        ""TransactionType"" text NOT NULL,
                        ""Description"" text NOT NULL,
                        ""ReferenceId"" text,
                        ""CreatedAt"" timestamp with time zone NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_AegisCoinTransactions_UserId"" ON ""AegisCoinTransactions"" (""UserId"");
                ";
                await _db.Database.ExecuteSqlRawAsync(sql, ct);
                _tableEnsured = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AegisCoin] Table creation self-healing skipped or table exists.");
                _tableEnsured = true;
            }
        }
    }
}
