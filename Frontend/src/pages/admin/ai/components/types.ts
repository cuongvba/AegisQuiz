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

export interface FinOpsTelemetry {
  totalRequestsProcessed: number;
  cacheHitsCount: number;
  cacheHitRatio: number;
  estimatedCostSavedUsd: number;
  totalSpentUsd: number;
  requestCountByProvider: Record<string, number>;
}

export interface AiSystemConfig {
  providers: ProviderConfig[];
  taskRoutingMap: Record<string, number>;
  enableSemanticCache: boolean;
  semanticCacheSimilarityThreshold: number;
  enableMultiAgentCommittee: boolean;
  keyQuarantineSeconds: number;
  telemetry: FinOpsTelemetry;
}
