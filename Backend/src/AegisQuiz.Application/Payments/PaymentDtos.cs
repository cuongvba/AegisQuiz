using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.Payments
{
    public enum PaymentProviderType
    {
        PayOS,           // VietQR Napas 24/7 (Việt Nam)
        StripeCard,      // Visa / Mastercard / Apple Pay / Google Pay (Quốc tế)
        ChinaWallet,     // Alipay (支付宝) & WeChat Pay (微信支付) (Trung Quốc)
        PayPal,          // PayPal Checkout (Toàn cầu)
        Web3Polygon      // Crypto MATIC / USDT (Phi biên giới)
    }

    public class CreatePaymentOrderRequest
    {
        public Guid UserId { get; set; }
        public Guid? TenantId { get; set; }
        public PaymentProviderType Provider { get; set; } = PaymentProviderType.PayOS;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string PlanTier { get; set; } = "VIP_LIFETIME"; // VIP_MONTHLY, VIP_YEARLY, VIP_LIFETIME
        public string Description { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string? BuyerName { get; set; }
        public string? BuyerEmail { get; set; }
    }

    public class PaymentOrderResponseDto
    {
        public Guid TransactionId { get; set; }
        public long OrderCode { get; set; }
        public string PaymentCode { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string Status { get; set; } = "Pending";
        public string CheckoutUrl { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpireAt { get; set; }
    }

    public class PaymentStatusCheckDto
    {
        public Guid TransactionId { get; set; }
        public long OrderCode { get; set; }
        public string Status { get; set; } = "Pending";
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class PaymentAnalyticsSummaryDto
    {
        public decimal TotalRevenueVnd { get; set; }
        public decimal TotalRevenueMatic { get; set; }
        public decimal TotalRevenueUsd { get; set; }
        public int TotalTransactions { get; set; }
        public int CompletedTransactions { get; set; }
        public double SuccessRate { get; set; }
        public List<PaymentTransactionItemDto> RecentTransactions { get; set; } = new();
    }

    public class PaymentTransactionItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
    }

    // PayOS Webhook Payload DTO chuẩn
    public class PayOSWebhookRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public PayOSWebhookData Data { get; set; } = new();
        public string Signature { get; set; } = string.Empty;
    }

    public class PayOSWebhookData
    {
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string TransactionDateTime { get; set; } = string.Empty;
        public string Currency { get; set; } = "VND";
        public string PaymentLinkId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
    }

    // DTO Cấu hình Cổng Thanh toán & Tài khoản Thụ hưởng
    public class PaymentGatewaySettingsDto
    {
        // 1. VietQR Napas Direct
        public string VietQrBankBin { get; set; } = "970422"; // MB Bank default
        public string VietQrBankName { get; set; } = "MB Bank (Ngân hàng Quân Đội)";
        public string VietQrAccountNumber { get; set; } = "0987654321";
        public string VietQrAccountName { get; set; } = "AEGISQUIZ CORP";

        // 2. PayOS API v2
        public string PayOsClientId { get; set; } = "";
        public string PayOsApiKey { get; set; } = "";
        public string PayOsChecksumKey { get; set; } = "";
        public string PayOsWebhookUrl { get; set; } = "https://aegisquiz.com/api/webhook/payos";

        // 3. Stripe & Global Card
        public string StripePublishableKey { get; set; } = "";
        public string StripeSecretKey { get; set; } = "";

        // 4. Web3 Crypto Treasury
        public string Web3TreasuryAddress { get; set; } = "0x16b607ED8d8a7c2937DF0EAc2Cd0B47a759600eE";

        // 5. System Mode
        public bool EnableSandboxTesting { get; set; } = true;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UpdatePaymentGatewaySettingsRequest
    {
        public string VietQrBankBin { get; set; } = "970422";
        public string VietQrBankName { get; set; } = "MB Bank";
        public string VietQrAccountNumber { get; set; } = "";
        public string VietQrAccountName { get; set; } = "";
        public string? PayOsClientId { get; set; }
        public string? PayOsApiKey { get; set; }
        public string? PayOsChecksumKey { get; set; }
        public string? StripePublishableKey { get; set; }
        public string? StripeSecretKey { get; set; }
        public string? Web3TreasuryAddress { get; set; }
        public bool EnableSandboxTesting { get; set; } = true;

        // [Enterprise Security] Cơ chế xác thực đa yếu tố Step-up / Sudo Mode bắt buộc của SysAdmin
        public string? SudoPassword { get; set; }
        public string? MfaVerificationCode { get; set; }
    }

    // ── Tra Cứu & Xác Thực Danh Tính Tài Khoản Ngân Hàng Thời Gian Thực ───────────
    public class BankAccountLookupRequest
    {
        public string BankBin { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
    }

    public class BankAccountLookupResponseDto
    {
        public bool IsValid { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsVerifiedByNapas { get; set; }
    }

    // ── Community Coffee Donation & Supporter Gratitude DTOs ───────────────────
    public class CreateCoffeeDonationRequest
    {
        public string DonorName { get; set; } = "Ẩn danh";
        public string DonorEmail { get; set; } = string.Empty; // Dùng để gửi tri ân / quà tặng
        public string? DonorPhone { get; set; }
        public decimal Amount { get; set; } = 25000;
        public string Message { get; set; } = "Mời team ly cafe & chúc dự án phát triển!";
    }

    public class CoffeeDonationResponseDto
    {
        public long OrderCode { get; set; }
        public decimal Amount { get; set; }
        public string EmvCoPayload { get; set; } = string.Empty; // Chuỗi chuẩn EMVCo Napas
        public string QrImageUrl { get; set; } = string.Empty;   // URL ảnh fallback
        public string BankBin { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string TransferContent { get; set; } = string.Empty;
        public string RewardVoucherCode { get; set; } = string.Empty; // Mã voucher tri ân
        public string RewardTier { get; set; } = "Nhà Đồng Hành";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CoffeeDonationItemDto
    {
        public long OrderCode { get; set; }
        public string DonorName { get; set; } = string.Empty;
        public string DonorEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RewardVoucherCode { get; set; } = string.Empty;
        public string RewardTier { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
    }
}
