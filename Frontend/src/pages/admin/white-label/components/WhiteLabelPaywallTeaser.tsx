import React from 'react';
import { useTranslation } from 'react-i18next';
import { ShieldAlert, Sparkles, ArrowRight } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

export const WhiteLabelPaywallTeaser: React.FC = () => {
  const { t } = useTranslation('white_label');
  const navigate = useNavigate();

  return (
    <div className="min-h-[500px] flex items-center justify-center p-6">
      <div className="max-w-md w-full bg-slate-900 border border-purple-500/30 rounded-3xl p-8 text-center shadow-2xl relative overflow-hidden">
        <div className="w-16 h-16 rounded-2xl bg-purple-500/10 border border-purple-500/25 text-purple-400 mx-auto flex items-center justify-center mb-6 shadow-inner">
          <ShieldAlert className="w-8 h-8" />
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
            className="w-full py-3 px-4 rounded-xl font-bold text-xs sm:text-sm bg-gradient-to-r from-purple-600 to-indigo-600 text-white shadow-lg shadow-purple-500/25 hover:from-purple-500 hover:to-indigo-500 active:scale-98 transition-all flex items-center justify-center gap-2"
          >
            <Sparkles className="w-4 h-4" />
            <span>{t('paywall.upgradeNow')}</span>
            <ArrowRight className="w-4 h-4 ml-1" />
          </button>
        </div>
      </div>
    </div>
  );
};
