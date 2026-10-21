import React, { useState } from 'react';
import { User, ShieldCheck, Sparkles, Monitor, Smartphone, Moon, Bell, LogOut } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useLearnerI18n } from '@/lib/i18n';
import { useMobileApp } from '../context/MobileAppContext';

export const MobileProfileView: React.FC = () => {
  const { variant, setIsSwitcherOpen, triggerHaptic } = useMobileApp();
  const { t } = useLearnerI18n();
  const navigate = useNavigate();
  const [hapticEnabled, setHapticEnabled] = useState<boolean>(true);
  const [autoNext, setAutoNext] = useState<boolean>(true);

  return (
    <div className="space-y-4">
      {/* ── User Card ──────────────────────────────────────────────────── */}
      <div className="p-4 rounded-3xl bg-slate-900/80 border border-slate-800 flex items-center gap-3.5">
        <div
          className="w-12 h-12 rounded-2xl flex items-center justify-center text-slate-950 font-black text-lg shadow-lg shrink-0"
          style={{ backgroundColor: variant.colorScheme.primary }}
        >
          AQ
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-1.5">
            <h2 className="text-sm font-black text-white truncate">{t('proLearner')}</h2>
            <ShieldCheck size={14} className="text-sky-400 shrink-0" />
          </div>
          <p className="text-[11px] text-slate-400">{t('proMembership')}</p>
        </div>
      </div>

      {/* ── Cài Đặt Cá Thể Hóa ─────────────────────────────────────────── */}
      <div className="space-y-2">
        <h3 className="text-xs font-black text-slate-400 uppercase tracking-wider">{t('customizationSettings')}</h3>
        <div className="rounded-2xl bg-slate-900/60 border border-slate-800 divide-y divide-slate-800/80 text-xs font-bold text-slate-200">
          <div className="p-3.5 flex items-center justify-between">
            <div className="flex items-center gap-2.5">
              <Smartphone size={16} className="text-purple-400" />
              <span>{t('hapticFeedback')}</span>
            </div>
            <input
              type="checkbox"
              checked={hapticEnabled}
              onChange={(e) => {
                triggerHaptic(15);
                setHapticEnabled(e.target.checked);
              }}
              className="accent-purple-500 w-4 h-4 cursor-pointer"
            />
          </div>

          <div className="p-3.5 flex items-center justify-between">
            <div className="flex items-center gap-2.5">
              <Sparkles size={16} className="text-amber-400" />
              <span>{t('autoNextOption')}</span>
            </div>
            <input
              type="checkbox"
              checked={autoNext}
              onChange={(e) => {
                triggerHaptic(15);
                setAutoNext(e.target.checked);
              }}
              className="accent-purple-500 w-4 h-4 cursor-pointer"
            />
          </div>

          <button
            type="button"
            onClick={() => {
              triggerHaptic(15);
              setIsSwitcherOpen(true);
            }}
            className="w-full p-3.5 flex items-center justify-between cursor-pointer hover:bg-slate-850 transition-colors text-left"
          >
            <div className="flex items-center gap-2.5">
              <Sparkles size={16} style={{ color: variant.colorScheme.primary }} />
              <span>{t('switchDomain')} ({variant.shortName})</span>
            </div>
            <span className="text-[11px] text-slate-400">➔</span>
          </button>
        </div>
      </div>

      {/* ── Chuyển Đổi Nền Tảng (Switch to Desktop Web Studio) ──────────── */}
      <div className="space-y-2 pt-2">
        <button
          type="button"
          onClick={() => {
            triggerHaptic(20);
            navigate('/practice');
          }}
          className="w-full p-3.5 rounded-2xl bg-slate-900 border border-slate-800 hover:border-slate-700 flex items-center justify-center gap-2 text-xs font-bold text-sky-400 cursor-pointer active:scale-98 transition-all"
        >
          <Monitor size={15} />
          <span>{t('switchToWeb')}</span>
        </button>
      </div>
    </div>
  );
};
