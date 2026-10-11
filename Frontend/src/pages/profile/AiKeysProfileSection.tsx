import React, { useState, useEffect } from 'react';
import { 
  Key, Plus, Trash2, Activity, CheckCircle2, AlertTriangle, 
  Shield, Sparkles, Award, Globe, Lock, Cpu, Server, X
} from 'lucide-react';
import { 
  aiSwarmService, 
  UserAiKeyDto, 
  SaveUserAiKeyRequest, 
  KeyUsageScope, 
  ModelProtocolType,
  CommunityContributorHonorDto 
} from '@/services/ai-swarm.service';

export const AiKeysProfileSection: React.FC = () => {
  const [keys, setKeys] = useState<UserAiKeyDto[]>([]);
  const [hallOfFame, setHallOfFame] = useState<CommunityContributorHonorDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showAddModal, setShowAddModal] = useState(false);
  
  const [form, setForm] = useState<SaveUserAiKeyRequest>({
    keyLabel: '',
    providerCompany: 'DeepSeek',
    protocolType: ModelProtocolType.OpenAiCompatible,
    rawApiKey: '',
    customEndpoint: '',
    customModelName: '',
    scope: KeyUsageScope.Private,
    monthlyTokenQuota: 0,
  });

  const [testingKey, setTestingKey] = useState(false);
  const [testResult, setTestResult] = useState<{ success: boolean; latencyMs: number; message: string } | null>(null);
  const [savingKey, setSavingKey] = useState(false);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [myKeys, fame] = await Promise.all([
        aiSwarmService.getMyKeys(),
        aiSwarmService.getHallOfFame(5)
      ]);
      setKeys(myKeys);
      setHallOfFame(fame);
    } catch (err) {
      console.error('Lỗi nạp danh sách khóa AI:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleTestKey = async () => {
    if (!form.rawApiKey && !form.customEndpoint?.includes('localhost')) {
      alert('Vui lòng nhập API Key để kiểm thử.');
      return;
    }

    try {
      setTestingKey(true);
      setTestResult(null);
      const res = await aiSwarmService.pingModelKey({
        providerCompany: form.providerCompany,
        protocolType: form.protocolType,
        rawApiKey: form.rawApiKey,
        customEndpoint: form.customEndpoint,
        customModelName: form.customModelName
      });
      setTestResult(res);
    } catch (err: any) {
      setTestResult({
        success: false,
        latencyMs: 0,
        message: err.message || 'Lỗi kết nối kiểm thử'
      });
    } finally {
      setTestingKey(false);
    }
  };

  const handleSaveKey = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.rawApiKey && !form.customEndpoint) {
      alert('Vui lòng nhập Khóa API hoặc Custom Endpoint.');
      return;
    }

    try {
      setSavingKey(true);
      await aiSwarmService.saveKey(form);
      setShowAddModal(false);
      setTestResult(null);
      setForm({
        keyLabel: '',
        providerCompany: 'DeepSeek',
        protocolType: ModelProtocolType.OpenAiCompatible,
        rawApiKey: '',
        customEndpoint: '',
        customModelName: '',
        scope: KeyUsageScope.Private,
        monthlyTokenQuota: 0,
      });
      await loadData();
    } catch (err) {
      console.error('Lỗi lưu khóa API:', err);
      alert('Không thể lưu khóa API.');
    } finally {
      setSavingKey(false);
    }
  };

  const handleDeleteKey = async (id: string, label: string) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa khóa [${label}]?`)) return;
    try {
      await aiSwarmService.deleteKey(id);
      await loadData();
    } catch (err) {
      console.error('Lỗi xóa khóa API:', err);
    }
  };

  return (
    <div className="space-y-6">
      
      {/* Banner Giới Thiệu BYOK Sovereign */}
      <div className="p-6 rounded-2xl bg-gradient-to-r from-indigo-950/80 via-purple-950/60 to-slate-900 border border-indigo-500/30 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="space-y-1">
          <div className="flex items-center gap-2">
            <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-indigo-500/20 text-indigo-300 border border-indigo-500/40">
              Quyền Tự Chủ AI (BYOK Sovereign)
            </span>
            <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-500/40">
              Mã Hóa AES-256-GCM
            </span>
          </div>
          <h3 className="text-lg font-bold text-white">Khóa AI Riêng & Đóng Góp Cộng Đồng</h3>
          <p className="text-xs text-slate-300 max-w-2xl leading-relaxed">
            Sử dụng API Key cá nhân để tối ưu $0 chi phí nền tảng, hoặc trỏ về cụm GPU Ollama nội bộ. 
            Khi hiến tặng khóa vào Bể Chung, bạn sẽ được thưởng AegisCoin và vinh danh trên Bảng Vàng toàn hệ thống.
          </p>
        </div>

        <button
          onClick={() => {
            setTestResult(null);
            setShowAddModal(true);
          }}
          className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white font-bold text-xs shadow-lg shadow-indigo-600/30 transition-all flex-shrink-0"
        >
          <Plus className="w-4 h-4" />
          Thêm Khóa AI Mới
        </button>
      </div>

      {/* Danh sách khóa hiện có */}
      <div className="space-y-3">
        <h4 className="text-sm font-bold text-white flex items-center gap-2">
          <Key className="w-4 h-4 text-amber-400" />
          Khóa AI Của Bạn ({keys.length})
        </h4>

        {loading ? (
          <div className="p-8 text-center text-xs text-slate-400">Đang tải danh sách khóa...</div>
        ) : keys.length === 0 ? (
          <div className="p-8 rounded-2xl bg-slate-900/50 border border-slate-800 text-center space-y-2">
            <Key className="w-8 h-8 text-slate-600 mx-auto" />
            <p className="text-xs text-slate-400 font-medium">Bạn chưa cấu hình khóa AI riêng nào.</p>
            <p className="text-[11px] text-slate-500">Mặc định hệ thống vẫn phục vụ bạn qua Bể Khóa Toàn Cục.</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {keys.map(k => (
              <div 
                key={k.id}
                className="p-5 rounded-2xl bg-slate-900/90 border border-slate-800 hover:border-indigo-500/40 transition-all space-y-3"
              >
                <div className="flex items-start justify-between">
                  <div>
                    <div className="flex items-center gap-2 mb-1">
                      <h5 className="font-bold text-white text-sm">{k.keyLabel}</h5>
                      <span className={`px-2 py-0.5 rounded-full text-[10px] font-semibold ${
                        k.scope === KeyUsageScope.CommunityContributed
                          ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30'
                          : 'bg-indigo-500/20 text-indigo-300 border border-indigo-500/30'
                      }`}>
                        {k.scope === KeyUsageScope.CommunityContributed ? 'Cộng Đồng (Bể Chung)' : 'Dùng Riêng'}
                      </span>
                    </div>
                    <p className="text-xs text-slate-400">{k.providerCompany} • Giao thức #{k.protocolType}</p>
                  </div>

                  <button
                    onClick={() => handleDeleteKey(k.id, k.keyLabel)}
                    className="p-1.5 rounded-lg bg-rose-950/40 hover:bg-rose-900/60 text-rose-300 text-xs transition-colors"
                    title="Xóa khóa"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </div>

                <div className="space-y-1.5 text-xs">
                  <div className="flex items-center justify-between font-mono bg-slate-950 px-3 py-1.5 rounded-lg border border-slate-800">
                    <span className="text-slate-500">Vân tay khóa:</span>
                    <span className="text-amber-400 font-bold">{k.keyFingerprint}</span>
                  </div>

                  {k.customEndpoint && (
                    <div className="flex items-center justify-between text-[11px] text-slate-400">
                      <span>Endpoint riêng:</span>
                      <span className="font-mono text-slate-300 truncate max-w-[200px]">{k.customEndpoint}</span>
                    </div>
                  )}

                  <div className="flex items-center justify-between text-[11px] text-slate-400 pt-1">
                    <span>Lượt phục vụ: <b className="text-white font-mono">{k.totalCallsCount}</b></span>
                    <span>Tokens tháng này: <b className="text-white font-mono">{k.tokensUsedThisMonth.toLocaleString()}</b></span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Bảng Vàng Vinh Danh (Hall of Fame) */}
      <div className="p-6 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Award className="w-5 h-5 text-amber-400" />
            <h4 className="text-sm font-bold text-white">Bảng Vàng Đóng Góp Bể Khóa AI Cộng Đồng</h4>
          </div>
          <span className="text-xs text-slate-400">Nhận thưởng AegisCoin & Vinh Danh Cố Vấn</span>
        </div>

        {hallOfFame.length === 0 ? (
          <p className="text-xs text-slate-500 italic">Chưa có lượt đóng góp công khai nào trong chu kỳ này.</p>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            {hallOfFame.map((hero, idx) => (
              <div 
                key={hero.userId}
                className="p-3.5 rounded-xl bg-slate-950/60 border border-slate-800 flex items-center justify-between text-xs"
              >
                <div className="flex items-center gap-2.5">
                  <div className="w-7 h-7 rounded-full bg-gradient-to-br from-amber-500 to-orange-600 flex items-center justify-center font-bold text-white text-[11px] shadow-sm">
                    #{idx + 1}
                  </div>
                  <div>
                    <div className="font-bold text-white">{hero.userFullName}</div>
                    <div className="text-[10px] text-slate-400">{hero.providerCompany}</div>
                  </div>
                </div>

                <div className="text-right">
                  <div className="font-bold text-amber-400 font-mono">+{hero.aegisCoinsAwarded} Coin</div>
                  <div className="text-[10px] text-slate-500">{hero.totalContributionsServed} lượt</div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* ── MODAL THÊM KHÓA AI MỚI ── */}
      {showAddModal && (
        <div className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-3xl w-full max-w-lg p-6 space-y-4 shadow-2xl">
            <div className="flex items-center justify-between pb-3 border-b border-slate-800">
              <div className="flex items-center gap-2">
                <Key className="w-5 h-5 text-amber-400" />
                <h3 className="font-bold text-white text-base">Thêm Khóa AI Cá Nhân (BYOK)</h3>
              </div>
              <button onClick={() => setShowAddModal(false)} className="text-slate-400 hover:text-white">
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleSaveKey} className="space-y-3.5 text-xs">
              <div>
                <label className="text-slate-300 font-semibold block mb-1">Tên Gợi Nhớ (Key Label):</label>
                <input
                  type="text"
                  required
                  placeholder="vd: DeepSeek R1 Nghiên Cứu Cá Nhân"
                  value={form.keyLabel}
                  onChange={(e) => setForm({ ...form, keyLabel: e.target.value })}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-white focus:outline-none focus:border-indigo-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-slate-300 font-semibold block mb-1">Nhà Cung Cấp:</label>
                  <select
                    value={form.providerCompany}
                    onChange={(e) => setForm({ ...form, providerCompany: e.target.value })}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-white focus:outline-none focus:border-indigo-500"
                  >
                    <option value="DeepSeek">DeepSeek (R1 / V3)</option>
                    <option value="Google">Google Gemini (2.5 / 2.0 / 1.5)</option>
                    <option value="Anthropic">Anthropic Claude (3.5 Sonnet / Haiku)</option>
                    <option value="OpenAI">OpenAI (GPT-4o / o1 / o3-mini)</option>
                    <option value="Groq">Groq LPU (Llama 3.3 70B Ultra-Fast)</option>
                    <option value="SambaNova">SambaNova Cloud (SN40L Llama 3.1 405B)</option>
                    <option value="Cerebras">Cerebras Wafer-Scale (Llama 3.3 70B &gt;2000 tps)</option>
                    <option value="Mistral">Mistral AI (Large 2411 / Codestral)</option>
                    <option value="GitHub">GitHub Models (Azure AI Gateway)</option>
                    <option value="Replicate">Replicate (MusicGen &amp; Multimodal Media)</option>
                    <option value="Ollama">Ollama (Local Sovereign GPU / Intranet)</option>
                  </select>
                </div>

                <div>
                  <label className="text-slate-300 font-semibold block mb-1">Chuẩn Giao Thức:</label>
                  <select
                    value={form.protocolType}
                    onChange={(e) => setForm({ ...form, protocolType: Number(e.target.value) })}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-white focus:outline-none focus:border-indigo-500"
                  >
                    <option value={ModelProtocolType.OpenAiCompatible}>OpenAI-Compatible</option>
                    <option value={ModelProtocolType.GoogleGenerativeAi}>Google Gemini Native</option>
                    <option value={ModelProtocolType.AnthropicMessages}>Anthropic Claude Native</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="text-slate-300 font-semibold block mb-1">Khóa API Bí Mật (API Key):</label>
                <input
                  type="password"
                  placeholder="sk-..."
                  value={form.rawApiKey}
                  onChange={(e) => setForm({ ...form, rawApiKey: e.target.value })}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-white font-mono text-[11px] focus:outline-none focus:border-indigo-500"
                />
                <p className="text-[10px] text-slate-500 mt-1">
                  Được mã hóa tức thì bằng AES-256-GCM trước khi lưu vào CSDL.
                </p>
              </div>

              <div>
                <label className="text-slate-300 font-semibold block mb-1">Custom Endpoint (Tùy chọn cho Ollama/Proxy):</label>
                <input
                  type="text"
                  placeholder="http://localhost:11434/v1 hoặc proxy URL riêng"
                  value={form.customEndpoint || ''}
                  onChange={(e) => setForm({ ...form, customEndpoint: e.target.value })}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-white font-mono text-[11px] focus:outline-none focus:border-indigo-500"
                />
              </div>

              <div>
                <label className="text-slate-300 font-semibold block mb-1">Mục Đích Sử Dụng:</label>
                <div className="grid grid-cols-2 gap-3 pt-1">
                  <button
                    type="button"
                    onClick={() => setForm({ ...form, scope: KeyUsageScope.Private })}
                    className={`p-3 rounded-xl border text-left transition-all ${
                      form.scope === KeyUsageScope.Private
                        ? 'bg-indigo-950/60 border-indigo-500 text-white'
                        : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-white'
                    }`}
                  >
                    <div className="font-bold flex items-center gap-1.5 mb-0.5">
                      <Lock className="w-3.5 h-3.5 text-indigo-400" />
                      Dùng Riêng ($0 Phụ Phí)
                    </div>
                    <p className="text-[10px] text-slate-500">Chỉ tài khoản của bạn được tiêu thụ</p>
                  </button>

                  <button
                    type="button"
                    onClick={() => setForm({ ...form, scope: KeyUsageScope.CommunityContributed })}
                    className={`p-3 rounded-xl border text-left transition-all ${
                      form.scope === KeyUsageScope.CommunityContributed
                        ? 'bg-emerald-950/60 border-emerald-500 text-white'
                        : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-white'
                    }`}
                  >
                    <div className="font-bold flex items-center gap-1.5 mb-0.5">
                      <Globe className="w-3.5 h-3.5 text-emerald-400" />
                      Hiến Tặng Bể Chung
                    </div>
                    <p className="text-[10px] text-emerald-500 font-medium">Nhận thưởng AegisCoin & Bảng Vàng</p>
                  </button>
                </div>
              </div>

              {testResult && (
                <div className={`p-3 rounded-xl text-xs flex items-center justify-between ${
                  testResult.success 
                    ? 'bg-emerald-950/50 text-emerald-300 border border-emerald-800' 
                    : 'bg-rose-950/50 text-rose-300 border border-rose-800'
                }`}>
                  <span className="flex items-center gap-1.5">
                    {testResult.success ? <CheckCircle2 className="w-4 h-4" /> : <AlertTriangle className="w-4 h-4" />}
                    {testResult.message}
                  </span>
                  <span className="font-mono font-bold">{testResult.latencyMs}ms</span>
                </div>
              )}

              <div className="flex items-center justify-between pt-3 border-t border-slate-800">
                <button
                  type="button"
                  onClick={handleTestKey}
                  disabled={testingKey}
                  className="flex items-center gap-1.5 px-3 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 font-medium transition-colors"
                >
                  <Activity className={`w-3.5 h-3.5 text-emerald-400 ${testingKey ? 'animate-spin' : ''}`} />
                  {testingKey ? 'Đang đo...' : 'Kiểm thử kết nối'}
                </button>

                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => setShowAddModal(false)}
                    className="px-3.5 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300"
                  >
                    Hủy
                  </button>
                  <button
                    type="submit"
                    disabled={savingKey}
                    className="px-4 py-2 rounded-xl bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white font-bold transition-all shadow-md shadow-indigo-600/30 disabled:opacity-50"
                  >
                    {savingKey ? 'Đang lưu...' : 'Lưu Khóa An Toàn'}
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}

    </div>
  );
};
