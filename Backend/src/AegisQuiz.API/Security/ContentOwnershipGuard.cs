using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.API.Security
{
    /// <summary>
    /// [Ownership Scoping] Kiểm tra quyền thao tác (Update/Delete/bulk-*) trên MỘT tài
    /// nguyên cụ thể (Question/Topic) sau khi đã qua cổng Role-Weight ở tầng controller.
    ///
    /// Quy tắc:
    ///   - Role-Weight ≥ ContentManager (ContentManager/Instructor/TenantAdmin/SystemAdmin):
    ///     toàn quyền trong phạm vi Tenant (đúng thiết kế hiện có — không đổi hành vi).
    ///   - TeamLeader: CHỈ được thao tác trên tài nguyên do chính mình tạo (CreatorId) hoặc
    ///     thuộc OrgUnit mình đang quản lý (tra theo UserRoles, cùng cách TeamLeaderController
    ///     đang dùng). Khớp đúng những gì Frontend `canManageResource()` đã giả định.
    ///   - Vai trò khác: không có quyền vào tới đây (đã bị chặn ở [Authorize] class-level).
    /// </summary>
    public interface IContentOwnershipGuard
    {
        Task<bool> CanManageAsync(ClaimsPrincipal user, string? creatorId, Guid? orgUnitId);
    }

    public class ContentOwnershipGuard : IContentOwnershipGuard
    {
        private readonly AegisQuizDbContext _db;
        private readonly ITenantContext     _tenant;

        public ContentOwnershipGuard(AegisQuizDbContext db, ITenantContext tenant)
        {
            _db     = db;
            _tenant = tenant;
        }

        public async Task<bool> CanManageAsync(ClaimsPrincipal user, string? creatorId, Guid? orgUnitId)
        {
            var role = _tenant.CurrentRole;

            // ContentManager/Instructor/TenantAdmin/SystemAdmin: toàn quyền trong Tenant.
            if (AppRoleWeights.GetWeight(role) >= AppRoleWeights.GetWeight(AppRoles.ContentManager))
                return true;

            if (role != AppRoles.TeamLeader)
                return false;

            var callerId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 1. Sở hữu trực tiếp (là người tạo).
            if (!string.IsNullOrEmpty(callerId) && !string.IsNullOrEmpty(creatorId) && callerId == creatorId)
                return true;

            // 2. Thuộc OrgUnit mà TeamLeader này đang quản lý.
            if (orgUnitId.HasValue && !string.IsNullOrEmpty(callerId))
            {
                var myOrgUnitId = await _db.UserRoles
                    .Where(r => r.UserId == callerId
                             && r.TenantId == _tenant.CurrentTenantId
                             && r.Role == AppRoles.TeamLeader
                             && r.IsActive
                             && r.OrgUnitId != null)
                    .Select(r => r.OrgUnitId)
                    .FirstOrDefaultAsync();

                if (myOrgUnitId.HasValue && myOrgUnitId.Value == orgUnitId.Value)
                    return true;
            }

            return false;
        }
    }
}
