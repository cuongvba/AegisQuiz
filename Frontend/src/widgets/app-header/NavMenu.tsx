/**
 * AegisQuiz — NavMenu Widget
 * ===========================
 * Desktop navigation menu — tách từ MainLayout.tsx.
 * FSD Layer: widgets/app-header/
 */

import { useNavigate, useLocation } from 'react-router-dom';
import { BookOpen, Trophy, Shield, Gamepad2, Target, Zap, Users } from 'lucide-react';
import { useLearnerI18n } from '@/lib/i18n';

interface NavItem {
  name:  string;
  path:  string;
  icon:  React.ReactNode;
}

interface NavMenuProps {
  className?: string;
}

export function useNavItems(): NavItem[] {
  const { t } = useLearnerI18n();
  return [
    { name: t('Dashboard'),     path: '/',                    icon: <BookOpen  className="w-5 h-5" aria-hidden="true" /> },
    { name: 'Đội nhóm 👥',     path: '/team',                icon: <Users     className="w-5 h-5 text-emerald-500" aria-hidden="true" /> },
    { name: 'Đấu trường ⚡',  path: '/arena',               icon: <Zap       className="w-5 h-5 text-amber-500" aria-hidden="true" /> },
    { name: 'Luyện thi',        path: '/practice',            icon: <Gamepad2  className="w-5 h-5" aria-hidden="true" /> },
    { name: 'Thi thử',          path: '/quiz/attempt/mock-123', icon: <Target  className="w-5 h-5" aria-hidden="true" /> },
    { name: t('leaderboard'),   path: '/leaderboard',         icon: <Trophy    className="w-5 h-5" aria-hidden="true" /> },
    { name: t('viewAllTrophies'), path: '/achievements',      icon: <Shield    className="w-5 h-5" aria-hidden="true" /> },
  ];
}

export function NavMenu({ className }: NavMenuProps) {
  const navigate  = useNavigate();
  const location  = useLocation();
  const navItems  = useNavItems();

  return (
    <nav
      className={className}
      aria-label="Menu chính"
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
              'px-4 py-2 rounded-full font-bold flex items-center gap-2 transition-all',
              isActive
                ? 'bg-[var(--nav-item-active-bg)] text-[var(--nav-item-active-text)] shadow-sm'
                : 'text-[var(--nav-item-text)] hover:bg-[var(--nav-item-hover-bg)] hover:text-[var(--nav-item-hover-text)]',
            ].join(' ')}
          >
            {item.icon}
            {item.name}
          </button>
        );
      })}
    </nav>
  );
}
