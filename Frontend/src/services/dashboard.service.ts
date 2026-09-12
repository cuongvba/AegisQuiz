import api from '@/services/api';

const QUIZ_API = '/api/quiz';
const MENTOR_API = '/api/mentor';

export const dashboardService = {
  getStudyPlan: async (userId: string, isPremium = false) => {
    const response = await api.post(`${MENTOR_API}/plan`, { userId, isPremium });
    return response.data;
  },

  getWeakTopics: async (userId: string) => {
    try {
      const response = await api.get(`${QUIZ_API}/weak-topics?userId=${userId}`);
      return response.data;
    } catch {
      return [];
    }
  },

  getLeaderboard: async (period = 'weekly') => {
    const response = await api.get(`${QUIZ_API}/leaderboard?period=${period}`);
    return response.data;
  },
};