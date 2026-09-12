export type QuestionType =
  | 'SINGLE'
  | 'MULTI'
  | 'TRUE_FALSE'
  | 'SHORT_ANSWER'
  | 'FILL_BLANK'
  | 'ORDERING'
  | 'MATCHING'
  | 'ESSAY';

export interface LearnerQuestion {
  id: string;
  questNo: number;
  content: string;
  questionType: QuestionType;
  questionMediaUrl?: string;
  questionMediaType?: string;
  optionMediaType?: string;
  contentType?: string;
  optionType?: string;
  options: string[];
  matchingLeftOptions?: string[];
  matchingRightOptions?: string[];
  answerRaw: string;
  citation: string;
  topicCode?: string;
  categoryCode?: string;
  topicName?: string;
  points: number;
  difficulty: number;
  durationSec: number;
  enabled: boolean;
  aiExplanation?: string;
  isCritical?: boolean;
  subCategory?: string;
  contextId?: string;
  contextTitle?: string;
  contextContent?: string;
  domainCode?: string;
  tags?: string[];
  targetLevel?: string;
  assessmentPurpose?: string;
  issuingOrg?: string;
  benchmarkYear?: number;
  benchmarkStandard?: string;
  // Ownership & Contribution metadata (Task: TeamLeader Contribution & Ownership)
  creatorId?: string;
  creatorName?: string;
  orgUnitId?: string;
  orgUnitName?: string;
  scope?: 'COMMUNITY' | 'TENANT' | 'TEAM' | 'PERSONAL';
  visibilityScope?: 'PUBLIC' | 'ORG_UNIT' | 'TEAM' | 'PRIVATE';
  contributionStatus?: 'DRAFT' | 'TEAM_PUBLISHED' | 'SUBMITTED_FOR_TENANT' | 'APPROVED_TENANT';
}

export interface TopicSummary {
  key: string;
  code: string;
  name: string;
  questionCount: number;
}

export interface CognitiveTopicNodeDto {
  id: string;
  name: string;
  code: string;
  description?: string;
  scope: string;
  domainCode: string;
  materializedPath?: string;
  depthLevel: number;
  parentId?: string;
  questionCount: number;
  children: CognitiveTopicNodeDto[];
}

export interface CognitiveDomainGroupDto {
  domainCode: string;
  domainName: string;
  icon?: string;
  colorBadge?: string;
  totalTopics?: number;
  totalQuestions?: number;
  totalQuestionCount?: number;
  rootNodes?: CognitiveTopicNodeDto[];
  rootTopics?: CognitiveTopicNodeDto[];
}

export type ExamCategoryPreset = 'general' | 'gplx_a1' | 'gplx_b1' | 'gplx_b2' | 'gplx_c' | 'gplx_fatal_only';

export interface PracticeTopicConfig {
  topicKey: string;
  numberOfQuestions: number;
  isRandom: boolean;
  startIndex: number;
  onlyCritical?: boolean;
}

export type PracticeMode = 'study' | 'exam' | 'kids' | 'paper';
export type LearnerLanguage = 'vi' | 'en';

export interface PracticeConfig {
  mode: PracticeMode;
  topicConfigs: PracticeTopicConfig[];
  shuffleOptions: boolean;
  shuffleQuestions: boolean;
  timeLimitSec: number;
  autoNextOnTimeout: boolean;
  onlyCritical?: boolean;
  preset?: ExamCategoryPreset;
}

export interface DisplayOption {
  id: string;
  label: string;
  value: string;
  isMedia: boolean;
  mediaType: string;
}

export interface MatchingPair {
  left: string;
  right: string;
}

export interface LearnerAnswer {
  selectedOption?: string;
  selectedOptions?: string[];
  shortText?: string;
  ordering?: string[];
  matching?: Record<string, string>;
  essay?: string;
}

export interface QuizResult {
  totalQuestions: number;
  answeredQuestions: number;
  scoredQuestions: number;
  totalPoints: number;
  earnedPoints: number;
  percentage: number;
  topicResults: TopicResult[];
  hasFatalMistake?: boolean;
  fatalQuestionsFailed?: LearnerQuestion[];
}

export interface TopicResult {
  topicKey: string;
  topicLabel: string;
  totalQuestions: number;
  answeredQuestions: number;
  scoredQuestions: number;
  earnedPoints: number;
  totalPoints: number;
  percentage: number;
}
