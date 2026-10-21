import { useRef, useState, useCallback, useEffect } from 'react';

/**
 * [Cognitive Studio] Hook đồng bộ tọa độ cuộn 2 khung hình thời gian thực (60 FPS)
 * Sử dụng requestAnimationFrame và lock cờ để triệt tiêu hiện tượng lag/vòng lặp cuộn.
 */
export function useStudioScrollSync() {
  const [isSyncEnabled, setIsSyncEnabled] = useState(true);
  const sourceRef = useRef<HTMLDivElement>(null);
  const extractedRef = useRef<HTMLDivElement>(null);
  const isScrollingRef = useRef<'source' | 'extracted' | null>(null);
  const rafIdRef = useRef<number | null>(null);

  const toggleSync = useCallback(() => {
    setIsSyncEnabled((prev) => !prev);
  }, []);

  const handleSourceScroll = useCallback(() => {
    if (!isSyncEnabled || isScrollingRef.current === 'extracted') return;
    isScrollingRef.current = 'source';

    if (rafIdRef.current) cancelAnimationFrame(rafIdRef.current);
    rafIdRef.current = requestAnimationFrame(() => {
      const source = sourceRef.current;
      const extracted = extractedRef.current;
      if (source && extracted) {
        const sourceScrollable = source.scrollHeight - source.clientHeight;
        const extractedScrollable = extracted.scrollHeight - extracted.clientHeight;
        if (sourceScrollable > 0 && extractedScrollable > 0) {
          const ratio = source.scrollTop / sourceScrollable;
          extracted.scrollTop = ratio * extractedScrollable;
        }
      }
      isScrollingRef.current = null;
    });
  }, [isSyncEnabled]);

  const handleExtractedScroll = useCallback(() => {
    if (!isSyncEnabled || isScrollingRef.current === 'source') return;
    isScrollingRef.current = 'extracted';

    if (rafIdRef.current) cancelAnimationFrame(rafIdRef.current);
    rafIdRef.current = requestAnimationFrame(() => {
      const source = sourceRef.current;
      const extracted = extractedRef.current;
      if (source && extracted) {
        const sourceScrollable = source.scrollHeight - source.clientHeight;
        const extractedScrollable = extracted.scrollHeight - extracted.clientHeight;
        if (sourceScrollable > 0 && extractedScrollable > 0) {
          const ratio = extracted.scrollTop / extractedScrollable;
          source.scrollTop = ratio * sourceScrollable;
        }
      }
      isScrollingRef.current = null;
    });
  }, [isSyncEnabled]);

  useEffect(() => {
    return () => {
      if (rafIdRef.current) cancelAnimationFrame(rafIdRef.current);
    };
  }, []);

  return {
    sourceRef,
    extractedRef,
    isSyncEnabled,
    toggleSync,
    handleSourceScroll,
    handleExtractedScroll,
  };
}
