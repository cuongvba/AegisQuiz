import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Brain, Compass, Loader2 } from 'lucide-react';

interface AiMentorCoachCardProps {
  planText: string;
  isLoading: boolean;
}

export function AiMentorCoachCard({ planText, isLoading }: AiMentorCoachCardProps) {
  const { t } = useTranslation('dashboard');
  const navigate = useNavigate();

  return (
    <div className="rounded-3xl bg-gradient-to-br from-indigo-950/80 via-slate-900/90 to-purple-950/70 border border-indigo-500/30 p-5 sm:p-6 shadow-xl backdrop-blur-xl relative overflow-hidden">
      {/* Glow gradient vi mô */}
      <div className="absolute top-0 right-0 w-32 h-32 bg-indigo-500/10 rounded-full blur-2xl pointer-events-none" />

      <div className="flex items-center gap-2.5 mb-3">
        <div className="w-8 h-8 rounded-xl bg-gradient-to-br from-purple-500 to-indigo-600 flex items-center justify-center text-white shadow-md shadow-indigo-500/30 shrink-0">
          <Brain size={16} />
        </div>
        <div>
          <h3 className="text-sm font-black text-white">{t('aiMentor.title')}</h3>
          <p className="text-[10px] text-slate-400">{t('aiMentor.subtitle')}</p>
        </div>
      </div>

      {isLoading ? (
        <div className="flex items-center gap-2 text-slate-400 text-xs py-4">
          <Loader2 size={16} className="animate-spin text-indigo-400" />
          <span>{t('aiMentor.loading')}</span>
        </div>
      ) : (
        <div className="bg-slate-950/70 border border-indigo-500/20 rounded-2xl p-3.5 mt-2">
          <p className="text-xs text-slate-300 font-medium leading-relaxed whitespace-pre-line line-clamp-4">
            {planText || t('aiMentor.fallbackPlan')}
          </p>
        </div>
      )}

      <button
        onClick={() => navigate('/paths')}
        className="mt-4 w-full py-2.5 bg-indigo-600/20 hover:bg-indigo-600/30 text-indigo-300 border border-indigo-500/40 rounded-xl text-xs font-black transition-all flex items-center justify-center gap-1.5 cursor-pointer active:scale-98"
      >
        <Compass size={14} />
        <span>{t('aiMentor.cta')}</span>
      </button>
    </div>
  );
}
