using System;

namespace AegisQuiz.Domain.Entities
{
    // [AegisAI BYOK] Phạm vi sử dụng khóa AI cá nhân
    public enum KeyUsageScope
    {
        Private = 1,              // Chỉ tài khoản sở hữu được dùng, 0$ phụ phí nền tảng
        CommunityContributed = 2  // Hiến tặng vào Bể Khóa Chung (Community Pool) để cộng đồng cùng dùng, được nhận AegisCoin và vinh danh Bảng Vàng
    }

    /// <summary>
    /// [AegisAI Bring-Your-Own-Key Sovereign]
    /// Lưu trữ khóa API riêng của người dùng / khách thuê với mã hóa bảo mật AES-256-GCM
    /// </summary>
    public class UserAiKey : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; } = Guid.Empty;

        public string KeyLabel { get; set; } = string.Empty;
        public string ProviderCompany { get; set; } = "DeepSeek"; // 'DeepSeek', 'Google', 'OpenAI', 'Anthropic', 'Groq', 'Local'
        public ModelProtocolType ProtocolType { get; set; } = ModelProtocolType.OpenAiCompatible;
        
        public string EncryptedApiKey { get; set; } = string.Empty; // Mã hóa AES-256-GCM
        public string KeyFingerprint { get; set; } = string.Empty;  // Hiển thị 4 ký tự đầu và 4 ký tự cuối (vd: 'sk-d...7f3a')
        
        public string? CustomEndpoint { get; set; }                  // Hỗ trợ Ollama / vLLM / Private Gateway (vd: http://localhost:11434/v1)
        public string? CustomModelName { get; set; }                 // Tên model tùy biến trên private endpoint

        public KeyUsageScope Scope { get; set; } = KeyUsageScope.Private;
        public bool IsActive { get; set; } = true;

        public long MonthlyTokenQuota { get; set; } = 0;             // 0 = Không giới hạn
        public long TokensUsedThisMonth { get; set; } = 0;
        public long TotalCallsCount { get; set; } = 0;
        public long SuccessCallsCount { get; set; } = 0;
        public long FailedCallsCount { get; set; } = 0;

        public DateTime? LastUsedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
