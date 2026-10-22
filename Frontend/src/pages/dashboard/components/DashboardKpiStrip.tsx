import { useTranslation } from 'react-i18next';
import { Flame, TrendingUp, Flag, Trophy, CheckCircle2, Sparkles, ChevronRight } from 'lucide-react';

export function DashboardKpiStrip() {
  const { t } = useTranslation('dashboard');

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      {/* ── Card 1: Streak Rèn Luyện Rực Lửa 🔥 ── */}
      <div className="rounded-2xl p-4 sm:p-5 bg-gradient-to-br from-amber-950/40 via-slate-900/80 to-slate-900/90 border border-amber-500/30 shadow-xl backdrop-blur-md relative overflow-hidden group hover:border-amber-400/60 transition-all">
        <div className="flex items-center justify-between mb-3">
          <span className="text-xs font-black text-amber-300 tracking-wide uppercase flex items-center gap-1.5">
            <Flame size={15} className="text-amber-400 animate-bounce" /> {t('metrics.streak.label')}
          </span>
          <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-amber-500/20 text-amber-300 border border-amber-500/30">
            {t('metrics.streak.badge')}
          </span>
        </div>
        <div className="flex items-baseline gap-1.5">
          <span className="text-3xl font-black text-white">{t('metrics.streak.value')}</span>
          <span className="text-xs text-slate-400 font-semibold">{t('metrics.streak.unit')}</span>
        </div>
        <div className="mt-3 w-full bg-slate-800 rounded-full h-2 overflow-hidden">
          <div 
            className="bg-gradient-to-r from-amber-500 to-orange-500 h-full rounded-full transition-all duration-700 shadow-sm shadow-amber-500/50" 
            style={{ width: '71%' }} 
          />
        </div>
        <p className="text-[11px] text-slate-400 mt-2 flex items-center gap-1">
          <CheckCircle2 size={12} className="text-emerald-400" /> {t('metrics.streak.footer')}
        </p>
      </div>

      {/* ── Card 2: Độ chính xác IRT ── */}
      <div className="rounded-2xl p-4 sm:p-5 bg-gradient-to-br from-emerald-950/40 via-slate-900/80 to-slate-900/90 border border-emerald-500/30 shadow-xl backdrop-blur-md relative overflow-hidden group hover:border-emerald-400/60 transition-all">
        <div className="flex items-center justify-between mb-3">
          <span className="text-xs font-black text-emerald-300 tracking-wide uppercase flex items-center gap-1.5">
            <TrendingUp size={15} className="text-emerald-400" /> {t('metrics.accuracy.label')}
          </span>
          <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
            {t('metrics.accuracy.badge')}
          </span>
        </div>
        <div className="flex items-baseline gap-1">
          <span className="text-3xl font-black text-white">82.4</span>
          <span className="text-base font-black text-emerald-400">%</span>
        </div>
        <div className="mt-3 w-full bg-slate-800 rounded-full h-2 overflow-hidden">
          <div 
            className="bg-gradient-to-r from-emerald-500 to-teal-400 h-full rounded-full transition-all duration-700 shadow-sm shadow-emerald-500/50" 
            style={{ width: '82.4%' }} 
          />
        </div>
        <p className="text-[11px] text-slate-400 mt-2 flex items-center gap-1">
          <Sparkles size={12} className="text-emerald-400" /> {t('metrics.accuracy.footer')}
        </p>
      </div>

      {/* ── Card 3: Khối lượng câu giải ── */}
      <div className="rounded-2xl p-4 sm:p-5 bg-gradient-to-br from-cyan-950/40 via-slate-900/80 to-slate-900/90 border border-cyan-500/30 shadow-xl backdrop-blur-md relative overflow-hidden group hover:border-cyan-400/60 transition-all">
        <div className="flex items-center justify-between mb-3">
          <span className="text-xs font-black text-cyan-300 tracking-wide uppercase flex items-center gap-1.5">
            <Flag size={15} className="text-cyan-400" /> {t('metrics.questionsSolved.label')}
          </span>
          <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-cyan-500/20 text-cyan-300 border border-cyan-500/30">
            {t('metrics.questionsSolved.badge')}
          </span>
        </div>
        <div className="flex items-baseline gap-1.5">
          <span className="text-3xl font-black text-white">1,248</span>
          <span className="text-xs text-slate-400 font-semibold">{t('metrics.questionsSolved.unit')}</span>
        </div>
        <div className="mt-3 w-full bg-slate-800 rounded-full h-2 overflow-hidden">
          <div 
            className="bg-gradient-to-r from-cyan-500 to-blue-500 h-full rounded-full transition-all duration-700 shadow-sm shadow-cyan-500/50" 
            style={{ width: '65%' }} 
          />
        </div>
        <p className="text-[11px] text-slate-400 mt-2 flex items-center gap-1">
          <Flame size={12} className="text-amber-400" /> {t('metrics.questionsSolved.footer')}
        </p>
      </div>

      {/* ── Card 4: ELO Năng lực & Milestone Bar ── */}
      <div className="rounded-2xl p-4 sm:p-5 bg-gradient-to-br from-purple-950/40 via-slate-900/80 to-slate-900/90 border border-purple-500/30 shadow-xl backdrop-blur-md relative overflow-hidden group hover:border-purple-400/60 transition-all">
        <div className="flex items-center justify-between mb-3">
          <span className="text-xs font-black text-purple-300 tracking-wide uppercase flex items-center gap-1.5">
            <Trophy size={15} className="text-amber-400" /> {t('metrics.eloRank.label')}
          </span>
          <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-purple-500/20 text-purple-300 border border-purple-500/30">
            {t('metrics.eloRank.badge')}
          </span>
        </div>
        <div className="flex items-baseline gap-2">
          <span className="text-3xl font-black text-white">{t('metrics.eloRank.value')}</span>
          <span className="text-xs text-purple-300 font-bold uppercase">{t('metrics.eloRank.tier')}</span>
        </div>
        <div className="mt-3 w-full bg-slate-800 rounded-full h-2 overflow-hidden">
          <div 
            className="bg-gradient-to-r from-purple-500 to-rose-400 h-full rounded-full transition-all duration-700 shadow-sm shadow-purple-500/50" 
            style={{ width: '85%' }} 
          />
        </div>
        <p className="text-[11px] text-slate-400 mt-2 flex items-center gap-1">
          <ChevronRight size={12} className="text-purple-400" /> {t('metrics.eloRank.footer')}
        </p>
      </div>
    </div>
  );
}
