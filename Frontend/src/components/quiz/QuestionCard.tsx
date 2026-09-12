import { 
  ArrowDown, 
  ArrowUp, 
  CheckCircle, 
  CheckSquare, 
  Circle, 
  GripHorizontal, 
  Square, 
  Undo2, 
  Volume2, 
  VolumeX, 
  Sparkles, 
  Star, 
  Zap, 
  Lightbulb, 
  Link2, 
  Clock3, 
  Check, 
  X,
  Layers,
  AlertTriangle,
  BookOpen,
  ChevronDown,
  ChevronUp
} from 'lucide-react';
import { useState } from 'react';
import type { ReactNode } from 'react';
import { getDisplayOptions, parseIndexList, parseMatchingPairs } from '@/lib/quiz-helpers';
import type { LearnerAnswer, LearnerQuestion } from '@/types/quiz';
import { MediaRenderer } from './MediaRenderer';
import { MathRenderer } from '@/components/common/MathRenderer';

type Props = {
  question: LearnerQuestion;
  answer?: LearnerAnswer;
  onAnswerChange: (questionId: string, answer: LearnerAnswer) => void;
  disabled?: boolean;
  questionSecondsLeft?: number | null;
  showHint?: boolean;
  t: (key: string) => string;
};

// ─── Web Audio Synthesizer: Âm thanh Game Hóa 0ms Delay ─────────────────────
function playSynthGameSound(type: 'select' | 'match' | 'undo' | 'gem', soundEnabled: boolean) {
  if (!soundEnabled) return;
  try {
    const AudioCtx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    if (!AudioCtx) return;
    const ctx = new AudioCtx();
    const now = ctx.currentTime;
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.connect(gain);
    gain.connect(ctx.destination);

    if (type === 'select') {
      // Crisp arcade click blip
      osc.type = 'sine';
      osc.frequency.setValueAtTime(587.33, now); // D5
      osc.frequency.exponentialRampToValueAtTime(880, now + 0.08); // A5
      gain.gain.setValueAtTime(0.18, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.09);
      osc.start(now);
      osc.stop(now + 0.09);
    } else if (type === 'match') {
      // Triumphant double chime harmony
      osc.type = 'triangle';
      osc.frequency.setValueAtTime(523.25, now); // C5
      osc.frequency.setValueAtTime(659.25, now + 0.05); // E5
      osc.frequency.setValueAtTime(783.99, now + 0.11); // G5
      osc.frequency.setValueAtTime(1046.50, now + 0.17); // C6
      gain.gain.setValueAtTime(0.24, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.32);
      osc.start(now);
      osc.stop(now + 0.32);
    } else if (type === 'undo') {
      // Gentle downward whoosh
      osc.type = 'sine';
      osc.frequency.setValueAtTime(440, now);
      osc.frequency.exponentialRampToValueAtTime(261.63, now + 0.12);
      gain.gain.setValueAtTime(0.15, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.13);
      osc.start(now);
      osc.stop(now + 0.13);
    } else if (type === 'gem') {
      // Snappy wood-block pop
      osc.type = 'square';
      osc.frequency.setValueAtTime(750, now);
      osc.frequency.exponentialRampToValueAtTime(1250, now + 0.05);
      gain.gain.setValueAtTime(0.12, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.06);
      osc.start(now);
      osc.stop(now + 0.06);
    }
  } catch {
    /* Browser audio policy safely ignored */
  }
}

// Bảng màu cặp đôi trực quan (Pair Color Palette)
const PAIR_PALETTE = [
  { border: 'border-cyan-400', bg: 'bg-cyan-50', text: 'text-cyan-800', badge: 'bg-cyan-500', glow: 'shadow-[0_0_15px_rgba(6,182,212,0.35)]' },
  { border: 'border-emerald-400', bg: 'bg-emerald-50', text: 'text-emerald-800', badge: 'bg-emerald-500', glow: 'shadow-[0_0_15px_rgba(16,185,129,0.35)]' },
  { border: 'border-amber-400', bg: 'bg-amber-50', text: 'text-amber-800', badge: 'bg-amber-500', glow: 'shadow-[0_0_15px_rgba(245,158,11,0.35)]' },
  { border: 'border-purple-400', bg: 'bg-purple-50', text: 'text-purple-800', badge: 'bg-purple-500', glow: 'shadow-[0_0_15px_rgba(168,85,247,0.35)]' },
  { border: 'border-rose-400', bg: 'bg-rose-50', text: 'text-rose-800', badge: 'bg-rose-500', glow: 'shadow-[0_0_15px_rgba(244,63,94,0.35)]' },
  { border: 'border-blue-400', bg: 'bg-blue-50', text: 'text-blue-800', badge: 'bg-blue-500', glow: 'shadow-[0_0_15px_rgba(59,130,246,0.35)]' },
];

export function QuestionCard({ question, answer, onAnswerChange, disabled = false, questionSecondsLeft, showHint = false, t }: Props) {
  const options = getDisplayOptions(question);
  const [dragOverIndex, setDragOverIndex] = useState<number | null>(null);
  const [activeLeftForMatch, setActiveLeftForMatch] = useState<string | null>(null);
  const [isContextCollapsed, setIsContextCollapsed] = useState(false);

  // Âm thanh Game Hóa
  const [soundEnabled, setSoundEnabled] = useState(() => {
    return localStorage.getItem('aegis_sound_enabled') !== 'false';
  });

  const toggleSound = () => {
    setSoundEnabled((prev) => {
      const next = !prev;
      localStorage.setItem('aegis_sound_enabled', String(next));
      return next;
    });
  };

  const update = (patch: LearnerAnswer) => {
    onAnswerChange(question.id, { ...answer, ...patch });
  };

  // ─── GAME MODE 1: Defeat the Goal-keeper (Single / Multi Choice) ───────────
  const renderOptionLine = (option: (typeof options)[number], selected: boolean, multi: boolean, index: number) => {
    const correct = parseIndexList(question.answerRaw);
    const isCorrectOpt = correct.includes(option.id);

    // Letter Badge Gradients
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
        key={option.id}
        type="button"
        disabled={disabled}
        onClick={() => {
          playSynthGameSound('select', soundEnabled);
          if (multi) {
            const current = answer?.selectedOptions ?? [];
            const next = current.includes(option.id) ? current.filter((x) => x !== option.id) : [...current, option.id];
            update({ selectedOptions: next });
            return;
          }
          update({ selectedOption: option.id });
        }}
        className={containerStyle}
      >
        {/* Letter Badge */}
        <div className={badgeStyle}>
          {showHint && selected && isCorrectOpt ? (
            <Check className="h-5 w-5 stroke-[3]" />
          ) : showHint && selected && !isCorrectOpt ? (
            <X className="h-5 w-5 stroke-[3]" />
          ) : (
            option.label
          )}
        </div>

        {/* Option Content */}
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

        {/* Selected Indicator */}
        {selected && !showHint && (
          <div className="flex h-6 w-6 items-center justify-center rounded-full bg-cyan-500 text-white shadow-md shadow-cyan-300 animate-game-pop">
            <Check className="h-3.5 w-3.5 stroke-[3]" />
          </div>
        )}
      </button>
    );
  };

  // ─── GAME MODE 2: Cool Pair Matching (Lật Thẻ Đôi Hoàn Hảo 3D) ────────────
  const renderMatching = () => {
    const pairs = parseMatchingPairs(options);
    if (pairs.length === 0) {
      return <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">{t('matchingFormatHint')}</div>;
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

    // Phân bổ màu cho từng cặp nối
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
      // Xóa các liên kết cũ nếu thẻ này đã được nối
      for (const key of Object.keys(current)) {
        if (current[key] === rightItem) {
          delete current[key];
        }
      }
      current[leftItem] = rightItem;
      update({ matching: current });
      playSynthGameSound('match', soundEnabled);
      setActiveLeftForMatch(null);
    };

    const clearMatch = (leftItem: string) => {
      const current = { ...matchingMap };
      delete current[leftItem];
      update({ matching: current });
      playSynthGameSound('undo', soundEnabled);
    };

    return (
      <div className="space-y-5 select-none">
        {/* Progress Bar & Header */}
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

        {/* Arena Dual Grid */}
        <div className="grid gap-4 md:grid-cols-2">
          {/* Left Column */}
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

          {/* Right Column */}
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
  };

  // ─── GAME MODE 3: Smart Monkey Word Gem Reordering (Sắp Xếp Từ Khỉ Thông Minh) ──
  const renderOrdering = () => {
    const currentOrder = answer?.ordering && answer.ordering.length > 0 ? answer.ordering : options.map((x) => x.id);

    const move = (index: number, offset: number) => {
      const nextIndex = index + offset;
      if (nextIndex < 0 || nextIndex >= currentOrder.length) return;
      const next = [...currentOrder];
      const tmp = next[index];
      next[index] = next[nextIndex];
      next[nextIndex] = tmp;
      update({ ordering: next });
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
      update({ ordering: next });
      playSynthGameSound('gem', soundEnabled);
    };

    return (
      <div className="space-y-4 select-none">
        {/* Guidance Header */}
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

        {/* Reorderable Items Container */}
        <div className="space-y-2.5" onDragLeave={() => setDragOverIndex(null)}>
          {currentOrder.map((optionId, index) => {
            const option = options.find((x) => x.id === optionId);
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
                
                {/* Position Gem */}
                <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 font-extrabold text-xs text-white shadow-sm">
                  {index + 1}
                </div>

                {/* Word / Statement Content */}
                <div className="min-w-0 flex-1 text-sm font-semibold text-slate-800">
                  <MathRenderer content={option.value} inline />
                </div>

                {/* Move Controls */}
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
  };

  // ─── Render Question Body by Type ─────────────────────────────────────────
  const renderBody = () => {
    switch (question.questionType) {
      case 'SINGLE':
      case 'TRUE_FALSE':
        return (
          <div className="space-y-3">
            {options.map((option, idx) => renderOptionLine(option, answer?.selectedOption === option.id, false, idx))}
          </div>
        );
      case 'MULTI':
        return (
          <div className="space-y-3">
            {options.map((option, idx) => renderOptionLine(option, (answer?.selectedOptions ?? []).includes(option.id), true, idx))}
          </div>
        );
      case 'SHORT_ANSWER':
      case 'FILL_BLANK':
        return (
          <div className="space-y-2">
            <div className="relative">
              <textarea
                disabled={disabled}
                value={answer?.shortText ?? ''}
                onChange={(event) => update({ shortText: event.target.value })}
                placeholder={t('typeAnswer') || 'Nhập đáp án của bạn (ví dụ: từ vựng, con số)...'}
                className="h-28 w-full rounded-2xl border-2 border-slate-200 bg-white p-4 font-semibold text-base text-slate-800 outline-none ring-teal-200 transition focus:border-teal-500 focus:ring-4 disabled:opacity-60 placeholder:text-slate-400 placeholder:font-normal"
              />
              <div className="absolute bottom-3 right-3 text-xs font-bold text-slate-400">
                {(answer?.shortText ?? '').length} ký tự
              </div>
            </div>
          </div>
        );
      case 'ORDERING':
        return renderOrdering();
      case 'MATCHING':
        return renderMatching();
      case 'ESSAY':
        return (
          <textarea
            disabled={disabled}
            value={answer?.essay ?? ''}
            onChange={(event) => update({ essay: event.target.value })}
            placeholder={t('writeEssay') || 'Viết bài luận của bạn tại đây...'}
            className="h-48 w-full rounded-2xl border-2 border-slate-200 bg-white p-4 text-base leading-relaxed text-slate-800 outline-none ring-teal-200 transition focus:border-teal-500 focus:ring-4 disabled:opacity-60"
          />
        );
      default:
        return null;
    }
  };

  const isUrgentTimer = question.durationSec > 0 && questionSecondsLeft != null && questionSecondsLeft <= 10;

  return (
    <article className="relative overflow-hidden rounded-3xl border border-white/80 bg-white/95 p-5 shadow-[0_20px_60px_-25px_rgba(15,23,42,0.18)] backdrop-blur-xl md:p-8 transition-all">
      {/* Top Ambient Glow Bar */}
      <div className="absolute top-0 left-0 right-0 h-1.5 bg-gradient-to-r from-sky-400 via-teal-400 to-indigo-500" />

      {/* Gamified Header Badges */}
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          {/* Question Type Badge */}
          <span className="inline-flex items-center gap-1.5 rounded-full border border-teal-300 bg-teal-50 px-3 py-1 text-xs font-bold uppercase tracking-wider text-teal-800 shadow-sm">
            <Zap className="h-3 w-3 text-teal-600" />
            {question.questionType}
          </span>

          {/* Difficulty Stars */}
          <span className="inline-flex items-center gap-1 rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-xs font-bold text-slate-700">
            <Star className="h-3 w-3 fill-amber-400 text-amber-400" />
            Độ khó {question.difficulty}
          </span>

          {/* Points Badge */}
          <span className="inline-flex items-center gap-1 rounded-full border border-amber-200 bg-amber-50 px-3 py-1 text-xs font-black text-amber-700">
            <Sparkles className="h-3 w-3 text-amber-500" />
            +{question.points} ĐIỂM
          </span>

          {/* Timer Badge */}
          {question.durationSec > 0 && questionSecondsLeft != null && (
            <span
              className={`inline-flex items-center gap-1 rounded-full border px-3 py-1 text-xs font-extrabold ${
                isUrgentTimer
                  ? 'border-rose-400 bg-rose-50 text-rose-700 animate-heartbeat-warn'
                  : 'border-slate-200 bg-slate-50 text-slate-600'
              }`}
            >
              <Clock3 className="h-3 w-3" />
              {questionSecondsLeft}s
            </span>
          )}

          {/* Critical Mistake Badge (Câu Điểm Liệt) */}
          {question.isCritical && (
            <span className="inline-flex items-center gap-1.5 rounded-full border border-rose-500 bg-rose-600 px-3.5 py-1 text-xs font-black uppercase tracking-wider text-white shadow-md shadow-rose-300 animate-pulse">
              <AlertTriangle className="h-3.5 w-3.5 fill-white text-rose-600" />
              ⚠️ CÂU ĐIỂM LIỆT (BẮT BUỘC ĐÚNG)
            </span>
          )}

          {/* Sa Hinh Category Badge */}
          {question.subCategory === 'SA_HINH' && (
            <span className="inline-flex items-center gap-1.5 rounded-full border border-indigo-300 bg-indigo-50 px-3 py-1 text-xs font-black text-indigo-700 shadow-xs">
              🚗 SA HÌNH
            </span>
          )}
        </div>

        {/* Audio Sound FX Toggle */}
        <button
          type="button"
          onClick={toggleSound}
          className="inline-flex items-center gap-1.5 rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-xs font-semibold text-slate-600 hover:bg-slate-100 hover:text-slate-900 transition"
          title={soundEnabled ? 'Tắt âm thanh hiệu ứng game' : 'Bật âm thanh hiệu ứng game'}
        >
          {soundEnabled ? (
            <>
              <Volume2 className="h-3.5 w-3.5 text-teal-600" />
              <span className="hidden sm:inline">Âm thanh: Bật</span>
            </>
          ) : (
            <>
              <VolumeX className="h-3.5 w-3.5 text-slate-400" />
              <span className="hidden sm:inline">Âm thanh: Tắt</span>
            </>
          )}
        </button>
      </div>

      {disabled && (
        <div className="mb-4 rounded-2xl border border-amber-200 bg-amber-50/80 px-4 py-2.5 text-sm font-semibold text-amber-800 flex items-center gap-2">
          <span>🔒</span> {t('kidLocked') || 'Câu hỏi đã bị khóa do hết thời gian.'}
        </div>
      )}

      {/* Sa Hinh Traffic Golden Rules Co-pilot */}
      {(question.subCategory === 'SA_HINH' || /sa\s*hình|thứ\s*tự\s*các\s*xe/i.test(question.content)) && (
        <div className="mb-4 flex items-center gap-2.5 rounded-2xl border border-indigo-200 bg-gradient-to-r from-indigo-50/90 via-white to-sky-50/80 p-3.5 text-xs text-indigo-900 shadow-xs">
          <span className="text-xl">🚗</span>
          <div className="flex-1 font-semibold leading-relaxed">
            <span className="font-black text-indigo-700 uppercase tracking-wide">Khẩu Quyết Sa Hình: </span>
            1. Nhất chớm ➔ 2. Nhì ưu (Hỏa-Sự-Công-Thương) ➔ 3. Tam đường ➔ 4. Tứ hướng (Phải trống) ➔ 5. Hướng rẽ (Phải - Thẳng - Trái)
          </div>
        </div>
      )}

      {/* ─── SHARED QUESTION CONTEXT / READING PASSAGE / CASE STUDY ─── */}
      {Boolean(question.contextContent) && (
        <div className="mb-5 overflow-hidden rounded-2xl border-2 border-indigo-200/90 bg-gradient-to-br from-indigo-50/70 via-white to-sky-50/60 p-4 shadow-sm transition-all">
          <div className="flex items-center justify-between gap-2 border-b border-indigo-100 pb-2.5 mb-2.5">
            <div className="flex items-center gap-2 min-w-0">
              <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-indigo-600 text-white shadow-xs">
                <BookOpen className="h-4 w-4" />
              </div>
              <div className="min-w-0">
                <h4 className="text-xs font-black uppercase tracking-wider text-indigo-950 truncate">
                  {question.contextTitle || 'Ngữ Cảnh Dùng Chung • Bài Đọc Hiểu / Tình Huống'}
                </h4>
                <p className="text-[10px] text-slate-500 font-medium">
                  Dữ liệu tham chiếu cốt lõi dùng chung cho các câu hỏi liên kết
                </p>
              </div>
            </div>

            <button
              type="button"
              onClick={() => setIsContextCollapsed(!isContextCollapsed)}
              className="inline-flex items-center gap-1 rounded-lg border border-indigo-200 bg-white px-2 py-1 text-[11px] font-bold text-indigo-700 hover:bg-indigo-50 transition"
              title={isContextCollapsed ? 'Mở rộng bài đọc' : 'Thu gọn bài đọc'}
            >
              <span>{isContextCollapsed ? 'Mở rộng' : 'Thu gọn'}</span>
              {isContextCollapsed ? <ChevronDown className="h-3.5 w-3.5" /> : <ChevronUp className="h-3.5 w-3.5" />}
            </button>
          </div>

          {!isContextCollapsed && (
            <div className="rounded-xl border border-indigo-100/80 bg-white/90 p-3.5 text-xs sm:text-sm leading-relaxed text-slate-800 shadow-inner max-h-72 overflow-y-auto font-sans">
              <MathRenderer content={question.contextContent!} />
            </div>
          )}
        </div>
      )}

      {/* Question Content */}
      <div className="text-lg font-bold leading-relaxed text-slate-900 md:text-xl tracking-tight">
        <MathRenderer content={question.content} />
      </div>

      {/* Multimodal Media (Audio / Image) */}
      {((question.contentType && question.contentType !== 'text') || (question.questionMediaType && question.questionMediaType !== 'text')) && (
        <div className="mt-4 rounded-2xl overflow-hidden border border-slate-200 bg-slate-50 p-2 shadow-inner">
          <MediaRenderer 
            url={question.questionMediaUrl || question.content} 
            mediaType={question.contentType || question.questionMediaType} 
          />
        </div>
      )}

      {/* Interactive Game Body */}
      <div className="mt-6">{renderBody()}</div>

      {/* ─── OMNI FLASH INSIGHT: Bộ Não Khắc Sâu Kiến Thức (CHỈ HIỆN KHI BẤM SHOW HINT) ───────────────── */}
      {showHint && (
        <div className="mt-7 overflow-hidden rounded-2xl border border-indigo-200/80 bg-gradient-to-br from-indigo-50/90 via-white to-sky-50/80 p-5 shadow-lg shadow-indigo-100/40 animate-game-pop">
          <div className="flex items-center gap-2 text-indigo-800 font-black text-sm uppercase tracking-wide mb-3">
            <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-indigo-600 text-white shadow-md shadow-indigo-300">
              <Lightbulb className="h-4 w-4" />
            </div>
            <span>Omni Flash Insight • Khắc Sâu Kiến Thức Cốt Lõi</span>
          </div>

          <div className="space-y-2 text-sm text-slate-700 leading-relaxed">
            {question.citation && (
              <div className="rounded-xl border border-slate-200/80 bg-white/80 p-3 shadow-xs">
                <span className="font-bold text-indigo-900">📌 Căn cứ & Nguồn khảo thí: </span>
                <MathRenderer content={question.citation} inline />
              </div>
            )}
            {question.aiExplanation && (
              <div className="rounded-xl border border-teal-200/80 bg-teal-50/80 p-3 shadow-xs text-teal-900">
                <span className="font-bold text-teal-950">🧠 Phân tích & Lời giải AI: </span>
                <MathRenderer content={question.aiExplanation} inline />
              </div>
            )}
            <div className="text-xs text-slate-500 font-medium">
              💡 <span className="font-bold">Mẹo vàng làm bài:</span> Đọc kỹ từ khóa then chốt trong đề bài, phân tích bản chất cấu trúc và loại trừ các phương án bẫy để đạt điểm tuyệt đối.
            </div>
          </div>
        </div>
      )}
    </article>
  );
}
