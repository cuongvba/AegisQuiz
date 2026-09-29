import api from './api';

export interface AegisCoinWallet {
  walletId: string;
  userId: string;
  balance: number;
  lifetimeEarned: number;
  lifetimeSpent: number;
  lastEarnedAt?: string;
  recentTransactions: Array<{
    id: string;
    amount: number;
    balanceAfter: number;
    transactionType: string;
    description: string;
    createdAt: string;
  }>;
}

export interface PeerChallenge {
  shareCode: string;
  examId: string;
  examTitle: string;
  creatorName: string;
  creatorScore: number;
  totalQuestions: number;
  durationMinutes: number;
  leaderboard: Array<{
    candidateIdentifier: string;
    score: number;
    timeSpentSeconds: number;
    completedAtUtc: string;
  }>;
}

export interface DktState {
  userId: string;
  calculatedAtUtc: string;
  conceptMasteryMap: Record<string, number>;
  overallCognitiveHealthIndex: number;
  identifiedBlindSpots: string[];
  recommendedNextConcepts: string[];
}

export interface KnowledgeGraphNode {
  conceptCode: string;
  name: string;
  domainCode: string;
  difficultyRank: number;
  studentMasteryPercent: number;
  curriculumReference?: string;
  prerequisites: string[];
  subConcepts: string[];
}

export interface PedagogicalRagResponse {
  questionId: string;
  socraticExplanation: string;
  curriculumStandard: string;
  citations: Array<{
    textbookName: string;
    chapterTitle: string;
    pageNumber: number;
    officialDecree: string;
    excerpt: string;
  }>;
  keyTheoremsAndFormulas: string[];
  socraticFollowUpQuestions: string[];
}

export const communityCognitiveService = {
  // ── AegisCoin ─────────────────────────────────────────────────────────────
  async getWallet(): Promise<AegisCoinWallet> {
    const res = await api.get('/api/aegiscoin/wallet');
    return res.data;
  },

  async rewardCoins(amount: number, transactionType: string, description: string) {
    const res = await api.post('/api/aegiscoin/reward', { amount, transactionType, description });
    return res.data;
  },

  async spendCoins(amount: number, itemCode: string, description: string) {
    const res = await api.post('/api/aegiscoin/spend', { amount, itemCode, description });
    return res.data;
  },

  // ── Peer Challenge ────────────────────────────────────────────────────────
  async createChallenge(examId: string, creatorName: string, score: number): Promise<{ shareCode: string; challenge: PeerChallenge }> {
    const res = await api.post('/api/peerchallenge/create', { examId, creatorName, score });
    return res.data;
  },

  async getChallenge(shareCode: string): Promise<PeerChallenge> {
    const res = await api.get(`/api/peerchallenge/${shareCode}`);
    return res.data;
  },

  async submitChallengeResult(shareCode: string, candidateIdentifier: string, score: number, timeSpentSeconds: number) {
    const res = await api.post('/api/peerchallenge/submit', { shareCode, candidateIdentifier, score, timeSpentSeconds });
    return res.data;
  },

  // ── Cognitive OS (DKT & Knowledge Graph) ───────────────────────────────────
  async getDktState(): Promise<DktState> {
    const res = await api.get('/api/cognitiveos/dkt/state');
    return res.data;
  },

  async recordDktInteraction(conceptCode: string, isCorrect: boolean, responseTimeMs: number): Promise<DktState> {
    const res = await api.post('/api/cognitiveos/dkt/interaction', { conceptCode, isCorrect, responseTimeMs });
    return res.data;
  },

  async getKnowledgeConcepts(domainCode: string = 'EDUCATION'): Promise<{ totalConcepts: number; concepts: KnowledgeGraphNode[] }> {
    const res = await api.get(`/api/cognitiveos/graph/concepts?domainCode=${domainCode}`);
    return res.data;
  },

  async traceRootGaps(conceptCode: string): Promise<{ targetConcept: string; rootGaps: string[] }> {
    const res = await api.get(`/api/cognitiveos/graph/root-gaps/${conceptCode}`);
    return res.data;
  },

  async getRemediationPath(conceptCode: string): Promise<{ targetConcept: string; remediationPath: KnowledgeGraphNode[] }> {
    const res = await api.get(`/api/cognitiveos/graph/remediation/${conceptCode}`);
    return res.data;
  },

  async getSocraticExplanation(questionId: string, questionContent: string, studentAnswer?: string): Promise<PedagogicalRagResponse> {
    const res = await api.post('/api/cognitiveos/rag/socratic-explanation', { questionId, questionContent, studentAnswer });
    return res.data;
  },
};
