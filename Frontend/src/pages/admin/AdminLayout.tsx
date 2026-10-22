import { useState } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  Bot,
  Menu,
  X,
  Shield,
  Sparkles,
  GraduationCap
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useAuthContext } from '@/app/providers/AuthProvider';
import { useEntitlement } from '@/shared/hooks/useEntitlement';
import { FeatureBadge } from '@/shared/components/entitlement';
import { LanguageSwitcher } from '@/widgets/app-header/LanguageSwitcher';
import { UserProfileDropdown } from '@/widgets/app-header/UserProfileDropdown';
import { AegisCoPilotSidecar, CoPilotTriggerOrb } from '@/features/copilot';
import { ADMIN_NAV_ITEMS } from './adminNavConfig';

export function AdminLayout() {
  const { t, i18n } = useTranslation('admin');
  const { user } = useAuthContext();
  const { plan, hasFeature, openPaywall } = useEntitlement();
  const isTeamLeader = user?.role === 'TeamLeader';
  const location = useLocation();
  const navigate = useNavigate();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isCoPilotOpen, setIsCoPilotOpen] = useState(false);

  const isRtl = i18n.dir() === 'rtl' || i18n.language === 'ar';

  const menuItems = ADMIN_NAV_ITEMS
    .filter(item => !isTeamLeader || !item.adminOnly)
    .map(item => ({
      ...item,
      label: t(item.i18nKey, item.defaultLabel),
    }));

  const currentItem = menuItems.find(item => item.href === location.pathname) 
    || menuItems.find(item => location.pathname.startsWith(item.href) && item.href !== '/admin');

  return (
    <div className={`flex min-h-screen bg-slate-950 text-white font-sans ${isRtl ? 'rtl' : 'ltr'}`} dir={isRtl ? 'rtl' : 'ltr'}>
      {/* Mobile Drawer Backdrop */}
      {isSidebarOpen && (
        <div 
          className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 md:hidden"
          onClick={() => setIsSidebarOpen(false)}
        />
      )}

      {/* SIDEBAR */}
      <aside className={`
        fixed inset-y-0 ${isRtl ? 'right-0 border-l' : 'left-0 border-r'} w-64 bg-slate-900 border-slate-800 p-6 flex flex-col z-50 transition-transform duration-300 transform
        md:translate-x-0 ${isSidebarOpen ? 'translate-x-0' : (isRtl ? 'translate-x-full' : '-translate-x-full')}
      `}>
        {/* Close Button (Mobile Only) */}
        <button 
          onClick={() => setIsSidebarOpen(false)}
          className={`md:hidden absolute top-4 ${isRtl ? 'left-4' : 'right-4'} p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-xl transition-colors cursor-pointer`}
          aria-label={t('nav.closeNav', 'Đóng thanh điều hướng')}
        >
          <X size={18} />
        </button>

        {/* Brand / Logo */}
        <div 
          className="mb-8 flex items-center gap-3 cursor-pointer" 
          onClick={() => { navigate('/admin'); if (window.innerWidth < 768) setIsSidebarOpen(false); }}
        >
          <div className="w-10 h-10 rounded-full bg-gradient-to-tr from-cyan-400 to-blue-500 shadow-md flex items-center justify-center shrink-0 border-2 border-white">
            <Bot className="h-6 w-6 text-white" />
          </div>
          <div>
            <h1 className="text-lg font-black bg-gradient-to-r from-cyan-400 to-blue-500 bg-clip-text text-transparent">
              Aegis Global
            </h1>
            <div className="flex items-center gap-1.5 mt-0.5">
              <span className="text-[9px] font-extrabold uppercase px-1.5 py-0.5 rounded bg-indigo-500/20 text-indigo-300 border border-indigo-400/30">
                {plan}
              </span>
              <span className="text-[10px] text-gray-500 font-bold uppercase tracking-wider">
                {isTeamLeader ? t('header.teamLeader', 'Leader') : t('header.admin', 'Portal')}
              </span>
            </div>
          </div>
        </div>

        {/* Navigation Links */}
        <nav className="flex-1 space-y-1 overflow-y-auto pr-1">
          {menuItems.map((item) => {
            const isActive = location.pathname === item.href;
            const isLocked = item.feature ? !hasFeature(item.feature) : false;

            return (
              <Link
                key={item.href}
                to={isLocked ? '#' : item.href}
                onClick={(e) => {
                  if (isLocked && item.feature) {
                    e.preventDefault();
                    openPaywall(item.feature);
                    return;
                  }
                  if (window.innerWidth < 768) setIsSidebarOpen(false);
                }}
                className={`flex items-center justify-between px-3.5 py-2.5 rounded-xl text-xs font-bold transition-all group
                  ${isActive 
                    ? 'bg-blue-600/20 text-blue-400 border border-blue-500/20 shadow-sm' 
                    : isLocked
                    ? 'text-gray-500 hover:text-gray-300 hover:bg-slate-800/60 opacity-75'
                    : 'text-gray-400 hover:bg-slate-800 hover:text-white'
                  }`}
              >
                <div className="flex items-center gap-2.5">
                  <span className={isLocked ? 'text-gray-500' : ''}>{item.icon}</span>
                  <span>{item.label}</span>
                </div>
                {isLocked && item.feature && (
                  <FeatureBadge feature={item.feature} size="sm" />
                )}
              </Link>
            );
          })}
        </nav>

        {/* Footer Navigation */}
        <div className="border-t border-slate-800/80 pt-4 mt-4 space-y-2">
          <Link 
            to="/dashboard" 
            onClick={() => { if (window.innerWidth < 768) setIsSidebarOpen(false); }} 
            className="text-xs text-cyan-400 hover:text-cyan-300 transition-colors flex items-center gap-2 font-bold px-2 py-1.5 rounded-lg hover:bg-slate-800"
          >
            <GraduationCap size={15} /> {t('nav.learningArea', 'Khu vực Học tập')}
          </Link>
          <Link 
            to="/" 
            onClick={() => { if (window.innerWidth < 768) setIsSidebarOpen(false); }} 
            className="text-xs text-gray-400 hover:text-gray-200 transition-colors flex items-center gap-2 font-semibold px-2 py-1 rounded-lg hover:bg-slate-800"
          >
            <ArrowLeft size={13} className={isRtl ? 'rotate-180' : ''} /> {t('nav.homepage', 'Trang chủ')}
          </Link>
        </div>
      </aside>

      {/* MAIN VIEWPORT */}
      <div className={`flex-1 flex flex-col ${isRtl ? 'md:mr-64' : 'md:ml-64'} min-h-screen transition-all`}>
        {/* Sticky Top Header */}
        <header className="sticky top-0 z-40 flex items-center justify-between bg-slate-900/95 backdrop-blur-xl border-b border-slate-800/80 px-4 md:px-8 py-3.5 transition-all shadow-sm">
          {/* Left: Mobile hamburger + Active module title */}
          <div className="flex items-center gap-3">
            <button 
              onClick={() => setIsSidebarOpen(true)}
              className="md:hidden p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-xl transition-colors cursor-pointer"
              aria-label={t('nav.openNav', 'Mở danh mục quản trị')}
            >
              <Menu size={20} />
            </button>
            <div className="flex items-center gap-2">
              <span className="hidden sm:inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-extrabold bg-blue-500/10 border border-blue-500/20 text-blue-400">
                <Shield size={12} />
                <span>{isTeamLeader ? t('header.teamLeader', 'Team Leader') : t('header.admin', 'Admin')}</span>
              </span>
              <span className="text-sm font-bold text-slate-200 truncate">
                {currentItem?.label || t('header.systemAdmin', 'Quản trị hệ thống')}
              </span>
            </div>
          </div>

          {/* Right: TeamLeader info + LanguageSwitcher + UserProfileDropdown */}
          <div className="flex items-center gap-3">
            {isTeamLeader && (
              <span className="hidden lg:inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-amber-950/60 border border-amber-500/40 text-amber-300">
                <Sparkles size={13} className="text-amber-400" />
                <span>{user?.orgUnitName || t('header.internalUnit', 'Đơn vị nội bộ')}</span>
              </span>
            )}
            <LanguageSwitcher />
            <UserProfileDropdown />
          </div>
        </header>

        {/* Page Content */}
        <main className="flex-1 p-4 md:p-8 overflow-y-auto">
          <div className="w-full max-w-7xl mx-auto">
            <Outlet />
          </div>
        </main>
      </div>

      {/* Aegis Sovereign Co-Pilot Sidecar & Trigger Orb */}
      <CoPilotTriggerOrb
        isOpen={isCoPilotOpen}
        onToggle={() => setIsCoPilotOpen(!isCoPilotOpen)}
      />
      <AegisCoPilotSidecar
        isOpen={isCoPilotOpen}
        onClose={() => setIsCoPilotOpen(false)}
        currentScreen={`Admin: ${currentItem?.label || t('header.systemAdmin', 'Quản trị')}`}
      />
    </div>
  );
}

export default AdminLayout;
