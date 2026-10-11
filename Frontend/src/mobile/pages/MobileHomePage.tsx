import React from 'react';
import { Play, Flame, Award, ChevronRight, Download, CheckCircle2, BookOpen } from 'lucide-react';
import { DynamicDomainIcon } from '@/components/common/DynamicDomainIcon';
import { useMobileApp } from '../context/MobileAppContext';

interface MobileHomePageProps {
  onStartTopic: (topicId: string, topicName: string) => void;
  onStartFullMockExam: () => void;
}

export const MobileHomePage: React.FC<MobileHomePageProps> = ({
  onStartTopic,
  onStartFullMockExam,
}) => {
  const { variant, triggerHaptic } = useMobileApp();

  return (
    <div className="space-y-4">
      {/* ── Hero Metamorphic Banner ────────────────────────────────────── */}
      <div
        className={`p-5 rounded-3xl bg-gradient-to-br ${variant.colorScheme.bgGradient} border border-slate-800 shadow-2xl relative overflow-hidden`}
      >
        <div className="relative z-10 space-y-3">
          <div className="flex items-center gap-2">
            <span
              className={`text-[10px] font-black uppercase tracking-wider px-2 py-0.5 rounded-full border ${variant.colorScheme.badgeBg} ${variant.colorScheme.badgeText}`}
            >
              CHUYÊN BIỆT HÓA 2026
            </span>
            <span className="text-[11px] text-slate-400 font-medium">Chuẩn Quốc Gia</span>
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
              <span className="text-[10px] text-slate-400 block font-bold">Đã Luyện</span>
              <span className="text-sm font-black text-white">420 câu</span>
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
            <span>Vào Thi Thử Thích Ứng Ngay</span>
          </button>
        </div>
      </div>

      {/* ── Tags Gợi Ý Chuyên Sâu ───────────────────────────────────────── */}
      <div className="flex gap-1.5 overflow-x-auto pb-1 scrollbar-none">
        {variant.suggestedTags.map((tag) => (
          <span
            key={tag}
            className="px-3 py-1 rounded-xl bg-slate-900 border border-slate-800 text-[11px] font-bold text-slate-300 whitespace-nowrap"
          >
            #{tag}
          </span>
        ))}
      </div>

      {/* ── Danh Sách Chuyên Đề Trọng Tâm ──────────────────────────────── */}
      <div className="space-y-2.5">
        <div className="flex items-center justify-between">
          <h2 className="text-xs font-black uppercase text-slate-400 tracking-wider flex items-center gap-1.5">
            <Flame size={14} className="text-amber-400" />
            <span>Chuyên Đề Trọng Tâm ({variant.sampleTopics.length})</span>
          </h2>
          <span className="text-[11px] font-bold text-slate-500">Xem tất cả</span>
        </div>

        <div className="space-y-2">
          {variant.sampleTopics.map((topic, idx) => (
            <button
              key={topic.id}
              type="button"
              onClick={() => {
                triggerHaptic(15);
                onStartTopic(topic.id, topic.name);
              }}
              className="w-full text-left p-3.5 rounded-2xl bg-slate-900/80 border border-slate-800 hover:border-slate-700 flex items-center justify-between gap-3 transition-all cursor-pointer active:scale-98"
            >
              <div className="flex items-center gap-3 min-w-0">
                <div className="w-8 h-8 rounded-xl bg-slate-800 flex items-center justify-center text-xs font-black text-slate-300 shrink-0">
                  {idx + 1}
                </div>
                <div className="min-w-0">
                  <h3 className="text-xs font-black text-white truncate">{topic.name}</h3>
                  <div className="flex items-center gap-2 mt-0.5 text-[10px] text-slate-400">
                    <span className="flex items-center gap-1">
                      <BookOpen size={11} />
                      {topic.questionCount} câu hỏi
                    </span>
                    <span>•</span>
                    <span className="text-emerald-400 font-bold">Đã nạp Offline</span>
                  </div>
                </div>
              </div>

              <div className="p-1 rounded-lg text-slate-400">
                <ChevronRight size={16} />
              </div>
            </button>
          ))}
        </div>
      </div>
    </div>
  );
};
