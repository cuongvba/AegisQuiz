/**
 * AegisQuiz — UserProfileDropdown Widget
 * ========================================
 * Avatar + dropdown menu — tách từ MainLayout.tsx.
 * Dùng useAuthContext thay vì localStorage trực tiếp.
 * FSD Layer: widgets/app-header/
 */

import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Settings, LogOut, Shield, Users, BookOpen } from 'lucide-react';
import { useAuthContext } from '@/app/providers/AuthProvider';

export function UserProfileDropdown() {
  const { user, logout } = useAuthContext();
  const { t } = useTranslation();
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
          className="px-4 py-2 text-xs font-bold text-slate-300
                     hover:bg-slate-800 hover:text-white
                     rounded-full transition-all cursor-pointer"
        >
          {t('auth.login', 'Đăng nhập')}
        </button>
        <button
          onClick={() => navigate('/register')}
          className="px-4 py-2 text-xs font-bold
                     bg-[var(--brand-gradient)] text-white
                     rounded-full transition-all
                     shadow-[var(--tenant-shadow-primary)]
                     active:scale-95 cursor-pointer hover:brightness-110"
        >
          {t('auth.register', 'Đăng ký')}
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
        aria-label={t('userMenu.accountOf', { name: user.name ?? t('userMenu.defaultName') })}
        className="flex items-center gap-2
                   bg-slate-900/80 hover:bg-slate-800 text-slate-200 hover:text-white
                   p-1 pe-3 rounded-full border border-slate-700/80 hover:border-slate-600
                   transition-all font-bold text-sm cursor-pointer shadow-sm select-none"
      >
        <div
          className="w-8 h-8 rounded-full flex items-center justify-center
                     text-white shadow-inner uppercase text-xs font-black ring-2 ring-cyan-500/30"
          style={{ background: 'var(--brand-gradient-vip)' }}
          aria-hidden="true"
        >
          {initials}
        </div>
        <span className="hidden sm:inline-block text-slate-200 truncate max-w-[110px]">
          {user.name ?? t('userMenu.defaultName')}
        </span>
      </button>

      {/* Dropdown */}
      {isOpen && (
        <div
          role="menu"
          aria-label={t('userMenu.menuLabel')}
          className="absolute end-0 mt-2.5 w-64
                     bg-slate-900/95 backdrop-blur-xl rounded-2xl
                     shadow-2xl border border-slate-700/80 p-2 z-[var(--z-dropdown,50)]
                     animate-scale-in-tl origin-top-right ring-1 ring-white/10"
        >
          {/* User info */}
          <div className="px-3 py-2.5 border-b border-slate-800/80 mb-1.5">
            <p className="font-bold text-sm truncate text-white">{user.name}</p>
            <p className="text-xs text-slate-400 truncate">{user.email}</p>
            <div className="mt-1.5 flex items-center gap-1.5 flex-wrap">
              <span className="px-2 py-0.5 text-[10px] font-bold rounded-full bg-cyan-500/10 text-cyan-400 border border-cyan-500/20 uppercase tracking-wide">
                {user.role}
              </span>
              {user.isPremium && (
                <span className="px-2 py-0.5 text-[10px] font-bold rounded-full bg-amber-500/10 text-amber-400 border border-amber-500/20">
                  PRO VIP
                </span>
              )}
            </div>
          </div>

          {/* Role-based quick portals */}
          <div className="space-y-1">
            <button
              role="menuitem"
              onClick={() => { setIsOpen(false); navigate('/dashboard'); }}
              className="w-full text-start px-3 py-2 flex items-center gap-2.5
                         text-sm font-medium text-cyan-400
                         hover:bg-cyan-500/10 rounded-xl transition-colors cursor-pointer"
            >
              <BookOpen className="w-4 h-4 text-cyan-400" aria-hidden="true" />
              {t('userMenu.learningDashboard', 'Khu vực Học tập')}
            </button>

            {(user.role === 'SystemAdmin' || user.role === 'TenantAdmin') && (
              <button
                role="menuitem"
                onClick={() => { setIsOpen(false); navigate('/admin'); }}
                className="w-full text-start px-3 py-2 flex items-center gap-2.5
                           text-sm font-medium text-purple-400
                           hover:bg-purple-500/10 rounded-xl transition-colors cursor-pointer"
              >
                <Shield className="w-4 h-4 text-purple-400" aria-hidden="true" />
                {t('userMenu.systemAdminPortal')}
              </button>
            )}

            {user.role === 'TeamLeader' && (
              <button
                role="menuitem"
                onClick={() => { setIsOpen(false); navigate('/team'); }}
                className="w-full text-start px-3 py-2 flex items-center gap-2.5
                           text-sm font-medium text-amber-400
                           hover:bg-amber-500/10 rounded-xl transition-colors cursor-pointer"
              >
                <Users className="w-4 h-4 text-amber-400" aria-hidden="true" />
                {t('userMenu.teamLeaderArea')}
              </button>
            )}

            {/* Profile */}
            <button
              role="menuitem"
              onClick={() => { setIsOpen(false); navigate('/profile'); }}
              className="w-full text-start px-3 py-2 flex items-center gap-2.5
                         text-sm font-medium text-slate-300
                         hover:bg-slate-800 hover:text-white rounded-xl transition-colors cursor-pointer"
            >
              <Settings className="w-4 h-4 text-slate-400" aria-hidden="true" />
              {t('userMenu.profileAndSecurity')}
            </button>
          </div>

          <div className="my-1.5 border-t border-slate-800/80" role="separator" />

          {/* Logout */}
          <button
            role="menuitem"
            onClick={() => { setIsOpen(false); logout(); }}
            className="w-full text-start px-3 py-2 flex items-center gap-2.5
                       text-sm font-bold text-rose-400
                       hover:bg-rose-500/10 rounded-xl transition-colors cursor-pointer"
          >
            <LogOut className="w-4 h-4 text-rose-400" aria-hidden="true" />
            {t('auth.logout', 'Đăng xuất')}
          </button>
        </div>
      )}
    </div>
  );
}
