using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.AI.Adapters
{
    public class AnthropicClaudeAdapter : IAiProviderAdapter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string DefaultEndpoint = "https://api.anthropic.com/v1/messages";

        public AnthropicClaudeAdapter(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public AiProviderType ProviderType => AiProviderType.AnthropicClaude;
        public string DefaultModel => "claude-3-5-sonnet-20241022";

        public async Task<(bool Success, string Content, string? Error, long DurationMs)> CallAsync(
            string apiKey,
            string? model,
            string prompt,
            string? systemPrompt,
            double temperature,
            int maxTokens,
            string? customEndpoint = null)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var endpoint = string.IsNullOrWhiteSpace(customEndpoint) ? DefaultEndpoint : customEndpoint;
                var modelToUse = string.IsNullOrWhiteSpace(model) ? DefaultModel : model;

                var messages = new[]
                {
                    new { role = "user", content = prompt }
                };

                var payload = new
                {
                    model = modelToUse,
                    messages = messages,
                    system = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt,
                    temperature = temperature,
                    max_tokens = maxTokens
                };

                var client = _httpClientFactory.CreateClient("AnthropicClient");
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("x-api-key", apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                sw.Stop();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, string.Empty, $"[Claude HTTP {(int)response.StatusCode}] {responseBody}", sw.ElapsedMilliseconds);
                }

                using var doc = JsonDocument.Parse(responseBody);
                var content = doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                return (true, content, null, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return (false, string.Empty, $"[Claude Exception] {ex.Message}", sw.ElapsedMilliseconds);
            }
        }
    }
}
