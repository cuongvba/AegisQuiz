using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.Models.AI
{
    public enum AiProviderType
    {
        DeepSeek = 1,
        GoogleGemini = 2,
        OpenAI = 3,
        AnthropicClaude = 4,
        LocalOllama = 5
    }

    public enum AiTaskType
    {
        SolveQuestions = 1,          // Tự động giải đề thi, tìm đáp án đúng
        CognitiveWalkthrough = 2,    // Sinh bản đồ tư duy sư phạm 5 bước
        GenerateQuestions = 3,       // Sinh đề từ tài liệu, bài giảng
        GradeEssay = 4,              // Chấm thi tự luận theo barem
        PersonalizedMentor = 5,      // Cố vấn học tập, phân tích lỗ hổng tri thức
        ArenaAdversary = 6,          // Kẻ đi săn / Đối thủ AI trong Đấu trường Gameshow
        DistractorAnalysis = 7,      // Phân tích bẫy nhận thức trong phương án nhiễu
        LatexConversion = 8          // Chuyển đổi công thức toán vector sang LaTeX
    }

    public enum AgentPersonaType
    {
        SupremeArbiter = 1,          // Giám khảo khảo thí tối cao chuẩn AERA/ETS
        BankingLegalCounsel = 2,     // Cố vấn pháp lý & nghiệp vụ ngân hàng cấp cao
        SocratesMentor = 3,          // Gia sư khai phóng tri thức, gợi mở tư duy
        RelentlessChaser = 4,        // Kẻ đi săn sắc lạnh trong Gameshow
        PsychProfiler = 5            // Bác sĩ tâm lý đo lường độ do dự và phản xạ nhận thức
    }

    public class PedagogicalWalkthroughDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionContent { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public string DomainCode { get; set; } = "GENERAL";

        // 5 Trụ cột bản đồ tư duy
        public string PromptAnatomy { get; set; } = string.Empty;       // 1. Bóc tách dữ kiện & Từ khóa cốt lõi
        public string TheoreticalGrounding { get; set; } = string.Empty; // 2. Tọa độ pháp lý / Tri thức áp dụng
        public string DistractorAutopsy { get; set; } = string.Empty;   // 3. Giải phẫu phương án bẫy & Sai lầm phổ biến
        public string MnemonicAndRecall { get; set; } = string.Empty;   // 4. Mẹo phản xạ nhanh & Bí quyết ghi nhớ
        public string ExtrapolatedCase { get; set; } = string.Empty;    // 5. Bài toán tình huống mở rộng tư duy

        // Siêu dữ liệu đo lường nhận thức
        public int BloomLevel { get; set; } = 2; // 1: Remember, 2: Understand, 3: Apply, 4: Analyze, 5: Evaluate, 6: Create
        public double EstimatedDifficultyIrt { get; set; } = 0.5; // b parameter trong IRT 3-PL [-3.0, +3.0]
        public string ResolvedByProvider { get; set; } = string.Empty;
        public string ResolvedByModel { get; set; } = string.Empty;
        public bool IsFromSemanticCache { get; set; } = false;
        public long ExecutionTimeMs { get; set; } = 0;
    }

    public class AiProviderEndpointConfig
    {
        public AiProviderType ProviderType { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public List<string> ApiKeys { get; set; } = new();
        public string DefaultModel { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; } = 1; // Ưu tiên thấp hơn chạy trước
        public double CostPerMillionInputTokens { get; set; } = 0.0;
        public double CostPerMillionOutputTokens { get; set; } = 0.0;
    }

    public class AiSystemConfigDto
    {
        public List<AiProviderEndpointConfig> Providers { get; set; } = new();
        public Dictionary<string, AiProviderType> TaskRoutingMap { get; set; } = new();
        public bool EnableSemanticCache { get; set; } = true;
        public double SemanticCacheSimilarityThreshold { get; set; } = 0.95;
        public bool EnableMultiAgentCommittee { get; set; } = true;
        public int KeyQuarantineSeconds { get; set; } = 60;
        public FinOpsTelemetryDto Telemetry { get; set; } = new();
    }

    public class FinOpsTelemetryDto
    {
        public long TotalRequestsProcessed { get; set; } = 0;
        public long CacheHitsCount { get; set; } = 0;
        public double CacheHitRatio => TotalRequestsProcessed > 0 ? (double)CacheHitsCount / TotalRequestsProcessed * 100.0 : 0.0;
        public double EstimatedCostSavedUsd { get; set; } = 0.0;
        public double TotalSpentUsd { get; set; } = 0.0;
        public Dictionary<string, long> RequestCountByProvider { get; set; } = new();
    }

    public class AiExecutionRequest
    {
        public AiTaskType TaskType { get; set; }
        public string Prompt { get; set; } = string.Empty;
        public string? SystemPrompt { get; set; }
        public AgentPersonaType Persona { get; set; } = AgentPersonaType.SupremeArbiter;
        public AiProviderType? PreferredProvider { get; set; }
        public string? PreferredModel { get; set; }
        public double Temperature { get; set; } = 0.2;
        public int MaxTokens { get; set; } = 2048;
        public string? TenantId { get; set; }
        public string? CacheKey { get; set; }
    }

    public class AiExecutionResponse
    {
        public bool Success { get; set; } = false;
        public string Content { get; set; } = string.Empty;
        public string ProviderUsed { get; set; } = string.Empty;
        public string ModelUsed { get; set; } = string.Empty;
        public bool IsFromCache { get; set; } = false;
        public long DurationMs { get; set; } = 0;
        public string? ErrorMessage { get; set; }
    }

    public class TenantByokConfigDto
    {
        public string TenantId { get; set; } = string.Empty;
        public AiProviderType ProviderType { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string? CustomEndpoint { get; set; }
        public string? CustomModel { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
