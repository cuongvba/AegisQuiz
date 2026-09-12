import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Users, Zap, Shield, Trophy } from 'lucide-react';
import { arenaService } from '@/services/arena.service';
import type { ArenaGameCode, ArenaRoomState } from '@/types/arena';
import { OlympiaArenaView } from './components/OlympiaArenaView';
import { GoldenBellArenaView } from './components/GoldenBellArenaView';
import { LuckyWheelArenaView } from './components/LuckyWheelArenaView';
import { UniversityChallengeView } from './components/UniversityChallengeView';
import { JeopardyArenaView } from './components/JeopardyArenaView';
import { LightningArenaView } from './components/LightningArenaView';
import { DynamicUniversalArenaView } from './components/DynamicUniversalArenaView';

export function ArenaRoomPage() {
  const { gameCode, roomId } = useParams<{ gameCode: string; roomId: string }>();
  const navigate = useNavigate();

  const [room, setRoom] = useState<ArenaRoomState | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchRoom = async () => {
      setLoading(true);
      if (roomId) {
        let r = await arenaService.getRoom(roomId);
        if (!r && gameCode) {
          // Fallback tạo room mock nếu chưa có
          r = await arenaService.createRoom(gameCode as ArenaGameCode, `${gameCode} Arena`, 'Người Chơi');
        }
        setRoom(r);
      }
      setLoading(false);
    };

    fetchRoom();
  }, [gameCode, roomId]);

  if (loading || !room) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-950 text-white">
        <div className="text-center">
          <div className="h-12 w-12 mx-auto animate-spin rounded-full border-4 border-amber-500 border-t-transparent mb-4" />
          <p className="font-bold text-sm text-slate-400">Đang Kết Nối Đấu Trường SignalR Sub-Millisecond...</p>
        </div>
      </div>
    );
  }

  const handleBuzzer = () => {
    setRoom((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        buzzerWinnerPlayerId: prev.hostUserId,
        buzzerTimestampMs: Date.now(),
      };
    });
  };

  const handleSubmitAnswer = (ans: string) => {
    setRoom((prev) => {
      if (!prev) return prev;
      const currentQ = prev.questions[prev.currentQuestionIndex] || prev.questions[0];
      const isCorrect = ans.trim().toLowerCase() === currentQ.AnswerRaw.trim().toLowerCase();
      const hostP = prev.players[prev.hostUserId];
      const newScore = isCorrect ? hostP.score + (currentQ.points || 10) : hostP.score;

      return {
        ...prev,
        players: {
          ...prev.players,
          [prev.hostUserId]: {
            ...hostP,
            score: newScore,
            streak: isCorrect ? (hostP.streak || 0) + 1 : 0,
          },
        },
        currentQuestionIndex: (prev.currentQuestionIndex + 1) % Math.max(1, prev.questions.length),
        buzzerWinnerPlayerId: null,
      };
    });
  };

  const handleAction = (actionType: string, payload: string) => {
    setRoom((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        customData: {
          ...prev.customData,
          [actionType]: payload,
        },
      };
    });
  };

  const handleNextStage = (next: string) => {
    setRoom((prev) => (prev ? { ...prev, currentStage: next } : prev));
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 selection:bg-amber-400 selection:text-slate-950 font-sans pb-16">
      {/* ─── TOP APP BAR ─── */}
      <header className="sticky top-0 z-40 border-b border-slate-800 bg-slate-900/90 backdrop-blur-md px-4 py-3">
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <div className="flex items-center gap-3">
            <button
              onClick={() => navigate('/arena')}
              className="flex h-9 w-9 items-center justify-center rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 transition"
              title="Rời phòng đấu trường"
            >
              <ArrowLeft className="h-4 w-4" />
            </button>
            <div>
              <div className="flex items-center gap-2">
                <span className="h-2 w-2 rounded-full bg-emerald-400 animate-ping" />
                <h1 className="text-sm font-black text-white">{room.roomName}</h1>
                <span className="rounded bg-slate-800 px-2 py-0.5 text-[10px] font-mono text-amber-400">
                  ID: {room.roomId}
                </span>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <span className="flex items-center gap-1.5 rounded-full bg-slate-800 px-3 py-1 text-xs font-bold text-slate-300">
              <Users className="h-3.5 w-3.5 text-amber-400" />
              {Object.keys(room.players).length} Người Trong Phòng
            </span>
          </div>
        </div>
      </header>

      {/* ─── ARENA GAMEPLAY VIEW CONTAINER ─── */}
      <main className="max-w-7xl mx-auto px-4 py-6">
        {room.gameCode === 'OLYMPIA' && (
          <OlympiaArenaView
            state={room}
            onBuzzer={handleBuzzer}
            onSubmitAnswer={handleSubmitAnswer}
            onAction={handleAction}
            onNextStage={handleNextStage}
          />
        )}
        {room.gameCode === 'GOLDEN_BELL' && (
          <GoldenBellArenaView
            state={room}
            onSubmitAnswer={handleSubmitAnswer}
            onAction={handleAction}
            onNextStage={handleNextStage}
          />
        )}
        {room.gameCode === 'LUCKY_WHEEL' && (
          <LuckyWheelArenaView
            state={room}
            onSpin={(sector) => handleAction('SPIN_SECTOR', sector)}
            onPickLetter={(letter) => handleAction('PICK_LETTER', letter)}
            onSolvePhrase={(phrase) => handleSubmitAnswer(phrase)}
          />
        )}
        {room.gameCode === 'UNIVERSITY_CHALLENGE' && (
          <UniversityChallengeView
            state={room}
            onBuzzer={handleBuzzer}
            onSubmitAnswer={handleSubmitAnswer}
            onAction={handleAction}
            onNextStage={handleNextStage}
          />
        )}
        {room.gameCode === 'JEOPARDY' && (
          <JeopardyArenaView
            state={room}
            onBuzzer={handleBuzzer}
            onSubmitAnswer={handleSubmitAnswer}
            onAction={handleAction}
            onNextStage={handleNextStage}
          />
        )}
        {room.gameCode === 'LIGHTNING' && (
          <LightningArenaView
            state={room}
            onSubmitAnswer={handleSubmitAnswer}
            onAction={handleAction}
            onNextStage={handleNextStage}
          />
        )}
        {!['OLYMPIA', 'GOLDEN_BELL', 'LUCKY_WHEEL', 'UNIVERSITY_CHALLENGE', 'JEOPARDY', 'LIGHTNING'].includes(room.gameCode) && (
          <DynamicUniversalArenaView
            state={room}
            onBuzzer={handleBuzzer}
            onSubmitAnswer={handleSubmitAnswer}
            onAction={handleAction}
            onNextStage={handleNextStage}
          />
        )}
      </main>
    </div>
  );
}
export default ArenaRoomPage;
