import React, { useState } from 'react';
import { Layers, AlertTriangle, Filter, Clock, Cpu } from 'lucide-react';
import { SymposiumDialogueTurn } from '@/services/ai-swarm.service';

interface DialecticalTimelineFeedProps {
  dialogueLog: SymposiumDialogueTurn[];
}

export function DialecticalTimelineFeed({ dialogueLog }: DialecticalTimelineFeedProps) {
  const [selectedPhaseFilter, setSelectedPhaseFilter] = useState<number | 'ALL'>('ALL');
  const [selectedCouncilFilter, setSelectedCouncilFilter] = useState<string | 'ALL'>('ALL');

  const uniqueCouncils = Array.from(new Set(dialogueLog.map(t => t.councilDisplayName)));

  const filteredLog = dialogueLog.filter(turn => {
    if (selectedPhaseFilter !== 'ALL' && turn.phaseNumber !== selectedPhaseFilter) return false;
    if (selectedCouncilFilter !== 'ALL' && turn.councilDisplayName !== selectedCouncilFilter) return false;
    return true;
  });

  return (
    <div className="rounded-3xl p-5 sm:p-7 bg-slate-900/90 border border-slate-800 shadow-2xl space-y-5">
      {/* Header & Filters */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-800/80 pb-4">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-xl bg-cyan-500/10 text-cyan-400 border border-cyan-500/20">
            <Layers size={18} />
          </div>
          <div>
            <h3 className="text-base font-black text-white">Toàn Văn Biên Bản Tranh Biện Từng Giai Đoạn</h3>
            <p className="text-xs text-slate-400">
              Tổng số {dialogueLog.length} lượt phát biểu biện chứng • Khử ngụy biện đa chiều
            </p>
          </div>
        </div>

        {/* Filters */}
        <div className="flex flex-wrap items-center gap-2 text-xs">
          <div className="flex items-center gap-1.5 bg-slate-950 px-2.5 py-1.5 rounded-xl border border-slate-800 text-slate-300">
            <Filter size={12} className="text-cyan-400" />
            <select
              value={selectedPhaseFilter}
              onChange={(e) => setSelectedPhaseFilter(e.target.value === 'ALL' ? 'ALL' : Number(e.target.value))}
              className="bg-transparent text-xs text-slate-200 focus:outline-none cursor-pointer"
            >
              <option value="ALL" className="bg-slate-900">Tất cả các Pha</option>
              <option value="1" className="bg-slate-900">Pha 1: Luận đề</option>
              <option value="2" className="bg-slate-900">Pha 2: Socratic</option>
              <option value="3" className="bg-slate-900">Pha 3: Phản biện</option>
              <option value="4" className="bg-slate-900">Pha 4: Hợp nhất</option>
            </select>
          </div>

          {uniqueCouncils.length > 1 && (
            <div className="bg-slate-950 px-2.5 py-1.5 rounded-xl border border-slate-800 text-slate-300">
              <select
                value={selectedCouncilFilter}
                onChange={(e) => setSelectedCouncilFilter(e.target.value)}
                className="bg-transparent text-xs text-slate-200 focus:outline-none cursor-pointer"
              >
                <option value="ALL" className="bg-slate-900">Tất cả Viện</option>
                {uniqueCouncils.map((cName, idx) => (
                  <option key={idx} value={cName} className="bg-slate-900">{cName}</option>
                ))}
              </select>
            </div>
          )}
        </div>
      </div>

      {/* Feed List */}
      <div className="space-y-3.5">
        {filteredLog.length === 0 ? (
          <div className="p-8 text-center text-xs text-slate-500">
            Không có lượt phát biểu nào phù hợp với bộ lọc.
          </div>
        ) : (
          filteredLog.map((turn, i) => (
            <div
              key={i}
              className={`p-4 sm:p-5 rounded-2xl border transition-all ${
                turn.phaseNumber === 3
                  ? 'bg-rose-950/15 border-rose-900/40 hover:border-rose-700/60'
                  : turn.phaseNumber === 4
                  ? 'bg-violet-950/15 border-violet-900/40 hover:border-violet-700/60'
                  : 'bg-slate-950/70 border-slate-800/80 hover:border-slate-700'
              }`}
            >
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 mb-2.5">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-slate-800 text-cyan-300 border border-slate-700">
                    Pha {turn.phaseNumber}: {turn.phaseName}
                  </span>
                  <span className="text-xs font-bold text-white">
                    {turn.speakerTitle}
                  </span>
                  <span className="text-xs text-slate-400">
                    — {turn.councilDisplayName}
                  </span>
                  {turn.spatioTemporalPerspective && (
                    <span className="text-[10px] font-mono px-2 py-0.5 rounded-full bg-cyan-950/80 border border-cyan-800/40 text-cyan-300">
                      {turn.spatioTemporalPerspective}
                    </span>
                  )}
                </div>

                <div className="flex items-center gap-2.5 text-[10px] font-mono text-slate-400">
                  {turn.modelCode ? (
                    <span className="flex items-center gap-1">
                      <Cpu size={11} className="text-cyan-400" />
                      <span>{turn.modelCode}</span>
                    </span>
                  ) : (
                    <span className="text-slate-500 italic">Mô phỏng</span>
                  )}
                  {turn.keySource && (
                    <span className={`px-2 py-0.5 rounded font-mono font-bold text-[9px] border ${
                      turn.keySource === 'UserBYOK'
                        ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40'
                        : turn.keySource === 'TenantBYOK'
                        ? 'bg-sky-500/20 text-sky-300 border-sky-500/40'
                        : turn.keySource === 'CommunityPool'
                        ? 'bg-blue-500/20 text-blue-300 border-blue-500/40'
                        : 'bg-purple-500/20 text-purple-300 border-purple-500/40'
                    }`}>
                      {turn.keySource === 'UserBYOK' ? '🔑 User BYOK' : turn.keySource === 'TenantBYOK' ? '🏛️ Tenant BYOK' : turn.keySource === 'CommunityPool' ? '🌐 Community Pool' : '🛡️ System Default'}
                    </span>
                  )}
                  {turn.latencyMs > 0 ? (
                    <span className="flex items-center gap-1 text-slate-400">
                      <Clock size={11} className="text-slate-500" />
                      <span>{turn.latencyMs}ms</span>
                    </span>
                  ) : (
                    <span className="px-1.5 py-0.2 rounded bg-slate-800 text-[9px] text-slate-400">
                      Chưa đo
                    </span>
                  )}
                </div>
              </div>

              {/* Rationale Phân vai động theo ngữ cảnh */}
              {turn.assignmentRationale && (
                <div className="mb-2 px-3 py-1.5 rounded-xl bg-slate-900/90 border border-slate-800 text-[11px] text-cyan-200/90 italic flex items-center gap-1.5">
                  <span className="text-cyan-400 font-bold not-italic">⚡ Phân vai:</span>
                  <span>{turn.assignmentRationale}</span>
                </div>
              )}

              <div className="text-xs text-slate-300 leading-relaxed font-sans whitespace-pre-line pl-3 border-l-2 border-cyan-500/40">
                {turn.content}
              </div>

              {turn.keyTheses && turn.keyTheses.length > 0 && (
                <div className="mt-3 flex flex-wrap gap-1.5 items-center">
                  <span className="text-[10px] uppercase font-bold text-slate-500">Hạt nhân luận điểm:</span>
                  {turn.keyTheses.map((thesis: string, tidx: number) => (
                    <span
                      key={tidx}
                      className="px-2 py-0.5 rounded-full text-[10px] bg-slate-800/90 text-cyan-200 border border-slate-700/80"
                    >
                      • {thesis}
                    </span>
                  ))}
                </div>
              )}

              {turn.exposedFallacies && turn.exposedFallacies.length > 0 && (
                <div className="mt-2.5 p-2 rounded-xl bg-rose-950/40 border border-rose-800/40 flex items-center gap-2 text-xs text-rose-300">
                  <AlertTriangle size={14} className="text-rose-400 flex-shrink-0" />
                  <span>
                    <strong>Bẫy ngụy biện đã vạch trần:</strong> {turn.exposedFallacies.join(', ')}
                  </span>
                </div>
              )}
            </div>
          ))
        )}
      </div>
    </div>
  );
}
