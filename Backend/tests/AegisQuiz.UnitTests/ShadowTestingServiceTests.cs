using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace AegisQuiz.UnitTests
{
    public class ShadowTestingServiceTests
    {
        private readonly ShadowTestingService _service;
        private readonly ITestOutputHelper _output;

        public ShadowTestingServiceTests(ITestOutputHelper output)
        {
            _output = output;
            var logger = NullLogger<ShadowTestingService>.Instance;
            _service = new ShadowTestingService(logger);
        }

        [Fact]
        public async Task ShadowTest_WithSafeNewRule_ShouldPassWithHighScoreAndZeroRegression()
        {
            // Arrange: Một quy tắc chuyên biệt không xung đột với các đề thi quốc gia hiện có
            var proposedRule = new QuestionParsingRuleDto
            {
                RuleCode = "CUSTOM_ACADEMY_EXAM",
                RuleName = "Đề thi Học viện Quốc tế",
                Priority = 50,
                AnchorSpec = new AnchorSpecDto
                {
                    RegexPattern = @"^(?:Task\s*#?\s*(\d+)[\.\:\-]|Bài\s*tập\s*(\d+)[\.\:\-])\s*(.*)"
                },
                InteractionSpec = new InteractionSpecDto
                {
                    OptionRegexPattern = @"^[A-D]\.\s*(.*)"
                }
            };

            var request = new ShadowTestRequestDto
            {
                ProposedRule = proposedRule,
                AutoPromote = false
            };

            // Act
            var result = await _service.RunShadowTestAsync(request);

            // Assert
            Assert.True(result.Passed);
            Assert.True(result.SafetyScore >= 95.0);
            Assert.Equal(0, result.RegressionsCount);
            Assert.False(result.AutoPromoted);
            Assert.Contains("an toàn tuyệt đối", result.Recommendation);
        }

        [Fact]
        public async Task ShadowTest_WithInvalidRegex_ShouldFailGracefully()
        {
            // Arrange: Regex bị hỏng cú pháp (unclosed parenthesis)
            var proposedRule = new QuestionParsingRuleDto
            {
                RuleCode = "INVALID_REGEX_RULE",
                RuleName = "Quy tắc lỗi regex",
                AnchorSpec = new AnchorSpecDto
                {
                    RegexPattern = @"^(?:Task\s*#?\s*(\d+[" // Thiếu ngoặc đóng
                }
            };

            var request = new ShadowTestRequestDto
            {
                ProposedRule = proposedRule
            };

            // Act
            var result = await _service.RunShadowTestAsync(request);

            // Assert
            Assert.False(result.Passed);
            Assert.Equal(0.0, result.SafetyScore);
            Assert.Contains("không hợp lệ", result.Recommendation);
        }

        [Fact]
        public async Task ShadowTest_WithTargetSamples_ShouldVerifyExtractionAndReportDetails()
        {
            // Arrange
            var proposedRule = new QuestionParsingRuleDto
            {
                RuleCode = "CUSTOM_MATH_OLYMPIAD",
                RuleName = "Đề thi Olympic Toán",
                AnchorSpec = new AnchorSpecDto
                {
                    RegexPattern = @"^Bài\s*toán\s*(\d+)[\.\:\-]\s*(.*)"
                },
                InteractionSpec = new InteractionSpecDto
                {
                    OptionRegexPattern = @"^[A-D]\.\s*(.*)"
                }
            };

            var sampleText = "Bài toán 1. Tính giá trị của biểu thức 2 + 2 = ?\nA. 4\nB. 3\nC. 2\nD. 1\nLời giải: 2+2=4";

            var request = new ShadowTestRequestDto
            {
                ProposedRule = proposedRule,
                CustomTestSamples = new List<string> { sampleText },
                AutoPromote = false
            };

            // Act
            var result = await _service.RunShadowTestAsync(request);

            // Assert
            Assert.True(result.Passed);
            Assert.Equal(1, result.TargetMatchesCount);
            Assert.NotEmpty(result.Details);
            var targetDetail = result.Details.Find(d => d.SampleName == "Target Sample");
            Assert.NotNull(targetDetail);
            Assert.True(targetDetail.IsTargetMatch);
            Assert.Equal("PASS", targetDetail.Status);
        }

        [Fact]
        public async Task ShadowTest_WithAutoPromote_ShouldPromoteRuleWhenSafe()
        {
            // Arrange
            var proposedRule = new QuestionParsingRuleDto
            {
                RuleCode = "SAFE_AUTO_PROMOTE_TEST_RULE",
                RuleName = "Quy tắc khảo sát",
                AnchorSpec = new AnchorSpecDto
                {
                    RegexPattern = @"^Mục\s*Khảo\s*Sát\s*(\d+)[\.\:\-]\s*(.*)"
                },
                InteractionSpec = new InteractionSpecDto
                {
                    OptionRegexPattern = @"^[A-D]\.\s*(.*)"
                }
            };

            var request = new ShadowTestRequestDto
            {
                ProposedRule = proposedRule,
                AutoPromote = true
            };

            // Act
            var result = await _service.RunShadowTestAsync(request);

            // Assert
            Assert.True(result.Passed);
            Assert.True(result.AutoPromoted);
        }
    }
}
