/**
 * AegisQuiz — SocialProofSection (Landing Page)
 * ================================================
 * Dải số liệu ấn tượng + logo/nhãn phân khúc khách hàng mục tiêu.
 * FSD Layer: pages/landing/components/
 */
import { useTranslation } from 'react-i18next';
import { Landmark, Building2, GraduationCap, Car } from 'lucide-react';

const SEGMENT_ICONS = [Landmark, Building2, GraduationCap, Car];

interface Stat {
  value: string;
  label: string;
}

export function SocialProofSection() {
  const { t } = useTranslation('landing');
  const stats = t('socialProof.stats', { returnObjects: true }) as Stat[];
  const segments = t('socialProof.segments', { returnObjects: true }) as string[];

  return (
    <section className="py-14 border-y border-slate-800/60 bg-slate-900/30">
      <div className="max-w-6xl mx-auto px-4 sm:px-6 space-y-10">
        <p className="text-center text-xs font-black uppercase tracking-widest text-slate-500">
          {t('socialProof.title')}
        </p>

        {/* Stats grid */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-6">
          {stats.map((stat, idx) => (
            <div key={idx} className="text-center">
              <p className="text-2xl sm:text-3xl font-black bg-gradient-to-r from-cyan-400 to-indigo-400 bg-clip-text text-transparent">
                {stat.value}
              </p>
              <p className="text-[11px] sm:text-xs text-slate-400 font-semibold mt-1">{stat.label}</p>
            </div>
          ))}
        </div>

        {/* Segment badges */}
        <div className="flex flex-wrap items-center justify-center gap-3">
          {segments.map((segment, idx) => {
            const Icon = SEGMENT_ICONS[idx] ?? Landmark;
            return (
              <span
                key={segment}
                className="inline-flex items-center gap-2 px-4 py-2 rounded-full text-xs font-bold
                           text-slate-300 bg-slate-950/60 border border-slate-800"
              >
                <Icon size={14} className="text-slate-500" aria-hidden="true" />
                {segment}
              </span>
            );
          })}
        </div>
      </div>
    </section>
  );
}
