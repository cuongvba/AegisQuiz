using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.API.Hubs
{
    /// <summary>
    /// [AegisAI Universal Symposium SignalR Hub]
    /// Trục truyền dẫn thời gian thực cho Đại Hội Thảo Bách Khoa Vạn Vật Tri Thức:
    /// Truyền phát trực tiếp (Live Stream) các lượt tranh biện giữa 6 Viện Hàn Lâm,
    /// cập nhật tiến trình 6 giai đoạn và công bố Kỷ yếu Nghị quyết chân lý.
    /// </summary>
    [AllowAnonymous]
    public class SymposiumHub : Hub
    {
        private readonly ILogger<SymposiumHub> _logger;

        public SymposiumHub(ILogger<SymposiumHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinSymposiumSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            _logger.LogInformation("[SymposiumHub] Connection {ConnId} đã tham gia phiên hội thảo {SessionId}",
                Context.ConnectionId, sessionId);
            await Clients.Caller.SendAsync("OnStatusMessage", $"Đã kết nối thính phòng Viện Hàn Lâm (Phiên {sessionId})");
        }

        public async Task LeaveSymposiumSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
            _logger.LogInformation("[SymposiumHub] Connection {ConnId} đã rời phiên hội thảo {SessionId}",
                Context.ConnectionId, sessionId);
        }
    }
}
