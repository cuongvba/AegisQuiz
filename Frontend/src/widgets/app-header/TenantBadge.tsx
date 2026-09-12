/**
 * AegisQuiz — TenantBadge Widget
 * ================================
 * Hiển thị trạng thái VIP / Admin portal / Upgrade button.
 * Tách từ MainLayout.tsx — dùng useAuthContext thay vì localStorage.
 * FSD Layer: widgets/app-header/
 */

import { useNavigate } from 'react-router-dom';
import { useAuthContext } from '@/app/providers/AuthProvider';

export function TenantBadge() {
  const navigate = useNavigate();
  const { user, isAdmin } = useAuthContext();

  if (!user) return null;

  if (isAdmin) {
    return (
      <button
        onClick={() => navigate('/admin')}
        aria-label="Vào trang Quản trị"
        className="hidden sm:flex items-center gap-1.5
                   bg-[var(--brand-gradient)] text-[var(--action-primary-text)]
                   text-xs font-bold px-4 py-2 rounded-[var(--tenant-radius-button)]
                   shadow-[var(--tenant-shadow-primary)]
                   transition-transform active:scale-95 cursor-pointer
                   hover:brightness-110"
      >
        Quản lý 📊
      </button>
    );
  }

  if (user.role === 'TeamLeader') {
    return (
      <button
        onClick={() => navigate('/admin/questions')}
        aria-label="Vào Ngân hàng Câu hỏi & Đề thi Cấp Nhóm"
        className="hidden sm:flex items-center gap-1.5
                   bg-gradient-to-r from-amber-500 to-orange-500 text-slate-950
                   text-xs font-black px-3.5 py-2 rounded-[var(--tenant-radius-button)]
                   shadow-md shadow-amber-500/20
                   transition-transform active:scale-95 cursor-pointer
                   hover:brightness-110"
      >
        📝 Đề thi Nhóm
      </button>
    );
  }

  if (user.isPremium) {
    return (
      <span
        role="status"
        aria-label="Thành viên VIP"
        className="hidden sm:inline-block text-xs
                   bg-[var(--brand-accent-light)] text-[var(--brand-accent)]
                   px-3.5 py-1.5 rounded-full border border-[var(--palette-amber-300)]
                   font-bold uppercase tracking-wider"
      >
        VIP 💎
      </span>
    );
  }

  return (
    <button
      onClick={() => navigate('/paywall')}
      aria-label="Nâng cấp lên VIP"
      className="hidden sm:flex items-center gap-1.5
                 bg-[var(--brand-gradient-vip)] text-[var(--action-primary-text)]
                 text-xs font-bold px-4 py-2 rounded-[var(--tenant-radius-button)]
                 shadow-md transition-transform active:scale-95 cursor-pointer
                 hover:brightness-110"
    >
      Nâng cấp VIP 💎
    </button>
  );
}
