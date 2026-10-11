import { LessonSlice } from '../types/slice-lesson';

/**
 * Explanation-to-Slice Transpiler
 * Phân rã văn bản lời giải chi tiết (Markdown / Text / LaTeX) thành 4 chặng nhận thức sư phạm:
 * 1. Manh mối (The Clue)
 * 2. Khái niệm / Công thức (The Concept)
 * 3. Từng bước giải phẫu (Step-by-step Deduction)
 * 4. Mẹo ghi nhớ sâu (Memory Hook)
 */
export function transpileExplanationToSlices(
  rawExplanation: string = '',
  questionPrompt: string = '',
  domainCode: string = 'GENERAL'
): LessonSlice[] {
  const cleanExp = rawExplanation.trim();
  const cleanPrompt = questionPrompt.trim();

  // 1. Manh mối nhận biết
  const sliceClue: LessonSlice = {
    id: 'slice-1-clue',
    order: 1,
    conceptType: 'clue',
    badgeLabel: 'Bước 1: Manh Mối',
    title: 'Nhận Diện Dấu Hiệu Then Chốt',
    content: cleanPrompt 
      ? `Quan sát kỹ yêu cầu đề bài: "${cleanPrompt.length > 90 ? cleanPrompt.substring(0, 87) + '...' : cleanPrompt}"` 
      : 'Hãy tìm các từ khóa mấu chốt được nhấn mạnh trong câu hỏi.',
    visualIcon: '🔍',
    speechText: 'Bước 1: Bé và các bạn hãy cùng chú ý đến từ khóa mấu chốt trong câu hỏi nhé!',
    highlightPhrase: 'Từ khóa mấu chốt',
    accentColor: 'from-amber-400 to-orange-500',
  };

  // 2. Khái niệm cốt lõi / Quy luật
  let conceptContent = 'Áp dụng quy tắc tiêu chuẩn và lý thuyết nền tảng tương ứng với bài học.';
  if (cleanExp.length > 0) {
    const sentences = cleanExp.split(/[.\n]/).filter(s => s.trim().length > 10);
    if (sentences.length > 0) {
      conceptContent = sentences[0].trim();
    }
  }

  const sliceConcept: LessonSlice = {
    id: 'slice-2-concept',
    order: 2,
    conceptType: 'concept',
    badgeLabel: 'Bước 2: Quy Luật',
    title: 'Bản Chất & Quy Tắc Cốt Lõi',
    content: conceptContent,
    visualIcon: '💡',
    speechText: `Bước 2: Bản chất của vấn đề ở đây là: ${conceptContent.substring(0, 100)}`,
    highlightPhrase: 'Quy tắc cốt lõi',
    accentColor: 'from-cyan-400 to-blue-500',
  };

  // 3. Phân tích từng bước
  let stepContent = cleanExp.length > 0 ? cleanExp : 'Loại trừ các phương án gây nhiễu và đối chiếu trực tiếp với điều kiện cần và đủ.';
  const sliceStep: LessonSlice = {
    id: 'slice-3-step',
    order: 3,
    conceptType: 'step',
    badgeLabel: 'Bước 3: Giải Phẫu',
    title: 'Phân Tích Chi Tiết & Thao Tác',
    content: stepContent,
    visualIcon: '⚡',
    speechText: 'Bước 3: Từng bước giải quyết rất rõ ràng và mạch lạc!',
    highlightPhrase: 'Thực hiện thao tác',
    accentColor: 'from-emerald-400 to-teal-500',
  };

  // 4. Mẹo ghi nhớ siêu tốc
  let hookContent = 'Ghi nhớ hình ảnh liên tưởng và quy luật then chốt để áp dụng cho các bài tập tương tự!';
  if (domainCode.includes('KID') || domainCode.includes('ENGLISH')) {
    hookContent = '⭐ Nhắm mắt lại và đọc to từ vựng 3 lần cùng nụ cười tươi: Bé đã làm chủ kiến thức này!';
  } else if (domainCode.includes('DRIVING')) {
    hookContent = '🚗 Mẹo: Nhất chớm, nhì ưu, tam đường, tứ hướng. Gặp xe ưu tiên luôn nhường đường!';
  } else if (domainCode.includes('BANKING')) {
    hookContent = '🏦 Mẹo: Luôn kiểm tra tính pháp lý và luồng tiền trước khi duyệt hồ sơ tín dụng!';
  }

  const sliceHook: LessonSlice = {
    id: 'slice-4-hook',
    order: 4,
    conceptType: 'memory_hook',
    badgeLabel: 'Bước 4: Mẹo Nhớ Sâu',
    title: 'Khắc Cốt Ghi Tâm',
    content: hookContent,
    visualIcon: '🎯',
    speechText: `Mẹo ghi nhớ siêu tốc cho bạn: ${hookContent.substring(0, 80)}`,
    highlightPhrase: 'Bí quyết ghi nhớ',
    accentColor: 'from-purple-400 to-pink-500',
  };

  return [sliceClue, sliceConcept, sliceStep, sliceHook];
}
