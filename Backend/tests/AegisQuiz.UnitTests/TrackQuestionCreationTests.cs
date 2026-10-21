using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using UglyToad.PdfPig;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.Infrastructure.Services.Docx;

namespace AegisQuiz.UnitTests
{
    public class TrackQuestionCreationTests
    {
        private readonly ITestOutputHelper _output;

        public TrackQuestionCreationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void FindLinesBeforeQuestion1()
        {
            var path = @"D:\Cuong\DuAn\mybank\2026-TNPT\600caugplx.pdf";
            if (!File.Exists(path)) return;
            using var doc = PdfDocument.Open(path);
            
            // Let's check pages 1, 2, 3
            for (int p = 1; p <= 4; p++)
            {
                var page = doc.GetPage(p);
                var words = page.GetWords().ToList();
                _output.WriteLine($"=== PAGE {p} (Words: {words.Count}) ===");
                
                // Group words into lines
                var lines = page.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var clean = line.Trim();
                    bool isQuestion = Regex.IsMatch(clean, @"^(?:Câu|Question|Item|Task|Exercise|Q|Bài|Bai)\s*\d+", RegexOptions.IgnoreCase);
                    bool isDyn = DynamicQuestionIngestionEngine.Instance.TryMatchQuestionAnchor(clean, out int qNum, out var rule);
                    if (isQuestion || isDyn)
                    {
                        _output.WriteLine($"[MATCH P.{p}] IsQ={isQuestion}, IsDyn={isDyn} (Rule={rule?.RuleCode}, QNum={qNum}): {clean}");
                    }
                }
            }
        }

        [Fact]
        public void ParseEnglish1102_ShouldExtractExactly40Questions_WithoutFakeHeaderQuestion()
        {
            var path = @"D:\Cuong\DuAn\DETHI\TNPT\thuvienhoclieu.com-De-thi-TN-THPT-2026-Tieng-Anh-Lan-2-Bo-GD-ma-de-1102.docx";
            if (!File.Exists(path)) return;
            
            var parser = new DocxParserService();
            using var stream = File.OpenRead(path);
            var questions = parser.ParseDocxFile(stream);
            
            // Đề thi tốt nghiệp THPT Tiếng Anh chuẩn 2026 của Bộ GD&ĐT gồm chính xác 40 câu hỏi trắc nghiệm
            Assert.Equal(40, questions.Count);
            
            // Câu 1: Sắp xếp các câu thành đoạn văn (Arrangement)
            Assert.Equal("SINGLE", questions[0].QuestionType);
            Assert.Equal(4, questions[0].Options.Count);
            Assert.Contains("Critics assert that the bioreactors", questions[0].Content);
            Assert.Contains("Mark the letter A, B, C or D", questions[0].ContextContent ?? "");

            // Câu 2: Sắp xếp hội thoại (Exchange)
            Assert.Equal("SINGLE", questions[1].QuestionType);
            Assert.Equal(4, questions[1].Options.Count);
            Assert.Contains("Client: That works, thanks.", questions[1].Content);
            Assert.Equal("3", questions[1].SuggestedAnswer); // Đáp án C
        }

        [Fact]
        public void ParseAllEnglishTnptExams_ShouldExtractCleanly()
        {
            var folder = @"D:\Cuong\DuAn\DETHI\TNPT";
            if (!Directory.Exists(folder)) return;

            var parser = new DocxParserService();
            var files = Directory.GetFiles(folder, "*Tieng-Anh*.docx");
            Assert.NotEmpty(files);

            foreach (var f in files)
            {
                using var stream = File.OpenRead(f);
                var questions = parser.ParseDocxFile(stream);
                _output.WriteLine($"FILE: {Path.GetFileName(f)} -> Parsed {questions.Count} questions");
                
                // Đề thi tốt nghiệp THPT Tiếng Anh Bộ GD&ĐT chuẩn là 40 câu
                Assert.True(questions.Count == 40 || questions.Count == 50, $"File {Path.GetFileName(f)} has unexpected question count: {questions.Count}");
                
                // Câu đầu tiên tuyệt đối không được chứa bảng thí sinh / phòng thi
                Assert.DoesNotContain("thời gian làm bài", questions[0].Content, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("thí sinh", questions[0].Content, StringComparison.OrdinalIgnoreCase);
                Assert.True(questions[0].Options.Count >= 2, $"First question in {Path.GetFileName(f)} has only {questions[0].Options.Count} options");
            }
        }

        [Fact]
        public void StructuralSentenceBoundary_EdgeCases_ShouldNotSplitNaturalSentences()
        {
            // Case 1: Thời gian làm bài 50 phút
            var sample1 = "Thời gian làm bài 50 phút, không kể thời gian phát đề";
            bool isQ1 = DynamicQuestionIngestionEngine.Instance.TryMatchQuestionAnchor(sample1, out int qNum1, out _);
            Assert.False(isQ1);
            Assert.Equal(0, qNum1);

            // Case 2: Chỉ thị Từ bài 1 đến bài 5
            var sample2 = "Từ bài 1 đến bài 5 là phần ôn tập ngữ pháp nâng cao.";
            bool isQ2 = DynamicQuestionIngestionEngine.Instance.TryMatchQuestionAnchor(sample2, out int qNum2, out _);
            Assert.False(isQ2);
            Assert.Equal(0, qNum2);

            // Case 3: Nhắc tới bài trong câu văn tự nhiên
            var sample3 = "Học sinh xem lại phương pháp giải ở bài 2 để áp dụng vào bài 3.";
            bool isQ3 = DynamicQuestionIngestionEngine.Instance.TryMatchQuestionAnchor(sample3, out int qNum3, out _);
            Assert.False(isQ3);
            Assert.Equal(0, qNum3);

            // Case 4: Câu hỏi thật sự có dấu phân cách rõ ràng
            var sample4 = "Câu 1. Cho hàm số y = f(x) liên tục trên R.";
            bool isQ4 = DynamicQuestionIngestionEngine.Instance.TryMatchQuestionAnchor(sample4, out int qNum4, out var rule4);
            Assert.True(isQ4);
            Assert.Equal(1, qNum4);
            Assert.NotNull(rule4);

            // Case 5: Đề thi tiếng Anh có Question 2:
            var sample5 = "Question 2: Choose the best answer to complete the sentence.";
            bool isQ5 = DynamicQuestionIngestionEngine.Instance.TryMatchQuestionAnchor(sample5, out int qNum5, out var rule5);
            Assert.True(isQ5);
            Assert.Equal(2, qNum5);
            Assert.NotNull(rule5);
        }

        [Fact]
        public void ParseMathExam_ShouldExtractHighFidelityAnswersAndExplanations()
        {
            var f = @"D:\Cuong\DuAn\mybank\2026-TNPT\DeToanGiaiChiTiet.docx";
            if (!File.Exists(f)) f = @"D:\Cuong\DuAn\mybank\DocPDF\DeToanGiaiChiTiet.docx";
            if (!File.Exists(f)) return;

            using var stream = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var parser = new DocxParserService();
            var questions = parser.ParseDocxFile(stream);
            
            // Bộ 10 đề thi Toán GDPT 2026 bóc tách đầy đủ
            Assert.True(questions.Count >= 200, $"Expected >= 200 questions, got {questions.Count}");

            int withExpl = questions.Count(q => !string.IsNullOrWhiteSpace(q.AiExplanation));
            int withAns = questions.Count(q => !string.IsNullOrWhiteSpace(q.SuggestedAnswer));
            
            // Toàn bộ các câu hỏi có lời giải trong docx phải được thu thập (>= 185 câu)
            Assert.True(withExpl >= 185, $"Expected >= 185 questions with explanation, got {withExpl}");
            Assert.Equal(questions.Count, withAns);

            // Kiểm tra Câu 1 Đề 1:
            var q1 = questions.FirstOrDefault(q => q.SourceExamName == "DE_01" && q.SourceSectionName == "SINGLE" && q.SourceQuestionNo == "1");
            Assert.NotNull(q1);
            Assert.Equal("2", q1.SuggestedAnswer); // Đáp án B
            Assert.False(string.IsNullOrWhiteSpace(q1.AiExplanation));
            Assert.Contains("cực tiểu", q1.AiExplanation, StringComparison.OrdinalIgnoreCase);

            // Kiểm tra Câu 4 Đề 1 (tiệm cận ngang):
            var q4 = questions.FirstOrDefault(q => q.SourceExamName == "DE_01" && q.SourceSectionName == "SINGLE" && q.SourceQuestionNo == "4");
            Assert.NotNull(q4);
            Assert.Equal("2", q4.SuggestedAnswer); // Đáp án B

            // Kiểm tra Câu 1 Phần II Đúng/Sai Đề 1:
            var q13 = questions.FirstOrDefault(q => q.SourceExamName == "DE_01" && q.SourceSectionName == "TRUE_FALSE" && q.SourceQuestionNo == "1");
            Assert.NotNull(q13);
            Assert.Contains("Đúng", q13.SuggestedAnswer);
            Assert.Contains("Sai", q13.SuggestedAnswer);
            Assert.False(string.IsNullOrWhiteSpace(q13.AiExplanation));
        }

        [Fact]
        public void TraceMathExamParagraphs765To865()
        {
            var f = @"D:\Cuong\DuAn\mybank\2026-TNPT\DeToanGiaiChiTiet.docx";
            if (!File.Exists(f)) return;

            using var archive = new System.IO.Compression.ZipArchive(File.Open(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            var entry = archive.GetEntry("word/document.xml");
            if (entry == null) return;

            using var ds = entry.Open();
            var doc = System.Xml.Linq.XDocument.Load(ds);
            var W = System.Xml.Linq.XNamespace.Get("http://schemas.openxmlformats.org/wordprocessingml/2006/main");
            var paras = doc.Descendants(W + "p")
                .Select(p => string.Concat(p.Descendants(W + "t").Select(t => t.Value)).Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            var bodyEl = doc.Descendants(W + "body").FirstOrDefault();
            _output.WriteLine("BODY ELEMENT NAMES: " + string.Join(", ", bodyEl?.Elements().Take(15).Select(e => e.Name.LocalName) ?? Array.Empty<string>()));
            var examHeadings = bodyEl?.Elements().Where(e => e.Name == W + "p" && DynamicQuestionIngestionEngine.Instance.TryMatchExamHeading(string.Concat(e.Descendants(W + "t").Select(t => t.Value)).Trim(), out _)).ToList();
            _output.WriteLine($"EXAM HEADINGS IN BODY.ELEMENTS: {examHeadings?.Count ?? 0}");
            if (examHeadings != null && examHeadings.Count > 0)
            {
                foreach (var eh in examHeadings)
                {
                    _output.WriteLine("  HEAD: " + string.Concat(eh.Descendants(W + "t").Select(t => t.Value)).Trim());
                }
            }

            var cig = DynamicQuestionIngestionEngine.Instance;
            for (int i = 765; i < 865; i++)
            {
                var line = paras[i];
                bool isExam = cig.TryMatchExamHeading(line, out var examCode);
                bool isSec = cig.TryMatchSectionHeader(line, out var secKey);
                bool isSol = cig.TryMatchSolutionPhase(line);
                bool isAns = cig.TryMatchAnswerTablePhase(line);
                bool isQ = cig.TryMatchQuestionAnchor(line, out int qNum, out var rule);
                bool isExpl = cig.IsExplanationLine(line);

                if (isExam || isSec || isSol || isAns || isQ || isExpl || line.StartsWith("Lời giải", StringComparison.OrdinalIgnoreCase))
                {
                    _output.WriteLine($"[L.{i}] Exam={isExam}({examCode}), Sec={isSec}({secKey}), SolPhase={isSol}, AnsPhase={isAns}, Q={isQ}({qNum}), Expl={isExpl} || TEXT: {line}");
                }
            }
        }
    }
}

