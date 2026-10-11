using System;
using System.Collections.Generic;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Symposium Architecture] Bộ soạn thảo Nghị quyết Hàn Lâm và Kỷ yếu Living Academic Document.
    /// Chịu trách nhiệm render văn bản nghị quyết thích ứng theo TeleologyMode và Markdown xuất sang Notebook Hub.
    /// </summary>
    public interface ISymposiumResolutionComposer
    {
        /// <summary>
        /// Soạn thảo Tuyên ngôn Nghị quyết kết thúc phiên hội thảo (hỗ trợ phân biệt Vòng trung gian vs Vòng Chung Cuộc).
        /// </summary>
        string ComposeFinalDeclaration(
            Guid sessionId,
            string topicTitle,
            SymposiumTeleologyMode mode,
            List<string> citedEvidenceIds,
            SymposiumRubricScore rubric,
            SymposiumStructuredOutcomeDto structuredOutcome,
            List<UniversalAiCouncil> participating,
            bool hasRealTurns,
            int roundNumber = 1,
            bool isFinalRound = false,
            string? socraticDirectives = null);

        /// <summary>
        /// Soạn thảo Kỷ yếu Markdown chuẩn Living Academic Document để xuất sang Notebook Hub.
        /// </summary>
        string ComposeNotebookMarkdown(
            SymposiumSession session, 
            SymposiumRubricScore? rubric,
            int roundNumber = 1,
            bool isFinalRound = false,
            string? socraticDirectives = null,
            string? additionalDocuments = null);
    }
}
