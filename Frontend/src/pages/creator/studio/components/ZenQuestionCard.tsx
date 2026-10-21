import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CheckCircle2, Sparkles, HelpCircle, Pencil, KeyRound, PenLine } from 'lucide-react';
import { MathRenderer } from '@/components/common/MathRenderer';
import { ZenQuestionPatch, ZenQuestionPreview } from '../types/zen-studio.types';
import { isChoiceAnswerForm, resolveCorrectIndexes } from '../utils/zen-answer';
import { ZenQuestionEditor } from './ZenQuestionEditor';

interface ZenQuestionCardProps {
  question: ZenQuestionPreview;
  onUpdate: (order: number, patch: ZenQuestionPatch) => void;
}

export const ZenQuestionCard: React.FC<ZenQuestionCardProps> = ({ question: q, onUpdate }) => {
  const { t } = useTranslation('creator');
  const [editing, setEditing] = useState(false);

  const options = q.options ?? [];
  const correct = resolveCorrectIndexes(q.correctAnswer, options.length);
  const showRawAnswer = !!q.correctAnswer && !isChoiceAnswerForm(q.correctAnswer, options.length);

  return (
    <article
      className={`p-5 rounded-2xl border transition-all ${
        editing
          ? 'bg-slate-900 border-amber-400/50 shadow-lg shadow-amber-500/10'
          : q.isDuplicate
            ? 'bg-slate-900/50 border-slate-800/80'
            : 'bg-slate-900/80 border-indigo-500/30'
      }`}
    >
      <header className="flex flex-wrap items-center justify-between gap-2 mb-3">
        <div className="flex items-center gap-2">
          <span className="font-bold text-sm text-slate-200">{t('preview.questionNum', { num: q.order })}</span>
          {q.edited && (
            <span className="px-2 py-0.5 text-[10px] font-semibold rounded-md bg-amber-500/15 text-amber-300 border border-amber-500/30">
              {t('editor.editedBadge')}
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          {q.isDuplicate ? (
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 text-xs font-medium rounded-md bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
              <CheckCircle2 className="w-3 h-3" />
              {t('preview.duplicateBadge')}
            </span>
          ) : (
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 text-xs font-medium rounded-md bg-indigo-500/10 text-indigo-400 border border-indigo-500/20">
              <Sparkles className="w-3 h-3" />
              {t('preview.uniqueBadge')}
            </span>
          )}
          {!editing && (
            <button
              type="button"
              onClick={() => setEditing(true)}
              className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium rounded-lg bg-slate-800 hover:bg-amber-500/20 hover:text-amber-200 text-slate-300 border border-slate-700 transition-colors"
            >
              <Pencil className="w-3 h-3" />
              {t('editor.edit')}
            </button>
          )}
        </div>
      </header>

      {editing ? (
        <ZenQuestionEditor
          question={q}
          onCancel={() => setEditing(false)}
          onSave={(patch) => {
            onUpdate(q.order, patch);
            setEditing(false);
          }}
        />
      ) : (
        <>
          <div className="text-sm md:text-base text-slate-100 leading-relaxed mb-4">
            <MathRenderer content={q.content} />
          </div>

          {options.length > 0 && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-2.5 mb-3">
              {options.map((opt, i) => (
                <div
                  key={i}
                  className={`p-2.5 rounded-xl text-xs md:text-sm border flex items-start gap-2 ${
                    correct.includes(i)
                      ? 'bg-emerald-500/15 border-emerald-500/40 text-emerald-200 font-medium'
                      : 'bg-slate-950/40 border-slate-800/80 text-slate-300'
                  }`}
                >
                  <span className="font-bold text-slate-400 shrink-0">{String.fromCharCode(65 + i)}.</span>
                  <div className="flex-1">
                    <MathRenderer content={opt} inline />
                  </div>
                  {correct.includes(i) && <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />}
                </div>
              ))}
            </div>
          )}

          {(correct.length > 0 || showRawAnswer) && (
            <div className="flex items-start gap-2 p-3 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-sm text-emerald-200 mb-2">
              <KeyRound className="w-4 h-4 mt-0.5 shrink-0" />
              <div>
                <strong className="mr-1.5">{t('preview.correctAnswer')}</strong>
                {showRawAnswer ? q.correctAnswer : correct.map((i) => String.fromCharCode(65 + i)).join(', ')}
              </div>
            </div>
          )}

          {q.explanation ? (
            <div className="flex items-start gap-2 p-3 rounded-xl bg-slate-950/50 border border-slate-800 text-xs md:text-sm text-slate-300">
              <HelpCircle className="w-4 h-4 text-indigo-400 shrink-0 mt-0.5" />
              <div className="flex-1">
                <strong className="text-slate-200 mr-1">{t('preview.explanation')}</strong>
                <MathRenderer content={q.explanation} />
              </div>
            </div>
          ) : (
            <button
              type="button"
              onClick={() => setEditing(true)}
              className="inline-flex items-center gap-1.5 text-xs text-slate-500 hover:text-amber-300"
            >
              <PenLine className="w-3.5 h-3.5" />
              {t('editor.addExplanation')}
            </button>
          )}
        </>
      )}
    </article>
  );
};
