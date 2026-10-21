import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Sparkles, Copy, Check, Gift } from 'lucide-react';
import { EducatorPartnershipInfo } from '../types/zen-studio.types';

interface ZenTrialBannerProps {
  partnership?: EducatorPartnershipInfo | null;
}

export const ZenTrialBanner: React.FC<ZenTrialBannerProps> = ({ partnership }) => {
  const { t } = useTranslation('creator');
  const [copied, setCopied] = useState(false);

  if (!partnership) return null;

  const handleCopy = () => {
    if (partnership.referralCode) {
      navigator.clipboard.writeText(partnership.referralCode);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <div className="relative overflow-hidden rounded-2xl bg-gradient-to-r from-emerald-600/15 via-teal-500/10 to-indigo-600/15 border border-emerald-500/30 p-4 mb-6 backdrop-blur-md">
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="p-2.5 rounded-xl bg-emerald-500/20 text-emerald-400 shrink-0">
            <Sparkles className="w-5 h-5 animate-pulse" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="font-semibold text-emerald-300 text-sm md:text-base">
                {t('trialBanner.title')}
              </span>
              <span className="px-2 py-0.5 text-xs font-bold rounded-full bg-emerald-500/30 text-emerald-300 border border-emerald-400/30">
                {t('trialBanner.daysLeft', { days: partnership.daysRemaining })}
              </span>
            </div>
            <p className="text-xs text-slate-300 mt-0.5">
              {t('trialBanner.revShare')} • <span className="text-amber-300 font-semibold">{partnership.karmaPoints} Karma</span>
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2 w-full md:w-auto bg-slate-900/60 p-1.5 rounded-xl border border-slate-700/60">
          <Gift className="w-4 h-4 text-emerald-400 ml-2 shrink-0" />
          <span className="text-xs text-slate-400 hidden sm:inline">{t('trialBanner.referralLabel')}</span>
          <code className="text-xs font-mono font-bold text-emerald-400 px-2 py-1 bg-slate-800 rounded">
            {partnership.referralCode}
          </code>
          <button
            onClick={handleCopy}
            className="flex items-center gap-1 px-2.5 py-1 text-xs font-medium rounded-lg bg-emerald-500/20 hover:bg-emerald-500/30 text-emerald-300 transition-colors"
          >
            {copied ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
            <span>{copied ? t('trialBanner.copied') : t('trialBanner.copyReferral')}</span>
          </button>
        </div>
      </div>
    </div>
  );
};
