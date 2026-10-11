import React, { useState, useEffect } from 'react';
import { ArrowLeft, Clock, CheckCircle2, ChevronLeft, ChevronRight, Sparkles, AlertTriangle, ShieldCheck } from 'lucide-react';
import { useMobileApp } from '../context/MobileAppContext';
import { learnerQuizService } from '@/services/learner-quiz.service';
import { offlineExamService } from '@/services/offline-exam.service';
import { KidsPracticeView } from './KidsPracticeView';
import { PracticeQuestionBody } from './PracticeQuestionBody';
import { InteractiveSliceViewer } from './InteractiveSliceViewer';
import { transpileExplanationToSlices } from '../utils/explanation-slice-transpiler';

import { PracticeQuestionItem, MobilePracticeRoomProps } from '../types/mobile-practice.types';

export const MobilePracticeRoom: React.FC<MobilePracticeRoomProps> = ({
  topicId,
  topicName,
  onExit,
}) => {
  const { variant, triggerHaptic } = useMobileApp();

  // If this variant is configured with Kids Gamified UI, render KidsPracticeView
  if (variant.uiMode === 'gamified_kids') {
    return <KidsPracticeView topicName={topicName} onExit={onExit} />;
  }

  const [currentIdx, setCurrentIdx] = useState<number>(0);
  const [selectedAnswers, setSelectedAnswers] = useState<Record<number, string>>({});
  const [showExplanation, setShowExplanation] = useState<boolean>(false);
  const [showStoryModal, setShowStoryModal] = useState<boolean>(false);
  const [timeLeft, setTimeLeft] = useState<number>(1200); // 20 phút
  const [dataSource, setDataSource] = useState<'db' | 'offline' | 'fallback'>('db');
  const [loading, setLoading] = useState<boolean>(true);

  const [questions, setQuestions] = useState<PracticeQuestionItem[]>([
    {
      id: 'q_fb_1',
      content: `[Bộ Đề Chuẩn ${variant.shortName}] Khái niệm cốt lõi nào sau đây là chuẩn mực hàng đầu trong lĩnh vực?`,
      options: ['A', 'B', 'C', 'D'].map((k) => ({ key: k, text: `Phương án chuẩn ngành ${k}` })),
      correctKey: 'A',
      explanation: 'Chuẩn kiến thức tích hợp hệ thống bảo đảm an toàn nghiệp vụ.',
    },
  ]);

  useEffect(() => {
    let isMounted = true;
    const loadRealQuestions = async () => {
      setLoading(true);
      const targetKey = topicId || variant.domainCode || 'ALL';

      // 1. Kiểm tra IndexedDB trước
      try {
        const cachedPkg = await offlineExamService.getOfflineExamPackage(targetKey);
        if (cachedPkg && Array.isArray(cachedPkg.questions) && cachedPkg.questions.length > 0) {
          if (isMounted) {
            setQuestions(cachedPkg.questions);
            setDataSource('offline');
            setLoading(false);
          }
          return;
        }
      } catch {}

      // 2. Nạp từ DB thật qua learnerQuizService
      try {
        const rawList = await learnerQuizService.generatePracticeQuestions({
          topicConfigs: [{ topicKey: targetKey, numberOfQuestions: 30, isRandom: false, startIndex: 1 }],
        });

        if (Array.isArray(rawList) && rawList.length > 0) {
          const mapped: PracticeQuestionItem[] = rawList.map((q: any) => ({
            id: String(q.id),
            content: q.content || q.text || 'Nội dung câu hỏi',
            isFatal: !!(q.isCritical || q.isFatal || (q.content && q.content.includes('*'))),
            options: Array.isArray(q.options)
              ? q.options.map((o: any, oIdx: number) => ({
                  key: ['A', 'B', 'C', 'D', 'E', 'F'][oIdx] || String.fromCharCode(65 + oIdx),
                  text: typeof o === 'string' ? o : (o.content || o.text || String(o)),
                }))
              : [
                  { key: 'A', text: 'Phương án A' },
                  { key: 'B', text: 'Phương án B' },
                  { key: 'C', text: 'Phương án C' },
                  { key: 'D', text: 'Phương án D' },
                ],
            correctKey: q.correctAnswer || (q.options?.find((o: any) => o.isCorrect)?.key) || 'A',
            explanation: q.explanation || 'Xem hướng dẫn chi tiết theo quy chuẩn hệ thống.',
          }));

          if (isMounted) {
            setQuestions(mapped);
            setDataSource('db');
            // Cache tự động vào IndexedDB
            offlineExamService.saveExamPackageOffline({
              examId: targetKey,
              title: topicName,
              domainCode: variant.domainCode,
              questionCount: mapped.length,
              downloadedAt: Date.now(),
              questions: mapped,
            });
          }
        }
      } catch (err) {
        if (isMounted) {
          setDataSource('fallback');
        }
      } finally {
        if (isMounted) setLoading(false);
      }
    };

    loadRealQuestions();
    return () => { isMounted = false; };
  }, [topicId, variant.domainCode, topicName]);

  const currentQ = questions[currentIdx] || questions[0];
  const totalQuestions = questions.length;

  useEffect(() => {
    const timer = setInterval(() => setTimeLeft((t) => (t > 0 ? t - 1 : 0)), 1000);
    return () => clearInterval(timer);
  }, []);

  const handleSelectOption = async (optKey: string) => {
    triggerHaptic(15);
    setSelectedAnswers((prev) => ({ ...prev, [currentIdx]: optKey }));
    await offlineExamService.recordAnswerAsync('practice-session-active', currentQ.id, optKey);
  };

  const formatTime = (s: number) =>
    `${Math.floor(s / 60).toString().padStart(2, '0')}:${(s % 60).toString().padStart(2, '0')}`;

  return (
    <div className="fixed inset-0 z-50 bg-slate-950 flex flex-col font-sans select-none">
      {/* ── Top Bar ────────────────────────────────────────────────────── */}
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
              Câu {currentIdx + 1} / {totalQuestions}
            </span>
            <span className={`text-[9px] font-black uppercase px-1.5 py-0.2 rounded ${
              dataSource === 'db' ? 'bg-emerald-950 text-emerald-400 border border-emerald-800' :
              dataSource === 'offline' ? 'bg-cyan-950 text-cyan-400 border border-cyan-800' :
              'bg-slate-800 text-slate-400'
            }`}>
              {dataSource === 'db' ? 'DB Thật' : dataSource === 'offline' ? 'Offline' : 'Dự Phòng'}
            </span>
          </div>
          <span className="text-[10px] text-slate-400 block truncate max-w-[180px]">{topicName}</span>
        </div>

        <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-xl bg-slate-900 border border-slate-800 text-amber-300 text-xs font-mono font-bold">
          <Clock size={12} />
          <span>{formatTime(timeLeft)}</span>
        </div>
      </div>

      {/* ── Progress Bar ────────────────────────────────────────────────── */}
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

      {/* ── Bottom Controls ────────────────────────────────────────────── */}
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
          <span>{showExplanation ? 'Ẩn Giải Thích' : 'Xem Mẹo/Giải Thích'}</span>
        </button>

        <button
          type="button"
          onClick={() => {
            triggerHaptic(15);
            if (currentIdx < totalQuestions - 1) setCurrentIdx((i) => i + 1);
            else onExit();
          }}
          className="px-4 py-2.5 rounded-xl font-black text-xs text-slate-950 flex items-center gap-1 cursor-pointer active:scale-95 shadow-lg"
          style={{ backgroundColor: variant.colorScheme.primary }}
        >
          <span>{currentIdx < totalQuestions - 1 ? 'Câu Tiếp' : 'Nộp Bài'}</span>
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
    </div>
  );
};
