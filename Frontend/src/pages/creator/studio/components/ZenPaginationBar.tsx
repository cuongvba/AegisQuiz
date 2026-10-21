import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ChevronRight, Play } from 'lucide-react';
import { ZEN_PAGE_SIZES } from '../hooks/useZenPagination';

interface ZenPaginationBarProps {
  page: number;
  totalPages: number;
  pageSize: number;
  total: number;
  rangeStart: number;
  rangeEnd: number;
  pageWindow: number[];
  editedCount: number;
  isLaunching: boolean;
  onGoTo: (page: number) => void;
  onPageSize: (size: number) => void;
  onLaunch: () => void;
}

/** Thanh điều khiển nổi (sticky) luôn hiển thị ở đáy màn hình — Creator không phải cuộn tìm nút. */
export const ZenPaginationBar: React.FC<ZenPaginationBarProps> = ({
  page, totalPages, pageSize, total, rangeStart, rangeEnd, pageWindow, editedCount, isLaunching,
  onGoTo, onPageSize, onLaunch,
}) => {
  const { t } = useTranslation('creator');
  const [jump, setJump] = useState('');

  const submitJump = (e: React.FormEvent) => {
    e.preventDefault();
    const q = parseInt(jump, 10);
    if (!Number.isNaN(q) && q >= 1 && q <= total) {
      onGoTo(Math.ceil(q / pageSize));
      setJump('');
    }
  };

  const navBtn = 'w-8 h-8 inline-flex items-center justify-center rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 disabled:opacity-30 disabled:cursor-not-allowed';

  return (
    <div className="fixed bottom-3 inset-x-3 z-40 flex justify-center pointer-events-none">
      <nav
        aria-label={t('pagination.label')}
        className="pointer-events-auto flex flex-wrap items-center justify-center gap-x-4 gap-y-2 px-4 py-2.5 rounded-2xl bg-slate-900/95 backdrop-blur-xl border border-indigo-500/40 shadow-2xl shadow-black/60 max-w-full"
      >
        <div className="flex items-center gap-1">
          <button className={navBtn} disabled={page <= 1} onClick={() => onGoTo(page - 1)} aria-label={t('pagination.prev')}>
            <ChevronLeft className="w-4 h-4 rtl:rotate-180" />
          </button>
          {pageWindow.map((p, i) => (
            <React.Fragment key={p}>
              {i > 0 && p - pageWindow[i - 1] > 1 && <span className="text-slate-500 text-xs px-0.5">…</span>}
              <button
                onClick={() => onGoTo(p)}
                aria-current={p === page ? 'page' : undefined}
                className={`min-w-8 h-8 px-2 rounded-lg text-xs font-semibold transition-colors ${
                  p === page ? 'bg-indigo-500 text-white' : 'bg-slate-800 hover:bg-slate-700 text-slate-300'
                }`}
              >
                {p}
              </button>
            </React.Fragment>
          ))}
          <button className={navBtn} disabled={page >= totalPages} onClick={() => onGoTo(page + 1)} aria-label={t('pagination.next')}>
            <ChevronRight className="w-4 h-4 rtl:rotate-180" />
          </button>
        </div>

        <span className="text-xs text-slate-400 whitespace-nowrap">
          {t('pagination.range', { from: rangeStart + 1, to: rangeEnd, total })}
        </span>

        <form onSubmit={submitJump} className="flex items-center gap-1.5">
          <input
            value={jump}
            onChange={(e) => setJump(e.target.value.replace(/\D/g, ''))}
            placeholder={t('pagination.jumpTo')}
            inputMode="numeric"
            className="w-24 h-8 rounded-lg bg-slate-950 border border-slate-700 px-2 text-xs text-slate-100 focus:border-indigo-400 focus:outline-none"
          />
        </form>

        <label className="flex items-center gap-1.5 text-xs text-slate-400">
          {t('pagination.perPage')}
          <select
            value={pageSize}
            onChange={(e) => onPageSize(Number(e.target.value))}
            className="h-8 rounded-lg bg-slate-950 border border-slate-700 px-1.5 text-xs text-slate-100"
          >
            {ZEN_PAGE_SIZES.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </label>

        {editedCount > 0 && (
          <span className="px-2 py-1 rounded-md bg-amber-500/15 text-amber-300 text-xs font-semibold border border-amber-500/30">
            {t('pagination.edited', { count: editedCount })}
          </span>
        )}

        <button
          onClick={onLaunch}
          disabled={isLaunching}
          className="inline-flex items-center gap-1.5 h-9 px-4 rounded-xl text-xs font-bold bg-gradient-to-r from-emerald-500 to-teal-500 hover:from-emerald-400 hover:to-teal-400 text-white shadow-lg shadow-emerald-500/25 disabled:opacity-60"
        >
          <Play className="w-3.5 h-3.5 fill-current" />
          {t('launcher.launchButton')}
        </button>
      </nav>
    </div>
  );
};
