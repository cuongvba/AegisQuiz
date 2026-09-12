using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Application.DTOs
{
    /// <summary>
    /// Bản đặc tả một Quy tắc Bóc tách Câu hỏi Cấu hình Động (Cognitive Ingestion Rule).
    /// Hỗ trợ nạp từ File JSON hoặc CSDL PostgreSQL.
    /// </summary>
    public class QuestionParsingRuleDto
    {
        public string RuleCode { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string TargetExamType { get; set; } = "GENERAL";
        public string Language { get; set; } = "vi";
        public int Priority { get; set; } = 100;
        public bool IsActive { get; set; } = true;
        public string Description { get; set; } = string.Empty;

        public AnchorSpecDto AnchorSpec { get; set; } = new();
        public StimulusSpecDto StimulusSpec { get; set; } = new();
        public InteractionSpecDto InteractionSpec { get; set; } = new();
        public EvaluationSpecDto EvaluationSpec { get; set; } = new();

        // Thuộc tính Alias tương thích ngược / LLM JSON mapping
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public AnchorSpecDto? Structure
        {
            get => AnchorSpec;
            set { if (value != null) AnchorSpec = value; }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public StimulusSpecDto? Semantics
        {
            get => StimulusSpec;
            set { if (value != null) StimulusSpec = value; }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InteractionSpecDto? Options
        {
            get => InteractionSpec;
            set { if (value != null) InteractionSpec = value; }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public EvaluationSpecDto? Evaluation
        {
            get => EvaluationSpec;
            set { if (value != null) EvaluationSpec = value; }
        }
    }

    /// <summary>Chiều 1: Điểm neo nhận diện bắt đầu câu hỏi</summary>
    public class AnchorSpecDto
    {
        public string RegexPattern { get; set; } = @"^(?:Câu|Question|Item|Task|Exercise|Q)\s*(\d+)";
        public int QuestionNumberGroupIndex { get; set; } = 1;
        public List<string> SectionMarkers { get; set; } = new();

        // Aliases cho AI / Schema compatibility
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? QuestionHeaderRegexPattern
        {
            get => RegexPattern;
            set { if (value != null) RegexPattern = value; }
        }

        public string DelimiterType { get; set; } = "REGEX_LINE_START";
    }

    /// <summary>Chiều 2: Bối cảnh kích thích trí tuệ (Công thức toán, hình vẽ, bài đọc)</summary>
    public class StimulusSpecDto
    {
        public bool SupportMathTypeWmf { get; set; } = true;
        public bool SupportVectorSvg { get; set; } = true;
        public bool ExtractInlineImages { get; set; } = true;
        public string? ReadingPassageStartRegex { get; set; }
        public string? ReadingPassageRangeRegex { get; set; }

        // Aliases
        public string? MathBlockMarkerRegex { get; set; }
        public string? CodeBlockMarkerRegex { get; set; }
        public string? ImageMarkerRegex { get; set; }
    }

    /// <summary>Chiều 3: Siêu hình thái tương tác (Options, Matrix, Short answer...)</summary>
    public class InteractionSpecDto
    {
        public string DefaultQuestionType { get; set; } = "SINGLE"; // SINGLE, MULTI, TRUE_FALSE, SHORT_ANSWER, MATCHING, ORDERING
        public string OptionRegexPattern { get; set; } = @"^(?:([A-D])|[\(]([A-D])[\)])[\.\:\s\-]+(.*)$";
        public int OptionLabelGroupIndex { get; set; } = 1;
        public int OptionContentGroupIndex { get; set; } = 3;
        public string? TrueFalseSubItemRegex { get; set; } = @"^(?:([a-d])|[\(]([a-d])[\)])[\.\:\s\-]+(.*)$";
        public string? ShortAnswerPromptRegex { get; set; }

        // Aliases
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? QuestionType
        {
            get => DefaultQuestionType;
            set { if (value != null) DefaultQuestionType = value; }
        }
    }

    /// <summary>Chiều 4: Hàm chân lý phân xử điểm số & cờ tử thần</summary>
    public class EvaluationSpecDto
    {
        public string AnswerRegexPattern { get; set; } = @"^(?:ĐÁP\s*ÁN|ANSWER|KEY|CHỌN)[\.\:\s\-]+([A-D0-9a-d\,\s]+)";
        public int AnswerGroupIndex { get; set; } = 1;
        public string ExplanationRegexPattern { get; set; } = @"^(?:Lời\s*giải|Hướng\s*dẫn|HD|Giải\s*thích|Explanation|Căn\s*cứ)\b";
        public string? CriticalQuestionMarkerRegex { get; set; } = @"(?:\[\s*ĐIỂM\s*LIỆT\s*\]|⚠️|TỬ\s*THẦN)";
        public string ScoreStrategy { get; set; } = "ALL_OR_NOTHING"; // ALL_OR_NOTHING, PARTIAL_CREDIT, FATAL_LOCK
    }

    /// <summary>Yêu cầu kiểm thử nhanh một đoạn snippet đề thi từ Admin UI Sandbox</summary>
    public class ParseSnippetRequestDto
    {
        public string SnippetText { get; set; } = string.Empty;
        public string? SpecificRuleCode { get; set; }
    }

    /// <summary>Yêu cầu AI tự động phân tích đề thi lạ và sinh quy tắc CIG</summary>
    public class AiRuleAutopilotRequestDto
    {
        public string SnippetText { get; set; } = string.Empty;
        public string? ExamHint { get; set; }
        public string Language { get; set; } = "vi";
    }

    /// <summary>Kết quả AI Autopilot tự sinh quy tắc</summary>
    public class AiRuleAutopilotResponseDto
    {
        public bool Success { get; set; } = true;
        public double ConfidenceScore { get; set; } = 95.0; // 0.0 - 100.0%
        public string Reasoning { get; set; } = string.Empty;
        public QuestionParsingRuleDto GeneratedRule { get; set; } = new();
        public ParseSnippetResponseDto TestedParseResult { get; set; } = new();
    }

    /// <summary>Kết quả kiểm thử phân tích đoạn văn bản mẫu đề thi</summary>
    public class ParseSnippetResponseDto
    {
        public bool Success { get; set; } = true;
        public string? MatchedRuleCode { get; set; }
        public int ParsedCount { get; set; } = 0;
        public int TotalQuestionsFound => ParsedCount > 0 ? ParsedCount : Questions.Count;
        public List<DocxImportPreviewDto> Questions { get; set; } = new();
        public List<string> ParsingLogs { get; set; } = new();
    }
}
