using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Services;

namespace AegisQuiz.UnitTests
{
    public class AnchorMatchDebugTests
    {
        private readonly ITestOutputHelper _output;

        public AnchorMatchDebugTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestWhyLineMatchedQuestion()
        {
            string line = "Trung tướng Nguyễn Văn Trung, Cục";
            var matchQuestion = Regex.Match(line, @"^(?:Câu|Question|Item|Task|Exercise|Q|Bài|Bai)\s*(\d+)[\.\:\s\-]*(.*)", RegexOptions.IgnoreCase);
            _output.WriteLine($"Standard Regex Match: {matchQuestion.Success}");

            bool isDynamic = DynamicQuestionIngestionEngine.Instance.TryMatchQuestionAnchor(line, out int qNum, out var matchedRule);
            _output.WriteLine($"Dynamic Match: {isDynamic}, Rule: {matchedRule?.RuleCode}, QNum: {qNum}");
            if (matchedRule != null)
            {
                _output.WriteLine($"Anchor Pattern: {matchedRule.AnchorSpec.RegexPattern}");
            }
        }

        [Fact]
        public void TestDeTiengAnhChiTietParse()
        {
            var path = @"D:\Cuong\DuAn\mybank\DocPDF\DeTiengAnhChiTiet.docx";
            if (!System.IO.File.Exists(path)) return;
            using var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            
            // Log tokens
            using var archive = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
            var rels = AegisQuiz.Infrastructure.Services.Docx.DocxRelationshipReader.LoadRelationships(archive);
            var media = AegisQuiz.Infrastructure.Services.Docx.DocxRelationshipReader.LoadMediaFiles(archive);
            var entry = archive.GetEntry("word/document.xml");
            using var ds = entry.Open();
            var doc = System.Xml.Linq.XDocument.Load(ds);
            var ansMap = AegisQuiz.Infrastructure.Services.Docx.DocxTableQuestionExtractor.ExtractAnswerTables(doc, out var answerTableElements);
            _output.WriteLine($"AnswerTableElements count: {answerTableElements.Count}");

            fs.Seek(0, System.IO.SeekOrigin.Begin);
            using var archive2 = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
            var entry2 = archive2.GetEntry("word/document.xml");
            using var ds2 = entry2.Open();
            var doc2 = System.Xml.Linq.XDocument.Load(ds2);
            var ansKeyMap = AegisQuiz.Infrastructure.Services.Docx.DocxTableQuestionExtractor.ExtractAnswerTables(doc2, out var ansTableEls);
            
            // Call DocxParserService private ExtractParagraphTokens via reflection or reproduce logic
            var method = typeof(DocxParserService).GetMethod("ExtractParagraphTokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var paras = (List<string>)method.Invoke(null, new object[] { doc2, rels, media, ansTableEls });
            var cig = DynamicQuestionIngestionEngine.Instance;
            var activeRules = cig.GetRulesForProfile(cig.DetectDocumentLanguageProfile(paras, "vi"));
            
            var results = AegisQuiz.Infrastructure.Services.Docx.DocxStateMachineClassifier.ParseParagraphList(paras, ansKeyMap);
            _output.WriteLine($"Total parsed questions: {results.Count}");
            foreach (var q in results.Where(r => r.SourceQuestionNo is "12" or "13" or "14" or "15" or "16"))
            {
                _output.WriteLine($"--- Question {q.SourceQuestionNo} ---");
                _output.WriteLine($"Type: {q.QuestionType}");
                _output.WriteLine($"SuggestedAnswer: {q.SuggestedAnswer}");
                _output.WriteLine($"Options ({q.Options.Count}): {string.Join(" | ", q.Options)}");
                _output.WriteLine($"Content: {q.Content}");
            }
            Assert.Equal(40, results.Count);
        }

        [Fact]
        public void TestDeToanGiaiChiTietStructure()
        {
            var path = @"D:\Cuong\DuAn\mybank\2026-TNPT\DeToanGiaiChiTiet.docx";
            if (!System.IO.File.Exists(path)) return;
            using var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            using var archive = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
            var rels = AegisQuiz.Infrastructure.Services.Docx.DocxRelationshipReader.LoadRelationships(archive);
            var media = AegisQuiz.Infrastructure.Services.Docx.DocxRelationshipReader.LoadMediaFiles(archive);
            var entry = archive.GetEntry("word/document.xml");
            using var ds = entry.Open();
            var doc = System.Xml.Linq.XDocument.Load(ds);
            var ansKeyMap = AegisQuiz.Infrastructure.Services.Docx.DocxTableQuestionExtractor.ExtractAnswerTables(doc, out var ansTableEls);

            var method = typeof(DocxParserService).GetMethod("ExtractParagraphTokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var paras = (List<string>)method.Invoke(null, new object[] { doc, rels, media, ansTableEls });
            _output.WriteLine($"Total paragraphs: {paras.Count}");

            var results = AegisQuiz.Infrastructure.Services.Docx.DocxStateMachineClassifier.ParseParagraphList(paras, ansKeyMap);
            _output.WriteLine($"Total parsed questions: {results.Count}");
            var exams = results.GroupBy(r => r.SourceExamName).OrderBy(g => g.Key).ToList();
            _output.WriteLine($"Total exams count: {exams.Count}");
            foreach (var exam in ansKeyMap.Keys)
            {
                _output.WriteLine($"Exam in ansKeyMap: {exam}");
                foreach (var sec in ansKeyMap[exam].Keys)
                {
                    _output.WriteLine($"  Sec {sec}: {string.Join(", ", ansKeyMap[exam][sec].Select(kv => $"Q{kv.Key}={kv.Value}"))}");
                }
            }

            foreach (var q in results.Where(r => r.QuestionType == "MULTI_TRUE_FALSE").Take(12))
            {
                _output.WriteLine($"Exam {q.SourceExamName} Q{q.SourceQuestionNo} ({q.QuestionType}): Ans = '{q.SuggestedAnswer}'");
            }
            var brokenFormulas = new List<string>();
            var latexRegex = new Regex(@"\$([^\$]+)\$");
            foreach (var q in results)
            {
                string fullText = $"{q.Content} {string.Join(" ", q.Options)} {q.AiExplanation}";
                var matches = latexRegex.Matches(fullText);
                foreach (Match m in matches)
                {
                    string f = m.Groups[1].Value;
                    // Lỗi phổ biến:
                    // 1. \left[ mà không có \right hoặc cú pháp ngoặc lỗi như \left[ ... \\ [\right]
                    // 2. Chứa \\ ngoài matrix/cases/array
                    // 3. Thiếu dấu ngoặc nhọn
                    // 4. Các ký tự unicode bị convert sai (như uur, cent, ...)
                    if (Regex.IsMatch(f, @"\\left\[.*?\\\\\s*\[\\right\]") 
                        || f.Contains(@"\\ [\right]") || f.Contains(@"\right") && !f.Contains(@"\left")
                        || Regex.IsMatch(f, @"(?<!begin\{(matrix|cases|array|aligned)\}.*)\\\\(?!.*end\{(matrix|cases|array|aligned)\})")
                        || f.Contains(@"\left[x=") || f.Contains(@"\left[y="))
                    {
                        brokenFormulas.Add($"Exam {q.SourceExamName} Q{q.SourceQuestionNo}: {m.Value}");
                    }
                }
            }
            _output.WriteLine($"Total broken formulas found: {brokenFormulas.Count}");
            foreach (var b in brokenFormulas.Take(25))
            {
                _output.WriteLine(b);
            }

            Assert.True(results.Count >= 200, "Must parse all 10 exams");
            Assert.All(results.Where(r => r.QuestionType == "MULTI_TRUE_FALSE"), q =>
            {
                Assert.False(string.IsNullOrWhiteSpace(q.SuggestedAnswer), $"Question {q.SourceExamName} Q{q.SourceQuestionNo} must have SuggestedAnswer");
                Assert.Contains(")", q.SuggestedAnswer);
                Assert.DoesNotContain("ý", q.SuggestedAnswer);
            });

            _output.WriteLine("=== FORMULAS WITH BRACKET/BRACE BEFORE END ===");
            foreach (var q in results)
            {
                string fullText = $"{q.Content} {string.Join(" ", q.Options)} {q.AiExplanation}";
                var matches = latexRegex.Matches(fullText);
                foreach (Match m in matches)
                {
                    if (Regex.IsMatch(m.Value, @"(?:\[|\\\{)\s*\\end\{(?:matrix|cases|aligned)\}"))
                    {
                        _output.WriteLine($"Exam {q.SourceExamName} Q{q.SourceQuestionNo}: {m.Value}");
                    }
                }
            }
        }

        [Fact]
        public void TestInspectWmfIntegral()
        {
            var path = @"D:\Cuong\DuAn\mybank\2026-TNPT\DeToanGiaiChiTiet.docx";
            if (!System.IO.File.Exists(path)) return;
            using var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            using var archive = new System.IO.Compression.ZipArchive(fs, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
            var media = AegisQuiz.Infrastructure.Services.Docx.DocxRelationshipReader.LoadMediaFiles(archive);

            byte[] MathTypeTag = System.Text.Encoding.ASCII.GetBytes("Design Science, Inc.\0");

            foreach (var kvp in media)
            {
                if (!kvp.Key.EndsWith(".wmf", StringComparison.OrdinalIgnoreCase)) continue;
                byte[] bytes = kvp.Value;
                int tagIdx = -1;
                for (int i = 0; i <= bytes.Length - MathTypeTag.Length; i++)
                {
                    bool match = true;
                    for (int j = 0; j < MathTypeTag.Length; j++)
                    {
                        if (bytes[i + j] != MathTypeTag[j]) { match = false; break; }
                    }
                    if (match) { tagIdx = i; break; }
                }
                if (tagIdx < 0) continue;

                // Inspect records
                int mtefStart = tagIdx + MathTypeTag.Length;
                int p = mtefStart + 5;
                while (p < bytes.Length && bytes[p] != 0) p++;
                p += 2; // skip null and inline flag

                // Scan for tag == 3 and selector == 15 (tmINTEG)
                for (int i = p; i < bytes.Length - 10; i++)
                {
                    if (bytes[i] == 3) // TMPL
                    {
                        byte selector = bytes[i + 2]; // tag(1) + opts(1) + sel(1) ?
                        if (selector == 15) // tmINTEG
                        {
                            byte b1 = bytes[i + 3];
                            ushort variation = b1;
                            _output.WriteLine($"Media: {kvp.Key}, TMPL at {i}, selector: {selector}, variation: {variation} (0x{variation:X2})");

                            // Debug trace slots
                            using var ms = new System.IO.MemoryStream(bytes, i + 5, bytes.Length - (i + 5));
                            using var r = new System.IO.BinaryReader(ms);
                            // Read first 3 slots
                            for (int s = 1; s <= 3; s++)
                            {
                                if (r.BaseStream.Position >= r.BaseStream.Length) break;
                                byte bTag = r.ReadByte();
                                r.BaseStream.Position--;
                                if (bTag == 0) { _output.WriteLine($"  Slot {s}: END (tag 0)"); break; }
                                string slotText = typeof(AegisQuiz.Infrastructure.Services.Docx.MtefToLatexConverter)
                                    .GetMethod("ParseSlot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                                    .Invoke(null, new object[] { r }) as string ?? "";
                                _output.WriteLine($"  Slot {s}: '{slotText}'");
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void TestDe2DocxExplanationHierarchy()
        {
            var path = @"D:\Cuong\DuAn\mybank\DocPDF\ĐỀ 2.docx";
            if (!System.IO.File.Exists(path)) return;
            using var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            var results = AegisQuiz.Infrastructure.Services.DocxParserService.ParseRichDocxStream(fs);
            _output.WriteLine($"Total parsed questions: {results.Count}");

            using var fsTokens = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            using var archive = new System.IO.Compression.ZipArchive(fsTokens, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true);
            var rels = AegisQuiz.Infrastructure.Services.Docx.DocxRelationshipReader.LoadRelationships(archive);
            var media = AegisQuiz.Infrastructure.Services.Docx.DocxRelationshipReader.LoadMediaFiles(archive);
            var entry = archive.GetEntry("word/document.xml");
            using var ds = entry.Open();
            var doc = System.Xml.Linq.XDocument.Load(ds);
            var ansKeyMap = AegisQuiz.Infrastructure.Services.Docx.DocxTableQuestionExtractor.ExtractAnswerTables(doc, out var ansTableEls);
            var method = typeof(AegisQuiz.Infrastructure.Services.DocxParserService).GetMethod("ExtractParagraphTokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var paras = (List<string>)method.Invoke(null, new object[] { doc, rels, media, ansTableEls });

            _output.WriteLine("=== RAW TOKENS AROUND PART II QUESTION 3 ===");
            for (int i = 0; i < paras.Count; i++)
            {
                if (paras[i].Contains("xạ thủ") || (i >= 40 && i <= 65) || (i >= 160 && i <= 185))
                {
                    _output.WriteLine($"T[{i}]: {paras[i]}");
                }
            }

            var allQ3 = results.Where(r => r.SourceQuestionNo == "3").ToList();
            _output.WriteLine($"Total Q3 found: {allQ3.Count}");
            var q3Tf = allQ3.FirstOrDefault(q => q.QuestionType == "MULTI_TRUE_FALSE");
            Assert.NotNull(q3Tf);
            Assert.Equal(4, q3Tf.Options.Count);
            Assert.Contains(@"P\left(A\right)", q3Tf.Options[0]);
            Assert.Contains(@"P\left(E\right)", q3Tf.Options[2]);
        }
    }
}
