/**
 * AegisQuiz — FinalCtaSection (Landing Page)
 * =============================================
 * Lời kêu gọi hành động cuối trang — lặp lại CTA lần cuối trước khi rời trang.
 * FSD Layer: pages/landing/components/
 */
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ArrowRight, Building2 } from 'lucide-react';

export function FinalCtaSection() {
  const { t } = useTranslation('landing');
  const navigate = useNavigate();

  return (
    <section className="py-20 sm:py-24">
      <div className="max-w-4xl mx-auto px-4 sm:px-6">
        <div className="relative overflow-hidden rounded-3xl border border-slate-800/80 bg-gradient-to-br from-slate-900 via-indigo-950/40 to-slate-900 p-8 sm:p-14 text-center">
          <div className="absolute -top-20 -end-20 w-72 h-72 rounded-full bg-cyan-500/15 blur-3xl pointer-events-none" />
          <div className="relative z-10 space-y-6">
            <h2 className="text-2xl sm:text-4xl font-black text-white tracking-tight">
              {t('finalCta.title')}
            </h2>
            <p className="max-w-xl mx-auto text-sm sm:text-base text-slate-300">
              {t('finalCta.subtitle')}
            </p>
            <div className="flex flex-col sm:flex-row items-center justify-center gap-3 pt-2">
              <button
                onClick={() => navigate('/register')}
                className="w-full sm:w-auto px-6 py-3.5 rounded-full font-bold text-sm text-white
                           bg-[var(--brand-gradient)] shadow-[var(--tenant-shadow-primary)]
                           transition-all active:scale-95 hover:brightness-110 cursor-pointer
                           flex items-center justify-center gap-2"
              >
                {t('finalCta.ctaPrimary')} <ArrowRight size={16} aria-hidden="true" />
              </button>
              <button
                onClick={() => navigate('/register')}
                className="w-full sm:w-auto px-6 py-3.5 rounded-full font-bold text-sm text-slate-200
                           bg-slate-900/80 border border-slate-700 hover:border-slate-500 hover:bg-slate-800
                           transition-all cursor-pointer flex items-center justify-center gap-2"
              >
                <Building2 size={16} aria-hidden="true" /> {t('finalCta.ctaSecondary')}
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
