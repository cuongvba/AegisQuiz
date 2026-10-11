import React, { useState, useEffect } from 'react';
import { X, ShieldAlert, Landmark, Save, BookOpen, Globe, Award, Sparkles } from 'lucide-react';
import { UniversalAiCouncilDto, CouncilCategory, aiSwarmService } from '@/services/ai-swarm.service';

interface EditCouncilModalProps {
  council: UniversalAiCouncilDto | null;
  isOpen: boolean;
  onClose: () => void;
  onSaved: (updatedCouncil: UniversalAiCouncilDto) => void;
}

export const EditCouncilModal: React.FC<EditCouncilModalProps> = ({
  council,
  isOpen,
  onClose,
  onSaved,
}) => {
  const [displayName, setDisplayName] = useState('');
  const [category, setCategory] = useState<CouncilCategory>(CouncilCategory.Academic);
  const [disciplineArea, setDisciplineArea] = useState('');
  const [spatioTemporalAffinity, setSpatioTemporalAffinity] = useState('');
  const [benchmarkPrestigeElo, setBenchmarkPrestigeElo] = useState(2400);
  const [epistemicManifesto, setEpistemicManifesto] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (council) {
      setDisplayName(council.displayName || '');
      setCategory(council.category || CouncilCategory.Academic);
      setDisciplineArea(council.disciplineArea || '');
      setSpatioTemporalAffinity(council.spatioTemporalAffinity || '');
      setBenchmarkPrestigeElo(council.benchmarkPrestigeElo || 2400);
      setEpistemicManifesto(council.epistemicManifesto || '');
      setError(null);
    }
  }, [council]);

  if (!isOpen || !council) return null;

  const handleSave = async () => {
    if (!displayName.trim()) {
      setError('Vui lòng nhập Tên Viện Hàn Lâm.');
      return;
    }
    if (!epistemicManifesto.trim()) {
      setError('Vui lòng nhập Tuyên Ngôn Nhận Thức Luận (System Prompt cấp Viện).');
      return;
    }

    try {
      setSaving(true);
      setError(null);

      const payload: Partial<UniversalAiCouncilDto> = {
        ...council,
        displayName: displayName.trim(),
        category,
        disciplineArea: disciplineArea.trim(),
        spatioTemporalAffinity: spatioTemporalAffinity.trim(),
        benchmarkPrestigeElo: Number(benchmarkPrestigeElo),
        epistemicManifesto: epistemicManifesto.trim(),
      };

      const saved = await aiSwarmService.saveCouncil(payload);
      onSaved(saved);
      onClose();
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || 'Lỗi lưu thông tin Viện Hàn Lâm.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/85 backdrop-blur-md flex items-center justify-center p-4">
      <div className="bg-slate-900 border border-purple-500/40 rounded-3xl w-full max-w-2xl overflow-hidden shadow-2xl flex flex-col max-h-[90vh] animate-in fade-in zoom-in-95 duration-200">
        {/* Header */}
        <div className="p-6 bg-gradient-to-r from-purple-950/90 via-slate-900 to-indigo-950/70 border-b border-slate-800 flex items-center justify-between shrink-0">
          <div className="flex items-center gap-3">
            <div className="p-3 rounded-2xl bg-purple-500/20 text-purple-400 border border-purple-500/30">
              <Landmark className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-[10px] font-black uppercase tracking-wider text-purple-400">Epistemic Council Architecture</span>
                <span className="px-2 py-0.5 rounded-full bg-purple-500/20 text-purple-300 text-[10px] font-mono border border-purple-500/30">
                  {council.councilCode}
                </span>
              </div>
              <h3 className="text-base font-black text-white mt-0.5">Hiệu Chỉnh Viện & Tuyên Ngôn Nhận Thức</h3>
            </div>
          </div>
          <button onClick={onClose} className="p-2 text-slate-400 hover:text-white rounded-xl hover:bg-slate-800/80 transition-colors">
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 space-y-5 text-xs overflow-y-auto">
          {error && (
            <div className="p-3.5 rounded-2xl bg-rose-950/60 border border-rose-500/40 text-rose-300 flex items-center gap-2">
              <ShieldAlert className="w-4 h-4 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          {/* 1. Tên Viện & Danh Mục */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div className="space-y-1.5 md:col-span-2">
              <label className="text-slate-300 font-bold flex items-center gap-1.5">
                <Landmark className="w-3.5 h-3.5 text-purple-400" />
                Tên Viện Hàn Lâm / Hội Đồng:
              </label>
              <input
                type="text"
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                placeholder="VD: Viện Hàn Lâm Triết Học & Nhận Thức Luận..."
                className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-purple-500 transition-colors"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-slate-300 font-bold block">Phân Loại Viện:</label>
              <select
                value={category}
                onChange={(e) => setCategory(Number(e.target.value) as CouncilCategory)}
                className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-purple-500 transition-colors"
              >
                <option value={CouncilCategory.Academic}>Học Thuật (Academic)</option>
                <option value={CouncilCategory.Secretariat}>Ban Thư Ký (Secretariat)</option>
                <option value={CouncilCategory.Arbitration}>Tổ Trọng Tài (Arbitration)</option>
                <option value={CouncilCategory.SystemCore}>Hệ Thống Lõi (SystemCore)</option>
              </select>
            </div>
          </div>

          {/* 2. Lĩnh Vực & Trường Phái & ELO */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div className="space-y-1.5">
              <label className="text-slate-300 font-bold flex items-center gap-1.5">
                <BookOpen className="w-3.5 h-3.5 text-indigo-400" />
                Lĩnh Vực Ngành:
              </label>
              <input
                type="text"
                value={disciplineArea}
                onChange={(e) => setDisciplineArea(e.target.value)}
                placeholder="VD: Triết học, Toán học, Kinh tế..."
                className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-indigo-500 transition-colors"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-slate-300 font-bold flex items-center gap-1.5">
                <Globe className="w-3.5 h-3.5 text-teal-400" />
                Trường Phái Không - Thời Gian:
              </label>
              <input
                type="text"
                value={spatioTemporalAffinity}
                onChange={(e) => setSpatioTemporalAffinity(e.target.value)}
                placeholder="VD: Tây phương cổ điển & Hiện đại..."
                className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-teal-500 transition-colors"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-slate-300 font-bold flex items-center gap-1.5">
                <Award className="w-3.5 h-3.5 text-amber-400" />
                Điểm Uy Tín Học Thuật (ELO):
              </label>
              <input
                type="number"
                value={benchmarkPrestigeElo}
                onChange={(e) => setBenchmarkPrestigeElo(parseInt(e.target.value) || 2000)}
                className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-amber-500 transition-colors"
              />
            </div>
          </div>

          {/* 3. Tuyên Ngôn Nhận Thức Luận (System Prompt Cấp Viện) */}
          <div className="space-y-1.5 pt-2 border-t border-slate-800">
            <div className="flex items-center justify-between">
              <label className="text-slate-300 font-bold flex items-center gap-1.5">
                <Sparkles className="w-3.5 h-3.5 text-purple-400" />
                Tuyên Ngôn Nhận Thức Luận (Epistemic Manifesto — System Prompt Cấp Viện):
              </label>
              <span className="text-[10px] text-purple-400 font-mono font-bold">Hierarchical Epistemic Layer 1</span>
            </div>
            <textarea
              rows={6}
              value={epistemicManifesto}
              onChange={(e) => setEpistemicManifesto(e.target.value)}
              placeholder="Nhập tuyên ngôn nhận thức và phương pháp luận bất biến của toàn Viện (Ví dụ: Mọi mệnh đề phải tuân thủ chặt chẽ phép biện chứng duy vật, phủ định của phủ định...)"
              className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 font-mono text-xs focus:outline-none focus:border-purple-500 transition-colors leading-relaxed"
            />
            <p className="text-[11px] text-slate-400 italic">
              * Tuyên ngôn này là kim chỉ nam tối cao truyền xuống tất cả các Ghế trong Viện khi tham gia vào 6 pha của Đại Hội Thảo.
            </p>
          </div>
        </div>

        {/* Footer */}
        <div className="p-5 bg-slate-950 border-t border-slate-800 flex items-center justify-end gap-3 shrink-0">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2.5 rounded-xl border border-slate-800 text-slate-300 hover:bg-slate-800/80 font-semibold transition-colors"
          >
            Hủy Bỏ
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={saving}
            className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-purple-600 via-indigo-600 to-pink-600 hover:from-purple-500 hover:to-pink-500 text-white font-bold transition-all shadow-lg shadow-purple-600/30 disabled:opacity-50"
          >
            <Save className="w-4 h-4" />
            {saving ? 'Đang Lưu Viện...' : 'Lưu Tuyên Ngôn & Viện'}
          </button>
        </div>
      </div>
    </div>
  );
};
