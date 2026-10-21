import React, { useState } from 'react';
import { Home, Zap, DownloadCloud, User, RefreshCw, Wifi, WifiOff, Sparkles, Globe } from 'lucide-react';
import { DynamicDomainIcon } from '@/components/common/DynamicDomainIcon';
import { useLearnerI18n } from '@/lib/i18n';
import { useMobileApp } from '../context/MobileAppContext';
import { MobileAppSwitcherModal } from './MobileAppSwitcherModal';

interface MobileShellProps {
  children: React.ReactNode;
  activeTab?: 'home' | 'exam' | 'offline' | 'profile';
  onTabChange?: (tab: 'home' | 'exam' | 'offline' | 'profile') => void;
}

export const MobileShell: React.FC<MobileShellProps> = ({
  children,
  activeTab = 'home',
  onTabChange,
}) => {
  const { variant, setIsSwitcherOpen, downloadedCount, triggerHaptic } = useMobileApp();
  const { lang, setLang, t } = useLearnerI18n();
  const [isOnline, setIsOnline] = useState<boolean>(() => (typeof navigator !== 'undefined' ? navigator.onLine : true));

  React.useEffect(() => {
    const handleOnline = () => setIsOnline(true);
    const handleOffline = () => setIsOnline(false);
    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);
    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  const handleTabClick = (tab: 'home' | 'exam' | 'offline' | 'profile') => {
    triggerHaptic(12);
    if (onTabChange) onTabChange(tab);
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col font-sans select-none antialiased">
      {/* ── Native Mobile Header ────────────────────────────────────────── */}
      <header
        className="sticky top-0 z-40 px-4 py-3 bg-slate-950/85 backdrop-blur-xl border-b border-slate-800/80 flex items-center justify-between shadow-lg"
        style={{ paddingTop: 'calc(0.75rem + env(safe-area-inset-top, 0px))' }}
      >
        <button
          type="button"
          onClick={() => {
            triggerHaptic(15);
            setIsSwitcherOpen(true);
          }}
          className="flex items-center gap-2.5 p-1.5 -ml-1 rounded-2xl hover:bg-slate-900 active:scale-95 transition-all cursor-pointer text-left"
        >
          <div
            className="p-2 rounded-xl text-white shadow-md transition-colors"
            style={{ backgroundColor: variant.colorScheme.primary }}
          >
            <DynamicDomainIcon icon={variant.icon} size={18} />
          </div>
          <div>
            <div className="flex items-center gap-1.5">
              <span className="text-xs font-black text-white tracking-wide">{variant.shortName}</span>
              <RefreshCw size={11} className="text-slate-400 opacity-60" />
            </div>
            <span className="text-[10px] text-slate-400 block line-clamp-1">{variant.name.split('—')[0]}</span>
          </div>
        </button>

        <div className="flex items-center gap-2">
          {/* Nút Đổi Ngôn Ngữ Đồng Bộ VI/EN */}
          <button
            type="button"
            onClick={() => {
              triggerHaptic(12);
              setLang(lang === 'vi' ? 'en' : 'vi');
            }}
            className="px-2 py-1 rounded-xl bg-slate-900 border border-slate-800 text-sky-400 hover:text-sky-300 text-xs font-mono font-black flex items-center gap-1 cursor-pointer"
            title="Đổi ngôn ngữ giao diện"
          >
            <Globe size={11} />
            <span>{lang.toUpperCase()}</span>
          </button>

          {/* Badge trạng thái Mạng */}
          <div
            className={`flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold border ${
              isOnline
                ? 'bg-emerald-950/60 border-emerald-500/40 text-emerald-400'
                : 'bg-amber-950/80 border-amber-500/50 text-amber-300 animate-pulse'
            }`}
          >
            {isOnline ? <Wifi size={11} /> : <WifiOff size={11} />}
            <span>{isOnline ? 'Online' : 'Offline'}</span>
          </div>

          {/* Nút Đổi Ngành */}
          <button
            type="button"
            onClick={() => {
              triggerHaptic(15);
              setIsSwitcherOpen(true);
            }}
            className="p-2 rounded-xl bg-slate-900 border border-slate-800 text-purple-400 hover:text-purple-300 cursor-pointer"
            title="Đổi chuyên ngành khác"
          >
            <Sparkles size={16} />
          </button>
        </div>
      </header>

      {/* ── Main Scrollable Body ────────────────────────────────────────── */}
      <main className="flex-1 overflow-y-auto pb-24 px-4 pt-4">
        {children}
      </main>

      {/* ── Native Mobile Bottom Navigation Bar ─────────────────────────── */}
      <nav
        className="fixed bottom-0 left-0 right-0 z-40 bg-slate-950/90 backdrop-blur-2xl border-t border-slate-800/80 px-2 flex items-center justify-around shadow-2xl"
        style={{ paddingBottom: 'calc(0.5rem + env(safe-area-inset-bottom, 12px))' }}
      >
        <button
          type="button"
          onClick={() => handleTabClick('home')}
          className={`flex flex-col items-center py-2 px-3 rounded-xl transition-all cursor-pointer ${
            activeTab === 'home' ? 'text-white' : 'text-slate-500 hover:text-slate-300'
          }`}
        >
          <div
            className="p-1 rounded-lg transition-colors"
            style={{ color: activeTab === 'home' ? variant.colorScheme.primary : undefined }}
          >
            <Home size={20} />
          </div>
          <span className="text-[10px] font-extrabold mt-0.5">{t('mobilePractice')}</span>
        </button>

        <button
          type="button"
          onClick={() => handleTabClick('exam')}
          className={`flex flex-col items-center py-2 px-3 rounded-xl transition-all cursor-pointer ${
            activeTab === 'exam' ? 'text-white' : 'text-slate-500 hover:text-slate-300'
          }`}
        >
          <div
            className="p-1 rounded-lg transition-colors"
            style={{ color: activeTab === 'exam' ? variant.colorScheme.primary : undefined }}
          >
            <Zap size={20} />
          </div>
          <span className="text-[10px] font-extrabold mt-0.5">{t('mobileExam')}</span>
        </button>

        <button
          type="button"
          onClick={() => handleTabClick('offline')}
          className={`flex flex-col items-center py-2 px-3 rounded-xl transition-all cursor-pointer relative ${
            activeTab === 'offline' ? 'text-white' : 'text-slate-500 hover:text-slate-300'
          }`}
        >
          <div
            className="p-1 rounded-lg transition-colors"
            style={{ color: activeTab === 'offline' ? variant.colorScheme.primary : undefined }}
          >
            <DownloadCloud size={20} />
          </div>
          <span className="text-[10px] font-extrabold mt-0.5">{t('mobileOffline')}</span>
          {downloadedCount > 0 && (
            <span
              className="absolute top-1.5 right-2 w-4 h-4 rounded-full text-[9px] font-black flex items-center justify-center text-slate-950"
              style={{ backgroundColor: variant.colorScheme.primary }}
            >
              {downloadedCount}
            </span>
          )}
        </button>

        <button
          type="button"
          onClick={() => handleTabClick('profile')}
          className={`flex flex-col items-center py-2 px-3 rounded-xl transition-all cursor-pointer ${
            activeTab === 'profile' ? 'text-white' : 'text-slate-500 hover:text-slate-300'
          }`}
        >
          <div
            className="p-1 rounded-lg transition-colors"
            style={{ color: activeTab === 'profile' ? variant.colorScheme.primary : undefined }}
          >
            <User size={20} />
          </div>
          <span className="text-[10px] font-extrabold mt-0.5">{t('mobileProfile')}</span>
        </button>
      </nav>

      {/* Modal chuyển ngành */}
      <MobileAppSwitcherModal />
    </div>
  );
};
