using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Epoch 4: Community Ecosystem] Pipeline kiểm duyệt nội dung câu hỏi do cộng đồng đóng góp.
    /// </summary>
    public interface IContentModerationService
    {
        Task<ContentModerationResultDto> ModerateQuestionAsync(string content, string? explanation, CancellationToken ct = default);
    }
}
