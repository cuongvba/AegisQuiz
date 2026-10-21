using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Cognitive Ingestion Grammar] Hợp đồng động cơ nhận dạng và bóc tách câu hỏi động theo chuẩn Clear Architecture.
    /// </summary>
    public interface IDynamicQuestionIngestionEngine
    {
        IReadOnlyList<QuestionParsingRuleDto> GetAllRules(Guid? tenantId = null);
        DocumentLanguageProfileDto DetectDocumentLanguageProfile(IEnumerable<string> paragraphs, string defaultLanguage = "vi");
        IReadOnlyList<QuestionParsingRuleDto> GetRulesForProfile(DocumentLanguageProfileDto profile, Guid? tenantId = null);
        bool TryMatchQuestionAnchor(string line, out int questionNumber, out QuestionParsingRuleDto? matchedRule, IEnumerable<QuestionParsingRuleDto>? activeRules = null);
        bool IsCriticalQuestion(string text, QuestionParsingRuleDto? rule = null);
        bool IsEssayQuestion(string text, QuestionParsingRuleDto? rule = null, string? topicCode = null);
        bool TryMatchExamHeading(string line, out string examCode);
        bool TryMatchTopicHeading(string line, out string topicName);
        bool TryMatchSectionHeader(string line, out string sectionKey);
        bool TryMatchSolutionPhase(string line);
        bool TryMatchAnswerTablePhase(string line);
        bool TryMatchReadingPassage(string line, out int startQ, out int endQ);
        bool TryParseScopeDirective(string line, out DirectiveScopeWindow? window);
        bool TryParsePassageContextPrompt(string line, out int startQ, out int endQ, out string contextTitle);
        ICognitiveScopeManager CreateScopeManager();
        bool TryExtractInlineAnswer(string line, string questionType, QuestionParsingRuleDto? matchedRule, out string extractedAnswer);
        bool TryExtractInlineAnswerStructured(string line, string questionType, QuestionParsingRuleDto? matchedRule, string? language, out InlineAnswerResult result);
        bool TryMatchExplanationStart(string line, QuestionParsingRuleDto? matchedRule, out string cleanRemainder);
        bool TryMatchSubItemMarker(string line, QuestionParsingRuleDto? matchedRule, out string subKey, out string content, out string? inlineAnswer);
        bool TryExtractQuestionNumber(string line, QuestionParsingRuleDto? matchedRule, out int questionNumber);
        List<(char Letter, string Content, bool IsMarkedCorrect)> ExtractOptions(string line, QuestionParsingRuleDto? matchedRule = null);
        /// <summary>Trích xuất danh sách phương án trắc nghiệm dạng chữ hoa (A, B, C, D) từ dòng.</summary>
        List<ParsedOptionDto> ExtractUppercaseOptionsFromLine(string line, QuestionParsingRuleDto? rule = null);
        /// <summary>Trích xuất phương án trắc nghiệm từ một dòng văn bản.</summary>
        List<ParsedOptionDto> ExtractOptionsFromLine(string line, QuestionParsingRuleDto? rule = null);
        /// <summary>Trích xuất phương án trắc nghiệm từ dưới lên (Bottom-Up) kẹp giữa câu hỏi.</summary>
        bool TryExtractOptionsBottomUp(List<string> lines, string currentSection, QuestionParsingRuleDto? rule, out List<ParsedOptionDto> options, out int optionStartIndex, out string questionType);
        /// <summary>Trích xuất phương án trắc nghiệm chữ hoa trên nhiều dòng liên tiếp.</summary>
        bool TryExtractUppercaseMultilineOptions(List<string> lines, QuestionParsingRuleDto? rule, out List<ParsedOptionDto> options, out int startIndex);
        /// <summary>Trích xuất phương án đúng/sai (a, b, c, d) trên nhiều dòng.</summary>
        bool TryExtractTrueFalseOptions(List<string> lines, out List<ParsedOptionDto> options, out int startIndex);
        /// <summary>Phân giải khối câu hỏi và các phương án từ dưới lên (Bottom-Up Resolution).</summary>
        void ResolveQuestionBlockBottomUp(DocxImportPreviewDto currentQuestion, List<string> rawLines, string currentSection, QuestionParsingRuleDto? matchedRule);
        /// <summary>Gắn dòng giải thích vào câu hỏi đang xử lý.</summary>
        void AppendExplanationLine(DocxImportPreviewDto q, string line, QuestionParsingRuleDto? rule = null);
        /// <summary>Cập nhật đáp án đúng sai cho từng ý con.</summary>
        void UpdateTrueFalseAnswer(DocxImportPreviewDto q, string itemLetter, string val);
        /// <summary>Gắn lời giải chi tiết cho từng ý con.</summary>
        void AppendSubItemExplanation(DocxImportPreviewDto q, string subKey, string text);
        /// <summary>Thử trích xuất đáp án từ dòng văn bản.</summary>
        void TryExtractAnswer(DocxImportPreviewDto q, string line, QuestionParsingRuleDto? rule = null);
        /// <summary>Chuẩn hóa khóa phân đoạn (SINGLE, TRUE_FALSE, SHORT_ANSWER, ESSAY...).</summary>
        string NormalizeSectionKey(string? rawSec, string? questionType = null);
        /// <summary>Đóng gói và làm giàu câu hỏi hoàn chỉnh (Tự luận, Trắc nghiệm, Độ khó, Mã băm, Tọa độ).</summary>
        void FinalizeQuestion(DocxImportPreviewDto q, DynamicDomainContext? ctx = null, QuestionParsingRuleDto? rule = null);
        /// <summary>Đóng gói và hợp nhất câu hỏi vào bộ nhớ đệm đề thi (tránh trùng lặp / merge lời giải chi tiết).</summary>
        void FinalizeAndMergeQuestion(List<DocxImportPreviewDto> results, Dictionary<string, DocxImportPreviewDto> questionMap, DocxImportPreviewDto q, string examCode, int qNum, DynamicDomainContext? ctx = null, QuestionParsingRuleDto? rule = null);
        /// <summary>Tra cứu câu hỏi 3 tầng theo phân cấp (Map -> Danh sách -> Fallback an toàn).</summary>
        DocxImportPreviewDto? FindQuestionByHierarchy(Dictionary<string, DocxImportPreviewDto> questionMap, List<DocxImportPreviewDto> results, string examCode, string section, int qNum);
        /// <summary>Danh sách phân đoạn tương đương khi tra cứu.</summary>
        List<string> GetEquivalentSections(string section);
        string? DetectSubCategory(string text, QuestionParsingRuleDto? matchedRule = null);
        bool IsExplanationLine(string line, QuestionParsingRuleDto? matchedRule = null);
        bool IsAnswerLine(string line, QuestionParsingRuleDto? matchedRule, out string answerText);
        /// <summary>Nhận diện ranh giới kết thúc đề thi (HẾT, THE END...).</summary>
        bool TryMatchEndOfExam(string line);
        /// <summary>Nhận diện dòng chỉ thị coi thi / ghi chú phụ ngoài nội dung câu hỏi.</summary>
        bool IsProctorDisclaimerLine(string line);
        /// <summary>Dạng câu hỏi có năng lực (FREE_TEXT, CHOICE, TRUE_FALSE_FAMILY, NUMERIC_ANSWER...) theo hồ sơ cấu hình.</summary>
        bool HasTypeCapability(string? questionType, string capability);
        /// <summary>Các dạng tương đương khi tra cứu ĐỀ_PHẦN_CÂU theo hồ sơ cấu hình.</summary>
        IReadOnlyList<string> GetEquivalentTypes(string? questionType);
        /// <summary>Độ khó mặc định theo hồ sơ dạng câu hỏi.</summary>
        int GetDefaultDifficulty(string? questionType);
        /// <summary>Quy tên phần (đa ngôn ngữ) về khóa phân đoạn chuẩn; null nếu không nhận diện.</summary>
        string? TryResolveSectionKeyByAlias(string? rawSection);
        /// <summary>Làm phẳng danh sách đoạn văn theo ranh giới cấu trúc khách quan (ảnh, lookahead câu hỏi).</summary>
        List<string> FlattenDocument(IEnumerable<string> paragraphs, DocumentLanguageProfileDto? languageProfile = null);
        /// <summary>Nhận diện hồ sơ đầu tài liệu (thời lượng, mã đề, môn học, tọa độ tri thức).</summary>
        DocumentHeaderProfileDto ProfileHeader(IEnumerable<string> headerParagraphs, IEnumerable<string>? sampleContents = null, DynamicDomainContext? dynamicContext = null);
        /// <summary>Kiểm tra dòng thông tin hành chính / thí sinh / giám thị trước câu hỏi đầu tiên.</summary>
        bool IsStudentInfoLine(string line);
        /// <summary>Trích xuất mã đề thi từ dòng văn bản.</summary>
        bool TryExtractExamCode(string line, out string? examCode);
        /// <summary>Trích xuất thời lượng thi (phút) từ dòng văn bản.</summary>
        int? TryExtractDurationMinutes(string line);
        /// <summary>Kiểm tra số đề có phải là năm học (1900-2099) để tránh nhầm mã đề.</summary>
        bool IsYearLikeNumber(int examNum);
        /// <summary>Làm sạch tiêu đề môn học (loại bỏ từ khóa rác: đề, chính thức, thử, năm...).</summary>
        string SanitizeSubjectHeading(string rawSubject);
        /// <summary>Xây dựng mã đề chuẩn hóa theo tiền tố của ngữ cảnh miền (VD: DE_01, EXAM_01...).</summary>
        string BuildExamCode(int examNum, DynamicDomainContext? dynamicContext = null);
        /// <summary>Tạo tiêu đề phạm vi ngữ cảnh / bài đọc hiểu theo dải câu hỏi.</summary>
        string BuildContextTitle(int startQ, int endQ);
        /// <summary>Khớp mẫu ngôn ngữ học đa ngôn ngữ theo khóa (LinguisticKeys).</summary>
        bool TryMatchLinguisticPattern(string patternKey, string line, out System.Text.RegularExpressions.Match match);
        ParseSnippetResponseDto ParseSnippet(string snippetText, string? specificRuleCode = null);
        bool HotReloadRules(string jsonConfig);
        void LoadDefaultRules();
        void AddOrUpdateRule(QuestionParsingRuleDto rule, bool persistToFile = true);
        bool RemoveRule(string ruleCode, bool persistToFile = true);
        DocumentStructureDictionaryDto GetStructureDictionary();
        void UpdateStructureDictionary(DocumentStructureDictionaryDto dictionary, bool persistToFile = true);
        Task<int> SyncRulesFromDatabaseAsync(CancellationToken ct = default);
        Task<int> ResetAndReseedRulesAsync(CancellationToken ct = default);
        Task<bool> SaveRuleToDatabaseAsync(QuestionParsingRuleDto rule, Guid? tenantId = null, CancellationToken ct = default);
    }

    /// <summary>
    /// [CIG v4.0] Kết quả trích xuất đáp án nằm trong lời giải có cấu trúc, hỗ trợ đa ngôn ngữ và phân cấp Ý con
    /// </summary>
    public class InlineAnswerResult
    {
        public bool Success { get; set; }
        public string AnswerValue { get; set; } = string.Empty;
        public string? SubItemKey { get; set; }       // "a", "b", "c", "d"
        public string? SubItemValue { get; set; }     // "Đúng", "Sai"
    }
}
