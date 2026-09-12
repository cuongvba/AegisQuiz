/**
 * useQuestions — TanStack Query hooks cho Admin Questions
 * =========================================================
 * Tách từ AdminQuestionsPage.tsx — data fetching logic.
 * FSD Layer: features/admin/manage-questions/
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { quizApi } from '@/shared/api/quiz-api';

export const QUERY_KEYS = {
  questions: ['admin', 'questions'] as const,
  topics:    ['admin', 'topics']    as const,
  contexts:  ['admin', 'contexts']  as const,
} as const;

// ── Queries ───────────────────────────────────────────────────────────────────

export function useAdminQuestions() {
  return useQuery({
    queryKey: QUERY_KEYS.questions,
    queryFn:  () => quizApi.getQuestions(),
    staleTime: 30_000,
  });
}

export function useAdminTopics() {
  return useQuery({
    queryKey: QUERY_KEYS.topics,
    queryFn:  () => quizApi.getTopics(),
    staleTime: 60_000,
  });
}

export function useAdminContexts() {
  return useQuery({
    queryKey: QUERY_KEYS.contexts,
    queryFn:  () => quizApi.getContexts(),
    staleTime: 60_000,
  });
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function useCreateQuestion() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: unknown) => quizApi.createQuestion(payload),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QUERY_KEYS.questions }),
  });
}

export function useUpdateQuestion() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: unknown }) =>
      quizApi.updateQuestion(id, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEYS.questions }),
  });
}

export function useDeleteQuestion() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => quizApi.deleteQuestion(id),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QUERY_KEYS.questions }),
  });
}

export function useBatchAssignContext() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: {
      questionIds: string[];
      contextId?: string;
      contextTitle?: string;
      contextContent?: string;
    }) => quizApi.batchAssignContext(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEYS.questions }),
  });
}

export function useBatchAssignTags() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: {
      questionIds: string[];
      tags: string[];
      domainCode?: string;
      targetLevel?: string;
      assessmentPurpose?: string;
      issuingOrg?: string;
      benchmarkYear?: number;
      benchmarkStandard?: string;
    }) => quizApi.batchAssignTags(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEYS.questions }),
  });
}

export function useConfirmImportExcel() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (items: unknown[]) => quizApi.confirmImportQuestionsExcel(items),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QUERY_KEYS.questions }),
  });
}

export function useConfirmImportDocx() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (items: unknown[]) => quizApi.confirmImportQuestionsDocx(items),
    onSuccess:  () => qc.invalidateQueries({ queryKey: QUERY_KEYS.questions }),
  });
}
