import api from './api';

export enum CognitiveArchetype {
  DeepReasoner = 1,
  MassiveContext = 2,
  LinguisticArbiter = 3,
  UltraFastScanner = 4,
  SovereignLocal = 5,
}

export const ARCHETYPE_LABELS: Record<CognitiveArchetype, { name: string; desc: string; badgeColor: string; icon: string }> = {
  [CognitiveArchetype.DeepReasoner]: {
    name: 'Tư Duy Suy Luận Chuỗi Dài (CoT)',
    desc: 'Suy luận logic nhiều bước, giải toán, giải mã bẫy nhận thức',
    badgeColor: '#8b5cf6',
    icon: 'Brain',
  },
  [CognitiveArchetype.MassiveContext]: {
    name: 'Ngữ Cảnh Cực Đại (1M - 10M Tokens)',
    desc: 'Bóc tách toàn văn giáo trình đồ sộ, tích hợp đa nguồn tài liệu',
    badgeColor: '#0284c7',
    icon: 'Layers',
  },
  [CognitiveArchetype.LinguisticArbiter]: {
    name: 'Trọng Tài Sư Phạm & Câu Chữ',
    desc: 'Barem chấm tự luận, chuẩn hóa văn phong và phản biện Devil Advocate',
    badgeColor: '#10b981',
    icon: 'Scale',
  },
  [CognitiveArchetype.UltraFastScanner]: {
    name: 'Siêu Tốc Độ Real-time (>150 TPS)',
    desc: 'Độ trễ phản hồi < 250ms phục vụ Đấu trường Gameshow thời gian thực',
    badgeColor: '#f59e0b',
    icon: 'Zap',
  },
  [CognitiveArchetype.SovereignLocal]: {
    name: 'Bảo Mật Nội Bộ On-Premise (Ollama)',
    desc: 'Bảo đảm an toàn dữ liệu 100% trong Intranet, không lọt ra ngoài Internet',
    badgeColor: '#6b7280',
    icon: 'Shield',
  },
};

export enum ModelProtocolType {
  OpenAiCompatible = 1,
  GoogleGenerativeAi = 2,
  AnthropicMessages = 3,
}

export enum KeyUsageScope {
  Private = 1,
  CommunityContributed = 2,
}

export interface WolfPackAuditRequest {
  prompt: string;
  context?: string;
  questionContent?: string;
  proposedAnswer?: string;
  targetArchetypes?: CognitiveArchetype[];
  timeoutSeconds?: number;
}

export interface WolfPackMemberVerdict {
  modelCode: string;
  displayName: string;
  providerCompany: string;
  archetype: CognitiveArchetype;
  rawResponse: string;
  normalizedAnswer: string;
  confidenceScore: number;
  pedagogicalRationale: string;
  identifiedFlaws: string[];
  latencyMs: number;
  tokenCostUsd: number;
  keySource: string;
  isSuccess: boolean;
  errorMessage?: string;
}

export interface WolfPackSwarmAuditResult {
  consensusIndex: number;
  consensusLevel: 'UNANIMOUS' | 'HIGH_CONSENSUS' | 'MODERATE_DIVERGENCE' | 'CONTESTED_FLAG';
  alphaModelCode: string;
  alphaModelDisplayName: string;
  winningAnswer: string;
  synthesisSummary: string;
  totalLatencyMs: number;
  totalCostUsd: number;
  memberVerdicts: WolfPackMemberVerdict[];
  hasContradictions: boolean;
}

export interface AiModelRegistryDto {
  id: string;
  modelCode: string;
  displayName: string;
  providerCompany: string;
  protocolType: ModelProtocolType;
  cognitiveArchetype: CognitiveArchetype;
  endpointUrl: string;
  modelName: string;
  maxContextTokens: number;
  costInputPerMillion: number;
  costOutputPerMillion: number;
  benchmarkElo: number;
  avgLatencyMs: number;
  isEnabled: boolean;
  isDefaultInArchetype: boolean;
  tenantId: string;
  createdAt: string;
}

export interface UpsertAiModelRequest {
  id?: string;
  modelCode: string;
  displayName: string;
  providerCompany: string;
  protocolType: ModelProtocolType;
  cognitiveArchetype: CognitiveArchetype;
  endpointUrl: string;
  modelName: string;
  maxContextTokens: number;
  costInputPerMillion: number;
  costOutputPerMillion: number;
  benchmarkElo: number;
  avgLatencyMs: number;
  isEnabled: boolean;
  isDefaultInArchetype: boolean;
}

export interface UserAiKeyDto {
  id: string;
  userId: string;
  keyLabel: string;
  providerCompany: string;
  protocolType: ModelProtocolType;
  keyFingerprint: string;
  customEndpoint?: string;
  customModelName?: string;
  scope: KeyUsageScope;
  isActive: boolean;
  monthlyTokenQuota: number;
  tokensUsedThisMonth: number;
  totalCallsCount: number;
  successCallsCount: number;
  failedCallsCount: number;
  lastUsedAt?: string;
  createdAt: string;
}

export interface SaveUserAiKeyRequest {
  id?: string;
  keyLabel: string;
  providerCompany: string;
  protocolType: ModelProtocolType;
  rawApiKey: string;
  customEndpoint?: string;
  customModelName?: string;
  scope: KeyUsageScope;
  monthlyTokenQuota: number;
}

export interface TestAiKeyRequest {
  providerCompany: string;
  protocolType: ModelProtocolType;
  rawApiKey: string;
  customEndpoint?: string;
  customModelName?: string;
}

export interface TestAiKeyResult {
  success: boolean;
  message: string;
  latencyMs: number;
  sampleOutput?: string;
}

export interface CommunityContributorHonorDto {
  userId: string;
  userFullName: string;
  avatarUrl: string;
  providerCompany: string;
  totalContributionsServed: number;
  aegisCoinsAwarded: number;
  honorTier: string;
}

export const aiSwarmService = {
  async auditWithSwarm(req: WolfPackAuditRequest): Promise<WolfPackSwarmAuditResult> {
    const res = await api.post<WolfPackSwarmAuditResult>('/api/ai/swarm/audit', req);
    return res.data;
  },

  async pingModelKey(req: TestAiKeyRequest): Promise<TestAiKeyResult> {
    const res = await api.post<TestAiKeyResult>('/api/ai/models/ping', req);
    return res.data;
  },

  async getModels(): Promise<AiModelRegistryDto[]> {
    const res = await api.get<AiModelRegistryDto[]>('/api/ai/models');
    return res.data;
  },

  async upsertModel(req: UpsertAiModelRequest): Promise<AiModelRegistryDto> {
    const res = await api.post<AiModelRegistryDto>('/api/ai/models', req);
    return res.data;
  },

  async deleteModel(id: string): Promise<boolean> {
    const res = await api.delete<{ success: boolean }>(`/api/ai/models/${id}`);
    return res.data.success;
  },

  async getGatewayPresets(gateway = 'all'): Promise<GatewayModelPresetDto[]> {
    const res = await api.get<GatewayModelPresetDto[]>(`/api/ai/models/gateway-presets?gateway=${gateway}`);
    return res.data;
  },

  async importGatewayPack(req: ImportGatewayModelsRequest): Promise<ImportGatewayModelsResult> {
    const res = await api.post<ImportGatewayModelsResult>('/api/ai/models/import-gateway-pack', req);
    return res.data;
  },

  async getMyKeys(): Promise<UserAiKeyDto[]> {
    const res = await api.get<UserAiKeyDto[]>('/api/ai/byok/my-keys');
    return res.data;
  },

  async saveKey(req: SaveUserAiKeyRequest): Promise<UserAiKeyDto> {
    const res = await api.post<UserAiKeyDto>('/api/ai/byok/keys', req);
    return res.data;
  },

  async deleteKey(id: string): Promise<boolean> {
    const res = await api.delete<{ success: boolean }>(`/api/ai/byok/keys/${id}`);
    return res.data.success;
  },

  async getHallOfFame(limit = 10): Promise<CommunityContributorHonorDto[]> {
    const res = await api.get<CommunityContributorHonorDto[]>(`/api/ai/byok/hall-of-fame?limit=${limit}`);
    return res.data;
  },

  async getSymposiumCouncils(): Promise<UniversalAiCouncilDto[]> {
    const res = await api.get<UniversalAiCouncilDto[]>('/api/ai/symposium/councils');
    return res.data;
  },

  async conveneSymposium(req: ConveneSymposiumRequest): Promise<SymposiumProceedingsResult> {
    const res = await api.post<SymposiumProceedingsResult>('/api/ai/symposium/convene', req);
    return res.data;
  },

  async exportSymposiumToNotebook(sessionId: string, targetNotebookId?: string): Promise<{ success: boolean; notebookId: string; message: string }> {
    const res = await api.post<{ success: boolean; notebookId: string; message: string }>('/api/ai/symposium/export-to-notebook', {
      sessionId,
      targetNotebookId
    });
    return res.data;
  },

  async saveCouncil(council: Partial<UniversalAiCouncilDto>): Promise<UniversalAiCouncilDto> {
    const res = await api.post<UniversalAiCouncilDto>('/api/ai/symposium/councils', council);
    return res.data;
  },

  async deleteCouncil(id: string): Promise<boolean> {
    const res = await api.delete<{ success: boolean }>(`/api/ai/symposium/councils/${id}`);
    return res.data.success;
  },

  async addCouncilSeat(councilId: string, seat: Partial<UniversalCouncilSeatDto>): Promise<UniversalCouncilSeatDto> {
    const res = await api.post<UniversalCouncilSeatDto>(`/api/ai/symposium/councils/${councilId}/seats`, seat);
    return res.data;
  },

  async removeCouncilSeat(seatId: string): Promise<boolean> {
    const res = await api.delete<{ success: boolean }>(`/api/ai/symposium/seats/${seatId}`);
    return res.data.success;
  },

  async updateSeatSwarm(seatId: string, req: {
    alphaModelCode: string;
    swarmModelCodesJson?: string;
    swarmPolicy?: string;
    seatTitle?: string;
    role?: number;
    decisionWeight?: number;
    personaPromptDirective?: string;
  }): Promise<UniversalCouncilSeatDto> {
    const res = await api.put<UniversalCouncilSeatDto>(`/api/ai/symposium/seats/${seatId}/swarm`, req);
    return res.data;
  }
};

export enum CouncilCategory {
  Academic = 1,
  Secretariat = 2,
  Arbitration = 3,
  SystemCore = 4,
}

export interface UniversalAiCouncilDto {
  id: string;
  councilCode: string;
  displayName: string;
  category: CouncilCategory;
  disciplineArea: string;
  materializedTaxonomyPath: string;
  epistemicManifesto: string;
  benchmarkPrestigeElo: number;
  isActive: boolean;
  spatioTemporalAffinity?: string;
  readinessStatus?: 'READY' | 'DEGRADED' | 'OFFLINE';
  readySeatsCount?: number;
  totalSeatsCount?: number;
  readinessMessage?: string;
  seats: UniversalCouncilSeatDto[];
}

export interface WolfModelKeyPoolSummaryDto {
  modelCode: string;
  displayName: string;
  providerCompany: string;
  cognitiveArchetype: CognitiveArchetype;
  contemporaryStrength: string;
  userKeysCount: number;
  hasSystemKey: boolean;
  hasCommunityKey: boolean;
  isKeyPoolReady: boolean;
  keyPriorityFlow: string;
}

export interface UniversalCouncilSeatDto {
  id: string;
  councilId: string;
  seatTitle: string;
  role: number;
  idealArchetype: CognitiveArchetype;
  preferredModelCode: string;
  assignedModelCode?: string;
  assignmentRationale?: string;
  alphaModelCode?: string;
  swarmModelCodesJson?: string;
  swarmPolicy?: string;
  isReady?: boolean;
  decisionWeight: number;
  personaPromptDirective: string;
  swarmWolfProfiles?: WolfModelKeyPoolSummaryDto[];
}

export enum SymposiumTeleologyMode {
  AutoDetect = 0,
  EmpiricalActionable = 1,
  EpistemicGenesis = 2,
  UniversalHybrid = 3
}

export interface ActionableSolutionItemDto {
  horizon: string;
  solutionTitle: string;
  description: string;
  responsibleDomain: string;
  feasibilityScore: number;
  groundedEvidenceIds?: string[];
}

export interface SymposiumStructuredOutcomeDto {
  appliedMode: SymposiumTeleologyMode;
  empiricalStatusQuo: string[];
  rootCauses: string[];
  validatedProgress: string[];
  coreAntinomy?: string;
  actionableSolutions: ActionableSolutionItemDto[];
  novelEpistemicAxiom?: string;
  openConjectures: string[];
}

export interface ConveneSymposiumRequest {
  topicTitle: string;
  coreProblemStatement: string;
  specificCouncilCodes?: string[];
  enableAdversarialCritique?: boolean;
  allowDynamicGenesis?: boolean;
  teleologyMode?: SymposiumTeleologyMode;
  locale?: string;
  clientSessionId?: string;
  roundNumber?: number;
  parentSessionId?: string;
  previousRoundResolution?: string;
  socraticDirectives?: string;
  additionalDocuments?: string;
  additionalUris?: string[];
  targetNotebookId?: string;
  isFinalRound?: boolean;
}

export interface SymposiumDialogueTurn {
  roundNumber?: number;
  phaseNumber: number;
  phaseName: string;
  councilCode: string;
  councilDisplayName: string;
  speakerTitle: string;
  modelCode: string;
  spatioTemporalPerspective?: string;
  assignmentRationale?: string;
  content: string;
  keyTheses: string[];
  exposedFallacies: string[];
  latencyMs: number;
  isSimulated?: boolean;
  simulationReason?: string;
  keySource?: string;
  timestamp: string;
}

export interface SymposiumRubricScore {
  isEvaluated?: boolean;
  evaluationNote?: string;
  evaluatedByModel?: string;
  groundingRate?: number;
  validCitationsCount?: number;
  logicalRigor: number;
  empiricalFalsifiability: number;
  ontologicalDepth: number;
  pragmaticActionability: number;
  trapResistance: number;
  overallCompositeScore: number;
}

export interface EvidenceLedgerItemDto {
  id: string;
  claimOrFact: string;
  quantitativeValue?: string;
  sourceTitle: string;
  doiOrUrl?: string;
  provenance: string;
  isOnlineVerified: boolean;
  confidenceWeight: number;
}

export interface EmpiricalEvidenceDossierDto {
  topicTitle: string;
  domainArea: string;
  evidenceLedger?: EvidenceLedgerItemDto[];
  verifiedFacts: string[];
  quantitativeDataPoints: string[];
  academicCitations: string[];
  unresolvedControversies: string[];
  formattedDossierMarkdown: string;
  gatheredAt: string;
  totalEvidenceUnits: number;
  provenance?: string;
  isOnlineVerified?: boolean;
  provenanceNote?: string;
}

export interface CouncilAcademicContributionDto {
  councilCode?: string;
  CouncilCode?: string;
  councilDisplayName?: string;
  CouncilDisplayName?: string;
  spatioTemporalAffinity?: string;
  SpatioTemporalAffinity?: string;
  representativeModel?: string;
  RepresentativeModel?: string;
  scholarTitle?: string;
  ScholarTitle?: string;
  coreEpistemicStance?: string;
  CoreEpistemicStance?: string;
  deepAnalysis?: string;
  DeepAnalysis?: string;
  primaryObjection?: string;
  PrimaryObjection?: string;
  proposedRemedy?: string;
  ProposedRemedy?: string;
  keyTheses?: string[];
  KeyTheses?: string[];
}

export interface SymposiumProceedingsResult {
  sessionId: string;
  topicTitle: string;
  coreProblemStatement: string;
  teleologyMode?: SymposiumTeleologyMode;
  participatingCouncils: string[];
  councilContributions?: CouncilAcademicContributionDto[];
  dialogueLog: SymposiumDialogueTurn[];
  unifiedMetatheory: string;
  finalConsensusDeclaration: string;
  evaluatedRubric: SymposiumRubricScore;
  epistemicEvolutionIndex: number;
  empiricalDossier?: EmpiricalEvidenceDossierDto;
  groundingRate?: number;
  groundedEvidenceIds?: string[];
  structuredOutcome?: SymposiumStructuredOutcomeDto;
  isSimulated?: boolean;
  simulationReason?: string;
  totalSessionDurationMs: number;
  concludedAt: string;
  roundNumber?: number;
  parentSessionId?: string;
  isFinalRound?: boolean;
  reachedEpistemicConsensus?: boolean;
  socraticDirectives?: string;
  targetNotebookId?: string;
  notebookArchiveMessage?: string;
}

export interface GatewayModelPresetDto {
  modelCode: string;
  modelName: string;
  displayName: string;
  providerCompany: string;
  endpointUrl: string;
  protocolType: ModelProtocolType;
  cognitiveArchetype: CognitiveArchetype;
  contemporaryStrength: string;
  recommendedCouncilRole: number;
  recommendedCouncilCodes: string[];
  benchmarkElo: number;
  avgLatencyMs: number;
  maxContextTokens: number;
  costInputPerMillion: number;
  costOutputPerMillion: number;
  isFree: boolean;
  badgeTag: string;
  description: string;
}

export interface ImportGatewayModelsRequest {
  gatewayProvider: string;
  selectedModelCodes: string[];
  autoAssignToCounterCouncils: boolean;
}

export interface ImportGatewayModelsResult {
  importedModelsCount: number;
  updatedModelsCount: number;
  assignedSeatsCount: number;
  importedModelNames: string[];
  message: string;
}


