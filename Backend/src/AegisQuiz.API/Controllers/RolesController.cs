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
    /// [RBAC] Role management endpoints.
    /// GET  /api/roles/me                    → Role của user hiện tại
    /// GET  /api/admin/users/{id}/roles      → [TenantAdmin+] Roles của user bất kỳ
    /// PUT  /api/admin/users/{id}/roles      → [TenantAdmin+] Gán role cho user
    /// DELETE /api/admin/users/{id}/roles/{roleId} → [TenantAdmin+] Thu hồi role
    /// </summary>
    [ApiController]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        private readonly ITenantContext     _tenant;

        public RolesController(AegisQuizDbContext db, ITenantContext tenant)
        {
            _db     = db;
            _tenant = tenant;
        }

        // ── GET /api/roles/me ─────────────────────────────────────────────────
        /// <summary>Lấy role(s) của user hiện đang đăng nhập trong tenant hiện tại.</summary>
        [HttpGet("api/roles/me")]
        public async Task<IActionResult> GetMyRoles()
        {
            var userId   = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = _tenant.CurrentTenantId;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không xác định được user." });

            var roles = await _db.UserRoles
                .Where(r => r.UserId == userId && r.TenantId == tenantId && r.IsActive)
                .Where(r => r.ExpiresAt == null || r.ExpiresAt > DateTime.UtcNow)
                .Select(r => new
                {
                    r.Id,
                    r.Role,
                    r.OrgUnitId,
                    r.AssignedAt,
                    r.ExpiresAt,
                    permissions = AppRolePermissions.GetPermissions(r.Role)
                })
                .ToListAsync();

            // Nếu chưa có role trong DB, trả về role từ JWT claim (backward compat)
            if (!roles.Any())
            {
                var jwtRole = _tenant.CurrentRole;
                return Ok(new
                {
                    userId,
                    source  = "jwt_claim",
                    roles   = new[] { new { Role = jwtRole, permissions = AppRolePermissions.GetPermissions(jwtRole) } }
                });
            }

            return Ok(new { userId, source = "database", roles });
        }

        // ── GET /api/admin/users/{userId}/roles ───────────────────────────────
        [HttpGet("api/admin/users/{userId}/roles")]
        [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.TenantAdmin}")]
        public async Task<IActionResult> GetUserRoles(string userId)
        {
            var tenantId = _tenant.CurrentTenantId;

            var roles = await _db.UserRoles
                .Where(r => r.UserId == userId && r.TenantId == tenantId && r.IsActive)
                .Include(r => r.OrgUnit)
                .Select(r => new
                {
                    r.Id,
                    r.Role,
                    r.OrgUnitId,
                    orgUnitName = r.OrgUnit != null ? r.OrgUnit.Name : null,
                    r.AssignedAt,
                    r.AssignedBy,
                    r.ExpiresAt
                })
                .ToListAsync();

            return Ok(new { userId, roles });
        }

        // ── PUT /api/admin/users/{userId}/roles ───────────────────────────────
        [HttpPut("api/admin/users/{userId}/roles")]
        [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.TenantAdmin}")]
        public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleRequest request)
        {
            if (!IsValidRole(request.Role))
                return BadRequest(new { message = $"Role '{request.Role}' không hợp lệ." });

            var tenantId   = _tenant.CurrentTenantId;
            var assignedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

            // TeamLeader bắt buộc phải có OrgUnitId
            if (request.Role == AppRoles.TeamLeader && request.OrgUnitId == null)
                return BadRequest(new { message = "TeamLeader phải được gán với OrgUnitId cụ thể." });

            // Kiểm tra OrgUnit thuộc tenant
            if (request.OrgUnitId.HasValue)
            {
                var orgExists = await _db.OrganizationUnits
                    .AnyAsync(o => o.Id == request.OrgUnitId && o.TenantId == tenantId);
                if (!orgExists)
                    return BadRequest(new { message = "OrgUnit không tồn tại trong tenant này." });
            }

            // Kiểm tra duplicate (active role cùng scope)
            var existing = await _db.UserRoles
                .FirstOrDefaultAsync(r =>
                    r.UserId    == userId      &&
                    r.TenantId  == tenantId    &&
                    r.Role      == request.Role &&
                    r.OrgUnitId == request.OrgUnitId &&
                    r.IsActive);

            if (existing != null)
                return Conflict(new { message = $"User đã có role '{request.Role}' trong scope này." });

            var userRole = new UserRole
            {
                UserId     = userId,
                TenantId   = tenantId,
                Role       = request.Role,
                OrgUnitId  = request.OrgUnitId,
                AssignedBy = assignedBy,
                ExpiresAt  = request.ExpiresAt,
                IsActive   = true
            };

            _db.UserRoles.Add(userRole);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message      = $"Đã gán role '{request.Role}' cho user '{userId}'.",
                userRoleId   = userRole.Id,
                permissions  = AppRolePermissions.GetPermissions(userRole.Role)
            });
        }

        // ── DELETE /api/admin/users/{userId}/roles/{roleId} ───────────────────
        [HttpDelete("api/admin/users/{userId}/roles/{roleId:guid}")]
        [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.TenantAdmin}")]
        public async Task<IActionResult> RevokeRole(string userId, Guid roleId)
        {
            var tenantId = _tenant.CurrentTenantId;

            var userRole = await _db.UserRoles
                .FirstOrDefaultAsync(r => r.Id == roleId && r.UserId == userId && r.TenantId == tenantId);

            if (userRole == null)
                return NotFound(new { message = "Không tìm thấy role assignment." });

            userRole.IsActive = false;
            await _db.SaveChangesAsync();

            return Ok(new { message = $"Đã thu hồi role '{userRole.Role}' của user '{userId}'." });
        }

        // ── GET /api/admin/roles/summary ──────────────────────────────────────
        /// <summary>Tổng số user theo từng role trong tenant (dùng cho Admin Dashboard).</summary>
        [HttpGet("api/admin/roles/summary")]
        [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.TenantAdmin}")]
        public async Task<IActionResult> GetRoleSummary()
        {
            var tenantId = _tenant.CurrentTenantId;

            var summary = await _db.UserRoles
                .Where(r => r.TenantId == tenantId && r.IsActive)
                .GroupBy(r => r.Role)
                .Select(g => new { role = g.Key, count = g.Count() })
                .ToListAsync();

            return Ok(new { tenantId, summary });
        }

        private static bool IsValidRole(string role) => role switch
        {
            AppRoles.SystemAdmin or AppRoles.TenantAdmin or AppRoles.ContentManager
            or AppRoles.Instructor or AppRoles.OrgUnitManager or AppRoles.TeamLeader
            or AppRoles.Learner or AppRoles.GuestViewer => true,
            _ => false
        };
    }

    public class AssignRoleRequest
    {
        public string  Role       { get; set; } = AppRoles.Learner;
        /// <summary>Bắt buộc với TeamLeader. Optional với các role khác.</summary>
        public Guid?   OrgUnitId  { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
