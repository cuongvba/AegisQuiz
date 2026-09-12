import { type ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { type AppRoleType, type PermissionType, canAccessAdminPanel } from '@/types/auth';
import { useAuth } from '@/hooks/useAuth';

interface ProtectedRouteProps {
  children: ReactNode;
  /**
   * @deprecated Dùng requiredRoles thay thế.
   * Giữ lại để backward-compat với các Route đang dùng adminOnly.
   */
  adminOnly?: boolean;
  /**
   * Danh sách role được phép vào route này (OR logic — có 1 trong số là được).
   * Nếu không truyền: chỉ yêu cầu đăng nhập, không giới hạn role.
   *
   * @example
   * // Chỉ TeamLeader và TenantAdmin được vào:
   * <ProtectedRoute requiredRoles={[AppRole.TeamLeader, AppRole.TenantAdmin]}>
   */
  requiredRoles?: AppRoleType[];
  /**
   * Kiểm tra theo permission thay vì role trực tiếp.
   * Hữu ích khi nhiều role có cùng quyền (e.g. team:view_progress).
   */
  requiredPermission?: PermissionType;
  /** Route redirect khi bị từ chối (mặc định: '/'). */
  redirectTo?: string;
}

/**
 * [P1 Security] Route guard — bảo vệ các trang yêu cầu đăng nhập và phân quyền.
 *
 * Logic:
 *   1. Chưa đăng nhập (không có token/user) → redirect /login
 *   2. requiredRoles được chỉ định → kiểm tra role của user
 *   3. requiredPermission được chỉ định → kiểm tra permission
 *   4. adminOnly=true (legacy) → kiểm tra canAccessAdminPanel
 *   5. Pass → render children
 */
export function ProtectedRoute({
  children,
  adminOnly      = false,
  requiredRoles,
  requiredPermission,
  redirectTo     = '/',
}: ProtectedRouteProps) {
  const location = useLocation();
  const { user, token, role, hasRole, hasPermission, isInitializing } = useAuth();

  // ── 0. Chờ khởi tạo session từ storage ──────────────────────────────────
  if (isInitializing) {
    return null;
  }

  // ── 1. Chưa đăng nhập ────────────────────────────────────────────────────
  if (!token || !user) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  // ── 2. requiredRoles check (OR logic) ─────────────────────────────────────
  if (requiredRoles && requiredRoles.length > 0) {
    if (!hasRole(...requiredRoles)) {
      return <Navigate to={redirectTo} replace />;
    }
  }

  // ── 3. requiredPermission check ───────────────────────────────────────────
  if (requiredPermission) {
    if (!hasPermission(requiredPermission)) {
      return <Navigate to={redirectTo} replace />;
    }
  }

  // ── 4. Legacy adminOnly (backward-compat) ─────────────────────────────────
  if (adminOnly && !canAccessAdminPanel(role)) {
    return <Navigate to={redirectTo} replace />;
  }

  return <>{children}</>;
}

export default ProtectedRoute;
