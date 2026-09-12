/**
 * AegisQuiz — AppHeader Widget
 * ==============================
 * Header tổng hợp từ các sub-widgets — tách từ MainLayout.tsx.
 * FSD Layer: widgets/app-header/
 */

import { useNavigate } from 'react-router-dom';
import { Bot } from 'lucide-react';
import { NavMenu }              from './NavMenu';
import { LanguageSwitcher }     from './LanguageSwitcher';
import { TenantBadge }          from './TenantBadge';
import { UserProfileDropdown }  from './UserProfileDropdown';
import { useTenant }            from '@/app/providers/TenantProvider';

export function AppHeader() {
  const navigate    = useNavigate();
  const { tenantName } = useTenant();

  return (
    <header
      className="sticky top-0 z-40
                 bg-slate-950/85 backdrop-blur-2xl
                 border-b border-slate-800/80
                 shadow-xl shadow-black/30"
    >
      <div className="max-w-7xl mx-auto px-4 h-[var(--nav-height)] flex items-center justify-between gap-4">

        {/* ── Logo & Brand ── */}
        <div
          className="flex items-center gap-3 cursor-pointer flex-shrink-0"
          onClick={() => navigate('/')}
          role="link"
          tabIndex={0}
          aria-label={`${tenantName} — Trang chủ`}
          onKeyDown={(e) => e.key === 'Enter' && navigate('/')}
        >
          <div
            className="w-10 h-10 rounded-full flex items-center justify-center
                       flex-shrink-0 border-2 border-white shadow-md"
            style={{ background: 'var(--tenant-brand-gradient)' }}
            aria-hidden="true"
          >
            <Bot className="h-6 w-6 text-white" />
          </div>
          <span
            className="text-xl font-extrabold tracking-tight"
            style={{
              backgroundImage: 'var(--tenant-brand-gradient)',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
              backgroundClip: 'text',
            }}
          >
            {tenantName}
          </span>
        </div>

        {/* ── Desktop Nav ── */}
        <NavMenu className="hidden md:flex items-center gap-2 flex-1 justify-center" />

        {/* ── Right Controls ── */}
        <div className="flex items-center gap-3 flex-shrink-0">
          <LanguageSwitcher />
          <TenantBadge />
          <UserProfileDropdown />
        </div>

      </div>
    </header>
  );
}
