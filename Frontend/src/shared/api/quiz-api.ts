/**
 * AegisQuiz — Quiz API Client (HTTP calls only)
 * ===============================================
 * Tách từ learner-quiz.service.ts — chỉ chứa HTTP calls thuần.
 * Tất cả parsing/normalization logic đã chuyển vào:
 *   shared/lib/quiz/normalizers.ts
 *
 * FSD Layer: shared/api/
 *
 * Cách dùng:
 *   import { quizApi } from '@/shared/api/quiz-api';
 *   const questions = await quizApi.getQuestions();
 */

import { apiClient } from './client';
import { normalizeQuestion } from '@/shared/lib/quiz/normalizers';
import { MOCK_QUESTIONS } from '@/data/mock-quiz';
import type { LearnerQuestion } from '@/types/quiz';

const QUIZ_API      = '/api/quiz';
const BANK_API_BASE = `${QUIZ_API}/bank`;

// Re-export để backward compat nếu code nào import trực tiếp
export { QUIZ_API, BANK_API_BASE };

// ── Helper: normalize list response ────────────────────────────────────────

function extractList(data: unknown): unknown[] {
  if (Array.isArray(data)) return data;
  if (data && typeof data === 'object') {
    const d = data as Record<string, unknown>;
    if (Array.isArray(d.items))     return d.items;
    if (Array.isArray(d.questions)) return d.questions;
  }
  return [];
}

// ── Quiz API ──────────────────────────────────────────────────────────────────

export const quizApi = {

  // ── Practice & Adaptive ────────────────────────────────────────────────────

  async generatePracticeQuestions(config: unknown): Promise<LearnerQuestion[]> {
    try {
      const res  = await apiClient.post(`${QUIZ_API}/practice`, config);
      const list = extractList(res.data);
      return list
        .map((item, i) => normalizeQuestion(item, i))
        .filter((q) => q.enabled);
    } catch {
      return MOCK_QUESTIONS;
    }
  },

  async getQuestions(params?: Record<string, unknown>): Promise<LearnerQuestion[]> {
    try {
      const res  = await apiClient.get(`${QUIZ_API}/questions`, {
        params: { all: true, pageSize: 100000, ...params },
      });
      const list = extractList(res.data);
      const mapped = list
        .map((item, i) => normalizeQuestion(item, i))
        .filter((q) => q.enabled !== false);

      if (mapped.length === 0) {
        try { localStorage.removeItem('aegis_bank_questions'); } catch {}
        return [];
      }
      if (mapped.length <= 1500) {
        try { localStorage.setItem('aegis_bank_questions', JSON.stringify(mapped)); } catch {}
      }
      return mapped;
    } catch (err) {
      console.warn('[quizApi] getQuestions failed, checking offline cache:', err);
    }

    // Offline cache fallback
    try {
      const cached = localStorage.getItem('aegis_bank_questions');
      if (cached) {
        const parsed = JSON.parse(cached) as unknown[];
        if (Array.isArray(parsed) && parsed.length > 0) {
          return parsed.map((item, i) => normalizeQuestion(item, i));
        }
      }
    } catch {}

    return [];
  },

  async loadPresetSample(preset: 'MATH' | 'GPLX' | 'GPLX_F2023' = 'MATH'): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/questions/load-preset-sample?preset=${preset}`);
    return res.data;
  },

  // ── Exam Room & Submission ─────────────────────────────────────────────────

  async submitAttempt(_quizId: string, payload: unknown): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/attempt`, payload);
    return res.data;
  },

  async generateAdaptiveQuiz(payload: unknown): Promise<LearnerQuestion[]> {
    const res  = await apiClient.post(`${QUIZ_API}/adaptive`, payload);
    const list = extractList(res.data);
    return list.map((item, i) => normalizeQuestion(item, i));
  },

  // ── IRT/CAT Adaptive Testing ───────────────────────────────────────────────

  async startCatSession(payload: {
    userId?: string; topicCode?: string;
    minItems?: number; maxItems?: number; seThreshold?: number;
  }): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/cat/start`, payload);
    if (res.data?.question) {
      (res.data as Record<string, unknown>).normalizedQuestion = normalizeQuestion(res.data.question, 0);
    }
    return res.data;
  },

  async respondCatSession(
    sessionId: string,
    payload: { questionId: string; selectedAnswer: string; timeSpentSeconds?: number },
  ): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/cat/${sessionId}/respond`, payload);
    if (res.data?.nextQuestion) {
      (res.data as Record<string, unknown>).normalizedNextQuestion =
        normalizeQuestion(res.data.nextQuestion, (res.data as Record<string, unknown>).administeredCount as number ?? 0);
    }
    return res.data;
  },

  async getCatResult(sessionId: string): Promise<unknown> {
    const res = await apiClient.get(`${QUIZ_API}/cat/${sessionId}/result`);
    return res.data;
  },

  // ── AI Features ────────────────────────────────────────────────────────────

  async explainQuestion(questionId: string, payload: unknown = {}): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/${questionId}/explain`, payload);
    return res.data;
  },

  async analyzeAttempt(payload: unknown = {}): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/attempt/analyze`, payload);
    return res.data;
  },

  async getStudyPlan(userId: string, isPremium = false): Promise<string> {
    try {
      const res = await apiClient.post('/api/mentor/plan', { userId, isPremium });
      return (res.data as Record<string, unknown>)?.plan as string ?? '';
    } catch {
      return '';
    }
  },

  // ── Learning Paths ─────────────────────────────────────────────────────────

  async getLearningPaths(): Promise<unknown[]> {
    try {
      const res = await apiClient.get(`${QUIZ_API}/paths`);
      return res.data as unknown[];
    } catch { return []; }
  },

  async getWeakTopics(userId: string): Promise<unknown[]> {
    try {
      const res = await apiClient.get(`${QUIZ_API}/weak-topics?userId=${userId}`);
      return res.data as unknown[];
    } catch { return []; }
  },

  // ── Gamification ───────────────────────────────────────────────────────────

  async getLeaderboard(period = 'weekly'): Promise<unknown> {
    const res = await apiClient.get(`${QUIZ_API}/leaderboard?period=${period}`);
    return res.data;
  },

  async getAchievements(userId?: string): Promise<unknown[]> {
    const params = userId ? `?userId=${userId}` : '';
    const res    = await apiClient.get(`${QUIZ_API}/achievements${params}`);
    return Array.isArray(res.data) ? res.data : [];
  },

  async checkAchievements(userId: string): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/achievements/check?userId=${userId}`, {});
    return res.data;
  },

  // ── Question Bank CRUD ─────────────────────────────────────────────────────

  async createQuestion(question: unknown): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/questions`, question);
    return res.data;
  },

  async updateQuestion(id: string, question: unknown): Promise<unknown> {
    const res = await apiClient.put(`${QUIZ_API}/questions/${id}`, question);
    return res.data;
  },

  async deleteQuestion(id: string): Promise<unknown> {
    const res = await apiClient.delete(`${QUIZ_API}/questions/${id}`);
    return res.data;
  },

  async getTopicCounts(): Promise<unknown[]> {
    try {
      const res = await apiClient.get(`${QUIZ_API}/topics/counts`);
      return res.data as unknown[];
    } catch { return []; }
  },

  // ── Import / Export ────────────────────────────────────────────────────────

  async importQuestions(file: File): Promise<unknown> {
    const formData = new FormData();
    formData.append('file', file);
    const res = await apiClient.post(`${QUIZ_API}/questions/import`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 60000,
    });
    return res.data;
  },

  async confirmImportQuestionsExcel(items: unknown[]): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/questions/confirm-import-excel`, items, { timeout: 60000 });
    return res.data;
  },

  async correctVietnameseQuestions(items: unknown[]): Promise<unknown[]> {
    const res = await apiClient.post(`${QUIZ_API}/questions/correct-vietnamese`, items, { timeout: 60000 });
    return res.data as unknown[];
  },

  async importQuestionsDocx(file: File, useAi = false): Promise<unknown[]> {
    const formData = new FormData();
    formData.append('file', file);
    const res = await apiClient.post(`${QUIZ_API}/questions/import-docx?useAi=${useAi}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }, timeout: 120000,
    });
    return res.data as unknown[];
  },

  async importQuestionsPdf(file: File, useAi = false): Promise<unknown[]> {
    const formData = new FormData();
    formData.append('file', file);
    const res = await apiClient.post(`${QUIZ_API}/questions/import-pdf?useAi=${useAi}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }, timeout: 120000,
    });
    return res.data as unknown[];
  },

  async generateQuestionsFromDoc(
    file: File,
    options?: { count?: number; type?: string; difficulty?: number; topic?: string; focusArea?: string },
  ): Promise<unknown[]> {
    const formData = new FormData();
    formData.append('file', file);
    const params = new URLSearchParams();
    if (options?.count)      params.append('count',      String(options.count));
    if (options?.type)       params.append('type',       options.type);
    if (options?.difficulty) params.append('difficulty', String(options.difficulty));
    if (options?.topic)      params.append('topic',      options.topic);
    if (options?.focusArea)  params.append('focusArea',  options.focusArea);
    const res = await apiClient.post(`${QUIZ_API}/questions/generate-from-doc?${params.toString()}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }, timeout: 180000,
    });
    return res.data as unknown[];
  },

  async generateQuestionsFromText(request: {
    documentText: string; questionCount?: number; questionType?: string;
    difficulty?: number; topicCode?: string; focusArea?: string;
  }): Promise<unknown[]> {
    const res = await apiClient.post(`${QUIZ_API}/questions/generate-from-text`, request, { timeout: 180000 });
    return res.data as unknown[];
  },

  async aiSolvePreview(items: unknown[]): Promise<unknown[]> {
    const res = await apiClient.post(`${QUIZ_API}/questions/ai-solve-preview`, items, { timeout: 120000 });
    return res.data as unknown[];
  },

  async convertMathImagesToLatex(items: unknown[]): Promise<unknown[]> {
    const res = await apiClient.post(`${QUIZ_API}/questions/convert-math-latex`, items, { timeout: 120000 });
    return res.data as unknown[];
  },

  async confirmImportQuestionsDocx(items: unknown[]): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/questions/confirm-import-docx`, items, { timeout: 120000 });
    return res.data;
  },

  async getQuestionsMissingAnswers(): Promise<unknown[]> {
    const res = await apiClient.get(`${QUIZ_API}/questions/missing-answers`);
    return Array.isArray(res.data) ? res.data : [];
  },

  async suggestQuestionAnswer(id: string): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/questions/${id}/suggest-answer`);
    return res.data;
  },

  async updateQuestionAnswer(id: string, payload: unknown): Promise<unknown> {
    const res = await apiClient.put(`${QUIZ_API}/questions/${id}/update-answer`, payload);
    return res.data;
  },

  // ── Topics CRUD ────────────────────────────────────────────────────────────

  async getTopics(): Promise<unknown[]> {
    try {
      const res = await apiClient.get(`${QUIZ_API}/topics`);
      return Array.isArray(res.data) ? res.data : [];
    } catch { return []; }
  },

  async getTopicTree(params?: { domainCode?: string; scope?: string; search?: string }): Promise<unknown[]> {
    try {
      const res = await apiClient.get(`${QUIZ_API}/topics/tree`, { params });
      return Array.isArray(res.data) ? res.data : [];
    } catch { return []; }
  },

  async createTopic(topic: unknown): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/topics`, topic);
    return res.data;
  },

  async updateTopic(id: string, topic: unknown): Promise<unknown> {
    const res = await apiClient.put(`${QUIZ_API}/topics/${id}`, topic);
    return res.data;
  },

  async deleteTopic(id: string, deleteQuestions?: boolean): Promise<unknown> {
    const res = await apiClient.delete(
      `${QUIZ_API}/topics/${id}${deleteQuestions ? '?deleteQuestions=true' : ''}`,
    );
    return res.data;
  },

  // ── Context CRUD ───────────────────────────────────────────────────────────

  async getContexts(): Promise<unknown[]> {
    try {
      const res = await apiClient.get(`${QUIZ_API}/contexts`);
      return Array.isArray(res.data) ? res.data : [];
    } catch { return []; }
  },

  async batchAssignContext(data: {
    questionIds: string[]; contextId?: string; contextTitle?: string; contextContent?: string;
  }): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/questions/batch-assign-context`, data);
    return res.data;
  },

  async batchRemoveContext(questionIds: string[]): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/questions/batch-remove-context`, { questionIds });
    return res.data;
  },

  // ── Tags & Domain ──────────────────────────────────────────────────────────

  async batchAssignTags(data: {
    questionIds: string[]; tags: string[]; domainCode?: string; targetLevel?: string;
    assessmentPurpose?: string; issuingOrg?: string; benchmarkYear?: number; benchmarkStandard?: string;
  }): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/questions/batch-tags`, data);
    return res.data;
  },

  async getTagSuggestions(domainCode?: string): Promise<{ tag: string; count: number }[]> {
    try {
      const res = await apiClient.get(`${QUIZ_API}/tags/suggestions`, {
        params: domainCode ? { domainCode } : undefined,
      });
      return Array.isArray(res.data) ? res.data : [];
    } catch { return []; }
  },

  async autoSniffCoordinates(payload?: { questionIds?: string[]; previewOnly?: boolean }): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/questions/auto-sniff-coordinates`, payload ?? {});
    return res.data;
  },

  async getDomains(): Promise<unknown[]> {
    try {
      const res = await apiClient.get(`${QUIZ_API}/domains`);
      return Array.isArray(res.data) ? res.data : [];
    } catch { return []; }
  },

  async getDomainCatalog(): Promise<unknown[]> {
    try {
      const res = await apiClient.get(`${QUIZ_API}/domains/catalog`);
      return Array.isArray(res.data) ? res.data : [];
    } catch { return []; }
  },

  async createCustomDomain(data: {
    code: string; name: string; description?: string; icon?: string;
    colorBadge?: string; parentDomainCode?: string; displayOrder?: number;
  }): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/domains`, data);
    return res.data;
  },

  async updateTenantDomainConfigs(configs: {
    domainCode: string; isEnabled: boolean; customDisplayName?: string; displayOrder: number;
  }[]): Promise<unknown> {
    const res = await apiClient.put(`${QUIZ_API}/domains/configs`, configs);
    return res.data;
  },

  async getCoordinatePresets(domainCode?: string, coordinateType?: string): Promise<unknown[]> {
    try {
      const res = await apiClient.get(`${QUIZ_API}/domains/presets`, { params: { domainCode, coordinateType } });
      return Array.isArray(res.data) ? res.data : [];
    } catch { return []; }
  },

  async createCoordinatePreset(data: {
    domainCode: string; coordinateType: string; presetCode?: string; presetLabel: string;
    synonymsJson?: string; isDefault?: boolean; displayOrder?: number;
  }): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/domains/presets`, data);
    return res.data;
  },

  async deleteCoordinatePreset(id: string): Promise<unknown> {
    const res = await apiClient.delete(`${QUIZ_API}/domains/presets/${id}`);
    return res.data;
  },

  // ── Org Units ──────────────────────────────────────────────────────────────

  async getOrgUnitsTree(): Promise<unknown[]> {
    try {
      const res = await apiClient.get(`${QUIZ_API}/org-units/tree`);
      return Array.isArray(res.data) ? res.data : [];
    } catch { return []; }
  },

  async getFlatOrgUnits(unitType?: number): Promise<unknown[]> {
    try {
      const res = await apiClient.get(`${QUIZ_API}/org-units`, {
        params: unitType !== undefined ? { unitType } : undefined,
      });
      return Array.isArray(res.data) ? res.data : [];
    } catch { return []; }
  },

  async createOrgUnit(data: {
    parentId?: string | null; code: string; name: string; unitType: number;
    email?: string; phoneNumber?: string; address?: string; displayOrder?: number;
  }): Promise<unknown> {
    const res = await apiClient.post(`${QUIZ_API}/org-units`, data);
    return res.data;
  },

  async updateOrgUnit(id: string, data: unknown): Promise<unknown> {
    const res = await apiClient.put(`${QUIZ_API}/org-units/${id}`, data);
    return res.data;
  },

  async deleteOrgUnit(id: string): Promise<unknown> {
    const res = await apiClient.delete(`${QUIZ_API}/org-units/${id}`);
    return res.data;
  },
};
