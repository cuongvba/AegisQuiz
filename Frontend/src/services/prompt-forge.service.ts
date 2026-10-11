import api from './api';

export enum DialecticalRigorLevel {
  StrictPopperian = 1,
  PragmaticEmpirical = 2,
  ConstructiveDialectical = 3,
  SystemicHolistic = 4,
}

export const RIGOR_LABELS: Record<DialecticalRigorLevel, { name: string; desc: string; badgeColor: string }> = {
  [DialecticalRigorLevel.StrictPopperian]: {
    name: 'Phản Nghiệm Triệt Để (Karl Popper)',
    desc: 'Đào sâu phản chứng, vạch trần ngụy biện, kiểm tra điều kiện bác bỏ.',
    badgeColor: 'border-red-500/50 bg-red-950/40 text-red-300',
  },
  [DialecticalRigorLevel.PragmaticEmpirical]: {
    name: 'Thực Nghiệm Thực Dụng',
    desc: 'Tôn trọng số liệu định lượng, bằng chứng lâm sàng và tính khả thi.',
    badgeColor: 'border-blue-500/50 bg-blue-950/40 text-blue-300',
  },
  [DialecticalRigorLevel.ConstructiveDialectical]: {
    name: 'Biện Chứng Xây Dựng',
    desc: 'Tổng hợp chính đề & phản đề để đúc kết thành hợp đề chân lý.',
    badgeColor: 'border-emerald-500/50 bg-emerald-950/40 text-emerald-300',
  },
  [DialecticalRigorLevel.SystemicHolistic]: {
    name: 'Tư Duy Hệ Thống Chỉnh Thể',
    desc: 'Tiếp cận phi tuyến tính, phân tích vòng lặp phản hồi và hiện tượng nổi lên.',
    badgeColor: 'border-purple-500/50 bg-purple-950/40 text-purple-300',
  },
};

export interface PromptProposalRequest {
  organizationDomain: string;
  coreAxioms: string;
  rigorLevel: DialecticalRigorLevel;
  locale?: string;
}

export interface MetaPromptProposalDto {
  templateCode: string;
  body: string;
  rationale: string;
  extractedAxioms: string[];
  estimatedTokens: number;
  version: number;
}

export interface GenerateStackRequest {
  metaPromptBody: string;
  domainFocus: string;
  locale?: string;
}

export interface CouncilCharterProposalDto {
  councilCode: string;
  displayName: string;
  disciplineArea: string;
  taxonomyPath: string;
  charterPromptBody: string;
}

export interface SeatRoleProposalDto {
  seatTitle: string;
  role: string;
  preferredModel: string;
  directivePromptBody: string;
}

export interface CascadingPromptStackDto {
  metaPromptLayer0: string;
  constitutionLayer1: string;
  councilChartersLayer2: CouncilCharterProposalDto[];
  seatRolesLayer3: SeatRoleProposalDto[];
  phaseDirectivesLayer4: string;
  outputContractLayer5: string;
  totalEstimatedTokens: number;
}

export interface AxiomaticInconsistencyDto {
  layerName: string;
  targetKey: string;
  conflictSummary: string;
  severity: string;
  remediationSuggestion: string;
}

export interface AxiomaticAuditReportDto {
  isFullyConsistent: boolean;
  consistencyScore: number;
  inconsistencies: AxiomaticInconsistencyDto[];
  auditChecklist: string[];
}

export interface EnactPromptStackRequest {
  metaPromptLayer0: string;
  constitutionLayer1: string;
  charters?: CouncilCharterProposalDto[];
  outputContractLayer5?: string;
  locale?: string;
  changeSummary?: string;
}

export interface PromptTemplateSummaryDto {
  id: string;
  templateCode: string;
  layer: number;
  layerName: string;
  targetKey: string;
  version: number;
  locale: string;
  body: string;
  description: string;
  isActive: boolean;
  updatedAt: string;
}

export const promptForgeService = {
  async getAllTemplates(locale: string = 'vi-VN'): Promise<PromptTemplateSummaryDto[]> {
    const res = await api.get('/api/ai/symposium/prompts/templates', { params: { locale } });
    return res.data;
  },

  async proposeMetaPrompt(request: PromptProposalRequest): Promise<MetaPromptProposalDto> {
    const res = await api.post('/api/ai/symposium/prompts/propose-meta', request);
    return res.data;
  },

  async generateCascadingStack(request: GenerateStackRequest): Promise<CascadingPromptStackDto> {
    const res = await api.post('/api/ai/symposium/prompts/generate-stack', request);
    return res.data;
  },

  async auditConsistency(locale: string = 'vi-VN'): Promise<AxiomaticAuditReportDto> {
    const res = await api.get('/api/ai/symposium/prompts/audit-consistency', { params: { locale } });
    return res.data;
  },

  async enactPromptStack(request: EnactPromptStackRequest): Promise<{ success: boolean; message: string }> {
    const res = await api.post('/api/ai/symposium/prompts/enact-stack', request);
    return res.data;
  },
};
