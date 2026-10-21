using System;
using System.IO;
using System.Linq;
using AegisQuiz.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace AegisQuiz.UnitTests
{
    public class RealDatasetDeThiBenchmarkTests
    {
        private readonly ITestOutputHelper _output;
        private const string DatasetRoot = @"D:\Cuong\DuAn\DETHI";

        public RealDatasetDeThiBenchmarkTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Benchmark_Lop7_TinHoc_ShouldParseQuestionsAndAnswers()
        {
            var filePath = Path.Combine(DatasetRoot, "LOP7", "thuvienhoclieu.com-De-kiem-tra-cuoi-ky-1-Tin-7-nam-25-26.docx");
            Assert.True(File.Exists(filePath), $"File not found: {filePath}");

            using var fs = File.OpenRead(filePath);
            var results = DocxParserService.ParseRichDocxStream(fs);

            _output.WriteLine($"[LOP 7 TIN HOC] Bóc tách thành công: {results.Count} câu hỏi.");
            Assert.NotEmpty(results);

            int answeredCount = results.Count(q => !string.IsNullOrEmpty(q.SuggestedAnswer));
            _output.WriteLine($"  - Số câu có đáp án: {answeredCount}/{results.Count}");

            foreach (var q in results.Take(3))
            {
                _output.WriteLine($"  - [{q.QuestionType}] Câu: {q.Content.Substring(0, Math.Min(50, q.Content.Length))}... (Options: {q.Options.Count}, Ans: {q.SuggestedAnswer})");
            }
        }

        [Fact]
        public void Benchmark_TNPT_English2026_ShouldParseReadingPassagesAndQuestions()
        {
            var filePath = Path.Combine(DatasetRoot, "TNPT", "thuvienhoclieu.com-De-thi-TN-THPT-2026-Tieng-Anh-Lan-2-Bo-GD-ma-de-1102.docx");
            Assert.True(File.Exists(filePath), $"File not found: {filePath}");

            using var fs = File.OpenRead(filePath);
            var results = DocxParserService.ParseRichDocxStream(fs);

            _output.WriteLine($"[TNPT 2026 TIENG ANH] Bóc tách thành công: {results.Count} câu hỏi.");
            Assert.NotEmpty(results);

            int answeredCount = results.Count(q => !string.IsNullOrEmpty(q.SuggestedAnswer));
            _output.WriteLine($"  - Số câu có đáp án: {answeredCount}/{results.Count}");

            foreach (var q in results.Take(3))
            {
                _output.WriteLine($"  - [{q.TopicCode}] Câu: {q.Content.Substring(0, Math.Min(60, q.Content.Length))}... Ans: {q.SuggestedAnswer}");
            }
        }

        [Fact]
        public void Benchmark_TuyenSinh10_Toan_ThaiNguyen_ShouldParseMathEssay()
        {
            var filePath = Path.Combine(DatasetRoot, "TUYENSINH10", "thuvienhoclieu.com-De-thi-thu-mon-Toan-vao-10-THPT-Ngo-Quyen-Thai-Nguyen-2026-2027.docx");
            if (!File.Exists(filePath)) return;

            using var fs = File.OpenRead(filePath);
            var results = DocxParserService.ParseRichDocxStream(fs);

            _output.WriteLine($"[TUYEN SINH 10 TOAN] Bóc tách thành công: {results.Count} câu hỏi/bài toán.");
            Assert.NotEmpty(results);

            foreach (var q in results.Take(4))
            {
                _output.WriteLine($"  - [{q.QuestionType}] {q.Content.Substring(0, Math.Min(60, q.Content.Length))}... (HasAnswer: {!string.IsNullOrEmpty(q.SuggestedAnswer)})");
            }
        }

        [Fact]
        public void Benchmark_MathFormula_ConversionRate()
        {
            var allDocxFiles = Directory.GetFiles(DatasetRoot, "*.docx", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).StartsWith("~$"))
                .Take(15) // Lấy mẫu đại diện 15 file lớn nhất
                .OrderByDescending(f => new FileInfo(f).Length)
                .ToList();

            int globalKatex = 0;
            int globalSvg = 0;
            var svgFiles = new List<(string FileName, int Katex, int Svg)>();

            foreach (var filePath in allDocxFiles)
            {
                var fileName = Path.GetFileName(filePath);
                try
                {
                    using var fs = File.OpenRead(filePath);
                    var results = DocxParserService.ParseRichDocxStream(fs);

                    int fileKatex = 0;
                    int fileSvg = 0;

                    foreach (var q in results)
                    {
                        fileKatex += System.Text.RegularExpressions.Regex.Matches(q.Content, @"\$[^\$]+\$").Count;
                        fileSvg += System.Text.RegularExpressions.Regex.Matches(q.Content, @"<img\s+src=""data:image/svg\+xml").Count;

                        foreach (var opt in q.Options)
                        {
                            fileKatex += System.Text.RegularExpressions.Regex.Matches(opt, @"\$[^\$]+\$").Count;
                            fileSvg += System.Text.RegularExpressions.Regex.Matches(opt, @"<img\s+src=""data:image/svg\+xml").Count;
                        }
                    }

                    globalKatex += fileKatex;
                    globalSvg += fileSvg;
                    if (fileSvg > 0)
                    {
                        svgFiles.Add((fileName, fileKatex, fileSvg));
                    }
                    int totalFile = fileKatex + fileSvg;
                    double rate = totalFile > 0 ? (fileKatex * 100.0 / totalFile) : 100.0;
                    _output.WriteLine($"[FILE: {fileName}] -> KaTeX: {fileKatex}, SVG: {fileSvg} ({rate:F1}% KaTeX)");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"[FILE ERROR: {fileName}] -> {ex.Message}");
                }
            }

            int globalTotal = globalKatex + globalSvg;
            double globalRate = globalTotal > 0 ? (globalKatex * 100.0 / globalTotal) : 0;

            _output.WriteLine($"==================================================");
            _output.WriteLine($"[GLOBAL MATH CONVERSION BENCHMARK (15 FILE LỚN NHẤT)]");
            _output.WriteLine($"  - Tổng số công thức: {globalTotal}");
            _output.WriteLine($"  - KaTeX ($...$): {globalKatex} ({globalRate:F1}%)");
            _output.WriteLine($"  - SVG Fallback (<img>): {globalSvg} ({100.0 - globalRate:F1}%)");
            _output.WriteLine($"  - Số file có SVG fallback: {svgFiles.Count}/{allDocxFiles.Count}");
            foreach (var sf in svgFiles)
            {
                _output.WriteLine($"     + {sf.FileName}: {sf.Svg} SVG, {sf.Katex} KaTeX");
            }
        }
    }
}
