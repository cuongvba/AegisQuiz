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
    /// Protocol Driver chuẩn hóa tương thích Anthropic Claude Native Messages REST
    /// </summary>
    public class AnthropicMessagesProtocolDriver : IModelProtocolDriver
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AnthropicMessagesProtocolDriver> _logger;

        public ModelProtocolType ProtocolType => ModelProtocolType.AnthropicMessages;

        public AnthropicMessagesProtocolDriver(
            IHttpClientFactory httpClientFactory,
            ILogger<AnthropicMessagesProtocolDriver> logger)
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
                if (!url.EndsWith("/messages", StringComparison.OrdinalIgnoreCase))
                {
                    url += "/messages";
                }

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(45);

                var requestMsg = new HttpRequestMessage(HttpMethod.Post, url);
                requestMsg.Headers.Add("x-api-key", apiKey.Trim());
                requestMsg.Headers.Add("anthropic-version", "2023-06-01");

                var payload = new
                {
                    model = modelName,
                    max_tokens = maxTokens,
                    temperature,
                    system = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt,
                    messages = new[]
                    {
                        new { role = "user", content = userPrompt }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                requestMsg.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var resp = await client.SendAsync(requestMsg, ct);
                sw.Stop();

                var respBody = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode)
                {
                    return (false, string.Empty, $"HTTP {(int)resp.StatusCode}: {respBody}", sw.ElapsedMilliseconds, 0);
                }

                using var doc = JsonDocument.Parse(respBody);
                var root = doc.RootElement;
                if (!root.TryGetProperty("content", out var contentArray) || contentArray.GetArrayLength() == 0)
                {
                    return (false, string.Empty, "No content blocks returned from Claude", sw.ElapsedMilliseconds, 0);
                }

                var resultText = string.Empty;
                if (contentArray[0].TryGetProperty("text", out var textProp))
                {
                    resultText = textProp.GetString() ?? string.Empty;
                }
                var tokensUsed = 0;
                if (root.TryGetProperty("usage", out var usage))
                {
                    var inputTokens = usage.TryGetProperty("input_tokens", out var inT) ? inT.GetInt32() : 0;
                    var outputTokens = usage.TryGetProperty("output_tokens", out var outT) ? outT.GetInt32() : 0;
                    tokensUsed = inputTokens + outputTokens;
                }

                return (true, resultText, null, sw.ElapsedMilliseconds, tokensUsed);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogWarning(ex, "[AnthropicDriver] Error communicating with {Endpoint} model {Model}", endpointUrl, modelName);
                return (false, string.Empty, ex.Message, sw.ElapsedMilliseconds, 0);
            }
        }
    }
}
