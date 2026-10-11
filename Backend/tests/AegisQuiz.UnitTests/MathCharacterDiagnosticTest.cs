using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.Infrastructure.Services.Docx;
using Xunit;
using Xunit.Abstractions;

namespace AegisQuiz.UnitTests
{
    public class MathCharacterDiagnosticTest
    {
        private readonly ITestOutputHelper _output;

        public MathCharacterDiagnosticTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DiagnoseMathCharacters_InRealDocxFiles()
        {
            var testFiles = new[]
            {
                @"D:\Cuong\DuAn\mybank\DocPDF\DeToan.docx",
                @"D:\Cuong\DuAn\mybank\2026-TNPT\thuvienhoclieu.com-De-thi-thu-TN-THPT-2026-Toan-Lien-Truong-Bac-Ninh-Lan-1-.docx",
                @"D:\Cuong\DuAn\mybank\DocPDF\DeToanGiaiChiTiet.docx"
            };

            var parser = new DocxParserService();

            foreach (var filePath in testFiles)
            {
                if (!File.Exists(filePath)) continue;

                _output.WriteLine($"==================================================");
                _output.WriteLine($"PHÂN TÍCH TỆP: {Path.GetFileName(filePath)}");
                _output.WriteLine($"==================================================");

                using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var questions = parser.ParseDocxFile(fs);

                _output.WriteLine($"Tổng số câu hỏi bóc tách: {questions.Count}");

                for (int i = 0; i < questions.Count; i++)
                {
                    var q = questions[i];
                    var allText = q.Content + " " + string.Join(" ", q.Options) + " " + q.AiExplanation;

                    // Tách các ảnh SVG ra để kiểm tra nội dung text bên trong
                    var svgMatches = Regex.Matches(allText, @"data:image/svg\+xml;base64,([A-Za-z0-9+/=]+)");
                    var svgDecodedTexts = new System.Text.StringBuilder();
                    foreach (Match sm in svgMatches)
                    {
                        try
                        {
                            var rawSvg = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(sm.Groups[1].Value));
                            var textNodes = Regex.Matches(rawSvg, @"<text[^>]*>([\s\S]*?)</text>");
                            foreach (Match tn in textNodes)
                            {
                                svgDecodedTexts.Append(" " + tn.Groups[1].Value);
                            }
                        }
                        catch { }
                    }

                    // Lọc bỏ toàn bộ Data URI dạng base64 ra khỏi text thuần trước khi quét ký tự khả nghi
                    var pureText = Regex.Replace(allText, @"data:image\/[a-zA-Z\+\-]+;base64,[A-Za-z0-9+/=]+", " ") + " " + svgDecodedTexts.ToString();

                    // Kiểm tra các ký tự lỗi kinh điển của MathType Symbol font:
                    // ¢ (0xA2 -> prime), ò (0xF2 -> integral), Å (0xC5 -> perp), Î (0xCE -> in), Ï (0xCF -> notin),
                    // ¥ (0xA5 -> infty), £ (0xA3 -> le), ³ (0xB3 -> ge), ¹ (0xB9 -> ne), ® (0xAE -> rightarrow),
                    // Þ (0xDE -> implies), Ö (0xD6 -> sqrt), uur (MT Extra arrow vector chưa ghép)
                    var suspiciousMatches = Regex.Matches(pureText, @"[\u00A2¢\u00A3£\u00B9\u00A5¥\u00CEÎ\u00CFÏ\u00CCÌ\u00CD\u00C8È\u00C7Ç\u00C6Æ\u00C5Å\u00AE®\u00AC¬\u00DEÞ\u00DB\u00DC\u00DAÚ\u00D6Ö\u00E5å\u00D5Õ]|\bu{1,4}r\b");

                    bool hasSvg = svgMatches.Count > 0;
                    bool hasSuspicious = suspiciousMatches.Count > 0;

                    if (hasSuspicious || i < 3)
                    {
                        _output.WriteLine($"[Câu {i + 1}] Loại: {q.QuestionType} | Số phương án: {q.Options.Count}");
                        _output.WriteLine($"  Nội dung tóm tắt: {q.Content.Substring(0, Math.Min(120, q.Content.Length))}...");
                        
                        if (suspiciousMatches.Count > 0)
                        {
                            var distinctSuspicious = suspiciousMatches.Cast<Match>().Select(m => m.Value).Distinct();
                            _output.WriteLine($"  >>> PHÁT HIỆN KÝ TỰ KHẢ NGHI: {string.Join(", ", distinctSuspicious)}");
                        }

                        // Giải mã SVG để soi nội dung text bên trong SVG
                        foreach (Match sm in svgMatches.Take(2))
                        {
                            try
                            {
                                var rawSvg = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(sm.Groups[1].Value));
                                var textNodes = Regex.Matches(rawSvg, @"<text[^>]*>([\s\S]*?)</text>");
                                if (textNodes.Count > 0)
                                {
                                    var texts = textNodes.Cast<Match>().Select(m => m.Groups[1].Value.Trim()).Where(t => !string.IsNullOrEmpty(t));
                                    _output.WriteLine($"  [SVG Texts]: {string.Join(" | ", texts.Take(10))}");
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        [Fact]
        public void InspectDetailedSuspiciousQuestions()
        {
            var filePath = @"D:\Cuong\DuAn\mybank\DocPDF\DeToan.docx";
            if (!File.Exists(filePath)) return;

            var parser = new DocxParserService();
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var questions = parser.ParseDocxFile(fs);

            int[] checkIdx = new[] { 6 }; // Câu 7 (index 6)
            foreach (var idx in checkIdx)
            {
                var q = questions[idx];
                _output.WriteLine($"=================== CÂU {idx + 1} ===================");
                _output.WriteLine($"CONTENT: {q.Content}");
                _output.WriteLine($"OPTIONS:\n{string.Join("\n", q.Options)}");
                _output.WriteLine($"EXPLANATION: {q.AiExplanation}");
            }
        }

        [Fact]
        public void InspectQuestion10_InDeToan()
        {
            var filePath = @"D:\Cuong\DuAn\mybank\2026-TNPT\DeToan.docx";
            if (!File.Exists(filePath)) return;

            using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);
            var wmfEntries = archive.Entries
                .Where(e => e.FullName.EndsWith(".wmf", StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.Name.Length).ThenBy(e => e.Name)
                .ToList();

            foreach (var entry in wmfEntries.Take(20))
            {
                using var ms = new MemoryStream();
                using var s = entry.Open();
                s.CopyTo(ms);
                var b = ms.ToArray();
                MtefToLatexConverter.TryConvert(b, out var katex);
                MathTypeWmfEngine.TryExtractSimpleMathText(b, out var simple);
                _output.WriteLine($"{entry.Name} ({b.Length}B) -> MTEF: {katex} | Simple: {simple}");

                if (entry.Name == "image8.wmf" || entry.Name == "image13.wmf" || entry.Name == "image49.wmf")
                {
                    int tagIdx = -1;
                    var tagBytes = System.Text.Encoding.ASCII.GetBytes("Design Science, Inc.\0");
                    for (int i = 0; i <= b.Length - tagBytes.Length; i++)
                    {
                        bool match = true;
                        for (int j = 0; j < tagBytes.Length; j++)
                            if (b[i + j] != tagBytes[j]) { match = false; break; }
                        if (match) { tagIdx = i + tagBytes.Length; break; }
                    }
                    if (tagIdx >= 0)
                    {
                        var hex = BitConverter.ToString(b, tagIdx, b.Length - tagIdx);
                        _output.WriteLine($"  FULL MTEF HEX ({entry.Name}, len={b.Length - tagIdx}): {hex}");
                    }
                }
            }
        }

        [Fact]
        public void InspectArchitectQuestion_InDeToan()
        {
            var filePath = @"D:\Cuong\DuAn\mybank\2026-TNPT\DeToan.docx";
            if (!File.Exists(filePath)) return;

            var parser = new DocxParserService();
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var questions = parser.ParseDocxFile(fs);

            var q = questions.FirstOrDefault(x => x.Content.Contains("kiến trúc sư") || x.Content.Contains("cây cầu"));
            Assert.NotNull(q);

            _output.WriteLine("=================== CÂU HỎI KIẾN TRÚC SƯ ===================");
            _output.WriteLine($"CONTENT: {q.Content}");
            _output.WriteLine($"RAW IMAGE TOKENS:");
            var matches = Regex.Matches(q.Content, @"data:image\/[^\s""]+");
            foreach (Match m in matches)
            {
                var val = m.Value;
                _output.WriteLine($"TOKEN: {val.Substring(0, Math.Min(60, val.Length))}... (len: {val.Length})");
                if (val.StartsWith("data:image/svg+xml;base64,"))
                {
                    try
                    {
                        var b64 = val.Replace("data:image/svg+xml;base64,", "");
                        var decodedSvg = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                        _output.WriteLine($"  DECODED SVG:\n{decodedSvg}");
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"  SVG ERROR: {ex.Message}");
                    }
                }
            }
        }

        [Fact]
        public void VerifyWmfSimplification_RatioAndQuality()
        {
            var filePath = @"D:\Cuong\DuAn\mybank\2026-TNPT\DeToan.docx";
            if (!File.Exists(filePath)) return;

            using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);
            var wmfEntries = archive.Entries.Where(e => e.FullName.EndsWith(".wmf", StringComparison.OrdinalIgnoreCase)).ToList();

            int simplifiedCount = 0;
            int complexCount = 0;

            _output.WriteLine($"==================================================");
            _output.WriteLine($"KIỂM NGHIỆM ĐỘNG CƠ RÚT GỌN WMF: Tổng {wmfEntries.Count} tệp WMF");
            _output.WriteLine($"==================================================");

            foreach (var entry in wmfEntries)
            {
                using var ms = new MemoryStream();
                using var entryStream = entry.Open();
                entryStream.CopyTo(ms);
                var bytes = ms.ToArray();

                if (MathTypeWmfEngine.TryExtractSimpleMathText(bytes, out var simpleLatex))
                {
                    simplifiedCount++;
                    if (simplifiedCount <= 10 || simplifiedCount % 10 == 0)
                    {
                        _output.WriteLine($"[RÚT GỌN THÀNH CÔNG] {entry.Name} -> {simpleLatex}");
                    }
                }
                else
                {
                    complexCount++;
                }
            }

            double ratio = (double)simplifiedCount / wmfEntries.Count * 100.0;
            _output.WriteLine($"--------------------------------------------------");
            _output.WriteLine($"KẾT QUẢ ĐỘT PHÁ TỐI ƯU HÓA:");
            _output.WriteLine($"- Số WMF đơn giản chuyển thành Text/LaTeX: {simplifiedCount} ({ratio:F1}%)");
            _output.WriteLine($"- Số WMF phức tạp giữ nguyên Vector SVG:   {complexCount} ({100.0 - ratio:F1}%)");
            _output.WriteLine($"--------------------------------------------------");

            Assert.True(simplifiedCount > 0, "Động cơ phải đơn giản hóa được ít nhất một số lượng WMF đáng kể.");
            Assert.True(ratio >= 50.0, "Tỷ lệ đơn giản hóa WMF trên đề chuẩn phải đạt tối thiểu 50% để tối ưu tài nguyên hệ thống.");
        }

        [Fact]
        public void InspectFailedImages()
        {
            var filePath = @"D:\Cuong\DuAn\mybank\2026-TNPT\DeToan.docx";
            if (!File.Exists(filePath)) return;

            using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);

            // 1. Trường hợp 1: Căn thức 3\sqrt{2}-2 (image87.wmf)
            var entry87 = archive.GetEntry("word/media/image87.wmf");
            if (entry87 != null)
            {
                using var ms = new MemoryStream();
                using var s = entry87.Open();
                s.CopyTo(ms);
                var b = ms.ToArray();
                bool sim = MathTypeWmfEngine.TryExtractSimpleMathText(b, out var txt);
                _output.WriteLine($"[CASE 1 - RADICAL] image87.wmf: Sim={sim}, Text='{txt}'");
                Assert.True(sim, "Căn thức image87.wmf phải đơn giản hóa thành công");
                Assert.Contains("\\sqrt{2}", txt);
            }

            // 2. Trường hợp 2: Hàm số phân thức y = (2x-4)/(x-1) (image17.wmf)
            var entry17 = archive.GetEntry("word/media/image17.wmf");
            if (entry17 != null)
            {
                using var ms = new MemoryStream();
                using var s = entry17.Open();
                s.CopyTo(ms);
                var b = ms.ToArray();
                bool sim = MathTypeWmfEngine.TryExtractSimpleMathText(b, out var txt);
                _output.WriteLine($"[CASE 2 - FRACTION EQUATION] image17.wmf: Sim={sim}, Text='{txt}'");
                Assert.True(sim, "Phân thức image17.wmf phải đơn giản hóa thành công");
                Assert.Contains("2x-4", txt);
                Assert.Contains("x-1", txt);
            }

            // 3. Trường hợp 3: Phân số xác suất 1/1786 (image75.wmf)
            var entry75 = archive.GetEntry("word/media/image75.wmf");
            if (entry75 != null)
            {
                using var ms = new MemoryStream();
                using var s = entry75.Open();
                s.CopyTo(ms);
                var b = ms.ToArray();
                bool sim = MathTypeWmfEngine.TryExtractSimpleMathText(b, out var txt);
                _output.WriteLine($"[CASE 3 - PROBABILITY FRACTION] image75.wmf: Sim={sim}, Text='{txt}'");
                Assert.True(sim, "Phân số xác suất image75.wmf phải đơn giản hóa thành công");
                Assert.Contains("1786", txt);
            }

            // 3b. Trường hợp vector a = 2k + i - 3j (image49.wmf)
            var entry49 = archive.GetEntry("word/media/image49.wmf");
            if (entry49 != null)
            {
                using var ms = new MemoryStream();
                using var s = entry49.Open();
                s.CopyTo(ms);
                var b = ms.ToArray();
                bool sim = MathTypeWmfEngine.TryExtractSimpleMathText(b, out var txt);
                string svg = MathTypeWmfEngine.PureWmfToSvg(b);
                _output.WriteLine($"[CASE VECTOR a=2k+i-3j] image49.wmf: Sim={sim}, Text='{txt}'");
                _output.WriteLine($"[SVG image49]: {svg.Substring(0, Math.Min(300, svg.Length))}");
                Assert.True(sim, "Công thức vector image49.wmf phải đơn giản hóa thành công");
                Assert.True(txt.Contains("\\vec{a}") || txt.Contains("\\overrightarrow{a}"), $"Expected \\vec{{a}} or \\overrightarrow{{a}} but got: {txt}");
                Assert.True(txt.Contains("\\vec{k}") || txt.Contains("\\overrightarrow{k}"), $"Expected \\vec{{k}} or \\overrightarrow{{k}} but got: {txt}");
            }

            // 3c. Trường hợp G(3; 8/3; 3) (image99.wmf)
            var entry99 = archive.GetEntry("word/media/image99.wmf");
            if (entry99 != null)
            {
                using var ms = new MemoryStream();
                using var s = entry99.Open();
                s.CopyTo(ms);
                var b = ms.ToArray();
                bool sim = MathTypeWmfEngine.TryExtractSimpleMathText(b, out var txt);
                string svg = MathTypeWmfEngine.PureWmfToSvg(b);
                _output.WriteLine($"[CASE G(3; 8/3; 3)] image99.wmf: Sim={sim}, Text='{txt}'");
                _output.WriteLine($"[SVG image99]: {svg}");
                Assert.True(sim, "Công thức ngoặc cao image99.wmf phải đơn giản hóa thành công");
                Assert.True(txt.Contains("G(3;\\frac{8}{3};3)") || txt.Contains(@"G\left(3;\frac{8}{3};3\right)"), $"Expected G(3;8/3;3) but got: {txt}");
                Assert.Contains("class=\"math-delimiter\"", svg);
            }

            // 3d. Trường hợp căn thức phân số a\sqrt{b}/c (image122.wmf)
            var entry122 = archive.GetEntry("word/media/image122.wmf");
            if (entry122 != null)
            {
                using var ms = new MemoryStream();
                using var s = entry122.Open();
                s.CopyTo(ms);
                var b = ms.ToArray();
                bool sim = MathTypeWmfEngine.TryExtractSimpleMathText(b, out var txt);
                string svg = MathTypeWmfEngine.PureWmfToSvg(b);
                _output.WriteLine($"[CASE a*sqrt(b)/c dm^3] image122.wmf: Sim={sim}, Text='{txt}'");
                _output.WriteLine($"[SVG image122]: {svg}");
                Assert.Contains("class=\"math-shape\"", svg);
            }

            // 4. Trường hợp 4: Toàn bộ đề thi sau khi parse qua DocxParserService
            var parser = new DocxParserService();
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var questions = parser.ParseDocxFile(fs);

            var qTiLe = questions.FirstOrDefault(q => q.Content.Contains("tiệm cận đứng"));
            if (qTiLe != null)
            {
                _output.WriteLine($"[DOCX PARSED - Tiệm cận đứng]: {qTiLe.Content}");
            }

            var qBi = questions.FirstOrDefault(q => q.Content.Contains("viên bi"));
            if (qBi != null)
            {
                _output.WriteLine($"[DOCX PARSED - Viên bi c)]: {qBi.Options.FirstOrDefault(o => o.Contains("toàn màu đỏ"))}");
            }
        }
    }
}

