using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.Services.CoPilot
{
    /// <summary>
    /// [AegisCoPilot OCP Command Strategy Contract]
    /// Định nghĩa giao diện xử lý độc lập cho từng Slash Command hoặc tác vụ Agentic Co-Pilot.
    /// Cho phép mở rộng vô hạn lệnh mới mà không sửa đổi CoPilotService lõi (Open/Closed Principle).
    /// </summary>
    public interface ICoPilotCommandHandler
    {
        string Command { get; }
        string Description { get; }
        string Example { get; }
        CoPilotPersona RecommendedPersona { get; }
        string Icon { get; }

        /// <summary>
        /// Kiểm tra xem handler này có xử lý thông điệp/lệnh hiện tại không
        /// </summary>
        bool CanHandle(string message);

        /// <summary>
        /// Thực thi lệnh độc lập, kết nối tới Subsystem tương ứng (UAR, WPSE, Symposium...)
        /// </summary>
        Task<CoPilotResponseDto> HandleAsync(
            CoPilotRequestDto request,
            string commandArgs,
            Stopwatch sw,
            CancellationToken ct = default);
    }
}
