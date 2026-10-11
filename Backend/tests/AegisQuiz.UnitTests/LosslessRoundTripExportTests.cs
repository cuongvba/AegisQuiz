using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using NPOI.XSSF.UserModel;
using Xunit;
using Xunit.Abstractions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Excel;
using AegisQuiz.Infrastructure.Excel.Strategies;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.Infrastructure.Services.Docx;

namespace AegisQuiz.UnitTests
{
    public class LosslessRoundTripExportTests
    {
        private readonly ITestOutputHelper _output;

        public LosslessRoundTripExportTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestRoundTripDocxToExcelAndBackToPreview_PreservesAll40QuestionsAndExplanations()
        {
            var docxPath = @"D:\Cuong\DuAn\mybank\DocPDF\DeTiengAnhChiTiet.docx";
            if (!File.Exists(docxPath)) return;

            // BƯỚC 1: Bóc tách file Word gốc DeTiengAnhChiTiet.docx
            using var fs = new FileStream(docxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: true);
            var rels = DocxRelationshipReader.LoadRelationships(archive);
            var media = DocxRelationshipReader.LoadMediaFiles(archive);
            var entry = archive.GetEntry("word/document.xml");
            Assert.NotNull(entry);
            using var ds = entry.Open();
            var doc = XDocument.Load(ds);

            var ansKeyMap = DocxTableQuestionExtractor.ExtractAnswerTables(doc, out var ansTableEls);
            var method = typeof(DocxParserService).GetMethod("ExtractParagraphTokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);
            var paras = (List<string>)method.Invoke(null, new object[] { doc, rels, media, ansTableEls })!;

            var originalQuestions = DocxStateMachineClassifier.ParseParagraphList(paras, ansKeyMap);
            _output.WriteLine($"[1. Word Parse] Total parsed questions: {originalQuestions.Count}");
            Assert.Equal(40, originalQuestions.Count);

            // BƯỚC 2: Xuất bản sang bảng tính Excel Native Isomorphic qua UniversalExcelExportService
            var exportService = new UniversalExcelExportService(new[] { new NativeIsomorphicExportStrategy() });
            var options = new ExcelExportOptionsDto
            {
                ExamTitle = "ĐỀ THI TIẾNG ANH CHI TIẾT 2026",
                IssuingOrg = "Bộ Giáo Dục & Khảo Thí AegisQuiz",
                BenchmarkYear = 2026,
                BenchmarkStandard = "Chuẩn Khảo Thí Quốc Gia GDPT 2025",
                TargetLevel = "Lớp 12",
                AssessmentPurpose = "Kỳ thi Tốt nghiệp THPT Quốc Gia",
                DomainCode = "EDUCATION",
                DefaultTopicCode = "TIENG_ANH",
                IncludeExplanation = true
            };

            byte[] excelBytes = exportService.Export(originalQuestions, "NATIVE_AEGIS", options);
            Assert.NotNull(excelBytes);
            Assert.True(excelBytes.Length > 1000, "File Excel sinh ra phải có dung lượng hợp lệ.");
            _output.WriteLine($"[2. Excel Export] Generated Excel file size: {excelBytes.Length} bytes");

            // BƯỚC 3: Dùng chính ExcelParserService của hệ thống nạp ngược lại file Excel vừa tạo (Round-Trip)
            var excelParser = new ExcelParserService(null!);
            using var excelStream = new MemoryStream(excelBytes);
            var previewResult = excelParser.ParseExcelFileToPreview(excelStream, "RoundTripTest.xlsx");

            _output.WriteLine($"[3. Excel Re-Import] Total sheets: {previewResult.TotalSheets}, Total restored questions: {previewResult.TotalQuestions}");
            _output.WriteLine($"[3. Excel Re-Import] Detected Domain: {previewResult.DetectedDomainCode}, TargetLevel: {previewResult.DetectedTargetLevel}, Year: {previewResult.DetectedBenchmarkYear}");

            // BƯỚC 4: Kiểm chứng đẳng cấu toán học (Lossless Round-Trip Isomorphism)
            Assert.Equal(40, previewResult.TotalQuestions);
            Assert.Single(previewResult.Sheets);
            var restoredQuestions = previewResult.Sheets[0].Questions;
            Assert.Equal(40, restoredQuestions.Count);

            for (int i = 0; i < originalQuestions.Count; i++)
            {
                var orig = originalQuestions[i];
                var rest = restoredQuestions[i];

                _output.WriteLine($"Q[{i + 1}] Orig: Ans='{orig.SuggestedAnswer}', ExplLen={orig.AiExplanation?.Length ?? 0} | Restored: Ans='{rest.SuggestedAnswer}', ExplLen={rest.AiExplanation?.Length ?? 0}");

                // 1. Số thứ tự câu hỏi khớp
                Assert.Equal(orig.SourceQuestionNo, rest.SourceQuestionNo);

                // 2. Nội dung câu hỏi được bảo toàn
                Assert.Equal(orig.Content, rest.Content);

                // 3. Toàn bộ 40 câu hỏi đều có lời giải chi tiết phục hồi từ Excel giống câu gốc
                Assert.Equal(orig.AiExplanation ?? "", rest.AiExplanation ?? "");

                // 4. Đáp án gợi ý phục hồi nguyên vẹn giống câu gốc
                Assert.Equal(orig.SuggestedAnswer ?? "", rest.SuggestedAnswer ?? "");

                // 5. Số lượng phương án lựa chọn được bảo toàn
                if (orig.Options != null && orig.Options.Count > 0)
                {
                    Assert.True(rest.Options != null && rest.Options.Count >= 2, $"Question {i + 1} must preserve options list");
                }

                // 6. Ngữ cảnh bài đọc hiểu được bảo toàn nếu câu gốc có bài đọc
                if (!string.IsNullOrWhiteSpace(orig.ContextContent))
                {
                    Assert.False(string.IsNullOrWhiteSpace(rest.ContextContent), $"Question {i + 1} must preserve Reading Passage ContextContent");
                }
            }

            _output.WriteLine(">>> CHÚC MỪNG: KIỂM CHỨNG ĐẲNG CẤU CHU TRÌNH KHÉP KÍN (LOSSLESS ROUND-TRIP) THÀNH CÔNG 100%! <<<");
        }

        [Fact]
        public void TestExportService_ProvidesMultipleStrategies_AndProducesValidWorkbooks()
        {
            var strategies = new IExcelExportStrategy[]
            {
                new NativeIsomorphicExportStrategy(),
                new MoetGdpt2025ExportStrategy(),
                new AzotaCompatibleExportStrategy()
            };

            var service = new UniversalExcelExportService(strategies);
            var templates = service.GetAvailableTemplates();

            Assert.Equal(4, templates.Count);
            Assert.Contains(templates, t => t.TemplateCode == "NATIVE_AEGIS");
            Assert.Contains(templates, t => t.TemplateCode == "GDPT_2025");
            Assert.Contains(templates, t => t.TemplateCode == "AZOTA");
            Assert.Contains(templates, t => t.TemplateCode == "CUSTOM_BYOT");

            // Mock danh sách 3 câu hỏi đa hình thái: 1 SINGLE, 1 TRUE_FALSE, 1 SHORT_ANSWER
            var mockQuestions = new List<DocxImportPreviewDto>
            {
                new()
                {
                    SourceQuestionNo = "1",
                    Content = "Tìm nghiệm của phương trình x + 1 = 2",
                    QuestionType = "SINGLE",
                    Options = new List<string> { "x = 1", "x = 2", "x = 3", "x = 0" },
                    SuggestedAnswer = "1",
                    AiExplanation = "x = 2 - 1 = 1. Chọn A"
                },
                new()
                {
                    SourceQuestionNo = "2",
                    Content = "Các mệnh đề sau đây về tam giác vuông là đúng hay sai?",
                    QuestionType = "TRUE_FALSE",
                    Options = new List<string> { "a) Định lý Pytago luôn đúng", "b) Có 2 góc vuông", "c) Cạnh huyền dài nhất", "d) Có 1 góc 90 độ" },
                    SuggestedAnswer = "a:Đ, b:S, c:Đ, d:Đ",
                    AiExplanation = "Tam giác vuông chỉ có đúng 1 góc 90 độ."
                },
                new()
                {
                    SourceQuestionNo = "3",
                    Content = "Tính giới hạn lim(1/n) khi n tiến ra vô cực",
                    QuestionType = "SHORT_ANSWER",
                    SuggestedAnswer = "0",
                    AiExplanation = "Giới hạn bằng 0."
                }
            };

            var options = new ExcelExportOptionsDto { ExamTitle = "ĐỀ THI KIỂM THỬ ĐA MẪU" };

            // 1. Kiểm tra xuất NATIVE_AEGIS
            var nativeBytes = service.Export(mockQuestions, "NATIVE_AEGIS", options);
            Assert.NotNull(nativeBytes);
            Assert.True(nativeBytes.Length > 1000);

            // 2. Kiểm tra xuất GDPT_2025 (phải có 3 sheet tương ứng)
            var gdptBytes = service.Export(mockQuestions, "GDPT_2025", options);
            Assert.NotNull(gdptBytes);
            using var gdptWb = new XSSFWorkbook(new MemoryStream(gdptBytes));
            Assert.Equal(3, gdptWb.NumberOfSheets);
            Assert.Equal("Phan_I_TracNghiem", gdptWb.GetSheetAt(0).SheetName);
            Assert.Equal("Phan_II_DungSai", gdptWb.GetSheetAt(1).SheetName);
            Assert.Equal("Phan_III_TraLoiNgan", gdptWb.GetSheetAt(2).SheetName);

            // 3. Kiểm tra xuất AZOTA
            var azotaBytes = service.Export(mockQuestions, "AZOTA", options);
            Assert.NotNull(azotaBytes);
            using var azotaWb = new XSSFWorkbook(new MemoryStream(azotaBytes));
            Assert.Equal(1, azotaWb.NumberOfSheets);
            Assert.Equal("DeThiAzota", azotaWb.GetSheetAt(0).SheetName);

            _output.WriteLine(">>> TẤT CẢ 3 CHIẾN LƯỢC XUẤT EXCEL ĐỀU HOẠT ĐỘNG HOÀN HẢO! <<<");
        }

        [Fact]
        public void TestExportDeToan_WithImages_DoesNotThrow()
        {
            var filePath = File.Exists(@"D:\Cuong\DuAn\mybank\DocPDF\DeToan.docx")
                ? @"D:\Cuong\DuAn\mybank\DocPDF\DeToan.docx"
                : @"D:\Cuong\DuAn\mybank\2026-TNPT\DeToan.docx";

            if (!File.Exists(filePath)) return;

            var docxService = new DocxParserService();
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var questions = docxService.ParseDocxFile(fs);

            var exportService = new UniversalExcelExportService(new IExcelExportStrategy[]
            {
                new NativeIsomorphicExportStrategy(),
                new MoetGdpt2025ExportStrategy(),
                new AzotaCompatibleExportStrategy(),
                new CustomTemplateExportStrategy()
            });

            var options = new ExcelExportOptionsDto { ExamTitle = "ĐỀ TOÁN TEST", IncludeImages = true, IncludeExplanation = true };

            // Thử xuất cả 4 định dạng
            var nativeBytes = exportService.Export(questions, "NATIVE_AEGIS", options);
            Assert.NotNull(nativeBytes);

            var gdptBytes = exportService.Export(questions, "GDPT_2025", options);
            Assert.NotNull(gdptBytes);

            var azotaBytes = exportService.Export(questions, "AZOTA", options);
            Assert.NotNull(azotaBytes);

            var customStarterBytes = exportService.Export(questions, "CUSTOM_BYOT", options);
            Assert.NotNull(customStarterBytes);

            // Thử nạp lại chính customStarterBytes làm template mẫu người dùng tải lên
            var byotOptions = new ExcelExportOptionsDto
            {
                ExamTitle = "ĐỀ TOÁN THEO MẪU TÙY BIẾN",
                CustomTemplateBytes = customStarterBytes
            };
            var customFilledBytes = exportService.Export(questions, "CUSTOM_BYOT", byotOptions);
            Assert.NotNull(customFilledBytes);
            Assert.True(customFilledBytes.Length > 1000);
        }

        [Fact]
        public void TestEnglishReadingPassage_MergesConsecutiveRows_AndRendersFullContentOnce()
        {
            var passageTitle = "Reading Passage 1: Artificial Intelligence in Education";
            var passageContent = "Artificial intelligence (AI) is transforming modern classrooms by providing personalized learning pathways...";

            var mockQuestions = new List<DocxImportPreviewDto>
            {
                new()
                {
                    SourceQuestionNo = "1",
                    Content = "What is the main topic of the passage?",
                    QuestionType = "SINGLE",
                    Options = new List<string> { "AI in education", "Space travel", "Cooking techniques", "History of cars" },
                    SuggestedAnswer = "1",
                    ContextTitle = passageTitle,
                    ContextContent = passageContent
                },
                new()
                {
                    SourceQuestionNo = "2",
                    Content = "According to paragraph 2, how does AI help students?",
                    QuestionType = "SINGLE",
                    Options = new List<string> { "By personalizing learning", "By taking tests for them", "By replacing teachers entirely", "None" },
                    SuggestedAnswer = "1",
                    ContextTitle = passageTitle,
                    ContextContent = passageContent
                },
                new()
                {
                    SourceQuestionNo = "3",
                    Content = "Which word is closest in meaning to 'transforming'?",
                    QuestionType = "SINGLE",
                    Options = new List<string> { "Changing", "Stopping", "Watching", "Ignoring" },
                    SuggestedAnswer = "1",
                    ContextTitle = passageTitle,
                    ContextContent = passageContent
                },
                new()
                {
                    SourceQuestionNo = "4",
                    Content = "A standalone grammar question without passage.",
                    QuestionType = "SINGLE",
                    Options = new List<string> { "Option A", "Option B", "Option C", "Option D" },
                    SuggestedAnswer = "2"
                }
            };

            var exportStrategy = new NativeIsomorphicExportStrategy();
            var options = new ExcelExportOptionsDto { ExamTitle = "English Reading Test 2026" };
            byte[] excelBytes = exportStrategy.Export(mockQuestions, options);

            using var wb = new XSSFWorkbook(new MemoryStream(excelBytes));
            var sheet = wb.GetSheetAt(0);

            // Kiểm tra có Merged Region trên Cột 9 (Bài đọc hiểu)
            Assert.True(sheet.NumMergedRegions >= 2, "Phải có ít nhất 2 merged regions (Cột 9 và Cột 10)");

            // Tìm region thuộc cột 9
            bool foundPassageMergedRegion = false;
            for (int i = 0; i < sheet.NumMergedRegions; i++)
            {
                var reg = sheet.GetMergedRegion(i);
                if (reg.FirstColumn == 9 && reg.LastColumn == 9)
                {
                    Assert.Equal(5, reg.FirstRow);
                    Assert.Equal(7, reg.LastRow);
                    foundPassageMergedRegion = true;
                    break;
                }
            }
            Assert.True(foundPassageMergedRegion, "Cột bài đọc hiểu của 3 câu đầu tiên phải được merge liền nhau từ dòng 5 đến 7");

            // Kiểm tra nội dung ô đầu tiên (Hàng 5) chứa đầy đủ Tiêu đề và Nội dung
            var cellFirst = sheet.GetRow(5).GetCell(9);
            Assert.NotNull(cellFirst);
            Assert.Contains("Artificial Intelligence in Education", cellFirst.StringCellValue);
            Assert.Contains("transforming modern classrooms", cellFirst.StringCellValue);

            // Kiểm tra các hàng tiếp theo trong block (Hàng 6, 7) để trống
            var cellSecond = sheet.GetRow(6).GetCell(9);
            Assert.True(string.IsNullOrWhiteSpace(cellSecond?.StringCellValue), "Hàng tiếp theo trong nhóm merge phải để trống");

            // Nạp lại bằng ExcelParserService để chứng minh Lossless Round-Trip
            var parser = new ExcelParserService(null!);
            var preview = parser.ParseExcelFileToPreview(new MemoryStream(excelBytes), "EnglishTest.xlsx");
            Assert.Equal(4, preview.TotalQuestions);
            var restored = preview.Sheets[0].Questions;
            Assert.False(string.IsNullOrWhiteSpace(restored[0].ContextContent), "Câu 1 phải phục hồi được bài đọc");
            Assert.False(string.IsNullOrWhiteSpace(restored[1].ContextContent), "Câu 2 phải phục hồi được bài đọc");
            Assert.False(string.IsNullOrWhiteSpace(restored[2].ContextContent), "Câu 3 phải phục hồi được bài đọc");
            Assert.True(string.IsNullOrWhiteSpace(restored[3].ContextContent), "Câu 4 không có bài đọc");
        }

        [Fact]
        public void TestExportOptionOrderingSequence_DoesNotStripLeadingLetter()
        {
            var testQuestion = new DocxImportPreviewDto
            {
                SourceQuestionNo = "12",
                Content = "Sắp xếp các câu sau thành đoạn văn hoàn chỉnh:",
                QuestionType = "SINGLE",
                SuggestedAnswer = "1",
                Options = new List<string>
                {
                    "a - c - d - b - e",
                    "c – e – a – d – b",
                    "b - a - d - c",
                    "d - e - a - b - c"
                }
            };

            var questions = new List<DocxImportPreviewDto> { testQuestion };
            var options = new ExcelExportOptionsDto { ExamTitle = "Test Ordering" };

            // 1. NativeIsomorphicExportStrategy
            var nativeStrategy = new NativeIsomorphicExportStrategy();
            var nativeBytes = nativeStrategy.Export(questions, options);
            using (var ms = new MemoryStream(nativeBytes))
            {
                var wb = new XSSFWorkbook(ms);
                var sheet = wb.GetSheetAt(0);
                var row = sheet.GetRow(5); // row 5 là câu hỏi đầu tiên
                string optA = row.GetCell(3)?.StringCellValue ?? ""; // Cột 3 là Option A
                string optB = row.GetCell(4)?.StringCellValue ?? ""; // Cột 4 là Option B
                Assert.StartsWith("a - c", optA);
                Assert.StartsWith("c – e", optB);
            }

            // 2. MoetGdpt2025ExportStrategy
            var moetStrategy = new MoetGdpt2025ExportStrategy();
            var moetBytes = moetStrategy.Export(questions, options);
            using (var ms = new MemoryStream(moetBytes))
            {
                var wb = new XSSFWorkbook(ms);
                var sheet = wb.GetSheetAt(0);
                // Tìm hàng có câu 12
                NPOI.SS.UserModel.IRow? targetRow = null;
                for (int r = 0; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row?.GetCell(0)?.StringCellValue == "12")
                    {
                        targetRow = row;
                        break;
                    }
                }
                Assert.NotNull(targetRow);
                string optA = targetRow.GetCell(4)?.StringCellValue ?? ""; // Cột 4 là Option A
                Assert.StartsWith("a - c", optA);
            }

            // 3. AzotaCompatibleExportStrategy
            var azotaStrategy = new AzotaCompatibleExportStrategy();
            var azotaBytes = azotaStrategy.Export(questions, options);
            using (var ms = new MemoryStream(azotaBytes))
            {
                var wb = new XSSFWorkbook(ms);
                var sheet = wb.GetSheetAt(0);
                var row = sheet.GetRow(1);
                string optA = row.GetCell(2)?.StringCellValue ?? "";
                Assert.StartsWith("a - c", optA);
            }
        }
    }
}
