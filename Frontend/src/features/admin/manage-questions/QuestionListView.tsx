/**
 * QuestionListView — Bảng danh sách câu hỏi
 * ==========================================
 * Tách từ AdminQuestionsPage.tsx — table + row actions.
 * FSD Layer: features/admin/manage-questions/
 */

import { Eye, ToggleLeft, ToggleRight, Trash2, ArrowUpDown, CheckCircle2, Sparkles } from 'lucide-react';
import type { LearnerQuestion } from '@/types/quiz';
import type { BankTopic } from '@/pages/admin/questions/types';
import { canManageResource, type UserProfile } from '@/types/auth';
import {
  LETTERS,
  getQuestionTypeLabel,
  getTopicFullPath,
  isOptionSelectedAsCorrect,
} from '@/pages/admin/questions/types';

// ── Helpers ───────────────────────────────────────────────────────────────────

function getCorrectAnswerDisplay(q: LearnerQuestion): { label: string; text: string } | null {
  if (!q.answerRaw || !q.answerRaw.trim()) return null;

  if (q.questionType === 'SINGLE' || q.questionType === 'TRUE_FALSE') {
    let idx = parseInt(q.answerRaw) - 1;
    if (isNaN(idx) || idx < 0 || !q.options?.[idx]) {
      if (q.options?.length) {
        const found = q.options.findIndex((opt, i) =>
          isOptionSelectedAsCorrect(opt, i, q.answerRaw, q.questionType, q.options));
        if (found !== -1) idx = found;
      }
    }
    if (q.options?.[idx] !== undefined) {
      const label = q.questionType === 'TRUE_FALSE'
        ? (idx === 0 ? 'Đúng (1)' : 'Sai (2)')
        : `${LETTERS[idx] || String(idx + 1)} (${idx + 1})`;
      return { label, text: q.options[idx] };
    }
    return { label: q.answerRaw, text: '' };
  }

  if (q.questionType === 'MULTI') {
    const labels: string[] = []; const texts: string[] = [];
    q.options?.forEach((opt, idx) => {
      if (isOptionSelectedAsCorrect(opt, idx, q.answerRaw, 'MULTI', q.options)) {
        labels.push(`${LETTERS[idx] || String(idx + 1)} (${idx + 1})`);
        texts.push(`${LETTERS[idx] || String(idx + 1)}. ${opt}`);
      }
    });
    if (labels.length > 0) return { label: labels.join(', '), text: texts.join(' | ') };
    return { label: q.answerRaw, text: '' };
  }

  return { label: q.answerRaw, text: '' };
}

// ── Pagination ─────────────────────────────────────────────────────────────────

export function Pagination({ page, totalPages, onChange }: {
  page: number; totalPages: number; onChange: (p: number) => void;
}) {
  return (
    <nav aria-label="Điều hướng trang" className="flex items-center gap-1.5">
      <button
        onClick={() => onChange(Math.max(1, page - 1))}
        disabled={page === 1}
        aria-label="Trang trước"
        className="px-3 py-1.5 rounded-lg bg-slate-950 text-slate-400 border border-slate-800
                   hover:bg-slate-800 hover:text-white disabled:opacity-40 disabled:cursor-not-allowed text-xs font-bold transition-all"
      >
        Trước
      </button>

      {Array.from({ length: totalPages }).map((_, idx) => {
        const pNum = idx + 1;
        if (pNum === 1 || pNum === totalPages || Math.abs(pNum - page) <= 1) {
          return (
            <button
              key={pNum}
              onClick={() => onChange(pNum)}
              aria-current={page === pNum ? 'page' : undefined}
              aria-label={`Trang ${pNum}`}
              className={`px-3 py-1.5 rounded-lg border text-xs font-bold transition-all ${
                page === pNum
                  ? 'bg-blue-600 border-blue-500 text-white shadow'
                  : 'bg-slate-950 border-slate-800 text-slate-400 hover:bg-slate-800 hover:text-white'
              }`}
            >
              {pNum}
            </button>
          );
        }
        if (pNum === 2 || pNum === totalPages - 1) {
          return <span key={pNum} className="text-slate-600 text-xs px-1 select-none" aria-hidden="true">...</span>;
        }
        return null;
      })}

      <button
        onClick={() => onChange(Math.min(totalPages, page + 1))}
        disabled={page === totalPages || totalPages === 0}
        aria-label="Trang sau"
        className="px-3 py-1.5 rounded-lg bg-slate-950 text-slate-400 border border-slate-800
                   hover:bg-slate-800 hover:text-white disabled:opacity-40 disabled:cursor-not-allowed text-xs font-bold transition-all"
      >
        Sau
      </button>
    </nav>
  );
}

// ── QuestionListView ──────────────────────────────────────────────────────────

interface QuestionListViewProps {
  questions:           LearnerQuestion[];
  pagedQuestions:      LearnerQuestion[];
  topics:              BankTopic[];
  selectedIds:         string[];
  sortField:           string;
  sortAsc:             boolean;
  page:                number;
  totalPages:          number;
  hasWritePermission:  boolean;
  currentUser?:        UserProfile | null;
  onToggleSelect:      (id: string) => void;
  onToggleSelectAll:   () => void;
  onEdit:              (q: LearnerQuestion) => void;
  onToggleActive:      (q: LearnerQuestion) => void;
  onDelete:            (id: string) => void;
  onSort:              (field: string) => void;
  onPageChange:        (p: number) => void;
  onContributeToTenant?: (q: LearnerQuestion) => void;
}

export function QuestionListView({
  questions, pagedQuestions, topics, selectedIds, sortField, sortAsc,
  page, totalPages, hasWritePermission, currentUser,
  onToggleSelect, onToggleSelectAll, onEdit, onToggleActive, onDelete, onSort, onPageChange,
  onContributeToTenant,
}: QuestionListViewProps) {
  const allPageIds     = pagedQuestions.map((q) => q.id);
  const allPageSelected = allPageIds.length > 0 && allPageIds.every((id) => selectedIds.includes(id));

  const SortIcon = ({ field }: { field: string }) => (
    <ArrowUpDown
      size={11}
      className={[
        'inline ml-1 transition-transform',
        field === sortField ? 'text-blue-400' : 'text-slate-600',
        field === sortField && !sortAsc ? 'rotate-180' : '',
      ].join(' ')}
      aria-hidden="true"
    />
  );

  return (
    <div>
      {/* Table */}
      <div className="overflow-x-auto rounded-xl border border-slate-800">
        <table className="w-full text-xs text-slate-300" role="grid" aria-label="Danh sách câu hỏi">
          <thead className="bg-slate-900 text-slate-500 uppercase tracking-wider">
            <tr>
              <th className="px-3 py-3 w-8 text-center">
                <input
                  type="checkbox"
                  checked={allPageSelected}
                  onChange={onToggleSelectAll}
                  aria-label="Chọn tất cả câu hỏi trên trang này"
                  className="rounded accent-blue-500 cursor-pointer"
                />
              </th>
              <th className="px-3 py-3 text-start w-12">STT</th>
              <th className="px-3 py-3 text-start cursor-pointer group" onClick={() => onSort('content')}>
                Nội dung câu hỏi <SortIcon field="content" />
              </th>
              <th className="px-3 py-3 text-start cursor-pointer" onClick={() => onSort('questionType')}>
                Loại <SortIcon field="questionType" />
              </th>
              <th className="px-3 py-3 text-start cursor-pointer" onClick={() => onSort('topicCode')}>
                Chủ đề <SortIcon field="topicCode" />
              </th>
              <th className="px-3 py-3 text-start">Đáp án đúng</th>
              <th className="px-3 py-3 text-start cursor-pointer" onClick={() => onSort('difficulty')}>
                Độ khó <SortIcon field="difficulty" />
              </th>
              <th className="px-3 py-3 text-center">Kích hoạt</th>
              {hasWritePermission && <th className="px-3 py-3 text-center">Hành động</th>}
            </tr>
          </thead>

          <tbody className="divide-y divide-slate-800">
            {pagedQuestions.length === 0 && (
              <tr>
                <td colSpan={hasWritePermission ? 9 : 8} className="py-12 text-center text-slate-500">
                  <div className="flex flex-col items-center gap-2">
                    <span className="text-2xl" aria-hidden="true">📭</span>
                    <span>Không tìm thấy câu hỏi nào</span>
                  </div>
                </td>
              </tr>
            )}

            {pagedQuestions.map((q, rowIdx) => {
              const stt        = (page - 1) * 10 + rowIdx + 1;
              const globalIdx  = questions.indexOf(q);
              const isSelected = selectedIds.includes(q.id);
              const topic      = topics.find((t) => t.code === q.topicCode || t.categoryCode === q.topicCode);
              const topicPath  = topic ? getTopicFullPath(topic, topics) : (q.topicCode || '—');
              const answer     = getCorrectAnswerDisplay(q);
              const isMissing  = !q.answerRaw || !q.answerRaw.trim();

              return (
                <tr
                  key={q.id}
                  className={[
                    'transition-colors',
                    isSelected ? 'bg-blue-950/40' : 'hover:bg-slate-800/40',
                    q.enabled === false ? 'opacity-50' : '',
                  ].join(' ')}
                >
                  {/* Checkbox */}
                  <td className="px-3 py-2 text-center">
                    <input
                      type="checkbox"
                      checked={isSelected}
                      onChange={() => onToggleSelect(q.id)}
                      aria-label={`Chọn câu hỏi ${stt}`}
                      className="rounded accent-blue-500 cursor-pointer"
                    />
                  </td>

                  {/* STT */}
                  <td className="px-3 py-2 text-slate-500 tabular-nums">{stt}</td>

                  {/* Content */}
                  <td className="px-3 py-2 max-w-xs">
                    <p className="line-clamp-2 text-slate-200 font-medium">{q.content}</p>
                    {q.contextTitle && (
                      <span className="mt-1 inline-block text-[10px] px-2 py-0.5 rounded-full
                                       bg-indigo-900/60 text-indigo-300 border border-indigo-700">
                        📖 {q.contextTitle}
                      </span>
                    )}
                    {q.tags?.length ? (
                      <div className="mt-1 flex flex-wrap gap-1">
                        {q.tags.slice(0, 3).map((tag) => (
                          <span key={tag} className="text-[10px] px-1.5 py-0.5 rounded-full bg-slate-700 text-slate-400">
                            #{tag}
                          </span>
                        ))}
                        {q.tags.length > 3 && (
                          <span className="text-[10px] text-slate-500">+{q.tags.length - 3}</span>
                        )}
                      </div>
                    ) : null}

                    {/* Attribution / Ownership Badge */}
                    {(q.creatorName || q.orgUnitName || q.scope === 'TEAM') && (
                      <div className="mt-1 flex items-center gap-1.5 flex-wrap">
                        <span className="inline-flex items-center gap-1 text-[10px] px-2 py-0.5 rounded-md bg-indigo-950/70 border border-indigo-500/40 text-indigo-300 font-semibold">
                          <span>👥 {q.orgUnitName || 'Nhóm'}</span>
                          {q.creatorName && (
                            <span className="text-indigo-200">• ✍️ {q.creatorName}</span>
                          )}
                        </span>
                        {q.contributionStatus === 'SUBMITTED_FOR_TENANT' && (
                          <span className="text-[10px] px-2 py-0.5 rounded-md bg-amber-950/60 border border-amber-500/40 text-amber-300 font-bold">
                            ⚡ Đã gửi duyệt toàn cơ quan
                          </span>
                        )}
                      </div>
                    )}
                  </td>

                  {/* Type */}
                  <td className="px-3 py-2 whitespace-nowrap">
                    <span className="px-2 py-0.5 rounded-full bg-slate-700 text-slate-300 font-medium text-[10px]">
                      {getQuestionTypeLabel(q.questionType)}
                    </span>
                  </td>

                  {/* Topic */}
                  <td className="px-3 py-2 text-slate-400 max-w-[150px] truncate" title={topicPath}>
                    {topicPath}
                  </td>

                  {/* Answer */}
                  <td className="px-3 py-2">
                    {isMissing ? (
                      <span className="flex items-center gap-1 text-amber-400 font-bold text-[10px]">
                        <span aria-hidden="true">⚠️</span> Chưa có
                      </span>
                    ) : answer ? (
                      <div>
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full
                                         bg-emerald-900/60 text-emerald-300 font-bold text-[10px]">
                          <CheckCircle2 className="w-3 h-3" aria-hidden="true" />
                          {answer.label}
                        </span>
                      </div>
                    ) : null}
                  </td>

                  {/* Difficulty */}
                  <td className="px-3 py-2">
                    <div className="flex gap-0.5" aria-label={`Độ khó: ${q.difficulty}/5`}>
                      {[1, 2, 3, 4, 5].map((star) => (
                        <div
                          key={star}
                          className={`w-2 h-2 rounded-full ${
                            star <= (q.difficulty || 1) ? 'bg-amber-400' : 'bg-slate-700'
                          }`}
                          aria-hidden="true"
                        />
                      ))}
                    </div>
                  </td>

                  {/* Toggle Active */}
                  {(() => {
                    const canManage = hasWritePermission && canManageResource(currentUser, q);
                    return (
                      <>
                        <td className="px-3 py-2 text-center">
                          <button
                            onClick={() => onToggleActive(q)}
                            disabled={!canManage}
                            aria-label={q.enabled === false ? 'Kích hoạt câu hỏi' : 'Vô hiệu hóa câu hỏi'}
                            aria-pressed={q.enabled !== false}
                            className="disabled:opacity-30 disabled:cursor-not-allowed transition-transform active:scale-95"
                          >
                            {q.enabled === false
                              ? <ToggleLeft  className="w-5 h-5 text-slate-600" aria-hidden="true" />
                              : <ToggleRight className="w-5 h-5 text-emerald-400" aria-hidden="true" />
                            }
                          </button>
                        </td>

                        {/* Actions */}
                        {hasWritePermission && (
                          <td className="px-3 py-2">
                            <div className="flex items-center justify-center gap-1">
                              {onContributeToTenant && q.scope === 'TEAM' && q.contributionStatus !== 'SUBMITTED_FOR_TENANT' && (
                                <button
                                  onClick={() => onContributeToTenant(q)}
                                  aria-label="Đóng góp câu hỏi lên Ngân hàng Toàn Cơ Quan"
                                  title="⚡ Đóng góp lên Ngân hàng Toàn Cơ Quan"
                                  className="p-1.5 rounded-lg text-amber-400 hover:text-amber-300 hover:bg-amber-950/50 transition-all cursor-pointer"
                                >
                                  <Sparkles className="w-3.5 h-3.5" aria-hidden="true" />
                                </button>
                              )}
                              <button
                                onClick={() => onEdit(q)}
                                aria-label={`Xem / Sửa câu hỏi: ${q.content.substring(0, 30)}...`}
                                className="p-1.5 rounded-lg text-slate-400 hover:text-blue-400 hover:bg-blue-950/50 transition-all cursor-pointer"
                              >
                                <Eye className="w-3.5 h-3.5" aria-hidden="true" />
                              </button>
                              {canManage && (
                                <button
                                  onClick={() => onDelete(q.id)}
                                  aria-label={`Xóa câu hỏi: ${q.content.substring(0, 30)}...`}
                                  className="p-1.5 rounded-lg text-slate-400 hover:text-rose-400 hover:bg-rose-950/50 transition-all cursor-pointer"
                                >
                                  <Trash2 className="w-3.5 h-3.5" aria-hidden="true" />
                                </button>
                              )}
                            </div>
                          </td>
                        )}
                      </>
                    );
                  })()}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex justify-center mt-4">
          <Pagination page={page} totalPages={totalPages} onChange={onPageChange} />
        </div>
      )}
    </div>
  );
}
