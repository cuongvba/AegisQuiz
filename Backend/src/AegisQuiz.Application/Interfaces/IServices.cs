using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    // [Smart Excel Ingestion — DOT-2026 Format] Interface cho tất cả Service
    public interface IExcelParserService
    {
        /// <summary>Nhập câu hỏi từ tất cả sheet của file Excel (kiểu cũ — trả về entities).</summary>
        List<QuestionBase> ParseExcelFile(Stream fileStream);

        /// <summary>
        /// [MỚI — DOT-2026 Smart Format] Nhập câu hỏi từ file Excel với nhận diện thông minh:
        /// - Phát hiện tự động dòng header bất kể số dòng tiêu đề
        /// - Bóc tách chủ đề từ tên Sheet, tên file, hoặc cột Chủ đề
        /// - Hỗ trợ tất cả các sheet có câu hỏi (bỏ qua sheet Văn bản / Danh mục)
        /// - Ánh xạ thông minh các cột: STT, CÂU HỎI, ĐÁP ÁN 1-4, ĐÁP ÁN ĐÚNG, TRÍCH DẪN
        /// Trả về danh sách DocxImportPreviewDto để hiển thị preview trước khi lưu
        /// </summary>
        ExcelImportPreviewResult ParseExcelFileToPreview(Stream fileStream, string? fileName = null);
    }

    /// <summary>Kết quả preview toàn diện từ một file đề thi (Excel, DOCX, PDF) theo kiến trúc Universal Ingestion</summary>
    public class UniversalImportPreviewResult
    {
        public string FileName { get; set; } = string.Empty;
        public string IngestionSource { get; set; } = "EXCEL"; // EXCEL, DOCX, PDF, OCR, AI
        public int TotalSheets { get; set; }
        public int TotalQuestions { get; set; }
        public List<ExcelSheetPreview> Sheets { get; set; } = new();
        public List<ExcelSkippedSheetPreview> SkippedSheets { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        // Siêu dữ liệu biểu ngữ toàn cục (Pre-Header Banner Mining)
        public Dictionary<string, string> GlobalMetadataBanner { get; set; } = new();

        // Hệ 5 Trục Tọa Độ & Thẻ Thông Minh Nhận Diện Toàn Cục
        public string DetectedDomainCode { get; set; } = "GENERAL";
        public string? DetectedTargetLevel { get; set; }
        public string? DetectedAssessmentPurpose { get; set; }
        public string? DetectedIssuingOrg { get; set; } = "GDDT";
        public int? DetectedBenchmarkYear { get; set; } = 2026;
        public string? DetectedBenchmarkStandard { get; set; } = "CIG V3";
        public List<string> DetectedTags { get; set; } = new();

        // [Universal Exam Deduplication & Provenance Summary]
        public int DuplicateCount { get; set; }
        public int UniqueCount { get; set; }
        public double DeduplicationRatePercentage => TotalQuestions > 0 ? System.Math.Round((double)DuplicateCount / TotalQuestions * 100, 1) : 0;
        public List<ExamClusterMatch> DetectedExamClusters { get; set; } = new();

        // [Universal Question List] Danh sách câu hỏi phẳng (phục vụ DOCX, PDF, hoặc Excel tổng hợp)
        public List<DocxImportPreviewDto> Questions { get; set; } = new();
    }

    /// <summary>Kết quả preview toàn diện từ một file Excel theo kiến trúc UCIS (kế thừa UniversalImportPreviewResult)</summary>
    public class ExcelImportPreviewResult : UniversalImportPreviewResult
    {
    }

    /// <summary>Thông tin thanh tra và câu hỏi trong một sheet Excel</summary>
    public class ExcelSheetPreview
    {
        public int SheetIndex { get; set; }
        public string SheetName { get; set; } = string.Empty;
        public string SheetRole { get; set; } = "EXAM_QUIZ"; // EXAM_QUIZ, REFERENCE_DOCS, SCORING_RUBRIC, CANDIDATE_ROSTER, METADATA_COVER
        public double ConfidenceScore { get; set; } = 1.0;
        public bool IsEnabledForImport { get; set; } = true;

        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public int HeaderRowIndex { get; set; }

        public string DetectedTopicCode { get; set; } = string.Empty;
        public string DetectedTopicName { get; set; } = string.Empty;
        public string? DetectedParentTopicCode { get; set; }
        public string? DetectedParentTopicName { get; set; }
        public string? DetectedTopicDescription { get; set; }
        public int QuestionCount { get; set; }
        public List<DocxImportPreviewDto> Questions { get; set; } = new();

        // Bản đồ ánh xạ cột được hệ thống tự động nhận diện (Tên vai trò -> Index cột)
        public Dictionary<string, int> ColumnMapping { get; set; } = new();

        // 10 hàng đầu tiên của sheet phục vụ render Data Grid ảo ở Frontend Studio
        public List<List<string>> SampleRows { get; set; } = new();

        // Chỉ số sức khỏe nhận thức của đề thi
        public ExcelCognitiveHealthStats HealthStats { get; set; } = new();

        // Hệ 5 Trục Tọa Độ & Thẻ Thông Minh Nhận Diện Riêng Cho Sheet
        public string DetectedDomainCode { get; set; } = "GENERAL";
        public string? DetectedTargetLevel { get; set; }
        public string? DetectedAssessmentPurpose { get; set; }
        public string? DetectedIssuingOrg { get; set; }
        public int? DetectedBenchmarkYear { get; set; }
        public string? DetectedBenchmarkStandard { get; set; }
        public List<string> DetectedTags { get; set; } = new();
    }

    /// <summary>Thông tin các sheet phi đề thi được tự động lọc bỏ (nhưng vẫn có thể kích hoạt lại trên UI Studio)</summary>
    public class ExcelSkippedSheetPreview
    {
        public int SheetIndex { get; set; }
        public string SheetName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string SuggestedRole { get; set; } = "REFERENCE_DOCS";
        public int RowCount { get; set; }
        public List<List<string>> SampleRows { get; set; } = new();
    }

    /// <summary>Chỉ số sức khỏe nhận thức và bất thường của bộ đề</summary>
    public class ExcelCognitiveHealthStats
    {
        public int ValidQuestionCount { get; set; }
        public int MissingAnswerCount { get; set; }
        public int FatalQuestionCount { get; set; }
        public Dictionary<string, int> QuestionTypeBreakdown { get; set; } = new();
        public Dictionary<int, int> BloomDistribution { get; set; } = new(); // 1, 2, 3, 4
    }


    public interface IGeminiGradingService
    {
        Task<GradingResult> GradeEssayAsync(string questionContent, string rubric, string studentAnswer);
    }

    public interface IGeminiMentorService
    {
        Task<string> GeneratePersonalizedStudyPlan(Guid userId, List<QuestionAttempt> history, bool isPremium);
    }

    public interface IPaymentService
    {
        Task<PaymentTransaction> GeneratePaymentCodeAsync(Guid userId, decimal amount, string currency);
        Task<bool> ProcessWebhookAsync(string paymentCode, decimal amountReceived);
    }

    // [AegisNotebook & AI Generator] Interface cho Pipeline PDF & Word → Giáo trình → Quiz
    public interface IPdfExtractorService
    {
        string ExtractTextFromPdf(Stream pdfStream);
        List<DocxImportPreviewDto> ParsePdfQuestions(Stream pdfStream);
        Task<string> ExtractTextFromScannedPdfAsync(Stream pdfStream);
    }

    // DTO chung cho AI Grading
    public class GradingResult
    {
        public double Score { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }

    /// <summary>
    /// [Universal Cognitive Sub-Item / Statement / Stepped Essay Entity]
    /// Thực thể phân cấp Ý con / Mệnh đề Đúng-Sai / Bước giải tự luận độc lập
    /// </summary>
    public class QuestionSubItemDto
    {
        public string Key { get; set; } = string.Empty;             // "a", "b", "c", "d" hoặc "1", "2"
        public string Content { get; set; } = string.Empty;         // Nội dung ý hỏi / mệnh đề con
        public string? ExpectedAnswer { get; set; }                 // "Đúng" / "Sai" / Giá trị đáp số (nếu có)
        public string? Explanation { get; set; }                    // Lời giải ĐỘC LẬP của ý này (Tư liệu kịch bản hoạt họa / video storyboard)
        public decimal? MaxScore { get; set; }                      // Biểu điểm riêng của ý (nếu là tự luận phân bước)
        public int OrderIndex { get; set; }
    }

    /// <summary>
    /// [Universal Cognitive Ingestion DTO]
    /// DTO nhận thức toàn năng dùng chung cho mọi động cơ bóc tách đề thi (Excel, Word, PDF, Scanned OCR, AI Generator)
    /// </summary>
    public class UniversalQuestionPreviewDto
    {
        public string TempId { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "SINGLE"; // SINGLE, MULTI, TRUE_FALSE, ESSAY, MATCHING, ORDERING, SHORT_ANSWER
        public string ContentType { get; set; } = "text"; // text | image
        public string OptionType { get; set; } = "text"; // text | image
        public List<string> Options { get; set; } = new();

        // [Adaptive Polymorphic Hierarchy: Tầng Ý Phân Cấp (Optional Resolution)]
        public List<QuestionSubItemDto> SubItems { get; set; } = new();
        public bool HasSubItems => SubItems != null && SubItems.Count > 0;
        public bool HasGranularExplanations => HasSubItems && SubItems.Any(s => !string.IsNullOrWhiteSpace(s.Explanation));

        public string SuggestedAnswer { get; set; } = string.Empty; // Gợi ý đáp án (ví dụ: "1", "2" hoặc "a) Đúng, b) Sai...")
        public string AiExplanation { get; set; } = string.Empty; // Lời giải toàn văn / giải thích chung
        public string TopicCode { get; set; } = "PARTY_BUILDING";
        public string? TopicName { get; set; }
        public string? ParentTopicCode { get; set; }
        public string? ParentTopicName { get; set; }
        public string? TopicDescription { get; set; }
        public int Difficulty { get; set; } = 3;
        public bool IsCritical { get; set; } = false; // [Sát hạch giao thông] Câu điểm liệt
        public string? SubCategory { get; set; } // SA_HINH | BIEN_BAO | LUAT | KY_THUAT

        // [Universal Shared Context / Stimulus] Dữ liệu dùng chung (Đoạn văn đọc hiểu, file nghe, biểu đồ)
        public string? ContextId { get; set; }
        public string? ContextTitle { get; set; }
        public string? ContextContent { get; set; }

        // [Universal Multi-Industry Semantic Taxonomy & Smart Tags] Hệ 5 trục tọa độ & thẻ thông minh
        public string DomainCode { get; set; } = "GENERAL";
        public List<string> Tags { get; set; } = new();
        public string? TargetLevel { get; set; }
        public string? AssessmentPurpose { get; set; }
        public string? IssuingOrg { get; set; } = "GDDT";
        public int? BenchmarkYear { get; set; } = 2026;
        public string? BenchmarkStandard { get; set; } = "CIG V3";

        // [Universal Extended Fields: Sư phạm, Khảo thí & Đa phương tiện]
        public string? GradingRubric { get; set; } // Tiêu chí chấm điểm / Barem điểm
        public int DurationSeconds { get; set; } = 60; // Thời gian chuẩn làm bài
        public string? MediaUrl { get; set; } // Đường dẫn hình ảnh, sơ đồ, audio

        // [Multi-tier Ownership & RLAC] Quyền tác giả, đơn vị & phạm vi sở hữu
        public string? CreatorId { get; set; }
        public string? CreatorName { get; set; }
        public string? OrgUnitId { get; set; }
        public string? OrgUnitName { get; set; }
        public string? Scope { get; set; } = "COMMUNITY";
        public string? VisibilityScope { get; set; } = "PUBLIC";
        public string? ContributionStatus { get; set; }

        // [Universal Question Provenance & Multi-Tier Deduplication Engine]
        public string? SourceFileName { get; set; }
        public string? SourceExamName { get; set; }
        public string? SourceSectionName { get; set; }
        public string? SourceQuestionNo { get; set; }
        public int? SourceRowIndex { get; set; }
        public string? FileHashSha256 { get; set; }
        public string? CanonicalContentHash { get; set; }
        public string? CanonicalExamUrn { get; set; }
        public bool IsDuplicate { get; set; } = false;
        public System.Guid? ExistingQuestionId { get; set; }
        public string? DuplicateReason { get; set; }
        public int OccurrenceCount { get; set; } = 1;
        public List<ProvenanceAppearance> ExistingAppearances { get; set; } = new();
    }

    /// <summary>
    /// [Legacy Compatibility Alias]
    /// Giữ nguyên DocxImportPreviewDto kế thừa từ UniversalQuestionPreviewDto để đảm bảo 100% tương thích ngược
    /// </summary>
    public class DocxImportPreviewDto : UniversalQuestionPreviewDto
    {
    }

    public interface IDocxParserService
    {
        List<DocxImportPreviewDto> ParseDocxFile(Stream fileStream);
        List<DocxImportPreviewDto> ParseDocxFile(Stream fileStream, DynamicDomainContext? dynamicContext);
        string ExtractTextFromDocx(Stream fileStream);
    }

    /// <summary>
    /// [SGI Engine] Hợp đồng quản trị phạm vi nhận thức khảo thí đa tầng
    /// </summary>
    public interface ICognitiveScopeManager
    {
        string CurrentMacroSection { get; }
        string? CurrentSectionTitle { get; }
        IReadOnlyList<DirectiveScopeWindow> ScopeWindows { get; }
        IReadOnlyList<ContextStimulusWindow> ContextWindows { get; }

        void RegisterMacroSection(string sectionKey, string? sectionTitle);
        void RegisterScopeDirective(DirectiveScopeWindow window);
        void RegisterScopeDirective(int startQ, int endQ, string questionType, string? rawDirective, double confidence = 1.0);
        void RegisterContextPassage(int startQ, int endQ, string content, string? title = null, string? contextId = null);
        string ResolveExpectedQuestionType(int qNum, string? fallbackType = null);
        ContextStimulusWindow? ResolveContextForQuestion(int qNum);
    }

    /// <summary>
    /// [CIG v4.0 Finite State Machine] Interface máy trạng thái bóc tách và phân loại câu hỏi đề thi
    /// </summary>
    public interface IDocxStateMachineClassifier
    {
        List<DocxImportPreviewDto> ParseParagraphList(
            List<string> paragraphs,
            Dictionary<string, Dictionary<string, Dictionary<int, string>>>? answerKeyMap = null,
            DynamicDomainContext? dynamicContext = null);
    }


    public interface IGeminiSolverService
    {
        Task<List<DocxImportPreviewDto>> AutoSolveQuestionsAsync(List<DocxImportPreviewDto> rawQuestions);
        Task<List<DocxImportPreviewDto>> ConvertMathImagesToLatexAsync(List<DocxImportPreviewDto> questions);
    }

    // [Kịch bản B] DTO & Service sinh câu hỏi tự động từ tài liệu nội dung
    public class GenerateQuestionsRequest
    {
        public string? DocumentText { get; set; }
        public int QuestionCount { get; set; } = 5;
        public string QuestionType { get; set; } = "ALL"; // SINGLE, MULTI, TRUE_FALSE, ESSAY, ALL
        public int Difficulty { get; set; } = 3;
        public string TopicCode { get; set; } = "GENERAL";
        public string? FocusArea { get; set; }
    }

    public interface IAiQuestionGeneratorService
    {
        Task<List<DocxImportPreviewDto>> GenerateQuestionsFromTextAsync(GenerateQuestionsRequest request);
        Task<List<DocxImportPreviewDto>> GenerateQuestionsFromDocumentAsync(Stream documentStream, string fileName, GenerateQuestionsRequest request);
    }
}

