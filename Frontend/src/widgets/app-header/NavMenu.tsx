/**
 * AegisQuiz — NavMenu Widget
 * ===========================
 * Desktop navigation menu — tách từ MainLayout.tsx.
 * FSD Layer: widgets/app-header/
 */

import { useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { BookOpen, Trophy, Shield, Gamepad2, Target, Zap, Users } from 'lucide-react';

interface NavItem {
  name:  string;
  path:  string;
  icon:  React.ReactNode;
}

interface NavMenuProps {
  className?: string;
}

export function useNavItems(): NavItem[] {
  const { t } = useTranslation();
  return [
    { name: t('nav.dashboard'),    path: '/dashboard',             icon: <BookOpen  className="w-4 h-4" aria-hidden="true" /> },
    { name: t('nav.team'),        path: '/team',                   icon: <Users     className="w-4 h-4 text-emerald-500" aria-hidden="true" /> },
    { name: t('nav.arena'),       path: '/arena',                  icon: <Zap       className="w-4 h-4 text-amber-500" aria-hidden="true" /> },
    { name: t('nav.practice'),    path: '/practice',                icon: <Gamepad2  className="w-4 h-4 text-cyan-500" aria-hidden="true" /> },
    { name: t('nav.mockExam'),    path: '/quiz/attempt/mock-123',   icon: <Target    className="w-4 h-4 text-rose-500" aria-hidden="true" /> },
    { name: t('nav.leaderboard'), path: '/leaderboard',             icon: <Trophy    className="w-4 h-4 text-amber-400" aria-hidden="true" /> },
    { name: t('nav.achievements'),path: '/achievements',            icon: <Shield    className="w-4 h-4 text-purple-500" aria-hidden="true" /> },
  ];
}

export function NavMenu({ className }: NavMenuProps) {
  const { t }     = useTranslation();
  const navigate  = useNavigate();
  const location  = useLocation();
  const navItems  = useNavItems();

  return (
    <nav
      className={className}
      aria-label={t('nav.mainMenu')}
    >
      <div className="flex items-center gap-1 p-1 bg-slate-900/60 backdrop-blur-xl rounded-2xl border border-slate-800/80 shadow-inner flex-shrink-0">
        {navItems.map((item) => {
          const isActive = location.pathname === item.path;
          return (
            <button
              key={item.path}
              onClick={() => navigate(item.path)}
              aria-current={isActive ? 'page' : undefined}
              aria-label={item.name}
              className={`px-2.5 py-1.5 rounded-xl font-bold flex items-center gap-1.5 transition-all text-xs whitespace-nowrap cursor-pointer
                ${isActive
                  ? 'bg-gradient-to-r from-cyan-500 to-indigo-600 text-white shadow-md shadow-cyan-500/25'
                  : 'text-slate-300 hover:text-white hover:bg-slate-800/60'
                }`}
            >
              {item.icon}
              <span>{item.name}</span>
            </button>
          );
        })}
      </div>
    </nav>
  );
}
