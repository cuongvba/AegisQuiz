import type { BankTopic } from '../../types';

export const DOMAIN_OPTIONS = [
  { code: 'GENERAL', label: '🌐 Tổng Hợp / Mặc Định', short: 'Tổng hợp', icon: '🌐', color: 'from-slate-500 to-gray-600', badge: 'bg-slate-800 text-slate-300 border-slate-700' },
  { code: 'EDUCATION', label: '🎓 Giáo Dục Phổ Thông & ĐH', short: 'Giáo dục', icon: '🎓', color: 'from-blue-500 to-indigo-600', badge: 'bg-blue-950/70 text-blue-300 border-blue-800/60' },
  { code: 'BANKING', label: '🏦 Ngân Hàng & Tài Chính', short: 'Ngân hàng', icon: '🏦', color: 'from-emerald-500 to-teal-600', badge: 'bg-emerald-950/70 text-emerald-300 border-emerald-800/60' },
  { code: 'HEALTHCARE', label: '🩺 Y Tế & Dược Phẩm', short: 'Y tế', icon: '🩺', color: 'from-rose-500 to-pink-600', badge: 'bg-rose-950/70 text-rose-300 border-rose-800/60' },
  { code: 'HSE', label: '🦺 An Toàn Lao Động & HSE', short: 'HSE', icon: '🦺', color: 'from-amber-500 to-yellow-600', badge: 'bg-amber-950/70 text-amber-300 border-amber-800/60' },
  { code: 'GOV_DRIVING', label: '🚗 Sát Hạch Lái Xe (GPLX)', short: 'GPLX', icon: '🚗', color: 'from-cyan-500 to-blue-600', badge: 'bg-cyan-950/70 text-cyan-300 border-cyan-800/60' },
  { code: 'IT_SECURITY', label: '💻 CNTT & An Toàn Thông Tin', short: 'CNTT', icon: '💻', color: 'from-violet-500 to-purple-600', badge: 'bg-violet-950/70 text-violet-300 border-violet-800/60' },
];

export const DOMAIN_SUGGESTED_TAGS: Record<string, string[]> = {
  EDUCATION: ['toan-hoc', 'thpt-quoc-gia', 'lop-12', 'lop-11', 'amsterdam', 'gdpt-2018', 'tieng-anh', 'doc-hieu', 'ngu-van', 'vat-ly', 'hoa-hoc'],
  BANKING: ['tin-dung', 'ngan-quy', 'aml-chong-rua-tien', 'kiem-soat-noi-bo', 'thanh-toan-quoc-te', 'thong-tu-41', 'basel-iii', 'vietcombank', 'bidv'],
  HEALTHCARE: ['bac-si-cki', 'dieu-duong', 'duoc-lam-sang', 'cap-cuu-hoi-suc', 'kiem-soat-nhiem-khuan', 'an-toan-nguoi-benh', 'bo-y-te', 'phac-do-2025'],
  HSE: ['an-toan-lao-dong', 'iso-45001', 'nhom-2', 'nhom-3', 'pccc', 'dien-cao-the', 'hoa-chat-doc-hai', 'khong-gian-kin'],
  GOV_DRIVING: ['gplx-b2', 'gplx-c', 'sa-hinh', 'bien-bao', 'diem-liet', 'nghi-dinh-100', 'luat-giao-thong-duong-bo', 'qcvn-41'],
  IT_SECURITY: ['iso-27001', 'owasp-top-10', 'soc-siem', 'devsecops', 'network-security', 'cloud-aws-azure', 'penetration-test'],
  GENERAL: ['nang-ngach', 'tuyen-dung', 'noi-quy', 'van-hoa-doanh-nghiep', 'ky-nang-mem'],
};

// Stop-words tiếng Việt thông dụng cần lọc khi sinh mã chủ đề
export const STOP_WORDS_SET = new Set([
  've', 'va', 'cua', 'cac', 'nhung', 'cho', 'trong', 'theo', 'voi', 'boi',
  'quy', 'dinh', 'huong', 'dan', 'nghiep', 'vu', 'ngan', 'hang', 'cau', 'hoi',
  'bo', 'de', 'thi', 'tuyen', 'dung', 'sat', 'hach', 'kiem', 'tra', 'tai', 'lieu'
]);

// Tập token viết tắt quốc tế hoặc chuyên ngành đã được chuẩn hoá sẵn
export const STANDARD_ACRONYM_TOKENS = new Set([
  'sme', 'aml', 'iso', 'gplx', 'cntt', 'attt', 'vcb', 'bidv', 'pccc', 
  'ielts', 'toeic', 'thpt', 'gdpt', 'jci', 'cvc', 'gdv', 'tsbd', 'tdts', 'soc', 'aws', 'cic'
]);

/**
 * [Dynamic Semantic Acronym Generator]
 * Thuật toán sinh mã chủ đề tự động thông minh:
 * 1. Ưu tiên đối chiếu với danh mục Topics hiện có trong DB của Tenant.
 * 2. Phân tích cụm từ tiếng Việt không dấu, loại bỏ stop-words, giữ nguyên acronym chuyên ngành.
 * 3. Tự động trích xuất các chữ cái đầu của các từ ghép để tạo mã UPPER_SNAKE_CASE chuẩn mực.
 */
export function generateTopicCode(
  name: string, 
  existingTopics?: BankTopic[], 
  customDictionary?: Record<string, string>, 
  maxLen = 14
): string {
  if (!name) return '';

  // 1. Đối chiếu kho Topics từ DB
  if (existingTopics && existingTopics.length > 0) {
    const cleanLower = name.trim().toLowerCase();
    const matched = existingTopics.find(t => 
      t.name.trim().toLowerCase() === cleanLower ||
      t.code.toLowerCase() === cleanLower
    );
    if (matched) return matched.code;
  }

  // 2. Tra cứu từ điển tùy biến nếu được Tenant cấu hình trong Presets
  if (customDictionary && Object.keys(customDictionary).length > 0) {
    const cleanLower = name.trim().toLowerCase();
    for (const [phrase, code] of Object.entries(customDictionary)) {
      if (cleanLower.includes(phrase.toLowerCase())) {
        return code.toUpperCase();
      }
    }
  }

  // 3. Xóa STT đầu dòng ("01.", "1 -", "Bài 2: "...)
  const clean = name.replace(/^(?:bài|phần|chương|đề|câu|stt)?\s*\d+[\.\:\-\s_]+\s*/i, '').trim();

  // Bỏ dấu tiếng Việt chuẩn Unicode NFD
  let nonAccent = clean
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[đĐ]/g, 'd')
    .toLowerCase();

  // Tách đợt / năm
  let periodTag = '';
  const mDot = nonAccent.match(/(?:dot|d)\s*0?(\d+)\b/i);
  const mYear = nonAccent.match(/\b20(2[0-9])\b/);
  if (mDot && mYear) periodTag = `D${mDot[1]}_${mYear[1]}`;
  else if (mDot) periodTag = `D${mDot[1]}`;
  else if (mYear) periodTag = mYear[1];

  nonAccent = nonAccent
    .replace(/(?:dot|d)\s*0?\d+\b/gi, ' ')
    .replace(/\b202[0-9]\b/g, ' ')
    .replace(/(?:nam|năm)\s*/gi, ' ');

  // Tách các từ còn lại, lọc stop words
  const words = nonAccent
    .replace(/[^a-z0-9\s_-]/g, ' ')
    .split(/[\s_-]+/)
    .filter(w => w.length > 0 && !STOP_WORDS_SET.has(w));

  const tokens: string[] = [];
  const normalWords: string[] = [];

  for (const w of words) {
    if (STANDARD_ACRONYM_TOKENS.has(w)) {
      tokens.push(w.toUpperCase());
    } else {
      normalWords.push(w);
    }
  }

  if (normalWords.length > 0) {
    if (normalWords.length === 1) {
      tokens.push(normalWords[0].toUpperCase());
    } else if (normalWords.length === 2 && tokens.length === 0) {
      tokens.push(normalWords[0].slice(0, 3).toUpperCase());
      tokens.push(normalWords[1].slice(0, 3).toUpperCase());
    } else {
      tokens.push(normalWords.map(w => w[0].toUpperCase()).join(''));
    }
  }

  if (periodTag) tokens.push(periodTag);
  if (tokens.length === 0) tokens.push('TOPIC');

  const uniqueTokens = Array.from(new Set(tokens));
  let result = uniqueTokens.join('_').replace(/_+/g, '_').replace(/^_|_$/g, '');
  if (result.length > maxLen) {
    result = result.slice(0, maxLen).replace(/_$/, '');
  }
  return result.toUpperCase();
}
