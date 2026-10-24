using System;
using System.IO;
using System.Linq;
using AegisQuiz.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace AegisQuiz.UnitTests
{
    public class ThcsExamIngestionTests
    {
        private readonly ITestOutputHelper _output;
        private const string ThcsDir = @"D:\Cuong\DuAn\mybank\2026-TNPT\THCS";

        public ThcsExamIngestionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ParseEnglish7_GlobalSuccess_ShouldExtractQuestions()
        {
            var filePath = Path.Combine(ThcsDir, "thuvienhoclieu.com-De-on-tap-giua-HK2-Tieng-Anh-7-global-25-26-De-1.docx");
            if (!File.Exists(filePath))
            {
                _output.WriteLine("File not found, skipping.");
                return;
            }

            using var fs = File.OpenRead(filePath);
            var results = DocxParserService.ParseRichDocxStream(fs);

            _output.WriteLine($"[English 7] Bóc tách thành công: {results.Count} câu hỏi.");
            foreach (var q in results.Take(5))
            {
                _output.WriteLine($"  - [{q.TopicCode}] {q.Content.Substring(0, Math.Min(50, q.Content.Length))}... (Opts: {q.Options.Count}, Ans: {q.SuggestedAnswer})");
            }

            Assert.NotEmpty(results);
        }

        [Fact]
        public void ParseBo10DeTiengAnh7_ExamineDirectives()
        {
            var filePath = @"D:\Cuong\DuAn\DETHI\THCS\LOP7\Bo-10-de-on-tap-giua-ky-1-Tieng-Anh-7-nam-25-26-co-dap-an.docx";
            if (!File.Exists(filePath))
            {
                _output.WriteLine("File not found, skipping.");
                return;
            }

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var results = DocxParserService.ParseRichDocxStream(fs);

            Assert.NotEmpty(results);
            Assert.True(results.Count >= 250, $"Expected >= 250 questions, got {results.Count}");

            var q1 = results.FirstOrDefault(q => q.SourceExamName == "DE_01" && q.SourceQuestionNo == "1");
            Assert.NotNull(q1);
            Assert.Contains("pronounced differently", q1.SourceSectionName, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(q1.ContextContent));
            Assert.Equal("Chỉ dẫn làm bài", q1.ContextTitle);

            var q13 = results.FirstOrDefault(q => q.SourceExamName == "DE_01" && q.SourceQuestionNo == "13");
            Assert.NotNull(q13);
            Assert.Contains("Odd one out", q13.SourceSectionName, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(q13.ContextContent));

            int withContext = results.Count(q => !string.IsNullOrWhiteSpace(q.ContextContent));
            Assert.True(withContext >= 200, $"Expected >= 200 questions with context, got {withContext}");
        }

        [Fact]
        public void ParseMath10_Probability_ShouldExtractQuestionsAndFormulas()
        {
            var filePath = Path.Combine(ThcsDir, "thuvienhoclieu.com-50-cau-trac-nghiem-bai-27-Thuc-hanh-tinh-xac-suat-theo-dinh-nghia-co-dien-Lop-10.docx");
            if (!File.Exists(filePath))
            {
                _output.WriteLine("File not found, skipping.");
                return;
            }

            using var fs = File.OpenRead(filePath);
            var results = DocxParserService.ParseRichDocxStream(fs);

            _output.WriteLine($"[Probability 10] Bóc tách thành công: {results.Count} câu hỏi.");
            foreach (var q in results.Take(5))
            {
                _output.WriteLine($"  - [{q.QuestionType}] {q.Content.Substring(0, Math.Min(60, q.Content.Length))}... Ans: {q.SuggestedAnswer}");
            }

            Assert.NotEmpty(results);
        }

        [Fact]
        public void ParseMath7_ReviewOutline_ShouldExtractQuestions()
        {
            var filePath = Path.Combine(ThcsDir, "thuvienhoclieu.com-De-cuong-on-tap-cuoi-HK1-Toan-7-nam-25-26.docx");
            if (!File.Exists(filePath))
            {
                _output.WriteLine("File not found, skipping.");
                return;
            }

            using var fs = File.OpenRead(filePath);
            var results = DocxParserService.ParseRichDocxStream(fs);

            _output.WriteLine($"[Math 7 Outline] Bóc tách thành công: {results.Count} câu hỏi.");
            foreach (var q in results.Take(5))
            {
                _output.WriteLine($"  - [{q.TopicCode}] {q.Content.Substring(0, Math.Min(60, q.Content.Length))}... Ans: {q.SuggestedAnswer}");
            }

            Assert.NotEmpty(results);
        }

        [Fact]
        public void ParseAll11ThcsFiles_ShouldReportComprehensiveExtraction()
        {
            if (!Directory.Exists(ThcsDir)) return;

            var files = Directory.GetFiles(ThcsDir, "*.docx");
            _output.WriteLine($"=================================================================");
            _output.WriteLine($"🚀 BẮT ĐẦU KIỂM THỬ BÓC TÁCH TOÀN BỘ {files.Length} TỆP ĐỀ THI THCS THỰC TẾ");
            _output.WriteLine($"=================================================================");

            int totalQuestionsExtracted = 0;
            int totalFilesProcessed = 0;

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var sw = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    using var fs = File.OpenRead(file);
                    var questions = DocxParserService.ParseRichDocxStream(fs);
                    sw.Stop();

                    totalQuestionsExtracted += questions.Count;
                    totalFilesProcessed++;

                    int mcqCount = questions.Count(q => q.QuestionType == "SINGLE");
                    int tfCount = questions.Count(q => q.QuestionType == "TRUE_FALSE");
                    int saCount = questions.Count(q => q.QuestionType == "SHORT_ANSWER");
                    int withAnsCount = questions.Count(q => !string.IsNullOrWhiteSpace(q.SuggestedAnswer));

                    _output.WriteLine($"✅ [{fileName}] -> {questions.Count} câu hỏi ({sw.ElapsedMilliseconds} ms)");
                    _output.WriteLine($"     (MCQ: {mcqCount}, Đúng/Sai: {tfCount}, Điền khuyết: {saCount}, Có đáp án: {withAnsCount})");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"❌ [{fileName}] -> Lỗi: {ex.Message}");
                }
            }

            _output.WriteLine($"=================================================================");
            _output.WriteLine($"🎉 TỔNG KẾT: Đã bóc tách {totalQuestionsExtracted} câu hỏi từ {totalFilesProcessed} tệp đề thi THCS!");
            _output.WriteLine($"=================================================================");

            Assert.True(totalQuestionsExtracted > 100, "Phải bóc tách được ít nhất 100 câu hỏi từ kho đề THCS.");
        }

        [Fact]
        public void AnalyzeAllThcsFilesStructure_DetailedReport()
        {
            if (!Directory.Exists(ThcsDir)) return;

            var files = Directory.GetFiles(ThcsDir, "*.docx");
            var reportPath = @"C:\Users\cuongnguyenviet8.CORP\.gemini\antigravity-ide\brain\677ad04f-961a-48df-a9b4-186acbbbad8d\thcs_structural_analysis.md";
            using var writer = new StreamWriter(reportPath, false, System.Text.Encoding.UTF8);

            writer.WriteLine("# 📊 BÁO CÁO GIẢI PHẪU CẤU TRÚC ĐỀ THI, ĐÁP ÁN & HƯỚNG DẪN GIẢI 11 FILE THCS");
            writer.WriteLine();
            writer.WriteLine($"- Thời gian khảo sát: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"- Tổng số tệp DOCX: {files.Length}");
            writer.WriteLine();

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                writer.WriteLine($"## 📁 {fileName}");
                writer.WriteLine($"- Kích thước: {new FileInfo(file).Length / 1024 / 1024.0:F2} MB");

                using var fs = File.OpenRead(file);
                using var za = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read);
                var docEntry = za.GetEntry("word/document.xml");
                if (docEntry == null)
                {
                    writer.WriteLine("- Không tìm thấy word/document.xml");
                    continue;
                }

                using var sr = new StreamReader(docEntry.Open());
                var xmlStr = sr.ReadToEnd();
                var xdoc = System.Xml.Linq.XDocument.Parse(xmlStr);
                System.Xml.Linq.XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

                var tables = xdoc.Descendants(w + "tbl").ToList();
                var paragraphs = xdoc.Descendants(w + "p").ToList();

                writer.WriteLine($"- Tổng số đoạn văn (paragraphs): {paragraphs.Count}");
                writer.WriteLine($"- Tổng số bảng (tables): {tables.Count}");

                // Khảo sát các tiêu đề đề thi (ĐỀ SỐ...)
                var examTitles = paragraphs
                    .Select(p => string.Join("", p.Descendants(w + "t").Select(t => t.Value)).Trim())
                    .Where(t => System.Text.RegularExpressions.Regex.IsMatch(t, @"^(ĐỀ\s+(?:SỐ\s+)?(?:KIỂM\s+TRA\s+)?(?:ÔN\s+TẬP\s+)?\d+|ĐỀ\s+THI|ĐỀ\s+KIỂM\s+TRA|ĐỀ\s+[A-Z0-9]{3,})", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    .Distinct()
                    .Take(10)
                    .ToList();
                writer.WriteLine($"- Các phân đoạn Đề thi phát hiện: {(examTitles.Any() ? string.Join(", ", examTitles) : "Đề đơn lẻ hoặc không có tiêu đề Đề số")}");

                // Khảo sát bảng đáp án & hướng dẫn giải
                int answerTableCount = 0;
                int matrixTableCount = 0;
                int solutionTableCount = 0;

                writer.WriteLine("### 📑 Khảo sát các bảng tiêu biểu trong tệp:");
                int tableIdx = 0;
                foreach (var tbl in tables)
                {
                    tableIdx++;
                    var rows = tbl.Elements(w + "tr").ToList();
                    if (!rows.Any()) continue;

                    var rowTexts = rows.Select(r =>
                        string.Join(" | ", r.Elements(w + "tc").Select(c =>
                            string.Join("", c.Descendants(w + "t").Select(t => t.Value)).Trim()
                        ))
                    ).ToList();

                    var allTblText = string.Join(" ", rowTexts);
                    bool isAnswerGrid = false;
                    bool isSolution = false;
                    bool isMatrix = false;

                    if (System.Text.RegularExpressions.Regex.IsMatch(allTblText, @"(?i)(ma\s+trận\s+đặc\s+tả|khung\s+ma\s+trận|mức\s+độ\s+đánh\s+giá)"))
                    {
                        isMatrix = true;
                        matrixTableCount++;
                    }
                    else if (System.Text.RegularExpressions.Regex.IsMatch(allTblText, @"(?i)(hướng\s+dẫn\s+chấm|biểu\s+điểm|thang\s+điểm|lời\s+giải)"))
                    {
                        isSolution = true;
                        solutionTableCount++;
                    }
                    else if (System.Text.RegularExpressions.Regex.IsMatch(allTblText, @"(?i)(bảng\s+đáp\s+án|đáp\s+án|câu\s*1|câu\s*2)") &&
                             System.Text.RegularExpressions.Regex.IsMatch(allTblText, @"\b[A-D]\b"))
                    {
                        isAnswerGrid = true;
                        answerTableCount++;
                    }

                    if ((isAnswerGrid || isSolution || isMatrix) && tableIdx <= 15)
                    {
                        var kind = isAnswerGrid ? "🎯 [BẢNG ĐÁP ÁN TRẮC NGHIỆM]" : (isSolution ? "📝 [HƯỚNG DẪN CHẤM / BIỂU ĐIỂM]" : "📐 [MA TRẬN ĐẶC TẢ]");
                        writer.WriteLine($"- **Bảng #{tableIdx}** ({rows.Count} dòng, {rows[0].Elements(w + "tc").Count()} cột) - {kind}:");
                        for (int i = 0; i < Math.Min(4, rowTexts.Count); i++)
                        {
                            var preview = rowTexts[i];
                            if (preview.Length > 150) preview = preview.Substring(0, 150) + "...";
                            writer.WriteLine($"  - Dòng {i + 1}: `{preview}`");
                        }
                    }
                }

                writer.WriteLine($"- Thống kê bảng: {answerTableCount} bảng đáp án trắc nghiệm, {solutionTableCount} bảng hướng dẫn chấm tự luận, {matrixTableCount} bảng ma trận.");

                // Thử bóc tách với DocxParserService hiện tại
                fs.Seek(0, SeekOrigin.Begin);
                var questions = DocxParserService.ParseRichDocxStream(fs);
                int hasAnswer = questions.Count(q => !string.IsNullOrWhiteSpace(q.SuggestedAnswer));
                int hasExplanation = questions.Count(q => !string.IsNullOrWhiteSpace(q.AiExplanation));
                writer.WriteLine($"- **Kết quả bóc tách hiện tại**:");
                writer.WriteLine($"  - Tổng câu hỏi: **{questions.Count}**");
                writer.WriteLine($"  - Có đáp án: **{hasAnswer}** ({(questions.Count > 0 ? hasAnswer * 100.0 / questions.Count : 0):F1}%)");
                writer.WriteLine($"  - Có hướng dẫn giải / biểu điểm: **{hasExplanation}** ({(questions.Count > 0 ? hasExplanation * 100.0 / questions.Count : 0):F1}%)");
                writer.WriteLine();
                writer.Flush();
            }

            writer.WriteLine("--- HẾT BÁO CÁO ---");
        }
    }
}
