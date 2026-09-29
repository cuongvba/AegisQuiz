import api from './api';

export interface ProctorSignalViolation {
  examSessionId: string;
  candidateIdentifier: string;
  eventType: string;
  severity: 'INFO' | 'WARNING' | 'CRITICAL' | 'DISQUALIFIED';
  details: string;
  clientIp: string;
  timestampUtc: string;
}

export interface FortressStatus {
  serverTimeUtc: string;
  activeExamSessionsCount: number;
  activeProctoredParticipantsCount: number;
  prewarmedExamsCount: number;
  prewarmedExamTitles: string[];
  cacheHitRatioPercent: number;
  memoryUsageMb: number;
  totalViolationsToday: number;
  recentViolationsFeed: ProctorSignalViolation[];
  redisConnected: boolean;
  healthStatus: string;
}

export interface AuditVerificationResult {
  examId: string;
  isChainValid: boolean;
  totalBlocksChecked: number;
  verifiedAtUtc: string;
  brokenAtBlockId?: string;
  brokenSequenceNumber?: number;
  anomalyDescription?: string;
  latestBlockHash: string;
}

export const fortressService = {
  async getStatus(): Promise<FortressStatus> {
    const res = await api.get('/api/sysadmin/fortress-status');
    return res.data;
  },

  async prewarmExam(examId: string) {
    const res = await api.post(`/api/fortress/prewarm/${examId}`);
    return res.data;
  },

  async getExamPackage(examId: string) {
    const res = await api.get(`/api/fortress/package/${examId}`);
    return res.data;
  },

  async verifyAuditIntegrity(examId: string): Promise<AuditVerificationResult> {
    const res = await api.get(`/api/fortress/audit/verify/${examId}`);
    return res.data;
  },

  async getAuditLogs(examId: string, limit: number = 50) {
    const res = await api.get(`/api/fortress/audit/logs/${examId}?limit=${limit}`);
    return res.data;
  },

  async getLiveViolations(limit: number = 20): Promise<{ feed: ProctorSignalViolation[] }> {
    const res = await api.get(`/api/sysadmin/violations/live?limit=${limit}`);
    return res.data;
  },
};
