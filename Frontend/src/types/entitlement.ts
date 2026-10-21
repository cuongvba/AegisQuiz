/**
 * [Enterprise Entitlements & Monetization Types]
 * SSOT cho kiểm soát tính năng và phân quyền 4 tầng: Tenant -> Domain -> Group -> User.
 */

export type TenantPlan = 'Starter' | 'Professional' | 'Enterprise' | 'Global';
export type TenantScaleType = 'Individual' | 'Team' | 'Classroom' | 'Organization' | 'Enterprise' | 'Global';

export const FeatureKey = {
  // Xuất bản đề thi (Universal Transpiler)
  TranspilerMoodle: 'transpiler:moodle',
  TranspilerGift:   'transpiler:gift',
  TranspilerQti:    'transpiler:qti',
  TranspilerCanvas: 'transpiler:canvas',

  // AI & Khảo thí nâng cao
  AiSymposium:      'ai:symposium',
  IrtCat3Pl:        'irt:cat3pl',
  AdvancedMathWmf:  'math:vector_wmf',

  // Đấu trường tương tác
  ArenaGameshow:    'arena:gameshow',

  // Quản trị tổ chức & Doanh nghiệp
  HierarchyTree:      'org:hierarchy_tree',
  WhiteLabel:         'branding:white_label',
  WhiteLabelTheming:  'branding:white_label_theming',
  AuditLog:           'security:audit_log',
  DynamicDomains:     'tenant:dynamic_domains',

  // Enterprise Studio & Trích Xuất Chuyên Sâu
  SplitScreenStudio:  'studio:split_screen',
  AiAutoRubricSolve:  'ai:auto_rubric_solve',
} as const;

export type FeatureKeyType = typeof FeatureKey[keyof typeof FeatureKey];

export interface TenantQuotaLimits {
  maxUsers: number;
  maxQuestionsPerMonth: number;
  maxConcurrentArenaSeats: number;
  aiTokensMonthly: number;
  storageLimitMb: number;
}

export interface TenantInfo {
  id: string;
  code: string;
  name: string;
  plan: TenantPlan;
  scaleType: TenantScaleType;
  primaryColor?: string;
  logoUrl?: string;
  isCommunity: boolean;
}

export interface UserEntitlementContext {
  id?: string;
  role: string;
  orgUnitId?: string;
  orgUnitPath?: string;
  capabilities: string[];
}

export interface EntitlementsResponse {
  tenant: TenantInfo;
  features: string[];
  quotas: TenantQuotaLimits;
  domains: string[];
  user: UserEntitlementContext;
}

export interface FeatureMetadata {
  key: FeatureKeyType;
  name: string;
  description: string;
  requiredPlan: TenantPlan;
  badge: 'STARTER' | 'PRO' | 'ENTERPRISE' | 'GLOBAL';
}

export const FEATURE_METADATA_CATALOG: Record<FeatureKeyType, FeatureMetadata> = {
  [FeatureKey.TranspilerMoodle]: {
    key: FeatureKey.TranspilerMoodle,
    name: 'Moodle XML Export',
    description: 'Xuất câu hỏi chuẩn định dạng Moodle XML tương thích mọi phiên bản Moodle.',
    requiredPlan: 'Starter',
    badge: 'STARTER',
  },
  [FeatureKey.TranspilerGift]: {
    key: FeatureKey.TranspilerGift,
    name: 'Moodle GIFT Export',
    description: 'Xuất câu hỏi chuẩn GIFT text format siêu tốc.',
    requiredPlan: 'Professional',
    badge: 'PRO',
  },
  [FeatureKey.TranspilerQti]: {
    key: FeatureKey.TranspilerQti,
    name: 'IMS Global QTI 3.0 Package',
    description: 'Đóng gói chuẩn khảo thí quốc tế IMS QTI 3.0 (Canvas, Blackboard, Schoology).',
    requiredPlan: 'Professional',
    badge: 'PRO',
  },
  [FeatureKey.TranspilerCanvas]: {
    key: FeatureKey.TranspilerCanvas,
    name: 'Canvas LMS Native Integration',
    description: 'Đẩy trực tiếp đề thi vào Canvas LMS qua REST API Enterprise.',
    requiredPlan: 'Enterprise',
    badge: 'ENTERPRISE',
  },
  [FeatureKey.AdvancedMathWmf]: {
    key: FeatureKey.AdvancedMathWmf,
    name: 'MathType & WMF Vector Engine',
    description: 'Bóc tách công thức toán MTEF và render đồ họa vector sắc nét không vỡ hạt.',
    requiredPlan: 'Starter',
    badge: 'STARTER',
  },
  [FeatureKey.ArenaGameshow]: {
    key: FeatureKey.ArenaGameshow,
    name: 'Đấu Trường Live Gameshow (6 Games)',
    description: 'Tổ chức thi tương tác trực tiếp: Rung chuông vàng, Đấu trường 100, Kahoot live.',
    requiredPlan: 'Professional',
    badge: 'PRO',
  },
  [FeatureKey.HierarchyTree]: {
    key: FeatureKey.HierarchyTree,
    name: 'Cây Phân Cấp Đơn Vị Đa Tầng O(1)',
    description: 'Quản trị phòng ban, chi nhánh, phân hiệu trường học không giới hạn độ sâu.',
    requiredPlan: 'Professional',
    badge: 'PRO',
  },
  [FeatureKey.DynamicDomains]: {
    key: FeatureKey.DynamicDomains,
    name: 'Miền Tri Thức Đa Chuyên Ngành',
    description: 'Tùy biến thuật ngữ, từ điển chuyên ngành Ngân hàng, Y khoa, Pháp luật.',
    requiredPlan: 'Professional',
    badge: 'PRO',
  },
  [FeatureKey.AiSymposium]: {
    key: FeatureKey.AiSymposium,
    name: 'Hội Đồng AI Đa Đại Diện (Symposium)',
    description: 'Tham vấn hội đồng 3 AI đa chuyên gia (Sáng tạo, Phản biện, Thẩm định) cho câu hỏi.',
    requiredPlan: 'Enterprise',
    badge: 'ENTERPRISE',
  },
  [FeatureKey.IrtCat3Pl]: {
    key: FeatureKey.IrtCat3Pl,
    name: 'Thử Nghiệm Thích Ứng IRT 3-PL CAT',
    description: 'Thuật toán khảo thí thích ứng máy tính (Computerized Adaptive Testing) chuẩn quốc tế.',
    requiredPlan: 'Enterprise',
    badge: 'ENTERPRISE',
  },
  [FeatureKey.WhiteLabel]: {
    key: FeatureKey.WhiteLabel,
    name: 'Tùy Biến Thương Hiệu (White-Labeling)',
    description: 'Tên miền riêng (Custom Domain), logo riêng, bảng màu thương hiệu doanh nghiệp.',
    requiredPlan: 'Enterprise',
    badge: 'ENTERPRISE',
  },
  [FeatureKey.WhiteLabelTheming]: {
    key: FeatureKey.WhiteLabelTheming,
    name: 'Bộ Động Hóa Giao Diện & CSS Tokens Enterprise',
    description: 'Tùy biến bảng màu, font chữ, border radius và theme CSS tokens trực tiếp từ DB không cần redeploy.',
    requiredPlan: 'Enterprise',
    badge: 'ENTERPRISE',
  },
  [FeatureKey.SplitScreenStudio]: {
    key: FeatureKey.SplitScreenStudio,
    name: 'Cognitive Split-Screen Ingestion Studio',
    description: 'Khung làm việc bóc tách đề thi phân đôi màn hình, đồng bộ toạ độ gốc 60fps và tương tác song song.',
    requiredPlan: 'Professional',
    badge: 'PRO',
  },
  [FeatureKey.AiAutoRubricSolve]: {
    key: FeatureKey.AiAutoRubricSolve,
    name: 'AI Giải Đề Tự Động & Lập Rubric Điểm',
    description: 'Trợ lý AI tự động phân tích đề bài, sinh lời giải chi tiết và phân rã ma trận rubric barem điểm.',
    requiredPlan: 'Professional',
    badge: 'PRO',
  },
  [FeatureKey.AuditLog]: {
    key: FeatureKey.AuditLog,
    name: 'Nhật Ký Kiểm Toán Bất Biến (Audit Trail)',
    description: 'Ghi vết mọi hành động chấm thi, can thiệp đề thi và phân quyền bảo mật ISO 27001.',
    requiredPlan: 'Enterprise',
    badge: 'ENTERPRISE',
  },
};
