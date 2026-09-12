import React, { useState, useRef, useEffect } from 'react';
import {
  BookOpen,
  Timer,
  FileText,
  Bot,
  SlidersHorizontal,
  Play,
  Sparkles,
  Check,
  RotateCcw,
  Clock,
  AlertCircle
} from 'lucide-react';
import type { PracticeMode } from '@/types/quiz';

interface Props {
  mode: PracticeMode;
  onModeChange: (mode: PracticeMode) => void;
  totalSelected: number;
  topicCount: number;
  timeLimitMin: number;
  onTimeLimitChange: (mins: number) => void;
  shuffleOptions: boolean;
  onShuffleOptionsChange: (val: boolean) => void;
  shuffleQuestions: boolean;
  onShuffleQuestionsChange: (val: boolean) => void;
  autoNextOnTimeout: boolean;
  onAutoNextChange: (val: boolean) => void;
  onStart: () => void;
  onResetTopics?: () => void;
  t: (key: string) => string;
}

const MODES: Array<{
  key: PracticeMode;
  label: string;
  shortLabel: string;
  icon: React.FC<{ className?: string }>;
  color: string;
  description: string;
}> = [
  {
    key: 'study',
    label: 'Chế độ Ôn tập',
    shortLabel: 'Ôn tập',
    icon: BookOpen,
    color: 'text-cyan-600',
    description: 'Hỗ trợ gợi ý, lời giải chi tiết & không áp lực thời gian',
  },
  {
    key: 'exam',
    label: 'Chế độ Thi thử',
    shortLabel: 'Thi thử',
    icon: Timer,
    color: 'text-rose-600',
    description: 'Bấm giờ chuẩn hóa phòng thi, tự động tính điểm & xếp hạng',
  },
  {
    key: 'paper',
    label: 'Làm bài giấy',
    shortLabel: 'Bài giấy',
    icon: FileText,
    color: 'text-slate-600',
    description: 'Hiển thị toàn bộ câu hỏi trên một trang, tối ưu in ấn PDF',
  },
  {
    key: 'kids',
    label: 'Chế độ Trẻ em',
    shortLabel: 'Trẻ em',
    icon: Bot,
    color: 'text-amber-500',
    description: 'Âm thanh sống động, chuỗi combo sao thưởng & linh vật AI',
  },
];

export const PracticeLaunchPad: React.FC<Props> = ({
  mode,
  onModeChange,
  totalSelected,
  topicCount,
  timeLimitMin,
  onTimeLimitChange,
  shuffleOptions,
  onShuffleOptionsChange,
  shuffleQuestions,
  onShuffleQuestionsChange,
  autoNextOnTimeout,
  onAutoNextChange,
  onStart,
  onResetTopics,
  t,
}) => {
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const settingsRef = useRef<HTMLDivElement>(null);

  // Close settings popover on click outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (settingsRef.current && !settingsRef.current.contains(event.target as Node)) {
        setIsSettingsOpen(false);
      }
    }
    if (isSettingsOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isSettingsOpen]);

  const currentModeInfo = MODES.find((m) => m.key === mode) || MODES[0];
  const estMinutes = Math.max(1, Math.round(totalSelected * 0.8));

  // Quick settings summary badge
  const settingsBadge = [
    shuffleOptions ? 'Xáo đáp án' : null,
    shuffleQuestions ? 'Xáo câu' : null,
    mode === 'exam' && timeLimitMin > 0 ? `${timeLimitMin}p` : null,
  ]
    .filter(Boolean)
    .join(' • ');

  const getStartButtonLabel = () => {
    if (totalSelected === 0) return 'Chọn câu hỏi để bắt đầu';
    switch (mode) {
      case 'kids':
        return t('startKid') || 'Chơi & Học Ngay 🎈';
      case 'exam':
        return timeLimitMin > 0
          ? `Bắt Đầu Thi (${timeLimitMin}p) →`
          : (t('startExam') || 'Bắt Đầu Thi Thử →');
      case 'paper':
        return 'Làm Đề Dạng Giấy →';
      case 'study':
      default:
        return t('startPractice') || 'Bắt Đầu Ôn Luyện →';
    }
  };

  return (
    <div className="sticky bottom-4 z-40 mx-auto w-full max-w-5xl px-2 sm:px-4">
      <div className="rounded-3xl border border-white/80 bg-white/95 p-3.5 sm:p-4 shadow-[0_20px_60px_-15px_rgba(14,116,144,0.35)] backdrop-blur-xl transition-all duration-300 ring-1 ring-slate-900/5">
        <div className="flex flex-col gap-3.5 lg:flex-row lg:items-center lg:justify-between">
          
          {/* ─── VÙNG 1: THỐNG KÊ LỰA CHỌN CÔNG THÁI HỌC ─── */}
          <div className="flex items-center justify-between gap-3 lg:justify-start">
            <div className="flex items-center gap-2.5">
              <div
                className={`flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl shadow-inner transition ${
                  totalSelected > 0
                    ? 'bg-gradient-to-tr from-cyan-600 to-teal-500 text-white'
                    : 'bg-slate-100 text-slate-400'
                }`}
              >
                {totalSelected > 0 ? (
                  <Sparkles className="h-5 w-5 animate-pulse" />
                ) : (
                  <AlertCircle className="h-5 w-5" />
                )}
              </div>

              <div>
                <div className="flex items-center gap-2">
                  <span className="text-sm font-black text-slate-900 tracking-tight">
                    {totalSelected > 0 ? (
                      <>
                        <span className="text-cyan-600 font-black text-base">{totalSelected}</span> câu hỏi đã chọn
                      </>
                    ) : (
                      <span className="text-slate-500 font-semibold">Chưa chọn câu hỏi nào</span>
                    )}
                  </span>

                  {totalSelected > 0 && onResetTopics && (
                    <button
                      type="button"
                      onClick={onResetTopics}
                      className="rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600 transition"
                      title="Bỏ chọn tất cả"
                    >
                      <RotateCcw className="h-3 w-3" />
                    </button>
                  )}
                </div>

                <p className="text-[11px] text-slate-500 flex items-center gap-1.5 mt-0.5">
                  {totalSelected > 0 ? (
                    <>
                      <span>{topicCount} chuyên đề</span>
                      <span>•</span>
                      <span className="inline-flex items-center gap-0.5 text-cyan-700 font-medium">
                        <Clock className="h-3 w-3 inline" /> ~{estMinutes} phút
                      </span>
                    </>
                  ) : (
                    'Tích chọn chuyên đề ở danh mục bên trên'
                  )}
                </p>
              </div>
            </div>

            {/* Mobile Mode Tag */}
            <div className="lg:hidden">
              <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-bold text-slate-700">
                {currentModeInfo.shortLabel}
              </span>
            </div>
          </div>

          {/* ─── VÙNG 2: COMBO CHỌN CHẾ ĐỘ & TÙY CHỌN NHANH (SEGMENTED CONTROL) ─── */}
          <div className="flex flex-wrap items-center justify-between gap-2 sm:gap-3 lg:justify-center">
            {/* Segmented Mode Switcher */}
            <div className="inline-flex items-center rounded-2xl border border-slate-200/80 bg-slate-100/90 p-1 shadow-inner">
              {MODES.map((m) => {
                const isActive = mode === m.key;
                const Icon = m.icon;
                return (
                  <button
                    key={m.key}
                    type="button"
                    onClick={() => onModeChange(m.key)}
                    className={`relative flex items-center gap-1.5 rounded-xl px-2.5 py-1.5 text-xs font-black transition-all duration-200 ${
                      isActive
                        ? 'bg-white text-slate-900 shadow-sm ring-1 ring-slate-900/5'
                        : 'text-slate-600 hover:text-slate-900 hover:bg-white/50'
                    }`}
                    title={m.description}
                  >
                    <Icon className={`h-3.5 w-3.5 ${isActive ? m.color : 'text-slate-400'}`} />
                    <span>{m.shortLabel}</span>
                    {isActive && (
                      <span className="absolute -top-1 -right-1 flex h-2 w-2">
                        <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-cyan-400 opacity-75"></span>
                        <span className="relative inline-flex h-2 w-2 rounded-full bg-cyan-500"></span>
                      </span>
                    )}
                  </button>
                );
              })}
            </div>

            {/* Input Thời Gian khi ở Chế Độ Thi */}
            {mode === 'exam' && (
              <div className="flex items-center gap-1.5 rounded-xl border border-rose-200 bg-rose-50/80 px-2 py-1 text-xs font-bold text-rose-700 animate-in fade-in zoom-in duration-150">
                <Timer className="h-3.5 w-3.5 text-rose-500" />
                <span>Giới hạn:</span>
                <input
                  type="number"
                  min={0}
                  max={300}
                  value={timeLimitMin}
                  onChange={(e) => onTimeLimitChange(Math.max(0, Number(e.target.value) || 0))}
                  className="h-6 w-12 rounded border border-rose-300 bg-white px-1 text-center font-bold text-rose-900 outline-none focus:ring-1 focus:ring-rose-500"
                />
                <span>phút</span>
              </div>
            )}

            {/* Nút Quick Tuning Popover */}
            <div className="relative" ref={settingsRef}>
              <button
                type="button"
                onClick={() => setIsSettingsOpen(!isSettingsOpen)}
                className={`inline-flex items-center gap-1.5 rounded-xl border px-2.5 py-1.5 text-xs font-bold transition ${
                  isSettingsOpen
                    ? 'border-cyan-500 bg-cyan-50 text-cyan-800 ring-2 ring-cyan-500/20'
                    : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
                }`}
                title="Tùy chỉnh xáo đề & thời gian"
              >
                <SlidersHorizontal className="h-3.5 w-3.5 text-slate-500" />
                <span className="hidden sm:inline">Tùy chọn</span>
                {settingsBadge && (
                  <span className="hidden md:inline rounded-md bg-slate-100 px-1.5 py-0.2 text-[10px] text-slate-600 font-normal">
                    {settingsBadge}
                  </span>
                )}
              </button>

              {/* Popover Settings */}
              {isSettingsOpen && (
                <div className="absolute right-0 bottom-full mb-2 w-72 rounded-2xl border border-slate-200 bg-white p-3.5 shadow-2xl ring-1 ring-slate-900/5 animate-in fade-in slide-in-from-bottom-2 duration-150 z-50">
                  <div className="flex items-center justify-between border-b border-slate-100 pb-2 mb-2.5">
                    <span className="text-xs font-black text-slate-900 flex items-center gap-1.5">
                      <SlidersHorizontal className="h-3.5 w-3.5 text-cyan-600" /> Cấu hình phiên học
                    </span>
                    <span className="rounded bg-cyan-100 text-cyan-800 text-[10px] font-bold px-1.5 py-0.5">
                      {currentModeInfo.shortLabel}
                    </span>
                  </div>

                  <p className="text-[11px] text-slate-500 mb-3 italic">
                    {currentModeInfo.description}
                  </p>

                  <div className="space-y-2.5">
                    <label className="flex items-center justify-between text-xs font-medium text-slate-700 cursor-pointer select-none">
                      <span>{t('shuffleOptions') || 'Xáo vị trí phương án'}</span>
                      <input
                        type="checkbox"
                        checked={mode === 'kids' ? true : shuffleOptions}
                        disabled={mode === 'kids'}
                        onChange={(e) => onShuffleOptionsChange(e.target.checked)}
                        className="h-4 w-4 rounded border-slate-300 text-cyan-600 focus:ring-cyan-500"
                      />
                    </label>

                    <label className="flex items-center justify-between text-xs font-medium text-slate-700 cursor-pointer select-none">
                      <span>{t('shuffleQuestions') || 'Xáo trộn thứ tự câu hỏi'}</span>
                      <input
                        type="checkbox"
                        checked={mode === 'kids' ? true : shuffleQuestions}
                        disabled={mode === 'kids'}
                        onChange={(e) => onShuffleQuestionsChange(e.target.checked)}
                        className="h-4 w-4 rounded border-slate-300 text-cyan-600 focus:ring-cyan-500"
                      />
                    </label>

                    <label className="flex items-center justify-between text-xs font-medium text-slate-700 cursor-pointer select-none">
                      <span>{t('autoNextTimeout') || 'Tự động qua câu khi hết giờ'}</span>
                      <input
                        type="checkbox"
                        checked={mode === 'kids' ? true : autoNextOnTimeout}
                        disabled={mode === 'kids'}
                        onChange={(e) => onAutoNextChange(e.target.checked)}
                        className="h-4 w-4 rounded border-slate-300 text-cyan-600 focus:ring-cyan-500"
                      />
                    </label>

                    {mode === 'exam' && (
                      <div className="pt-2 border-t border-slate-100 flex items-center justify-between text-xs font-medium text-slate-700">
                        <span>{t('timeLimitMinutes') || 'Thời gian làm bài (phút)'}</span>
                        <input
                          type="number"
                          min={0}
                          value={timeLimitMin}
                          onChange={(e) => onTimeLimitChange(Math.max(0, Number(e.target.value) || 0))}
                          className="h-7 w-16 rounded-lg border border-slate-300 px-2 text-right font-bold"
                        />
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* ─── VÙNG 3: PRIMARY ACTION CTA BUTTON ĐẲNG CẤP ─── */}
          <div className="flex items-center">
            <button
              type="button"
              onClick={onStart}
              disabled={totalSelected === 0}
              className={`group relative flex w-full lg:w-auto items-center justify-center gap-2 rounded-2xl px-6 py-3 text-sm font-black text-white shadow-lg transition-all duration-200 active:scale-[0.98] ${
                totalSelected > 0
                  ? 'bg-gradient-to-r from-teal-500 via-cyan-600 to-sky-600 shadow-cyan-500/30 hover:shadow-cyan-500/40 hover:brightness-105'
                  : 'bg-slate-300 cursor-not-allowed shadow-none text-slate-500'
              }`}
            >
              <Play className="h-4 w-4 fill-current transition-transform group-hover:scale-110" />
              <span>{getStartButtonLabel()}</span>
            </button>
          </div>

        </div>
      </div>
    </div>
  );
};
