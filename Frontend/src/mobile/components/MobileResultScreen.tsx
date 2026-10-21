import React, { useState, useMemo } from 'react';
import { Trophy, CheckCircle2, XCircle, RotateCcw, Printer, ArrowLeft, Flame, Eye } from 'lucide-react';
import { MathRenderer } from '@/components/common/MathRenderer';
import { useLearnerI18n } from '@/lib/i18n';
import { PracticeQuestionItem } from '../types/mobile-practice.types';
import { evaluateMobileSession } from '../utils/mobile-scoring-adapter';

interface MobileResultScreenProps {
  questions: PracticeQuestionItem[];
  selectedAnswers: Record<number, string>;
  topicName: string;
  onRetestMistakes: (mistakeQuestions: PracticeQuestionItem[]) => void;
  onRestartAll: () => void;
  onExit: () => void;
}

export const MobileResultScreen: React.FC<MobileResultScreenProps> = ({
  questions,
  selectedAnswers,
  topicName,
  onRetestMistakes,
  onRestartAll,
  onExit,
}) => {
  const { t } = useLearnerI18n();
  const [filterMode, setFilterMode] = useState<'all' | 'wrong' | 'correct'>('all');

  // Đánh giá đúng/sai từng câu kế thừa 100% thuật toán scoring từ Web Quiz Helpers
  const evaluation = useMemo(
    () => evaluateMobileSession(questions, selectedAnswers),
    [questions, selectedAnswers]
  );

  const filteredItems = useMemo(() => {
    if (filterMode === 'wrong') return evaluation.evaluatedQuestions.filter((x) => !x.isCorrect && !x.isManualGrading);
    if (filterMode === 'correct') return evaluation.evaluatedQuestions.filter((x) => x.isCorrect);
    return evaluation.evaluatedQuestions;
  }, [evaluation.evaluatedQuestions, filterMode]);

  const handlePrintPdf = () => {
    window.print();
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col font-sans select-none pb-20">
      {/* ── Top Bar ── */}
      <div className="sticky top-0 z-40 px-4 py-3 bg-slate-950/90 backdrop-blur-xl border-b border-slate-800 flex items-center justify-between print:hidden">
        <button
          type="button"
          onClick={onExit}
          className="p-2 rounded-xl bg-slate-900 border border-slate-800 text-slate-300 hover:text-white cursor-pointer"
        >
          <ArrowLeft size={18} />
        </button>
        <div className="text-center">
          <h2 className="text-xs font-black text-white">{t('examSummary') || 'Tổng kết kết quả'}</h2>
          <span className="text-[10px] text-slate-400 block truncate max-w-[200px]">{topicName}</span>
        </div>
        <button
          type="button"
          onClick={handlePrintPdf}
          className="p-2 rounded-xl bg-slate-900 border border-slate-800 text-sky-400 hover:text-sky-300 cursor-pointer"
          title="Xuất PDF chi tiết"
        >
          <Printer size={18} />
        </button>
      </div>

      {/* ── Thống kê tổng quan ── */}
      <div className="p-4 space-y-4">
        <div className="p-5 rounded-3xl bg-gradient-to-br from-slate-900 via-slate-900/90 to-cyan-950/50 border border-slate-800 shadow-xl text-center space-y-3">
          <div className="w-14 h-14 rounded-2xl bg-cyan-500/20 text-cyan-400 flex items-center justify-center mx-auto shadow-inner">
            <Trophy size={28} />
          </div>
          <div>
            <div className="text-3xl font-black text-white tracking-tight">
              {evaluation.accuracy}%
            </div>
            <p className="text-xs text-slate-400 mt-0.5">
              {evaluation.accuracy >= 80 ? 'Xuất sắc! Nắm vững kiến thức cốt lõi!' : evaluation.accuracy >= 50 ? 'Khá tốt! Hãy luyện thêm các câu chưa đúng.' : 'Cần nỗ lực thêm để bứt phá!'}
            </p>
          </div>

          <div className="grid grid-cols-3 gap-2 pt-2 border-t border-slate-800/80">
            <div className="p-2 rounded-xl bg-emerald-950/40 border border-emerald-900/50">
              <span className="text-[10px] font-bold text-emerald-400 block">{t('correctAnswersCount') || 'Đúng'}</span>
              <span className="text-base font-black text-emerald-300">{evaluation.correctCount}</span>
            </div>
            <div className="p-2 rounded-xl bg-rose-950/40 border border-rose-900/50">
              <span className="text-[10px] font-bold text-rose-400 block">{t('wrongAnswersCount') || 'Sai'}</span>
              <span className="text-base font-black text-rose-300">{evaluation.wrongCount}</span>
            </div>
            <div className="p-2 rounded-xl bg-slate-900 border border-slate-800">
              <span className="text-[10px] font-bold text-slate-400 block">{t('unansweredCount') || 'Bỏ qua'}</span>
              <span className="text-base font-black text-slate-300">{evaluation.unansweredCount}</span>
            </div>
          </div>
        </div>

        {/* ── Nút Hành Động Trọng Tâm: Chinh phục lại câu sai ── */}
        <div className="grid grid-cols-2 gap-2 print:hidden">
          {evaluation.mistakeQuestions.length > 0 ? (
            <button
              type="button"
              onClick={() => onRetestMistakes(evaluation.mistakeQuestions)}
              className="py-3 px-3 rounded-2xl bg-gradient-to-r from-rose-500 to-amber-500 text-slate-950 font-black text-xs flex items-center justify-center gap-1.5 shadow-lg active:scale-95 cursor-pointer"
            >
              <Flame size={15} />
              <span>{t('retestMistakes') || 'Chinh phục lại câu sai'} ({evaluation.mistakeQuestions.length})</span>
            </button>
          ) : (
            <div className="py-3 px-3 rounded-2xl bg-emerald-950/60 border border-emerald-800 text-emerald-300 font-bold text-xs flex items-center justify-center gap-1.5">
              <CheckCircle2 size={15} />
              <span>Hoàn hảo! Không có câu sai</span>
            </div>
          )}

          <button
            type="button"
            onClick={handlePrintPdf}
            className="py-3 px-3 rounded-2xl bg-slate-900 border border-slate-800 text-sky-400 hover:text-white font-black text-xs flex items-center justify-center gap-1.5 cursor-pointer"
          >
            <Printer size={15} />
            <span>{t('exportPdf') || 'Xuất file PDF chi tiết'}</span>
          </button>
        </div>

        {/* ── Bộ lọc xem lại chi tiết ── */}
        <div className="space-y-3 pt-2">
          <div className="flex items-center justify-between px-1 print:hidden">
            <span className="text-xs font-black uppercase text-slate-400 tracking-wider flex items-center gap-1.5">
              <Eye size={13} /> {t('reviewMistakes') || 'Xem chi tiết bài làm'}
            </span>
            <div className="inline-flex rounded-xl bg-slate-900 border border-slate-800 p-0.5 text-[10px] font-bold">
              <button
                type="button"
                onClick={() => setFilterMode('all')}
                className={`px-2 py-1 rounded-lg cursor-pointer ${filterMode === 'all' ? 'bg-slate-800 text-white' : 'text-slate-400'}`}
              >
                Tất cả ({questions.length})
              </button>
              <button
                type="button"
                onClick={() => setFilterMode('wrong')}
                className={`px-2 py-1 rounded-lg cursor-pointer ${filterMode === 'wrong' ? 'bg-rose-950 text-rose-300' : 'text-slate-400'}`}
              >
                Sai ❌ ({evaluation.wrongCount + evaluation.unansweredCount})
              </button>
              <button
                type="button"
                onClick={() => setFilterMode('correct')}
                className={`px-2 py-1 rounded-lg cursor-pointer ${filterMode === 'correct' ? 'bg-emerald-950 text-emerald-300' : 'text-slate-400'}`}
              >
                Đúng ✅ ({evaluation.correctCount})
              </button>
            </div>
          </div>

          {/* Danh sách từng câu */}
          <div className="space-y-3">
            {filteredItems.map(({ question: q, idx, userDisplayLabel, correctDisplayLabel, isCorrect, isAnswered, isManualGrading }) => (
              <div
                key={q.id || idx}
                className={`p-4 rounded-2xl border transition-all ${
                  isManualGrading
                    ? 'bg-amber-950/20 border-amber-900/50'
                    : isCorrect
                    ? 'bg-slate-900/60 border-slate-800'
                    : 'bg-rose-950/20 border-rose-900/50'
                }`}
              >
                <div className="flex items-center justify-between gap-2 mb-2">
                  <span className="text-[11px] font-mono font-bold text-slate-400">
                    Câu {idx + 1}
                  </span>
                  <span
                    className={`px-2 py-0.5 rounded-full text-[10px] font-black flex items-center gap-1 ${
                      isManualGrading
                        ? 'bg-amber-950 text-amber-400 border border-amber-800'
                        : isCorrect
                        ? 'bg-emerald-950 text-emerald-400 border border-emerald-800'
                        : isAnswered
                        ? 'bg-rose-950 text-rose-400 border border-rose-800'
                        : 'bg-slate-800 text-slate-400'
                    }`}
                  >
                    {isManualGrading ? (
                      <span>📝 Chấm thủ công</span>
                    ) : isCorrect ? (
                      <>
                        <CheckCircle2 size={11} />
                        <span>Đúng</span>
                      </>
                    ) : (
                      <>
                        <XCircle size={11} />
                        <span>{isAnswered ? 'Sai' : 'Chưa trả lời'}</span>
                      </>
                    )}
                  </span>
                </div>

                <div className="text-xs font-medium text-white mb-3 leading-relaxed">
                  <MathRenderer content={q.content} />
                </div>

                <div className="space-y-1.5 text-xs">
                  <div className="flex items-start gap-1.5">
                    <span className="text-slate-400 text-[11px] shrink-0">Bạn chọn:</span>
                    <span className={`font-mono font-bold ${
                      isManualGrading ? 'text-amber-300' : isCorrect ? 'text-emerald-400' : 'text-rose-400'
                    }`}>
                      {userDisplayLabel}
                    </span>
                  </div>
                  {isManualGrading ? (
                    <div className="p-2 rounded-xl bg-amber-950/40 border border-amber-800/50 text-[11px] text-amber-300 font-bold flex items-center gap-1">
                      📝 Chấm thủ công — Đang chờ giáo viên/giám khảo chấm điểm
                    </div>
                  ) : !isCorrect ? (
                    <div className="flex items-start gap-1.5">
                      <span className="text-slate-400 text-[11px] shrink-0">Đáp án đúng:</span>
                      <span className="font-mono font-bold text-emerald-400">
                        {correctDisplayLabel}
                      </span>
                    </div>
                  ) : null}
                </div>

                {q.explanation && (
                  <div className="mt-3 p-2.5 rounded-xl bg-slate-950/80 border border-slate-800 text-[11px] text-slate-300 leading-relaxed">
                    <span className="font-bold text-purple-400">💡 Giải thích: </span>
                    <MathRenderer content={q.explanation} inline />
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* Nút hành động chân trang */}
        <div className="pt-4 flex items-center gap-2 print:hidden">
          <button
            type="button"
            onClick={onRestartAll}
            className="flex-1 py-3 rounded-2xl bg-slate-900 border border-slate-800 text-xs font-bold text-slate-300 hover:text-white flex items-center justify-center gap-1.5 cursor-pointer"
          >
            <RotateCcw size={14} /> {t('retakeAll') || 'Làm lại từ đầu'}
          </button>
          <button
            type="button"
            onClick={onExit}
            className="flex-1 py-3 rounded-2xl bg-slate-800 text-xs font-bold text-white hover:bg-slate-700 cursor-pointer"
          >
            Về Trang Chủ
          </button>
        </div>
      </div>
    </div>
  );
};
