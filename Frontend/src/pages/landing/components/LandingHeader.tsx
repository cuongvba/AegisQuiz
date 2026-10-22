/**
 * AegisQuiz — LandingHeader
 * ===========================
 * Header công khai cho Landing Page — tự động nhận diện phiên người dùng:
 * - Khi chưa đăng nhập: hiển thị nút Đăng nhập / Bắt đầu ngay
 * - Khi đã đăng nhập: hiển thị Avatar + Dropdown hồ sơ, nút Vào Học tập và Khu Quản trị (nếu có quyền Admin)
 * FSD Layer: pages/landing/components/
 */
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Bot, Menu, X, Shield, LayoutDashboard } from 'lucide-react';
import { useTenant } from '@/app/providers/TenantProvider';
import { useAuthContext } from '@/app/providers/AuthProvider';
import { LanguageSwitcher } from '@/widgets/app-header/LanguageSwitcher';
import { UserProfileDropdown } from '@/widgets/app-header/UserProfileDropdown';
import { WorkspaceDropdown } from '@/widgets/app-header/WorkspaceDropdown';

export function LandingHeader() {
  const { t } = useTranslation('landing');
  const navigate = useNavigate();
  const { tenantName } = useTenant();
  const { user, isAuthenticated, canAccessAdmin } = useAuthContext();
  const [isMobileOpen, setIsMobileOpen] = useState(false);

  const navLinks = [
    { label: t('nav.features'),  href: '#features' },
    { label: t('nav.gameshows'), href: '#gameshows' },
    { label: t('nav.pricing'),   href: '#pricing' },
  ];

  return (
    <header className="sticky top-0 z-40 bg-slate-950/80 backdrop-blur-2xl border-b border-slate-800/80">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between gap-4">
        {/* Logo */}
        <button
          onClick={() => navigate('/')}
          className="flex items-center gap-2.5 flex-shrink-0 cursor-pointer"
          aria-label={`${tenantName} — ${t('nav.features')}`}
        >
          <div
            className="w-9 h-9 rounded-full flex items-center justify-center border-2 border-white shadow-md flex-shrink-0"
            style={{ background: 'var(--tenant-brand-gradient)' }}
            aria-hidden="true"
          >
            <Bot className="h-5 w-5 text-white" />
          </div>
          <span
            className="text-lg font-extrabold tracking-tight"
            style={{
              backgroundImage: 'var(--tenant-brand-gradient)',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
              backgroundClip: 'text',
            }}
          >
            {tenantName}
          </span>
        </button>

        {/* Desktop nav */}
        <nav className="hidden md:flex items-center gap-8" aria-label="Landing navigation">
          {navLinks.map((link) => (
            <a
              key={link.href}
              href={link.href}
              className="text-sm font-semibold text-slate-300 hover:text-white transition-colors cursor-pointer"
            >
              {link.label}
            </a>
          ))}
        </nav>

        {/* Right controls */}
        <div className="hidden md:flex items-center gap-3 flex-shrink-0">
          <LanguageSwitcher />

          {isAuthenticated && user ? (
            <>
              <WorkspaceDropdown />
              <UserProfileDropdown />
            </>
          ) : (
            <>
              <button
                onClick={() => navigate('/login')}
                className="px-4 py-2 text-sm font-bold text-slate-300 hover:text-white transition-colors cursor-pointer"
              >
                {t('nav.login')}
              </button>
              <button
                onClick={() => navigate('/register')}
                className="px-4 py-2 text-sm font-bold bg-[var(--brand-gradient)] text-white rounded-full
                           shadow-[var(--tenant-shadow-primary)] transition-all active:scale-95 cursor-pointer hover:brightness-110"
              >
                {t('nav.cta')}
              </button>
            </>
          )}
        </div>

        {/* Mobile toggle */}
        <button
          onClick={() => setIsMobileOpen((v) => !v)}
          className="md:hidden p-2 rounded-lg text-slate-300 hover:bg-slate-800 cursor-pointer"
          aria-label={isMobileOpen ? 'Close menu' : 'Open menu'}
          aria-expanded={isMobileOpen}
        >
          {isMobileOpen ? <X size={22} /> : <Menu size={22} />}
        </button>
      </div>

      {/* Mobile menu */}
      {isMobileOpen && (
        <div className="md:hidden border-t border-slate-800/80 bg-slate-950/95 backdrop-blur-xl px-4 py-4 space-y-3">
          {navLinks.map((link) => (
            <a
              key={link.href}
              href={link.href}
              onClick={() => setIsMobileOpen(false)}
              className="block text-sm font-semibold text-slate-300 hover:text-white transition-colors py-1.5"
            >
              {link.label}
            </a>
          ))}

          <div className="flex items-center justify-between pt-2 border-t border-slate-800/80">
            <LanguageSwitcher />
            {isAuthenticated && user && <UserProfileDropdown />}
          </div>

          {isAuthenticated && user ? (
            <div className="space-y-2 pt-1">
              <button
                onClick={() => { setIsMobileOpen(false); navigate('/dashboard'); }}
                className="w-full px-4 py-2.5 text-xs font-bold bg-[var(--brand-gradient)] text-white rounded-2xl shadow-[var(--tenant-shadow-primary)] flex items-center justify-center gap-2 cursor-pointer"
              >
                <LayoutDashboard size={15} />
                <span>Không gian làm việc (Dashboard)</span>
              </button>
            </div>
          ) : (
            <div className="space-y-2">
              <button
                onClick={() => { setIsMobileOpen(false); navigate('/login'); }}
                className="w-full px-3 py-2 text-sm font-bold text-slate-300 hover:text-white text-center cursor-pointer"
              >
                {t('nav.login')}
              </button>
              <button
                onClick={() => { setIsMobileOpen(false); navigate('/register'); }}
                className="w-full px-4 py-2.5 text-sm font-bold bg-[var(--brand-gradient)] text-white rounded-full
                           shadow-[var(--tenant-shadow-primary)] cursor-pointer"
              >
                {t('nav.cta')}
              </button>
            </div>
          )}
        </div>
      )}
    </header>
  );
}
