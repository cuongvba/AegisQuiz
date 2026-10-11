export type CoPilotPersona =
  | 'AssessmentArchitect'
  | 'SocraticMentor'
  | 'GovernanceFinOps'
  | 'ResearchScholar';

export interface ToolCall {
  toolName: string;
  description: string;
  status: 'running' | 'completed' | 'failed';
  durationMs: number;
  outputSummary?: Record<string, unknown>;
}

export interface CoPilotArtifact {
  artifactType: 'question' | 'matrix' | 'rubric' | 'symposium' | 'walkthrough' | 'swarm-audit';
  title: string;
  data: any;
  rawMarkdown?: string;
  canApplyDirectly: boolean;
}

export interface CoPilotMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  thoughtTrace?: string;
  toolCalls?: ToolCall[];
  artifact?: CoPilotArtifact;
  suggestedActions?: string[];
  modelUsed?: string;
  keySourceUsed?: string;
  latencyMs?: number;
  timestamp: string;
}

export interface CoPilotSlashCommand {
  command: string;
  description: string;
  example: string;
  recommendedPersona: CoPilotPersona;
  icon: string;
}

export interface CoPilotContext {
  screenName?: string;
  selectedText?: string;
  currentQuestionId?: string;
  currentTopicId?: string;
  domainCode?: string;
  metadata?: Record<string, string>;
}

export interface CoPilotRequest {
  message: string;
  persona: CoPilotPersona;
  modelCode?: string;
  context?: CoPilotContext;
  history?: Array<{ role: string; content: string }>;
}

export interface CoPilotResponse {
  sessionId: string;
  thoughtTrace: string;
  toolCalls: ToolCall[];
  content: string;
  artifact?: CoPilotArtifact;
  suggestedActions: string[];
  modelUsed: string;
  keySourceUsed?: string;
  latencyMs: number;
  tokenUsage: number;
  timestampUtc: string;
}

export interface AiModelOption {
  modelCode: string;
  displayName: string;
  archetype?: string;
}

export interface CoPilotModelItem {
  modelCode: string;
  displayName: string;
  archetype: string;
  description: string;
  keySource: 'UserBYOK' | 'TenantBYOK' | 'System' | 'SystemDefault';
  isRecommended: boolean;
  recommendationReason?: string;
  latencyBenchmarkMs: number;
  costPerMillionTokensUsd: number;
  isAvailable?: boolean;
  availabilityStatus?: 'Ready' | 'FreeCommunity' | 'RequiresKey' | 'LocalNode';
  statusBadge?: string;
  isFreeOrLocal?: boolean;
}

export interface CoPilotModelGroup {
  providerName: string;
  providerCode: string;
  providerIcon: string;
  badgeColor: string;
  models: CoPilotModelItem[];
}
