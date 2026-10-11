import { useState, useEffect } from 'react';

/**
 * ⏱️ Đồng hồ bấm giờ thời gian thực định dạng [HH:mm:ss SSS]
 * Cập nhật mượt mà qua requestAnimationFrame cho trải nghiệm AI Engine đỉnh cao.
 */
export function ProcessingStopwatch() {
  const [elapsedMs, setElapsedMs] = useState(0);

  useEffect(() => {
    const startTime = performance.now();
    let animId: number;

    const tick = () => {
      setElapsedMs(performance.now() - startTime);
      animId = requestAnimationFrame(tick);
    };

    animId = requestAnimationFrame(tick);
    return () => {
      if (animId) cancelAnimationFrame(animId);
    };
  }, []);

  const totalMs = Math.floor(elapsedMs);
  const hours = Math.floor(totalMs / 3600000);
  const minutes = Math.floor((totalMs % 3600000) / 60000);
  const seconds = Math.floor((totalMs % 60000) / 1000);
  const ms = totalMs % 1000;

  const pad2 = (n: number) => String(n).padStart(2, '0');
  const pad3 = (n: number) => String(n).padStart(3, '0');

  return (
    <span className="font-mono tabular-nums text-amber-300 bg-amber-950/50 border border-amber-500/30 px-2.5 py-0.5 rounded-lg shadow-inner font-bold tracking-wider text-base inline-block">
      [{pad2(hours)}:{pad2(minutes)}:{pad2(seconds)} {pad3(ms)}]
    </span>
  );
}
