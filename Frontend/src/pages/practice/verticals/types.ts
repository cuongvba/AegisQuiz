export type PracticeVerticalKey = 'ALL' | 'DRIVING' | 'K12_MATH' | 'BANKING' | 'ENGLISH' | 'GENERAL';

export interface PracticeVerticalConfig {
  key: PracticeVerticalKey;
  domainCode: string;
  name: string;
  shortTitle: string;
  description: string;
  heroBadge: string;
  heroIcon: string;
  themeGradient: string;
  accentColor: string;
  tagline: string;
}

export const VERTICAL_PRESETS: Record<string, PracticeVerticalConfig> = {
  DRIVING: {
    key: 'DRIVING',
    domainCode: 'DRIVING',
    name: 'Trung Tâm Luyện Thi Sát Hạch Giấy Phép Lái Xe',
    shortTitle: 'Sát Hạch Lái Xe',
    description: 'Kho 600 câu hỏi sát hạch chuẩn Cục Đường bộ Việt Nam, lọc 60 câu điểm liệt và bộ đề thi hạng A1, A2, B1, B2, C.',
    heroBadge: 'CHUẨN CỤC ĐƯỜNG BỘ',
    heroIcon: '🚗',
    themeGradient: 'from-amber-600/30 via-orange-600/20 to-slate-900',
    accentColor: 'text-amber-400',
    tagline: 'Luyện đề ngẫu nhiên 35 câu • 22 phút • Đỗ 100%',
  },
  BANKING: {
    key: 'BANKING',
    domainCode: 'BANKING',
    name: 'Đấu Trường Khảo Thí Nghiệp Vụ Ngân Hàng & Tài Chính',
    shortTitle: 'Nghiệp Vụ Ngân Hàng',
    description: 'Ngân hàng đề thi nội bộ phân cấp theo Chi nhánh, Khối phòng ban: Tín dụng, Rủi ro, Kế toán và AML.',
    heroBadge: 'ENTERPRISE COMPLIANCE',
    heroIcon: '🏦',
    themeGradient: 'from-emerald-600/30 via-teal-600/20 to-slate-900',
    accentColor: 'text-emerald-400',
    tagline: 'Chuẩn mực ISO 27001 • Đánh giá năng lực định kỳ',
  },
  K12_MATH: {
    key: 'K12_MATH',
    domainCode: 'EDUCATION',
    name: 'Đấu Trường Khảo Thí Toán Học & K-12 GDPT',
    shortTitle: 'Toán Học K-12',
    description: 'Phân tầng theo chương trình GDPT 2018, chuẩn thang Bloom: Nhận biết, Thông hiểu, Vận dụng, Vận dụng cao.',
    heroBadge: 'CHƯƠNG TRÌNH GDPT 2018',
    heroIcon: '📐',
    themeGradient: 'from-blue-600/30 via-indigo-600/20 to-slate-900',
    accentColor: 'text-blue-400',
    tagline: 'KaTeX MathType Vector • Đúng/Sai 4 ý • Trả lời ngắn',
  },
  ENGLISH: {
    key: 'ENGLISH',
    domainCode: 'EDUCATION',
    name: 'English Assessment & IELTS Mastery Studio',
    shortTitle: 'Tiếng Anh Chuẩn Quốc Tế',
    description: 'Luyện tập kỹ năng Reading, Listening, Ngữ pháp chuyên sâu tương thích khung tham chiếu Châu Âu CEFR.',
    heroBadge: 'GLOBAL CEFR & IELTS',
    heroIcon: '🇬🇧',
    themeGradient: 'from-purple-600/30 via-pink-600/20 to-slate-900',
    accentColor: 'text-purple-400',
    tagline: 'Split-screen Reading • Chấm điểm tự động tức thì',
  },
  GENERAL: {
    key: 'GENERAL',
    domainCode: 'ALL',
    name: 'Không Gian Luyện Tập Tri Thức Toàn Năng',
    shortTitle: 'Tổng Hợp Toàn Ngành',
    description: 'Khám phá toàn bộ kho tri thức đa lĩnh vực: Khảo thí thích ứng IRT, Đấu trường trực tiếp và Luyện tập cá nhân hóa.',
    heroBadge: 'SOVEREIGN ADAPTIVE',
    heroIcon: '✨',
    themeGradient: 'from-cyan-600/30 via-blue-600/20 to-slate-900',
    accentColor: 'text-cyan-400',
    tagline: 'IRT 3-PL Thích ứng • Chấm thi đa năng',
  },
};
