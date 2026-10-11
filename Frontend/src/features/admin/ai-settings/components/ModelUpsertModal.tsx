import React, { useState, useEffect, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { 
  X, Sparkles, Database, Layers, CheckCircle2, AlertCircle, 
  Terminal, ChevronDown, ChevronUp, Cpu, Activity, DollarSign, 
  Shield, Brain, Zap, Scale
} from 'lucide-react';
import { 
  aiSwarmService, 
  AiModelRegistryDto, 
  UpsertAiModelRequest, 
  CognitiveArchetype, 
  ModelProtocolType,
  ARCHETYPE_LABELS
} from '@/services/ai-swarm.service';

interface ModelUpsertModalProps {
  isOpen: boolean;
  onClose: () => void;
  modelToEdit: AiModelRegistryDto | null;
  onSaved: (savedModel: AiModelRegistryDto) => void;
}

// ── Smart Predictive & Epistemic Inference Engine ────────────────────────────
export function parseCurlCommand(curlText: string): { endpointUrl?: string; modelName?: string } {
  const result: { endpointUrl?: string; modelName?: string } = {};
  if (!curlText || typeof curlText !== 'string') return result;

  // Extract URL from curl command
  const urlMatch = curlText.match(/curl\s+(?:-[^\s]+\s+)*['"]?(https?:\/\/[^\s'"\\]+)/i);
  if (urlMatch && urlMatch[1]) {
    result.endpointUrl = urlMatch[1];
  }

  // Extract model name from JSON payload (-d or --data)
  const modelMatch = curlText.match(/["']model["']\s*:\s*["']([^"']+)["']/i);
  if (modelMatch && modelMatch[1]) {
    result.modelName = modelMatch[1].trim();
  }

  return result;
}

export function normalizeEndpointUrl(rawUrl: string): string {
  let url = (rawUrl || '').trim();
  if (!url) return '';
  // Remove trailing slashes
  url = url.replace(/\/+$/, '');
  // Normalize typical chat completion endpoints to base provider endpoint
  url = url.replace(/\/chat\/completions$/i, '');
  url = url.replace(/\/messages$/i, '');
  return url;
}

export interface InferredSpecs {
  providerCompany: string;
  protocolType: ModelProtocolType;
  modelCode: string;
  displayName: string;
  cognitiveArchetype: CognitiveArchetype;
  benchmarkElo: number;
  avgLatencyMs: number;
  maxContextTokens: number;
  costInputPerMillion: number;
  costOutputPerMillion: number;
}

export function inferModelSpecifications(endpointUrl: string, modelName: string): InferredSpecs {
  const normEndpoint = normalizeEndpointUrl(endpointUrl).toLowerCase();
  const normModel = (modelName || '').toLowerCase().trim();

  // 1. Detect Provider Company
  let provider = 'Custom';
  if (normEndpoint.includes('openrouter.ai') || normModel.includes('openrouter')) {
    provider = 'OpenRouter';
  } else if (normEndpoint.includes('cerebras.ai') || normModel.includes('cerebras')) {
    provider = 'Cerebras';
  } else if (normEndpoint.includes('deepseek.com') || normModel.includes('deepseek')) {
    provider = 'DeepSeek';
  } else if (normEndpoint.includes('generativelanguage.googleapis.com') || normEndpoint.includes('google') || normModel.includes('gemini')) {
    provider = 'Google';
  } else if (normEndpoint.includes('anthropic.com') || normModel.includes('claude')) {
    provider = 'Anthropic';
  } else if (normEndpoint.includes('api.openai.com') || normModel.startsWith('gpt-') || normModel.startsWith('o1') || normModel.startsWith('o3')) {
    provider = 'OpenAI';
  } else if (normEndpoint.includes('groq.com') || normModel.includes('groq')) {
    provider = 'Groq';
  } else if (normEndpoint.includes('sambanova.ai') || normModel.includes('sambanova')) {
    provider = 'SambaNova';
  } else if (normEndpoint.includes('mistral.ai') || normModel.includes('mistral')) {
    provider = 'Mistral';
  } else if (normEndpoint.includes('inference.ai.azure.com') || normEndpoint.includes('github')) {
    provider = 'GitHub';
  } else if (normEndpoint.includes('replicate.com') || normModel.includes('replicate')) {
    provider = 'Replicate';
  } else if (normEndpoint.includes('11434') || normEndpoint.includes('ollama') || normEndpoint.includes('localhost') || normEndpoint.includes('127.0.0.1')) {
    provider = 'LocalOllama';
  } else {
    // Attempt to extract domain name
    try {
      const parsedUrl = new URL(endpointUrl.startsWith('http') ? endpointUrl : `https://${endpointUrl}`);
      const hostParts = parsedUrl.hostname.split('.');
      if (hostParts.length >= 2) {
        const domain = hostParts[hostParts.length - 2];
        provider = domain.charAt(0).toUpperCase() + domain.slice(1);
      }
    } catch {
      provider = 'CustomProvider';
    }
  }

  // 2. Detect Protocol Type
  let protocol = ModelProtocolType.OpenAiCompatible;
  if (provider === 'Google' && !normEndpoint.includes('/openai')) {
    protocol = ModelProtocolType.GoogleGenerativeAi;
  } else if (provider === 'Anthropic' && !normEndpoint.includes('/openai')) {
    protocol = ModelProtocolType.AnthropicMessages;
  }

  // 3. Detect Cognitive Archetype
  let archetype = CognitiveArchetype.DeepReasoner;
  if (
    normModel.includes('reasoner') || 
    normModel.includes('r1') || 
    normModel.includes('o1') || 
    normModel.includes('o3') || 
    normModel.includes('cot') || 
    normModel.includes('thinking') ||
    normModel.includes('405b')
  ) {
    archetype = CognitiveArchetype.DeepReasoner;
  } else if (
    normModel.includes('context') || 
    normModel.includes('1m') || 
    normModel.includes('2m') || 
    normModel.includes('gemini-1.5') || 
    normModel.includes('gemini-2') ||
    normModel.includes('musicgen') ||
    normModel.includes('multimodal')
  ) {
    archetype = CognitiveArchetype.MassiveContext;
  } else if (
    normModel.includes('claude') || 
    normModel.includes('sonnet') || 
    normModel.includes('opus') || 
    normModel.includes('arbiter') ||
    normModel.includes('mistral-large')
  ) {
    archetype = CognitiveArchetype.LinguisticArbiter;
  } else if (
    provider === 'Cerebras' || 
    provider === 'Groq' || 
    normModel.includes('flash') || 
    normModel.includes('mini') || 
    normModel.includes('haiku') || 
    normModel.includes('turbo') || 
    normModel.includes('fast') ||
    normModel.includes('8b') ||
    normModel.includes('27b')
  ) {
    archetype = CognitiveArchetype.UltraFastScanner;
  } else if (provider === 'LocalOllama' || normEndpoint.includes('localhost') || normEndpoint.includes('11434')) {
    archetype = CognitiveArchetype.SovereignLocal;
  } else {
    // Default by provider
    switch (provider) {
      case 'Cerebras':
      case 'Groq':
        archetype = CognitiveArchetype.UltraFastScanner;
        break;
      case 'Anthropic':
      case 'Mistral':
        archetype = CognitiveArchetype.LinguisticArbiter;
        break;
      case 'Google':
        archetype = CognitiveArchetype.MassiveContext;
        break;
      default:
        archetype = CognitiveArchetype.DeepReasoner;
        break;
    }
  }

  // 4. Estimate ELO
  let elo = 1300;
  if (normModel.includes('405b') || normModel.includes('r1') || normModel.includes('o1') || normModel.includes('sonnet')) {
    elo = 1370;
  } else if (normModel.includes('70b') || normModel.includes('gpt-4o') || normModel.includes('mistral-large')) {
    elo = 1340;
  } else if (normModel.includes('27b') || normModel.includes('flash') || normModel.includes('gemini-2.0')) {
    elo = 1310;
  } else if (normModel.includes('8b') || normModel.includes('mini') || normModel.includes('haiku')) {
    elo = 1280;
  } else if (provider === 'LocalOllama') {
    elo = 1260;
  }

  // 5. Estimate SLA Latency (ms)
  let latency = 300;
  if (provider === 'Cerebras') {
    latency = 80;
  } else if (provider === 'Groq') {
    latency = 160;
  } else if (normModel.includes('flash') || normModel.includes('mini') || normModel.includes('27b')) {
    latency = 220;
  } else if (provider === 'DeepSeek') {
    latency = 350;
  } else if (normModel.includes('405b') || normModel.includes('reasoner') || normModel.includes('sonnet')) {
    latency = 550;
  }

  // 6. Max Context Tokens
  let contextTokens = 128000;
  if (provider === 'Google' || normModel.includes('gemini')) {
    contextTokens = 1048576;
  } else if (provider === 'Anthropic' || normModel.includes('claude')) {
    contextTokens = 200000;
  } else if (provider === 'LocalOllama') {
    contextTokens = 32768;
  }

  // 7. Estimate FinOps Pricing (USD per 1M tokens)
  let costIn = 0.50;
  let costOut = 1.00;
  if (provider === 'LocalOllama') {
    costIn = 0.0;
    costOut = 0.0;
  } else if (provider === 'DeepSeek') {
    costIn = 0.14;
    costOut = 0.28;
  } else if (provider === 'Google' || normModel.includes('flash')) {
    costIn = 0.10;
    costOut = 0.40;
  } else if (provider === 'Cerebras') {
    costIn = 0.60;
    costOut = 0.60;
  } else if (provider === 'Groq') {
    costIn = 0.59;
    costOut = 0.79;
  } else if (provider === 'OpenAI' && normModel.includes('mini')) {
    costIn = 0.15;
    costOut = 0.60;
  } else if (normModel.includes('405b')) {
    costIn = 5.00;
    costOut = 10.00;
  } else if (normModel.includes('sonnet')) {
    costIn = 3.00;
    costOut = 15.00;
  }

  // 8. Generate Clean Model Code (slug)
  const cleanModelSlug = normModel
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '') || 'ai-model';
  
  const provPrefix = provider.toLowerCase();
  const modelCode = cleanModelSlug.startsWith(provPrefix) 
    ? cleanModelSlug 
    : `${provPrefix}-${cleanModelSlug}`;

  // 9. Generate Human-Readable Display Name
  // Format words nicely
  const formattedModelName = modelName
    ? modelName
        .split(/[-_/:]/)
        .map(w => w.charAt(0).toUpperCase() + w.slice(1))
        .join(' ')
    : 'Unknown Model';

  let tag = '';
  if (archetype === CognitiveArchetype.DeepReasoner) tag = ' (Deep Reasoning)';
  else if (archetype === CognitiveArchetype.UltraFastScanner) tag = ' (Ultra-Fast)';
  else if (archetype === CognitiveArchetype.MassiveContext) tag = ' (Massive Context)';
  else if (archetype === CognitiveArchetype.LinguisticArbiter) tag = ' (Linguistic Arbiter)';

  const displayName = formattedModelName.toLowerCase().includes(provider.toLowerCase())
    ? `${formattedModelName}${tag}`
    : `${provider} ${formattedModelName}${tag}`;

  return {
    providerCompany: provider,
    protocolType: protocol,
    modelCode,
    displayName,
    cognitiveArchetype: archetype,
    benchmarkElo: elo,
    avgLatencyMs: latency,
    maxContextTokens: contextTokens,
    costInputPerMillion: costIn,
    costOutputPerMillion: costOut
  };
}

export const ARCHETYPE_I18N_KEYS: Record<CognitiveArchetype, { nameKey: string; descKey: string }> = {
  [CognitiveArchetype.DeepReasoner]: { nameKey: 'archetypes.deep_reasoner', descKey: 'archetypes.deep_reasoner_desc' },
  [CognitiveArchetype.MassiveContext]: { nameKey: 'archetypes.massive_context', descKey: 'archetypes.massive_context_desc' },
  [CognitiveArchetype.LinguisticArbiter]: { nameKey: 'archetypes.linguistic_arbiter', descKey: 'archetypes.linguistic_arbiter_desc' },
  [CognitiveArchetype.UltraFastScanner]: { nameKey: 'archetypes.ultra_fast_scanner', descKey: 'archetypes.ultra_fast_scanner_desc' },
  [CognitiveArchetype.SovereignLocal]: { nameKey: 'archetypes.sovereign_local', descKey: 'archetypes.sovereign_local_desc' },
};

export const ModelUpsertModal: React.FC<ModelUpsertModalProps> = ({
  isOpen,
  onClose,
  modelToEdit,
  onSaved
}) => {
  const { t } = useTranslation('ai_settings');
  const isEdit = Boolean(modelToEdit);

  // Tab mode: 'manual' (Direct Endpoint & Model) vs 'curl' (Paste cURL command)
  const [inputMode, setInputMode] = useState<'manual' | 'curl'>('manual');
  const [curlInput, setCurlInput] = useState('');
  const [showAdvanced, setShowAdvanced] = useState(false);

  // Core essential inputs
  const [endpointUrl, setEndpointUrl] = useState('');
  const [modelName, setModelName] = useState('');

  // 4D Epistemic Inferred Parameters (with manual override capability)
  const [form, setForm] = useState<UpsertAiModelRequest>({
    id: undefined,
    modelCode: '',
    displayName: '',
    providerCompany: 'Cerebras',
    protocolType: ModelProtocolType.OpenAiCompatible,
    cognitiveArchetype: CognitiveArchetype.UltraFastScanner,
    endpointUrl: '',
    modelName: '',
    maxContextTokens: 128000,
    costInputPerMillion: 0.60,
    costOutputPerMillion: 0.60,
    benchmarkElo: 1300,
    avgLatencyMs: 80,
    isEnabled: true,
    isDefaultInArchetype: false
  });

  const [saving, setSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Initialize or reset form
  useEffect(() => {
    if (modelToEdit) {
      setEndpointUrl(modelToEdit.endpointUrl);
      setModelName(modelToEdit.modelName);
      setForm({
        id: modelToEdit.id,
        modelCode: modelToEdit.modelCode,
        displayName: modelToEdit.displayName,
        providerCompany: modelToEdit.providerCompany,
        protocolType: modelToEdit.protocolType,
        cognitiveArchetype: modelToEdit.cognitiveArchetype,
        endpointUrl: modelToEdit.endpointUrl,
        modelName: modelToEdit.modelName,
        maxContextTokens: modelToEdit.maxContextTokens,
        costInputPerMillion: modelToEdit.costInputPerMillion,
        costOutputPerMillion: modelToEdit.costOutputPerMillion,
        benchmarkElo: modelToEdit.benchmarkElo,
        avgLatencyMs: modelToEdit.avgLatencyMs,
        isEnabled: modelToEdit.isEnabled,
        isDefaultInArchetype: modelToEdit.isDefaultInArchetype
      });
      setShowAdvanced(true); // show advanced when editing
    } else {
      // Default initial state
      const defaultEndpoint = 'https://api.cerebras.ai/v1';
      const defaultModel = 'qwen-3.8-27b';
      setEndpointUrl(defaultEndpoint);
      setModelName(defaultModel);
      setCurlInput('');
      setShowAdvanced(false);

      const inferred = inferModelSpecifications(defaultEndpoint, defaultModel);
      setForm({
        id: undefined,
        endpointUrl: defaultEndpoint,
        modelName: defaultModel,
        modelCode: inferred.modelCode,
        displayName: inferred.displayName,
        providerCompany: inferred.providerCompany,
        protocolType: inferred.protocolType,
        cognitiveArchetype: inferred.cognitiveArchetype,
        benchmarkElo: inferred.benchmarkElo,
        avgLatencyMs: inferred.avgLatencyMs,
        maxContextTokens: inferred.maxContextTokens,
        costInputPerMillion: inferred.costInputPerMillion,
        costOutputPerMillion: inferred.costOutputPerMillion,
        isEnabled: true,
        isDefaultInArchetype: false
      });
    }
    setErrorMessage(null);
  }, [modelToEdit, isOpen]);

  // Predictive Auto-Inference triggered when Endpoint or Model Name changes
  const handleAutoInfer = (newEndpoint: string, newModel: string, forceUpdateCodeAndName = true) => {
    const cleanEndpoint = normalizeEndpointUrl(newEndpoint);
    const cleanModel = (newModel || '').trim();

    if (!cleanEndpoint && !cleanModel) return;

    const inferred = inferModelSpecifications(cleanEndpoint, cleanModel);

    setForm(prev => ({
      ...prev,
      endpointUrl: cleanEndpoint,
      modelName: cleanModel,
      providerCompany: inferred.providerCompany,
      protocolType: inferred.protocolType,
      cognitiveArchetype: inferred.cognitiveArchetype,
      benchmarkElo: inferred.benchmarkElo,
      avgLatencyMs: inferred.avgLatencyMs,
      maxContextTokens: inferred.maxContextTokens,
      costInputPerMillion: inferred.costInputPerMillion,
      costOutputPerMillion: inferred.costOutputPerMillion,
      modelCode: forceUpdateCodeAndName || !prev.modelCode ? inferred.modelCode : prev.modelCode,
      displayName: forceUpdateCodeAndName || !prev.displayName ? inferred.displayName : prev.displayName
    }));
  };

  const handleEndpointChange = (val: string) => {
    setEndpointUrl(val);
    handleAutoInfer(val, modelName, !isEdit);
  };

  const handleModelNameChange = (val: string) => {
    setModelName(val);
    handleAutoInfer(endpointUrl, val, !isEdit);
  };

  // cURL parser trigger
  const handleParseCurl = () => {
    if (!curlInput.trim()) return;
    const parsed = parseCurlCommand(curlInput);
    if (parsed.endpointUrl || parsed.modelName) {
      const newEp = normalizeEndpointUrl(parsed.endpointUrl || endpointUrl);
      const newM = parsed.modelName || modelName;
      setEndpointUrl(newEp);
      setModelName(newM);
      handleAutoInfer(newEp, newM, true);
      setInputMode('manual');
      setErrorMessage(null);
    } else {
      setErrorMessage(t('model_upsert.curl_parse_error'));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const cleanEndpoint = normalizeEndpointUrl(endpointUrl);
    const cleanModel = modelName.trim();

    if (!cleanEndpoint || !cleanModel) {
      setErrorMessage(t('model_upsert.err_required'));
      return;
    }

    // Ensure inferred fields are fully valid before submitting
    const inferred = inferModelSpecifications(cleanEndpoint, cleanModel);
    const finalRequest: UpsertAiModelRequest = {
      id: form.id,
      modelCode: form.modelCode || inferred.modelCode,
      displayName: form.displayName || inferred.displayName,
      providerCompany: form.providerCompany || inferred.providerCompany,
      protocolType: form.protocolType || inferred.protocolType,
      cognitiveArchetype: form.cognitiveArchetype || inferred.cognitiveArchetype,
      endpointUrl: cleanEndpoint,
      modelName: cleanModel,
      maxContextTokens: form.maxContextTokens || inferred.maxContextTokens,
      costInputPerMillion: form.costInputPerMillion >= 0 ? form.costInputPerMillion : inferred.costInputPerMillion,
      costOutputPerMillion: form.costOutputPerMillion >= 0 ? form.costOutputPerMillion : inferred.costOutputPerMillion,
      benchmarkElo: form.benchmarkElo || inferred.benchmarkElo,
      avgLatencyMs: form.avgLatencyMs || inferred.avgLatencyMs,
      isEnabled: form.isEnabled,
      isDefaultInArchetype: form.isDefaultInArchetype
    };

    setSaving(true);
    setErrorMessage(null);
    try {
      const res = await aiSwarmService.upsertModel(finalRequest);
      onSaved(res);
      onClose();
    } catch (err: any) {
      setErrorMessage(err?.response?.data?.message || err?.message || t('model_upsert.err_save'));
    } finally {
      setSaving(false);
    }
  };

  if (!isOpen) return null;

  const archMeta = ARCHETYPE_LABELS[form.cognitiveArchetype] || ARCHETYPE_LABELS[CognitiveArchetype.DeepReasoner];
  const localizedArchetypeName = t(ARCHETYPE_I18N_KEYS[form.cognitiveArchetype]?.nameKey) || archMeta.name;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-md overflow-y-auto">
      <div className="relative w-full max-w-2xl my-8 rounded-3xl bg-slate-900 border border-slate-800 shadow-2xl p-6 text-slate-100 max-h-[92vh] overflow-y-auto">
        
        {/* Header */}
        <div className="flex items-center justify-between pb-4 border-b border-slate-800">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-indigo-600/20 border border-indigo-500/30 flex items-center justify-center text-indigo-400">
              <Database className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-white flex items-center gap-2">
                <span>{isEdit ? t('model_upsert.title_edit') : t('model_upsert.title_create')}</span>
                <span className="px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 text-[10px] font-bold border border-emerald-500/30 flex items-center gap-1">
                  <Sparkles className="w-3 h-3" />
                  {t('model_upsert.smart_infer_badge')}
                </span>
              </h3>
              <p className="text-xs text-slate-400">{t('model_upsert.subtitle')}</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-400 hover:text-white transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {errorMessage && (
          <div className="mt-4 p-3 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-300 flex items-center gap-2 text-xs">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{errorMessage}</span>
          </div>
        )}

        {/* Input Mode Selector */}
        {!isEdit && (
          <div className="mt-4 flex items-center gap-2 p-1 rounded-xl bg-slate-950 border border-slate-800 w-fit text-xs">
            <button
              type="button"
              onClick={() => setInputMode('manual')}
              className={`px-3 py-1.5 rounded-lg font-bold transition-all ${
                inputMode === 'manual' 
                  ? 'bg-indigo-600 text-white shadow-sm' 
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              {t('model_upsert.manual_tab')}
            </button>
            <button
              type="button"
              onClick={() => setInputMode('curl')}
              className={`px-3 py-1.5 rounded-lg font-bold transition-all flex items-center gap-1.5 ${
                inputMode === 'curl' 
                  ? 'bg-indigo-600 text-white shadow-sm' 
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <Terminal className="w-3.5 h-3.5" />
              <span>{t('model_upsert.curl_tab')}</span>
            </button>
          </div>
        )}

        {/* cURL Fast-Import Area */}
        {inputMode === 'curl' && !isEdit && (
          <div className="mt-4 p-4 rounded-2xl bg-slate-950 border border-indigo-500/30 space-y-3">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-indigo-300 flex items-center gap-1.5">
                <Terminal className="w-4 h-4" />
                {t('model_upsert.curl_label')}
              </span>
            </div>
            <textarea
              rows={4}
              value={curlInput}
              onChange={(e) => setCurlInput(e.target.value)}
              placeholder={t('model_upsert.curl_placeholder')}
              className="w-full bg-slate-900 border border-slate-800 rounded-xl p-3 text-xs text-indigo-200 font-mono focus:outline-none focus:border-indigo-500"
            />
            <button
              type="button"
              onClick={handleParseCurl}
              className="px-4 py-2 rounded-xl bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white text-xs font-bold transition-all shadow-md flex items-center gap-1.5"
            >
              <Sparkles className="w-3.5 h-3.5" />
              {t('model_upsert.parse_curl_btn')}
            </button>
          </div>
        )}

        <form onSubmit={handleSubmit} className="mt-5 space-y-5 text-xs">
          
          {/* ── ZONE 1: ESSENTIAL USER INPUTS (CHỈ CẦN 2 TRƯỜNG NÀY) ── */}
          <div className="p-4 rounded-2xl bg-slate-950/80 border border-indigo-500/40 space-y-4 shadow-inner">
            <div className="flex items-center justify-between">
              <h4 className="text-xs font-bold uppercase tracking-wider text-indigo-400 flex items-center gap-2">
                <Zap className="w-4 h-4 text-amber-400" />
                {t('model_upsert.essential_header')}
              </h4>
              <button
                type="button"
                onClick={() => handleAutoInfer(endpointUrl, modelName, true)}
                className="text-[11px] text-indigo-300 hover:text-indigo-200 underline flex items-center gap-1 font-semibold"
              >
                <Sparkles className="w-3 h-3 text-amber-300" />
                {t('model_upsert.re_infer_btn')}
              </button>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="text-slate-200 font-bold block mb-1">
                  {t('model_upsert.endpoint_label')} *
                </label>
                <input
                  type="text"
                  required
                  placeholder="https://api.cerebras.ai/v1"
                  value={endpointUrl}
                  onChange={(e) => handleEndpointChange(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-700 rounded-xl px-3 py-2.5 text-white font-mono text-[11px] focus:outline-none focus:border-indigo-500 shadow-sm"
                />
                <span className="text-[10px] text-slate-400 mt-1 block">
                  {t('model_upsert.endpoint_hint')}
                </span>
              </div>

              <div>
                <label className="text-slate-200 font-bold block mb-1">
                  {t('model_upsert.model_name_label')} *
                </label>
                <input
                  type="text"
                  required
                  placeholder={t('model_upsert.model_name_placeholder')}
                  value={modelName}
                  onChange={(e) => handleModelNameChange(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-700 rounded-xl px-3 py-2.5 text-white font-mono text-[11px] focus:outline-none focus:border-indigo-500 shadow-sm font-semibold"
                />
                <span className="text-[10px] text-slate-400 mt-1 block">
                  {t('model_upsert.model_name_hint')}
                </span>
              </div>
            </div>
          </div>

          {/* ── ZONE 2: LIVE SMART INFERRED PREVIEW CARD ── */}
          <div className="p-4 rounded-2xl bg-gradient-to-br from-indigo-950/40 via-purple-950/20 to-slate-900 border border-indigo-500/20 space-y-3">
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-bold text-indigo-300 flex items-center gap-1.5">
                <Sparkles className="w-3.5 h-3.5 text-amber-400 animate-pulse" />
                {t('model_upsert.smart_inferred_banner')}
              </span>
              <span className="text-[11px] px-2 py-0.5 rounded-full bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 font-bold font-mono">
                {form.providerCompany}
              </span>
            </div>

            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5 text-[11px]">
              <div className="p-2.5 rounded-xl bg-slate-950/60 border border-slate-800">
                <span className="text-slate-400 block text-[10px]">{t('model_upsert.inferred_archetype')}</span>
                <span 
                  className="font-bold inline-block mt-0.5"
                  style={{ color: archMeta.badgeColor }}
                >
                  {localizedArchetypeName}
                </span>
              </div>

              <div className="p-2.5 rounded-xl bg-slate-950/60 border border-slate-800">
                <span className="text-slate-400 block text-[10px]">{t('model_upsert.inferred_elo')}</span>
                <span className="font-bold text-amber-400 font-mono mt-0.5 block">{form.benchmarkElo} ELO</span>
              </div>

              <div className="p-2.5 rounded-xl bg-slate-950/60 border border-slate-800">
                <span className="text-slate-400 block text-[10px]">{t('model_upsert.inferred_latency')}</span>
                <span className="font-bold text-emerald-400 font-mono mt-0.5 block">{form.avgLatencyMs} ms</span>
              </div>

              <div className="p-2.5 rounded-xl bg-slate-950/60 border border-slate-800">
                <span className="text-slate-400 block text-[10px]">{t('model_upsert.inferred_cost')}</span>
                <span className="font-bold text-slate-200 font-mono mt-0.5 block">
                  ${form.costInputPerMillion} / ${form.costOutputPerMillion}
                </span>
              </div>
            </div>

            <div className="text-[11px] text-slate-300 bg-slate-950/40 p-2.5 rounded-xl border border-slate-800 flex items-center justify-between">
              <div>
                <span className="text-slate-400">{t('model_upsert.preview_display_name')} </span>
                <span className="font-bold text-white">{form.displayName || t('model_upsert.preview_unassigned')}</span>
              </div>
              <div>
                <span className="text-slate-400">{t('model_upsert.preview_code')} </span>
                <span className="font-mono text-indigo-300">{form.modelCode || t('model_upsert.preview_not_created')}</span>
              </div>
            </div>
          </div>

          {/* ── ZONE 3: ADVANCED 4D EPISTEMIC ACCORDION (TÙY CHỈNH CHUYÊN SÂU) ── */}
          <div className="border border-slate-800 rounded-2xl overflow-hidden bg-slate-950/40">
            <button
              type="button"
              onClick={() => setShowAdvanced(!showAdvanced)}
              className="w-full flex items-center justify-between p-3.5 text-xs font-bold text-slate-300 hover:text-white transition-colors bg-slate-900/60"
            >
              <span className="flex items-center gap-2">
                <Cpu className="w-4 h-4 text-purple-400" />
                {showAdvanced ? t('model_upsert.advanced_toggle_hide') : t('model_upsert.advanced_toggle_show')}
              </span>
              {showAdvanced ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
            </button>

            {showAdvanced && (
              <div className="p-4 space-y-4 border-t border-slate-800 bg-slate-950/70">
                {/* Identifiers override */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label className="text-slate-400 block mb-1">{t('model_upsert.field_model_code')}</label>
                    <input
                      type="text"
                      disabled={isEdit}
                      value={form.modelCode}
                      onChange={(e) => setForm({ ...form, modelCode: e.target.value })}
                      className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-white font-mono text-xs focus:outline-none focus:border-indigo-500 disabled:opacity-50"
                    />
                  </div>
                  <div>
                    <label className="text-slate-400 block mb-1">{t('model_upsert.field_display_name')}</label>
                    <input
                      type="text"
                      value={form.displayName}
                      onChange={(e) => setForm({ ...form, displayName: e.target.value })}
                      className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-white text-xs focus:outline-none focus:border-indigo-500"
                    />
                  </div>
                </div>

                {/* Provider, Archetype & Protocol override */}
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                  <div>
                    <label className="text-slate-400 block mb-1">{t('model_upsert.field_provider')}</label>
                    <input
                      type="text"
                      value={form.providerCompany}
                      onChange={(e) => setForm({ ...form, providerCompany: e.target.value })}
                      className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-white text-xs focus:outline-none focus:border-indigo-500"
                    />
                  </div>
                  <div>
                    <label className="text-slate-400 block mb-1">{t('model_upsert.field_archetype')}</label>
                    <select
                      value={form.cognitiveArchetype}
                      onChange={(e) => setForm({ ...form, cognitiveArchetype: Number(e.target.value) })}
                      className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-white text-xs focus:outline-none focus:border-indigo-500"
                    >
                      <option value={CognitiveArchetype.DeepReasoner}>1. {t('archetypes.deep_reasoner')}</option>
                      <option value={CognitiveArchetype.MassiveContext}>2. {t('archetypes.massive_context')}</option>
                      <option value={CognitiveArchetype.LinguisticArbiter}>3. {t('archetypes.linguistic_arbiter')}</option>
                      <option value={CognitiveArchetype.UltraFastScanner}>4. {t('archetypes.ultra_fast_scanner')}</option>
                      <option value={CognitiveArchetype.SovereignLocal}>5. {t('archetypes.sovereign_local')}</option>
                    </select>
                  </div>
                  <div>
                    <label className="text-slate-400 block mb-1">{t('model_upsert.field_protocol')}</label>
                    <select
                      value={form.protocolType}
                      onChange={(e) => setForm({ ...form, protocolType: Number(e.target.value) })}
                      className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-white text-xs focus:outline-none focus:border-indigo-500"
                    >
                      <option value={ModelProtocolType.OpenAiCompatible}>{t('model_upsert.protocols.openai')}</option>
                      <option value={ModelProtocolType.GoogleGenerativeAi}>{t('model_upsert.protocols.google')}</option>
                      <option value={ModelProtocolType.AnthropicMessages}>{t('model_upsert.protocols.anthropic')}</option>
                    </select>
                  </div>
                </div>

                {/* ELO, Latency, Context override */}
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                  <div>
                    <label className="text-slate-400 block mb-1">{t('model_upsert.field_elo')}</label>
                    <input
                      type="number"
                      value={form.benchmarkElo}
                      onChange={(e) => setForm({ ...form, benchmarkElo: Number(e.target.value) })}
                      className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-white font-mono text-xs focus:outline-none focus:border-indigo-500"
                    />
                  </div>
                  <div>
                    <label className="text-slate-400 block mb-1">{t('model_upsert.field_latency')}</label>
                    <input
                      type="number"
                      value={form.avgLatencyMs}
                      onChange={(e) => setForm({ ...form, avgLatencyMs: Number(e.target.value) })}
                      className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-white font-mono text-xs focus:outline-none focus:border-indigo-500"
                    />
                  </div>
                  <div>
                    <label className="text-slate-400 block mb-1">{t('model_upsert.field_max_context')}</label>
                    <input
                      type="number"
                      value={form.maxContextTokens}
                      onChange={(e) => setForm({ ...form, maxContextTokens: Number(e.target.value) })}
                      className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-white font-mono text-xs focus:outline-none focus:border-indigo-500"
                    />
                  </div>
                </div>

                {/* Pricing override */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label className="text-slate-400 block mb-1">{t('model_upsert.field_cost_in')}</label>
                    <input
                      type="number"
                      step="0.01"
                      value={form.costInputPerMillion}
                      onChange={(e) => setForm({ ...form, costInputPerMillion: Number(e.target.value) })}
                      className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-white font-mono text-xs focus:outline-none focus:border-indigo-500"
                    />
                  </div>
                  <div>
                    <label className="text-slate-400 block mb-1">{t('model_upsert.field_cost_out')}</label>
                    <input
                      type="number"
                      step="0.01"
                      value={form.costOutputPerMillion}
                      onChange={(e) => setForm({ ...form, costOutputPerMillion: Number(e.target.value) })}
                      className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-white font-mono text-xs focus:outline-none focus:border-indigo-500"
                    />
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Checkboxes */}
          <div className="flex flex-wrap items-center gap-6 pt-1">
            <label className="flex items-center gap-2 cursor-pointer select-none">
              <input
                type="checkbox"
                checked={form.isEnabled}
                onChange={(e) => setForm({ ...form, isEnabled: e.target.checked })}
                className="rounded border-slate-700 bg-slate-950 text-indigo-600 focus:ring-indigo-500"
              />
              <span className="text-slate-300 font-medium">{t('model_upsert.enable_immediately')}</span>
            </label>

            <label className="flex items-center gap-2 cursor-pointer select-none">
              <input
                type="checkbox"
                checked={form.isDefaultInArchetype}
                onChange={(e) => setForm({ ...form, isDefaultInArchetype: e.target.checked })}
                className="rounded border-slate-700 bg-slate-950 text-indigo-600 focus:ring-indigo-500"
              />
              <span className="text-slate-300 font-medium">{t('model_upsert.default_leader_checkbox')}</span>
            </label>
          </div>

          {/* Action Buttons */}
          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-800">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 transition-colors"
            >
              {t('model_upsert.btn_cancel')}
            </button>
            <button
              type="submit"
              disabled={saving}
              className="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 hover:from-indigo-500 hover:to-pink-500 text-white font-bold transition-all shadow-lg shadow-indigo-600/30 disabled:opacity-50"
            >
              <Sparkles className="w-4 h-4" />
              {saving ? t('model_upsert.btn_saving') : t('model_upsert.btn_save')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ModelUpsertModal;
