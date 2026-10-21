import { Check, Link2, Undo2 } from 'lucide-react';
import { useState } from 'react';
import { parseMatchingPairs, getDisplayOptions } from '@/lib/quiz-helpers';
import type { QuestionWidgetProps } from './types';
import { playSynthGameSound } from './sound-effects';

const PAIR_PALETTE = [
  { border: 'border-cyan-400', bg: 'bg-cyan-50/70', badge: 'bg-cyan-500', glow: 'shadow-cyan-100' },
  { border: 'border-emerald-400', bg: 'bg-emerald-50/70', badge: 'bg-emerald-500', glow: 'shadow-emerald-100' },
  { border: 'border-amber-400', bg: 'bg-amber-50/70', badge: 'bg-amber-500', glow: 'shadow-amber-100' },
  { border: 'border-purple-400', bg: 'bg-purple-50/70', badge: 'bg-purple-500', glow: 'shadow-purple-100' },
  { border: 'border-rose-400', bg: 'bg-rose-50/70', badge: 'bg-rose-500', glow: 'shadow-rose-100' },
  { border: 'border-teal-400', bg: 'bg-teal-50/70', badge: 'bg-teal-500', glow: 'shadow-teal-100' },
];

export default function MatchingWidget({
  question,
  answer,
  onUpdate,
  disabled,
  soundEnabled,
  t
}: QuestionWidgetProps) {
  const displayOptions = getDisplayOptions(question);
  const pairs = parseMatchingPairs(displayOptions, question);
  const [activeLeftForMatch, setActiveLeftForMatch] = useState<string | null>(null);

  if (pairs.length === 0) {
    return (
      <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
        {t('matchingFormatHint') || 'Định dạng câu hỏi ghép cặp: "Mục A - Mục B"'}
      </div>
    );
  }

  const leftPool = question.matchingLeftOptions && question.matchingLeftOptions.length > 0
    ? question.matchingLeftOptions
    : pairs.map((x) => x.left);
  const rightPool = question.matchingRightOptions && question.matchingRightOptions.length > 0
    ? question.matchingRightOptions
    : pairs.map((x) => x.right);

  const matchingMap = answer?.matching ?? {};
  const matchedCount = Object.keys(matchingMap).filter((k) => matchingMap[k]).length;
  const totalPairs = leftPool.length;
  const isCompleted = matchedCount === totalPairs && totalPairs > 0;

  const assignedRightToColor = new Map<string, typeof PAIR_PALETTE[0]>();
  const assignedLeftToColor = new Map<string, typeof PAIR_PALETTE[0]>();
  let colorIdx = 0;
  for (const leftKey of Object.keys(matchingMap)) {
    const rightVal = matchingMap[leftKey];
    if (rightVal) {
      const pal = PAIR_PALETTE[colorIdx % PAIR_PALETTE.length];
      assignedLeftToColor.set(leftKey, pal);
      assignedRightToColor.set(rightVal, pal);
      colorIdx++;
    }
  }

  const assignMatch = (leftItem: string, rightItem: string) => {
    const current = { ...matchingMap };
    for (const key of Object.keys(current)) {
      if (current[key] === rightItem) {
        delete current[key];
      }
    }
    current[leftItem] = rightItem;
    onUpdate({ matching: current });
    playSynthGameSound('match', soundEnabled);
    setActiveLeftForMatch(null);
  };

  const clearMatch = (leftItem: string) => {
    const current = { ...matchingMap };
    delete current[leftItem];
    onUpdate({ matching: current });
    playSynthGameSound('undo', soundEnabled);
  };

  return (
    <div className="space-y-5 select-none">
      <div className="flex items-center justify-between rounded-2xl border border-teal-200/70 bg-gradient-to-r from-teal-50/80 via-white to-sky-50/80 p-3.5 shadow-sm">
        <div className="flex items-center gap-2.5">
          <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-teal-500 text-white shadow-md shadow-teal-200">
            <Link2 className="h-4 w-4" />
          </div>
          <div>
            <div className="text-xs font-bold uppercase tracking-wider text-teal-800">
              {isCompleted ? '🎉 Đã Ghép Đủ Cặp Đôi Hoàn Hảo!' : 'Đấu Trường Ghép Đôi (Cool Pair Matching)'}
            </div>
            <div className="text-xs text-slate-500">
              Nhấp thẻ cột Trái rồi nhấp thẻ cột Phải tương ứng để kết nối
            </div>
          </div>
        </div>
        <div className="text-right">
          <span className="text-base font-black text-teal-600">{matchedCount}/{totalPairs}</span>
          <span className="text-xs font-semibold text-slate-400 ml-1">cặp</span>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        {/* Cột Trái */}
        <div className="space-y-3">
          <div className="text-xs font-bold uppercase tracking-wider text-slate-400 px-1 flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-full bg-cyan-500"></span>
            {t('matchingLeftCol') || 'Mục Khởi Đầu (Left Item)'}
          </div>

          {leftPool.map((leftItem, idx) => {
            const matchedRight = matchingMap[leftItem];
            const isSelected = activeLeftForMatch === leftItem;
            const palette = assignedLeftToColor.get(leftItem);

            let itemClass = 'group relative flex w-full items-center justify-between rounded-2xl border-2 p-3.5 text-left transition-all duration-200 ';

            if (palette && matchedRight) {
              itemClass += `${palette.border} ${palette.bg} ${palette.glow} `;
            } else if (isSelected) {
              itemClass += 'border-cyan-500 bg-cyan-50/90 shadow-lg shadow-cyan-200 ring-2 ring-cyan-300 scale-[1.02] ';
            } else {
              itemClass += 'border-slate-200 bg-white hover:border-cyan-300 hover:bg-slate-50 hover:shadow-sm ';
            }

            return (
              <button
                key={`left-${leftItem}`}
                type="button"
                disabled={disabled}
                onClick={() => {
                  playSynthGameSound('gem', soundEnabled);
                  setActiveLeftForMatch(isSelected ? null : leftItem);
                }}
                className={itemClass}
              >
                <div className="flex items-center gap-3 min-w-0 flex-1">
                  <span className="flex h-7 w-7 flex-shrink-0 items-center justify-center rounded-lg bg-slate-100 text-xs font-bold text-slate-600 group-hover:bg-cyan-100 group-hover:text-cyan-800">
                    {idx + 1}
                  </span>
                  <span className="text-sm font-semibold text-slate-800 truncate">{leftItem}</span>
                </div>

                {matchedRight && (
                  <div className="flex items-center gap-2">
                    <span className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-bold text-white ${palette?.badge || 'bg-teal-500'}`}>
                      <Check className="h-3 w-3" /> Cặp {idx + 1}
                    </span>
                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        clearMatch(leftItem);
                      }}
                      className="rounded-lg p-1 text-slate-400 hover:bg-rose-100 hover:text-rose-600 transition"
                      title="Hủy ghép cặp này"
                    >
                      <Undo2 className="h-3.5 w-3.5" />
                    </button>
                  </div>
                )}

                {!matchedRight && isSelected && (
                  <span className="text-xs font-bold text-cyan-600 animate-pulse">Chọn đối ứng ➔</span>
                )}
              </button>
            );
          })}
        </div>

        {/* Cột Phải */}
        <div className="space-y-3">
          <div className="text-xs font-bold uppercase tracking-wider text-slate-400 px-1 flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-full bg-emerald-500"></span>
            {t('matchingRightCol') || 'Thẻ Đối Ứng (Right Item)'}
          </div>

          {rightPool.map((rightItem) => {
            const matchedLeft = Object.keys(matchingMap).find((k) => matchingMap[k] === rightItem);
            const palette = assignedRightToColor.get(rightItem);

            let itemClass = 'group relative flex w-full items-center justify-between rounded-2xl border-2 p-3.5 text-left transition-all duration-200 ';

            if (palette && matchedLeft) {
              itemClass += `${palette.border} ${palette.bg} ${palette.glow} `;
            } else if (activeLeftForMatch) {
              itemClass += 'border-dashed border-emerald-300 bg-emerald-50/40 hover:border-emerald-500 hover:bg-emerald-50 hover:scale-[1.02] ';
            } else {
              itemClass += 'border-slate-200 bg-white hover:border-emerald-300 hover:bg-slate-50 ';
            }

            return (
              <button
                key={`right-${rightItem}`}
                type="button"
                disabled={disabled}
                onClick={() => {
                  if (activeLeftForMatch) {
                    assignMatch(activeLeftForMatch, rightItem);
                  } else if (matchedLeft) {
                    clearMatch(matchedLeft);
                  }
                }}
                className={itemClass}
              >
                <div className="flex items-center gap-2.5 min-w-0 flex-1">
                  <span className="h-2 w-2 rounded-full bg-slate-300 group-hover:bg-emerald-400"></span>
                  <span className="text-sm font-semibold text-slate-800 truncate">{rightItem}</span>
                </div>

                {matchedLeft && (
                  <span className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-bold text-white ${palette?.badge || 'bg-emerald-500'}`}>
                    <Link2 className="h-3 w-3" /> Đã Khớp
                  </span>
                )}

                {!matchedLeft && activeLeftForMatch && (
                  <span className="text-xs font-bold text-emerald-600">Ghép vào đây ✚</span>
                )}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}
