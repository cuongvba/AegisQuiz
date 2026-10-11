using System;

namespace AegisQuiz.Application.Models.AI
{
    /// <summary>
    /// [AegisAI Grounded Symposium - Seat Model Invocation Request]
    /// Yêu cầu triệu gọi mô hình AI thật sự cho một ghế trong Đại Hội Thảo.
    /// </summary>
    public class SeatInvocationRequest
    {
        public string ModelCode { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string UserPrompt { get; set; } = string.Empty;
        public double Temperature { get; set; } = 0.2;
        public int MaxTokens { get; set; } = 2048;
        public string? ResponseJsonSchema { get; set; }
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
    }

    /// <summary>
    /// Kết quả triệu gọi mô hình AI thật cho ghế, phản ánh độ trễ đo thực tế và nguồn khóa xác thực.
    /// </summary>
    public class SeatInvocationResponse
    {
        public bool Success { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ModelUsed { get; set; } = string.Empty;
        public string ProviderCompany { get; set; } = string.Empty;
        /// <summary>Thời gian phản hồi ĐO THỰC TẾ qua Stopwatch (mili-giây). Tuyệt đối không sinh ngẫu nhiên.</summary>
        public long LatencyMs { get; set; }
        public int TokensUsed { get; set; }
        public double EstimatedCostUsd { get; set; }
        public string KeySource { get; set; } = string.Empty; // UserBYOK | TenantBYOK | CommunityPool | SystemConfig
        public string? ErrorMessage { get; set; }
    }
}
