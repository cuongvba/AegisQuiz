import React from 'react';
import { useTranslation } from 'react-i18next';
import { Lock, Sparkles, ArrowRight } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

export const StudioPaywallTeaser: React.FC = () => {
  const { t } = useTranslation('split_studio');
  const navigate = useNavigate();

  return (
    <div className="min-h-screen bg-slate-950 flex items-center justify-center p-4">
      <div className="max-w-md w-full bg-slate-900 border border-indigo-500/30 rounded-3xl p-8 text-center shadow-2xl relative overflow-hidden">
        {/* Glow decoration */}
        <div className="absolute -top-24 -left-24 w-48 h-48 bg-indigo-500/20 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-24 -right-24 w-48 h-48 bg-purple-500/20 rounded-full blur-3xl pointer-events-none" />

        <div className="w-16 h-16 rounded-2xl bg-indigo-500/10 border border-indigo-500/25 text-indigo-400 mx-auto flex items-center justify-center mb-6 shadow-inner">
          <Lock className="w-8 h-8" />
        </div>

        <h2 className="text-xl sm:text-2xl font-bold text-white mb-3 tracking-tight">
          {t('paywall.title')}
        </h2>

        <p className="text-slate-400 text-xs sm:text-sm mb-8 leading-relaxed">
          {t('paywall.description')}
        </p>

        <div className="space-y-3">
          <button
            onClick={() => navigate('/paywall')}
            className="w-full py-3 px-4 rounded-xl font-bold text-xs sm:text-sm bg-gradient-to-r from-indigo-500 to-purple-600 text-white shadow-lg shadow-indigo-500/25 hover:from-indigo-400 hover:to-purple-500 active:scale-98 transition-all flex items-center justify-center gap-2"
          >
            <Sparkles className="w-4 h-4" />
            <span>{t('paywall.upgradeNow')}</span>
            <ArrowRight className="w-4 h-4 ml-1" />
          </button>

          <button
            onClick={() => navigate('/admin/questions')}
            className="w-full py-2.5 px-4 rounded-xl text-xs font-semibold text-slate-400 hover:text-white bg-slate-800/60 hover:bg-slate-800 transition-colors"
          >
            {t('backToQuestions')}
          </button>
        </div>
      </div>
    </div>
  );
};
