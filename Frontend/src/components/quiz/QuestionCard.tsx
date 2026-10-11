import { 
  Sparkles, 
  Star, 
  Zap, 
  Lightbulb, 
  Clock3, 
  Volume2, 
  VolumeX, 
  AlertTriangle, 
  BookOpen, 
  ChevronDown, 
  ChevronUp 
} from 'lucide-react';
import { Suspense, useState } from 'react';
import { getDisplayOptions } from '@/lib/quiz-helpers';
import type { LearnerAnswer, LearnerQuestion } from '@/types/quiz';
import { MediaRenderer } from './MediaRenderer';
import { MathRenderer } from '@/components/common/MathRenderer';
import { resolveQuestionWidget } from './widgets/QuestionWidgetRegistry';

type Props = {
  question: LearnerQuestion;
  answer?: LearnerAnswer;
  onAnswerChange: (questionId: string, answer: LearnerAnswer) => void;
  disabled?: boolean;
  questionSecondsLeft?: number | null;
  showHint?: boolean;
  t: (key: string) => string;
};

/**
 * QuestionCard — Micro-Widget Orchestrator (Zero-Monolith & OCP Compliant)
 * Tuân thủ nghiêm ngặt Luật Hệ Thống: Giới hạn <= 250 LOC, phân rã 100% dạng câu hỏi
 * sang Dynamic Question Micro-Widget Registry với Suspense & Lazy Loading.
 */
export function QuestionCard({ 
  question, 
  answer, 
  onAnswerChange, 
  disabled = false, 
  questionSecondsLeft, 
  showHint = false, 
  t 
}: Props) {
  const options = getDisplayOptions(question);
  const [isContextCollapsed, setIsContextCollapsed] = useState(false);
  const [soundEnabled, setSoundEnabled] = useState(() => {
    return localStorage.getItem('aegis_sound_enabled') !== 'false';
  });

  const toggleSound = () => {
    setSoundEnabled((prev) => {
      const next = !prev;
      localStorage.setItem('aegis_sound_enabled', String(next));
      return next;
    });
  };

  const handleWidgetAnswerChange = (patch: Partial<LearnerAnswer>) => {
    onAnswerChange(question.id, { ...answer, ...patch });
  };

  const Widget = resolveQuestionWidget(question.questionType, options.length);
  const isUrgentTimer = question.durationSec > 0 && questionSecondsLeft != null && questionSecondsLeft <= 10;

  return (
    <article className="relative overflow-hidden rounded-3xl border border-white/80 bg-white/95 p-5 shadow-[0_20px_60px_-25px_rgba(15,23,42,0.18)] backdrop-blur-xl md:p-8 transition-all">
      {/* Top Ambient Glow Bar */}
      <div className="absolute top-0 left-0 right-0 h-1.5 bg-gradient-to-r from-sky-400 via-teal-400 to-indigo-500" />

      {/* Gamified Header Badges */}
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          {/* Question Type Badge */}
          <span className="inline-flex items-center gap-1.5 rounded-full border border-teal-300 bg-teal-50 px-3 py-1 text-xs font-bold uppercase tracking-wider text-teal-800 shadow-sm">
            <Zap className="h-3 w-3 text-teal-600" />
            {question.questionType}
          </span>

          {/* Difficulty Stars */}
          <span className="inline-flex items-center gap-1 rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-xs font-bold text-slate-700">
            <Star className="h-3 w-3 fill-amber-400 text-amber-400" />
            Độ khó {question.difficulty}
          </span>

          {/* Points Badge */}
          <span className="inline-flex items-center gap-1 rounded-full border border-amber-200 bg-amber-50 px-3 py-1 text-xs font-black text-amber-700">
            <Sparkles className="h-3 w-3 text-amber-500" />
            +{question.points} ĐIỂM
          </span>

          {/* Timer Badge */}
          {question.durationSec > 0 && questionSecondsLeft != null && (
            <span
              className={`inline-flex items-center gap-1 rounded-full border px-3 py-1 text-xs font-extrabold ${
                isUrgentTimer
                  ? 'border-rose-400 bg-rose-50 text-rose-700 animate-heartbeat-warn'
                  : 'border-slate-200 bg-slate-50 text-slate-600'
              }`}
            >
              <Clock3 className="h-3 w-3" />
              {questionSecondsLeft}s
            </span>
          )}

          {/* Critical Mistake Badge (Câu Điểm Liệt) */}
          {question.isCritical && (
            <span className="inline-flex items-center gap-1.5 rounded-full border border-rose-500 bg-rose-600 px-3.5 py-1 text-xs font-black uppercase tracking-wider text-white shadow-md shadow-rose-300 animate-pulse">
              <AlertTriangle className="h-3.5 w-3.5 fill-white text-rose-600" />
              ⚠️ CÂU ĐIỂM LIỆT (BẮT BUỘC ĐÚNG)
            </span>
          )}

          {/* Sa Hinh Category Badge */}
          {question.subCategory === 'SA_HINH' && (
            <span className="inline-flex items-center gap-1.5 rounded-full border border-indigo-300 bg-indigo-50 px-3 py-1 text-xs font-black text-indigo-700 shadow-xs">
              🚗 SA HÌNH
            </span>
          )}
        </div>

        {/* Audio Sound FX Toggle */}
        <button
          type="button"
          onClick={toggleSound}
          className="inline-flex items-center gap-1.5 rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-xs font-semibold text-slate-600 hover:bg-slate-100 hover:text-slate-900 transition"
          title={soundEnabled ? 'Tắt âm thanh hiệu ứng game' : 'Bật âm thanh hiệu ứng game'}
        >
          {soundEnabled ? (
            <>
              <Volume2 className="h-3.5 w-3.5 text-teal-600" />
              <span className="hidden sm:inline">Âm thanh: Bật</span>
            </>
          ) : (
            <>
              <VolumeX className="h-3.5 w-3.5 text-slate-400" />
              <span className="hidden sm:inline">Âm thanh: Tắt</span>
            </>
          )}
        </button>
      </div>

      {disabled && (
        <div className="mb-4 rounded-2xl border border-amber-200 bg-amber-50/80 px-4 py-2.5 text-sm font-semibold text-amber-800 flex items-center gap-2">
          <span>🔒</span> {t('kidLocked') || 'Câu hỏi đã bị khóa do hết thời gian.'}
        </div>
      )}

      {/* Sa Hinh Traffic Golden Rules Co-pilot */}
      {(question.subCategory === 'SA_HINH' || /sa\s*hình|thứ\s*tự\s*các\s*xe/i.test(question.content)) && (
        <div className="mb-4 flex items-center gap-2.5 rounded-2xl border border-indigo-200 bg-gradient-to-r from-indigo-50/90 via-white to-sky-50/80 p-3.5 text-xs text-indigo-900 shadow-xs">
          <span className="text-xl">🚗</span>
          <div className="flex-1 font-semibold leading-relaxed">
            <span className="font-black text-indigo-700 uppercase tracking-wide">Khẩu Quyết Sa Hình: </span>
            1. Nhất chớm ➔ 2. Nhì ưu (Hỏa-Sự-Công-Thương) ➔ 3. Tam đường ➔ 4. Tứ hướng (Phải trống) ➔ 5. Hướng rẽ (Phải - Thẳng - Trái)
          </div>
        </div>
      )}

      {/* ─── SHARED QUESTION CONTEXT / READING PASSAGE / CASE STUDY ─── */}
      {Boolean(question.contextContent) && (
        <div className="mb-5 overflow-hidden rounded-2xl border-2 border-indigo-200/90 bg-gradient-to-br from-indigo-50/70 via-white to-sky-50/60 p-4 shadow-sm transition-all">
          <div className="flex items-center justify-between gap-2 border-b border-indigo-100 pb-2.5 mb-2.5">
            <div className="flex items-center gap-2 min-w-0">
              <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-indigo-600 text-white shadow-xs">
                <BookOpen className="h-4 w-4" />
              </div>
              <div className="min-w-0">
                <h4 className="text-xs font-black uppercase tracking-wider text-indigo-950 truncate">
                  {question.contextTitle || 'Ngữ Cảnh Dùng Chung • Bài Đọc Hiểu / Tình Huống'}
                </h4>
                <p className="text-[10px] text-slate-500 font-medium">
                  Dữ liệu tham chiếu cốt lõi dùng chung cho các câu hỏi liên kết
                </p>
              </div>
            </div>

            <button
              type="button"
              onClick={() => setIsContextCollapsed(!isContextCollapsed)}
              className="inline-flex items-center gap-1 rounded-lg border border-indigo-200 bg-white px-2 py-1 text-[11px] font-bold text-indigo-700 hover:bg-indigo-50 transition"
              title={isContextCollapsed ? 'Mở rộng bài đọc' : 'Thu gọn bài đọc'}
            >
              <span>{isContextCollapsed ? 'Mở rộng' : 'Thu gọn'}</span>
              {isContextCollapsed ? <ChevronDown className="h-3.5 w-3.5" /> : <ChevronUp className="h-3.5 w-3.5" />}
            </button>
          </div>

          {!isContextCollapsed && (
            <div className="rounded-xl border border-indigo-100/80 bg-white/90 p-3.5 text-xs sm:text-sm leading-relaxed text-slate-800 shadow-inner max-h-72 overflow-y-auto font-sans">
              <MathRenderer content={question.contextContent!} />
            </div>
          )}
        </div>
      )}

      {/* Question Content */}
      <div className="text-lg font-bold leading-relaxed text-slate-900 md:text-xl tracking-tight">
        <MathRenderer content={question.content} />
      </div>

      {/* Multimodal Media (Audio / Image) */}
      {((question.contentType && question.contentType !== 'text') || (question.questionMediaType && question.questionMediaType !== 'text')) && (
        <div className="mt-4 rounded-2xl overflow-hidden border border-slate-200 bg-slate-50 p-2 shadow-inner">
          <MediaRenderer 
            url={question.questionMediaUrl || question.content} 
            mediaType={question.contentType || question.questionMediaType} 
          />
        </div>
      )}

      {/* Interactive Micro-Widget Registry Body (Lazy Loaded & OCP Isolated) */}
      <div className="mt-6">
        <Suspense
          fallback={
            <div className="space-y-3 animate-pulse" aria-busy="true">
              <div className="h-16 w-full rounded-2xl border-2 border-slate-200/60 bg-slate-100/70" />
              <div className="h-16 w-full rounded-2xl border-2 border-slate-200/60 bg-slate-100/70" />
              <div className="h-16 w-full rounded-2xl border-2 border-slate-200/60 bg-slate-100/70" />
            </div>
          }
        >
          <Widget
            question={question}
            options={options}
            answer={answer}
            onUpdate={handleWidgetAnswerChange}
            disabled={disabled}
            showHint={showHint}
            soundEnabled={soundEnabled}
            t={t}
          />
        </Suspense>
      </div>

      {/* ─── OMNI FLASH INSIGHT: Bộ Não Khắc Sâu Kiến Thức (CHỈ HIỆN KHI BẤM SHOW HINT) ───────────────── */}
      {showHint && (
        <div className="mt-7 overflow-hidden rounded-2xl border border-indigo-200/80 bg-gradient-to-br from-indigo-50/90 via-white to-sky-50/80 p-5 shadow-lg shadow-indigo-100/40 animate-game-pop">
          <div className="flex items-center gap-2 text-indigo-800 font-black text-sm uppercase tracking-wide mb-3">
            <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-indigo-600 text-white shadow-md shadow-indigo-300">
              <Lightbulb className="h-4 w-4" />
            </div>
            <span>Omni Flash Insight • Khắc Sâu Kiến Thức Cốt Lõi</span>
          </div>

          <div className="space-y-2 text-sm text-slate-700 leading-relaxed">
            {question.citation && (
              <div className="rounded-xl border border-slate-200/80 bg-white/80 p-3 shadow-xs">
                <span className="font-bold text-indigo-900">📌 Căn cứ & Nguồn khảo thí: </span>
                <MathRenderer content={question.citation} inline />
              </div>
            )}
            {question.aiExplanation && (
              <div className="rounded-xl border border-teal-200/80 bg-teal-50/80 p-3 shadow-xs text-teal-900">
                <span className="font-bold text-teal-950">🧠 Phân tích & Lời giải AI: </span>
                <MathRenderer content={question.aiExplanation} inline />
              </div>
            )}
            <div className="text-xs text-slate-500 font-medium">
              💡 <span className="font-bold">Mẹo vàng làm bài:</span> Đọc kỹ từ khóa then chốt trong đề bài, phân tích bản chất cấu trúc và loại trừ các phương án bẫy để đạt điểm tuyệt đối.
            </div>
          </div>
        </div>
      )}
    </article>
  );
}
