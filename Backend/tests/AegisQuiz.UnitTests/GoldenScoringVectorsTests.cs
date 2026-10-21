using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Services.Scoring;

namespace AegisQuiz.UnitTests
{
    public class GoldenScoringVectorsTests
    {
        private readonly IQuestionEvaluationDispatcher _dispatcher;

        public GoldenScoringVectorsTests()
        {
            var strategies = new List<IQuestionEvaluatorStrategy>
            {
                new SingleChoiceEvaluatorStrategy(),
                new MultiChoiceEvaluatorStrategy(),
                new MultiTrueFalseEvaluatorStrategy(),
                new ShortAnswerEvaluatorStrategy(),
                new OrderingEvaluatorStrategy(),
                new MatchingEvaluatorStrategy(),
                new EssayEvaluatorStrategy()
            };
            _dispatcher = new QuestionEvaluationDispatcher(strategies);
        }

        private static string FindVectorFilePath()
        {
            var directoriesToCheck = new[] { AppDomain.CurrentDomain.BaseDirectory, Directory.GetCurrentDirectory() };
            foreach (var dir in directoriesToCheck)
            {
                var current = dir;
                for (int i = 0; i < 10; i++)
                {
                    var candidate = Path.Combine(current, "tests", "scoring-vectors", "scoring-vectors.v1.json");
                    if (File.Exists(candidate)) return candidate;
                    var parent = Directory.GetParent(current);
                    if (parent == null) break;
                    current = parent.FullName;
                }
            }
            throw new FileNotFoundException("Cannot find scoring-vectors.v1.json");
        }

        public class VectorCaseDto
        {
            public string id { get; set; } = string.Empty;
            public QuestionDto question { get; set; } = new();
            public AnswerDto? answer { get; set; }
            public ExpectedDto expected { get; set; } = new();
        }

        public class QuestionDto
        {
            public string questionType { get; set; } = string.Empty;
            public List<string>? options { get; set; }
            public string answerRaw { get; set; } = string.Empty;
            public double points { get; set; }
            public JsonElement? payload { get; set; }
        }

        public class AnswerDto
        {
            public string? selectedOption { get; set; }
            public List<string>? selectedOptions { get; set; }
            public string? shortText { get; set; }
            public List<string>? ordering { get; set; }
            public Dictionary<string, string>? matching { get; set; }
            public Dictionary<string, bool>? matrixAnswer { get; set; }
            public string? essay { get; set; }
        }

        public class ExpectedDto
        {
            public double score { get; set; }
            public string status { get; set; } = string.Empty;
            public bool answered { get; set; }
        }

        public class VectorFileRoot
        {
            public int version { get; set; }
            public List<VectorCaseDto> cases { get; set; } = new();
        }

        [Fact]
        public void All_Golden_Vectors_Should_Evaluate_Identically_To_Frontend()
        {
            var filePath = FindVectorFilePath();
            var json = File.ReadAllText(filePath);
            var root = JsonSerializer.Deserialize<VectorFileRoot>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(root);
            Assert.True(root.cases.Count >= 30, "Golden vectors should contain at least 30 test cases.");

            foreach (var c in root.cases)
            {
                var submission = c.answer == null ? null : new LearnerSubmissionDto
                {
                    SelectedOption = c.answer.selectedOption,
                    SelectedOptions = c.answer.selectedOptions,
                    ShortText = c.answer.shortText,
                    Ordering = c.answer.ordering,
                    Matching = c.answer.matching,
                    MatrixAnswer = c.answer.matrixAnswer,
                    Essay = c.answer.essay
                };

                var payloadJson = c.question.payload.HasValue ? c.question.payload.Value.GetRawText() : null;

                var ctx = new QuestionEvaluationContext(
                    c.question.questionType,
                    c.question.answerRaw,
                    c.question.options,
                    c.question.points,
                    payloadJson,
                    submission
                );

                var result = _dispatcher.Evaluate(ctx);

                Assert.True(
                    Math.Abs(result.Score - c.expected.score) < 0.01,
                    $"[{c.id}] Score mismatch: expected {c.expected.score}, got {result.Score}");

                var expectedStatus = c.expected.status.Equals("manual", StringComparison.OrdinalIgnoreCase)
                    ? EvaluationStatus.Manual
                    : EvaluationStatus.Scored;

                Assert.True(
                    result.Status == expectedStatus,
                    $"[{c.id}] Status mismatch: expected {expectedStatus}, got {result.Status}");

                Assert.True(
                    result.IsAnswered == c.expected.answered,
                    $"[{c.id}] IsAnswered mismatch: expected {c.expected.answered}, got {result.IsAnswered}");
            }
        }
    }
}
