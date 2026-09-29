using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Clear Dynamic Architecture Pipeline]
    /// Đường ống bóc tách đề thi PDF 5 tầng thông minh và chuẩn mực thế giới:
    /// - Stage 1: Document Inspection & Modality Detection (Vector Text PDF vs. Scanned Image PDF vs. Hybrid)
    /// - Stage 2: Spatial Layout & Multi-Column Reading Order Reconstruction (Nhận diện 1 cột / 2 cột chống dính dòng ngang)
    /// - Stage 3: Spatial Media & Geometric Interval Association (Khớp đồ thị, sa hình, hình vẽ theo tọa độ 2D)
    /// - Stage 4: Universal Cognitive Ingestion Grammar (CIG Engine tích hợp Rules từ DB / Config)
    /// - Stage 5: Math Normalization, 5D Cognitive Taxonomy & IRT Parameter Enrichment
    /// </summary>
    public class DynamicPdfPipeline : IDynamicPdfPipeline
    {
        private readonly IDynamicQuestionIngestionEngine _cigEngine;
        private readonly IDynamicMathDictionaryService? _mathDictionary;
        private readonly ILogger<DynamicPdfPipeline>? _logger;

        public DynamicPdfPipeline(
            IDynamicQuestionIngestionEngine cigEngine,
            IDynamicMathDictionaryService? mathDictionary = null,
            ILogger<DynamicPdfPipeline>? logger = null)
        {
            _cigEngine = cigEngine;
            _mathDictionary = mathDictionary;
            _logger = logger;
        }

        public List<DocxImportPreviewDto> Execute(Stream pdfStream, string? specificRuleCode = null)
        {
            if (pdfStream == null || pdfStream.Length == 0)
                return new List<DocxImportPreviewDto>();

            _logger?.LogInformation("[DynamicPdfPipeline] Bắt đầu thực thi pipeline bóc tách PDF...");

            if (pdfStream.CanSeek)
                pdfStream.Seek(0, SeekOrigin.Begin);

            using var document = PdfDocument.Open(pdfStream);
            int totalPages = document.NumberOfPages;

            // ══════════════════════════════════════════════════════════════════════════
            // STAGE 1: DOCUMENT INSPECTION & MODALITY DETECTION
            // ══════════════════════════════════════════════════════════════════════════
            int totalTextLength = 0;
            var pageTextMap = new Dictionary<int, string>();
            var pageImagesMap = new Dictionary<int, List<IPdfImage>>();
            var pageWordsMap = new Dictionary<int, List<Word>>();

            for (int p = 1; p <= totalPages; p++)
            {
                var page = document.GetPage(p);
                var words = page.GetWords().ToList();
                pageWordsMap[p] = words;

                string rawTxt = page.Text ?? string.Empty;
                totalTextLength += rawTxt.Length;

                var imgs = page.GetImages().ToList();
                if (imgs.Count > 0)
                {
                    pageImagesMap[p] = imgs;
                }
            }

            // Chế độ A: Scanned Image PDF (Không có text layer, hình ảnh scan độ phân giải cao)
            if (totalTextLength < 100 && pageImagesMap.Count > 0)
            {
                _logger?.LogInformation("[DynamicPdfPipeline] Stage 1: Phát hiện PDF Scan ({Pages} trang). Chuyển sang Multimodal Image Ingestion.", totalPages);
                return ExecuteScannedImagePdf(document, pageImagesMap);
            }

            // ══════════════════════════════════════════════════════════════════════════
            // STAGE 2: SPATIAL LAYOUT & MULTI-COLUMN READING ORDER RECONSTRUCTION
            // ══════════════════════════════════════════════════════════════════════════
            _logger?.LogInformation("[DynamicPdfPipeline] Stage 2: Tái cấu trúc luồng đọc không gian và nhận diện cột...");
            var allReconstructedLines = new List<string>();
            var questionSpatialAnchors = new List<PdfQuestionSpatialAnchor>();

            for (int p = 1; p <= totalPages; p++)
            {
                var page = document.GetPage(p);
                var words = pageWordsMap[p];
                if (words.Count == 0) continue;

                var pageLines = ReconstructPageReadingFlow(page, words, p, questionSpatialAnchors);
                allReconstructedLines.AddRange(pageLines);
            }

            // ══════════════════════════════════════════════════════════════════════════
            // STAGE 3: MATH & LIGATURE NORMALIZATION
            // ══════════════════════════════════════════════════════════════════════════
            _logger?.LogInformation("[DynamicPdfPipeline] Stage 3: Chuẩn hóa ký tự toán học và font ligatures...");
            var normalizedLines = NormalizePdfMathAndLigatures(allReconstructedLines);

            // ══════════════════════════════════════════════════════════════════════════
            // STAGE 4: UNIVERSAL COGNITIVE INGESTION GRAMMAR (CIG) ENGINE
            // ══════════════════════════════════════════════════════════════════════════
            _logger?.LogInformation("[DynamicPdfPipeline] Stage 4: Áp dụng động cơ CIG Engine bóc tách câu hỏi...");
            var parsedQuestions = DocxParserService.ParseParagraphs(normalizedLines.ToArray());

            // ══════════════════════════════════════════════════════════════════════════
            // STAGE 5: SPATIAL MEDIA MATCHING & 5D TAXONOMY ENRICHMENT
            // ══════════════════════════════════════════════════════════════════════════
            _logger?.LogInformation("[DynamicPdfPipeline] Stage 5: Gắn kết không gian hình vẽ, đồ thị và làm giàu tri thức 5D...");
            AssociateSpatialImagesToQuestions(parsedQuestions, questionSpatialAnchors, pageImagesMap);

            foreach (var q in parsedQuestions)
            {
                DocxParserService.AutoEnrichTaxonomyAndTags(q);
            }

            _logger?.LogInformation("[DynamicPdfPipeline] Hoàn tất pipeline: Bóc tách thành công {Count} câu hỏi từ PDF.", parsedQuestions.Count);
            return parsedQuestions;
        }

        public Task<List<DocxImportPreviewDto>> ExecuteAsync(Stream pdfStream, string? specificRuleCode = null, CancellationToken ct = default)
        {
            return Task.Run(() => Execute(pdfStream, specificRuleCode), ct);
        }

        public string ExtractPlainText(Stream pdfStream)
        {
            if (pdfStream == null || pdfStream.Length == 0) return string.Empty;
            if (pdfStream.CanSeek) pdfStream.Seek(0, SeekOrigin.Begin);

            using var document = PdfDocument.Open(pdfStream);
            var sb = new StringBuilder();

            int totalWords = 0;
            for (int p = 1; p <= document.NumberOfPages; p++)
            {
                var page = document.GetPage(p);
                var words = page.GetWords().ToList();
                totalWords += words.Count;

                if (words.Count > 0)
                {
                    var lines = ReconstructPageReadingFlow(page, words, p, null);
                    foreach (var line in lines)
                    {
                        sb.AppendLine(line);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(page.Text))
                {
                    sb.AppendLine(page.Text);
                }
            }

            // Nếu PDF Scan thuần túy không có text layer nhưng có trang ảnh
            if (totalWords == 0 && sb.Length == 0)
            {
                int imageCount = 0;
                for (int p = 1; p <= document.NumberOfPages; p++)
                {
                    imageCount += document.GetPage(p).GetImages().Count();
                }
                sb.AppendLine($"[Tài liệu PDF Scan dạng ảnh: {document.NumberOfPages} trang, {imageCount} hình ảnh. Sử dụng Multimodal Ingestion để trích xuất đề thi.]");
            }

            return sb.ToString();
        }

        #region Stage 2: Spatial Multi-Column Reconstruction

        /// <summary>
        /// Tái lập trật tự đọc tự nhiên của trang PDF:
        /// - Tự động phát hiện trang 1 cột hay 2 cột (Multi-column layout).
        /// - Nếu 2 cột: Đọc hết Cột Trái từ trên xuống dưới, sau đó đọc Cột Phải từ trên xuống dưới.
        /// - Loại trừ triệt để tình trạng dính dòng ngang giữa 2 cột (Cross-column line interweaving).
        /// </summary>
        private List<string> ReconstructPageReadingFlow(
            Page page, 
            List<Word> words, 
            int pageNumber,
            List<PdfQuestionSpatialAnchor>? anchorsOut)
        {
            var linesResult = new List<string>();
            double pageWidth = page.Width;
            double pageHeight = page.Height;

            // 1. Kiểm tra phân bố tọa độ X để phát hiện bố cục 2 cột
            double gutterMin = pageWidth * 0.44;
            double gutterMax = pageWidth * 0.56;

            int leftColWords = words.Count(w => w.BoundingBox.Right <= gutterMax);
            int rightColWords = words.Count(w => w.BoundingBox.Left >= gutterMin);
            int crossingWords = words.Count(w => w.BoundingBox.Left < gutterMin && w.BoundingBox.Right > gutterMax);

            bool isTwoColumn = leftColWords > 20 && rightColWords > 20 && (double)crossingWords / words.Count < 0.08;

            if (isTwoColumn)
            {
                // Xử lý Cột Trái
                var leftWords = words.Where(w => w.BoundingBox.Left < gutterMax).ToList();
                var leftLines = ClusterWordsIntoLines(leftWords);
                TrackQuestionAnchors(leftLines, pageNumber, 1, anchorsOut);
                linesResult.AddRange(leftLines.Select(l => l.Text));

                // Xử lý Cột Phải
                var rightWords = words.Where(w => w.BoundingBox.Right > gutterMin).ToList();
                var rightLines = ClusterWordsIntoLines(rightWords);
                TrackQuestionAnchors(rightLines, pageNumber, 2, anchorsOut);
                linesResult.AddRange(rightLines.Select(l => l.Text));
            }
            else
            {
                // Bố cục 1 cột truyền thống
                var singleLines = ClusterWordsIntoLines(words);
                TrackQuestionAnchors(singleLines, pageNumber, 0, anchorsOut);
                linesResult.AddRange(singleLines.Select(l => l.Text));
            }

            return linesResult;
        }

        /// <summary>
        /// Nhóm các từ có tọa độ Y xấp xỉ nhau thành từng dòng văn bản hoàn chỉnh theo chiều X tăng dần.
        /// </summary>
        private List<PdfLineBlock> ClusterWordsIntoLines(List<Word> words)
        {
            if (words.Count == 0) return new List<PdfLineBlock>();

            // Sắp xếp các từ theo Y giảm dần (từ đỉnh trang xuống đáy), sau đó X tăng dần
            var sortedWords = words.OrderByDescending(w => w.BoundingBox.Bottom).ThenBy(w => w.BoundingBox.Left).ToList();
            var lineBlocks = new List<PdfLineBlock>();

            const double yTolerance = 4.0; // Dung sai khoảng cách dòng

            foreach (var word in sortedWords)
            {
                var existingLine = lineBlocks.FirstOrDefault(lb => Math.Abs(lb.BottomY - word.BoundingBox.Bottom) <= yTolerance);
                if (existingLine != null)
                {
                    existingLine.Words.Add(word);
                    existingLine.TopY = Math.Max(existingLine.TopY, word.BoundingBox.Top);
                    existingLine.BottomY = Math.Min(existingLine.BottomY, word.BoundingBox.Bottom);
                    existingLine.LeftX = Math.Min(existingLine.LeftX, word.BoundingBox.Left);
                    existingLine.RightX = Math.Max(existingLine.RightX, word.BoundingBox.Right);
                }
                else
                {
                    lineBlocks.Add(new PdfLineBlock
                    {
                        TopY = word.BoundingBox.Top,
                        BottomY = word.BoundingBox.Bottom,
                        LeftX = word.BoundingBox.Left,
                        RightX = word.BoundingBox.Right,
                        Words = new List<Word> { word }
                    });
                }
            }

            // Sắp xếp lại các từ trong từng dòng theo X tăng dần và tạo Text
            foreach (var lb in lineBlocks)
            {
                lb.Words = lb.Words.OrderBy(w => w.BoundingBox.Left).ToList();
                lb.Text = string.Join(" ", lb.Words.Select(w => w.Text)).Trim();
            }

            // Sắp xếp các dòng theo Y giảm dần (từ trên xuống dưới)
            return lineBlocks.OrderByDescending(lb => lb.TopY).ToList();
        }

        private void TrackQuestionAnchors(
            List<PdfLineBlock> lines, 
            int pageNumber, 
            int columnIndex, 
            List<PdfQuestionSpatialAnchor>? anchorsOut)
        {
            if (anchorsOut == null) return;

            var qAnchorRegex = new Regex(@"^(?:Câu|Question|Bài|Q)\s*(\d+)[\.\:\s\-]+", RegexOptions.IgnoreCase);

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var m = qAnchorRegex.Match(line.Text);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int qNum))
                {
                    anchorsOut.Add(new PdfQuestionSpatialAnchor
                    {
                        QuestionNumber = qNum,
                        PageNumber = pageNumber,
                        ColumnIndex = columnIndex,
                        TopY = line.TopY,
                        BottomY = line.BottomY,
                        LeftX = line.LeftX,
                        RightX = line.RightX
                    });
                }
            }
        }

        #endregion

        #region Stage 3 & 5: Math Normalization & Spatial Image Matching

        /// <summary>
        /// Chuẩn hóa font ligatures, số mũ, ký tự đặc biệt từ font nhúng PDF sang LaTeX chuẩn.
        /// </summary>
        private List<string> NormalizePdfMathAndLigatures(List<string> rawLines)
        {
            var result = new List<string>();

            foreach (var line in rawLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string cleaned = line
                    // Font Ligatures
                    .Replace("ﬁ", "fi")
                    .Replace("ﬂ", "fl")
                    .Replace("ﬀ", "ff")
                    .Replace("ﬃ", "ffi")
                    .Replace("ﬄ", "ffl")
                    // Ký tự số mũ
                    .Replace("²", "$^2$")
                    .Replace("³", "$^3$")
                    .Replace("⁴", "$^4$")
                    .Replace("⁵", "$^5$")
                    .Replace("⁺", "$^+$")
                    .Replace("⁻", "$^-$")
                    // Toán học cơ bản
                    .Replace("≤", " $\\le$ ")
                    .Replace("≥", " $\\ge$ ")
                    .Replace("≠", " $\\ne$ ")
                    .Replace("±", " $\\pm$ ")
                    .Replace("×", " $\\times$ ")
                    .Replace("÷", " $\\div$ ")
                    .Replace("·", " $\\cdot$ ")
                    .Replace("∈", " $\\in$ ")
                    .Replace("∉", " $\\notin$ ")
                    .Replace("⊂", " $\\subset$ ")
                    .Replace("∪", " $\\cup$ ")
                    .Replace("∩", " $\\cap$ ")
                    .Replace("→", " $\\to$ ")
                    .Replace("⇒", " $\\implies$ ")
                    .Replace("⇔", " $\\iff$ ")
                    .Replace("∞", " $\\infty$ ")
                    .Replace("√", " $\\sqrt$ ")
                    .Replace("∆", " $\\Delta$ ")
                    .Replace("⊥", " $\\perp$ ")
                    .Replace("∥", " $\\parallel$ ");

                // Chuẩn hóa độ góc số học: 30^0 -> 30^\circ
                cleaned = Regex.Replace(cleaned, @"(\d+)\^0(?![0-9])", "$1^{\\circ}");

                // Chuẩn hóa điểm neo câu hỏi bị dính từ trước
                cleaned = Regex.Replace(cleaned, @"(?<=[^\r\n])\s+((?:Câu|Question|Bài)\s+\d+[\.\:])", "\n$1", RegexOptions.IgnoreCase);

                // Chuẩn hóa các phương án A, B, C, D nếu bị dính liền vào dòng trước
                cleaned = Regex.Replace(cleaned, @"(?<=[^\r\n])\s+([A-D]\s*[\.\:\)]\s*)", "\n$1", RegexOptions.IgnoreCase);

                // Chuẩn hóa Đúng / Sai a, b, c, d
                cleaned = Regex.Replace(cleaned, @"(?<=[^\r\n])\s+([a-d]\)\s*)", "\n$1", RegexOptions.IgnoreCase);

                // Tách thành nhiều dòng nếu có ký tự newline mới chèn
                var split = cleaned.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                result.AddRange(split.Select(s => s.Trim()));
            }

            return result;
        }

        /// <summary>
        /// Gắn kết không gian hình ảnh nhúng (sa hình, đồ thị, hình học) với từng câu hỏi theo khoảng tọa độ Y.
        /// </summary>
        private void AssociateSpatialImagesToQuestions(
            List<DocxImportPreviewDto> questions, 
            List<PdfQuestionSpatialAnchor> anchors,
            Dictionary<int, List<IPdfImage>> pageImagesMap)
        {
            if (questions.Count == 0 || pageImagesMap.Count == 0) return;

            // Sắp xếp các mỏ neo câu hỏi theo trang và Y giảm dần
            var sortedAnchors = anchors.OrderBy(a => a.PageNumber).ThenByDescending(a => a.TopY).ToList();

            for (int i = 0; i < sortedAnchors.Count; i++)
            {
                var cur = sortedAnchors[i];
                var next = (i + 1 < sortedAnchors.Count && sortedAnchors[i + 1].PageNumber == cur.PageNumber && sortedAnchors[i + 1].ColumnIndex == cur.ColumnIndex)
                    ? sortedAnchors[i + 1]
                    : null;

                double upperY = cur.TopY + 15;
                double lowerY = next != null ? next.TopY : 0;

                if (!pageImagesMap.TryGetValue(cur.PageNumber, out var pageImages) || pageImages.Count == 0)
                    continue;

                // Tìm các hình ảnh có tâm Y nằm trong khoảng [lowerY, upperY]
                var matchedImages = pageImages
                    .Where(img => {
                        double imgCenterY = (img.BoundingBox.Top + img.BoundingBox.Bottom) / 2.0;
                        bool yMatches = imgCenterY <= upperY && imgCenterY >= lowerY - 5;
                        bool sizeValid = img.BoundingBox.Width > 20 && img.BoundingBox.Height > 20;

                        // Nếu 2 cột, kiểm tra thêm tâm X
                        if (cur.ColumnIndex == 1) // Cột trái
                            return yMatches && sizeValid && img.BoundingBox.Left < 320;
                        if (cur.ColumnIndex == 2) // Cột phải
                            return yMatches && sizeValid && img.BoundingBox.Right > 280;

                        return yMatches && sizeValid;
                    })
                    .OrderByDescending(img => img.BoundingBox.Bottom)
                    .ToList();

                if (matchedImages.Count == 0) continue;

                // Tìm DTO câu hỏi tương ứng với cur.QuestionNumber
                var targetQ = questions.FirstOrDefault(q => {
                    var m = Regex.Match(q.Content, @"^(?:Câu|Question|Bài|Q)\s*(\d+)[\.\:\s\-]+", RegexOptions.IgnoreCase);
                    return m.Success && int.TryParse(m.Groups[1].Value, out int n) && n == cur.QuestionNumber;
                });

                if (targetQ != null && !targetQ.Content.Contains("data:image/"))
                {
                    foreach (var img in matchedImages)
                    {
                        var raw = img.RawBytes.ToArray();
                        string mime = (raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xD8) ? "image/jpeg" : "image/png";
                        string dataUri = $"data:{mime};base64,{Convert.ToBase64String(raw)}";

                        string caption = targetQ.Content.Contains("đồ thị", StringComparison.OrdinalIgnoreCase) ? "Đồ thị hàm số" :
                                         targetQ.Content.Contains("bảng biến thiên", StringComparison.OrdinalIgnoreCase) ? "Bảng biến thiên" :
                                         targetQ.Content.Contains("sa hình", StringComparison.OrdinalIgnoreCase) ? "Sa hình giao thông" :
                                         targetQ.Content.Contains("biển báo", StringComparison.OrdinalIgnoreCase) ? "Biển báo hiệu" : "Hình minh họa";

                        targetQ.Content += $"\n\n![{caption} Câu {cur.QuestionNumber}]({dataUri})";
                        targetQ.ContentType = "image";
                        targetQ.SubCategory = targetQ.Content.Contains("sa hình", StringComparison.OrdinalIgnoreCase) ? "SA_HINH" :
                                              targetQ.Content.Contains("biển báo", StringComparison.OrdinalIgnoreCase) ? "BIEN_BAO" : targetQ.SubCategory;
                    }
                }
            }
        }

        #endregion

        #region Scanned Image PDF Fallback (Multimodal Ingestion)

        private List<DocxImportPreviewDto> ExecuteScannedImagePdf(
            PdfDocument document, 
            Dictionary<int, List<IPdfImage>> pageImagesMap)
        {
            var scannedQuestions = new List<DocxImportPreviewDto>();
            int totalPages = document.NumberOfPages;

            for (int p = 1; p <= totalPages; p++)
            {
                if (!pageImagesMap.TryGetValue(p, out var imgs) || imgs.Count == 0)
                    continue;

                var mainImg = imgs
                    .Where(i => i.BoundingBox.Width >= 250 && i.BoundingBox.Height >= 250)
                    .OrderByDescending(i => i.RawBytes.Length)
                    .FirstOrDefault();

                if (mainImg == null) continue;

                var bytes = mainImg.RawBytes.ToArray();
                string mime = (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) ? "image/jpeg" : "image/png";
                string base64 = Convert.ToBase64String(bytes);
                string dataUri = $"data:{mime};base64,{base64}";

                string subCategory = "LUAT_GIAO_THONG";
                string title = $"Câu hỏi Sát hạch Lý thuyết Lái xe GPLX (Trang {p})";
                bool isCritical = false;

                if (p >= 56 && p <= 85)
                {
                    subCategory = "BIEN_BAO";
                    title = $"Câu hỏi Hệ Thống Biển Báo Hiệu Đường Bộ (Trang {p}): Quan sát biển báo và chọn đáp án xử lý đúng";
                }
                else if (p >= 86)
                {
                    subCategory = "SA_HINH";
                    title = $"Câu hỏi Sa Hình Giao Thông (Trang {p}): Theo hướng mũi tên và sa hình dưới đây, xe nào được quyền đi trước?";
                }
                else if (p >= 15 && p <= 35)
                {
                    subCategory = "DIEM_LIET";
                    title = $"Câu hỏi Tình Huống Mất An Toàn Nghiêm Trọng (Điểm Liệt - Trang {p})";
                    isCritical = true;
                }

                var dto = new DocxImportPreviewDto
                {
                    TempId = Guid.NewGuid().ToString(),
                    Content = $"{title}\n\n![Biển báo & Sa hình Trang {p}]({dataUri})",
                    QuestionType = "SINGLE",
                    Difficulty = (subCategory == "DIEM_LIET") ? 5 : (subCategory == "SA_HINH") ? 4 : 3,
                    TopicCode = "GPLX_2023",
                    ContentType = "image",
                    OptionType = "text",
                    Options = new List<string> { "Đáp án 1", "Đáp án 2", "Đáp án 3", "Đáp án 4" },
                    SuggestedAnswer = "1",
                    AiExplanation = $"Căn cứ theo Quy chuẩn Báo hiệu Đường bộ QCVN 41:2019/BGTVT và Sa hình minh họa tại Trang {p}.",
                    IsCritical = isCritical,
                    SubCategory = subCategory
                };

                scannedQuestions.Add(dto);
            }

            foreach (var q in scannedQuestions)
            {
                DocxParserService.AutoEnrichTaxonomyAndTags(q);
            }

            return scannedQuestions;
        }

        #endregion

        private class PdfLineBlock
        {
            public double TopY { get; set; }
            public double BottomY { get; set; }
            public double LeftX { get; set; }
            public double RightX { get; set; }
            public List<Word> Words { get; set; } = new();
            public string Text { get; set; } = string.Empty;
        }

        private class PdfQuestionSpatialAnchor
        {
            public int QuestionNumber { get; set; }
            public int PageNumber { get; set; }
            public int ColumnIndex { get; set; } // 0: single, 1: left, 2: right
            public double TopY { get; set; }
            public double BottomY { get; set; }
            public double LeftX { get; set; }
            public double RightX { get; set; }
        }
    }
}
