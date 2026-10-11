using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [Enterprise Entitlements & Monetization Gateway]
    /// Cung cấp ma trận phân quyền 4 tầng: Tenant -> Domain -> Group (OrgUnit) -> User
    /// Cho phép Frontend render có điều kiện (Feature Gating), hiển thị Paywalls và thích ứng UI.
    /// </summary>
    [ApiController]
    [Route("api/entitlements")]
    [AllowAnonymous]
    public class EntitlementsController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<EntitlementsController> _logger;

        public EntitlementsController(
            AegisQuizDbContext db,
            ITenantContext tenantContext,
            ILogger<EntitlementsController> logger)
        {
            _db = db;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentEntitlements()
        {
            try
            {
                // 1. Xác định Tenant hiện tại
                var tenantId = _tenantContext.CurrentTenantId;
                if (tenantId == Guid.Empty)
                {
                    var claimVal = User.FindFirst("tenant_id")?.Value
                                ?? User.FindFirst("tenantId")?.Value
                                ?? User.FindFirst("TenantId")?.Value;
                    if (!string.IsNullOrEmpty(claimVal) && Guid.TryParse(claimVal, out var tid))
                    {
                        tenantId = tid;
                    }
                }

                Tenant? tenant = null;
                if (tenantId != Guid.Empty)
                {
                    tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId);
                }

                // Fallback nếu không có tenant
                if (tenant == null)
                {
                    tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == Tenant.CommunityTenantId)
                          ?? await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.IsActive)
                          ?? new Tenant
                          {
                              Id = Tenant.CommunityTenantId,
                              Code = Tenant.CommunityTenantCode,
                              Name = "Aegis Community Platform",
                              Plan = TenantPlan.Starter,
                              ScaleType = TenantScaleType.Individual
                          };
                }

                // 2. Tính toán Features theo Plan & Overrides
                var enabledFeatures = PlanFeatureCatalog.GetFeaturesForPlan(tenant.Plan, tenant.FeatureFlagsJson);
                var quotas = PlanFeatureCatalog.GetQuotasForPlan(tenant.Plan);

                // 3. Lấy Domain Scopes được kích hoạt
                var activeDomains = await _db.TenantDomainConfigs
                    .AsNoTracking()
                    .Where(d => d.TenantId == tenant.Id && d.IsEnabled)
                    .Select(d => d.DomainCode)
                    .ToListAsync();

                if (!activeDomains.Any())
                {
                    activeDomains = new List<string> { "general", "education" };
                }

                // 4. Lấy thông tin User & Group (OrgUnit)
                string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                string? role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value ?? _tenantContext.CurrentRole;
                Guid? orgUnitId = null;
                string? orgUnitPath = null;
                var userCapabilities = new List<string>();

                if (!string.IsNullOrEmpty(userId))
                {
                    var userRole = await _db.UserRoles
                        .AsNoTracking()
                        .Where(r => r.UserId == userId && r.TenantId == tenant.Id && r.IsActive)
                        .FirstOrDefaultAsync();

                    if (userRole != null)
                    {
                        role = userRole.Role;
                        orgUnitId = userRole.OrgUnitId;
                        if (orgUnitId.HasValue)
                        {
                            var ou = await _db.OrganizationUnits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orgUnitId.Value);
                            orgUnitPath = ou?.HierarchyPath;
                        }
                    }

                    if (!string.IsNullOrEmpty(role))
                    {
                        userCapabilities.AddRange(AppRolePermissions.GetPermissions(role));
                    }
                }
                else
                {
                    role = AppRoles.GuestViewer;
                }

                var response = new
                {
                    tenant = new
                    {
                        id = tenant.Id,
                        code = tenant.Code,
                        name = tenant.Name,
                        plan = tenant.Plan.ToString(),
                        scaleType = tenant.ScaleType.ToString(),
                        primaryColor = tenant.PrimaryColor,
                        logoUrl = tenant.LogoUrl,
                        isCommunity = tenant.IsCommunityHub
                    },
                    features = enabledFeatures.ToList(),
                    quotas = quotas,
                    domains = activeDomains,
                    user = new
                    {
                        id = userId,
                        role = role,
                        orgUnitId = orgUnitId,
                        orgUnitPath = orgUnitPath,
                        capabilities = userCapabilities
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin entitlements hiện tại.");
                return StatusCode(500, new { message = "Không thể lấy thông tin bản quyền và phân quyền." });
            }
        }
    }
}
