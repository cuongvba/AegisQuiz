using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
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
    /// [Enterprise IAM / RBAC] Quản trị người dùng & phân quyền đa tầng đa khách thuê (Multi-Tenant & Scoped OU).
    /// Chỉ SystemAdmin và TenantAdmin có thẩm quyền truy cập.
    /// </summary>
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = $"{AppRoles.SystemAdmin},{AppRoles.TenantAdmin}")]
    public class AdminUsersController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        private readonly ITenantContext _tenant;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            AegisQuizDbContext db,
            ITenantContext tenant,
            ILogger<AdminUsersController> logger)
        {
            _db = db;
            _tenant = tenant;
            _logger = logger;
        }

        // ── GET api/admin/users ────────────────────────────────────────────────
        /// <summary>
        /// Lấy danh sách người dùng trong Tenant có phân trang, tìm kiếm và lọc đa chiều theo Role, OU, Tier, Trạng thái.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] Guid? orgUnitId = null,
            [FromQuery] string? tier = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var tenantId = _tenant.CurrentTenantId;
                if (tenantId == Guid.Empty)
                    return BadRequest(new { message = "Không xác định được Tenant ID của phiên làm việc." });

                var query = _db.UserAccounts
                    .AsNoTracking()
                    .Include(u => u.OrgUnit)
                    .Where(u => u.TenantId == tenantId);

                // 1. Tìm kiếm theo Tên hoặc Email
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLowerInvariant();
                    query = query.Where(u => u.Email.ToLower().Contains(term) || u.FullName.ToLower().Contains(term));
                }

                // 2. Lọc theo Role
                if (!string.IsNullOrWhiteSpace(role))
                {
                    var cleanRole = role.Trim();
                    query = query.Where(u => u.Role == cleanRole);
                }

                // 3. Lọc theo OrgUnit (Đơn vị phòng ban trong cây)
                if (orgUnitId.HasValue && orgUnitId.Value != Guid.Empty)
                {
                    // Lấy hierarchyPath của node được chọn để lọc toàn bộ cây con nếu có
                    var selectedOrg = await _db.OrganizationUnits
                        .AsNoTracking()
                        .FirstOrDefaultAsync(o => o.Id == orgUnitId.Value && o.TenantId == tenantId);

                    if (selectedOrg != null && !string.IsNullOrEmpty(selectedOrg.HierarchyPath))
                    {
                        var pathPrefix = selectedOrg.HierarchyPath;
                        query = query.Where(u => u.OrgUnitId == orgUnitId.Value ||
                                                 (u.OrgUnit != null && u.OrgUnit.HierarchyPath.StartsWith(pathPrefix)));
                    }
                    else
                    {
                        query = query.Where(u => u.OrgUnitId == orgUnitId.Value);
                    }
                }

                // 4. Lọc theo Gói thuê bao
                if (!string.IsNullOrWhiteSpace(tier))
                {
                    var cleanTier = tier.Trim().ToUpperInvariant();
                    query = query.Where(u => u.SubscriptionTier.ToUpper() == cleanTier);
                }

                // 5. Lọc theo Trạng thái hoạt động
                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                var totalCount = await query.CountAsync();

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var items = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        id = u.Id.ToString(),
                        email = u.Email,
                        fullName = u.FullName,
                        phoneNumber = u.PhoneNumber,
                        avatarUrl = u.AvatarUrl,
                        role = u.Role,
                        orgUnitId = u.OrgUnitId.HasValue ? u.OrgUnitId.Value.ToString() : null,
                        orgUnitName = u.OrgUnit != null ? u.OrgUnit.Name : null,
                        orgUnitHierarchyPath = u.OrgUnit != null ? u.OrgUnit.HierarchyPath : null,
                        isPremium = u.IsPremium,
                        subscriptionTier = u.SubscriptionTier,
                        subscriptionExpiresAt = u.SubscriptionExpiresAt,
                        isActive = u.IsActive,
                        failedLoginAttempts = u.FailedLoginAttempts,
                        lockoutEnd = u.LockoutEnd,
                        lastLoginAt = u.LastLoginAt,
                        createdAt = u.CreatedAt
                    })
                    .ToListAsync();

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
                _logger.LogError(ex, "[AdminUsers] Lỗi lấy danh sách người dùng: {Message}", ex.Message);
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        // ── GET api/admin/users/stats ──────────────────────────────────────────
        /// <summary>
        /// Thống kê KPI người dùng theo Tenant phục vụ Admin Dashboard.
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                var tenantId = _tenant.CurrentTenantId;
                if (tenantId == Guid.Empty)
                    return BadRequest(new { message = "Không xác định được Tenant ID." });

                var users = await _db.UserAccounts
                    .AsNoTracking()
                    .Where(u => u.TenantId == tenantId)
                    .Select(u => new { u.Role, u.IsActive, u.IsPremium, u.SubscriptionTier })
                    .ToListAsync();

                var totalUsers = users.Count;
                var activeUsers = users.Count(u => u.IsActive);
                var lockedUsers = users.Count(u => !u.IsActive);
                var adminUsers = users.Count(u => u.Role == AppRoles.SystemAdmin || u.Role == AppRoles.TenantAdmin);
                var leaderUsers = users.Count(u => u.Role == AppRoles.TeamLeader || u.Role == AppRoles.OrgUnitManager || u.Role == AppRoles.Instructor);
                var vipUsers = users.Count(u => u.IsPremium || u.SubscriptionTier == "VIP" || u.SubscriptionTier == "ENTERPRISE");

                return Ok(new
                {
                    totalUsers,
                    activeUsers,
                    lockedUsers,
                    adminUsers,
                    leaderUsers,
                    vipUsers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminUsers] Lỗi lấy thống kê người dùng: {Message}", ex.Message);
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        // ── POST api/admin/users ───────────────────────────────────────────────
        /// <summary>
        /// Tạo mới một tài khoản người dùng trực tiếp trong Tenant (dành cho Admin).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserRequest request)
        {
            try
            {
                var tenantId = _tenant.CurrentTenantId;
                if (tenantId == Guid.Empty)
                    return BadRequest(new { message = "Không xác định được Tenant ID." });

                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest(new { message = "Email và Mật khẩu là bắt buộc." });

                var emailClean = request.Email.Trim().ToLowerInvariant();
                var exists = await _db.UserAccounts.AnyAsync(u => u.Email == emailClean);
                if (exists)
                    return Conflict(new { message = $"Email '{emailClean}' đã tồn tại trong hệ thống." });

                var roleClean = string.IsNullOrWhiteSpace(request.Role) ? AppRoles.Learner : request.Role.Trim();
                if (!IsValidRole(roleClean))
                    return BadRequest(new { message = $"Vai trò '{roleClean}' không hợp lệ." });

                if (roleClean == AppRoles.TeamLeader && !request.OrgUnitId.HasValue)
                    return BadRequest(new { message = "Vai trò Trưởng nhóm (TeamLeader) bắt buộc phải gán Đơn vị phòng ban (OrgUnitId)." });

                if (request.OrgUnitId.HasValue)
                {
                    var orgExists = await _db.OrganizationUnits
                        .AnyAsync(o => o.Id == request.OrgUnitId.Value && o.TenantId == tenantId);
                    if (!orgExists)
                        return BadRequest(new { message = "Đơn vị phòng ban không tồn tại trong Tenant này." });
                }

                var passwordHash = PasswordSecurityHelper.HashPassword(request.Password);
                var tierClean = string.IsNullOrWhiteSpace(request.SubscriptionTier) ? "FREE" : request.SubscriptionTier.Trim().ToUpperInvariant();
                var isPremium = tierClean == "VIP" || tierClean == "ENTERPRISE";

                var user = new UserAccount
                {
                    Id = Guid.NewGuid(),
                    Email = emailClean,
                    FullName = string.IsNullOrWhiteSpace(request.FullName) ? emailClean : request.FullName.Trim(),
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    PasswordHash = passwordHash,
                    Role = roleClean,
                    TenantId = tenantId,
                    OrgUnitId = request.OrgUnitId,
                    IsPremium = isPremium,
                    SubscriptionTier = tierClean,
                    SubscriptionExpiresAt = request.SubscriptionExpiresAt,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.UserAccounts.Add(user);

                // Đồng bộ bảng UserRoles
                var assignedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";
                _db.UserRoles.Add(new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id.ToString(),
                    TenantId = tenantId,
                    Role = roleClean,
                    OrgUnitId = request.OrgUnitId,
                    AssignedBy = assignedBy,
                    IsActive = true
                });

                await _db.SaveChangesAsync();

                _logger.LogInformation("[AdminUsers] Admin {Admin} đã tạo tài khoản mới: {Email} ({Role})", assignedBy, user.Email, user.Role);

                return Created($"api/admin/users/{user.Id}", new
                {
                    message = "Tạo tài khoản người dùng thành công.",
                    userId = user.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminUsers] Lỗi tạo tài khoản: {Message}", ex.Message);
                return StatusCode(500, new { message = $"Lỗi tạo tài khoản: {ex.Message}" });
            }
        }

        // ── PUT api/admin/users/{id} ───────────────────────────────────────────
        /// <summary>
        /// Cập nhật thông tin quản trị: Vai trò, Đơn vị phòng ban, Gói thuê bao, Trạng thái Khóa/Mở.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateAdminUserRequest request)
        {
            try
            {
                var tenantId = _tenant.CurrentTenantId;
                var user = await _db.UserAccounts
                    .Include(u => u.OrgUnit)
                    .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

                if (user == null)
                    return NotFound(new { message = "Không tìm thấy người dùng trong Tenant này." });

                var assignedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";

                // Cập nhật Họ tên
                if (!string.IsNullOrWhiteSpace(request.FullName))
                    user.FullName = request.FullName.Trim();

                // Cập nhật SĐT
                if (request.PhoneNumber != null)
                    user.PhoneNumber = request.PhoneNumber.Trim();

                // Cập nhật Role
                if (!string.IsNullOrWhiteSpace(request.Role) && request.Role != user.Role)
                {
                    var cleanRole = request.Role.Trim();
                    if (!IsValidRole(cleanRole))
                        return BadRequest(new { message = $"Vai trò '{cleanRole}' không hợp lệ." });

                    user.Role = cleanRole;

                    // Đồng bộ role vào UserRoles
                    var existingRole = await _db.UserRoles
                        .FirstOrDefaultAsync(r => r.UserId == user.Id.ToString() && r.TenantId == tenantId && r.IsActive);

                    if (existingRole != null)
                    {
                        existingRole.Role = cleanRole;
                        existingRole.OrgUnitId = request.OrgUnitId ?? user.OrgUnitId;
                        existingRole.AssignedBy = assignedBy;
                    }
                    else
                    {
                        _db.UserRoles.Add(new UserRole
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id.ToString(),
                            TenantId = tenantId,
                            Role = cleanRole,
                            OrgUnitId = request.OrgUnitId ?? user.OrgUnitId,
                            AssignedBy = assignedBy,
                            IsActive = true
                        });
                    }
                }

                // Cập nhật OrgUnitId
                if (request.OrgUnitId.HasValue)
                {
                    if (request.OrgUnitId.Value == Guid.Empty)
                    {
                        if (user.Role == AppRoles.TeamLeader)
                            return BadRequest(new { message = "Vai trò Trưởng nhóm (TeamLeader) bắt buộc phải gắn với một Đơn vị phòng ban cụ thể." });

                        user.OrgUnitId = null;
                    }
                    else
                    {
                        var orgExists = await _db.OrganizationUnits
                            .AnyAsync(o => o.Id == request.OrgUnitId.Value && o.TenantId == tenantId);
                        if (!orgExists)
                            return BadRequest(new { message = "Đơn vị phòng ban không tồn tại trong Tenant này." });

                        user.OrgUnitId = request.OrgUnitId.Value;
                    }
                }
                else if (user.Role == AppRoles.TeamLeader && !user.OrgUnitId.HasValue)
                {
                    return BadRequest(new { message = "Vai trò Trưởng nhóm (TeamLeader) bắt buộc phải gắn với một Đơn vị phòng ban cụ thể." });
                }

                // Cập nhật Gói thuê bao
                if (!string.IsNullOrWhiteSpace(request.SubscriptionTier))
                {
                    var cleanTier = request.SubscriptionTier.Trim().ToUpperInvariant();
                    user.SubscriptionTier = cleanTier;
                    user.IsPremium = cleanTier == "VIP" || cleanTier == "ENTERPRISE";
                }

                if (request.SubscriptionExpiresAt.HasValue)
                {
                    user.SubscriptionExpiresAt = request.SubscriptionExpiresAt.Value;
                }

                // Cập nhật Trạng thái hoạt động (Khóa / Mở)
                if (request.IsActive.HasValue)
                {
                    user.IsActive = request.IsActive.Value;
                    if (user.IsActive)
                    {
                        user.FailedLoginAttempts = 0;
                        user.LockoutEnd = null;
                    }
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("[AdminUsers] Admin {Admin} đã cập nhật người dùng {UserId} ({Email})", assignedBy, user.Id, user.Email);

                return Ok(new
                {
                    message = "Cập nhật thông tin người dùng thành công.",
                    userId = user.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminUsers] Lỗi cập nhật người dùng: {Message}", ex.Message);
                return StatusCode(500, new { message = $"Lỗi cập nhật người dùng: {ex.Message}" });
            }
        }

        // ── POST api/admin/users/{id}/reset-password ───────────────────────────
        /// <summary>
        /// Quản trị viên đặt lại mật khẩu an toàn cho tài khoản người dùng.
        /// </summary>
        [HttpPost("{id:guid}/reset-password")]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] AdminResetPasswordRequest request)
        {
            try
            {
                var tenantId = _tenant.CurrentTenantId;
                var user = await _db.UserAccounts
                    .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

                if (user == null)
                    return NotFound(new { message = "Không tìm thấy người dùng trong Tenant này." });

                string newPlainPassword;
                if (!string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    if (request.NewPassword.Length < 6)
                        return BadRequest(new { message = "Mật khẩu mới phải có tối thiểu 6 ký tự." });

                    newPlainPassword = request.NewPassword;
                }
                else
                {
                    // Tự động sinh mật khẩu ngẫu nhiên an toàn 10 ký tự
                    newPlainPassword = GenerateSecurePassword(10);
                }

                user.PasswordHash = PasswordSecurityHelper.HashPassword(newPlainPassword);
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var admin = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";
                _logger.LogInformation("[Security] Admin {Admin} đã reset mật khẩu cho người dùng {Email}", admin, user.Email);

                return Ok(new
                {
                    message = "Đặt lại mật khẩu thành công.",
                    newPassword = newPlainPassword
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminUsers] Lỗi reset mật khẩu: {Message}", ex.Message);
                return StatusCode(500, new { message = $"Lỗi reset mật khẩu: {ex.Message}" });
            }
        }

        // ── POST api/admin/users/{id}/send-activation-link ────────────────────
        /// <summary>
        /// Tạo và gửi đường link kích hoạt tài khoản / đặt mật khẩu lần đầu (hoặc reset) cho nhân sự.
        /// </summary>
        [HttpPost("{id:guid}/send-activation-link")]
        public async Task<IActionResult> SendActivationLink(Guid id)
        {
            try
            {
                var tenantId = _tenant.CurrentTenantId;
                var user = await _db.UserAccounts
                    .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

                if (user == null)
                    return NotFound(new { message = "Không tìm thấy người dùng trong Tenant này." });

                // Sinh cryptographic token 32-byte
                var tokenBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(tokenBytes);
                }
                var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();

                user.PasswordResetToken = token;
                user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(24); // Link kích hoạt gửi từ Admin có hiệu lực 24 giờ
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var clientBaseUrl = "https://daotao.dehoc.vn";
                var activationLink = $"{clientBaseUrl}/reset-password?token={token}&email={Uri.EscapeDataString(user.Email)}";

                var admin = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "admin";
                _logger.LogInformation("[Security] Admin {Admin} đã tạo link kích hoạt cho {Email}: {Link}", admin, user.Email, activationLink);

                return Ok(new
                {
                    message = $"Đã tạo đường link kích hoạt mật khẩu cho {user.Email}.",
                    activationLink = activationLink,
                    expiresAt = user.PasswordResetTokenExpiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminUsers] Lỗi tạo link kích hoạt: {Message}", ex.Message);
                return StatusCode(500, new { message = $"Lỗi tạo link kích hoạt: {ex.Message}" });
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static bool IsValidRole(string role) => role switch
        {
            AppRoles.SystemAdmin or AppRoles.TenantAdmin or AppRoles.ContentManager
            or AppRoles.Instructor or AppRoles.OrgUnitManager or AppRoles.TeamLeader
            or AppRoles.Learner or AppRoles.GuestViewer => true,
            _ => false
        };

        private static string GenerateSecurePassword(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[bytes[i] % chars.Length];
            }
            return new string(result);
        }
    }

    public class CreateAdminUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; } = AppRoles.Learner;
        public Guid? OrgUnitId { get; set; }
        public string? SubscriptionTier { get; set; } = "FREE";
        public DateTime? SubscriptionExpiresAt { get; set; }
    }

    public class UpdateAdminUserRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Role { get; set; }
        public Guid? OrgUnitId { get; set; }
        public string? SubscriptionTier { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public bool? IsActive { get; set; }
    }

    public class AdminResetPasswordRequest
    {
        public string? NewPassword { get; set; }
    }
}
