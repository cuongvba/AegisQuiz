using System;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.AI.Adapters
{
    public interface IAiProviderAdapter
    {
        AiProviderType ProviderType { get; }
        string DefaultModel { get; }

        Task<(bool Success, string Content, string? Error, long DurationMs)> CallAsync(
            string apiKey,
            string? model,
            string prompt,
            string? systemPrompt,
            double temperature,
            int maxTokens,
            string? customEndpoint = null);
    }
}
