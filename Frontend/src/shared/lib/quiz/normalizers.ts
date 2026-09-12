/**
 * AegisQuiz — Quiz Normalizers Library
 * ======================================
 * Tách từ learner-quiz.service.ts (lines 1–284).
 * Đây là pure functions — không có HTTP calls, không có side effects.
 *
 * FSD Layer: shared/lib/quiz/
 *
 * Functions được export:
 *   normalizeQuestion()      — chuẩn hóa raw API object → LearnerQuestion
 *   normalizeQuestionType()  — map alias type strings → QuestionType enum
 *   looksLikeMultiFromAnswer() — detect MULTI từ answerRaw
 *   looksLikeMatchingFromTwoColumns() — detect MATCHING format
 */

import type { LearnerQuestion, QuestionType } from '@/types/quiz';

// ── Private Helpers ──────────────────────────────────────────────────────────

export function parsePayload(raw: unknown): Record<string, unknown> {
  if (!raw) return {};
  if (typeof raw === 'object') return raw as Record<string, unknown>;
  if (typeof raw === 'string') {
    try {
      const parsed = JSON.parse(raw);
      return typeof parsed === 'object' && parsed ? (parsed as Record<string, unknown>) : {};
    } catch {
      return {};
    }
  }
  return {};
}

export function pickFirst(
  source: Record<string, unknown>,
  payload: Record<string, unknown>,
  keys: string[],
): unknown {
  for (const key of keys) {
    const direct = source[key];
    if (direct !== undefined && direct !== null && String(direct).trim() !== '') return direct;
    const nested = payload[key];
    if (nested !== undefined && nested !== null && String(nested).trim() !== '') return nested;
  }
  return undefined;
}

// ── Type Normalization ────────────────────────────────────────────────────────

const TYPE_ALIAS_MAP: Record<string, QuestionType> = {
  SINGLE:          'SINGLE',
  SINGLE_CHOICE:   'SINGLE',
  ONE_CHOICE:      'SINGLE',
  MULTI:           'MULTI',
  MULTIPLE:        'MULTI',
  MULTIPLE_CHOICE: 'MULTI',
  MULTI_SELECT:    'MULTI',
  TRUE_FALSE:      'TRUE_FALSE',
  TRUEFALSE:       'TRUE_FALSE',
  SHORT_ANSWER:    'SHORT_ANSWER',
  SHORTANSWER:     'SHORT_ANSWER',
  FILL_BLANK:      'FILL_BLANK',
  FILL_IN_BLANK:   'FILL_BLANK',
  ORDERING:        'ORDERING',
  MATCHING:        'MATCHING',
  ESSAY:           'ESSAY',
};

const ALLOWED_TYPES: QuestionType[] = [
  'SINGLE', 'MULTI', 'TRUE_FALSE', 'SHORT_ANSWER',
  'FILL_BLANK', 'ORDERING', 'MATCHING', 'ESSAY',
];

export function normalizeTypeAlias(input: string): string {
  const token = input.toUpperCase().replace(/[\s-]+/g, '_');
  return TYPE_ALIAS_MAP[token] ?? token;
}

export function normalizeQuestionType(input: unknown): QuestionType {
  const raw = normalizeTypeAlias(String(input ?? 'SINGLE'));
  return ALLOWED_TYPES.includes(raw as QuestionType) ? (raw as QuestionType) : 'SINGLE';
}

// ── Detection Heuristics ─────────────────────────────────────────────────────

/**
 * Detect nếu answerRaw có dấu phẩy phân tách nhiều phương án ngắn
 * → gợi ý đây là câu MULTI thay vì SINGLE.
 */
export function looksLikeMultiFromAnswer(questionType: QuestionType, answerRaw: string): boolean {
  if (questionType !== 'SINGLE') return false;
  const trimmed = answerRaw.trim();
  if (!trimmed) return false;
  const parts = trimmed.split(/[,;]/).map((s) => s.trim()).filter((s) => s.length > 0);
  if (parts.length < 2) return false;
  // Mỗi phần phải là mã phương án ngắn (A, B, 1, 2, PA 1...) — không phải câu văn dài có dấu phẩy
  return parts.every(
    (p) => /^(?:(?:phương\s*án|đáp\s*án|pa|d\/a|option)?\s*[a-j0-9]+[a-j0-9.]*)$/i.test(p) || p.length <= 4,
  );
}

function splitMatchingList(input: string): string[] {
  return input.split(/[|;\n\r]+/).map((x) => x.trim()).filter((x) => x.length > 0);
}

/**
 * Detect nếu options[0] và options[1] thực ra là 2 cột matching
 * dùng pipe/semicolon làm delimiter.
 */
export function looksLikeMatchingFromTwoColumns(questionType: QuestionType, options: string[]): boolean {
  if (questionType !== 'SINGLE') return false;
  const first  = options[0] ?? '';
  const second = options[1] ?? '';
  const hasTail     = options.slice(2).some((x) => (x ?? '').trim().length > 0);
  const hasSplitter = /[|;\n\r]/.test(first) && /[|;\n\r]/.test(second);
  if (hasTail || !hasSplitter) return false;
  const leftList  = splitMatchingList(first);
  const rightList = splitMatchingList(second);
  return leftList.length >= 2 && leftList.length === rightList.length;
}

function normalizeMatchingFromTwoColumns(
  options: string[],
  answerRaw: string,
): { options: string[]; answerRaw: string } {
  const leftList  = splitMatchingList(options[0] ?? '');
  const rightList = splitMatchingList(options[1] ?? '');
  const pairs     = leftList
    .map((left, idx) => ({ left, right: rightList[idx] ?? '' }))
    .filter((x) => x.left && x.right);

  const resolveIndex = (raw: string, size: number): number | null => {
    const token = raw.trim();
    if (!/^\d+$/.test(token)) return null;
    const n = Number(token);
    if (n >= 0 && n < size) return n;
    if (n >= 1 && n <= size) return n - 1;
    return null;
  };

  const pairOptions = pairs.slice(0, 5).map((x) => `${x.left} => ${x.right}`);
  while (pairOptions.length < 5) pairOptions.push('');

  let normalizedAnswer = pairs.map((x) => `${x.left}:${x.right}`).join(',');
  if (answerRaw.includes(':')) {
    const resolved = answerRaw
      .split(/[;,]+/)
      .map((entry) => entry.trim())
      .filter((entry) => entry.length > 0)
      .map((entry) => {
        const [rawLeft, rawRight] = entry.split(':').map((x) => x.trim());
        if (!rawLeft || !rawRight) return '';
        const leftIdx  = resolveIndex(rawLeft,  leftList.length);
        const rightIdx = resolveIndex(rawRight, rightList.length);
        if (leftIdx != null && rightIdx != null) {
          const left  = leftList[leftIdx];
          const right = rightList[rightIdx];
          return left && right ? `${left}:${right}` : '';
        }
        return `${rawLeft}:${rawRight}`;
      })
      .filter((entry) => entry.length > 0);
    if (resolved.length > 0) normalizedAnswer = resolved.join(',');
  }

  return { options: pairOptions, answerRaw: normalizedAnswer };
}

function readOptions(source: Record<string, unknown>, payload: Record<string, unknown>): string[] {
  const sourceOptions  = Array.isArray(source.options)  ? source.options  : [];
  const payloadOptions = Array.isArray(payload.options) ? payload.options : [];
  const rawArray       = sourceOptions.length > 0 ? sourceOptions : payloadOptions;

  if (rawArray.length > 0) {
    return rawArray.map((item) => {
      if (item && typeof item === 'object') {
        const row = item as Record<string, unknown>;
        return String(row.text ?? row.value ?? '');
      }
      return String(item ?? '');
    });
  }

  const direct = [source.option1, source.option2, source.option3, source.option4, source.option5];
  const nested = [payload.option1, payload.option2, payload.option3, payload.option4, payload.option5];
  const hasDirect = direct.some((x) => x != null && String(x).trim() !== '');
  const chosen    = hasDirect ? direct : nested;
  return chosen.map((x) => String(x ?? '')).filter((x) => x.trim() !== '');
}

// ── Main Normalizer ───────────────────────────────────────────────────────────

/**
 * Chuẩn hóa raw API response object → typed LearnerQuestion.
 *
 * Xử lý:
 *  - Polymorphic field names (camelCase / snake_case / payload-nested)
 *  - Type alias normalization (SINGLE_CHOICE → SINGLE)
 *  - Auto-detect MATCHING từ 2-column format
 *  - Auto-detect MULTI từ comma-separated answerRaw
 *  - Auto-detect ESSAY khi không có options
 *  - Shared context pattern [Bài đọc hiểu]:...\n---\n{câu hỏi}
 *  - Ánh xạ ngược answerRaw text → index 1-based
 */
export function normalizeQuestion(data: unknown, idx: number): LearnerQuestion {
  const source  = (data ?? {}) as Record<string, unknown>;
  const payload = parsePayload(source.payload ?? source.Payload);

  const rawOptions     = readOptions(source, payload);
  const rawType        = pickFirst(source, payload, ['questionType', 'question_type', 'type', 'loai']);
  const normalizedType = normalizeQuestionType(rawType);

  // Ưu tiên correctAnswer compact (index/letter) từ payload trước
  let rawAnswer = String(
    payload.correctAnswer  ??
    payload.correct_answer ??
    payload.correctOption  ??
    payload.correct_option ??
    source.correctAnswer   ??
    source.correct_answer  ??
    source.correctOption   ??
    source.correct_option  ??
    source.answerRaw       ??
    source.answer_raw      ??
    payload.answerRaw      ??
    payload.answer_raw     ??
    pickFirst(source, payload, ['gradingRubric', 'grading_rubric', 'rubric']) ??
    '',
  ).trim();

  // Nếu answerRaw là text dài trùng với một trong các options → ánh xạ về index 1-based
  if (rawAnswer && !/^\d+$/.test(rawAnswer) && rawOptions.length > 0) {
    const cleanRaw = rawAnswer.trim().toLowerCase();
    const foundIdx = rawOptions.findIndex((opt) => {
      const cleanOpt    = (opt ?? '').trim().toLowerCase();
      if (!cleanOpt) return false;
      if (cleanOpt === cleanRaw) return true;
      const strippedOpt = cleanOpt.replace(/^[a-j1-9][.):\s-]+\s*/i, '').trim();
      const strippedRaw = cleanRaw.replace(/^[a-j1-9][.):\s-]+\s*/i, '').trim();
      return (
        (strippedOpt && strippedOpt === cleanRaw) ||
        (strippedRaw && strippedRaw === cleanOpt) ||
        (strippedOpt && strippedOpt === strippedRaw)
      );
    });
    if (foundIdx !== -1) rawAnswer = String(foundIdx + 1);
  }

  const inferredMatching   = looksLikeMatchingFromTwoColumns(normalizedType, rawOptions);
  const matchingNormalized = inferredMatching
    ? normalizeMatchingFromTwoColumns(rawOptions, rawAnswer)
    : { options: rawOptions, answerRaw: rawAnswer };

  const inferredMulti = !inferredMatching && looksLikeMultiFromAnswer(normalizedType, matchingNormalized.answerRaw);

  // Auto-detect ESSAY khi không có options hoặc payload có rubric
  const isEssayFallback =
    !rawType &&
    (rawOptions.length === 0 || !!payload.rubric || !!payload.gradingRubric || rawAnswer.toLowerCase().includes('barem'));

  const effectiveType = isEssayFallback ? 'ESSAY' : normalizedType;
  const finalType: QuestionType = inferredMatching ? 'MATCHING' : inferredMulti ? 'MULTI' : effectiveType;

  const optionsArray = Array.isArray(source.options)
    ? source.options.map((x) => String(x ?? ''))
    : Array.isArray(payload.options)
    ? (payload.options as unknown[]).map((x) =>
        typeof x === 'object' && x ? String((x as Record<string, unknown>).text ?? (x as Record<string, unknown>).value ?? '') : String(x ?? ''),
      )
    : [];

  let content        = String(pickFirst(source, payload, ['content', 'question', 'title']) ?? '');
  let contextTitle   = String(pickFirst(source, payload, ['contextTitle', 'context_title']) ?? (source.context as Record<string, unknown>)?.title ?? '');
  let contextContent = String(pickFirst(source, payload, ['contextContent', 'context_content']) ?? (source.context as Record<string, unknown>)?.content ?? '');
  const contextId    = String(pickFirst(source, payload, ['contextId', 'context_id']) ?? (source.context as Record<string, unknown>)?.id ?? '') || undefined;

  // [Universal Shared Context] Auto-detect [Bài đọc hiểu]:...\n---\n{câu hỏi}
  if (!contextContent) {
    const isSharedContextPattern =
      /(?:b(?:ài|ai)\s*d(?:ọc|oc)\s*hi(?:ểu|eu)|reading\s*passage|đoạn\s*văn)/i.test(content) &&
      /\n\s*---\s*\n/.test(content);
    if (isSharedContextPattern) {
      const parts = content.split(/\n\s*---\s*\n/);
      if (parts.length >= 2) {
        const rawPassage  = parts[0];
        const passagePart = rawPassage.replace(/^[\[\s\w]*\[?.*?(?:bài|bai|reading|đoạn).*?(?:hiểu|hieu|passage|văn)\]?:?\s*/i, '').trim();
        const specificPart = parts.slice(1).join('\n---\n').trim();
        if (passagePart && specificPart) {
          contextContent = passagePart;
          contextTitle   = contextTitle || 'Bài đọc hiểu dùng chung';
          content        = specificPart;
        }
      }
    }
  }

  return {
    id:                  String(pickFirst(source, payload, ['id']) ?? `local-${idx + 1}`),
    questNo:             Number(pickFirst(source, payload, ['questNo', 'quest_no', 'index']) ?? idx + 1),
    content,
    questionType:        finalType,
    questionMediaUrl:    String(pickFirst(source, payload, ['questionMediaUrl', 'question_media_url']) ?? ''),
    questionMediaType:   String(pickFirst(source, payload, ['questionMediaType', 'question_media_type', 'contentType', 'content_type']) ?? 'text'),
    optionMediaType:     String(pickFirst(source, payload, ['optionMediaType', 'option_media_type', 'optionType', 'option_type']) ?? 'text'),
    contentType:         String(pickFirst(source, payload, ['contentType', 'content_type', 'questionMediaType', 'question_media_type']) ?? 'text'),
    optionType:          String(pickFirst(source, payload, ['optionType', 'option_type', 'optionMediaType', 'option_media_type']) ?? 'text'),
    options:             optionsArray.length > 0 ? optionsArray : matchingNormalized.options.filter((x) => x.trim() !== ''),
    answerRaw:           matchingNormalized.answerRaw,
    citation:            String(pickFirst(source, payload, ['citation', 'reference', 'explanation']) ?? ''),
    topicCode:           String(pickFirst(source, payload, ['topicCode', 'topic_code', 'categoryCode', 'category_code']) ?? ''),
    topicName:           String(pickFirst(source, payload, ['topicName', 'topic_name', 'categoryName', 'category_name']) ?? ''),
    points:              Number(pickFirst(source, payload, ['points', 'score']) ?? 1),
    difficulty:          Number(pickFirst(source, payload, ['difficulty', 'level']) ?? 1),
    durationSec:         Number(pickFirst(source, payload, ['durationSec', 'duration_sec', 'duration', 'durationSeconds']) ?? 0),
    enabled:             Boolean(pickFirst(source, payload, ['enabled', 'isEnabled', 'active']) ?? true),
    isCritical:          Boolean(pickFirst(source, payload, ['isCritical', 'is_critical', 'critical']) ?? false),
    subCategory:         String(pickFirst(source, payload, ['subCategory', 'sub_category']) ?? '') || undefined,
    contextId,
    contextTitle:        contextTitle  || undefined,
    contextContent:      contextContent || undefined,
    domainCode:          String(pickFirst(source, payload, ['domainCode', 'domain_code', 'domain']) ?? 'EDUCATION'),
    tags:                (Array.isArray(source.tags) ? source.tags : Array.isArray(payload?.tags) ? payload.tags : []) as string[],
    targetLevel:         String(pickFirst(source, payload, ['targetLevel', 'target_level', 'gradeLevel', 'grade']) ?? '') || undefined,
    assessmentPurpose:   String(pickFirst(source, payload, ['assessmentPurpose', 'assessment_purpose', 'examType', 'exam_type']) ?? '') || undefined,
    issuingOrg:          String(pickFirst(source, payload, ['issuingOrg', 'issuing_org', 'sourceSchool', 'sourceOrg', 'source_org']) ?? '') || undefined,
    benchmarkYear:       Number(pickFirst(source, payload, ['benchmarkYear', 'benchmark_year', 'year']) ?? 0) || undefined,
    benchmarkStandard:   String(pickFirst(source, payload, ['benchmarkStandard', 'benchmark_standard', 'standard']) ?? '') || undefined,
    // Ownership & Attribution metadata
    creatorId:           String(pickFirst(source, payload, ['creatorId', 'creator_id', 'authorId', 'author_id', 'userId']) ?? '') || undefined,
    creatorName:         String(pickFirst(source, payload, ['creatorName', 'creator_name', 'authorName', 'author_name', 'author']) ?? '') || undefined,
    orgUnitId:           String(pickFirst(source, payload, ['orgUnitId', 'org_unit_id', 'teamId', 'team_id']) ?? '') || undefined,
    orgUnitName:         String(pickFirst(source, payload, ['orgUnitName', 'org_unit_name', 'teamName', 'team_name']) ?? '') || undefined,
    scope:               (pickFirst(source, payload, ['scope']) as any) || 'COMMUNITY',
    visibilityScope:     (pickFirst(source, payload, ['visibilityScope', 'visibility_scope']) as any) || 'PUBLIC',
    contributionStatus:  (pickFirst(source, payload, ['contributionStatus', 'contribution_status']) as any) || undefined,
  };
}
