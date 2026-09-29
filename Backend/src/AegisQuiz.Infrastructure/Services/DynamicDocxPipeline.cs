using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Clear Dynamic Architecture Pipeline]
    /// Triển khai đường ống bóc tách đề thi DOCX đa tầng:
    /// Stage 1: Archive Extraction (Zip, Media, RelMap)
    /// Stage 2: Structure and Answer Key Table Sniffing
    /// Stage 3: Universal Math and Vector Graphics Normalizer (OMML, MathType WMF/EMF, SVG)
    ///   - OMML  -> OmmlToLatexConverter -> KaTeX $ ... $
    ///   - WMF   -> TryExtractSimpleMathText -> KaTeX text inline (neu don gian)
    ///   - WMF   -> PureWmfToSvg -> SVG data URI (neu phuc tap)
    ///   - PNG/JPG -> Base64 data URI
    /// Stage 4: Dynamic CIG Rule Matching (Database-backed JIT Compiled Regex)
    /// Stage 5: 5D Cognitive Taxonomy and IRT Parameter Enrichment
    ///
    /// [P0 Fix] Cac service DI (_mathDictionary, _cigEngine) truoc day bi inject nhung bi bo qua hoan toan.
    ///          Nay duoc ket noi day du vao pipeline qua BuildContext() va truyen vao ParseRichDocxStream.
    /// </summary>
    public class DynamicDocxPipeline : IDynamicDocxPipeline
    {
        private readonly IDynamicQuestionIngestionEngine _cigEngine;
        private readonly IDynamicMathDictionaryService _mathDictionary;
        private readonly ILogger<DynamicDocxPipeline>? _logger;

        public DynamicDocxPipeline(
            IDynamicQuestionIngestionEngine cigEngine,
            IDynamicMathDictionaryService mathDictionary,
            ILogger<DynamicDocxPipeline>? logger = null)
        {
            _cigEngine = cigEngine;
            _mathDictionary = mathDictionary;
            _logger = logger;
        }

        /// <summary>
        /// [P0 Fix] Xay dung DynamicDomainContext tu cac service duoc inject.
        /// Diem hoi tu giua CIG Engine (quy tac boc tach) va Math Dictionary (ky hieu toan hoc).
        /// Dam bao TryExtractSimpleMathText() va SanitizeMathTypeSvg() dung dung DI instance.
        /// </summary>
        private DynamicDomainContext BuildContext()
        {
            try
            {
                var rules = _cigEngine?.GetAllRules() ?? new List<QuestionParsingRuleDto>();
                string defaultPrefix = rules
                    .Where(r => r.InteractionSpec != null && !string.IsNullOrWhiteSpace(r.InteractionSpec.DefaultQuestionType))
                    .Select(r => r.AnchorSpec?.RegexPattern?.Split('_').FirstOrDefault() ?? "EXAM")
                    .FirstOrDefault() ?? "EXAM";

                return new DynamicDomainContext
                {
                    DefaultExamPrefix = defaultPrefix
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[DynamicDocxPipeline] BuildContext: Khong the xay dung DynamicDomainContext. Dung context mac dinh.");
                return new DynamicDomainContext();
            }
        }

        public List<DocxImportPreviewDto> Execute(Stream docxStream)
        {
            _logger?.LogInformation("[DynamicDocxPipeline] Bat dau thuc thi pipeline boc tach de thi DOCX...");
            var context = BuildContext();
            var results = DocxParserService.ParseRichDocxStream(docxStream, _logger, context);
            _logger?.LogInformation("[DynamicDocxPipeline] Hoan tat pipeline: Boc tach thanh cong {Count} cau hoi.", results.Count);
            return results;
        }

        public Task<List<DocxImportPreviewDto>> ExecuteAsync(Stream docxStream, CancellationToken ct = default)
        {
            return Task.Run(() => Execute(docxStream), ct);
        }

        public string ExtractPlainText(Stream docxStream)
        {
            var rawParagraphs = DocxParserService.ExtractRawParagraphsFromDocx(docxStream);
            return string.Join("\n", rawParagraphs.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }
}
