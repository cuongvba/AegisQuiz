using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.AI.Adapters
{
    public class ReplicateAdapter : IAiProviderAdapter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string DefaultEndpoint = "https://api.replicate.com/v1/predictions";

        public ReplicateAdapter(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public AiProviderType ProviderType => AiProviderType.Replicate;
        public string DefaultModel => "facebook/musicgen-small";

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

                var payload = new
                {
                    version = modelToUse,
                    input = new
                    {
                        prompt = prompt,
                        model_version = "stereo-large"
                    }
                };

                var client = _httpClientFactory.CreateClient("ReplicateClient");
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Token", apiKey);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                sw.Stop();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, string.Empty, $"[Replicate HTTP {(int)response.StatusCode}] {responseBody}", sw.ElapsedMilliseconds);
                }

                return (true, responseBody, null, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return (false, string.Empty, $"[Replicate Exception] {ex.Message}", sw.ElapsedMilliseconds);
            }
        }
    }
}
