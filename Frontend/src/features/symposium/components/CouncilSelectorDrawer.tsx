import React, { useState } from 'react';
import { X, Search, Award, CheckCircle2, ChevronDown, ChevronUp, Compass, Users, Sparkles, Eye } from 'lucide-react';
import { UniversalAiCouncilDto } from '@/services/ai-swarm.service';
import { CouncilDetailModal } from './CouncilDetailModal';

interface CouncilSelectorDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  councils: UniversalAiCouncilDto[];
  selectedCouncilCodes: string[];
  onToggleCouncil: (code: string) => void;
  onSelectAll: () => void;
  onClearToAutomatic: () => void;
}

export function CouncilSelectorDrawer({
  isOpen,
  onClose,
  councils,
  selectedCouncilCodes,
  onToggleCouncil,
  onSelectAll,
  onClearToAutomatic
}: CouncilSelectorDrawerProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [expandedCouncilId, setExpandedCouncilId] = useState<string | null>(null);
  const [inspectingCouncil, setInspectingCouncil] = useState<UniversalAiCouncilDto | null>(null);

  if (!isOpen) return null;

  const filteredCouncils = councils.filter(c =>
    c.displayName.toLowerCase().includes(searchQuery.toLowerCase()) ||
    c.disciplineArea.toLowerCase().includes(searchQuery.toLowerCase()) ||
    c.councilCode.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <div className="fixed inset-0 z-50 flex justify-end bg-black/70 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="w-full max-w-2xl bg-slate-900 border-l border-slate-800 h-full flex flex-col shadow-2xl animate-in slide-in-from-right duration-300">
        {/* Header */}
        <div className="p-6 border-b border-slate-800 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-cyan-500/10 border border-cyan-500/20 text-cyan-400 flex items-center justify-center">
              <Users size={18} />
            </div>
            <div>
              <h2 className="text-lg font-black text-white">Chỉ Định Viện Hàn Lâm Tham Gia</h2>
              <p className="text-xs text-slate-400">
                {selectedCouncilCodes.length === 0
                  ? 'Chế độ: AI Tự Động Tuyển Chọn theo đề tài'
                  : `Đã chọn thủ công: ${selectedCouncilCodes.length} / ${councils.length} Viện`}
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 rounded-xl text-slate-400 hover:text-white hover:bg-slate-800 transition cursor-pointer"
          >
            <X size={18} />
          </button>
        </div>

        {/* Toolbar */}
        <div className="p-4 border-b border-slate-800/80 bg-slate-950/40 flex flex-col sm:flex-row gap-3 items-center justify-between">
          <div className="relative w-full sm:w-72">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Tìm theo tên viện, chuyên môn..."
              className="w-full pl-9 pr-3 py-1.5 rounded-xl bg-slate-900 border border-slate-700 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-cyan-500"
            />
          </div>

          <div className="flex items-center gap-2 w-full sm:w-auto justify-end">
            <button
              onClick={onClearToAutomatic}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition cursor-pointer ${
                selectedCouncilCodes.length === 0
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                  : 'bg-slate-800 text-slate-400 hover:text-white'
              }`}
            >
              Tự Động (AI Dynamic)
            </button>
            <button
              onClick={onSelectAll}
              className="px-3 py-1.5 rounded-lg text-xs font-semibold bg-slate-800 text-slate-300 hover:text-white transition cursor-pointer"
            >
              Chọn Tất Cả
            </button>
          </div>
        </div>

        {/* Hint Banner: Kích đúp để xem chi tiết bản chất viện */}
        <div className="px-4 py-2 bg-gradient-to-r from-cyan-950/40 via-indigo-950/30 to-slate-950 border-b border-cyan-800/30 flex items-center justify-between text-[11px] text-cyan-300 select-none">
          <div className="flex items-center gap-2">
            <Sparkles size={13} className="text-cyan-400 animate-pulse flex-shrink-0" />
            <span><strong>Mẹo:</strong> Kích đúp (Double-click) vào viện bất kỳ để xem <strong>Bản chất Nhận thức & Ghế Viện sĩ</strong>.</span>
          </div>
        </div>

        {/* List of Councils */}
        <div className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-3">
          {filteredCouncils.map((c) => {
            const isSelected = selectedCouncilCodes.includes(c.councilCode);
            const isExpanded = expandedCouncilId === c.id;

            return (
              <div
                key={c.id}
                onDoubleClick={() => setInspectingCouncil(c)}
                title="Kích đúp để xem chi tiết bản chất Viện & cấu trúc Viện sĩ"
                className={`rounded-2xl border transition-all duration-200 overflow-hidden select-none cursor-pointer group ${
                  isSelected
                    ? 'bg-gradient-to-r from-slate-900 via-cyan-950/20 to-slate-900 border-cyan-500/40 ring-1 ring-cyan-500/20'
                    : 'bg-slate-950/50 border-slate-800/80 hover:border-slate-700'
                }`}
              >
                <div className="p-4 flex items-start justify-between gap-3">
                  <div
                    className="flex-1"
                    onClick={() => onToggleCouncil(c.councilCode)}
                  >
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-800 text-cyan-400 border border-slate-700">
                        {c.councilCode}
                      </span>
                      <span className="text-[10px] text-amber-400 font-bold flex items-center gap-1">
                        <Award size={10} /> ELO {c.benchmarkPrestigeElo}
                      </span>
                      {/* Trạng thái sẵn sàng Quorum */}
                      <span className={`text-[10px] font-semibold px-2 py-0.5 rounded-full border ${
                        c.readinessStatus === 'OFFLINE'
                          ? 'bg-rose-950/80 border-rose-500/40 text-rose-300'
                          : c.readinessStatus === 'DEGRADED'
                          ? 'bg-amber-950/80 border-amber-500/40 text-amber-300'
                          : 'bg-emerald-950/80 border-emerald-500/40 text-emerald-300'
                      }`}>
                        {c.readinessStatus === 'OFFLINE' ? '🔴 Chưa cấu hình' : c.readinessStatus === 'DEGRADED' ? '🟡 Dự phòng' : '🟢 Sẵn sàng 100%'}
                      </span>
                    </div>

                    <h4 className="text-sm font-bold text-white mt-1.5 group-hover:text-cyan-300 transition-colors flex items-center gap-2">
                      <span>{c.displayName}</span>
                    </h4>
                    <p className="text-xs text-slate-400 mt-0.5">{c.disciplineArea}</p>

                    {/* Nhãn Không Gian Tri Thức & Văn Hóa */}
                    {c.spatioTemporalAffinity && (
                      <div className="mt-1 flex items-center gap-1.5 text-[11px] font-mono text-cyan-300/90">
                        <Compass size={11} className="text-cyan-400 flex-shrink-0" />
                        <span>Trường phái: {c.spatioTemporalAffinity}</span>
                      </div>
                    )}

                    <p className="text-xs text-slate-500 italic mt-1 line-clamp-2">"{c.epistemicManifesto}"</p>
                  </div>

                  <div className="flex flex-col items-end gap-2">
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        onToggleCouncil(c.councilCode);
                      }}
                      className={`w-6 h-6 rounded-lg flex items-center justify-center border transition cursor-pointer ${
                        isSelected
                          ? 'bg-cyan-500 border-cyan-400 text-slate-950 font-bold'
                          : 'border-slate-700 bg-slate-800 text-transparent hover:border-slate-500'
                      }`}
                    >
                      ✓
                    </button>
                    
                    {/* Nút Xem Chi Tiết Bản Chất Viện */}
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        setInspectingCouncil(c);
                      }}
                      className="px-2 py-1 rounded-lg bg-slate-800/80 hover:bg-cyan-950 hover:text-cyan-300 text-slate-400 text-[10px] font-medium border border-slate-700/60 hover:border-cyan-500/40 flex items-center gap-1 transition cursor-pointer"
                      title="Xem bản chất nhận thức & các ghế Viện sĩ"
                    >
                      <Eye size={10} />
                      <span>Chi tiết</span>
                    </button>

                    {c.seats && c.seats.length > 0 && (
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          setExpandedCouncilId(isExpanded ? null : c.id);
                        }}
                        className="text-[10px] text-slate-400 hover:text-cyan-400 flex items-center gap-0.5 cursor-pointer"
                      >
                        <span>{c.seats.length} ghế</span>
                        {isExpanded ? <ChevronUp size={10} /> : <ChevronDown size={10} />}
                      </button>
                    )}
                  </div>
                </div>

                {/* Expanded Seats View: Cơ chế Phân Vai Động Theo Không - Thời Gian */}
                {isExpanded && c.seats && c.seats.length > 0 && (
                  <div className="bg-slate-950/90 border-t border-slate-800/80 p-3.5 space-y-2">
                    <div className="flex items-center justify-between text-[10px] uppercase font-bold tracking-wider text-slate-400">
                      <span>Ghế Viện Sĩ & Mô Hình Đảm Nhiệm:</span>
                      <span className="text-cyan-400 lowercase font-normal italic">phân vai động theo ngữ cảnh</span>
                    </div>
                    {c.seats.map(seat => (
                      <div
                        key={seat.id}
                        className="p-2.5 rounded-xl bg-slate-900 border border-slate-800/80 space-y-1.5 text-xs"
                      >
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-2">
                            <span className="font-semibold text-slate-200">{seat.seatTitle}</span>
                            <span className="text-[10px] text-slate-500">
                              ({seat.role === 1 ? 'Chủ tịch' : seat.role === 2 ? 'Phản biện đối kháng' : seat.role === 3 ? 'Kiểm chứng' : 'Siêu lý thuyết'})
                            </span>
                          </div>
                          <div className="flex items-center gap-1.5">
                            {seat.assignedModelCode && (
                              <span className="text-[10px] font-mono text-emerald-300 px-2 py-0.5 rounded bg-emerald-950/60 border border-emerald-800/40">
                                Động: {seat.assignedModelCode}
                              </span>
                            )}
                            <span className="text-[10px] font-mono text-cyan-400 px-2 py-0.5 rounded bg-cyan-950/60 border border-cyan-800/40">
                              Gốc: {seat.preferredModelCode}
                            </span>
                          </div>
                        </div>

                        {/* Rationale Phân vai theo thời - không */}
                        {seat.assignmentRationale && (
                          <div className="text-[11px] text-slate-400 leading-relaxed pl-2 border-l-2 border-cyan-500/40 italic">
                            💡 {seat.assignmentRationale}
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            );
          })}
        </div>

        {/* Footer */}
        <div className="p-4 border-t border-slate-800 bg-slate-950/60 flex items-center justify-between">
          <div className="text-xs text-slate-400">
            {selectedCouncilCodes.length === 0 ? (
              <span className="text-cyan-400 font-medium">✨ Tầng 0 sẽ tự chọn Viện phù hợp nhất</span>
            ) : (
              <span>Đã chọn <strong className="text-cyan-300">{selectedCouncilCodes.length}</strong> Viện</span>
            )}
          </div>
          <button
            onClick={onClose}
            className="px-5 py-2 rounded-xl bg-cyan-500 hover:bg-cyan-400 text-slate-950 text-xs font-bold transition cursor-pointer"
          >
            Áp Dụng & Đóng
          </button>
        </div>
      </div>

      {/* Modal Chi Tiết Bản Chất Viện Hàn Lâm (Double-click Activated) */}
      <CouncilDetailModal
        council={inspectingCouncil}
        isOpen={!!inspectingCouncil}
        onClose={() => setInspectingCouncil(null)}
        isSelected={inspectingCouncil ? selectedCouncilCodes.includes(inspectingCouncil.councilCode) : false}
        onToggleSelect={onToggleCouncil}
      />
    </div>
  );
}
