import { useQuery } from '@tanstack/react-query';
import { Trophy, Medal, Flame, TrendingUp, RefreshCw } from 'lucide-react';
import { gamificationService } from '@/services/gamification.service';

interface LeaderboardEntry {
  rank?: number;
  userId?: string;
  name?: string;
  displayName?: string;
  score?: number;
  totalScore?: number;
  averageScore?: number;
  totalAttempts?: number;
  xp?: number;
  xpTotal?: number;
  isCurrentUser?: boolean;
  avatarInitial?: string;
  avatar?: { url?: string };
}

function getDisplayName(entry: LeaderboardEntry, idx: number): string {
  return entry.displayName || entry.name || `Học viên ${idx + 1}`;
}

function getScore(entry: LeaderboardEntry): number {
  return Math.round(entry.averageScore ?? entry.totalScore ?? entry.score ?? 0);
}

function getXP(entry: LeaderboardEntry): number {
  return entry.xpTotal ?? entry.xp ?? 0;
}

export function LeaderboardPage() {
  const { data: rawData, isLoading, refetch } = useQuery({
    queryKey: ['leaderboard', 'weekly'],
    queryFn: () => gamificationService.getLeaderboard('weekly', 20),
    staleTime: 60_000,
  });

  // Xử lý response từ AegisQuiz API: { period, source, data: [...] } hoặc mảng trực tiếp
  const entries: LeaderboardEntry[] = Array.isArray(rawData)
    ? rawData
    : Array.isArray(rawData?.data)
    ? rawData.data
    : FALLBACK_LEADERBOARD;

  const top3 = entries.slice(0, 3);
  const rest = entries.slice(3);

  return (
    <div className="max-w-4xl mx-auto px-4 py-8 pb-24">
      <div className="text-center mb-10">
        <div className="inline-flex items-center justify-center p-4 bg-amber-100 text-amber-500 rounded-[2rem] shadow-sm mb-4">
          <Trophy size={48} className="drop-shadow-sm" />
        </div>
        <h1 className="text-4xl font-black text-slate-900">Bảng Vinh Danh</h1>
        <p className="text-slate-500 font-medium mt-2 max-w-md mx-auto">
          Top những học viên xuất sắc nhất. Hãy luyện tập để ghi tên mình!
        </p>
        <button
          onClick={() => refetch()}
          disabled={isLoading}
          className="mt-4 inline-flex items-center gap-2 px-4 py-2 rounded-full border border-slate-200 text-sm font-semibold text-slate-600 hover:bg-slate-50 transition-colors"
        >
          <RefreshCw size={14} className={isLoading ? 'animate-spin' : ''} />
          {isLoading ? 'Đang tải...' : 'Làm mới'}
        </button>
      </div>

      {/* Podium top 3 */}
      <div className="flex items-end justify-center gap-2 md:gap-6 mb-12 h-64">
        {/* #2 */}
        <div className="flex flex-col items-center justify-end w-28 md:w-32">
          <Avatar entry={top3[1]} size={16} borderColor="border-slate-300" />
          <div className="bg-slate-200 text-slate-700 w-full h-32 rounded-t-xl flex flex-col items-center justify-start pt-8 pb-4 shadow-inner">
            <Medal size={24} className="text-slate-500 mb-1" />
            <span className="font-black text-xl">2</span>
            <span className="text-xs font-bold truncate w-full px-2 text-center mt-1">{getDisplayName(top3[1] ?? {}, 1)}</span>
            <span className="text-sm font-black mt-auto">{getScore(top3[1] ?? {})}%</span>
          </div>
        </div>

        {/* #1 */}
        <div className="flex flex-col items-center justify-end w-32 md:w-40 z-10">
          <div className="relative z-10 -mb-8">
            <div className="absolute -top-6 left-1/2 -translate-x-1/2">
              <Trophy size={32} className="text-amber-400 drop-shadow-md animate-bounce" fill="currentColor" />
            </div>
            <Avatar entry={top3[0]} size={20} borderColor="border-amber-400 shadow-amber-200" />
          </div>
          <div className="bg-gradient-to-t from-amber-200 to-amber-100 text-amber-900 w-full h-40 rounded-t-xl flex flex-col items-center justify-start pt-10 pb-4 shadow-inner">
            <span className="font-black text-2xl drop-shadow-sm">1</span>
            <span className="text-sm font-bold truncate w-full px-2 text-center mt-1 text-amber-950">{getDisplayName(top3[0] ?? {}, 0)}</span>
            <span className="text-base font-black mt-auto flex items-center gap-1 bg-amber-500/20 px-3 py-1 rounded-full">
              <Flame size={14} className="text-orange-500" /> {getScore(top3[0] ?? {})}%
            </span>
          </div>
        </div>

        {/* #3 */}
        <div className="flex flex-col items-center justify-end w-28 md:w-32">
          <Avatar entry={top3[2]} size={16} borderColor="border-orange-300" />
          <div className="bg-orange-100 text-orange-900 w-full h-24 rounded-t-xl flex flex-col items-center justify-start pt-8 pb-4 shadow-inner">
            <Medal size={24} className="text-orange-400 mb-1" />
            <span className="font-black text-lg">3</span>
            <span className="text-xs font-bold truncate w-full px-2 text-center mt-1">{getDisplayName(top3[2] ?? {}, 2)}</span>
            <span className="text-sm font-black mt-auto">{getScore(top3[2] ?? {})}%</span>
          </div>
        </div>
      </div>

      {/* List #4+ */}
      {rest.length > 0 && (
        <div className="bg-white rounded-3xl shadow-sm border border-slate-200 overflow-hidden">
          {rest.map((player, idx) => (
            <div
              key={player.userId ?? idx}
              className={`flex items-center gap-4 p-4 border-b border-slate-100 last:border-0 transition-colors ${
                player.isCurrentUser ? 'bg-indigo-50' : 'hover:bg-slate-50'
              }`}
            >
              <div className="w-8 font-black text-slate-400 text-center">{player.rank ?? idx + 4}</div>
              <div className="w-12 h-12 rounded-full bg-gradient-to-br from-indigo-400 to-purple-500 flex items-center justify-center text-white font-black text-lg shadow">
                {(getDisplayName(player, idx + 3)[0] ?? '?').toUpperCase()}
              </div>
              <div className="flex-1 font-bold text-slate-800">
                {getDisplayName(player, idx + 3)}
                {player.isCurrentUser && <span className="ml-2 text-xs text-indigo-500">(Bạn)</span>}
              </div>
              <div className="font-black text-indigo-600 tracking-tight flex items-center gap-1 bg-indigo-50 px-3 py-1.5 rounded-lg">
                <TrendingUp size={16} />
                {getScore(player)}%
              </div>
              {getXP(player) > 0 && (
                <div className="hidden sm:flex items-center gap-1 text-xs font-bold text-amber-600 bg-amber-50 px-2 py-1 rounded-lg">
                  ⚡{getXP(player)} XP
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function Avatar({ entry, size, borderColor }: { entry?: LeaderboardEntry; size: number; borderColor: string }) {
  const sizeClass = `w-${size} h-${size}`;
  const initial = ((entry?.displayName || entry?.name || '?')[0] ?? '?').toUpperCase();
  return (
    <div className={`${sizeClass} rounded-full border-4 ${borderColor} bg-gradient-to-br from-indigo-400 to-purple-500 text-white flex items-center justify-center font-black text-2xl shadow z-10 -mb-6`}>
      {initial}
    </div>
  );
}

const FALLBACK_LEADERBOARD: LeaderboardEntry[] = [
  { rank: 1, displayName: 'Trần Minh Hoàng', score: 98, xp: 1240 },
  { rank: 2, displayName: 'Lê Thị Thanh',   score: 87, xp: 980  },
  { rank: 3, displayName: 'Nguyễn Văn A',   score: 84, xp: 840, isCurrentUser: true },
  { rank: 4, displayName: 'Phạm Lan Phương', score: 79, xp: 750 },
  { rank: 5, displayName: 'Vũ Minh Đức',    score: 65, xp: 620 },
];
