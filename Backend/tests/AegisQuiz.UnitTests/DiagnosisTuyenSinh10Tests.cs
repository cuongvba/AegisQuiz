using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.Infrastructure.Services.Docx;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace AegisQuiz.UnitTests
{
    public class DiagnosisTuyenSinh10Tests
    {
        private readonly ITestOutputHelper _output;

        public DiagnosisTuyenSinh10Tests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Diagnose_ToanTuyenSinh10_QuestionTypes()
        {
            var filePath = @"D:\Cuong\DuAn\DETHI\TUYENSINH10\thuvienhoclieu.com-De-thi-thu-mon-Toan-vao-10-THPT-Ngo-Quyen-Thai-Nguyen-2026-2027.docx";
            if (!File.Exists(filePath))
            {
                _output.WriteLine($"File không tồn tại: {filePath}");
                return;
            }

            using (var streamRaw = File.OpenRead(filePath))
            {
                var rawParas = DocxParserService.ExtractRawParagraphsFromDocx(streamRaw);
                _output.WriteLine($"RAW PARAGRAPHS COUNT: {rawParas.Count}");
                var cig = DynamicQuestionIngestionEngine.Instance;
                _output.WriteLine($"RAW[5]='{rawParas[5]}'");
                _output.WriteLine($"TryMatchTopicHeading RAW[5]: {cig.TryMatchTopicHeading(rawParas[5], out var tName)} -> '{tName}'");
                _output.WriteLine($"ResolveTopicCode '{tName}': {DocxStateMachineClassifier.ResolveTopicCode(tName)}");
                for (int i = 0; i < Math.Min(15, rawParas.Count); i++)
                {
                    _output.WriteLine($"RAW[{i}]: {rawParas[i]}");
                }
            }

            var parser = new DocxParserService(NullLogger<DocxParserService>.Instance);
            using var stream = File.OpenRead(filePath);
            var result = parser.ParseDocxFile(stream);

            _output.WriteLine($"TỔNG SỐ CÂU HỎI TRÍCH XUẤT: {result.Count}");
            foreach (var q in result)
            {
                _output.WriteLine($"[Q{q.SourceQuestionNo}] Type={q.QuestionType} | Opts={q.Options.Count} | Content={q.Content?.Substring(0, Math.Min(80, q.Content?.Length ?? 0)).Replace("\n", " ")}");
            }

            // Assertions bảo đảm 100% đề Toán Tự luận Tuyển sinh 10 được nhận diện đúng
            Assert.Equal(8, result.Count);
            Assert.All(result, q => Assert.Equal("ESSAY", q.QuestionType));
            Assert.Contains(result, q => q.SourceQuestionNo == "7");
            Assert.Contains(result, q => q.SourceQuestionNo == "8");
        }

        [Fact]
        public void Diagnose_TiengAnhTuyenSinh10_Context()
        {
            var filePath = @"D:\Cuong\DuAn\DETHI\TUYENSINH10\thuvienhoclieu.com-De-thi-tuyen-sinh-10-Tieng-Anh-So-GD-Ha-Noi-nam-26-27.docx";
            if (!File.Exists(filePath))
            {
                _output.WriteLine($"File không tồn tại: {filePath}");
                return;
            }

            var parser = new DocxParserService(NullLogger<DocxParserService>.Instance);
            using var stream = File.OpenRead(filePath);
            var result = parser.ParseDocxFile(stream);

            _output.WriteLine($"TỔNG SỐ CÂU HỎI TRÍCH XUẤT: {result.Count}");
            foreach (var q in result)
            {
                _output.WriteLine($"[Q{q.SourceQuestionNo}] Type={q.QuestionType} | ContextId={q.ContextId} | ContextTitle={q.ContextTitle} | HasContextContent={!string.IsNullOrEmpty(q.ContextContent)}");
            }

            // Assertions bảo đảm bóc tách đúng Context của đề Tiếng Anh Tuyển sinh 10
            Assert.Equal(40, result.Count);

            // Đoạn văn đọc hiểu (Câu 11 - 16)
            for (int i = 11; i <= 16; i++)
            {
                var q = result.FirstOrDefault(x => x.SourceQuestionNo == i.ToString());
                Assert.NotNull(q);
                Assert.False(string.IsNullOrEmpty(q.ContextId));
                Assert.False(string.IsNullOrEmpty(q.ContextContent));
            }

            // Các câu độc lập không dính context (Câu 17 - 20)
            for (int i = 17; i <= 20; i++)
            {
                var q = result.FirstOrDefault(x => x.SourceQuestionNo == i.ToString());
                Assert.NotNull(q);
                Assert.True(string.IsNullOrEmpty(q.ContextId));
            }

            // Đoạn văn Cloze Test điền từ (Câu 27 - 32)
            for (int i = 27; i <= 32; i++)
            {
                var q = result.FirstOrDefault(x => x.SourceQuestionNo == i.ToString());
                Assert.NotNull(q);
                Assert.False(string.IsNullOrEmpty(q.ContextId));
                Assert.False(string.IsNullOrEmpty(q.ContextContent));
            }
        }
    }
}
