export type LayoutShellType =
  | 'PODIUM'
  | 'MEGA_GRID'
  | 'STEP_LADDER'
  | 'RADIAL_WHEEL'
  | 'TOPIC_MATRIX'
  | 'HIDDEN_TILES';

export type ContentionMode =
  | 'BUZZER_FASTEST'
  | 'SIMULTANEOUS_ALL'
  | 'TURN_ROUND_ROBIN'
  | 'SELECTIVE_PICK';

export interface ScoringRuleConfig {
  basePointsPerCorrect: number;
  penaltyPerWrong?: number;
  speedBonus?: boolean;
  streakMultiplier?: boolean;
  dropToZeroOnWrong?: boolean;
  ladderPoints?: number[];
}

export interface SurvivalRuleConfig {
  type: 'ACCUMULATIVE' | 'SUDDEN_DEATH' | 'STRIKE_LIMIT';
  maxStrikes?: number;
  safeMilestones?: number[];
}

export interface LifelineConfig {
  code: string;
  name: string;
  icon?: string;
  usageLimit?: number;
  description?: string;
}

export interface RoundDefinition {
  roundIndex: number;
  roundName: string;
  durationSeconds: number;
  questionCategory?: string;
  pointsMultiplier?: number;
}

export interface GameshowManifest {
  gameCode: string;
  displayName: string;
  description: string;
  icon: string;
  minPlayers: number;
  maxPlayers: number;
  layoutShell: LayoutShellType;
  contentionMode: ContentionMode;
  scoringRule: ScoringRuleConfig;
  survivalRule: SurvivalRuleConfig;
  enabledLifelines: LifelineConfig[];
  rounds: RoundDefinition[];
  themeColor?: string;
  isBuiltIn?: boolean;
  createdBy?: string;
  createdAtUtc?: string;
}

export type ArenaGameCode =
  | 'OLYMPIA'
  | 'GOLDEN_BELL'
  | 'LUCKY_WHEEL'
  | 'UNIVERSITY_CHALLENGE'
  | 'JEOPARDY'
  | 'LIGHTNING'
  | string;


export interface ArenaPlayer {
  id: string;
  name: string;
  avatar: string;
  score: number;
  isConnected?: boolean;
  seatNumber?: number;
  status?: 'ACTIVE' | 'ELIMINATED' | 'BUZZED' | 'SPECTATOR' | 'SKIPPED' | string;
  teamName?: string;
  streak?: number;
  stepPosition?: number;
  starOfHopeUsed?: boolean;
  [key: string]: any;
}

export interface ArenaQuestionItem {
  id: string;
  content: string;
  questionType: string;
  options: string[];
  answerRaw: string;
  explanation: string;
  points: number;
  mediaUrl?: string;
  timeLimitSeconds: number;
  category: string;
  hint?: string;
  [key: string]: any;
}

export interface ArenaRoomState {
  roomId: string;
  gameCode: ArenaGameCode;
  roomName: string;
  hostUserId: string;
  currentStage: string;
  currentQuestionIndex: number;
  questions: ArenaQuestionItem[];
  players: Record<string, ArenaPlayer>;
  buzzerWinnerPlayerId?: string | null;
  buzzerTimestampMs?: number;
  stageStartTimeUtc?: string;
  stageDurationSeconds?: number;
  customData?: Record<string, any>;
  CurrentStage?: string;
  CurrentQuestionIndex?: number;
  Questions?: ArenaQuestionItem[];
  Players?: Record<string, ArenaPlayer>;
  BuzzerWinnerPlayerId?: string | null;
  [key: string]: any;
}

export interface ArenaGamePluginMeta {
  gameCode: ArenaGameCode;
  displayName: string;
  description: string;
  icon: string;
  badge: string;
  minPlayers: number;
  maxPlayers: number;
  gradient: string;
  accentColor: string;
}

export const ARENA_GAMES_METADATA: ArenaGamePluginMeta[] = [
  {
    gameCode: 'OLYMPIA',
    displayName: 'Đường Lên Đỉnh Olympia (Olympia Supreme)',
    description: 'Format 4 vòng thi huyền thoại: Khởi Động 60s, Vượt CNV ma trận, Tăng Tốc mili-giây, Về Đích & Ngôi Sao Hy Vọng cướp điểm!',
    icon: '🏛️',
    badge: 'Kinh Điển VTV',
    minPlayers: 1,
    maxPlayers: 4,
    gradient: 'from-amber-500 via-orange-600 to-rose-600',
    accentColor: 'border-amber-400 text-amber-600 bg-amber-50',
  },
  {
    gameCode: 'GOLDEN_BELL',
    displayName: 'Rung Chuông Vàng (Golden Bell Mega-Grid)',
    description: 'Đấu trường Battle Royale 100 - 10,000 thí sinh trên sàn đấu số. Sai 1 câu là rời sàn! Minigame Cứu Trợ của Thầy Cô hồi sinh cả trường.',
    icon: '🔔',
    badge: 'Battle Royale 100+',
    minPlayers: 1,
    maxPlayers: 10000,
    gradient: 'from-yellow-500 via-amber-500 to-emerald-600',
    accentColor: 'border-yellow-400 text-yellow-700 bg-yellow-50',
  },
  {
    gameCode: 'LUCKY_WHEEL',
    displayName: 'Chiếc Nón Kỳ Diệu (Quantum Lucky Wheel)',
    description: 'Vòng quay vật lý Canvas 60fps chân thực, bảng lật mở ô chữ realtime, ô Nhân Đôi, Mất Lượt, Phần Thưởng Bí Ẩn!',
    icon: '🎡',
    badge: 'Giải Ô Chữ TV',
    minPlayers: 1,
    maxPlayers: 4,
    gradient: 'from-cyan-500 via-teal-600 to-indigo-600',
    accentColor: 'border-teal-400 text-teal-700 bg-teal-50',
  },
  {
    gameCode: 'UNIVERSITY_CHALLENGE',
    displayName: 'University Challenge (Đấu Trường Đại Học)',
    description: 'Đối kháng học thuật đỉnh cao giữa các trường đại học (4 vs 4). Starter Buzzer 10đ và chuỗi câu hỏi Bonus 15đ với kênh thảo luận kín.',
    icon: '🎓',
    badge: 'Liên Trường 4v4',
    minPlayers: 2,
    maxPlayers: 8,
    gradient: 'from-blue-600 via-indigo-600 to-violet-700',
    accentColor: 'border-blue-400 text-blue-700 bg-blue-50',
  },
  {
    gameCode: 'JEOPARDY',
    displayName: 'Jeopardy! American Matrix',
    description: 'Bảng ma trận 30 ô chủ đề rực rỡ, format câu hỏi ngược ("Ai là...? / Là gì...?"), Daily Double và Final Jeopardy!',
    icon: '🇺🇸',
    badge: 'Ma Trận Điểm Số',
    minPlayers: 1,
    maxPlayers: 3,
    gradient: 'from-blue-700 via-indigo-800 to-sky-900',
    accentColor: 'border-sky-400 text-sky-700 bg-sky-50',
  },
  {
    gameCode: 'LIGHTNING',
    displayName: 'Nhanh Như Chớp (Lightning Incline)',
    description: 'Cỗ máy leo dốc 10 bậc đứng! Đúng leo 1 bậc, sai tụt ngay về vạch số 0! 2 phút chinh phục đỉnh cao 10 câu liên tiếp.',
    icon: '⚡',
    badge: 'Thang Dốc 10 Bậc',
    minPlayers: 1,
    maxPlayers: 4,
    gradient: 'from-rose-500 via-pink-600 to-purple-600',
    accentColor: 'border-rose-400 text-rose-700 bg-rose-50',
  },
];
