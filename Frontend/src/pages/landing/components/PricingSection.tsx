/**
 * AegisQuiz — PricingSection (Landing Page)
 * ============================================
 * 3 cột giá chuẩn SaaS — cột giữa (Pro/Team) luôn được làm nổi bật.
 * FSD Layer: pages/landing/components/
 */
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Check, Sparkles } from 'lucide-react';

interface PricingTier {
  name: string;
  price: string;
  period: string;
  description: string;
  features: string[];
  cta: string;
  highlighted: boolean;
}

export function PricingSection() {
  const { t } = useTranslation('landing');
  const navigate = useNavigate();
  const tiers = t('pricing.tiers', { returnObjects: true }) as PricingTier[];

  return (
    <section id="pricing" className="py-20 sm:py-28 bg-slate-900/30 border-y border-slate-800/60">
      <div className="max-w-6xl mx-auto px-4 sm:px-6">
        <div className="text-center max-w-2xl mx-auto mb-14 space-y-3">
          <h2 className="text-2xl sm:text-4xl font-black text-white tracking-tight">{t('pricing.title')}</h2>
          <p className="text-slate-400 text-sm sm:text-base">{t('pricing.subtitle')}</p>
        </div>

        <div className="grid sm:grid-cols-3 gap-6 items-start">
          {tiers.map((tier) => (
            <div
              key={tier.name}
              className={`relative rounded-3xl p-6 sm:p-7 flex flex-col h-full
                ${tier.highlighted
                  ? 'border-2 border-cyan-500/60 bg-gradient-to-b from-cyan-950/40 to-slate-900/60 shadow-2xl shadow-cyan-500/10 sm:-translate-y-3'
                  : 'border border-slate-800/80 bg-slate-900/50'
                }`}
            >
              {tier.highlighted && (
                <span className="absolute -top-3 start-1/2 -translate-x-1/2 px-3 py-1 rounded-full text-[10px] font-black uppercase tracking-wider bg-gradient-to-r from-cyan-500 to-indigo-600 text-white flex items-center gap-1">
                  <Sparkles size={10} aria-hidden="true" /> Popular
                </span>
              )}

              <h3 className="text-lg font-bold text-white">{tier.name}</h3>
              <p className="text-xs text-slate-400 mt-1 mb-4">{tier.description}</p>

              <div className="mb-5">
                <span className="text-2xl sm:text-3xl font-black text-white">{tier.price}</span>
                {tier.period && <span className="text-sm text-slate-500 ms-1">{tier.period}</span>}
              </div>

              <ul className="space-y-2.5 mb-6 flex-1">
                {tier.features.map((feature) => (
                  <li key={feature} className="flex items-start gap-2 text-sm text-slate-300">
                    <Check size={16} className="text-emerald-400 flex-shrink-0 mt-0.5" aria-hidden="true" />
                    <span>{feature}</span>
                  </li>
                ))}
              </ul>

              <button
                onClick={() => navigate('/register')}
                className={`w-full py-3 rounded-full font-bold text-sm transition-all active:scale-95 cursor-pointer
                  ${tier.highlighted
                    ? 'bg-[var(--brand-gradient)] text-white shadow-[var(--tenant-shadow-primary)] hover:brightness-110'
                    : 'bg-slate-800 text-slate-200 hover:bg-slate-700 border border-slate-700'
                  }`}
              >
                {tier.cta}
              </button>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
