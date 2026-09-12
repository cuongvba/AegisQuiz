using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Arena;

namespace AegisQuiz.API.Controllers
{
    public class CreateArenaRoomRequest
    {
        public string GameCode { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string HostUserId { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/arena")]
    public class ArenaController : ControllerBase
    {
        private readonly ArenaRoomManager _roomManager;

        public ArenaController(ArenaRoomManager roomManager)
        {
            _roomManager = roomManager;
        }

        // ── GET api/arena/games ───────────────────────────────────────────────
        [HttpGet("games")]
        public IActionResult GetAvailableGames()
        {
            var plugins = _roomManager.GetAvailablePlugins().Select(p => new
            {
                p.GameCode,
                p.DisplayName,
                p.Description,
                p.Icon,
                p.MinPlayers,
                p.MaxPlayers
            });
            return Ok(plugins);
        }

        // ── GET api/arena/rooms ───────────────────────────────────────────────
        [HttpGet("rooms")]
        public IActionResult GetActiveRooms([FromQuery] string? gameCode = null)
        {
            var rooms = _roomManager.ListActiveRooms();
            if (!string.IsNullOrEmpty(gameCode))
            {
                rooms = rooms.Where(r => r.GameCode.Equals(gameCode, StringComparison.OrdinalIgnoreCase));
            }

            var result = rooms.Select(r => new
            {
                r.RoomId,
                r.GameCode,
                r.RoomName,
                r.HostUserId,
                r.CurrentStage,
                PlayerCount = r.Players.Count,
                StageStartTime = r.StageStartTimeUtc
            });

            return Ok(result);
        }

        // ── POST api/arena/rooms ──────────────────────────────────────────────
        [HttpPost("rooms")]
        public IActionResult CreateRoom([FromBody] CreateArenaRoomRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.GameCode))
                    return BadRequest(new { message = "GameCode không được để trống" });

                var hostId = string.IsNullOrWhiteSpace(request.HostUserId) ? Guid.NewGuid().ToString("N")[..8] : request.HostUserId;
                var room = _roomManager.CreateRoom(request.GameCode, request.RoomName, hostId, request.HostName);
                return Ok(room);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── GET api/arena/rooms/{id} ──────────────────────────────────────────
        [HttpGet("rooms/{id}")]
        public IActionResult GetRoom(string id)
        {
            var room = _roomManager.GetRoom(id);
            if (room == null)
                return NotFound(new { message = "Không tìm thấy phòng đấu trường" });

            return Ok(room);
        }

        // ── GET api/arena/templates ───────────────────────────────────────────
        [HttpGet("templates")]
        public IActionResult GetTemplates()
        {
            var templates = _roomManager.GetAllTemplates();
            return Ok(templates);
        }

        // ── GET api/arena/templates/{gameCode} ────────────────────────────────
        [HttpGet("templates/{gameCode}")]
        public IActionResult GetTemplate(string gameCode)
        {
            var template = _roomManager.GetTemplate(gameCode);
            if (template == null)
                return NotFound(new { message = $"Không tìm thấy template: {gameCode}" });

            return Ok(template);
        }

        // ── POST api/arena/templates ──────────────────────────────────────────
        [HttpPost("templates")]
        public IActionResult SaveTemplate([FromBody] GameshowManifest manifest)
        {
            if (string.IsNullOrWhiteSpace(manifest.GameCode))
                return BadRequest(new { message = "GameCode không được để trống" });

            if (string.IsNullOrWhiteSpace(manifest.DisplayName))
                return BadRequest(new { message = "DisplayName không được để trống" });

            manifest.GameCode = manifest.GameCode.Trim().ToUpperInvariant();
            bool saved = _roomManager.SaveTemplate(manifest);
            if (!saved)
                return BadRequest(new { message = "Không thể lưu template" });

            return Ok(new { message = "Đã lưu template gameshow thành công", template = manifest });
        }

        // ── DELETE api/arena/templates/{gameCode} ─────────────────────────────
        [HttpDelete("templates/{gameCode}")]
        public IActionResult DeleteTemplate(string gameCode)
        {
            bool deleted = _roomManager.DeleteTemplate(gameCode);
            if (!deleted)
                return BadRequest(new { message = "Không thể xóa template (template không tồn tại hoặc là preset hệ thống)" });

            return Ok(new { message = $"Đã xóa template {gameCode} thành công" });
        }
    }
}

