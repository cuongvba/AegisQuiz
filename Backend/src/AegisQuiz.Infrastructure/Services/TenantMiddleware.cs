using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Infrastructure.Services
{
    // ── TenantContext — Scoped per request ────────────────────────────────────
    public class TenantContext : ITenantContext
    {
        private Guid   _currentTenantId = Guid.Empty;
        private string _tenantCode      = string.Empty;
        private bool   _isSystemAdmin   = false;
        private string _currentRole     = AppRoles.GuestViewer;

        public Guid   CurrentTenantId => _currentTenantId;
        public string TenantCode      => _tenantCode;
        public bool   IsSystemAdmin   => _isSystemAdmin;
        public string CurrentRole     => _currentRole;

        public void SetTenant(Guid tenantId, string code, bool isSystemAdmin = false)
        {
            _currentTenantId = tenantId;
            _tenantCode      = code;
            _isSystemAdmin   = isSystemAdmin;
        }

        public void SetRole(string role)
        {
            _currentRole   = role;
            _isSystemAdmin = role is AppRoles.SystemAdmin or AppRoles.TenantAdmin;
        }
    }

    // ── TenantMiddleware — Extract TenantId + Role từ JWT claims ─────────────
    /// <summary>
    /// [Phase B + RBAC Fix] Middleware tự động xác định Tenant và Role từ JWT claims.
    /// Thứ tự ưu tiên Tenant:
    ///   1. JWT claim "tenant_id"
    ///   2. Header "X-Tenant-ID"
    ///   3. Subdomain (agribank.aegisquiz.com → "agribank")
    ///
    /// Role resolution:
    ///   - Đọc ClaimTypes.Role (http://schemas.microsoft.com/ws/2008/06/identity/claims/role)
    ///   - Fallback: claim key "role" (dùng bởi Keycloak)
    ///   - Map sang AppRoles.* constants
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantMiddleware> _logger;

        public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
        {
            // ── 1. Resolve Role từ JWT ────────────────────────────────────────
            // [RBAC FIX] Đọc ClaimTypes.Role hoặc "role" claim (Keycloak chuẩn)
            var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value
                         ?? context.User.FindFirst("role")?.Value
                         ?? context.User.FindFirst("roles")?.Value;

            // Normalize về AppRoles constants (tương thích legacy "admin"/"student")
            var resolvedRole = NormalizeRole(roleClaim);

            if (tenantContext is TenantContext ctx)
                ctx.SetRole(resolvedRole);

            var isAdmin = resolvedRole is AppRoles.SystemAdmin or AppRoles.TenantAdmin;

            _logger.LogDebug("[RBAC] Resolved role: {RoleClaim} → {ResolvedRole}", roleClaim, resolvedRole);

            // ── 2. Resolve Tenant ─────────────────────────────────────────────
            var tenantIdClaim   = context.User.FindFirst("tenant_id");
            var tenantCodeClaim = context.User.FindFirst("tenant_code");

            if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                tenantContext.SetTenant(tenantId, tenantCodeClaim?.Value ?? "", isAdmin);
                _logger.LogDebug("[Tenant] Resolved from JWT: {TenantId}", tenantId);
            }
            // 2a. Fallback: X-Tenant-ID header (internal service-to-service)
            else if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var headerVal)
                     && Guid.TryParse(headerVal, out var headerTenantId))
            {
                tenantContext.SetTenant(headerTenantId, "", isAdmin);
                _logger.LogDebug("[Tenant] Resolved from header: {TenantId}", headerTenantId);
            }
            // 2b. Fallback: Subdomain extraction
            else
            {
                var host  = context.Request.Host.Host;
                var parts = host.Split('.');
                if (parts.Length >= 3)
                {
                    var subdomain = parts[0]; // "agribank" from "agribank.aegisquiz.com"
                    _logger.LogDebug("[Tenant] Subdomain detected: {Subdomain}", subdomain);
                    // TODO Sprint 2: Lookup TenantId từ subdomain via TenantService
                }
            }

            await _next(context);
        }

        /// <summary>
        /// Map raw claim value → AppRoles.* constant.
        /// Hỗ trợ legacy values ("admin" → TenantAdmin, "student" → Learner).
        /// </summary>
        private static string NormalizeRole(string? raw) => raw?.Trim() switch
        {
            // ── Canonical AppRoles (ưu tiên trước) ───────────────────────────
            AppRoles.SystemAdmin    => AppRoles.SystemAdmin,
            AppRoles.TenantAdmin    => AppRoles.TenantAdmin,
            AppRoles.ContentManager => AppRoles.ContentManager,
            AppRoles.Instructor     => AppRoles.Instructor,
            AppRoles.OrgUnitManager => AppRoles.OrgUnitManager,
            AppRoles.TeamLeader     => AppRoles.TeamLeader,
            AppRoles.Learner        => AppRoles.Learner,
            AppRoles.GuestViewer    => AppRoles.GuestViewer,

            // ── Legacy aliases (backward-compat) ──────────────────────────────
            "admin"   => AppRoles.TenantAdmin,
            "student" => AppRoles.Learner,
            "viewer"  => AppRoles.GuestViewer,

            // ── Unknown / unauthenticated ─────────────────────────────────────
            _ => AppRoles.GuestViewer
        };
    }
}
