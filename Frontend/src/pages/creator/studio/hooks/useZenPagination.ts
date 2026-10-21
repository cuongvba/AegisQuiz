import { useCallback, useEffect, useMemo, useState } from 'react';

export const ZEN_PAGE_SIZES = [1, 5, 10, 20] as const;

/** Phân trang cho danh sách câu hỏi: cuộn lên đầu khi đổi trang, hỗ trợ phím ← / →. */
export function useZenPagination(total: number, initialPageSize = 5) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState<number>(initialPageSize);

  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const safePage = Math.min(page, totalPages);

  const goTo = useCallback(
    (next: number) => {
      setPage(Math.min(Math.max(1, next), totalPages));
      window.scrollTo({ top: 0, behavior: 'smooth' });
    },
    [totalPages],
  );

  const changePageSize = useCallback((size: number) => {
    setPageSize(size);
    setPage(1);
  }, []);

  const range = useMemo(() => {
    const start = (safePage - 1) * pageSize;
    return { start, end: Math.min(total, start + pageSize) };
  }, [safePage, pageSize, total]);

  /** Cửa sổ số trang hiển thị: 1 … 4 5 [6] 7 8 … N */
  const pageWindow = useMemo(() => {
    const pages = new Set<number>([1, totalPages]);
    for (let p = safePage - 2; p <= safePage + 2; p++) if (p >= 1 && p <= totalPages) pages.add(p);
    return [...pages].sort((a, b) => a - b);
  }, [safePage, totalPages]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const tag = (e.target as HTMLElement | null)?.tagName;
      if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;
      if (e.key === 'ArrowRight') goTo(safePage + 1);
      if (e.key === 'ArrowLeft') goTo(safePage - 1);
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [goTo, safePage]);

  return { page: safePage, pageSize, totalPages, range, pageWindow, goTo, changePageSize };
}
