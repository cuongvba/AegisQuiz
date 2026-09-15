/**
 * [RBAC SSOT] AegisQuiz — Auth Types & Role Constants
 *
 * QUAN TRỌNG: Đây là nguồn sự thật duy nhất cho tất cả role/permission trên Frontend.
 * KHÔNG dùng string literal 'admin'/'student' ở bất kỳ nơi nào khác.
 * Phải khớp với AppRoles constants trong Backend/Domain/Entities/AppRole.cs
 */

// ── Role Constants ────────────────────────────────────────────────────────────
export const AppRole = {
  SystemAdmin:    'SystemAdmin',
  TenantAdmin:    'TenantAdmin',
  ContentManager: 'ContentManager',
  Instructor:     'Instructor',
  OrgUnitManager: 'OrgUnitManager',
  /** Trưởng nhóm — quản lý nhóm Learner, xem tiến độ, giao đề thi */
  TeamLeader:     'TeamLeader',
  Learner:        'Learner',
  GuestViewer:    'GuestViewer',
} as const;

export type AppRoleType = typeof AppRole[keyof typeof AppRole];

// ── Permission Constants ──────────────────────────────────────────────────────
export const Permission = {
  QuestionsWrite:    'questions:write',
  QuestionsRead:     'questions:read',
  ExamsCreate:       'exams:create',
  ExamsAssign:       'exams:assign',
  TeamViewMembers:   'team:view_members',
  TeamViewProgress:  'team:view_progress',
  TeamManageMembers: 'team:manage_members',
  UsersManage:       'users:manage',
  ReportsTeam:       'reports:team',
  PracticeAccess:    'practice:access',
  AdminAccess:       'admin:access',
} as const;

export type PermissionType = typeof Permission[keyof typeof Permission];

// ── Role → Permission Map (mirror của Backend AppRolePermissions) ─────────────
const ROLE_PERMISSIONS: Record<AppRoleType, PermissionType[]> = {
  [AppRole.SystemAdmin]: [
    Permission.QuestionsWrite, Permission.QuestionsRead,
    Permission.ExamsCreate,    Permission.ExamsAssign,
    Permission.TeamViewMembers,Permission.TeamViewProgress,
    Permission.TeamManageMembers,Permission.UsersManage,
    Permission.ReportsTeam,    Permission.PracticeAccess,
    Permission.AdminAccess,
  ],
  [AppRole.TenantAdmin]: [
    Permission.QuestionsWrite, Permission.QuestionsRead,
    Permission.ExamsCreate,    Permission.ExamsAssign,
    Permission.TeamViewMembers,Permission.TeamViewProgress,
    Permission.TeamManageMembers,Permission.UsersManage,
    Permission.ReportsTeam,    Permission.PracticeAccess,
    Permission.AdminAccess,
  ],
  [AppRole.ContentManager]: [
    Permission.QuestionsWrite, Permission.QuestionsRead,
    Permission.ExamsCreate,    Permission.ExamsAssign,
    Permission.TeamViewMembers,Permission.TeamViewProgress,
    Permission.ReportsTeam,    Permission.PracticeAccess,
    Permission.AdminAccess,
  ],
  [AppRole.Instructor]: [
    Permission.QuestionsWrite, Permission.QuestionsRead,
    Permission.ExamsCreate,    Permission.ExamsAssign,
    Permission.TeamViewMembers,Permission.TeamViewProgress,
    Permission.ReportsTeam,    Permission.PracticeAccess,
    Permission.AdminAccess,
  ],
  [AppRole.OrgUnitManager]: [
    Permission.ExamsAssign,
    Permission.TeamViewMembers,Permission.TeamViewProgress,
    Permission.TeamManageMembers,Permission.UsersManage,
    Permission.ReportsTeam,    Permission.PracticeAccess,
    Permission.AdminAccess,
  ],
  // ★ TeamLeader: quản lý nhóm, phân bổ đề thi, tạo & sở hữu câu hỏi/chủ đề nhóm
  [AppRole.TeamLeader]: [
    Permission.QuestionsWrite,
    Permission.QuestionsRead,
    Permission.ExamsCreate,
    Permission.ExamsAssign,
    Permission.TeamViewMembers,
    Permission.TeamViewProgress,
    Permission.ReportsTeam,
    Permission.PracticeAccess,
    Permission.AdminAccess,
  ],
  [AppRole.Learner]: [
    Permission.QuestionsRead,
    Permission.PracticeAccess,
  ],
  [AppRole.GuestViewer]: [],
};

export function getPermissions(role: AppRoleType): PermissionType[] {
  return ROLE_PERMISSIONS[role] ?? [];
}

export function hasPermission(role: AppRoleType | undefined, perm: PermissionType): boolean {
  if (!role) return false;
  return ROLE_PERMISSIONS[role]?.includes(perm) ?? false;
}

// ── Legacy normalization (backward-compat với localStorage cũ) ───────────────
export function normalizeRole(raw?: string | null): AppRoleType {
  switch (raw?.trim()) {
    case AppRole.SystemAdmin:    return AppRole.SystemAdmin;
    case AppRole.TenantAdmin:    return AppRole.TenantAdmin;
    case AppRole.ContentManager: return AppRole.ContentManager;
    case AppRole.Instructor:     return AppRole.Instructor;
    case AppRole.OrgUnitManager: return AppRole.OrgUnitManager;
    case AppRole.TeamLeader:     return AppRole.TeamLeader;
    case AppRole.Learner:        return AppRole.Learner;
    case AppRole.GuestViewer:    return AppRole.GuestViewer;
    // Legacy aliases
    case 'admin':   return AppRole.TenantAdmin;
    case 'student': return AppRole.Learner;
    case 'viewer':  return AppRole.GuestViewer;
    default:        return AppRole.GuestViewer;
  }
}

// ── User Profile Interface ────────────────────────────────────────────────────
export interface UserProfile {
  id:          string;
  name?:       string;
  email?:      string;
  phoneNumber?: string;
  /** AppRole constant — đã được normalize */
  role:        AppRoleType;
  isPremium:   boolean;
  subscriptionTier?: 'FREE' | 'VIP' | 'ENTERPRISE' | string;
  subscriptionExpiresAt?: string;
  avatar?:     string;
  tenantId?:   string;
  tenantName?: string;
  tenantCode?: string;
  /** OrgUnitId scope — bắt buộc với TeamLeader */
  orgUnitId?:  string;
  orgUnitName?: string;
  orgUnitHierarchyPath?: string;
  ou?:         string; // tên đơn vị hiển thị (display)
  createdAt?:  string;
  lastLoginAt?: string;
}

// ── Admin Panel access check ──────────────────────────────────────────────────
export const ADMIN_PANEL_ROLES: AppRoleType[] = [
  AppRole.SystemAdmin,
  AppRole.TenantAdmin,
  AppRole.ContentManager,
  AppRole.Instructor,
  AppRole.OrgUnitManager,
  AppRole.TeamLeader,
];

export function canAccessAdminPanel(role?: AppRoleType | null): boolean {
  return !!role && ADMIN_PANEL_ROLES.includes(role);
}

export function isAdminLevel(role?: AppRoleType | null): boolean {
  return role === AppRole.SystemAdmin || role === AppRole.TenantAdmin;
}

/**
 * Kiểm tra quyền quản lý / chỉnh sửa / xóa tài nguyên có cơ chế sở hữu (Questions, Topics).
 * - SystemAdmin, TenantAdmin, ContentManager: Quản lý toàn diện.
 * - TeamLeader: Quản lý các tài nguyên do mình tạo (creatorId) hoặc thuộc đơn vị mình (orgUnitId).
 */
export function canManageResource(
  user: UserProfile | null | undefined,
  resource: { creatorId?: string; orgUnitId?: string; scope?: string } | null | undefined
): boolean {
  if (!user) return false;
  if (user.role === AppRole.SystemAdmin || user.role === AppRole.TenantAdmin || user.role === AppRole.ContentManager) {
    return true;
  }
  if (user.role === AppRole.TeamLeader) {
    if (resource?.creatorId && resource.creatorId === user.id) return true;
    if (user.orgUnitId && resource?.orgUnitId && resource.orgUnitId === user.orgUnitId) return true;
    if (resource?.scope === 'TEAM' || resource?.scope === 'PERSONAL') return true;
    return false;
  }
  return false;
}
