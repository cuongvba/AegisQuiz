using System;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
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
    }
}
