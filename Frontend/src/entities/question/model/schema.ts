/**
 * AegisQuiz — Question Entity Model + Zod Schemas
 * =================================================
 * SSOT cho Question domain model.
 * FSD Layer: entities/question/model/
 */

import { z } from 'zod';

// ── Question Types ────────────────────────────────────────────────────────────

export const QuestionTypeSchema = z.enum([
  'SINGLE',
  'MULTI',
  'TRUE_FALSE',
  'SHORT_ANSWER',
  'FILL_BLANK',
  'ORDERING',
  'MATCHING',
  'ESSAY',
]);

export type QuestionType = z.infer<typeof QuestionTypeSchema>;

// ── Question Schema ───────────────────────────────────────────────────────────

export const LearnerQuestionSchema = z.object({
  id:              z.string(),
  content:         z.string().min(1),
  questionType:    QuestionTypeSchema.default('SINGLE'),
  options:         z.array(z.string()).default([]),
  explanation:     z.string().optional(),
  points:          z.number().int().nonnegative().default(10),
  timeLimitSeconds:z.number().int().positive().optional(),
  difficulty:      z.number().int().min(1).max(5).optional(),
  topicCode:       z.string().optional(),
  domainCode:      z.string().optional(),
  imageUrl:        z.string().url().optional(),
  // Matching type
  matchingPairs:   z.array(z.object({ left: z.string(), right: z.string() })).optional(),
  // Admin-visible
  answerRaw:       z.string().optional(),
  category:        z.string().optional(),
  // Ownership & Scope
  creatorId:       z.string().optional(),
  creatorName:     z.string().optional(),
  orgUnitId:       z.string().optional(),
  orgUnitName:     z.string().optional(),
  scope:           z.enum(['COMMUNITY', 'TENANT', 'TEAM', 'PERSONAL']).optional(),
  visibilityScope: z.enum(['PUBLIC', 'ORG_UNIT', 'TEAM', 'PRIVATE']).optional(),
  contributionStatus: z.enum(['DRAFT', 'TEAM_PUBLISHED', 'SUBMITTED_FOR_TENANT', 'APPROVED_TENANT']).optional(),
});

export type LearnerQuestion = z.infer<typeof LearnerQuestionSchema>;

// ── IRT Item Schema ───────────────────────────────────────────────────────────

export const QuestionIrtSchema = z.object({
  id:         z.string(),
  difficulty: z.number(),   // b parameter (IRT)
  guessRate:  z.number(),   // c parameter (IRT)
});

export type QuestionIrt = z.infer<typeof QuestionIrtSchema>;

// ── Attempt Answer Schema ─────────────────────────────────────────────────────

export const AttemptAnswerSchema = z.object({
  questionId:     z.string(),
  selectedAnswer: z.string(),
  timeSpent:      z.number().int().nonnegative().optional(),
});

export type AttemptAnswer = z.infer<typeof AttemptAnswerSchema>;

// ── Safe parse helper ─────────────────────────────────────────────────────────

export function parseQuestionSafe(raw: unknown): LearnerQuestion | null {
  const result = LearnerQuestionSchema.safeParse(raw);
  return result.success ? result.data : null;
}

export function parseQuestionsSafe(raw: unknown): LearnerQuestion[] {
  if (!Array.isArray(raw)) return [];
  return raw.map(parseQuestionSafe).filter((q): q is LearnerQuestion => q !== null);
}
