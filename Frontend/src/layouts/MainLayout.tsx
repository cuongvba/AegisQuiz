/**
 * AegisQuiz — MainLayout (Refactored Composition Shell)
 * ======================================================
 * TRƯỚC: God Component 244 dòng — Header + Nav + Profile + i18n + Auth logic
 * SAU:   ~50 dòng shell — compose widgets độc lập
 *
 * Mọi sub-component đã được tách vào:
 *   widgets/app-header/AppHeader.tsx
 *   widgets/app-header/MobileNav.tsx
 *   widgets/app-header/NavMenu.tsx
 *   widgets/app-header/LanguageSwitcher.tsx
 *   widgets/app-header/TenantBadge.tsx
 *   widgets/app-header/UserProfileDropdown.tsx
 */

import { Outlet } from 'react-router-dom';
import { AppHeader } from '@/widgets/app-header/AppHeader';
import { MobileNav } from '@/widgets/app-header/MobileNav';

export function MainLayout() {
  return (
    <div
      className="min-h-screen selection:bg-[var(--palette-cyan-200)] selection:text-[var(--palette-cyan-900)]"
      style={{ backgroundColor: 'var(--surface-page)', color: 'var(--text-primary)', fontFamily: 'var(--tenant-font-body)' }}
    >
      {/* ── Sticky Header ── */}
      <AppHeader />

      {/* ── Main Content ── */}
      <main
        id="main-content"
        className="w-full relative min-h-[calc(100vh-var(--nav-height))] pb-16 md:pb-0"
        tabIndex={-1}
        aria-label="Nội dung chính"
      >
        <Outlet />
      </main>

      {/* ── Mobile Bottom Nav ── */}
      <MobileNav />
    </div>
  );
}
