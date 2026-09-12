import React, { useState } from 'react';
import { DollarSign, Sparkles, Award, Zap, Check, X } from 'lucide-react';
import type { ArenaRoomState, ArenaPlayer } from '@/types/arena';
import { playArenaSfx } from '@/services/arena.service';

interface Props {
  state: ArenaRoomState;
  onBuzzer: () => void;
  onSubmitAnswer: (ans: string) => void;
  onAction: (actionType: string, payload: string) => void;
  onNextStage: (next: string) => void;
}

const CATEGORIES = ['LỊCH SỬ', 'KHOA HỌC', 'TOÁN HỌC', 'VĂN HỌC', 'CÔNG NGHỆ', 'ĐỊA LÝ'];
const POINT_TIERS = [200, 400, 600, 800, 1000];

export function JeopardyArenaView({ state, onBuzzer, onSubmitAnswer, onAction, onNextStage }: Props) {
  const [selectedCell, setSelectedCell] = useState<{ cat: string; pts: number } | null>(null);
  const [solvedCells, setSolvedCells] = useState<string[]>([]);
  const [reverseAnswer, setReverseAnswer] = useState('');

  const rawPlayers = state.players || state.Players || {};
  const players: ArenaPlayer[] = Object.values(rawPlayers);

  const handleCellClick = (cat: string, pts: number) => {
    const key = `${cat}_${pts}`;
    if (solvedCells.includes(key)) return;
    setSelectedCell({ cat, pts });
    playArenaSfx('clock');
    onAction('SELECT_CLUE', key);
  };

  const handleAnswer = () => {
    if (!selectedCell || !reverseAnswer.trim()) return;
    playArenaSfx('buzzer');
    onSubmitAnswer(reverseAnswer.trim());
    setSolvedCells((prev) => [...prev, `${selectedCell.cat}_${selectedCell.pts}`]);
    setSelectedCell(null);
    setReverseAnswer('');
  };

  return (
    <div className="space-y-6">
      {/* ─── HEADER ─── */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-gradient-to-r from-blue-900 via-indigo-900 to-slate-900 p-5 text-white shadow-xl border border-blue-500/30">
        <div className="flex items-center gap-3">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-blue-600 text-3xl shadow-inner shadow-blue-400/50">
            🇺🇸
          </div>
          <div>
            <span className="text-xs font-black uppercase tracking-widest text-blue-300">
              Jeopardy! American Matrix
            </span>
            <h2 className="text-xl font-black text-white">
              Bảng Ma Trận 30 Ô Điểm & Quy Tắc Câu Hỏi Ngược
            </h2>
          </div>
        </div>

        {/* Player score pills */}
        <div className="flex items-center gap-3">
          {players.map((p: any, idx: number) => (
            <div key={p.id || p.Id || idx} className="rounded-xl border border-blue-400/40 bg-blue-950/80 px-4 py-2 text-center">
              <span className="block text-[11px] text-blue-300 font-bold">{p.name || p.Name}</span>
              <span className="text-base font-black text-amber-400">${p.score ?? p.Score ?? 0}</span>
            </div>
          ))}
        </div>
      </div>

      {/* ─── JEOPARDY 6x5 MATRIX BOARD ─── */}
      <div className="overflow-x-auto rounded-3xl border-4 border-blue-950 bg-slate-950 p-4 shadow-2xl">
        <div className="grid grid-cols-6 gap-2 min-w-[700px]">
          {/* Category Headers */}
          {CATEGORIES.map((cat) => (
            <div
              key={cat}
              className="flex h-16 items-center justify-center rounded-xl bg-blue-900/90 p-2 text-center font-black text-xs text-white border border-blue-400/40 shadow-md uppercase tracking-wider"
            >
              {cat}
            </div>
          ))}

          {/* 5 Rows of Point Values */}
          {POINT_TIERS.map((pts) =>
            CATEGORIES.map((cat) => {
              const key = `${cat}_${pts}`;
              const isSolved = solvedCells.includes(key);
              return (
                <button
                  key={key}
                  disabled={isSolved}
                  onClick={() => handleCellClick(cat, pts)}
                  className={`flex h-20 items-center justify-center rounded-xl border font-black text-xl transition ${
                    isSolved
                      ? 'border-slate-800 bg-slate-900/50 text-slate-700 cursor-not-allowed'
                      : 'border-blue-500/40 bg-blue-950/80 text-amber-400 hover:bg-blue-600 hover:text-white hover:scale-105 shadow-md shadow-blue-900/40 cursor-pointer'
                  }`}
                >
                  {isSolved ? '-' : `$${pts}`}
                </button>
              );
            })
          )}
        </div>
      </div>

      {/* ─── CLUE ACTIVE MODAL ─── */}
      {selectedCell && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/85 backdrop-blur-md p-4">
          <div className="w-full max-w-2xl rounded-3xl border-4 border-blue-500 bg-blue-950 p-8 text-white shadow-2xl text-center">
            <div className="flex items-center justify-between border-b border-blue-800 pb-3">
              <span className="rounded-full bg-blue-600 px-3 py-1 text-xs font-black uppercase tracking-widest text-blue-100">
                {selectedCell.cat}
              </span>
              <span className="text-2xl font-black text-amber-400">${selectedCell.pts}</span>
            </div>

            <div className="py-8">
              <p className="text-xl sm:text-2xl font-black leading-relaxed text-blue-50">
                Dữ kiện: "Nhà bác học đã tìm ra Định luật Vạn vật Hấp dẫn khi quan sát một quả táo rơi."
              </p>
            </div>

            {/* Reverse format instruction */}
            <div className="rounded-2xl border border-blue-500/40 bg-blue-900/40 p-3 text-xs text-blue-200 mb-4">
              💡 <span className="font-bold">Quy tắc câu hỏi ngược:</span> Thí sinh bắt buộc phải trả lời dưới dạng câu hỏi, ví dụ: <i>"Ai là Isaac Newton?"</i> hoặc <i>"Là Isaac Newton?"</i>
            </div>

            <div className="flex gap-2">
              <input
                type="text"
                value={reverseAnswer}
                onChange={(e) => setReverseAnswer(e.target.value)}
                placeholder="Ai là...? / Là gì...?"
                className="flex-1 rounded-2xl border-2 border-blue-500 bg-slate-900 px-4 py-3 font-bold text-amber-300 placeholder:text-slate-500 focus:outline-none"
                onKeyDown={(e) => e.key === 'Enter' && handleAnswer()}
              />
              <button
                onClick={handleAnswer}
                className="rounded-2xl bg-amber-400 hover:bg-amber-500 px-6 py-3 font-black text-slate-950 transition"
              >
                Khẳng Định
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
