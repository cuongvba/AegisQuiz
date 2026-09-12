import { MOCK_QUESTIONS } from '@/data/mock-quiz';
import api from '@/services/api';
import type { LearnerQuestion, QuestionType } from '@/types/quiz';

// AegisQuiz backend endpoints (trỏ về /api/quiz — proxy qua Vite → localhost:8080)
const QUIZ_API = '/api/quiz';
const BANK_API_BASE = `${QUIZ_API}/bank`;

import {
  normalizeQuestion,
  normalizeQuestionType,
  looksLikeMultiFromAnswer,
  looksLikeMatchingFromTwoColumns,
  parsePayload,
  pickFirst,
} from '@/shared/lib/quiz/normalizers';

export {
  normalizeQuestion,
  normalizeQuestionType,
  looksLikeMultiFromAnswer,
  looksLikeMatchingFromTwoColumns,
  parsePayload,
  pickFirst,
};

export const learnerQuizService = {
  async getTopicCounts(): Promise<any[]> {
    try {
      const response = await api.get(`${QUIZ_API}/topics/counts`);
      return response.data;
    } catch {
      return [];
    }
  },

  async generatePracticeQuestions(config: any): Promise<LearnerQuestion[]> {
    try {
      const response = await api.post(`${QUIZ_API}/practice`, config);
      const rawList = Array.isArray(response.data)
        ? response.data
        : (Array.isArray(response.data?.items) ? response.data.items : (Array.isArray(response.data?.questions) ? response.data.questions : []));
      return rawList
        .map((item: any, index: number) => normalizeQuestion(item, index))
        .filter((item: any) => item.enabled);
    } catch {
      return MOCK_QUESTIONS;
    }
  },

  async getQuestions(params?: any): Promise<LearnerQuestion[]> {
    try {
      // Gỡ bỏ hạn chế 1000 câu hỏi: truyền all=true và pageSize=100000 để nhận toàn bộ ngân hàng câu hỏi
      const response = await api.get(`${QUIZ_API}/questions`, { params: { all: true, pageSize: 100000, ...params } });
      const rawList = Array.isArray(response.data)
        ? response.data
        : (Array.isArray(response.data?.items) ? response.data.items : []);
      const mapped = rawList
        .map((item: any, index: number) => normalizeQuestion(item, index))
        .filter((item: any) => item.enabled !== false);

      // Khi server phản hồi thành công (kể cả khi ngân hàng rỗng 0 câu)
      if (mapped.length === 0) {
        try {
          localStorage.removeItem('aegis_bank_questions');
        } catch {}
        return [];
      }

      if (mapped.length <= 1500) {
        try {
          localStorage.setItem('aegis_bank_questions', JSON.stringify(mapped));
        } catch {}
      }
      return mapped;
    } catch (err) {
      console.warn('Could not fetch questions from server, checking offline cache:', err);
    }

    // [Resilience Upgrade] Chỉ kiểm tra offline cache từ localStorage khi server thực sự lỗi/mất mạng
    try {
      const cached = localStorage.getItem('aegis_bank_questions');
      if (cached) {
        const parsed = JSON.parse(cached);
        if (Array.isArray(parsed) && parsed.length > 0) {
          return parsed.map((item: any, index: number) => normalizeQuestion(item, index));
        }
      }
    } catch {}

    return [];
  },

  async loadPresetSample(preset: 'MATH' | 'GPLX' | 'GPLX_F2023' = 'MATH'): Promise<any> {
    const res = await api.post(`${QUIZ_API}/questions/load-preset-sample?preset=${preset}`);
    return res.data;
  },

  // MODULE 1: EXAM ROOM & ADAPTIVE PRACTICE
  async submitAttempt(quizId: string, payload: any): Promise<any> {
    const res = await api.post(`${QUIZ_API}/attempt`, payload);
    return res.data;
  },

  async generateAdaptiveQuiz(payload: any): Promise<LearnerQuestion[]> {
    const res = await api.post(`${QUIZ_API}/adaptive`, payload);
    const questions = Array.isArray(res.data?.questions) ? res.data.questions : (Array.isArray(res.data) ? res.data : []);
    return questions.map((item: any, index: number) => normalizeQuestion(item, index));
  },

  // MODULE: IRT/CAT 3-PL COMPUTERIZED ADAPTIVE TESTING
  async startCatSession(payload: { userId?: string; topicCode?: string; minItems?: number; maxItems?: number; seThreshold?: number }): Promise<any> {
    const res = await api.post(`${QUIZ_API}/cat/start`, payload);
    if (res.data?.question) {
      res.data.normalizedQuestion = normalizeQuestion(res.data.question, 0);
    }
    return res.data;
  },

  async respondCatSession(sessionId: string, payload: { questionId: string; selectedAnswer: string; timeSpentSeconds?: number }): Promise<any> {
    const res = await api.post(`${QUIZ_API}/cat/${sessionId}/respond`, payload);
    if (res.data?.nextQuestion) {
      res.data.normalizedNextQuestion = normalizeQuestion(res.data.nextQuestion, res.data.administeredCount ?? 0);
    }
    return res.data;
  },

  async getCatResult(sessionId: string): Promise<any> {
    const res = await api.get(`${QUIZ_API}/cat/${sessionId}/result`);
    return res.data;
  },

  // MODULE 2: AI EXPLAIN & ANALYZE
  async explainQuestion(questionId: string, payload: any = {}): Promise<any> {
    const res = await api.post(`${QUIZ_API}/${questionId}/explain`, payload);
    return res.data;
  },

  async analyzeAttempt(payload: any = {}): Promise<any> {
    const res = await api.post(`${QUIZ_API}/attempt/analyze`, payload);
    return res.data;
  },

  // MODULE 3: LEARNING PATHS
  async getLearningPaths(): Promise<any[]> {
    try {
      const res = await api.get(`${QUIZ_API}/paths`);
      return res.data;
    } catch {
      return [];
    }
  },

  async getWeakTopics(personId: string): Promise<any[]> {
    try {
      const res = await api.get(`${QUIZ_API}/weak-topics?userId=${personId}`);
      return res.data;
    } catch {
      return [];
    }
  },

  // MODULE 4: GAMIFICATION
  async getLeaderboard(period = 'weekly'): Promise<any> {
    const res = await api.get(`${QUIZ_API}/leaderboard?period=${period}`);
    return res.data;
  },

  async getAchievements(userId?: string): Promise<any[]> {
    const params = userId ? `?userId=${userId}` : '';
    const res = await api.get(`${QUIZ_API}/achievements${params}`);
    return Array.isArray(res.data) ? res.data : [];
  },

  async checkAchievements(personId: string): Promise<any> {
    const res = await api.post(`${QUIZ_API}/achievements/check?userId=${personId}`, {});
    return res.data;
  },

  // AI MENTOR
  async getStudyPlan(userId: string, isPremium = false): Promise<string> {
    try {
      const res = await api.post('/api/mentor/plan', { userId, isPremium });
      return res.data?.plan ?? '';
    } catch {
      return '';
    }
  },

  // QUESTION BANK CRUD
  async createQuestion(question: any): Promise<any> {
    const res = await api.post(`${QUIZ_API}/questions`, question);
    return res.data;
  },

  async updateQuestion(id: string, question: any): Promise<any> {
    const res = await api.put(`${QUIZ_API}/questions/${id}`, question);
    return res.data;
  },

  async deleteQuestion(id: string): Promise<any> {
    const res = await api.delete(`${QUIZ_API}/questions/${id}`);
    return res.data;
  },

  // EXCEL IMPORT (DOT-2026 Smart Format — trả về preview)
  async importQuestions(file: File): Promise<any> {
    const formData = new FormData();
    formData.append('file', file);
    const res = await api.post(`${QUIZ_API}/questions/import`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      timeout: 60000,
    });
    // Server trả về { fileName, totalSheets, totalQuestions, warnings, sheets: [{sheetName, detectedTopicCode, detectedTopicName, questionCount, questions:[]}] }
    return res.data;
  },

  // XÁC NHẬN nhập câu hỏi Excel (sau khi user review preview)
  async confirmImportQuestionsExcel(items: any[]): Promise<any> {
    const res = await api.post(`${QUIZ_API}/questions/confirm-import-excel`, items, {
      timeout: 60000,
    });
    return res.data;
  },

  // CHUẨN HÓA TIẾNG VIỆT THEO YÊU CẦU (On-Demand VNCorrect & Maprepl)
  async correctVietnameseQuestions(items: any[]): Promise<any[]> {
    const res = await api.post(`${QUIZ_API}/questions/correct-vietnamese`, items, {
      timeout: 60000,
    });
    return res.data;
  },

  // TOPIC MANAGEMENT CRUD
  async getTopics(): Promise<any[]> {
    try {
      const res = await api.get(`${QUIZ_API}/topics`);
      return Array.isArray(res.data) ? res.data : [];
    } catch {
      return [];
    }
  },

  async getTopicTree(params?: { domainCode?: string; scope?: string; search?: string }): Promise<any[]> {
    try {
      const res = await api.get(`${QUIZ_API}/topics/tree`, { params });
      return Array.isArray(res.data) ? res.data : [];
    } catch {
      return [];
    }
  },

  async createTopic(topic: any): Promise<any> {
    const res = await api.post(`${QUIZ_API}/topics`, topic);
    return res.data;
  },

  async updateTopic(id: string, topic: any): Promise<any> {
    const res = await api.put(`${QUIZ_API}/topics/${id}`, topic);
    return res.data;
  },

  async deleteTopic(id: string, deleteQuestions?: boolean): Promise<any> {
    const res = await api.delete(`${QUIZ_API}/topics/${id}${deleteQuestions ? '?deleteQuestions=true' : ''}`);
    return res.data;
  },

  // DOCX & PDF IMPORT WITH AI PREDICT SOLVER (Kịch bản A)
  async importQuestionsDocx(file: File, useAi = false): Promise<any[]> {
    const formData = new FormData();
    formData.append('file', file);
    const res = await api.post(`${QUIZ_API}/questions/import-docx?useAi=${useAi}`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      timeout: 120000,
    });
    return res.data;
  },

  async importQuestionsPdf(file: File, useAi = false): Promise<any[]> {
    const formData = new FormData();
    formData.append('file', file);
    const res = await api.post(`${QUIZ_API}/questions/import-pdf?useAi=${useAi}`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      timeout: 120000,
    });
    return res.data;
  },

  // AI QUESTION GENERATOR TỪ TÀI LIỆU (Kịch bản B)
  async generateQuestionsFromDoc(
    file: File,
    options?: { count?: number; type?: string; difficulty?: number; topic?: string; focusArea?: string }
  ): Promise<any[]> {
    const formData = new FormData();
    formData.append('file', file);
    const params = new URLSearchParams();
    if (options?.count) params.append('count', String(options.count));
    if (options?.type) params.append('type', options.type);
    if (options?.difficulty) params.append('difficulty', String(options.difficulty));
    if (options?.topic) params.append('topic', options.topic);
    if (options?.focusArea) params.append('focusArea', options.focusArea);

    const res = await api.post(`${QUIZ_API}/questions/generate-from-doc?${params.toString()}`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      timeout: 180000,
    });
    return res.data;
  },

  async generateQuestionsFromText(request: {
    documentText: string;
    questionCount?: number;
    questionType?: string;
    difficulty?: number;
    topicCode?: string;
    focusArea?: string;
  }): Promise<any[]> {
    const res = await api.post(`${QUIZ_API}/questions/generate-from-text`, request, {
      timeout: 180000,
    });
    return res.data;
  },

  async aiSolvePreview(items: any[]): Promise<any[]> {
    const res = await api.post(`${QUIZ_API}/questions/ai-solve-preview`, items, {
      timeout: 120000,
    });
    return res.data;
  },

  async convertMathImagesToLatex(items: any[]): Promise<any[]> {
    const res = await api.post(`${QUIZ_API}/questions/convert-math-latex`, items, {
      timeout: 120000,
    });
    return res.data;
  },

  async confirmImportQuestionsDocx(items: any[]): Promise<any> {
    const res = await api.post(`${QUIZ_API}/questions/confirm-import-docx`, items, {
      timeout: 120000,
    });
    return res.data;
  },

  // MISSING ANSWER AI DETECTOR & SOLVER
  async getQuestionsMissingAnswers(): Promise<any[]> {
    const res = await api.get(`${QUIZ_API}/questions/missing-answers`);
    return Array.isArray(res.data) ? res.data : [];
  },

  async suggestQuestionAnswer(id: string): Promise<any> {
    const res = await api.post(`${QUIZ_API}/questions/${id}/suggest-answer`);
    return res.data;
  },

  async updateQuestionAnswer(id: string, payload: any): Promise<any> {
    const res = await api.put(`${QUIZ_API}/questions/${id}/update-answer`, payload);
    return res.data;
  },

  // UNIVERSAL SHARED CONTEXT APIS
  async getContexts(): Promise<any[]> {
    try {
      const res = await api.get(`${QUIZ_API}/contexts`);
      return Array.isArray(res.data) ? res.data : [];
    } catch {
      return [];
    }
  },

  async batchAssignContext(data: { questionIds: string[]; contextId?: string; contextTitle?: string; contextContent?: string }): Promise<any> {
    const res = await api.post(`${QUIZ_API}/questions/batch-assign-context`, data);
    return res.data;
  },

  async batchRemoveContext(questionIds: string[]): Promise<any> {
    const res = await api.post(`${QUIZ_API}/questions/batch-remove-context`, { questionIds });
    return res.data;
  },

  // UNIVERSAL MULTI-INDUSTRY & SMART TAG APIS
  async batchAssignTags(data: {
    questionIds: string[];
    tags: string[];
    domainCode?: string;
    targetLevel?: string;
    assessmentPurpose?: string;
    issuingOrg?: string;
    benchmarkYear?: number;
    benchmarkStandard?: string;
  }): Promise<any> {
    const res = await api.post(`${QUIZ_API}/questions/batch-tags`, data);
    return res.data;
  },

  async getTagSuggestions(domainCode?: string): Promise<{ tag: string; count: number }[]> {
    try {
      const res = await api.get(`${QUIZ_API}/tags/suggestions`, {
        params: domainCode ? { domainCode } : undefined,
      });
      return Array.isArray(res.data) ? res.data : [];
    } catch {
      return [];
    }
  },

  async autoSniffCoordinates(payload?: { questionIds?: string[]; previewOnly?: boolean }): Promise<any> {
    const res = await api.post(`${QUIZ_API}/questions/auto-sniff-coordinates`, payload || {});
    return res.data;
  },

  // DYNAMIC DOMAIN TAXONOMY APIS (KỲ QUAN 16)
  async getDomains(): Promise<any[]> {
    try {
      const res = await api.get(`${QUIZ_API}/domains`);
      return Array.isArray(res.data) ? res.data : [];
    } catch {
      return [];
    }
  },

  async getDomainCatalog(): Promise<any[]> {
    try {
      const res = await api.get(`${QUIZ_API}/domains/catalog`);
      return Array.isArray(res.data) ? res.data : [];
    } catch {
      return [];
    }
  },

  async createCustomDomain(data: {
    code: string;
    name: string;
    description?: string;
    icon?: string;
    colorBadge?: string;
    parentDomainCode?: string;
    displayOrder?: number;
  }): Promise<any> {
    const res = await api.post(`${QUIZ_API}/domains`, data);
    return res.data;
  },

  async updateTenantDomainConfigs(configs: {
    domainCode: string;
    isEnabled: boolean;
    customDisplayName?: string;
    displayOrder: number;
  }[]): Promise<any> {
    const res = await api.put(`${QUIZ_API}/domains/configs`, configs);
    return res.data;
  },

  async getCoordinatePresets(domainCode?: string, coordinateType?: string): Promise<any[]> {
    try {
      const res = await api.get(`${QUIZ_API}/domains/presets`, {
        params: { domainCode, coordinateType }
      });
      return Array.isArray(res.data) ? res.data : [];
    } catch {
      return [];
    }
  },

  async createCoordinatePreset(data: {
    domainCode: string;
    coordinateType: string;
    presetCode?: string;
    presetLabel: string;
    synonymsJson?: string;
    isDefault?: boolean;
    displayOrder?: number;
  }): Promise<any> {
    const res = await api.post(`${QUIZ_API}/domains/presets`, data);
    return res.data;
  },

  async deleteCoordinatePreset(id: string): Promise<any> {
    const res = await api.delete(`${QUIZ_API}/domains/presets/${id}`);
    return res.data;
  },

  // 5-TIER HIERARCHY ORGANIZATION APIS (KỲ QUAN 16)
  async getOrgUnitsTree(): Promise<any[]> {
    try {
      const res = await api.get(`${QUIZ_API}/org-units/tree`);
      return Array.isArray(res.data) ? res.data : [];
    } catch {
      return [];
    }
  },

  async getFlatOrgUnits(unitType?: number): Promise<any[]> {
    try {
      const res = await api.get(`${QUIZ_API}/org-units`, {
        params: unitType !== undefined ? { unitType } : undefined
      });
      return Array.isArray(res.data) ? res.data : [];
    } catch {
      return [];
    }
  },

  async createOrgUnit(data: {
    parentId?: string | null;
    code: string;
    name: string;
    unitType: number;
    email?: string;
    phoneNumber?: string;
    address?: string;
    displayOrder?: number;
  }): Promise<any> {
    const res = await api.post(`${QUIZ_API}/org-units`, data);
    return res.data;
  },

  async updateOrgUnit(id: string, data: any): Promise<any> {
    const res = await api.put(`${QUIZ_API}/org-units/${id}`, data);
    return res.data;
  },

  async deleteOrgUnit(id: string): Promise<any> {
    const res = await api.delete(`${QUIZ_API}/org-units/${id}`);
    return res.data;
  }
};


