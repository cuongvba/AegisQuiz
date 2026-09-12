import React from 'react';
import { Car, ShieldAlert } from 'lucide-react';
import type { ExamCategoryPreset } from '@/types/quiz';

interface Props {
  activePreset: ExamCategoryPreset | null;
  onSelectPreset: (preset: ExamCategoryPreset, count: number, mins: number) => void;
}

export const DrivingPresetWidget: React.FC<Props> = ({
  activePreset,
  onSelectPreset,
}) => {
  return (
    <div className="my-3 rounded-2xl border-2 border-emerald-500/20 bg-gradient-to-r from-emerald-50/80 via-teal-50/60 to-sky-50/80 p-4 shadow-sm transition">
      <div className="flex flex-wrap items-center justify-between gap-2 mb-3">
        <div className="flex items-center gap-2">
          <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-emerald-600 text-white shadow-xs">
            <Car className="h-4 w-4" />
          </div>
          <div>
            <h4 className="text-sm font-black text-slate-900 tracking-tight flex items-center gap-1.5">
              Ma Trận Sát Hạch Lái Xe Chuẩn Quốc Gia
              <span className="rounded-full bg-emerald-100 text-emerald-800 text-[10px] px-2 py-0.5 font-bold">
                Cục Đường Bộ VN
              </span>
            </h4>
            <p className="text-xs text-slate-500">
              1-chạm nạp chuẩn phòng thi: thời gian, định mức câu hỏi & tự động kích hoạt Bộ Lọc Điểm Liệt
            </p>
          </div>
        </div>

        {activePreset && (
          <span className="inline-flex items-center gap-1 rounded-full bg-emerald-600 px-3 py-1 text-xs font-black text-white shadow-xs animate-pulse">
            <span>✓ Đang áp dụng bộ đề mẫu</span>
          </span>
        )}
      </div>

      <div className="grid grid-cols-2 gap-2.5 sm:grid-cols-3 lg:grid-cols-5">
        <button
          type="button"
          onClick={() => onSelectPreset('gplx_a1', 25, 19)}
          className={`group rounded-xl border p-2.5 text-left transition ${
            activePreset === 'gplx_a1'
              ? 'border-emerald-600 bg-white shadow-md ring-2 ring-emerald-500'
              : 'border-slate-200/90 bg-white/80 hover:bg-white hover:border-emerald-300 hover:shadow-xs'
          }`}
        >
          <div className="flex items-center justify-between">
            <span className="font-extrabold text-xs text-slate-900 group-hover:text-emerald-700 transition-colors">
              🏍️ Hạng A1
            </span>
            <span className="text-[10px] font-bold text-emerald-700 bg-emerald-100 px-1.5 py-0.5 rounded">
              19 phút
            </span>
          </div>
          <p className="mt-1 text-[11px] text-slate-600">25 câu (Đạt 21/25 + 0 sai điểm liệt)</p>
        </button>

        <button
          type="button"
          onClick={() => onSelectPreset('gplx_b1', 30, 20)}
          className={`group rounded-xl border p-2.5 text-left transition ${
            activePreset === 'gplx_b1'
              ? 'border-emerald-600 bg-white shadow-md ring-2 ring-emerald-500'
              : 'border-slate-200/90 bg-white/80 hover:bg-white hover:border-emerald-300 hover:shadow-xs'
          }`}
        >
          <div className="flex items-center justify-between">
            <span className="font-extrabold text-xs text-slate-900 group-hover:text-emerald-700 transition-colors">
              🚗 Hạng B1
            </span>
            <span className="text-[10px] font-bold text-emerald-700 bg-emerald-100 px-1.5 py-0.5 rounded">
              20 phút
            </span>
          </div>
          <p className="mt-1 text-[11px] text-slate-600">30 câu (Đạt 27/30 + 0 sai điểm liệt)</p>
        </button>

        <button
          type="button"
          onClick={() => onSelectPreset('gplx_b2', 35, 22)}
          className={`group rounded-xl border p-2.5 text-left transition ${
            activePreset === 'gplx_b2'
              ? 'border-emerald-600 bg-white shadow-md ring-2 ring-emerald-500'
              : 'border-slate-200/90 bg-white/80 hover:bg-white hover:border-emerald-300 hover:shadow-xs'
          }`}
        >
          <div className="flex items-center justify-between">
            <span className="font-extrabold text-xs text-slate-900 group-hover:text-emerald-700 transition-colors">
              🚙 Hạng B2
            </span>
            <span className="text-[10px] font-bold text-emerald-700 bg-emerald-100 px-1.5 py-0.5 rounded">
              22 phút
            </span>
          </div>
          <p className="mt-1 text-[11px] text-slate-600">35 câu (Đạt 32/35 + 0 sai điểm liệt)</p>
        </button>

        <button
          type="button"
          onClick={() => onSelectPreset('gplx_c', 40, 24)}
          className={`group rounded-xl border p-2.5 text-left transition ${
            activePreset === 'gplx_c'
              ? 'border-emerald-600 bg-white shadow-md ring-2 ring-emerald-500'
              : 'border-slate-200/90 bg-white/80 hover:bg-white hover:border-emerald-300 hover:shadow-xs'
          }`}
        >
          <div className="flex items-center justify-between">
            <span className="font-extrabold text-xs text-slate-900 group-hover:text-emerald-700 transition-colors">
              🚛 Hạng C
            </span>
            <span className="text-[10px] font-bold text-emerald-700 bg-emerald-100 px-1.5 py-0.5 rounded">
              24 phút
            </span>
          </div>
          <p className="mt-1 text-[11px] text-slate-600">40 câu (Đạt 36/40 + 0 sai điểm liệt)</p>
        </button>

        <button
          type="button"
          onClick={() => onSelectPreset('gplx_fatal_only', 60, 30)}
          className={`group rounded-xl border p-2.5 text-left transition ${
            activePreset === 'gplx_fatal_only'
              ? 'border-rose-600 bg-white shadow-md ring-2 ring-rose-500'
              : 'border-rose-200 bg-rose-50/80 hover:bg-rose-100 hover:border-rose-400 hover:shadow-xs'
          }`}
        >
          <div className="flex items-center justify-between">
            <span className="font-extrabold text-xs text-rose-700 flex items-center gap-1">
              <ShieldAlert className="h-3.5 w-3.5" /> 60 Điểm Liệt
            </span>
            <span className="text-[10px] font-bold text-rose-700 bg-rose-100 px-1.5 py-0.5 rounded">
              Cốt Tử
            </span>
          </div>
          <p className="mt-1 text-[11px] text-rose-800 font-semibold">Chống Trượt 100% (Sai 1 câu là trượt)</p>
        </button>
      </div>
    </div>
  );
};
