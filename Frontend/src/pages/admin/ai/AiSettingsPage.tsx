import React from 'react';
import { useTranslation } from 'react-i18next';
import { 
  Brain, Zap, Plus, RefreshCw, Layers, Sparkles 
} from 'lucide-react';
import { CouncilDetailModal } from '@/features/symposium/components/CouncilDetailModal';
import { SeatSwarmConfigModal } from '@/features/symposium/components/SeatSwarmConfigModal';
import { AddCouncilSeatModal } from '@/features/symposium/components/AddCouncilSeatModal';
import { EditCouncilModal } from '@/features/symposium/components/EditCouncilModal';
import { ModelDetailModal } from '@/features/admin/ai-settings/components/ModelDetailModal';
import { ModelUpsertModal } from '@/features/admin/ai-settings/components/ModelUpsertModal';
import { GatewayCatalogModal } from '@/features/admin/ai-settings/components/GatewayCatalogModal';
import { useAuth } from '@/hooks/useAuth';

import { useAiSettingsState } from './components/useAiSettingsState';
import { SwarmRegistryTab } from './components/SwarmRegistryTab';
import { UniversalSymposiumTab } from './components/UniversalSymposiumTab';
import { ConveneSymposiumModal } from './components/ConveneSymposiumModal';
import { SymposiumProceedingsCard } from './components/SymposiumProceedingsCard';

/**
 * AiSettingsPage — Universal AI Swarm Orchestrator (Zero-Monolith & OCP Compliant)
 * Phân rã từ 1,259 dòng code thành Orchestrator tinh giản <= 250 LOC theo chuẩn AGENTS.md.
 */
export const AiSettingsPage: React.FC = () => {
  const { t } = useTranslation('ai_settings');
  const { isAdmin } = useAuth();
  const state = useAiSettingsState();

  if (state.loading && state.loadingModels) {
    return (
      <div className="p-8 flex flex-col items-center justify-center min-h-[60vh] space-y-3">
        <RefreshCw className="w-8 h-8 text-indigo-400 animate-spin" />
        <p className="text-slate-400 text-sm">{t('loading_orchestrator')}</p>
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
              {t('title')}
              <span className="text-xs px-2.5 py-0.5 rounded-full bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 font-semibold">
                {t('sovereign_badge')}
              </span>
            </h1>
            <p className="text-sm text-slate-400">
              {t('subtitle')}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={() => state.setShowGatewayModal(true)}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-gradient-to-r from-amber-500/20 to-orange-500/20 hover:from-amber-500/30 hover:to-orange-500/30 text-amber-300 border border-amber-500/30 text-sm font-bold transition-all shadow-lg shadow-amber-500/10 cursor-pointer"
          >
            <Zap className="w-4 h-4 text-amber-400" />
            {t('buttons.gateway_quick_import', '⚡ Nạp Nhanh Gateway')}
          </button>
          <button
            onClick={() => { state.setSelectedModelToEdit(null); state.setShowUpsertModal(true); }}
            className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white text-sm font-bold transition-all shadow-lg shadow-indigo-600/30 cursor-pointer"
          >
            <Plus className="w-4 h-4" />
            {t('buttons.register_model')}
          </button>
        </div>
      </div>

      {/* Tab Selector */}
      <div className="flex items-center gap-2 p-1.5 rounded-2xl bg-slate-900 border border-slate-800 w-fit flex-wrap">
        <button
          onClick={() => state.setCurrentTab('swarm-registry')}
          className={`flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-bold transition-all cursor-pointer ${
            state.currentTab === 'swarm-registry'
              ? 'bg-gradient-to-r from-indigo-600 to-purple-600 text-white shadow-md'
              : 'text-slate-400 hover:text-white'
          }`}
        >
          <Layers className="w-4 h-4" />
          {t('tabs.model_catalog')}
          <span className="px-1.5 py-0.5 rounded-full bg-white/20 text-white text-[10px]">
            {t('tabs.models_count', { count: state.models.length })}
          </span>
        </button>

        <button
          onClick={() => state.setCurrentTab('universal-symposium')}
          className={`flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-bold transition-all cursor-pointer ${
            state.currentTab === 'universal-symposium'
              ? 'bg-gradient-to-r from-purple-600 to-pink-600 text-white shadow-md'
              : 'text-slate-400 hover:text-white'
          }`}
        >
          <Sparkles className="w-4 h-4 text-amber-300" />
          {t('tabs.isomorphic_councils')}
          <span className="px-1.5 py-0.5 rounded-full bg-amber-400/20 text-amber-300 text-[10px]">
            {t('tabs.councils_count', { count: state.councils.length })}
          </span>
        </button>
      </div>

      {/* Tab Content */}
      {state.currentTab === 'swarm-registry' ? (
        <SwarmRegistryTab
          models={state.models}
          loadingModels={state.loadingModels}
          selectedProviderFilter={state.selectedProviderFilter}
          setSelectedProviderFilter={state.setSelectedProviderFilter}
          modelSearchQuery={state.modelSearchQuery}
          setModelSearchQuery={state.setModelSearchQuery}
          pingingModelCode={state.pingingModelCode}
          modelPingResults={state.modelPingResults}
          handlePingModel={state.handlePingModel}
          handleDeleteModel={state.handleDeleteModel}
          setSelectedModelForDetail={state.setSelectedModelForDetail}
          isAdmin={isAdmin}
          t={t}
        />
      ) : (
        <div className="space-y-6">
          <UniversalSymposiumTab
            councils={state.councils}
            councilCategoryFilter={state.councilCategoryFilter}
            setCouncilCategoryFilter={state.setCouncilCategoryFilter}
            setSelectedCouncilForDetail={state.setSelectedCouncilForDetail}
            setEditingCouncil={state.setEditingCouncil}
            setSelectedSeatForSwarm={state.setSelectedSeatForSwarm}
            handleDeleteSeat={state.handleDeleteSeat}
            onOpenConveneModal={() => {
              state.setConveneTopic('');
              state.setConveneProblem('');
              state.setSelectedCouncilCodes([]);
              state.setShowConveneModal(true);
            }}
            t={t}
          />
          <SymposiumProceedingsCard symposiumResult={state.symposiumResult} t={t} />
        </div>
      )}

      {/* Modals */}
      <ConveneSymposiumModal
        isOpen={state.showConveneModal}
        onClose={() => state.setShowConveneModal(false)}
        conveneTopic={state.conveneTopic}
        setConveneTopic={state.setConveneTopic}
        conveneProblem={state.conveneProblem}
        setConveneProblem={state.setConveneProblem}
        selectedCouncilCodes={state.selectedCouncilCodes}
        setSelectedCouncilCodes={state.setSelectedCouncilCodes}
        councils={state.councils}
        convening={state.convening}
        handleConveneSymposium={state.handleConveneSymposium}
        t={t}
      />

      <ModelDetailModal
        model={state.selectedModelForDetail}
        isOpen={Boolean(state.selectedModelForDetail)}
        onClose={() => state.setSelectedModelForDetail(null)}
        onEdit={(m) => { state.setSelectedModelToEdit(m); state.setShowUpsertModal(true); }}
        onDelete={state.handleDeleteModel}
        userKeys={state.myKeys}
        systemProviders={state.config?.providers || []}
        onUpdateProviderKeys={state.handleUpdateProviderKeys}
        onToggleProviderEnabled={state.toggleProviderEnabled}
        onRefreshKeys={state.fetchMyKeys}
        onPingModel={state.handlePingModel}
        pingResult={state.selectedModelForDetail ? state.modelPingResults[state.selectedModelForDetail.modelCode] : undefined}
        isPinging={state.selectedModelForDetail ? state.pingingModelCode === state.selectedModelForDetail.modelCode : false}
      />

      <ModelUpsertModal
        isOpen={state.showUpsertModal}
        onClose={() => state.setShowUpsertModal(false)}
        modelToEdit={state.selectedModelToEdit}
        onSaved={(saved) => {
          state.fetchModels();
          if (state.selectedModelForDetail && state.selectedModelForDetail.id === saved.id) {
            state.setSelectedModelForDetail(saved);
          }
        }}
      />

      <GatewayCatalogModal
        isOpen={state.showGatewayModal}
        onClose={() => state.setShowGatewayModal(false)}
        onImportSuccess={() => state.fetchModels()}
      />

      <CouncilDetailModal
        council={state.selectedCouncilForDetail}
        isOpen={!!state.selectedCouncilForDetail}
        onClose={() => state.setSelectedCouncilForDetail(null)}
        isSelected={false}
        onToggleSelect={() => {}}
      />

      <SeatSwarmConfigModal
        seat={state.selectedSeatForSwarm?.seat || null}
        councilName={state.selectedSeatForSwarm?.councilName || ''}
        isOpen={!!state.selectedSeatForSwarm}
        onClose={() => state.setSelectedSeatForSwarm(null)}
        availableModels={state.models}
        userKeys={state.myKeys}
        onSaved={() => { state.fetchCouncils(); state.setSelectedSeatForSwarm(null); }}
      />

      <AddCouncilSeatModal
        council={state.addingSeatToCouncil}
        isOpen={!!state.addingSeatToCouncil}
        onClose={() => state.setAddingSeatToCouncil(null)}
        availableModels={state.models}
        onAdded={() => { state.fetchCouncils(); state.setAddingSeatToCouncil(null); }}
      />

      <EditCouncilModal
        council={state.editingCouncil}
        isOpen={!!state.editingCouncil}
        onClose={() => state.setEditingCouncil(null)}
        onSaved={() => { state.fetchCouncils(); state.setEditingCouncil(null); }}
      />
    </div>
  );
};
