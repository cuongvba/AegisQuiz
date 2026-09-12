/**
 * AegisQuiz — AuthProvider
 * ========================
 * React Context cung cấp reactive auth state cho toàn bộ app.
 *
 * GIẢI QUYẾT GAP-03:
 *  - Trước: mỗi component đọc localStorage trực tiếp → không reactive
 *  - Sau:   1 AuthContext → toàn bộ app nhận state thông qua hook
 *
 * Features:
 *  ✅ Cross-tab sync (storage event listener)
 *  ✅ Auto-logout khi token 401 (aegis:auth:expired event)
 *  ✅ JWT expiry detection
 *  ✅ Optimistic state (show UI ngay từ localStorage, không block)
 *
 * FSD Layer: app/providers/
 */

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import {
  normalizeRole,
  hasPermission as checkPermission,
  canAccessAdminPanel,
  isAdminLevel,
  AppRole,
  type UserProfile,
  type AppRoleType,
  type PermissionType,
} from '@/types/auth';

// ── Context Shape ───────────────────────────────────────────────────────────

export interface AuthContextValue {
  /** Parsed & normalized user profile. null = chưa đăng nhập */
  user:              UserProfile | null;
  /** Role đã normalize thành AppRoleType */
  role:              AppRoleType;
  /** JWT token từ storage */
  token:             string | null;
  /** true khi đã xác thực (có token + user) */
  isAuthenticated:   boolean;
  /** true trong quá trình khởi tạo đầu tiên */
  isInitializing:    boolean;

  // ── Role flags ────────────────────────────────────────────────────────
  isAdmin:           boolean;
  isSystemAdmin:     boolean;
  isTenantAdmin:     boolean;
  isContentManager:  boolean;
  isInstructor:      boolean;
  isOrgUnitManager:  boolean;
  isTeamLeader:      boolean;
  isLearner:         boolean;
  isGuest:           boolean;
  canAccessAdmin:    boolean;

  // ── Permission check ──────────────────────────────────────────────────
  hasPermission:     (permission: PermissionType) => boolean;
  hasRole:           (...roles: AppRoleType[]) => boolean;

  // ── Auth actions ──────────────────────────────────────────────────────
  /** Gọi sau khi login thành công — set token + user vào storage & context */
  login:             (token: string, user: UserProfile) => void;
  /** Xóa session, redirect về /login */
  logout:            () => void;
  /** Cập nhật user profile (sau khi update profile API) */
  updateUser:        (updates: Partial<UserProfile>) => void;
}

// ── Context ────────────────────────────────────────────────────────────────

export const AuthContext = createContext<AuthContextValue | null>(null);

// ── Helpers ────────────────────────────────────────────────────────────────

function readFromStorage(): { token: string | null; user: UserProfile | null } {
  const token = localStorage.getItem('token');
  const userRaw = localStorage.getItem('user');

  if (!token || !userRaw) return { token: null, user: null };

  try {
    const parsed = JSON.parse(userRaw) as Partial<UserProfile> & { role?: string };
    const user: UserProfile = {
      id:          parsed.id ?? '',
      name:        parsed.name,
      email:       parsed.email,
      role:        normalizeRole(parsed.role),
      isPremium:   parsed.isPremium ?? false,
      tenantId:    parsed.tenantId,
      orgUnitId:   parsed.orgUnitId,
      orgUnitName: parsed.orgUnitName,
      ou:          parsed.ou,
    };
    return { token, user };
  } catch {
    return { token: null, user: null };
  }
}

/**
 * Kiểm tra xem JWT token có còn hạn không.
 * Trả về true nếu token còn hạn hoặc không decode được (let server decide).
 */
/**
 * Kiểm tra xem JWT token có còn hạn không.
 * Trả về true nếu token còn hạn hoặc không decode được (let server decide).
 */
function isTokenValid(token: string): boolean {
  if (!token) return false;
  // Dev & mock tokens are always valid
  if (
    token.startsWith('mock_') ||
    token.startsWith('sso_') ||
    token.startsWith('dev_') ||
    token.startsWith('pki_')
  ) {
    return true;
  }
  try {
    const parts = token.split('.');
    if (parts.length !== 3) {
      // Non-standard token format: do not reject outright
      return true;
    }
    const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
    const exp = payload.exp as number | undefined;
    if (!exp) return true; // No expiry = valid
    return Date.now() < exp * 1000;
  } catch {
    return true; // Cannot decode → assume valid, server will reject if not
  }
}

// ── Provider ───────────────────────────────────────────────────────────────

export function AuthProvider({ children }: { children: ReactNode }) {
  const [initialData] = useState(() => {
    const { token: storedToken, user: storedUser } = readFromStorage();
    if (storedToken && storedUser && isTokenValid(storedToken)) {
      return { token: storedToken, user: storedUser };
    }
    return { token: null, user: null };
  });

  const [token, setToken] = useState<string | null>(initialData.token);
  const [user, setUser] = useState<UserProfile | null>(initialData.user);
  const [isInitializing, setIsInitializing] = useState(false);

  // Prevent stale closure in event listeners
  const tokenRef = useRef(token);
  tokenRef.current = token;

  // ── Initialize từ localStorage ─────────────────────────────────────
  useEffect(() => {
    const { token: storedToken, user: storedUser } = readFromStorage();

    if (storedToken && storedUser && isTokenValid(storedToken)) {
      setToken(storedToken);
      setUser(storedUser);
    } else if (storedToken && !isTokenValid(storedToken)) {
      // Token expired — clear storage
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      setToken(null);
      setUser(null);
    }

    setIsInitializing(false);
  }, []);

  // ── Cross-tab sync ─────────────────────────────────────────────────
  useEffect(() => {
    const handleStorage = (e: StorageEvent) => {
      if (e.key === 'token' || e.key === 'user') {
        const { token: newToken, user: newUser } = readFromStorage();
        setToken(newToken);
        setUser(newUser);
      }
    };

    window.addEventListener('storage', handleStorage);
    return () => window.removeEventListener('storage', handleStorage);
  }, []);

  // ── Auto-logout on 401 (from apiClient interceptor) ───────────────
  useEffect(() => {
    const handleAuthExpired = () => {
      setToken(null);
      setUser(null);
      window.location.href = '/login';
    };

    window.addEventListener('aegis:auth:expired', handleAuthExpired);
    return () => window.removeEventListener('aegis:auth:expired', handleAuthExpired);
  }, []);

  // ── Auth actions ───────────────────────────────────────────────────

  const login = useCallback((newToken: string, newUser: UserProfile) => {
    localStorage.setItem('token', newToken);
    localStorage.setItem('user', JSON.stringify(newUser));
    setToken(newToken);
    setUser(newUser);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    localStorage.removeItem('tenant_id');
    localStorage.removeItem('tenantId');
    setToken(null);
    setUser(null);
    window.location.href = '/login';
  }, []);

  const updateUser = useCallback((updates: Partial<UserProfile>) => {
    setUser((prev) => {
      if (!prev) return prev;
      const updated = { ...prev, ...updates };
      localStorage.setItem('user', JSON.stringify(updated));
      return updated;
    });
  }, []);

  // ── Derived values ─────────────────────────────────────────────────

  const role: AppRoleType = user?.role ?? AppRole.GuestViewer;
  const isAuthenticated = !!token && !!user;

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      role,
      token,
      isAuthenticated,
      isInitializing,

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

      hasPermission: (perm) => checkPermission(role, perm),
      hasRole: (...roles) => roles.includes(role),

      login,
      logout,
      updateUser,
    }),
    [user, role, token, isAuthenticated, isInitializing, login, logout, updateUser],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// ── Hook ───────────────────────────────────────────────────────────────────

/**
 * useAuthContext — hook chính, dùng thay thế useAuth.ts cũ.
 * Phải được dùng bên trong <AuthProvider>.
 */
export function useAuthContext(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('[AegisQuiz] useAuthContext phải được dùng bên trong <AuthProvider>');
  }
  return ctx;
}
