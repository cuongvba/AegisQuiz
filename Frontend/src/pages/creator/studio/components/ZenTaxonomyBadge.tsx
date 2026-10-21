import React from 'react';
import { useTranslation } from 'react-i18next';
import { Compass, BookOpen, Layers, Award } from 'lucide-react';
import { TaxonomyAlignment } from '../types/zen-studio.types';

interface ZenTaxonomyBadgeProps {
  taxonomy?: TaxonomyAlignment;
  onChangeSubject?: () => void;
}

export const ZenTaxonomyBadge: React.FC<ZenTaxonomyBadgeProps> = ({
  taxonomy,
  onChangeSubject,
}) => {
  const { t } = useTranslation('creator');

  if (!taxonomy) return null;

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 p-3.5 rounded-xl bg-slate-900/80 border border-indigo-500/30 backdrop-blur-md mb-4 shadow-sm">
      <div className="flex flex-wrap items-center gap-3">
        <div className="flex items-center gap-2 px-3 py-1 rounded-lg bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 font-medium text-xs">
          <BookOpen className="w-3.5 h-3.5" />
          <span>{taxonomy.domainName} ({taxonomy.categoryName})</span>
        </div>

        <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-800 text-slate-300 text-xs border border-slate-700">
          <Layers className="w-3.5 h-3.5 text-purple-400" />
          <span>{t('taxonomy.standard')} <strong>{taxonomy.benchmarkStandard}</strong></span>
        </div>

        <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-800 text-slate-300 text-xs border border-slate-700">
          <Compass className="w-3.5 h-3.5 text-sky-400" />
          <span>{t('taxonomy.bloomLevel')} <strong className="text-sky-300">{taxonomy.cognitiveLevel}</strong></span>
        </div>

        <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-800 text-slate-300 text-xs border border-slate-700">
          <Award className="w-3.5 h-3.5 text-amber-400" />
          <span>{t('taxonomy.confidence')} <strong className="text-amber-300">{Math.round(taxonomy.confidenceScore * 100)}%</strong></span>
        </div>
      </div>

      {onChangeSubject && (
        <button
          onClick={onChangeSubject}
          className="text-xs text-indigo-400 hover:text-indigo-300 font-medium underline underline-offset-2 transition-colors ml-auto"
        >
          {t('preview.changeSubject')}
        </button>
      )}
    </div>
  );
};
