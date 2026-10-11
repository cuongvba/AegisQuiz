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
    /// <summary>
    /// Universal OpenAI-Compatible Adapter: Động cơ vạn năng cho mọi mô hình AI tuân thủ chuẩn OpenAI REST API
    /// (OpenAI, DeepSeek, Groq, SambaNova, Cerebras, Mistral, GitHub Models, Together AI, Fireworks, Ollama v1...).
    /// Chỉ cần cung cấp Endpoint, Model và API Key.
    /// </summary>
    public class OpenAiCompatibleAdapter : IAiProviderAdapter
    {
        protected readonly IHttpClientFactory HttpClientFactory;
        public virtual AiProviderType ProviderType { get; }
        public virtual string ProviderCode { get; }
        public virtual string DefaultModel { get; }
        public virtual string DefaultEndpoint { get; }

        public OpenAiCompatibleAdapter(
            IHttpClientFactory httpClientFactory,
            AiProviderType providerType = AiProviderType.OpenAI,
            string defaultModel = "gpt-4o-mini",
            string defaultEndpoint = "https://api.openai.com/v1/chat/completions")
        {
            HttpClientFactory = httpClientFactory;
            ProviderType = providerType;
            ProviderCode = providerType.ToString().ToLowerInvariant();
            DefaultModel = defaultModel;
            DefaultEndpoint = defaultEndpoint;
        }

        public OpenAiCompatibleAdapter(
            IHttpClientFactory httpClientFactory,
            string providerCode,
            string defaultModel = "gpt-4o-mini",
            string defaultEndpoint = "https://api.openai.com/v1/chat/completions")
        {
            HttpClientFactory = httpClientFactory;
            ProviderCode = string.IsNullOrWhiteSpace(providerCode) ? "openai" : providerCode.ToLowerInvariant();
            ProviderType = AiProviderType.OpenAI;
            DefaultModel = defaultModel;
            DefaultEndpoint = defaultEndpoint;
        }

        public virtual async Task<(bool Success, string Content, string? Error, long DurationMs)> CallAsync(
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
                endpoint = NormalizeEndpoint(endpoint);
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

                var client = HttpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(60);

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                if (!string.IsNullOrWhiteSpace(apiKey) && apiKey != "local" && apiKey != "local_no_key_required")
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
                }

                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                sw.Stop();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, string.Empty, $"[{ProviderType} HTTP {(int)response.StatusCode}] {responseBody}", sw.ElapsedMilliseconds);
                }

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                {
                    return (false, string.Empty, $"[{ProviderType}] Phản hồi rỗng hoặc không có choices hợp lệ.", sw.ElapsedMilliseconds);
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

                return (true, content, null, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return (false, string.Empty, $"[{ProviderType} Exception] {ex.Message}", sw.ElapsedMilliseconds);
            }
        }

        public static string NormalizeEndpoint(string url)
        {
            var trimmed = url.Trim().TrimEnd('/');
            if (!trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                trimmed += "/chat/completions";
            }
            return trimmed;
        }
    }
}
