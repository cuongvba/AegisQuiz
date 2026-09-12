import api from '@/services/api';

const QUIZ_API = '/api/quiz';

export const gamificationService = {
  getLeaderboard: async (period = 'weekly', top = 20) => {
    const response = await api.get(`${QUIZ_API}/leaderboard?period=${period}&top=${top}`);
    return response.data;
  },

  getAllAchievements: async (userId?: string) => {
    const params = userId ? `?userId=${userId}` : '';
    const response = await api.get(`${QUIZ_API}/achievements${params}`);
    return Array.isArray(response.data) ? response.data : [];
  },

  seedDefaultAchievements: async () => {
    const response = await api.post(`${QUIZ_API}/achievements/seed-defaults`);
    return response.data;
  },

  checkNewAchievements: async (userId: string) => {
    try {
      const response = await api.post(`${QUIZ_API}/achievements/check?userId=${userId}`);
      return response.data;
    } catch {
      return [];
    }
  },
};