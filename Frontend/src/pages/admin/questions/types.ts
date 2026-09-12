// ── AdminQuestions Shared Types ───────────────────────────────────────────────
// Tách ra từ AdminQuestionsPage.tsx (107KB → modules) — Task A11

export interface BankTopic {
  id: string;
  code: string;
  name: string;
  description?: string;
  categoryCode: string;
  enabled: boolean;
  visibilityScope: string;
  parentId?: string;
  domainCode?: string;
  scope?: string;
  materializedPath?: string;
  depthLevel?: number;
  questionCountCached?: number;
}

// ── Helper functions ──────────────────────────────────────────────────────────

export const LETTERS = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J'];

export const QUESTION_TYPE_LABELS: Record<string, string> = {
  SINGLE:       'Trắc nghiệm 1 đáp án',
  MULTI:        'Nhiều đáp án đúng',
  TRUE_FALSE:   'Đúng / Sai',
  SHORT_ANSWER: 'Trả lời ngắn',
  FILL_BLANK:   'Điền vào chỗ trống',
  ORDERING:     'Sắp xếp thứ tự',
  MATCHING:     'Nối cặp',
  ESSAY:        'Tự luận (AI chấm)',
};

export function getQuestionTypeLabel(type: string): string {
  return QUESTION_TYPE_LABELS[type] ?? type;
}

export function getCorrectAnswerPlaceholder(questionType: string): string {
  switch (questionType) {
    case 'SINGLE':       return 'Ví dụ: A (hoặc 1)';
    case 'MULTI':        return 'Ví dụ: A,C (các phương án đúng cách nhau bởi dấu phẩy)';
    case 'TRUE_FALSE':   return 'Điền 1 (Đúng) hoặc 2 (Sai)';
    case 'SHORT_ANSWER':
    case 'FILL_BLANK':   return 'Điền từ/cụm từ chính xác';
    case 'ORDERING':     return 'Điền thứ tự đúng (Ví dụ: A,B,C,D hoặc 1,2,3,4)';
    case 'MATCHING':     return 'Điền các cặp đúng (Ví dụ: Hà Nội:Việt Nam, Tokyo:Nhật Bản)';
    case 'ESSAY':        return 'Đáp án mẫu hoặc hướng dẫn chấm điểm (Không bắt buộc)';
    default:             return 'Điền đáp án chính xác...';
  }
}

export function getTopicFullPath(topic: BankTopic, allTopics: BankTopic[]): string {
  const path: string[] = [topic.name];
  let parentId = topic.parentId;
  const visited = new Set<string>([topic.id]);

  while (parentId) {
    if (visited.has(parentId)) break;
    visited.add(parentId);

    const parent = allTopics.find(t => t.id === parentId);
    if (!parent) break;

    path.unshift(parent.name);
    parentId = parent.parentId;
  }

  return path.join(' → ');
}

/**
 * Polymorphic correct answer matcher:
 * Robustly matches whether correctAnswer is a numeric index ("1", "2", "3"),
 * a letter ("A", "B", "C", "D"), full option text ("Đáp án 1 và 2"),
 * or letter-prefixed option text ("C. Đáp án 1 và 2").
 */
export function isOptionSelectedAsCorrect(
  opt: string,
  idx: number,
  correctAnswer: string,
  questionType: string,
  _allOptions?: string[]
): boolean {
  if (!correctAnswer || !correctAnswer.trim()) return false;
  const raw = correctAnswer.trim();
  const targetNum = String(idx + 1);
  const targetLetter = (LETTERS[idx] || '').toUpperCase();
  const cleanOpt = (opt || '').trim().toLowerCase();

  const matchToken = (token: string): boolean => {
    if (!token) return false;
    const t = token.trim();
    const tUpper = t.toUpperCase();
    const tLower = t.toLowerCase();

    // 1. Direct index match: "1", "2", "3", "3."
    if (t === targetNum || t.replace(/[^0-9]/g, '') === targetNum) return true;

    // 2. Direct letter match: "A", "B", "C", "D" or "A.", "C)", "A:"
    if (targetLetter) {
      if (tUpper === targetLetter) return true;
      if (tUpper.startsWith(`${targetLetter}.`) || tUpper.startsWith(`${targetLetter})`) || tUpper.startsWith(`${targetLetter}:`)) {
        return true;
      }
    }

    // 3. Option text match (e.g. raw is "Đáp án 1 và 2" or option has text)
    if (cleanOpt) {
      if (tLower === cleanOpt) return true;
      // Strip leading letter prefix like "C. Đáp án 1 và 2" vs "Đáp án 1 và 2"
      const strippedToken = tLower.replace(/^[a-j1-9][.):\s-]+\s*/i, '').trim();
      const strippedOpt = cleanOpt.replace(/^[a-j1-9][.):\s-]+\s*/i, '').trim();
      if (strippedToken && (strippedToken === cleanOpt || strippedToken === strippedOpt)) return true;
      if (strippedOpt && strippedOpt === tLower) return true;

      // Match stripped punctuation
      const cleanTokenOnly = tLower.replace(/[.,:;()]/g, '').trim();
      const cleanOptOnly = cleanOpt.replace(/[.,:;()]/g, '').trim();
      if (cleanTokenOnly && cleanTokenOnly === cleanOptOnly) return true;
    }

    return false;
  };

  if (questionType === 'SINGLE' || questionType === 'TRUE_FALSE') {
    return matchToken(raw);
  }

  if (questionType === 'MULTI') {
    const tokens = raw.split(/[;,\n|]+/).map(x => x.trim()).filter(Boolean);
    if (tokens.length > 0) {
      return tokens.some(tok => matchToken(tok));
    }
    return matchToken(raw);
  }

  return false;
}
