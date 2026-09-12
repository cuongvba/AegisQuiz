using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AegisQuiz.Application.DTOs
{
    public class QuestionIrtDto
    {
        [JsonPropertyName("question_id")]
        public string QuestionId { get; set; } = string.Empty;

        [JsonPropertyName("a")]
        public double A { get; set; } = 1.0;

        [JsonPropertyName("b")]
        public double B { get; set; } = 0.0;

        [JsonPropertyName("c")]
        public double C { get; set; } = 0.25;

        [JsonPropertyName("topic_code")]
        public string TopicCode { get; set; } = string.Empty;

        [JsonPropertyName("difficulty")]
        public int Difficulty { get; set; } = 3;
    }

    public class CatConfigDto
    {
        [JsonPropertyName("min_items")]
        public int MinItems { get; set; } = 5;

        [JsonPropertyName("max_items")]
        public int MaxItems { get; set; } = 20;

        [JsonPropertyName("se_threshold")]
        public double SeThreshold { get; set; } = 0.3;

        [JsonPropertyName("initial_theta")]
        public double InitialTheta { get; set; } = 0.0;

        [JsonPropertyName("topic_code")]
        public string? TopicCode { get; set; }
    }

    public class StartCatSessionRequest
    {
        public Guid UserId { get; set; }
        public string? TopicCode { get; set; }
        public int? MinItems { get; set; }
        public int? MaxItems { get; set; }
        public double? SeThreshold { get; set; }
        public double? InitialTheta { get; set; }
    }

    public class CatStartResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public object? Question { get; set; }
        public double CurrentTheta { get; set; }
        public int AdministeredCount { get; set; }
        public int MaxItems { get; set; }
        public bool Finished { get; set; }
    }

    public class SubmitCatAnswerRequest
    {
        public Guid QuestionId { get; set; }
        public string SelectedAnswer { get; set; } = string.Empty;
        public int TimeSpentSeconds { get; set; } = 0;
    }

    public class CatStepResponse
    {
        public bool Finished { get; set; }
        public object? NextQuestion { get; set; }
        public double CurrentTheta { get; set; }
        public double Se { get; set; }
        public int AdministeredCount { get; set; }
        public bool LastIsCorrect { get; set; }
        public string? LastExplanation { get; set; }
        public object? FinalReport { get; set; }
    }
}
