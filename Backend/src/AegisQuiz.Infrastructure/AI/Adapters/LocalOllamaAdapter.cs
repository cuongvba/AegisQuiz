using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.AI.Adapters
{
    public class LocalOllamaAdapter : IAiProviderAdapter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string DefaultEndpoint = "http://localhost:11434/v1/chat/completions";

        public LocalOllamaAdapter(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public AiProviderType ProviderType => AiProviderType.LocalOllama;
        public string DefaultModel => "qwen2.5:14b";

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

                var messages = new System.Collections.Generic.List<object>();
                if (!string.IsNullOrWhiteSpace(systemPrompt))
                {
                    messages.Add(new { role = "system", content = systemPrompt });
                }
                messages.Add(new { role = "user", content = prompt });

                var payload = new
                {
                    model = modelToUse,
                    messages = messages,
                    temperature = temperature,
                    max_tokens = maxTokens,
                    stream = false
                };

                var client = _httpClientFactory.CreateClient("OllamaClient");
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                sw.Stop();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, string.Empty, $"[Ollama HTTP {(int)response.StatusCode}] {responseBody}", sw.ElapsedMilliseconds);
                }

                using var doc = JsonDocument.Parse(responseBody);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                return (true, content, null, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return (false, string.Empty, $"[Local Ollama Exception] {ex.Message} (Đảm bảo Ollama đang chạy tại {customEndpoint ?? DefaultEndpoint})", sw.ElapsedMilliseconds);
            }
        }
    }
}
