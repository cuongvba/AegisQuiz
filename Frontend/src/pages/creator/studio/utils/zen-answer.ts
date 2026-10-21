/**
 * Chuẩn hóa đáp án do bộ bóc tách Word trả về.
 * Định dạng thực tế từ backend: chỉ số 1-based ("2"), nhiều đáp án ("1,3"), chữ cái ("B"),
 * chuỗi Đúng/Sai nhiều ý ("a) Đúng, b) Sai") hoặc đáp số tự luận ("0,4").
 */

const CHOICE_FORM = /^[\d,;\sA-Da-d]*$/;

export function isChoiceAnswerForm(answer: string | undefined, optionCount: number): boolean {
  return optionCount > 0 && CHOICE_FORM.test(answer ?? '');
}

/** Trả về danh sách chỉ số 0-based của các phương án đúng. */
export function resolveCorrectIndexes(answer: string | undefined, optionCount: number): number[] {
  if (!answer || !isChoiceAnswerForm(answer, optionCount)) return [];
  const result = new Set<number>();
  for (const token of answer.split(/[,;\s]+/).filter(Boolean)) {
    const index = /^\d+$/.test(token)
      ? parseInt(token, 10) - 1
      : token.toUpperCase().charCodeAt(0) - 65;
    if (index >= 0 && index < optionCount) result.add(index);
  }
  return [...result].sort((a, b) => a - b);
}

/** Chuyển danh sách chỉ số 0-based về chuỗi đáp án backend (1-based). */
export function serializeCorrectIndexes(indexes: number[]): string {
  return [...indexes].sort((a, b) => a - b).map((i) => i + 1).join(',');
}

export function toggleCorrectIndex(answer: string | undefined, optionCount: number, index: number, multi: boolean): string {
  const current = resolveCorrectIndexes(answer, optionCount);
  if (!multi) return serializeCorrectIndexes([index]);
  return serializeCorrectIndexes(
    current.includes(index) ? current.filter((i) => i !== index) : [...current, index],
  );
}

export const isMultiAnswerType = (questionType?: string): boolean =>
  (questionType ?? '').toUpperCase() === 'MULTI';
