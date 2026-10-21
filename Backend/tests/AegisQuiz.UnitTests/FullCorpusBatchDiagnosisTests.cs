using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.Infrastructure.Services.Docx;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace AegisQuiz.UnitTests
{
    public class FullCorpusBatchDiagnosisTests
    {
        private readonly ITestOutputHelper _output;
        private const string RootDir = @"D:\Cuong\DuAn\DETHI";

        public FullCorpusBatchDiagnosisTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public class FileBatchResult
        {
            public string Category { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
            public long FileSizeBytes { get; set; }
            public long DurationMs { get; set; }
            public int TotalQuestions { get; set; }
            public int SingleCount { get; set; }
            public int TrueFalseCount { get; set; }
            public int ShortAnswerCount { get; set; }
            public int EssayCount { get; set; }
            public int AnswerCount { get; set; }
            public int DirtyCount { get; set; }
            public List<string> DirtySamples { get; set; } = new();
            public List<string> Anomalies { get; set; } = new();
            public bool Success { get; set; } = true;
            public string? ErrorMessage { get; set; }
        }

        private List<FileBatchResult> ExecuteDiagnosis(IEnumerable<string> filePaths, string groupTitle)
        {
            _output.WriteLine($"===============================================================================");
            _output.WriteLine($" {groupTitle} — SỐ LƯỢNG: {filePaths.Count()} TỆP");
            _output.WriteLine($"===============================================================================");

            var parser = new DocxParserService(NullLogger<DocxParserService>.Instance);
            var results = new List<FileBatchResult>();
            int idx = 0;

            foreach (var filePath in filePaths)
            {
                idx++;
                var fileInfo = new FileInfo(filePath);
                string relativePath = Path.GetRelativePath(RootDir, filePath);
                string category = relativePath.Contains(Path.DirectorySeparatorChar)
                    ? relativePath.Split(Path.DirectorySeparatorChar)[0]
                    : "ROOT";

                var item = new FileBatchResult
                {
                    Category = category,
                    FileName = fileInfo.Name,
                    FileSizeBytes = fileInfo.Length
                };

                var sw = Stopwatch.StartNew();
                try
                {
                    using var stream = File.OpenRead(filePath);
                    var questions = parser.ParseDocxFile(stream);
                    sw.Stop();
                    item.DurationMs = sw.ElapsedMilliseconds;
                    item.TotalQuestions = questions.Count;

                    item.SingleCount = questions.Count(q => q.QuestionType == "SINGLE" || q.QuestionType == "MULTI");
                    item.TrueFalseCount = questions.Count(q => q.QuestionType == "TRUE_FALSE" || q.QuestionType == "MULTI_TRUE_FALSE");
                    item.ShortAnswerCount = questions.Count(q => q.QuestionType == "SHORT_ANSWER");
                    item.EssayCount = questions.Count(q => q.QuestionType == "ESSAY");
                    item.AnswerCount = questions.Count(q => !string.IsNullOrWhiteSpace(q.SuggestedAnswer) && !q.SuggestedAnswer.Contains("Điền đáp số"));

                    foreach (var q in questions)
                    {
                        string content = q.Content ?? "";
                        string expl = q.AiExplanation ?? "";

                        bool hasHet = Regex.IsMatch(content, @"(?m)^[\-\–\—_*\=.\s]*\b(?:HẾT|THE\s*END)\b[\-\–\—_*\=.\s]*$")
                                   || Regex.IsMatch(expl, @"(?m)^[\-\–\—_*\=.\s]*\b(?:HẾT|THE\s*END)\b[\-\–\—_*\=.\s]*$");
                        bool hasProctor = Regex.IsMatch(content, @"(?i)(?:Cán\s*bộ\s*coi\s*thi|Giám\s*thị)\s+không\s+giải\s+thích")
                                       || Regex.IsMatch(expl, @"(?i)(?:Cán\s*bộ\s*coi\s*thi|Giám\s*thị)\s+không\s+giải\s+thích");

                        if (hasHet || hasProctor)
                        {
                            item.DirtyCount++;
                            if (item.DirtySamples.Count < 3)
                            {
                                item.DirtySamples.Add($"Q{q.SourceQuestionNo}: " + (content.Length > 80 ? content.Substring(0, 80) : content));
                            }
                        }

                        if ((q.QuestionType == "SINGLE" || q.QuestionType == "MULTI") && q.Options.Count == 0)
                        {
                            if (item.Anomalies.Count < 3)
                            {
                                item.Anomalies.Add($"Q{q.SourceQuestionNo} SINGLE nhưng Options=0");
                            }
                        }

                        if (string.IsNullOrWhiteSpace(q.Content))
                        {
                            item.Anomalies.Add($"Q{q.SourceQuestionNo} Content rỗng");
                        }
                    }

                    _output.WriteLine($"[{idx:D2}] {item.FileName.PadRight(45).Substring(0, 45)} | Qs={item.TotalQuestions:D3} | S={item.SingleCount:D2} TF={item.TrueFalseCount:D2} SA={item.ShortAnswerCount:D2} E={item.EssayCount:D2} | Ans={item.AnswerCount:D2} | Dirty={item.DirtyCount} | {item.DurationMs}ms");
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    item.Success = false;
                    item.DurationMs = sw.ElapsedMilliseconds;
                    item.ErrorMessage = ex.Message;
                    _output.WriteLine($"[{idx:D2}] [LỖI] {item.FileName} => {ex.GetType().Name}: {ex.Message}");
                }

                results.Add(item);
            }

            int successCount = results.Count(r => r.Success);
            int totalQ = results.Sum(r => r.TotalQuestions);
            int totalAns = results.Sum(r => r.AnswerCount);
            int totalDirty = results.Sum(r => r.DirtyCount);

            _output.WriteLine($"--> KẾT QUẢ: {successCount}/{results.Count} tệp OK | {totalQ} câu hỏi | {totalAns} có đáp án | {totalDirty} câu bị dính dirty marker\n");
            return results;
        }

        [Fact]
        public void Diagnosis_TNPT_Batch()
        {
            string dir = Path.Combine(RootDir, "TNPT");
            if (!Directory.Exists(dir)) return;

            var files = Directory.GetFiles(dir, "*.docx").OrderBy(f => f).ToList();
            var results = ExecuteDiagnosis(files, "KIỂM THỬ NHÓM ĐỀ TỐT NGHIỆP THPT (TNPT)");

            Assert.All(results, r => Assert.True(r.Success));
            Assert.All(results, r => Assert.True(r.TotalQuestions > 0));
        }

        [Fact]
        public void Diagnosis_Tuyensinh10_IndividualExams()
        {
            string dir = Path.Combine(RootDir, "TUYENSINH10");
            if (!Directory.Exists(dir)) return;

            // Lọc các đề thi lẻ (không phải bộ 20/30/40 đề gom chung để chạy nhanh)
            var files = Directory.GetFiles(dir, "*.docx")
                .Where(f => !Path.GetFileName(f).StartsWith("thuvienhoclieu.com-Bo-", StringComparison.OrdinalIgnoreCase) 
                         && !Path.GetFileName(f).StartsWith("thuvienhoclieu.com-40-", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .ToList();

            var results = ExecuteDiagnosis(files, "KIỂM THỬ NHÓM ĐỀ THI LẺ TUYỂN SINH 10 CÁC TỈNH THÀNH");

            Assert.All(results, r => Assert.True(r.Success));
            Assert.All(results, r => Assert.True(r.TotalQuestions > 0));
        }

        [Fact]
        public void Diagnosis_LOP7_Batch()
        {
            string dir = Path.Combine(RootDir, "LOP7");
            if (!Directory.Exists(dir)) return;

            var files = Directory.GetFiles(dir, "*.docx")
                .Where(f => new FileInfo(f).Length < 3 * 1024 * 1024) // Các file < 3MB
                .OrderBy(f => f)
                .ToList();

            var results = ExecuteDiagnosis(files, "KIỂM THỬ NHÓM ĐỀ THI LỚP 7");

            Assert.All(results, r => Assert.True(r.Success));
            Assert.All(results, r => Assert.True(r.TotalQuestions > 0));
        }

        [Fact]
        public void Diagnosis_THCS_Batch()
        {
            string dir = Path.Combine(RootDir, "THCS");
            if (!Directory.Exists(dir)) return;

            var files = Directory.GetFiles(dir, "*.docx")
                .Where(f => new FileInfo(f).Length < 3 * 1024 * 1024)
                .OrderBy(f => f)
                .ToList();

            var results = ExecuteDiagnosis(files, "KIỂM THỬ NHÓM ĐỀ THI THCS (LỚP 6, 7, 8)");

            Assert.All(results, r => Assert.True(r.Success));
            Assert.All(results, r => Assert.True(r.TotalQuestions > 0));
        }

        [Fact]
        public void Inspect_Dirty_Samples()
        {
            var testFiles = new[]
            {
                @"D:\Cuong\DuAn\DETHI\TNPT\thuvienhoclieu.com-De-thi-TN-THPT-2026-mon-Hoa-Bo-GD-ma-de-0323.docx",
                @"D:\Cuong\DuAn\DETHI\TUYENSINH10\thuvienhoclieu.com-De-thi-thu-mon-Toan-vao-10-THPT-Ngo-Quyen-Thai-Nguyen-2026-2027.docx"
            };

            var parser = new DocxParserService(NullLogger<DocxParserService>.Instance);
            foreach (var file in testFiles)
            {
                if (!File.Exists(file)) continue;
                using var stream = File.OpenRead(file);
                var questions = parser.ParseDocxFile(stream);
                _output.WriteLine($"\n=== FILE: {Path.GetFileName(file)} ({questions.Count} câu) ===");
                foreach (var q in questions)
                {
                    string content = q.Content ?? "";
                    string expl = q.AiExplanation ?? "";

                    bool hasHet = Regex.IsMatch(content, @"(?m)^[\-\–\—_*\=.\s]*\b(?:HẾT|THE\s*END)\b[\-\–\—_*\=.\s]*$")
                               || Regex.IsMatch(expl, @"(?m)^[\-\–\—_*\=.\s]*\b(?:HẾT|THE\s*END)\b[\-\–\—_*\=.\s]*$");
                    bool hasProctor = Regex.IsMatch(content, @"(?i)(?:Cán\s*bộ\s*coi\s*thi|Giám\s*thị)\s+không\s+giải\s+thích")
                                   || Regex.IsMatch(expl, @"(?i)(?:Cán\s*bộ\s*coi\s*thi|Giám\s*thị)\s+không\s+giải\s+thích");

                    if (hasHet || hasProctor)
                    {
                        _output.WriteLine($"[FLAGGED Q{q.SourceQuestionNo}]: Type={q.QuestionType}");
                        _output.WriteLine($"CONTENT: {content}");
                        if (!string.IsNullOrWhiteSpace(expl)) _output.WriteLine($"EXPL: {expl}");
                    }
                }
            }
        }
    }
}
