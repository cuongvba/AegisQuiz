import api from './api';

export interface FsrsDueItem {
  stateId: string;
  questionId: string;
  content: string;
  categoryCode?: string;
  difficulty: number;
  stability: number;
  retrievability: number;
  reps: number;
  lapses: number;
  dueAt: string;
  intervals: {
    again: string;
    hard: string;
    good: string;
    easy: string;
  };
}

export interface FsrsSummary {
  totalCardsLearned: number;
  dueToday: number;
  relearningCount: number;
  averageStabilityDays: number;
  retentionRateEstimate: number;
  streakDays: number;
}

export const cognitiveService = {
  async getDueQuestions(limit: number = 20): Promise<{ totalDue: number; items: FsrsDueItem[] }> {
    const res = await api.get(`/api/cognitive/due?limit=${limit}`);
    return res.data;
  },

  async submitReview(questionId: string, rating: 1 | 2 | 3 | 4, responseTimeMs: number = 0) {
    const res = await api.post('/api/cognitive/review', {
      questionId,
      rating,
      responseTimeMs,
    });
    return res.data;
  },

  async getSummary(): Promise<FsrsSummary> {
    const res = await api.get('/api/cognitive/summary');
    return res.data;
  },
};
