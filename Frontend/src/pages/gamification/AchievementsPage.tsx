import { useQuery } from '@tanstack/react-query';
import { Award, Lock, Unlock, Zap, Shield, Sparkles, Flame, RefreshCw } from 'lucide-react';
import { gamificationService } from '@/services/gamification.service';

const ICON_MAP: Record<string, React.ComponentType<any>> = {
  '🌟': Sparkles, star: Sparkles,
  '🔥': Flame, flame: Flame, streak: Flame,
  '⚔️': Shield, shield: Shield, warrior: Shield,
  '🏆': Award, master: Award,
  default: Award,
};

function getIcon(code: string, emoji?: string) {
  if (emoji) {
    const match = ICON_MAP[emoji.trim()];
    if (match) return match;
  }
  const lower = (code ?? '').toLowerCase();
  for (const [key, Comp] of Object.entries(ICON_MAP)) {
    if (lower.includes(key)) return Comp;
  }
  return ICON_MAP.default;
}

interface Achievement {
  id?: string;
  code?: string;
  title?: string;
  name?: string;
  description?: string;
  iconEmoji?: string;
  badgeColor?: string;
  xpReward?: number;
  isUnlocked?: boolean;
  earnedAt?: string;
}

export function AchievementsPage() {
  const userRaw = localStorage.getItem('user');
  const userId = userRaw ? (JSON.parse(userRaw) as { id?: string })?.id : undefined;

  const { data: rawAchievements, isLoading, refetch } = useQuery({
    queryKey: ['achievements', userId],
    queryFn: () => gamificationService.getAllAchievements(userId),
    staleTime: 60_000,
  });

  const achievements: Achievement[] = Array.isArray(rawAchievements)
    ? rawAchievements
    : FALLBACK_ACHIEVEMENTS;

  const unlockedCount = achievements.filter((a) => a.isUnlocked).length;

  return (
    <div className="max-w-6xl mx-auto px-4 py-8 pb-20">
      {/* Hero Banner */}
      <div className="flex flex-col items-center justify-center p-8 bg-gradient-to-tr from-indigo-900 to-purple-800 text-white rounded-3xl shadow-xl mb-12 relative overflow-hidden">
        <div className="absolute -top-10 -right-10 opacity-10">
          <Award size={200} />
        </div>
        <div className="relative z-10 text-center">
          <h1 className="text-4xl font-black mb-2 flex items-center justify-center gap-3">
            <Award size={40} className="text-amber-400" /> Tủ Vinh Danh
          </h1>
          <p className="text-indigo-200 font-medium text-lg max-w-xl mx-auto">
            Hành trình học tập của bạn không chỉ là kiến thức, mà còn là những dấu ấn đáng tự hào.
          </p>
        </div>
      </div>

      {/* Stats header */}
      <div className="mb-6 flex items-center justify-between">
        <h2 className="text-2xl font-bold text-slate-800">Bộ Sưu Tập Huy Hiệu</h2>
        <div className="flex items-center gap-3">
          <span className="font-semibold text-slate-500 bg-slate-100 px-4 py-1.5 rounded-full">
            Đã đạt: {unlockedCount} / {achievements.length}
          </span>
          <button
            onClick={() => refetch()}
            disabled={isLoading}
            className="p-2 rounded-full border border-slate-200 hover:bg-slate-50 transition-colors"
          >
            <RefreshCw size={16} className={isLoading ? 'animate-spin text-indigo-500' : 'text-slate-400'} />
          </button>
        </div>
      </div>

      {/* Achievement Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
        {achievements.map((achievement, idx) => {
          const code = achievement.code ?? achievement.id ?? String(idx);
          const title = achievement.title ?? achievement.name ?? code;
          const IconComp = getIcon(code, achievement.iconEmoji);
          const color = achievement.badgeColor ?? '#4f46e5';
          const xp = achievement.xpReward ?? 0;

          return (
            <div
              key={code}
              className={`relative p-6 rounded-2xl border-2 transition-all duration-300 flex flex-col items-center text-center ${
                achievement.isUnlocked
                  ? 'bg-white border-transparent shadow-lg hover:-translate-y-1 hover:shadow-xl'
                  : 'bg-slate-50 border-slate-200 grayscale opacity-70 hover:opacity-90'
              }`}
            >
              {achievement.isUnlocked && (
                <span className="absolute top-4 right-4 bg-emerald-100 text-emerald-700 text-[10px] font-bold px-2 py-1 rounded-md">
                  Đã mở
                </span>
              )}
              {xp > 0 && (
                <span className="absolute top-4 left-4 bg-amber-100 text-amber-700 text-[10px] font-bold px-2 py-1 rounded-md flex items-center gap-1">
                  ⚡{xp} XP
                </span>
              )}

              <div
                className="w-24 h-24 flex items-center justify-center rounded-full mb-4"
                style={{ backgroundColor: achievement.isUnlocked ? `${color}20` : undefined }}
              >
                {achievement.iconEmoji ? (
                  <span className="text-5xl">{achievement.iconEmoji}</span>
                ) : (
                  <IconComp
                    size={40}
                    style={{ color: achievement.isUnlocked ? color : undefined }}
                    className={achievement.isUnlocked ? '' : 'text-slate-400'}
                  />
                )}
              </div>

              <h3 className="text-xl font-bold text-slate-800 mb-2">{title}</h3>
              <p className="text-slate-500 text-sm font-medium flex-1">{achievement.description}</p>

              {achievement.isUnlocked ? (
                <div className="mt-4 pt-4 border-t border-slate-100 w-full text-xs font-bold text-indigo-500 flex items-center justify-center gap-1">
                  <Unlock size={14} />
                  {achievement.earnedAt ? `Mở khóa ${new Date(achievement.earnedAt).toLocaleDateString('vi-VN')}` : 'Đã mở khóa'}
                </div>
              ) : (
                <div className="mt-4 pt-4 border-t border-slate-200 w-full text-xs font-bold text-slate-400 flex items-center justify-center gap-1">
                  <Lock size={14} /> Chưa mở khóa
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

const FALLBACK_ACHIEVEMENTS: Achievement[] = [
  { code: 'first_attempt', title: 'Khởi đầu rực rỡ',     description: 'Hoàn thành bài thi đầu tiên với điểm > 80', iconEmoji: '🌟', badgeColor: '#F59E0B', xpReward: 50,  isUnlocked: false },
  { code: 'night_owl',     title: 'Cú đêm chăm chỉ',     description: 'Học hoặc thi vào khoảng 0:00 – 4:00 sáng',  iconEmoji: '🦉', badgeColor: '#8B5CF6', xpReward: 30,  isUnlocked: false },
  { code: 'streak_10',     title: 'Chiến binh bất bại',   description: 'Đạt chuỗi 10 câu trả lời đúng liên tiếp',   iconEmoji: '⚔️', badgeColor: '#10B981', xpReward: 100, isUnlocked: false },
  { code: 'it_master',     title: 'Bậc thầy IT Enterprise', description: 'Trả lời đúng 100% các câu hỏi về IT',     iconEmoji: '🖥️', badgeColor: '#0EA5E9', xpReward: 200, isUnlocked: false },
];
