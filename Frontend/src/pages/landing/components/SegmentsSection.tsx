/**
 * AegisQuiz — SegmentsSection (Landing Page)
 * =============================================
 * 4 tab phân khúc khách hàng — mỗi tab: Nỗi đau (painPoint) + Giải pháp (solution).
 * FSD Layer: pages/landing/components/
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Landmark, Building2, GraduationCap, Car, ArrowRight } from 'lucide-react';

const ICONS = [Landmark, Building2, GraduationCap, Car];

interface SegmentTab {
  id: string;
  label: string;
  painPoint: string;
  solution: string;
}

export function SegmentsSection() {
  const { t } = useTranslation('landing');
  const tabs = t('segments.tabs', { returnObjects: true }) as SegmentTab[];
  const [activeIdx, setActiveIdx] = useState(0);
  const active = tabs[activeIdx];

  return (
    <section id="segments" className="py-20 sm:py-28">
      <div className="max-w-5xl mx-auto px-4 sm:px-6">
        <div className="text-center max-w-2xl mx-auto mb-12 space-y-3">
          <h2 className="text-2xl sm:text-4xl font-black text-white tracking-tight">{t('segments.title')}</h2>
          <p className="text-slate-400 text-sm sm:text-base">{t('segments.subtitle')}</p>
        </div>

        {/* Tab selector */}
        <div className="flex flex-wrap items-center justify-center gap-2 mb-8">
          {tabs.map((tab, idx) => {
            const Icon = ICONS[idx] ?? Landmark;
            const isActive = idx === activeIdx;
            return (
              <button
                key={tab.id}
                onClick={() => setActiveIdx(idx)}
                aria-current={isActive ? 'true' : undefined}
                className={`px-4 py-2.5 rounded-xl font-bold text-xs flex items-center gap-2 transition-all cursor-pointer
                  ${isActive
                    ? 'bg-gradient-to-r from-cyan-500 to-indigo-600 text-white shadow-md shadow-cyan-500/25'
                    : 'text-slate-300 bg-slate-900/60 border border-slate-800 hover:bg-slate-800'
                  }`}
              >
                <Icon size={14} aria-hidden="true" /> {tab.label}
              </button>
            );
          })}
        </div>

        {/* Active content */}
        {active && (
          <div className="rounded-3xl border border-slate-800/80 bg-slate-900/50 backdrop-blur-xl p-6 sm:p-8 grid sm:grid-cols-2 gap-6">
            <div>
              <p className="text-[11px] font-black uppercase tracking-widest text-rose-400 mb-2">Pain Point</p>
              <p className="text-sm text-slate-300 leading-relaxed">{active.painPoint}</p>
            </div>
            <div className="sm:border-s sm:border-slate-800 sm:ps-6">
              <p className="text-[11px] font-black uppercase tracking-widest text-emerald-400 mb-2 flex items-center gap-1.5">
                <ArrowRight size={12} aria-hidden="true" /> AegisQuiz Solution
              </p>
              <p className="text-sm text-slate-300 leading-relaxed">{active.solution}</p>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
