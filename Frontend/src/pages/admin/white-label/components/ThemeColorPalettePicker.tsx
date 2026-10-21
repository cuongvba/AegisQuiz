import React from 'react';
import { useTranslation } from 'react-i18next';
import { Palette, Check } from 'lucide-react';
import type { WhiteLabelConfig } from '../hooks/useWhiteLabelTheme';

interface ThemeColorPalettePickerProps {
  config: WhiteLabelConfig;
  onUpdate: (partial: Partial<WhiteLabelConfig>) => void;
}

const PRESET_PALETTES = [
  { name: 'Indigo Tech', primary: '#4f46e5', secondary: '#0ea5e9', accent: '#8b5cf6' },
  { name: 'Emerald Banking', primary: '#059669', secondary: '#0d9488', accent: '#10b981' },
  { name: 'Royal Navy', primary: '#1e40af', secondary: '#0284c7', accent: '#3b82f6' },
  { name: 'Crimson Shield', primary: '#dc2626', secondary: '#ea580c', accent: '#f43f5e' },
  { name: 'Sleek Obsidian', primary: '#334155', secondary: '#64748b', accent: '#475569' },
];

export const ThemeColorPalettePicker: React.FC<ThemeColorPalettePickerProps> = ({
  config,
  onUpdate,
}) => {
  const { t } = useTranslation('white_label');

  return (
    <div className="bg-slate-900/90 border border-slate-800 rounded-2xl p-5 space-y-5">
      <div className="flex items-center gap-2 text-sm font-bold text-white pb-3 border-b border-slate-800">
        <Palette className="w-4 h-4 text-indigo-400" />
        <span>{t('colorSystem')}</span>
      </div>

      {/* Preset Palettes */}
      <div className="space-y-2">
        <span className="text-xs text-slate-400 font-medium">Bảng màu doanh nghiệp đề xuất</span>
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
          {PRESET_PALETTES.map((p) => {
            const isSelected =
              config.primaryColor === p.primary && config.secondaryColor === p.secondary;
            return (
              <button
                key={p.name}
                type="button"
                onClick={() =>
                  onUpdate({
                    primaryColor: p.primary,
                    secondaryColor: p.secondary,
                    accentColor: p.accent,
                  })
                }
                className={`flex items-center justify-between p-2 rounded-xl border text-xs font-medium transition-all ${
                  isSelected
                    ? 'bg-slate-800 border-indigo-500 shadow-md'
                    : 'bg-slate-950/40 border-slate-800 hover:border-slate-700'
                }`}
              >
                <div className="flex items-center gap-1.5">
                  <span
                    className="w-3.5 h-3.5 rounded-full"
                    style={{ backgroundColor: p.primary }}
                  />
                  <span
                    className="w-3.5 h-3.5 rounded-full"
                    style={{ backgroundColor: p.secondary }}
                  />
                  <span className="text-slate-300 text-[11px] truncate max-w-[80px]">
                    {p.name}
                  </span>
                </div>
                {isSelected && <Check className="w-3 h-3 text-indigo-400 shrink-0" />}
              </button>
            );
          })}
        </div>
      </div>

      {/* Tùy chỉnh màu chi tiết */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 pt-2">
        <div>
          <label className="text-xs text-slate-400 block mb-1.5">{t('primaryColor')}</label>
          <div className="flex items-center gap-2 bg-slate-950 border border-slate-800 rounded-xl p-2">
            <input
              type="color"
              value={config.primaryColor}
              onChange={(e) => onUpdate({ primaryColor: e.target.value })}
              className="w-6 h-6 rounded cursor-pointer border-0 bg-transparent"
            />
            <span className="text-xs font-mono text-slate-300 uppercase">
              {config.primaryColor}
            </span>
          </div>
        </div>

        <div>
          <label className="text-xs text-slate-400 block mb-1.5">{t('secondaryColor')}</label>
          <div className="flex items-center gap-2 bg-slate-950 border border-slate-800 rounded-xl p-2">
            <input
              type="color"
              value={config.secondaryColor}
              onChange={(e) => onUpdate({ secondaryColor: e.target.value })}
              className="w-6 h-6 rounded cursor-pointer border-0 bg-transparent"
            />
            <span className="text-xs font-mono text-slate-300 uppercase">
              {config.secondaryColor}
            </span>
          </div>
        </div>

        <div>
          <label className="text-xs text-slate-400 block mb-1.5">{t('accentColor')}</label>
          <div className="flex items-center gap-2 bg-slate-950 border border-slate-800 rounded-xl p-2">
            <input
              type="color"
              value={config.accentColor}
              onChange={(e) => onUpdate({ accentColor: e.target.value })}
              className="w-6 h-6 rounded cursor-pointer border-0 bg-transparent"
            />
            <span className="text-xs font-mono text-slate-300 uppercase">
              {config.accentColor}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
};
