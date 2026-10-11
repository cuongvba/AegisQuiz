using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Transpiler;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class UniversalExamTranspilerTests
    {
        private List<DocxImportPreviewDto> CreateSampleQuestions()
        {
            return new List<DocxImportPreviewDto>
            {
                // 1. Single Choice
                new DocxImportPreviewDto
                {
                    Content = "Tìm nghiệm của phương trình $x^2 - 4 = 0$",
                    QuestionType = "SINGLE",
                    Options = new List<string> { "A. $x = \\pm 2$", "B. $x = 2$", "C. $x = -2$", "D. $x = 4$" },
                    SuggestedAnswer = "A",
                    AiExplanation = "Phương trình $x^2 - 4 = 0 \\iff x = \\pm 2$.",
                    TopicCode = "MATH_12"
                },
                // 2. Multi Choice
                new DocxImportPreviewDto
                {
                    Content = "Các phát biểu nào sau đây là đúng về số hữu tỉ?",
                    QuestionType = "MULTI",
                    Options = new List<string> { "A. Số hữu tỉ viết được dưới dạng phân số $a/b$", "B. Số $\\sqrt{2}$ là số hữu tỉ", "C. Mọi số nguyên đều là số hữu tỉ", "D. $\\pi$ là số hữu tỉ" },
                    SuggestedAnswer = "A,C",
                    AiExplanation = "Phát biểu A và C đúng.",
                    TopicCode = "MATH_7"
                },
                // 3. Multi True False (GDPT 2018)
                new DocxImportPreviewDto
                {
                    Content = "Cho hàm số $y = f(x)$ có bảng biến thiên...",
                    QuestionType = "MULTI_TRUE_FALSE",
                    Options = new List<string> { "Hàm số đồng biến trên khoảng $(0; 2)$", "Hàm số đạt cực đại tại $x = 1$", "Giá trị nhỏ nhất bằng $-3$", "Đồ thị có 2 tiệm cận" },
                    SuggestedAnswer = "Đ,S,Đ,S",
                    AiExplanation = "Ý a đúng, b sai, c đúng, d sai.",
                    TopicCode = "CALCULUS"
                },
                // 4. Short Answer
                new DocxImportPreviewDto
                {
                    Content = "Cho hình vuông có cạnh bằng 5cm. Diện tích hình vuông là bao nhiêu $cm^2$?",
                    QuestionType = "SHORT_ANSWER",
                    SuggestedAnswer = "25",
                    AiExplanation = "$S = 5 \\times 5 = 25$.",
                    TopicCode = "GEOMETRY"
                }
            };
        }

        [Fact]
        public void MoodleXmlExport_GeneratesValidXml_WithAllQuestionTypes()
        {
            // Arrange
            var strategy = new MoodleXmlExportStrategy();
            var questions = CreateSampleQuestions();
            var options = new TranspileOptionsDto
            {
                ExamTitle = "Kiểm Tra Toán Học Kỳ 1",
                CategoryName = "Toan12/GiaiTich"
            };

            // Act
            byte[] bytes = strategy.Transpile(questions, options);
            string xmlContent = Encoding.UTF8.GetString(bytes);

            // Assert
            Assert.NotEmpty(bytes);
            Assert.Contains("<quiz>", xmlContent);
            Assert.Contains("</quiz>", xmlContent);
            Assert.Contains("<category>", xmlContent);
            Assert.Contains("Toan12/GiaiTich", xmlContent);

            // Kiểm tra cấu trúc XML parse được hợp lệ
            var xDoc = XDocument.Parse(xmlContent);
            Assert.NotNull(xDoc.Root);

            // Kiểm tra câu multichoice
            Assert.Contains("type=\"multichoice\"", xmlContent);

            // Kiểm tra câu cloze (Multi True False)
            Assert.Contains("type=\"cloze\"", xmlContent);
            Assert.Contains("{1:MULTICHOICE:", xmlContent);

            // Kiểm tra câu shortanswer
            Assert.Contains("type=\"shortanswer\"", xmlContent);
            Assert.Contains("<text>25</text>", xmlContent);
        }

        [Fact]
        public void MoodleGiftExport_GeneratesValidGiftSyntax()
        {
            // Arrange
            var strategy = new MoodleGiftExportStrategy();
            var questions = CreateSampleQuestions();
            var options = new TranspileOptionsDto
            {
                ExamTitle = "Test GIFT Exam"
            };

            // Act
            byte[] bytes = strategy.Transpile(questions, options);
            string giftContent = Encoding.UTF8.GetString(bytes);

            // Assert
            Assert.NotEmpty(bytes);
            Assert.Contains("$CATEGORY:", giftContent);
            Assert.Contains("::[MATH_12] Câu 1::", giftContent);
            Assert.Contains("=", giftContent); // Đúng
            Assert.Contains("~", giftContent); // Sai
            Assert.Contains("{=25", giftContent); // Short answer
        }

        [Fact]
        public void Qti30PackageExport_GeneratesValidZipWithManifest()
        {
            // Arrange
            var strategy = new Qti30PackageExportStrategy();
            var questions = CreateSampleQuestions();
            var options = new TranspileOptionsDto
            {
                ExamTitle = "Canvas QTI Assessment"
            };

            // Act
            byte[] bytes = strategy.Transpile(questions, options);

            // Assert
            Assert.NotEmpty(bytes);

            using var ms = new MemoryStream(bytes);
            using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

            // Kiểm tra file imsmanifest.xml tồn tại trong gói zip
            var manifestEntry = archive.GetEntry("imsmanifest.xml");
            Assert.NotNull(manifestEntry);

            using var reader = new StreamReader(manifestEntry.Open(), Encoding.UTF8);
            string manifestText = reader.ReadToEnd();
            Assert.Contains("<manifest", manifestText);
            Assert.Contains("type=\"imsqti_test_xmlv2p1\"", manifestText);

            // Kiểm tra file assessment xml tồn tại
            bool hasAssessmentXml = false;
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith("_qti.xml"))
                {
                    hasAssessmentXml = true;
                    using var qReader = new StreamReader(entry.Open(), Encoding.UTF8);
                    string qtiText = qReader.ReadToEnd();
                    Assert.Contains("<questestinterop", qtiText);
                    Assert.Contains("<assessment", qtiText);
                    Assert.Contains("<item", qtiText);
                }
            }
            Assert.True(hasAssessmentXml, "Gói QTI Zip phải chứa ít nhất 1 file assessment XML.");
        }

        [Fact]
        public void UniversalExamTranspilerService_DispatchesCorrectStrategy()
        {
            // Arrange
            var strategies = new List<ITranspilerExportStrategy>
            {
                new MoodleXmlExportStrategy(),
                new MoodleGiftExportStrategy(),
                new Qti30PackageExportStrategy()
            };
            var service = new UniversalExamTranspilerService(strategies, NullLogger<UniversalExamTranspilerService>.Instance);
            var questions = CreateSampleQuestions();

            // Act
            var formats = service.GetAvailableFormats();
            byte[] xmlBytes = service.Transpile(questions, "MOODLE_XML");
            byte[] giftBytes = service.Transpile(questions, "MOODLE_GIFT");
            byte[] qtiBytes = service.Transpile(questions, "QTI_PACKAGE");

            // Assert
            Assert.Equal(3, formats.Count);
            Assert.True(xmlBytes.Length > 0);
            Assert.True(giftBytes.Length > 0);
            Assert.True(qtiBytes.Length > 0);
        }
    }
}
