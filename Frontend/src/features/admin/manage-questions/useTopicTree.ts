/**
 * useTopicTree — TanStack Query hook cho Topic Tree
 * ===================================================
 * Tách từ AdminQuestionsPage.tsx.
 * FSD Layer: features/admin/manage-questions/
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { quizApi } from '@/shared/api/quiz-api';
import type { BankTopic } from '@/pages/admin/questions/types';

export const TOPIC_KEYS = {
  tree:   ['admin', 'topics', 'tree']   as const,
  flat:   ['admin', 'topics', 'flat']   as const,
  counts: ['admin', 'topics', 'counts'] as const,
} as const;

// ── Queries ───────────────────────────────────────────────────────────────────

export function useTopicFlat() {
  return useQuery({
    queryKey: TOPIC_KEYS.flat,
    queryFn:  () => quizApi.getTopics() as Promise<BankTopic[]>,
    staleTime: 60_000,
  });
}

export function useTopicTree(params?: { domainCode?: string; scope?: string; search?: string }) {
  return useQuery({
    queryKey: [...TOPIC_KEYS.tree, params],
    queryFn:  () => quizApi.getTopicTree(params) as Promise<BankTopic[]>,
    staleTime: 60_000,
  });
}

export function useTopicCounts() {
  return useQuery({
    queryKey: TOPIC_KEYS.counts,
    queryFn:  () => quizApi.getTopicCounts(),
    staleTime: 30_000,
  });
}

// ── Mutations ─────────────────────────────────────────────────────────────────

export function useCreateTopic() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (topic: unknown) => quizApi.createTopic(topic),
    onSuccess:  () => {
      qc.invalidateQueries({ queryKey: TOPIC_KEYS.flat });
      qc.invalidateQueries({ queryKey: TOPIC_KEYS.tree });
    },
  });
}

export function useUpdateTopic() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, topic }: { id: string; topic: unknown }) =>
      quizApi.updateTopic(id, topic),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: TOPIC_KEYS.flat });
      qc.invalidateQueries({ queryKey: TOPIC_KEYS.tree });
    },
  });
}

export function useDeleteTopic() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, deleteQuestions }: { id: string; deleteQuestions?: boolean }) =>
      quizApi.deleteTopic(id, deleteQuestions),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: TOPIC_KEYS.flat });
      qc.invalidateQueries({ queryKey: TOPIC_KEYS.tree });
    },
  });
}
