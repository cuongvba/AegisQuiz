import React, { useState, useEffect, useRef } from 'react';
import { Sparkles, Trophy, RotateCcw, AlertTriangle, CheckCircle2 } from 'lucide-react';
import type { ArenaRoomState, ArenaPlayer } from '@/types/arena';
import { playArenaSfx } from '@/services/arena.service';

interface LuckyWheelArenaViewProps {
  state: ArenaRoomState;
  onSpin: (sector: string) => void;
  onPickLetter: (letter: string) => void;
  onSolvePhrase: (phrase: string) => void;
}

const SECTORS = [
  { label: '500', color: '#0284c7' },
  { label: '300', color: '#10b981' },
  { label: 'MẤT LƯỢT', color: '#ef4444' },
  { label: '800', color: '#8b5cf6' },
  { label: 'MAY MẮN', color: '#f59e0b' },
  { label: '200', color: '#06b6d4' },
  { label: 'CHIA ĐÔI', color: '#64748b' },
  { label: '1000', color: '#ec4899' },
  { label: '400', color: '#14b8a6' },
  { label: 'NHÂN ĐÔI', color: '#eab308' },
];

const ALPHABET = 'AĂÂBCDĐEÊGHIKLMNOÔƠPQRSTUƯVXY'.split('');

export function LuckyWheelArenaView({ state, onSpin, onPickLetter, onSolvePhrase }: LuckyWheelArenaViewProps) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const [isSpinning, setIsSpinning] = useState(false);
  const [currentAngle, setCurrentAngle] = useState(0);
  const [landedSector, setLandedSector] = useState<string>('500');
  const [selectedLetter, setSelectedLetter] = useState('');
  const [fullPhraseGuess, setFullPhraseGuess] = useState('');
  const [showSolveModal, setShowSolveModal] = useState(false);

  const targetWord = (state.customData?.targetWord || 'TRI THUC LA SUC MANH').toUpperCase();
  const revealed = (state.customData?.revealedLetters as string[]) || [' '];
  const rawPlayers = state.players || state.Players || {};
  const players: ArenaPlayer[] = Object.values(rawPlayers);

  // Vẽ nón lên Canvas
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const size = canvas.width;
    const center = size / 2;
    const radius = center - 10;
    const arc = (2 * Math.PI) / SECTORS.length;

    ctx.clearRect(0, 0, size, size);

    // Vẽ từng nan nón
    SECTORS.forEach((sector, i) => {
      const angle = currentAngle + i * arc;
      ctx.beginPath();
      ctx.fillStyle = sector.color;
      ctx.moveTo(center, center);
      ctx.arc(center, center, radius, angle, angle + arc);
      ctx.lineTo(center, center);
      ctx.fill();
      ctx.stroke();

      // Vẽ text nan nón
      ctx.save();
      ctx.translate(center, center);
      ctx.rotate(angle + arc / 2);
      ctx.textAlign = 'right';
      ctx.fillStyle = '#ffffff';
      ctx.font = 'bold 13px Inter, sans-serif';
      ctx.shadowColor = 'rgba(0,0,0,0.5)';
      ctx.shadowBlur = 4;
      ctx.fillText(sector.label, radius - 15, 5);
      ctx.restore();
    });

    // Vẽ tâm trục nón
    ctx.beginPath();
    ctx.arc(center, center, 24, 0, 2 * Math.PI);
    ctx.fillStyle = '#1e293b';
    ctx.fill();
    ctx.lineWidth = 4;
    ctx.strokeStyle = '#f59e0b';
    ctx.stroke();

    ctx.beginPath();
    ctx.arc(center, center, 8, 0, 2 * Math.PI);
    ctx.fillStyle = '#fbbf24';
    ctx.fill();
  }, [currentAngle]);

  // Hiệu ứng quay nón vật lý
  const spinTheWheel = () => {
    if (isSpinning) return;
    setIsSpinning(true);

    const extraRounds = 5 + Math.floor(Math.random() * 5);
    const randomSectorIdx = Math.floor(Math.random() * SECTORS.length);
    const arc = (2 * Math.PI) / SECTORS.length;
    const targetAngle = extraRounds * 2 * Math.PI + (SECTORS.length - randomSectorIdx) * arc;

    const startTime = performance.now();
    const duration = 4000; // 4s quay

    const animate = (now: number) => {
      const elapsed = now - startTime;
      const progress = Math.min(elapsed / duration, 1);
      // Ease out cubic
      const easeOut = 1 - Math.pow(1 - progress, 3);
      setCurrentAngle(easeOut * targetAngle);

      if (progress < 1) {
        if (Math.floor(elapsed / 120) % 2 === 0) {
          playArenaSfx('wheel_tick');
        }
        requestAnimationFrame(animate);
      } else {
        setIsSpinning(false);
        const resultSector = SECTORS[randomSectorIdx].label;
        setLandedSector(resultSector);
        onSpin(resultSector);
        if (resultSector === 'MẤT LƯỢT') {
          playArenaSfx('drop');
        } else {
          playArenaSfx('victory');
        }
      }
    };

    requestAnimationFrame(animate);
  };

  const handlePickLetter = (letter: string) => {
    if (revealed.includes(letter)) return;
    setSelectedLetter(letter);
    onPickLetter(letter);
  };

  const handleSolveSubmit = () => {
    if (fullPhraseGuess.trim()) {
      onSolvePhrase(fullPhraseGuess.trim().toUpperCase());
      setShowSolveModal(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* ─── STAGE HEADER ─── */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-slate-900/90 p-4 border border-teal-500/30 text-white shadow-xl">
        <div className="flex items-center gap-3">
          <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-teal-500 text-2xl font-black shadow-lg shadow-teal-500/30">
            🎡
          </span>
          <div>
            <span className="text-xs uppercase tracking-widest text-teal-400 font-bold">Chiếc Nón Kỳ Diệu</span>
            <h2 className="text-lg font-black text-white">Vòng Quay May Mắn & Giải Mã Ô Chữ</h2>
          </div>
        </div>

        <button
          onClick={() => setShowSolveModal(true)}
          className="rounded-xl bg-gradient-to-r from-amber-400 to-yellow-500 hover:from-amber-500 hover:to-yellow-600 px-5 py-2.5 text-xs font-black text-slate-950 shadow-lg shadow-amber-400/20 transition hover:scale-105 active:scale-95"
        >
          ⭐ ĐOÁN TOÀN BỘ Ô CHỮ
        </button>
      </div>

      {/* ─── BẢNG Ô CHỮ CHỮ CÁI LẬT MỞ 3D ─── */}
      <div className="rounded-3xl border-2 border-teal-500/30 bg-slate-900 p-6 text-white shadow-2xl">
        <div className="text-center mb-4">
          <span className="text-xs font-bold uppercase tracking-wider text-teal-400">
            Chủ Đề: Châm Ngôn & Triết Lý Cuộc Sống ({targetWord.replace(/\s+/g, '').length} Chữ Cái)
          </span>
        </div>

        {/* Word Tiles */}
        <div className="flex flex-wrap justify-center gap-2 max-w-3xl mx-auto py-4">
          {targetWord.split('').map((char: string, idx: number) => {
            if (char === ' ') {
              return <div key={idx} className="w-4" />;
            }
            const isRevealed = revealed.includes(char);
            return (
              <div
                key={idx}
                className={`flex h-12 w-10 sm:h-14 sm:w-12 items-center justify-center rounded-xl border-2 font-black text-xl sm:text-2xl shadow-md transition-all duration-500 ${
                  isRevealed
                    ? 'border-teal-400 bg-white text-slate-950 shadow-teal-500/50 rotate-0'
                    : 'border-slate-700 bg-slate-800 text-transparent'
                }`}
              >
                {isRevealed ? char : '?'}
              </div>
            );
          })}
        </div>
      </div>

      {/* ─── WHEEL & KEYBOARD INTERACTION ─── */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Canvas Physical Wheel */}
        <div className="flex flex-col items-center justify-center rounded-3xl border-2 border-slate-700 bg-slate-900 p-6 text-center shadow-xl">
          <div className="relative">
            {/* Kim chỉ ô nón */}
            <div className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-2 z-10 w-0 h-0 border-x-8 border-x-transparent border-t-[16px] border-t-rose-500 drop-shadow-md" />
            <canvas ref={canvasRef} width={280} height={280} className="rounded-full shadow-2xl" />
          </div>

          <div className="mt-4">
            <span className="text-xs text-slate-400">Ô quay hiện tại:</span>
            <p className="text-2xl font-black text-amber-400 tracking-tight">{landedSector}</p>
          </div>

          <button
            disabled={isSpinning}
            onClick={spinTheWheel}
            className={`mt-4 rounded-2xl px-8 py-3.5 text-sm font-black uppercase tracking-wider text-white shadow-lg transition ${
              isSpinning
                ? 'bg-slate-700 cursor-not-allowed'
                : 'bg-gradient-to-r from-teal-500 to-cyan-500 hover:scale-105 active:scale-95 shadow-cyan-500/30'
            }`}
          >
            {isSpinning ? 'Đang Quay Nón...' : '🎡 QUAY NÓN (LỰC VẬT LÝ)'}
          </button>
        </div>

        {/* Alphabet Letter Selector */}
        <div className="rounded-3xl border-2 border-slate-700 bg-slate-900 p-6 shadow-xl text-white">
          <h3 className="text-sm font-black text-cyan-300 uppercase tracking-wider mb-3">
            Chọn Chữ Cái Để Lật Ô:
          </h3>
          <div className="grid grid-cols-6 sm:grid-cols-8 gap-2">
            {ALPHABET.map((letter) => {
              const isUsed = revealed.includes(letter);
              return (
                <button
                  key={letter}
                  disabled={isUsed || isSpinning}
                  onClick={() => handlePickLetter(letter)}
                  className={`flex h-10 items-center justify-center rounded-xl font-black text-sm transition ${
                    isUsed
                      ? 'bg-slate-800 text-slate-600 cursor-not-allowed'
                      : 'border border-slate-700 bg-slate-800/80 text-white hover:border-cyan-400 hover:bg-cyan-500/20 hover:scale-110 active:scale-95'
                  }`}
                >
                  {letter}
                </button>
              );
            })}
          </div>

          {/* Player Score Board */}
          <div className="mt-6 border-t border-slate-800 pt-4">
            <h4 className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-2">
              Bảng Điểm Thí Sinh
            </h4>
            <div className="space-y-2">
              {players.map((p: any, idx: number) => (
                <div key={p.id || p.Id || idx} className="flex items-center justify-between rounded-xl bg-slate-800 p-3">
                  <div className="flex items-center gap-2">
                    <span className="text-lg">{p.avatar || p.Avatar || '🎓'}</span>
                    <span className="font-bold text-sm text-white">{p.name || p.Name}</span>
                  </div>
                  <span className="font-black text-amber-400">{p.score ?? p.Score ?? 0} điểm</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* ─── SOLVE PHRASE MODAL ─── */}
      {showSolveModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm p-4">
          <div className="w-full max-w-md rounded-3xl border-2 border-amber-400 bg-slate-900 p-6 text-white shadow-2xl">
            <h3 className="text-lg font-black text-amber-400">⭐ Giải Toàn Bộ Ô Chữ</h3>
            <p className="mt-1 text-xs text-slate-300">
              Đoán chính xác cụm từ khóa bí ẩn để giành ngay 2,000 điểm và chiến thắng chung cuộc!
            </p>
            <input
              type="text"
              autoFocus
              value={fullPhraseGuess}
              onChange={(e) => setFullPhraseGuess(e.target.value)}
              placeholder="Nhập toàn bộ ô chữ..."
              className="mt-4 w-full rounded-2xl border-2 border-amber-400 bg-slate-800 p-4 font-black uppercase tracking-wider text-center text-white placeholder:text-slate-600 focus:outline-none"
            />
            <div className="mt-6 flex justify-end gap-3">
              <button
                onClick={() => setShowSolveModal(false)}
                className="rounded-xl px-4 py-2 font-bold text-slate-400 hover:text-white"
              >
                Hủy
              </button>
              <button
                onClick={handleSolveSubmit}
                className="rounded-xl bg-amber-400 hover:bg-amber-500 px-6 py-2.5 font-black text-slate-950 transition"
              >
                Xác Nhận Giải Mã
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
