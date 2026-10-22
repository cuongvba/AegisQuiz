using System;
using System.ComponentModel.DataAnnotations;

namespace AegisQuiz.Domain.Entities
{
    // [Cảnh giới 5: Động cơ Kinh tế & Thanh toán Đa Kênh Toàn Cầu]
    public class PaymentTransaction
    {
        [Key]
        public Guid TransactionId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid? TenantId { get; set; }
        
        // Mã đơn hàng dạng số nguyên duy nhất (Bắt buộc đối với PayOS và Napas VietQR)
        public long OrderCode { get; set; }
        
        // Chuỗi mã code hiển thị (Ví dụ: AEGISQ_12345678)
        public string PaymentCode { get; set; } = string.Empty; 
        
        public decimal AmountRequired { get; set; }
        public string Currency { get; set; } = "VND"; // VND, USD, CNY, MATIC, USDT
        
        // Cổng thanh toán (PayOS, Stripe, Alipay, WeChatPay, Web3Polygon)
        public string Provider { get; set; } = "PayOS";
        
        // Nội dung mô tả chuyển khoản
        public string Description { get; set; } = string.Empty;
        
        // Link thanh toán Checkout (PayOS URL hoặc Stripe Hosted Checkout)
        public string CheckoutUrl { get; set; } = string.Empty;
        
        // Mã QR Code (VietQR text/link hoặc Stripe QRCode)
        public string QrCode { get; set; } = string.Empty;
        
        // Mã định danh giao dịch của đối tác thanh toán
        public string PaymentLinkId { get; set; } = string.Empty;
        
        // Hash giao dịch trên Blockchain nếu là Web3
        public string TransactionHash { get; set; } = string.Empty;
        
        // Dữ liệu mở rộng dạng JSON (Metadata)
        public string MetadataJson { get; set; } = "{}";
        
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public void MarkAsCompleted(string? note = null)
        {
            Status = PaymentStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        public void MarkAsExpired()
        {
            Status = PaymentStatus.Expired;
        }

        public void MarkAsCancelled()
        {
            Status = PaymentStatus.Cancelled;
        }
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Expired,
        Cancelled
    }
}
