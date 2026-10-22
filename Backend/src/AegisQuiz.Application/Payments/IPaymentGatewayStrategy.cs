using System;
using System.Threading.Tasks;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Payments
{
    // Strategy Pattern: Mở rộng cổng thanh toán mới mà không sửa mã nguồn lõi (OCP)
    public interface IPaymentGatewayStrategy
    {
        PaymentProviderType ProviderType { get; }
        Task<PaymentOrderResponseDto> CreatePaymentAsync(PaymentTransaction transaction, CreatePaymentOrderRequest request);
        Task<bool> VerifyWebhookSignatureAsync(string requestBody, string signature);
    }

    public interface IPaymentOrchestratorService
    {
        Task<PaymentOrderResponseDto> InitiatePaymentAsync(CreatePaymentOrderRequest request);
        Task<PaymentStatusCheckDto> CheckPaymentStatusAsync(long orderCode);
        Task<bool> HandlePayOSWebhookAsync(PayOSWebhookRequest webhook);
        Task<bool> HandleWeb3PaymentAsync(string paymentCode, decimal maticAmount, string txHash);
        Task<PaymentAnalyticsSummaryDto> GetPaymentAnalyticsAsync(Guid? tenantId = null);
        Task<PaymentGatewaySettingsDto> GetSettingsAsync(Guid? tenantId = null);
        Task<PaymentGatewaySettingsDto> UpdateSettingsAsync(UpdatePaymentGatewaySettingsRequest request, Guid? tenantId = null);
        Task<bool> TestConnectionAsync(string provider, Guid? tenantId = null);
        Task<CoffeeDonationResponseDto> ProcessCoffeeDonationAsync(CreateCoffeeDonationRequest request, Guid? userId = null);
        Task<System.Collections.Generic.List<CoffeeDonationItemDto>> GetCoffeeDonationsAsync(int limit = 50);
        Task<BankAccountLookupResponseDto> LookupBankAccountAsync(string bankBin, string accountNumber);
    }
}
