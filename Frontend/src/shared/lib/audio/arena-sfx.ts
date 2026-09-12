/**
 * AegisQuiz — Arena Audio SFX Library
 * =====================================
 * Web Audio API synthesizer tách ra từ arena.service.ts.
 * Đây là pure utility function — không có React dependency.
 *
 * FSD Layer: shared/lib/audio/
 */

export type ArenaSfxType =
  | 'buzzer'      // Chuông bấm giật quyền trả lời
  | 'wheel_tick'  // Tiếng gõ nan nón (Chiếc Nón Kỳ Diệu)
  | 'victory'     // Tiếng chuông chiến thắng
  | 'drop'        // Tiếng tụt dốc (Nhanh Như Chớp)
  | 'clock'       // Đếm ngược tích tắc
  | 'correct'     // Đúng câu hỏi
  | 'wrong';      // Sai câu hỏi

/**
 * Phát âm thanh hiệu ứng cho Đấu trường Gameshow.
 * Web Audio API — không cần external audio files.
 * Safe: không throw nếu browser không hỗ trợ.
 */
export function playArenaSfx(type: ArenaSfxType): void {
  try {
    const AudioCtx =
      window.AudioContext ||
      (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    if (!AudioCtx) return;

    const ctx  = new AudioCtx();
    const now  = ctx.currentTime;
    const osc  = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.connect(gain);
    gain.connect(ctx.destination);

    switch (type) {
      case 'buzzer':
        osc.type = 'square';
        osc.frequency.setValueAtTime(880,  now);
        osc.frequency.setValueAtTime(1760, now + 0.08);
        gain.gain.setValueAtTime(0.3,  now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.35);
        osc.start(now); osc.stop(now + 0.35);
        break;

      case 'wheel_tick':
        osc.type = 'triangle';
        osc.frequency.setValueAtTime(1200, now);
        gain.gain.setValueAtTime(0.15, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.04);
        osc.start(now); osc.stop(now + 0.04);
        break;

      case 'victory':
        osc.type = 'sine';
        osc.frequency.setValueAtTime(523.25, now);        // C5
        osc.frequency.setValueAtTime(659.25, now + 0.1);  // E5
        osc.frequency.setValueAtTime(783.99, now + 0.2);  // G5
        osc.frequency.setValueAtTime(1046.5, now + 0.3);  // C6
        gain.gain.setValueAtTime(0.25, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.8);
        osc.start(now); osc.stop(now + 0.8);
        break;

      case 'drop':
        osc.type = 'sawtooth';
        osc.frequency.setValueAtTime(440, now);
        osc.frequency.exponentialRampToValueAtTime(110, now + 0.4);
        gain.gain.setValueAtTime(0.25, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.42);
        osc.start(now); osc.stop(now + 0.42);
        break;

      case 'clock':
        osc.type = 'sine';
        osc.frequency.setValueAtTime(1000, now);
        gain.gain.setValueAtTime(0.1,  now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.05);
        osc.start(now); osc.stop(now + 0.05);
        break;

      case 'correct':
        osc.type = 'sine';
        osc.frequency.setValueAtTime(659.25, now);        // E5
        osc.frequency.setValueAtTime(783.99, now + 0.1);  // G5
        gain.gain.setValueAtTime(0.2,  now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.4);
        osc.start(now); osc.stop(now + 0.4);
        break;

      case 'wrong':
        osc.type = 'square';
        osc.frequency.setValueAtTime(220, now);
        osc.frequency.setValueAtTime(185, now + 0.1);
        gain.gain.setValueAtTime(0.2,  now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.3);
        osc.start(now); osc.stop(now + 0.3);
        break;
    }
  } catch {
    // Safe ignore — browser audio policy / không hỗ trợ
  }
}

/**
 * Preload AudioContext để tránh browser autoplay policy.
 * Gọi trong event handler đầu tiên của user.
 */
export function primeAudioContext(): void {
  try {
    const AudioCtx =
      window.AudioContext ||
      (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    if (!AudioCtx) return;
    const ctx = new AudioCtx();
    // Tạo silent buffer để unlock audio
    const buf = ctx.createBuffer(1, 1, 22050);
    const src = ctx.createBufferSource();
    src.buffer = buf;
    src.connect(ctx.destination);
    src.start(0);
    src.stop(ctx.currentTime + 0.001);
  } catch { /* ignore */ }
}
