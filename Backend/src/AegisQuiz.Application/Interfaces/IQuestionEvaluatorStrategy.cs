using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.Interfaces
{
    public enum EvaluationStatus
    {
        Scored,
        Manual
    }

    public record EvaluationResult(
        double Score,
        double MaxScore,
        EvaluationStatus Status,
        bool IsAnswered
    );

    public class LearnerSubmissionDto
    {
        public string? SelectedOption { get; set; }
        public List<string>? SelectedOptions { get; set; }
        public string? ShortText { get; set; }
        public List<string>? Ordering { get; set; }
        public Dictionary<string, string>? Matching { get; set; }
        public Dictionary<string, bool>? MatrixAnswer { get; set; }
        public string? Essay { get; set; }
    }

    public record QuestionEvaluationContext(
        string QuestionType,
        string AnswerRaw,
        List<string>? Options,
        double Points,
        string? PayloadJson,
        LearnerSubmissionDto? Submission
    );

    /// <summary>
    /// [P1 Supreme Scoring Sovereign] Interface Strategy chấm điểm cho từng QuestionType độc lập theo OCP.
    /// </summary>
    public interface IQuestionEvaluatorStrategy
    {
        bool CanHandle(string questionType);
        EvaluationResult Evaluate(QuestionEvaluationContext context);
    }

    /// <summary>
    /// [P1 Supreme Scoring Sovereign] Dispatcher trung tâm điều phối Strategy tương ứng.
    /// </summary>
    public interface IQuestionEvaluationDispatcher
    {
        EvaluationResult Evaluate(QuestionEvaluationContext context);
    }
}
