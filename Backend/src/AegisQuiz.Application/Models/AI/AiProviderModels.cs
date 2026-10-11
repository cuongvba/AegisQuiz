using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.Models.AI
{
    #region Well-Known Standard Slugs (100% Dynamic - Zero Hardcode)

    /// <summary>
    /// Các mã định danh Nhà Cung Cấp tiêu chuẩn (Well-Known Provider Codes).
    /// Hệ thống hỗ trợ mở rộng động 100% từ Database PostgreSQL mà không cần sửa code.
    /// </summary>
    public static class WellKnownAiProviders
    {
        public const string DeepSeek = "deepseek";
        public const string GoogleGemini = "gemini";
        public const string OpenAI = "openai";
        public const string AnthropicClaude = "anthropic";
        public const string LocalOllama = "ollama";
        public const string Groq = "groq";
        public const string SambaNova = "sambanova";
        public const string Cerebras = "cerebras";
        public const string MistralAI = "mistral";
        public const string GitHubModels = "github";
        public const string Replicate = "replicate";
        public const string OpenRouter = "openrouter";

        public static string Normalize(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "default";
            return code.Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
        }

        public static string ToCode(AiProviderType type) => type switch
        {
            AiProviderType.DeepSeek => DeepSeek,
            AiProviderType.GoogleGemini => GoogleGemini,
            AiProviderType.OpenAI => OpenAI,
            AiProviderType.AnthropicClaude => AnthropicClaude,
            AiProviderType.LocalOllama => LocalOllama,
            AiProviderType.Groq => Groq,
            AiProviderType.SambaNova => SambaNova,
            AiProviderType.Cerebras => Cerebras,
            AiProviderType.MistralAI => MistralAI,
            AiProviderType.GitHubModels => GitHubModels,
            AiProviderType.Replicate => Replicate,
            AiProviderType.OpenRouter => OpenRouter,
            _ => type.ToString().ToLowerInvariant()
        };

        public static AiProviderType? FromCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var norm = Normalize(code);
            if (norm.Contains("deepseek")) return AiProviderType.DeepSeek;
            if (norm.Contains("gemini") || norm.Contains("google")) return AiProviderType.GoogleGemini;
            if (norm.Contains("openai") || norm.Contains("gpt")) return AiProviderType.OpenAI;
            if (norm.Contains("anthropic") || norm.Contains("claude")) return AiProviderType.AnthropicClaude;
            if (norm.Contains("ollama") || norm.Contains("local") || norm.Contains("qwen")) return AiProviderType.LocalOllama;
            if (norm.Contains("groq")) return AiProviderType.Groq;
            if (norm.Contains("sambanova")) return AiProviderType.SambaNova;
            if (norm.Contains("cerebras")) return AiProviderType.Cerebras;
            if (norm.Contains("mistral") || norm.Contains("codestral")) return AiProviderType.MistralAI;
            if (norm.Contains("github")) return AiProviderType.GitHubModels;
            if (norm.Contains("replicate")) return AiProviderType.Replicate;
            if (norm.Contains("openrouter")) return AiProviderType.OpenRouter;
            return AiProviderType.OpenAI;
        }
    }

    /// <summary>
    /// Các mã tác vụ AI tiêu chuẩn (Well-Known Task Codes).
    /// Hệ thống hỗ trợ định nghĩa thêm bất kỳ TaskCode mới nào từ DB mà không cần sửa code.
    /// </summary>
    public static class WellKnownAiTasks
    {
        public const string SolveQuestions = "solve_questions";
        public const string CognitiveWalkthrough = "cognitive_walkthrough";
        public const string GenerateQuestions = "generate_questions";
        public const string GradeEssay = "grade_essay";
        public const string PersonalizedMentor = "personalized_mentor";
        public const string ArenaAdversary = "arena_adversary";
        public const string DistractorAnalysis = "distractor_analysis";
        public const string LatexConversion = "latex_conversion";
        public const string RealTimeProctoringAlert = "realtime_proctoring_alert";
        public const string SymposiumDebate = "symposium_debate";
        public const string ExamDeduplication = "exam_deduplication";
        public const string SpeechToText = "speech_to_text";

        public static string Normalize(string? task)
        {
            if (string.IsNullOrWhiteSpace(task)) return SolveQuestions;
            return task.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }

        public static string ToCode(AiTaskType taskType) => taskType switch
        {
            AiTaskType.SolveQuestions => SolveQuestions,
            AiTaskType.CognitiveWalkthrough => CognitiveWalkthrough,
            AiTaskType.GenerateQuestions => GenerateQuestions,
            AiTaskType.GradeEssay => GradeEssay,
            AiTaskType.PersonalizedMentor => PersonalizedMentor,
            AiTaskType.ArenaAdversary => ArenaAdversary,
            AiTaskType.DistractorAnalysis => DistractorAnalysis,
            AiTaskType.LatexConversion => LatexConversion,
            AiTaskType.RealTimeProctoringAlert => RealTimeProctoringAlert,
            _ => taskType.ToString().ToLowerInvariant()
        };

        public static AiTaskType FromCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return AiTaskType.SolveQuestions;
            var norm = Normalize(code);
            if (norm.Contains("solve")) return AiTaskType.SolveQuestions;
            if (norm.Contains("walkthrough") || norm.Contains("pedagogy")) return AiTaskType.CognitiveWalkthrough;
            if (norm.Contains("generate") || norm.Contains("quiz_gen")) return AiTaskType.GenerateQuestions;
            if (norm.Contains("grade") || norm.Contains("essay")) return AiTaskType.GradeEssay;
            if (norm.Contains("mentor") || norm.Contains("personal")) return AiTaskType.PersonalizedMentor;
            if (norm.Contains("adversary") || norm.Contains("arena")) return AiTaskType.ArenaAdversary;
            if (norm.Contains("distractor") || norm.Contains("trap")) return AiTaskType.DistractorAnalysis;
            if (norm.Contains("latex") || norm.Contains("math")) return AiTaskType.LatexConversion;
            if (norm.Contains("proctor") || norm.Contains("alert")) return AiTaskType.RealTimeProctoringAlert;
            return AiTaskType.SolveQuestions;
        }
    }

    /// <summary>
    /// Các Persona AI tiêu chuẩn (Well-Known Personas).
    /// </summary>
    public static class WellKnownAgentPersonas
    {
        public const string SupremeArbiter = "supreme_arbiter";
        public const string BankingLegalCounsel = "banking_legal_counsel";
        public const string SocratesMentor = "socrates_mentor";
        public const string RelentlessChaser = "relentless_chaser";
        public const string PsychProfiler = "psych_profiler";

        public static string ToCode(AgentPersonaType persona) => persona switch
        {
            AgentPersonaType.SupremeArbiter => SupremeArbiter,
            AgentPersonaType.BankingLegalCounsel => BankingLegalCounsel,
            AgentPersonaType.SocratesMentor => SocratesMentor,
            AgentPersonaType.RelentlessChaser => RelentlessChaser,
            AgentPersonaType.PsychProfiler => PsychProfiler,
            _ => persona.ToString().ToLowerInvariant()
        };

        public static AgentPersonaType FromCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return AgentPersonaType.SupremeArbiter;
            var norm = code.Trim().ToLowerInvariant();
            if (norm.Contains("legal") || norm.Contains("counsel")) return AgentPersonaType.BankingLegalCounsel;
            if (norm.Contains("socrates") || norm.Contains("mentor")) return AgentPersonaType.SocratesMentor;
            if (norm.Contains("chaser") || norm.Contains("hunter")) return AgentPersonaType.RelentlessChaser;
            if (norm.Contains("psych") || norm.Contains("profiler")) return AgentPersonaType.PsychProfiler;
            return AgentPersonaType.SupremeArbiter;
        }
    }

    #endregion

    #region Legacy Enums (DEPRECATED - Vi phạm OCP, chỉ duy trì cho tương thích ngược)

    [Obsolete("DEPRECATED: Vi phạm nguyên lý OCP. Hệ thống chuyển 100% sang chuỗi ProviderCode động lưu trong Database.")]
    public enum AiProviderType
    {
        DeepSeek = 1,
        GoogleGemini = 2,
        OpenAI = 3,
        AnthropicClaude = 4,
        LocalOllama = 5,
        Groq = 6,
        SambaNova = 7,
        Cerebras = 8,
        MistralAI = 9,
        GitHubModels = 10,
        Replicate = 11,
        OpenRouter = 12
    }

    [Obsolete("DEPRECATED: Vi phạm nguyên lý OCP. Hệ thống chuyển 100% sang chuỗi TaskCode động lưu trong Database.")]
    public enum AiTaskType
    {
        SolveQuestions = 1,          // Tự động giải đề thi, tìm đáp án đúng
        CognitiveWalkthrough = 2,    // Sinh bản đồ tư duy sư phạm 5 bước
        GenerateQuestions = 3,       // Sinh đề từ tài liệu, bài giảng
        GradeEssay = 4,              // Chấm thi tự luận theo barem
        PersonalizedMentor = 5,      // Cố vấn học tập, phân tích lỗ hổng tri thức
        ArenaAdversary = 6,          // Kẻ đi săn / Đối thủ AI trong Đấu trường Gameshow
        DistractorAnalysis = 7,      // Phân tích bẫy nhận thức trong phương án nhiễu
        LatexConversion = 8,         // Chuyển đổi công thức toán vector sang LaTeX
        RealTimeProctoringAlert = 9  // Cảnh báo giám thị thời gian thực độ trễ siêu thấp
    }

    [Obsolete("DEPRECATED: Vi phạm nguyên lý OCP. Hệ thống chuyển 100% sang chuỗi PersonaCode động cấu hình trong Council.")]
    public enum AgentPersonaType
    {
        SupremeArbiter = 1,          // Giám khảo khảo thí tối cao chuẩn AERA/ETS
        BankingLegalCounsel = 2,     // Cố vấn pháp lý & nghiệp vụ ngân hàng cấp cao
        SocratesMentor = 3,          // Gia sư khai phóng tri thức, gợi mở tư duy
        RelentlessChaser = 4,        // Kẻ đi săn sắc lạnh trong Gameshow
        PsychProfiler = 5            // Bác sĩ tâm lý đo lường độ do dự và phản xạ nhận thức
    }

    #endregion

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
        public string ProviderCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public List<string> ApiKeys { get; set; } = new();
        public string DefaultModel { get; set; } = string.Empty;
        public AegisQuiz.Domain.Entities.ModelProtocolType ProtocolType { get; set; } = AegisQuiz.Domain.Entities.ModelProtocolType.OpenAiCompatible;
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; } = 1; // Ưu tiên thấp hơn chạy trước
        public double CostPerMillionInputTokens { get; set; } = 0.0;
        public double CostPerMillionOutputTokens { get; set; } = 0.0;

        // [Backward Compatibility]
        [Obsolete("Sử dụng ProviderCode (string) để đảm bảo OCP")]
        public AiProviderType ProviderType
        {
            get => WellKnownAiProviders.FromCode(ProviderCode) ?? AiProviderType.OpenAI;
            set
            {
                if (string.IsNullOrWhiteSpace(ProviderCode))
                {
                    ProviderCode = WellKnownAiProviders.ToCode(value);
                }
            }
        }

        // [4D Epistemic Properties]
        public AegisQuiz.Domain.Entities.CognitiveArchetype Archetype { get; set; } = AegisQuiz.Domain.Entities.CognitiveArchetype.DeepReasoner;
        public int MaxContextTokens { get; set; } = 128000;
        public int BenchmarkElo { get; set; } = 1250;
        public int AvgLatencyMs { get; set; } = 300;
    }

    public class AiSystemConfigDto
    {
        public List<AiProviderEndpointConfig> Providers { get; set; } = new();
        public Dictionary<string, string> TaskRoutingMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, AegisQuiz.Domain.Entities.CognitiveArchetype> TaskArchetypeMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> DynamicTaskRoutingMap => TaskRoutingMap;
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
        public Dictionary<string, long> RequestCountByProvider { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class AiExecutionRequest
    {
        // 100% Dynamic Task Code (VD: "solve_questions", "grade_essay", hoặc tác vụ tùy biến từ DB)
        public string TaskCode { get; set; } = WellKnownAiTasks.SolveQuestions;

        [Obsolete("Sử dụng TaskCode (string) để đảm bảo OCP")]
        public AiTaskType TaskType
        {
            get => WellKnownAiTasks.FromCode(TaskCode);
            set => TaskCode = WellKnownAiTasks.ToCode(value);
        }

        public string Prompt { get; set; } = string.Empty;
        public string? SystemPrompt { get; set; }

        public string PersonaCode { get; set; } = WellKnownAgentPersonas.SupremeArbiter;

        [Obsolete("Sử dụng PersonaCode (string) để đảm bảo OCP")]
        public AgentPersonaType Persona
        {
            get => WellKnownAgentPersonas.FromCode(PersonaCode);
            set => PersonaCode = WellKnownAgentPersonas.ToCode(value);
        }

        // 100% Dynamic Provider Code (VD: "openrouter", "deepseek", "perplexity", "internal_bank_llm")
        public string? PreferredProviderCode { get; set; }

        [Obsolete("Sử dụng PreferredProviderCode (string) để đảm bảo OCP")]
        public AiProviderType? PreferredProvider
        {
            get => WellKnownAiProviders.FromCode(PreferredProviderCode);
            set => PreferredProviderCode = value.HasValue ? WellKnownAiProviders.ToCode(value.Value) : null;
        }

        public string? PreferredModel { get; set; }
        public double Temperature { get; set; } = 0.2;
        public int MaxTokens { get; set; } = 2048;
        public string? TenantId { get; set; }
        public string? CacheKey { get; set; }

        // [Epistemic 4D Steering Parameters]
        public AegisQuiz.Domain.Entities.CognitiveArchetype? RequiredArchetype { get; set; }
        public int? MinContextTokens { get; set; }
        public bool EnableHedgedExecution { get; set; } = false;
    }

    public class AiExecutionResponse
    {
        public bool Success { get; set; } = false;
        public string Content { get; set; } = string.Empty;
        public string ProviderUsed { get; set; } = string.Empty;
        public string ModelUsed { get; set; } = string.Empty;
        public bool IsFromCache { get; set; } = false;
        public long DurationMs { get; set; } = 0;
        public int TokensUsed { get; set; } = 0;
        public double EstimatedCostUsd { get; set; } = 0.0;
        public string? ErrorMessage { get; set; }
    }

    public class TenantByokConfigDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string ProviderCode { get; set; } = string.Empty;

        [Obsolete("Sử dụng ProviderCode (string) để đảm bảo OCP")]
        public AiProviderType ProviderType
        {
            get => WellKnownAiProviders.FromCode(ProviderCode) ?? AiProviderType.OpenAI;
            set => ProviderCode = WellKnownAiProviders.ToCode(value);
        }

        public string ApiKey { get; set; } = string.Empty;
        public string? CustomEndpoint { get; set; }
        public string? CustomModel { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
