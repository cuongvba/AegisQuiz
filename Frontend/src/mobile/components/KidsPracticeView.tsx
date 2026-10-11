import React, { useState } from 'react';
import { ArrowLeft, Star, Flame, Volume2, VolumeX, CheckCircle, RotateCcw } from 'lucide-react';
import confetti from 'canvas-confetti';
import { KidsMascot } from './KidsMascot';
import { playKidAudio, speakEnglish } from '../utils/kids-sound';

export interface KidsQuestion {
  id: string;
  wordEn: string;
  vietnameseMeaning: string;
  prompt: string;
  correctKey: string;
  options: Array<{ key: string; text: string; icon: string; color: string }>;
}

interface KidsPracticeViewProps {
  topicName: string;
  onExit: () => void;
}

const DEFAULT_KIDS_QUESTIONS: KidsQuestion[] = [
  {
    id: 'kq1',
    wordEn: 'Cat',
    vietnameseMeaning: 'Con mèo',
    prompt: 'Bé hãy tìm chú Mèo (Cat) nào! 🐱',
    correctKey: 'A',
    options: [
      { key: 'A', text: 'Cat', icon: '🐱', color: 'from-amber-400 to-orange-500' },
      { key: 'B', text: 'Dog', icon: '🐶', color: 'from-blue-400 to-cyan-500' },
      { key: 'C', text: 'Duck', icon: '🦆', color: 'from-emerald-400 to-teal-500' },
      { key: 'D', text: 'Elephant', icon: '🐘', color: 'from-purple-400 to-pink-500' },
    ],
  },
  {
    id: 'kq2',
    wordEn: 'Apple',
    vietnameseMeaning: 'Quả táo',
    prompt: 'Bé yêu hãy chọn quả Táo đỏ (Apple) nhé! 🍎',
    correctKey: 'C',
    options: [
      { key: 'A', text: 'Banana', icon: '🍌', color: 'from-yellow-400 to-amber-500' },
      { key: 'B', text: 'Orange', icon: '🍊', color: 'from-orange-400 to-rose-500' },
      { key: 'C', text: 'Apple', icon: '🍎', color: 'from-red-500 to-rose-600' },
      { key: 'D', text: 'Grape', icon: '🍇', color: 'from-violet-500 to-purple-600' },
    ],
  },
  {
    id: 'kq3',
    wordEn: 'Blue',
    vietnameseMeaning: 'Màu xanh dương',
    prompt: 'Đâu là màu Xanh Dương (Blue) của bầu trời? 🌊',
    correctKey: 'B',
    options: [
      { key: 'A', text: 'Red', icon: '🔴', color: 'from-red-500 to-rose-600' },
      { key: 'B', text: 'Blue', icon: '🔵', color: 'from-blue-500 to-cyan-600' },
      { key: 'C', text: 'Green', icon: '🟢', color: 'from-emerald-500 to-teal-600' },
      { key: 'D', text: 'Yellow', icon: '🟡', color: 'from-amber-400 to-yellow-500' },
    ],
  },
];

export const KidsPracticeView: React.FC<KidsPracticeViewProps> = ({ topicName, onExit }) => {
  const [currentIdx, setCurrentIdx] = useState(0);
  const [stars, setStars] = useState(10);
  const [streak, setStreak] = useState(1);
  const [soundEnabled, setSoundEnabled] = useState(true);
  const [selectedKey, setSelectedKey] = useState<string | null>(null);
  const [isCompleted, setIsCompleted] = useState(false);
  const [mascotMood, setMascotMood] = useState<'idle' | 'happy' | 'thinking' | 'cheering'>('happy');

  const currentQ = DEFAULT_KIDS_QUESTIONS[currentIdx] || DEFAULT_KIDS_QUESTIONS[0];
  const isLast = currentIdx === DEFAULT_KIDS_QUESTIONS.length - 1;

  const handleSelectOption = (optKey: string) => {
    if (selectedKey) return; // Prevent double taps
    setSelectedKey(optKey);

    const isCorrect = optKey === currentQ.correctKey;
    if (isCorrect) {
      if (soundEnabled) playKidAudio('correct');
      setMascotMood('cheering');
      setStars((s) => s + 5);
      setStreak((st) => st + 1);
      confetti({ particleCount: 80, spread: 80, origin: { y: 0.6 } });
      speakEnglish(`Excellent! ${currentQ.wordEn}`);
    } else {
      if (soundEnabled) playKidAudio('wrong');
      setMascotMood('thinking');
      speakEnglish(`Try again! ${currentQ.wordEn}`);
    }

    setTimeout(() => {
      if (isLast) {
        setIsCompleted(true);
        if (soundEnabled) playKidAudio('cheer');
      } else {
        setCurrentIdx((idx) => idx + 1);
        setSelectedKey(null);
        setMascotMood('happy');
      }
    }, 1500);
  };

  const handleRestart = () => {
    setCurrentIdx(0);
    setSelectedKey(null);
    setIsCompleted(false);
    setMascotMood('happy');
  };

  return (
    <div className="flex flex-col min-h-screen bg-gradient-to-b from-purple-950 via-slate-950 to-amber-950/40 text-white p-4">
      {/* Top Gaming Bar */}
      <div className="flex items-center justify-between gap-3 mb-4">
        <button
          onClick={onExit}
          className="w-10 h-10 rounded-2xl bg-white/10 border border-white/20 flex items-center justify-center text-white active:scale-95 transition-transform cursor-pointer"
        >
          <ArrowLeft size={18} />
        </button>

        {/* Stars & Streak Badges */}
        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1.5 px-3 py-1.5 bg-amber-500/20 border border-amber-400/40 rounded-full shadow-inner">
            <Star className="text-amber-400 fill-amber-400 animate-spin" style={{ animationDuration: '6s' }} size={16} />
            <span className="text-xs font-black text-amber-300">{stars} ⭐</span>
          </div>

          <div className="flex items-center gap-1.5 px-3 py-1.5 bg-rose-500/20 border border-rose-400/40 rounded-full shadow-inner">
            <Flame className="text-rose-400 fill-rose-400 animate-pulse" size={16} />
            <span className="text-xs font-black text-rose-300">x{streak} 🔥</span>
          </div>
        </div>

        {/* Audio Toggle */}
        <button
          onClick={() => setSoundEnabled(!soundEnabled)}
          className="w-10 h-10 rounded-2xl bg-white/10 border border-white/20 flex items-center justify-center text-white active:scale-95 transition-transform cursor-pointer"
        >
          {soundEnabled ? <Volume2 size={18} className="text-amber-300" /> : <VolumeX size={18} className="text-slate-400" />}
        </button>
      </div>

      {/* Candy Progress Dots */}
      <div className="flex items-center justify-center gap-2 mb-4">
        {DEFAULT_KIDS_QUESTIONS.map((_, i) => (
          <div
            key={i}
            className={`h-3 rounded-full transition-all duration-300 ${
              i === currentIdx
                ? 'w-8 bg-gradient-to-r from-amber-400 to-pink-500 shadow-md shadow-pink-500/40'
                : i < currentIdx
                ? 'w-3 bg-emerald-400'
                : 'w-3 bg-white/20'
            }`}
          />
        ))}
      </div>

      {/* Mascot Speech Area */}
      <div className="mb-6">
        <KidsMascot
          mood={mascotMood}
          speechText={currentQ.prompt}
          wordToPronounce={currentQ.wordEn}
        />
      </div>

      {/* Chunky 3D Option Grid */}
      <div className="grid grid-cols-2 gap-3.5 flex-1 content-center">
        {currentQ.options.map((opt) => {
          const isSelected = selectedKey === opt.key;
          const isCorrect = opt.key === currentQ.correctKey;
          const showState = isSelected;

          return (
            <button
              key={opt.key}
              onClick={() => handleSelectOption(opt.key)}
              disabled={selectedKey !== null}
              className={`flex flex-col items-center justify-center p-4 rounded-3xl text-center font-black transition-all cursor-pointer relative overflow-hidden select-none active:translate-y-1 ${
                showState
                  ? isCorrect
                    ? 'bg-gradient-to-b from-emerald-400 to-emerald-600 border-4 border-emerald-300 shadow-lg shadow-emerald-500/40 text-white animate-bounce'
                    : 'bg-gradient-to-b from-rose-500 to-rose-700 border-4 border-rose-300 shadow-lg shadow-rose-500/40 text-white'
                  : 'bg-slate-900/90 border-2 border-white/15 hover:border-amber-400/60 shadow-md hover:shadow-amber-500/20'
              }`}
              style={{ minHeight: '110px' }}
            >
              <span className="text-4xl mb-2 drop-shadow-md">{opt.icon}</span>
              <span className="text-sm tracking-wide text-white drop-shadow">
                {opt.text}
              </span>
            </button>
          );
        })}
      </div>

      {/* Completion Modal */}
      {isCompleted && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md animate-fade-in">
          <div className="w-full max-w-sm bg-gradient-to-b from-slate-900 to-purple-950 border-2 border-amber-400/50 rounded-3xl p-6 text-center shadow-2xl">
            <div className="text-5xl mb-3 animate-bounce">🎁</div>
            <h3 className="text-xl font-black text-amber-300 mb-1">
              Bé Giỏi Quá! Super Star!
            </h3>
            <p className="text-xs text-slate-300 mb-4">
              Bé đã nhận được <span className="text-amber-400 font-bold">+{stars} ⭐ Sao Vàng</span> và mở khóa rương kho báu!
            </p>
            <div className="flex gap-2">
              <button
                onClick={handleRestart}
                className="flex-1 py-3 rounded-2xl bg-white/10 hover:bg-white/20 text-white font-bold text-xs flex items-center justify-center gap-1.5 transition-colors cursor-pointer"
              >
                <RotateCcw size={14} /> Chơi Lại
              </button>
              <button
                onClick={onExit}
                className="flex-1 py-3 rounded-2xl bg-gradient-to-r from-amber-400 to-pink-500 text-slate-950 font-black text-xs shadow-lg transition-transform active:scale-95 cursor-pointer"
              >
                Nhận Thưởng 🎉
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
