using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Infrastructure.AI.Protocols
{
    /// <summary>
    /// Giao diện chuẩn hóa các giao thức truyền tin AI mở trên toàn cầu
    /// </summary>
    public interface IModelProtocolDriver
    {
        ModelProtocolType ProtocolType { get; }

        Task<(bool Success, string Content, string? Error, long DurationMs, int TokensUsed)> ExecuteAsync(
            string endpointUrl,
            string apiKey,
            string modelName,
            string systemPrompt,
            string userPrompt,
            double temperature = 0.2,
            int maxTokens = 2048,
            CancellationToken ct = default);
    }
}
