using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Grounded Symposium - Seat Model Invoker]
    /// Cung cấp dịch vụ triệu gọi mô hình AI thật sự cho từng ghế/học viện trong Đại Hội Thảo.
    /// Thực hiện thác lũ phân giải khóa đa tầng (User BYOK -> Tenant BYOK -> Community Pool -> System Default),
    /// đo độ trễ thực tế qua Stopwatch và tự động xoay tua khóa (key rotation) khi gặp sự cố mạng/quota.
    /// </summary>
    public interface ISeatModelInvoker
    {
        /// <summary>
        /// Thực thi lượt phát biểu của một ghế mô hình cụ thể.
        /// </summary>
        Task<SeatInvocationResponse> InvokeSeatAsync(SeatInvocationRequest request, CancellationToken ct = default);
    }
}
