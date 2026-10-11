import React, { useState } from 'react';
import { Landmark, Compass, Cpu, AlertTriangle, Lightbulb, BookOpen, ChevronDown, ChevronUp } from 'lucide-react';
import { CouncilAcademicContributionDto } from '@/services/ai-swarm.service';

interface CouncilContributionsPanelProps {
  contributions: CouncilAcademicContributionDto[];
}

export function CouncilContributionsPanel({ contributions }: CouncilContributionsPanelProps) {
  const [selectedCouncilCode, setSelectedCouncilCode] = useState<string>(
    contributions.length > 0 ? (contributions[0].CouncilCode || contributions[0].councilCode || '') : ''
  );
  const [isCollapsed, setIsCollapsed] = useState<boolean>(false);

  if (!contributions || contributions.length === 0) return null;

  // Hỗ trợ cả PascalCase và camelCase
  const normalizedList = contributions.map(c => ({
    councilCode: c.CouncilCode || c.councilCode || '',
    councilDisplayName: c.CouncilDisplayName || c.councilDisplayName || 'Hội đồng',
    spatioTemporalAffinity: c.SpatioTemporalAffinity || c.spatioTemporalAffinity || '',
    representativeModel: c.RepresentativeModel || c.representativeModel || '',
    scholarTitle: c.ScholarTitle || c.scholarTitle || '',
    coreEpistemicStance: c.CoreEpistemicStance || c.coreEpistemicStance || '',
    deepAnalysis: c.DeepAnalysis || c.deepAnalysis || '',
    primaryObjection: c.PrimaryObjection || c.primaryObjection || '',
    proposedRemedy: c.ProposedRemedy || c.proposedRemedy || '',
    keyTheses: c.KeyTheses || c.keyTheses || []
  }));

  const activeItem = normalizedList.find(c => c.councilCode === selectedCouncilCode) || normalizedList[0];

  return (
    <div className="rounded-3xl p-5 sm:p-7 bg-slate-900/95 border border-cyan-500/30 shadow-2xl space-y-5 animate-in fade-in duration-300">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-800/80 pb-4">
        <div className="flex items-center gap-3">
          <div className="p-2.5 rounded-2xl bg-cyan-500/10 text-cyan-400 border border-cyan-500/20">
            <Landmark size={22} />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="text-[10px] font-black uppercase tracking-wider text-cyan-400">
                Epistemic Divergence & Council Stances
              </span>
              <span className="text-[10px] px-2 py-0.5 rounded-full bg-cyan-950/80 border border-cyan-500/40 text-cyan-300 font-bold">
                {normalizedList.length} Viện Độc Lập
              </span>
            </div>
            <h3 className="text-base sm:text-lg font-black text-white">
              Hồ Sơ Luận Điểm & Đóng Góp Học Thuật Chi Tiết Từng Viện
            </h3>
          </div>
        </div>

        <button
          onClick={() => setIsCollapsed(!isCollapsed)}
          className="self-end sm:self-auto p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition cursor-pointer"
        >
          {isCollapsed ? <ChevronDown size={18} /> : <ChevronUp size={18} />}
        </button>
      </div>

      {!isCollapsed && (
        <div className="space-y-4">
          {/* Council Selector Tabs */}
          <div className="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-thin scrollbar-thumb-slate-800">
            {normalizedList.map((c) => {
              const isActive = c.councilCode === activeItem.councilCode;
              return (
                <button
                  key={c.councilCode}
                  onClick={() => setSelectedCouncilCode(c.councilCode)}
                  className={`px-4 py-2.5 rounded-2xl text-xs font-bold transition flex items-center gap-2.5 whitespace-nowrap cursor-pointer border ${
                    isActive
                      ? 'bg-gradient-to-r from-cyan-950 to-slate-900 border-cyan-500/60 text-cyan-200 shadow-lg shadow-cyan-950/50 ring-1 ring-cyan-500/30'
                      : 'bg-slate-950/70 border-slate-800 text-slate-400 hover:text-white hover:border-slate-700'
                  }`}
                >
                  <span className={`w-2 h-2 rounded-full ${isActive ? 'bg-cyan-400 animate-pulse' : 'bg-slate-600'}`} />
                  <span>{c.councilDisplayName}</span>
                  <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-slate-900 border border-slate-800 text-slate-400">
                    {c.representativeModel}
                  </span>
                </button>
              );
            })}
          </div>

          {/* Active Council Detail Card */}
          <div className="p-5 sm:p-6 rounded-2xl bg-slate-950/90 border border-slate-800/80 space-y-4">
            {/* Meta Row */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-800/70 pb-3.5">
              <div>
                <div className="flex items-center gap-2">
                  <span className="text-sm sm:text-base font-bold text-white">
                    {activeItem.councilDisplayName}
                  </span>
                  <span className="text-xs text-slate-400">
                    — {activeItem.scholarTitle}
                  </span>
                </div>
                <div className="flex items-center gap-1.5 text-xs text-cyan-400 font-mono mt-0.5">
                  <Compass size={12} />
                  <span>Trường phái: {activeItem.spatioTemporalAffinity}</span>
                </div>
              </div>

              <div className="flex items-center gap-2">
                <span className="text-xs px-3 py-1 rounded-xl bg-cyan-950/60 border border-cyan-500/30 text-cyan-300 font-mono font-semibold flex items-center gap-1.5">
                  <Cpu size={12} className="text-cyan-400" />
                  <span>Mô hình phân vai: <strong>{activeItem.representativeModel}</strong></span>
                </span>
              </div>
            </div>

            {/* Core Stance */}
            <div className="p-3.5 rounded-xl bg-cyan-950/30 border border-cyan-500/20 text-xs sm:text-sm text-cyan-200 font-semibold leading-relaxed">
              🎯 Lập trường nhận thức then chốt: "{activeItem.coreEpistemicStance}"
            </div>

            {/* Deep Analysis */}
            <div className="space-y-1.5">
              <span className="text-[11px] font-bold uppercase tracking-wider text-slate-400 flex items-center gap-1">
                <BookOpen size={12} className="text-indigo-400" /> Luận Giải Chuyên Sâu Của Viện:
              </span>
              <div className="p-4 rounded-xl bg-slate-900/80 border border-slate-800/80 text-xs sm:text-sm text-slate-200 leading-relaxed pl-3.5 border-l-2 border-cyan-500/40">
                {activeItem.deepAnalysis}
              </div>
            </div>

            {/* Key Theses Badges */}
            {activeItem.keyTheses && activeItem.keyTheses.length > 0 && (
              <div className="flex flex-wrap gap-2 items-center pt-1">
                <span className="text-[10px] uppercase font-bold text-slate-500">Hạt nhân luận điểm:</span>
                {activeItem.keyTheses.map((thesis: string, idx: number) => (
                  <span
                    key={idx}
                    className="px-2.5 py-1 rounded-xl text-xs bg-slate-900 border border-slate-800 text-cyan-300 font-medium"
                  >
                    • {thesis}
                  </span>
                ))}
              </div>
            )}

            {/* 2 Cột: Phản biện & Đề xuất giải pháp */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5 pt-2">
              <div className="p-3.5 rounded-xl bg-rose-950/20 border border-rose-900/30 space-y-1.5">
                <div className="flex items-center gap-1.5 text-xs font-bold text-rose-300">
                  <AlertTriangle size={14} className="text-rose-400" />
                  <span>Điểm Phản Biện Sắc Bén Của Viện:</span>
                </div>
                <p className="text-xs text-slate-300 leading-relaxed italic">
                  "{activeItem.primaryObjection}"
                </p>
              </div>

              <div className="p-3.5 rounded-xl bg-emerald-950/20 border border-emerald-900/30 space-y-1.5">
                <div className="flex items-center gap-1.5 text-xs font-bold text-emerald-300">
                  <Lightbulb size={14} className="text-emerald-400" />
                  <span>Giải Pháp Hòa Giải Đột Phá:</span>
                </div>
                <p className="text-xs text-slate-300 leading-relaxed">
                  {activeItem.proposedRemedy}
                </p>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
