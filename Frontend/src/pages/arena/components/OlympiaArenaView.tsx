import React, { useState } from 'react';
import { Trophy, Star, Bell, Shield, CheckCircle2, XCircle, AlertCircle, ArrowRight } from 'lucide-react';
import type { ArenaRoomState, ArenaPlayer, ArenaQuestionItem } from '@/types/arena';
import { playArenaSfx } from '@/services/arena.service';

interface OlympiaArenaViewProps {
  state: ArenaRoomState;
  onAnswer?: (answer: string) => void;
  onSubmitAnswer?: (answer: string) => void;
  onBuzzer: () => void;
  onNextStage: (nextStage: string) => void;
  onAction: (actionType: string, payload: any) => void;
}

export function OlympiaArenaView({ state, onAnswer, onSubmitAnswer, onBuzzer, onNextStage, onAction }: OlympiaArenaViewProps) {
  const doAnswer = (ans: string) => {
    if (onSubmitAnswer) onSubmitAnswer(ans);
    if (onAnswer) onAnswer(ans);
  };
  const [typedAnswer, setTypedAnswer] = useState('');
  const [starUsed, setStarUsed] = useState(false);
  const [cnvGuess, setCnvGuess] = useState('');
  const [showCnvModal, setShowCnvModal] = useState(false);

  const currentStage = state.currentStage || state.CurrentStage || 'KHOI_DONG';
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
    category: 'OLYMPIA'
  };

  const rawPlayers = state.players || state.Players || {};
  const players: ArenaPlayer[] = Object.values(rawPlayers);
  const winnerPlayerId = state.buzzerWinnerPlayerId || state.BuzzerWinnerPlayerId;
  const winnerPlayer = winnerPlayerId ? rawPlayers[winnerPlayerId] : null;
  const isBuzzed = Boolean(winnerPlayerId);

  const handleBuzzClick = () => {
    playArenaSfx('buzzer');
    onBuzzer();
  };

  const handleStarClick = () => {
    if (!starUsed) {
      setStarUsed(true);
      playArenaSfx('victory');
      onAction('USE_STAR_OF_HOPE', '');
    }
  };

  const handleCnvSubmit = () => {
    if (cnvGuess.trim()) {
      onAction('GUESS_CNV', cnvGuess);
      setShowCnvModal(false);
    }
  };

  const options: string[] = currentQ.options || currentQ.Options || [];
  const qContent = currentQ.content || currentQ.Content || '';
  const qCategory = currentQ.category || currentQ.Category || 'TRI THỨC TỔNG HỢP';
  const qPoints = currentQ.points || currentQ.Points || 10;

  return (
    <div className="space-y-6">
      {/* ─── STAGE PROGRESSION BAR ─── */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-slate-900/90 p-4 border border-amber-500/30 text-white shadow-xl">
        <div className="flex items-center gap-3">
          <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-amber-500 text-2xl font-black shadow-lg shadow-amber-500/30">
            🏛️
          </span>
          <div>
            <span className="text-xs uppercase tracking-widest text-amber-400 font-bold">Đường Lên Đỉnh Olympia</span>
            <h2 className="text-lg font-black text-white">
              {currentStage === 'KHOI_DONG' && 'Vòng 1: KHỞI ĐỘNG (60 Giây Tốc Biến)'}
              {currentStage === 'VUOT_CNV' && 'Vòng 2: VƯỢT CHƯỚNG NGẠI VẬT (Ma Trận Ẩn Số)'}
              {currentStage === 'TANG_TOC' && 'Vòng 3: TĂNG TỐC (Mili-giây 40-30-20-10)'}
              {currentStage === 'VE_DICH' && 'Vòng 4: VỀ ĐÍCH & NGÔI SAO HY VỌNG'}
              {currentStage === 'FINISHED' && 'CHUNG KẾT TRAO VÒNG NGUYỆT QUẾ'}
            </h2>
          </div>
        </div>

        {/* Stage Controller */}
        <div className="flex items-center gap-2">
          {currentStage === 'KHOI_DONG' && (
            <button onClick={() => onNextStage('VUOT_CNV')} className="rounded-xl bg-amber-500 hover:bg-amber-600 px-4 py-2 text-xs font-black text-slate-950 transition">
              Sang Vòng 2: Vượt CNV ➔
            </button>
          )}
          {currentStage === 'VUOT_CNV' && (
            <button onClick={() => onNextStage('TANG_TOC')} className="rounded-xl bg-orange-500 hover:bg-orange-600 px-4 py-2 text-xs font-black text-white transition">
              Sang Vòng 3: Tăng Tốc ➔
            </button>
          )}
          {currentStage === 'TANG_TOC' && (
            <button onClick={() => onNextStage('VE_DICH')} className="rounded-xl bg-rose-600 hover:bg-rose-700 px-4 py-2 text-xs font-black text-white transition">
              Sang Vòng 4: Về Đích ➔
            </button>
          )}
          {currentStage === 'VE_DICH' && (
            <button onClick={() => onNextStage('FINISHED')} className="rounded-xl bg-emerald-600 hover:bg-emerald-700 px-4 py-2 text-xs font-black text-white transition">
              Kết Thúc & Trao Thưởng 🏆
            </button>
          )}
        </div>
      </div>

      {/* ─── 4 BỤC THÍ SINH (4 3D PODIUMS) ─── */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {players.map((p, idx) => {
          const playerId = p.id || p.Id;
          const playerName = p.name || p.Name;
          const playerAvatar = p.avatar || p.Avatar || '🎓';
          const playerScore = p.score ?? p.Score ?? 0;
          const playerStreak = p.streak ?? p.Streak ?? 0;
          const isWinner = winnerPlayerId === playerId;

          return (
            <div
              key={playerId || idx}
              className={`relative overflow-hidden rounded-2xl border-2 p-4 transition-all ${
                isWinner
                  ? 'border-amber-400 bg-amber-500/10 shadow-[0_0_30px_rgba(245,158,11,0.5)] scale-105'
                  : 'border-slate-700 bg-slate-900/80 text-white'
              }`}
            >
              <div className="flex items-center justify-between">
                <span className="rounded-full bg-slate-800 px-2 py-0.5 text-[10px] font-bold text-amber-400">
                  Vị trí {idx + 1}
                </span>
                {isWinner && (
                  <span className="animate-pulse rounded-full bg-amber-500 px-2 py-0.5 text-[10px] font-black text-slate-950">
                    🔔 ĐÃ BẤM CHUÔNG
                  </span>
                )}
              </div>

              <div className="mt-3 text-center">
                <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-gradient-to-tr from-amber-400 to-orange-500 text-2xl shadow-md">
                  {playerAvatar}
                </div>
                <h4 className="mt-2 truncate font-black text-sm text-white">{playerName}</h4>
                <p className="mt-1 text-2xl font-black text-amber-400 tracking-tight">{playerScore} <span className="text-xs font-normal text-slate-400">điểm</span></p>
              </div>

              {playerStreak > 1 && (
                <div className="mt-2 text-center text-[11px] font-bold text-emerald-400">
                  🔥 Streak: {playerStreak} liên tiếp!
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* ─── VÒNG 2: VƯỢT CHƯỚNG NGẠI VẬT MATRIX ─── */}
      {currentStage === 'VUOT_CNV' && (
        <div className="rounded-3xl border-2 border-orange-500/30 bg-slate-900/90 p-5 text-white shadow-xl">
          <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
            <div>
              <span className="text-xs uppercase tracking-wider text-orange-400 font-extrabold">Bản Đồ Chướng Ngại Vật</span>
              <h3 className="text-base font-black text-white">4 Hàng Ngang + 1 Ẩn Số Trung Tâm</h3>
            </div>
            <button
              onClick={() => setShowCnvModal(true)}
              className="rounded-xl bg-gradient-to-r from-orange-500 to-rose-600 px-4 py-2.5 text-xs font-black text-white shadow-lg shadow-orange-500/30 hover:scale-105 transition"
            >
              🚩 BẤM CHUÔNG TRẢ LỜI CHƯỚNG NGẠI VẬT (80đ)
            </button>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 text-center">
            {['HÀNG NGANG 1 (10 KÝ TỰ)', 'HÀNG NGANG 2 (8 KÝ TỰ)', 'HÀNG NGANG 3 (12 KÝ TỰ)', 'HÀNG NGANG 4 (7 KÝ TỰ)'].map((row, i) => (
              <div key={i} className="rounded-xl border border-slate-700 bg-slate-800/80 p-3">
                <span className="text-xs font-bold text-amber-400">Mảnh ghép #{i + 1}</span>
                <p className="mt-1 text-xs text-slate-300">{row}</p>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ─── QUESTION ARENA BOARD ─── */}
      <div className="relative overflow-hidden rounded-3xl border-2 border-slate-700 bg-slate-900 p-6 text-white shadow-2xl">
        {/* Category & Timer */}
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-800 pb-4">
          <div className="flex items-center gap-2">
            <span className="rounded-full bg-amber-500/20 px-3 py-1 text-xs font-extrabold text-amber-400 border border-amber-500/30">
              {qCategory}
            </span>
            <span className="rounded-full bg-slate-800 px-3 py-1 text-xs font-bold text-slate-300">
              +{qPoints} ĐIỂM
            </span>
          </div>

          {/* Star of Hope Trigger (Vòng Về Đích) */}
          {currentStage === 'VE_DICH' && (
            <button
              disabled={starUsed}
              onClick={handleStarClick}
              className={`flex items-center gap-1.5 rounded-full px-4 py-1.5 text-xs font-black transition ${
                starUsed
                  ? 'bg-slate-800 text-slate-500 cursor-not-allowed'
                  : 'bg-amber-400 text-slate-950 shadow-lg shadow-amber-400/40 hover:scale-105 active:scale-95'
              }`}
            >
              <Star className="h-4 w-4 fill-current" />
              {starUsed ? 'Đã Dùng Ngôi Sao Hy Vọng' : '⭐ ĐẶT NGÔI SAO HY VỌNG (x2 ĐIỂM)'}
            </button>
          )}
        </div>

        {/* Question Text */}
        <div className="py-6 text-center">
          <p className="text-xl sm:text-2xl font-black leading-relaxed text-slate-100 max-w-3xl mx-auto">
            {qContent}
          </p>
        </div>

        {/* Multiple Choice Options */}
        {options.length > 0 ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 max-w-2xl mx-auto">
            {options.map((opt: string, i: number) => (
              <button
                key={i}
                onClick={() => doAnswer(opt)}
                className="flex items-center gap-3 rounded-2xl border border-slate-700 bg-slate-800/80 p-4 text-left font-bold text-slate-200 hover:border-amber-400 hover:bg-amber-500/10 hover:text-white transition group"
              >
                <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl bg-slate-700 group-hover:bg-amber-500 group-hover:text-slate-950 font-black text-sm transition">
                  {String.fromCharCode(65 + i)}
                </span>
                <span className="text-sm leading-relaxed">{opt}</span>
              </button>
            ))}
          </div>
        ) : (
          <div className="flex gap-2 max-w-xl mx-auto">
            <input
              type="text"
              value={typedAnswer}
              onChange={(e) => setTypedAnswer(e.target.value)}
              placeholder="Nhập đáp án trực tiếp của bạn..."
              className="flex-1 rounded-2xl border-2 border-slate-700 bg-slate-800 px-4 py-3 font-bold text-white placeholder:text-slate-500 focus:border-amber-400 focus:outline-none"
              onKeyDown={(e) => {
                if (e.key === 'Enter' && typedAnswer.trim()) {
                  doAnswer(typedAnswer.trim());
                  setTypedAnswer('');
                }
              }}
            />
            <button
              onClick={() => {
                if (typedAnswer.trim()) {
                  doAnswer(typedAnswer.trim());
                  setTypedAnswer('');
                }
              }}
              className="rounded-2xl bg-amber-500 px-6 py-3 font-black text-slate-950 hover:bg-amber-400 transition"
            >
              Gửi
            </button>
          </div>
        )}

        {/* ─── BUZZER / CƯỚP ĐIỂM BAR ─── */}
        <div className="mt-8 flex flex-col sm:flex-row items-center justify-between gap-4 border-t border-slate-800 pt-5">
          <div className="flex items-center gap-3 text-xs text-slate-400">
            <Bell className="h-4 w-4 text-amber-400" />
            <span>
              {isBuzzed
                ? `Thí sinh ${(winnerPlayer?.name || winnerPlayer?.Name) || 'khác'} đã giành được quyền trả lời!`
                : 'Chuông giành quyền trả lời đang mở'}
            </span>
          </div>

          <button
            onClick={handleBuzzClick}
            disabled={isBuzzed}
            className={`flex items-center gap-2 rounded-2xl px-8 py-4 font-black uppercase tracking-wider transition ${
              isBuzzed
                ? 'bg-slate-800 text-slate-500 cursor-not-allowed border border-slate-700'
                : 'bg-gradient-to-r from-red-600 to-rose-500 text-white shadow-[0_0_30px_rgba(225,29,72,0.5)] hover:scale-105 active:scale-95 border border-red-400'
            }`}
          >
            <Bell className="h-5 w-5 fill-current animate-bounce" />
            🔔 BẤM CHUÔNG GIÀNH QUYỀN TRẢ LỜI
          </button>
        </div>
      </div>

      {/* CNV Guess Modal */}
      {showCnvModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm p-4">
          <div className="w-full max-w-md rounded-3xl border-2 border-orange-500 bg-slate-900 p-6 text-white shadow-2xl">
            <h3 className="text-xl font-black text-orange-400">🚩 BẤM CHUÔNG VƯỢT CHƯỚNG NGẠI VẬT</h3>
            <p className="mt-2 text-xs text-slate-400">
              Lưu ý: Nếu đoán sai bạn sẽ bị mất quyền chơi toàn bộ phần còn lại của Vòng 2!
            </p>
            <input
              type="text"
              autoFocus
              value={cnvGuess}
              onChange={(e) => setCnvGuess(e.target.value.toUpperCase())}
              placeholder="NHẬP TỪ KHÓA CHƯỚNG NGẠI VẬT..."
              className="mt-4 w-full rounded-2xl border-2 border-orange-500/50 bg-slate-800 p-4 font-black tracking-widest text-center text-white placeholder:text-slate-600 uppercase focus:border-orange-400 focus:outline-none"
            />
            <div className="mt-6 flex justify-end gap-3">
              <button
                onClick={() => setShowCnvModal(false)}
                className="rounded-xl px-4 py-2 font-bold text-slate-400 hover:text-white"
              >
                Hủy bỏ
              </button>
              <button
                onClick={handleCnvSubmit}
                className="rounded-xl bg-orange-500 hover:bg-orange-600 px-6 py-2.5 font-black text-slate-950 transition"
              >
                Xác Nhận Đoán (80 Điểm)
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
