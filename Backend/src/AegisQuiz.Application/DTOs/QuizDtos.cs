using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AegisQuiz.Application.DTOs
{
    public class UpdateAnswerRequest
    {
        public string CorrectAnswer { get; set; } = string.Empty;
        public string? AiExplanation { get; set; }
    }

    /// <summary>[NEW P3] Bulk enable/disable câu hỏi</summary>
    public class BulkToggleRequest
    {
        public List<Guid> Ids { get; set; } = new();
        public bool Enabled { get; set; } = true;
    }

    public class AdaptiveQuizRequest
    {
        public Guid UserId { get; set; }
        public bool ShuffleQuestions { get; set; } = true;
        public bool ShuffleAnswers { get; set; } = true;
        public int Count { get; set; } = 10;
    }

    public class QuestionAttemptDto
    {
        public Guid UserId { get; set; }
        public Guid QuestionId { get; set; }
        public string Category { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        public string SelectedAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int TimeSpentSeconds { get; set; }
    }

    public class PracticeTopicConfig
    {
        public string TopicKey { get; set; } = string.Empty;
        public int NumberOfQuestions { get; set; } = 5;
        public bool IsRandom { get; set; } = true;
        public int StartIndex { get; set; } = 1;
        public bool? OnlyCritical { get; set; }
    }

    public class GeneratePracticeRequest
    {
        public string Mode { get; set; } = "study";
        public List<PracticeTopicConfig> TopicConfigs { get; set; } = new();
        public bool ShuffleQuestions { get; set; } = true;
        public bool ShuffleAnswers { get; set; } = true;
        public bool OnlyCritical { get; set; } = false;
    }

    public class ExplainRequest
    {
        public string SelectedAnswer { get; set; } = string.Empty;
    }

    public class AttemptItemDto
    {
        public Guid QuestionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string UserAnswer { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public class AnalyzeAttemptRequest
    {
        public List<AttemptItemDto> Attempts { get; set; } = new();
    }

    public class QuestionUpsertDto
    {
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "SINGLE";
        public int Difficulty { get; set; } = 1;
        public int DurationSeconds { get; set; } = 60;
        public string CategoryCode { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text";
        public string OptionType { get; set; } = "text";
        public List<string>? Options { get; set; }
        public string? CorrectOption { get; set; }
        public JsonObject? Payload { get; set; }
        public bool IsCritical { get; set; } = false;
        public string? SubCategory { get; set; }
        public Guid? ContextId { get; set; }
        public string? ContextTitle { get; set; }
        public string? ContextContent { get; set; }

        // [Universal Multi-Industry Taxonomy & Smart Tags]
        public string DomainCode { get; set; } = "EDUCATION";
        public List<string>? Tags { get; set; }
        public string? TargetLevel { get; set; }
        public string? AssessmentPurpose { get; set; }
        public string? IssuingOrg { get; set; }
        public int? BenchmarkYear { get; set; }
        public string? BenchmarkStandard { get; set; }
    }

    public class BankTopicUpsertDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CategoryCode { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public bool Enabled { get; set; } = true;
        public string VisibilityScope { get; set; } = "PRIVATE";
        public string? DomainCode { get; set; }
        public string? Scope { get; set; }
    }

    public class BatchAssignContextDto
    {
        public List<Guid> QuestionIds { get; set; } = new();
        public Guid? ContextId { get; set; }
        public string? ContextTitle { get; set; }
        public string? ContextContent { get; set; }
    }

    public class BatchRemoveContextDto
    {
        public List<Guid> QuestionIds { get; set; } = new();
    }

    public class BatchAssignTagsDto
    {
        public List<Guid> QuestionIds { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public string? DomainCode { get; set; }
        public string? TargetLevel { get; set; }
        public string? AssessmentPurpose { get; set; }
        public string? IssuingOrg { get; set; }
        public int? BenchmarkYear { get; set; }
        public string? BenchmarkStandard { get; set; }
    }

    public class AutoSniffCoordinatesRequest
    {
        public List<Guid>? QuestionIds { get; set; }
        public bool PreviewOnly { get; set; } = false;
    }

    /// <summary>[Omni-Century] Nhóm Lĩnh Vực Nhận Thức (Cognitive Domain Group)</summary>
    public class CognitiveDomainGroupDto
    {
        public string DomainCode { get; set; } = string.Empty;
        public string DomainName { get; set; } = string.Empty;
        public string Icon { get; set; } = "Folder";
        public string ColorBadge { get; set; } = "#2563eb";
        public int TotalQuestionCount { get; set; } = 0;
        public List<CognitiveTopicNodeDto> RootTopics { get; set; } = new();
    }

    /// <summary>[Omni-Century] Nút Phân Cấp Tri Thức Nhận Thức (Cognitive Topic Node)</summary>
    public class CognitiveTopicNodeDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DomainCode { get; set; } = string.Empty;
        public string Scope { get; set; } = "COMMUNITY";
        public string MaterializedPath { get; set; } = string.Empty;
        public int DepthLevel { get; set; } = 0;
        public Guid? ParentId { get; set; }
        public int QuestionCount { get; set; } = 0;
        public bool Enabled { get; set; } = true;
        public List<CognitiveTopicNodeDto> Children { get; set; } = new();
    }
}
