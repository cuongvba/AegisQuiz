using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Domain.Factories;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.Infrastructure.Services.Docx;

namespace AegisQuiz.UnitTests
{
    /// <summary>
    /// Bộ kiểm thử cô lập chống biến dạng loại câu hỏi (Fault Isolation Tests):
    /// Đảm bảo các dạng câu hỏi Single Choice, Essay, Reading Passage, Numbered Stems
    /// tuyệt đối KHÔNG bị nuốt/chuyển nhầm thành MULTI_TRUE_FALSE.
    /// </summary>
    public class MultiTrueFalseIsolationTests
    {
        private readonly ITestOutputHelper _output;

        public MultiTrueFalseIsolationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void SingleChoice_WithOptionsOnSeparateLines_ShouldStaySingle()
        {
            var paragraphs = new List<string>
            {
                "Câu 1. Thủ đô của nước Pháp là thành phố nào?",
                "A. Paris",
                "B. Luân Đôn",
                "C. Berlin",
                "D. Rome",
                "Đáp án: A"
            };

            var results = DocxStateMachineClassifier.ParseParagraphList(paragraphs);
            Assert.Single(results);
            var q = results[0];
            Assert.Equal("SINGLE", q.QuestionType);
            Assert.Equal(4, q.Options.Count);
            Assert.Equal("1", q.SuggestedAnswer);
        }

        [Fact]
        public void SingleChoice_WithLowercaseOptions_ShouldStaySingle()
        {
            var paragraphs = new List<string>
            {
                "Câu 2. Số nào sau đây là số nguyên tố chẵn duy nhất?",
                "a) 2",
                "b) 4",
                "c) 6",
                "d) 8",
                "Đáp án: A"
            };

            var results = DocxStateMachineClassifier.ParseParagraphList(paragraphs);
            Assert.Single(results);
            var q = results[0];
            Assert.Equal("SINGLE", q.QuestionType);
            Assert.Equal(4, q.Options.Count);
        }

        [Fact]
        public void SingleChoice_WithNumberedStatementsInPrompt_ShouldStaySingleAndPreserveContent()
        {
            var paragraphs = new List<string>
            {
                "Câu 3. Cho các phát biểu sau:",
                "1. Nước sôi ở 100 độ C ở áp suất 1 atm.",
                "2. Kim loại đồng dẫn điện tốt hơn nhôm.",
                "3. Khí oxi nặng hơn không khí.",
                "Có bao nhiêu phát biểu đúng?",
                "A. 1",
                "B. 2",
                "C. 3",
                "D. 0",
                "Đáp án: C"
            };

            var results = DocxStateMachineClassifier.ParseParagraphList(paragraphs);
            Assert.Single(results);
            var q = results[0];
            Assert.Equal("SINGLE", q.QuestionType);
            Assert.Equal(4, q.Options.Count);
            Assert.Contains("1. Nước sôi ở 100 độ C", q.Content);
            Assert.Contains("2. Kim loại đồng dẫn điện", q.Content);
            Assert.Contains("3. Khí oxi nặng hơn", q.Content);
        }

        [Fact]
        public void ExamWithPhanII_TuLuan_ShouldParseAsEssay_NotMultiTrueFalse()
        {
            var paragraphs = new List<string>
            {
                "PHẦN I. TRẮC NGHIỆM",
                "Câu 1. 1 + 1 bằng bao nhiêu?",
                "A. 1    B. 2    C. 3    D. 4",
                "PHẦN II. TỰ LUẬN",
                "Câu 2. Trình bày suy nghĩ của anh/chị về lòng yêu nước trong thời đại số."
            };

            var results = DocxStateMachineClassifier.ParseParagraphList(paragraphs);
            Assert.Equal(2, results.Count);

            var q1 = results[0];
            Assert.Equal("SINGLE", q1.QuestionType);

            var q2 = results[1];
            Assert.Equal("ESSAY", q2.QuestionType);
            Assert.NotEqual("MULTI_TRUE_FALSE", q2.QuestionType);
            Assert.NotEqual("TRUE_FALSE", q2.QuestionType);
        }

        [Fact]
        public void EnglishExam_WithPartB_ShouldStaySingle_NotMultiTrueFalse()
        {
            var paragraphs = new List<string>
            {
                "PART A: LISTENING",
                "Question 1. Where are they going this weekend?",
                "A. To the cinema    B. To the park    C. To the beach    D. To the mall",
                "PART B: READING COMPREHENSION",
                "Question 2. What is the main idea of the passage?",
                "A. Climate change    B. Technology    C. Education    D. Healthcare"
            };

            var results = DocxStateMachineClassifier.ParseParagraphList(paragraphs);
            Assert.Equal(2, results.Count);

            var q1 = results[0];
            Assert.Equal("SINGLE", q1.QuestionType);

            var q2 = results[1];
            Assert.Equal("SINGLE", q2.QuestionType);
            Assert.NotEqual("MULTI_TRUE_FALSE", q2.QuestionType);
        }

        [Fact]
        public void Gdpt2025_PartIITrueFalse_ShouldBeRecognizedAsMultiTrueFalse()
        {
            var paragraphs = new List<string>
            {
                "PHẦN II. Câu trắc nghiệm đúng sai. Thí sinh trả lời từ câu 1 đến câu 2. Trong mỗi ý a), b), c), d) ở mỗi câu, thí sinh chọn đúng hoặc sai.",
                "Câu 1. Cho hàm số y = f(x) xác định trên R.",
                "a) Hàm số đồng biến trên (0; +vô cùng).",
                "b) Đồ thị hàm số có tiệm cận đứng.",
                "c) Giá trị nhỏ nhất của hàm số bằng 0.",
                "d) f'(x) có hai nghiệm phân biệt.",
                "Hướng dẫn giải:",
                "a) Đúng. b) Sai. c) Đúng. d) Sai."
            };

            var results = DocxStateMachineClassifier.ParseParagraphList(paragraphs);
            Assert.Single(results);
            var q = results[0];
            Assert.Equal("MULTI_TRUE_FALSE", q.QuestionType);
            Assert.Equal(4, q.Options.Count);
            Assert.Contains("a)", q.Options[0]);
            Assert.Contains("b)", q.Options[1]);
        }

        [Fact]
        public void DynamicIngestionEngine_SingleChoiceSeparateLines_ShouldStaySingle()
        {
            var snippet = @"Câu 1. Đơn vị đo cường độ dòng điện trong hệ SI là:
A. Ampe
B. Vôn
C. Oát
D. Jun
Đáp án: A";

            var engine = DynamicQuestionIngestionEngine.Instance;
            var response = engine.ParseSnippet(snippet);

            Assert.Single(response.Questions);
            var q = response.Questions[0];
            Assert.Equal("SINGLE", q.QuestionType);
            Assert.Equal(4, q.Options.Count);
        }

        [Fact]
        public void ControllerNormalization_SingleWith4Options_ShouldStaySingle()
        {
            var type = QuestionFactory.NormalizeType("SINGLE");
            var item = new DocxImportPreviewDto
            {
                QuestionType = "SINGLE",
                Content = "Thủ đô của Việt Nam là gì?",
                Options = new List<string> { "A. Hà Nội", "B. Huế", "C. Đà Nẵng", "D. Cần Thơ" },
                SuggestedAnswer = "1"
            };

            // Mô phỏng logic chuẩn hóa trong QuestionsController.cs
            if (type == "TRUE_FALSE" && item.Options?.Count >= 3)
            {
                type = "MULTI_TRUE_FALSE";
            }
            else if (!string.IsNullOrWhiteSpace(item.SuggestedAnswer) &&
                     Regex.IsMatch(item.SuggestedAnswer, @"[a-d][\)\:\.\s]+(đúng|sai|true|false)", RegexOptions.IgnoreCase))
            {
                type = "MULTI_TRUE_FALSE";
            }

            Assert.Equal("SINGLE", type);
        }

        [Fact]
        public void English_OrderingQuestion_12_ShouldPreserveItemsInContent_AndExtractOptionsFromBottom()
        {
            var paragraphs = new List<string>
            {
                "Question 12.",
                "Dear Ms Hoang,",
                "a. A payment of $5,000 will be released in the beginning, and the remaining balance after the submission of your mid-term report.",
                "b. Failure to respond by the stated date will result in the offer being granted to the next qualified candidate.",
                "c. We are delighted to inform you that you have been awarded the Young Researcher Grant.",
                "d. To confirm your acceptance, please sign the attached form and submit it by 31 August.",
                "e. The funding is intended to support your fieldwork and must not be used for equipment purchases without prior approval.",
                "Yours sincerely,",
                "A. c – e – a – d – b\tB. c – b – e – d – a\tC. c – a – b – e – d\tD. c – a – b – d – e"
            };

            var results = DocxStateMachineClassifier.ParseParagraphList(paragraphs);
            Assert.Single(results);
            var q = results[0];
            Assert.Equal("SINGLE", q.QuestionType);
            Assert.Equal(4, q.Options.Count);
            Assert.Contains("Dear Ms Hoang,", q.Content);
            Assert.Contains("a. A payment of $5,000", q.Content);
            Assert.Contains("b. Failure to respond", q.Content);
            Assert.Contains("c. We are delighted", q.Content);
            Assert.Contains("d. To confirm your acceptance", q.Content);
            Assert.Contains("e. The funding is intended", q.Content);
            Assert.Contains("Yours sincerely,", q.Content);
            Assert.Equal("c – e – a – d – b", q.Options[0]);
            Assert.Equal("c – b – e – d – a", q.Options[1]);
            Assert.Equal("c – a – b – e – d", q.Options[2]);
            Assert.Equal("c – a – b – d – e", q.Options[3]);
        }

        [Fact]
        public void English_TwoQuestionsConsecutive_ShouldProcessEachBlockBetweenAnchors()
        {
            var paragraphs = new List<string>
            {
                "Question 12.",
                "Dear Ms Hoang,",
                "a. Item A",
                "b. Item B",
                "c. Item C",
                "Yours sincerely,",
                "A. a - b - c\tB. b - c - a\tC. c - a - b\tD. c - b - a",
                "Question 13.",
                "Mark the letter A, B, C, or D on your answer sheet to indicate the correct answer to each of the following questions.",
                "She decided to take up yoga to ______ stress.",
                "A. reduce",
                "B. increase",
                "C. extend",
                "D. destroy"
            };

            var results = DocxStateMachineClassifier.ParseParagraphList(paragraphs);
            Assert.Equal(2, results.Count);

            // Câu 12
            Assert.Equal("SINGLE", results[0].QuestionType);
            Assert.Equal(4, results[0].Options.Count);
            Assert.Contains("Dear Ms Hoang,", results[0].Content);
            Assert.Contains("a. Item A", results[0].Content);
            Assert.Contains("Yours sincerely,", results[0].Content);

            // Câu 13
            Assert.Equal("SINGLE", results[1].QuestionType);
            Assert.Equal(4, results[1].Options.Count);
            Assert.Contains("She decided to take up yoga", results[1].Content);
            Assert.Equal("reduce", results[1].Options[0]);
            Assert.Equal("increase", results[1].Options[1]);
            Assert.Equal("extend", results[1].Options[2]);
            Assert.Equal("destroy", results[1].Options[3]);
        }
    }
}
