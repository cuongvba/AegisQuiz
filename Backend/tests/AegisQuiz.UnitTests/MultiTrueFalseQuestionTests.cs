using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Domain.Factories;

namespace AegisQuiz.UnitTests
{
    public class MultiTrueFalseQuestionTests
    {
        [Fact]
        public void MultiTrueFalse_Factory_Should_Create_Correct_Instance()
        {
            var q = QuestionFactory.CreateQuestion("MULTI_TRUE_FALSE", "Cho hàm số f(x)...");
            Assert.IsType<MultiTrueFalseQuestion>(q);
            Assert.Equal("MULTI_TRUE_FALSE", q.QuestionType);

            Assert.Equal("MULTI_TRUE_FALSE", QuestionFactory.NormalizeType("MULTI_TRUE_FALSE"));
            Assert.Equal("MULTI_TRUE_FALSE", QuestionFactory.NormalizeType("TF_CLUSTER"));
            Assert.Equal("MULTI_TRUE_FALSE", QuestionFactory.NormalizeType("DUNG_SAI_DA_Y"));
        }

        [Theory]
        [InlineData("{\"a\":true,\"b\":false,\"c\":false,\"d\":false}", 0.25)] // 1 ý đúng / 4
        [InlineData("{\"a\":true,\"b\":true,\"c\":false,\"d\":false}", 0.50)]  // 2 ý đúng / 4
        [InlineData("{\"a\":true,\"b\":true,\"c\":true,\"d\":false}", 0.75)]   // 3 ý đúng / 4
        [InlineData("{\"a\":true,\"b\":true,\"c\":true,\"d\":true}", 1.00)]    // 4 ý đúng / 4
        [InlineData("{\"a\":false,\"b\":false,\"c\":false,\"d\":false}", 0.0)] // 0 ý đúng / 4
        public void EvaluateAnswer_4Options_ProportionalScoring_Should_Calculate_Correctly(
            string studentAnswerJson, double expectedScore)
        {
            // Đáp án chuẩn: a: Đúng, b: Đúng, c: Đúng, d: Đúng
            var q = new MultiTrueFalseQuestion
            {
                Options = new List<string>
                {
                    "a) Mệnh đề A",
                    "b) Mệnh đề B",
                    "c) Mệnh đề C",
                    "d) Mệnh đề D"
                },
                CorrectOption = "a) Đúng, b) Đúng, c) Đúng, d) Đúng"
            };

            double score = q.EvaluateAnswer(studentAnswerJson);
            Assert.Equal(expectedScore, score, precision: 2);
        }

        [Fact]
        public void EvaluateAnswer_5Options_FlexibleExpansion_Should_Calculate_Proportionally()
        {
            // Kiểm tra tính linh động mở rộng 5 options (a, b, c, d, e)
            var q = new MultiTrueFalseQuestion
            {
                Options = new List<string>
                {
                    "a) Ý 1",
                    "b) Ý 2",
                    "c) Ý 3",
                    "d) Ý 4",
                    "e) Ý 5"
                },
                CorrectOption = "a: Đúng, b: Sai, c: Đúng, d: Sai, e: Đúng"
            };

            // Học sinh đúng 3 ý: a (Đúng), b (Sai), c (Đúng) nhưng d chọn Đúng (sai), e chọn Sai (sai)
            string studentAns = JsonSerializer.Serialize(new Dictionary<string, bool>
            {
                ["a"] = true,
                ["b"] = false,
                ["c"] = true,
                ["d"] = true,  // mong đợi: false
                ["e"] = false  // mong đợi: true
            });

            double score = q.EvaluateAnswer(studentAns);
            // 3 ý đúng trên tổng số 5 option = 3/5 = 0.60
            Assert.Equal(0.60, score, precision: 2);
        }

        [Fact]
        public void EvaluateAnswer_With_MatchingFormat_Should_Parse_Correctly()
        {
            var q = new MultiTrueFalseQuestion
            {
                Options = new List<string> { "a) Ý 1", "b) Ý 2", "c) Ý 3", "d) Ý 4" },
                CorrectOption = "a) Đúng, b) Sai, c) Đúng, d) Sai"
            };

            // Dạng frontend gửi: {"matching": {"a": "TRUE", "b": "FALSE", "c": "TRUE", "d": "TRUE"}}
            // Đúng a, b, c (3 ý); d chọn TRUE trong khi chuẩn là FALSE -> 3 ý đúng / 4 = 0.75
            string studentAns = "{\"matching\": {\"a\": \"TRUE\", \"b\": \"FALSE\", \"c\": \"TRUE\", \"d\": \"TRUE\"}}";

            double score = q.EvaluateAnswer(studentAns);
            Assert.Equal(0.75, score, precision: 2);
        }

        [Fact]
        public void EvaluateAnswer_TNPT_2025_Scheme_WhenConfigured_Should_Use_NonlinearLadder()
        {
            // Barem đặc thù TNPT 2025: 1 ý = 0.1, 2 ý = 0.25, 3 ý = 0.5, 4 ý = 1.0
            var q = new MultiTrueFalseQuestion
            {
                Options = new List<string> { "a) Ý 1", "b) Ý 2", "c) Ý 3", "d) Ý 4" },
                CorrectOption = "a: Đúng, b: Đúng, c: Đúng, d: Đúng",
                Payload = JsonSerializer.SerializeToDocument(new
                {
                    scoringScheme = "TNPT_2025",
                    correctAnswers = new Dictionary<string, bool>
                    {
                        ["a"] = true,
                        ["b"] = true,
                        ["c"] = true,
                        ["d"] = true
                    }
                })
            };

            // Đúng 1 ý -> 0.1
            Assert.Equal(0.1, q.EvaluateAnswer("{\"a\": true, \"b\": false, \"c\": false, \"d\": false}"));

            // Đúng 2 ý -> 0.25
            Assert.Equal(0.25, q.EvaluateAnswer("{\"a\": true, \"b\": true, \"c\": false, \"d\": false}"));

            // Đúng 3 ý -> 0.5
            Assert.Equal(0.5, q.EvaluateAnswer("{\"a\": true, \"b\": true, \"c\": true, \"d\": false}"));

            // Đúng cả 4 ý -> 1.0
            Assert.Equal(1.0, q.EvaluateAnswer("{\"a\": true, \"b\": true, \"c\": true, \"d\": true}"));
        }

        [Fact]
        public void Legacy_TrueFalseQuestion_WithMultiOptions_Should_AutoDelegate_ToMultiTf()
        {
            // Dữ liệu cũ đã lưu dạng TrueFalseQuestion nhưng có 4 options
            var legacyQ = new TrueFalseQuestion
            {
                Options = new List<string> { "a) Ý 1", "b) Ý 2", "c) Ý 3", "d) Ý 4" },
                CorrectOption = "a) Đúng, b) Sai, c) Đúng, d) Đúng"
            };

            // Học sinh đúng 2 ý trên 4 ý
            string ans = "{\"a\": true, \"b\": false, \"c\": false, \"d\": false}";
            double score = legacyQ.EvaluateAnswer(ans);

            Assert.Equal(0.50, score, precision: 2);
        }
    }
}
