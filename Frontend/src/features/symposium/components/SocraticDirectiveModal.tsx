import React, { useState } from 'react';
import {
  X,
  Compass,
  FileText,
  Link2,
  Sparkles,
  ArrowRight,
  ShieldAlert,
  Award,
  BookOpen
} from 'lucide-react';
import { SymposiumTeleologyMode } from '@/services/ai-swarm.service';

interface SocraticDirectiveModalProps {
  isOpen: boolean;
  onClose: () => void;
  currentRound: number;
  previousResolutionSummary: string;
  currentTeleologyMode: SymposiumTeleologyMode;
  onSubmit: (params: {
    socraticDirectives: string;
    additionalDocuments: string;
    additionalUris: string[];
    teleologyMode: SymposiumTeleologyMode;
    isFinalRound: boolean;
  }) => void;
  isConvening: boolean;
}

export function SocraticDirectiveModal({
  isOpen,
  onClose,
  currentRound,
  previousResolutionSummary,
  currentTeleologyMode,
  onSubmit,
  isConvening
}: SocraticDirectiveModalProps) {
  const [socraticDirectives, setSocraticDirectives] = useState('');
  const [additionalDocuments, setAdditionalDocuments] = useState('');
  const [rawUris, setRawUris] = useState('');
  const [teleologyMode, setTeleologyMode] = useState<SymposiumTeleologyMode>(currentTeleologyMode);
  const [isFinalRound, setIsFinalRound] = useState(false);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const uris = rawUris
      .split('\n')
      .map(u => u.trim())
      .filter(u => u.length > 0);

    onSubmit({
      socraticDirectives,
      additionalDocuments,
      additionalUris: uris,
      teleologyMode,
      isFinalRound
    });
  };

  const nextRound = currentRound + 1;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-md animate-in fade-in duration-200">
      <div className="relative w-full max-w-3xl bg-slate-900 border border-cyan-500/30 rounded-3xl shadow-2xl shadow-cyan-950/50 overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="px-6 py-4 bg-gradient-to-r from-slate-900 via-indigo-950/60 to-slate-900 border-b border-slate-800 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-cyan-500/10 border border-cyan-500/30 flex items-center justify-center text-cyan-400">
              <Compass size={22} className="animate-spin-slow" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-base sm:text-lg font-black text-slate-100 uppercase tracking-wider">
                  Chỉ Đạo Biện Chứng Của Chủ Tọa
                </h3>
                <span className="px-2.5 py-0.5 rounded-full text-[10px] font-mono font-bold bg-cyan-500/20 text-cyan-300 border border-cyan-500/40">
                  Chuẩn bị Vòng {nextRound}
                </span>
              </div>
              <p className="text-xs text-slate-400">
                Nạp ý kiến chỉ đạo, văn kiện hoặc tài liệu trích dẫn để định hướng phiên làm việc tiếp theo
              </p>
            </div>
          </div>

          <button
            onClick={onClose}
            disabled={isConvening}
            className="p-2 rounded-xl text-slate-400 hover:text-slate-100 hover:bg-slate-800 transition cursor-pointer"
          >
            <X size={18} />
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="flex-1 overflow-y-auto p-6 space-y-5 scrollbar-thin scrollbar-thumb-slate-800">
          {/* Tóm tắt nghị quyết vòng trước */}
          <div className="p-3.5 rounded-2xl bg-slate-950/80 border border-slate-800 space-y-1.5">
            <div className="flex items-center justify-between text-xs font-bold text-slate-400">
              <span className="flex items-center gap-1.5 text-cyan-400">
                <BookOpen size={14} /> Tiền Đề Nghị Quyết Vòng {currentRound}
              </span>
              <span className="text-[11px] text-slate-500 font-mono">Đầu vào cho Vòng {nextRound}</span>
            </div>
            <p className="text-xs text-slate-300 line-clamp-3 italic leading-relaxed">
              "{previousResolutionSummary || 'Chưa có tóm tắt nghị quyết vòng trước.'}"
            </p>
          </div>

          {/* 1. Ý kiến chỉ đạo của Chủ tọa */}
          <div className="space-y-1.5">
            <label className="text-xs font-bold text-slate-200 flex items-center justify-between">
              <span className="flex items-center gap-1.5 text-indigo-400">
                <Sparkles size={14} />
                <span>Ý Kiến Chỉ Đạo & Câu Hỏi Chất Vấn Của Chủ Tọa (Socratic Directives)</span>
              </span>
              <span className="text-[10px] text-indigo-300 font-medium bg-indigo-950/60 px-2 py-0.5 rounded border border-indigo-800/40">
                Tối Quan Trọng
              </span>
            </label>
            <textarea
              value={socraticDirectives}
              onChange={e => setSocraticDirectives(e.target.value)}
              rows={3}
              placeholder="Ví dụ: Đề nghị Viện Trưởng làm rõ cơ chế bảo đảm khi mạng lưới bị tấn công lượng tử; Yêu cầu tập trung vào bài toán bồi thường người dùng..."
              className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950/90 border border-slate-800 focus:border-cyan-500 focus:ring-1 focus:ring-cyan-500 text-xs text-slate-100 placeholder:text-slate-600 outline-none transition resize-none"
            />
          </div>

          {/* 2. Văn kiện / Tư liệu bổ sung */}
          <div className="space-y-1.5">
            <label className="text-xs font-bold text-slate-200 flex items-center gap-1.5">
              <FileText size={14} className="text-emerald-400" />
              <span>Văn Kiện, Tư Liệu Hoặc Số Liệu Bổ Sung (Dán nội dung)</span>
            </label>
            <textarea
              value={additionalDocuments}
              onChange={e => setAdditionalDocuments(e.target.value)}
              rows={3}
              placeholder="Dán nội dung tài liệu trích lục, báo cáo mật, điều khoản thông tư, hoặc số liệu thống kê mới nhất để các Viện tiếp nhận mã [E#]..."
              className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950/90 border border-slate-800 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 text-xs text-slate-100 placeholder:text-slate-600 outline-none transition resize-none"
            />
          </div>

          {/* 3. Liên kết URI / DOI tham chiếu */}
          <div className="space-y-1.5">
            <label className="text-xs font-bold text-slate-200 flex items-center gap-1.5">
              <Link2 size={14} className="text-cyan-400" />
              <span>Danh Sách URI / DOI / Nguồn Web Trực Tiếp (Mỗi liên kết một dòng)</span>
            </label>
            <textarea
              value={rawUris}
              onChange={e => setRawUris(e.target.value)}
              rows={2}
              placeholder={"https://doi.org/10.1145/...\nhttps://sbv.gov.vn/thong-tu-50..."}
              className="w-full px-3.5 py-2 rounded-xl bg-slate-950/90 border border-slate-800 focus:border-cyan-500 focus:ring-1 focus:ring-cyan-500 text-xs font-mono text-cyan-300 placeholder:text-slate-600 outline-none transition resize-none"
            />
          </div>

          {/* Tùy chọn vòng chung cuộc */}
          <div className="pt-2 border-t border-slate-800/80 flex items-center justify-between">
            <div className="flex items-center gap-2.5">
              <input
                type="checkbox"
                id="isFinalRoundCheck"
                checked={isFinalRound}
                onChange={e => setIsFinalRound(e.target.checked)}
                className="w-4 h-4 rounded border-slate-700 bg-slate-950 text-amber-500 focus:ring-amber-500 cursor-pointer"
              />
              <label htmlFor="isFinalRoundCheck" className="text-xs text-slate-300 cursor-pointer">
                Tuyên bố đây là <strong className="text-amber-400 font-bold">Vòng Chung Cuộc</strong> (kết thúc và ban hành Nghị quyết Tối hậu)
              </label>
            </div>

            {isFinalRound && (
              <span className="flex items-center gap-1 text-[11px] text-amber-400 bg-amber-950/60 px-2.5 py-1 rounded-lg border border-amber-500/30">
                <Award size={13} />
                <span>Nghị quyết Chung cuộc Cấp 1</span>
              </span>
            )}
          </div>

          {/* Footer Actions */}
          <div className="pt-4 border-t border-slate-800 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              disabled={isConvening}
              className="px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 font-bold text-xs transition cursor-pointer"
            >
              Hủy
            </button>

            <button
              type="submit"
              disabled={isConvening}
              className={`px-5 py-2.5 rounded-xl font-black text-xs flex items-center gap-2 shadow-lg transition cursor-pointer ${
                isFinalRound
                  ? 'bg-amber-500 hover:bg-amber-400 text-slate-950 shadow-amber-500/20'
                  : 'bg-cyan-500 hover:bg-cyan-400 text-slate-950 shadow-cyan-500/20'
              }`}
            >
              {isConvening ? (
                <>
                  <Sparkles size={14} className="animate-spin" />
                  <span>Đang Triệu Tập...</span>
                </>
              ) : isFinalRound ? (
                <>
                  <Award size={14} />
                  <span>Triệu Tập Vòng Chung Cuộc</span>
                </>
              ) : (
                <>
                  <span>Triệu Tập Vòng {nextRound}</span>
                  <ArrowRight size={14} />
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
