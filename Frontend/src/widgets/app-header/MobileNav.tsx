/**
 * AegisQuiz — MobileNav Widget
 * ==============================
 * Bottom navigation bar cho mobile — tách từ MainLayout.tsx.
 * FSD Layer: widgets/app-header/
 */

import { useNavigate, useLocation } from 'react-router-dom';
import { useNavItems } from './NavMenu';

export function MobileNav() {
  const navigate  = useNavigate();
  const location  = useLocation();
  const navItems  = useNavItems();

  return (
    <nav
      className="fixed md:hidden bottom-0 inset-inline-0 h-16
                 bg-[var(--nav-bg)] backdrop-blur
                 border-t border-[var(--nav-border)] z-[var(--z-sticky)]
                 flex items-center justify-around px-4"
      style={{ paddingBottom: 'env(safe-area-inset-bottom)' }}
      aria-label="Menu điều hướng dưới"
    >
      {navItems.map((item) => {
        const isActive = location.pathname === item.path;
        return (
          <button
            key={item.path}
            onClick={() => navigate(item.path)}
            aria-current={isActive ? 'page' : undefined}
            aria-label={item.name}
            className={[
              'flex flex-col items-center justify-center w-16 h-full gap-1 transition-colors cursor-pointer',
              isActive
                ? 'text-[var(--nav-item-active-text)]'
                : 'text-[var(--text-muted)] hover:text-[var(--text-secondary)]',
            ].join(' ')}
          >
            {item.icon}
            <span className="text-[10px] font-bold leading-none">{item.name}</span>
          </button>
        );
      })}
    </nav>
  );
}
