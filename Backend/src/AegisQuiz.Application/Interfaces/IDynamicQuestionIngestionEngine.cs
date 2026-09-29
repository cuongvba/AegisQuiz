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
        bool TryMatchExamHeading(string line, out string examCode);
        bool TryMatchTopicHeading(string line, out string topicName);
        bool TryMatchSectionHeader(string line, out string sectionKey);
        bool TryMatchSolutionPhase(string line);
        bool TryMatchAnswerTablePhase(string line);
        bool TryMatchReadingPassage(string line, out int startQ, out int endQ);
        bool TryExtractInlineAnswer(string line, string questionType, QuestionParsingRuleDto? matchedRule, out string extractedAnswer);
        List<(char Letter, string Content, bool IsMarkedCorrect)> ExtractOptions(string line, QuestionParsingRuleDto? matchedRule = null);
        string? DetectSubCategory(string text, QuestionParsingRuleDto? matchedRule = null);
        bool IsExplanationLine(string line, QuestionParsingRuleDto? matchedRule = null);
        bool IsAnswerLine(string line, QuestionParsingRuleDto? matchedRule, out string answerText);
        ParseSnippetResponseDto ParseSnippet(string snippetText, string? specificRuleCode = null);
        bool HotReloadRules(string jsonConfig);
        void LoadDefaultRules();
        void AddOrUpdateRule(QuestionParsingRuleDto rule, bool persistToFile = true);
        Task<int> SyncRulesFromDatabaseAsync(CancellationToken ct = default);
        Task<bool> SaveRuleToDatabaseAsync(QuestionParsingRuleDto rule, Guid? tenantId = null, CancellationToken ct = default);
    }
}
