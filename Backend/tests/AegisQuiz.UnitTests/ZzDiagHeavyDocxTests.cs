using System;
using System.Diagnostics;
using System.IO;
using AegisQuiz.Infrastructure.Services;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class ZzDiagHeavyDocxTests
    {
        private const string Report = @"D:\Cuong\DuAn\mybank\AegisQuiz\Backend\zz_diag_timing.txt";

        [Fact]
        public void SlowFileEndToEnd()
        {
            File.WriteAllText(Report, "");
            var path = @"D:\Cuong\DuAn\mybank\2026-TNPT\THCS\thuvienhoclieu.com-40-De-on-tap-cuoi-HK1-Toan-7-nam-24-25.docx";
            using var fs = File.OpenRead(path);
            var sw = Stopwatch.StartNew();
            var results = DocxParserService.ParseRichDocxStream(fs);
            File.AppendAllText(Report, $"E2E slow file: {results.Count} questions in {sw.ElapsedMilliseconds}ms{Environment.NewLine}");
        }
    }
}
