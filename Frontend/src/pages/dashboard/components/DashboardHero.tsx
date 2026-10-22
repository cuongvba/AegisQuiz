import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Sparkles, ShieldCheck, Award, Flame, Clock, Play, Zap, Target, Users, ArrowRight } from 'lucide-react';
import type { UserProfile } from '@/types/auth';

interface DashboardHeroProps {
  user: UserProfile | null;
  isAdmin: boolean;
  isTeamLeader: boolean;
}

export function DashboardHero({ user, isAdmin, isTeamLeader }: DashboardHeroProps) {
  const { t } = useTranslation('dashboard');
  const navigate = useNavigate();

  // Xác định lời chào theo múi giờ địa phương
  const currentHour = new Date().getHours();
  const greetingTime = currentHour < 12 
    ? t('greeting.morning') 
    : currentHour < 18 
      ? t('greeting.afternoon') 
      : t('greeting.evening');

  return (
    <div className="relative overflow-hidden rounded-3xl bg-gradient-to-r from-slate-900/95 via-indigo-950/70 to-slate-900/95 border border-slate-800/80 shadow-2xl backdrop-blur-xl p-5 sm:p-7">
      {/* Hiệu ứng hào quang nền tinh tế */}
      <div className="absolute -top-24 -right-24 w-80 h-80 rounded-full bg-gradient-to-br from-cyan-500/20 via-indigo-500/10 to-transparent blur-3xl pointer-events-none" />

      <div className="flex flex-col lg:flex-row items-stretch justify-between gap-6 relative z-10">
        
        {/* ── 70% Khối Trọng tâm: AI Daily Mission ── */}
        <div className="flex-1 space-y-4">
          {/* Header Eyebrow & Badges */}
          <div className="flex flex-wrap items-center gap-2">
            <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[11px] font-black bg-cyan-500/15 border border-cyan-500/30 text-cyan-300">
              <Sparkles size={12} className="text-cyan-400 animate-pulse" />
              <span>{t('hero.eyebrow')}</span>
            </span>

            {isTeamLeader ? (
              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-black bg-amber-500/15 border border-amber-500/30 text-amber-300">
                <ShieldCheck size={12} /> {t('hero.badgeTeamLeader')}
              </span>
            ) : isAdmin ? (
              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-black bg-purple-500/15 border border-purple-500/30 text-purple-300">
                <Award size={12} /> {t('hero.badgeAdmin')}
              </span>
            ) : (
              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-black bg-emerald-500/15 border border-emerald-500/30 text-emerald-300">
                <Flame size={12} /> {t('hero.badgeLearner')}
              </span>
            )}

            <span className="inline-flex items-center gap-1 text-[11px] text-slate-400 font-medium">
              <Clock size={11} /> {greetingTime}
            </span>
          </div>

          {/* Lời chào cá nhân hóa */}
          <h1 className="text-xl sm:text-3xl font-black text-white tracking-tight">
            {greetingTime},{' '}
            <span className="bg-gradient-to-r from-cyan-400 via-indigo-300 to-purple-400 bg-clip-text text-transparent">
              {user?.name ?? t('greeting.defaultName')}
            </span>! 👋
          </h1>

          {/* Hộp Nhiệm vụ Rèn Luyện 1-Chạm do AI tạo */}
          <div className="p-4 rounded-2xl bg-gradient-to-r from-indigo-950/80 to-slate-900/90 border border-indigo-500/30 shadow-lg space-y-3">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <span className="text-[10px] font-black uppercase tracking-wider text-cyan-400 px-2 py-0.5 rounded-md bg-cyan-950/60 border border-cyan-500/30">
                {t('hero.mission.tag')}
              </span>
              <div className="flex items-center gap-2 text-[11px] text-slate-300 font-semibold">
                <span>{t('hero.mission.progressLabel')}</span>
                <span className="text-cyan-300 font-black">{t('hero.mission.progressValue')}</span>
              </div>
            </div>

            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <div>
                <h3 className="text-sm sm:text-base font-bold text-white flex items-center gap-2">
                  <Target size={16} className="text-rose-400 shrink-0" />
                  <span>{t('hero.mission.title')}</span>
                </h3>
                <p className="text-xs text-slate-400 mt-1 line-clamp-1">
                  {t('hero.mission.desc')}
                </p>
              </div>

              <button
                onClick={() => navigate('/practice')}
                className="shrink-0 px-5 py-2.5 bg-gradient-to-r from-cyan-500 via-blue-600 to-indigo-600 hover:brightness-110 text-white font-black text-xs rounded-xl shadow-lg shadow-cyan-500/25 transition-all flex items-center gap-2 cursor-pointer active:scale-95 group"
              >
                <Play size={14} className="fill-white" />
                <span>{t('hero.mission.cta')}</span>
                <ArrowRight size={13} className="group-hover:translate-x-1 transition-transform" />
              </button>
            </div>
          </div>
        </div>

        {/* ── 30% Khối Lối tắt Vệ tinh (Quick Satellite Links) ── */}
        <div className="lg:w-72 flex flex-col justify-between gap-2.5">
          <button
            onClick={() => navigate('/arena')}
            className="group flex items-center justify-between p-3 rounded-2xl bg-slate-950/60 hover:bg-slate-900/80 border border-amber-500/30 hover:border-amber-400 transition-all cursor-pointer text-left"
          >
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-xl bg-amber-500/20 text-amber-400 flex items-center justify-center shrink-0 group-hover:scale-110 transition-transform">
                <Zap size={16} />
              </div>
              <div>
                <div className="text-xs font-black text-white group-hover:text-amber-300 transition-colors">
                  {t('actions.arena.title')}
                </div>
                <div className="text-[10px] text-slate-400">{t('actions.arena.desc')}</div>
              </div>
            </div>
            <ArrowRight size={14} className="text-slate-500 group-hover:text-amber-400 group-hover:translate-x-0.5 transition-all" />
          </button>

          <button
            onClick={() => navigate('/quiz/adaptive')}
            className="group flex items-center justify-between p-3 rounded-2xl bg-slate-950/60 hover:bg-slate-900/80 border border-rose-500/30 hover:border-rose-400 transition-all cursor-pointer text-left"
          >
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-xl bg-rose-500/20 text-rose-400 flex items-center justify-center shrink-0 group-hover:scale-110 transition-transform">
                <Target size={16} />
              </div>
              <div>
                <div className="text-xs font-black text-white group-hover:text-rose-300 transition-colors">
                  {t('actions.adaptive.title')}
                </div>
                <div className="text-[10px] text-slate-400">{t('actions.adaptive.desc')}</div>
              </div>
            </div>
            <ArrowRight size={14} className="text-slate-500 group-hover:text-rose-400 group-hover:translate-x-0.5 transition-all" />
          </button>

          <button
            onClick={() => navigate('/team')}
            className="group flex items-center justify-between p-3 rounded-2xl bg-slate-950/60 hover:bg-slate-900/80 border border-emerald-500/30 hover:border-emerald-400 transition-all cursor-pointer text-left"
          >
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-xl bg-emerald-500/20 text-emerald-400 flex items-center justify-center shrink-0 group-hover:scale-110 transition-transform">
                <Users size={16} />
              </div>
              <div>
                <div className="text-xs font-black text-white group-hover:text-emerald-300 transition-colors">
                  {t('actions.team.title')}
                </div>
                <div className="text-[10px] text-slate-400">{t('actions.team.desc')}</div>
              </div>
            </div>
            <ArrowRight size={14} className="text-slate-500 group-hover:text-emerald-400 group-hover:translate-x-0.5 transition-all" />
          </button>
        </div>

      </div>
    </div>
  );
}
