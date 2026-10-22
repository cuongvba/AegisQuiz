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
    }
}
