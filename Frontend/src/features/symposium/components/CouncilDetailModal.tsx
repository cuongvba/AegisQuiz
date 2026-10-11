import React, { useEffect } from 'react';
import {
  X, Award, Compass, BookOpen, UserCheck,
  CheckCircle2, Sparkles, Brain, Info
} from 'lucide-react';
import {
  UniversalAiCouncilDto,
  CognitiveArchetype,
  ARCHETYPE_LABELS
} from '@/services/ai-swarm.service';

interface CouncilDetailModalProps {
  council: UniversalAiCouncilDto | null;
  isOpen: boolean;
  onClose: () => void;
  isSelected: boolean;
  onToggleSelect: (code: string) => void;
}

const ROLE_LABELS: Record<number, { title: string; badge: string; desc: string }> = {
  1: {
    title: 'Viện Trưởng / Chủ Tịch Đoàn',
    badge: 'bg-indigo-950/80 border-indigo-500/40 text-indigo-300',
    desc: 'Định hình tiên đề, dẫn dắt nghị trình và mở đầu vấn đáp nhận thức.'
  },
  2: {
    title: 'Biện Sĩ Đối Kháng (Devil\'s Advocate)',
    badge: 'bg-rose-950/80 border-rose-500/40 text-rose-300',
    desc: 'Tấn công vào các giả định ngây thơ, bóc trần ngụy biện và bảo vệ nguyên tắc phản nghiệm (Popper).'
  },
  3: {
    title: 'Viện Sĩ Thực Nghiệm & Đo Kiểm',
    badge: 'bg-emerald-950/80 border-emerald-500/40 text-emerald-300',
    desc: 'Đối chiếu số liệu thực chứng [E#], kiểm tra tính khả thi và dữ liệu định lượng.'
  },
  4: {
    title: 'Thư Ký Tổng Hợp Siêu Lý Thuyết',
    badge: 'bg-cyan-950/80 border-cyan-500/40 text-cyan-300',
    desc: 'Liên kết liên ngành, dung hợp nghịch lý và kiến tạo mô hình nhận thức mới.'
  }
};

export function CouncilDetailModal({
  council,
  isOpen,
  onClose,
  isSelected,
  onToggleSelect
}: CouncilDetailModalProps) {
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    if (isOpen) {
      window.addEventListener('keydown', handleKeyDown);
    }
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen || !council) return null;

  const seats = council.seats || [];
  const readySeats = seats.filter(s => s.isReady !== false).length;

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center p-3 sm:p-6 bg-black/80 backdrop-blur-md animate-in fade-in duration-200">
      <div
        className="w-full max-w-4xl max-h-[90vh] bg-slate-950 border border-cyan-500/30 rounded-3xl shadow-2xl flex flex-col overflow-hidden animate-in zoom-in-95 duration-200"
        onClick={(e) => e.stopPropagation()}
      >
        {/* HEADER TRANG TRỌNG */}
        <div className="relative p-6 sm:p-7 border-b border-slate-800 bg-gradient-to-r from-slate-900 via-cyan-950/30 to-slate-900 flex items-start justify-between gap-4">
          <div className="space-y-2 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <span className="text-xs font-mono font-bold px-2.5 py-1 rounded-lg bg-cyan-950/80 text-cyan-400 border border-cyan-500/30 tracking-wider">
                {council.councilCode}
              </span>
              <span className="text-xs font-bold text-amber-400 flex items-center gap-1 px-2.5 py-1 rounded-lg bg-amber-950/60 border border-amber-500/30">
                <Award size={13} /> ELO {council.benchmarkPrestigeElo}
              </span>
              <span className={`text-xs font-semibold px-2.5 py-1 rounded-lg border flex items-center gap-1.5 ${
                council.readinessStatus === 'OFFLINE'
                  ? 'bg-rose-950/70 border-rose-500/40 text-rose-300'
                  : council.readinessStatus === 'DEGRADED'
                  ? 'bg-amber-950/70 border-amber-500/40 text-amber-300'
                  : 'bg-emerald-950/70 border-emerald-500/40 text-emerald-300'
              }`}>
                <span className={`w-2 h-2 rounded-full ${
                  council.readinessStatus === 'OFFLINE' ? 'bg-rose-400 animate-pulse' : council.readinessStatus === 'DEGRADED' ? 'bg-amber-400' : 'bg-emerald-400'
                }`} />
                {council.readinessStatus === 'OFFLINE' ? '🔴 Chưa cấu hình Quorum' : council.readinessStatus === 'DEGRADED' ? '🟡 Chế độ dự phòng' : '🟢 Sẵn sàng 100%'}
              </span>
              {council.materializedTaxonomyPath && (
                <span className="text-[11px] font-mono text-slate-400 px-2 py-0.5 rounded bg-slate-900 border border-slate-800 hidden md:inline-block">
                  Phả hệ: {council.materializedTaxonomyPath}
                </span>
              )}
            </div>

            <h2 className="text-xl sm:text-2xl font-black text-white tracking-tight flex items-center gap-2.5">
              <span>{council.displayName}</span>
            </h2>

            <div className="flex flex-wrap items-center gap-4 text-xs text-slate-300">
              <span className="flex items-center gap-1 text-cyan-400 font-medium">
                <BookOpen size={14} /> Chuyên môn: <strong className="text-white">{council.disciplineArea}</strong>
              </span>
              {council.spatioTemporalAffinity && (
                <span className="flex items-center gap-1 text-violet-300 font-medium">
                  <Compass size={14} className="text-violet-400" /> Trường phái: <strong className="text-white">{council.spatioTemporalAffinity}</strong>
                </span>
              )}
            </div>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => onToggleSelect(council.councilCode)}
              className={`px-4 py-2 rounded-xl text-xs font-bold transition flex items-center gap-1.5 cursor-pointer shadow-lg ${
                isSelected
                  ? 'bg-rose-600/90 hover:bg-rose-600 text-white border border-rose-500/50'
                  : 'bg-cyan-500 hover:bg-cyan-400 text-slate-950 font-black'
              }`}
            >
              <CheckCircle2 size={14} />
              <span>{isSelected ? 'Bỏ Tham Gia' : 'Chỉ Định Tham Gia'}</span>
            </button>
            <button
              onClick={onClose}
              className="p-2 rounded-xl text-slate-400 hover:text-white hover:bg-slate-800 transition cursor-pointer"
              title="Đóng (Esc)"
            >
              <X size={20} />
            </button>
          </div>
        </div>

        {/* BODY CUỘN NỘI DUNG */}
        <div className="flex-1 overflow-y-auto p-6 sm:p-8 space-y-6">
          {/* 1. KHỐI BẢN CHẤT NHẬN THỨC & TRIẾT LÝ NỀN TẢNG */}
          <div className="p-5 rounded-2xl bg-gradient-to-br from-slate-900 to-slate-950 border border-slate-800/80 shadow-lg space-y-3">
            <div className="flex items-center gap-2 text-xs font-black text-cyan-400 uppercase tracking-widest">
              <Brain size={15} />
              <span>Bản Chất Nhận Thức & Tuyên Ngôn Triết Lý</span>
            </div>

            <blockquote className="p-4 rounded-xl bg-slate-950/70 border-l-4 border-cyan-500 text-sm sm:text-base text-slate-200 italic font-serif leading-relaxed">
              "{council.epistemicManifesto}"
            </blockquote>

            <p className="text-xs text-slate-400 leading-relaxed">
              Khi được triệu tập vào Đại Hội Thảo, <strong>{council.displayName}</strong> đóng vai trò trọng yếu trong việc tiếp cận bài toán dưới lăng kính <strong>{council.disciplineArea}</strong>, áp dụng hệ hình lý thuyết của trường phái <em>{council.spatioTemporalAffinity || 'chuẩn tắc'}</em> để xây dựng luận đề và thẩm định tính chân lý của các giả thuyết khoa học.
            </p>
          </div>

          {/* 2. CẤU TRÚC ĐOÀN VIỆN SĨ & PHÂN VAI ĐỘNG HỌC */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 text-xs font-black text-violet-400 uppercase tracking-widest">
                <UserCheck size={15} />
                <span>Cấu Trúc Ghế Viện Sĩ & Mô Hình Đảm Nhiệm ({seats.length} Ghế)</span>
              </div>
              <span className="text-xs text-slate-400">
                {readySeats} / {seats.length} ghế đã được nạp năng lực tính toán
              </span>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5">
              {seats.map((seat) => {
                const roleMeta = ROLE_LABELS[seat.role] || {
                  title: 'Viện Sĩ Thành Viên',
                  badge: 'bg-slate-800 text-slate-300 border-slate-700',
                  desc: 'Tham gia đóng góp luận điểm và phân tích chuyên môn sâu.'
                };
                const archetypeMeta = ARCHETYPE_LABELS[seat.idealArchetype as CognitiveArchetype];

                return (
                  <div
                    key={seat.id}
                    className="p-4 rounded-2xl bg-slate-900/80 border border-slate-800 hover:border-slate-700 transition space-y-3 flex flex-col justify-between"
                  >
                    <div className="space-y-2">
                      <div className="flex items-start justify-between gap-2">
                        <div>
                          <h4 className="text-sm font-bold text-white flex items-center gap-2">
                            <span>{seat.seatTitle}</span>
                          </h4>
                          <span className={`inline-block text-[10px] font-semibold px-2 py-0.5 rounded-full border mt-1 ${roleMeta.badge}`}>
                            {roleMeta.title}
                          </span>
                        </div>
                        <span className="text-[10px] font-mono text-slate-400 bg-slate-800/80 px-2 py-0.5 rounded border border-slate-700">
                          Trọng số: {seat.decisionWeight}x
                        </span>
                      </div>

                      <p className="text-[11px] text-slate-400 leading-normal">
                        {roleMeta.desc}
                      </p>

                      {/* Thông tin mô hình AI đảm nhiệm */}
                      <div className="pt-2 border-t border-slate-800/60 space-y-1.5 text-xs">
                        <div className="flex items-center justify-between text-[11px]">
                          <span className="text-slate-400">Mô hình phân vai động:</span>
                          <span className="font-mono font-bold text-emerald-400 bg-emerald-950/60 px-2 py-0.5 rounded border border-emerald-800/40">
                            {seat.assignedModelCode || seat.preferredModelCode}
                          </span>
                        </div>

                        {archetypeMeta && (
                          <div className="flex items-center justify-between text-[11px]">
                            <span className="text-slate-400">Archetype tư duy:</span>
                            <span className="text-slate-300 flex items-center gap-1">
                              <Sparkles size={11} className="text-cyan-400" />
                              {archetypeMeta.name}
                            </span>
                          </div>
                        )}

                        {seat.assignmentRationale && (
                          <div className="text-[11px] text-slate-400 italic bg-slate-950/60 p-2 rounded-xl border border-slate-800/40 leading-relaxed">
                            💡 <strong>Lý giải phân vai:</strong> {seat.assignmentRationale}
                          </div>
                        )}

                        {seat.personaPromptDirective && (
                          <details className="text-[11px] text-slate-400 group cursor-pointer pt-1">
                            <summary className="hover:text-cyan-400 font-semibold flex items-center gap-1 select-none">
                              <Info size={11} /> Chỉ thị Nhân cách Học thuật (Persona Directive)
                            </summary>
                            <div className="mt-2 p-2.5 rounded-xl bg-slate-950 border border-slate-800 font-mono text-[10px] text-slate-300 whitespace-pre-wrap leading-relaxed">
                              {seat.personaPromptDirective}
                            </div>
                          </details>
                        )}
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        {/* FOOTER */}
        <div className="p-4 sm:p-5 border-t border-slate-800 bg-slate-950 flex items-center justify-between">
          <div className="text-xs text-slate-400 flex items-center gap-1.5">
            <Info size={14} className="text-cyan-400" />
            <span>Kích đúp vào bất kỳ Viện nào trên danh sách để mở bản hồ sơ chi tiết này.</span>
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={() => onToggleSelect(council.councilCode)}
              className={`px-5 py-2 rounded-xl text-xs font-bold transition cursor-pointer ${
                isSelected
                  ? 'bg-rose-950/80 hover:bg-rose-900 text-rose-300 border border-rose-700/50'
                  : 'bg-cyan-500 hover:bg-cyan-400 text-slate-950'
              }`}
            >
              {isSelected ? 'Bỏ Chọn Viện Này' : 'Chỉ Định Viện Này'}
            </button>
            <button
              onClick={onClose}
              className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold transition cursor-pointer"
            >
              Đóng
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
