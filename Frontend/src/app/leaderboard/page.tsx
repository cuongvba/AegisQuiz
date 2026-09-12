'use client';
import { useState, useEffect } from 'react';
import Link from 'next/link';
import { Trophy, Medal, Crown, ArrowLeft } from 'lucide-react';

interface LeaderboardUser {
  name: string;
  score: number;
  avatar: string;
  isCurrentUser: boolean;
}

export default function LeaderboardPage() {
  const [list, setList] = useState<LeaderboardUser[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('http://localhost:8080/api/quiz/leaderboard')
      .then(res => res.json())
      .then(data => {
        setList(data);
        setLoading(false);
      })
      .catch(err => {
        console.error(err);
        setLoading(false);
      });
  }, []);

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-950 flex flex-col items-center justify-center text-white">
        <div className="animate-spin text-5xl mb-4">🌀</div>
        <p className="text-gray-400">Đang cập nhật bảng xếp hạng học tập...</p>
      </div>
    );
  }

  // Top 3 for Podium: rank 1 is index 0, rank 2 is index 1, rank 3 is index 2
  const top1 = list[0];
  const top2 = list[1];
  const top3 = list[2];
  const restUsers = list.slice(3);

  return (
    <div className="min-h-screen bg-gray-950 text-white p-8 font-sans select-none">
      <div className="max-w-4xl mx-auto space-y-12">
        {/* Header */}
        <div className="flex justify-between items-center">
          <div className="flex items-center gap-3">
            <Trophy className="text-amber-500 w-8 h-8" />
            <h1 className="text-3xl font-black">Bảng Xếp Hạng Tuần</h1>
          </div>
          <Link href="/" className="text-xs text-gray-400 hover:text-white px-4 py-2 border border-gray-850 rounded-xl flex items-center gap-1">
            <ArrowLeft size={12} /> Quay lại
          </Link>
        </div>

        {/* Podium Layout for Top 3 */}
        {list.length >= 3 && (
          <div className="grid grid-cols-3 items-end gap-4 max-w-lg mx-auto pt-12 pb-6 border-b border-gray-850">
            {/* Rank 2 (Left) */}
            <div className="flex flex-col items-center space-y-3">
              <div className="relative">
                <div className="w-16 h-16 bg-gray-800 rounded-full flex items-center justify-center border-2 border-gray-400 font-extrabold text-lg shadow-lg">
                  {top2.avatar}
                </div>
                <div className="absolute -top-3 -right-2 bg-gray-400 text-gray-950 rounded-full w-6 h-6 flex items-center justify-center text-xs font-black">
                  2
                </div>
              </div>
              <div className="text-center">
                <p className="text-sm font-bold text-gray-300 truncate max-w-[90px]">{top2.name}</p>
                <p className="text-xs text-gray-500 font-bold">{top2.score} XP</p>
              </div>
              <div className="w-full bg-gray-850 h-24 rounded-t-2xl flex items-center justify-center text-gray-400 font-black border-t border-gray-700">
                🥈 Bạc
              </div>
            </div>

            {/* Rank 1 (Middle) */}
            <div className="flex flex-col items-center space-y-3">
              <Crown className="text-amber-500 animate-bounce" size={28} />
              <div className="relative">
                <div className="w-20 h-20 bg-gray-800 rounded-full flex items-center justify-center border-4 border-amber-500 font-extrabold text-xl shadow-2xl">
                  {top1.avatar}
                </div>
                <div className="absolute -top-3 -right-2 bg-amber-500 text-gray-950 rounded-full w-6 h-6 flex items-center justify-center text-xs font-black shadow-lg shadow-amber-500/20">
                  1
                </div>
              </div>
              <div className="text-center">
                <p className="text-base font-extrabold text-amber-400 truncate max-w-[110px]">{top1.name}</p>
                <p className="text-xs text-amber-500 font-black">{top1.score} XP</p>
              </div>
              <div className="w-full bg-amber-500/10 border-t border-amber-500/30 h-32 rounded-t-2xl flex items-center justify-center text-amber-400 font-black">
                🥇 Vàng
              </div>
            </div>

            {/* Rank 3 (Right) */}
            <div className="flex flex-col items-center space-y-3">
              <div className="relative">
                <div className="w-14 h-14 bg-gray-800 rounded-full flex items-center justify-center border-2 border-amber-700 font-extrabold text-md shadow-lg">
                  {top3.avatar}
                </div>
                <div className="absolute -top-3 -right-2 bg-amber-700 text-white rounded-full w-6 h-6 flex items-center justify-center text-xs font-black">
                  3
                </div>
              </div>
              <div className="text-center">
                <p className="text-sm font-bold text-gray-300 truncate max-w-[90px]">{top3.name}</p>
                <p className="text-xs text-gray-500 font-bold">{top3.score} XP</p>
              </div>
              <div className="w-full bg-gray-850 h-20 rounded-t-2xl flex items-center justify-center text-amber-700 font-black border-t border-gray-700">
                🥉 Đồng
              </div>
            </div>
          </div>
        )}

        {/* Rest of the ranks list */}
        <div className="space-y-3 max-w-2xl mx-auto">
          {restUsers.map((user, idx) => {
            const rank = idx + 4;
            return (
              <div 
                key={rank}
                className={`flex items-center justify-between p-4 rounded-2xl border transition-all ${
                  user.isCurrentUser 
                    ? 'bg-purple-950/20 border-purple-500/40 shadow-inner' 
                    : 'bg-gray-900 border-gray-850 hover:border-gray-800'
                }`}
              >
                <div className="flex items-center gap-4">
                  <span className="font-mono text-gray-500 font-bold w-6">{rank}</span>
                  <div className="w-10 h-10 bg-gray-850 rounded-full border border-gray-800 flex items-center justify-center font-bold">
                    {user.avatar}
                  </div>
                  <div>
                    <span className="font-bold text-sm block">
                      {user.name} {user.isCurrentUser && <span className="text-[10px] bg-purple-600/30 text-purple-400 font-bold px-2 py-0.5 rounded-full ml-2">Bạn</span>}
                    </span>
                    <span className="text-[10px] text-gray-500">Học viên xuất sắc</span>
                  </div>
                </div>
                <div className="font-bold text-sm text-gray-300">
                  {user.score} XP
                </div>
              </div>
            );
          })}
        </div>

      </div>
    </div>
  );
}
