import React from 'react';
import { Volume2, Sparkles } from 'lucide-react';
import { speakEnglish } from '../utils/kids-sound';

interface KidsMascotProps {
  mood: 'happy' | 'thinking' | 'cheering' | 'idle';
  speechText?: string;
  wordToPronounce?: string;
}

export const KidsMascot: React.FC<KidsMascotProps> = ({
  mood,
  speechText = 'Bé hãy chọn hình đúng nhé! ✨',
  wordToPronounce,
}) => {
  const handleSpeak = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (wordToPronounce) {
      speakEnglish(wordToPronounce);
    }
  };

  const getMascotEmoji = () => {
    switch (mood) {
      case 'cheering': return '🎉';
      case 'happy': return '⭐';
      case 'thinking': return '💭';
      default: return '✨';
    }
  };

  return (
    <div className="flex items-center gap-3 bg-gradient-to-r from-amber-500/20 via-pink-500/20 to-purple-500/20 border-2 border-amber-400/40 rounded-3xl p-3.5 shadow-lg relative overflow-hidden backdrop-blur-md">
      {/* Decorative Sparkle */}
      <Sparkles className="absolute -top-1 -right-1 text-amber-300/40 animate-pulse" size={28} />

      {/* Mascot Avatar with Animation */}
      <div 
        onClick={handleSpeak}
        className={`w-14 h-14 rounded-2xl bg-gradient-to-tr from-amber-400 via-orange-400 to-pink-500 flex items-center justify-center text-3xl shadow-md border-2 border-white/80 cursor-pointer select-none transition-transform active:scale-95 ${
          mood === 'cheering' ? 'animate-bounce' : mood === 'happy' ? 'animate-pulse' : ''
        }`}
        title="Bấm vào Bé Rồng để nghe phát âm!"
      >
        🐲
      </div>

      {/* Speech Bubble */}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-1.5 mb-0.5">
          <span className="text-[11px] font-black uppercase text-amber-300 tracking-wider">
            Bé Rồng Aegis
          </span>
          <span className="text-xs">{getMascotEmoji()}</span>
        </div>
        <p className="text-xs font-bold text-white line-clamp-2 leading-relaxed">
          {speechText}
        </p>
      </div>

      {/* Pronunciation Audio Button */}
      {wordToPronounce && (
        <button
          onClick={handleSpeak}
          className="flex-shrink-0 w-10 h-10 rounded-2xl bg-gradient-to-r from-pink-500 to-rose-500 flex items-center justify-center text-white shadow-md border border-white/40 active:scale-90 transition-transform cursor-pointer"
          aria-label="Phát âm từ vựng"
          title="Nghe phát âm"
        >
          <Volume2 size={18} className="animate-pulse" />
        </button>
      )}
    </div>
  );
};
