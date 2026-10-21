import React from 'react';
import { useTranslation } from 'react-i18next';
import { Type, Square } from 'lucide-react';
import type { WhiteLabelConfig } from '../hooks/useWhiteLabelTheme';

interface ThemeFontSelectorProps {
  config: WhiteLabelConfig;
  onUpdate: (partial: Partial<WhiteLabelConfig>) => void;
}

const FONT_OPTIONS = [
  { label: 'Inter (Quốc tế & Tối giản)', value: 'Inter, system-ui, sans-serif' },
  { label: 'Be Vietnam Pro (Chuẩn tiếng Việt)', value: '"Be Vietnam Pro", sans-serif' },
  { label: 'Outfit (Hiện đại & Táo bạo)', value: 'Outfit, sans-serif' },
  { label: 'Roboto (Cổ điển & Dễ đọc)', value: 'Roboto, sans-serif' },
];

const RADIUS_OPTIONS = [
  { label: 'Sắc nét (Sharp - 4px)', value: '0.25rem' },
  { label: 'Tiêu chuẩn (Smooth - 12px)', value: '0.75rem' },
  { label: 'Mềm mại (Curved - 16px)', value: '1rem' },
  { label: 'Bo tròn (Pill - 24px)', value: '1.5rem' },
];

export const ThemeFontSelector: React.FC<ThemeFontSelectorProps> = ({
  config,
  onUpdate,
}) => {
  const { t } = useTranslation('white_label');

  return (
    <div className="bg-slate-900/90 border border-slate-800 rounded-2xl p-5 space-y-5">
      {/* Phông chữ */}
      <div className="space-y-2">
        <div className="flex items-center gap-2 text-sm font-bold text-white pb-2 border-b border-slate-800">
          <Type className="w-4 h-4 text-sky-400" />
          <span>{t('fontFamily')}</span>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
          {FONT_OPTIONS.map((f) => {
            const isSelected = config.fontFamily === f.value;
            return (
              <button
                key={f.label}
                type="button"
                onClick={() => onUpdate({ fontFamily: f.value })}
                className={`p-2.5 rounded-xl border text-left text-xs font-medium transition-all ${
                  isSelected
                    ? 'bg-slate-800 border-sky-500 shadow-md text-white'
                    : 'bg-slate-950/40 border-slate-800 text-slate-400 hover:border-slate-700'
                }`}
                style={{ fontFamily: f.value }}
              >
                {f.label}
              </button>
            );
          })}
        </div>
      </div>

      {/* Bo góc (Border radius) */}
      <div className="space-y-2 pt-2">
        <div className="flex items-center gap-2 text-sm font-bold text-white pb-2 border-b border-slate-800">
          <Square className="w-4 h-4 text-emerald-400" />
          <span>{t('borderRadius')}</span>
        </div>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
          {RADIUS_OPTIONS.map((r) => {
            const isSelected = config.borderRadius === r.value;
            return (
              <button
                key={r.label}
                type="button"
                onClick={() => onUpdate({ borderRadius: r.value })}
                className={`p-2 rounded-xl border text-center text-xs font-medium transition-all ${
                  isSelected
                    ? 'bg-slate-800 border-emerald-500 shadow-md text-white'
                    : 'bg-slate-950/40 border-slate-800 text-slate-400 hover:border-slate-700'
                }`}
              >
                {r.label}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
};
