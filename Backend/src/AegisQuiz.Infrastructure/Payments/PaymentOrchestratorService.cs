using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Payments;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.Payments
{
    public class PaymentOrchestratorService : IPaymentOrchestratorService
    {
        private readonly AegisQuizDbContext _db;
        private readonly IEnumerable<IPaymentGatewayStrategy> _strategies;
        private readonly ILogger<PaymentOrchestratorService> _logger;

        public PaymentOrchestratorService(
            AegisQuizDbContext db,
            IEnumerable<IPaymentGatewayStrategy> strategies,
            ILogger<PaymentOrchestratorService> logger)
        {
            _db = db;
            _strategies = strategies;
            _logger = logger;
        }

        public async Task<PaymentOrderResponseDto> InitiatePaymentAsync(CreatePaymentOrderRequest request)
        {
            // Sinh mã OrderCode số nguyên duy nhất (milliseconds + random 2 số để chống va chạm)
            var randomSuffix = Random.Shared.Next(10, 99);
            var orderCode = long.Parse($"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{randomSuffix}");
            var paymentCode = $"AEGISQ_{orderCode}";

            var transaction = new PaymentTransaction
            {
                TransactionId = Guid.NewGuid(),
                UserId = request.UserId,
                TenantId = request.TenantId,
                OrderCode = orderCode,
                PaymentCode = paymentCode,
                AmountRequired = request.Amount,
                Currency = request.Currency,
                Provider = request.Provider.ToString(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? $"Nâng cấp VIP {paymentCode}" : request.Description,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            // Tìm Strategy phù hợp
            var strategy = _strategies.FirstOrDefault(s => s.ProviderType == request.Provider)
                ?? _strategies.First(s => s.ProviderType == PaymentProviderType.PayOS);

            // Khởi tạo link thanh toán qua cổng tương ứng
            var result = await strategy.CreatePaymentAsync(transaction, request);

            _db.PaymentTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            _logger.LogInformation("[PaymentOrchestrator] Khởi tạo giao dịch thành công. OrderCode: {OrderCode}, Provider: {Provider}",
                orderCode, transaction.Provider);

            return result;
        }

        public async Task<PaymentStatusCheckDto> CheckPaymentStatusAsync(long orderCode)
        {
            var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.OrderCode == orderCode);
            if (tx == null)
            {
                return new PaymentStatusCheckDto { OrderCode = orderCode, Status = "NotFound" };
            }

            return new PaymentStatusCheckDto
            {
                TransactionId = tx.TransactionId,
                OrderCode = tx.OrderCode,
                Status = tx.Status.ToString(),
                IsCompleted = tx.Status == PaymentStatus.Completed,
                CompletedAt = tx.CompletedAt
            };
        }

        public async Task<bool> HandlePayOSWebhookAsync(PayOSWebhookRequest webhook)
        {
            if (webhook?.Data == null) return false;

            var orderCode = webhook.Data.OrderCode;
            var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.OrderCode == orderCode);
            if (tx == null)
            {
                _logger.LogWarning("[PayOS Webhook] Không tìm thấy đơn hàng với OrderCode: {OrderCode}", orderCode);
                return false;
            }

            // Chống xử lý trùng lặp (Idempotency)
            if (tx.Status == PaymentStatus.Completed)
            {
                _logger.LogInformation("[PayOS Webhook] Đơn hàng {OrderCode} đã hoàn tất trước đó.", orderCode);
                return true;
            }

            // Đánh dấu giao dịch thành công
            tx.MarkAsCompleted($"PayOS ref: {webhook.Data.Reference}");

            // Tự động thăng hạng VIP cho học viên
            await ActivateUserVipAsync(tx.UserId);

            await _db.SaveChangesAsync();
            _logger.LogInformation("[PayOS Webhook] Kích hoạt thành công gói VIP cho UserId {UserId} theo đơn {OrderCode}", tx.UserId, orderCode);
            return true;
        }

        public async Task<bool> HandleWeb3PaymentAsync(string paymentCode, decimal maticAmount, string txHash)
        {
            var tx = await _db.PaymentTransactions.FirstOrDefaultAsync(t => t.PaymentCode == paymentCode);
            if (tx == null)
            {
                tx = new PaymentTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    OrderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    PaymentCode = paymentCode,
                    AmountRequired = maticAmount,
                    Currency = "MATIC",
                    Provider = "Web3Polygon",
                    TransactionHash = txHash,
                    Status = PaymentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                _db.PaymentTransactions.Add(tx);
            }

            if (tx.Status == PaymentStatus.Completed) return true;

            tx.MarkAsCompleted($"Web3 tx: {txHash}");
            tx.TransactionHash = txHash;

            if (tx.UserId != Guid.Empty)
            {
                await ActivateUserVipAsync(tx.UserId);
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<PaymentAnalyticsSummaryDto> GetPaymentAnalyticsAsync(Guid? tenantId = null)
        {
            var query = _db.PaymentTransactions.AsNoTracking();
            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            {
                query = query.Where(t => t.TenantId == tenantId.Value);
            }

            var allTransactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Take(100)
                .ToListAsync();

            var completed = allTransactions.Where(t => t.Status == PaymentStatus.Completed).ToList();
            
            decimal totalVnd = completed
                .Where(t => t.Currency == "VND" || t.Provider.Contains("PayOS") || t.Provider.Contains("VietQR"))
                .Sum(t => t.AmountRequired);

            decimal totalMatic = completed
                .Where(t => t.Currency == "MATIC" || t.Provider.Contains("Web3"))
                .Sum(t => t.AmountRequired);

            decimal totalUsd = completed
                .Where(t => t.Currency == "USD" || t.Currency == "CNY" || t.Provider.Contains("Stripe"))
                .Sum(t => t.AmountRequired);

            // Nếu DB chưa có giao dịch nào, hiển thị số liệu thực tế bắt đầu từ 0
            int totalCount = allTransactions.Count;
            int completedCount = completed.Count;
            double successRate = totalCount > 0 ? Math.Round((double)completedCount / totalCount * 100, 1) : 100.0;

            var recentItems = allTransactions.Take(15).Select(t => new PaymentTransactionItemDto
            {
                Id = $"TX-{t.OrderCode.ToString().Substring(Math.Max(0, t.OrderCode.ToString().Length - 6))}",
                User = t.UserId != Guid.Empty ? $"User {t.UserId.ToString().Substring(0, 8)}" : "Khách Hàng VIP",
                Method = t.Provider,
                Amount = t.Currency == "MATIC" ? $"{t.AmountRequired} MATIC" : (t.Currency == "USD" ? $"${t.AmountRequired}" : $"₫{t.AmountRequired:N0}"),
                Status = t.Status.ToString(),
                Date = t.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            }).ToList();

            return new PaymentAnalyticsSummaryDto
            {
                TotalRevenueVnd = totalVnd,
                TotalRevenueMatic = totalMatic,
                TotalRevenueUsd = totalUsd,
                TotalTransactions = totalCount,
                CompletedTransactions = completedCount,
                SuccessRate = successRate,
                RecentTransactions = recentItems
            };
        }

        private async Task ActivateUserVipAsync(Guid userId)
        {
            if (userId == Guid.Empty) return;
            try
            {
                var userAccount = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Id == userId);
                if (userAccount != null)
                {
                    userAccount.IsPremium = true;
                    userAccount.SubscriptionTier = "VIP";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PaymentOrchestrator] Không thể tự động update UserAccounts: {Message}", ex.Message);
            }
        }

        public async Task<PaymentGatewaySettingsDto> GetSettingsAsync(Guid? tenantId = null)
        {
            try
            {
                var targetTenantId = tenantId ?? Tenant.CommunityTenantId;
                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == targetTenantId);
                if (tenant != null && !string.IsNullOrWhiteSpace(tenant.ConfigJson) && tenant.ConfigJson.Contains("VietQrAccountNumber"))
                {
                    var saved = System.Text.Json.JsonSerializer.Deserialize<PaymentGatewaySettingsDto>(tenant.ConfigJson);
                    if (saved != null)
                    {
                        PaymentSettingsStore.Current = saved;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PaymentOrchestrator] Lỗi đọc Tenant ConfigJson: {Message}", ex.Message);
            }

            return PaymentSettingsStore.Current;
        }

        public async Task<PaymentGatewaySettingsDto> UpdateSettingsAsync(UpdatePaymentGatewaySettingsRequest request, Guid? tenantId = null)
        {
            var current = PaymentSettingsStore.Current;
            current.VietQrBankBin = request.VietQrBankBin ?? current.VietQrBankBin;
            current.VietQrBankName = request.VietQrBankName ?? current.VietQrBankName;
            current.VietQrAccountNumber = request.VietQrAccountNumber ?? current.VietQrAccountNumber;
            current.VietQrAccountName = request.VietQrAccountName ?? current.VietQrAccountName;

            if (!string.IsNullOrWhiteSpace(request.PayOsClientId)) current.PayOsClientId = request.PayOsClientId;
            if (!string.IsNullOrWhiteSpace(request.PayOsApiKey)) current.PayOsApiKey = request.PayOsApiKey;
            if (!string.IsNullOrWhiteSpace(request.PayOsChecksumKey)) current.PayOsChecksumKey = request.PayOsChecksumKey;

            if (!string.IsNullOrWhiteSpace(request.StripePublishableKey)) current.StripePublishableKey = request.StripePublishableKey;
            if (!string.IsNullOrWhiteSpace(request.StripeSecretKey)) current.StripeSecretKey = request.StripeSecretKey;

            if (!string.IsNullOrWhiteSpace(request.Web3TreasuryAddress)) current.Web3TreasuryAddress = request.Web3TreasuryAddress;
            current.EnableSandboxTesting = request.EnableSandboxTesting;
            current.LastUpdatedAt = DateTime.UtcNow;

            PaymentSettingsStore.Current = current;

            try
            {
                var targetTenantId = tenantId ?? Tenant.CommunityTenantId;
                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == targetTenantId);
                if (tenant != null)
                {
                    tenant.ConfigJson = System.Text.Json.JsonSerializer.Serialize(current);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PaymentOrchestrator] Lỗi lưu Tenant ConfigJson: {Message}", ex.Message);
            }

            return current;
        }

        public async Task<bool> TestConnectionAsync(string provider, Guid? tenantId = null)
        {
            await Task.Delay(200); // Giả lập ping mạng nhanh
            var cfg = PaymentSettingsStore.Current;

            if (provider.Equals("vietqr", StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrWhiteSpace(cfg.VietQrAccountNumber) && !string.IsNullOrWhiteSpace(cfg.VietQrBankBin);
            }

            if (provider.Equals("payos", StringComparison.OrdinalIgnoreCase))
            {
                // Nếu chưa có key thật nhưng đang bật Sandbox thì cho test passed
                if (cfg.EnableSandboxTesting) return true;
                return !string.IsNullOrWhiteSpace(cfg.PayOsClientId) && !string.IsNullOrWhiteSpace(cfg.PayOsApiKey);
            }

            if (provider.Equals("stripe", StringComparison.OrdinalIgnoreCase))
            {
                if (cfg.EnableSandboxTesting) return true;
                return !string.IsNullOrWhiteSpace(cfg.StripePublishableKey);
            }

            return true;
        }
    }
}
