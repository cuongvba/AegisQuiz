import React, { useState, useEffect } from 'react';
import { ArrowUpDown, ChevronUp, ChevronDown, Check, Shuffle } from 'lucide-react';
import { MathRenderer } from '@/components/common/MathRenderer';
import { useLearnerI18n } from '@/lib/i18n';
import { PracticeQuestionItem } from '../types/mobile-practice.types';

interface MobileOrderingWidgetProps {
  currentQ: PracticeQuestionItem;
  selectedKey?: string;
  primaryColor?: string;
  onSelectOption: (orderKey: string) => void;
}

interface OrderItem {
  id: string;
  originalIndex: number;
  text: string;
}

export const MobileOrderingWidget: React.FC<MobileOrderingWidgetProps> = ({
  currentQ,
  selectedKey,
  primaryColor,
  onSelectOption,
}) => {
  const { t } = useLearnerI18n();
  const [selectedSwapIdx, setSelectedSwapIdx] = useState<number | null>(null);

  // Chuẩn bị danh sách options gốc với ID 1-based: "1", "2", "3"...
  const rawItems: OrderItem[] = currentQ.options.map((opt, idx) => {
    const text = typeof opt === 'string' ? opt : (opt.text || (opt as any).content || String(opt));
    return {
      id: String(idx + 1),
      originalIndex: idx,
      text,
    };
  });

  // State trật tự hiện tại
  const [orderedItems, setOrderedItems] = useState<OrderItem[]>(() => {
    if (selectedKey && selectedKey.trim()) {
      const ids = selectedKey.split(/[,;\s]+/).map((s) => s.trim()).filter(Boolean);
      const byId = new Map(rawItems.map((item) => [item.id, item]));
      const restored = ids.map((id) => byId.get(id)).filter(Boolean) as OrderItem[];
      if (restored.length === rawItems.length) return restored;
    }
    return rawItems;
  });

  // Đồng bộ khi đổi câu hỏi
  useEffect(() => {
    if (selectedKey && selectedKey.trim()) {
      const ids = selectedKey.split(/[,;\s]+/).map((s) => s.trim()).filter(Boolean);
      const byId = new Map(rawItems.map((item) => [item.id, item]));
      const restored = ids.map((id) => byId.get(id)).filter(Boolean) as OrderItem[];
      if (restored.length === rawItems.length) {
        setOrderedItems(restored);
        return;
      }
    }
    setOrderedItems(rawItems);
    setSelectedSwapIdx(null);
  }, [currentQ.id, selectedKey]);

  // Hoán đổi vị trí qua nút Lên / Xuống
  const handleMove = (index: number, offset: number) => {
    const targetIdx = index + offset;
    if (targetIdx < 0 || targetIdx >= orderedItems.length) return;

    const next = [...orderedItems];
    const temp = next[index];
    next[index] = next[targetIdx];
    next[targetIdx] = temp;

    setOrderedItems(next);
    setSelectedSwapIdx(null);
    onSelectOption(next.map((x) => x.id).join(','));
  };

  // Chạm để chọn và hoán đổi 2 thẻ bất kỳ
  const handleCardClick = (index: number) => {
    if (selectedSwapIdx === null) {
      setSelectedSwapIdx(index);
    } else if (selectedSwapIdx === index) {
      setSelectedSwapIdx(null);
    } else {
      const next = [...orderedItems];
      const temp = next[selectedSwapIdx];
      next[selectedSwapIdx] = next[index];
      next[index] = temp;

      setOrderedItems(next);
      setSelectedSwapIdx(null);
      onSelectOption(next.map((x) => x.id).join(','));
    }
  };

  return (
    <div className="space-y-3 select-none">
      {/* Tiêu đề & Hướng dẫn */}
      <div className="flex items-center justify-between px-1">
        <div className="text-[11px] font-bold text-teal-400 uppercase tracking-wider flex items-center gap-1.5">
          <ArrowUpDown size={13} />
          <span>Sắp xếp trật tự các giai đoạn chuẩn:</span>
        </div>
        <span className="text-[10px] text-slate-400">
          Chạm 2 thẻ hoặc bấm mũi tên
        </span>
      </div>

      {/* Danh sách thẻ sắp xếp */}
      <div className="space-y-2">
        {orderedItems.map((item, index) => {
          const isSelectedForSwap = selectedSwapIdx === index;
          const isFirst = index === 0;
          const isLast = index === orderedItems.length - 1;

          return (
            <div
              key={`${item.id}-${index}`}
              className={`p-3 rounded-2xl border transition-all flex items-center gap-2.5 active:scale-[0.99] ${
                isSelectedForSwap
                  ? 'bg-teal-950/40 border-teal-400 ring-2 ring-teal-500/40 shadow-lg shadow-teal-950/50'
                  : 'bg-slate-900/90 border-slate-800 hover:border-slate-700'
              }`}
            >
              {/* Huy hiệu số thứ tự hiện tại */}
              <button
                type="button"
                onClick={() => handleCardClick(index)}
                className={`w-7 h-7 rounded-xl font-mono font-black flex items-center justify-center text-xs shrink-0 cursor-pointer transition-colors ${
                  isSelectedForSwap
                    ? 'bg-teal-400 text-slate-950 shadow-md shadow-teal-400/30'
                    : 'bg-teal-950/80 text-teal-300 border border-teal-800/80'
                }`}
                title="Bấm để chọn hoán đổi vị trí"
              >
                {index + 1}
              </button>

              {/* Nội dung phương án */}
              <div
                onClick={() => handleCardClick(index)}
                className="flex-1 min-w-0 text-xs text-slate-200 leading-relaxed cursor-pointer"
              >
                <MathRenderer content={item.text} inline />
              </div>

              {/* Nút điều hướng Lên / Xuống */}
              <div className="flex flex-col gap-1 shrink-0">
                <button
                  type="button"
                  disabled={isFirst}
                  onClick={(e) => {
                    e.stopPropagation();
                    handleMove(index, -1);
                  }}
                  className="w-7 h-6 rounded-lg bg-slate-950 border border-slate-800 text-slate-300 hover:text-white hover:border-slate-700 disabled:opacity-20 flex items-center justify-center cursor-pointer active:scale-90 transition-all"
                  title="Di chuyển lên trên"
                >
                  <ChevronUp size={14} />
                </button>
                <button
                  type="button"
                  disabled={isLast}
                  onClick={(e) => {
                    e.stopPropagation();
                    handleMove(index, 1);
                  }}
                  className="w-7 h-6 rounded-lg bg-slate-950 border border-slate-800 text-slate-300 hover:text-white hover:border-slate-700 disabled:opacity-20 flex items-center justify-center cursor-pointer active:scale-90 transition-all"
                  title="Di chuyển xuống dưới"
                >
                  <ChevronDown size={14} />
                </button>
              </div>
            </div>
          );
        })}
      </div>

      {/* Thanh trạng thái chọn hoán đổi */}
      {selectedSwapIdx !== null && (
        <div className="p-2.5 rounded-xl bg-teal-950/60 border border-teal-800 text-[11px] text-teal-300 flex items-center justify-between animate-pulse">
          <span>Đang chọn thẻ #{selectedSwapIdx + 1} — Hãy chạm thẻ thứ 2 để đổi chỗ</span>
          <button
            type="button"
            onClick={() => setSelectedSwapIdx(null)}
            className="px-2 py-0.5 rounded-lg bg-teal-900 text-white font-bold text-[10px] cursor-pointer"
          >
            Hủy
          </button>
        </div>
      )}
    </div>
  );
};
