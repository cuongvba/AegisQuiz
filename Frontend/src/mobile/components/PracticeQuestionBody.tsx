import React, { useState } from 'react';
import { AlertTriangle, Sparkles, BookOpen, ChevronDown, ChevronUp } from 'lucide-react';
import { PracticeQuestionItem } from '../types/mobile-practice.types';
import { MathRenderer } from '@/components/common/MathRenderer';
import { useLearnerI18n } from '@/lib/i18n';
import {
  MobileMultiChoiceWidget,
  MobileMultiTfWidget,
  MobileShortAnswerWidget,
  MobileSpecializedWidget,
} from './MobileQuestionWidgets';

interface PracticeQuestionBodyProps {
  currentQ: PracticeQuestionItem;
  currentIdx: number;
  selectedKey?: string;
  showExplanation: boolean;
  primaryColor?: string;
  onSelectOption: (optKey: string) => void;
  onOpenStory: () => void;
}

export const PracticeQuestionBody: React.FC<PracticeQuestionBodyProps> = ({
  currentQ,
  currentIdx,
  selectedKey,
  showExplanation,
  primaryColor,
  onSelectOption,
  onOpenStory,
}) => {
  const { t } = useLearnerI18n();
  const [isContextCollapsed, setIsContextCollapsed] = useState<boolean>(false);

  const qType = currentQ.questionType?.toUpperCase() || 'SINGLE';

  return (
    <div className="flex-1 overflow-y-auto p-4 space-y-4">
      {/* ── Cảnh báo câu điểm liệt ── */}
      {currentQ.isFatal && (
        <div className="p-2.5 rounded-xl bg-red-950/60 border border-red-500/50 flex items-center gap-2 text-xs text-red-200">
          <AlertTriangle size={15} className="text-red-400 shrink-0 animate-bounce" />
          <span className="font-bold">{t('fatalWarning')}</span>
        </div>
      )}

      {/* ── 1. QUESTION CONTEXT / BÀI ĐỌC HIỂU / CASE STUDY DÙNG CHUNG ── */}
      {Boolean(currentQ.contextContent) && (
        <div className="overflow-hidden rounded-2xl border border-indigo-500/40 bg-gradient-to-br from-indigo-950/60 via-slate-900/80 to-purple-950/50 p-3.5 space-y-2.5 shadow-sm">
          <div className="flex items-center justify-between gap-2 border-b border-indigo-900/80 pb-2">
            <div className="flex items-center gap-2 min-w-0">
              <div className="w-6 h-6 rounded-lg bg-indigo-600 text-white flex items-center justify-center shrink-0">
                <BookOpen size={13} />
              </div>
              <div className="min-w-0">
                <h4 className="text-xs font-black uppercase text-indigo-300 truncate tracking-wide">
                  {currentQ.contextTitle || t('sharedContext')}
                </h4>
                <p className="text-[10px] text-slate-400 font-medium">{t('sharedContextSub')}</p>
              </div>
            </div>
            <button
              type="button"
              onClick={() => setIsContextCollapsed(!isContextCollapsed)}
              className="px-2 py-1 rounded-lg bg-slate-900 border border-slate-800 text-indigo-300 text-[11px] font-bold flex items-center gap-1 cursor-pointer"
            >
              <span>{isContextCollapsed ? t('expand') : t('collapse')}</span>
              {isContextCollapsed ? <ChevronDown size={13} /> : <ChevronUp size={13} />}
            </button>
          </div>
          {!isContextCollapsed && (
            <div className="text-xs text-slate-200 leading-relaxed font-sans max-h-48 overflow-y-auto pr-1">
              <MathRenderer content={currentQ.contextContent!} />
            </div>
          )}
        </div>
      )}

      {/* ── 2. HÌNH ẢNH MINH HỌA RETINA (NẾU CÓ) ── */}
      {Boolean(currentQ.imageUrl) && (
        <div className="rounded-2xl overflow-hidden border border-slate-800 bg-slate-900/90 p-2 flex items-center justify-center">
          <img
            src={currentQ.imageUrl}
            alt="Question illustration"
            className="max-h-48 w-auto object-contain rounded-xl"
          />
        </div>
      )}

      {/* ── 3. NỘI DUNG CÂU HỎI (KaTeX LaTeX TỰ ĐỘNG) ── */}
      <div className="p-4 rounded-2xl bg-slate-900/70 border border-slate-800">
        <div className="text-sm font-bold text-white leading-relaxed">
          <MathRenderer content={currentQ.content} />
        </div>
      </div>

      {/* ── 4. MICRO-WIDGET THEO TỪNG DẠNG CÂU HỎI ── */}
      {qType === 'MULTI' ? (
        <MobileMultiChoiceWidget
          currentQ={currentQ}
          selectedKey={selectedKey}
          primaryColor={primaryColor}
          onSelectOption={onSelectOption}
        />
      ) : qType === 'MULTI_TRUE_FALSE' ? (
        <MobileMultiTfWidget
          currentQ={currentQ}
          selectedKey={selectedKey}
          primaryColor={primaryColor}
          onSelectOption={onSelectOption}
        />
      ) : qType === 'SHORT_ANSWER' || qType === 'FILL_BLANK' ? (
        <MobileShortAnswerWidget
          currentQ={currentQ}
          selectedKey={selectedKey}
          primaryColor={primaryColor}
          onSelectOption={onSelectOption}
        />
      ) : ['MATCHING', 'ORDERING', 'ESSAY'].includes(qType) ? (
        <MobileSpecializedWidget
          currentQ={currentQ}
          selectedKey={selectedKey}
          primaryColor={primaryColor}
          onSelectOption={onSelectOption}
        />
      ) : (
        /* Mặc định: Dạng SINGLE CHOICE (A, B, C, D) */
        <div className="space-y-2.5">
          {currentQ.options.map((opt, optIdx) => {
            const displayLabel = ['A', 'B', 'C', 'D', 'E'][optIdx] || opt.key;
            const textContent = typeof opt === 'string' ? opt : (opt.text || (opt as any).content || String(opt));
            const isChosen = !!selectedKey && (selectedKey === opt.key || selectedKey === displayLabel);
            return (
              <button
                key={`${displayLabel}-${optIdx}`}
                type="button"
                onClick={() => onSelectOption(opt.key || displayLabel)}
                className={`w-full text-left p-3.5 rounded-2xl border text-xs font-bold transition-all cursor-pointer flex items-start gap-3 active:scale-98 ${
                  isChosen
                    ? 'bg-slate-900 border-slate-700 ring-2'
                    : 'bg-slate-950 border-slate-800 hover:bg-slate-900/50'
                }`}
                style={{ borderColor: isChosen ? primaryColor : undefined }}
              >
                <div
                  className={`w-7 h-7 rounded-xl flex items-center justify-center text-xs font-black shrink-0 transition-colors ${
                    isChosen ? 'text-slate-950 shadow-md font-black' : 'bg-slate-900 text-slate-300 border border-slate-800'
                  }`}
                  style={{ backgroundColor: isChosen ? primaryColor : undefined }}
                >
                  {displayLabel}
                </div>
                <div className="flex-1 pt-0.5 text-slate-200 leading-normal">
                  <MathRenderer content={textContent} inline />
                </div>
              </button>
            );
          })}
        </div>
      )}

      {/* ── 5. GIẢI THÍCH CHI TIẾT & BÀI GIẢNG SLICE 🎬 ── */}
      {showExplanation && (
        <div className="p-3.5 rounded-2xl bg-purple-950/40 border border-purple-500/40 space-y-2 animate-in fade-in duration-200">
          <div className="flex items-center justify-between text-xs font-bold text-purple-300">
            <span className="flex items-center gap-1.5">
              <Sparkles size={14} /> {t('detailedExplanation')}
            </span>
            <button
              type="button"
              onClick={onOpenStory}
              className="px-2.5 py-1 rounded-full bg-gradient-to-r from-amber-400 to-pink-500 text-slate-950 text-[10px] font-black flex items-center gap-1 active:scale-95 transition-transform cursor-pointer shadow-sm"
            >
              <Sparkles size={11} /> {t('sliceStory')}
            </button>
          </div>
          <div className="text-xs text-slate-300 leading-relaxed">
            <MathRenderer content={currentQ.explanation} />
          </div>
        </div>
      )}
    </div>
  );
};
