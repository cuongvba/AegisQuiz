import type { QuestionWidgetProps } from './types';

export default function EssayWidget({
  answer,
  onUpdate,
  disabled,
  t
}: QuestionWidgetProps) {
  return (
    <textarea
      disabled={disabled}
      value={answer?.essay ?? ''}
      onChange={(event) => onUpdate({ essay: event.target.value })}
      placeholder={t('writeEssay') || 'Viết bài luận của bạn tại đây...'}
      className="h-48 w-full rounded-2xl border-2 border-slate-200 bg-white p-4 text-base leading-relaxed text-slate-800 outline-none ring-teal-200 transition focus:border-teal-500 focus:ring-4 disabled:opacity-60"
    />
  );
}
