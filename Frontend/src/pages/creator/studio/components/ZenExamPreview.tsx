import React from 'react';
import { useTranslation } from 'react-i18next';
import { RotateCcw, CopyCheck } from 'lucide-react';
import { ZenQuestionPatch, ZenUploadResult } from '../types/zen-studio.types';
import { ZenTaxonomyBadge } from './ZenTaxonomyBadge';
import { ZenQuestionCard } from './ZenQuestionCard';
import { ZenPaginationBar } from './ZenPaginationBar';
import { useZenPagination } from '../hooks/useZenPagination';

interface ZenExamPreviewProps {
  data: ZenUploadResult;
  onLaunch: () => void;
  onReset: () => void;
  onUpdateQuestion: (order: number, patch: ZenQuestionPatch) => void;
  isLaunching: boolean;
}

export const ZenExamPreview: React.FC<ZenExamPreviewProps> = ({
  data, onLaunch, onReset, onUpdateQuestion, isLaunching,
}) => {
  const { t } = useTranslation('creator');
  const pager = useZenPagination(data.questions.length);
  const visible = data.questions.slice(pager.range.start, pager.range.end);
  const editedCount = data.questions.filter((q) => q.edited).length;

  return (
    <div className="space-y-6 pb-28 animate-fadeIn">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-5 rounded-2xl bg-slate-900/90 border border-slate-800 shadow-xl">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="text-lg md:text-xl font-bold text-white tracking-tight">{data.examTitle}</h2>
            <span className="px-2.5 py-0.5 text-xs font-semibold rounded-full bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">
              {t('preview.totalQuestions', { count: data.totalQuestions })}
            </span>
          </div>
          <div className="flex items-center gap-2 mt-1 text-xs text-emerald-400 font-medium">
            <CopyCheck className="w-4 h-4 shrink-0" />
            <span>{t('preview.casDeduplication', { rate: data.deduplicationRatePercentage })}</span>
          </div>
        </div>
        <button
          onClick={onReset}
          className="flex items-center gap-1.5 px-4 py-2.5 rounded-xl text-xs font-medium bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700"
        >
          <RotateCcw className="w-3.5 h-3.5" />
          {t('actions.reset')}
        </button>
      </div>

      <ZenTaxonomyBadge taxonomy={data.suggestedTaxonomy} />

      <div className="space-y-4">
        {visible.map((q) => (
          <ZenQuestionCard key={q.order} question={q} onUpdate={onUpdateQuestion} />
        ))}
      </div>

      <ZenPaginationBar
        page={pager.page}
        totalPages={pager.totalPages}
        pageSize={pager.pageSize}
        total={data.questions.length}
        rangeStart={pager.range.start}
        rangeEnd={pager.range.end}
        pageWindow={pager.pageWindow}
        editedCount={editedCount}
        isLaunching={isLaunching}
        onGoTo={pager.goTo}
        onPageSize={pager.changePageSize}
        onLaunch={onLaunch}
      />
    </div>
  );
};
