import React, { useState, useEffect } from 'react';
import { X, ShieldAlert, Cpu, Layers, Save, CheckCircle2, UserCheck, Scale, Terminal, Key, ShieldCheck, Zap, Sparkles, ArrowRight } from 'lucide-react';
import { UniversalCouncilSeatDto, AiModelRegistryDto, aiSwarmService, UserAiKeyDto } from '@/services/ai-swarm.service';

interface SeatSwarmConfigModalProps {
  seat: UniversalCouncilSeatDto | null;
  councilName: string;
  isOpen: boolean;
  onClose: () => void;
  availableModels: AiModelRegistryDto[];
  userKeys?: UserAiKeyDto[];
  onSaved: (updatedSeat: UniversalCouncilSeatDto) => void;
}

export const SeatSwarmConfigModal: React.FC<SeatSwarmConfigModalProps> = ({
  seat,
  councilName,
  isOpen,
  onClose,
  availableModels,
  userKeys = [],
  onSaved,
}) => {
  const [seatTitle, setSeatTitle] = useState('');
  const [role, setRole] = useState(1);
  const [decisionWeight, setDecisionWeight] = useState(1.0);
  const [personaPromptDirective, setPersonaPromptDirective] = useState('');
  const [alphaModel, setAlphaModel] = useState('');
  const [swarmMembers, setSwarmMembers] = useState<string[]>([]);
  const [policy, setPolicy] = useState('CASCADE_FAILOVER');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (seat) {
      setSeatTitle(seat.seatTitle || '');
      setRole(seat.role || 1);
      setDecisionWeight(seat.decisionWeight ?? 1.0);
      setPersonaPromptDirective(seat.personaPromptDirective || '');
      setAlphaModel(seat.alphaModelCode || seat.assignedModelCode || seat.preferredModelCode || (availableModels[0]?.modelCode ?? ''));
      if (seat.swarmModelCodesJson) {
        try {
          const parsed = JSON.parse(seat.swarmModelCodesJson);
          if (Array.isArray(parsed)) {
            setSwarmMembers(parsed);
          } else {
            setSwarmMembers([]);
          }
        } catch {
          setSwarmMembers([]);
        }
      } else {
        setSwarmMembers([]);
      }
      setPolicy(seat.swarmPolicy || 'CASCADE_FAILOVER');
      setError(null);
    }
  }, [seat, availableModels]);

  if (!isOpen || !seat) return null;

  const toggleSwarmMember = (modelCode: string) => {
    if (swarmMembers.includes(modelCode)) {
      setSwarmMembers(swarmMembers.filter(m => m !== modelCode));
    } else {
      setSwarmMembers([...swarmMembers, modelCode]);
    }
  };

  const getModelStrength = (modelCode: string) => {
    const code = modelCode.toLowerCase();
    if (code.includes('deepseek-r1') || code.includes('reasoner') || code.includes('o1') || code.includes('o3'))
      return { tag: 'CoT Reasoning', desc: 'Suy luận chuỗi dài logic đa bước, FinOps tối ưu', color: 'bg-purple-500/20 text-purple-300 border-purple-500/30' };
    if (code.includes('gemini') || code.includes('flash') || code.includes('pro'))
      return { tag: 'Massive Context', desc: 'Ngữ cảnh 1M-2M tokens, nuốt trọn hồ sơ thực chứng', color: 'bg-sky-500/20 text-sky-300 border-sky-500/30' };
    if (code.includes('claude') || code.includes('sonnet') || code.includes('opus'))
      return { tag: 'Linguistic Rigor', desc: 'Phản biện đối kháng Devil Advocate, văn phong chuẩn mực', color: 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30' };
    if (code.includes('gpt-4o') || code.includes('gpt-4'))
      return { tag: 'Universal Synthesis', desc: 'Cân bằng đa nhiệm, trực giác nhạy bén, tốc độ cao', color: 'bg-amber-500/20 text-amber-300 border-amber-500/30' };
    if (code.includes('qwen'))
      return { tag: 'Cross-Cultural Logic', desc: 'Phân tích đa văn hóa, đối sánh Đông - Tây luận', color: 'bg-rose-500/20 text-rose-300 border-rose-500/30' };
    return { tag: 'Analytical Core', desc: 'Suy luận học thuật và thẩm định nhận thức', color: 'bg-indigo-500/20 text-indigo-300 border-indigo-500/30' };
  };

  const getKeyPoolStatus = (providerCompany: string) => {
    const hasUserKey = userKeys.some(k => k.isActive && k.providerCompany.toLowerCase() === providerCompany.toLowerCase());
    return {
      hasUserKey,
      hasSystemKey: true, // Mặc định hệ thống luôn có fallback pool
      label: hasUserKey ? 'Ưu tiên: User BYOK ➔ System' : 'System Default Pool'
    };
  };

  const handleSave = async () => {
    if (!seatTitle.trim()) {
      setError('Vui lòng nhập Chức Danh Ghế / Viện sĩ.');
      return;
    }
    if (!alphaModel.trim()) {
      setError('Vui lòng chọn Sói Đầu Đàn (Alpha Model).');
      return;
    }

    try {
      setSaving(true);
      setError(null);

      // Đảm bảo Alpha Model nằm đầu danh sách bầy sói
      const membersToSave = [alphaModel, ...swarmMembers.filter(m => m !== alphaModel)];
      const updated = await aiSwarmService.updateSeatSwarm(seat.id, {
        seatTitle: seatTitle.trim(),
        role: Number(role),
        decisionWeight: Number(decisionWeight),
        personaPromptDirective: personaPromptDirective.trim(),
        alphaModelCode: alphaModel,
        swarmModelCodesJson: JSON.stringify(membersToSave),
        swarmPolicy: policy,
      });

      onSaved(updated);
      onClose();
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || 'Lỗi lưu cấu hình Bầy Sói.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/85 backdrop-blur-md flex items-center justify-center p-4">
      <div className="bg-slate-900 border border-indigo-500/40 rounded-3xl w-full max-w-3xl overflow-hidden shadow-2xl flex flex-col max-h-[92vh] animate-in fade-in zoom-in-95 duration-200">
        
        {/* Header */}
        <div className="p-6 bg-gradient-to-r from-indigo-950/90 via-slate-900 to-purple-950/80 border-b border-slate-800 flex items-center justify-between shrink-0">
          <div className="flex items-center gap-3">
            <div className="p-3 rounded-2xl bg-amber-500/20 text-amber-400 border border-amber-500/30">
              <span className="text-2xl">🐺</span>
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-[10px] font-black uppercase tracking-wider text-amber-400">Wolfpack Swarm Architecture & Keys Pool Waterfall</span>
                <span className="px-2 py-0.5 rounded-full bg-indigo-500/20 text-indigo-300 text-[10px] font-mono border border-indigo-500/30">
                  {councilName}
                </span>
              </div>
              <h3 className="text-base font-black text-white mt-0.5">{seatTitle || seat.seatTitle}</h3>
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

          {/* Triết Lý Kiến Trúc Bầy Sói & Bể Khóa (Banner Trực Quan) */}
          <div className="p-4 rounded-2xl bg-gradient-to-r from-purple-950/60 via-slate-950/80 to-indigo-950/60 border border-purple-500/30 space-y-2">
            <div className="flex items-center gap-2 text-purple-300 font-bold text-xs">
              <Sparkles className="w-4 h-4 text-amber-400" />
              <span>Bản Chất Wolfpack Swarm: Chuỗi Sói Đa Mô Hình & Thác Lũ Ưu Tiên Bể Khóa</span>
            </div>
            <p className="text-[11px] text-slate-300 leading-relaxed">
              Mỗi con Sói trong bầy là một <strong className="text-white">Mô Hình AI gắn liền với Bể Khóa (Keys Pool)</strong> riêng của nhà cung cấp. 
              Khi thực thi, hệ thống luôn tuân thủ thác lũ ưu tiên:
            </p>
            <div className="flex items-center gap-2 text-[10px] font-mono flex-wrap pt-1">
              <span className="px-2 py-1 rounded-lg bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 font-bold flex items-center gap-1">
                <Key className="w-3 h-3" /> 1. User Keys (BYOK - 0$ Token)
              </span>
              <ArrowRight className="w-3 h-3 text-slate-500" />
              <span className="px-2 py-1 rounded-lg bg-sky-500/20 text-sky-300 border border-sky-500/40 font-bold">
                2. Community Keys Pool
              </span>
              <ArrowRight className="w-3 h-3 text-slate-500" />
              <span className="px-2 py-1 rounded-lg bg-purple-500/20 text-purple-300 border border-purple-500/40 font-bold flex items-center gap-1">
                <ShieldCheck className="w-3 h-3" /> 3. System Keys Pool
              </span>
            </div>
            <p className="text-[10px] text-slate-400 italic">
              * Nếu toàn bộ Keys Pool của Sói Alpha gặp sự cố (429, Quota, Timeout), hệ thống lập tức chuyển tiếp 0ms sang Sói tiếp theo kèm Keys Pool riêng của mô hình đó.
            </p>
          </div>

          {/* 1. Thông tin Ghế & Vai Trò Hội Đồng */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div className="space-y-1.5 md:col-span-2">
              <label className="text-slate-300 font-bold flex items-center gap-1.5">
                <UserCheck className="w-3.5 h-3.5 text-indigo-400" />
                Chức Danh Ghế / Viện sĩ:
              </label>
              <input
                type="text"
                value={seatTitle}
                onChange={(e) => setSeatTitle(e.target.value)}
                placeholder="VD: Chủ tịch Hội đồng, Biện hộ viên đối kháng..."
                className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 text-xs focus:outline-none focus:border-indigo-500 transition-colors"
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

          <div className="space-y-1.5">
            <label className="text-slate-300 font-bold block">Vai Trò Phân Khảo Trong Phiên Tranh Luận:</label>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
              {[
                { value: 1, label: 'Chủ tịch Viện (President)', desc: 'Tổng kết & Định hướng' },
                { value: 2, label: 'Phản biện (Critic)', desc: "Devil's Advocate vạch lỗi" },
                { value: 3, label: 'Thẩm tra (Auditor)', desc: 'Fact-checker dữ kiện' },
                { value: 4, label: 'Kiến tạo (Synthesizer)', desc: 'Giải pháp siêu hình' },
              ].map((r) => (
                <button
                  key={r.value}
                  type="button"
                  onClick={() => setRole(r.value)}
                  className={`p-2.5 rounded-xl border text-left transition-all ${
                    role === r.value
                      ? 'bg-indigo-950/80 border-indigo-500 text-indigo-200 shadow-md shadow-indigo-900/30'
                      : 'bg-slate-950/50 border-slate-800 text-slate-400 hover:border-slate-700'
                  }`}
                >
                  <div className="font-bold text-[11px] truncate">{r.label}</div>
                  <div className="text-[10px] text-slate-400 mt-0.5">{r.desc}</div>
                </button>
              ))}
            </div>
          </div>

          {/* 2. Persona Prompt Directive (System Prompt Của Ghế Này) */}
          <div className="space-y-1.5 pt-2 border-t border-slate-800">
            <div className="flex items-center justify-between">
              <label className="text-slate-300 font-bold flex items-center gap-1.5">
                <Terminal className="w-3.5 h-3.5 text-emerald-400" />
                Chỉ Thị Nhân Cách & Luận Thuyết Cốt Lõi (Seat Persona Prompt Directive):
              </label>
              <span className="text-[10px] text-emerald-400/80 font-mono">Hierarchical Epistemic Layer 2</span>
            </div>
            <textarea
              rows={3}
              value={personaPromptDirective}
              onChange={(e) => setPersonaPromptDirective(e.target.value)}
              placeholder="Nhập System Prompt riêng cho ghế này. Sẽ được tự động kết hợp với Tuyên Ngôn Nhận Thức (Manifesto) của Viện khi kích hoạt tranh luận..."
              className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 font-mono text-xs focus:outline-none focus:border-emerald-500 transition-colors leading-relaxed"
            />
          </div>

          {/* 3. Sói Đầu Đàn (Alpha Leader) & Bể Khóa Của Nó */}
          <div className="space-y-2 pt-2 border-t border-slate-800">
            <div className="flex items-center justify-between">
              <label className="text-slate-300 font-bold flex items-center gap-1.5">
                <Cpu className="w-4 h-4 text-amber-400" />
                Sói Đầu Đàn (Alpha Model) & Bể Khóa (Keys Pool 1):
              </label>
              {(() => {
                const selected = availableModels.find(m => m.modelCode === alphaModel);
                if (!selected) return null;
                const pool = getKeyPoolStatus(selected.providerCompany);
                const str = getModelStrength(selected.modelCode);
                return (
                  <span className={`text-[10px] px-2 py-0.5 rounded-full border font-bold ${str.color}`}>
                    {str.tag}
                  </span>
                );
              })()}
            </div>

            <select
              value={alphaModel}
              onChange={(e) => setAlphaModel(e.target.value)}
              className="w-full px-3.5 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-slate-200 font-mono text-xs focus:outline-none focus:border-amber-500 transition-colors"
            >
              {availableModels.map((m) => {
                const str = getModelStrength(m.modelCode);
                const pool = getKeyPoolStatus(m.providerCompany);
                return (
                  <option key={m.modelCode} value={m.modelCode}>
                    {m.displayName} ({m.modelCode}) — ELO {m.benchmarkElo} [{m.providerCompany}] • {str.tag} • {pool.label}
                  </option>
                );
              })}
            </select>

            {(() => {
              const selected = availableModels.find(m => m.modelCode === alphaModel);
              if (!selected) return null;
              const str = getModelStrength(selected.modelCode);
              const pool = getKeyPoolStatus(selected.providerCompany);
              return (
                <div className="p-2.5 rounded-xl bg-slate-950/80 border border-slate-800 flex items-center justify-between gap-3 text-[11px]">
                  <div>
                    <div className="font-semibold text-slate-300">Thế mạnh đương thời: <span className="text-amber-300 font-normal">{str.desc}</span></div>
                    <div className="text-[10px] text-slate-400 mt-0.5 flex items-center gap-2">
                      <span>Bể khóa ({selected.providerCompany}):</span>
                      {pool.hasUserKey ? (
                        <span className="text-emerald-400 font-bold flex items-center gap-1">
                          <CheckCircle2 className="w-3 h-3" /> User BYOK sẵn sàng (Tiết kiệm 100%)
                        </span>
                      ) : (
                        <span className="text-amber-400/90 font-mono">Dùng System Key dự trữ</span>
                      )}
                    </div>
                  </div>
                  <span className="text-[10px] font-bold px-2 py-1 rounded bg-amber-500/20 text-amber-300 border border-amber-500/30 whitespace-nowrap">
                    Triệu Gọi Số 1
                  </span>
                </div>
              );
            })()}
          </div>

          {/* 4. Các Sói Thành Viên Dự Phòng & Chuỗi Bể Khóa Kế Thừa */}
          <div className="space-y-1.5 pt-2 border-t border-slate-800">
            <div className="flex items-center justify-between">
              <label className="text-slate-300 font-bold flex items-center gap-1.5">
                <Layers className="w-4 h-4 text-indigo-400" />
                Chuỗi Sói Dự Phòng & Tiếp Bước (Cascade Failover Swarm):
              </label>
              <span className="text-[10px] text-indigo-400 font-mono font-bold">
                {swarmMembers.length} mô hình trong chuỗi
              </span>
            </div>
            <p className="text-[11px] text-slate-400">
              Nếu Sói Alpha cạn Keys Pool hoặc timeout, hệ thống lập tức chuyển tiếp 0ms sang Sói tiếp theo với Bể Khóa riêng của nó:
            </p>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-1 max-h-48 overflow-y-auto pr-1">
              {availableModels.map((m) => {
                const isSelected = swarmMembers.includes(m.modelCode);
                const isAlpha = m.modelCode === alphaModel;
                const str = getModelStrength(m.modelCode);
                const pool = getKeyPoolStatus(m.providerCompany);

                return (
                  <button
                    key={m.modelCode}
                    type="button"
                    onClick={() => toggleSwarmMember(m.modelCode)}
                    disabled={isAlpha}
                    className={`p-2.5 rounded-xl border text-left transition-all flex items-start justify-between gap-2 ${
                      isAlpha
                        ? 'bg-amber-950/20 border-amber-500/40 text-amber-300 opacity-60 cursor-not-allowed'
                        : isSelected
                        ? 'bg-indigo-950/60 border-indigo-500/60 text-indigo-200'
                        : 'bg-slate-950/60 border-slate-800 text-slate-400 hover:border-slate-700'
                    }`}
                  >
                    <div className="truncate min-w-0 flex-1">
                      <div className="flex items-center gap-1.5">
                        <span className="font-semibold truncate text-[11px]">{m.displayName}</span>
                        <span className={`text-[9px] px-1.5 py-0.2 rounded font-bold border ${str.color}`}>
                          {str.tag}
                        </span>
                      </div>
                      <div className="text-[10px] font-mono text-slate-400 truncate mt-0.5">
                        {m.modelCode} • ELO {m.benchmarkElo}
                      </div>
                      <div className="text-[9px] text-slate-400 mt-1 flex items-center gap-1">
                        <Key className="w-2.5 h-2.5 text-slate-500" />
                        <span>Bể {m.providerCompany}: {pool.hasUserKey ? 'Có User Key' : 'System Key'}</span>
                      </div>
                    </div>
                    {isAlpha ? (
                      <span className="text-[10px] font-bold px-1.5 py-0.5 rounded bg-amber-500/20 text-amber-300 whitespace-nowrap shrink-0">
                        Alpha
                      </span>
                    ) : isSelected ? (
                      <CheckCircle2 className="w-4 h-4 text-indigo-400 shrink-0 mt-0.5" />
                    ) : null}
                  </button>
                );
              })}
            </div>
          </div>

          {/* 5. Chính Sách Vận Hành (Swarm Policy) */}
          <div className="space-y-1.5 pt-2 border-t border-slate-800">
            <label className="text-slate-300 font-bold block">Chính Sách Vận Hành Bầy Sói (Execution Policy):</label>
            <div className="grid grid-cols-2 gap-3">
              <label
                className={`p-3 rounded-xl border cursor-pointer transition-all flex flex-col justify-between ${
                  policy === 'CASCADE_FAILOVER'
                    ? 'bg-emerald-950/40 border-emerald-500/60 text-emerald-200'
                    : 'bg-slate-950 border-slate-800 text-slate-400'
                }`}
              >
                <div className="flex items-center gap-2">
                  <input
                    type="radio"
                    name="swarmPolicy"
                    value="CASCADE_FAILOVER"
                    checked={policy === 'CASCADE_FAILOVER'}
                    onChange={() => setPolicy('CASCADE_FAILOVER')}
                    className="sr-only"
                  />
                  <span className="font-bold text-xs">Cascade Failover (Khuyến nghị)</span>
                </div>
                <p className="text-[10px] text-slate-400 mt-1 leading-snug">
                  Gọi Sói Alpha trước (với Keys Pool của nó: User ➔ System). Nếu cạn khóa hoặc lỗi, tự động chuyển 0ms sang Sói tiếp theo. Tiết kiệm FinOps tối đa.
                </p>
              </label>

              <label
                className={`p-3 rounded-xl border cursor-pointer transition-all flex flex-col justify-between ${
                  policy === 'SWARM_CONSENSUS'
                    ? 'bg-purple-950/40 border-purple-500/60 text-purple-200'
                    : 'bg-slate-950 border-slate-800 text-slate-400'
                }`}
              >
                <div className="flex items-center gap-2">
                  <input
                    type="radio"
                    name="swarmPolicy"
                    value="SWARM_CONSENSUS"
                    checked={policy === 'SWARM_CONSENSUS'}
                    onChange={() => setPolicy('SWARM_CONSENSUS')}
                    className="sr-only"
                  />
                  <span className="font-bold text-xs">Swarm Consensus</span>
                </div>
                <p className="text-[10px] text-slate-400 mt-1 leading-snug">
                  Triệu gọi đồng thời toàn bầy để lấy đồng thuận ma trận đa mô hình và đối sánh kết quả.
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
            onClick={handleSave}
            disabled={saving}
            className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-amber-500 via-indigo-600 to-purple-600 hover:from-amber-400 hover:to-purple-500 text-white font-bold transition-all shadow-lg shadow-indigo-600/30 disabled:opacity-50"
          >
            <Save className="w-4 h-4" />
            {saving ? 'Đang Lưu...' : 'Lưu Chuỗi Bầy Sói & Bể Khóa'}
          </button>
        </div>
      </div>
    </div>
  );
};
