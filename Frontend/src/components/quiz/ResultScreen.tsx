import { useEffect, useMemo, useState } from 'react';
import { 
  Bot, 
  Printer, 
  RotateCcw, 
  Lightbulb, 
  Sparkles, 
  Target, 
  CheckCircle2, 
  XCircle, 
  BookOpen, 
  ChevronRight,
  AlertTriangle
} from 'lucide-react';
import { getDisplayOptions, isQuestionAnswered, parseIndexList, parseMatchingPairs, scoreQuestion } from '@/lib/quiz-helpers';
import type { LearnerAnswer, LearnerQuestion, QuizResult } from '@/types/quiz';
import { MathRenderer } from '@/components/common/MathRenderer';

type Props = {
  result: QuizResult;
  questions: LearnerQuestion[];
  answers: Record<string, LearnerAnswer>;
  flaggedQuestions?: Record<string, boolean>;
  isKidMode?: boolean;
  kidBestStreak?: number;
  onRestart: () => void;
  onRetestMistakes?: (wrongQuestions: LearnerQuestion[]) => void;
  t: (key: string) => string;
};

function textOrDash(value?: string): string {
  const clean = (value ?? '').trim();
  return clean.length > 0 ? clean : '-';
}

function resolveOneBasedOrZeroBasedIndex(raw: string, size: number): number | null {
  const token = raw.trim();
  if (!/^\d+$/.test(token)) return null;
  const n = Number(token);
  if (n >= 0 && n < size) return n;
  if (n >= 1 && n <= size) return n - 1;
  return null;
}

function formatIndexedAnswerText(question: LearnerQuestion, raw: string): string {
  const options = getDisplayOptions(question);
  const byId = new Map(options.map((opt) => [opt.id, `${opt.label}. ${opt.value}`]));
  const items = parseIndexList(raw)
    .map((id) => byId.get(id) ?? id)
    .filter((x) => x.trim().length > 0);
  return items.length > 0 ? items.join(', ') : '-';
}

function formatMatchingCorrectAnswer(question: LearnerQuestion): string {
  const pairs = parseMatchingPairs(getDisplayOptions(question));
  const leftPool = pairs.map((x) => x.left);
  const rightPool = pairs.map((x) => x.right);

  const entries = question.answerRaw
    .split(/[;,]+/)
    .map((entry) => entry.trim())
    .filter((entry) => entry.length > 0)
    .map((entry) => {
      const [rawLeft, rawRight] = entry.split(':').map((x) => x.trim());
      if (!rawLeft || !rawRight) return '';

      const leftIdx = resolveOneBasedOrZeroBasedIndex(rawLeft, leftPool.length);
      const rightIdx = resolveOneBasedOrZeroBasedIndex(rawRight, rightPool.length);
      if (leftIdx != null && rightIdx != null) {
        const left = leftPool[leftIdx];
        const right = rightPool[rightIdx];
        return left && right ? `${left}: ${right}` : '';
      }

      return `${rawLeft}: ${rawRight}`;
    })
    .filter((x) => x.length > 0);

  return entries.length > 0 ? entries.join(', ') : '-';
}

function suggestEssayScore(question: LearnerQuestion, answer?: LearnerAnswer): number {
  const text = (answer?.essay ?? '').trim();
  if (!text) return 0;

  const words = text.split(/\s+/).filter((x) => x.length > 0).length;
  const lengthFactor = Math.min(1, words / 120);

  const rubricTokens = (question.answerRaw ?? '')
    .toLowerCase()
    .split(/[^\p{L}\p{N}_]+/u)
    .map((x) => x.trim())
    .filter((x) => x.length >= 3);
  const answerTokens = new Set(
    text
      .toLowerCase()
      .split(/[^\p{L}\p{N}_]+/u)
      .map((x) => x.trim())
      .filter((x) => x.length >= 3),
  );

  const overlap = rubricTokens.length > 0
    ? rubricTokens.filter((token) => answerTokens.has(token)).length / rubricTokens.length
    : Math.min(1, words / 80);

  const blended = Math.max(0, Math.min(1, lengthFactor * 0.45 + overlap * 0.55));
  return Number((question.points * blended).toFixed(2));
}

function renderQuestionDetail(
  question: LearnerQuestion,
  answer: LearnerAnswer | undefined,
  t: (key: string) => string,
  essayScore?: number,
) {
  const options = getDisplayOptions(question);
  const correct = parseIndexList(question.answerRaw);

  if (question.questionType === 'SINGLE' || question.questionType === 'TRUE_FALSE' || question.questionType === 'MULTI') {
    const selectedSet = new Set(answer?.selectedOptions ?? (answer?.selectedOption ? [answer.selectedOption] : []));
    const correctSet = new Set(correct);

    return (
      <div className="mt-3 space-y-2">
        {options.map((opt) => {
          const selected = selectedSet.has(opt.id);
          const isCorrect = correctSet.has(opt.id);

          let rowClass = 'flex items-center gap-3 rounded-xl border p-2.5 text-sm transition ';
          if (isCorrect) {
            rowClass += 'border-emerald-300 bg-emerald-50 text-emerald-900 font-bold';
          } else if (selected && !isCorrect) {
            rowClass += 'border-rose-300 bg-rose-50 text-rose-900 font-medium line-through';
          } else {
            rowClass += 'border-slate-200 bg-white text-slate-700';
          }

          return (
            <div key={`${question.id}-${opt.id}`} className={rowClass}>
              <span className={`flex h-6 w-6 flex-shrink-0 items-center justify-center rounded-lg text-xs font-bold ${
                isCorrect ? 'bg-emerald-600 text-white' : selected ? 'bg-rose-600 text-white' : 'bg-slate-100 text-slate-600'
              }`}>
                {opt.label}
              </span>
              <span className="min-w-0 flex-1">{opt.value}</span>
              {isCorrect && (
                <span className="flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-bold text-emerald-800">
                  <CheckCircle2 className="h-3.5 w-3.5" /> Đáp án đúng
                </span>
              )}
              {selected && !isCorrect && (
                <span className="flex items-center gap-1 rounded-full bg-rose-100 px-2 py-0.5 text-xs font-bold text-rose-800">
                  <XCircle className="h-3.5 w-3.5" /> Lựa chọn của bạn
                </span>
              )}
            </div>
          );
        })}
      </div>
    );
  }

  if (question.questionType === 'ORDERING') {
    const selected = answer?.ordering ?? [];
    const earned = scoreQuestion(question, answer);
    const isCorrect = earned >= (question.points || 1);
    return (
      <div className="mt-3 space-y-2 text-sm rounded-2xl border border-slate-200 bg-slate-50/80 p-3.5">
        <div className={isCorrect ? 'text-emerald-800 font-bold' : 'text-rose-700 font-bold'}>
          <span>{isCorrect ? '✔️' : '❌'} {t('yourAnswer')}: </span>
          <span className="font-semibold text-slate-800">
            {selected.length > 0 ? formatIndexedAnswerText(question, selected.join(',')) : '-'}
          </span>
          {isCorrect && (
            <span className="ml-2 rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-black text-emerald-800">
              Chính xác
            </span>
          )}
        </div>
        {!isCorrect && (
          <div className="text-emerald-800 font-bold pt-1.5 border-t border-slate-200">
            <span>✔️ {t('correctAnswer')}: </span>
            <span className="font-semibold text-slate-800">
              {formatIndexedAnswerText(question, question.answerRaw)}
            </span>
          </div>
        )}
      </div>
    );
  }

  if (question.questionType === 'MATCHING') {
    const selectedMap = answer?.matching ?? {};
    const pairs = Object.entries(selectedMap);
    const earned = scoreQuestion(question, answer);
    const isCorrect = earned >= (question.points || 1);

    return (
      <div className="mt-3 space-y-2.5 text-sm rounded-2xl border border-slate-200 bg-slate-50/80 p-3.5">
        <div>
          <div className={`flex items-center gap-1.5 font-bold ${isCorrect ? 'text-emerald-800' : 'text-rose-700'}`}>
            <span>{isCorrect ? '✔️' : '❌'}</span>
            <span>{t('yourAnswer')}:</span>
            {isCorrect && (
              <span className="ml-1 rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-black text-emerald-800">
                Khớp chuẩn xác 100%
              </span>
            )}
          </div>
          {pairs.length > 0 ? (
            <div className="mt-2 flex flex-wrap gap-2">
              {pairs.map(([left, right], pIdx) => (
                <span
                  key={pIdx}
                  className={`inline-flex items-center gap-1.5 rounded-xl border px-3 py-1 text-xs font-bold shadow-xs ${
                    isCorrect
                      ? 'border-emerald-300 bg-emerald-50 text-emerald-950'
                      : 'border-rose-300 bg-rose-50 text-rose-950'
                  }`}
                >
                  <span className="font-semibold">{left}</span>
                  <span className={isCorrect ? 'text-emerald-500 font-black' : 'text-rose-400 font-black'}>➔</span>
                  <span className={isCorrect ? 'text-emerald-700' : 'text-rose-700'}>{right}</span>
                </span>
              ))}
            </div>
          ) : (
            <div className="mt-1 text-slate-400 text-xs italic">-</div>
          )}
        </div>

        {!isCorrect && (
          <div className="pt-2 border-t border-slate-200">
            <div className="flex items-center gap-1.5 font-bold text-emerald-800 mb-1.5">
              <span>✔️</span>
              <span>{t('correctAnswer')}:</span>
            </div>
            <div className="text-slate-800 font-medium text-xs bg-white rounded-xl border border-emerald-200 p-2.5">
              {formatMatchingCorrectAnswer(question)}
            </div>
          </div>
        )}
      </div>
    );
  }

  if (question.questionType === 'SHORT_ANSWER' || question.questionType === 'FILL_BLANK') {
    const earned = scoreQuestion(question, answer);
    const isCorrect = earned >= (question.points || 1);
    return (
      <div className="mt-3 space-y-2 text-sm rounded-2xl border border-slate-200 bg-slate-50/80 p-3.5">
        <div className={isCorrect ? 'text-emerald-800 font-bold' : 'text-rose-700 font-bold'}>
          <span>{isCorrect ? '✔️' : '❌'} {t('yourAnswer')}: </span>
          <span className="font-semibold text-slate-800">{textOrDash(answer?.shortText)}</span>
          {isCorrect && (
            <span className="ml-2 rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-black text-emerald-800">
              Chính xác
            </span>
          )}
        </div>
        {!isCorrect && (
          <div className="text-emerald-800 font-bold pt-1.5 border-t border-slate-200">
            <span>✔️ {t('correctAnswer')}: </span>
            <span className="font-semibold text-slate-800">{textOrDash(question.answerRaw)}</span>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="mt-3 space-y-2 text-sm rounded-xl border border-slate-200 bg-slate-50 p-3">
      <div><span className="font-bold">{t('yourAnswer')}:</span> {textOrDash(answer?.essay)}</div>
      <div className="text-amber-700 font-semibold">{t('manualGrading')}</div>
      {typeof essayScore === 'number' && <div className="text-cyan-700 font-bold">{t('essayScore')}: {essayScore.toFixed(2)} / {question.points}</div>}
    </div>
  );
}

function gradeLabel(score: number, t: (key: string) => string): string {
  if (score >= 90) return t('mastery') || 'Làm chủ xuất sắc';
  if (score >= 75) return t('strong') || 'Nắm vững kiến thức';
  if (score >= 60) return t('developing') || 'Đang tiến bộ';
  return t('needsReview') || 'Cần ôn luyện thêm';
}

type PrintStatus = 'correct' | 'incorrect' | 'pending';

function statusLabel(status: PrintStatus, t: (key: string) => string): string {
  if (status === 'correct') return t('statusCorrect') || 'Chính xác';
  if (status === 'incorrect') return t('statusIncorrect') || 'Chưa đúng';
  return t('statusPending') || 'Chờ chấm';
}

function statusClass(status: PrintStatus): string {
  if (status === 'correct') return 'border-emerald-300 bg-emerald-100 text-emerald-800';
  if (status === 'incorrect') return 'border-rose-300 bg-rose-100 text-rose-800';
  return 'border-amber-300 bg-amber-100 text-amber-800';
}

export function ResultScreen({ 
  result, 
  questions, 
  answers, 
  flaggedQuestions = {}, 
  isKidMode = false, 
  kidBestStreak = 0, 
  onRestart, 
  onRetestMistakes,
  t 
}: Props) {
  const now = new Date();
  const [activeTab, setActiveTab] = useState<'overview' | 'mistakes' | 'all'>('overview');

  const user = useMemo(() => {
    try {
      const raw = localStorage.getItem('user');
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }, []);

  const [manualEssayScores, setManualEssayScores] = useState<Record<string, number>>({});
  const [aiEssayScores, setAiEssayScores] = useState<Record<string, number>>({});

  const essayQuestions = useMemo(
    () => questions.filter((q) => q.questionType === 'ESSAY'),
    [questions],
  );

  const essayMaxPoints = useMemo(
    () => essayQuestions.reduce((sum, q) => sum + q.points, 0),
    [essayQuestions],
  );

  const essayEarnedPoints = useMemo(() => {
    return essayQuestions.reduce((sum, q) => {
      const manual = manualEssayScores[q.id];
      const ai = aiEssayScores[q.id];
      const picked = Number.isFinite(ai) ? ai : Number.isFinite(manual) ? manual : 0;
      return sum + Math.max(0, Math.min(q.points, picked));
    }, 0);
  }, [aiEssayScores, essayQuestions, manualEssayScores]);

  const finalPoints = result.earnedPoints + essayEarnedPoints;
  const finalMaxPoints = result.totalPoints + essayMaxPoints;
  const finalPercentage = finalMaxPoints > 0 ? (finalPoints / finalMaxPoints) * 100 : result.percentage;

  const setManualEssayScore = (questionId: string, value: number) => {
    setManualEssayScores((prev) => ({ ...prev, [questionId]: value }));
  };

  const runAiEssayScoring = () => {
    const next: Record<string, number> = {};
    for (const question of essayQuestions) {
      next[question.id] = suggestEssayScore(question, answers[question.id]);
    }
    setAiEssayScores(next);
  };

  useEffect(() => {
    if (essayQuestions.length === 0) return;
    runAiEssayScoring();
  }, [answers, essayQuestions]);

  const printRows = useMemo(() => {
    return questions.map((question, index) => {
      const answer = answers[question.id];
      const aiEssay = aiEssayScores[question.id];
      const manualEssay = manualEssayScores[question.id];

      if (question.questionType === 'ESSAY') {
        const picked = Number.isFinite(aiEssay) ? aiEssay : Number.isFinite(manualEssay) ? manualEssay : undefined;
        const earned = Math.max(0, Math.min(question.points, picked ?? 0));
        const answered = isQuestionAnswered(question, answer);
        const status: PrintStatus | null = !answered ? null : picked != null ? 'correct' : 'pending';
        return { question, index, answer, earned, max: question.points, status };
      }

      const earned = scoreQuestion(question, answer);
      const answered = isQuestionAnswered(question, answer);
      const status: PrintStatus | null = !answered ? null : earned >= question.points ? 'correct' : 'incorrect';
      return { question, index, answer, earned, max: question.points, status };
    });
  }, [aiEssayScores, answers, manualEssayScores, questions]);

  const wrongRows = useMemo(
    () => printRows.filter((r) => r.status === 'incorrect'),
    [printRows],
  );

  const fatalWrongRows = useMemo(
    () => wrongRows.filter((r) => r.question.isCritical),
    [wrongRows],
  );
  const isFatalFailed = fatalWrongRows.length > 0;

  const flaggedRows = useMemo(
    () => questions.filter((q) => flaggedQuestions[q.id]),
    [flaggedQuestions, questions],
  );

  return (
    <section className="mx-auto max-w-4xl rounded-[2.5rem] border border-white/80 bg-white/95 p-6 shadow-[0_25px_80px_-40px_rgba(2,132,199,0.5)] backdrop-blur-xl md:p-8 print:mx-0 print:max-w-none print:rounded-none print:border-0 print:bg-white print:p-0 print:shadow-none print:backdrop-blur-none">
      {isKidMode ? (
        /* ===== KID MODE RESULT SCREEN ===== */
        <div className="text-center">
          <p className="text-5xl">{result.percentage >= 90 ? '🏆' : result.percentage >= 70 ? '⭐' : result.percentage >= 50 ? '😊' : '💪'}</p>
          <h2 className="mt-2 text-3xl font-black text-slate-900">{t('kidResultTitle')}</h2>
          <p className="mt-1 text-2xl font-black text-cyan-600">{result.percentage.toFixed(0)}%</p>

          {/* Stars */}
          <div className="mt-3 flex justify-center gap-1.5 text-3xl">
            {Array.from({ length: 5 }).map((_, i) => (
              <span key={i} className={i < Math.round((result.percentage / 100) * 5) ? 'text-amber-400' : 'text-slate-200'}>⭐</span>
            ))}
          </div>
          <p className="mt-2 text-sm font-semibold text-slate-500">{t('kidResultStars')}: {Math.round((result.percentage / 100) * 5)}/5</p>

          {/* Grade message */}
          <p className="mt-4 rounded-2xl bg-gradient-to-r from-cyan-50 via-teal-50 to-sky-50 border border-teal-200 px-5 py-3.5 text-base font-bold text-teal-900 shadow-xs">
            {result.percentage >= 90 ? t('kidResultPerfect') : result.percentage >= 70 ? t('kidResultGreat') : result.percentage >= 50 ? t('kidResultGood') : t('kidResultTryAgain')}
          </p>

          {/* Stats compact */}
          <div className="mt-6 grid grid-cols-3 gap-3">
            <div className="rounded-2xl border border-slate-200 bg-slate-50/80 p-3.5 shadow-xs">
              <p className="text-2xl font-black text-slate-900">{result.answeredQuestions}</p>
              <p className="text-xs font-semibold text-slate-500">{t('answered')}</p>
            </div>
            <div className="rounded-2xl border border-emerald-200 bg-emerald-50/80 p-3.5 shadow-xs">
              <p className="text-2xl font-black text-emerald-700">{result.earnedPoints}</p>
              <p className="text-xs font-semibold text-slate-500">{t('pointsEarned')}</p>
            </div>
            <div className="rounded-2xl border border-amber-200 bg-amber-50/80 p-3.5 shadow-xs">
              <p className="text-2xl font-black text-amber-700">{kidBestStreak}</p>
              <p className="text-xs font-semibold text-slate-500">{t('kidBest')}</p>
            </div>
          </div>

          {/* Retest Mistakes Banner for Kids */}
          {wrongRows.length > 0 && onRetestMistakes && (
            <div className="mt-6 rounded-3xl border-2 border-rose-200 bg-gradient-to-r from-rose-50 via-white to-amber-50 p-5 shadow-sm">
              <div className="flex items-center justify-center gap-2 text-rose-800 font-extrabold text-base">
                <Target className="h-5 w-5" />
                <span>Bạn có {wrongRows.length} câu cần khắc phục!</span>
              </div>
              <p className="mt-1 text-xs text-slate-600">
                Thử sức lại ngay lúc này để ghi nhớ sâu và đạt trọn 100 điểm nhé!
              </p>
              <button
                type="button"
                onClick={() => onRetestMistakes(wrongRows.map((r) => r.question))}
                className="mt-3.5 inline-flex items-center gap-2 rounded-2xl bg-gradient-to-r from-rose-600 to-orange-500 px-6 py-3 text-sm font-black text-white shadow-md shadow-rose-300 hover:scale-[1.02] active:scale-[0.98] transition"
              >
                <RotateCcw className="h-4 w-4" />
                🔥 Chinh Phục Lại {wrongRows.length} Câu Sai Ngay
              </button>
            </div>
          )}

          {/* Action buttons */}
          <div className="mt-8 flex flex-col gap-3 print:hidden">
            <button
              type="button"
              onClick={onRestart}
              className="w-full rounded-2xl bg-gradient-to-r from-teal-500 via-cyan-500 to-sky-500 px-5 py-4 text-lg font-black text-white shadow-lg shadow-cyan-400/30 transition hover:opacity-95"
            >
              🔄 {t('restartAttempt') || 'Luyện Tập Lại Toàn Bộ'}
            </button>
            <button
              type="button"
              onClick={() => window.print()}
              className="inline-flex w-full items-center justify-center gap-2 rounded-2xl border border-slate-200 bg-white px-5 py-3 text-sm font-bold text-slate-700 hover:border-slate-300"
            >
              <Printer className="h-4 w-4" />
              🖨️ {t('printPdf') || 'In Báo Cáo Chẩn Đoán'}
            </button>
          </div>

          {/* Kid print sheet */}
          <div className="print-sheet mt-8 hidden border-t-2 border-slate-300 pt-4 print:block">
            <h2 className="text-center text-2xl font-black text-slate-900">📝 {t('kidResultTitle')}</h2>
            {user && (
              <p className="mt-2 text-center text-sm text-slate-700">
                <span className="font-semibold">{t('learner')}:</span> {user.name || user.email || t('userUnknown')} {user.email && user.name ? `(${user.email})` : ''}
              </p>
            )}
            <p className="mt-1 text-center text-base font-bold text-cyan-700">{result.percentage.toFixed(0)}% — {result.percentage >= 90 ? t('kidResultPerfect') : result.percentage >= 70 ? t('kidResultGreat') : result.percentage >= 50 ? t('kidResultGood') : t('kidResultTryAgain')}</p>
            <p className="mt-1 text-center text-sm text-slate-500">{t('printedAt')}: {now.toLocaleString()}</p>

            {wrongRows.length === 0 ? (
              <p className="mt-6 text-center text-lg font-bold text-emerald-600">🎉 {t('kidResultPerfect')} — 0 câu sai!</p>
            ) : (
              <>
                <p className="mt-4 text-center text-sm font-semibold text-rose-600">❌ Câu cần xem lại: {wrongRows.length} câu</p>
                <div className="mt-5 space-y-5">
                  {wrongRows.map((row, pos) => (
                    <article key={`kid-print-${row.question.id}`} className="rounded-xl border-2 border-rose-200 bg-rose-50 p-4">
                      <p className="text-base font-bold text-slate-900">❓ Câu {pos + 1}: {row.question.content}</p>
                      {renderQuestionDetail(row.question, row.answer, t)}
                    </article>
                  ))}
                </div>
              </>
            )}
          </div>
        </div>
      ) : (
      /* ===== STANDARD / HIGH SCHOOL RESULT SCREEN ===== */
      <>
        {/* Score Header */}
        <div className="text-center">
          <p className="text-xs font-black uppercase tracking-[0.12em] text-teal-600">Báo Cáo Khảo Thí & Chẩn Đoán Năng Lực</p>
          <h2 className="mt-1.5 text-4xl font-black tracking-tight text-slate-900 md:text-5xl">{result.percentage.toFixed(1)}%</h2>
          {isFatalFailed ? (
            <p className="mt-2 inline-flex items-center gap-1.5 rounded-full bg-rose-100 px-4 py-1 text-sm font-black text-rose-700">
              <AlertTriangle className="h-4 w-4" /> ⛔ KHÔNG ĐẠT (TRƯỢT VÌ SAI CÂU ĐIỂM LIỆT)
            </p>
          ) : (
            <p className="mt-2 text-base font-bold text-slate-600">{gradeLabel(result.percentage, t)}</p>
          )}
          {essayQuestions.length > 0 && (
            <p className="mt-1 text-sm font-semibold text-cyan-700">{t('finalScoreWithEssay')}: {finalPercentage.toFixed(1)}%</p>
          )}
        </div>

        {/* 4 Stat Boxes */}
        <div className="mt-7 grid gap-3 grid-cols-2 md:grid-cols-4">
          <article className="rounded-2xl border border-slate-200 bg-slate-50/80 p-4 shadow-xs">
            <p className="text-xs font-bold uppercase tracking-wider text-slate-500">{t('answered')}</p>
            <p className="mt-1 text-xl font-black text-slate-900">{result.answeredQuestions}/{result.totalQuestions}</p>
          </article>
          <article className="rounded-2xl border border-emerald-200 bg-emerald-50/80 p-4 shadow-xs">
            <p className="text-xs font-bold uppercase tracking-wider text-emerald-700">Chính xác</p>
            <p className="mt-1 text-xl font-black text-emerald-800">{printRows.filter(r => r.status === 'correct').length} câu</p>
          </article>
          <article className={`rounded-2xl border p-4 shadow-xs ${isFatalFailed ? 'border-rose-400 bg-rose-50/90' : 'border-rose-200 bg-rose-50/80'}`}>
            <p className="text-xs font-bold uppercase tracking-wider text-rose-700">
              {isFatalFailed ? '⚠️ Điểm liệt sai' : 'Cần khắc phục'}
            </p>
            <p className="mt-1 text-xl font-black text-rose-800">
              {isFatalFailed ? `${fatalWrongRows.length} câu liệt` : `${wrongRows.length} câu`}
            </p>
          </article>
          <article className="rounded-2xl border border-sky-200 bg-sky-50/80 p-4 shadow-xs">
            <p className="text-xs font-bold uppercase tracking-wider text-sky-700">{t('pointsEarned')}</p>
            <p className="mt-1 text-xl font-black text-sky-900">{result.earnedPoints} / {result.totalPoints}</p>
          </article>
        </div>

        {/* ─── FATAL CRITICAL QUESTION DANGER BANNER ─────────────────────── */}
        {isFatalFailed && (
          <div className="mt-6 rounded-3xl border-2 border-rose-500 bg-gradient-to-r from-rose-100 via-rose-50 to-red-50 p-5 shadow-lg shadow-rose-200">
            <div className="flex items-start gap-3.5">
              <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-2xl bg-rose-600 text-white shadow-md shadow-rose-300 animate-pulse">
                <AlertTriangle className="h-6 w-6" />
              </div>
              <div className="flex-1">
                <h4 className="text-base font-black text-rose-950">
                  CẢNH BÁO QUY CHẾ: BẠN ĐÃ TRẢ LỜI SAI {fatalWrongRows.length} CÂU HỎI ĐIỂM LIỆT!
                </h4>
                <p className="mt-1 text-xs font-medium text-rose-800 leading-relaxed">
                  Theo Quy chuẩn Quốc gia của Cục Đường bộ Việt Nam, sai dù chỉ 1 câu điểm liệt sẽ bị TRƯỢT TRỰC TIẾP toàn bộ kỳ thi sát hạch lý thuyết lái xe, bất kể số điểm các câu khác đạt tối đa.
                </p>
              </div>
            </div>

            {onRetestMistakes && (
              <div className="mt-4 flex flex-wrap gap-3">
                <button
                  type="button"
                  onClick={() => onRetestMistakes(fatalWrongRows.map((r) => r.question))}
                  className="inline-flex items-center gap-2 rounded-2xl bg-rose-600 px-5 py-3 text-sm font-black text-white shadow-md hover:bg-rose-700 hover:scale-[1.02] active:scale-[0.98] transition"
                >
                  <RotateCcw className="h-4 w-4" />
                  🔥 Chinh Phục Lại Riêng {fatalWrongRows.length} Câu Điểm Liệt Này
                </button>
                <button
                  type="button"
                  onClick={() => onRetestMistakes(wrongRows.map((r) => r.question))}
                  className="inline-flex items-center gap-2 rounded-2xl border border-rose-300 bg-white px-5 py-3 text-sm font-bold text-rose-800 hover:bg-rose-50 transition"
                >
                  Luyện lại tất cả {wrongRows.length} câu sai
                </button>
              </div>
            )}
          </div>
        )}

        {/* ─── RETEST MISTAKES HERO BANNER (Standard) ─────────────────────── */}
        {!isFatalFailed && wrongRows.length > 0 && (
          <div className="mt-6 flex flex-col sm:flex-row items-center justify-between gap-4 rounded-3xl border-2 border-rose-300 bg-gradient-to-r from-rose-50/90 via-white to-amber-50/80 p-5 shadow-lg shadow-rose-100/60">
            <div className="flex items-center gap-3.5">
              <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-2xl bg-gradient-to-br from-rose-600 to-orange-500 text-white shadow-md shadow-rose-300">
                <Target className="h-6 w-6" />
              </div>
              <div>
                <h4 className="text-base font-black text-slate-900">
                  Chẩn đoán: Có {wrongRows.length} câu cần khắc phục để bứt phá điểm số!
                </h4>
                <p className="text-xs font-medium text-slate-600">
                  Khác với IOE giấu câu sai, AegisQuiz giúp bạn học từ vấp ngã để làm chủ 100% kiến thức.
                </p>
              </div>
            </div>

            {onRetestMistakes && (
              <button
                type="button"
                onClick={() => onRetestMistakes(wrongRows.map((r) => r.question))}
                className="flex-shrink-0 inline-flex items-center gap-2 rounded-2xl bg-gradient-to-r from-rose-600 to-orange-600 px-5 py-3.5 text-sm font-black text-white shadow-lg shadow-rose-300 hover:scale-[1.02] active:scale-[0.98] transition"
              >
                <RotateCcw className="h-4 w-4" />
                🔥 Chinh Phục Lại {wrongRows.length} Câu Sai
              </button>
            )}
          </div>
        )}

        {/* ─── INTERACTIVE TAB BAR ────────────────────────────────────────── */}
        <div className="mt-7 flex rounded-2xl bg-slate-100 p-1.5 font-black text-xs md:text-sm print:hidden">
          <button
            type="button"
            onClick={() => setActiveTab('overview')}
            className={`flex-1 rounded-xl py-2.5 transition ${
              activeTab === 'overview' ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            📊 Tổng Quan & Phân Tích
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('mistakes')}
            className={`flex-1 rounded-xl py-2.5 transition flex items-center justify-center gap-1.5 ${
              activeTab === 'mistakes' ? 'bg-white text-rose-700 shadow-sm' : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            ❌ Mổ Xẻ Câu Sai ({wrongRows.length})
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('all')}
            className={`flex-1 rounded-xl py-2.5 transition ${
              activeTab === 'all' ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            📋 Chi Tiết Toàn Bộ Đề ({questions.length})
          </button>
        </div>

        {/* ─── TAB 1: OVERVIEW ────────────────────────────────────────────── */}
        {activeTab === 'overview' && (
          <div className="mt-6 space-y-6">
            {/* Topic Performance */}
            <div className="rounded-3xl border border-slate-200 bg-slate-50/80 p-5 shadow-xs">
              <h3 className="text-xs font-black uppercase tracking-wider text-slate-600">{t('topicReview') || 'Phân Tích Năng Lực Từng Chuyên Đề'}</h3>
              <div className="mt-3 space-y-2.5">
                {result.topicResults.map((topic) => (
                  <div key={topic.topicKey} className="grid grid-cols-[1.5fr,1fr,1fr] items-center gap-3 rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm shadow-xs">
                    <div className="font-bold text-slate-800 truncate">{topic.topicLabel}</div>
                    <div className="text-slate-500 text-xs font-semibold">{t('progress')}: {topic.answeredQuestions}/{topic.totalQuestions}</div>
                    <div className="text-right font-black text-cyan-700">{topic.percentage.toFixed(0)}%</div>
                  </div>
                ))}
              </div>
            </div>

            {/* Topic Performance Progress Bars */}
            <div className="rounded-3xl border border-slate-200 bg-slate-50/80 p-5 shadow-xs">
              <h3 className="text-xs font-black uppercase tracking-wider text-slate-600">{t('topicPerformanceChart') || 'Thước Đo Làm Chủ Kiến Thức'}</h3>
              <div className="mt-4 space-y-3.5">
                {result.topicResults.map((topic) => (
                  <div key={`chart-${topic.topicKey}`}>
                    <div className="mb-1.5 flex items-center justify-between text-xs font-bold text-slate-700">
                      <span>{topic.topicLabel}</span>
                      <span className={topic.percentage >= 80 ? 'text-emerald-700' : topic.percentage >= 50 ? 'text-amber-700' : 'text-rose-700'}>
                        {topic.percentage.toFixed(0)}%
                      </span>
                    </div>
                    <div className="h-2.5 rounded-full bg-slate-200 overflow-hidden">
                      <div
                        className={`h-full rounded-full transition-all duration-500 ${
                          topic.percentage >= 80 
                            ? 'bg-gradient-to-r from-emerald-400 to-teal-500' 
                            : topic.percentage >= 50 
                            ? 'bg-gradient-to-r from-amber-400 to-orange-500' 
                            : 'bg-gradient-to-r from-rose-500 to-red-600'
                        }`}
                        style={{ width: `${Math.max(0, Math.min(100, topic.percentage))}%` }}
                      />
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* ─── TAB 2: MISTAKES BREAKDOWN (HỌC TỪ SAI LẦM) ────────────────── */}
        {activeTab === 'mistakes' && (
          <div className="mt-6 space-y-5">
            {wrongRows.length === 0 ? (
              <div className="rounded-3xl border-2 border-emerald-200 bg-emerald-50/80 p-8 text-center shadow-xs">
                <p className="text-4xl">🎉</p>
                <h4 className="mt-2 text-xl font-black text-emerald-900">Hoàn Hảo Tuyệt Đối — Không Có Câu Nào Sai!</h4>
                <p className="mt-1 text-sm font-medium text-emerald-700">Bạn đã làm chủ 100% nội dung kiến thức trong bài khảo thí này.</p>
              </div>
            ) : (
              wrongRows.map((row, pos) => (
                <article key={`wrong-${row.question.id}`} className="overflow-hidden rounded-3xl border-2 border-rose-200/90 bg-white p-5 md:p-6 shadow-md shadow-rose-100/50">
                  <div className="flex flex-wrap items-center justify-between gap-2 border-b border-rose-100 pb-3">
                    <div className="flex items-center gap-2">
                      <span className="flex h-7 w-7 items-center justify-center rounded-xl bg-rose-600 text-xs font-black text-white">
                        {pos + 1}
                      </span>
                      <span className="text-xs font-black uppercase tracking-wider text-rose-800">
                        {t('questionLabel')} {row.index + 1} • {row.question.questionType}
                      </span>
                    </div>
                    <span className="rounded-full bg-rose-100 px-2.5 py-0.5 text-xs font-bold text-rose-700">
                      0 / {row.max} Điểm
                    </span>
                  </div>

                  <div className="mt-4 text-base font-bold text-slate-900 leading-relaxed">
                    <MathRenderer content={row.question.content} />
                  </div>

                  {/* Detailed Option Comparison */}
                  {renderQuestionDetail(row.question, row.answer, t)}

                  {/* Omni Flash Insight / AI Explanation */}
                  {(row.question.aiExplanation || row.question.citation) && (
                    <div className="mt-4 rounded-2xl border border-indigo-200/90 bg-gradient-to-br from-indigo-50/90 via-white to-sky-50/80 p-4 text-sm leading-relaxed text-slate-800 shadow-xs">
                      <div className="flex items-center gap-2 text-indigo-800 font-extrabold text-xs uppercase tracking-wide mb-2">
                        <Lightbulb className="h-4 w-4 text-indigo-600" />
                        <span>Mổ Xẻ Sai Lầm & Khắc Sâu Kiến Thức</span>
                      </div>
                      {row.question.aiExplanation && (
                        <div className="text-slate-700 font-medium">
                          <MathRenderer content={row.question.aiExplanation} />
                        </div>
                      )}
                      {row.question.citation && (
                        <div className="mt-2 text-xs font-semibold text-slate-500">
                          📌 Căn cứ khảo thí: <MathRenderer content={row.question.citation} inline />
                        </div>
                      )}
                    </div>
                  )}
                </article>
              ))
            )}
          </div>
        )}

        {/* ─── TAB 3: ALL QUESTIONS REVIEW ───────────────────────────────── */}
        {activeTab === 'all' && (
          <div className="mt-6 space-y-4">
            {printRows.map((row) => (
              <article key={`all-${row.question.id}`} className="rounded-3xl border border-slate-200 bg-white p-5 shadow-xs">
                <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 pb-3">
                  <span className="text-xs font-black uppercase tracking-wider text-slate-600">
                    {t('questionLabel')} {row.index + 1}
                  </span>
                  <div className="flex items-center gap-2">
                    <span className="text-xs font-bold text-slate-500">{row.earned}/{row.max} Đ</span>
                    {row.status && (
                      <span className={`rounded-full px-2.5 py-0.5 text-xs font-bold border ${statusClass(row.status)}`}>
                        {statusLabel(row.status, t)}
                      </span>
                    )}
                  </div>
                </div>

                <div className="mt-3 text-sm font-bold text-slate-900 leading-relaxed">
                  <MathRenderer content={row.question.content} />
                </div>

                {renderQuestionDetail(row.question, row.answer, t)}
              </article>
            ))}
          </div>
        )}

        {/* Flagged Review */}
        {flaggedRows.length > 0 && (
          <div className="mt-6 rounded-3xl border border-amber-200 bg-amber-50/80 p-5 shadow-xs">
            <h3 className="text-xs font-black uppercase tracking-wider text-amber-900 flex items-center gap-1.5">
              <AlertTriangle className="h-4 w-4 text-amber-600" />
              {t('flaggedReview') || 'Câu Hỏi Đã Đặt Cờ Cần Xem Lại'}
            </h3>
            <div className="mt-3 space-y-2">
              {flaggedRows.map((q, idx) => (
                <div key={`flagged-${q.id}`} className="rounded-2xl border border-amber-200 bg-white px-4 py-2.5 text-sm text-slate-700 shadow-xs">
                  {t('questionLabel')} {idx + 1}: <MathRenderer content={q.content} inline />
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Action Buttons */}
        <div className="mt-8 flex flex-col sm:flex-row gap-3.5 print:hidden">
          <button
            type="button"
            onClick={onRestart}
            className="flex-1 rounded-2xl bg-gradient-to-r from-teal-500 via-cyan-500 to-sky-500 px-5 py-4 text-base font-black text-white shadow-lg shadow-cyan-400/30 transition hover:opacity-95"
          >
            🔄 {t('restartAttempt') || 'Luyện Tập Lại Toàn Bộ'}
          </button>

          <button
            type="button"
            onClick={() => window.print()}
            className="inline-flex items-center justify-center gap-2 rounded-2xl border-2 border-slate-200 bg-white px-5 py-4 text-base font-bold text-slate-700 hover:border-slate-300 transition"
          >
            <Printer className="h-5 w-5 text-slate-600" />
            {t('printPdf') || 'In Báo Cáo Chẩn Đoán'}
          </button>
        </div>

        {/* Print Sheet for Parents / Teachers */}
        <div className="print-sheet mt-8 hidden border-t-2 border-slate-300 pt-4 print:block">
          <h2 className="text-center text-2xl font-black text-slate-900">PHIẾU CHẨN ĐOÁN NĂNG LỰC & HỌC TẬP TỪ SAI LẦM</h2>
          {user && (
            <p className="mt-2 text-center text-sm text-slate-700">
              <span className="font-semibold">{t('learner')}:</span> {user.name || user.email || t('userUnknown')} {user.email && user.name ? `(${user.email})` : ''}
            </p>
          )}
          <p className="mt-1 text-center text-sm font-bold text-slate-500">{t('printedAt')}: {now.toLocaleString()}</p>
          <p className="mt-1 text-center text-sm font-black text-cyan-700">
            Kết quả: {finalPercentage.toFixed(1)}% • Số câu cần khắc phục: {wrongRows.length}/{questions.length}
          </p>

          <div className="mt-5 space-y-4">
            {printRows.map((row) => (
              <article key={`print-${row.question.id}`} className="print-question border-b border-slate-300 pb-3">
                <h4 className="text-base font-bold text-slate-900">
                  {t('questionLabel')} {row.index + 1}: {row.question.content}
                </h4>
                <div className="mt-1 flex items-center gap-2 text-xs font-semibold">
                  <span className={`rounded-full border px-2 py-0.5 ${statusClass(row.status || 'incorrect')}`}>
                    {statusLabel(row.status || 'incorrect', t)}
                  </span>
                  <span>Điểm: {row.earned}/{row.max}</span>
                </div>
                {renderQuestionDetail(row.question, row.answer, t)}
              </article>
            ))}
          </div>
        </div>
      </>
      )}
    </section>
  );
}
