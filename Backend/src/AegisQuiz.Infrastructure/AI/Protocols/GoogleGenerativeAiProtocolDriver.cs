using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisQuiz.Infrastructure.AI.Protocols
{
    /// <summary>
    /// Protocol Driver chuẩn hóa tương thích Google Gemini Native REST
    /// </summary>
    public class GoogleGenerativeAiProtocolDriver : IModelProtocolDriver
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GoogleGenerativeAiProtocolDriver> _logger;

        public ModelProtocolType ProtocolType => ModelProtocolType.GoogleGenerativeAi;

        public GoogleGenerativeAiProtocolDriver(
            IHttpClientFactory httpClientFactory,
            ILogger<GoogleGenerativeAiProtocolDriver> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(bool Success, string Content, string? Error, long DurationMs, int TokensUsed)> ExecuteAsync(
            string endpointUrl,
            string apiKey,
            string modelName,
            string systemPrompt,
            string userPrompt,
            double temperature = 0.2,
            int maxTokens = 2048,
            CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var baseUrl = endpointUrl.TrimEnd('/');
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    baseUrl = "https://generativelanguage.googleapis.com/v1beta";
                }

                var url = $"{baseUrl}/models/{modelName}:generateContent?key={apiKey.Trim()}";

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(45);

                var promptContent = string.IsNullOrWhiteSpace(systemPrompt)
                    ? userPrompt
                    : $"[System Instruction]\n{systemPrompt}\n\n[User Task]\n{userPrompt}";

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = promptContent }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature,
                        maxOutputTokens = maxTokens
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var contentReq = new StringContent(json, Encoding.UTF8, "application/json");

                var resp = await client.PostAsync(url, contentReq, ct);
                sw.Stop();

                var respBody = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode)
                {
                    return (false, string.Empty, $"HTTP {(int)resp.StatusCode}: {respBody}", sw.ElapsedMilliseconds, 0);
                }

                using var doc = JsonDocument.Parse(respBody);
                var root = doc.RootElement;
                if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    return (false, string.Empty, "No candidates returned from Gemini", sw.ElapsedMilliseconds, 0);
                }

                var firstCandidate = candidates[0];
                var resultText = string.Empty;
                if (firstCandidate.TryGetProperty("content", out var contentElem) &&
                    contentElem.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var textProp))
                {
                    resultText = textProp.GetString() ?? string.Empty;
                }

                var totalTokens = 0;
                if (root.TryGetProperty("usageMetadata", out var usage) && usage.TryGetProperty("totalTokenCount", out var tokenCount))
                {
                    totalTokens = tokenCount.GetInt32();
                }

                return (true, resultText, null, sw.ElapsedMilliseconds, totalTokens);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogWarning(ex, "[GoogleGeminiDriver] Error communicating with {Endpoint} model {Model}", endpointUrl, modelName);
                return (false, string.Empty, ex.Message, sw.ElapsedMilliseconds, 0);
            }
        }
    }
}
