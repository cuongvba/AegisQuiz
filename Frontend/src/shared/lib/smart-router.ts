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

  // 2. Mặc định sau khi đăng nhập: dẫn thẳng vào Không gian Bảng điều khiển (Dashboard)
  return '/dashboard';
}
