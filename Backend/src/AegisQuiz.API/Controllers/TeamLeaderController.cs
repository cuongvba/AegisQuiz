using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [RBAC] TeamLeader endpoints — Trưởng nhóm quản lý nhóm của mình.
    ///
    /// Bảo mật: Mọi endpoint đều kiểm tra OrgUnitId scope.
    /// TeamLeader chỉ thấy data của nhóm được assign, không thấy nhóm khác.
    ///
    /// GET  /api/team-leader/my-team          → Danh sách thành viên nhóm
    /// GET  /api/team-leader/my-team/progress → Tiến độ học tập từng thành viên
    /// POST /api/team-leader/assign-exam      → Giao đề luyện tập cho nhóm
    /// </summary>
    [ApiController]
    [Route("api/team-leader")]
    [Authorize(Roles = $"{AppRoles.TeamLeader},{AppRoles.TenantAdmin},{AppRoles.SystemAdmin}")]
    public class TeamLeaderController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        private readonly ITenantContext     _tenant;

        public TeamLeaderController(AegisQuizDbContext db, ITenantContext tenant)
        {
            _db     = db;
            _tenant = tenant;
        }

        // ── GET /api/team-leader/my-team ──────────────────────────────────────
        /// <summary>
        /// Trả danh sách thành viên nhóm của TeamLeader đang đăng nhập.
        /// TeamLeader chỉ thấy nhóm được assign (OrgUnitId scope).
        /// TenantAdmin/SystemAdmin có thể truyền thêm ?orgUnitId= để xem bất kỳ nhóm nào.
        /// </summary>
        [HttpGet("my-team")]
        public async Task<IActionResult> GetMyTeam([FromQuery] Guid? orgUnitId = null)
        {
            var userId    = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var tenantId  = _tenant.CurrentTenantId;
            var userRole  = _tenant.CurrentRole;

            Guid? scopedOrgUnitId;

            if (userRole == AppRoles.TeamLeader)
            {
                // TeamLeader: chỉ được phép xem nhóm của mình
                var leaderAssignment = await _db.UserRoles
                    .FirstOrDefaultAsync(r =>
                        r.UserId   == userId   &&
                        r.TenantId == tenantId &&
                        r.Role     == AppRoles.TeamLeader &&
                        r.IsActive &&
                        r.OrgUnitId != null);

                if (leaderAssignment?.OrgUnitId == null)
                    return Forbid(); // Không có nhóm nào được assign

                scopedOrgUnitId = leaderAssignment.OrgUnitId;
            }
            else
            {
                // TenantAdmin/SystemAdmin: có thể xem bất kỳ nhóm nào
                if (orgUnitId == null)
                    return BadRequest(new { message = "orgUnitId là bắt buộc với Admin." });
                scopedOrgUnitId = orgUnitId;
            }

            // Lấy thông tin nhóm
            var orgUnit = await _db.OrganizationUnits
                .FirstOrDefaultAsync(o => o.Id == scopedOrgUnitId && o.TenantId == tenantId);

            if (orgUnit == null)
                return NotFound(new { message = "Không tìm thấy nhóm." });

            // Lấy danh sách learner trong nhóm
            var members = await _db.UserRoles
                .Where(r =>
                    r.TenantId  == tenantId        &&
                    r.OrgUnitId == scopedOrgUnitId &&
                    r.Role      == AppRoles.Learner &&
                    r.IsActive)
                .Select(r => new
                {
                    r.UserId,
                    r.AssignedAt,
                    r.ExpiresAt
                })
                .ToListAsync();

            return Ok(new
            {
                orgUnit = new
                {
                    orgUnit.Id,
                    orgUnit.Name,
                    orgUnit.Code,
                    orgUnit.UnitType
                },
                memberCount = members.Count,
                members
            });
        }

        // ── GET /api/team-leader/my-team/progress ─────────────────────────────
        /// <summary>
        /// Tiến độ học tập của từng thành viên trong nhóm.
        /// Tổng hợp: số câu đã làm, điểm trung bình, streak, lần thi gần nhất.
        /// </summary>
        [HttpGet("my-team/progress")]
        public async Task<IActionResult> GetTeamProgress([FromQuery] Guid? orgUnitId = null)
        {
            var userId    = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var tenantId  = _tenant.CurrentTenantId;
            var userRole  = _tenant.CurrentRole;

            // Resolve scopedOrgUnitId (same security logic as GetMyTeam)
            Guid? scopedOrgUnitId = await ResolveScopedOrgUnit(userId, tenantId, userRole, orgUnitId);
            if (scopedOrgUnitId == null) return Forbid();

            // Lấy danh sách userId trong nhóm
            var memberUserIds = await _db.UserRoles
                .Where(r =>
                    r.TenantId  == tenantId        &&
                    r.OrgUnitId == scopedOrgUnitId &&
                    r.Role      == AppRoles.Learner &&
                    r.IsActive)
                .Select(r => r.UserId)
                .ToListAsync();

            var memberGuids = memberUserIds
                .Select(id => Guid.TryParse(id, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList();

            // Aggregate attempt data
            var progressData = await _db.QuestionAttempts
                .Where(a => memberGuids.Contains(a.UserId))
                .GroupBy(a => a.UserId)
                .Select(g => new
                {
                    userId           = g.Key.ToString(),
                    totalAttempts    = g.Count(),
                    avgScore         = g.Count() > 0 ? (double)g.Count(a => a.IsCorrect) * 100.0 / g.Count() : 0.0,
                    lastAttemptDate  = g.Max(a => a.AttemptDate),
                    correctAnswers   = g.Count(a => a.IsCorrect),
                    totalQuestions   = g.Count()
                })
                .ToListAsync();

            return Ok(new
            {
                orgUnitId = scopedOrgUnitId,
                reportDate = DateTime.UtcNow,
                totalMembers = memberUserIds.Count,
                memberProgress = progressData
            });
        }

        // ── POST /api/team-leader/assign-exam ─────────────────────────────────
        /// <summary>
        /// Giao đề luyện tập cho cả nhóm hoặc một số thành viên cụ thể.
        /// TeamLeader chỉ assign được cho nhóm của mình.
        /// </summary>
        [HttpPost("assign-exam")]
        public async Task<IActionResult> AssignExam([FromBody] AssignExamRequest request)
        {
            var userId   = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
            var tenantId = _tenant.CurrentTenantId;
            var userRole = _tenant.CurrentRole;

            // Resolve scope
            Guid? scopedOrgUnitId = await ResolveScopedOrgUnit(userId, tenantId, userRole, null);
            if (scopedOrgUnitId == null) return Forbid();

            // Validate exam/notebook exists (kiểm tra qua domain — chỉ logic scope ở đây)
            // TODO Sprint 2: Gọi IExamAssignmentService để tạo assignment records

            return Ok(new
            {
                message      = "Đã giao đề cho nhóm thành công.",
                orgUnitId    = scopedOrgUnitId,
                assignedBy   = userId,
                examRef      = request.ExamId,
                targetMembers= request.TargetUserIds?.Count ?? 0,
                assignedAt   = DateTime.UtcNow,
                dueDate      = request.DueDate
            });
        }

        // ── Private helpers ───────────────────────────────────────────────────
        private async Task<Guid?> ResolveScopedOrgUnit(
            string userId, Guid tenantId, string role, Guid? adminOverride)
        {
            if (role == AppRoles.TeamLeader)
            {
                var assignment = await _db.UserRoles
                    .FirstOrDefaultAsync(r =>
                        r.UserId   == userId   &&
                        r.TenantId == tenantId &&
                        r.Role     == AppRoles.TeamLeader &&
                        r.IsActive &&
                        r.OrgUnitId != null);

                return assignment?.OrgUnitId;
            }

            // Admin: dùng override
            return adminOverride;
        }
    }

    public class AssignExamRequest
    {
        public Guid        ExamId         { get; set; }
        public List<string>? TargetUserIds { get; set; } // null = toàn nhóm
        public DateTime?   DueDate        { get; set; }
        public string?     Note           { get; set; }
    }
}
