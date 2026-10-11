import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { 
  X, Brain, Zap, Shield, Layers, Scale, DollarSign, 
  Activity, Key, Trash2, Edit3, Plus, CheckCircle2, 
  AlertTriangle, Lock, Eye, EyeOff, Sparkles, Server, Clock, Award,
  Copy, Check, ShieldCheck, KeyRound, Sliders, RefreshCw
} from 'lucide-react';
import { useAuth } from '@/hooks/useAuth';
import { 
  aiSwarmService, 
  AiModelRegistryDto, 
  UserAiKeyDto, 
  CognitiveArchetype, 
  ModelProtocolType,
  KeyUsageScope,
  ARCHETYPE_LABELS
} from '@/services/ai-swarm.service';
import { ARCHETYPE_I18N_KEYS } from './ModelUpsertModal';

export interface ProviderConfig {
  providerCode?: string;
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

interface ModelDetailModalProps {
  model: AiModelRegistryDto | null;
  isOpen: boolean;
  onClose: () => void;
  onEdit: (model: AiModelRegistryDto) => void;
  onDelete: (modelId: string, displayName: string) => void;
  userKeys: UserAiKeyDto[];
  systemProviders?: ProviderConfig[];
  onUpdateProviderKeys?: (providerType: number, newKeys: string[], providerCode?: string) => Promise<void>;
  onToggleProviderEnabled?: (providerType: number, providerCode?: string) => void;
  onRefreshKeys: () => void;
  onPingModel: (model: AiModelRegistryDto) => void;
  pingResult?: { success: boolean; latencyMs: number; message: string };
  isPinging?: boolean;
}

// 100% Dynamic Slugification - ZERO Hardcode
export function toProviderCode(companyOrName?: string): string {
  if (!companyOrName) return '';
  return companyOrName.toLowerCase().replace(/[^a-z0-9]/g, '').trim();
}

export const ModelDetailModal: React.FC<ModelDetailModalProps> = ({
  model,
  isOpen,
  onClose,
  onEdit,
  onDelete,
  userKeys,
  systemProviders = [],
  onUpdateProviderKeys,
  onToggleProviderEnabled,
  onRefreshKeys,
  onPingModel,
  pingResult,
  isPinging
}) => {
  const { t } = useTranslation('ai_settings');
  const { isAdmin } = useAuth();

  // Active sub-tab inside Model Detail: 'specs' (4D Epistemic Specs) vs 'keys-pool' (Keys Pool)
  const [activeTab, setActiveTab] = useState<'specs' | 'keys-pool'>('specs');

  // Keys Pool management states
  const [visibleIndices, setVisibleIndices] = useState<Record<number, boolean>>({});
  const [copiedKey, setCopiedKey] = useState<string | null>(null);
  const [newKeyInput, setNewKeyInput] = useState('');
  const [showAddKeyForm, setShowAddKeyForm] = useState(false);
  const [savingKey, setSavingKey] = useState(false);

  // Key testing state
  const [testingKeyString, setTestingKeyString] = useState<string | null>(null);
  const [keyPingResults, setKeyPingResults] = useState<Record<string, { success: boolean; latencyMs: number; message: string }>>({});

  useEffect(() => {
    // Reset to specs tab whenever opening a new model
    if (isOpen) {
      setActiveTab('specs');
      setShowAddKeyForm(false);
      setNewKeyInput('');
    }
  }, [isOpen, model?.id]);

  if (!isOpen || !model) return null;

  const archMeta = ARCHETYPE_LABELS[model.cognitiveArchetype] || ARCHETYPE_LABELS[CognitiveArchetype.DeepReasoner];

  // Resolve matching provider in systemProviders (100% dynamic string matching)
  const targetCode = toProviderCode(model.providerCompany);
  const matchedProvider = systemProviders.find(p => {
    const pCode = toProviderCode(p.providerCode || p.displayName);
    if (pCode && targetCode && (pCode === targetCode || pCode.includes(targetCode) || targetCode.includes(pCode))) return true;
    if (model.endpointUrl && p.baseUrl) {
      const epHost = model.endpointUrl.replace(/^https?:\/\//, '').split('/')[0].toLowerCase();
      const baseHost = p.baseUrl.replace(/^https?:\/\//, '').split('/')[0].toLowerCase();
      if (epHost && baseHost && (epHost.includes(baseHost) || baseHost.includes(epHost))) return true;
    }
    return false;
  });

  const systemKeys = matchedProvider?.apiKeys || [];

  // BYOK user keys for this provider
  const communityKeys = userKeys.filter(
    k => k.providerCompany.toLowerCase() === model.providerCompany.toLowerCase()
  );

  const totalKeysCount = systemKeys.length + communityKeys.length;

  const maskKey = (rawKey: string): string => {
    const trimmed = rawKey.trim();
    if (trimmed.length <= 12) return '••••••••••••••••';
    const prefix = trimmed.slice(0, 6);
    const suffix = trimmed.slice(-6);
    return `${prefix}••••••••••••••••${suffix}`;
  };

  const toggleVisibility = (idx: number) => {
    setVisibleIndices(prev => {
      const next = !prev[idx];
      const updated = { ...prev, [idx]: next };
      if (next) {
        setTimeout(() => {
          setVisibleIndices(curr => ({ ...curr, [idx]: false }));
        }, 10000);
      }
      return updated;
    });
  };

  const handleCopy = async (key: string) => {
    try {
      await navigator.clipboard.writeText(key.trim());
      setCopiedKey(key);
      setTimeout(() => setCopiedKey(null), 2000);
    } catch {
      // Fallback
    }
  };

  // Test Ping a specific key string
  const handleTestSpecificKey = async (rawKey: string) => {
    setTestingKeyString(rawKey);
    try {
      const res = await aiSwarmService.pingModelKey({
        providerCompany: model.providerCompany,
        protocolType: model.protocolType,
        rawApiKey: rawKey,
        customEndpoint: model.endpointUrl,
        customModelName: model.modelName
      });
      setKeyPingResults(prev => ({
        ...prev,
        [rawKey]: {
          success: res.success,
          latencyMs: res.latencyMs,
          message: res.message
        }
      }));
    } catch (err: any) {
      setKeyPingResults(prev => ({
        ...prev,
        [rawKey]: {
          success: false,
          latencyMs: 0,
          message: err?.response?.data?.message || err?.message || 'Lỗi kiểm thử khóa'
        }
      }));
    } finally {
      setTestingKeyString(null);
    }
  };

  // Delete a system key from the provider's pool
  const handleDeleteSystemKey = async (idxToDelete: number) => {
    if (!matchedProvider || !onUpdateProviderKeys) return;
    if (!window.confirm('Xác nhận xóa khóa này khỏi Bể Khóa của Nhà Cung Cấp?')) return;

    const updated = systemKeys.filter((_, idx) => idx !== idxToDelete);
    setSavingKey(true);
    try {
      await onUpdateProviderKeys(matchedProvider.providerType, updated, matchedProvider.providerCode || targetCode);
    } finally {
      setSavingKey(false);
    }
  };

  // Add new system key(s) to the provider's pool
  const handleAddSystemKeys = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!matchedProvider || !onUpdateProviderKeys || !newKeyInput.trim()) return;

    const lines = newKeyInput.split(/[\r\n;,]+/).map(s => s.trim()).filter(Boolean);
    const combined = Array.from(new Set([...systemKeys, ...lines]));

    setSavingKey(true);
    try {
      await onUpdateProviderKeys(matchedProvider.providerType, combined, matchedProvider.providerCode || targetCode);
      setNewKeyInput('');
      setShowAddKeyForm(false);
    } finally {
      setSavingKey(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-md overflow-y-auto">
      <div className="relative w-full max-w-4xl my-8 rounded-3xl bg-slate-900 border border-slate-800 shadow-2xl p-6 md:p-8 text-slate-100 max-h-[92vh] overflow-y-auto">
        
        {/* Header */}
        <div className="flex items-start justify-between pb-4 border-b border-slate-800">
          <div className="flex items-start gap-4">
            <div 
              className="w-12 h-12 rounded-2xl flex items-center justify-center text-white shrink-0 shadow-lg"
              style={{ backgroundColor: `${archMeta.badgeColor}25`, border: `1px solid ${archMeta.badgeColor}50` }}
            >
              <Brain className="w-6 h-6" style={{ color: archMeta.badgeColor }} />
            </div>
            <div>
              <div className="flex flex-wrap items-center gap-2 mb-1">
                <h2 className="text-xl font-black text-white">{model.displayName}</h2>
                {model.isDefaultInArchetype && (
                  <span className="text-xs px-2.5 py-0.5 rounded-full bg-amber-500/20 text-amber-300 border border-amber-500/40 font-bold flex items-center gap-1">
                    <Award className="w-3 h-3" />
                    {t('model_detail.field_alpha')}
                  </span>
                )}
                <span className={`text-[11px] px-2.5 py-0.5 rounded-full font-bold border ${
                  model.isEnabled 
                    ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30' 
                    : 'bg-slate-800 text-slate-400 border-slate-700'
                }`}>
                  {model.isEnabled ? t('model_detail.status_active') : t('model_detail.status_paused')}
                </span>
              </div>
              <p className="text-xs font-mono text-slate-400">{model.modelCode} • {model.providerCompany}</p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => onPingModel(model)}
              disabled={isPinging}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-emerald-400 text-xs font-semibold transition-all border border-slate-700"
              title={t('model_catalog.btn_ping')}
            >
              <Activity className={`w-3.5 h-3.5 ${isPinging ? 'animate-spin' : ''}`} />
              <span>{isPinging ? t('model_catalog.pinging') : t('model_catalog.btn_ping')}</span>
            </button>
            <button
              onClick={onClose}
              className="p-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-400 hover:text-white transition-colors"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Global Latency Ping Feedback if present */}
        {pingResult && (
          <div className={`mt-4 p-3 rounded-2xl text-xs flex items-center justify-between border ${
            pingResult.success 
              ? 'bg-emerald-950/40 text-emerald-300 border-emerald-800/80' 
              : 'bg-rose-950/40 text-rose-300 border-rose-800/80'
          }`}>
            <div className="flex items-center gap-2">
              <Activity className="w-4 h-4 shrink-0" />
              <span>{pingResult.message}</span>
            </div>
            <span className="font-mono font-bold text-sm">{pingResult.latencyMs}ms</span>
          </div>
        )}

        {/* Sub-Tabs & Action Bar */}
        <div className="mt-4 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 p-2 rounded-2xl bg-slate-950/70 border border-slate-800">
          {/* Sub-Tab Navigation */}
          <div className="flex items-center gap-1.5">
            <button
              onClick={() => setActiveTab('specs')}
              className={`flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl text-xs font-bold transition-all ${
                activeTab === 'specs'
                  ? 'bg-gradient-to-r from-indigo-600 to-purple-600 text-white shadow-md'
                  : 'text-slate-400 hover:text-white bg-slate-900/60'
              }`}
            >
              <Layers className="w-3.5 h-3.5" />
              <span>{t('model_detail.tab_specs')}</span>
            </button>

            <button
              onClick={() => setActiveTab('keys-pool')}
              className={`flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl text-xs font-bold transition-all ${
                activeTab === 'keys-pool'
                  ? 'bg-gradient-to-r from-amber-600 to-orange-600 text-white shadow-md'
                  : 'text-slate-400 hover:text-white bg-slate-900/60'
              }`}
            >
              <Key className="w-3.5 h-3.5 text-amber-400" />
              <span>{t('model_detail.tab_keys_pool')}</span>
              <span className={`text-[10px] px-1.5 py-0.2 rounded-full font-mono ${
                activeTab === 'keys-pool' ? 'bg-white/20 text-white' : 'bg-slate-800 text-amber-300'
              }`}>
                {totalKeysCount}
              </span>
            </button>
          </div>

          {/* RBAC Action Buttons */}
          <div className="flex items-center gap-2 self-end sm:self-auto">
            {isAdmin ? (
              <>
                <button
                  onClick={() => {
                    onClose();
                    onEdit(model);
                  }}
                  className="flex items-center gap-1 px-3 py-1.5 rounded-xl bg-indigo-600/30 hover:bg-indigo-600/50 text-indigo-300 border border-indigo-500/40 text-xs font-semibold transition-all"
                >
                  <Edit3 className="w-3.5 h-3.5" />
                  <span>{t('model_catalog.btn_edit')}</span>
                </button>
                <button
                  onClick={() => {
                    onClose();
                    onDelete(model.id, model.displayName);
                  }}
                  className="flex items-center gap-1 px-3 py-1.5 rounded-xl bg-rose-600/20 hover:bg-rose-600/40 text-rose-300 border border-rose-500/30 text-xs font-semibold transition-all"
                >
                  <Trash2 className="w-3.5 h-3.5" />
                  <span>{t('model_catalog.btn_delete')}</span>
                </button>
              </>
            ) : (
              <span className="text-[11px] text-slate-400 flex items-center gap-1">
                <Lock className="w-3 h-3 text-amber-400" />
                {t('model_detail.role_badge_readonly')}
              </span>
            )}
          </div>
        </div>

        {/* ── SUB-TAB 1: 4D EPISTEMIC SPECS ── */}
        {activeTab === 'specs' && (
          <div className="mt-5 space-y-4">
            <h3 className="text-xs font-bold text-slate-400 uppercase tracking-wider flex items-center gap-2">
              <Layers className="w-4 h-4 text-indigo-400" />
              {t('model_detail.section_specs')}
            </h3>

            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-xs">
              <div className="p-3.5 rounded-2xl bg-slate-950/70 border border-slate-800">
                <span className="text-slate-400 block mb-1">{t('model_detail.field_archetype')}</span>
                <span 
                  className="inline-block px-2 py-0.5 rounded-md font-semibold text-[11px]"
                  style={{ backgroundColor: `${archMeta.badgeColor}20`, color: archMeta.badgeColor }}
                >
                  {t(ARCHETYPE_I18N_KEYS[model.cognitiveArchetype]?.nameKey) || archMeta.name}
                </span>
              </div>

              <div className="p-3.5 rounded-2xl bg-slate-950/70 border border-slate-800">
                <span className="text-slate-400 block mb-1">{t('model_detail.field_protocol')}</span>
                <span className="font-mono text-slate-200">
                  {model.protocolType === 1 ? 'OpenAI-Compatible' : model.protocolType === 2 ? 'Google Native' : 'Anthropic Native'}
                </span>
              </div>

              <div className="p-3.5 rounded-2xl bg-slate-950/70 border border-slate-800">
                <span className="text-slate-400 block mb-1">{t('model_detail.field_elo')}</span>
                <span className="font-bold text-amber-400 text-sm font-mono">{model.benchmarkElo} ELO</span>
              </div>

              <div className="p-3.5 rounded-2xl bg-slate-950/70 border border-slate-800">
                <span className="text-slate-400 block mb-1">{t('model_detail.field_latency')}</span>
                <span className="font-bold text-emerald-400 text-sm font-mono">{model.avgLatencyMs} ms</span>
              </div>

              <div className="p-3.5 rounded-2xl bg-slate-950/70 border border-slate-800 col-span-2">
                <span className="text-slate-400 block mb-1">{t('model_detail.field_endpoint')}</span>
                <span className="font-mono text-slate-300 break-all text-[11px]">{model.endpointUrl}</span>
              </div>

              <div className="p-3.5 rounded-2xl bg-slate-950/70 border border-slate-800 col-span-2">
                <span className="text-slate-400 block mb-1">{t('model_detail.field_payload_model')}</span>
                <span className="font-mono text-indigo-300 font-semibold break-all text-[11px]">{model.modelName}</span>
              </div>

              <div className="p-3.5 rounded-2xl bg-slate-950/70 border border-slate-800">
                <span className="text-slate-400 block mb-1">{t('model_detail.field_max_context')}</span>
                <span className="font-mono text-slate-200 text-sm">{(model.maxContextTokens / 1000).toFixed(0)}k tokens</span>
              </div>

              <div className="p-3.5 rounded-2xl bg-slate-950/70 border border-slate-800">
                <span className="text-slate-400 block mb-1">{t('model_detail.field_cost_in')}</span>
                <span className="font-mono text-emerald-400 text-sm">${model.costInputPerMillion}</span>
              </div>

              <div className="p-3.5 rounded-2xl bg-slate-950/70 border border-slate-800">
                <span className="text-slate-400 block mb-1">{t('model_detail.field_cost_out')}</span>
                <span className="font-mono text-emerald-400 text-sm">${model.costOutputPerMillion}</span>
              </div>

              <div className="p-3.5 rounded-2xl bg-slate-950/70 border border-slate-800">
                <span className="text-slate-400 block mb-1">{t('model_detail.field_tenant')}</span>
                <span className="font-mono text-slate-400 text-[11px]">{model.tenantId || t('model_detail.tenant_global')}</span>
              </div>
            </div>

            {/* Quick Button to Switch to Keys Pool */}
            <div className="p-4 rounded-2xl bg-gradient-to-r from-amber-950/30 to-slate-900 border border-amber-500/20 flex items-center justify-between">
              <div>
                <span className="text-xs font-bold text-amber-300 flex items-center gap-1.5">
                  <Key className="w-4 h-4 text-amber-400" />
                  {t('model_detail.key_pool_teaser_title', { count: totalKeysCount })}
                </span>
                <p className="text-[11px] text-slate-400 mt-0.5">
                  {t('model_detail.key_pool_teaser_desc', { provider: model.providerCompany })}
                </p>
              </div>
              <button
                onClick={() => setActiveTab('keys-pool')}
                className="px-3.5 py-1.5 rounded-xl bg-amber-500 hover:bg-amber-400 text-slate-950 text-xs font-bold transition-all shadow-md"
              >
                {t('model_detail.open_keys_pool_btn')}
              </button>
            </div>
          </div>
        )}

        {/* ── SUB-TAB 2: INTEGRATED KEYS POOL FOR THIS MODEL ── */}
        {activeTab === 'keys-pool' && (
          <div className="mt-5 space-y-4">
            
            {/* Keys Pool Provider Banner */}
            <div className="p-4 rounded-2xl bg-gradient-to-br from-amber-950/40 via-slate-950 to-slate-900 border border-amber-500/30 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div>
                <div className="flex items-center gap-2">
                  <KeyRound className="w-5 h-5 text-amber-400" />
                  <h3 className="text-sm font-bold text-white">
                    {t('model_detail.keys_pool_banner_title', { provider: model.providerCompany })}
                  </h3>
                  <span className="text-[10px] px-2 py-0.2 rounded-full bg-amber-500/20 text-amber-300 font-mono border border-amber-500/30">
                    {totalKeysCount} keys
                  </span>
                </div>
                <p className="text-[11px] text-slate-400 mt-1">
                  {matchedProvider 
                    ? t('model_detail.keys_pool_cost_desc', {
                        costIn: matchedProvider.costPerMillionInputTokens,
                        costOut: matchedProvider.costPerMillionOutputTokens
                      })
                    : t('model_detail.keys_pool_auto_rotate_desc', { provider: model.providerCompany })}
                </p>
              </div>

              <div className="flex items-center gap-2 self-start sm:self-auto">
                {matchedProvider && onToggleProviderEnabled && isAdmin && (
                  <button
                    type="button"
                    onClick={() => onToggleProviderEnabled(matchedProvider.providerType, matchedProvider.providerCode || targetCode)}
                    className={`px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all ${
                      matchedProvider.isEnabled
                        ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40 hover:bg-emerald-500/30'
                        : 'bg-slate-800 text-slate-400 border-slate-700 hover:bg-slate-700'
                    }`}
                  >
                    {matchedProvider.isEnabled ? t('model_detail.pool_status_enabled') : t('model_detail.pool_status_disabled')}
                  </button>
                )}

                {isAdmin && (
                  <button
                    type="button"
                    onClick={() => setShowAddKeyForm(!showAddKeyForm)}
                    className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-amber-500 hover:bg-amber-400 text-slate-950 text-xs font-bold transition-all shadow-md"
                  >
                    <Plus className="w-4 h-4" />
                    <span>{showAddKeyForm ? t('model_detail.close_form_btn') : t('model_detail.add_key_btn')}</span>
                  </button>
                )}
              </div>
            </div>

            {/* Add Key Form */}
            {showAddKeyForm && isAdmin && (
              <form onSubmit={handleAddSystemKeys} className="p-4 rounded-2xl bg-slate-950 border border-amber-500/40 space-y-3 text-xs">
                <span className="font-bold text-amber-300 flex items-center gap-1.5">
                  <Key className="w-4 h-4" />
                  {t('model_detail.add_key_form_title', { provider: model.providerCompany })}
                </span>
                <p className="text-[11px] text-slate-400">
                  {t('model_detail.add_key_form_desc')}
                </p>
                <textarea
                  rows={3}
                  required
                  placeholder={t('model_detail.input_key_placeholder')}
                  value={newKeyInput}
                  onChange={(e) => setNewKeyInput(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded-xl p-3 text-white font-mono text-xs focus:outline-none focus:border-amber-500"
                />
                <div className="flex items-center justify-end gap-2">
                  <button
                    type="button"
                    onClick={() => setShowAddKeyForm(false)}
                    className="px-3 py-1.5 rounded-xl bg-slate-800 text-slate-300 hover:bg-slate-700"
                  >
                    {t('model_detail.btn_cancel_key')}
                  </button>
                  <button
                    type="submit"
                    disabled={savingKey}
                    className="px-4 py-1.5 rounded-xl bg-amber-500 hover:bg-amber-400 text-slate-950 font-bold transition-all disabled:opacity-50"
                  >
                    {savingKey ? t('model_detail.adding_key') : t('model_detail.add_key_submit_btn')}
                  </button>
                </div>
              </form>
            )}

            {/* System Keys List */}
            <div className="space-y-3">
              <h4 className="text-xs font-bold text-slate-400 uppercase tracking-wider flex items-center gap-2">
                <ShieldCheck className="w-4 h-4 text-emerald-400" />
                {t('model_detail.keys_system_title', { count: systemKeys.length })}
              </h4>

              {systemKeys.length === 0 ? (
                <div className="p-5 rounded-2xl bg-slate-950/60 border border-dashed border-slate-800 text-center text-slate-400 text-xs">
                  {t('model_detail.no_system_keys', { provider: model.providerCompany })}
                </div>
              ) : (
                <div className="space-y-2">
                  {systemKeys.map((keyStr, idx) => {
                    const isVisible = !!visibleIndices[idx];
                    const isCopied = copiedKey === keyStr;
                    const ping = keyPingResults[keyStr];
                    const isTestingThis = testingKeyString === keyStr;

                    return (
                      <div 
                        key={idx}
                        className="p-3 rounded-2xl bg-slate-950/90 border border-slate-800 hover:border-slate-700 transition-all text-xs font-mono space-y-2"
                      >
                        <div className="flex items-center justify-between gap-3">
                          <div className="flex items-center gap-2 overflow-hidden flex-1">
                            <span className="text-[10px] px-1.5 py-0.5 rounded bg-slate-800 text-slate-400 font-mono shrink-0">
                              #{idx + 1}
                            </span>
                            <ShieldCheck className="w-4 h-4 text-emerald-400 shrink-0" />
                            <span className={`truncate select-all ${isVisible ? 'text-amber-200 font-bold' : 'text-slate-300'}`}>
                              {isVisible ? keyStr : maskKey(keyStr)}
                            </span>
                          </div>

                          <div className="flex items-center gap-1.5 shrink-0">
                            {/* Toggle Reveal */}
                            <button
                              type="button"
                              onClick={() => toggleVisibility(idx)}
                              title={isVisible ? t('buttons.hide_keys') : t('buttons.show_keys')}
                              className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
                            >
                              {isVisible ? <EyeOff className="w-3.5 h-3.5 text-amber-400" /> : <Eye className="w-3.5 h-3.5" />}
                            </button>

                            {/* Copy */}
                            <button
                              type="button"
                              onClick={() => handleCopy(keyStr)}
                              title={t('buttons.copy_key')}
                              className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
                            >
                              {isCopied ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
                            </button>

                            {/* Ping Test Specific Key */}
                            <button
                              type="button"
                              onClick={() => handleTestSpecificKey(keyStr)}
                              disabled={isTestingThis}
                              className="flex items-center gap-1 px-2.5 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-emerald-400 text-[11px] font-semibold transition-all border border-slate-700"
                              title={t('buttons.test_key')}
                            >
                              <Activity className={`w-3 h-3 ${isTestingThis ? 'animate-spin' : ''}`} />
                              <span>{isTestingThis ? '...' : t('buttons.test_key')}</span>
                            </button>

                            {/* Delete Key */}
                            {isAdmin && (
                              <button
                                type="button"
                                onClick={() => handleDeleteSystemKey(idx)}
                                title={t('model_detail.delete_key_btn')}
                                className="p-1.5 rounded-lg text-rose-400 hover:bg-rose-950/50 transition-colors"
                              >
                                <Trash2 className="w-3.5 h-3.5" />
                              </button>
                            )}
                          </div>
                        </div>

                        {/* Latency Feedback */}
                        {ping && (
                          <div className={`p-2 rounded-xl text-[11px] flex items-center justify-between ${
                            ping.success ? 'bg-emerald-950/40 text-emerald-300 border border-emerald-800' : 'bg-rose-950/40 text-rose-300 border border-rose-800'
                          }`}>
                            <span>{ping.message}</span>
                            <span className="font-mono font-bold">{ping.latencyMs}ms</span>
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}

              {/* BYOK Community / Private Keys section if any */}
              {communityKeys.length > 0 && (
                <div className="pt-4 border-t border-slate-800/80 space-y-2">
                  <h4 className="text-xs font-bold text-slate-400 uppercase tracking-wider flex items-center gap-2">
                    <Key className="w-4 h-4 text-indigo-400" />
                    {t('model_detail.keys_byok_title', { count: communityKeys.length })}
                  </h4>
                  <div className="space-y-2">
                    {communityKeys.map(k => (
                      <div key={k.id} className="p-3 rounded-2xl bg-slate-950/60 border border-slate-800 flex items-center justify-between text-xs">
                        <div>
                          <span className="font-bold text-white mr-2">{k.keyLabel}</span>
                          <span className="font-mono text-slate-400 text-[11px]">{k.keyFingerprint}</span>
                        </div>
                        <span className="text-[10px] px-2 py-0.5 rounded-full bg-indigo-500/10 text-indigo-300">
                          {k.scope === KeyUsageScope.CommunityContributed ? t('model_detail.scope_community') : t('model_detail.scope_private')}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
export default ModelDetailModal;
