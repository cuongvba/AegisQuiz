using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services
{
    public class IrtClientService : IIrtClientService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IrtClientService> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public IrtClientService(IHttpClientFactory httpClientFactory, ILogger<IrtClientService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private HttpClient GetClient() => _httpClientFactory.CreateClient("irt-service");

        public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var client = GetClient();
                var resp = await client.GetAsync("/health", cancellationToken);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[IrtClientService] Health check failed to IRT service.");
                return false;
            }
        }

        public async Task<CatStartResponse?> StartCatSessionAsync(
            string userId,
            List<QuestionIrtDto> questionBank,
            CatConfigDto config,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = GetClient();
                var payload = new
                {
                    user_id = userId,
                    question_bank = questionBank,
                    config
                };

                var resp = await client.PostAsJsonAsync("/cat/start", payload, cancellationToken);
                if (!resp.IsSuccessStatusCode)
                {
                    var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("[IrtClientService] /cat/start failed: {Status} - {Error}", resp.StatusCode, err);
                    return null;
                }

                using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var root = doc.RootElement;

                var result = new CatStartResponse
                {
                    SessionId = root.GetProperty("session_id").GetString() ?? string.Empty,
                    CurrentTheta = root.GetProperty("current_theta").GetDouble(),
                    AdministeredCount = root.GetProperty("n_administered").GetInt32(),
                    MaxItems = root.GetProperty("max_items").GetInt32(),
                    Finished = false
                };

                if (root.TryGetProperty("question", out var qElem))
                {
                    result.Question = JsonSerializer.Deserialize<object>(qElem.GetRawText(), JsonOptions);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IrtClientService] Exception calling /cat/start");
                return null;
            }
        }

        public async Task<CatStepResponse?> SubmitAnswerAndGetNextAsync(
            string sessionId,
            bool isCorrect,
            int timeSpentSecs,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = GetClient();
                var payload = new
                {
                    session_id = sessionId,
                    is_correct = isCorrect,
                    time_spent_secs = timeSpentSecs
                };

                var resp = await client.PostAsJsonAsync($"/cat/{sessionId}/respond", payload, cancellationToken);
                if (!resp.IsSuccessStatusCode)
                {
                    var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("[IrtClientService] /cat/{SessionId}/respond failed: {Status} - {Error}", sessionId, resp.StatusCode, err);
                    return null;
                }

                using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var root = doc.RootElement;

                bool finished = root.GetProperty("finished").GetBoolean();
                var result = new CatStepResponse
                {
                    Finished = finished
                };

                if (root.TryGetProperty("theta", out var thetaElem))
                    result.CurrentTheta = thetaElem.GetDouble();
                else if (root.TryGetProperty("current_theta", out var curTheta))
                    result.CurrentTheta = curTheta.GetDouble();

                if (root.TryGetProperty("se", out var seElem))
                    result.Se = seElem.GetDouble();

                if (root.TryGetProperty("n_administered", out var adminElem))
                    result.AdministeredCount = adminElem.GetInt32();
                else if (root.TryGetProperty("n_items", out var nItemsElem))
                    result.AdministeredCount = nItemsElem.GetInt32();

                if (root.TryGetProperty("question", out var qElem))
                {
                    result.NextQuestion = JsonSerializer.Deserialize<object>(qElem.GetRawText(), JsonOptions);
                }

                if (root.TryGetProperty("report", out var repElem))
                {
                    result.FinalReport = JsonSerializer.Deserialize<object>(repElem.GetRawText(), JsonOptions);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IrtClientService] Exception calling /cat/{SessionId}/respond", sessionId);
                return null;
            }
        }

        public async Task<object?> GetSessionResultAsync(
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = GetClient();
                var resp = await client.GetAsync($"/cat/{sessionId}/result", cancellationToken);
                if (!resp.IsSuccessStatusCode) return null;

                using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonSerializer.DeserializeAsync<object>(stream, JsonOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IrtClientService] Exception calling /cat/{SessionId}/result", sessionId);
                return null;
            }
        }
    }
}
