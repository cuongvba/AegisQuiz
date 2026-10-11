import type { QuestionWidgetProps } from './types';

export default function ShortAnswerWidget({
  answer,
  onUpdate,
  disabled,
  t
}: QuestionWidgetProps) {
  const currentText = answer?.shortText ?? '';

  return (
    <div className="space-y-2">
      <div className="relative">
        <textarea
          disabled={disabled}
          value={currentText}
          onChange={(event) => onUpdate({ shortText: event.target.value })}
          placeholder={t('typeAnswer') || 'Nhập đáp án của bạn (ví dụ: từ vựng, con số)...'}
          className="h-28 w-full rounded-2xl border-2 border-slate-200 bg-white p-4 font-semibold text-base text-slate-800 outline-none ring-teal-200 transition focus:border-teal-500 focus:ring-4 disabled:opacity-60 placeholder:text-slate-400 placeholder:font-normal"
        />
        <div className="absolute bottom-3 right-3 text-xs font-bold text-slate-400">
          {currentText.length} ký tự
        </div>
      </div>
    </div>
  );
}
