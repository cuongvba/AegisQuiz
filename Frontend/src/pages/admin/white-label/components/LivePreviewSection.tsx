import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Eye, CheckCircle2, ShieldCheck, Globe, Building } from 'lucide-react';
import type { WhiteLabelConfig } from '../hooks/useWhiteLabelTheme';

interface LivePreviewSectionProps {
  config: WhiteLabelConfig;
}

export const LivePreviewSection: React.FC<LivePreviewSectionProps> = ({ config }) => {
  const { t } = useTranslation('white_label');
  const [selectedOption, setSelectedOption] = useState<string>('B');

  return (
    <div className="bg-slate-900/90 border border-slate-800 rounded-3xl p-6 space-y-5 shadow-2xl relative overflow-hidden">
      <div className="flex items-center justify-between pb-3 border-b border-slate-800">
        <div className="flex items-center gap-2 text-sm font-bold text-white">
          <Eye className="w-4 h-4 text-purple-400" />
          <span>{t('previewHeader')}</span>
        </div>
        <div className="flex items-center gap-1.5 text-[11px] font-mono text-emerald-400 bg-emerald-500/10 px-2 py-0.5 rounded-full border border-emerald-500/20">
          <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-ping" />
          <span>0ms Hot-Reload</span>
        </div>
      </div>

      {/* Mô phỏng Browser / Client Exam Sandbox */}
      <div
        className="bg-slate-950 border border-slate-800 p-5 shadow-inner transition-all space-y-4"
        style={{
          borderRadius: config.borderRadius,
          fontFamily: config.fontFamily,
        }}
      >
        {/* Mock Exam Header */}
        <div className="flex items-center justify-between pb-3 border-b border-slate-850">
          <div className="flex items-center gap-3">
            <div
              className="w-8 h-8 flex items-center justify-center font-bold text-white shadow-md"
              style={{
                backgroundColor: config.primaryColor,
                borderRadius: config.borderRadius,
              }}
            >
              <Building className="w-4 h-4" />
            </div>
            <div>
              <div className="text-xs font-bold text-white">
                {config.organizationName || 'Tên Tổ Chức'}
              </div>
              <div className="text-[10px] text-slate-500 flex items-center gap-1">
                <Globe className="w-3 h-3" />
                <span>{config.customDomain || 'exam.portal.internal'}</span>
              </div>
            </div>
          </div>

          <div
            className="text-[11px] font-semibold px-2.5 py-1 text-white shadow-sm"
            style={{
              backgroundColor: config.accentColor,
              borderRadius: config.borderRadius,
            }}
          >
            Sát hạch 2026
          </div>
        </div>

        {/* Mock Question Card */}
        <div className="space-y-3">
          <div className="flex items-center justify-between text-xs text-slate-400">
            <span>Câu hỏi 01 / 50</span>
            <span className="flex items-center gap-1 text-emerald-400">
              <ShieldCheck className="w-3.5 h-3.5" /> Chế độ bảo mật cao
            </span>
          </div>

          <h3 className="text-sm font-semibold text-slate-100 leading-snug">
            {t('sampleQuizCardTitle')}: Theo hiệp ước Basel III, tỷ lệ an toàn vốn tối thiểu (CAR) quy định là bao nhiêu?
          </h3>
          <p className="text-xs text-slate-400 leading-relaxed">
            {t('sampleQuizCardDesc')}
          </p>

          {/* Mock Options */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-1">
            {[
              { key: 'A', text: 'Tối thiểu 6.0%' },
              { key: 'B', text: 'Tối thiểu 8.0%' },
              { key: 'C', text: 'Tối thiểu 10.5%' },
              { key: 'D', text: 'Tối thiểu 12.0%' },
            ].map((opt) => {
              const isSelected = selectedOption === opt.key;
              return (
                <div
                  key={opt.key}
                  onClick={() => setSelectedOption(opt.key)}
                  className="p-3 border text-xs cursor-pointer transition-all flex items-center justify-between select-none"
                  style={{
                    borderRadius: config.borderRadius,
                    borderColor: isSelected ? config.primaryColor : 'rgba(51, 65, 85, 0.6)',
                    backgroundColor: isSelected ? `${config.primaryColor}20` : 'rgba(15, 23, 42, 0.4)',
                    color: isSelected ? '#ffffff' : '#94a3b8',
                  }}
                >
                  <span className="font-semibold">{opt.key}. {opt.text}</span>
                  {isSelected && (
                    <CheckCircle2
                      className="w-4 h-4 shrink-0"
                      style={{ color: config.primaryColor }}
                    />
                  )}
                </div>
              );
            })}
          </div>

          {/* Mock Action Button */}
          <div className="pt-2 flex justify-end">
            <button
              type="button"
              className="px-4 py-2 text-xs font-bold text-white shadow-lg transition-transform active:scale-95"
              style={{
                backgroundColor: config.primaryColor,
                borderRadius: config.borderRadius,
              }}
            >
              {t('sampleButton')}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
