import { AppRole, canAccessAdminPanel, type UserProfile } from '@/types/auth';

/**
 * [Smart Navigator] Phân luồng điều hướng thông minh theo vai trò chức năng và gói thuê bao.
 * Đảm bảo người dùng ngay sau khi đăng nhập / đăng ký được đưa về đúng không gian làm việc tối ưu nhất.
 *
 * Quy tắc:
 * 1. Nếu có trang đích returnTo hợp lệ và tài khoản đủ quyền -> Chuyển về returnTo.
 * 2. SystemAdmin / TenantAdmin -> /admin (Trung tâm chỉ huy & quản trị).
 * 3. ContentManager / Instructor -> /admin/questions (Ngân hàng câu hỏi & đề thi).
 * 4. TeamLeader / OrgUnitManager -> /team (Không gian đội nhóm, tiến độ thành viên).
 * 5. Learner VIP / Enterprise -> /practice (Smart LaunchPad AI thích ứng cao cấp).
 * 6. Learner Free / Default -> /practice (hoặc / theo ngữ cảnh).
 */
export function resolveSmartRedirect(user: UserProfile | null | undefined, returnTo?: string | null): string {
  if (!user) return '/login';

  // 1. Kiểm tra đích đến trước đó (returnTo)
  if (returnTo && typeof returnTo === 'string') {
    let cleanPath = returnTo.trim();

    // Nếu là full URL của daotao.dehoc.vn -> lấy pathname
    try {
      if (cleanPath.startsWith('http://') || cleanPath.startsWith('https://')) {
        const parsed = new URL(cleanPath);
        cleanPath = parsed.pathname + parsed.search + parsed.hash;
      }
    } catch {}

    // Bảo vệ không redirect ra ngoài domain và không loop lại trang login
    if (cleanPath.startsWith('/') && !cleanPath.startsWith('//') && !cleanPath.startsWith('/login') && !cleanPath.startsWith('/register')) {
      // Kiểm tra quyền hạn nếu đích đến là trang admin
      if (cleanPath.startsWith('/admin')) {
        if (canAccessAdminPanel(user.role)) {
          return cleanPath;
        }
      } else {
        return cleanPath;
      }
    }
  }

  // 2. Phân luồng mặc định theo Vai trò chức năng & Thuê bao
  const role = user.role;

  // Quản trị viên cấp cao
  if (role === AppRole.SystemAdmin || role === AppRole.TenantAdmin) {
    return '/admin';
  }

  // Quản lý nội dung & Giảng viên
  if (role === AppRole.ContentManager || role === AppRole.Instructor) {
    return '/admin/questions';
  }

  // Trưởng nhóm & Quản lý đơn vị
  if (role === AppRole.TeamLeader || role === AppRole.OrgUnitManager) {
    return '/team';
  }

  // Học viên VIP / Thuê bao nâng cao
  if (user.isPremium || user.subscriptionTier === 'VIP' || user.subscriptionTier === 'ENTERPRISE') {
    return '/practice';
  }

  // Học viên thông thường
  return '/practice';
}
