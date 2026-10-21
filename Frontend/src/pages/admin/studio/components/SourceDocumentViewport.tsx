import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { FileText, ZoomIn, ZoomOut, Search, Code2, Eye } from 'lucide-react';
import type { StudioQuestionItem } from '../hooks/useSplitStudioData';

interface SourceDocumentViewportProps {
  scrollRef: React.RefObject<HTMLDivElement | null>;
  onScroll: () => void;
  documentTitle: string;
  questions: StudioQuestionItem[];
  activeQuestionId: string | null;
  onSelectQuestion: (id: string) => void;
}

export const SourceDocumentViewport: React.FC<SourceDocumentViewportProps> = ({
  scrollRef,
  onScroll,
  documentTitle,
  questions,
  activeQuestionId,
  onSelectQuestion,
}) => {
  const { t } = useTranslation('split_studio');
  const [zoomLevel, setZoomLevel] = useState(100);
  const [searchQuery, setSearchQuery] = useState('');
  const [isRawMode, setIsRawMode] = useState(false);

  const handleZoom = (delta: number) => {
    setZoomLevel((prev) => Math.min(150, Math.max(80, prev + delta)));
  };

  return (
    <div className="flex flex-col h-full bg-slate-900/60 border-r border-slate-800">
      {/* Viewport Sub-header */}
      <div className="h-12 px-4 border-b border-slate-800/80 bg-slate-900/90 flex items-center justify-between shrink-0">
        <div className="flex items-center gap-2 text-xs font-semibold text-slate-300">
          <FileText className="w-4 h-4 text-sky-400" />
          <span className="truncate max-w-[200px]">{documentTitle}</span>
        </div>

        <div className="flex items-center gap-2">
          {/* Tìm kiếm */}
          <div className="relative hidden md:block">
            <Search className="w-3.5 h-3.5 text-slate-500 absolute left-2.5 top-2" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder={t('sourceViewport.search')}
              className="w-36 lg:w-48 pl-8 pr-2.5 py-1 text-xs bg-slate-850 border border-slate-700/80 rounded-lg text-slate-200 placeholder-slate-500 focus:outline-none focus:border-sky-500"
            />
          </div>

          {/* Chế độ thô / Vector Math */}
          <button
            onClick={() => setIsRawMode((prev) => !prev)}
            className={`p-1.5 rounded-lg border text-xs transition-all ${
              isRawMode
                ? 'bg-sky-500/20 border-sky-500/40 text-sky-300'
                : 'bg-slate-800 border-slate-700 text-slate-400 hover:text-slate-200'
            }`}
            title={isRawMode ? t('sourceViewport.vectorMathMode') : t('sourceViewport.rawTextMode')}
          >
            {isRawMode ? <Code2 className="w-3.5 h-3.5" /> : <Eye className="w-3.5 h-3.5" />}
          </button>

          {/* Zoom */}
          <div className="flex items-center gap-1 bg-slate-800/80 p-0.5 rounded-lg border border-slate-700 text-slate-400">
            <button
              onClick={() => handleZoom(-10)}
              className="p-1 hover:text-white rounded transition-colors"
              title={t('sourceViewport.zoomOut')}
            >
              <ZoomOut className="w-3.5 h-3.5" />
            </button>
            <span className="text-[11px] font-mono px-1">{zoomLevel}%</span>
            <button
              onClick={() => handleZoom(10)}
              className="p-1 hover:text-white rounded transition-colors"
              title={t('sourceViewport.zoomIn')}
            >
              <ZoomIn className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      </div>

      {/* Vùng hiển thị tài liệu gốc có thể cuộn */}
      <div
        ref={scrollRef}
        onScroll={onScroll}
        className="flex-1 overflow-y-auto p-4 sm:p-6 space-y-4 font-sans transition-all"
        style={{ fontSize: `${(zoomLevel / 100) * 14}px` }}
      >
        <div className="bg-slate-950/70 border border-slate-800 rounded-2xl p-5 shadow-inner space-y-6">
          <div className="pb-3 border-b border-slate-800 flex items-center justify-between text-xs text-slate-500">
            <span>{t('sourceViewport.title')}</span>
            <span>{t('sourceViewport.pageIndicator', { current: 1, total: 2 })}</span>
          </div>

          {questions.map((q, idx) => {
            const isActive = activeQuestionId === q.id;
            return (
              <div
                key={q.id}
                onClick={() => onSelectQuestion(q.id)}
                className={`p-4 rounded-xl transition-all cursor-pointer border ${
                  isActive
                    ? 'bg-sky-500/10 border-sky-500/40 shadow-lg shadow-sky-500/5 ring-1 ring-sky-500/30'
                    : 'bg-slate-900/60 border-slate-800/80 hover:border-slate-700'
                }`}
              >
                <div className="flex items-center justify-between mb-2">
                  <span className="text-xs font-mono font-bold text-sky-400 bg-sky-500/10 px-2 py-0.5 rounded">
                    {q.sourceAnchor}
                  </span>
                  <span className="text-[11px] font-mono text-slate-500">
                    {t('sourceViewport.coordinates', { line: (idx + 1) * 12, char: 1 })}
                  </span>
                </div>

                <div className="text-slate-300 leading-relaxed font-mono text-xs whitespace-pre-wrap select-text">
                  {q.originalSnippet}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
