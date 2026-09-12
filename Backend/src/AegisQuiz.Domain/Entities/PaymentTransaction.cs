using System;

using System.ComponentModel.DataAnnotations;

namespace AegisQuiz.Domain.Entities
{
    // [Cảnh giới 5: Động cơ Kinh tế]
    public class PaymentTransaction
    {
        [Key]
        public Guid TransactionId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        
        // Mã nạp tiền độc nhất để khách hàng điền vào Nội dung chuyển khoản
        // Hoặc truyền vào Data payload của Smart Contract Web3
        public string PaymentCode { get; set; } = string.Empty; 
        
        public decimal AmountRequired { get; set; }
        public string Currency { get; set; } = "VND"; // Có thể là MATIC/USDT
        
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public void MarkAsCompleted()
        {
            Status = PaymentStatus.Completed;
            // Logic bắn SignalR Event cho Frontend mở khóa ở đây
        }
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Expired
    }
}
