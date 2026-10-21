import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Check, X, Plus, Trash2 } from 'lucide-react';
import { MathRenderer } from '@/components/common/MathRenderer';
import { ZenQuestionPatch, ZenQuestionPreview } from '../types/zen-studio.types';
import {
  isChoiceAnswerForm,
  isMultiAnswerType,
  resolveCorrectIndexes,
  toggleCorrectIndex,
} from '../utils/zen-answer';

interface ZenQuestionEditorProps {
  question: ZenQuestionPreview;
  onSave: (patch: ZenQuestionPatch) => void;
  onCancel: () => void;
}

const fieldClass =
  'w-full rounded-xl bg-slate-950/70 border border-slate-700 focus:border-indigo-400 focus:outline-none px-3 py-2 text-sm text-slate-100 font-mono resize-y';

export const ZenQuestionEditor: React.FC<ZenQuestionEditorProps> = ({ question, onSave, onCancel }) => {
  const { t } = useTranslation('creator');
  const [content, setContent] = useState(question.content);
  const [options, setOptions] = useState<string[]>(question.options ?? []);
  const [answer, setAnswer] = useState(question.correctAnswer ?? '');
  const [explanation, setExplanation] = useState(question.explanation ?? '');

  const choiceMode = isChoiceAnswerForm(answer, options.length);
  const multi = isMultiAnswerType(question.questionType);
  const correct = resolveCorrectIndexes(answer, options.length);

  const updateOption = (i: number, value: string) =>
    setOptions((prev) => prev.map((o, idx) => (idx === i ? value : o)));

  return (
    <div className="space-y-4">
      <label className="block space-y-1.5">
        <span className="text-xs font-semibold text-indigo-300">{t('editor.content')}</span>
        <textarea rows={4} value={content} onChange={(e) => setContent(e.target.value)} className={fieldClass} />
        <div className="p-2.5 rounded-xl bg-slate-950/40 border border-slate-800 text-sm">
          <MathRenderer content={content} />
        </div>
      </label>

      <div className="space-y-2">
        <span className="text-xs font-semibold text-indigo-300">{t('editor.options')}</span>
        {options.map((opt, i) => (
          <div key={i} className="flex items-start gap-2">
            <button
              type="button"
              disabled={!choiceMode}
              onClick={() => setAnswer(toggleCorrectIndex(answer, options.length, i, multi))}
              title={t('editor.markCorrect')}
              className={`w-8 h-8 shrink-0 rounded-lg font-bold text-xs border transition-colors ${
                correct.includes(i)
                  ? 'bg-emerald-500/30 border-emerald-400 text-emerald-200'
                  : 'bg-slate-900 border-slate-700 text-slate-400 hover:border-emerald-500/60'
              }`}
            >
              {String.fromCharCode(65 + i)}
            </button>
            <input value={opt} onChange={(e) => updateOption(i, e.target.value)} className={fieldClass} />
            <button
              type="button"
              onClick={() => setOptions((prev) => prev.filter((_, idx) => idx !== i))}
              className="p-2 text-slate-500 hover:text-rose-400"
              aria-label={t('editor.removeOption')}
            >
              <Trash2 className="w-4 h-4" />
            </button>
          </div>
        ))}
        {options.length < 8 && (
          <button
            type="button"
            onClick={() => setOptions((prev) => [...prev, ''])}
            className="inline-flex items-center gap-1 text-xs text-indigo-400 hover:text-indigo-300"
          >
            <Plus className="w-3.5 h-3.5" />
            {t('editor.addOption')}
          </button>
        )}
      </div>

      {!choiceMode && (
        <label className="block space-y-1.5">
          <span className="text-xs font-semibold text-indigo-300">{t('editor.answer')}</span>
          <input value={answer} onChange={(e) => setAnswer(e.target.value)} className={fieldClass} />
        </label>
      )}

      <label className="block space-y-1.5">
        <span className="text-xs font-semibold text-indigo-300">{t('editor.explanation')}</span>
        <textarea rows={4} value={explanation} onChange={(e) => setExplanation(e.target.value)} className={fieldClass} />
        {explanation && (
          <div className="p-2.5 rounded-xl bg-slate-950/40 border border-slate-800 text-sm">
            <MathRenderer content={explanation} />
          </div>
        )}
      </label>

      <div className="flex justify-end gap-2">
        <button
          type="button"
          onClick={onCancel}
          className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-medium bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700"
        >
          <X className="w-3.5 h-3.5" />
          {t('editor.cancel')}
        </button>
        <button
          type="button"
          onClick={() => onSave({ content, options, correctAnswer: answer, explanation })}
          className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-bold bg-emerald-500 hover:bg-emerald-400 text-white"
        >
          <Check className="w-3.5 h-3.5" />
          {t('editor.save')}
        </button>
      </div>
    </div>
  );
};
