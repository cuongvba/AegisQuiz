using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.API.Security
{
    /// <summary>
    /// [RBAC — Privilege Ceiling] Yêu cầu Caller phải có trọng số vai trò (Role Weight)
    /// tối thiểu bằng <see cref="MinWeight"/>. Thay thế cho danh sách role dạng chuỗi
    /// (<c>[Authorize(Roles = "A,B,C")]</c>) — vai trò cao hơn tự động kế thừa quyền của
    /// vai trò thấp hơn, tránh phải liệt kê thủ công và quên cập nhật khi thêm role mới.
    /// </summary>
    public class MinimumRoleWeightRequirement : IAuthorizationRequirement
    {
        public int MinWeight { get; }

        public MinimumRoleWeightRequirement(int minWeight)
        {
            MinWeight = minWeight;
        }

        /// <summary>Tiện ích: khởi tạo ngưỡng theo đúng trọng số của một AppRoles.* cụ thể.</summary>
        public static MinimumRoleWeightRequirement ForRole(string role) =>
            new MinimumRoleWeightRequirement(AppRoleWeights.GetWeight(role));
    }

    public class RoleWeightAuthorizationHandler : AuthorizationHandler<MinimumRoleWeightRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            MinimumRoleWeightRequirement requirement)
        {
            var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value
                          ?? context.User.FindFirst("role")?.Value
                          ?? context.User.FindFirst("roles")?.Value;

            var callerWeight = AppRoleWeights.GetWeight(roleClaim);

            if (callerWeight >= requirement.MinWeight)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }

    /// <summary>Tên các Policy "MinRole:{Role}" đăng ký trong Program.cs — tránh magic string rải rác.</summary>
    public static class RoleWeightPolicies
    {
        public const string SystemAdmin    = "MinRole:SystemAdmin";
        public const string TenantAdmin    = "MinRole:TenantAdmin";
        public const string ContentManager = "MinRole:ContentManager";
        public const string OrgUnitManager = "MinRole:OrgUnitManager";
        public const string TeamLeader     = "MinRole:TeamLeader";
        public const string Learner        = "MinRole:Learner";
    }
}
