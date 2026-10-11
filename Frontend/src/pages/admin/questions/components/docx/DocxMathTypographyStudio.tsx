import { SlidersHorizontal, RotateCcw, X } from 'lucide-react';

interface DocxMathTypographyStudioProps {
  showMathStudio: boolean;
  setShowMathStudio: (val: boolean) => void;
  mathSettings: any;
  updateMathSettings: (settings: any) => void;
  availablePresets: any[];
  currentPreset: string | null;
  applyPreset: (presetId: string) => void;
  applySubjectProfile: (subjectId: string) => void;
  resetMathDefaults: () => void;
}

export function DocxMathTypographyStudio({
  showMathStudio,
  setShowMathStudio,
  mathSettings,
  updateMathSettings,
  availablePresets,
  currentPreset,
  applyPreset,
  applySubjectProfile,
  resetMathDefaults,
}: DocxMathTypographyStudioProps) {
  if (!showMathStudio) return null;

  return (
    <div className="bg-slate-950/90 border border-blue-500/30 rounded-2xl p-4 shadow-xl shadow-black/40 space-y-3 animate-in fade-in duration-150 mb-2.5">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-800/80 pb-2.5">
        <div className="flex items-center gap-2">
          <span className="p-1.5 rounded-lg bg-blue-500/20 text-blue-400">
            <SlidersHorizontal size={16} />
          </span>
          <div>
            <h4 className="text-xs font-black text-slate-100 uppercase tracking-wider flex items-center gap-2">
              Math Typography Studio & Dynamic Engine 2026
              <span className="text-[10px] font-semibold bg-emerald-950 text-emerald-400 border border-emerald-800 px-2 py-0.5 rounded-full">
                ⚡ Động hóa 100%
              </span>
            </h4>
            <p className="text-[11px] text-slate-400">
              Hiệu chỉnh trực tiếp cỡ chữ, giãn cách ký tự và tỷ lệ công thức SVG MathType - Phản hồi tức thì
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={resetMathDefaults}
            className="px-2.5 py-1 text-xs font-semibold text-slate-400 hover:text-white bg-slate-900 hover:bg-slate-800 border border-slate-700/60 rounded-lg transition-colors flex items-center gap-1 cursor-pointer"
          >
            <RotateCcw size={12} /> Khôi phục mặc định
          </button>
          <button
            type="button"
            onClick={() => setShowMathStudio(false)}
            className="p-1 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 cursor-pointer"
          >
            <X size={14} />
          </button>
        </div>
      </div>

      {/* Presets & Subjects */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3 pt-1">
        {/* Cấu hình dựng sẵn */}
        <div className="space-y-1.5">
          <label className="text-[11px] font-bold text-slate-300">
            🎯 Cấu hình dựng sẵn (Presets)
          </label>
          <div className="flex flex-wrap gap-1.5">
            {availablePresets.map(preset => (
              <button
                key={preset.id}
                type="button"
                onClick={() => applyPreset(preset.id)}
                className={`px-2.5 py-1 text-xs font-bold rounded-lg border transition-all cursor-pointer ${
                  currentPreset === preset.id
                    ? 'bg-blue-600 border-blue-400 text-white shadow-sm'
                    : 'bg-slate-900 border-slate-800 text-slate-300 hover:border-slate-700 hover:text-white'
                }`}
              >
                {preset.name}
              </button>
            ))}
          </div>
        </div>

        {/* Môn học chuyên ngành */}
        <div className="space-y-1.5">
          <label className="text-[11px] font-bold text-slate-300">
            🔬 Cấu hình tối ưu theo môn
          </label>
          <div className="flex flex-wrap gap-1.5">
            {[
              { id: 'math', label: '📐 Toán học' },
              { id: 'physics', label: '⚡ Vật lý' },
              { id: 'chemistry', label: '🧪 Hóa học' },
              { id: 'banking', label: '🏦 Tài chính / Lãi suất' },
            ].map(sub => (
              <button
                key={sub.id}
                type="button"
                onClick={() => applySubjectProfile(sub.id)}
                className="px-2.5 py-1 text-xs font-medium rounded-lg bg-slate-900 border border-slate-800 text-slate-300 hover:border-slate-700 hover:text-white transition-all cursor-pointer"
              >
                {sub.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Sliders hiệu chỉnh vi mô */}
      <div className="grid grid-cols-1 sm:grid-cols-4 gap-3 pt-1 border-t border-slate-800/60">
        <div className="space-y-1">
          <div className="flex justify-between text-[11px]">
            <span className="text-slate-300 font-bold">Cỡ chữ văn bản:</span>
            <span className="text-blue-400 font-mono font-bold">{mathSettings.baseFontUnit}rem</span>
          </div>
          <input
            type="range"
            min="0.8"
            max="1.8"
            step="0.05"
            value={mathSettings.baseFontUnit}
            onChange={e => updateMathSettings({ baseFontUnit: parseFloat(e.target.value) })}
            className="w-full accent-blue-500 cursor-pointer h-1.5 bg-slate-800 rounded-lg"
          />
          <div className="flex justify-between text-[9px] text-slate-500">
            <span>1.0x (Nhỏ)</span>
            <span>1.45x (Chuẩn)</span>
            <span>1.8x (Lớn)</span>
          </div>
        </div>

        <div className="space-y-1">
          <div className="flex justify-between text-[11px]">
            <span className="text-slate-300 font-bold">Giãn cách ký tự:</span>
            <span className="text-emerald-400 font-mono font-bold">{mathSettings.letterSpacing}</span>
          </div>
          <input
            type="range"
            min="0.00"
            max="0.25"
            step="0.01"
            value={parseFloat(mathSettings.letterSpacing) || 0.1}
            onChange={e => updateMathSettings({ letterSpacing: `${parseFloat(e.target.value).toFixed(2)}em` })}
            className="w-full accent-emerald-500 cursor-pointer h-1.5 bg-slate-800 rounded-lg"
          />
          <div className="flex justify-between text-[9px] text-slate-500">
            <span>0.00em (Khít)</span>
            <span>0.10em (Chuẩn)</span>
            <span>0.25em (Rộng)</span>
          </div>
        </div>

        <div className="space-y-1">
          <div className="flex justify-between text-[11px]">
            <span className="text-slate-300 font-bold">Độ dày nét Vector:</span>
            <span className="text-amber-400 font-mono font-bold">{(mathSettings.vectorStrokeWidthRatio * 100).toFixed(0)}%</span>
          </div>
          <input
            type="range"
            min="0.04"
            max="0.12"
            step="0.01"
            value={mathSettings.vectorStrokeWidthRatio}
            onChange={e => updateMathSettings({ vectorStrokeWidthRatio: parseFloat(e.target.value) })}
            className="w-full accent-amber-500 cursor-pointer h-1.5 bg-slate-800 rounded-lg"
          />
          <div className="flex justify-between text-[9px] text-slate-500">
            <span>4% (Mảnh)</span>
            <span>8% (Chuẩn)</span>
            <span>12% (Đậm)</span>
          </div>
        </div>

        <div className="space-y-1">
          <div className="flex justify-between text-[11px]">
            <span className="text-slate-300 font-bold">Độ cong ngoặc đơn ( ):</span>
            <span className="text-purple-400 font-mono font-bold">{(mathSettings.parenthesisBowRatio * 100).toFixed(0)}%</span>
          </div>
          <input
            type="range"
            min="0.08"
            max="0.25"
            step="0.01"
            value={mathSettings.parenthesisBowRatio}
            onChange={e => updateMathSettings({ parenthesisBowRatio: parseFloat(e.target.value) })}
            className="w-full accent-purple-500 cursor-pointer h-1.5 bg-slate-800 rounded-lg"
          />
          <div className="flex justify-between text-[9px] text-slate-500">
            <span>8% (Thẳng)</span>
            <span>16% (Chuẩn)</span>
            <span>25% (Cong)</span>
          </div>
        </div>
      </div>
    </div>
  );
}
