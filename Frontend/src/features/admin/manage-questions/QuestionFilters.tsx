/**
 * QuestionFilters — Filter bar cho Admin Questions
 * =================================================
 * Tách từ AdminQuestionsPage.tsx — search + filters.
 * FSD Layer: features/admin/manage-questions/
 */

import { Search } from 'lucide-react';
import type { BankTopic } from '@/pages/admin/questions/types';

const QUESTION_TYPE_OPTIONS = [
  { value: '',             label: 'Tất cả loại' },
  { value: 'SINGLE',       label: 'Trắc nghiệm 1 đáp án' },
  { value: 'MULTI',        label: 'Nhiều đáp án đúng' },
  { value: 'TRUE_FALSE',   label: 'Đúng / Sai' },
  { value: 'SHORT_ANSWER', label: 'Trả lời ngắn' },
  { value: 'FILL_BLANK',   label: 'Điền vào chỗ trống' },
  { value: 'ORDERING',     label: 'Sắp xếp thứ tự' },
  { value: 'MATCHING',     label: 'Nối cặp' },
  { value: 'ESSAY',        label: 'Tự luận (AI chấm)' },
];

interface QuestionFiltersProps {
  search:        string;
  typeFilter:    string;
  topicFilter:   string;
  domainFilter:  string;
  tagFilter:     string | null;
  scopeFilter?:  string;
  topics:        BankTopic[];
  domains:       { code: string; name: string }[];
  availableTags: string[];
  onSearch:      (v: string) => void;
  onTypeFilter:  (v: string) => void;
  onTopicFilter: (v: string) => void;
  onDomainFilter:(v: string) => void;
  onTagFilter:   (v: string | null) => void;
  onScopeFilter?:(v: string) => void;
}

export function QuestionFilters({
  search, typeFilter, topicFilter, domainFilter, tagFilter, scopeFilter,
  topics, domains, availableTags,
  onSearch, onTypeFilter, onTopicFilter, onDomainFilter, onTagFilter, onScopeFilter,
}: QuestionFiltersProps) {
  const selectClass = `
    px-3 py-2 rounded-lg bg-slate-800 text-slate-300 border border-slate-700
    hover:border-slate-500 focus:border-blue-500 focus:outline-none
    text-xs font-medium transition-colors cursor-pointer
  `.trim();

  return (
    <div className="flex flex-wrap items-center gap-2 mb-3">
      {/* Search */}
      <div className="relative flex-1 min-w-[180px]">
        <Search
          className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500 pointer-events-none"
          aria-hidden="true"
        />
        <input
          type="search"
          value={search}
          onChange={(e) => onSearch(e.target.value)}
          placeholder="Tìm câu hỏi... (dùng #tag để lọc tags)"
          className="w-full pl-9 pr-3 py-2 rounded-lg bg-slate-800 text-slate-200 border border-slate-700
                     focus:border-blue-500 focus:outline-none text-xs placeholder:text-slate-500 transition-colors"
          aria-label="Tìm kiếm câu hỏi"
        />
      </div>

      {/* Type filter */}
      <select
        value={typeFilter}
        onChange={(e) => onTypeFilter(e.target.value)}
        className={selectClass}
        aria-label="Lọc theo loại câu hỏi"
      >
        {QUESTION_TYPE_OPTIONS.map((o) => (
          <option key={o.value} value={o.value}>{o.label}</option>
        ))}
      </select>

      {/* Topic filter */}
      <select
        value={topicFilter}
        onChange={(e) => onTopicFilter(e.target.value)}
        className={selectClass}
        aria-label="Lọc theo chủ đề"
      >
        <option value="">Tất cả chủ đề</option>
        {topics.map((t) => (
          <option key={t.id} value={t.code}>{t.name}</option>
        ))}
      </select>

      {/* Domain filter */}
      <select
        value={domainFilter}
        onChange={(e) => onDomainFilter(e.target.value)}
        className={selectClass}
        aria-label="Lọc theo ngành"
      >
        <option value="ALL">Tất cả ngành</option>
        {domains.map((d) => (
          <option key={d.code} value={d.code}>{d.name}</option>
        ))}
      </select>

      {/* Scope / Ownership filter */}
      {onScopeFilter && (
        <select
          value={scopeFilter || 'ALL'}
          onChange={(e) => onScopeFilter(e.target.value)}
          className={`${selectClass} border-indigo-500/50 bg-indigo-950/40 text-indigo-200 font-semibold`}
          aria-label="Lọc theo phạm vi sở hữu"
        >
          <option value="ALL">🌐 Mọi phạm vi</option>
          <option value="TEAM">👥 Thuộc Nhóm của tôi</option>
          <option value="MY_QUESTIONS">👤 Do tôi đóng góp</option>
        </select>
      )}

      {/* Tag filter */}
      {availableTags.length > 0 && (
        <select
          value={tagFilter ?? ''}
          onChange={(e) => onTagFilter(e.target.value || null)}
          className={selectClass}
          aria-label="Lọc theo tag"
        >
          <option value="">Tất cả tags</option>
          {availableTags.map((tag) => (
            <option key={tag} value={tag}>#{tag}</option>
          ))}
        </select>
      )}
    </div>
  );
}
