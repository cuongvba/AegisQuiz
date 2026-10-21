export interface TaxonomyAlignment {
  domainCode: string;
  domainName: string;
  targetLevel: string;
  categoryCode: string;
  categoryName: string;
  benchmarkStandard: string;
  cognitiveLevel: string;
  confidenceScore: number;
}

export interface ZenQuestionPreview {
  order: number;
  content: string;
  canonicalHash: string;
  isDuplicate: boolean;
  questionType?: string;
  options: string[];
  correctAnswer?: string;
  explanation?: string;
  /** true khi Creator đã chỉnh sửa trực tiếp trên Studio */
  edited?: boolean;
}

export type ZenQuestionPatch = Partial<Pick<ZenQuestionPreview, 'content' | 'options' | 'correctAnswer' | 'explanation'>>;

export interface ZenUploadResult {
  success: boolean;
  examTitle: string;
  totalQuestions: number;
  duplicateCount: number;
  uniqueCount: number;
  deduplicationRatePercentage: number;
  suggestedTaxonomy?: TaxonomyAlignment;
  questions: ZenQuestionPreview[];
}

export interface EducatorPartnershipInfo {
  id: string;
  userId: string;
  referralCode: string;
  trialExpiresAt: string;
  daysRemaining: number;
  isActive: boolean;
  revenueShareRate: number;
  karmaPoints: number;
  payoutBankAccount?: string;
}

export interface ZenRoomLaunchResult {
  pinCode: string;
  roomId: string;
  title: string;
  joinUrl: string;
  qrCodeData: string;
  launchedAt: string;
}
