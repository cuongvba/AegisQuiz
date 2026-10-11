import { 
  Sparkles, Activity, Trash2, Info 
} from 'lucide-react';
import { 
  AiModelRegistryDto, 
  CognitiveArchetype, 
  ARCHETYPE_LABELS 
} from '@/services/ai-swarm.service';
import { ARCHETYPE_I18N_KEYS } from '@/features/admin/ai-settings/components/ModelUpsertModal';

interface SwarmRegistryTabProps {
  models: AiModelRegistryDto[];
  loadingModels: boolean;
  selectedProviderFilter: string;
  setSelectedProviderFilter: (v: string) => void;
  modelSearchQuery: string;
  setModelSearchQuery: (v: string) => void;
  pingingModelCode: string | null;
  modelPingResults: Record<string, { success: boolean; latencyMs: number; message: string }>;
  handlePingModel: (m: AiModelRegistryDto) => void;
  handleDeleteModel: (id: string, name: string) => void;
  setSelectedModelForDetail: (m: AiModelRegistryDto) => void;
  isAdmin: boolean;
  t: (key: string, opts?: any) => string;
}

export function SwarmRegistryTab({
  models,
  selectedProviderFilter,
  setSelectedProviderFilter,
  modelSearchQuery,
  setModelSearchQuery,
  pingingModelCode,
  modelPingResults,
  handlePingModel,
  handleDeleteModel,
  setSelectedModelForDetail,
  isAdmin,
  t,
}: SwarmRegistryTabProps) {
  const providerList = Array.from(new Set(models.map(m => m.providerCompany))).filter(Boolean);
  const filteredModels = models.filter(m => {
    const matchesProvider = selectedProviderFilter === 'ALL' || m.providerCompany.toLowerCase() === selectedProviderFilter.toLowerCase();
    const query = modelSearchQuery.trim().toLowerCase();
    const matchesSearch = !query || 
      m.displayName.toLowerCase().includes(query) || 
      m.modelCode.toLowerCase().includes(query) || 
      m.providerCompany.toLowerCase().includes(query) ||
      m.modelName.toLowerCase().includes(query);
    return matchesProvider && matchesSearch;
  });

  return (
    <div className="space-y-6">
      {/* 5 Cognitive Archetypes Guide Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-5 gap-3">
        {[
          CognitiveArchetype.DeepReasoner,
          CognitiveArchetype.MassiveContext,
          CognitiveArchetype.LinguisticArbiter,
          CognitiveArchetype.UltraFastScanner,
          CognitiveArchetype.SovereignLocal,
        ].map(archetype => {
          const meta = ARCHETYPE_LABELS[archetype];
          const count = models.filter(m => m.cognitiveArchetype === archetype).length;
          const localizedName = t(ARCHETYPE_I18N_KEYS[archetype]?.nameKey) || meta.name;
          const localizedDesc = t(ARCHETYPE_I18N_KEYS[archetype]?.descKey) || meta.desc;
          return (
            <div 
              key={archetype}
              className="p-4 rounded-2xl bg-slate-900/80 border border-slate-800 hover:border-slate-700 transition-all space-y-2"
            >
              <div className="flex items-center justify-between">
                <span 
                  className="px-2 py-0.5 rounded-full text-[10px] font-bold"
                  style={{ backgroundColor: `${meta.badgeColor}20`, color: meta.badgeColor, border: `1px solid ${meta.badgeColor}40` }}
                >
                  {t('archetypes.archetype_badge', { number: archetype })}
                </span>
                <span className="text-xs font-bold text-white font-mono bg-slate-800 px-2 py-0.5 rounded-md">
                  {t('archetypes.models_count', { count })}
                </span>
              </div>
              <h4 className="text-xs font-bold text-white leading-tight">{localizedName}</h4>
              <p className="text-[11px] text-slate-400 leading-relaxed">{localizedDesc}</p>
            </div>
          );
        })}
      </div>

      {/* Model Registry Cards List */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-bold text-white flex items-center gap-2">
            <Sparkles className="w-5 h-5 text-indigo-400" />
            {t('model_catalog.title')}
          </h2>
          <span className="text-xs text-slate-400">
            {t('model_catalog.subtitle')}
          </span>
        </div>

        {/* Provider Filter Bar & Search */}
        <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-3 p-3.5 rounded-2xl bg-slate-900/90 border border-slate-800">
          <div className="flex items-center gap-1.5 flex-wrap">
            <button
              onClick={() => setSelectedProviderFilter('ALL')}
              className={`px-3 py-1.5 rounded-xl text-xs font-bold transition-all ${
                selectedProviderFilter === 'ALL'
                  ? 'bg-gradient-to-r from-indigo-600 to-purple-600 text-white shadow-md'
                  : 'text-slate-400 hover:text-white bg-slate-950/70 border border-slate-800'
              }`}
            >
              {t('model_catalog.all_providers')} ({models.length})
            </button>
            {providerList.map(prov => {
              const count = models.filter(m => m.providerCompany.toLowerCase() === prov.toLowerCase()).length;
              const isActive = selectedProviderFilter.toLowerCase() === prov.toLowerCase();
              return (
                <button
                  key={prov}
                  onClick={() => setSelectedProviderFilter(prov)}
                  className={`px-3 py-1.5 rounded-xl text-xs font-bold transition-all flex items-center gap-1.5 ${
                    isActive
                      ? 'bg-indigo-600 text-white shadow-md'
                      : 'text-slate-400 hover:text-white bg-slate-950/70 border border-slate-800'
                  }`}
                >
                  <span>{prov}</span>
                  <span className={`text-[10px] px-1.5 py-0.2 rounded-full font-mono ${isActive ? 'bg-white/20 text-white' : 'bg-slate-800 text-slate-300'}`}>
                    {count}
                  </span>
                </button>
              );
            })}
          </div>

          <div className="w-full md:w-64">
            <input
              type="text"
              placeholder={t('model_catalog.search_placeholder')}
              value={modelSearchQuery}
              onChange={(e) => setModelSearchQuery(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-1.5 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-indigo-500"
            />
          </div>
        </div>

        {/* Models Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredModels.map(m => {
            const archMeta = ARCHETYPE_LABELS[m.cognitiveArchetype] || ARCHETYPE_LABELS[CognitiveArchetype.DeepReasoner];
            const localizedArchName = t(ARCHETYPE_I18N_KEYS[m.cognitiveArchetype]?.nameKey) || archMeta.name;
            const ping = modelPingResults[m.modelCode];
            return (
              <div 
                key={m.id}
                className={`p-5 rounded-2xl border transition-all flex flex-col justify-between ${
                  m.isEnabled 
                    ? 'bg-slate-900/90 border-slate-800 hover:border-indigo-500/50 shadow-md' 
                    : 'bg-slate-950/40 border-slate-800/40 opacity-60'
                }`}
              >
                <div>
                  <div className="flex items-start justify-between pb-3 border-b border-slate-800">
                    <div>
                      <div className="flex items-center gap-2 mb-1">
                        <h3 className="font-bold text-white text-sm">{m.displayName}</h3>
                        {m.isDefaultInArchetype && (
                          <span className="text-[10px] px-1.5 py-0.2 rounded bg-amber-500/20 text-amber-300 border border-amber-500/30">
                            {t('model_catalog.alpha_badge')}
                          </span>
                        )}
                      </div>
                      <div className="flex items-center gap-2">
                        <span className="text-xs text-slate-400 font-mono">{m.modelCode}</span>
                        <span className="text-[10px] px-1.5 py-0.2 rounded bg-slate-800 text-indigo-300 font-semibold border border-slate-700">
                          {m.providerCompany}
                        </span>
                      </div>
                    </div>

                    <div className="flex items-center gap-1.5">
                      <button
                        onClick={() => handlePingModel(m)}
                        disabled={pingingModelCode === m.modelCode}
                        className="p-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs transition-colors border border-slate-700"
                        title={t('model_catalog.btn_ping')}
                      >
                        <Activity className={`w-3.5 h-3.5 text-emerald-400 ${pingingModelCode === m.modelCode ? 'animate-spin' : ''}`} />
                      </button>
                      {isAdmin && (
                        <button
                          onClick={() => handleDeleteModel(m.id, m.displayName)}
                          className="p-1.5 rounded-lg bg-rose-950/40 hover:bg-rose-900/60 text-rose-300 text-xs transition-colors border border-rose-900/30"
                          title={t('model_catalog.btn_delete')}
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      )}
                    </div>
                  </div>

                  <div className="pt-3 space-y-2.5 text-xs">
                    <div className="flex items-center justify-between">
                      <span className="text-slate-400">{t('model_catalog.card_archetype')}</span>
                      <span 
                        className="px-2 py-0.5 rounded-md text-[11px] font-semibold"
                        style={{ backgroundColor: `${archMeta.badgeColor}20`, color: archMeta.badgeColor }}
                      >
                        {localizedArchName}
                      </span>
                    </div>

                    <div className="flex items-center justify-between">
                      <span className="text-slate-400">{t('model_catalog.card_protocol')}</span>
                      <span className="font-mono text-slate-300">
                        {m.protocolType === 1 ? 'OpenAI-Compatible' : m.protocolType === 2 ? 'Google Native' : 'Anthropic Native'}
                      </span>
                    </div>

                    <div className="flex items-center justify-between">
                      <span className="text-slate-400">{t('model_catalog.card_elo')}</span>
                      <span className="font-bold text-amber-400">{m.benchmarkElo} ELO</span>
                    </div>

                    <div className="flex items-center justify-between">
                      <span className="text-slate-400">{t('model_catalog.card_context')}</span>
                      <span className="font-mono text-slate-300">{(m.maxContextTokens / 1000).toFixed(0)}k tokens</span>
                    </div>

                    <div className="flex items-center justify-between">
                      <span className="text-slate-400">{t('model_catalog.card_cost')}</span>
                      <span className="font-mono text-emerald-400">${m.costInputPerMillion} in / ${m.costOutputPerMillion} out</span>
                    </div>

                    {ping && (
                      <div className={`p-2 rounded-lg text-[11px] flex items-center justify-between ${
                        ping.success ? 'bg-emerald-950/40 text-emerald-300 border border-emerald-800' : 'bg-rose-950/40 text-rose-300 border border-rose-800'
                      }`}>
                        <span>{ping.message}</span>
                        <span className="font-mono font-bold">{ping.latencyMs}ms</span>
                      </div>
                    )}
                  </div>
                </div>

                {/* Action Button: Chi Tiết */}
                <div className="pt-4 border-t border-slate-800/80 mt-3">
                  <button
                    onClick={() => setSelectedModelForDetail(m)}
                    className="w-full flex items-center justify-center gap-1.5 py-2 px-3 rounded-xl bg-indigo-600/20 hover:bg-indigo-600/30 text-indigo-300 border border-indigo-500/30 text-xs font-bold transition-all shadow-sm group"
                  >
                    <Info className="w-3.5 h-3.5 transition-transform group-hover:scale-110" />
                    <span>{t('model_catalog.btn_detail')}</span>
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
