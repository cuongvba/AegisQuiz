import React, { useState, useEffect } from 'react';
import { 
  Brain, Zap, Shield, Database, Cpu, Activity, 
  Key, RefreshCw, CheckCircle2, AlertTriangle, 
  Settings, Save, Sliders, DollarSign, Layers, ArrowRight, Server
} from 'lucide-react';
import api from '@/shared/api/client';

interface ProviderConfig {
  providerType: number;
  displayName: string;
  baseUrl: string;
  apiKeys: string[];
  defaultModel: string;
  isEnabled: boolean;
  priority: number;
  costPerMillionInputTokens: number;
  costPerMillionOutputTokens: number;
}

interface FinOpsTelemetry {
  totalRequestsProcessed: number;
  cacheHitsCount: number;
  cacheHitRatio: number;
  estimatedCostSavedUsd: number;
  totalSpentUsd: number;
  requestCountByProvider: Record<string, number>;
}

interface AiSystemConfig {
  providers: ProviderConfig[];
  taskRoutingMap: Record<string, number>;
  enableSemanticCache: boolean;
  semanticCacheSimilarityThreshold: number;
  enableMultiAgentCommittee: boolean;
  keyQuarantineSeconds: number;
  telemetry: FinOpsTelemetry;
}

export const AiSettingsPage: React.FC = () => {
  const [config, setConfig] = useState<AiSystemConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [testResults, setTestResults] = useState<Record<string, { success: boolean; latencyMs: number; message: string }> | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  useEffect(() => {
    fetchConfig();
  }, []);

  const fetchConfig = async () => {
    try {
      setLoading(true);
      const res = await api.get('/api/admin/ai/config');
      setConfig(res.data);
    } catch (err) {
      console.error('Lỗi nạp cấu hình AI:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSaveConfig = async () => {
    if (!config) return;
    try {
      setSaving(true);
      await api.post('/api/admin/ai/config', config);
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 3000);
      await fetchConfig();
    } catch (err) {
      console.error('Lỗi lưu cấu hình:', err);
      alert('Không thể lưu cấu hình AI. Vui lòng kiểm tra quyền quản trị.');
    } finally {
      setSaving(false);
    }
  };

  const handleTestLatency = async () => {
    try {
      setTesting(true);
      setTestResults(null);
      const res = await api.post('/api/admin/ai/test');
      setTestResults(res.data);
    } catch (err) {
      console.error('Lỗi kiểm thử latency:', err);
      alert('Kiểm thử kết nối thất bại.');
    } finally {
      setTesting(false);
    }
  };

  const updateProviderKeys = (providerType: number, keysText: string) => {
    if (!config) return;
    const keysArray = keysText
      .split('\n')
      .map(k => k.trim())
      .filter(k => k.length > 0);

    const updated = config.providers.map(p => 
      p.providerType === providerType ? { ...p, apiKeys: keysArray, isEnabled: keysArray.length > 0 } : p
    );
    setConfig({ ...config, providers: updated });
  };

  const toggleProviderEnabled = (providerType: number) => {
    if (!config) return;
    const updated = config.providers.map(p => 
      p.providerType === providerType ? { ...p, isEnabled: !p.isEnabled } : p
    );
    setConfig({ ...config, providers: updated });
  };

  if (loading) {
    return (
      <div className="p-8 flex flex-col items-center justify-center min-h-[60vh] space-y-3">
        <RefreshCw className="w-8 h-8 text-indigo-400 animate-spin" />
        <p className="text-slate-400 text-sm">Đang tải cấu hình Hệ Điều Hành Tác Tử AI...</p>
      </div>
    );
  }

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-8 text-slate-100">
      
      {/* Top Banner */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-6 border-b border-slate-800">
        <div className="flex items-center gap-3">
          <div className="p-3 rounded-2xl bg-gradient-to-br from-indigo-500 via-purple-500 to-pink-500 shadow-xl shadow-indigo-500/20">
            <Brain className="w-8 h-8 text-white" />
          </div>
          <div>
            <h1 className="text-2xl font-black tracking-tight text-white flex items-center gap-2">
              Hệ Điều Hành Tác Tử AI & FinOps Gateway
              <span className="text-xs px-2.5 py-0.5 rounded-full bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 font-semibold">
                ACM-OS 2026
              </span>
            </h1>
            <p className="text-sm text-slate-400">
              Định tuyến Đa nhà cung cấp (DeepSeek, Gemini, OpenAI, Claude, Ollama) • Bể API Key tự phục hồi • Semantic Cache (0$)
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={handleTestLatency}
            disabled={testing}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-sm font-semibold transition-all border border-slate-700 shadow-sm"
          >
            <Activity className={`w-4 h-4 text-emerald-400 ${testing ? 'animate-spin' : ''}`} />
            {testing ? 'Đang đo độ trễ...' : 'Kiểm Thử Kết Nối (Latency)'}
          </button>
          
          <button
            onClick={handleSaveConfig}
            disabled={saving}
            className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white text-sm font-bold transition-all shadow-lg shadow-indigo-600/30"
          >
            <Save className="w-4 h-4" />
            {saving ? 'Đang lưu...' : 'Lưu Cấu Hình'}
          </button>
        </div>
      </div>

      {saveSuccess && (
        <div className="p-4 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-300 flex items-center gap-2 text-sm">
          <CheckCircle2 className="w-5 h-5" />
          Cập nhật cấu hình Hệ Điều Hành AI thành công! Các Key mới đã được đồng bộ vào Bể chứa (Key Pool).
        </div>
      )}

      {/* FinOps AI Dashboard Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        
        <div className="p-5 rounded-2xl bg-slate-900/90 border border-slate-800 shadow-sm flex items-center gap-4">
          <div className="p-3 rounded-xl bg-indigo-500/20 text-indigo-400">
            <Cpu className="w-6 h-6" />
          </div>
          <div>
            <div className="text-2xl font-black text-white">
              {config?.telemetry.totalRequestsProcessed.toLocaleString() || 0}
            </div>
            <div className="text-xs text-slate-400 font-medium">Tổng yêu cầu AI đã xử lý</div>
          </div>
        </div>

        <div className="p-5 rounded-2xl bg-slate-900/90 border border-slate-800 shadow-sm flex items-center gap-4">
          <div className="p-3 rounded-xl bg-emerald-500/20 text-emerald-400">
            <Database className="w-6 h-6" />
          </div>
          <div>
            <div className="text-2xl font-black text-emerald-400">
              {config?.telemetry.cacheHitRatio.toFixed(1) || '0.0'}%
            </div>
            <div className="text-xs text-slate-400 font-medium">
              Tỷ lệ Semantic Cache (0$ Token)
            </div>
          </div>
        </div>

        <div className="p-5 rounded-2xl bg-slate-900/90 border border-slate-800 shadow-sm flex items-center gap-4">
          <div className="p-3 rounded-xl bg-amber-500/20 text-amber-400">
            <DollarSign className="w-6 h-6" />
          </div>
          <div>
            <div className="text-2xl font-black text-amber-300">
              ${config?.telemetry.estimatedCostSavedUsd.toFixed(2) || '0.00'}
            </div>
            <div className="text-xs text-slate-400 font-medium">Ngân sách tiết kiệm (FinOps)</div>
          </div>
        </div>

        <div className="p-5 rounded-2xl bg-slate-900/90 border border-slate-800 shadow-sm flex items-center gap-4">
          <div className="p-3 rounded-xl bg-purple-500/20 text-purple-400">
            <Layers className="w-6 h-6" />
          </div>
          <div>
            <div className="text-2xl font-black text-purple-300">
              {config?.providers.filter(p => p.isEnabled).length || 0} / {config?.providers.length || 0}
            </div>
            <div className="text-xs text-slate-400 font-medium">Providers đang hoạt động</div>
          </div>
        </div>

      </div>

      {/* Latency Test Results Banner */}
      {testResults && (
        <div className="p-5 rounded-2xl bg-slate-950/80 border border-indigo-500/30 space-y-3">
          <h3 className="text-sm font-bold text-indigo-300 flex items-center gap-2">
            <Activity className="w-4 h-4 text-emerald-400" />
            Kết Quả Đo Độ Trễ & Sức Khỏe Kết Nối (Health Ping):
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-5 gap-3">
            {Object.entries(testResults).map(([provider, res]) => (
              <div key={provider} className="p-3 rounded-xl bg-slate-900/90 border border-slate-800 flex items-center justify-between text-xs">
                <span className="font-semibold text-slate-200">{provider}</span>
                {res.success ? (
                  <span className="flex items-center gap-1 font-bold text-emerald-400">
                    <CheckCircle2 className="w-3.5 h-3.5" /> {res.latencyMs}ms
                  </span>
                ) : (
                  <span className="flex items-center gap-1 text-rose-400" title={res.message}>
                    <AlertTriangle className="w-3.5 h-3.5" /> Lỗi
                  </span>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Provider Cards Matrix */}
      <div className="space-y-4">
        <h2 className="text-lg font-bold text-white flex items-center gap-2">
          <Server className="w-5 h-5 text-indigo-400" />
          Ma Trận Các Nhà Cung Cấp AI & Bể Khóa (Key Pool)
        </h2>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
          {config?.providers.map(p => (
            <div 
              key={p.providerType}
              className={`p-5 rounded-2xl border transition-all ${
                p.isEnabled 
                  ? 'bg-slate-900/90 border-slate-800 hover:border-indigo-500/40 shadow-lg' 
                  : 'bg-slate-950/40 border-slate-800/50 opacity-60'
              }`}
            >
              <div className="flex items-center justify-between pb-3 border-b border-slate-800">
                <div className="flex items-center gap-2.5">
                  <div className={`w-3 h-3 rounded-full ${p.isEnabled ? 'bg-emerald-400 shadow-md shadow-emerald-400/50' : 'bg-slate-600'}`} />
                  <div>
                    <h3 className="font-bold text-white text-base">{p.displayName}</h3>
                    <p className="text-xs text-slate-400 font-mono">{p.defaultModel}</p>
                  </div>
                </div>

                <div className="flex items-center gap-3">
                  <span className="text-[11px] font-mono px-2 py-0.5 rounded bg-slate-800 text-slate-300">
                    Ưu tiên #{p.priority}
                  </span>
                  <label className="relative inline-flex items-center cursor-pointer">
                    <input 
                      type="checkbox" 
                      checked={p.isEnabled} 
                      onChange={() => toggleProviderEnabled(p.providerType)}
                      className="sr-only peer"
                    />
                    <div className="w-11 h-6 bg-slate-700 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-indigo-600"></div>
                  </label>
                </div>
              </div>

              <div className="pt-4 space-y-3">
                <div className="flex items-center justify-between text-xs">
                  <span className="text-slate-400 flex items-center gap-1">
                    <Key className="w-3.5 h-3.5 text-amber-400" />
                    Bể API Keys ({p.apiKeys.length} keys hoạt động):
                  </span>
                  <span className="text-slate-500 text-[11px]">Mỗi dòng 1 Key (Cân bằng tải Round-Robin)</span>
                </div>

                <textarea
                  rows={3}
                  value={p.apiKeys.join('\n')}
                  onChange={(e) => updateProviderKeys(p.providerType, e.target.value)}
                  placeholder="Dán API Key tại đây (Hệ thống tự động xoay tua Round-Robin)..."
                  className="w-full px-3 py-2 text-xs font-mono bg-slate-950/80 border border-slate-800 rounded-xl text-slate-200 focus:outline-none focus:border-indigo-500 transition-colors"
                />

                <div className="flex items-center justify-between text-[11px] text-slate-500 pt-1">
                  <span>Chi phí: ~${p.costPerMillionInputTokens}/1M in • ${p.costPerMillionOutputTokens}/1M out</span>
                  <span>Tự cách ly khi gặp 429: Có (60s)</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Task Routing Section */}
      <div className="p-6 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-lg font-bold text-white flex items-center gap-2">
              <Sliders className="w-5 h-5 text-indigo-400" />
              Định Tuyến Tác Vụ Thông Minh Theo Sở Trường (Task-Specific Routing)
            </h2>
            <p className="text-xs text-slate-400">
              Tự động phân bổ tác vụ cho model có năng lực cao nhất với chi phí rẻ nhất
            </p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 text-xs">
          {[
            { task: 'SolveQuestions', label: 'Giải Đề Toán & Nghiệp Vụ', desc: 'Ưu tiên DeepSeek-R1 (Chi phí siêu rẻ, logic sâu)' },
            { task: 'CognitiveWalkthrough', label: 'Bản Đồ Tư Duy Sư Phạm', desc: 'DeepSeek-R1 / Claude 3.5 Sonnet' },
            { task: 'GenerateQuestions', label: 'Bóc Tách & Sinh Đề Từ File', desc: 'Google Gemini 2.0 Flash (Tốc độ & Context lớn)' },
            { task: 'GradeEssay', label: 'Chấm Điểm Tự Luận Barem', desc: 'Anthropic Claude 3.5 Sonnet' },
            { task: 'ArenaAdversary', label: 'Kẻ Đi Săn Đấu Trường', desc: 'OpenAI GPT-4o-mini / Gemini' },
          ].map(t => (
            <div key={t.task} className="p-3.5 rounded-xl bg-slate-950/60 border border-slate-800/80 space-y-1.5">
              <div className="font-bold text-slate-200">{t.label}</div>
              <p className="text-[11px] text-slate-500">{t.desc}</p>
              <div className="pt-1 text-indigo-400 font-medium">
                Khuyên dùng: {t.task === 'GradeEssay' ? 'Anthropic' : t.task === 'GenerateQuestions' ? 'Google Gemini' : 'DeepSeek'}
              </div>
            </div>
          ))}
        </div>
      </div>

    </div>
  );
};
export default AiSettingsPage;
