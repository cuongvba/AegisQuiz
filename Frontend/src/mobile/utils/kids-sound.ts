// Web Audio API Synthesizer & Speech Synthesis for Kids Gamified App
export function playKidAudio(type: 'correct' | 'wrong' | 'cheer' | 'pop') {
  if (typeof window === 'undefined') return;
  try {
    const AudioCtx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
    if (!AudioCtx) return;
    const ctx = new AudioCtx();

    const note = (freq: number, start: number, dur: number, wave: OscillatorType = 'triangle', vol = 0.25) => {
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.type = wave;
      osc.frequency.value = freq;
      const t0 = ctx.currentTime + start;
      gain.gain.setValueAtTime(0, t0);
      gain.gain.linearRampToValueAtTime(vol, t0 + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.001, t0 + dur);
      osc.start(t0);
      osc.stop(t0 + dur + 0.05);
    };

    if (type === 'correct') {
      // Fanfare: C5 -> E5 -> G5 -> C6
      note(523.25, 0.00, 0.12, 'triangle', 0.22);
      note(659.25, 0.10, 0.12, 'triangle', 0.22);
      note(783.99, 0.20, 0.12, 'triangle', 0.22);
      note(1046.50, 0.30, 0.35, 'sine', 0.28);
    } else if (type === 'wrong') {
      // Gentle bouncy boing (Zero punishment)
      note(320, 0.00, 0.15, 'sine', 0.20);
      note(260, 0.12, 0.25, 'sine', 0.15);
    } else if (type === 'cheer') {
      // Magic star chime
      note(880, 0.00, 0.10, 'sine', 0.20);
      note(1174, 0.08, 0.12, 'sine', 0.22);
      note(1396, 0.16, 0.14, 'sine', 0.24);
      note(1760, 0.24, 0.30, 'sine', 0.25);
    } else if (type === 'pop') {
      note(440, 0.00, 0.06, 'triangle', 0.18);
    }
  } catch {
    // Ignore audio errors on unsupported browsers
  }
}

export function speakEnglish(text: string) {
  if (typeof window === 'undefined' || !('speechSynthesis' in window)) return;
  try {
    window.speechSynthesis.cancel(); // Stop any pending speech
    const clean = text.replace(/<[^>]*>/g, '').trim();
    const utterance = new SpeechSynthesisUtterance(clean);
    utterance.lang = 'en-US';
    utterance.rate = 0.85; // Slightly slower for kids
    utterance.pitch = 1.15; // Friendly warm pitch
    window.speechSynthesis.speak(utterance);
  } catch {
    // Ignore speech errors
  }
}
