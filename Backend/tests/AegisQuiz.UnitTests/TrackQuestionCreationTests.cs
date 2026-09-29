using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using UglyToad.PdfPig;
using AegisQuiz.Infrastructure.Services;

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
    }
}
