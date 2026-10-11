import { Check, X } from 'lucide-react';
import { MediaRenderer } from '../MediaRenderer';
import { MathRenderer } from '@/components/common/MathRenderer';

export type OptionItem = {
  id: string;
  label: string;
  value: string;
  isMedia?: boolean;
  mediaType?: string;
};

interface Props {
  option: OptionItem;
  selected: boolean;
  index: number;
  disabled?: boolean;
  showHint?: boolean;
  isCorrectOpt?: boolean;
  onSelect: () => void;
}

export function OptionChoiceItem({
  option,
  selected,
  index,
  disabled,
  showHint,
  isCorrectOpt,
  onSelect
}: Props) {
  const letterGradients = [
    'from-sky-500 to-blue-600',
    'from-emerald-500 to-teal-600',
    'from-amber-500 to-orange-600',
    'from-purple-500 to-indigo-600',
    'from-rose-500 to-pink-600',
    'from-teal-500 to-cyan-600'
  ];
  const letterGrad = letterGradients[index % letterGradients.length];

  let containerStyle = 'group relative flex w-full items-center gap-4 rounded-2xl border-2 p-4 text-left transition-all duration-200 select-none ';
  let badgeStyle = 'flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-xl font-black text-sm transition-all duration-200 shadow-sm ';

  if (showHint) {
    if (selected && isCorrectOpt) {
      containerStyle += 'border-emerald-500 bg-gradient-to-r from-emerald-50 via-white to-emerald-50/40 shadow-lg shadow-emerald-200/50 scale-[1.01] ';
      badgeStyle += 'bg-emerald-600 text-white shadow-emerald-400 ';
    } else if (selected && !isCorrectOpt) {
      containerStyle += 'border-rose-500 bg-gradient-to-r from-rose-50 via-white to-rose-50/40 shadow-lg shadow-rose-200/50 ';
      badgeStyle += 'bg-rose-600 text-white shadow-rose-400 ';
    } else if (isCorrectOpt) {
      containerStyle += 'border-emerald-400/80 bg-emerald-50/30 border-dashed ';
      badgeStyle += 'bg-emerald-500 text-white ';
    } else {
      containerStyle += 'border-slate-200 bg-white/60 opacity-60 ';
      badgeStyle += 'bg-slate-100 text-slate-400 ';
    }
  } else if (selected) {
    containerStyle += 'border-cyan-500 bg-gradient-to-r from-cyan-50/90 via-white to-sky-50/60 shadow-lg shadow-cyan-200/50 scale-[1.015] ring-2 ring-cyan-300 ';
    badgeStyle += `bg-gradient-to-br ${letterGrad} text-white shadow-md scale-110 `;
  } else {
    containerStyle += 'border-slate-200/80 bg-white hover:border-cyan-300 hover:bg-slate-50/80 hover:shadow-md hover:-translate-y-0.5 active:translate-y-0 ';
    badgeStyle += 'bg-slate-100 text-slate-600 group-hover:bg-slate-200 group-hover:text-slate-800 ';
  }

  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onSelect}
      className={containerStyle}
    >
      <div className={badgeStyle}>
        {showHint && selected && isCorrectOpt ? (
          <Check className="h-5 w-5 stroke-[3]" />
        ) : showHint && selected && !isCorrectOpt ? (
          <X className="h-5 w-5 stroke-[3]" />
        ) : (
          option.label
        )}
      </div>

      <div className="min-w-0 flex-1">
        {option.isMedia ? (
          <div className="space-y-2">
            <div className="text-xs font-bold uppercase tracking-wider text-slate-400">{option.label}</div>
            <MediaRenderer url={option.value} mediaType={option.mediaType} />
          </div>
        ) : (
          <div className="text-slate-800 font-medium text-base leading-relaxed">
            <MathRenderer content={option.value} inline className="text-[1.05rem]" />
          </div>
        )}
      </div>

      {selected && !showHint && (
        <div className="flex h-6 w-6 items-center justify-center rounded-full bg-cyan-500 text-white shadow-md shadow-cyan-300 animate-game-pop">
          <Check className="h-3.5 w-3.5 stroke-[3]" />
        </div>
      )}
    </button>
  );
}
