using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AegisQuiz.Application.Models.AI
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CoPilotPersona
    {
        AssessmentArchitect, // Giảng viên, Chuyên gia soạn đề, Thẩm định Bloom
        SocraticMentor,      // Gia sư phát vấn thích ứng cho người học (không mớm bài)
        GovernanceFinOps,    // Quản trị viên trường/doanh nghiệp, FinOps & kiểm toán gian lận
        ResearchScholar      // Nhà nghiên cứu tri thức liên nguồn (AegisNotebook V2.0)
    }

    public class CoPilotContextDto
    {
        public string? ScreenName { get; set; } // "ExamRoom", "AdminQuestions", "Notebooks", "Symposium", "Practice"
        public string? SelectedText { get; set; }
        public Guid? CurrentQuestionId { get; set; }
        public Guid? CurrentTopicId { get; set; }
        public string? DomainCode { get; set; }
        public Dictionary<string, string>? Metadata { get; set; } = new();
    }

    public class CoPilotHistoryItemDto
    {
        public string Role { get; set; } = "user"; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
    }

    public class CoPilotRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public CoPilotPersona Persona { get; set; } = CoPilotPersona.AssessmentArchitect;
        public string? ModelCode { get; set; }
        public CoPilotContextDto? Context { get; set; }
        public List<CoPilotHistoryItemDto>? History { get; set; } = new();
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
    }

    public class CoPilotToolCallDto
    {
        public string ToolName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "completed"; // "running", "completed", "failed"
        public long DurationMs { get; set; } = 0;
        public Dictionary<string, object>? OutputSummary { get; set; }
    }

    public class CoPilotArtifactDto
    {
        public string ArtifactType { get; set; } = "question"; // "question", "matrix", "rubric", "walkthrough", "symposium", "swarm-audit"
        public string Title { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string? RawMarkdown { get; set; }
        public bool CanApplyDirectly { get; set; } = true;
    }

    public class CoPilotResponseDto
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
        public string ThoughtTrace { get; set; } = string.Empty;
        public List<CoPilotToolCallDto> ToolCalls { get; set; } = new();
        public string Content { get; set; } = string.Empty;
        public CoPilotArtifactDto? Artifact { get; set; }
        public List<string> SuggestedActions { get; set; } = new();
        public string ModelUsed { get; set; } = "UniversalAiRouter:Auto";
        public string KeySourceUsed { get; set; } = "SystemDefault"; // "UserBYOK" | "TenantBYOK" | "CommunityPool" | "SystemDefault"
        public long LatencyMs { get; set; }
        public int TokenUsage { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }

    public class CoPilotSlashCommandDto
    {
        public string Command { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
        public CoPilotPersona RecommendedPersona { get; set; } = CoPilotPersona.AssessmentArchitect;
        public string Icon { get; set; } = "Terminal";
    }

    public class CoPilotModelItemDto
    {
        public string ModelCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Archetype { get; set; } = "DeepReasoner";
        public string Description { get; set; } = string.Empty;
        public string KeySource { get; set; } = "System"; // "UserBYOK" | "TenantBYOK" | "System"
        public bool IsRecommended { get; set; } = false;
        public string? RecommendationReason { get; set; }
        public double LatencyBenchmarkMs { get; set; } = 450;
        public double CostPerMillionTokensUsd { get; set; } = 0.0;

        // Trạng thái và chỉ số sẵn sàng của mô hình (WPSE Health Status)
        public bool IsAvailable { get; set; } = true;
        public string AvailabilityStatus { get; set; } = "Ready"; // "Ready" | "FreeCommunity" | "RequiresKey" | "LocalNode"
        public string StatusBadge { get; set; } = "Khả dụng";
        public bool IsFreeOrLocal { get; set; } = false;
    }

    public class CoPilotModelGroupDto
    {
        public string ProviderName { get; set; } = string.Empty; // "Anthropic", "Google Gemini", "OpenAI", "DeepSeek", "Local Ollama"
        public string ProviderCode { get; set; } = string.Empty;
        public string ProviderIcon { get; set; } = "Cpu";
        public string BadgeColor { get; set; } = "indigo";
        public List<CoPilotModelItemDto> Models { get; set; } = new();
    }

    public class ExportCoPilotToNotebookRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ArtifactType { get; set; }
        public object? ArtifactData { get; set; }
        public Guid? TargetNotebookId { get; set; }
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
    }

    public class ExportCoPilotToNotebookResponse
    {
        public Guid NotebookId { get; set; }
        public Guid? ChapterId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; } = true;
    }
}
