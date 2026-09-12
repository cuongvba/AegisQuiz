import React, { useState } from 'react';
import { Award, Users, Volume2, Clock, AlertTriangle, MessageSquare } from 'lucide-react';
import type { ArenaRoomState, ArenaPlayer, ArenaQuestionItem } from '@/types/arena';
import { playArenaSfx } from '@/services/arena.service';

interface Props {
  state: ArenaRoomState;
  onBuzzer: () => void;
  onSubmitAnswer: (ans: string) => void;
  onAction: (actionType: string, payload: string) => void;
  onNextStage: (next: string) => void;
}

export function UniversityChallengeView({ state, onBuzzer, onSubmitAnswer, onAction, onNextStage }: Props) {
  const [ans, setAns] = useState('');
  const questions: ArenaQuestionItem[] = state.questions || state.Questions || [];
  const qIndex = state.currentQuestionIndex ?? state.CurrentQuestionIndex ?? 0;
  const currentQ: ArenaQuestionItem = questions[qIndex] || questions[0] || {
    id: 'q0',
    content: 'Đang tải câu hỏi...',
    questionType: 'DIRECT',
    options: [],
    answerRaw: '',
    explanation: '',
    points: 10,
    timeLimitSeconds: 15,
    category: 'HỌC THUẬT QUỐC TẾ'
  };

  const rawPlayers = state.players || state.Players || {};
  const players: ArenaPlayer[] = Object.values(rawPlayers);

  const teamA = players.filter((p: any) => (p.teamName || p.TeamName) === 'Team A' || (p.seatNumber || p.SeatNumber || 1) % 2 === 1);
  const teamB = players.filter((p: any) => (p.teamName || p.TeamName) === 'Team B' || (p.seatNumber || p.SeatNumber || 2) % 2 === 0);

  const teamAScore = teamA.reduce((sum, p: any) => sum + (p.score ?? p.Score ?? 0), 0);
  const teamBScore = teamB.reduce((sum, p: any) => sum + (p.score ?? p.Score ?? 0), 0);

  const currentStage = state.currentStage || state.CurrentStage || 'STARTER_QUESTION';

  const handleBuzz = () => {
    playArenaSfx('buzzer');
    onBuzzer();
  };

  const handleSend = () => {
    if (ans.trim()) {
      onSubmitAnswer(ans.trim());
      setAns('');
    }
  };

  const qCategory = currentQ.category || currentQ.Category || 'HỌC THUẬT QUỐC TẾ';
  const qContent = currentQ.content || currentQ.Content || '';

  return (
    <div className="space-y-6">
      {/* ─── HEADER BAR ─── */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-gradient-to-r from-blue-700 via-indigo-700 to-violet-800 p-5 text-white shadow-xl">
        <div className="flex items-center gap-3">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/20 text-3xl shadow-inner backdrop-blur-sm">
            🎓
          </div>
          <div>
            <span className="text-xs font-black uppercase tracking-widest text-blue-200">
              Đại Học Đỉnh Cao (BBC University Challenge)
            </span>
            <h2 className="text-xl font-black text-white">
              {currentStage === 'STARTER_QUESTION' ? 'Câu Hỏi Khởi Động Cá Nhân (Starter Question 10đ)' : 'Bộ 3 Câu Hỏi Đội Nhóm (Bonus Set 15đ)'}
            </h2>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <span className="rounded-xl bg-white/10 px-3 py-1.5 text-xs font-bold text-blue-200 border border-white/20">
            {currentStage === 'BONUS_SET' ? 'Đang Mở Micro Hội Ý 15s 🎙️' : 'Không Được Trao Đổi 🔒'}
          </span>
        </div>
      </div>

      {/* ─── 2 TEAM PODIUMS ─── */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Team A: ĐH Bách Khoa */}
        <div className="rounded-3xl border-2 border-blue-500/40 bg-slate-900 p-5 text-white shadow-xl">
          <div className="flex items-center justify-between border-b border-slate-800 pb-3">
            <div className="flex items-center gap-2">
              <span className="text-2xl">🏛️</span>
              <div>
                <h3 className="font-black text-base text-blue-400">ĐH Bách Khoa Hà Nội</h3>
                <span className="text-xs text-slate-400">Đội A • 4 Thành Viên</span>
              </div>
            </div>
            <span className="text-3xl font-black text-blue-400">{teamAScore} <span className="text-xs text-slate-400 font-normal">pts</span></span>
          </div>

          <div className="mt-4 grid grid-cols-4 gap-2">
            {[1, 2, 3, 4].map((slot) => {
              const p: any = teamA[slot - 1];
              return (
                <div key={slot} className="rounded-2xl border border-slate-700 bg-slate-800 p-2.5 text-center">
                  <div className="text-xl">{p ? (p.avatar || p.Avatar || '👨‍🎓') : '👤'}</div>
                  <p className="mt-1 truncate text-xs font-bold text-slate-300">{p ? (p.name || p.Name) : `Thành viên ${slot}`}</p>
                  <span className="text-[10px] text-blue-400 font-bold">{p ? `${p.score ?? p.Score ?? 0}đ` : '0đ'}</span>
                </div>
              );
            })}
          </div>
        </div>

        {/* Team B: ĐH Ngoại Thương */}
        <div className="rounded-3xl border-2 border-violet-500/40 bg-slate-900 p-5 text-white shadow-xl">
          <div className="flex items-center justify-between border-b border-slate-800 pb-3">
            <div className="flex items-center gap-2">
              <span className="text-2xl">🌐</span>
              <div>
                <h3 className="font-black text-base text-violet-400">ĐH Ngoại Thương (FTU)</h3>
                <span className="text-xs text-slate-400">Đội B • 4 Thành Viên</span>
              </div>
            </div>
            <span className="text-3xl font-black text-violet-400">{teamBScore} <span className="text-xs text-slate-400 font-normal">pts</span></span>
          </div>

          <div className="mt-4 grid grid-cols-4 gap-2">
            {[1, 2, 3, 4].map((slot) => {
              const p: any = teamB[slot - 1];
              return (
                <div key={slot} className="rounded-2xl border border-slate-700 bg-slate-800 p-2.5 text-center">
                  <div className="text-xl">{p ? (p.avatar || p.Avatar || '👩‍🎓') : '👤'}</div>
                  <p className="mt-1 truncate text-xs font-bold text-slate-300">{p ? (p.name || p.Name) : `Thành viên ${slot}`}</p>
                  <span className="text-[10px] text-violet-400 font-bold">{p ? `${p.score ?? p.Score ?? 0}đ` : '0đ'}</span>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* ─── ACADEMIC QUESTION BOARD ─── */}
      <div className="rounded-3xl border-2 border-slate-700 bg-slate-900 p-6 text-white shadow-2xl">
        <div className="text-center max-w-3xl mx-auto py-4">
          <span className="rounded-full bg-blue-500/20 px-3 py-1 text-xs font-bold text-blue-300 border border-blue-500/30">
            {qCategory}
          </span>
          <p className="mt-4 text-xl sm:text-2xl font-black text-slate-100 leading-relaxed">
            {qContent}
          </p>
        </div>

        <div className="mt-6 max-w-xl mx-auto flex gap-2">
          <input
            type="text"
            value={ans}
            onChange={(e) => setAns(e.target.value)}
            placeholder="Nhập câu trả lời học thuật..."
            className="flex-1 rounded-2xl border border-slate-700 bg-slate-800 px-4 py-3 font-bold text-white focus:border-blue-500 focus:outline-none"
            onKeyDown={(e) => e.key === 'Enter' && handleSend()}
          />
          <button
            onClick={handleSend}
            className="rounded-2xl bg-blue-600 hover:bg-blue-700 px-6 py-3 font-black text-white transition"
          >
            Nộp
          </button>
        </div>

        {/* Big Buzzer Button */}
        <div className="mt-6 text-center">
          <button
            onClick={handleBuzz}
            className="rounded-full bg-gradient-to-r from-blue-600 to-indigo-600 px-8 py-4 text-lg font-black uppercase text-white shadow-lg hover:scale-105 active:scale-95 transition"
          >
            🔔 STARTER BUZZER (CƯỚP QUYỀN TRẢ LỜI)
          </button>
          <p className="mt-2 text-xs text-slate-400">
            Chú ý: Bấm chuông ngắt lời mà trả lời sai sẽ bị trừ 5 điểm Penalty theo quy chuẩn BBC!
          </p>
        </div>
      </div>
    </div>
  );
}
