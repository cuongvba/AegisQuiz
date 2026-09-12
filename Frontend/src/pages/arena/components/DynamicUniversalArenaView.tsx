import { useState, useEffect } from 'react';
import { Zap, Trophy, Users, Sparkles, Brain, Shield, AlertTriangle, CheckCircle2, XCircle, ArrowRight, Flame, RefreshCw } from 'lucide-react';
import type { ArenaRoomState, GameshowManifest } from '@/types/arena';
import { playArenaSfx } from '@/services/arena.service';

interface DynamicUniversalArenaViewProps {
  state: ArenaRoomState;
  onBuzzer: () => void;
  onSubmitAnswer: (ans: string) => void;
  onAction: (actionType: string, payload?: any) => void;
  onNextStage: (nextStage: string) => void;
}

export function DynamicUniversalArenaView({
  state,
  onBuzzer,
  onSubmitAnswer,
  onAction,
  onNextStage,
}: DynamicUniversalArenaViewProps) {
  const manifest: GameshowManifest = state.customData?.Manifest || {
    gameCode: state.gameCode || 'CUSTOM',
    displayName: state.roomName || 'Đấu Trường Vạn Năng',
    description: 'Gameshow tương tác thời gian thực',
    icon: 'Trophy',
    minPlayers: 1,
    maxPlayers: 100,
    layoutShell: (state.customData?.LayoutShell as any) || 'STEP_LADDER',
    contentionMode: (state.customData?.ContentionMode as any) || 'BUZZER_FASTEST',
    themeColor: '#f59e0b',
    scoringRule: { basePointsPerCorrect: 10 },
    survivalRule: { type: 'ACCUMULATIVE' },
    enabledLifelines: [
      { code: '50_50', name: '50:50', icon: 'Zap' },
      { code: 'ASK_AI', name: 'Gia Sư AI', icon: 'Brain' },
      { code: 'POLL_AUDIENCE', name: 'Khán Giả', icon: 'Users' },
    ],
    rounds: [{ roundIndex: 1, roundName: 'Vòng Đấu Chính', durationSeconds: 30 }],
  };

  const currentQ = state.questions?.[state.currentQuestionIndex] || state.questions?.[0] || {
    content: 'Đang tải câu hỏi thi đấu...',
    options: ['Phương án A', 'Phương án B', 'Phương án C', 'Phương án D'],
    answerRaw: 'Phương án A',
    points: 10,
    timeLimitSeconds: 30,
    category: 'CHUNG',
  };

  const hostPlayer = state.players?.[state.hostUserId] || {
    id: state.hostUserId,
    name: 'Kiện Tướng',
    avatar: '👑',
    score: 0,
    streak: 0,
    stepPosition: 0,
    status: 'ACTIVE',
  };

  const isBuzzerWinner = state.buzzerWinnerPlayerId === state.hostUserId;
  const isSomeoneBuzzed = !!state.buzzerWinnerPlayerId;
  const [selectedOpt, setSelectedOpt] = useState<string | null>(null);
  const [hiddenOptions, setHiddenOptions] = useState<string[]>([]);
  const [aiHint, setAiHint] = useState<string | null>(null);
  const [audiencePoll, setAudiencePoll] = useState<Record<string, number> | null>(null);
  const [timeLeft, setTimeLeft] = useState(manifest.rounds?.[0]?.durationSeconds || 30);

  // Đếm ngược thời gian
  useEffect(() => {
    setTimeLeft(manifest.rounds?.[0]?.durationSeconds || 30);
    setHiddenOptions([]);
    setSelectedOpt(null);
    setAiHint(null);
    setAudiencePoll(null);
  }, [state.currentQuestionIndex]);

  useEffect(() => {
    if (timeLeft <= 0) return;
    const timer = setInterval(() => {
      setTimeLeft((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);
    return () => clearInterval(timer);
  }, [timeLeft]);

  const handleSelectOption = (opt: string) => {
    setSelectedOpt(opt);
    onSubmitAnswer(opt);
  };

  const handleUseLifeline = (code: string) => {
    playArenaSfx('wheel_tick');
    onAction('USE_LIFELINE', code);

    if (code === '50_50') {
      // Ẩn 2 phương án sai
      const wrongOpts = (currentQ.options || []).filter((o) => o !== currentQ.answerRaw);
      setHiddenOptions(wrongOpts.slice(0, 2));
    } else if (code === 'ASK_AI') {
      setAiHint(`Gia Sư AI Socrates: "Hãy chú ý đến từ khóa '${currentQ.category || 'cốt lõi'}' và loại trừ các đáp án mang tính tuyệt đối!"`);
    } else if (code === 'POLL_AUDIENCE') {
      const poll: Record<string, number> = {};
      const opts = currentQ.options || [];
      const correct = currentQ.answerRaw;
      let remaining = 100;
      opts.forEach((o) => {
        if (o === correct) {
          poll[o] = 68;
          remaining -= 68;
        } else {
          poll[o] = Math.floor(remaining / (opts.length - 1));
        }
      });
      setAudiencePoll(poll);
    }
  };

  const totalSteps = manifest.scoringRule?.ladderPoints?.length || 15;
  const ladderSteps = Array.from({ length: totalSteps }, (_, i) => totalSteps - i);

  return (
    <div className="space-y-6 animate-fadeIn">
      {/* ─── TOP BAR THÔNG TIN GAMESHOW ─── */}
      <div className="flex flex-wrap items-center justify-between gap-4 rounded-3xl border border-slate-800 bg-slate-900/80 p-5 backdrop-blur shadow-2xl">
        <div className="flex items-center gap-3">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-amber-500/20 border border-amber-500/40 text-2xl shadow-inner">
            {manifest.icon === 'Trophy' ? '🏆' : manifest.icon === 'Flame' ? '🔥' : manifest.icon === 'Users' ? '👥' : '⚡'}
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-xl font-black text-white tracking-tight">{manifest.displayName}</h2>
              <span className="rounded-full bg-amber-500/10 border border-amber-500/30 px-2.5 py-0.5 text-[10px] font-black uppercase text-amber-400">
                {manifest.layoutShell}
              </span>
            </div>
            <p className="text-xs text-slate-400">{manifest.description}</p>
          </div>
        </div>

        {/* Đồng hồ đếm ngược & Điểm */}
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2 rounded-2xl border border-slate-800 bg-slate-950 px-4 py-2">
            <span className="text-xs font-bold text-slate-400">THỜI GIAN:</span>
            <span className={`text-xl font-black tabular-nums ${timeLeft <= 5 ? 'text-rose-500 animate-pulse' : 'text-amber-400'}`}>
              {timeLeft}s
            </span>
          </div>
          <div className="flex items-center gap-2 rounded-2xl border border-amber-500/30 bg-amber-500/10 px-5 py-2">
            <span className="text-xs font-bold text-amber-300">ĐIỂM:</span>
            <span className="text-2xl font-black text-white tabular-nums">{hostPlayer.score}</span>
          </div>
        </div>
      </div>

      {/* ─── MAIN STAGE LAYOUT CONTAINER ─── */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* CỘT TRÁI: VISUAL SHELL (THANG LEO DỐC / PODIUM / BẢNG GHẾ SỐ) */}
        <div className="lg:col-span-4 rounded-3xl border border-slate-800 bg-slate-900/60 p-5 backdrop-blur">
          <h3 className="text-xs font-black uppercase tracking-widest text-slate-400 mb-4 flex items-center gap-2">
            <Trophy className="h-4 w-4 text-amber-400" /> SÀN ĐẤU TRỰC QUAN ({manifest.layoutShell})
          </h3>

          {/* 1. SHELL: STEP_LADDER (Ai Là Triệu Phú / The Chase / Nhanh Như Chớp) */}
          {manifest.layoutShell === 'STEP_LADDER' && (
            <div className="space-y-1.5 max-h-[480px] overflow-y-auto pr-1">
              {ladderSteps.map((step) => {
                const isCurrent = (hostPlayer.stepPosition || 0) === step;
                const isPassed = (hostPlayer.stepPosition || 0) > step;
                const isMilestone = manifest.survivalRule?.safeMilestones?.includes(step);
                const pts = manifest.scoringRule?.ladderPoints?.[step - 1] || step * 100;

                return (
                  <div
                    key={step}
                    className={`flex items-center justify-between rounded-xl px-4 py-2 text-xs font-black transition-all ${
                      isCurrent
                        ? 'bg-gradient-to-r from-amber-500 to-orange-500 text-slate-950 scale-105 shadow-lg shadow-amber-500/30'
                        : isPassed
                        ? 'bg-emerald-500/20 border border-emerald-500/40 text-emerald-400'
                        : isMilestone
                        ? 'bg-slate-800/90 border border-amber-400/50 text-amber-300'
                        : 'bg-slate-950/60 text-slate-400'
                    }`}
                  >
                    <span className="flex items-center gap-2">
                      <span className="opacity-60">#{step}</span>
                      {isMilestone && <span>⭐ MỐC AN TOÀN</span>}
                    </span>
                    <span className="tabular-nums font-mono">{pts.toLocaleString()} đ</span>
                  </div>
                );
              })}
            </div>
          )}

          {/* 2. SHELL: MEGA_GRID (Rung Chuông Vàng / 1 vs 100) */}
          {manifest.layoutShell === 'MEGA_GRID' && (
            <div>
              <div className="grid grid-cols-8 gap-1.5 max-h-[450px] overflow-y-auto p-2 rounded-2xl bg-slate-950/80 border border-slate-800">
                {Array.from({ length: 64 }, (_, i) => {
                  const seat = i + 1;
                  const isHost = seat === 1;
                  return (
                    <div
                      key={seat}
                      className={`flex h-9 items-center justify-center rounded-lg text-[10px] font-black transition ${
                        isHost
                          ? 'bg-amber-500 text-slate-950 ring-2 ring-amber-300 shadow-md scale-105'
                          : seat % 7 === 0
                          ? 'bg-rose-950/40 text-rose-500/40 border border-rose-900/30 line-through'
                          : 'bg-emerald-950/30 text-emerald-400/80 border border-emerald-900/20'
                      }`}
                    >
                      {seat}
                    </div>
                  );
                })}
              </div>
              <p className="mt-3 text-center text-xs text-slate-500">Màu vàng: Vị trí của bạn • Xanh: Đang thi đấu</p>
            </div>
          )}

          {/* 3. SHELL: PODIUM / OTHERS (Olympia / University / Hidden Tiles) */}
          {manifest.layoutShell !== 'STEP_LADDER' && manifest.layoutShell !== 'MEGA_GRID' && (
            <div className="space-y-3">
              {Object.values(state.players || {}).map((p, idx) => (
                <div
                  key={p.id}
                  className={`flex items-center justify-between rounded-2xl p-4 border transition ${
                    p.id === state.hostUserId
                      ? 'border-amber-500/50 bg-amber-500/10'
                      : 'border-slate-800 bg-slate-950/60'
                  }`}
                >
                  <div className="flex items-center gap-3">
                    <span className="text-2xl">{p.avatar || '🎓'}</span>
                    <div>
                      <p className="text-sm font-black text-white">{p.name}</p>
                      <p className="text-[10px] text-slate-400">Bục số #{idx + 1}</p>
                    </div>
                  </div>
                  <span className="text-lg font-black text-amber-400 tabular-nums">{p.score} đ</span>
                </div>
              ))}
            </div>
          )}

          {/* HIỂN THỊ PHAO CỨU SINH (LIFELINES) */}
          {manifest.enabledLifelines && manifest.enabledLifelines.length > 0 && (
            <div className="mt-6 pt-5 border-t border-slate-800">
              <p className="text-xs font-black uppercase text-slate-400 mb-3 flex items-center gap-1.5">
                <Sparkles className="h-3.5 w-3.5 text-amber-400" /> PHAO CỨU SINH ({manifest.enabledLifelines.length}):
              </p>
              <div className="grid grid-cols-2 gap-2">
                {manifest.enabledLifelines.map((life) => (
                  <button
                    key={life.code}
                    onClick={() => handleUseLifeline(life.code)}
                    className="flex items-center gap-2 rounded-xl border border-slate-700 bg-slate-800/80 px-3 py-2 text-xs font-bold text-slate-200 hover:border-amber-500 hover:bg-amber-500/10 hover:text-amber-300 active:scale-95 transition cursor-pointer"
                  >
                    {life.code === '50_50' ? <Zap className="h-3.5 w-3.5 text-amber-400" /> : life.code === 'ASK_AI' ? <Brain className="h-3.5 w-3.5 text-cyan-400" /> : <Users className="h-3.5 w-3.5 text-emerald-400" />}
                    <span>{life.name}</span>
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* CỘT PHẢI: HỘP CÂU HỎI TRUNG TÂM & NÚT CHUÔNG */}
        <div className="lg:col-span-8 space-y-6">
          {/* Hộp gợi ý AI hoặc Khán giả (nếu được kích hoạt) */}
          {aiHint && (
            <div className="rounded-2xl border border-cyan-500/40 bg-cyan-500/10 p-4 text-xs font-bold text-cyan-300 animate-fadeIn">
              💡 {aiHint}
            </div>
          )}

          {audiencePoll && (
            <div className="rounded-2xl border border-emerald-500/40 bg-emerald-500/10 p-4 text-xs font-bold text-emerald-300 animate-fadeIn">
              📊 KẾT QUẢ BÌNH CHỌN KHÁN GIẢ:
              <div className="grid grid-cols-4 gap-2 mt-2">
                {Object.entries(audiencePoll).map(([opt, pct]) => (
                  <div key={opt} className="rounded-lg bg-slate-900/80 p-2 text-center border border-slate-800">
                    <span className="block font-black text-amber-400">{pct}%</span>
                    <span className="text-[10px] text-slate-400 truncate block">{opt}</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* CÂU HỎI */}
          <div className="rounded-3xl border border-slate-800 bg-gradient-to-b from-slate-900 to-slate-950 p-8 shadow-2xl relative overflow-hidden">
            <div className="flex items-center justify-between gap-4 mb-4">
              <span className="rounded-full bg-slate-800 px-3 py-1 text-xs font-bold text-slate-400">
                CÂU HỎI #{state.currentQuestionIndex + 1} • {currentQ.category || 'CHUNG'}
              </span>
              <span className="text-sm font-black text-amber-400">+{currentQ.points || 10} ĐIỂM</span>
            </div>

            <h4 className="text-xl sm:text-2xl font-black text-white leading-snug">
              {currentQ.content}
            </h4>

            {/* CÁC PHƯƠNG ÁN TRẢ LỜI */}
            <div className="mt-8 grid grid-cols-1 sm:grid-cols-2 gap-4">
              {(currentQ.options || []).map((opt, idx) => {
                const label = String.fromCharCode(65 + idx);
                const isHidden = hiddenOptions.includes(opt);
                const isSelected = selectedOpt === opt;

                if (isHidden) {
                  return (
                    <div
                      key={opt}
                      className="rounded-2xl border border-dashed border-slate-800 bg-slate-950/40 p-4 text-sm font-bold text-slate-600 opacity-40 select-none"
                    >
                      {label}. [Đã loại trừ 50:50]
                    </div>
                  );
                }

                return (
                  <button
                    key={opt}
                    onClick={() => handleSelectOption(opt)}
                    className={`flex items-center gap-3.5 rounded-2xl border p-5 text-left font-bold transition-all cursor-pointer ${
                      isSelected
                        ? 'border-amber-400 bg-amber-500/20 text-amber-200 ring-2 ring-amber-400/50 shadow-lg'
                        : 'border-slate-800 bg-slate-900/90 text-slate-200 hover:border-amber-500/50 hover:bg-slate-850 hover:scale-[1.01]'
                    }`}
                  >
                    <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl bg-slate-800 text-xs font-black text-amber-400">
                      {label}
                    </span>
                    <span className="text-sm leading-snug">{opt}</span>
                  </button>
                );
              })}
            </div>
          </div>

          {/* CƠ CHẾ BẤM CHUÔNG BUZZER (NẾU CHẾ ĐỘ LÀ BUZZER_FASTEST) */}
          {manifest.contentionMode === 'BUZZER_FASTEST' && (
            <div className="rounded-3xl border border-slate-800 bg-slate-900/60 p-6 backdrop-blur flex flex-col sm:flex-row items-center justify-between gap-4">
              <div>
                <p className="text-sm font-black text-white flex items-center gap-2">
                  <Zap className="h-4 w-4 text-amber-400" /> CƠ CHẾ BẤM CHUÔNG MICROSECOND
                </p>
                <p className="text-xs text-slate-400 mt-1">
                  {isSomeoneBuzzed
                    ? `Người giành quyền bấm chuông: ${state.buzzerWinnerPlayerId === state.hostUserId ? 'BẠN' : 'Đối thủ'}`
                    : 'Hãy bấm chuông thật nhanh để giành quyền ưu tiên trả lời!'}
                </p>
              </div>

              <button
                onClick={() => {
                  playArenaSfx('buzzer');
                  onBuzzer();
                }}
                disabled={isSomeoneBuzzed}
                className={`flex items-center gap-3 rounded-2xl px-8 py-4 font-black text-base shadow-xl transition cursor-pointer ${
                  isBuzzerWinner
                    ? 'bg-emerald-500 text-slate-950 ring-4 ring-emerald-400/50 animate-pulse'
                    : isSomeoneBuzzed
                    ? 'bg-slate-800 text-slate-500 cursor-not-allowed'
                    : 'bg-gradient-to-r from-amber-500 to-rose-600 text-slate-950 hover:scale-105 active:scale-95 shadow-amber-500/25'
                }`}
              >
                <Zap className="h-5 w-5 fill-current" />
                {isBuzzerWinner ? 'BẠN ĐANG CÓ CHUÔNG!' : isSomeoneBuzzed ? 'ĐÃ CÓ NGƯỜI BẤM' : 'BẤM CHUÔNG GIÀNH QUYỀN!'}
              </button>
            </div>
          )}

          {/* THANH ĐIỀU HƯỚNG CÂU HỎI TIẾP THEO */}
          <div className="flex items-center justify-between pt-2">
            <span className="text-xs text-slate-500">
              Chế độ tính điểm: {manifest.scoringRule?.streakMultiplier ? 'Nhân đôi chuỗi thắng 🔥' : 'Cố định'}
            </span>
            <button
              onClick={() => onNextStage(`ROUND_${(state.currentQuestionIndex % 3) + 1}`)}
              className="flex items-center gap-2 rounded-2xl bg-slate-800 border border-slate-700 px-6 py-3 text-xs font-black text-white hover:bg-slate-700 active:scale-95 transition cursor-pointer"
            >
              CÂU KẾ TIẾP ➔
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
