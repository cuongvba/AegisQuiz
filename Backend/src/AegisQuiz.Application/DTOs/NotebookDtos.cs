using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.DTOs
{
    // [AegisNotebook Sovereign DTOs]
    public class NotebookSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int TotalPages { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DomainCode { get; set; } = "GENERAL";
        public Guid? BankTopicId { get; set; }
        public string? BankTopicTitle { get; set; }
        public int SourceCount { get; set; }
        public int ChapterCount { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class NotebookDetailDto : NotebookSummaryDto
    {
        public string SourcePdfUrl { get; set; } = string.Empty;
        public string CognitiveStatsJson { get; set; } = "{}";
        public string ConflictReportJson { get; set; } = "[]";

        // Persona Role
        public string PersonaName { get; set; } = "Cố Vấn Tri Thức Trưởng";
        public int PersonaRole { get; set; } = 1;
        public string CustomPersonaPrompt { get; set; } = string.Empty;

        public List<NotebookSourceDto> Sources { get; set; } = new();
        public List<NotebookChapterDto> Chapters { get; set; } = new();
        public List<NotebookChatMessageDto> ChatMessages { get; set; } = new();
    }

    public class NotebookSourceDto
    {
        public Guid Id { get; set; }
        public Guid NotebookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int TotalPages { get; set; }
        public string Type { get; set; } = "Pdf"; // Pdf, Docx, Excel, WebUrl, PlainText
        public int Authority { get; set; } = 2; // 4: SupremeLegal, 3: Regulatory, 2: InternalSop, 1: ReferenceCase
        public string Status { get; set; } = "Ready";
        public DateTime UploadedAt { get; set; }
    }

    public class NotebookChapterDto
    {
        public Guid Id { get; set; }
        public Guid NotebookId { get; set; }
        public int OrderIndex { get; set; }
        public string Title { get; set; } = string.Empty;
        public int StartPage { get; set; }
        public int EndPage { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string RawContent { get; set; } = string.Empty;
        public List<Guid> SourceIds { get; set; } = new();
        public List<string> KeyPoints { get; set; } = new();
        public string PinpointCoordinatesJson { get; set; } = "[]";
        public int QuestionCount { get; set; }
        public List<Guid> GeneratedQuestionIds { get; set; } = new();
    }

    public class NotebookCreateHubDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DomainCode { get; set; } = "GENERAL";
        public Guid? BankTopicId { get; set; }
        public string? PersonaName { get; set; }
        public int? PersonaRole { get; set; }
        public string? CustomPersonaPrompt { get; set; }
    }

    public class NotebookUploadRequestDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? DomainCode { get; set; }
        public Guid? BankTopicId { get; set; }
        public int AuthorityLevel { get; set; } = 2;
    }

    public class AddSourceUrlDto
    {
        public string Url { get; set; } = string.Empty;
        public string? Title { get; set; }
        public int AuthorityLevel { get; set; } = 1;
    }

    public class UpdatePersonaDto
    {
        public string PersonaName { get; set; } = string.Empty;
        public int PersonaRole { get; set; } = 1;
        public string? CustomPersonaPrompt { get; set; }
    }

    public class NotebookChatMessageDto
    {
        public Guid Id { get; set; }
        public Guid NotebookId { get; set; }
        public string Sender { get; set; } = "User";
        public string RoleName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string CitationsJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; }
    }

    public class SendChatMessageDto
    {
        public string Message { get; set; } = string.Empty;
    }

    public class GenerateChapterQuizRequestDto
    {
        public int QuestionCount { get; set; } = 10;
        public List<string> QuestionTypes { get; set; } = new() { "Single", "MultipleChoice", "TrueFalse" };
        public int DifficultyBloomLevel { get; set; } = 2; // 1-6 Bloom
        public bool IncludeIrtCalibration { get; set; } = true;
    }

    public class GenerateCrossQuizRequestDto
    {
        public int TotalQuestionCount { get; set; } = 10;
        public int SingleSourcePercent { get; set; } = 40; // Tỷ lệ câu hỏi đơn nguồn
        public int CrossSourcePercent { get; set; } = 60;  // Tỷ lệ câu hỏi tình huống đa nguồn
        public List<string> QuestionTypes { get; set; } = new() { "Single", "MultipleChoice", "SituationalCase" };
        public int TargetBloomLevel { get; set; } = 5; // 1-6 Bloom
        public bool IncludeIrtCalibration { get; set; } = true;
    }

    public class PublishNotebookQuestionsDto
    {
        public Guid? ChapterId { get; set; } // Null = Publish toàn bộ Notebook
        public Guid TargetBankTopicId { get; set; }
        public string VisibilityScope { get; set; } = "PRIVATE"; // PRIVATE, COMMUNITY, PUBLIC
    }
}
