/**
 * useAuth — Hook tập trung quản lý xác thực và phân quyền.
 *
 * Cách dùng:
 *   const { user, role, isAdmin, isTeamLeader, hasPermission, hasRole } = useAuth();
 *
 * Source: localStorage('user') — được set bởi LoginPage/CallbackPage sau login.
 * Role được normalize qua normalizeRole() đảm bảo luôn là AppRoleType hợp lệ.
 */
import { useMemo, useContext } from 'react';
import { AuthContext } from '@/app/providers/AuthProvider';
import {
  type UserProfile,
  type AppRoleType,
  type PermissionType,
  AppRole,
  normalizeRole,
  hasPermission as checkPermission,
  canAccessAdminPanel,
  isAdminLevel,
} from '@/types/auth';

export interface UseAuthReturn {
  /** User profile đã được parse và normalize. null nếu chưa login. */
  user:           UserProfile | null;
  /** Role hiện tại đã normalize thành AppRoleType */
  role:           AppRoleType;
  /** Token JWT từ localStorage */
  token:          string | null;
  /** true nếu đã đăng nhập (có token + user) */
  isAuthenticated: boolean;
  /** true trong quá trình khởi tạo */
  isInitializing?: boolean;

  // ── Convenience role flags ────────────────────────────────────────────────
  /** SystemAdmin hoặc TenantAdmin */
  isAdmin:         boolean;
  isSystemAdmin:   boolean;
  isTenantAdmin:   boolean;
  isContentManager:boolean;
  isInstructor:    boolean;
  isOrgUnitManager:boolean;
  /** ★ Trưởng nhóm — có quyền xem/quản lý nhóm learner của mình */
  isTeamLeader:    boolean;
  isLearner:       boolean;
  isGuest:         boolean;
  /** Bất kỳ role nào có quyền vào Admin Panel */
  canAccessAdmin:  boolean;

  // ── Permission check ──────────────────────────────────────────────────────
  /** Kiểm tra xem role hiện tại có permission này không */
  hasPermission:  (permission: PermissionType) => boolean;
  /** Kiểm tra xem user có ít nhất 1 trong danh sách role */
  hasRole:        (...roles: AppRoleType[]) => boolean;

  // ── Auth helpers ──────────────────────────────────────────────────────────
  logout:         () => void;
}

export function useAuth(): UseAuthReturn {
  const context = useContext(AuthContext);
  if (context) {
    return context;
  }

  const token   = localStorage.getItem('token');
  const userRaw = localStorage.getItem('user');

  const user = useMemo<UserProfile | null>(() => {
    if (!userRaw) return null;
    try {
      const parsed = JSON.parse(userRaw) as Partial<UserProfile> & { role?: string };
      return {
        id:          parsed.id ?? '',
        name:        parsed.name,
        email:       parsed.email,
        // [RBAC] Normalize về AppRoleType để đảm bảo backward-compat với 'admin'/'student'
        role:        normalizeRole(parsed.role),
        isPremium:   parsed.isPremium ?? false,
        tenantId:    parsed.tenantId,
        orgUnitId:   parsed.orgUnitId,
        orgUnitName: parsed.orgUnitName,
        ou:          parsed.ou,
      };
    } catch {
      return null;
    }
  }, [userRaw]);

  const role: AppRoleType = user?.role ?? AppRole.GuestViewer;
  const isAuthenticated   = !!token && !!user;

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = '/login';
  };

  return {
    user,
    role,
    token,
    isAuthenticated,

    // Role flags
    isAdmin:          isAdminLevel(role),
    isSystemAdmin:    role === AppRole.SystemAdmin,
    isTenantAdmin:    role === AppRole.TenantAdmin,
    isContentManager: role === AppRole.ContentManager,
    isInstructor:     role === AppRole.Instructor,
    isOrgUnitManager: role === AppRole.OrgUnitManager,
    isTeamLeader:     role === AppRole.TeamLeader,
    isLearner:        role === AppRole.Learner,
    isGuest:          role === AppRole.GuestViewer,
    canAccessAdmin:   canAccessAdminPanel(role),

    // Permission + Role checks
    hasPermission: (perm) => checkPermission(role, perm),
    hasRole:       (...roles) => roles.includes(role),

    logout,
  };
}
