import React, { useState } from 'react';
import { Check, X, CheckSquare, Square, Send, Link2, ArrowUpDown } from 'lucide-react';
import { MathRenderer } from '@/components/common/MathRenderer';
import { useLearnerI18n } from '@/lib/i18n';
import { PracticeQuestionItem } from '../types/mobile-practice.types';
import { MobileMatchingWidget } from './MobileMatchingWidget';
import { MobileOrderingWidget } from './MobileOrderingWidget';

interface WidgetCommonProps {
  currentQ: PracticeQuestionItem;
  selectedKey?: string;
  primaryColor?: string;
  onSelectOption: (val: string) => void;
}

// ── 1. MULTI CHOICE (Chọn nhiều đáp án) ────────────────────────────────────────
export const MobileMultiChoiceWidget: React.FC<WidgetCommonProps> = ({
  currentQ,
  selectedKey,
  primaryColor,
  onSelectOption,
}) => {
  const { t } = useLearnerI18n();
  const currentSelected = (selectedKey ? selectedKey.split(',') : []).map((s) => s.trim());

  const handleToggle = (optKey: string) => {
    const next = currentSelected.includes(optKey)
      ? currentSelected.filter((k) => k !== optKey)
      : [...currentSelected, optKey].sort();
    onSelectOption(next.join(','));
  };

  return (
    <div className="space-y-2.5">
      <div className="text-[11px] font-bold text-sky-400 uppercase tracking-wider flex items-center justify-between px-1">
        <span>{t('multiChoicePrompt')}</span>
        <span className="text-[10px] bg-sky-950/80 px-2 py-0.5 rounded-full border border-sky-800">
          {t('answered')}: {currentSelected.length}
        </span>
      </div>
      {currentQ.options.map((opt, optIdx) => {
        const displayLabel = ['A', 'B', 'C', 'D', 'E'][optIdx] || opt.key;
        const textContent = typeof opt === 'string' ? opt : (opt.text || (opt as any).content || String(opt));
        const isChosen = currentSelected.includes(displayLabel) || currentSelected.includes(opt.key);
        return (
          <button
            key={`${displayLabel}-${optIdx}`}
            type="button"
            onClick={() => handleToggle(opt.key || displayLabel)}
            className={`w-full text-left p-3.5 rounded-2xl border text-xs font-bold transition-all cursor-pointer flex items-start gap-3 active:scale-98 ${
              isChosen ? 'bg-slate-900 border-sky-500 ring-1 ring-sky-500/50' : 'bg-slate-950 border-slate-800 hover:bg-slate-900/50'
            }`}
          >
            <div
              className={`w-6 h-6 rounded-lg flex items-center justify-center text-xs shrink-0 transition-colors ${
                isChosen ? 'bg-sky-500 text-slate-950 font-black' : 'bg-slate-900 text-slate-400 border border-slate-800'
              }`}
            >
              {isChosen ? <CheckSquare size={14} /> : <Square size={14} />}
            </div>
            <div className="flex-1 pt-0.5 text-slate-200 leading-normal">
              <span className="font-mono text-sky-300 mr-1.5">{displayLabel}.</span>
              <MathRenderer content={textContent} inline />
            </div>
          </button>
        );
      })}
    </div>
  );
};

// ── 2. MULTI TRUE/FALSE (Đúng/Sai 4 Mệnh Đề Chuẩn GDPT 2025) ───────────────────
export const MobileMultiTfWidget: React.FC<WidgetCommonProps> = ({
  currentQ,
  selectedKey,
  onSelectOption,
}) => {
  const { t } = useLearnerI18n();
  // Parse state dạng "a:T,b:F,c:T,d:T"
  const currentMap: Record<string, boolean> = {};
  if (selectedKey) {
    selectedKey.split(',').forEach((part) => {
      const [k, v] = part.split(':');
      if (k && v) currentMap[k.trim().toLowerCase()] = v.trim().toUpperCase() === 'T';
    });
  }

  const handleSetTf = (itemKey: string, val: boolean) => {
    const nextMap = { ...currentMap, [itemKey.toLowerCase()]: val };
    const serialized = Object.entries(nextMap).map(([k, v]) => `${k}:${v ? 'T' : 'F'}`).join(',');
    onSelectOption(serialized);
  };

  const letters = ['a', 'b', 'c', 'd'];

  return (
    <div className="space-y-3">
      <div className="text-[11px] font-bold text-purple-400 uppercase tracking-wider flex items-center justify-between px-1">
        <span>{t('multiTfPrompt')}</span>
        <span className="text-[10px] bg-purple-950/80 px-2 py-0.5 rounded-full border border-purple-800">
          GDPT 2025
        </span>
      </div>
      <div className="space-y-2">
        {currentQ.options.map((opt, optIdx) => {
          const letter = letters[optIdx] || String.fromCharCode(97 + optIdx);
          const textContent = typeof opt === 'string' ? opt : (opt.text || (opt as any).content || String(opt));
          const cleanText = textContent.replace(/^[a-d0-9]+[\)\.\:\s\-]+/i, '').trim();
          const userVal = currentMap[letter];

          return (
            <div key={letter} className="p-3 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-2.5">
              <div className="flex items-start gap-2.5 text-xs text-slate-200">
                <span className="w-5 h-5 rounded-md bg-purple-900/60 text-purple-300 font-mono font-bold flex items-center justify-center shrink-0 text-[11px]">
                  {letter}
                </span>
                <div className="flex-1 leading-relaxed">
                  <MathRenderer content={cleanText || textContent} inline />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-2 pt-1">
                <button
                  type="button"
                  onClick={() => handleSetTf(letter, true)}
                  className={`py-2 rounded-xl text-xs font-bold flex items-center justify-center gap-1.5 transition-all cursor-pointer ${
                    userVal === true
                      ? 'bg-emerald-600 text-white shadow-md shadow-emerald-950 font-black'
                      : 'bg-slate-950 text-slate-400 border border-slate-800 hover:text-white'
                  }`}
                >
                  <Check size={13} /> {t('statusCorrect')}
                </button>
                <button
                  type="button"
                  onClick={() => handleSetTf(letter, false)}
                  className={`py-2 rounded-xl text-xs font-bold flex items-center justify-center gap-1.5 transition-all cursor-pointer ${
                    userVal === false
                      ? 'bg-rose-600 text-white shadow-md shadow-rose-950 font-black'
                      : 'bg-slate-950 text-slate-400 border border-slate-800 hover:text-white'
                  }`}
                >
                  <X size={13} /> {t('statusIncorrect')}
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

// ── 3. SHORT ANSWER (Điền khuyết / Trả lời ngắn) ───────────────────────────────
export const MobileShortAnswerWidget: React.FC<WidgetCommonProps> = ({
  selectedKey,
  primaryColor,
  onSelectOption,
}) => {
  const { t } = useLearnerI18n();
  const [val, setVal] = useState(selectedKey || '');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (val.trim()) onSelectOption(val.trim());
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-3">
      <div className="text-[11px] font-bold text-amber-400 uppercase tracking-wider px-1">
        {t('shortAnswerPrompt')}
      </div>
      <div className="flex items-center gap-2">
        <input
          type="text"
          value={val}
          onChange={(e) => setVal(e.target.value)}
          placeholder={t('typeAnswer')}
          className="flex-1 p-3.5 rounded-2xl bg-slate-900 border border-slate-800 text-white text-xs font-semibold focus:outline-none focus:border-amber-500"
        />
        <button
          type="submit"
          className="px-4 py-3.5 rounded-2xl font-black text-xs text-slate-950 flex items-center gap-1 cursor-pointer active:scale-95 shadow-md"
          style={{ backgroundColor: primaryColor || '#f59e0b' }}
        >
          <Send size={14} /> Gửi
        </button>
      </div>
      {selectedKey && (
        <div className="text-[11px] text-slate-400 px-1">
          {t('yourAnswer')}: <span className="font-mono text-amber-300 font-bold">{selectedKey}</span>
        </div>
      )}
    </form>
  );
};

// ── 4. MATCHING / ORDERING / ESSAY FALLBACK WIDGET ────────────────────────────
export const MobileSpecializedWidget: React.FC<WidgetCommonProps> = ({
  currentQ,
  selectedKey,
  primaryColor,
  onSelectOption,
}) => {
  const { t } = useLearnerI18n();

  if (currentQ.questionType === 'MATCHING') {
    return (
      <MobileMatchingWidget
        currentQ={currentQ}
        selectedKey={selectedKey}
        primaryColor={primaryColor}
        onSelectOption={onSelectOption}
      />
    );
  }

  if (currentQ.questionType === 'ESSAY') {
    return (
      <div className="space-y-3">
        <div className="text-[11px] font-bold text-indigo-400 uppercase tracking-wider px-1">
          {t('essayPrompt')}
        </div>
        <textarea
          rows={4}
          value={selectedKey || ''}
          onChange={(e) => onSelectOption(e.target.value)}
          placeholder={t('writeEssay')}
          className="w-full p-3.5 rounded-2xl bg-slate-900 border border-slate-800 text-white text-xs leading-relaxed focus:outline-none focus:border-indigo-500 resize-none"
        />
        {currentQ.payload?.rubric && (
          <div className="p-3 rounded-xl bg-indigo-950/40 border border-indigo-800/50 text-[11px] text-indigo-200 whitespace-pre-line">
            <span className="font-black text-indigo-300">{t('rubricTitle')}</span> {currentQ.payload.rubric}
          </div>
        )}
      </div>
    );
  }

  // Ordering: Sử dụng MobileOrderingWidget với khả năng di chuyển vị trí mượt mà
  return (
    <MobileOrderingWidget
      currentQ={currentQ}
      selectedKey={selectedKey}
      primaryColor={primaryColor}
      onSelectOption={onSelectOption}
    />
  );
};
