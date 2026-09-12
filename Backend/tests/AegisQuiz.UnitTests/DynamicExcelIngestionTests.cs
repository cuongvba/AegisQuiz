using System;
using System.IO;
using System.Linq;
using System.Text;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Excel;
using AegisQuiz.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace AegisQuiz.UnitTests
{
    public class DynamicExcelIngestionTests
    {
        private readonly ITestOutputHelper _output;

        public DynamicExcelIngestionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private static MemoryStream CreateWorkbookStream(Action<ISheet> populate)
        {
            using var wb = new XSSFWorkbook();
            var sheet = wb.CreateSheet("Tín dụng KHDN");
            populate(sheet);
            using var tempMs = new MemoryStream();
            wb.Write(tempMs, leaveOpen: true);
            var bytes = tempMs.ToArray();
            return new MemoryStream(bytes);
        }

        [Fact]
        public void StandardDot2026_WithPreHeaderRows_ShouldDetectHeaderAndExtractQuestions()
        {
            using var stream = CreateWorkbookStream(sheet =>
            {
                // Dòng 0-3: Tiêu đề cơ quan/hội đồng thi
                sheet.CreateRow(0).CreateCell(0).SetCellValue("NGÂN HÀNG NÔNG NGHIỆP VÀ PHÁT TRIỂN NÔNG THÔN VIỆT NAM");
                sheet.CreateRow(1).CreateCell(0).SetCellValue("HỘI ĐỒNG THI TUYỂN DỤNG LAO ĐỘNG NĂM 2026");
                sheet.CreateRow(2).CreateCell(0).SetCellValue("NGÂN HÀNG CÂU HỎI THI NGHIỆP VỤ TÍN DỤNG DOANH NGHIỆP");

                // Dòng 3: Header chuẩn
                var hRow = sheet.CreateRow(3);
                hRow.CreateCell(0).SetCellValue("STT");
                hRow.CreateCell(1).SetCellValue("CÂU HỎI");
                hRow.CreateCell(2).SetCellValue("ĐÁP ÁN 1");
                hRow.CreateCell(3).SetCellValue("ĐÁP ÁN 2");
                hRow.CreateCell(4).SetCellValue("ĐÁP ÁN 3");
                hRow.CreateCell(5).SetCellValue("ĐÁP ÁN 4");
                hRow.CreateCell(6).SetCellValue("ĐÁP ÁN ĐÚNG");
                hRow.CreateCell(7).SetCellValue("TRÍCH DẪN NGUỒN");

                // Dòng 4: Dữ liệu câu 1
                var dRow1 = sheet.CreateRow(4);
                dRow1.CreateCell(0).SetCellValue("1");
                dRow1.CreateCell(1).SetCellValue("Thời hạn cấp tín dụng trung hạn tối đa là bao nhiêu năm?");
                dRow1.CreateCell(2).SetCellValue("1 năm");
                dRow1.CreateCell(3).SetCellValue("3 năm");
                dRow1.CreateCell(4).SetCellValue("5 năm");
                dRow1.CreateCell(5).SetCellValue("10 năm");
                dRow1.CreateCell(6).SetCellValue("3");
                dRow1.CreateCell(7).SetCellValue("Luật Các tổ chức tín dụng 2024 Điều 4");

                // Dòng 5: Dữ liệu câu 2 (Đáp án bằng chữ C)
                var dRow2 = sheet.CreateRow(5);
                dRow2.CreateCell(0).SetCellValue("2");
                dRow2.CreateCell(1).SetCellValue("Thẩm quyền phê duyệt khoản vay vượt hạn mức thuộc về?");
                dRow2.CreateCell(2).SetCellValue("Trưởng phòng Tín dụng");
                dRow2.CreateCell(3).SetCellValue("Giám đốc Chi nhánh");
                dRow2.CreateCell(4).SetCellValue("Hội đồng Quản trị");
                dRow2.CreateCell(5).SetCellValue("Ban Kiểm soát");
                dRow2.CreateCell(6).SetCellValue("C");
                dRow2.CreateCell(7).SetCellValue("Quy chế cấp tín dụng Agribank");
            });

            var parser = new ExcelParserService(null!);
            var preview = parser.ParseExcelFileToPreview(stream, "1. Tín dụng KHDN.xlsx");

            Assert.Equal(1, preview.TotalSheets);
            Assert.Equal(2, preview.TotalQuestions);
            Assert.Contains("TD", preview.Sheets[0].DetectedTopicCode);

            var q1 = preview.Sheets[0].Questions[0];
            Assert.Equal("SINGLE", q1.QuestionType);
            Assert.Equal(4, q1.Options.Count);
            Assert.Equal("3", q1.SuggestedAnswer);
            Assert.Contains("Luật Các tổ chức tín dụng", q1.AiExplanation);

            var q2 = preview.Sheets[0].Questions[1];
            Assert.Equal("SINGLE", q2.QuestionType);
            Assert.Equal("3", q2.SuggestedAnswer); // 'C' ánh xạ sang index 3
        }

        [Fact]
        public void MultiSheet_WithNonQuizReferenceSheets_ShouldSkipNonQuizSheetsAndKeepOnlyRealQuestions()
        {
            using var stream = new MemoryStream();
            using (var wb = new XSSFWorkbook())
            {
                // Sheet 1: Câu hỏi thi trắc nghiệm
                var qSheet = wb.CreateSheet("Tín dụng KHDN");
                var hRow = qSheet.CreateRow(0);
                hRow.CreateCell(0).SetCellValue("STT");
                hRow.CreateCell(1).SetCellValue("CÂU HỎI");
                hRow.CreateCell(2).SetCellValue("ĐÁP ÁN 1");
                hRow.CreateCell(3).SetCellValue("ĐÁP ÁN 2");
                hRow.CreateCell(4).SetCellValue("ĐÁP ÁN 3");
                hRow.CreateCell(5).SetCellValue("ĐÁP ÁN 4");
                hRow.CreateCell(6).SetCellValue("ĐÁP ÁN ĐÚNG");

                for (int i = 1; i <= 5; i++)
                {
                    var dRow = qSheet.CreateRow(i);
                    dRow.CreateCell(0).SetCellValue(i);
                    dRow.CreateCell(1).SetCellValue($"Nội dung câu hỏi nghiệp vụ số {i}?");
                    dRow.CreateCell(2).SetCellValue($"Phương án A {i}");
                    dRow.CreateCell(3).SetCellValue($"Phương án B {i}");
                    dRow.CreateCell(4).SetCellValue($"Phương án C {i}");
                    dRow.CreateCell(5).SetCellValue($"Phương án D {i}");
                    dRow.CreateCell(6).SetCellValue("2");
                }

                // Sheet 2: Danh mục văn bản quy phạm pháp luật (phi đề thi)
                var docSheet = wb.CreateSheet("Văn bản");
                var docHRow = docSheet.CreateRow(0);
                docHRow.CreateCell(0).SetCellValue("STT");
                docHRow.CreateCell(1).SetCellValue("DANH MỤC VĂN BẢN THAM KHẢO");
                for (int i = 1; i <= 10; i++)
                {
                    var docRow = docSheet.CreateRow(i);
                    docRow.CreateCell(0).SetCellValue(i);
                    docRow.CreateCell(1).SetCellValue($"Luật Các tổ chức tín dụng số {i}/2024/QH15");
                }

                // Sheet 3: Hướng dẫn sử dụng / Quy chế (phi đề thi)
                var guideSheet = wb.CreateSheet("Hướng dẫn");
                var gRow = guideSheet.CreateRow(0);
                gRow.CreateCell(0).SetCellValue("Nội dung hướng dẫn ôn tập cho thí sinh");

                wb.Write(stream, leaveOpen: true);
            }
            stream.Position = 0;

            var parser = new ExcelParserService(null!);
            var preview = parser.ParseExcelFileToPreview(stream, "1. Tín dụng KHDN.xlsx");

            // Chỉ duy nhất Sheet 1 được nhận diện là đề thi, Sheet 2 và Sheet 3 chuyển sang SkippedSheets
            Assert.Equal(1, preview.Sheets.Count);
            Assert.Equal(2, preview.SkippedSheets.Count);
            Assert.Equal("Tín dụng KHDN", preview.Sheets[0].SheetName);
            Assert.Equal("EXAM_QUIZ", preview.Sheets[0].SheetRole);
            Assert.True(preview.Sheets[0].ConfidenceScore >= 0.9);
            Assert.Equal(5, preview.TotalQuestions);

            // Kiểm tra siêu dữ liệu thanh tra Studio (SampleRows, ColumnMapping, HealthStats)
            Assert.True(preview.Sheets[0].SampleRows.Count >= 5);
            Assert.True(preview.Sheets[0].ColumnMapping.ContainsKey("content"));
            Assert.True(preview.Sheets[0].ColumnMapping.ContainsKey("answer"));
            Assert.Equal(5, preview.Sheets[0].HealthStats.ValidQuestionCount);
            Assert.Equal(0, preview.Sheets[0].HealthStats.MissingAnswerCount);

            // Kiểm tra thông tin SkippedSheets
            Assert.Equal("Văn bản", preview.SkippedSheets[0].SheetName);
            Assert.Equal("REFERENCE_DOCS", preview.SkippedSheets[0].SuggestedRole);
            Assert.True(preview.SkippedSheets[0].SampleRows.Count >= 5);

            // Đảm bảo không có câu hỏi TỰ LUẬN (ESSAY) rác nào sinh ra từ sheet danh mục văn bản
            Assert.All(preview.Sheets[0].Questions, q =>
            {
                Assert.Equal("SINGLE", q.QuestionType);
                Assert.Equal("2", q.SuggestedAnswer);
                Assert.Equal(4, q.Options.Count);
            });
        }

        [Fact]
        public void NumericAnswer_NumericAndPrefixFormatting_ShouldMapDirectlyToIndex()
        {
            using var stream = CreateWorkbookStream(sheet =>
            {
                var hRow = sheet.CreateRow(0);
                hRow.CreateCell(0).SetCellValue("STT");
                hRow.CreateCell(1).SetCellValue("CÂU HỎI");
                hRow.CreateCell(2).SetCellValue("ĐÁP ÁN 1");
                hRow.CreateCell(3).SetCellValue("ĐÁP ÁN 2");
                hRow.CreateCell(4).SetCellValue("ĐÁP ÁN 3");
                hRow.CreateCell(5).SetCellValue("ĐÁP ÁN 4");
                hRow.CreateCell(6).SetCellValue("PHƯƠNG ÁN ĐÚNG");

                // Câu 1: Số double (2.0) trong ô numeric Excel
                var r1 = sheet.CreateRow(1);
                r1.CreateCell(0).SetCellValue(1);
                r1.CreateCell(1).SetCellValue("Câu hỏi 1 với số double trong Excel?");
                r1.CreateCell(2).SetCellValue("Lựa chọn 1");
                r1.CreateCell(3).SetCellValue("Lựa chọn 2");
                r1.CreateCell(4).SetCellValue("Lựa chọn 3");
                r1.CreateCell(5).SetCellValue("Lựa chọn 4");
                r1.CreateCell(6).SetCellValue(2.0); // Numeric cell

                // Câu 2: Text "Phương án 3"
                var r2 = sheet.CreateRow(2);
                r2.CreateCell(0).SetCellValue(2);
                r2.CreateCell(1).SetCellValue("Câu hỏi 2 với text có tiền tố?");
                r2.CreateCell(2).SetCellValue("Lựa chọn 1");
                r2.CreateCell(3).SetCellValue("Lựa chọn 2");
                r2.CreateCell(4).SetCellValue("Lựa chọn 3");
                r2.CreateCell(5).SetCellValue("Lựa chọn 4");
                r2.CreateCell(6).SetCellValue("Phương án 3");

                // Câu 3: Text "Đáp án B"
                var r3 = sheet.CreateRow(3);
                r3.CreateCell(0).SetCellValue(3);
                r3.CreateCell(1).SetCellValue("Câu hỏi 3 với chữ cái B?");
                r3.CreateCell(2).SetCellValue("Lựa chọn 1");
                r3.CreateCell(3).SetCellValue("Lựa chọn 2");
                r3.CreateCell(4).SetCellValue("Lựa chọn 3");
                r3.CreateCell(5).SetCellValue("Lựa chọn 4");
                r3.CreateCell(6).SetCellValue("Đáp án B");
            });

            var parser = new ExcelParserService(null!);
            var preview = parser.ParseExcelFileToPreview(stream, "NumericAnswerTest.xlsx");

            Assert.Equal(3, preview.TotalQuestions);
            Assert.Equal("SINGLE", preview.Sheets[0].Questions[0].QuestionType);
            Assert.Equal("2", preview.Sheets[0].Questions[0].SuggestedAnswer);

            Assert.Equal("SINGLE", preview.Sheets[0].Questions[1].QuestionType);
            Assert.Equal("3", preview.Sheets[0].Questions[1].SuggestedAnswer);

            Assert.Equal("SINGLE", preview.Sheets[0].Questions[2].QuestionType);
            Assert.Equal("2", preview.Sheets[0].Questions[2].SuggestedAnswer);
        }

        [Fact]
        public void RealDot2026File_ShouldSkipReferenceSheets_AndParseAccurately()
        {
            var dir = @"D:\Cuong\DuAn\mybank\AegisQuiz\DOT-2026";
            if (!Directory.Exists(dir)) return;

            var f1 = Path.Combine(dir, "1. Tín dụng KHDN.xlsx");
            if (!File.Exists(f1)) return;

            var parser = new ExcelParserService(null!);
            using var fs = new FileStream(f1, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var preview = parser.ParseExcelFileToPreview(fs, Path.GetFileName(f1));

            _output.WriteLine($"[Real File 1] Total Sheets parsed: {preview.Sheets.Count}, Total Questions: {preview.TotalQuestions}");
            foreach (var sh in preview.Sheets)
            {
                _output.WriteLine($"  -> Sheet: {sh.SheetName}, Questions: {sh.Questions.Count}");
            }

            // Sheet 2 "Văn bản" phải bị bỏ qua hoàn toàn, chỉ parse Sheet 1
            Assert.Equal(1, preview.Sheets.Count);
            Assert.Equal(240, preview.TotalQuestions);

            // Mọi câu hỏi đều phải là SINGLE với SuggestedAnswer là "1", "2", "3", "4"
            var essayCount = preview.Sheets[0].Questions.Count(q => q.QuestionType == "ESSAY");
            Assert.Equal(0, essayCount);

            var sampleQ = preview.Sheets[0].Questions[0];
            Assert.Equal("SINGLE", sampleQ.QuestionType);
            Assert.Contains(sampleQ.SuggestedAnswer, new[] { "1", "2", "3", "4" });
        }

        [Fact]
        public void Unstructured_SingleCell_ShouldDecomposeViaDynamicIngestionEngine()
        {
            using var stream = CreateWorkbookStream(sheet =>
            {
                var hRow = sheet.CreateRow(0);
                hRow.CreateCell(0).SetCellValue("STT");
                hRow.CreateCell(1).SetCellValue("NỘI DUNG CÂU HỎI VÀ PHƯƠNG ÁN");

                var dRow = sheet.CreateRow(1);
                dRow.CreateCell(0).SetCellValue("1");
                dRow.CreateCell(1).SetCellValue(
                    "Câu 1. Vốn tự có của Agribank bao gồm những thành phần nào?\n" +
                    "A. Vốn cấp 1 và Vốn cấp 2\n" +
                    "B. Vốn lưu động và Vốn cố định\n" +
                    "C. Vốn điều lệ và Vốn vay\n" +
                    "D. Vốn ủy thác đầu tư\n" +
                    "ĐÁP ÁN: A\n" +
                    "Giải thích: Theo Thông tư 41/2016/TT-NHNN quy định tỷ lệ an toàn vốn.");
            });

            var parser = new ExcelParserService(null!);
            var preview = parser.ParseExcelFileToPreview(stream, "UnstructuredQuiz.xlsx");

            Assert.True(preview.TotalQuestions >= 1);
            var q = preview.Sheets[0].Questions[0];
            _output.WriteLine($"[Unstructured] Content: {q.Content}");
            _output.WriteLine($"[Unstructured] Options: {string.Join(" | ", q.Options)}");
            _output.WriteLine($"[Unstructured] Answer: {q.SuggestedAnswer}");

            Assert.Contains("Vốn tự có của Agribank", q.Content);
            Assert.True(q.Options.Count >= 2);
            Assert.Equal("1", q.SuggestedAnswer);
        }

        [Fact]
        public void Asterisk_MarkedCorrectOption_ShouldAutoDetectAnswerKeyAndCleanOption()
        {
            using var stream = CreateWorkbookStream(sheet =>
            {
                var hRow = sheet.CreateRow(0);
                hRow.CreateCell(0).SetCellValue("STT");
                hRow.CreateCell(1).SetCellValue("CÂU HỎI");
                hRow.CreateCell(2).SetCellValue("A");
                hRow.CreateCell(3).SetCellValue("B");
                hRow.CreateCell(4).SetCellValue("C");
                hRow.CreateCell(5).SetCellValue("D");
                hRow.CreateCell(6).SetCellValue("ĐÁP ÁN ĐÚNG"); // Để trống ô đáp án

                var dRow = sheet.CreateRow(1);
                dRow.CreateCell(0).SetCellValue("1");
                dRow.CreateCell(1).SetCellValue("Biện pháp bảo đảm tiền vay nào dưới đây là biện pháp bảo đảm bằng tài sản?");
                dRow.CreateCell(2).SetCellValue("Bảo lãnh của bên thứ ba");
                dRow.CreateCell(3).SetCellValue("* Thế chấp tài sản gắn liền với đất"); // Đánh dấu sao
                dRow.CreateCell(4).SetCellValue("Tín chấp của tổ chức chính trị xã hội");
                dRow.CreateCell(5).SetCellValue("Cam kết bảo đảm của cổ đông");
                dRow.CreateCell(6).SetCellValue(""); // Trống
            });

            var parser = new ExcelParserService(null!);
            var preview = parser.ParseExcelFileToPreview(stream, "BaoDamTienVay.xlsx");

            Assert.Equal(1, preview.TotalQuestions);
            var q = preview.Sheets[0].Questions[0];

            // Tự động nhận diện phương án B (vị trí 2) là đáp án đúng
            Assert.Equal("2", q.SuggestedAnswer);
            // Dấu sao đã được làm sạch khỏi nội dung phương án
            Assert.False(q.Options[1].StartsWith("*"));
            Assert.Contains("Thế chấp tài sản gắn liền với đất", q.Options[1]);
        }

        [Fact]
        public void MultiChoice_And_TrueFalseMatrix_ShouldBeIdentifiedAccurately()
        {
            using var stream = CreateWorkbookStream(sheet =>
            {
                var hRow = sheet.CreateRow(0);
                hRow.CreateCell(0).SetCellValue("STT");
                hRow.CreateCell(1).SetCellValue("CÂU HỎI");
                hRow.CreateCell(2).SetCellValue("PA 1");
                hRow.CreateCell(3).SetCellValue("PA 2");
                hRow.CreateCell(4).SetCellValue("PA 3");
                hRow.CreateCell(5).SetCellValue("PA 4");
                hRow.CreateCell(6).SetCellValue("ĐÁP ÁN");

                // Câu 1: Nhiều đáp án đúng (A, C)
                var r1 = sheet.CreateRow(1);
                r1.CreateCell(0).SetCellValue("1");
                r1.CreateCell(1).SetCellValue("Các hình thức cấp tín dụng bao gồm những hình thức nào?");
                r1.CreateCell(2).SetCellValue("Cho vay");
                r1.CreateCell(3).SetCellValue("Gửi tiền tiết kiệm");
                r1.CreateCell(4).SetCellValue("Bảo lãnh ngân hàng");
                r1.CreateCell(5).SetCellValue("Mua bán ngoại tệ giao ngay");
                r1.CreateCell(6).SetCellValue("A, C");

                // Câu 2: Chùm mệnh đề Đúng / Sai GDPT 2025
                var r2 = sheet.CreateRow(2);
                r2.CreateCell(0).SetCellValue("2");
                r2.CreateCell(1).SetCellValue("Cho biết tính đúng/sai của các nhận định sau về lãi suất:");
                r2.CreateCell(2).SetCellValue("a) Lãi suất tái cấp vốn do NHNN công bố");
                r2.CreateCell(3).SetCellValue("b) Lãi suất cơ bản cố định trọn đời");
                r2.CreateCell(4).SetCellValue("c) Lãi suất liên ngân hàng biến động từng ngày");
                r2.CreateCell(5).SetCellValue("d) Lãi suất tiền gửi không kỳ hạn cao hơn có kỳ hạn");
                r2.CreateCell(6).SetCellValue("a-Đ, b-S, c-Đ, d-S");
            });

            var parser = new ExcelParserService(null!);
            var preview = parser.ParseExcelFileToPreview(stream, "AdvancedTypes.xlsx");

            Assert.Equal(2, preview.TotalQuestions);

            var q1 = preview.Sheets[0].Questions[0];
            Assert.Equal("MULTI", q1.QuestionType);
            Assert.Equal("1, 3", q1.SuggestedAnswer);

            var q2 = preview.Sheets[0].Questions[1];
            Assert.Equal("TRUE_FALSE", q2.QuestionType);
            Assert.Contains("a-Đ", q2.SuggestedAnswer);
        }

        [Fact]
        public void CriticalQuestion_ShouldBeDetectedAndTaggedWithFatalLock()
        {
            using var stream = CreateWorkbookStream(sheet =>
            {
                var hRow = sheet.CreateRow(0);
                hRow.CreateCell(0).SetCellValue("STT");
                hRow.CreateCell(1).SetCellValue("CÂU HỎI");
                hRow.CreateCell(2).SetCellValue("ĐÁP ÁN 1");
                hRow.CreateCell(3).SetCellValue("ĐÁP ÁN 2");
                hRow.CreateCell(4).SetCellValue("ĐÁP ÁN ĐÚNG");
                hRow.CreateCell(5).SetCellValue("ĐIỂM LIỆT");

                // Câu 1: Có cột điểm liệt ghi "Có"
                var r1 = sheet.CreateRow(1);
                r1.CreateCell(0).SetCellValue("1");
                r1.CreateCell(1).SetCellValue("Hành vi làm giả chứng từ kế toán trong hoạt động ngân hàng có bị cấm không?");
                r1.CreateCell(2).SetCellValue("Bị nghiêm cấm tuyệt đối");
                r1.CreateCell(3).SetCellValue("Được phép trong trường hợp đặc biệt");
                r1.CreateCell(4).SetCellValue("1");
                r1.CreateCell(5).SetCellValue("Có");

                // Câu 2: Chứa từ khóa tử thần [ĐIỂM LIỆT] ngay trong câu hỏi
                var r2 = sheet.CreateRow(2);
                r2.CreateCell(0).SetCellValue("2");
                r2.CreateCell(1).SetCellValue("[ĐIỂM LIỆT ⚠️] Nghiêm cấm cán bộ tín dụng nhận hối lộ dưới mọi hình thức đúng hay sai?");
                r2.CreateCell(2).SetCellValue("Đúng");
                r2.CreateCell(3).SetCellValue("Sai");
                r2.CreateCell(4).SetCellValue("1");
                r2.CreateCell(5).SetCellValue("");
            });

            var parser = new ExcelParserService(null!);
            var preview = parser.ParseExcelFileToPreview(stream, "KiemSoatNoiBo.xlsx");

            Assert.Equal(2, preview.TotalQuestions);
            var q1 = preview.Sheets[0].Questions[0];
            var q2 = preview.Sheets[0].Questions[1];

            Assert.True(q1.IsCritical, "Câu 1 phải được nhận diện là điểm liệt do có cột điểm liệt");
            Assert.Contains("critical", q1.Tags);
            Assert.Contains("fatal-fail", q1.Tags);

            Assert.True(q2.IsCritical, "Câu 2 phải được nhận diện là điểm liệt do có từ khóa [ĐIỂM LIỆT]");
            Assert.Contains("critical", q2.Tags);
        }

        [Fact]
        public void TextSanitization_ShouldRemoveExcelArtifactsAndHealSpacing()
        {
            using var stream = CreateWorkbookStream(sheet =>
            {
                var hRow = sheet.CreateRow(0);
                hRow.CreateCell(0).SetCellValue("STT");
                hRow.CreateCell(1).SetCellValue("CÂU HỎI");
                hRow.CreateCell(2).SetCellValue("ĐÁP ÁN 1");
                hRow.CreateCell(3).SetCellValue("ĐÁP ÁN 2");
                hRow.CreateCell(4).SetCellValue("ĐÁP ÁN ĐÚNG");

                var r = sheet.CreateRow(1);
                r.CreateCell(0).SetCellValue("1");
                // Chứa rác _x000D_\n và non-breaking space
                r.CreateCell(1).SetCellValue("Quy định_x000D_\nvề an toàn vốn\u00A0theo chuẩn Basel II?");
                r.CreateCell(2).SetCellValue("A.Đúng quy định"); // Dính chữ A.Đúng
                r.CreateCell(3).SetCellValue("B. Không đúng");
                r.CreateCell(4).SetCellValue("1");
            });

            var parser = new ExcelParserService(null!);
            var preview = parser.ParseExcelFileToPreview(stream, "CleanTest.xlsx");

            var q = preview.Sheets[0].Questions[0];
            Assert.False(q.Content.Contains("_x000D_"));
            Assert.False(q.Content.Contains("\u00A0"));
            Assert.Contains("A. Đúng quy định", q.Options[0]); // Đã tự động chèn khoảng trắng sau A.
        }

        [Theory]
        [InlineData("Tất cả các đáp án trên đều đúng", "Tất cả các phương án trên đều đúng")]
        [InlineData("Tất cả các đáp án trên", "Tất cả các phương án trên đều đúng")]
        [InlineData("Tất cả phương án trên", "Tất cả các phương án trên đều đúng")]
        [InlineData("Cả 3 phương án trên đều đúng", "Tất cả các phương án trên đều đúng")]
        [InlineData("Cả 4 đáp án trên đều đúng", "Tất cả các phương án trên đều đúng")]
        [InlineData("Không có đáp án nào đúng", "Không có phương án nào đúng")]
        [InlineData("Không phương án nào đúng", "Không có phương án nào đúng")]
        [InlineData("Tất cả các đáp án trên đều sai", "Không có phương án nào đúng")]
        [InlineData("Tất cả các phương án trên đều sai", "Không có phương án nào đúng")]
        [InlineData("Cả A và B", "Cả (1) và (2)")]
        [InlineData("Cả A và B đều đúng", "Cả (1) và (2)")]
        [InlineData("Đáp án A và B", "Cả (1) và (2)")]
        [InlineData("Đáp án 1 và 2", "Cả (1) và (2)")]
        [InlineData("Cả hai phương án trên", "Cả (1) và (2)")]
        [InlineData("Cả 2 đáp án trên", "Cả (1) và (2)")]
        [InlineData("1 và 2", "Cả (1) và (2)")]
        [InlineData("1,2", "Cả (1) và (2)")]
        [InlineData("1, 2", "Cả (1) và (2)")]
        [InlineData("1 hoặc 2", "(1) hoặc (2)")]
        [InlineData("1 và 3", "Cả (1) và (3)")]
        [InlineData("1, 3", "Cả (1) và (3)")]
        [InlineData("1,3", "Cả (1) và (3)")]
        [InlineData("1 hoặc 3", "(1) hoặc (3)")]
        [InlineData("2 và 3", "Cả (2) và (3)")]
        [InlineData("2, 3", "Cả (2) và (3)")]
        [InlineData("2,3", "Cả (2) và (3)")]
        [InlineData("2 hoặc 3", "(2) hoặc (3)")]
        [InlineData("1 và 4", "Cả (1) và (4)")]
        [InlineData("1, 4", "Cả (1) và (4)")]
        [InlineData("1,4", "Cả (1) và (4)")]
        [InlineData("1 hoặc 4", "(1) hoặc (4)")]
        [InlineData("2 và 4", "Cả (2) và (4)")]
        [InlineData("2, 4", "Cả (2) và (4)")]
        [InlineData("3 và 4", "Cả (3) và (4)")]
        [InlineData("3, 4", "Cả (3) và (4)")]
        [InlineData("Cả 1 và 2", "Cả (1) và (2)")]
        [InlineData("Cả 1, 2", "Cả (1) và (2)")]
        [InlineData("Cả 1,2", "Cả (1) và (2)")]
        [InlineData("Cả 1 hoặc 2", "(1) hoặc (2)")]
        [InlineData("Ý 1 và 2", "Cả (1) và (2)")]
        [InlineData("Ý 1, 2", "Cả (1) và (2)")]
        [InlineData("Câu 1, 2", "Cả (1) và (2)")]
        [InlineData("(1) và (2)", "Cả (1) và (2)")]
        [InlineData("(1) hoặc (2)", "(1) hoặc (2)")]
        [InlineData("(1), (2)", "Cả (1) và (2)")]
        [InlineData("A, B", "Cả (1) và (2)")]
        [InlineData("A,B", "Cả (1) và (2)")]
        [InlineData("A hoặc B", "(1) hoặc (2)")]
        [InlineData("B và C", "Cả (2) và (3)")]
        [InlineData("I và II", "Cả (1) và (2)")]
        [InlineData("I, II", "Cả (1) và (2)")]
        [InlineData("I,II", "Cả (1) và (2)")]
        [InlineData("I hoặc II", "(1) hoặc (2)")]
        [InlineData("II và III", "Cả (2) và (3)")]
        [InlineData("1, 2 và 3", "Cả (1), (2) và (3)")]
        [InlineData("1, 2, 3", "Cả (1), (2) và (3)")]
        [InlineData("1,2,3", "Cả (1), (2) và (3)")]
        [InlineData("A, B, C", "Cả (1), (2) và (3)")]
        [InlineData("I, II, III", "Cả (1), (2) và (3)")]
        [InlineData("1, 2 và 4", "Cả (1), (2) và (4)")]
        [InlineData("Chỉ có ý 1", "Chỉ có (1)")]
        [InlineData("Chỉ có đáp án A", "Chỉ có (1)")]
        [InlineData("1,2%", "1,2%")]
        [InlineData("1,2 triệu đồng", "1,2 triệu đồng")]
        [InlineData("Cả A và C", "Cả (1) và (3)")]
        [InlineData("Cả B và C", "Cả (2) và (3)")]
        [InlineData("Cả 1, 2 và 3", "Cả (1), (2) và (3)")]
        [InlineData("D. Tất cả các đáp án trên đều đúng", "Tất cả các phương án trên đều đúng")]
        [InlineData("4. Tất cả các phương án trên", "Tất cả các phương án trên đều đúng")]
        public void TranslateAnchorOption_ShouldStandardizeAccordingToVnCorrectRules(string input, string expected)
        {
            var actual = SmartOptionShufflerService.TranslateAnchorOption(input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ExcelIngestion_WithAnchorOption_ShouldTranslateAndPinToBottom()
        {
            using var stream = CreateWorkbookStream(sheet =>
            {
                var hRow = sheet.CreateRow(0);
                hRow.CreateCell(0).SetCellValue("STT");
                hRow.CreateCell(1).SetCellValue("CÂU HỎI");
                hRow.CreateCell(2).SetCellValue("ĐÁP ÁN 1");
                hRow.CreateCell(3).SetCellValue("ĐÁP ÁN 2");
                hRow.CreateCell(4).SetCellValue("ĐÁP ÁN 3");
                hRow.CreateCell(5).SetCellValue("ĐÁP ÁN 4");
                hRow.CreateCell(6).SetCellValue("ĐÁP ÁN ĐÚNG");

                var r = sheet.CreateRow(1);
                r.CreateCell(0).SetCellValue("1");
                r.CreateCell(1).SetCellValue("Đâu là tài sản bảo đảm hợp pháp theo quy định?");
                r.CreateCell(2).SetCellValue("Bất động sản có sổ đỏ");
                r.CreateCell(3).SetCellValue("Phương tiện vận tải đăng ký chính chủ");
                r.CreateCell(4).SetCellValue("Số dư tiền gửi tiết kiệm");
                r.CreateCell(5).SetCellValue("Tất cả các đáp án trên đều đúng"); // Cụm từ neo cần dịch và ghim
                r.CreateCell(6).SetCellValue("4"); // Đáp án là phương án 4
            });

            // Sử dụng Dummy text correction service
            var dummyCorrection = new DummyTextCorrectionService();
            var shuffler = new SmartOptionShufflerService(dummyCorrection);
            var parser = new ExcelParserService(null!, dummyCorrection, shuffler);

            var preview = parser.ParseExcelFileToPreview(stream, "AnchorTest.xlsx");
            var q = preview.Sheets[0].Questions[0];

            // Phương án neo phải được dịch chuẩn theo luật VN Correct
            Assert.Equal("Tất cả các phương án trên đều đúng", q.Options[3]);

            // Phương án neo phải luôn được GHIM ở vị trí cuối cùng (vị trí thứ 4, index 3)
            Assert.Equal(4, q.Options.Count);
            Assert.Equal("Tất cả các phương án trên đều đúng", q.Options.Last());

            // Đáp án đúng phải được bảo toàn là "4" (vị trí phương án neo)
            Assert.Equal("4", q.SuggestedAnswer);
        }

        [Fact]
        public void ExcelIngestion_WithRelativeAnchor_ShouldRewriteNumbersAndSortAscending()
        {
            using var stream = CreateWorkbookStream(sheet =>
            {
                var hRow = sheet.CreateRow(0);
                hRow.CreateCell(0).SetCellValue("STT");
                hRow.CreateCell(1).SetCellValue("CÂU HỎI");
                hRow.CreateCell(2).SetCellValue("ĐÁP ÁN 1");
                hRow.CreateCell(3).SetCellValue("ĐÁP ÁN 2");
                hRow.CreateCell(4).SetCellValue("ĐÁP ÁN 3");
                hRow.CreateCell(5).SetCellValue("ĐÁP ÁN 4");
                hRow.CreateCell(6).SetCellValue("ĐÁP ÁN ĐÚNG");

                var r = sheet.CreateRow(1);
                r.CreateCell(0).SetCellValue("1");
                r.CreateCell(1).SetCellValue("Biện pháp phòng ngừa rủi ro tín dụng hiệu quả?");
                r.CreateCell(2).SetCellValue("Thẩm định kỹ phương án vay vốn"); // Độc lập 1
                r.CreateCell(3).SetCellValue("Kiểm tra sau cho vay định kỳ");   // Độc lập 2
                r.CreateCell(4).SetCellValue("Bỏ qua xếp hạng tín dụng");      // Độc lập 3
                r.CreateCell(5).SetCellValue("D. Cả A và B đều đúng");         // Neo tham chiếu tương đối
                r.CreateCell(6).SetCellValue("D"); // Đáp án D
            });

            var dummyCorrection = new DummyTextCorrectionService();
            var shuffler = new SmartOptionShufflerService(dummyCorrection);
            var parser = new ExcelParserService(null!, dummyCorrection, shuffler);

            var preview = parser.ParseExcelFileToPreview(stream, "RelativeAnchorTest.xlsx");
            var q = preview.Sheets[0].Questions[0];

            // Phương án neo phải nằm ở vị trí thứ 4
            Assert.Equal(4, q.Options.Count);
            var anchorText = q.Options[3];

            // Phải có định dạng Cả (min) và (max) và các chỉ số phải tăng dần
            Assert.StartsWith("Cả (", anchorText);
            Assert.Contains(") và (", anchorText);

            var match = System.Text.RegularExpressions.Regex.Match(anchorText, @"Cả\s+\(([1-3])\)\s+và\s+\(([1-3])\)");
            Assert.True(match.Success, $"Nội dung phương án neo '{anchorText}' phải đúng cú pháp Cả (X) và (Y)");
            int x = int.Parse(match.Groups[1].Value);
            int y = int.Parse(match.Groups[2].Value);
            Assert.True(x < y, $"Chỉ số phải được sắp xếp tăng dần: {x} < {y}");

            // Đáp án đúng vẫn phải là "4" (vị trí phương án neo)
            Assert.Equal("4", q.SuggestedAnswer);
        }

        [Theory]
        [InlineData("1 và 2")]
        [InlineData("1,2")]
        [InlineData("1, 2")]
        [InlineData("1 hoặc 2")]
        [InlineData("1 và 3")]
        [InlineData("2 và 3")]
        [InlineData("1, 3")]
        [InlineData("2, 3")]
        [InlineData("1 và 4")]
        [InlineData("1, 2 và 3")]
        public void ShuffleOptions_WithNumericPairForms_ShouldPinAnchorAndRewriteNumbers(string rawAnchor)
        {
            var dummyCorrection = new DummyTextCorrectionService();
            var shuffler = new SmartOptionShufflerService(dummyCorrection);

            var options = new List<string>
            {
                "Phương án A độc lập",
                "Phương án B độc lập",
                "Phương án C độc lập",
                rawAnchor // Vị trí ban đầu là 4
            };

            var res = shuffler.ShuffleOptions(options, "4", rawAnchor);

            Assert.True(res.WasShuffled);
            Assert.True(res.HasAnchorPinned);
            Assert.Equal(4, res.Options.Count);
            Assert.Equal("4", res.CorrectAnswer); // Neo ở vị trí 4, đáp án đúng vẫn là 4

            var lastOption = res.Options[3];
            // Phải chứa dạng Cả (X) và (Y) hoặc (X) hoặc (Y) hoặc Cả (X), (Y) và (Z)
            Assert.True(
                lastOption.StartsWith("Cả (") || lastOption.StartsWith("("),
                $"Phương án cuối '{lastOption}' phải có định dạng chuẩn hóa SmartOptionShuffler");
        }

        private class DummyTextCorrectionService : AegisQuiz.Application.Interfaces.IVietnameseTextCorrectionService
        {
            public string CorrectText(string input) => input?.Trim() ?? string.Empty;
            public AegisQuiz.Application.Interfaces.DocxImportPreviewDto CorrectQuestionDto(AegisQuiz.Application.Interfaces.DocxImportPreviewDto dto) => dto;
            public System.Collections.Generic.List<AegisQuiz.Application.Interfaces.DocxImportPreviewDto> CorrectQuestionsBatch(System.Collections.Generic.List<AegisQuiz.Application.Interfaces.DocxImportPreviewDto> dtos) => dtos;
            public bool ContainsPositionalReference(string optionText) => SmartOptionShufflerService.TranslateAnchorOption(optionText).StartsWith("Cả (");
            public bool IsAnchorOption(string optionText)
            {
                var t = SmartOptionShufflerService.TranslateAnchorOption(optionText);
                return t.StartsWith("Tất cả các phương án") || t.StartsWith("Không có phương án nào") || t.StartsWith("Cả (");
            }
        }

        [Fact]
        public void RealDot2026_AllFiles_InspectionTest()
        {
            var dir = @"D:\Cuong\DuAn\mybank\AegisQuiz\DOT-2026";
            if (!Directory.Exists(dir)) return;

            var files = Directory.GetFiles(dir, "*.xlsx");
            var parser = new ExcelParserService(null!);

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                try
                {
                    using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var preview = parser.ParseExcelFileToPreview(fs, fileName);

                    _output.WriteLine($"==================================================================");
                    _output.WriteLine($"FILE: {fileName} | TotalSheets: {preview.Sheets.Count} | TotalQuestions: {preview.TotalQuestions}");
                    foreach (var sh in preview.Sheets)
                    {
                        var typeBreakdown = string.Join(", ", sh.Questions.GroupBy(q => q.QuestionType).Select(g => $"{g.Key}:{g.Count()}"));
                        _output.WriteLine($"   Sheet '{sh.SheetName}': {sh.QuestionCount} questions -> Types: [{typeBreakdown}]");

                        var nonSingle = sh.Questions.Where(q => q.QuestionType != "SINGLE").Take(3).ToList();
                        foreach (var nq in nonSingle)
                        {
                            _output.WriteLine($"      [NON-SINGLE: {nq.QuestionType}] AnsRaw suggested: '{nq.SuggestedAnswer}' | Options count: {nq.Options.Count} | Content: {nq.Content.Substring(0, Math.Min(60, nq.Content.Length))}...");
                        }

                        var emptyAns = sh.Questions.Where(q => string.IsNullOrWhiteSpace(q.SuggestedAnswer)).Take(3).ToList();
                        foreach (var eq in emptyAns)
                        {
                            _output.WriteLine($"      [EMPTY-ANSWER] Type: {eq.QuestionType} | Content: {eq.Content.Substring(0, Math.Min(60, eq.Content.Length))}...");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"FILE: {fileName} -> FAILED WITH EXCEPTION: {ex.Message}");
                }
            }
        }

        [Fact]
        public void InspectFile7()
        {
            var file = @"D:\Cuong\DuAn\mybank\AegisQuiz\DOT-2026\7. Kế toán giao dịch nội bộ.xlsx";
            if (!File.Exists(file)) return;

            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook(fs);
            _output.WriteLine($"File 7 has {workbook.NumberOfSheets} sheets:");
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                _output.WriteLine($"  Sheet {i}: '{sheet.SheetName}' (LastRowNum: {sheet.LastRowNum})");
                for (int r = 0; r <= Math.Min(10, sheet.LastRowNum); r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;
                    var cells = new List<string>();
                    for (int c = 0; c < Math.Min(15, (int)row.LastCellNum); c++)
                    {
                        var cell = row.GetCell(c);
                        cells.Add(cell?.ToString()?.Trim() ?? "");
                    }
                    _output.WriteLine($"    Row {r}: [{string.Join(" | ", cells)}]");
                }
            }
        }

        [Fact]
        public void InspectQuestionTypes()
        {
            var dir = @"D:\Cuong\DuAn\mybank\AegisQuiz\DOT-2026";
            var testFiles = new[] { "15. Văn thư lễ tân.xlsx", "3. Tín dụng KHCN.xlsx", "6. Kế hoạch và Quản lý rủi ro.xlsx", "16. KTC + 80% Câu hỏi CN Campuchia.xlsx" };
            var parser = new ExcelParserService(null!);

            foreach (var tf in testFiles)
            {
                var f = Path.Combine(dir, tf);
                if (!File.Exists(f)) continue;

                using var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var preview = parser.ParseExcelFileToPreview(fs, tf);
                _output.WriteLine($"=== INSPECTING: {tf} ===");
                foreach (var sh in preview.Sheets)
                {
                    _output.WriteLine($"  Sheet: {sh.SheetName} (Detected Header Row: {sh.HeaderRowIndex})");
                    _output.WriteLine($"  Detected Columns: {string.Join(", ", sh.ColumnMapping.Select(kv => $"{kv.Key}->Col{kv.Value}"))}");

                    var nonSingle = sh.Questions.Where(q => q.QuestionType != "SINGLE").Take(5).ToList();
                    foreach (var q in nonSingle)
                    {
                        _output.WriteLine($"    [QType: {q.QuestionType}] Ans: '{q.SuggestedAnswer}' | OptCount: {q.Options.Count}");
                        _output.WriteLine($"      Content: {q.Content}");
                        for (int i = 0; i < q.Options.Count; i++)
                        {
                            _output.WriteLine($"        Opt {i+1}: {q.Options[i]}");
                        }
                    }
                }
            }
        }

        [Fact]
        public void UniversalColumnTaxonomy_MultiLingualRecognition_Fact()
        {
            var taxonomy = UniversalColumnTaxonomyMatrix.Instance;

            // 1. Tiếng Việt
            var viAns = taxonomy.MatchColumn("Phương án đúng");
            Assert.Equal("ANSWER_KEY", viAns.TargetField);

            var viOpt = taxonomy.MatchColumn("Phương án B");
            Assert.Equal("OPTION", viOpt.TargetField);
            Assert.Equal(2, viOpt.OptionIndex);

            var viContent = taxonomy.MatchColumn("Nội dung câu hỏi");
            Assert.Equal("QUESTION_CONTENT", viContent.TargetField);

            var viRef = taxonomy.MatchColumn("Căn cứ pháp lý");
            Assert.Equal("EXPLANATION", viRef.TargetField);

            // 2. English
            var enAns = taxonomy.MatchColumn("Correct Answer");
            Assert.Equal("ANSWER_KEY", enAns.TargetField);

            var enOpt = taxonomy.MatchColumn("Option C");
            Assert.Equal("OPTION", enOpt.TargetField);
            Assert.Equal(3, enOpt.OptionIndex);

            var enContent = taxonomy.MatchColumn("Question Prompt");
            Assert.Equal("QUESTION_CONTENT", enContent.TargetField);

            var enRef = taxonomy.MatchColumn("Reference & Rationale");
            Assert.Equal("EXPLANATION", enRef.TargetField);

            // 3. Tiếng Trung & Nhật
            var zhAns = taxonomy.MatchColumn("正确答案");
            Assert.Equal("ANSWER_KEY", zhAns.TargetField);

            var jaAns = taxonomy.MatchColumn("正解");
            Assert.Equal("ANSWER_KEY", jaAns.TargetField);
        }

        [Fact]
        public void UniversalColumnTaxonomy_SelfEnrichmentLoop_Fact()
        {
            var taxonomy = UniversalColumnTaxonomyMatrix.Instance;
            string novelHeader = "Đáp số chuẩn xác của hội đồng thẩm định";

            // Ban đầu chưa có quy tắc cho chuỗi độc lạ này
            var beforeMatch = taxonomy.MatchColumn(novelHeader);
            
            // Kích hoạt cơ chế tự làm giàu tri thức
            bool enriched = taxonomy.EnrichRule("ANSWER_KEY", novelHeader, "BANKING", "vi");
            Assert.True(enriched || beforeMatch.TargetField == "ANSWER_KEY");

            // Sau khi làm giàu: hệ thống nhận diện tức thì
            var afterMatch = taxonomy.MatchColumn(novelHeader);
            Assert.Equal("ANSWER_KEY", afterMatch.TargetField);
        }

        [Fact]
        public void MultiRowHeaderFusion_HierarchicalMergedHeader_Fact()
        {
            // Kiểm tra khả năng hợp nhất tiêu đề 2 tầng:
            // Dòng 3: [STT] | [NỘI DUNG CÂU HỎI] | [PHƯƠNG ÁN LỰA CHỌN (merge)] | [ĐÁP ÁN ĐÚNG]
            // Dòng 4:       |                    |   A   |   B   |   C   |   D   |
            using var stream = CreateWorkbookStream(sheet =>
            {
                // Dòng 0..2: Tiêu đề hành chính
                sheet.CreateRow(0).CreateCell(0).SetCellValue("TRƯỜNG ĐÀO TẠO CÁN BỘ NGÂN HÀNG");
                sheet.CreateRow(1).CreateCell(0).SetCellValue("BỘ CÂU HỎI ÔN TẬP NGHIỆP VỤ 2026");

                // Dòng 2: Tầng 1 của Header
                var r2 = sheet.CreateRow(2);
                r2.CreateCell(0).SetCellValue("STT");
                r2.CreateCell(1).SetCellValue("NỘI DUNG CÂU HỎI");
                r2.CreateCell(2).SetCellValue("CÁC PHƯƠNG ÁN LỰA CHỌN");
                r2.CreateCell(6).SetCellValue("ĐÁP ÁN ĐÚNG");
                r2.CreateCell(7).SetCellValue("CĂN CỨ");

                // Dòng 3: Tầng 2 của Header (Các nhãn phương án A, B, C, D)
                var r3 = sheet.CreateRow(3);
                r3.CreateCell(2).SetCellValue("A");
                r3.CreateCell(3).SetCellValue("B");
                r3.CreateCell(4).SetCellValue("C");
                r3.CreateCell(5).SetCellValue("D");

                // Dòng 4: Dữ liệu câu hỏi thực tế
                var r4 = sheet.CreateRow(4);
                r4.CreateCell(0).SetCellValue("1");
                r4.CreateCell(1).SetCellValue("Hợp đồng tín dụng có hiệu lực từ thời điểm nào?");
                r4.CreateCell(2).SetCellValue("Từ ngày ký");
                r4.CreateCell(3).SetCellValue("Từ ngày giải ngân đầu tiên");
                r4.CreateCell(4).SetCellValue("Theo thỏa thuận trong hợp đồng");
                r4.CreateCell(5).SetCellValue("Cả A và C");
                r4.CreateCell(6).SetCellValue("3");
                r4.CreateCell(7).SetCellValue("Điều 15 Luật TCTD 2024");
            });

            var parser = new ExcelParserService(null!);
            var preview = parser.ParseExcelFileToPreview(stream, "HierarchicalTest.xlsx");

            Assert.Single(preview.Sheets);
            var sh = preview.Sheets[0];
            Assert.Equal(2, sh.HeaderRowIndex); // Dòng header gốc là dòng 2
            Assert.True(sh.ColumnMapping.ContainsKey("option_1"));
            Assert.True(sh.ColumnMapping.ContainsKey("option_2"));
            Assert.True(sh.ColumnMapping.ContainsKey("option_3"));
            Assert.True(sh.ColumnMapping.ContainsKey("option_4"));
            Assert.True(sh.ColumnMapping.ContainsKey("answer"));

            Assert.Single(sh.Questions);
            var q = sh.Questions[0];
            Assert.Equal("SINGLE", q.QuestionType);
            Assert.Equal("3", q.SuggestedAnswer);
            Assert.Equal(4, q.Options.Count);
        }

        [Fact]
        public void AutoTopicNaming_TenTmTenFile_Fact()
        {
            var parser = new ExcelParserService(null!);
            using var stream = CreateWorkbookStream(sheet =>
            {
                var h = sheet.CreateRow(0);
                h.CreateCell(0).SetCellValue("STT");
                h.CreateCell(1).SetCellValue("CÂU HỎI");
                h.CreateCell(2).SetCellValue("A");
                h.CreateCell(3).SetCellValue("B");
                h.CreateCell(4).SetCellValue("ĐÁP ÁN ĐÚNG");

                var d = sheet.CreateRow(1);
                d.CreateCell(0).SetCellValue("1");
                d.CreateCell(1).SetCellValue("Test topic name detection");
                d.CreateCell(2).SetCellValue("Option 1");
                d.CreateCell(3).SetCellValue("Option 2");
                d.CreateCell(4).SetCellValue("1");
            });

            string testPath = @"d:\Cuong\DuAn\mybank\AegisQuiz\2026-DOT2\7. Kế toán giao dịch nội bộ.xlsx";
            var preview = parser.ParseExcelFileToPreview(stream, testPath);

            Assert.NotEmpty(preview.Sheets);
            var sh = preview.Sheets[0];

            // Nguyên tắc nhận Topic tự động UCIS v4.0 Hierarchical:
            // Chủ đề cha ~ Tên thư mục: 2026-DOT2 (Mã: 2026_DOT2)
            // Chủ đề con ~ Tên file: 7. Kế toán giao dịch nội bộ (Mã: 2026_DOT2_7_KE_TOAN_GIAO_DICH_NOI_BO)
            // Mô tả chủ đề: Tên file + đường dẫn + số lượng câu hỏi
            Assert.StartsWith("2026_DOT2_KT_GDNB", sh.DetectedTopicCode);
            Assert.StartsWith("7. Kế toán giao dịch nội bộ", sh.DetectedTopicName);
            Assert.Equal("2026_DOT2", sh.DetectedParentTopicCode);
            Assert.Equal("2026-DOT2", sh.DetectedParentTopicName);
            Assert.Contains("7. Kế toán giao dịch nội bộ", sh.DetectedTopicDescription);
            Assert.Contains("2026-DOT2", sh.DetectedTopicDescription);
        }

        [Fact]
        public void AllFiles_In_2026Dot2_Directory_ShouldParseSuccessfully_Fact()
        {
            var candidates = new[]
            {
                @"D:\Cuong\DuAn\mybank\2026-DOT2",
                @"D:\Cuong\DuAn\mybank\AegisQuiz\2026-DOT2"
            };
            var dir = candidates.FirstOrDefault(Directory.Exists);
            Assert.True(dir != null && Directory.Exists(dir), $"Thư mục 2026-DOT2 phải tồn tại ở một trong các đường dẫn: {string.Join(", ", candidates)}");

            var files = Directory.GetFiles(dir, "*.xlsx");
            Assert.True(files.Length >= 19, $"Phải có ít nhất 19 file trong thư mục 2026-DOT2, tìm thấy: {files.Length}");

            var parser = new ExcelParserService(null!);
            int totalQuestionsAcrossAllFiles = 0;

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var preview = parser.ParseExcelFileToPreview(fs, filePath);

                _output.WriteLine($"[2026-DOT2] File: {fileName} -> Sheets: {preview.Sheets.Count}, Total Qs: {preview.TotalQuestions}");
                Assert.True(preview.TotalQuestions > 0, $"File {fileName} phải bóc tách được ít nhất 1 câu hỏi.");

                foreach (var sh in preview.Sheets)
                {
                    Assert.StartsWith("2026_DOT2", sh.DetectedTopicCode);
                    Assert.Equal("2026-DOT2", sh.DetectedParentTopicName);
                    Assert.Equal("2026_DOT2", sh.DetectedParentTopicCode);
                    Assert.NotEmpty(sh.DetectedTopicName);
                    Assert.NotEmpty(sh.DetectedTopicDescription);
                    _output.WriteLine($"   Sheet '{sh.SheetName}': TopicCode='{sh.DetectedTopicCode}', TopicName='{sh.DetectedTopicName}', Parent='{sh.DetectedParentTopicName}', QCount={sh.Questions.Count}");
                }

                totalQuestionsAcrossAllFiles += preview.TotalQuestions;
            }

            _output.WriteLine($"===> TỔNG CỘNG TOÀN BỘ 19 FILE THƯ MỤC 2026-DOT2: {totalQuestionsAcrossAllFiles} CÂU HỎI HOÀN TOÀN HỢP LỆ!");
            Assert.True(totalQuestionsAcrossAllFiles > 2000, "Tổng số câu hỏi của 19 file phải vượt 2000 câu.");
        }

        [Fact]
        public void Excel_With_Shared_Context_And_Extended_Cognitive_Fields_Should_Map_Properly()
        {
            // Tạo workbook in-memory với đầy đủ các trường nhận thức mở rộng
            using var wb = new NPOI.XSSF.UserModel.XSSFWorkbook();
            var sheet = wb.CreateSheet("Tín dụng nâng cao");

            var hRow = sheet.CreateRow(0);
            hRow.CreateCell(0).SetCellValue("STT");
            hRow.CreateCell(1).SetCellValue("Tiêu đề ngữ cảnh");
            hRow.CreateCell(2).SetCellValue("Bài đọc hiểu");
            hRow.CreateCell(3).SetCellValue("Nội dung câu hỏi");
            hRow.CreateCell(4).SetCellValue("Phương án A");
            hRow.CreateCell(5).SetCellValue("Phương án B");
            hRow.CreateCell(6).SetCellValue("Phương án C");
            hRow.CreateCell(7).SetCellValue("Phương án D");
            hRow.CreateCell(8).SetCellValue("Đáp án đúng");
            hRow.CreateCell(9).SetCellValue("Tiêu chí chấm điểm");
            hRow.CreateCell(10).SetCellValue("Thời gian làm bài");
            hRow.CreateCell(11).SetCellValue("Thẻ tag");
            hRow.CreateCell(12).SetCellValue("Hình ảnh");
            hRow.CreateCell(13).SetCellValue("Trình độ");
            hRow.CreateCell(14).SetCellValue("Lĩnh vực");

            var dRow = sheet.CreateRow(1);
            dRow.CreateCell(0).SetCellValue("1");
            dRow.CreateCell(1).SetCellValue("Tình huống thẩm định ACB 2026");
            dRow.CreateCell(2).SetCellValue("Công ty TNHH Hải An đề nghị vay vốn lưu động 50 tỷ đồng kèm tài sản bảo đảm là quyền sử dụng đất.");
            dRow.CreateCell(3).SetCellValue("Doanh nghiệp cần cung cấp báo cáo tài chính kiểm toán thời hạn bao lâu?");
            dRow.CreateCell(4).SetCellValue("1 năm");
            dRow.CreateCell(5).SetCellValue("2 năm gần nhất");
            dRow.CreateCell(6).SetCellValue("3 năm gần nhất");
            dRow.CreateCell(7).SetCellValue("Không cần");
            dRow.CreateCell(8).SetCellValue("2");
            dRow.CreateCell(9).SetCellValue("Đúng quy chế tín dụng 10 điểm");
            dRow.CreateCell(10).SetCellValue("90 giây");
            dRow.CreateCell(11).SetCellValue("#tindung, #khdn");
            dRow.CreateCell(12).SetCellValue("https://aegisquiz.com/media/diagram1.png");
            dRow.CreateCell(13).SetCellValue("Chuyên viên RM");
            dRow.CreateCell(14).SetCellValue("BANKING");

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                wb.Write(ms, true);
                bytes = ms.ToArray();
            }

            using var readStream = new MemoryStream(bytes);
            var parser = new ExcelParserService(null!);
            var preview = parser.ParseExcelFileToPreview(readStream, "2026-DOT2/ThamDinhTinDung.xlsx");

            Assert.Single(preview.Sheets);
            var sh = preview.Sheets[0];
            Assert.Single(sh.Questions);

            var q = sh.Questions[0];
            Assert.Equal("Tình huống thẩm định ACB 2026", q.ContextTitle);
            Assert.Contains("Công ty TNHH Hải An", q.ContextContent);
            Assert.Equal("2 năm gần nhất", q.Options[1]);
            Assert.Equal("2", q.SuggestedAnswer);
            Assert.Equal("Đúng quy chế tín dụng 10 điểm", q.GradingRubric);
            Assert.Equal(90, q.DurationSeconds);
            Assert.Contains("#tindung", q.Tags);
            Assert.Equal("https://aegisquiz.com/media/diagram1.png", q.MediaUrl);
            Assert.Equal("image", q.ContentType);
            Assert.Equal("Chuyên viên RM", q.TargetLevel);
            Assert.Equal("BANKING", q.DomainCode);
        }

        [Fact]
        public void Topic_MaterializedPath_And_Hierarchy_Should_Compute_Correctly()
        {
            var parent = new BankTopic
            {
                Id = Guid.NewGuid(),
                Code = "2026_DOT2",
                Name = "2026-DOT2",
                DomainCode = "BANKING",
                Scope = "COMMUNITY",
                MaterializedPath = "/BANKING/2026_DOT2/",
                DepthLevel = 1,
                Urn = "urn:aegis:topic:banking:2026-dot2"
            };

            var child = new BankTopic
            {
                Id = Guid.NewGuid(),
                Code = "2026_DOT2_1_TIN_DUNG_KHDN",
                Name = "1. Tín dụng KHDN",
                DomainCode = "BANKING",
                Scope = "COMMUNITY",
                ParentId = parent.Id,
                MaterializedPath = $"/BANKING/2026_DOT2/{parent.Code}_1_TIN_DUNG_KHDN/",
                DepthLevel = 2,
                QuestionCountCached = 240,
                Urn = "urn:aegis:topic:banking:2026-dot2:1_tin_dung_khdn"
            };

            Assert.Equal("/BANKING/2026_DOT2/", parent.MaterializedPath);
            Assert.Equal(1, parent.DepthLevel);
            Assert.Equal("COMMUNITY", parent.Scope);
            Assert.StartsWith(parent.MaterializedPath, child.MaterializedPath);
            Assert.Equal(2, child.DepthLevel);
            Assert.Equal(240, child.QuestionCountCached);
        }
    }
}
