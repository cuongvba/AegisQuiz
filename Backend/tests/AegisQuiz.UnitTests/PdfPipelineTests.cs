using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Services;

namespace AegisQuiz.UnitTests
{
    public class PdfPipelineTests
    {
        private readonly ITestOutputHelper _output;

        public PdfPipelineTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DynamicPdfPipeline_GPLX_ShouldExtractQuestionsWithSpatialImages()
        {
            string[] possiblePaths = new[]
            {
                @"D:\Cuong\DuAn\mybank\2026-TNPT\600caugplx.pdf",
                @"D:\Cuong\DuAn\mybank\DocPDF\600caugplx.pdf",
                @"D:\Cuong\DuAn\mybank\AegisQuiz\600caugplx.pdf",
                @"D:\Cuong\DuAn\mybank\DocPDF\60-cau-diem-liet-2023.pdf"
            };

            var validPath = possiblePaths.FirstOrDefault(File.Exists);
            if (validPath == null)
            {
                _output.WriteLine("Không tìm thấy tệp PDF GPLX để kiểm thử.");
                return;
            }

            var pipeline = new DynamicPdfPipeline(DynamicQuestionIngestionEngine.Instance);
            using var fs = File.OpenRead(validPath);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var results = pipeline.Execute(fs);
            sw.Stop();

            _output.WriteLine($"[DynamicPdfPipeline] Tệp: {Path.GetFileName(validPath)}");
            _output.WriteLine($" - Thời gian xử lý: {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2}s)");
            _output.WriteLine($" - Tổng số câu hỏi bóc tách: {results.Count}");

            int withImages = results.Count(q => q.ContentType == "image" || q.Content.Contains("data:image/"));
            int criticals = results.Count(q => q.IsCritical);
            int withAnswers = results.Count(q => !string.IsNullOrWhiteSpace(q.SuggestedAnswer));

            _output.WriteLine($" - Số câu hỏi có hình ảnh/sa hình: {withImages}");
            _output.WriteLine($" - Số câu hỏi điểm liệt: {criticals}");
            _output.WriteLine($" - Số câu có đáp án: {withAnswers}");

            Assert.NotEmpty(results);
            Assert.True(results.Count >= 50, $"Kỳ vọng bóc tách được ít nhất 50 câu hỏi từ {Path.GetFileName(validPath)}");
        }

        [Fact]
        public void DynamicPdfPipeline_ScannedPdf_ShouldExtractMultimodalQuestions()
        {
            string[] possiblePaths = new[]
            {
                @"D:\Cuong\DuAn\mybank\DocPDF\GPLXf2023.pdf",
                @"D:\Cuong\DuAn\mybank\AegisQuiz\GPLXf2023.pdf"
            };

            var validPath = possiblePaths.FirstOrDefault(File.Exists);
            if (validPath == null)
            {
                _output.WriteLine("Không tìm thấy tệp GPLXf2023.pdf scan.");
                return;
            }

            var pipeline = new DynamicPdfPipeline(DynamicQuestionIngestionEngine.Instance);
            using var fs = File.OpenRead(validPath);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var results = pipeline.Execute(fs);
            sw.Stop();

            _output.WriteLine($"[DynamicPdfPipeline] PDF Scan {Path.GetFileName(validPath)}: {results.Count} câu ({sw.ElapsedMilliseconds} ms)");
            Assert.NotEmpty(results);
            Assert.True(results.Count >= 100, $"Kỳ vọng ít nhất 100 câu hỏi từ PDF Scan GPLXf2023.pdf, thực tế: {results.Count}");
        }

        [Fact]
        public void DynamicPdfPipeline_ExtractPlainText_ShouldReconstructSpatialFlow()
        {
            string[] possiblePaths = new[]
            {
                @"D:\Cuong\DuAn\mybank\DocPDF\600caugplx.pdf",
                @"D:\Cuong\DuAn\mybank\AegisQuiz\600caugplx.pdf",
                @"D:\Cuong\DuAn\mybank\DocPDF\thuvienhoclieu.com-SGK-Toan-7-tu-nam-2026-Tap-1.pdf"
            };

            var validPath = possiblePaths.FirstOrDefault(File.Exists);
            if (validPath == null) return;

            var pipeline = new DynamicPdfPipeline(DynamicQuestionIngestionEngine.Instance);
            using var fs = File.OpenRead(validPath);
            string text = pipeline.ExtractPlainText(fs);

            _output.WriteLine($"[DynamicPdfPipeline] Trích xuất toàn văn {Path.GetFileName(validPath)}: {text.Length} ký tự");
            Assert.NotEmpty(text);
            Assert.True(text.Length > 500, "Văn bản trích xuất phải có độ dài đáng kể.");
        }
    }
}
