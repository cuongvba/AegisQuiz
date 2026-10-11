import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { 
  X, Sparkles, Database, Check, Shield, Brain, Zap, Scale, 
  Layers, CheckCircle2, AlertCircle, RefreshCw, Cpu, Globe, 
  Award, ExternalLink, HelpCircle
} from 'lucide-react';
import { 
  aiSwarmService, 
  GatewayModelPresetDto, 
  CognitiveArchetype,
  ARCHETYPE_LABELS 
} from '@/services/ai-swarm.service';
import { ARCHETYPE_I18N_KEYS } from './ModelUpsertModal';

interface GatewayCatalogModalProps {
  isOpen: boolean;
  onClose: () => void;
  onImportSuccess: () => void;
}

export const GatewayCatalogModal: React.FC<GatewayCatalogModalProps> = ({
  isOpen,
  onClose,
  onImportSuccess
}) => {
  const { t } = useTranslation('ai_settings');
  const [presets, setPresets] = useState<GatewayModelPresetDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [importing, setImporting] = useState(false);
  const [selectedGateway, setSelectedGateway] = useState<string>('all');
  const [categoryFilter, setCategoryFilter] = useState<'all' | 'free' | 'counter' | 'reasoning'>('all');
  const [selectedCodes, setSelectedCodes] = useState<Set<string>>(new Set());
  const [autoAssignCounter, setAutoAssignCounter] = useState(true);
  const [resultMessage, setResultMessage] = useState<string | null>(null);

  // Load presets from backend
  useEffect(() => {
    if (isOpen) {
      loadPresets(selectedGateway);
    }
  }, [isOpen, selectedGateway]);

  const loadPresets = async (gw: string) => {
    setLoading(true);
    try {
      const data = await aiSwarmService.getGatewayPresets(gw);
      setPresets(data);
      // Mặc định chọn trước tất cả các model Free và Counter
      const defaultSelected = new Set(
        data.filter(d => d.isFree || d.recommendedCouncilRole === 2).map(d => d.modelCode)
      );
      setSelectedCodes(defaultSelected);
    } catch (err) {
      console.error('Failed to load gateway presets', err);
    } finally {
      setLoading(false);
    }
  };

  const toggleSelect = (code: string) => {
    setSelectedCodes(prev => {
      const next = new Set(prev);
      if (next.has(code)) {
        next.delete(code);
      } else {
        next.add(code);
      }
      return next;
    });
  };

  const selectAll = () => {
    setSelectedCodes(new Set(filteredPresets.map(p => p.modelCode)));
  };

  const deselectAll = () => {
    setSelectedCodes(new Set());
  };

  const handleImport = async () => {
    if (selectedCodes.size === 0) return;
    setImporting(true);
    setResultMessage(null);
    try {
      const res = await aiSwarmService.importGatewayPack({
        gatewayProvider: selectedGateway,
        selectedModelCodes: Array.from(selectedCodes),
        autoAssignToCounterCouncils: autoAssignCounter
      });
      setResultMessage(res.message);
      setTimeout(() => {
        onImportSuccess();
        onClose();
      }, 1500);
    } catch (err: any) {
      setResultMessage(err?.response?.data?.message || err?.message || 'Lỗi nạp mô hình');
    } finally {
      setImporting(false);
    }
  };

  if (!isOpen) return null;

  const filteredPresets = presets.filter(p => {
    if (categoryFilter === 'free') return p.isFree;
    if (categoryFilter === 'counter') return p.recommendedCouncilRole === 2 || p.cognitiveArchetype === CognitiveArchetype.LinguisticArbiter;
    if (categoryFilter === 'reasoning') return p.cognitiveArchetype === CognitiveArchetype.DeepReasoner;
    return true;
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-md overflow-y-auto">
      <div className="relative w-full max-w-5xl my-6 rounded-3xl bg-slate-900 border border-slate-800 shadow-2xl p-6 md:p-8 text-slate-100 max-h-[92vh] flex flex-col">
        
        {/* Header */}
        <div className="flex items-start justify-between pb-4 border-b border-slate-800 shrink-0">
          <div className="flex items-center gap-3.5">
            <div className="w-12 h-12 rounded-2xl bg-gradient-to-tr from-indigo-600 to-purple-600 flex items-center justify-center text-white shadow-lg shadow-indigo-600/30">
              <Sparkles className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-xl font-black text-white">
                  {t('gateway_catalog.title', 'Khám Phá & Nạp Nhanh Gateway Models')}
                </h3>
                <span className="px-2.5 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 text-xs font-bold font-mono">
                  1-Key • Multi-Model
                </span>
              </div>
              <p className="text-xs text-slate-400 mt-0.5">
                {t('gateway_catalog.subtitle', 'Chỉ cần 1 API Key trong Vault để dùng chung vô số models • Tự động nhận diện thế mạnh nhận thức & tối ưu hóa phân vai cho Bầy Sói Phản Biện (Counter Swarms)')}
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-400 hover:text-white transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Gateway Provider Filter Tabs */}
        <div className="mt-4 flex flex-wrap items-center justify-between gap-3 shrink-0">
          <div className="flex items-center gap-2 p-1 rounded-2xl bg-slate-950 border border-slate-800 text-xs">
            <button
              onClick={() => setSelectedGateway('all')}
              className={`px-3.5 py-1.5 rounded-xl font-bold transition-all ${
                selectedGateway === 'all'
                  ? 'bg-indigo-600 text-white shadow'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              {t('gateway_catalog.tab_all_gateways', 'Tất Cả Gateways')}
            </button>
            <button
              onClick={() => setSelectedGateway('OpenRouter')}
              className={`px-3.5 py-1.5 rounded-xl font-bold transition-all flex items-center gap-1.5 ${
                selectedGateway === 'OpenRouter'
                  ? 'bg-indigo-600 text-white shadow'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <Globe className="w-3.5 h-3.5 text-sky-400" />
              <span>OpenRouter (20+ Models)</span>
            </button>
            <button
              onClick={() => setSelectedGateway('GitHub')}
              className={`px-3.5 py-1.5 rounded-xl font-bold transition-all flex items-center gap-1.5 ${
                selectedGateway === 'GitHub'
                  ? 'bg-indigo-600 text-white shadow'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <Cpu className="w-3.5 h-3.5 text-purple-400" />
              <span>GitHub Models (PAT)</span>
            </button>
          </div>

          {/* Category Filter Pills */}
          <div className="flex items-center gap-1.5 text-xs">
            <button
              onClick={() => setCategoryFilter('all')}
              className={`px-2.5 py-1 rounded-lg font-medium transition-colors ${
                categoryFilter === 'all' ? 'bg-slate-800 text-indigo-300 border border-indigo-500/40' : 'text-slate-400 hover:text-white'
              }`}
            >
              {t('gateway_catalog.filter_all', 'Tất cả')} ({presets.length})
            </button>
            <button
              onClick={() => setCategoryFilter('counter')}
              className={`px-2.5 py-1 rounded-lg font-medium transition-colors flex items-center gap-1 ${
                categoryFilter === 'counter' ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40' : 'text-slate-400 hover:text-white'
              }`}
            >
              <Scale className="w-3 h-3 text-amber-400" />
              <span>{t('gateway_catalog.filter_counter', 'Chuyên Phản Biện (Counter)')}</span>
            </button>
            <button
              onClick={() => setCategoryFilter('free')}
              className={`px-2.5 py-1 rounded-lg font-medium transition-colors flex items-center gap-1 ${
                categoryFilter === 'free' ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40' : 'text-slate-400 hover:text-white'
              }`}
            >
              <Zap className="w-3 h-3 text-emerald-400" />
              <span>{t('gateway_catalog.filter_free', 'Miễn Phí (0$ Token)')}</span>
            </button>
            <button
              onClick={() => setCategoryFilter('reasoning')}
              className={`px-2.5 py-1 rounded-lg font-medium transition-colors flex items-center gap-1 ${
                categoryFilter === 'reasoning' ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40' : 'text-slate-400 hover:text-white'
              }`}
            >
              <Brain className="w-3 h-3 text-purple-400" />
              <span>{t('gateway_catalog.filter_reasoning', 'Suy Luận (CoT)')}</span>
            </button>
          </div>
        </div>

        {/* Multi-Select Action Bar */}
        <div className="mt-3 py-2 px-3 rounded-xl bg-slate-950/60 border border-slate-800/80 flex items-center justify-between text-xs shrink-0">
          <div className="flex items-center gap-3">
            <span className="text-slate-300">
              {t('gateway_catalog.selected_count', 'Đã chọn:')}{' '}
              <strong className="text-indigo-400 font-mono text-sm">{selectedCodes.size}</strong> / {filteredPresets.length} mô hình
            </span>
            <div className="flex items-center gap-1.5 border-l border-slate-800 pl-3">
              <button 
                type="button" 
                onClick={selectAll} 
                className="text-[11px] text-indigo-400 hover:text-indigo-300 font-medium underline"
              >
                {t('gateway_catalog.btn_select_all', 'Chọn tất cả')}
              </button>
              <span className="text-slate-600">•</span>
              <button 
                type="button" 
                onClick={deselectAll} 
                className="text-[11px] text-slate-400 hover:text-slate-300 font-medium underline"
              >
                {t('gateway_catalog.btn_deselect_all', 'Bỏ chọn')}
              </button>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <label className="flex items-center gap-1.5 cursor-pointer select-none text-[11px] text-slate-300">
              <input
                type="checkbox"
                checked={autoAssignCounter}
                onChange={(e) => setAutoAssignCounter(e.target.checked)}
                className="rounded border-slate-700 bg-slate-900 text-indigo-600 focus:ring-indigo-500"
              />
              <span className="font-semibold text-amber-300">
                {t('gateway_catalog.checkbox_auto_assign', 'Tự động gán vào Ghế Phản Biện (Counter Swarms) của các Viện')}
              </span>
            </label>
          </div>
        </div>

        {/* Message Banner */}
        {resultMessage && (
          <div className="mt-3 p-3 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-300 flex items-center gap-2 text-xs shrink-0">
            <CheckCircle2 className="w-4 h-4 shrink-0 text-emerald-400" />
            <span>{resultMessage}</span>
          </div>
        )}

        {/* Cards Grid */}
        <div className="mt-4 flex-1 overflow-y-auto pr-1 space-y-3 min-h-0">
          {loading ? (
            <div className="py-16 text-center text-slate-400 flex flex-col items-center gap-3">
              <RefreshCw className="w-6 h-6 animate-spin text-indigo-500" />
              <span>{t('gateway_catalog.loading', 'Đang tải danh mục mô hình & phân tích ma trận năng lực...')}</span>
            </div>
          ) : filteredPresets.length === 0 ? (
            <div className="py-16 text-center text-slate-500">
              <span>{t('gateway_catalog.empty', 'Không tìm thấy mô hình phù hợp.')}</span>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5">
              {filteredPresets.map(preset => {
                const isSelected = selectedCodes.has(preset.modelCode);
                const archLabel = ARCHETYPE_LABELS[preset.cognitiveArchetype];
                const localizedArch = t(ARCHETYPE_I18N_KEYS[preset.cognitiveArchetype]?.nameKey) || archLabel.name;

                return (
                  <div
                    key={preset.modelCode}
                    onClick={() => toggleSelect(preset.modelCode)}
                    className={`cursor-pointer p-4 rounded-2xl border transition-all relative flex flex-col justify-between ${
                      isSelected
                        ? 'bg-slate-950/90 border-indigo-500/70 shadow-lg shadow-indigo-950/40 ring-1 ring-indigo-500/30'
                        : 'bg-slate-950/50 border-slate-800 hover:border-slate-700 hover:bg-slate-950/70'
                    }`}
                  >
                    <div>
                      {/* Top Row: Provider badge + Name + Checkbox */}
                      <div className="flex items-start justify-between gap-2 mb-2">
                        <div className="flex items-center gap-2">
                          <span className={`text-[10px] px-2 py-0.5 rounded-full font-bold uppercase tracking-wider border ${
                            preset.providerCompany === 'OpenRouter'
                              ? 'bg-sky-500/20 text-sky-300 border-sky-500/30'
                              : 'bg-purple-500/20 text-purple-300 border-purple-500/30'
                          }`}>
                            {preset.providerCompany}
                          </span>
                          {preset.isFree ? (
                            <span className="text-[10px] px-2 py-0.5 rounded-full font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 flex items-center gap-1">
                              <Zap className="w-2.5 h-2.5" />
                              FREE
                            </span>
                          ) : (
                            <span className="text-[10px] px-2 py-0.5 rounded-full font-bold bg-amber-500/20 text-amber-300 border border-amber-500/30 font-mono">
                              ${preset.costInputPerMillion} in
                            </span>
                          )}
                        </div>

                        <div className={`w-5 h-5 rounded-lg flex items-center justify-center border transition-all ${
                          isSelected
                            ? 'bg-indigo-600 border-indigo-500 text-white'
                            : 'border-slate-700 bg-slate-900'
                        }`}>
                          {isSelected && <Check className="w-3.5 h-3.5 stroke-[3]" />}
                        </div>
                      </div>

                      {/* Model Title & ID */}
                      <h4 className="text-sm font-bold text-white mb-0.5 flex items-center gap-1.5">
                        <span>{preset.displayName}</span>
                      </h4>
                      <div className="text-[11px] font-mono text-indigo-300/80 mb-2 truncate">
                        {preset.modelName}
                      </div>

                      {/* Cognitive Archetype Badge */}
                      <div className="mb-2.5 flex items-center gap-2 text-[11px]">
                        <span 
                          className="px-2 py-0.5 rounded-md font-bold text-[10px] border flex items-center gap-1"
                          style={{ 
                            color: archLabel.badgeColor, 
                            borderColor: `${archLabel.badgeColor}40`,
                            backgroundColor: `${archLabel.badgeColor}15`
                          }}
                        >
                          <Brain className="w-3 h-3" />
                          {localizedArch}
                        </span>
                        <span className="text-slate-400 font-mono text-[10px]">
                          ELO {preset.benchmarkElo} • {preset.avgLatencyMs}ms
                        </span>
                      </div>

                      {/* Cognitive Strength (Thế mạnh nhận thức) */}
                      <div className="p-2.5 rounded-xl bg-slate-900/90 border border-slate-800 text-xs mb-2.5">
                        <div className="flex items-center gap-1.5 text-amber-300 font-bold mb-1 text-[11px]">
                          <Award className="w-3.5 h-3.5 text-amber-400 shrink-0" />
                          <span>{t('gateway_catalog.strength_label', 'Thế mạnh đặc trưng:')}</span>
                        </div>
                        <p className="text-[11px] text-slate-300 leading-relaxed">
                          {preset.contemporaryStrength}
                        </p>
                      </div>

                      {/* Recommended Counter Role & Councils */}
                      <div className="text-[10px] text-slate-400 bg-indigo-950/30 p-2 rounded-xl border border-indigo-500/20 flex flex-wrap items-center justify-between gap-1">
                        <div>
                          <span className="text-indigo-300 font-semibold">{t('gateway_catalog.council_role_label', 'Tối ưu cho vai trò:')} </span>
                          <strong className="text-white">
                            {preset.recommendedCouncilRole === 2 
                              ? '⚔️ Phản Biện (Adversarial Critic)' 
                              : preset.recommendedCouncilRole === 3 
                              ? '🔍 Kiểm Chứng (Empirical Auditor)' 
                              : preset.recommendedCouncilRole === 1 
                              ? '👑 Chủ Tịch (President)' 
                              : '🔮 Hợp Đề (Synthesizer)'}
                          </strong>
                        </div>
                        <div className="font-mono text-purple-300 font-semibold">
                          {preset.recommendedCouncilCodes.join(', ')}
                        </div>
                      </div>
                    </div>

                    <div className="mt-2 text-[10px] text-slate-500 italic line-clamp-1">
                      {preset.description}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Footer Actions */}
        <div className="mt-4 pt-4 border-t border-slate-800 flex items-center justify-between shrink-0">
          <div className="text-xs text-slate-400">
            {t('gateway_catalog.footer_note', '💡 Tất cả các mô hình sau khi nạp sẽ tự động dùng chung chùm API Key tương ứng trong Key Vault.')}
          </div>

          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 font-bold text-xs transition-colors"
            >
              {t('buttons.btn_cancel', 'Hủy')}
            </button>
            <button
              type="button"
              onClick={handleImport}
              disabled={selectedCodes.size === 0 || importing}
              className="px-5 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 hover:from-indigo-500 hover:to-pink-500 text-white font-bold text-xs transition-all shadow-lg shadow-indigo-600/30 disabled:opacity-50 flex items-center gap-2"
            >
              {importing ? (
                <>
                  <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                  <span>{t('gateway_catalog.importing_btn', 'Đang nạp & tối ưu hóa phân vai...')}</span>
                </>
              ) : (
                <>
                  <Zap className="w-3.5 h-3.5" />
                  <span>
                    {t('gateway_catalog.import_submit_btn', 'Nạp {{count}} Mô Hình Vào Bầy Sói Phản Biện', { count: selectedCodes.size })}
                  </span>
                </>
              )}
            </button>
          </div>
        </div>

      </div>
    </div>
  );
};
