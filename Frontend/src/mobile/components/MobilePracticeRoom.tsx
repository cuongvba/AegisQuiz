import React, { useState, useEffect } from 'react';
import { ArrowLeft, Clock, ChevronLeft, ChevronRight, Sparkles, Globe, SlidersHorizontal } from 'lucide-react';
import { useMobileApp } from '../context/MobileAppContext';
import { useLearnerI18n } from '@/lib/i18n';
import { KidsPracticeView } from './KidsPracticeView';
import { PracticeQuestionBody } from './PracticeQuestionBody';
import { InteractiveSliceViewer } from './InteractiveSliceViewer';
import { transpileExplanationToSlices } from '../utils/explanation-slice-transpiler';
import { useMobilePracticeData } from '../hooks/useMobilePracticeData';
import { MobileSessionConfigModal, getStoredMobileConfig, MobileSessionConfig } from './MobileSessionConfigModal';
import { MobileResultScreen } from './MobileResultScreen';
import { MobilePracticeRoomProps } from '../types/mobile-practice.types';

export const MobilePracticeRoom: React.FC<MobilePracticeRoomProps> = ({
  topicId,
  topicName,
  onExit,
}) => {
  const { variant, triggerHaptic } = useMobileApp();
  const { lang, setLang, t } = useLearnerI18n();

  // 1. Cấu hình phiên học Mobile (Mặc định enableQuestionTimer = false)
  const [sessionConfig, setSessionConfig] = useState<MobileSessionConfig>(getStoredMobileConfig);
  const [isConfigOpen, setIsConfigOpen] = useState<boolean>(false);
  const [isSubmitted, setIsSubmitted] = useState<boolean>(false);

  // Gamified Kids UI Mode
  if (variant.uiMode === 'gamified_kids') {
    return <KidsPracticeView topicName={topicName} onExit={onExit} />;
  }

  const { questions, setQuestions, dataSource } = useMobilePracticeData({
    topicId,
    domainCode: variant.domainCode,
    topicName,
    shortName: variant.shortName,
    shuffleQuestions: sessionConfig.shuffleQuestions,
  });

  const [currentIdx, setCurrentIdx] = useState<number>(0);
  const [selectedAnswers, setSelectedAnswers] = useState<Record<number, string>>({});
  const [showExplanation, setShowExplanation] = useState<boolean>(false);
  const [showStoryModal, setShowStoryModal] = useState<boolean>(false);
  const [timeLeft, setTimeLeft] = useState<number>(() => (sessionConfig.timeLimitMin || 20) * 60);

  // Đếm ngược thời gian tổng phiên
  useEffect(() => {
    if (isSubmitted || timeLeft <= 0) return;
    const timer = setInterval(() => setTimeLeft((t) => (t > 0 ? t - 1 : 0)), 1000);
    return () => clearInterval(timer);
  }, [isSubmitted, timeLeft]);

  const currentQ = questions[currentIdx] || questions[0];
  const totalQuestions = questions.length;

  const handleSelectOption = (optKey: string) => {
    triggerHaptic(15);
    setSelectedAnswers((prev) => ({ ...prev, [currentIdx]: optKey }));
  };

  const formatTime = (s: number) =>
    `${Math.floor(s / 60).toString().padStart(2, '0')}:${(s % 60).toString().padStart(2, '0')}`;

  // 2. Chuyển sang màn hình Tổng kết kết quả sau khi nộp bài
  if (isSubmitted) {
    return (
      <MobileResultScreen
        questions={questions}
        selectedAnswers={selectedAnswers}
        topicName={topicName}
        onRetestMistakes={(mistakeQuestions) => {
          triggerHaptic(20);
          setQuestions(mistakeQuestions);
          setCurrentIdx(0);
          setSelectedAnswers({});
          setTimeLeft((sessionConfig.timeLimitMin || 20) * 60);
          setIsSubmitted(false);
        }}
        onRestartAll={() => {
          triggerHaptic(15);
          setCurrentIdx(0);
          setSelectedAnswers({});
          setTimeLeft((sessionConfig.timeLimitMin || 20) * 60);
          setIsSubmitted(false);
        }}
        onExit={onExit}
      />
    );
  }

  return (
    <div className="fixed inset-0 z-50 bg-slate-950 flex flex-col font-sans select-none">
      {/* ── Top Bar ── */}
      <div
        className="px-4 py-3 bg-slate-950/90 backdrop-blur-xl border-b border-slate-800 flex items-center justify-between"
        style={{ paddingTop: 'calc(0.75rem + env(safe-area-inset-top, 0px))' }}
      >
        <button
          type="button"
          onClick={() => {
            triggerHaptic(10);
            onExit();
          }}
          className="p-2 rounded-xl bg-slate-900 border border-slate-800 text-slate-300 hover:text-white cursor-pointer"
        >
          <ArrowLeft size={18} />
        </button>

        <div className="text-center">
          <div className="flex items-center justify-center gap-1.5">
            <span className="text-xs font-black text-white">
              {t('questionLabel')} {currentIdx + 1} / {totalQuestions}
            </span>
            <span className={`text-[9px] font-black uppercase px-1.5 py-0.2 rounded ${
              dataSource === 'db' ? 'bg-emerald-950 text-emerald-400 border border-emerald-800' :
              dataSource === 'offline' ? 'bg-cyan-950 text-cyan-400 border border-cyan-800' :
              'bg-slate-800 text-slate-400'
            }`}>
              {dataSource === 'db' ? t('dbReal') : dataSource === 'offline' ? t('dbOffline') : t('dbFallback')}
            </span>
          </div>
          <span className="text-[10px] text-slate-400 block truncate max-w-[130px]">{topicName}</span>
        </div>

        <div className="flex items-center gap-1.5">
          {/* Nút Cấu hình phiên học */}
          <button
            type="button"
            onClick={() => {
              triggerHaptic(12);
              setIsConfigOpen(true);
            }}
            className="p-1.5 rounded-xl bg-slate-900 border border-slate-800 text-cyan-400 hover:text-cyan-300 cursor-pointer"
            title="Cấu hình phiên làm bài"
          >
            <SlidersHorizontal size={14} />
          </button>

          {/* Nút Đổi Ngôn Ngữ */}
          <button
            type="button"
            onClick={() => {
              triggerHaptic(12);
              setLang(lang === 'vi' ? 'en' : 'vi');
            }}
            className="px-2 py-1 rounded-xl bg-slate-900 border border-slate-800 text-sky-400 hover:text-sky-300 text-xs font-mono font-black flex items-center gap-1 cursor-pointer"
          >
            <Globe size={11} />
            <span>{lang.toUpperCase()}</span>
          </button>

          {/* Đồng hồ phiên */}
          <div className="flex items-center gap-1 px-2 py-1 rounded-xl bg-slate-900 border border-slate-800 text-amber-300 text-xs font-mono font-bold">
            <Clock size={11} />
            <span>{formatTime(timeLeft)}</span>
          </div>
        </div>
      </div>

      {/* ── Progress Bar ── */}
      <div className="w-full h-1 bg-slate-900">
        <div
          className="h-full transition-all duration-300"
          style={{
            width: `${((currentIdx + 1) / totalQuestions) * 100}%`,
            backgroundColor: variant.colorScheme.primary,
          }}
        />
      </div>

      {/* ── Question Body Sub-component ── */}
      <PracticeQuestionBody
        currentQ={currentQ}
        currentIdx={currentIdx}
        selectedKey={selectedAnswers[currentIdx]}
        showExplanation={showExplanation}
        primaryColor={variant.colorScheme.primary}
        onSelectOption={handleSelectOption}
        onOpenStory={() => setShowStoryModal(true)}
      />

      {/* ── Bottom Controls ── */}
      <div
        className="p-3 bg-slate-950/90 backdrop-blur-xl border-t border-slate-800 flex items-center justify-between gap-2"
        style={{ paddingBottom: 'calc(0.75rem + env(safe-area-inset-bottom, 12px))' }}
      >
        <button
          type="button"
          onClick={() => {
            triggerHaptic(10);
            setCurrentIdx((i) => Math.max(0, i - 1));
          }}
          disabled={currentIdx === 0}
          className="p-3 rounded-xl bg-slate-900 border border-slate-800 text-slate-300 disabled:opacity-30 cursor-pointer"
        >
          <ChevronLeft size={18} />
        </button>

        <button
          type="button"
          onClick={() => {
            triggerHaptic(15);
            setShowExplanation(!showExplanation);
          }}
          className="px-3.5 py-2.5 rounded-xl bg-slate-900 border border-slate-800 text-purple-300 text-xs font-bold flex items-center gap-1.5 cursor-pointer"
        >
          <Sparkles size={13} />
          <span>{showExplanation ? t('hideExplanation') : t('showExplanation')}</span>
        </button>

        <button
          type="button"
          onClick={() => {
            triggerHaptic(15);
            if (currentIdx < totalQuestions - 1) {
              setCurrentIdx((i) => i + 1);
            } else {
              setIsSubmitted(true);
            }
          }}
          className="px-4 py-2.5 rounded-xl font-black text-xs text-slate-950 flex items-center gap-1 cursor-pointer active:scale-95 shadow-lg"
          style={{ backgroundColor: variant.colorScheme.primary }}
        >
          <span>{currentIdx < totalQuestions - 1 ? t('nextQuestion') : t('submitAttempt')}</span>
          <ChevronRight size={16} />
        </button>
      </div>

      {/* Micro-Lesson Interactive Slice Storyboard */}
      {showStoryModal && (
        <InteractiveSliceViewer
          slices={transpileExplanationToSlices(currentQ.explanation, currentQ.content, variant.code)}
          topicName={`${topicName} — Câu ${currentIdx + 1}`}
          onClose={() => setShowStoryModal(false)}
        />
      )}

      {/* Modal Cấu hình phiên học */}
      <MobileSessionConfigModal
        isOpen={isConfigOpen}
        onClose={() => setIsConfigOpen(false)}
        config={sessionConfig}
        onSave={(newCfg) => setSessionConfig(newCfg)}
      />
    </div>
  );
};
