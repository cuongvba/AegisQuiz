using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using AegisQuiz.API.Hubs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.API.Services
{
    /// <summary>
    /// Cài đặt phát sóng SignalR cho Đại Hội Thảo Bách Khoa Vạn Vật Tri Thức
    /// </summary>
    public class SignalRSymposiumBroadcaster : ISymposiumLiveBroadcaster
    {
        private readonly IHubContext<SymposiumHub> _hubContext;
        private readonly ILogger<SignalRSymposiumBroadcaster> _logger;

        public SignalRSymposiumBroadcaster(
            IHubContext<SymposiumHub> hubContext,
            ILogger<SignalRSymposiumBroadcaster> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task BroadcastPhaseProgressAsync(string sessionId, int phaseNumber, string phaseName, double progressPercentage, CancellationToken ct = default)
        {
            try
            {
                await _hubContext.Clients.Group(sessionId).SendAsync("OnPhaseChanged", new
                {
                    SessionId = sessionId,
                    PhaseNumber = phaseNumber,
                    PhaseName = phaseName,
                    ProgressPercentage = progressPercentage,
                    Timestamp = DateTime.UtcNow
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SignalR Broadcaster] Lỗi phát sóng Phase {Phase} cho session {SessionId}", phaseNumber, sessionId);
            }
        }

        public async Task BroadcastTurnSpokenAsync(string sessionId, SymposiumDialogueTurn turn, CancellationToken ct = default)
        {
            try
            {
                await _hubContext.Clients.Group(sessionId).SendAsync("OnTurnSpoken", turn, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SignalR Broadcaster] Lỗi phát sóng TurnSpoken cho session {SessionId}", sessionId);
            }
        }

        public async Task BroadcastPhaseResultAsync(string sessionId, int phaseNumber, string phaseName, object phaseResultData, CancellationToken ct = default)
        {
            try
            {
                await _hubContext.Clients.Group(sessionId).SendAsync("OnPhaseResult", new
                {
                    SessionId = sessionId,
                    PhaseNumber = phaseNumber,
                    PhaseName = phaseName,
                    Result = phaseResultData,
                    Timestamp = DateTime.UtcNow
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SignalR Broadcaster] Lỗi phát sóng PhaseResult {Phase} cho session {SessionId}", phaseNumber, sessionId);
            }
        }

        public async Task BroadcastProceedingsCompletedAsync(string sessionId, SymposiumProceedingsResult proceedings, CancellationToken ct = default)
        {
            try
            {
                await _hubContext.Clients.Group(sessionId).SendAsync("OnProceedingsCompleted", proceedings, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SignalR Broadcaster] Lỗi phát sóng ProceedingsCompleted cho session {SessionId}", sessionId);
            }
        }
    }
}
