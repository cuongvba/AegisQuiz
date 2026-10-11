export interface MobileAppVariant {
  id: string;
  code: string;
  domainCode: string;
  name: string;
  shortName: string;
  tagline: string;
  icon: string; // Lucide icon identifier
  colorScheme: {
    primary: string; // Hex color
    primaryGlow: string; // Tailwind glow / shadow
    accent: string;
    bgGradient: string;
    badgeBg: string;
    badgeText: string;
  };
  features: string[];
  suggestedTags: string[];
  sampleTopics: { id: string; name: string; questionCount: number; icon: string }[];
}

export const MOBILE_APP_VARIANTS: Record<string, MobileAppVariant> = {
  driving: {
    id: 'driving',
    code: 'DRIVING',
    domainCode: 'DRIVING',
    name: 'AegisDrive — Sát Hạch Lái Xe',
    shortName: 'Lái Xe GPLX',
    tagline: 'Luyện 600 câu hỏi GPLX, 60 câu điểm liệt & Sa hình chuẩn Quốc gia',
    icon: 'Car',
    colorScheme: {
      primary: '#10b981', // Emerald
      primaryGlow: 'rgba(16, 185, 129, 0.35)',
      accent: '#34d399',
      bgGradient: 'from-emerald-950/80 via-slate-950 to-slate-950',
      badgeBg: 'bg-emerald-900/60 border-emerald-500/40',
      badgeText: 'text-emerald-300',
    },
    features: ['60 Câu Điểm Liệt', 'Mẹo Sa Hình & Biển Báo', 'Thi Thử B1/B2/C/D'],
    suggestedTags: ['diem-liet', 'sa-hinh', 'bien-bao', 'gplx-b2', 'gplx-c'],
    sampleTopics: [
      { id: 'dl-60', name: '60 Câu Hỏi Điểm Liệt Cốt Tử', questionCount: 60, icon: 'AlertTriangle' },
      { id: 'bb-road', name: 'Hệ Thống Biển Báo Đường Bộ', questionCount: 182, icon: 'ShieldAlert' },
      { id: 'sh-rule', name: 'Quy Tắc Sa Hình & Thứ Tự Đi', questionCount: 114, icon: 'Navigation' },
      { id: 'nv-drive', name: 'Nghiệp Vụ Vận Tải & Kỹ Thuật Lái', questionCount: 85, icon: 'Truck' },
    ],
  },

  banking: {
    id: 'banking',
    code: 'BANKING',
    domainCode: 'BANKING',
    name: 'AegisBank — Khảo Thí Ngân Hàng',
    shortName: 'Tài Chính & Bank',
    tagline: 'Nghiệp vụ Tín dụng, Thẩm định Tài chính, AML & Tiêu chuẩn Basel II',
    icon: 'Landmark',
    colorScheme: {
      primary: '#0284c7', // Sky / Deep Blue
      primaryGlow: 'rgba(2, 132, 199, 0.35)',
      accent: '#38bdf8',
      bgGradient: 'from-sky-950/80 via-slate-950 to-slate-950',
      badgeBg: 'bg-sky-900/60 border-sky-500/40',
      badgeText: 'text-sky-300',
    },
    features: ['Tín Dụng & Thẩm Định', 'Phòng Chống Rửa Tiền AML', 'Chuẩn Mực Basel II/III'],
    suggestedTags: ['aml', 'tin-dung', 'tham-dinh', 'kiem-soat-noi-bo', 'ngoai-hoi'],
    sampleTopics: [
      { id: 'aml-law', name: 'Luật Phòng Chống Rửa Tiền (AML)', questionCount: 120, icon: 'ShieldCheck' },
      { id: 'cr-appr', name: 'Thẩm Định Phương Án Vay Vốn', questionCount: 95, icon: 'FileText' },
      { id: 'fx-risk', name: 'Quản Trị Rủi Ro Ngoại Hối & Lãi Suất', questionCount: 75, icon: 'TrendingUp' },
      { id: 'retail-op', name: 'Nghiệp Vụ Giao Dịch Viên Bán Lẻ', questionCount: 110, icon: 'Users' },
    ],
  },

  thpt: {
    id: 'thpt',
    code: 'EDUCATION',
    domainCode: 'EDUCATION',
    name: 'Dehoc THPT — Chinh Phục Điểm 10',
    shortName: 'Thi Tốt Nghiệp THPT',
    tagline: 'Luyện thi Quốc Gia, Đánh giá Năng lực (ĐHQG) theo Chuẩn GDPT 2018',
    icon: 'GraduationCap',
    colorScheme: {
      primary: '#8b5cf6', // Violet
      primaryGlow: 'rgba(139, 92, 246, 0.35)',
      accent: '#a78bfa',
      bgGradient: 'from-purple-950/80 via-slate-950 to-slate-950',
      badgeBg: 'bg-purple-900/60 border-purple-500/40',
      badgeText: 'text-purple-300',
    },
    features: ['Toán Học 12 Chuyên Sâu', 'ĐGNL ĐHQG Hà Nội & TP.HCM', 'Format Đề Mới 2026'],
    suggestedTags: ['toan-12', 'ham-so', 'hinh-khong-gian', 'dgnl', 'gdpt-2018'],
    sampleTopics: [
      { id: 'math-func', name: 'Khảo Sát Hàm Số & Ứng Dụng Thực Tế', questionCount: 150, icon: 'TrendingUp' },
      { id: 'math-geo', name: 'Hình Học Không Gian Tọa Độ Oxyz', questionCount: 120, icon: 'Box' },
      { id: 'math-prob', name: 'Xác Suất Có Điều Kiện & Thống Kê', questionCount: 90, icon: 'PieChart' },
      { id: 'dgnl-logic', name: 'Tư Duy Định Lượng & Suy Luận Logic', questionCount: 200, icon: 'Brain' },
    ],
  },

  med: {
    id: 'med',
    code: 'HEALTHCARE',
    domainCode: 'HEALTHCARE',
    name: 'AegisMed — Y Khoa & Dược Phẩm',
    shortName: 'Y Dược & Bác Sĩ',
    tagline: 'Chứng chỉ Hành nghề Y, Dược lâm sàng, Bác sĩ Nội trú & Chuẩn JCI',
    icon: 'HeartPulse',
    colorScheme: {
      primary: '#e11d48', // Rose / Ruby
      primaryGlow: 'rgba(225, 29, 72, 0.35)',
      accent: '#fb7185',
      bgGradient: 'from-rose-950/80 via-slate-950 to-slate-950',
      badgeBg: 'bg-rose-900/60 border-rose-500/40',
      badgeText: 'text-rose-300',
    },
    features: ['Dược Lâm Sàng', 'Phác Đồ Cấp Cứu A&E', 'Chẩn Đoán Hình Ảnh'],
    suggestedTags: ['duoc-ly', 'noi-tru', 'cap-cuu', 'jci', 'ngoai-khoa'],
    sampleTopics: [
      { id: 'pharma-core', name: 'Dược Lý Học & Tương Tác Thuốc', questionCount: 140, icon: 'Pill' },
      { id: 'icu-emrg', name: 'Hồi Sức Cấp Cứu Chống Độc', questionCount: 115, icon: 'Activity' },
      { id: 'diag-img', name: 'Đọc Phim X-Quang & CT Scan Ngực', questionCount: 80, icon: 'Eye' },
    ],
  },

  hse: {
    id: 'hse',
    code: 'HSE',
    domainCode: 'HSE',
    name: 'AegisSafety — An Toàn Lao Động & PCCC',
    shortName: 'An Toàn PCCC',
    tagline: 'Sát hạch Thẻ An toàn Nhóm 1-6, PCCC Cứu nạn & Tiêu chuẩn ISO 45001',
    icon: 'HardHat',
    colorScheme: {
      primary: '#ea580c', // Orange
      primaryGlow: 'rgba(234, 88, 12, 0.35)',
      accent: '#fb923c',
      bgGradient: 'from-orange-950/80 via-slate-950 to-slate-950',
      badgeBg: 'bg-orange-900/60 border-orange-500/40',
      badgeText: 'text-orange-300',
    },
    features: ['An Toàn Điện & Làm Việc Trên Cao', 'Quy Chuẩn PCCC 2026', 'Thẻ An Toàn Lao Động'],
    suggestedTags: ['pccc', 'iso-45001', 'an-toan-dien', 'the-an-toan', 'hoa-chat'],
    sampleTopics: [
      { id: 'fire-pccc', name: 'Quy Trình Thoát Hiểm & Sử Dụng Bình PCCC', questionCount: 95, icon: 'Flame' },
      { id: 'elec-safe', name: 'An Toàn Thi Công Điện Cao Thế', questionCount: 80, icon: 'Zap' },
      { id: 'chem-haz', name: 'Xử Lý Sự Cố Tràn Đổ Hóa Chất', questionCount: 70, icon: 'AlertOctagon' },
    ],
  },
};

export const DEFAULT_MOBILE_VARIANT = MOBILE_APP_VARIANTS.driving;
