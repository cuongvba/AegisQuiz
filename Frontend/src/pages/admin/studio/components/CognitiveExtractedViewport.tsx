import React from 'react';
import { useTranslation } from 'react-i18next';
import { Check, Trash2, CheckCircle2, AlertTriangle, Calculator, Sparkles } from 'lucide-react';
import type { StudioQuestionItem } from '../hooks/useSplitStudioData';

interface CognitiveExtractedViewportProps {
  scrollRef: React.RefObject<HTMLDivElement | null>;
  onScroll: () => void;
  questions: StudioQuestionItem[];
  activeQuestionId: string | null;
  onSelectQuestion: (id: string) => void;
  onUpdateText: (id: string, text: string) => void;
  onToggleCorrect: (qId: string, optKey: string) => void;
  onDeleteQuestion: (id: string) => void;
  filterType: 'ALL' | 'REVIEW' | 'VERIFIED' | 'MATH';
  onFilterChange: (type: 'ALL' | 'REVIEW' | 'VERIFIED' | 'MATH') => void;
}

export const CognitiveExtractedViewport: React.FC<CognitiveExtractedViewportProps> = ({
  scrollRef,
  onScroll,
  questions,
  activeQuestionId,
  onSelectQuestion,
  onUpdateText,
  onToggleCorrect,
  onDeleteQuestion,
  filterType,
  onFilterChange,
}) => {
  const { t } = useTranslation('split_studio');

  return (
    <div className="flex flex-col h-full bg-slate-950/40">
      {/* Sub-header & Bộ lọc */}
      <div className="h-12 px-4 border-b border-slate-800/80 bg-slate-900/90 flex items-center justify-between shrink-0">
        <span className="text-xs font-semibold text-slate-300">
          {t('extractedViewport.totalCount', { count: questions.length })}
        </span>

        {/* Filter Pills */}
        <div className="flex items-center gap-1.5 bg-slate-850 p-1 rounded-xl border border-slate-800 text-xs">
          <button
            onClick={() => onFilterChange('ALL')}
            className={`px-2.5 py-1 rounded-lg font-medium transition-all ${
              filterType === 'ALL' ? 'bg-indigo-600 text-white shadow' : 'text-slate-400 hover:text-white'
            }`}
          >
            {t('extractedViewport.filterAll')}
          </button>
          <button
            onClick={() => onFilterChange('REVIEW')}
            className={`px-2.5 py-1 rounded-lg font-medium transition-all ${
              filterType === 'REVIEW' ? 'bg-amber-600 text-white shadow' : 'text-slate-400 hover:text-white'
            }`}
          >
            {t('extractedViewport.filterNeedsReview', { count: 1 })}
          </button>
          <button
            onClick={() => onFilterChange('MATH')}
            className={`px-2.5 py-1 rounded-lg font-medium transition-all ${
              filterType === 'MATH' ? 'bg-purple-600 text-white shadow' : 'text-slate-400 hover:text-white'
            }`}
          >
            <span className="flex items-center gap-1">
              <Calculator className="w-3 h-3" />
              {t('extractedViewport.filterMath')}
            </span>
          </button>
        </div>
      </div>

      {/* Danh sách câu hỏi bóc tách */}
      <div
        ref={scrollRef}
        onScroll={onScroll}
        className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-5"
      >
        {questions.map((q, idx) => {
          const isActive = activeQuestionId === q.id;
          return (
            <div
              key={q.id}
              onClick={() => onSelectQuestion(q.id)}
              className={`p-5 rounded-2xl transition-all border ${
                isActive
                  ? 'bg-slate-900/90 border-indigo-500/50 shadow-xl shadow-indigo-500/5 ring-1 ring-indigo-500/30'
                  : 'bg-slate-900/40 border-slate-800/80 hover:border-slate-700'
              }`}
            >
              {/* Question Card Meta */}
              <div className="flex flex-wrap items-center justify-between gap-2 mb-3 pb-3 border-b border-slate-800/80">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-bold text-white bg-slate-800 px-2.5 py-1 rounded-lg">
                    {t('extractedViewport.questionNum', { num: idx + 1 })}
                  </span>
                  <span className="text-[11px] font-semibold text-emerald-400 bg-emerald-500/10 px-2 py-0.5 rounded-full border border-emerald-500/20">
                    {t('extractedViewport.confidenceScore', { score: q.confidenceScore })}
                  </span>
                  <span className="text-[11px] font-medium text-slate-400 bg-slate-800/60 px-2 py-0.5 rounded-md">
                    {t('extractedViewport.taxonomySubject', { subject: q.subject })}
                  </span>
                </div>

                <div className="flex items-center gap-2">
                  {q.isVerified ? (
                    <span className="flex items-center gap-1 text-[11px] font-semibold text-emerald-400">
                      <CheckCircle2 className="w-3.5 h-3.5" />
                      {t('extractedViewport.correctBadge')}
                    </span>
                  ) : (
                    <span className="flex items-center gap-1 text-[11px] font-semibold text-amber-400">
                      <AlertTriangle className="w-3.5 h-3.5" />
                      {t('extractedViewport.filterNeedsReview', { count: '' })}
                    </span>
                  )}
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      onDeleteQuestion(q.id);
                    }}
                    className="p-1 text-slate-500 hover:text-rose-400 rounded transition-colors"
                    title={t('extractedViewport.deleteAction')}
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>

              {/* Textarea chỉnh sửa câu hỏi */}
              <div className="mb-3">
                <textarea
                  value={q.questionText}
                  onChange={(e) => onUpdateText(q.id, e.target.value)}
                  rows={2}
                  className="w-full text-xs sm:text-sm font-medium bg-slate-950/80 border border-slate-800 rounded-xl p-3 text-slate-100 focus:outline-none focus:border-indigo-500 transition-colors"
                />
              </div>

              {/* Danh sách phương án trắc nghiệm */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 mb-3">
                {q.options.map((opt) => (
                  <div
                    key={opt.key}
                    onClick={(e) => {
                      e.stopPropagation();
                      onToggleCorrect(q.id, opt.key);
                    }}
                    className={`flex items-center justify-between p-2.5 rounded-xl border cursor-pointer transition-all ${
                      opt.isCorrect
                        ? 'bg-emerald-500/15 border-emerald-500/40 text-emerald-200'
                        : 'bg-slate-950/40 border-slate-800/80 text-slate-300 hover:border-slate-700'
                    }`}
                  >
                    <div className="flex items-center gap-2 text-xs">
                      <span className="font-bold w-4">{opt.key}.</span>
                      <span>{opt.text}</span>
                    </div>
                    {opt.isCorrect && <Check className="w-3.5 h-3.5 text-emerald-400 shrink-0" />}
                  </div>
                ))}
              </div>

              {/* Rubric Barem Điểm AI */}
              {q.rubricScore && (
                <div className="p-2.5 rounded-xl bg-purple-500/10 border border-purple-500/20 text-purple-200 text-xs flex items-start gap-2">
                  <Sparkles className="w-3.5 h-3.5 text-purple-400 shrink-0 mt-0.5" />
                  <div>
                    <span className="font-bold block mb-0.5">{t('extractedViewport.rubricScore')}:</span>
                    <span>{q.rubricScore}</span>
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};
