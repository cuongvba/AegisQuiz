import React from 'react';
import { Activity } from 'lucide-react';

interface SymposiumPhaseProgressProps {
  activePhaseIndex: number;
  isDone: boolean;
  isConvening: boolean;
}

const PHASES = [
  '1. Đặt Vấn Đề',
  '2. Vấn Đáp Socratic',
  '3. Phản Biện Đối Kháng',
  '4. Hợp Nhất Metatheory',
  '5. Chấm Ma Trận Rubric',
  '6. Tuyên Ngôn Chân Lý'
];

export function SymposiumPhaseProgress({
  activePhaseIndex,
  isDone,
  isConvening
}: SymposiumPhaseProgressProps) {
  return (
    <div className="p-4 sm:p-5 rounded-2xl bg-slate-900/90 border border-slate-800 shadow-xl space-y-3">
      <div className="flex items-center justify-between text-xs font-bold">
        <span className="text-cyan-400 uppercase tracking-widest flex items-center gap-2">
          <Activity size={14} /> Chuỗi Biện Chứng Nhận Thức 6 Bước
        </span>
        <span className="text-slate-400">
          {isDone ? 'Đã hoàn tất 100%' : `Đang xử lý Pha ${activePhaseIndex}/6`}
        </span>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-6 gap-2">
        {PHASES.map((name, i) => {
          const stepNum = i + 1;
          const stepDone = isDone || activePhaseIndex > stepNum;
          const stepCurrent = isConvening && activePhaseIndex === stepNum;

          return (
            <div
              key={i}
              className={`p-2.5 rounded-xl border text-center transition-all ${
                stepDone
                  ? 'bg-cyan-950/40 border-cyan-500/50 text-cyan-300'
                  : stepCurrent
                  ? 'bg-amber-950/40 border-amber-500/50 text-amber-300 animate-pulse'
                  : 'bg-slate-950 border-slate-800 text-slate-500'
              }`}
            >
              <div className="text-[10px] font-mono">{stepNum}/6</div>
              <div className="text-[11px] font-bold line-clamp-1 mt-0.5">{name}</div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
