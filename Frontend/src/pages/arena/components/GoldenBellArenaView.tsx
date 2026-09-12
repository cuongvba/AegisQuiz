import React, { useState } from 'react';
import { Bell, HeartHandshake, Award, ShieldAlert, Check, X, Sparkles } from 'lucide-react';
import type { ArenaRoomState, ArenaPlayer, ArenaQuestionItem } from '@/types/arena';
import { playArenaSfx } from '@/services/arena.service';

interface Props {
  state: ArenaRoomState;
  onSubmitAnswer: (ans: string) => void;
  onAction: (actionType: string, payload: string) => void;
  onNextStage: (next: string) => void;
}

export function GoldenBellArenaView({ state, onSubmitAnswer, onAction, onNextStage }: Props) {
  const [writtenAnswer, setWrittenAnswer] = useState('');
  const [hasSubmitted, setHasSubmitted] = useState(false);

  const questions: ArenaQuestionItem[] = state.questions || state.Questions || [];
  const qIndex = state.currentQuestionIndex ?? state.CurrentQuestionIndex ?? 0;
  const currentQ: ArenaQuestionItem = questions[qIndex] || questions[0] || {
    id: 'q0',
    content: 'Đang tải câu hỏi...',
    questionType: 'MULTIPLE_CHOICE',
    options: [],
    answerRaw: '',
    explanation: '',
    points: 10,
    timeLimitSeconds: 15,
    category: 'ĐỊA LÝ - VĂN HÓA'
  };

  const rawPlayers = state.players || state.Players || {};
  const players: ArenaPlayer[] = Object.values(rawPlayers);

  // Giả lập 100 ghế số trên sàn đấu Rung Chuông Vàng
  const TOTAL_SEATS = 100;
  const activeCount = players.filter((p: any) => (p.status || p.Status) === 'ACTIVE').length || 85;
  const eliminatedCount = TOTAL_SEATS - activeCount;

  const handleRescue = () => {
    playArenaSfx('victory');
    onAction('TEACHER_RESCUE', '');
  };

  const handleSendAnswer = () => {
    if (writtenAnswer.trim()) {
      setHasSubmitted(true);
      onSubmitAnswer(writtenAnswer.trim());
    }
  };

  const qCategory = currentQ.category || currentQ.Category || 'ĐỊA LÝ - VĂN HÓA';
  const qContent = currentQ.content || currentQ.Content || '';
  const options: string[] = currentQ.options || currentQ.Options || [];

  return (
    <div className="space-y-6">
      {/* ─── HEADER BAR ─── */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-gradient-to-r from-yellow-500 via-amber-500 to-orange-600 p-5 text-white shadow-xl">
        <div className="flex items-center gap-3">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/20 text-3xl shadow-inner backdrop-blur-sm animate-bounce">
            🔔
          </div>
          <div>
            <span className="text-xs font-black uppercase tracking-widest text-yellow-100">
              Battle Royale Tri Thức
            </span>
            <h2 className="text-xl font-black text-white">
              Đấu Trường Rung Chuông Vàng — Câu Số {qIndex + 1}/50
            </h2>
          </div>
        </div>

        {/* Stats on Floor */}
        <div className="flex items-center gap-2">
          <div className="rounded-xl bg-white/20 px-3 py-1.5 text-center backdrop-blur-sm">
            <span className="block text-xs text-yellow-100 font-semibold">Trên Sàn Đấu</span>
            <span className="font-black text-lg text-white">{activeCount} Thí Sinh</span>
          </div>
          <div className="rounded-xl bg-black/20 px-3 py-1.5 text-center backdrop-blur-sm">
            <span className="block text-xs text-rose-200 font-semibold">Đã Bị Loại</span>
            <span className="font-black text-lg text-rose-300">{eliminatedCount}</span>
          </div>
          <button
            onClick={handleRescue}
            className="flex items-center gap-1.5 rounded-xl bg-white px-4 py-2.5 text-xs font-black text-amber-700 shadow-md hover:bg-yellow-50 active:scale-95 transition"
          >
            <HeartHandshake className="h-4 w-4 text-rose-600" />
            Thầy Cô Cứu Trợ 🎈
          </button>
        </div>
      </div>

      {/* ─── MEGA-GRID 100 GHẾ SỐ MA TRẬN SÀN ĐẤU ─── */}
      <div className="rounded-3xl border-2 border-amber-400/30 bg-slate-950 p-5 text-white shadow-2xl">
        <div className="flex items-center justify-between border-b border-slate-800 pb-3 mb-4">
          <div className="flex items-center gap-2">
            <span className="h-3 w-3 rounded-full bg-emerald-500 animate-ping" />
            <h3 className="font-black text-sm text-slate-200 uppercase tracking-wider">
              Sơ Đồ 100 Vị Trí Sàn Đấu Trực Tiếp
            </h3>
          </div>
          <div className="flex gap-4 text-xs font-bold text-slate-400">
            <span className="flex items-center gap-1.5">
              <span className="h-2.5 w-2.5 rounded-full bg-emerald-500" /> Đang Thi Đấu
            </span>
            <span className="flex items-center gap-1.5">
              <span className="h-2.5 w-2.5 rounded-full bg-rose-600" /> Đã Bị Loại
            </span>
          </div>
        </div>

        {/* 10x10 Matrix Seats */}
        <div className="grid grid-cols-10 gap-1.5 sm:gap-2">
          {Array.from({ length: TOTAL_SEATS }).map((_, idx) => {
            const seatNo = idx + 1;
            // Cho một số ghế minh họa trạng thái loại
            const isEliminated = seatNo % 7 === 0;
            return (
              <div
                key={seatNo}
                className={`relative flex aspect-square flex-col items-center justify-center rounded-xl border text-center transition-all ${
                  isEliminated
                    ? 'border-rose-900 bg-rose-950/40 text-rose-400/50 opacity-40'
                    : 'border-emerald-500/40 bg-emerald-950/30 text-emerald-300 shadow-sm hover:scale-110 hover:border-amber-400'
                }`}
                title={`Vị trí ghế số ${seatNo}`}
              >
                <span className="text-[10px] sm:text-xs font-black">{seatNo}</span>
                <span className="text-[9px] hidden sm:block opacity-70">
                  {isEliminated ? 'Rời sàn' : '🎓'}
                </span>
              </div>
            );
          })}
        </div>
      </div>

      {/* ─── DIGITAL SLATE: BẢNG VIẾT ĐIỆN TỬ CỦA THÍ SINH ─── */}
      <div className="rounded-3xl border-2 border-slate-700 bg-slate-900 p-6 text-white shadow-2xl">
        <div className="text-center max-w-2xl mx-auto">
          <span className="rounded-full bg-amber-500/20 px-3 py-1 text-xs font-bold text-amber-400 border border-amber-500/30">
            {qCategory}
          </span>
          <p className="mt-4 text-xl sm:text-2xl font-black text-slate-100 leading-relaxed">
            {qContent}
          </p>
        </div>

        {/* Multiple choices or writing slate */}
        {options.length > 0 ? (
          <div className="mt-6 grid grid-cols-1 sm:grid-cols-2 gap-3 max-w-xl mx-auto">
            {options.map((opt: string, i: number) => (
              <button
                key={i}
                onClick={() => {
                  setWrittenAnswer(opt);
                  setHasSubmitted(true);
                  onSubmitAnswer(opt);
                }}
                className={`flex items-center gap-3 rounded-2xl border p-4 text-left font-bold transition ${
                  writtenAnswer === opt
                    ? 'border-amber-400 bg-amber-500/20 text-white'
                    : 'border-slate-700 bg-slate-800 text-slate-300 hover:border-slate-500'
                }`}
              >
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-slate-700 font-black text-xs">
                  {String.fromCharCode(65 + i)}
                </span>
                <span className="text-sm">{opt}</span>
              </button>
            ))}
          </div>
        ) : (
          <div className="mt-6 max-w-md mx-auto">
            <div className="rounded-2xl border-2 border-amber-400/40 bg-slate-950 p-4 shadow-inner">
              <label className="text-[11px] font-bold uppercase tracking-wider text-amber-400 block mb-2">
                ✍️ Viết đáp án lên bảng Mica điện tử:
              </label>
              <input
                type="text"
                value={writtenAnswer}
                onChange={(e) => setWrittenAnswer(e.target.value)}
                placeholder="Gõ đáp án của bạn..."
                className="w-full rounded-xl border border-slate-700 bg-slate-800 px-4 py-3 font-black text-amber-300 placeholder:text-slate-500 focus:outline-none focus:border-amber-400 text-center text-lg uppercase"
                onKeyDown={(e) => e.key === 'Enter' && handleSendAnswer()}
              />
              <button
                onClick={handleSendAnswer}
                className="mt-3 w-full rounded-xl bg-gradient-to-r from-amber-500 to-yellow-500 py-3 font-black text-slate-950 shadow-md hover:opacity-95 transition"
              >
                {hasSubmitted ? '✓ ĐÃ GIƠ BẢNG ĐÁP ÁN' : 'GIƠ BẢNG NỘP ĐÁP ÁN ➔'}
              </button>
            </div>
          </div>
        )}

        <div className="mt-6 text-center">
          <button
            onClick={() => onNextStage('FLOOR_BATTLE')}
            className="rounded-xl border border-slate-700 bg-slate-800/80 px-6 py-2.5 text-xs font-bold text-slate-300 hover:bg-slate-700 transition"
          >
            Chuyển Sang Câu Hỏi Tiếp Theo ➔
          </button>
        </div>
      </div>
    </div>
  );
}
