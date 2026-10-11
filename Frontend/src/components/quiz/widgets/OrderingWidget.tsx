import { ArrowDown, ArrowUp, GripHorizontal, Layers } from 'lucide-react';
import { useState } from 'react';
import { MathRenderer } from '@/components/common/MathRenderer';
import { getDisplayOptions } from '@/lib/quiz-helpers';
import type { QuestionWidgetProps } from './types';
import { playSynthGameSound } from './sound-effects';

export default function OrderingWidget({
  question,
  answer,
  onUpdate,
  disabled,
  soundEnabled
}: QuestionWidgetProps) {
  const displayOptions = getDisplayOptions(question);
  const [dragOverIndex, setDragOverIndex] = useState<number | null>(null);

  const currentOrder = answer?.ordering && answer.ordering.length > 0 
    ? answer.ordering 
    : displayOptions.map((x) => x.id);

  const move = (index: number, offset: number) => {
    const nextIndex = index + offset;
    if (nextIndex < 0 || nextIndex >= currentOrder.length) return;
    const next = [...currentOrder];
    const tmp = next[index];
    next[index] = next[nextIndex];
    next[nextIndex] = tmp;
    onUpdate({ ordering: next });
    playSynthGameSound('gem', soundEnabled);
  };

  const handleDragStart = (e: React.DragEvent<HTMLDivElement>, index: number) => {
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', String(index));
  };

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>, index: number) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    setDragOverIndex(index);
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>, targetIndex: number) => {
    e.preventDefault();
    setDragOverIndex(null);
    const sourceIndex = Number(e.dataTransfer.getData('text/plain'));
    if (sourceIndex === targetIndex) return;
    const next = [...currentOrder];
    const [moved] = next.splice(sourceIndex, 1);
    next.splice(targetIndex, 0, moved);
    onUpdate({ ordering: next });
    playSynthGameSound('gem', soundEnabled);
  };

  return (
    <div className="space-y-4 select-none">
      <div className="flex items-center justify-between rounded-2xl border border-indigo-200/70 bg-gradient-to-r from-indigo-50/80 via-white to-purple-50/80 p-3 shadow-sm">
        <div className="flex items-center gap-2">
          <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-indigo-600 text-white shadow-md shadow-indigo-200">
            <Layers className="h-4 w-4" />
          </div>
          <span className="text-xs font-bold uppercase tracking-wider text-indigo-900">
            Khỉ Thông Minh: Sắp Xếp Trật Tự Câu Chuẩn Xác
          </span>
        </div>
        <span className="text-xs text-slate-500 font-medium">Kéo thả hoặc dùng mũi tên</span>
      </div>

      <div className="space-y-2.5" onDragLeave={() => setDragOverIndex(null)}>
        {currentOrder.map((optionId, index) => {
          const option = displayOptions.find((x) => x.id === optionId);
          if (!option) return null;
          const isDragOver = dragOverIndex === index;

          return (
            <div
              key={option.id}
              draggable={!disabled}
              onDragStart={(e) => handleDragStart(e, index)}
              onDragOver={(e) => handleDragOver(e, index)}
              onDrop={(e) => handleDrop(e, index)}
              className={`flex items-center gap-3.5 rounded-2xl border-2 p-3.5 transition-all duration-200 ${
                isDragOver
                  ? 'border-indigo-400 bg-indigo-50 scale-[1.02] shadow-lg ring-2 ring-indigo-200'
                  : 'border-slate-200/80 bg-white hover:border-indigo-300 hover:shadow-md'
              } ${!disabled ? 'cursor-grab active:cursor-grabbing' : ''}`}
            >
              <GripHorizontal className="h-4 w-4 flex-shrink-0 text-slate-400" />
              
              <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 font-extrabold text-xs text-white shadow-sm">
                {index + 1}
              </div>

              <div className="min-w-0 flex-1 text-sm font-semibold text-slate-800">
                <MathRenderer content={option.value} inline />
              </div>

              <div className="flex items-center gap-1">
                <button
                  type="button"
                  disabled={disabled || index === 0}
                  onClick={() => move(index, -1)}
                  className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 bg-slate-50 text-slate-600 hover:bg-indigo-50 hover:text-indigo-600 hover:border-indigo-200 disabled:opacity-20 transition"
                  title="Đẩy lên"
                >
                  <ArrowUp className="h-4 w-4" />
                </button>
                <button
                  type="button"
                  disabled={disabled || index === currentOrder.length - 1}
                  onClick={() => move(index, 1)}
                  className="flex h-8 w-8 items-center justify-center rounded-lg border border-slate-200 bg-slate-50 text-slate-600 hover:bg-indigo-50 hover:text-indigo-600 hover:border-indigo-200 disabled:opacity-20 transition"
                  title="Hạ xuống"
                >
                  <ArrowDown className="h-4 w-4" />
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
