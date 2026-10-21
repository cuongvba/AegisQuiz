import { readFileSync } from 'fs';
import { resolve } from 'path';
import { describe, expect, it } from 'vitest';
import { evaluateQuestion } from '@/lib/quiz-helpers';
import type { LearnerAnswer, LearnerQuestion, QuestionType } from '@/types/quiz';

/**
 * Contract test: Frontend phải khớp 100% golden vectors — cùng file với xUnit backend.
 * Nếu test này đỏ, KHÔNG sửa vector cho "xanh"; hãy sửa logic chấm điểm.
 */
interface VectorCase {
  id: string;
  question: {
    questionType: QuestionType;
    options: string[];
    answerRaw: string;
    points: number;
    payload?: Record<string, unknown>;
  };
  answer: LearnerAnswer | null;
  expected: { score: number; status: 'scored' | 'manual'; answered: boolean };
}

const vectorPath = resolve(__dirname, '../../../tests/scoring-vectors/scoring-vectors.v1.json');
const { cases } = JSON.parse(readFileSync(vectorPath, 'utf-8')) as { cases: VectorCase[] };

function toLearnerQuestion(c: VectorCase): LearnerQuestion {
  return {
    id: c.id,
    questNo: 1,
    content: c.id,
    citation: '',
    difficulty: 0,
    durationSec: 0,
    enabled: true,
    ...c.question,
  };
}

describe('golden scoring vectors v1', () => {
  it('loads a non-trivial vector set', () => {
    expect(cases.length).toBeGreaterThanOrEqual(30);
  });

  it.each(cases.map((c) => [c.id, c] as const))('%s', (_id, c) => {
    const result = evaluateQuestion(toLearnerQuestion(c), c.answer ?? undefined);
    expect(result.score).toBeCloseTo(c.expected.score, 2);
    expect(result.status).toBe(c.expected.status);
    expect(result.answered).toBe(c.expected.answered);
  });
});
