import { Flag } from 'lucide-react';
import { scoreQuestion } from '@/lib/quiz-helpers';
import type { LearnerAnswer, LearnerQuestion } from '@/types/quiz';

type Props = {
  questions: LearnerQuestion[];
  answers: Record<string, LearnerAnswer>;
  expiredQuestions?: Record<string, boolean>;
  flaggedQuestions?: Record<string, boolean>;
  isHintGlobalActive?: boolean;
  currentIndex: number;
  onPick: (index: number) => void;
  onSubmitAttempt?: () => void;
  t: (key: string) => string;
};

function isAnswered(answer?: LearnerAnswer): boolean {
  if (!answer) return false;

  if (answer.selectedOption) return true;
  if (answer.selectedOptions && answer.selectedOptions.length > 0) return true;
  if (answer.shortText && answer.shortText.trim().length > 0) return true;
  if (answer.ordering && answer.ordering.length > 0) return true;
  if (answer.matching && Object.keys(answer.matching).length > 0) return true;
  if (answer.essay && answer.essay.trim().length > 0) return true;

  return false;
}

export function QuestionNavigator({
  questions,
  answers,
  expiredQuestions = {},
  flaggedQuestions = {},
  isHintGlobalActive = false,
  currentIndex,
  onPick,
  onSubmitAttempt,
  t,
}: Props) {
  return (
    <aside className="rounded-3xl border border-white/70 bg-white/85 p-4 shadow-[0_24px_70px_-45px_rgba(14,116,144,0.6)] backdrop-blur">
      <h3 className="mb-3 text-sm font-bold uppercase tracking-[0.08em] text-slate-600">{t('quickJump')}</h3>
        <div className="grid grid-cols-[repeat(auto-fill,minmax(2.5rem,1fr))] justify-items-stretch gap-2">
        {questions.map((question, index) => {
          const answer = answers[question.id];
          const answered = isAnswered(answer);
          const expired = Boolean(expiredQuestions[question.id]);
          const flagged = Boolean(flaggedQuestions[question.id]);
          const active = index === currentIndex;
          const hintShown = isHintGlobalActive;
          const isCorrect = answered ? scoreQuestion(question, answer) > 0 : false;

          return (
            <button
              key={question.id}
              type="button"
              onClick={() => onPick(index)}
              className={`rounded-xl border px-2 py-2 text-sm font-semibold transition ${
                active
                  ? 'ring-2 ring-cyan-400 ring-offset-2 ' + (
                      hintShown && answered 
                        ? (isCorrect ? 'border-emerald-300 bg-emerald-100 text-emerald-800' : 'border-rose-300 bg-rose-100 text-rose-800') 
                        : 'border-cyan-400 bg-cyan-100 text-cyan-700'
                    )
                  : hintShown && answered
                  ? (isCorrect ? 'border-emerald-300 bg-emerald-50 text-emerald-700 hover:border-emerald-400' : 'border-rose-300 bg-rose-50 text-rose-700 hover:border-rose-400')
                  : flagged
                  ? 'border-amber-300 bg-amber-50 text-amber-800 hover:border-amber-400'
                  : expired
                  ? 'border-rose-200 bg-rose-50 text-rose-700 hover:border-rose-300'
                  : answered
                  ? 'border-teal-200 bg-teal-50 text-teal-700 hover:border-teal-300'
                  : 'border-slate-200 bg-white text-slate-500 hover:border-slate-300 hover:bg-slate-50'
              }`}
            >
              <div>{index + 1}</div>
              {flagged && <div className="mt-0.5 text-[10px] font-bold leading-none uppercase">{t('flagged')}</div>}
              {expired && <div className="mt-0.5 text-[10px] font-bold leading-none uppercase">{t('expired')}</div>}
            </button>
          );
        })}
      </div>
      {onSubmitAttempt && (
        <div className="mt-4 pt-3 border-t border-slate-100">
          <button
            type="button"
            onClick={onSubmitAttempt}
            className="w-full flex items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-rose-500 to-orange-500 px-3 py-2 text-xs font-bold text-white shadow shadow-orange-400/20 hover:brightness-105 active:scale-98 transition cursor-pointer"
          >
            <Flag className="h-3.5 w-3.5" />
            <span>{t('submitAttempt') || 'Nộp bài / Kết thúc'}</span>
          </button>
        </div>
      )}
    </aside>
  );
}
