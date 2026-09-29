/**
 * AegisQuiz — GameshowSection (Landing Page)
 * =============================================
 * Showcase 6 Gameshow học thuật đã build thật — điểm khác biệt lớn nhất so
 * với đối thủ (Kahoot/Quizizz không có).
 * FSD Layer: pages/landing/components/
 */
import { useTranslation } from 'react-i18next';
import { Trophy, CircleDot, Users, Grid3x3, Flame, BellRing } from 'lucide-react';

const ICONS = [Trophy, CircleDot, Users, Grid3x3, Flame, BellRing];

interface GameshowItem {
  name: string;
  desc: string;
}

export function GameshowSection() {
  const { t } = useTranslation('landing');
  const items = t('gameshows.items', { returnObjects: true }) as GameshowItem[];

  return (
    <section id="gameshows" className="py-20 sm:py-28 bg-slate-900/30 border-y border-slate-800/60">
      <div className="max-w-6xl mx-auto px-4 sm:px-6">
        <div className="text-center max-w-2xl mx-auto mb-14 space-y-3">
          <h2 className="text-2xl sm:text-4xl font-black text-white tracking-tight">{t('gameshows.title')}</h2>
          <p className="text-slate-400 text-sm sm:text-base">{t('gameshows.subtitle')}</p>
        </div>

        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-5">
          {items.map((item, idx) => {
            const Icon = ICONS[idx] ?? Trophy;
            return (
              <div
                key={item.name}
                className="relative overflow-hidden rounded-2xl border border-amber-500/20 bg-gradient-to-br from-slate-900 via-slate-900 to-amber-950/20 p-6"
              >
                <div className="absolute -top-8 -right-8 w-32 h-32 rounded-full bg-amber-500/10 blur-2xl pointer-events-none" />
                <div className="relative z-10">
                  <div className="w-11 h-11 rounded-xl bg-amber-500/15 border border-amber-500/30 text-amber-400 flex items-center justify-center mb-4">
                    <Icon size={20} aria-hidden="true" />
                  </div>
                  <h3 className="text-base font-bold text-white mb-2">{item.name}</h3>
                  <p className="text-sm text-slate-400 leading-relaxed">{item.desc}</p>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
