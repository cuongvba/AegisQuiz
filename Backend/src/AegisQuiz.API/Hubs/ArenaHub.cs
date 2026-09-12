using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Arena;

namespace AegisQuiz.API.Hubs
{
    public class ArenaHub : Hub
    {
        private readonly ArenaRoomManager _roomManager;
        private readonly ILogger<ArenaHub> _logger;

        public ArenaHub(ArenaRoomManager roomManager, ILogger<ArenaHub> logger)
        {
            _roomManager = roomManager;
            _logger = logger;
        }

        public async Task<ArenaRoomState?> JoinArenaRoom(string roomId, string playerName, string avatar)
        {
            var playerId = Context.ConnectionId;
            var player = new ArenaPlayer
            {
                Id = playerId,
                Name = string.IsNullOrWhiteSpace(playerName) ? $"Player #{playerId[..4]}" : playerName,
                Avatar = string.IsNullOrWhiteSpace(avatar) ? "🎓" : avatar
            };

            bool joined = _roomManager.JoinRoom(roomId, player);
            if (!joined) return null;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            var room = _roomManager.GetRoom(roomId);
            if (room != null)
            {
                await Clients.Group(roomId).SendAsync("RoomStateUpdated", room);
            }
            return room;
        }

        public async Task LeaveArenaRoom(string roomId)
        {
            var playerId = Context.ConnectionId;
            _roomManager.LeaveRoom(roomId, playerId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            var room = _roomManager.GetRoom(roomId);
            if (room != null)
            {
                await Clients.Group(roomId).SendAsync("RoomStateUpdated", room);
            }
        }

        public async Task<bool> PressBuzzer(string roomId, long clientTimestampMs)
        {
            var playerId = Context.ConnectionId;
            bool won = await _roomManager.PressBuzzerAsync(roomId, playerId, clientTimestampMs);
            var room = _roomManager.GetRoom(roomId);
            if (room != null)
            {
                if (won)
                {
                    var player = room.Players.GetValueOrDefault(playerId);
                    await Clients.Group(roomId).SendAsync("BuzzerWon", new
                    {
                        playerId,
                        playerName = player?.Name ?? "Player",
                        timestamp = clientTimestampMs
                    });
                }
                await Clients.Group(roomId).SendAsync("RoomStateUpdated", room);
            }
            return won;
        }

        public async Task SubmitAnswer(string roomId, string answerPayload)
        {
            var playerId = Context.ConnectionId;
            await _roomManager.SubmitAnswerAsync(roomId, playerId, answerPayload);
            var room = _roomManager.GetRoom(roomId);
            if (room != null)
            {
                await Clients.Group(roomId).SendAsync("RoomStateUpdated", room);
            }
        }

        public async Task ExecuteAction(string roomId, string actionType, string payload)
        {
            var playerId = Context.ConnectionId;
            await _roomManager.ExecuteActionAsync(roomId, playerId, actionType, payload);
            var room = _roomManager.GetRoom(roomId);
            if (room != null)
            {
                await Clients.Group(roomId).SendAsync("ActionExecuted", new { actionType, payload });
                await Clients.Group(roomId).SendAsync("RoomStateUpdated", room);
            }
        }

        public async Task TransitionStage(string roomId, string nextStage)
        {
            await _roomManager.TransitionStageAsync(roomId, nextStage);
            var room = _roomManager.GetRoom(roomId);
            if (room != null)
            {
                await Clients.Group(roomId).SendAsync("RoomStateUpdated", room);
            }
        }

        public ArenaRoomState? GetRoomState(string roomId)
        {
            return _roomManager.GetRoom(roomId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Tự động dọn dẹp khi người chơi ngắt kết nối
            foreach (var room in _roomManager.ListActiveRooms())
            {
                if (room.Players.ContainsKey(Context.ConnectionId))
                {
                    _roomManager.LeaveRoom(room.RoomId, Context.ConnectionId);
                    await Clients.Group(room.RoomId).SendAsync("RoomStateUpdated", room);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
