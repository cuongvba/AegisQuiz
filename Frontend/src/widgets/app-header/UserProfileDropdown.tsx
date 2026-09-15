/**
 * AegisQuiz — UserProfileDropdown Widget
 * ========================================
 * Avatar + dropdown menu — tách từ MainLayout.tsx.
 * Dùng useAuthContext thay vì localStorage trực tiếp.
 * FSD Layer: widgets/app-header/
 */

import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Settings, LogOut, Shield, Users } from 'lucide-react';
import { useAuthContext } from '@/app/providers/AuthProvider';
import { useLearnerI18n } from '@/lib/i18n';

export function UserProfileDropdown() {
  const { user, logout } = useAuthContext();
  const { t } = useLearnerI18n();
  const navigate = useNavigate();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Close on outside click
  useEffect(() => {
    if (!isOpen) return;
    const handler = (e: MouseEvent) => {
      if (!dropdownRef.current?.contains(e.target as Node)) setIsOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [isOpen]);

  // Close on Escape
  useEffect(() => {
    if (!isOpen) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setIsOpen(false);
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [isOpen]);

  if (!user) {
    return (
      <div className="flex items-center gap-2">
        <button
          onClick={() => navigate('/login')}
          className="px-4 py-2 text-xs font-bold text-[var(--text-secondary)]
                     hover:bg-[var(--surface-secondary)] hover:text-[var(--text-primary)]
                     rounded-full transition-all cursor-pointer"
        >
          Đăng nhập
        </button>
        <button
          onClick={() => navigate('/register')}
          className="px-4 py-2 text-xs font-bold
                     bg-[var(--brand-gradient)] text-white
                     rounded-full transition-all
                     shadow-[var(--tenant-shadow-primary)]
                     active:scale-95 cursor-pointer hover:brightness-110"
        >
          Đăng ký
        </button>
      </div>
    );
  }

  const initials = user.name?.[0]?.toUpperCase() ?? '?';

  return (
    <div className="relative" ref={dropdownRef}>
      {/* Avatar button */}
      <button
        onClick={() => setIsOpen((v) => !v)}
        aria-expanded={isOpen}
        aria-haspopup="menu"
        aria-label={`Tài khoản của ${user.name ?? 'bạn'}`}
        className="flex items-center gap-2
                   bg-[var(--surface-secondary)] hover:bg-[var(--surface-tertiary)]
                   p-1 pe-3 rounded-full border border-[var(--border-default)]
                   transition-all font-bold text-sm cursor-pointer"
      >
        <div
          className="w-8 h-8 rounded-full flex items-center justify-center
                     text-white shadow-inner uppercase text-sm font-bold"
          style={{ background: 'var(--brand-gradient-vip)' }}
          aria-hidden="true"
        >
          {initials}
        </div>
        <span className="hidden sm:inline-block text-[var(--text-primary)] truncate max-w-[100px]">
          {user.name ?? 'User'}
        </span>
      </button>

      {/* Dropdown */}
      {isOpen && (
        <div
          role="menu"
          aria-label="Menu tài khoản"
          className="absolute end-0 mt-3 w-56
                     bg-[var(--surface-elevated)] rounded-2xl
                     shadow-xl border border-[var(--border-subtle)] p-2 z-[var(--z-dropdown)]
                     animate-scale-in-tl origin-top-right"
        >
          {/* User info */}
          <div className="px-3 py-2 border-b border-[var(--border-subtle)] mb-2">
            <p className="font-bold text-sm truncate text-[var(--text-primary)]">{user.name}</p>
            <p className="text-xs text-[var(--text-muted)] truncate">{user.email}</p>
          </div>

          {/* Role-based quick portals */}
          {(user.role === 'SystemAdmin' || user.role === 'TenantAdmin') && (
            <button
              role="menuitem"
              onClick={() => { setIsOpen(false); navigate('/admin'); }}
              className="w-full text-start px-3 py-2 flex items-center gap-2
                         text-sm font-medium text-purple-400
                         hover:bg-purple-500/10 rounded-xl transition-colors cursor-pointer"
            >
              <Shield className="w-4 h-4 text-purple-400" aria-hidden="true" />
              Quản trị Hệ thống
            </button>
          )}

          {user.role === 'TeamLeader' && (
            <button
              role="menuitem"
              onClick={() => { setIsOpen(false); navigate('/team'); }}
              className="w-full text-start px-3 py-2 flex items-center gap-2
                         text-sm font-medium text-cyan-400
                         hover:bg-cyan-500/10 rounded-xl transition-colors cursor-pointer"
            >
              <Users className="w-4 h-4 text-cyan-400" aria-hidden="true" />
              Khu vực Trưởng nhóm
            </button>
          )}

          {/* Profile */}
          <button
            role="menuitem"
            onClick={() => { setIsOpen(false); navigate('/profile'); }}
            className="w-full text-start px-3 py-2 flex items-center gap-2
                       text-sm font-medium text-[var(--text-secondary)]
                       hover:bg-[var(--surface-secondary)] rounded-xl transition-colors cursor-pointer"
          >
            <Settings className="w-4 h-4" aria-hidden="true" />
            Hồ sơ cá nhân & Bảo mật
          </button>

          <div className="my-1 border-t border-[var(--border-subtle)]" role="separator" />

          {/* Logout */}
          <button
            role="menuitem"
            onClick={() => { setIsOpen(false); logout(); }}
            className="w-full text-start px-3 py-2 flex items-center gap-2
                       text-sm font-bold text-[var(--text-danger)]
                       hover:bg-[var(--state-danger-bg)] rounded-xl transition-colors cursor-pointer"
          >
            <LogOut className="w-4 h-4" aria-hidden="true" />
            Đăng xuất
          </button>
        </div>
      )}
    </div>
  );
}
