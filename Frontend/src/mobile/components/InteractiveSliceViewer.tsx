import React, { useState, useEffect, useCallback } from 'react';
import { X, ChevronLeft, ChevronRight, Volume2, Sparkles, Star, Award, CheckCircle2 } from 'lucide-react';
import confetti from 'canvas-confetti';
import { LessonSlice } from '../types/slice-lesson';
import { KidsMascot } from './KidsMascot';
import { playKidAudio, speakEnglish } from '../utils/kids-sound';

interface InteractiveSliceViewerProps {
  slices: LessonSlice[];
  topicName?: string;
  onClose: () => void;
  onComplete?: () => void;
}

export const InteractiveSliceViewer: React.FC<InteractiveSliceViewerProps> = ({
  slices,
  topicName = 'Bài Giảng Vi Mô Tương Tác',
  onClose,
  onComplete,
}) => {
  const [currentIdx, setCurrentIdx] = useState(0);
  const [isCompleted, setIsCompleted] = useState(false);

  const total = slices.length;
  const currentSlice = slices[currentIdx] || slices[0];

  const handleNext = useCallback(() => {
    if (currentIdx < total - 1) {
      setCurrentIdx((prev) => prev + 1);
      playKidAudio('pop');
    } else {
      setIsCompleted(true);
      playKidAudio('cheer');
      confetti({ particleCount: 100, spread: 80, origin: { y: 0.5 } });
      if (onComplete) onComplete();
    }
  }, [currentIdx, total, onComplete]);

  const handlePrev = useCallback(() => {
    if (currentIdx > 0) {
      setCurrentIdx((prev) => prev - 1);
      playKidAudio('pop');
    }
  }, [currentIdx]);

  // Read slice narration when switching slides
  useEffect(() => {
    if (currentSlice?.speechText) {
      speakEnglish(currentSlice.speechText);
    }
  }, [currentIdx, currentSlice?.speechText]);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-3 md:p-6 bg-slate-950/90 backdrop-blur-xl animate-fade-in text-white">
      <div className="w-full max-w-md h-[90vh] max-h-[750px] bg-gradient-to-b from-slate-900 via-slate-950 to-indigo-950 border border-indigo-500/30 rounded-3xl p-5 flex flex-col shadow-2xl relative overflow-hidden">
        {/* Decorative Ambient Glow */}
        <div className="absolute -top-24 -right-24 w-52 h-52 bg-pink-500/20 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-24 -left-24 w-52 h-52 bg-amber-500/20 rounded-full blur-3xl pointer-events-none" />

        {/* 1. Top Story Progress Bars (Instagram/TikTok style) */}
        <div className="flex items-center gap-1.5 mb-4 z-10">
          {slices.map((s, idx) => (
            <div
              key={s.id || idx}
              onClick={() => setCurrentIdx(idx)}
              className="flex-1 h-1.5 rounded-full overflow-hidden bg-white/20 cursor-pointer"
            >
              <div
                className={`h-full transition-all duration-300 ${
                  idx === currentIdx
                    ? 'w-full bg-gradient-to-r from-amber-400 to-pink-500 shadow-sm'
                    : idx < currentIdx
                    ? 'w-full bg-emerald-400'
                    : 'w-0'
                }`}
              />
            </div>
          ))}
        </div>

        {/* 2. Header: Title & Close Button */}
        <div className="flex items-center justify-between mb-4 z-10">
          <div>
            <span className="text-[10px] uppercase font-black tracking-widest text-amber-400 flex items-center gap-1">
              <Sparkles size={11} /> Micro-Lesson Story
            </span>
            <h4 className="text-xs font-bold text-slate-300 truncate max-w-[240px]">
              {topicName}
            </h4>
          </div>

          <button
            onClick={onClose}
            className="w-8 h-8 rounded-full bg-white/10 hover:bg-white/20 flex items-center justify-center text-slate-300 hover:text-white transition-colors cursor-pointer"
            aria-label="Đóng bài giảng"
          >
            <X size={16} />
          </button>
        </div>

        {/* 3. Main Slide Content Area */}
        <div className="flex-1 flex flex-col justify-between py-2 z-10 overflow-y-auto pr-1">
          {/* Badge & Step Icon */}
          <div className="text-center mb-3">
            <span className="inline-block px-3 py-1 rounded-full text-[11px] font-black uppercase tracking-wider bg-white/10 border border-white/20 text-white mb-3">
              {currentSlice.badgeLabel}
            </span>
            <div className="text-6xl mb-3 drop-shadow-lg animate-bounce select-none">
              {currentSlice.visualIcon}
            </div>
            <h3 className="text-lg font-black text-white drop-shadow leading-snug">
              {currentSlice.title}
            </h3>
          </div>

          {/* Formatted Content Card */}
          <div className="bg-slate-900/80 border border-white/15 rounded-2xl p-4 shadow-inner mb-4 backdrop-blur-md">
            <p className="text-xs text-slate-200 leading-relaxed font-medium">
              {currentSlice.content}
            </p>
          </div>

          {/* Mascot Virtual Lecturer */}
          <KidsMascot
            mood={currentIdx === total - 1 ? 'cheering' : 'happy'}
            speechText={currentSlice.speechText}
          />
        </div>

        {/* 4. Bottom Navigation Controls */}
        <div className="flex items-center gap-3 pt-3 border-t border-white/10 z-10">
          <button
            onClick={handlePrev}
            disabled={currentIdx === 0}
            className="px-4 py-2.5 rounded-xl bg-white/10 hover:bg-white/20 disabled:opacity-30 disabled:pointer-events-none text-xs font-bold transition-all flex items-center gap-1 cursor-pointer"
          >
            <ChevronLeft size={16} /> Trước
          </button>

          <button
            onClick={handleNext}
            className="flex-1 py-2.5 rounded-xl bg-gradient-to-r from-amber-400 via-pink-500 to-purple-600 hover:opacity-95 text-slate-950 font-black text-xs shadow-lg flex items-center justify-center gap-1.5 transition-transform active:scale-95 cursor-pointer"
          >
            {currentIdx === total - 1 ? (
              <>
                <Award size={15} /> Hoàn Tất & Nhận Sao ⭐
              </>
            ) : (
              <>
                Tiếp Theo <ChevronRight size={16} />
              </>
            )}
          </button>
        </div>

        {/* 5. Completion Modal Overlay */}
        {isCompleted && (
          <div className="absolute inset-0 z-20 flex flex-col items-center justify-center p-6 bg-slate-950/95 backdrop-blur-md text-center animate-fade-in">
            <div className="text-6xl mb-3 animate-bounce">🏆</div>
            <h2 className="text-xl font-black text-amber-300 mb-1">
              Xuất Sắc! Bé Đã Hiểu Bài!
            </h2>
            <p className="text-xs text-slate-300 mb-6 max-w-xs">
              Bé đã làm chủ toàn bộ bài giảng và nhận thêm <span className="text-amber-400 font-bold">+10 ⭐ Sao Vàng</span> vào kho báu!
            </p>
            <button
              onClick={onClose}
              className="w-full py-3.5 rounded-2xl bg-gradient-to-r from-amber-400 to-pink-500 text-slate-950 font-black text-sm shadow-xl active:scale-95 transition-transform cursor-pointer"
            >
              Tiếp Tục Luyện Thi Ngay 🚀
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
