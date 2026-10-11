import React, { useState, useEffect } from 'react';
import { Play, Flame, Award, ChevronRight, Download, CheckCircle2, BookOpen, Database, RefreshCw } from 'lucide-react';
import { useMobileApp } from '../context/MobileAppContext';
import { learnerQuizService } from '@/services/learner-quiz.service';
import { offlineExamService } from '@/services/offline-exam.service';

interface MobileHomePageProps {
  onStartTopic: (topicId: string, topicName: string) => void;
  onStartFullMockExam: () => void;
}

interface DisplayTopic {
  id: string;
  code: string;
  name: string;
  questionCount: number;
  isRealDb: boolean;
  isDownloaded: boolean;
}

export const MobileHomePage: React.FC<MobileHomePageProps> = ({
  onStartTopic,
  onStartFullMockExam,
}) => {
  const { variant, triggerHaptic } = useMobileApp();
  const [topics, setTopics] = useState<DisplayTopic[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;
    const fetchRealData = async () => {
      setIsLoading(true);
      try {
        const [dbTopics, dbCounts, offlineExams] = await Promise.all([
          learnerQuizService.getTopics(true),
          learnerQuizService.getTopicCounts(),
          offlineExamService.listDownloadedExams(),
        ]);

        const countMap = new Map<string, number>();
        if (Array.isArray(dbCounts)) {
          dbCounts.forEach((c: any) => countMap.set(c.topicCode || c.code, c.count || 0));
        }

        const downloadedSet = new Set(offlineExams.map((e) => e.examId));

        // Lọc topic theo domain của variant
        const domain = variant.domainCode?.toUpperCase() || 'GENERAL';
        const matched = Array.isArray(dbTopics)
          ? dbTopics.filter((t: any) => {
              const tDomain = (t.domainCode || t.categoryCode || '').toUpperCase();
              if (domain === 'DRIVING') return tDomain.includes('DRIVING') || tDomain.includes('GT') || t.code?.startsWith('GPLX');
              if (domain === 'THPT') return tDomain.includes('TNPT') || tDomain.includes('TOAN') || t.code?.startsWith('TNPT');
              if (domain === 'EDUCATION') return tDomain.includes('EDU') || tDomain.includes('TIENGANH') || tDomain.includes('ENGLISH');
              if (domain === 'BANKING') return tDomain.includes('BANK') || t.code?.startsWith('2026_DOT2');
              return tDomain === domain;
            })
          : [];

        if (isMounted) {
          if (matched.length > 0) {
            setTopics(
              matched.map((t: any) => ({
                id: t.code,
                code: t.code,
                name: t.name || t.code,
                questionCount: countMap.get(t.code) || t.questionCountCached || 50,
                isRealDb: true,
                isDownloaded: downloadedSet.has(t.code),
              }))
            );
          } else {
            // Fallback sang sampleTopics nếu DB chưa có category tương ứng
            setTopics(
              variant.sampleTopics.map((st) => ({
                id: st.id,
                code: st.id,
                name: st.name,
                questionCount: st.questionCount,
                isRealDb: false,
                isDownloaded: downloadedSet.has(st.id),
              }))
            );
          }
        }
      } catch (err) {
        if (isMounted) {
          setTopics(
            variant.sampleTopics.map((st) => ({
              id: st.id,
              code: st.id,
              name: st.name,
              questionCount: st.questionCount,
              isRealDb: false,
              isDownloaded: false,
            }))
          );
        }
      } finally {
        if (isMounted) setIsLoading(false);
      }
    };

    fetchRealData();
    return () => { isMounted = false; };
  }, [variant.id, variant.domainCode]);

  const handleDownloadOffline = async (e: React.MouseEvent, topic: DisplayTopic) => {
    e.stopPropagation();
    triggerHaptic(20);
    setDownloadingId(topic.id);
    try {
      const questions = await learnerQuizService.generatePracticeQuestions({
        topicConfigs: [{ topicKey: topic.code, numberOfQuestions: 100, isRandom: false, startIndex: 1 }],
      });

      await offlineExamService.saveExamPackageOffline({
        examId: topic.id,
        title: topic.name,
        domainCode: variant.domainCode,
        questionCount: questions.length,
        downloadedAt: Date.now(),
        questions,
      });

      setTopics((prev) =>
        prev.map((t) => (t.id === topic.id ? { ...t, isDownloaded: true } : t))
      );
    } finally {
      setDownloadingId(null);
    }
  };

  const totalQuestions = topics.reduce((sum, t) => sum + t.questionCount, 0);

  return (
    <div className="space-y-4">
      {/* ── Hero Metamorphic Banner ────────────────────────────────────── */}
      <div className={`p-5 rounded-3xl bg-gradient-to-br ${variant.colorScheme.bgGradient} border border-slate-800 shadow-2xl relative overflow-hidden`}>
        <div className="relative z-10 space-y-3">
          <div className="flex items-center justify-between">
            <span className={`text-[10px] font-black uppercase tracking-wider px-2 py-0.5 rounded-full border ${variant.colorScheme.badgeBg} ${variant.colorScheme.badgeText}`}>
              CHUYÊN BIỆT HÓA 2026
            </span>
            <div className="flex items-center gap-1.5 text-[10px] font-bold text-emerald-400 bg-emerald-950/60 px-2 py-0.5 rounded-full border border-emerald-800/80">
              <Database size={10} />
              <span>Dữ liệu PostgreSQL</span>
            </div>
          </div>

          <div>
            <h1 className="text-xl font-black text-white leading-tight">{variant.name}</h1>
            <p className="text-xs text-slate-300 mt-1 leading-relaxed">{variant.tagline}</p>
          </div>

          {/* Quick Metrics Bar */}
          <div className="grid grid-cols-3 gap-2 pt-2 border-t border-slate-800/80 text-center">
            <div className="p-2 rounded-xl bg-slate-900/60 border border-slate-800">
              <span className="text-[10px] text-slate-400 block font-bold">Năng Lực IRT</span>
              <span className="text-sm font-black text-amber-300">+1.45 θ</span>
            </div>
            <div className="p-2 rounded-xl bg-slate-900/60 border border-slate-800">
              <span className="text-[10px] text-slate-400 block font-bold">Tổng Đề DB</span>
              <span className="text-sm font-black text-white">{totalQuestions} câu</span>
            </div>
            <div className="p-2 rounded-xl bg-slate-900/60 border border-slate-800">
              <span className="text-[10px] text-slate-400 block font-bold">Chính Xác</span>
              <span className="text-sm font-black text-emerald-400">92%</span>
            </div>
          </div>

          {/* Nút Thi Thử Toàn Diện Full-Width */}
          <button
            type="button"
            onClick={() => {
              triggerHaptic(20);
              onStartFullMockExam();
            }}
            className="w-full py-3.5 rounded-2xl text-slate-950 font-black text-xs uppercase tracking-wider flex items-center justify-center gap-2 shadow-xl transition-all cursor-pointer active:scale-98"
            style={{ backgroundColor: variant.colorScheme.primary }}
          >
            <Play size={16} fill="currentColor" />
            <span>Vào Thi Thử Thích Ứng Toàn Diện</span>
          </button>
        </div>
      </div>

      {/* ── Danh Sách Chuyên Đề Trọng Tâm ──────────────────────────────── */}
      <div className="space-y-2.5">
        <div className="flex items-center justify-between">
          <h2 className="text-xs font-black uppercase text-slate-400 tracking-wider flex items-center gap-1.5">
            <Flame size={14} className="text-amber-400" />
            <span>Chuyên Đề Trọng Tâm ({topics.length})</span>
          </h2>
          <span className="text-[11px] font-bold text-slate-500">Chuẩn Quốc Gia</span>
        </div>

        <div className="space-y-2">
          {topics.map((topic, idx) => (
            <div
              key={topic.id}
              onClick={() => {
                triggerHaptic(15);
                onStartTopic(topic.id, topic.name);
              }}
              className="w-full p-3.5 rounded-2xl bg-slate-900/80 border border-slate-800 hover:border-slate-700 flex items-center justify-between gap-3 transition-all cursor-pointer active:scale-98"
            >
              <div className="flex items-center gap-3 min-w-0">
                <div className="w-8 h-8 rounded-xl bg-slate-800 flex items-center justify-center text-xs font-black text-slate-300 shrink-0">
                  {idx + 1}
                </div>
                <div className="min-w-0">
                  <h3 className="text-xs font-black text-white truncate">{topic.name}</h3>
                  <div className="flex items-center gap-2 mt-0.5 text-[10px] text-slate-400">
                    <span className="flex items-center gap-1 text-slate-300 font-bold">
                      <BookOpen size={11} />
                      {topic.questionCount} câu hỏi
                    </span>
                    <span>•</span>
                    <span className={topic.isRealDb ? 'text-cyan-400 font-bold' : 'text-slate-400'}>
                      {topic.isRealDb ? 'Online DB' : 'Mẫu chuẩn'}
                    </span>
                  </div>
                </div>
              </div>

              <div className="flex items-center gap-2">
                <button
                  type="button"
                  title="Tải về ngoại tuyến (Offline Pack)"
                  onClick={(e) => handleDownloadOffline(e, topic)}
                  disabled={downloadingId === topic.id}
                  className={`p-2 rounded-xl transition-all ${
                    topic.isDownloaded
                      ? 'text-emerald-400 bg-emerald-950/60 border border-emerald-800/80'
                      : 'text-slate-400 hover:text-white bg-slate-800/80 hover:bg-slate-700'
                  }`}
                >
                  {downloadingId === topic.id ? (
                    <RefreshCw size={14} className="animate-spin text-amber-400" />
                  ) : topic.isDownloaded ? (
                    <CheckCircle2 size={14} />
                  ) : (
                    <Download size={14} />
                  )}
                </button>
                <div className="p-1 text-slate-400">
                  <ChevronRight size={16} />
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
