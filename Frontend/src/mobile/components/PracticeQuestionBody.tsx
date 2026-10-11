import React from 'react';
import { AlertTriangle, Sparkles } from 'lucide-react';
import { PracticeQuestionItem } from '../types/mobile-practice.types';

interface PracticeQuestionBodyProps {
  currentQ: PracticeQuestionItem;
  currentIdx: number;
  selectedKey?: string;
  showExplanation: boolean;
  primaryColor?: string;
  onSelectOption: (optKey: string) => void;
  onOpenStory: () => void;
}

export const PracticeQuestionBody: React.FC<PracticeQuestionBodyProps> = ({
  currentQ,
  currentIdx,
  selectedKey,
  showExplanation,
  primaryColor,
  onSelectOption,
  onOpenStory,
}) => {
  const renderQuestionContent = (text: string) => {
    const imgRegex = /!\[(.*?)\]\((data:image\/[^;]+;base64,[^)]+|https?:\/\/[^)]+)\)/g;
    const parts: React.ReactNode[] = [];
    let lastIndex = 0;
    let match;

    while ((match = imgRegex.exec(text)) !== null) {
      if (match.index > lastIndex) {
        parts.push(<span key={lastIndex}>{text.substring(lastIndex, match.index)}</span>);
      }
      parts.push(
        <div key={match.index} className="my-3 flex justify-center">
          <img
            src={match[2]}
            alt={match[1] || 'Hình ảnh minh họa câu hỏi'}
            className="max-h-52 rounded-xl border border-slate-700 object-contain shadow-lg bg-slate-900/80 p-1"
          />
        </div>
      );
      lastIndex = match.index + match[0].length;
    }

    if (lastIndex < text.length) {
      parts.push(<span key={lastIndex}>{text.substring(lastIndex)}</span>);
    }

    return parts.length > 0 ? parts : text;
  };

  return (
    <div className="flex-1 overflow-y-auto p-4 space-y-4">
      {currentQ.isFatal && (
        <div className="p-2.5 rounded-xl bg-red-950/60 border border-red-500/50 flex items-center gap-2 text-xs text-red-200">
          <AlertTriangle size={15} className="text-red-400 shrink-0 animate-bounce" />
          <span className="font-bold">CÂU ĐIỂM LIỆT: Trả lời sai sẽ bị tính điểm liệt trượt sát hạch!</span>
        </div>
      )}

      <div className="p-4 rounded-2xl bg-slate-900/60 border border-slate-800">
        <div className="text-sm font-bold text-white leading-relaxed">
          {renderQuestionContent(currentQ.content)}
        </div>
      </div>

      {/* Danh sách phương án A, B, C, D */}
      <div className="space-y-2.5">
        {currentQ.options.map((opt, optIdx) => {
          const displayLabel = ['A', 'B', 'C', 'D', 'E'][optIdx] || opt.key;
          const textContent = typeof opt === 'string' ? opt : (opt.text || (opt as any).content || String(opt));
          const isChosen = !!selectedKey && (selectedKey === opt.key || selectedKey === displayLabel);
          return (
            <button
              key={`${displayLabel}-${optIdx}`}
              type="button"
              onClick={() => onSelectOption(opt.key || displayLabel)}
              className={`w-full text-left p-3.5 rounded-2xl border text-xs font-bold transition-all cursor-pointer flex items-start gap-3 active:scale-98 ${
                isChosen
                  ? 'bg-slate-900 border-slate-700 ring-2'
                  : 'bg-slate-950 border-slate-800 hover:bg-slate-900/50'
              }`}
              style={{ borderColor: isChosen ? primaryColor : undefined }}
            >
              <div
                className={`w-7 h-7 rounded-xl flex items-center justify-center text-xs font-black shrink-0 transition-colors ${
                  isChosen ? 'text-slate-950 shadow-md font-black' : 'bg-slate-900 text-slate-300 border border-slate-800'
                }`}
                style={{ backgroundColor: isChosen ? primaryColor : undefined }}
              >
                {displayLabel}
              </div>
              <div className="flex-1 pt-1 text-slate-200 leading-normal">{textContent}</div>
            </button>
          );
        })}
      </div>

      {/* Giải thích thông minh & Micro-Lesson Story */}
      {showExplanation && (
        <div className="p-3.5 rounded-2xl bg-purple-950/40 border border-purple-500/40 space-y-2 animate-in fade-in duration-200">
          <div className="flex items-center justify-between text-xs font-bold text-purple-300">
            <span className="flex items-center gap-1.5">
              <Sparkles size={14} /> Giải thích chuẩn:
            </span>
            <button
              type="button"
              onClick={onOpenStory}
              className="px-2.5 py-1 rounded-full bg-gradient-to-r from-amber-400 to-pink-500 text-slate-950 text-[10px] font-black flex items-center gap-1 active:scale-95 transition-transform cursor-pointer shadow-sm"
            >
              <Sparkles size={11} /> Bài Giảng Slice 🎬
            </button>
          </div>
          <p className="text-xs text-slate-300 leading-relaxed">{currentQ.explanation}</p>
        </div>
      )}
    </div>
  );
};
