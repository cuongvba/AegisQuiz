using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [AegisAI Dynamic Registry & BYOK Swarm Hub]
    /// API điều phối Bầy Sói Đa Mô Hình, Quản trị Model Registry động 100% CSDL và Khóa Cá Nhân BYOK.
    /// </summary>
    [ApiController]
    [Route("api/ai")]
    public class AiModelsController : ControllerBase
    {
        private readonly IWolfPackSwarmEngine _swarmEngine;
        private readonly ITenantContext _tenantContext;
        private readonly IUniversalSymposiumEngine _symposiumEngine;

        public AiModelsController(
            IWolfPackSwarmEngine swarmEngine,
            ITenantContext tenantContext,
            IUniversalSymposiumEngine symposiumEngine)
        {
            _swarmEngine = swarmEngine;
            _tenantContext = tenantContext;
            _symposiumEngine = symposiumEngine;
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(claim, out var uid) && uid != Guid.Empty)
                return uid;

            // Default fallback cho dev environment
            return Guid.Parse("00000000-0000-0000-0000-000000000001");
        }

        private Guid GetCurrentTenantId()
        {
            return _tenantContext.CurrentTenantId != Guid.Empty
                ? _tenantContext.CurrentTenantId
                : Guid.Empty;
        }

        /// <summary>
        /// Kích hoạt Bầy Sói Đa Mô Hình kiểm định câu hỏi hoặc bài giải (Task.WhenAll + SCI)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("swarm/audit")]
        public async Task<IActionResult> AuditWithSwarm([FromBody] WolfPackAuditRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { message = "Prompt kiểm định không được để trống." });
            }

            request.UserId ??= GetCurrentUserId();
            request.TenantId ??= GetCurrentTenantId();

            var result = await _swarmEngine.AuditQuestionWithSwarmAsync(request, ct);
            return Ok(result);
        }

        /// <summary>
        /// Ping thử nghiệm độ trễ và khả năng kết nối của khóa API / Endpoint
        /// </summary>
        [AllowAnonymous]
        [HttpPost("models/ping")]
        public async Task<IActionResult> PingModelKey([FromBody] TestAiKeyRequest request, CancellationToken ct = default)
        {
            var result = await _swarmEngine.PingModelKeyAsync(request, ct);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách toàn bộ các mô hình AI động trong hệ thống
        /// </summary>
        [AllowAnonymous]
        [HttpGet("models")]
        public async Task<IActionResult> GetModels(CancellationToken ct = default)
        {
            var models = await _swarmEngine.GetActiveModelsAsync(GetCurrentTenantId(), ct);
            return Ok(models);
        }

        /// <summary>
        /// Thêm hoặc cập nhật mô hình AI động (100% CSDL)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("models")]
        public async Task<IActionResult> UpsertModel([FromBody] UpsertAiModelRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.ModelCode) || string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return BadRequest(new { message = "ModelCode và DisplayName là bắt buộc." });
            }

            var model = await _swarmEngine.UpsertModelAsync(request, GetCurrentTenantId(), ct);
            return Ok(model);
        }

        /// <summary>
        /// Xóa mô hình AI
        /// </summary>
        [AllowAnonymous]
        [HttpDelete("models/{id:guid}")]
        public async Task<IActionResult> DeleteModel([FromRoute] Guid id, CancellationToken ct = default)
        {
            var success = await _swarmEngine.DeleteModelAsync(id, GetCurrentTenantId(), ct);
            if (!success) return NotFound(new { message = "Không tìm thấy mô hình." });
            return Ok(new { success = true });
        }

        /// <summary>
        /// Khám phá danh mục Model Presets của Gateway (OpenRouter, GitHub Models) kèm ma trận thế mạnh & vai trò Counter
        /// </summary>
        [AllowAnonymous]
        [HttpGet("models/gateway-presets")]
        public async Task<IActionResult> GetGatewayPresets([FromQuery] string? gateway = "all", CancellationToken ct = default)
        {
            var presets = await _swarmEngine.GetGatewayPresetsAsync(gateway ?? "all", ct);
            return Ok(presets);
        }

        /// <summary>
        /// Nạp nhanh hàng loạt model từ Gateway vào Database và tự động tối ưu hóa gán vào các Viện/Ghế phản biện (Counter)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("models/import-gateway-pack")]
        public async Task<IActionResult> ImportGatewayPack([FromBody] ImportGatewayModelsRequest request, CancellationToken ct = default)
        {
            if (request == null || request.SelectedModelCodes == null || request.SelectedModelCodes.Count == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn ít nhất một mô hình để nạp." });
            }

            var result = await _swarmEngine.ImportGatewayModelsAsync(request, GetCurrentTenantId(), ct);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách khóa BYOK của người dùng hiện tại
        /// </summary>
        [AllowAnonymous]
        [HttpGet("byok/my-keys")]
        public async Task<IActionResult> GetMyKeys(CancellationToken ct = default)
        {
            var keys = await _swarmEngine.GetUserKeysAsync(GetCurrentUserId(), GetCurrentTenantId(), ct);
            return Ok(keys);
        }

        /// <summary>
        /// Lưu hoặc cập nhật khóa BYOK riêng (mã hóa AES-256-GCM)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("byok/keys")]
        public async Task<IActionResult> SaveKey([FromBody] SaveUserAiKeyRequest request, CancellationToken ct = default)
        {
            if (request.Id == null && string.IsNullOrWhiteSpace(request.RawApiKey))
            {
                return BadRequest(new { message = "Khóa API không được để trống khi tạo mới." });
            }

            var key = await _swarmEngine.SaveUserKeyAsync(request, GetCurrentUserId(), GetCurrentTenantId(), ct);
            return Ok(key);
        }

        /// <summary>
        /// Xóa khóa BYOK riêng
        /// </summary>
        [AllowAnonymous]
        [HttpDelete("byok/keys/{id:guid}")]
        public async Task<IActionResult> DeleteKey([FromRoute] Guid id, CancellationToken ct = default)
        {
            var success = await _swarmEngine.DeleteUserKeyAsync(id, GetCurrentUserId(), GetCurrentTenantId(), ct);
            if (!success) return NotFound(new { message = "Không tìm thấy khóa." });
            return Ok(new { success = true });
        }

        /// <summary>
        /// Bảng Vàng Vinh Danh các anh hùng hiến tặng khóa cho cộng đồng
        /// </summary>
        [AllowAnonymous]
        [HttpGet("byok/hall-of-fame")]
        public async Task<IActionResult> GetHallOfFame([FromQuery] int limit = 10, CancellationToken ct = default)
        {
            var list = await _swarmEngine.GetCommunityHallOfFameAsync(limit, ct);
            return Ok(list);
        }

        /// <summary>
        /// Danh sách 6 Viện Hàn Lâm Nhận Thức Vũ Trụ của Đại Hội Thảo Bách Khoa
        /// </summary>
        [AllowAnonymous]
        [HttpGet("symposium/councils")]
        public async Task<IActionResult> GetSymposiumCouncils(CancellationToken ct = default)
        {
            var councils = await _symposiumEngine.GetActiveCouncilsAsync(ct);
            return Ok(councils);
        }

        /// <summary>
        /// Triệu tập Đại Hội Thảo Bách Khoa Vạn Vật Tri Thức (6 giai đoạn tranh biện & tiến hóa)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("symposium/convene")]
        public async Task<IActionResult> ConveneSymposium([FromBody] ConveneSymposiumRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.TopicTitle))
            {
                return BadRequest(new { message = "Tiêu đề chủ đề Đại Hội Thảo không được để trống." });
            }

            request.UserId ??= GetCurrentUserId();
            request.TenantId ??= GetCurrentTenantId();

            var result = await _symposiumEngine.ConveneSymposiumAsync(request, ct);
            return Ok(result);
        }

        /// <summary>
        /// Xuất Kỷ yếu Đại Hội Thảo thành Hồ sơ Tri thức Chân lý Tối cao (Authority Level 1) trong AegisNotebook Hub
        /// </summary>
        [AllowAnonymous]
        [HttpPost("symposium/export-to-notebook")]
        public async Task<IActionResult> ExportToNotebook(
            [FromBody] ExportSymposiumRequest request,
            CancellationToken ct)
        {
            if (request == null || request.SessionId == Guid.Empty)
            {
                return BadRequest(new { message = "SessionId không hợp lệ." });
            }

            var userId = GetCurrentUserId();
            var tenantId = GetCurrentTenantId();

            var notebookId = await _symposiumEngine.ExportSymposiumToNotebookAsync(
                request.SessionId,
                request.TargetNotebookId,
                userId,
                tenantId,
                ct);

            return Ok(new
            {
                success = true,
                notebookId = notebookId,
                message = "Đã xuất Kỷ yếu Đại Hội Thảo sang Không Gian Tri Thức Hub thành công với cấp Thẩm quyền 1 (SupremeLegal/UniversalTruth)."
            });
        }

        /// <summary>
        /// Tạo mới hoặc cập nhật Viện Hàn Lâm Tri Thức
        /// </summary>
        [HttpPost("symposium/councils")]
        [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.TenantAdmin},Admin")]
        public async Task<IActionResult> SaveCouncil([FromBody] UniversalAiCouncil council, CancellationToken ct)
        {
            if (council == null || string.IsNullOrWhiteSpace(council.CouncilCode))
                return BadRequest(new { message = "Thông tin Viện Hàn Lâm không hợp lệ." });

            var saved = await _symposiumEngine.CreateOrUpdateCouncilAsync(council, ct);
            return Ok(saved);
        }

        /// <summary>
        /// Ẩn/Hủy Viện Hàn Lâm
        /// </summary>
        [HttpDelete("symposium/councils/{id:guid}")]
        [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.TenantAdmin},Admin")]
        public async Task<IActionResult> DeleteCouncil(Guid id, CancellationToken ct)
        {
            var success = await _symposiumEngine.DeleteCouncilAsync(id, ct);
            return Ok(new { success });
        }

        /// <summary>
        /// Thêm Ghế Viện sĩ / Chuyên gia vào Hội đồng
        /// </summary>
        [HttpPost("symposium/councils/{id:guid}/seats")]
        [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.TenantAdmin},Admin")]
        public async Task<IActionResult> AddSeat(Guid id, [FromBody] UniversalCouncilSeat seat, CancellationToken ct)
        {
            if (seat == null || string.IsNullOrWhiteSpace(seat.SeatTitle))
                return BadRequest(new { message = "Thông tin ghế viện sĩ không hợp lệ." });

            var saved = await _symposiumEngine.AddCouncilSeatAsync(id, seat, ct);
            return Ok(saved);
        }

        /// <summary>
        /// Xóa Ghế Viện sĩ
        /// </summary>
        [HttpDelete("symposium/seats/{seatId:guid}")]
        [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.TenantAdmin},Admin")]
        public async Task<IActionResult> RemoveSeat(Guid seatId, CancellationToken ct)
        {
            var success = await _symposiumEngine.RemoveCouncilSeatAsync(seatId, ct);
            return Ok(new { success });
        }

        /// <summary>
        /// Cấu hình Bầy Sói (Wolfpack Swarm) cho Ghế/Vai trò trong Hội đồng: Sói đầu đàn + Bầy Sói dự phòng
        /// </summary>
        [HttpPut("symposium/seats/{seatId:guid}/swarm")]
        [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.TenantAdmin},Admin")]
        public async Task<IActionResult> UpdateSeatSwarm(
            Guid seatId,
            [FromBody] UpdateSeatSwarmRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AlphaModelCode))
                return BadRequest(new { message = "Mô hình Sói đầu đàn (Alpha Model) không được để trống." });

            var updated = await _symposiumEngine.UpdateCouncilSeatSwarmAsync(
                seatId,
                request.AlphaModelCode,
                request.SwarmModelCodesJson ?? "[]",
                request.SwarmPolicy ?? "CASCADE_FAILOVER",
                request.PersonaPromptDirective,
                request.SeatTitle,
                request.Role.HasValue ? (CouncilSeatRole)request.Role.Value : null,
                request.DecisionWeight,
                ct);

            if (updated == null) return NotFound(new { message = "Không tìm thấy Ghế hội đồng." });
            return Ok(updated);
        }
    }

    public class ExportSymposiumRequest
    {
        public Guid SessionId { get; set; }
        public Guid? TargetNotebookId { get; set; }
    }

    public class UpdateSeatSwarmRequest
    {
        public string AlphaModelCode { get; set; } = string.Empty;
        public string SwarmModelCodesJson { get; set; } = "[]";
        public string SwarmPolicy { get; set; } = "CASCADE_FAILOVER";
        public string? SeatTitle { get; set; }
        public int? Role { get; set; }
        public double? DecisionWeight { get; set; }
        public string? PersonaPromptDirective { get; set; }
    }
}
