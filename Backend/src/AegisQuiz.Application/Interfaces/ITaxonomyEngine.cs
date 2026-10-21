using System.Collections.Generic;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Universal Cognitive Coordinate & Taxonomy Engine Interface]
    /// Giao diện chuẩn cho động cơ phân loại tọa độ nhận thức và taxonomy theo Clean Architecture & OCP.
    /// Cho phép tiêm phụ thuộc (DI), mở rộng linh hoạt theo từng Tenant và triệt tiêu hard-code.
    /// </summary>
    public interface ITaxonomyEngine
    {
        CognitiveCoordinateTaxonomyResult SniffCoordinates(
            string? fileName,
            string? sheetName,
            IEnumerable<string>? headerBannerLines,
            IEnumerable<string>? sampleContents = null,
            IEnumerable<string>? sampleReferences = null,
            DynamicDomainContext? dynamicContext = null);

        void AutoEnrichTaxonomyAndTags(
            QuestionBase question,
            DynamicDomainContext? dynamicContext = null);

        void AutoEnrichTaxonomyAndTags(
            DocxImportPreviewDto question,
            DynamicDomainContext? dynamicContext = null);

        void EnrichExamTaxonomy(
            UniversalImportPreviewResult result,
            DynamicDomainContext? dynamicContext = null);

        void EnrichExamTaxonomy(
            List<DocxImportPreviewDto> results,
            DynamicDomainContext? dynamicContext = null);

        string ResolveTopicCode(
            string? rawTopicOrSubject,
            DynamicDomainContext? dynamicContext = null);

        string InferTopicFromContent(
            string? text,
            DynamicDomainContext? dynamicContext = null);

        bool IsAcademicTopic(
            string? topicCode,
            DynamicDomainContext? dynamicContext = null);
    }
}
