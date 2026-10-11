using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisQuiz.Infrastructure.AI.Protocols
{
    /// <summary>
    /// Protocol Driver chuẩn hóa tương thích OpenAI cho 95% LLMs (DeepSeek, Groq, Together, Ollama, OpenAI, Mistral...)
    /// </summary>
    public class OpenAiCompatibleProtocolDriver : IModelProtocolDriver
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OpenAiCompatibleProtocolDriver> _logger;

        public ModelProtocolType ProtocolType => ModelProtocolType.OpenAiCompatible;

        public OpenAiCompatibleProtocolDriver(
            IHttpClientFactory httpClientFactory,
            ILogger<OpenAiCompatibleProtocolDriver> logger)
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
                var url = endpointUrl.TrimEnd('/');
                if (!url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                {
                    url += "/chat/completions";
                }

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(45);

                var requestMsg = new HttpRequestMessage(HttpMethod.Post, url);
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    requestMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
                }

                var messages = new System.Collections.Generic.List<object>();
                if (!string.IsNullOrWhiteSpace(systemPrompt))
                {
                    messages.Add(new { role = "system", content = systemPrompt });
                }
                messages.Add(new { role = "user", content = userPrompt });

                var effectiveMaxTokens = maxTokens;
                if (!string.IsNullOrWhiteSpace(endpointUrl) && endpointUrl.Contains("groq.com", StringComparison.OrdinalIgnoreCase))
                {
                    effectiveMaxTokens = Math.Min(effectiveMaxTokens, 800);
                }

                var payload = new
                {
                    model = modelName,
                    messages,
                    temperature,
                    max_tokens = effectiveMaxTokens
                };

                var json = JsonSerializer.Serialize(payload);
                requestMsg.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var resp = await client.SendAsync(requestMsg, ct);
                sw.Stop();

                var respBody = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode)
                {
                    var cleanError = FormatHttpErrorMessage((int)resp.StatusCode, respBody);
                    return (false, string.Empty, cleanError, sw.ElapsedMilliseconds, 0);
                }

                using var doc = JsonDocument.Parse(respBody);
                var root = doc.RootElement;
                if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                {
                    return (false, string.Empty, "Empty choices returned from model", sw.ElapsedMilliseconds, 0);
                }

                var firstChoice = choices[0];
                var content = string.Empty;

                if (firstChoice.TryGetProperty("message", out var msgElement))
                {
                    if (msgElement.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String)
                    {
                        content = contentProp.GetString() ?? string.Empty;
                    }

                    // Fallback cho mô hình Suy luận / CoT (DeepSeek R1 'reasoning_content', Cerebras / Qwen 'reasoning')
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        if (msgElement.TryGetProperty("reasoning_content", out var rcProp) && rcProp.ValueKind == JsonValueKind.String)
                        {
                            content = rcProp.GetString() ?? string.Empty;
                        }
                        else if (msgElement.TryGetProperty("reasoning", out var rProp) && rProp.ValueKind == JsonValueKind.String)
                        {
                            content = rProp.GetString() ?? string.Empty;
                        }
                    }
                }
                else if (firstChoice.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                {
                    content = textProp.GetString() ?? string.Empty;
                }

                var tokensUsed = 0;
                if (root.TryGetProperty("usage", out var usage) && usage.TryGetProperty("total_tokens", out var totalTokens))
                {
                    tokensUsed = totalTokens.GetInt32();
                }

                return (true, content, null, sw.ElapsedMilliseconds, tokensUsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogWarning(ex, "[OpenAiDriver] Error communicating with {Endpoint} model {Model}", endpointUrl, modelName);
                return (false, string.Empty, ex.Message, sw.ElapsedMilliseconds, 0);
            }
        }

        private static string FormatHttpErrorMessage(int statusCode, string rawBody)
        {
            if (string.IsNullOrWhiteSpace(rawBody)) return $"HTTP {statusCode}";
            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                var root = doc.RootElement;
                if (root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                {
                    return $"HTTP {statusCode}: {msgProp.GetString()}";
                }
                if (root.TryGetProperty("error", out var errProp))
                {
                    if (errProp.ValueKind == JsonValueKind.String)
                        return $"HTTP {statusCode}: {errProp.GetString()}";
                    if (errProp.TryGetProperty("message", out var errMsg) && errMsg.ValueKind == JsonValueKind.String)
                        return $"HTTP {statusCode}: {errMsg.GetString()}";
                }
            }
            catch
            {
                // Fallback nếu không phải JSON
            }
            return $"HTTP {statusCode}: {rawBody}";
        }
    }
}
