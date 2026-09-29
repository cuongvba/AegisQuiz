using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Common.Security;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [Enterprise Multi-Tenancy Hub] Quản trị Khách thuê (Tenant Management).
    /// Dành riêng cho SystemAdmin toàn nền tảng (Platform SuperAdmin).
    /// </summary>
    [ApiController]
    [Route("api/admin/tenants")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.TenantAdmin}")]
    public class TenantsController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        private readonly ILogger<TenantsController> _logger;
        private readonly ITenantContext _tenantContext;

        public TenantsController(AegisQuizDbContext db, ILogger<TenantsController> logger, ITenantContext tenantContext)
        {
            _db = db;
            _logger = logger;
            _tenantContext = tenantContext;
        }

        private async Task<Guid> GetTenantIdAsync()
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (tenantId != Guid.Empty) return tenantId;

            var claimVal = User.FindFirst("tenant_id")?.Value
                        ?? User.FindFirst("tenantId")?.Value
                        ?? User.FindFirst("TenantId")?.Value;
            if (!string.IsNullOrEmpty(claimVal) && Guid.TryParse(claimVal, out var tid) && tid != Guid.Empty)
            {
                return tid;
            }

            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var uId))
            {
                var u = await _db.UserAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == uId);
                if (u != null && u.TenantId != Guid.Empty)
                {
                    return u.TenantId;
                }
            }

            var defaultTenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.IsActive);
            if (defaultTenant != null)
            {
                return defaultTenant.Id;
            }

            return Guid.Empty;
        }

        // ── GET api/admin/tenants ──────────────────────────────────────────────
        /// <summary>
        /// Lấy danh sách toàn bộ khách thuê (Tenants) kèm phân trang, tìm kiếm và thống kê.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTenants(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] TenantPlan? plan = null,
            [FromQuery] TenantScaleType? scaleType = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
                var isSystemAdmin = role == AppRoles.SystemAdmin;

                var query = _db.Tenants.AsNoTracking();

                if (!isSystemAdmin)
                {
                    var myTenantId = await GetTenantIdAsync();
                    query = query.Where(t => t.Id == myTenantId);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLowerInvariant();
                    query = query.Where(t => t.Name.ToLower().Contains(term) || t.Code.ToLower().Contains(term) || (t.Domain != null && t.Domain.ToLower().Contains(term)));
                }

                if (plan.HasValue)
                {
                    query = query.Where(t => t.Plan == plan.Value);
                }

                if (scaleType.HasValue)
                {
                    query = query.Where(t => t.ScaleType == scaleType.Value);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(t => t.IsActive == isActive.Value);
                }

                var totalCount = await query.CountAsync();

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var tenantList = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Lấy thống kê nhân sự và đơn vị cho từng tenant
                var tenantIds = tenantList.Select(t => t.Id).ToList();

                var userCounts = await _db.UserAccounts
                    .AsNoTracking()
                    .Where(u => tenantIds.Contains(u.TenantId))
                    .GroupBy(u => u.TenantId)
                    .Select(g => new { TenantId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TenantId, x => x.Count);

                var orgUnitCounts = await _db.OrganizationUnits
                    .AsNoTracking()
                    .Where(o => tenantIds.Contains(o.TenantId) && o.IsActive)
                    .GroupBy(o => o.TenantId)
                    .Select(g => new { TenantId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TenantId, x => x.Count);

                var questionCounts = await _db.Questions
                    .AsNoTracking()
                    .Where(q => tenantIds.Contains(q.TenantId))
                    .GroupBy(q => q.TenantId)
                    .Select(g => new { TenantId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TenantId, x => x.Count);

                var items = tenantList.Select(t => new
                {
                    id = t.Id,
                    code = t.Code,
                    name = t.Name,
                    logoUrl = t.LogoUrl,
                    primaryColor = t.PrimaryColor,
                    domain = t.Domain,
                    customDomain = t.CustomDomain,
                    plan = t.Plan.ToString(),
                    scaleType = t.ScaleType.ToString(),
                    maxUsers = t.MaxUsers,
                    isActive = t.IsActive,
                    createdAt = t.CreatedAt,
                    expiresAt = t.ExpiresAt,
                    currentUsersCount = userCounts.GetValueOrDefault(t.Id, 0),
                    orgUnitsCount = orgUnitCounts.GetValueOrDefault(t.Id, 0),
                    questionsCount = questionCounts.GetValueOrDefault(t.Id, 0)
                });

                return Ok(new
                {
                    items,
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Tenants] Lỗi lấy danh sách tenants: {Message}", ex.Message);
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        // ── GET api/admin/tenants/summary ──────────────────────────────────────
        /// <summary>
        /// Tổng hợp số liệu KPI toàn bộ hệ thống Khách thuê cho SystemAdmin và TenantAdmin.
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetTenantsSummary()
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
                var isSystemAdmin = role == AppRoles.SystemAdmin;

                if (!isSystemAdmin)
                {
                    var myTenantId = await GetTenantIdAsync();
                    var myTenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == myTenantId);
                    var myUsers = await _db.UserAccounts.CountAsync(u => u.TenantId == myTenantId);
                    return Ok(new
                    {
                        totalTenants = 1,
                        activeTenants = (myTenant?.IsActive ?? true) ? 1 : 0,
                        inactiveTenants = (myTenant?.IsActive ?? true) ? 0 : 1,
                        totalUsers = myUsers,
                        enterpriseTenants = (myTenant?.Plan == TenantPlan.Enterprise || myTenant?.Plan == TenantPlan.Global) ? 1 : 0
                    });
                }

                var totalTenants = await _db.Tenants.CountAsync();
                var activeTenants = await _db.Tenants.CountAsync(t => t.IsActive);
                var totalUsers = await _db.UserAccounts.CountAsync();
                var enterpriseTenants = await _db.Tenants.CountAsync(t => t.Plan == TenantPlan.Enterprise || t.Plan == TenantPlan.Global);

                return Ok(new
                {
                    totalTenants,
                    activeTenants,
                    inactiveTenants = totalTenants - activeTenants,
                    totalUsers,
                    enterpriseTenants
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Tenants] Lỗi lấy tóm tắt tenants: {Message}", ex.Message);
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        // ── GET api/admin/tenants/{id} ──────────────────────────────────────────
        /// <summary>
        /// Lấy chi tiết thông tin 1 Tenant kèm danh sách Admin đại diện.
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetTenantById(Guid id)
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
                if (role != AppRoles.SystemAdmin)
                {
                    var myTenantId = await GetTenantIdAsync();
                    if (id != myTenantId)
                    {
                        return Forbid();
                    }
                }

                var tenant = await _db.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tenant == null)
                    return NotFound(new { message = "Không tìm thấy khách thuê." });

                var adminUsers = await _db.UserAccounts
                    .AsNoTracking()
                    .Where(u => u.TenantId == id && (u.Role == AppRoles.TenantAdmin || u.Role == AppRoles.SystemAdmin))
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FullName,
                        u.PhoneNumber,
                        u.Role,
                        u.IsActive,
                        u.LastLoginAt
                    })
                    .ToListAsync();

                var currentUsersCount = await _db.UserAccounts.CountAsync(u => u.TenantId == id);
                var orgUnitsCount = await _db.OrganizationUnits.CountAsync(o => o.TenantId == id && o.IsActive);

                return Ok(new
                {
                    tenant,
                    currentUsersCount,
                    orgUnitsCount,
                    adminUsers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Tenants] Lỗi lấy chi tiết tenant {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        // ── POST api/admin/tenants ─────────────────────────────────────────────
        /// <summary>
        /// Khởi tạo một Khách thuê mới, tự động tạo Hội sở chính (Root OU) và tùy chọn cấp TenantAdmin mặc định.
        /// Chỉ dành riêng cho SystemAdmin toàn nền tảng.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest req)
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
                if (role != AppRoles.SystemAdmin)
                {
                    return Forbid();
                }

                if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
                    return BadRequest(new { message = "Mã khách thuê (Code) và Tên khách thuê (Name) là bắt buộc." });

                var cleanCode = req.Code.Trim().ToLowerInvariant();
                var exists = await _db.Tenants.AnyAsync(t => t.Code == cleanCode);
                if (exists)
                    return Conflict(new { message = $"Mã khách thuê '{cleanCode}' đã tồn tại." });

                var tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Code = cleanCode,
                    Name = req.Name.Trim(),
                    LogoUrl = req.LogoUrl?.Trim(),
                    PrimaryColor = string.IsNullOrWhiteSpace(req.PrimaryColor) ? "#1a73e8" : req.PrimaryColor.Trim(),
                    Domain = req.Domain?.Trim().ToLowerInvariant(),
                    CustomDomain = req.CustomDomain?.Trim().ToLowerInvariant(),
                    Plan = req.Plan,
                    ScaleType = req.ScaleType,
                    MaxUsers = req.MaxUsers > 0 ? req.MaxUsers : 1000,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = req.ExpiresAt
                };

                _db.Tenants.Add(tenant);

                // 1. Tự động khởi tạo OU gốc Hội sở chính
                var rootOrg = new OrganizationUnit
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    ParentId = null,
                    Code = "HQ",
                    Name = $"{tenant.Name} — Hội sở chính / Ban Lãnh đạo",
                    UnitType = OrgUnitType.HeadOffice,
                    HierarchyPath = "/HQ",
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _db.OrganizationUnits.Add(rootOrg);

                // 2. Tùy chọn tạo tài khoản TenantAdmin mặc định
                if (!string.IsNullOrWhiteSpace(req.AdminEmail) && !string.IsNullOrWhiteSpace(req.AdminPassword))
                {
                    var adminEmailClean = req.AdminEmail.Trim().ToLowerInvariant();
                    var userExists = await _db.UserAccounts.AnyAsync(u => u.Email == adminEmailClean);
                    if (userExists)
                        return Conflict(new { message = $"Email quản trị '{adminEmailClean}' đã tồn tại trong hệ thống." });

                    var adminUser = new UserAccount
                    {
                        Id = Guid.NewGuid(),
                        Email = adminEmailClean,
                        FullName = string.IsNullOrWhiteSpace(req.AdminFullName) ? $"{tenant.Name} Admin" : req.AdminFullName.Trim(),
                        PasswordHash = PasswordSecurityHelper.HashPassword(req.AdminPassword),
                        Role = AppRoles.TenantAdmin,
                        TenantId = tenant.Id,
                        OrgUnitId = rootOrg.Id,
                        IsPremium = true,
                        SubscriptionTier = "ENTERPRISE",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.UserAccounts.Add(adminUser);

                    _db.UserRoles.Add(new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = adminUser.Id.ToString(),
                        TenantId = tenant.Id,
                        Role = AppRoles.TenantAdmin,
                        OrgUnitId = rootOrg.Id,
                        AssignedBy = "SystemAdmin",
                        IsActive = true
                    });
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("[Tenants] Đã tạo thành công khách thuê mới: {Code} ({Name})", tenant.Code, tenant.Name);

                return Created($"api/admin/tenants/{tenant.Id}", new
                {
                    message = "Tạo khách thuê và cơ cấu ban đầu thành công.",
                    tenantId = tenant.Id,
                    tenantCode = tenant.Code
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Tenants] Lỗi tạo khách thuê: {Message}", ex.Message);
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        // ── PUT api/admin/tenants/{id} ─────────────────────────────────────────
        /// <summary>
        /// Cập nhật thông tin Khách thuê: Tên, Gói cước, Hạn mức, Domain, Trạng thái.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest req)
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
                if (role != AppRoles.SystemAdmin)
                {
                    var myTenantId = await GetTenantIdAsync();
                    if (id != myTenantId)
                    {
                        return Forbid();
                    }
                }

                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
                if (tenant == null)
                    return NotFound(new { message = "Không tìm thấy khách thuê." });

                if (!string.IsNullOrWhiteSpace(req.Name)) tenant.Name = req.Name.Trim();
                if (req.LogoUrl != null) tenant.LogoUrl = req.LogoUrl.Trim();
                if (!string.IsNullOrWhiteSpace(req.PrimaryColor)) tenant.PrimaryColor = req.PrimaryColor.Trim();
                if (req.Domain != null) tenant.Domain = req.Domain.Trim().ToLowerInvariant();
                if (req.CustomDomain != null) tenant.CustomDomain = req.CustomDomain.Trim().ToLowerInvariant();
                
                // Chỉ SystemAdmin mới có quyền đổi Gói cước (Plan) và Hạn mức người dùng (MaxUsers)
                if (role == AppRoles.SystemAdmin)
                {
                    if (req.Plan.HasValue) tenant.Plan = req.Plan.Value;
                    if (req.ScaleType.HasValue) tenant.ScaleType = req.ScaleType.Value;
                    if (req.MaxUsers.HasValue && req.MaxUsers.Value > 0) tenant.MaxUsers = req.MaxUsers.Value;
                    if (req.IsActive.HasValue) tenant.IsActive = req.IsActive.Value;
                    if (req.ExpiresAt.HasValue) tenant.ExpiresAt = req.ExpiresAt.Value;
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("[Tenants] Đã cập nhật thông tin khách thuê: {Code}", tenant.Code);

                return Ok(new { message = "Cập nhật khách thuê thành công.", tenant });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Tenants] Lỗi cập nhật khách thuê: {Message}", ex.Message);
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        // ── DELETE api/admin/tenants/{id} ──────────────────────────────────────
        /// <summary>
        /// Khóa hoặc vô hiệu hóa Khách thuê an toàn.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTenant(Guid id)
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
                if (role != AppRoles.SystemAdmin)
                {
                    return Forbid();
                }

                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
                if (tenant == null)
                    return NotFound(new { message = "Không tìm thấy khách thuê." });

                tenant.IsActive = false;
                await _db.SaveChangesAsync();

                _logger.LogInformation("[Tenants] Đã vô hiệu hóa khách thuê: {Code}", tenant.Code);
                return Ok(new { message = "Đã tạm khóa khách thuê thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Tenants] Lỗi xóa khách thuê: {Message}", ex.Message);
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
    }

    public class CreateTenantRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; } = "#1a73e8";
        public string? Domain { get; set; }
        public string? CustomDomain { get; set; }
        public TenantPlan Plan { get; set; } = TenantPlan.Enterprise;
        public TenantScaleType ScaleType { get; set; } = TenantScaleType.Enterprise;
        public int MaxUsers { get; set; } = 1000;
        public DateTime? ExpiresAt { get; set; }

        // Tùy chọn tạo sẵn tài khoản Admin đầu tiên
        public string? AdminEmail { get; set; }
        public string? AdminPassword { get; set; }
        public string? AdminFullName { get; set; }
    }

    public class UpdateTenantRequest
    {
        public string? Name { get; set; }
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? Domain { get; set; }
        public string? CustomDomain { get; set; }
        public TenantPlan? Plan { get; set; }
        public TenantScaleType? ScaleType { get; set; }
        public int? MaxUsers { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
