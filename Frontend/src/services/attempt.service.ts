import api from '@/services/api';

const QUIZ_API = '/api/quiz';

export const attemptService = {
  submitAttempt: async (_quizId: string, payload: any) => {
    const response = await api.post(`${QUIZ_API}/attempt`, payload);
    return response.data;
  },

  getAdaptiveQuiz: async (payload: any) => {
    const response = await api.post(`${QUIZ_API}/adaptive`, payload);
    return response.data;
  },

  getAttemptAnswers: async (_quizId: string, _attemptId: string) => {
    // Lấy từ localStorage (saved by SecureExamRoom/AttemptRoom)
    const raw = localStorage.getItem('attempt_review_data');
    return raw ? JSON.parse(raw) : null;
  },
};
