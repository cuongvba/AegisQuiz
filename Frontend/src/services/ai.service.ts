import api from '@/services/api';

const QUIZ_API = '/api/quiz';
const MENTOR_API = '/api/mentor';

export const aiService = {
  explainAnswer: async (questionId: string, selectedAnswer: string) => {
    const response = await api.post(`${QUIZ_API}/${questionId}/explain`, { selectedAnswer });
    return response.data;
  },

  analyzeAttempt: async (attempts: any[]) => {
    const response = await api.post(`${QUIZ_API}/attempt/analyze`, { attempts });
    return response.data;
  },

  getStudyPlan: async (userId: string, isPremium = false) => {
    const response = await api.post(`${MENTOR_API}/plan`, { userId, isPremium });
    return response.data;
  },
};