/**
 * QuestionToolbar — Action controls cho Admin Questions
 * =======================================================
 * Tách từ AdminQuestionsPage.tsx — toolbar + filter + import buttons.
 * FSD Layer: features/admin/manage-questions/
 */

import { useRef } from 'react';
import {
  Plus, FileUp, Sparkles, Brain, Network, Tag, Unlink,
  Wand2, Cpu, Eye, FileSpreadsheet, AlertTriangle, BookOpen,
} from 'lucide-react';

interface QuestionToolbarProps {
  hasWritePermission:       boolean;
  totalCount:               number;
  filteredCount:            number;
  missingAnswersCount:      number;
  selectedCount:            number;
  showMissingOnly:          boolean;
  isDocxProcessing:         boolean;
  onAddQuestion:            () => void;
  onOpenAiGenerate:         () => void;
  onOpenGovernance:         () => void;
  onOpenExcelStudio:        () => void;
  onFileChange:             (e: React.ChangeEvent<HTMLInputElement>) => void;
  onToggleMissingOnly:      () => void;
  onOpenBatchContext:       () => void;
  onOpenBatchTag:           () => void;
  onBatchRemoveContext:     () => void;
}

export function QuestionToolbar({
  hasWritePermission,
  totalCount,
  filteredCount,
  missingAnswersCount,
  selectedCount,
  showMissingOnly,
  isDocxProcessing,
  onAddQuestion,
  onOpenAiGenerate,
  onOpenGovernance,
  onOpenExcelStudio,
  onFileChange,
  onToggleMissingOnly,
  onOpenBatchContext,
  onOpenBatchTag,
  onBatchRemoveContext,
}: QuestionToolbarProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
      {/* ── Left: Stats ── */}
      <div className="flex items-center gap-3 text-sm text-slate-400">
        <span>
          <span className="font-bold text-slate-200">{filteredCount}</span> / {totalCount} câu
        </span>
        {missingAnswersCount > 0 && (
          <button
            onClick={onToggleMissingOnly}
            className={[
              'flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold border transition-all',
              showMissingOnly
                ? 'bg-amber-900/60 border-amber-500 text-amber-300'
                : 'bg-slate-800 border-slate-700 text-amber-400 hover:border-amber-500',
            ].join(' ')}
            aria-pressed={showMissingOnly}
            aria-label="Lọc câu thiếu đáp án"
          >
            <AlertTriangle className="w-3 h-3" aria-hidden="true" />
            {missingAnswersCount} thiếu đáp án
          </button>
        )}
        {selectedCount > 0 && (
          <span className="px-2.5 py-1 rounded-full bg-blue-900/60 border border-blue-600 text-blue-300 text-xs font-bold">
            ✓ {selectedCount} đã chọn
          </span>
        )}
      </div>

      {/* ── Right: Actions ── */}
      <div className="flex flex-wrap items-center gap-2">

        {/* Batch actions (show khi có selection) */}
        {selectedCount > 0 && hasWritePermission && (
          <>
            <button
              onClick={onOpenBatchContext}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-indigo-900/70 text-indigo-300 border border-indigo-700 hover:bg-indigo-800 transition-all"
              aria-label="Gán bài đọc hiểu dùng chung"
            >
              <BookOpen className="w-3.5 h-3.5" aria-hidden="true" />
              Gán Context
            </button>
            <button
              onClick={onBatchRemoveContext}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-rose-900/70 text-rose-300 border border-rose-700 hover:bg-rose-800 transition-all"
              aria-label="Bỏ gán context"
            >
              <Unlink className="w-3.5 h-3.5" aria-hidden="true" />
              Bỏ Context
            </button>
            <button
              onClick={onOpenBatchTag}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-teal-900/70 text-teal-300 border border-teal-700 hover:bg-teal-800 transition-all"
              aria-label="Gán tag hàng loạt"
            >
              <Tag className="w-3.5 h-3.5" aria-hidden="true" />
              Gán Tags
            </button>
          </>
        )}

        {/* Governance */}
        {hasWritePermission && (
          <button
            onClick={onOpenGovernance}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-purple-900/70 text-purple-300 border border-purple-700 hover:bg-purple-800 transition-all"
            aria-label="Quản trị Multi-Tenant"
          >
            <Network className="w-3.5 h-3.5" aria-hidden="true" />
            Governance
          </button>
        )}

        {/* Excel Ingestion Studio */}
        {hasWritePermission && (
          <button
            onClick={onOpenExcelStudio}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-emerald-900/70 text-emerald-300 border border-emerald-700 hover:bg-emerald-800 transition-all"
            aria-label="Mở Excel Ingestion Studio"
          >
            <FileSpreadsheet className="w-3.5 h-3.5" aria-hidden="true" />
            UCIS Studio
          </button>
        )}

        {/* Import File (Excel/DOCX/PDF) */}
        {hasWritePermission && (
          <>
            <input
              ref={fileInputRef}
              type="file"
              accept=".xlsx,.xls,.docx,.pdf"
              className="hidden"
              onChange={onFileChange}
              id="question-file-import"
              aria-label="Chọn file Excel, DOCX hoặc PDF để nhập câu hỏi"
            />
            <button
              onClick={() => fileInputRef.current?.click()}
              disabled={isDocxProcessing}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-slate-700 text-slate-200 border border-slate-600 hover:bg-slate-600 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
              aria-label="Nhập từ file Excel/DOCX/PDF"
            >
              {isDocxProcessing
                ? <><Cpu className="w-3.5 h-3.5 animate-spin" aria-hidden="true" />Đang xử lý...</>
                : <><FileUp className="w-3.5 h-3.5" aria-hidden="true" />Nhập file</>
              }
            </button>
          </>
        )}

        {/* AI Generate */}
        {hasWritePermission && (
          <button
            onClick={onOpenAiGenerate}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-gradient-to-r from-violet-700 to-cyan-700 text-white border border-violet-600 hover:brightness-110 transition-all shadow-md"
            aria-label="Tạo câu hỏi bằng AI"
          >
            <Sparkles className="w-3.5 h-3.5" aria-hidden="true" />
            AI Tạo câu
          </button>
        )}

        {/* Add Question */}
        {hasWritePermission && (
          <button
            onClick={onAddQuestion}
            className="flex items-center gap-1.5 px-4 py-1.5 rounded-lg text-xs font-bold bg-blue-600 text-white border border-blue-500 hover:bg-blue-500 transition-all shadow-md"
            aria-label="Thêm câu hỏi mới"
          >
            <Plus className="w-3.5 h-3.5" aria-hidden="true" />
            Thêm câu hỏi
          </button>
        )}
      </div>
    </div>
  );
}
