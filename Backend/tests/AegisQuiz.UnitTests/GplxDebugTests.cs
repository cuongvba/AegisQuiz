using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Services;

namespace AegisQuiz.UnitTests
{
    public class GplxDebugTests
    {
        private readonly ITestOutputHelper _output;

        public GplxDebugTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DebugGplxFirstFewQuestions()
        {
            var path = @"D:\Cuong\DuAn\mybank\2026-TNPT\600caugplx.pdf";
            var pipeline = new DynamicPdfPipeline(DynamicQuestionIngestionEngine.Instance);
            using var fs = File.OpenRead(path);
            var results = pipeline.Execute(fs);

            for (int i = 0; i < Math.Min(15, results.Count); i++)
            {
                var q = results[i];
                _output.WriteLine($"Q[{i}]: Type={q.QuestionType}, Options={q.Options.Count}, Content={q.Content.Replace("\n", " ").Substring(0, Math.Min(80, q.Content.Length))}");
            }
        }
    }
}
