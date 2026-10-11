import { useState, useEffect } from 'react';
import api from '@/shared/api/client';
import { createLogger } from '@/shared/logger';
import { 
  aiSwarmService, 
  AiModelRegistryDto, 
  UniversalAiCouncilDto, 
  UniversalCouncilSeatDto, 
  SymposiumProceedingsResult, 
  UserAiKeyDto 
} from '@/services/ai-swarm.service';
import type { AiSystemConfig } from './types';

const logger = createLogger('useAiSettingsState');

export function useAiSettingsState() {
  const [currentTab, setCurrentTab] = useState<'swarm-registry' | 'universal-symposium'>('swarm-registry');
  
  // FinOps Config
  const [config, setConfig] = useState<AiSystemConfig | null>(null);
  const [loading, setLoading] = useState(true);

  // Swarm Models
  const [models, setModels] = useState<AiModelRegistryDto[]>([]);
  const [loadingModels, setLoadingModels] = useState(false);
  const [selectedModelForDetail, setSelectedModelForDetail] = useState<AiModelRegistryDto | null>(null);
  const [selectedModelToEdit, setSelectedModelToEdit] = useState<AiModelRegistryDto | null>(null);
  const [showUpsertModal, setShowUpsertModal] = useState(false);
  const [showGatewayModal, setShowGatewayModal] = useState(false);
  const [selectedProviderFilter, setSelectedProviderFilter] = useState<string>('ALL');
  const [modelSearchQuery, setModelSearchQuery] = useState<string>('');
  const [pingingModelCode, setPingingModelCode] = useState<string | null>(null);
  const [modelPingResults, setModelPingResults] = useState<Record<string, { success: boolean; latencyMs: number; message: string }>>({});

  // Universal Symposium
  const [councils, setCouncils] = useState<UniversalAiCouncilDto[]>([]);
  const [loadingCouncils, setLoadingCouncils] = useState(false);
  const [showConveneModal, setShowConveneModal] = useState(false);
  const [conveneTopic, setConveneTopic] = useState('');
  const [conveneProblem, setConveneProblem] = useState('');
  const [selectedCouncilCodes, setSelectedCouncilCodes] = useState<string[]>([]);
  const [convening, setConvening] = useState(false);
  const [symposiumResult, setSymposiumResult] = useState<SymposiumProceedingsResult | null>(null);

  // Seat & Council Modals
  const [selectedSeatForSwarm, setSelectedSeatForSwarm] = useState<{ seat: UniversalCouncilSeatDto; councilName: string } | null>(null);
  const [selectedCouncilForDetail, setSelectedCouncilForDetail] = useState<UniversalAiCouncilDto | null>(null);
  const [editingCouncil, setEditingCouncil] = useState<UniversalAiCouncilDto | null>(null);
  const [addingSeatToCouncil, setAddingSeatToCouncil] = useState<UniversalAiCouncilDto | null>(null);
  const [councilCategoryFilter, setCouncilCategoryFilter] = useState<string>('ALL');
  const [myKeys, setMyKeys] = useState<UserAiKeyDto[]>([]);

  useEffect(() => {
    fetchConfig();
    fetchModels();
    fetchCouncils();
    fetchMyKeys();
  }, []);

  const fetchConfig = async () => {
    try {
      setLoading(true);
      const res = await api.get('/api/admin/ai/config');
      setConfig(res.data);
    } catch (err) {
      logger.error('Lỗi nạp cấu hình AI:', err);
    } finally {
      setLoading(false);
    }
  };

  const fetchModels = async () => {
    try {
      setLoadingModels(true);
      const data = await aiSwarmService.getModels();
      setModels(data);
    } catch (err) {
      logger.error('Lỗi nạp danh sách mô hình bầy sói:', err);
    } finally {
      setLoadingModels(false);
    }
  };

  const fetchCouncils = async () => {
    try {
      setLoadingCouncils(true);
      const data = await aiSwarmService.getSymposiumCouncils();
      setCouncils(data);
    } catch (err) {
      logger.error('Lỗi nạp danh sách hội đồng AI:', err);
    } finally {
      setLoadingCouncils(false);
    }
  };

  const fetchMyKeys = async () => {
    try {
      const data = await aiSwarmService.getMyKeys();
      setMyKeys(data);
    } catch {
      // Ignore
    }
  };

  const toggleProviderEnabled = async (providerType: number, providerCode?: string) => {
    if (!config) return;
    const targetCode = providerCode ? providerCode.toLowerCase().replace(/[^a-z0-9]/g, '').trim() : '';
    const updated = config.providers.map(p => {
      const pCode = (p.providerCode || p.displayName).toLowerCase().replace(/[^a-z0-9]/g, '').trim();
      const match = (targetCode && pCode && (pCode === targetCode || pCode.includes(targetCode) || targetCode.includes(pCode))) ||
                    p.providerType === providerType;
      return match ? { ...p, isEnabled: !p.isEnabled } : p;
    });
    const newConfig = { ...config, providers: updated };
    setConfig(newConfig);
    try {
      await api.post('/api/admin/ai/config', newConfig);
      await fetchConfig();
    } catch (err) {
      logger.error('Lỗi thay đổi trạng thái nhà cung cấp:', err);
    }
  };

  const handleUpdateProviderKeys = async (providerType: number, newKeys: string[], providerCode?: string) => {
    if (!config) return;
    const targetCode = providerCode ? providerCode.toLowerCase().replace(/[^a-z0-9]/g, '').trim() : '';
    const updated = config.providers.map(p => {
      const pCode = (p.providerCode || p.displayName).toLowerCase().replace(/[^a-z0-9]/g, '').trim();
      const match = (targetCode && pCode && (pCode === targetCode || pCode.includes(targetCode) || targetCode.includes(pCode))) ||
                    p.providerType === providerType;
      return match ? { ...p, apiKeys: newKeys, isEnabled: newKeys.length > 0 } : p;
    });
    const newConfig = { ...config, providers: updated };
    setConfig(newConfig);
    try {
      await api.post('/api/admin/ai/config', newConfig);
      await fetchConfig();
    } catch (err) {
      logger.error('Lỗi cập nhật bể khóa AI:', err);
      alert('Không thể lưu bể khóa AI. Vui lòng kiểm tra quyền hệ thống.');
    }
  };

  const handleDeleteModel = async (id: string, name: string) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa mô hình [${name}] khỏi hệ thống?`)) return;
    try {
      await aiSwarmService.deleteModel(id);
      await fetchModels();
    } catch (err) {
      logger.error('Lỗi xóa mô hình AI:', err);
      alert('Không thể xóa mô hình AI.');
    }
  };

  const handlePingModel = async (model: AiModelRegistryDto) => {
    try {
      setPingingModelCode(model.modelCode);
      const res = await aiSwarmService.pingModelKey({
        providerCompany: model.providerCompany,
        protocolType: model.protocolType,
        rawApiKey: '',
        customEndpoint: model.endpointUrl,
        customModelName: model.modelName
      });
      setModelPingResults(prev => ({
        ...prev,
        [model.modelCode]: {
          success: res.success,
          latencyMs: res.latencyMs,
          message: res.message
        }
      }));
    } catch (err: any) {
      setModelPingResults(prev => ({
        ...prev,
        [model.modelCode]: {
          success: false,
          latencyMs: 0,
          message: err.message || 'Lỗi kiểm thử'
        }
      }));
    } finally {
      setPingingModelCode(null);
    }
  };

  const handleDeleteSeat = async (seatId: string, seatTitle: string) => {
    if (!window.confirm(`Bạn có chắc chắn muốn giải tán ghế viện sĩ [${seatTitle}]?`)) return;
    try {
      await aiSwarmService.removeCouncilSeat(seatId);
      await fetchCouncils();
    } catch (err) {
      logger.error('Lỗi xóa ghế viện sĩ:', err);
      alert('Không thể xóa ghế viện sĩ.');
    }
  };

  const handleConveneSymposium = async () => {
    if (!conveneTopic.trim()) return;
    try {
      setConvening(true);
      const res = await aiSwarmService.conveneSymposium({
        topicTitle: conveneTopic,
        coreProblemStatement: conveneProblem,
        specificCouncilCodes: selectedCouncilCodes.length > 0 ? selectedCouncilCodes : undefined,
        allowDynamicGenesis: true,
        enableAdversarialCritique: true,
      });
      setSymposiumResult(res);
      setShowConveneModal(false);
    } catch (err: any) {
      logger.error('Lỗi triệu tập Đại Hội Thảo:', err);
      alert(err.response?.data?.message || 'Lỗi khi triệu tập Đại Hội Thảo');
    } finally {
      setConvening(false);
    }
  };

  return {
    currentTab,
    setCurrentTab,
    config,
    loading,
    models,
    loadingModels,
    selectedModelForDetail,
    setSelectedModelForDetail,
    selectedModelToEdit,
    setSelectedModelToEdit,
    showUpsertModal,
    setShowUpsertModal,
    showGatewayModal,
    setShowGatewayModal,
    selectedProviderFilter,
    setSelectedProviderFilter,
    modelSearchQuery,
    setModelSearchQuery,
    pingingModelCode,
    modelPingResults,
    councils,
    showConveneModal,
    setShowConveneModal,
    conveneTopic,
    setConveneTopic,
    conveneProblem,
    setConveneProblem,
    selectedCouncilCodes,
    setSelectedCouncilCodes,
    convening,
    symposiumResult,
    selectedSeatForSwarm,
    setSelectedSeatForSwarm,
    selectedCouncilForDetail,
    setSelectedCouncilForDetail,
    editingCouncil,
    setEditingCouncil,
    addingSeatToCouncil,
    setAddingSeatToCouncil,
    councilCategoryFilter,
    setCouncilCategoryFilter,
    myKeys,
    fetchModels,
    fetchCouncils,
    fetchMyKeys,
    toggleProviderEnabled,
    handleUpdateProviderKeys,
    handleDeleteModel,
    handlePingModel,
    handleDeleteSeat,
    handleConveneSymposium,
  };
}
