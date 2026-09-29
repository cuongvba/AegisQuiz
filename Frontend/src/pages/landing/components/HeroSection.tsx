/**
 * AegisQuiz — HeroSection (Landing Page)
 * =========================================
 * Màn hình đầu tiên — quyết định phần lớn tỉ lệ ở lại của khách truy cập.
 * FSD Layer: pages/landing/components/
 */
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Sparkles, ArrowRight, Building2, Radar as RadarIcon, Zap } from 'lucide-react';

export function HeroSection() {
  const { t } = useTranslation('landing');
  const navigate = useNavigate();

  return (
    <section className="relative overflow-hidden pt-16 pb-20 sm:pt-24 sm:pb-28">
      {/* Ambient glow background — nhất quán thẩm mỹ với Dashboard */}
      <div className="absolute inset-0 pointer-events-none overflow-hidden">
        <div className="absolute top-[-10%] left-[10%] w-[600px] h-[600px] rounded-full bg-cyan-600/10 blur-[130px]" />
        <div className="absolute top-[10%] right-[5%] w-[500px] h-[500px] rounded-full bg-indigo-600/10 blur-[140px]" />
        <div className="absolute bottom-[-10%] left-[35%] w-[700px] h-[700px] rounded-full bg-purple-600/10 blur-[160px]" />
      </div>

      <div className="relative z-10 max-w-5xl mx-auto px-4 sm:px-6 text-center space-y-8">
        <span className="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-full text-xs font-black bg-cyan-500/15 border border-cyan-500/30 text-cyan-300">
          <Sparkles size={13} className="text-cyan-400" aria-hidden="true" />
          {t('hero.eyebrow')}
        </span>

        <h1 className="text-3xl sm:text-5xl lg:text-6xl font-black tracking-tight leading-tight text-white">
          {t('hero.headline')}{' '}
          <span className="block sm:inline bg-gradient-to-r from-cyan-400 via-indigo-300 to-purple-400 bg-clip-text text-transparent">
            {t('hero.headlineHighlight')}
          </span>
        </h1>

        <p className="max-w-2xl mx-auto text-base sm:text-lg text-slate-300 leading-relaxed">
          {t('hero.subheadline')}
        </p>

        <div className="flex flex-col sm:flex-row items-center justify-center gap-3 pt-2">
          <button
            onClick={() => navigate('/register')}
            className="w-full sm:w-auto px-6 py-3.5 rounded-full font-bold text-sm text-white
                       bg-[var(--brand-gradient)] shadow-[var(--tenant-shadow-primary)]
                       transition-all active:scale-95 hover:brightness-110 cursor-pointer
                       flex items-center justify-center gap-2"
          >
            {t('hero.ctaPrimary')} <ArrowRight size={16} aria-hidden="true" />
          </button>
          <a
            href="#segments"
            className="w-full sm:w-auto px-6 py-3.5 rounded-full font-bold text-sm text-slate-200
                       bg-slate-900/80 border border-slate-700 hover:border-slate-500 hover:bg-slate-800
                       transition-all cursor-pointer flex items-center justify-center gap-2"
          >
            <Building2 size={16} aria-hidden="true" /> {t('hero.ctaSecondary')}
          </a>
        </div>

        {/* Visual proof card — thay ảnh tĩnh bằng khối "sống động" nhẹ */}
        <div className="pt-10 max-w-3xl mx-auto">
          <div className="rounded-3xl border border-slate-800/80 bg-slate-900/60 backdrop-blur-xl shadow-2xl p-4 sm:p-6">
            <div className="flex items-center gap-2 mb-4">
              <span className="w-2.5 h-2.5 rounded-full bg-rose-500" />
              <span className="w-2.5 h-2.5 rounded-full bg-amber-500" />
              <span className="w-2.5 h-2.5 rounded-full bg-emerald-500" />
              <span className="ms-3 text-[11px] font-bold text-slate-500 uppercase tracking-wider flex items-center gap-1.5">
                <RadarIcon size={12} aria-hidden="true" /> {t('hero.previewCaption')}
              </span>
            </div>
            <div className="grid grid-cols-3 gap-3">
              {[68, 92, 85, 64, 88, 77].map((value, idx) => (
                <div
                  key={idx}
                  className="rounded-xl bg-slate-950/60 border border-slate-800/80 p-3 flex flex-col gap-2"
                >
                  <div className="h-1.5 rounded-full bg-slate-800 overflow-hidden">
                    <div
                      className="h-full rounded-full bg-gradient-to-r from-cyan-500 to-indigo-500"
                      style={{ width: `${value}%` }}
                    />
                  </div>
                  <span className="text-[10px] font-bold text-slate-500 flex items-center gap-1">
                    <Zap size={10} className="text-amber-400" aria-hidden="true" /> {value}%
                  </span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
