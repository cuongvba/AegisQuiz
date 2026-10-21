import React, { useState, useMemo } from 'react';
import { Link2, RotateCcw, X, Check } from 'lucide-react';
import { MathRenderer } from '@/components/common/MathRenderer';
import { parseMatchingPairs } from '@/lib/quiz-helpers';
import { useLearnerI18n } from '@/lib/i18n';
import { PracticeQuestionItem } from '../types/mobile-practice.types';

interface MobileMatchingWidgetProps {
  currentQ: PracticeQuestionItem;
  selectedKey?: string;
  primaryColor?: string;
  onSelectOption: (val: string) => void;
}

const PAIR_PALETTE = [
  { border: 'border-teal-500', bg: 'bg-teal-950/70', badge: 'bg-teal-400 text-slate-950', glow: 'ring-1 ring-teal-400' },
  { border: 'border-cyan-500', bg: 'bg-cyan-950/70', badge: 'bg-cyan-400 text-slate-950', glow: 'ring-1 ring-cyan-400' },
  { border: 'border-amber-500', bg: 'bg-amber-950/70', badge: 'bg-amber-400 text-slate-950', glow: 'ring-1 ring-amber-400' },
  { border: 'border-purple-500', bg: 'bg-purple-950/70', badge: 'bg-purple-400 text-slate-950', glow: 'ring-1 ring-purple-400' },
  { border: 'border-rose-500', bg: 'bg-rose-950/70', badge: 'bg-rose-400 text-slate-950', glow: 'ring-1 ring-rose-400' },
  { border: 'border-indigo-500', bg: 'bg-indigo-950/70', badge: 'bg-indigo-400 text-slate-950', glow: 'ring-1 ring-indigo-400' },
];

export const MobileMatchingWidget: React.FC<MobileMatchingWidgetProps> = ({
  currentQ,
  selectedKey,
  onSelectOption,
}) => {
  const { t } = useLearnerI18n();
  const [selectedLeft, setSelectedLeft] = useState<string | null>(null);

  // 1. Trích xuất các cặp ghép nối chuẩn (hỗ trợ cả payload, options string, delimiter)
  const pairs = useMemo(() => {
    const displayOptions = currentQ.options.map((opt, idx) => ({
      id: opt.key || String(idx + 1),
      label: ['A', 'B', 'C', 'D', 'E', 'F'][idx] || opt.key,
      value: opt.text || String(opt),
      isMedia: false,
    }));
    const parsed = parseMatchingPairs(displayOptions, currentQ as any);
    if (parsed.length > 0) return parsed;

    // Fallback: Nếu parseMatchingPairs rỗng, tự bóc tách options dạng "Left - Right"
    const fallbackPairs: Array<{ left: string; right: string }> = [];
    currentQ.options.forEach((opt) => {
      const text = opt.text || '';
      const parts = text.split(/\s+[-–—:=|]\s+/);
      if (parts.length >= 2) {
        fallbackPairs.push({ left: parts[0].trim(), right: parts.slice(1).join(' - ').trim() });
      }
    });
    return fallbackPairs;
  }, [currentQ]);

  const leftItems = useMemo(() => pairs.map((p) => p.left), [pairs]);
  const rightItems = useMemo(() => Array.from(new Set(pairs.map((p) => p.right))), [pairs]);

  // 2. Parse cấu trúc đáp án đã lưu từ selectedKey: "left1::right1;left2::right2"
  const currentMatching = useMemo<Record<string, string>>(() => {
    if (!selectedKey) return {};
    const map: Record<string, string> = {};
    selectedKey.split(';').forEach((entry) => {
      const [l, r] = entry.split('::').map((s) => s.trim());
      if (l && r) map[l] = r;
    });
    return map;
  }, [selectedKey]);

  // Gán bảng màu và số cặp cho từng kết nối
  const assignedColors = useMemo(() => {
    const leftColorMap = new Map<string, { palette: typeof PAIR_PALETTE[0]; pairNum: number }>();
    const rightColorMap = new Map<string, { palette: typeof PAIR_PALETTE[0]; pairNum: number }>();
    let pairIdx = 0;
    for (const [left, right] of Object.entries(currentMatching)) {
      if (right) {
        const pal = PAIR_PALETTE[pairIdx % PAIR_PALETTE.length];
        leftColorMap.set(left, { palette: pal, pairNum: pairIdx + 1 });
        rightColorMap.set(right, { palette: pal, pairNum: pairIdx + 1 });
        pairIdx++;
      }
    }
    return { leftColorMap, rightColorMap };
  }, [currentMatching]);

  const saveMatching = (nextMap: Record<string, string>) => {
    const serialized = Object.entries(nextMap)
      .map(([l, r]) => `${l}::${r}`)
      .join(';');
    onSelectOption(serialized);
  };

  const handleSelectLeft = (leftText: string) => {
    if (selectedLeft === leftText) {
      setSelectedLeft(null);
    } else {
      setSelectedLeft(leftText);
    }
  };

  const handleSelectRight = (rightText: string) => {
    if (!selectedLeft) return;
    // Bỏ ghép rightText khỏi cặp cũ (nếu có)
    const nextMap = { ...currentMatching };
    for (const [k, v] of Object.entries(nextMap)) {
      if (v === rightText) delete nextMap[k];
    }
    nextMap[selectedLeft] = rightText;
    saveMatching(nextMap);
    setSelectedLeft(null);
  };

  const handleUnpair = (leftText: string) => {
    const nextMap = { ...currentMatching };
    delete nextMap[leftText];
    saveMatching(nextMap);
    if (selectedLeft === leftText) setSelectedLeft(null);
  };

  const handleReset = () => {
    setSelectedLeft(null);
    saveMatching({});
  };

  const pairedCount = Object.keys(currentMatching).length;
  const totalCount = pairs.length;

  if (pairs.length === 0) {
    return (
      <div className="p-3.5 rounded-2xl bg-slate-900/80 border border-slate-800 text-xs text-slate-400">
        {t('matchingFormatHint') || 'Định dạng câu hỏi ghép cặp: "Mục A - Mục B"'}
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {/* ── Tiêu đề & Tiến độ ghép nối ── */}
      <div className="flex items-center justify-between text-xs font-bold text-teal-400 px-1">
        <span className="flex items-center gap-1.5 uppercase tracking-wider text-[11px]">
          <Link2 size={13} /> {t('matchingPrompt') || 'Các cặp ghép nối tương ứng:'}
        </span>
        <div className="flex items-center gap-2">
          <span className="text-[10px] bg-teal-950/80 px-2 py-0.5 rounded-full border border-teal-800 text-teal-300 font-mono font-bold">
            {pairedCount}/{totalCount} cặp
          </span>
          {pairedCount > 0 && (
            <button
              type="button"
              onClick={handleReset}
              className="text-[10px] text-slate-400 hover:text-rose-400 flex items-center gap-1 cursor-pointer"
            >
              <RotateCcw size={11} /> {t('clearAllPairs') || 'Xóa ghép'}
            </button>
          )}
        </div>
      </div>

      <div className="text-[10px] text-slate-400 px-1 italic">
        {selectedLeft
          ? '👉 Đang chọn vế trái: Hãy nhấp thẻ Cột Phải để tạo cặp ghép nối!'
          : t('tapToPair') || 'Chạm vế trái rồi chạm vế phải để ghép nối'}
      </div>

      {/* ── Khối 2 Cột Song Song: Cột Trái (A) & Cột Phải (B) Luôn Xuất Hiện ── */}
      <div className="grid grid-cols-2 gap-2">
        {/* CỘT TRÁI */}
        <div className="space-y-2">
          <div className="text-[10px] font-black uppercase text-teal-400/90 tracking-wider px-1 flex items-center gap-1">
            <span className="h-1.5 w-1.5 rounded-full bg-teal-400"></span>
            <span>{t('matchingLeftCol') || 'Cột A (Vế trái)'}</span>
          </div>

          {leftItems.map((leftItem, idx) => {
            const isSelected = selectedLeft === leftItem;
            const matchedRight = currentMatching[leftItem];
            const colorInfo = assignedColors.leftColorMap.get(leftItem);

            return (
              <button
                key={`left-${idx}`}
                type="button"
                onClick={() => handleSelectLeft(leftItem)}
                className={`w-full text-left p-2.5 rounded-xl border transition-all cursor-pointer relative flex flex-col justify-between min-h-[64px] active:scale-98 ${
                  isSelected
                    ? 'bg-slate-900 border-teal-400 ring-2 ring-teal-400/60 shadow-lg'
                    : colorInfo
                    ? `${colorInfo.palette.bg} ${colorInfo.palette.border} ${colorInfo.palette.glow}`
                    : 'bg-slate-900/80 border-slate-800 hover:border-slate-700'
                }`}
              >
                <div className="flex items-start justify-between gap-1 w-full">
                  <span className="w-4 h-4 rounded text-[9px] font-mono font-black flex items-center justify-center shrink-0 bg-slate-800 text-slate-300">
                    {idx + 1}
                  </span>
                  {colorInfo && (
                    <span className={`px-1.5 py-0.2 rounded text-[8px] font-black uppercase ${colorInfo.palette.badge}`}>
                      Cặp #{colorInfo.pairNum}
                    </span>
                  )}
                </div>

                <div className="text-xs text-slate-100 font-medium leading-snug my-1">
                  <MathRenderer content={leftItem} inline />
                </div>

                {matchedRight && (
                  <div className="flex items-center justify-between text-[9px] text-teal-300/80 pt-1 border-t border-slate-800/80 mt-1">
                    <span className="truncate">➔ <MathRenderer content={matchedRight} inline /></span>
                    <span
                      onClick={(e) => {
                        e.stopPropagation();
                        handleUnpair(leftItem);
                      }}
                      className="p-0.5 rounded hover:text-rose-400 text-slate-400"
                    >
                      <X size={10} />
                    </span>
                  </div>
                )}
              </button>
            );
          })}
        </div>

        {/* CỘT PHẢI (Luôn hiển thị đầy đủ trên màn hình) */}
        <div className="space-y-2">
          <div className="text-[10px] font-black uppercase text-cyan-400/90 tracking-wider px-1 flex items-center gap-1">
            <span className="h-1.5 w-1.5 rounded-full bg-cyan-400"></span>
            <span>{t('matchingRightCol') || 'Cột B (Vế phải)'}</span>
          </div>

          {rightItems.map((rightItem, idx) => {
            const letterLabel = String.fromCharCode(65 + idx);
            const colorInfo = assignedColors.rightColorMap.get(rightItem);
            const isPaired = Boolean(colorInfo);

            return (
              <button
                key={`right-${idx}`}
                type="button"
                onClick={() => handleSelectRight(rightItem)}
                className={`w-full text-left p-2.5 rounded-xl border transition-all cursor-pointer relative flex flex-col justify-between min-h-[64px] active:scale-98 ${
                  selectedLeft && !isPaired
                    ? 'border-cyan-400/80 bg-slate-900 ring-1 ring-cyan-400/40 animate-pulse'
                    : colorInfo
                    ? `${colorInfo.palette.bg} ${colorInfo.palette.border} ${colorInfo.palette.glow}`
                    : 'bg-slate-900/80 border-slate-800 hover:border-slate-700'
                }`}
              >
                <div className="flex items-start justify-between gap-1 w-full">
                  <span className="w-4 h-4 rounded text-[9px] font-mono font-black flex items-center justify-center shrink-0 bg-slate-800 text-cyan-300">
                    {letterLabel}
                  </span>
                  {colorInfo && (
                    <span className={`px-1.5 py-0.2 rounded text-[8px] font-black uppercase ${colorInfo.palette.badge}`}>
                      Cặp #{colorInfo.pairNum}
                    </span>
                  )}
                </div>

                <div className="text-xs text-slate-100 font-medium leading-snug my-1">
                  <MathRenderer content={rightItem} inline />
                </div>

                {isPaired && (
                  <div className="flex items-center gap-1 text-[9px] text-cyan-300/80 pt-1 border-t border-slate-800/80 mt-1">
                    <Check size={10} className="text-cyan-400" />
                    <span>Đã kết nối</span>
                  </div>
                )}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
};
