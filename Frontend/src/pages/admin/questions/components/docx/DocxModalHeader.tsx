import { Sparkles } from 'lucide-react';

interface DocxModalHeaderProps {
  totalQuestions: number;
  processingDurationMs?: number | null;
  singleMultiCount: number;
  essayCount: number;
  ingestionSource?: 'client_edge' | 'cloud_server' | 'cloud_fallback';
}

export function DocxModalHeader({
  totalQuestions,
  processingDurationMs,
  singleMultiCount,
  essayCount,
  ingestionSource = 'client_edge',
}: DocxModalHeaderProps) {
  return (
    <div className="flex items-center justify-between pb-2 mb-2 border-b border-slate-800/80 pr-8 shrink-0">
      <div className="flex items-center gap-2.5">
        <span className="w-7 h-7 rounded-lg bg-gradient-to-br from-purple-500/20 to-blue-500/20 border border-purple-500/30 flex items-center justify-center text-purple-300">
          <Sparkles size={13} />
        </span>
        <h2 className="text-base sm:text-lg font-black text-white tracking-tight flex items-center gap-2">
          Xem Trước & Duyệt Câu Hỏi
          <span className="px-2 py-0.5 text-xs font-black rounded-full bg-blue-500/20 text-blue-300 border border-blue-500/30">
            {totalQuestions} câu
          </span>
        </h2>
        <span className="hidden lg:inline text-[11px] text-slate-500 border-l border-slate-800 pl-2.5">
          Word / Excel / PDF / Gemini AI
        </span>
      </div>

      <div className="flex items-center gap-2 text-xs">
        {ingestionSource === 'client_edge' && (
          <span
            className="px-2.5 py-0.5 bg-purple-950/70 text-purple-300 border border-purple-500/40 rounded-lg text-[11px] font-bold flex items-center gap-1 shadow-sm"
            title="Được bóc tách siêu tốc bằng RAM trình duyệt Client Edge"
          >
            ⚡ Edge Ingestion ({processingDurationMs ?? 0}ms)
          </span>
        )}
        {ingestionSource === 'cloud_fallback' && (
          <span
            className="px-2.5 py-0.5 bg-amber-950/70 text-amber-300 border border-amber-500/40 rounded-lg text-[11px] font-bold flex items-center gap-1 shadow-sm"
            title="Tự động chuyển tiếp lên Cloud Server vì cấu trúc tài liệu nâng cao"
          >
            ☁️ Cloud Fallback ({processingDurationMs ?? 0}ms)
          </span>
        )}
        {ingestionSource === 'cloud_server' && (
          <span
            className="px-2.5 py-0.5 bg-blue-950/70 text-blue-300 border border-blue-500/40 rounded-lg text-[11px] font-bold flex items-center gap-1 shadow-sm"
            title="Xử lý trên Cloud Server Engine"
          >
            ☁️ Cloud Server ({processingDurationMs ?? 0}ms)
          </span>
        )}
        {typeof processingDurationMs === 'number' && processingDurationMs >= 0 && ingestionSource === undefined && (
          <span
            className="px-2.5 py-0.5 bg-emerald-950/70 text-emerald-300 border border-emerald-500/40 rounded-lg text-[11px] font-bold font-mono flex items-center gap-1"
            title="Tổng thời gian đọc và phân tích cấu trúc tệp"
          >
            ⏱️ Thời gian xử lý: {(processingDurationMs / 1000).toFixed(2)}s ({processingDurationMs}ms)
          </span>
        )}
        <span className="px-2 py-0.5 bg-blue-950/60 text-blue-300 border border-blue-800/40 rounded-lg text-[11px] font-bold">
          Trắc nghiệm: {singleMultiCount}
        </span>
        <span className="px-2 py-0.5 bg-amber-950/60 text-amber-300 border border-amber-800/40 rounded-lg text-[11px] font-bold">
          Tự luận: {essayCount}
        </span>
      </div>
    </div>
  );
}
