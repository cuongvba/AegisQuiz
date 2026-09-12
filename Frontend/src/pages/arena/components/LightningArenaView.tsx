import React, { useState } from 'react';
import { Zap, ArrowUp, AlertTriangle, Sparkles, Trophy, RotateCcw } from 'lucide-react';
import confetti from 'canvas-confetti';
import type { ArenaRoomState, ArenaQuestionItem } from '@/types/arena';
import { playArenaSfx } from '@/services/arena.service';

interface Props {
  state: ArenaRoomState;
  onSubmitAnswer: (ans: string) => void;
  onAction: (actionType: string, payload: string) => void;
  onNextStage: (next: string) => void;
}

export function LightningArenaView({ state, onSubmitAnswer, onAction, onNextStage }: Props) {
  const [currentStep, setCurrentStep] = useState(0);
  const [typedAnswer, setTypedAnswer] = useState('');

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
    category: 'CÂU ĐỐ MẸO'
  };

  const expectedAnswer = currentQ.answerRaw || currentQ.AnswerRaw || '';
  const qContent = currentQ.content || currentQ.Content || '';

  const handleSendAnswer = (answer: string) => {
    const isCorrect = answer.trim().toLowerCase() === expectedAnswer.trim().toLowerCase();

    if (isCorrect) {
      playArenaSfx('victory');
      const nextStep = Math.min(10, currentStep + 1);
      setCurrentStep(nextStep);
      if (nextStep === 10) {
        confetti({ particleCount: 200, spread: 120, origin: { y: 0.6 } });
      }
    } else {
      // TỤT DỐC VỀ VẠCH SỐ 0
      playArenaSfx('drop');
      setCurrentStep(0);
    }

    onSubmitAnswer(answer);
    setTypedAnswer('');
  };

  return (
    <div className="space-y-6">
      {/* ─── HEADER BAR ─── */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-gradient-to-r from-rose-600 via-pink-600 to-purple-700 p-5 text-white shadow-xl">
        <div className="flex items-center gap-3">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/20 text-3xl shadow-inner backdrop-blur-sm animate-pulse">
            ⚡
          </div>
          <div>
            <span className="text-xs font-black uppercase tracking-widest text-pink-200">
              Nhanh Như Chớp (Lightning Incline)
            </span>
            <h2 className="text-xl font-black text-white">
              Cỗ Máy Leo Dốc 10 Bậc Đứng (Sai Là Tụt Về 0!)
            </h2>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="rounded-xl bg-white/10 px-4 py-2 text-center backdrop-blur-sm border border-white/20">
            <span className="block text-[11px] text-pink-200 font-bold">Vị Trí Hiện Tại</span>
            <span className="text-2xl font-black text-yellow-300">BẬC {currentStep}/10</span>
          </div>
        </div>
      </div>

      {/* ─── 10-STEP INCLINE LADDER GRAPHIC ─── */}
      <div className="rounded-3xl border-2 border-rose-500/30 bg-slate-950 p-6 text-white shadow-2xl">
        <h3 className="text-xs font-bold uppercase tracking-wider text-rose-400 mb-4 text-center">
          Dốc Nghiêng Chinh Phục: 10 Câu Đúng Liên Tiếp
        </h3>

        <div className="flex flex-col-reverse gap-2 max-w-xl mx-auto">
          {Array.from({ length: 10 }).map((_, idx) => {
            const stepNum = idx + 1;
            const isActive = currentStep === stepNum;
            const isPassed = currentStep > stepNum;

            return (
              <div
                key={stepNum}
                className={`relative flex items-center justify-between rounded-2xl border-2 p-3 font-black text-sm transition-all duration-500 ${
                  isActive
                    ? 'border-yellow-400 bg-gradient-to-r from-rose-600 to-pink-600 text-white shadow-[0_0_30px_rgba(251,191,36,0.6)] scale-105'
                    : isPassed
                    ? 'border-emerald-500 bg-emerald-950/40 text-emerald-300'
                    : 'border-slate-800 bg-slate-900/60 text-slate-500'
                }`}
              >
                <div className="flex items-center gap-3">
                  <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-black/30 text-xs">
                    {stepNum}
                  </span>
                  <span>{stepNum === 10 ? '👑 ĐỈNH CAO NHANH NHƯ CHỚP (CHIẾN THẮNG)' : `Nấc Thang Số ${stepNum}`}</span>
                </div>

                {isActive && (
                  <span className="flex items-center gap-1.5 rounded-full bg-yellow-400 px-3 py-1 text-xs font-black text-slate-950 animate-bounce">
                    🚀 BẠN ĐANG Ở ĐÂY
                  </span>
                )}
                {isPassed && <span className="text-emerald-400">✓</span>}
              </div>
            );
          })}
        </div>
      </div>

      {/* ─── RIDDLE QUESTION CARD ─── */}
      <div className="rounded-3xl border-2 border-slate-700 bg-slate-900 p-6 text-white shadow-2xl">
        <div className="text-center max-w-2xl mx-auto py-4">
          <span className="rounded-full bg-pink-500/20 px-3 py-1 text-xs font-bold text-pink-300 border border-pink-500/30">
            CÂU ĐỐ MẸO & LOGIC NHANH
          </span>
          <p className="mt-4 text-xl sm:text-2xl font-black text-slate-100 leading-relaxed">
            {qContent}
          </p>
        </div>

        <div className="mt-6 max-w-md mx-auto flex gap-2">
          <input
            type="text"
            value={typedAnswer}
            onChange={(e) => setTypedAnswer(e.target.value)}
            placeholder="Đáp án mẹo của bạn..."
            className="flex-1 rounded-2xl border border-slate-700 bg-slate-800 px-4 py-3 font-bold text-white placeholder:text-slate-500 focus:border-rose-500 focus:outline-none"
            onKeyDown={(e) => e.key === 'Enter' && typedAnswer.trim() && handleSendAnswer(typedAnswer)}
          />
          <button
            onClick={() => typedAnswer.trim() && handleSendAnswer(typedAnswer)}
            className="rounded-2xl bg-gradient-to-r from-rose-500 to-pink-500 px-6 py-3 font-black text-white shadow-md hover:scale-105 active:scale-95 transition"
          >
            Chốt Đáp Án ➔
          </button>
        </div>

        {/* Quick pass or drop simulator button */}
        <div className="mt-6 flex justify-center gap-3 text-xs font-bold">
          <button
            onClick={() => handleSendAnswer(expectedAnswer)}
            className="rounded-xl border border-emerald-500/40 bg-emerald-950/40 px-4 py-2 text-emerald-300 hover:bg-emerald-900/60 transition"
          >
            ✓ Trả lời đúng (Leo +1 nấc)
          </button>
          <button
            onClick={() => handleSendAnswer('SAI')}
            className="rounded-xl border border-rose-500/40 bg-rose-950/40 px-4 py-2 text-rose-300 hover:bg-rose-900/60 transition"
          >
            ✕ Trả lời sai (Tụt về 0)
          </button>
        </div>
      </div>
    </div>
  );
}
