import React from 'react';
import { BookOpen, CheckCircle2, Tag, Trash2, Unlink } from 'lucide-react';
import { useTranslation } from 'react-i18next';

interface BatchActionBarProps {
  selectedCount: number;
  isBulkDeleting: boolean;
  onOpenBatchContext: () => void;
  onOpenBatchTag: () => void;
  onBatchRemoveContext: () => void;
  onBulkDelete: () => void;
  onDeselectAll: () => void;
}

export const BatchActionBar: React.FC<BatchActionBarProps> = ({
  selectedCount,
  isBulkDeleting,
  onOpenBatchContext,
  onOpenBatchTag,
  onBatchRemoveContext,
  onBulkDelete,
  onDeselectAll,
}) => {
  const { t } = useTranslation('admin_questions');
  if (selectedCount === 0) return null;

  return (
    <div className="fixed bottom-6 left-1/2 -translate-x-1/2 z-40 bg-slate-900/95 border border-indigo-500/50 backdrop-blur-xl shadow-2xl rounded-2xl px-6 py-3 flex items-center gap-3 text-xs animate-in fade-in slide-in-from-bottom-5">
      <span className="font-bold text-white flex items-center gap-1.5 whitespace-nowrap">
        <CheckCircle2 className="text-emerald-400" size={16} />
        <span>{t('batch.selectedCount', { count: selectedCount })}</span>
      </span>
      <div className="h-4 w-px bg-slate-750" />
      <button
        type="button"
        onClick={onOpenBatchContext}
        className="px-3.5 py-2 bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white font-bold rounded-xl flex items-center gap-1.5 cursor-pointer shadow-lg shadow-indigo-600/30 active:scale-95 transition-all whitespace-nowrap"
      >
        <BookOpen size={13} /> {t('batch.assignContext')}
      </button>
      <button
        type="button"
        onClick={onOpenBatchTag}
        className="px-3.5 py-2 bg-gradient-to-r from-purple-600 to-pink-600 hover:from-purple-500 hover:to-pink-500 text-white font-bold rounded-xl flex items-center gap-1.5 cursor-pointer shadow-lg shadow-purple-600/30 active:scale-95 transition-all whitespace-nowrap"
      >
        <Tag size={13} /> {t('batch.assignTags')}
      </button>
      <button
        type="button"
        onClick={onBatchRemoveContext}
        className="px-3 py-2 bg-slate-800 hover:bg-slate-750 text-slate-300 hover:text-red-400 font-semibold rounded-xl flex items-center gap-1 cursor-pointer transition-colors whitespace-nowrap"
        title={t('batch.removeContext')}
      >
        <Unlink size={12} /> {t('batch.removeContext')}
      </button>
      <button
        type="button"
        disabled={isBulkDeleting}
        onClick={onBulkDelete}
        className="px-3.5 py-2 bg-gradient-to-r from-red-600 to-rose-600 hover:from-red-500 hover:to-rose-500 text-white font-bold rounded-xl flex items-center gap-1.5 cursor-pointer shadow-lg shadow-red-600/30 active:scale-95 transition-all whitespace-nowrap disabled:opacity-50"
        title={t('batch.deleteSelected')}
      >
        <Trash2 size={13} /> {isBulkDeleting ? t('batch.deleting') : t('batch.deleteSelected')}
      </button>
      <button
        type="button"
        onClick={onDeselectAll}
        className="text-slate-400 hover:text-white px-2 py-1 font-semibold cursor-pointer whitespace-nowrap"
      >
        {t('batch.deselectAll')}
      </button>
    </div>
  );
};
