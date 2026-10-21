import {
  getDisplayOptions,
  isOptionCorrect,
  scoreQuestion,
} from '@/lib/quiz-helpers';
import { parseMultiTfAnswer } from '@/shared/lib/quiz/normalizers';
import type { LearnerAnswer, LearnerQuestion, QuestionType } from '@/types/quiz';
import type { PracticeQuestionItem } from '../types/mobile-practice.types';

export const LETTERS = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J'];

export interface EvaluatedMobileItem {
  question: PracticeQuestionItem;
  learnerQuestion: LearnerQuestion;
  idx: number;
  userAns: string;
  userDisplayLabel: string;
  correctDisplayLabel: string;
  isAnswered: boolean;
  isCorrect: boolean;
  score: number;
  isManualGrading?: boolean;
}

/**
 * Chuyển đổi PracticeQuestionItem sang LearnerQuestion chuẩn của AegisQuiz Web
 */
export function toLearnerQuestion(q: PracticeQuestionItem, idx: number): LearnerQuestion {
  const options = Array.isArray(q.options)
    ? q.options.map((o) => (typeof o === 'string' ? o : o.text || (o as any).content || String(o)))
    : [];

  return {
    id: q.id || `q-${idx + 1}`,
    questNo: idx + 1,
    content: q.content,
    questionType: (q.questionType?.toUpperCase() as QuestionType) || 'SINGLE',
    options,
    answerRaw: q.correctKey || String(q.payload?.correctAnswer ?? q.payload?.correctOption ?? ''),
    citation: q.explanation || '',
    points: 1,
    difficulty: 1,
    durationSec: 0,
    enabled: true,
    payload: q.payload,
    questionMediaUrl: q.imageUrl,
    isCritical: q.isFatal,
  };
}

/**
 * Chuẩn hóa câu trả lời của người dùng sang LearnerAnswer
 */
export function toLearnerAnswer(learnerQ: LearnerQuestion, userAns: string): LearnerAnswer {
  const displayOptions = getDisplayOptions(learnerQ);
  const qType = learnerQ.questionType;

  if (qType === 'ORDERING') {
    const defaultOrder = displayOptions.map((o) => o.id);
    const ordering = userAns && userAns.trim()
      ? userAns.split(/[,;\s]+/).map((s) => s.trim()).filter(Boolean)
      : defaultOrder;
    return { ordering };
  }

  if (qType === 'ESSAY') {
    return { essay: (userAns || '').trim() };
  }

  if (!userAns || !userAns.trim()) return {};

  if (qType === 'SINGLE' || qType === 'TRUE_FALSE') {
    const clean = userAns.trim().toUpperCase();
    const matched = displayOptions.find(
      (o) => o.label.toUpperCase() === clean || o.id === userAns.trim()
    );
    return {
      selectedOption: matched ? matched.id : userAns.trim(),
    };
  }

  if (qType === 'MULTI') {
    const parts = userAns
      .split(/[,;\s]+/)
      .map((s) => s.trim().toUpperCase())
      .filter(Boolean);

    const mappedIds = parts.map((part) => {
      const matched = displayOptions.find(
        (o) => o.label.toUpperCase() === part || o.id === part
      );
      return matched ? matched.id : part;
    });

    return {
      selectedOptions: mappedIds,
    };
  }

  if (qType === 'MULTI_TRUE_FALSE') {
    const matrix = parseMultiTfAnswer(userAns);
    return {
      matrixAnswer: matrix,
      shortText: userAns,
    };
  }

  if (qType === 'MATCHING') {
    const pairs = userAns.split(',').map((p) => p.trim()).filter(Boolean);
    const mapping: Record<string, string> = {};
    for (const pair of pairs) {
      const sep = pair.includes('::') ? '::' : pair.includes(':') ? ':' : null;
      if (sep) {
        const [l, r] = pair.split(sep);
        if (l && r) mapping[l.trim()] = r.trim();
      }
    }
    return { matching: mapping };
  }

  if (qType === 'SHORT_ANSWER' || qType === 'FILL_BLANK') {
    return { shortText: userAns.trim() };
  }

  return { shortText: userAns };
}

/**
 * Hiển thị nhãn đáp án đúng
 */
export function formatCorrectAnswerDisplay(learnerQ: LearnerQuestion, fallbackKey: string): string {
  const displayOptions = getDisplayOptions(learnerQ);

  if (learnerQ.questionType === 'SINGLE' || learnerQ.questionType === 'TRUE_FALSE') {
    const correctOpt = displayOptions.find((o) => isOptionCorrect(learnerQ, o));
    if (correctOpt) {
      return correctOpt.label;
    }
    if (/^\d+$/.test(fallbackKey.trim())) {
      const num = Number(fallbackKey.trim());
      const letter = LETTERS[num - 1];
      if (letter) return letter;
    }
  }

  if (learnerQ.questionType === 'MULTI') {
    const correctOpts = displayOptions.filter((o) => isOptionCorrect(learnerQ, o));
    if (correctOpts.length > 0) {
      return correctOpts.map((o) => o.label).join(', ');
    }
    const parts = fallbackKey.split(/[,;\s]+/).map((s) => s.trim()).filter(Boolean);
    const converted = parts.map((p) => {
      if (/^\d+$/.test(p)) {
        return LETTERS[Number(p) - 1] || p;
      }
      return p;
    });
    if (converted.length > 0) return converted.join(', ');
  }

  if (learnerQ.questionType === 'MULTI_TRUE_FALSE') {
    const expectedMap = parseMultiTfAnswer(learnerQ.answerRaw);
    const keys = Object.keys(expectedMap).sort();
    if (keys.length > 0) {
      return keys.map((k) => `${k}:${expectedMap[k] ? 'T' : 'F'}`).join(', ');
    }
    return fallbackKey;
  }

  if (learnerQ.questionType === 'ORDERING') {
    return fallbackKey || '1, 2, 3, 4';
  }

  if (learnerQ.questionType === 'ESSAY') {
    return 'Chấm thủ công';
  }

  return fallbackKey || 'Xem giải thích';
}

/**
 * Định dạng nhãn hiển thị của lựa chọn người dùng
 */
export function formatUserAnswerDisplay(learnerQ: LearnerQuestion, userAns: string): string {
  if (learnerQ.questionType === 'ESSAY') {
    return userAns || '(Chưa nhập bài luận)';
  }

  if (learnerQ.questionType === 'ORDERING') {
    if (!userAns || !userAns.trim()) {
      const displayOptions = getDisplayOptions(learnerQ);
      return displayOptions.map((o) => o.id).join(', ') + ' (Mặc định)';
    }
    return userAns;
  }

  if (learnerQ.questionType === 'MULTI_TRUE_FALSE') {
    const userMap = parseMultiTfAnswer(userAns);
    const keys = Object.keys(userMap).sort();
    if (keys.length > 0) {
      return keys.map((k) => `${k}:${userMap[k] ? 'T' : 'F'}`).join(', ');
    }
    return userAns || '(Chưa chọn)';
  }

  if (!userAns || !userAns.trim()) return '(Chưa chọn)';

  if (learnerQ.questionType === 'SINGLE' || learnerQ.questionType === 'TRUE_FALSE') {
    const clean = userAns.trim();
    if (/^\d+$/.test(clean)) {
      const num = Number(clean);
      const letter = LETTERS[num - 1];
      if (letter) return letter;
    }
    return clean.toUpperCase();
  }

  if (learnerQ.questionType === 'MULTI') {
    const parts = userAns.split(/[,;\s]+/).map((s) => s.trim()).filter(Boolean);
    const formatted = parts.map((p) => {
      if (/^\d+$/.test(p)) {
        return LETTERS[Number(p) - 1] || p;
      }
      return p.toUpperCase();
    });
    return formatted.join(', ');
  }

  return userAns;
}

/**
 * Đánh giá chi tiết toàn bộ danh sách câu hỏi trong phiên thi Mobile
 */
export function evaluateMobileSession(
  questions: PracticeQuestionItem[],
  selectedAnswers: Record<number, string>
) {
  let correctCount = 0;
  let wrongCount = 0;
  let unansweredCount = 0;

  const evaluatedQuestions: EvaluatedMobileItem[] = questions.map((q, idx) => {
    const userAns = selectedAnswers[idx]?.trim() || '';
    const learnerQ = toLearnerQuestion(q, idx);
    const isEssay = learnerQ.questionType === 'ESSAY';
    const isOrdering = learnerQ.questionType === 'ORDERING';
    const isAnswered = isOrdering || isEssay ? Boolean(userAns || isOrdering) : Boolean(userAns);
    const learnerAns = toLearnerAnswer(learnerQ, userAns);

    let isCorrect = false;
    let score = 0;

    if (isEssay) {
      // Bài luận chờ chấm thủ công, không tính tự động
      isCorrect = false;
      score = 0;
    } else if (isOrdering) {
      score = scoreQuestion(learnerQ, learnerAns);
      isCorrect = score > 0;
    } else if (learnerQ.questionType === 'MULTI_TRUE_FALSE') {
      const expectedMap = parseMultiTfAnswer(learnerQ.answerRaw);
      const userMap = parseMultiTfAnswer(userAns);
      const keys = Object.keys(expectedMap);
      let matchedCount = 0;
      for (const k of keys) {
        if (userMap[k] !== undefined && userMap[k] === expectedMap[k]) {
          matchedCount++;
        }
      }
      score = scoreQuestion(learnerQ, learnerAns);
      isCorrect = keys.length > 0 && matchedCount === keys.length;
    } else if (isAnswered) {
      score = scoreQuestion(learnerQ, learnerAns);
      isCorrect = score > 0;

      // Fallback đặc biệt cho Matching nếu người dùng đã ghép nối và score tính ra chưa có cấu hình payload
      if (!isCorrect && learnerQ.questionType === 'MATCHING' && userAns.includes('::')) {
        isCorrect = true;
        score = 1;
      }
    }

    if (isEssay) {
      // Không tăng wrongCount cho bài luận chờ chấm
    } else if (!isAnswered) {
      unansweredCount++;
    } else if (isCorrect) {
      correctCount++;
    } else {
      wrongCount++;
    }

    return {
      question: q,
      learnerQuestion: learnerQ,
      idx,
      userAns,
      userDisplayLabel: formatUserAnswerDisplay(learnerQ, userAns),
      correctDisplayLabel: formatCorrectAnswerDisplay(learnerQ, q.correctKey),
      isAnswered,
      isCorrect,
      score,
      isManualGrading: isEssay,
    };
  });

  const mistakeQuestions = evaluatedQuestions
    .filter((item) => !item.isCorrect && !item.isManualGrading)
    .map((item) => item.question);

  const scorableQuestions = questions.filter((q) => q.questionType !== 'ESSAY');
  const accuracy = scorableQuestions.length > 0 
    ? Math.round((correctCount / scorableQuestions.length) * 100) 
    : 0;

  return {
    evaluatedQuestions,
    mistakeQuestions,
    correctCount,
    wrongCount,
    unansweredCount,
    accuracy,
  };
}
