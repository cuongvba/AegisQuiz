import { Check, X } from 'lucide-react';
import { MathRenderer } from '@/components/common/MathRenderer';
import { parseMultiTfAnswer, serializeMultiTfVector } from '@/shared/lib/quiz/normalizers';
import { getDisplayOptions } from '@/lib/quiz-helpers';
import type { QuestionWidgetProps } from './types';
import { playSynthGameSound } from './sound-effects';

export default function MultiTrueFalseWidget({
  question,
  answer,
  onUpdate,
  disabled,
  showHint,
  soundEnabled,
  t
}: QuestionWidgetProps) {
  const displayOptions = getDisplayOptions(question);

  const currentMatrix: Record<string, boolean> = (() => {
    if (answer?.matrixAnswer && Object.keys(answer.matrixAnswer).length > 0) {
      return answer.matrixAnswer;
    }
    if (answer?.shortText) {
      return parseMultiTfAnswer(answer.shortText);
    }
    return {};
  })();

  const expectedMap: Record<string, boolean> = parseMultiTfAnswer(question.answerRaw);

  const handleSelectTf = (key: string, val: boolean) => {
    if (disabled) return;
    playSynthGameSound('select', soundEnabled);
    const nextMap = { ...currentMatrix, [key]: val };
    const vectorStr = serializeMultiTfVector(nextMap);
    onUpdate({
      matrixAnswer: nextMap,
      shortText: vectorStr
    });
  };

  return (
    <div className="space-y-3">
      <div className="text-xs font-bold text-slate-500 uppercase tracking-wider flex items-center justify-between pb-1 border-b border-slate-100">
        <span>{t('multiTfPrompt') || 'Xét tính Đúng / Sai của mỗi mệnh đề:'}</span>
        <span className="text-[11px] text-cyan-600 font-semibold bg-cyan-50 px-2 py-0.5 rounded-full border border-cyan-200/60">
          Chuẩn GDPT 2025
        </span>
      </div>

      <div className="grid gap-3">
        {displayOptions.map((option, idx) => {
          const letter = String.fromCharCode(97 + idx); // a, b, c, d
          const cleanText = option.value.replace(/^[a-d0-9]+[\)\.\:\s\-]+/i, '').trim();
          const userChoice = currentMatrix[letter];
          const hasUserChoice = userChoice !== undefined;
          const expectedVal = expectedMap[letter];

          let rowBorder = 'border-slate-200/90 bg-white hover:border-slate-300';
          if (showHint && hasUserChoice) {
            const isCorrect = userChoice === expectedVal;
            rowBorder = isCorrect
              ? 'border-emerald-500 bg-emerald-50/30'
              : 'border-rose-500 bg-rose-50/30';
          } else if (hasUserChoice) {
            rowBorder = 'border-cyan-400 bg-cyan-50/20 shadow-sm';
          }

          return (
            <div
              key={option.id || letter}
              className={`relative flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-4 rounded-2xl border-2 transition-all duration-200 ${rowBorder}`}
            >
              <div className="flex items-start gap-3 min-w-0 flex-1">
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-purple-500 to-indigo-600 text-white font-mono font-bold text-xs shadow-sm">
                  {letter})
                </span>
                <div className="text-sm font-semibold text-slate-800 leading-relaxed pt-0.5">
                  <MathRenderer content={cleanText || option.value} />
                </div>
              </div>

              {/* Đúng / Sai Button Group */}
              <div className="flex items-center gap-2 shrink-0 self-end sm:self-center pl-10 sm:pl-0">
                <button
                  type="button"
                  disabled={disabled}
                  onClick={() => handleSelectTf(letter, true)}
                  className={`px-4 py-2 rounded-xl text-xs font-black transition-all cursor-pointer flex items-center gap-1.5 select-none disabled:cursor-not-allowed ${
                    userChoice === true
                      ? 'bg-emerald-600 text-white shadow-md shadow-emerald-500/30 scale-105 ring-2 ring-emerald-300'
                      : 'bg-slate-100 text-slate-600 hover:bg-emerald-50 hover:text-emerald-700 hover:border-emerald-300 border border-slate-200'
                  }`}
                >
                  <Check size={14} />
                  <span>Đúng</span>
                </button>

                <button
                  type="button"
                  disabled={disabled}
                  onClick={() => handleSelectTf(letter, false)}
                  className={`px-4 py-2 rounded-xl text-xs font-black transition-all cursor-pointer flex items-center gap-1.5 select-none disabled:cursor-not-allowed ${
                    userChoice === false
                      ? 'bg-rose-600 text-white shadow-md shadow-rose-500/30 scale-105 ring-2 ring-rose-300'
                      : 'bg-slate-100 text-slate-600 hover:bg-rose-50 hover:text-rose-700 hover:border-rose-300 border border-slate-200'
                  }`}
                >
                  <X size={14} />
                  <span>Sai</span>
                </button>

                {showHint && expectedVal !== undefined && (
                  <span className={`ml-1 text-[11px] font-bold px-2 py-1 rounded-lg ${
                    expectedVal ? 'bg-emerald-100 text-emerald-800' : 'bg-rose-100 text-rose-800'
                  }`}>
                    Đáp án: {expectedVal ? 'Đúng' : 'Sai'}
                  </span>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
