'use client';
import { useState, useEffect } from 'react';
import Link from 'next/link';
import { Award, Lock, Unlock, Zap, Shield, Flame, Sparkles } from 'lucide-react';

interface Achievement {
  id: string;
  title: string;
  description: string;
  isUnlocked: boolean;
  unlockDate?: string;
  color: string;
  bg: string;
}

export default function AchievementsPage() {
  const [achievements, setAchievements] = useState<Achievement[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('http://localhost:8080/api/quiz/achievements')
      .then(res => res.json())
      .then(data => {
        setAchievements(data);
        setLoading(false);
      })
      .catch(err => {
        console.error(err);
        setLoading(false);
      });
  }, []);

  const getIcon = (id: string) => {
    switch (id) {
      case '1': return Sparkles;
      case '2': return Zap;
      case '3': return Shield;
      case '4': return Award;
      default: return Flame;
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-950 flex flex-col items-center justify-center text-white">
        <div className="animate-spin text-5xl mb-4">🌀</div>
        <p className="text-gray-400">Đang tải tủ thành tích...</p>
      </div>
    );
  }

  const unlockedCount = achievements.filter(a => a.isUnlocked).length;

  return (
    <div className="min-h-screen bg-gray-950 text-white p-8 select-none font-sans">
      <div className="max-w-6xl mx-auto space-y-12">
        {/* Banner */}
        <div className="flex flex-col items-center justify-center p-8 bg-gradient-to-tr from-purple-950 to-indigo-900 text-white rounded-3xl shadow-xl relative overflow-hidden border border-purple-900/20">
          <div className="absolute -top-10 -right-10 opacity-5">
            <Award size={200} />
          </div>
          <div className="relative z-10 text-center space-y-2">
            <h1 className="text-4xl font-black flex items-center justify-center gap-3">
              🏆 Tủ Vinh Danh (Trophy Room)
            </h1>
            <p className="text-indigo-200 font-medium text-sm max-w-xl mx-auto">
              Học tập là một chặng đường vinh quang. Lưu trữ những cột mốc đáng tự hào của bạn tại AegisQuiz.
            </p>
          </div>
        </div>

        {/* Info Header */}
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold text-gray-200">Bộ Sưu Tập Huy Hiệu</h2>
          <div className="flex items-center gap-4">
            <span className="font-semibold text-xs text-gray-400 bg-gray-900 border border-gray-850 px-4 py-2 rounded-full">
              Đã mở: {unlockedCount} / {achievements.length} Huy hiệu
            </span>
            <Link href="/" className="text-xs text-gray-400 hover:text-white px-4 py-2 border border-gray-850 rounded-xl">
              Trở lại
            </Link>
          </div>
        </div>

        {/* Badges Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
          {achievements.map(achievement => {
            const IconComponent = getIcon(achievement.id);
            return (
              <div 
                key={achievement.id} 
                className={`relative p-6 rounded-3xl border-2 transition-all duration-300 flex flex-col items-center text-center ${
                  achievement.isUnlocked 
                    ? 'bg-gray-900 border-transparent shadow-lg shadow-indigo-500/5 hover:-translate-y-1 hover:shadow-indigo-500/10' 
                    : 'bg-gray-900/40 border-gray-850 grayscale opacity-45 hover:opacity-60'
                }`}
              >
                {achievement.isUnlocked && (
                  <span className="absolute top-4 right-4 bg-green-500/10 text-green-400 text-[10px] font-bold px-2 py-1 rounded-md">
                    Unlocked
                  </span>
                )}
                
                <div className={`w-20 h-20 flex items-center justify-center rounded-full mb-4 ${
                  achievement.isUnlocked ? achievement.bg : 'bg-gray-950 border border-gray-850'
                }`}>
                  <IconComponent size={32} className={achievement.isUnlocked ? achievement.color : 'text-gray-600'} />
                </div>
                
                <h3 className="text-lg font-bold text-gray-200 mb-2">{achievement.title}</h3>
                <p className="text-gray-400 text-xs font-medium flex-1">{achievement.description}</p>
                
                {achievement.isUnlocked ? (
                  <div className="mt-4 pt-4 border-t border-gray-850 w-full text-[10px] font-bold text-purple-400 flex items-center justify-center gap-1">
                    <Unlock size={12} /> Đạt được vào {achievement.unlockDate}
                  </div>
                ) : (
                  <div className="mt-4 pt-4 border-t border-gray-850/50 w-full text-[10px] font-bold text-gray-600 flex items-center justify-center gap-1">
                    <Lock size={12} /> Chưa mở khóa
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
