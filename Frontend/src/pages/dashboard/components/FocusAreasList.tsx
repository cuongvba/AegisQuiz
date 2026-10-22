import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Zap, BookOpen, ArrowRight } from 'lucide-react';
import type { TopicData } from './CognitiveRadarPanel';

interface FocusAreasListProps {
  topics: TopicData[];
}

export function FocusAreasList({ topics }: FocusAreasListProps) {
  const { t } = useTranslation('dashboard');
  const navigate = useNavigate();

  return (
    <div className="rounded-3xl bg-slate-900/80 border border-slate-800/80 p-5 sm:p-6 shadow-xl backdrop-blur-xl">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-base font-black text-white flex items-center gap-2">
          <Zap size={18} className="text-rose-400" /> {t('focusAreas.title')}
        </h3>
        <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-rose-500/20 text-rose-300 border border-rose-500/30">
          {t('focusAreas.priority')}
        </span>
      </div>

      <div className="space-y-3">
        {topics.map((topic, idx) => {
          const percent = topic.A;
          const isCritical = percent < 70;
          return (
            <div 
              key={idx} 
              className="p-3 rounded-2xl bg-slate-950/60 border border-slate-800/70 hover:border-slate-700/80 transition-all space-y-2"
            >
              <div className="flex items-center justify-between text-xs font-bold">
                <span className="text-slate-200">{topic.subject}</span>
                <div className="flex items-center gap-2">
                  <span className={isCritical ? 'text-rose-400 font-black' : 'text-amber-400 font-black'}>
                    {percent}%
                  </span>
                  <button
                    onClick={() => navigate(`/practice?topic=${encodeURIComponent(topic.subject)}`)}
                    className="text-[10px] px-2 py-0.5 rounded-md bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white font-semibold flex items-center gap-0.5 transition-colors cursor-pointer"
                  >
                    <span>{t('focusAreas.quickPractice')}</span>
                    <ArrowRight size={10} />
                  </button>
                </div>
              </div>

              <div className="w-full bg-slate-800 rounded-full h-1.5 overflow-hidden">
                <div
                  className={`h-full rounded-full transition-all duration-700 ${
                    isCritical 
                      ? 'bg-gradient-to-r from-rose-500 to-orange-500 shadow-sm shadow-rose-500/50' 
                      : 'bg-gradient-to-r from-amber-500 to-emerald-400 shadow-sm shadow-amber-500/50'
                  }`}
                  style={{ width: `${percent}%` }}
                />
              </div>
            </div>
          );
        })}
      </div>

      <button
        onClick={() => navigate('/practice')}
        className="mt-4 w-full py-2.5 bg-gradient-to-r from-rose-600 to-orange-600 hover:brightness-110 text-white rounded-xl text-xs font-black shadow-lg shadow-rose-600/25 transition-all flex items-center justify-center gap-2 cursor-pointer active:scale-98"
      >
        <BookOpen size={15} />
        <span>{t('focusAreas.cta')}</span>
      </button>
    </div>
  );
}
