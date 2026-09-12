using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services
{
    // ── IIRTService Interface ──────────────────────────────────────────────────
    public interface IIRTService
    {
        /// <summary>Bắt đầu phiên CAT mới — trả về session_id + câu hỏi đầu tiên.</summary>
        Task<CATStartResult> StartCATSessionAsync(string userId, List<QuestionIRTParam> bank, CATConfigParam config);

        /// <summary>Gửi câu trả lời — trả về theta mới + câu hỏi tiếp theo (hoặc báo cáo kết quả).</summary>
        Task<CATRespondResult> RespondAsync(string sessionId, bool isCorrect, int timeSpentSecs);

        /// <summary>Lấy kết quả cuối phiên CAT.</summary>
        Task<CATReport> GetResultAsync(string sessionId);

        /// <summary>Ước lượng năng lực người học từ lịch sử làm bài (ngoài phiên CAT).</summary>
        Task<AbilityEstimate> EstimateAbilityAsync(List<IRTResponse> responses);
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    public class QuestionIRTParam
    {
        public string QuestionId { get; set; } = string.Empty;
        public double A          { get; set; } = 1.0;
        public double B          { get; set; } = 0.0;
        public double C          { get; set; } = 0.25;
        public string TopicCode  { get; set; } = string.Empty;
        public int    Difficulty { get; set; } = 3;
    }

    public class CATConfigParam
    {
        public int    MinItems     { get; set; } = 5;
        public int    MaxItems     { get; set; } = 20;
        public double SeThreshold  { get; set; } = 0.3;
        public double InitialTheta { get; set; } = 0.0;
        public string? TopicCode  { get; set; }
    }

    public class CATStartResult
    {
        public string  SessionId      { get; set; } = string.Empty;
        public string  QuestionId     { get; set; } = string.Empty;
        public double  CurrentTheta   { get; set; } = 0.0;
        public int     MaxItems       { get; set; }
    }

    public class CATRespondResult
    {
        public bool    Finished       { get; set; }
        public string? NextQuestionId { get; set; }
        public double  CurrentTheta   { get; set; }
        public double  Se             { get; set; }
        public int     NAdministered  { get; set; }
        public CATReport? Report      { get; set; }
    }

    public class CATReport
    {
        public string SessionId    { get; set; } = string.Empty;
        public double Theta        { get; set; }
        public double Se           { get; set; }
        public double Percentile   { get; set; }
        public string Level        { get; set; } = string.Empty;
        public int    NItems       { get; set; }
        public double Accuracy     { get; set; }
        public string FinishReason { get; set; } = string.Empty;
        public string EngineVersion { get; set; } = string.Empty;
    }

    public class IRTResponse
    {
        public double A         { get; set; } = 1.0;
        public double B         { get; set; } = 0.0;
        public double C         { get; set; } = 0.25;
        public bool   IsCorrect { get; set; }
    }

    public class AbilityEstimate
    {
        public double Theta      { get; set; }
        public double Se         { get; set; }
        public double Percentile { get; set; }
        public string Level      { get; set; } = string.Empty;
    }

    // ── IRT Service Implementation ─────────────────────────────────────────────
    /// <summary>
    /// [Phase C] HTTP client tới Python FastAPI IRT/CAT Engine.
    /// Sử dụng IHttpClientFactory ("irt-service" named client từ Program.cs).
    /// </summary>
    public class IrtHttpService : IIRTService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly JsonSerializerOptions _jsonOptions;

        public IrtHttpService(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy       = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true,
            };
        }

        public async Task<CATStartResult> StartCATSessionAsync(
            string userId, List<QuestionIRTParam> bank, CATConfigParam config)
        {
            var payload = new
            {
                user_id       = userId,
                question_bank = bank.Select(q => new
                {
                    question_id = q.QuestionId,
                    a = q.A, b = q.B, c = q.C,
                    topic_code  = q.TopicCode,
                    difficulty  = q.Difficulty
                }),
                config = new
                {
                    min_items     = config.MinItems,
                    max_items     = config.MaxItems,
                    se_threshold  = config.SeThreshold,
                    initial_theta = config.InitialTheta,
                    topic_code    = config.TopicCode
                }
            };

            var response = await PostAsync("/cat/start", payload);
            return JsonSerializer.Deserialize<CATStartResult>(response, _jsonOptions)
                   ?? new CATStartResult();
        }

        public async Task<CATRespondResult> RespondAsync(string sessionId, bool isCorrect, int timeSpentSecs)
        {
            var payload = new
            {
                session_id       = sessionId,
                is_correct       = isCorrect,
                time_spent_secs  = timeSpentSecs
            };

            var response = await PostAsync($"/cat/{sessionId}/respond", payload);
            return JsonSerializer.Deserialize<CATRespondResult>(response, _jsonOptions)
                   ?? new CATRespondResult();
        }

        public async Task<CATReport> GetResultAsync(string sessionId)
        {
            var client   = _httpFactory.CreateClient("irt-service");
            var response = await client.GetStringAsync($"/cat/{sessionId}/result");
            return JsonSerializer.Deserialize<CATReport>(response, _jsonOptions)
                   ?? new CATReport();
        }

        public async Task<AbilityEstimate> EstimateAbilityAsync(List<IRTResponse> responses)
        {
            var payload = responses.Select(r => new
            {
                a          = r.A,
                b          = r.B,
                c          = r.C,
                is_correct = r.IsCorrect ? 1 : 0
            });

            var response = await PostAsync("/irt/ability-estimate", payload);
            return JsonSerializer.Deserialize<AbilityEstimate>(response, _jsonOptions)
                   ?? new AbilityEstimate();
        }

        private async Task<string> PostAsync(string path, object payload)
        {
            var client  = _httpFactory.CreateClient("irt-service");
            var json    = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(path, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
