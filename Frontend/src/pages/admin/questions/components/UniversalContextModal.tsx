import React from 'react';
import { BookOpen, X } from 'lucide-react';
import { MathRenderer } from '@/components/common/MathRenderer';

interface UniversalContextModalProps {
  context: { title: string; content: string } | null;
  onClose: () => void;
}

export const UniversalContextModal: React.FC<UniversalContextModalProps> = ({
  context,
  onClose,
}) => {
  if (!context) return null;

  const wordCount = context.content.split(/\s+/).filter(Boolean).length;

  return (
    <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-md z-50 flex items-center justify-center p-4">
      <div className="w-full max-w-3xl bg-slate-900 border border-slate-750 rounded-3xl p-6 sm:p-8 shadow-2xl relative space-y-4 max-h-[85vh] flex flex-col justify-between animate-in fade-in zoom-in-95">
        <button
          onClick={onClose}
          className="absolute top-5 right-5 text-slate-400 hover:text-white p-1.5 hover:bg-slate-800 rounded-xl transition-colors cursor-pointer"
        >
          <X size={18} />
        </button>
        <div className="space-y-1">
          <div className="inline-flex items-center gap-1.5 px-2.5 py-0.5 bg-indigo-950/60 text-indigo-300 border border-indigo-500/30 rounded-full text-[10px] font-black uppercase tracking-wider">
            <BookOpen size={11} /> Universal Shared Stimulus / Context
          </div>
          <h3 className="text-xl font-black text-transparent bg-clip-text bg-gradient-to-r from-indigo-300 via-purple-300 to-pink-300">
            {context.title}
          </h3>
          <p className="text-xs text-slate-400">
            Đoạn văn / Ngữ cảnh bài thi dùng chung cho các câu hỏi liên quan. Lưu trữ tối ưu một lần duy nhất trong cơ sở dữ liệu.
          </p>
        </div>
        <div className="flex-1 overflow-y-auto bg-slate-950/70 border border-indigo-900/40 rounded-2xl p-5 text-sm text-slate-200 leading-relaxed font-sans shadow-inner whitespace-pre-wrap">
          <MathRenderer content={context.content} />
        </div>
        <div className="flex items-center justify-between pt-2 border-t border-slate-800 text-xs text-slate-400">
          <span>Độ dài: {context.content.length} ký tự (~{wordCount} từ)</span>
          <button
            onClick={onClose}
            className="px-4 py-2 bg-slate-800 hover:bg-slate-750 text-white rounded-xl font-bold transition-all text-xs cursor-pointer"
          >
            Đóng
          </button>
        </div>
      </div>
    </div>
  );
};
