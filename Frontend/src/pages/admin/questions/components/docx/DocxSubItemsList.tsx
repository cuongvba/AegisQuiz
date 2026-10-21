import React from 'react';
import { Check, X, BookOpen, Award } from 'lucide-react';
import type { QuestionSubItemDto } from '@/types/quiz';
import { MathRenderer } from '../../../../../components/common/MathRenderer';

interface DocxSubItemsListProps {
  subItems?: QuestionSubItemDto[];
  questionIdx: number;
  onUpdateSubItem: (qIdx: number, subIdx: number, field: string, value: any) => void;
}

export const DocxSubItemsList: React.FC<DocxSubItemsListProps> = ({
  subItems,
  questionIdx,
  onUpdateSubItem,
}) => {
  if (!subItems || subItems.length === 0) return null;

  return (
    <div className="space-y-2.5 bg-slate-950/60 border border-purple-500/30 rounded-2xl p-3.5">
      <div className="flex items-center justify-between border-b border-purple-900/40 pb-2">
        <label className="text-[10px] font-black uppercase text-purple-300 tracking-wide flex items-center gap-1.5">
          <span>🎯 Phân tầng Ý con & Barem vi mô ({subItems.length} ý)</span>
          <span className="text-[9px] bg-purple-950 text-purple-400 px-1.5 py-0.2 rounded border border-purple-800/40 font-mono">
            SGI v5.6
          </span>
        </label>
        <span className="text-[10px] text-slate-400 italic">
          Tư liệu kịch bản hoạt họa / Storyboard phân nhánh
        </span>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {subItems.map((sub, sIdx) => {
          const isTrue = sub.expectedAnswer?.toLowerCase().startsWith('đ') || 
                        sub.expectedAnswer?.toLowerCase().startsWith('d') || 
                        sub.expectedAnswer?.toLowerCase().startsWith('t');
          const isFalse = sub.expectedAnswer?.toLowerCase().startsWith('s') || 
                         sub.expectedAnswer?.toLowerCase().startsWith('f');

          return (
            <div
              key={sub.key || sIdx}
              className={`p-3 rounded-xl border flex flex-col justify-between gap-2 transition-all ${
                isTrue
                  ? 'bg-emerald-950/20 border-emerald-500/40'
                  : isFalse
                  ? 'bg-rose-950/20 border-rose-500/40'
                  : 'bg-slate-900/60 border-slate-800'
              }`}
            >
              {/* Header của Ý con */}
              <div className="flex items-center justify-between gap-2">
                <div className="flex items-center gap-1.5">
                  <span className="w-5 h-5 rounded-md bg-purple-600/20 border border-purple-500/40 text-purple-300 font-black flex items-center justify-center text-[11px] font-mono shrink-0">
                    {sub.key})
                  </span>
                  <span className="text-[11px] font-bold text-slate-200">
                    Ý {sub.key}
                  </span>
                </div>

                {/* Nút chuyển đổi nhanh Đúng / Sai */}
                <div className="flex items-center gap-1">
                  <button
                    type="button"
                    onClick={() => onUpdateSubItem(questionIdx, sIdx, 'expectedAnswer', 'Đúng')}
                    className={`px-2 py-0.5 rounded-lg text-[10px] font-bold flex items-center gap-0.5 cursor-pointer transition-all ${
                      isTrue
                        ? 'bg-emerald-600 text-white shadow-sm'
                        : 'bg-slate-800 text-slate-400 hover:text-white'
                    }`}
                  >
                    <Check size={10} /> Đúng
                  </button>
                  <button
                    type="button"
                    onClick={() => onUpdateSubItem(questionIdx, sIdx, 'expectedAnswer', 'Sai')}
                    className={`px-2 py-0.5 rounded-lg text-[10px] font-bold flex items-center gap-0.5 cursor-pointer transition-all ${
                      isFalse
                        ? 'bg-rose-600 text-white shadow-sm'
                        : 'bg-slate-800 text-slate-400 hover:text-white'
                    }`}
                  >
                    <X size={10} /> Sai
                  </button>
                  {/* Barem điểm */}
                  <div className="flex items-center gap-0.5 ml-1 bg-slate-950 px-1.5 py-0.5 rounded border border-slate-800 text-[10px] text-slate-400 font-mono" title="Thang điểm">
                    <Award size={10} className="text-amber-400" />
                    <input
                      type="number"
                      step="0.05"
                      value={sub.maxScore ?? 0.25}
                      onChange={e => onUpdateSubItem(questionIdx, sIdx, 'maxScore', parseFloat(e.target.value) || 0)}
                      className="w-10 bg-transparent text-right font-bold text-amber-300 focus:outline-none"
                    />
                    <span>đ</span>
                  </div>
                </div>
              </div>

              {/* Nội dung ý con */}
              <div className="space-y-1">
                <div className="min-h-[28px] text-xs text-slate-300">
                  <MathRenderer content={sub.content} />
                </div>
                <input
                  type="text"
                  value={sub.content}
                  onChange={e => onUpdateSubItem(questionIdx, sIdx, 'content', e.target.value)}
                  className="w-full bg-slate-950/70 border border-slate-800 rounded-lg px-2.5 py-1 text-[11px] text-slate-300 focus:outline-none focus:border-purple-500 font-sans"
                  placeholder="Nội dung mệnh đề / khẳng định..."
                />
              </div>

              {/* Lời giải vi mô phân tách theo từng ý */}
              {sub.explanation && (
                <div className="pt-1.5 border-t border-slate-850/80 space-y-1">
                  <div className="text-[10px] font-bold text-cyan-400 flex items-center gap-1">
                    <BookOpen size={10} />
                    <span>Lời giải ý {sub.key}:</span>
                  </div>
                  <div className="p-2 bg-slate-950/80 rounded-lg border border-slate-850 text-[11px] text-slate-300">
                    <MathRenderer content={sub.explanation} />
                  </div>
                  <input
                    type="text"
                    value={sub.explanation}
                    onChange={e => onUpdateSubItem(questionIdx, sIdx, 'explanation', e.target.value)}
                    className="w-full bg-slate-950/70 border border-slate-800 rounded-lg px-2 py-0.5 text-[10px] text-slate-400 focus:outline-none focus:text-white"
                    placeholder="Biên tập lời giải ý này..."
                  />
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};
