using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Services.Docx;
using Microsoft.Extensions.Logging;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [DOCX Ingestion Orchestrator] Lớp mỏng điều phối bóc tách đề thi Word:
    ///   1. Mở gói OOXML, nạp quan hệ (rels) và media (WMF/EMF/PNG...) qua <see cref="DocxRelationshipReader"/>.
    ///   2. Duyệt body → chuỗi đoạn văn thuần (công thức OMML → LaTeX, MathType WMF → text/SVG, ảnh → data URI).
    ///   3. Bảng đáp án ma trận → <see cref="DocxTableQuestionExtractor"/>.
    ///   4. Máy trạng thái phân loại câu hỏi → <see cref="DocxStateMachineClassifier"/> (điều khiển bởi CIG Engine).
    /// Mọi quy tắc nhận diện đều nằm ở dữ liệu (parsing_rules_registry.json) — lớp này không chứa ngữ pháp đề thi.
    /// </summary>
    public class DocxParserService : IDocxParserService
    {
        private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private static readonly XNamespace M = "http://schemas.openxmlformats.org/officeDocument/2006/math";

        private static readonly HashSet<string> MediaContainerNames = new(StringComparer.Ordinal) { "drawing", "object", "pict" };
        private static readonly HashSet<string> MediaLeafNames = new(StringComparer.Ordinal) { "drawing", "object", "pict", "imagedata", "blip" };
        private static readonly HashSet<string> MediaAncestorNames = new(StringComparer.Ordinal) { "drawing", "object", "pict", "shape" };
        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp", ".emf", ".wmf", ".svg" };

        private static readonly Regex MatrixSpecTablePattern = new(@"(?i)(?:ma\s+trận\s+đặc\s+tả|khung\s+ma\s+trận|mức\s+độ\s+đánh\s+giá)", RegexOptions.Compiled);
        private static readonly Regex QuestionLikeCellPattern = new(@"^(?:Câu|Question|Item|Task|Bài|Bai)\s*\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ExamLikeCellPattern = new(@"^(?:ĐỀ|D[E|Ê])\s*(?:SỐ\s*|THI\s*|ÔN\s*THI\s*)?(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SectionLikeCellPattern = new(@"\b(?:PHẦN|PHAN)\s+[I|V|X]+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex OptionLikeCellPattern = new(@"^\s*[\(\[]?[A-Da-d][\.\:\)\s\]]\s*\S", RegexOptions.Compiled);
        private static readonly Regex RelationshipIdPattern = new(@"(?:[a-zA-Z0-9_\-]+:)?(?:id|embed)=""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ILogger<DocxParserService>? _logger;
        private readonly DynamicDomainContext? _dynamicContext;

        public DocxParserService(ILogger<DocxParserService>? logger = null, DynamicDomainContext? dynamicContext = null)
        {
            _logger = logger;
            _dynamicContext = dynamicContext;
        }

        public DocxParserService(DynamicDomainContext? dynamicContext) : this(null, dynamicContext)
        {
        }

        // ─────────────────────────────────────────────────────────────────────
        // IDocxParserService
        // ─────────────────────────────────────────────────────────────────────

        public List<DocxImportPreviewDto> ParseDocxFile(Stream fileStream)
            => ParseRichDocxStream(fileStream, _logger, _dynamicContext);

        public List<DocxImportPreviewDto> ParseDocxFile(Stream fileStream, DynamicDomainContext? dynamicContext)
            => ParseRichDocxStream(fileStream, _logger, dynamicContext ?? _dynamicContext);

        public string ExtractTextFromDocx(Stream fileStream)
        {
            var paragraphs = ExtractRawParagraphsFromDocx(fileStream);
            return string.Join("\n", paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Static pipeline entry points (dùng bởi DynamicDocxPipeline, Excel/PDF pipelines, Controllers, Tests)
        // ─────────────────────────────────────────────────────────────────────

        public static List<DocxImportPreviewDto> ParseParagraphs(IEnumerable<string> paragraphs, DynamicDomainContext? dynamicContext = null)
            => DocxStateMachineClassifier.ParseParagraphList(paragraphs.ToList(), null, dynamicContext);

        public static List<DocxImportPreviewDto> ParseParagraphList(
            List<string> paragraphs,
            Dictionary<string, Dictionary<string, Dictionary<int, string>>>? answerKeyMap = null,
            DynamicDomainContext? dynamicContext = null)
            => DocxStateMachineClassifier.ParseParagraphList(paragraphs, answerKeyMap, dynamicContext);

        /// <summary>Bóc tách đầy đủ một luồng .docx theo Pipeline v4.0 Early Reconnaissance (Trinh sát phần đầu & Nhận thức sớm).</summary>
        public static List<DocxImportPreviewDto> ParseRichDocxStream(Stream fileStream, ILogger? logger = null, DynamicDomainContext? dynamicContext = null)
        {
            var result = new List<DocxImportPreviewDto>();
            try
            {
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true);
                var relsMap = DocxRelationshipReader.LoadRelationships(archive);
                var mediaDict = DocxRelationshipReader.LoadMediaFiles(archive);

                var documentEntry = archive.GetEntry("word/document.xml");
                if (documentEntry == null)
                {
                    logger?.LogWarning("[DocxParserService] ParseRichDocxStream | word/document.xml=Missing -> Empty result");
                    return result;
                }

                using var documentStream = documentEntry.Open();
                var doc = XDocument.Load(documentStream);

                // [Pipeline v4.0 - Giai đoạn 2: Early Reconnaissance & Language Profile]
                // Quét nhanh khối Header và nhận thức ngôn ngữ/thể chế TRƯỚC KHI bóc tách bảng đáp án ma trận
                var headerTexts = FastScanDocumentHeader(doc, 35);
                var cig = DynamicQuestionIngestionEngine.Instance;
                var langProfile = cig.DetectDocumentLanguageProfile(headerTexts, dynamicContext?.Language ?? "vi");

                var effectiveContext = dynamicContext ?? DynamicDomainContext.CreateDefault();
                var sniffedHeader = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                    fileName: null,
                    sheetName: null,
                    headerBannerLines: headerTexts,
                    dynamicContext: effectiveContext);

                // Làm giàu DynamicDomainContext từ kết quả trinh sát phần đầu
                if (!string.IsNullOrWhiteSpace(sniffedHeader.TargetLevel) 
                    && !effectiveContext.CoordinatePresets.Any(p => p.CoordinateType == "TARGET_LEVEL" && p.PresetLabel == sniffedHeader.TargetLevel))
                {
                    effectiveContext.CoordinatePresets.Add(new TenantCoordinatePresetDto 
                    { 
                        CoordinateType = "TARGET_LEVEL", 
                        PresetCode = sniffedHeader.TargetLevel, 
                        PresetLabel = sniffedHeader.TargetLevel 
                    });
                }

                // [Pipeline v4.0 - Giai đoạn 3: Bóc tách Bảng Đáp án Ma trận có định hướng ngôn ngữ]
                var answerKeyMap = DocxTableQuestionExtractor.ExtractAnswerTables(doc, out var answerTableElements);

                // [Pipeline v4.0 - Giai đoạn 4: Duyệt Cây DOM & Chuẩn hóa Toán học Lai]
                var paragraphs = ExtractParagraphTokens(doc, relsMap, mediaDict, answerTableElements);

                // [Pipeline v4.0 - Giai đoạn 5 & 6: Cỗ máy trạng thái tất định kế thừa 27 siêu trường]
                return DocxStateMachineClassifier.ParseParagraphList(paragraphs, answerKeyMap, effectiveContext);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "[DocxParserService] ParseRichDocxStream | Error -> {Message}", ex.Message);
            }
            return result;
        }

        /// <summary>
        /// [Pipeline v4.0 - Early Reconnaissance]
        /// Quét nhanh văn bản thuần từ 35 nodes đầu tiên (gồm đoạn văn và bảng header 2 cột) để trinh sát siêu dữ liệu.
        /// </summary>
        public static List<string> FastScanDocumentHeader(XDocument doc, int maxParagraphs = 35)
        {
            var headers = new List<string>();
            var body = doc.Descendants(W + "body").FirstOrDefault();
            if (body == null) return headers;

            int count = 0;
            foreach (var el in body.Elements())
            {
                if (count >= maxParagraphs) break;

                if (el.Name == W + "p")
                {
                    var text = CellText(el);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        headers.Add(text);
                        count++;
                    }
                }
                else if (el.Name == W + "tbl")
                {
                    // Quét các ô trong bảng header (thường là bảng 2 cột: Cột 1 Sở/Trường, Cột 2 Kỳ thi/Năm học/Mã đề)
                    foreach (var p in el.Descendants(W + "p"))
                    {
                        var text = CellText(p);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            headers.Add(text);
                            count++;
                            if (count >= maxParagraphs) break;
                        }
                    }
                }
            }
            return headers;
        }

        public static List<string> ExtractRawParagraphsFromDocx(Stream docxStream)
            => DocxRelationshipReader.ExtractRawParagraphs(docxStream);

        public static void AutoEnrichTaxonomyAndTags(DocxImportPreviewDto q, DynamicDomainContext? ctx = null)
            => DocxStateMachineClassifier.AutoEnrichTaxonomyAndTags(q, ctx);

        // ─────────────────────────────────────────────────────────────────────
        // Body traversal: paragraphs, tables, content controls
        // ─────────────────────────────────────────────────────────────────────

        internal static List<string> ExtractParagraphTokens(XDocument doc, Dictionary<string, string> relsMap, Dictionary<string, byte[]> mediaDict, HashSet<XElement>? answerTableElements = null)
        {
            var paragraphs = new List<string>();
            var body = doc.Descendants(W + "body").FirstOrDefault();
            if (body == null) return paragraphs;

            foreach (var element in body.Elements())
            {
                Traverse(element);
            }
            return paragraphs;

            void Traverse(XElement el)
            {
                if (el.Name == W + "p")
                {
                    var text = ExtractParagraphText(el, relsMap, mediaDict);
                    if (!string.IsNullOrWhiteSpace(text)) paragraphs.Add(text);
                    return;
                }

                if (el.Name == W + "tbl")
                {
                    if (answerTableElements != null && answerTableElements.Contains(el)) return;
                    if (el.Descendants(W + "p").Any(p => MatrixSpecTablePattern.IsMatch(CellText(p)))) return; // bảng ma trận đặc tả — bỏ qua

                    // Bảng chứa câu hỏi / tiêu đề đề / phân đoạn / phương án A-D → duyệt từng ô như văn bản thường
                    bool containsQuestionStructure = el.Descendants(W + "p").Any(p =>
                    {
                        var cell = CellText(p);
                        return QuestionLikeCellPattern.IsMatch(cell) 
                            || ExamLikeCellPattern.IsMatch(cell) 
                            || SectionLikeCellPattern.IsMatch(cell)
                            || OptionLikeCellPattern.IsMatch(cell);
                    });

                    if (containsQuestionStructure)
                    {
                        foreach (var cellChild in el.Elements(W + "tr").SelectMany(tr => tr.Elements(W + "tc")).SelectMany(tc => tc.Elements()))
                        {
                            Traverse(cellChild);
                        }
                        return;
                    }

                    // Bảng dữ liệu thuần → Markdown để giữ nguyên ngữ nghĩa trong nội dung câu hỏi
                    var markdown = DocxTableQuestionExtractor.ConvertTableToMarkdown(el, p => ExtractParagraphText(p, relsMap, mediaDict));
                    if (!string.IsNullOrWhiteSpace(markdown)) paragraphs.Add(markdown);
                    return;
                }

                if (el.Name == W + "sdt")
                {
                    var content = el.Element(W + "sdtContent");
                    if (content == null) return;
                    foreach (var child in content.Elements())
                    {
                        Traverse(child);
                    }
                }
            }
        }

        private static string CellText(XElement paragraph)
            => string.Concat(paragraph.Descendants(W + "t").Select(t => t.Value)).Trim();

        // ─────────────────────────────────────────────────────────────────────
        // Paragraph → text (OMML, runs, sub/superscript, underline, media)
        // ─────────────────────────────────────────────────────────────────────

        private static string ExtractParagraphText(XElement p, Dictionary<string, string> relsMap, Dictionary<string, byte[]> mediaDict)
        {
            var sb = new StringBuilder();
            foreach (var child in p.Elements())
            {
                ProcessNode(child);
            }
            return NormalizeMathInlineText(sb.ToString().Trim());

            void ProcessNode(XElement elem)
            {
                if (elem.Name == M + "oMath" || elem.Name == M + "oMathPara")
                {
                    var latex = OmmlToLatexConverter.Convert(elem);
                    if (!string.IsNullOrEmpty(latex)) sb.Append(' ').Append(latex).Append(' ');
                    return;
                }

                if (MediaContainerNames.Contains(elem.Name.LocalName))
                {
                    ProcessDrawingOrMedia(elem);
                    return;
                }

                if (elem.Name == W + "r")
                {
                    ProcessRun(elem);
                    return;
                }

                foreach (var child in elem.Elements())
                {
                    ProcessNode(child);
                }
            }

            void ProcessRun(XElement r)
            {
                var rPr = r.Element(W + "rPr");
                bool underlined = rPr?.Element(W + "u") != null;
                var vertAlign = rPr?.Element(W + "vertAlign")?.Attribute(W + "val")?.Value?.ToLowerInvariant();

                var runText = new StringBuilder();
                foreach (var child in r.Elements())
                {
                    if (child.Name == W + "t") runText.Append(child.Value);
                    else if (child.Name == W + "tab") runText.Append("    ");
                    else if (child.Name == W + "br") runText.Append('\n');
                }

                var text = runText.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    if (vertAlign == "subscript") text = "$_{" + text + "}$";
                    else if (vertAlign == "superscript") text = "$^{" + text + "}$";
                    if (underlined) text = "<u>" + text + "</u>";
                    sb.Append(text);
                }

                // Media lồng trực tiếp trong run (không nằm trong container đã xử lý ở tổ tiên)
                var mediaLeaves = r.DescendantsAndSelf()
                    .Where(e => MediaLeafNames.Contains(e.Name.LocalName))
                    .Where(e => !e.Ancestors().Any(a => a != r && MediaAncestorNames.Contains(a.Name.LocalName)));
                foreach (var leaf in mediaLeaves)
                {
                    ProcessDrawingOrMedia(leaf);
                }
            }

            void ProcessDrawingOrMedia(XElement container)
            {
                var relationshipIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var el in container.DescendantsAndSelf())
                {
                    foreach (var attr in el.Attributes())
                    {
                        var local = attr.Name.LocalName;
                        if ((local.Equals("id", StringComparison.OrdinalIgnoreCase) || local.Equals("embed", StringComparison.OrdinalIgnoreCase) || local.Equals("href", StringComparison.OrdinalIgnoreCase))
                            && relsMap.ContainsKey(attr.Value))
                        {
                            relationshipIds.Add(attr.Value);
                        }
                    }
                }
                foreach (Match m in RelationshipIdPattern.Matches(container.ToString()))
                {
                    if (relsMap.ContainsKey(m.Groups[1].Value)) relationshipIds.Add(m.Groups[1].Value);
                }

                // Ưu tiên quan hệ trỏ tới file ảnh/công thức, chỉ lấy media đầu tiên hợp lệ của khối
                var ordered = relationshipIds.OrderBy(rId => relsMap.TryGetValue(rId, out var target) && ImageExtensions.Contains(Path.GetExtension(target)) ? 0 : 1);
                foreach (var rId in ordered)
                {
                    if (!relsMap.TryGetValue(rId, out var target)) continue;
                    var mediaKey = target.Replace("word/", "");
                    if (!mediaDict.TryGetValue(mediaKey, out var bytes)) continue;

                    if (Path.GetExtension(mediaKey).Equals(".wmf", StringComparison.OrdinalIgnoreCase)
                        && MathTypeWmfEngine.TryExtractSimpleMathText(bytes, out var inlineMathText))
                    {
                        sb.Append(' ').Append(inlineMathText).Append(' ');
                        break;
                    }

                    var dataUri = MathTypeWmfEngine.ConvertMediaToDataUri(mediaKey, bytes);
                    if (!string.IsNullOrEmpty(dataUri))
                    {
                        sb.Append(' ').Append(dataUri).Append(' ');
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Chuẩn hóa ký hiệu toán học inline sau khi ghép văn bản: từ điển ký tự động (Symbol/MT Extra → Unicode),
        /// dấu phẩy trên (A′ → A'), vector (AB⃗ → \overrightarrow{AB}), căn bậc hai Unicode (√2 → \sqrt{2}).
        /// </summary>
        public static string NormalizeMathInlineText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            try
            {
                text = DynamicMathDictionaryService.Instance.NormalizeInlineText(text);
            }
            catch
            {
                // Từ điển toán không sẵn sàng → giữ nguyên văn bản, các bước chuẩn hóa regex bên dưới vẫn chạy.
            }

            text = Regex.Replace(text, "(?<=[A-Z])[’´`′]", "'");
            text = Regex.Replace(text, @"([A-Z]{2})\u20D7", @"$\overrightarrow{$1}$");
            text = Regex.Replace(text, @"([a-zA-Z])\u20D7", @"$\vec{$1}$");
            text = Regex.Replace(text, @"(?:∠|\\angle\s*)([A-Z]{3})\b", @"$\widehat{$1}$");
            text = Regex.Replace(text, @"([A-Z]{3})\u0302", @"$\widehat{$1}$");
            text = Regex.Replace(text, @"(?<=\b[Gg]óc\s+)([A-Z]{3})\b(?!\$)", @"$\widehat{$1}$");
            text = Regex.Replace(text, @"([A-Z])\\prime\b", "$1'");
            text = Regex.Replace(text, @"([A-Z]{3,4})\s*\\cdot\s*([A-Z])", "$1.$2");
            text = Regex.Replace(text, @"(?<!\$)(?<!\\)√\s*(\d+|[a-zA-Z])(?!\$)", @"$\sqrt{$1}$");
            text = Regex.Replace(text, @"(?<!\$)(?<!\\)√\s*\(([^)]+)\)(?!\$)", @"$\sqrt{$1}$");
            return text;
        }
    }
}
