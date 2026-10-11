import React from 'react';
import { useTranslation } from 'react-i18next';
import { Atom, ShieldAlert, Cpu, BookOpen, RefreshCw } from 'lucide-react';

interface SymposiumHeaderProps {
  councilsCount: number;
  loading: boolean;
  onRefresh: () => void;
}

export function SymposiumHeader({ councilsCount, loading, onRefresh }: SymposiumHeaderProps) {
  const { t } = useTranslation('symposium');

  return (
    <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 border-b border-slate-800/80 pb-6">
      <div className="space-y-1">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-cyan-500/10 border border-cyan-500/20 flex items-center justify-center text-cyan-400 shadow-lg shadow-cyan-500/10">
            <Atom size={24} className="animate-spin-slow" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl sm:text-2xl font-black text-white tracking-tight">
                {t('header.title')}
              </h1>
              <span className="text-[10px] font-black uppercase px-2 py-0.5 rounded-full bg-cyan-950/80 border border-cyan-500/30 text-cyan-300">
                {t('header.badge')}
              </span>
            </div>
            <p className="text-xs text-slate-400">
              {t('header.subtitle', { count: councilsCount })}
            </p>
          </div>
        </div>
      </div>

      <div className="flex items-center gap-2.5">
        <div className="hidden lg:flex items-center gap-3 text-xs text-slate-400 bg-slate-900/60 px-3 py-1.5 rounded-xl border border-slate-800">
          <div className="flex items-center gap-1.5">
            <ShieldAlert size={13} className="text-amber-400" />
            <span>{t('header.zeroHardcode')}</span>
          </div>
          <span className="text-slate-700">•</span>
          <div className="flex items-center gap-1.5">
            <Cpu size={13} className="text-cyan-400" />
            <span>{t('header.multiModelCot')}</span>
          </div>
        </div>

        <button
          onClick={onRefresh}
          disabled={loading}
          className="p-2 sm:px-3 sm:py-2 rounded-xl bg-slate-900 hover:bg-slate-800 border border-slate-800 hover:border-slate-700 text-xs font-semibold text-slate-300 flex items-center gap-2 transition cursor-pointer"
          title={t('header.refresh', { count: councilsCount })}
        >
          <RefreshCw size={14} className={loading ? 'animate-spin text-cyan-400' : ''} />
          <span className="hidden sm:inline">
            {loading ? t('header.refreshing') : t('header.refresh', { count: councilsCount })}
          </span>
        </button>
      </div>
    </div>
  );
}
