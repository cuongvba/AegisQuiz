using System;

namespace AegisQuiz.Domain.Entities
{
    // [AegisAI Dynamic Registry] Mẫu hình Năng lực Nhận thức của Mô hình AI (Không trói buộc thương hiệu)
    public enum CognitiveArchetype
    {
        DeepReasoner = 1,       // Suy luận chuỗi dài CoT, giải toán, suy diễn logic, phát hiện bẫy
        MassiveContext = 2,     // Cửa sổ ngữ cảnh cực đại 1M-10M tokens, bóc tách giáo trình lớn, đa phương thức
        LinguisticArbiter = 3,  // Sư phạm, barem tự luận, phản biện Devil's Advocate, hiệu chỉnh câu chữ
        UltraFastScanner = 4,   // Tốc độ siêu tốc > 150 tps, độ trễ < 250ms cho Gameshow thời gian thực
        SovereignLocal = 5      // On-Premise nội bộ bảo đảm an ninh dữ liệu 100% trong Intranet
    }

    // Giao thức truyền tin tiêu chuẩn toàn cầu
    public enum ModelProtocolType
    {
        OpenAiCompatible = 1,   // Chuẩn hóa 95% LLMs thế giới (DeepSeek, Groq, Ollama, Together, vLLM, Mistral...)
        GoogleGenerativeAi = 2, // Chuẩn hóa Google Gemini Native RPC/REST
        AnthropicMessages = 3   // Chuẩn hóa Anthropic Claude Native Messages REST
    }

    // Thực thể Bảng Đăng Ký Mô Hình Động 100% Cơ sở dữ liệu
    public class AiModelRegistry : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ModelCode { get; set; } = string.Empty;         // vd: 'deepseek-r1', 'gemini-2.0-flash', 'groq-llama-3.3'
        public string DisplayName { get; set; } = string.Empty;       // vd: 'DeepSeek R1 Reasoning'
        public string ProviderCompany { get; set; } = "DeepSeek";     // 'DeepSeek', 'Google', 'Anthropic', 'Groq', 'Meta', 'Local'
        public ModelProtocolType ProtocolType { get; set; } = ModelProtocolType.OpenAiCompatible;

        public CognitiveArchetype CognitiveArchetype { get; set; } = CognitiveArchetype.DeepReasoner;
        public string EndpointUrl { get; set; } = string.Empty;       // URL endpoint
        public string ModelName { get; set; } = string.Empty;         // Tên model gửi trong payload JSON (vd: 'deepseek-reasoner')
        public int MaxContextTokens { get; set; } = 128000;

        public double CostInputPerMillion { get; set; } = 0.14;       // Chi phí 1M input tokens USD
        public double CostOutputPerMillion { get; set; } = 0.28;      // Chi phí 1M output tokens USD
        public int BenchmarkElo { get; set; } = 1250;                 // Điểm chuẩn nhận thức (LMSYS Arena hoặc nội bộ)
        public int AvgLatencyMs { get; set; } = 400;                  // Độ trễ trung bình

        public bool IsEnabled { get; set; } = true;
        public bool IsDefaultInArchetype { get; set; } = false;       // Mô hình đại diện cho Archetype
        public Guid TenantId { get; set; } = Guid.Empty;              // Guid.Empty = Hệ thống toàn cục; có giá trị = Private Model của Khách thuê

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
