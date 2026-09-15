using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.AI.Adapters
{
    public class GoogleGeminiAdapter : IAiProviderAdapter
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GoogleGeminiAdapter(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public AiProviderType ProviderType => AiProviderType.GoogleGemini;
        public string DefaultModel => "gemini-2.0-flash";

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
                var modelToUse = string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
                var endpoint = string.IsNullOrWhiteSpace(customEndpoint)
                    ? $"https://generativelanguage.googleapis.com/v1beta/models/{modelToUse}:generateContent?key={apiKey}"
                    : customEndpoint;

                var fullPrompt = string.IsNullOrWhiteSpace(systemPrompt)
                    ? prompt
                    : $"System Instruction:\n{systemPrompt}\n\nUser Request:\n{prompt}";

                var payload = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = fullPrompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = temperature,
                        maxOutputTokens = maxTokens,
                        responseMimeType = "text/plain"
                    }
                };

                var client = _httpClientFactory.CreateClient("GeminiClient");
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                sw.Stop();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, string.Empty, $"[Gemini HTTP {(int)response.StatusCode}] {responseBody}", sw.ElapsedMilliseconds);
                }

                using var doc = JsonDocument.Parse(responseBody);
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                return (true, text, null, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return (false, string.Empty, $"[Gemini Exception] {ex.Message}", sw.ElapsedMilliseconds);
            }
        }
    }
}
