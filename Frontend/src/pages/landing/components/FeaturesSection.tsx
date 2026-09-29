/**
 * AegisQuiz — FeaturesSection (Landing Page)
 * =============================================
 * 6 Đột phá công nghệ — Bento grid, mỗi ô 1 màu icon nhất quán theo vị trí.
 * FSD Layer: pages/landing/components/
 */
import { useTranslation } from 'react-i18next';
import { Brain, Zap, ShieldCheck, QrCode, FileUp, GraduationCap } from 'lucide-react';

const ICONS = [Brain, Zap, ShieldCheck, QrCode, FileUp, GraduationCap];
const ICON_STYLES = [
  'bg-cyan-500/15 border-cyan-500/30 text-cyan-400',
  'bg-amber-500/15 border-amber-500/30 text-amber-400',
  'bg-rose-500/15 border-rose-500/30 text-rose-400',
  'bg-emerald-500/15 border-emerald-500/30 text-emerald-400',
  'bg-indigo-500/15 border-indigo-500/30 text-indigo-400',
  'bg-purple-500/15 border-purple-500/30 text-purple-400',
];

interface FeatureItem {
  title: string;
  desc: string;
}

export function FeaturesSection() {
  const { t } = useTranslation('landing');
  const items = t('features.items', { returnObjects: true }) as FeatureItem[];

  return (
    <section id="features" className="py-20 sm:py-28">
      <div className="max-w-6xl mx-auto px-4 sm:px-6">
        <div className="text-center max-w-2xl mx-auto mb-14 space-y-3">
          <h2 className="text-2xl sm:text-4xl font-black text-white tracking-tight">{t('features.title')}</h2>
          <p className="text-slate-400 text-sm sm:text-base">{t('features.subtitle')}</p>
        </div>

        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-5">
          {items.map((item, idx) => {
            const Icon = ICONS[idx] ?? Brain;
            return (
              <div
                key={item.title}
                className="group rounded-2xl border border-slate-800/80 bg-slate-900/50 backdrop-blur-xl p-6
                           hover:border-slate-700 hover:-translate-y-0.5 transition-all duration-300"
              >
                <div
                  className={`w-11 h-11 rounded-xl border flex items-center justify-center mb-4 ${ICON_STYLES[idx % ICON_STYLES.length]}`}
                >
                  <Icon size={20} aria-hidden="true" />
                </div>
                <h3 className="text-base font-bold text-white mb-2">{item.title}</h3>
                <p className="text-sm text-slate-400 leading-relaxed">{item.desc}</p>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
