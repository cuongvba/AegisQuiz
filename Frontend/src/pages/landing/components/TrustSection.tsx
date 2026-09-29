/**
 * AegisQuiz — TrustSection (Landing Page)
 * ==========================================
 * Huy hiệu tin cậy — quan trọng nhất với nhóm khách Banking/Enterprise.
 * FSD Layer: pages/landing/components/
 */
import { useTranslation } from 'react-i18next';
import { KeyRound, Lock, ShieldCheck, Layers } from 'lucide-react';

const ICONS = [KeyRound, Lock, ShieldCheck, Layers];

interface TrustBadge {
  label: string;
  desc: string;
}

export function TrustSection() {
  const { t } = useTranslation('landing');
  const badges = t('trust.badges', { returnObjects: true }) as TrustBadge[];

  return (
    <section className="py-16 sm:py-20">
      <div className="max-w-6xl mx-auto px-4 sm:px-6">
        <h2 className="text-center text-xl sm:text-2xl font-black text-white tracking-tight mb-10">
          {t('trust.title')}
        </h2>

        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          {badges.map((badge, idx) => {
            const Icon = ICONS[idx] ?? ShieldCheck;
            return (
              <div
                key={badge.label}
                className="rounded-2xl border border-slate-800/80 bg-slate-900/50 p-5 text-center flex flex-col items-center gap-2.5"
              >
                <div className="w-10 h-10 rounded-xl bg-emerald-500/15 border border-emerald-500/30 text-emerald-400 flex items-center justify-center">
                  <Icon size={18} aria-hidden="true" />
                </div>
                <p className="text-xs font-black text-white">{badge.label}</p>
                <p className="text-[11px] text-slate-500 leading-snug">{badge.desc}</p>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
