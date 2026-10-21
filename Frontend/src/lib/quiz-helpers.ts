import { parseMultiTfAnswer } from '@/shared/lib/quiz/normalizers';
import type {
  DisplayOption,
  LearnerAnswer,
  LearnerQuestion,
  MatchingPair,
  PracticeConfig,
  PracticeTopicConfig,
  QuizResult,
  TopicSummary,
} from '@/types/quiz';

export const LETTERS = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J'];

export function normalizeMediaType(input?: string): string {
  const raw = (input ?? '').trim().toLowerCase();
  return raw || 'text';
}

export function getDisplayOptions(question: LearnerQuestion): DisplayOption[] {
  const mediaType = normalizeMediaType(question.optionType || question.optionMediaType);
  const values = question.options ?? [];

  return values
    .filter((x) => (x ?? '').trim().length > 0)
    .map((value, index) => ({
      id: String(index + 1),
      label: LETTERS[index] ?? String(index + 1),
      value,
      isMedia: mediaType !== 'text',
      mediaType,
    }));
}

export function parseIndexList(raw: string): string[] {
  return raw
    .split(/[;,\s]+/)
    .map((x) => x.trim())
    .filter((x) => x.length > 0);
}

export function parseMatchingPairs(options: DisplayOption[], question?: LearnerQuestion): MatchingPair[] {
  // 1. Kiểm tra payload của question (đặc biệt là GuestShow / Seed DB)
  const payload = question?.payload;
  if (payload) {
    if (Array.isArray(payload.correctPairs) && payload.correctPairs.length > 0) {
      const fromPairs = payload.correctPairs.map((p: any) => ({
        left: String(p.left ?? '').trim(),
        right: String(p.right ?? '').trim(),
      })).filter((p: MatchingPair) => p.left && p.right);
      if (fromPairs.length > 0) return fromPairs;
    }
    if (Array.isArray(payload.leftItems) && Array.isArray(payload.rightItems) && payload.leftItems.length > 0) {
      const fromItems = payload.leftItems.map((left: any, idx: number) => ({
        left: String(left ?? '').trim(),
        right: String(payload.rightItems[idx] ?? '').trim(),
      })).filter((p: MatchingPair) => p.left && p.right);
      if (fromItems.length > 0) return fromItems;
    }
  }

  // 2. Parse từ display options
  const pairs: MatchingPair[] = [];
  for (const option of options) {
    const value = option.value;
    if (value.includes('=>')) {
      const [l, r] = value.split('=>');
      pairs.push({ left: l.trim(), right: r.trim() });
      continue;
    }
    if (value.includes('|')) {
      const [l, r] = value.split('|');
      pairs.push({ left: l.trim(), right: r.trim() });
      continue;
    }
    // Dấu gạch ngang / em-dash / en-dash: ' - ', ' – ', ' — '
    const dashMatch = value.split(/\s+[-–—]\s+/);
    if (dashMatch.length === 2) {
      pairs.push({ left: dashMatch[0].trim(), right: dashMatch[1].trim() });
      continue;
    }
    // Dấu hai chấm: 'left : right'
    const colonMatch = value.split(/\s*:\s*/);
    if (colonMatch.length === 2 && colonMatch[0].length > 1 && colonMatch[1].length > 1) {
      pairs.push({ left: colonMatch[0].trim(), right: colonMatch[1].trim() });
      continue;
    }
  }

  if (pairs.length > 0) return pairs;

  // 3. Hai cột riêng biệt
  const splitList = (input: string): string[] => {
    return input
      .split(/[|;\n\r]+/)
      .map((x) => x.trim())
      .filter((x) => x.length > 0);
  };

  if (options.length >= 2) {
    const leftList = splitList(options[0].value);
    const rightList = splitList(options[1].value);

    if (leftList.length >= 2 && leftList.length === rightList.length) {
      for (let i = 0; i < leftList.length; i += 1) {
        pairs.push({ left: leftList[i], right: rightList[i] });
      }
    }
  }

  return pairs;
}

/**
 * Kiểm tra xem một OptionChoice có phải là đáp án đúng của câu hỏi hay không.
 * Khớp chuẩn hóa 100% qua:
 * - option.id (chuỗi số 1-based "1", "2")
 * - option.label (chữ cái "A", "B")
 * - option.value (nội dung text)
 * - question.answerRaw hoặc question.payload.correctAnswer
 */
export function isOptionCorrect(question: LearnerQuestion, option: DisplayOption): boolean {
  const rawAnswer = question.answerRaw || String(question.payload?.correctAnswer ?? question.payload?.correctOption ?? '');
  if (!rawAnswer.trim()) return false;

  const rawList = parseIndexList(rawAnswer);
  if (rawList.length === 0) return false;

  const optId = option.id.trim();
  const optLabel = option.label.trim().toUpperCase();
  const optVal = option.value.trim().toLowerCase();

  for (const item of rawList) {
    const cleanItem = item.trim();
    if (!cleanItem) continue;

    // Khớp ID trực tiếp: "1" === "1"
    if (cleanItem === optId) return true;

    // Khớp Nhãn chữ cái trực tiếp: "A" === "A"
    if (cleanItem.toUpperCase() === optLabel) return true;

    // Chuyển đổi chữ cái sang số: "A" -> 1 === optId
    if (/^[a-j]$/i.test(cleanItem)) {
      const numFromLetter = String(cleanItem.toUpperCase().charCodeAt(0) - 64);
      if (numFromLetter === optId) return true;
    }

    // Chuyển đổi số sang chữ cái: "1" -> "A" === optLabel
    if (/^\d+$/.test(cleanItem)) {
      const letterFromNum = LETTERS[Number(cleanItem) - 1];
      if (letterFromNum && letterFromNum === optLabel) return true;
    }

    // Khớp nội dung text nếu câu hỏi lưu đáp án nguyên văn
    if (cleanItem.toLowerCase() === optVal) return true;
  }

  return false;
}

function shuffleArray<T>(input: T[]): T[] {
  const arr = [...input];
  for (let i = arr.length - 1; i > 0; i -= 1) {
    const j = Math.floor(Math.random() * (i + 1));
    const tmp = arr[i];
    arr[i] = arr[j];
    arr[j] = tmp;
  }
  return arr;
}

export function getTopicKey(question: LearnerQuestion): string {
  const code = (question.topicCode ?? '').trim();
  if (code) return code.toUpperCase();
  const name = (question.topicName ?? '').trim();
  if (name) return name.toUpperCase().replace(/\s+/g, '_');
  return 'GENERAL';
}

export function buildTopicSummaries(questions: LearnerQuestion[]): TopicSummary[] {
  const topicMap = new Map<string, TopicSummary>();

  for (const question of questions.filter((q) => q.enabled)) {
    const key = getTopicKey(question);
    const code = (question.topicCode ?? '').trim() || key;
    const name = (question.topicName ?? '').trim() || code;

    const existing = topicMap.get(key);
    if (!existing) {
      topicMap.set(key, {
        key,
        code,
        name,
        questionCount: 1,
      });
      continue;
    }

    existing.questionCount += 1;
  }

  return [...topicMap.values()].sort((a, b) => a.code.localeCompare(b.code));
}

function applyTopicConfig(questions: LearnerQuestion[], config: PracticeTopicConfig): LearnerQuestion[] {
  const sorted = [...questions].sort((a, b) => (a.questNo || 0) - (b.questNo || 0));
  const quantity = Math.max(1, config.numberOfQuestions || 1);

  if (config.isRandom) {
    return shuffleArray(sorted).slice(0, quantity);
  }

  const start = Math.max(0, (config.startIndex || 1) - 1);
  return sorted.slice(start, start + quantity);
}

function remapAnswerByOptionOrder(
  questionType: LearnerQuestion['questionType'],
  answerRaw: string,
  positionMap: Record<string, string>,
): string {
  if (!answerRaw.trim()) return answerRaw;

  if (questionType === 'SINGLE' || questionType === 'TRUE_FALSE' || questionType === 'MULTI' || questionType === 'ORDERING') {
    const mapped = parseIndexList(answerRaw)
      .map((idx) => {
        if (positionMap[idx]) return positionMap[idx];
        if (/^[a-j]$/i.test(idx)) {
          const numIdx = String(idx.toUpperCase().charCodeAt(0) - 64);
          if (positionMap[numIdx]) return positionMap[numIdx];
        }
        return idx;
      })
      .filter((idx): idx is string => Boolean(idx));
    return mapped.length > 0 ? mapped.join(',') : answerRaw;
  }

  return answerRaw;
}

function remapOptionTextByPosition(value: string, positionMap: Record<string, string>): string {
  const matches: { text: string; index: string; start: number; end: number }[] = [];
  const regex = /\((\d+)\)/g;
  let match;
  while ((match = regex.exec(value)) !== null) {
    matches.push({
      text: match[0],
      index: match[1],
      start: match.index,
      end: regex.lastIndex,
    });
  }

  if (matches.length === 0) return value;

  if (matches.length === 1) {
    const rawIndex = matches[0].index;
    const mappedIndex = positionMap[rawIndex];
    if (!mappedIndex) return value;
    const label = LETTERS[Number(mappedIndex) - 1] ?? mappedIndex;
    return value.substring(0, matches[0].start) + label + value.substring(matches[0].end);
  }

  const labels = matches
    .map((m) => {
      const mappedIndex = positionMap[m.index];
      return mappedIndex ? (LETTERS[Number(mappedIndex) - 1] ?? mappedIndex) : '';
    })
    .filter(Boolean);

  if (labels.length === 0) return value;

  labels.sort();

  const middleText = value.substring(matches[0].start, matches[matches.length - 1].end);
  let conjunction = ' và ';
  if (middleText.includes('hoặc') || middleText.includes('hoac')) conjunction = ' hoặc ';
  else if (middleText.includes('or')) conjunction = ' or ';
  else if (middleText.includes('and')) conjunction = ' and ';

  let replacement = '';
  if (labels.length === 2) {
    replacement = labels.join(conjunction);
  } else {
    replacement = labels.slice(0, -1).join(', ') + conjunction + labels[labels.length - 1];
  }

  return value.substring(0, matches[0].start) + replacement + value.substring(matches[matches.length - 1].end);
}

function shuffleQuestionOptions(question: LearnerQuestion, shouldShuffle: boolean = true): LearnerQuestion {
  const supportsOptions = ['SINGLE', 'MULTI', 'TRUE_FALSE', 'ORDERING'];
  if (!supportsOptions.includes(question.questionType)) return question;

  const rawValues = question.options ?? [];

  const options = rawValues
    .map((value, index) => ({ value, oldIndex: String(index + 1) }))
    .filter((item) => (item.value ?? '').trim().length > 0);

  if (options.length < 2) return question;

  const shuffled = shouldShuffle ? shuffleArray(options) : options;
  const positionMap: Record<string, string> = {};
  shuffled.forEach((item, newIndex) => {
    const newPos = String(newIndex + 1);
    positionMap[item.oldIndex] = newPos;
    const oldNum = Number(item.oldIndex);
    if (oldNum >= 1 && oldNum <= 26) {
      const letter = LETTERS[oldNum - 1];
      if (letter) {
        positionMap[letter] = newPos;
        positionMap[letter.toLowerCase()] = newPos;
      }
    }
  });

  const values = shuffled.map((item) => remapOptionTextByPosition(item.value, positionMap));

  return {
    ...question,
    options: values,
    answerRaw: remapAnswerByOptionOrder(question.questionType, question.answerRaw, positionMap),
  };
}

function prepareMatchingDisplay(question: LearnerQuestion, shouldShuffle: boolean): LearnerQuestion {
  if (question.questionType !== 'MATCHING') return question;

  const pairs = parseMatchingPairs(getDisplayOptions(question), question);
  if (pairs.length === 0) return question;

  const leftPool = pairs.map((pair) => pair.left);
  const rightPool = pairs.map((pair) => pair.right);

  return {
    ...question,
    matchingLeftOptions: shouldShuffle ? shuffleArray(leftPool) : leftPool,
    matchingRightOptions: shouldShuffle ? shuffleArray(rightPool) : rightPool,
  };
}

export function buildPracticeSessionQuestions(allQuestions: LearnerQuestion[], config: PracticeConfig): LearnerQuestion[] {
  const enabledQuestions = allQuestions.filter((q) => q.enabled);
  const byTopic = new Map<string, LearnerQuestion[]>();

  for (const question of enabledQuestions) {
    const key = getTopicKey(question);
    const bucket = byTopic.get(key) ?? [];
    bucket.push(question);
    byTopic.set(key, bucket);
  }

  const selected = config.topicConfigs.flatMap((topicConfig) => {
    const questions = byTopic.get(topicConfig.topicKey) ?? [];
    return applyTopicConfig(questions, topicConfig);
  });

  const unique = new Map<string, LearnerQuestion>();
  for (const item of selected) {
    unique.set(item.id, item);
  }

  const baseList = [...unique.values()];
  const ordered = config.mode === 'kids' ? shuffleArray(baseList) : baseList.sort((a, b) => (a.questNo || 0) - (b.questNo || 0));
  const shouldShuffleOptions = config.mode === 'kids' ? true : config.shuffleOptions;

  return ordered.map((question) => {
    const prepared = prepareMatchingDisplay(question, shouldShuffleOptions);
    if (prepared.questionType === 'MATCHING') return prepared;
    return shuffleQuestionOptions(prepared, shouldShuffleOptions);
  });
}

export function processBackendQuestions(
  fetchedQuestions: LearnerQuestion[],
  config: PracticeConfig,
): LearnerQuestion[] {
  const baseList = config.shuffleQuestions || config.mode === 'kids'
    ? shuffleArray([...fetchedQuestions])
    : [...fetchedQuestions];

  const shouldShuffleOptions = config.mode === 'kids' ? true : config.shuffleOptions;

  return baseList.map((question) => {
    const prepared = prepareMatchingDisplay(question, shouldShuffleOptions);
    if (prepared.questionType === 'MATCHING') return prepared;
    return shuffleQuestionOptions(prepared, shouldShuffleOptions);
  });
}

function normalizeText(input?: string): string {
  return (input ?? '').trim().toLowerCase();
}

function compareSets(a: string[], b: string[]): boolean {
  const left = [...a].sort();
  const right = [...b].sort();
  if (left.length !== right.length) return false;
  return left.every((item, idx) => item === right[idx]);
}

function compareOrdered(a: string[], b: string[]): boolean {
  if (a.length !== b.length) return false;
  return a.every((item, idx) => item === b[idx]);
}

function resolveOneBasedOrZeroBasedIndex(raw: string, size: number): number | null {
  const token = raw.trim();
  if (!/^\d+$/.test(token)) return null;
  const n = Number(token);
  // Ưu tiên 1-based index chuẩn khảo thí: 1..size -> 0..size-1
  if (n >= 1 && n <= size) return n - 1;
  // Fallback 0-based index nếu dùng từ 0
  if (n >= 0 && n < size) return n;
  return null;
}

function canonicalMatchingPair(left: string, right: string): string {
  return `${left}:${right}`.replace(/\s+/g, '').toLowerCase();
}

function normalizeMatchingExpected(question: LearnerQuestion): string[] {
  // Ưu tiên correctPairs từ payload
  if (Array.isArray(question.payload?.correctPairs) && question.payload.correctPairs.length > 0) {
    return question.payload.correctPairs
      .map((p: any) => canonicalMatchingPair(String(p.left || ''), String(p.right || '')))
      .filter(Boolean);
  }

  const pairs = parseMatchingPairs(getDisplayOptions(question), question);
  const leftPool = pairs.map((x) => x.left);
  const rightPool = pairs.map((x) => x.right);

  return question.answerRaw
    .split(/[;,]+/)
    .map((entry) => entry.trim())
    .filter((entry) => entry.length > 0)
    .map((entry) => {
      const [rawLeft, rawRight] = entry.split(':').map((x) => x.trim());
      if (!rawLeft || !rawRight) return '';

      const leftIdx = resolveOneBasedOrZeroBasedIndex(rawLeft, leftPool.length);
      const rightIdx = resolveOneBasedOrZeroBasedIndex(rawRight, rightPool.length);
      if (leftIdx != null && rightIdx != null) {
        const left = leftPool[leftIdx];
        const right = rightPool[rightIdx];
        return left && right ? canonicalMatchingPair(left, right) : '';
      }

      return canonicalMatchingPair(rawLeft, rawRight);
    })
    .filter((entry) => entry.length > 0);
}

export function isQuestionAnswered(question: LearnerQuestion, answer?: LearnerAnswer): boolean {
  // ORDERING: trật tự đang hiển thị luôn là một câu trả lời hợp lệ, kể cả khi thí sinh không chạm vào.
  if (!answer) return question.questionType === 'ORDERING' && getDisplayOptions(question).length > 0;

  switch (question.questionType) {
    case 'SINGLE':
    case 'TRUE_FALSE':
      return Boolean(answer.selectedOption);
    case 'MULTI':
      return Boolean(answer.selectedOptions && answer.selectedOptions.length > 0);
    case 'MULTI_TRUE_FALSE': {
      if (answer.matrixAnswer && Object.keys(answer.matrixAnswer).length > 0) return true;
      if (answer.shortText && answer.shortText.trim().length > 0) return true;
      return false;
    }
    case 'SHORT_ANSWER':
    case 'FILL_BLANK':
      return normalizeText(answer.shortText).length > 0;
    case 'ORDERING':
      // Nếu người thi đã kéo thả HOẶC câu hỏi đã hiển thị trật tự mặc định sẵn
      return Boolean((answer.ordering && answer.ordering.length > 0) || getDisplayOptions(question).length > 0);
    case 'MATCHING':
      return Boolean(answer.matching && Object.keys(answer.matching).length > 0);
    case 'ESSAY':
      return normalizeText(answer.essay).length > 0;
    default:
      return false;
  }
}

/** Chuẩn hóa token chỉ số phương án: "B" -> "2", "2" -> "2". */
function normalizeIndexToken(token: string): string {
  const t = token.trim();
  return /^[a-j]$/i.test(t) ? String(t.toUpperCase().charCodeAt(0) - 64) : t;
}

export function scoreQuestion(question: LearnerQuestion, rawAnswer?: LearnerAnswer): number {
  // ORDERING được chấm cả khi chưa có answer (thứ tự mặc định); các dạng khác cần answer.
  if (!rawAnswer && question.questionType !== 'ORDERING') return 0;
  const answer: LearnerAnswer = rawAnswer ?? {};

  const displayOptions = getDisplayOptions(question);

  switch (question.questionType) {
    case 'SINGLE':
    case 'TRUE_FALSE': {
      if (!answer.selectedOption) return 0;
      const opt = displayOptions.find((o) => o.id === answer.selectedOption);
      return opt && isOptionCorrect(question, opt) ? question.points : 0;
    }
    case 'MULTI': {
      const selected = answer.selectedOptions ?? [];
      if (selected.length === 0) return 0;
      const correctIds = displayOptions.filter((o) => isOptionCorrect(question, o)).map((o) => o.id);
      return correctIds.length > 0 && compareSets(selected, correctIds) ? question.points : 0;
    }
    case 'MULTI_TRUE_FALSE': {
      const expectedMap = parseMultiTfAnswer(question.answerRaw);
      const userMap = (answer.matrixAnswer && Object.keys(answer.matrixAnswer).length > 0)
        ? answer.matrixAnswer
        : parseMultiTfAnswer(answer.shortText);

      const keys = Object.keys(expectedMap);
      if (keys.length === 0) return 0;

      let matched = 0;
      for (const k of keys) {
        if (userMap[k] !== undefined && userMap[k] === expectedMap[k]) {
          matched++;
        }
      }

      const total = keys.length;
      const pts = question.points || 1;
      // Quy chuẩn Bộ GD&ĐT 2025 cho Đúng/Sai 4 ý:
      // 1 ý đúng: 0.1đ | 2 ý đúng: 0.25đ | 3 ý đúng: 0.5đ | 4 ý đúng: 1.0đ
      if (total === 4) {
        if (matched === 4) return pts;
        if (matched === 3) return Number((pts * 0.5).toFixed(2));
        if (matched === 2) return Number((pts * 0.25).toFixed(2));
        if (matched === 1) return Number((pts * 0.1).toFixed(2));
        return 0;
      }

      return Number(((matched / total) * pts).toFixed(2));
    }
    case 'SHORT_ANSWER':
    case 'FILL_BLANK': {
      const expected = normalizeText(question.answerRaw);
      return expected.length > 0 && expected === normalizeText(answer.shortText) ? question.points : 0;
    }
    case 'ORDERING': {
      // Nếu người thi không tác động vào giao diện nhưng thứ tự mặc định đã đúng, vẫn tính điểm
      const defaultOrder = displayOptions.map((o) => o.id);
      const selected = (answer.ordering && answer.ordering.length > 0)
        ? answer.ordering
        : defaultOrder;
      const expected = parseIndexList(question.answerRaw).map(normalizeIndexToken);
      return selected.length > 0 && compareOrdered(selected, expected) ? question.points : 0;
    }
    case 'MATCHING': {
      const mapping = answer.matching ?? {};
      const expected = normalizeMatchingExpected(question);
      const actual = Object.entries(mapping)
        .map(([left, right]) => canonicalMatchingPair(left, right));
      return expected.length > 0 && compareSets(actual, expected) ? question.points : 0;
    }
    case 'ESSAY':
      return 0;
    default:
      return 0;
  }
}

export type ScoringStatus = 'scored' | 'manual';

export interface QuestionEvaluation {
  score: number;
  maxScore: number;
  status: ScoringStatus;
  answered: boolean;
}

/**
 * API chấm điểm hợp nhất — cùng hợp đồng với server `IQuestionEvaluatorStrategy` (ScoringResult).
 * Được khóa bởi golden vectors tại tests/scoring-vectors.
 */
export function evaluateQuestion(question: LearnerQuestion, answer?: LearnerAnswer): QuestionEvaluation {
  const isManual = question.questionType === 'ESSAY';
  return {
    score: isManual ? 0 : scoreQuestion(question, answer),
    maxScore: question.points || 0,
    status: isManual ? 'manual' : 'scored',
    answered: isQuestionAnswered(question, answer),
  };
}

export function calculateQuizResult(questions: LearnerQuestion[], answers: Record<string, LearnerAnswer>): QuizResult {
  const activeQuestions = questions.filter((q) => q.enabled);
  const answeredQuestions = activeQuestions.filter((q) => isQuestionAnswered(q, answers[q.id])).length;
  const scoredQuestions = activeQuestions.filter((q) => q.questionType !== 'ESSAY').length;
  const totalPoints = activeQuestions
    .filter((q) => q.questionType !== 'ESSAY')
    .reduce((sum, q) => sum + (q.points || 0), 0);

  const earnedPoints = activeQuestions
    .filter((q) => q.questionType !== 'ESSAY')
    .reduce((sum, q) => sum + scoreQuestion(q, answers[q.id]), 0);

  const percentage = totalPoints > 0 ? (earnedPoints / totalPoints) * 100 : 0;

  const topicMap = new Map<
    string,
    {
      topicLabel: string;
      totalQuestions: number;
      answeredQuestions: number;
      scoredQuestions: number;
      earnedPoints: number;
      totalPoints: number;
    }
  >();

  for (const question of activeQuestions) {
    const topicKey = getTopicKey(question);
    const topicLabel = (question.topicName ?? '').trim() || (question.topicCode ?? '').trim() || topicKey;
    const current = topicMap.get(topicKey) ?? {
      topicLabel,
      totalQuestions: 0,
      answeredQuestions: 0,
      scoredQuestions: 0,
      earnedPoints: 0,
      totalPoints: 0,
    };

    current.totalQuestions += 1;
    if (isQuestionAnswered(question, answers[question.id])) {
      current.answeredQuestions += 1;
    }

    if (question.questionType !== 'ESSAY') {
      current.scoredQuestions += 1;
      current.totalPoints += question.points || 0;
      current.earnedPoints += scoreQuestion(question, answers[question.id]);
    }

    topicMap.set(topicKey, current);
  }

  const topicResults = [...topicMap.entries()]
    .map(([topicKey, stat]) => ({
      topicKey,
      topicLabel: stat.topicLabel,
      totalQuestions: stat.totalQuestions,
      answeredQuestions: stat.answeredQuestions,
      scoredQuestions: stat.scoredQuestions,
      earnedPoints: stat.earnedPoints,
      totalPoints: stat.totalPoints,
      percentage: stat.totalPoints > 0 ? (stat.earnedPoints / stat.totalPoints) * 100 : 0,
    }))
    .sort((a, b) => a.topicLabel.localeCompare(b.topicLabel));

  const fatalQuestionsFailed: LearnerQuestion[] = [];
  for (const question of activeQuestions) {
    if (question.isCritical && question.questionType !== 'ESSAY') {
      const earned = scoreQuestion(question, answers[question.id]);
      const maxPts = question.points || 1;
      if (earned < maxPts) {
        fatalQuestionsFailed.push(question);
      }
    }
  }
  const hasFatalMistake = fatalQuestionsFailed.length > 0;

  return {
    totalQuestions: activeQuestions.length,
    answeredQuestions,
    scoredQuestions,
    totalPoints,
    earnedPoints,
    percentage,
    topicResults,
    hasFatalMistake,
    fatalQuestionsFailed,
  };
}

export function getStudyHint(question: LearnerQuestion): string {
  if ((question.citation ?? '').trim()) {
    return question.citation.trim();
  }

  switch (question.questionType) {
    case 'SINGLE':
    case 'TRUE_FALSE': {
      const first = parseIndexList(question.answerRaw)[0];
      const label = first ? LETTERS[Number(first) - 1] ?? first : '';
      return label ? `Đáp án đúng nằm ở phương án ${label}.` : 'Hãy đọc kỹ từng phương án và loại trừ đáp án nhiễu.';
    }
    case 'MULTI': {
      const count = parseIndexList(question.answerRaw).length;
      return count > 0 ? `Câu này có ${count} phương án đúng.` : 'Câu này có nhiều hơn một phương án đúng.';
    }
    case 'ORDERING':
      return 'Hãy xác định trình tự logic hoặc trình tự thời gian trước khi sắp xếp.';
    case 'MATCHING':
      return 'Hãy ghép theo cặp khái niệm tương ứng và loại trừ các cặp không cùng nhóm.';
    case 'SHORT_ANSWER':
    case 'FILL_BLANK': {
      const raw = (question.answerRaw ?? '').trim();
      return raw ? `Đáp án bắt đầu bằng: ${raw.slice(0, Math.min(3, raw.length))}${raw.length > 3 ? '...' : ''}` : 'Hãy dùng từ khóa ngắn gọn và chính xác.';
    }
    case 'ESSAY':
      return 'Hãy trả lời theo cấu trúc: ý chính, lập luận, ví dụ minh họa.';
    default:
      return 'Hãy dựa vào từ khóa trọng tâm trong câu hỏi để suy luận đáp án.';
  }
}
