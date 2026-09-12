using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [RBAC SSOT] Hằng số vai trò — Nguồn sự thật duy nhất cho toàn bộ stack.
    /// Sử dụng ở: JWT claims, [Authorize(Roles=...)], ProtectedRoute, useAuth hook.
    /// KHÔNG dùng string literal "admin"/"student" ở bất kỳ nơi nào khác.
    /// </summary>
    public static class AppRoles
    {
        /// <summary>Quản trị toàn hệ thống (cross-tenant). Platform operations.</summary>
        public const string SystemAdmin    = "SystemAdmin";

        /// <summary>Quản trị viên Tenant. Quản lý user/role/nội dung/billing trong org.</summary>
        public const string TenantAdmin    = "TenantAdmin";

        /// <summary>Quản lý nội dung. CRUD câu hỏi, đề thi, notebook.</summary>
        public const string ContentManager = "ContentManager";

        /// <summary>Giảng viên / Trainer. Tạo đề thi, giám sát phòng thi, xem kết quả học viên của mình.</summary>
        public const string Instructor     = "Instructor";

        /// <summary>Quản lý đơn vị tổ chức. Phân công học viên vào nhóm/lớp.</summary>
        public const string OrgUnitManager = "OrgUnitManager";

        /// <summary>
        /// Trưởng nhóm. Quản lý nhóm Learner được assign.
        /// Scope: gắn với OrgUnitId cụ thể (UserRole.OrgUnitId).
        /// Quyền: xem thành viên, xem tiến độ, assign đề, xem báo cáo nhóm.
        /// KHÔNG có: quản lý nội dung, quản lý user, truy cập /admin.
        /// </summary>
        public const string TeamLeader     = "TeamLeader";

        /// <summary>Học viên (Free / Premium). Luyện tập và thi.</summary>
        public const string Learner        = "Learner";

        /// <summary>Khách xem. Read-only, chưa kích hoạt / chưa premium.</summary>
        public const string GuestViewer    = "GuestViewer";

        // ── Legacy alias (backward-compat, sẽ xóa sau Sprint 2) ──────────────
        /// <summary>@deprecated Dùng TenantAdmin thay thế.</summary>
        [Obsolete("Use AppRoles.TenantAdmin. Will be removed in Sprint 2.")]
        public const string Admin          = TenantAdmin;

        /// <summary>@deprecated Dùng Learner thay thế.</summary>
        [Obsolete("Use AppRoles.Learner. Will be removed in Sprint 2.")]
        public const string Student        = Learner;

        // ── Helper: Roles có quyền vào Admin Panel ───────────────────────────
        public static readonly string[] AdminPanelRoles =
        {
            SystemAdmin, TenantAdmin, ContentManager, Instructor, OrgUnitManager
        };

        // ── Helper: Roles có quyền xem dữ liệu nhóm ─────────────────────────
        public static readonly string[] TeamDataRoles =
        {
            SystemAdmin, TenantAdmin, Instructor, OrgUnitManager, TeamLeader
        };

        // ── Helper: Check nhanh ───────────────────────────────────────────────
        public static bool IsAdminLevel(string? role) =>
            role is SystemAdmin or TenantAdmin;

        public static bool CanAccessAdminPanel(string? role) =>
            Array.IndexOf(AdminPanelRoles, role) >= 0;

        public static bool CanViewTeamData(string? role) =>
            Array.IndexOf(TeamDataRoles, role) >= 0;
    }

    // ── Permission Constants ──────────────────────────────────────────────────
    /// <summary>
    /// Fine-grained permission strings.
    /// Map từ Role → Permissions được định nghĩa tại: AppRolePermissions.GetPermissions()
    /// </summary>
    public static class AppPermissions
    {
        public const string QuestionsWrite      = "questions:write";
        public const string QuestionsRead       = "questions:read";
        public const string ExamsCreate         = "exams:create";
        public const string ExamsAssign         = "exams:assign";
        public const string TeamViewMembers     = "team:view_members";
        public const string TeamViewProgress    = "team:view_progress";
        public const string TeamManageMembers   = "team:manage_members";
        public const string UsersManage         = "users:manage";
        public const string ReportsTeam         = "reports:team";
        public const string PracticeAccess      = "practice:access";
        public const string AdminAccess         = "admin:access";
    }

    // ── Role → Permission Map ─────────────────────────────────────────────────
    public static class AppRolePermissions
    {
        public static string[] GetPermissions(string role) => role switch
        {
            AppRoles.SystemAdmin or AppRoles.TenantAdmin => new[]
            {
                AppPermissions.QuestionsWrite, AppPermissions.QuestionsRead,
                AppPermissions.ExamsCreate,    AppPermissions.ExamsAssign,
                AppPermissions.TeamViewMembers,AppPermissions.TeamViewProgress,
                AppPermissions.TeamManageMembers,AppPermissions.UsersManage,
                AppPermissions.ReportsTeam,    AppPermissions.PracticeAccess,
                AppPermissions.AdminAccess
            },
            AppRoles.ContentManager or AppRoles.Instructor => new[]
            {
                AppPermissions.QuestionsWrite, AppPermissions.QuestionsRead,
                AppPermissions.ExamsCreate,    AppPermissions.ExamsAssign,
                AppPermissions.TeamViewMembers,AppPermissions.TeamViewProgress,
                AppPermissions.ReportsTeam,    AppPermissions.PracticeAccess,
                AppPermissions.AdminAccess
            },
            AppRoles.OrgUnitManager => new[]
            {
                AppPermissions.ExamsAssign,
                AppPermissions.TeamViewMembers,AppPermissions.TeamViewProgress,
                AppPermissions.TeamManageMembers,AppPermissions.UsersManage,
                AppPermissions.ReportsTeam,    AppPermissions.PracticeAccess,
                AppPermissions.AdminAccess
            },
            // ★ TeamLeader: xem + assign nhóm mình, KHÔNG quản lý nội dung/user
            AppRoles.TeamLeader => new[]
            {
                AppPermissions.ExamsAssign,
                AppPermissions.TeamViewMembers,
                AppPermissions.TeamViewProgress,
                AppPermissions.ReportsTeam,
                AppPermissions.PracticeAccess
            },
            AppRoles.Learner => new[]
            {
                AppPermissions.QuestionsRead,
                AppPermissions.PracticeAccess
            },
            _ => Array.Empty<string>() // GuestViewer — no permissions
        };

        public static bool HasPermission(string role, string permission) =>
            Array.IndexOf(GetPermissions(role), permission) >= 0;
    }
}
