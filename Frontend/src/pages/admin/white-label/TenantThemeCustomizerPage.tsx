import React from 'react';
import { useTranslation } from 'react-i18next';
import { FeatureGate } from '@/shared/components/entitlement';
import { FeatureKey } from '@/types/entitlement';
import { Sparkles, Save, RotateCcw, CheckCircle2, Building2 } from 'lucide-react';
import { useWhiteLabelTheme } from './hooks/useWhiteLabelTheme';
import { ThemeColorPalettePicker } from './components/ThemeColorPalettePicker';
import { ThemeFontSelector } from './components/ThemeFontSelector';
import { LivePreviewSection } from './components/LivePreviewSection';
import { WhiteLabelPaywallTeaser } from './components/WhiteLabelPaywallTeaser';

export const TenantThemeCustomizerPage: React.FC = () => {
  const { t } = useTranslation('white_label');
  const { config, updateConfig, saveConfig, resetDefaults, isSaved } = useWhiteLabelTheme();

  return (
    <FeatureGate feature={FeatureKey.WhiteLabelTheming} fallback={<WhiteLabelPaywallTeaser />}>
      <div className="space-y-6 max-w-7xl mx-auto pb-12">
        {/* Page Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pb-4 border-b border-slate-800">
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl sm:text-2xl font-black text-white tracking-tight flex items-center gap-2">
                <Building2 className="w-6 h-6 text-indigo-400" />
                {t('title')}
              </h1>
              <span className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-purple-500/15 text-purple-300 border border-purple-500/30">
                ENTERPRISE TIER
              </span>
            </div>
            <p className="text-xs sm:text-sm text-slate-400 mt-1">
              {t('subtitle')}
            </p>
          </div>

          <div className="flex items-center gap-2.5">
            <button
              type="button"
              onClick={resetDefaults}
              className="flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs font-semibold text-slate-400 hover:text-white bg-slate-850 hover:bg-slate-800 border border-slate-700/80 transition-all"
            >
              <RotateCcw className="w-3.5 h-3.5" />
              <span>{t('resetDefault')}</span>
            </button>

            <button
              type="button"
              onClick={saveConfig}
              className="flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-bold bg-gradient-to-r from-indigo-500 to-purple-600 text-white shadow-lg shadow-indigo-500/25 hover:from-indigo-400 hover:to-purple-500 active:scale-95 transition-all"
            >
              <Save className="w-3.5 h-3.5" />
              <span>{t('saveChanges')}</span>
            </button>
          </div>
        </div>

        {/* Success Alert */}
        {isSaved && (
          <div className="flex items-center gap-2.5 p-3.5 rounded-2xl bg-emerald-500/15 border border-emerald-500/30 text-emerald-300 text-xs sm:text-sm animate-fadeIn">
            <CheckCircle2 className="w-4 h-4 shrink-0" />
            <span>{t('savedSuccess')}</span>
          </div>
        )}

        {/* Two-Column Responsive Studio Workspace */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
          {/* Cột Trái: Cấu hình bảng màu & Phông chữ */}
          <div className="lg:col-span-7 space-y-6">
            {/* Nhận diện tổ chức */}
            <div className="bg-slate-900/90 border border-slate-800 rounded-2xl p-5 space-y-4">
              <span className="text-sm font-bold text-white block pb-2 border-b border-slate-800">
                {t('brandIdentity')}
              </span>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="text-xs text-slate-400 block mb-1">
                    {t('organizationName')}
                  </label>
                  <input
                    type="text"
                    value={config.organizationName}
                    onChange={(e) => updateConfig({ organizationName: e.target.value })}
                    className="w-full text-xs bg-slate-950 border border-slate-800 rounded-xl p-2.5 text-slate-100 focus:outline-none focus:border-indigo-500"
                  />
                </div>
                <div>
                  <label className="text-xs text-slate-400 block mb-1">
                    {t('customDomain')}
                  </label>
                  <input
                    type="text"
                    value={config.customDomain}
                    onChange={(e) => updateConfig({ customDomain: e.target.value })}
                    placeholder={t('domainPlaceholder')}
                    className="w-full text-xs bg-slate-950 border border-slate-800 rounded-xl p-2.5 text-slate-100 focus:outline-none focus:border-indigo-500 font-mono"
                  />
                </div>
              </div>
            </div>

            {/* Bộ chọn màu */}
            <ThemeColorPalettePicker config={config} onUpdate={updateConfig} />

            {/* Bộ chọn phông chữ & bo góc */}
            <ThemeFontSelector config={config} onUpdate={updateConfig} />
          </div>

          {/* Cột Phải: Khung Live Preview 0ms */}
          <div className="lg:col-span-5 sticky top-6">
            <LivePreviewSection config={config} />
          </div>
        </div>
      </div>
    </FeatureGate>
  );
};

export default TenantThemeCustomizerPage;
