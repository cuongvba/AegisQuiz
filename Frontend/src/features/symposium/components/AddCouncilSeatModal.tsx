import React, { useState } from 'react';
import { X, ShieldAlert, Cpu, Layers, PlusCircle, CheckCircle2, UserCheck, Scale, Terminal } from 'lucide-react';
import { UniversalAiCouncilDto, UniversalCouncilSeatDto, AiModelRegistryDto, CognitiveArchetype, aiSwarmService } from '@/services/ai-swarm.service';

interface AddCouncilSeatModalProps {
  council: UniversalAiCouncilDto | null;
  isOpen: boolean;
  onClose: () => void;
  availableModels: AiModelRegistryDto[];
  onAdded: (newSeat: UniversalCouncilSeatDto) => void;
}

export const AddCouncilSeatModal: React.FC<AddCouncilSeatModalProps> = ({
  council,
  isOpen,
  onClose,
  availableModels,
  onAdded,
}) => {
  const [seatTitle, setSeatTitle] = useState('');
  const [role, setRole] = useState(2); // Mặc định Critic
  const [idealArchetype, setIdealArchetype] = useState<CognitiveArchetype>(CognitiveArchetype.LinguisticArbiter);
  const [decisionWeight, setDecisionWeight] = useState(1.0);
  const [personaPromptDirective, setPersonaPromptDirective] = useState('');
  const [alphaModel, setAlphaModel] = useState(availableModels[0]?.modelCode || '');
  const [swarmMembers, setSwarmMembers] = useState<string[]>([]);
  const [policy, setPolicy] = useState('CASCADE_FAILOVER');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!isOpen || !council) return null;

  const toggleSwarmMember = (modelCode: string) => {
    if (swarmMembers.includes(modelCode)) {
      setSwarmMembers(swarmMembers.filter(m => m !== modelCode));
    } else {
      setSwarmMembers([...swarmMembers, modelCode]);
    }
  };

  const handleSubmit = async () => {
    if (!seatTitle.trim()) {
      setError('Vui lòng nhập Chức Danh Ghế / Viện sĩ.');
      return;
    }
    const currentAlpha = alphaModel || availableModels[0]?.modelCode || '';
    if (!currentAlpha) {
      setError('Vui lòng chọn Sói Đầu Đàn (Alpha Model).');
      return;
    }

    try {
      setSaving(true);
      setError(null);

      const membersToSave = [currentAlpha, ...swarmMembers.filter(m => m !== currentAlpha)];
      const newSeatPayload: Partial<UniversalCouncilSeatDto> = {
        councilId: council.id,
        seatTitle: seatTitle.trim(),
        role: Number(role),
        idealArchetype,
        decisionWeight: Number(decisionWeight),
        personaPromptDirective: personaPromptDirective.trim(),
        alphaModelCode: currentAlpha,
        preferredModelCode: currentAlpha,
        assignedModelCode: currentAlpha,
        assignmentRationale: `Khởi tạo ghế mới trong Viện ${council.displayName}`,
        swarmModelCodesJson: JSON.stringify(membersToSave),
        swarmPolicy: policy,
        isReady: true,
      };

      const created = await aiSwarmService.addCouncilSeat(council.id, newSeatPayload);
      onAdded(created);
      onClose();
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || 'Lỗi thêm Ghế mới vào Viện.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/85 backdrop-blur-md flex items-center justify-center p-4">
      <div className="bg-slate-900 border border-emerald-500/40 rounded-3xl w-full max-w-2xl overflow-hidden shadow-2xl flex flex-col max-h-[90vh] animate-in fade-in zoom-in-95 duration-200">
        {/* Header */}
        <div className="p-6 bg-gradient-to-r from-emerald-950/90 via-slate-900 to-indigo-950/70 border-b border-slate-800 flex items-center justify-between shrink-0">
          <div className="flex items-center gap-3">
            <div className="p-3 rounded-2xl bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
              <PlusCircle className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-[10px] font-black uppercase tracking-wider text-emerald-400">Thêm Ghế Viện Sĩ Mới</span>
                <span className="px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 text-[10px] font-mono border border-emerald-500/30">
                  {council.displayName}
                </span>
              </div>
              <h3 className="text-base font-black text-white mt-0.5">Biên Chế Ghế Mới & Phân Bổ Bầy Sói</h3>
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

          {/* 1. Thông tin Ghế */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div className="space-y-1.5 md:col-span-2">
              <label className="text-slate-300 font-bold flex items-center gap-1.5">
                <UserCheck className="w-3.5 h-3.5 text-emerald-400" />
                Chức Danh Ghế / Viện sĩ:
              </label>
              <input
                type="text"
                value={seatTitle}
                onChange={(e) => setSeatTitle(e.target.value)}
                placeholder="VD: Viện Sĩ Giám Định Thực Nghiệm..."
                className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-emerald-500 transition-colors"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-slate-300 font-bold flex items-center gap-1.5">
                <Scale className="w-3.5 h-3.5 text-amber-400" />
                Trọng Số Biểu Quyết:
              </label>
              <input
                type="number"
                step="0.1"
                min="0.1"
                max="5.0"
                value={decisionWeight}
                onChange={(e) => setDecisionWeight(parseFloat(e.target.value) || 1.0)}
                className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-amber-500 transition-colors"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label className="text-slate-300 font-bold block">Vai Trò Phân Khảo:</label>
              <select
                value={role}
                onChange={(e) => setRole(Number(e.target.value))}
                className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-emerald-500 transition-colors"
              >
                <option value={1}>Chủ tịch Viện (President) — Định hướng & Kết luận</option>
                <option value={2}>Phản biện đối kháng (Critic) — Devil's Advocate vạch lỗi</option>
                <option value={3}>Thẩm tra thực nghiệm (Auditor) — Fact-checker kiểm chứng</option>
                <option value={4}>Kiến tạo giải pháp (Synthesizer) — Metatheorist dung hòa</option>
              </select>
            </div>

            <div className="space-y-1.5">
              <label className="text-slate-300 font-bold block">Nguyên Mẫu Nhận Thức (Archetype):</label>
              <select
                value={idealArchetype}
                onChange={(e) => setIdealArchetype(Number(e.target.value) as CognitiveArchetype)}
                className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-emerald-500 transition-colors"
              >
                <option value={CognitiveArchetype.DeepReasoner}>DeepReasoner — Tư duy suy luận chuỗi dài (CoT)</option>
                <option value={CognitiveArchetype.MassiveContext}>MassiveContext — Ngữ cảnh cực đại 1M-10M tokens</option>
                <option value={CognitiveArchetype.LinguisticArbiter}>LinguisticArbiter — Trọng tài sư phạm & Phản biện</option>
                <option value={CognitiveArchetype.UltraFastScanner}>UltraFastScanner — Phản ứng siêu tốc thời gian thực</option>
                <option value={CognitiveArchetype.SovereignLocal}>SovereignLocal — Bảo mật nội bộ On-Premise</option>
              </select>
            </div>
          </div>

          {/* 2. Persona Prompt Directive (System Prompt Của Ghế Này) */}
          <div className="space-y-1.5 pt-2 border-t border-slate-800">
            <div className="flex items-center justify-between">
              <label className="text-slate-300 font-bold flex items-center gap-1.5">
                <Terminal className="w-3.5 h-3.5 text-emerald-400" />
                Chỉ Thị Nhân Cách & Luận Thuyết Cốt Lõi (Seat Persona Prompt Directive):
              </label>
              <span className="text-[10px] text-emerald-400/80 font-mono">System Prompt Riêng Của Ghế</span>
            </div>
            <textarea
              rows={3}
              value={personaPromptDirective}
              onChange={(e) => setPersonaPromptDirective(e.target.value)}
              placeholder="Nhập System Prompt chuyên biệt cho ghế này (Ví dụ: Bạn là học giả phái hoài nghi luận triệt để, không chấp nhận bất kỳ mệnh đề nào thiếu bằng chứng lặp lại...)"
              className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 font-mono text-xs focus:outline-none focus:border-emerald-500 transition-colors leading-relaxed"
            />
          </div>

          {/* 3. Sói Đầu Đàn (Alpha Leader) */}
          <div className="space-y-1.5 pt-2 border-t border-slate-800">
            <label className="text-slate-300 font-bold flex items-center gap-1.5">
              <Cpu className="w-4 h-4 text-amber-400" />
              Sói Đầu Đàn (Alpha Model - Ưu tiên triệu gọi số 1):
            </label>
            <select
              value={alphaModel}
              onChange={(e) => setAlphaModel(e.target.value)}
              className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 font-mono text-xs focus:outline-none focus:border-amber-500 transition-colors"
            >
              {availableModels.map((m) => (
                <option key={m.modelCode} value={m.modelCode}>
                  {m.displayName} ({m.modelCode}) — ELO {m.benchmarkElo} [{m.providerCompany}]
                </option>
              ))}
            </select>
          </div>

          {/* 4. Các Sói Thành Viên Dự Phòng */}
          <div className="space-y-1.5">
            <div className="flex items-center justify-between">
              <label className="text-slate-300 font-bold flex items-center gap-1.5">
                <Layers className="w-4 h-4 text-indigo-400" />
                Bầy Sói Dự Phòng & Tiếp Bước (Cascade Failover Swarm):
              </label>
              <span className="text-[10px] text-indigo-400 font-mono font-bold">
                {swarmMembers.length} mô hình dự phòng
              </span>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-1 max-h-36 overflow-y-auto pr-1">
              {availableModels.map((m) => {
                const isSelected = swarmMembers.includes(m.modelCode);
                const isAlpha = m.modelCode === alphaModel;

                return (
                  <button
                    key={m.modelCode}
                    type="button"
                    onClick={() => toggleSwarmMember(m.modelCode)}
                    disabled={isAlpha}
                    className={`p-2 rounded-xl border text-left transition-all flex items-center justify-between gap-2 ${
                      isAlpha
                        ? 'bg-amber-950/20 border-amber-500/40 text-amber-300 opacity-60 cursor-not-allowed'
                        : isSelected
                        ? 'bg-emerald-950/60 border-emerald-500/60 text-emerald-200'
                        : 'bg-slate-950/60 border-slate-800 text-slate-400 hover:border-slate-700'
                    }`}
                  >
                    <div className="truncate">
                      <div className="font-semibold truncate text-[11px]">{m.displayName}</div>
                      <div className="text-[9px] font-mono text-slate-400">{m.modelCode}</div>
                    </div>
                    {isAlpha ? (
                      <span className="text-[9px] font-bold px-1.5 py-0.5 rounded bg-amber-500/20 text-amber-300 whitespace-nowrap">
                        Alpha
                      </span>
                    ) : isSelected ? (
                      <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
                    ) : null}
                  </button>
                );
              })}
            </div>
          </div>

          {/* 5. Chính Sách Vận Hành (Swarm Policy) */}
          <div className="space-y-1.5 pt-2 border-t border-slate-800">
            <label className="text-slate-300 font-bold block">Chính Sách Vận Hành Bầy Sói:</label>
            <div className="grid grid-cols-2 gap-3">
              <label
                className={`p-2.5 rounded-xl border cursor-pointer transition-all flex flex-col justify-between ${
                  policy === 'CASCADE_FAILOVER'
                    ? 'bg-emerald-950/40 border-emerald-500/60 text-emerald-200'
                    : 'bg-slate-950 border-slate-800 text-slate-400'
                }`}
              >
                <div className="flex items-center gap-2">
                  <input
                    type="radio"
                    name="newSwarmPolicy"
                    value="CASCADE_FAILOVER"
                    checked={policy === 'CASCADE_FAILOVER'}
                    onChange={() => setPolicy('CASCADE_FAILOVER')}
                    className="sr-only"
                  />
                  <span className="font-bold text-xs">Cascade Failover</span>
                </div>
                <p className="text-[10px] text-slate-400 mt-1 leading-snug">
                  Gọi Sói Alpha trước, nếu lỗi tự động nối tiếp sang các Sói sau trong 0ms.
                </p>
              </label>

              <label
                className={`p-2.5 rounded-xl border cursor-pointer transition-all flex flex-col justify-between ${
                  policy === 'SWARM_CONSENSUS'
                    ? 'bg-purple-950/40 border-purple-500/60 text-purple-200'
                    : 'bg-slate-950 border-slate-800 text-slate-400'
                }`}
              >
                <div className="flex items-center gap-2">
                  <input
                    type="radio"
                    name="newSwarmPolicy"
                    value="SWARM_CONSENSUS"
                    checked={policy === 'SWARM_CONSENSUS'}
                    onChange={() => setPolicy('SWARM_CONSENSUS')}
                    className="sr-only"
                  />
                  <span className="font-bold text-xs">Swarm Consensus</span>
                </div>
                <p className="text-[10px] text-slate-400 mt-1 leading-snug">
                  Triệu gọi đồng thời toàn bầy để lấy đồng thuận ma trận đa mô hình.
                </p>
              </label>
            </div>
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
            onClick={handleSubmit}
            disabled={saving}
            className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-emerald-600 via-teal-600 to-indigo-600 hover:from-emerald-500 hover:to-indigo-500 text-white font-bold transition-all shadow-lg shadow-emerald-600/30 disabled:opacity-50"
          >
            <PlusCircle className="w-4 h-4" />
            {saving ? 'Đang Khởi Tạo...' : 'Thêm Ghế Vào Viện'}
          </button>
        </div>
      </div>
    </div>
  );
};
