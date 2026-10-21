import React from 'react';
import { BookOpen, Edit, Eye, Sparkles, Star, Tag, ToggleLeft, ToggleRight, Trash2, UploadCloud, Users, User, ShieldCheck } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { MathRenderer } from '@/components/common/MathRenderer';
import type { LearnerQuestion } from '@/types/quiz';
import { canManageResource } from '@/types/auth';
import type { BankTopic } from '../types';
import { getCorrectAnswerDisplay, getQuestionTypeLabel, getTopicFullPath } from '../types';

interface QuestionTableSectionProps {
  questions: LearnerQuestion[];
  topics: BankTopic[];
  filteredQuestions: LearnerQuestion[];
  pagedQuestions: LearnerQuestion[];
  user: any;
  hasWritePermission: boolean;
  selectedQuestionIds: string[];
  rangeFrom: string;
  rangeTo: string;
  setRangeFrom: (v: string) => void;
  setRangeTo: (v: string) => void;
  handleSelectRange: () => void;
  handleToggleSelect: (id: string) => void;
  handleToggleSelectAll: () => void;
  qSearch: string;
  setQSearch: (v: string) => void;
  qDomainFilter: string;
  setQDomainFilter: (v: string) => void;
  qTopicFilter: string;
  setQTopicFilter: (v: string) => void;
  qTypeFilter: string;
  setQTypeFilter: (v: string) => void;
  showMissingOnly: boolean;
  setShowMissingOnly: (v: boolean) => void;
  qTagFilter: string | null;
  setQTagFilter: (v: string | null) => void;
  qPage: number;
  setQPage: (p: number) => void;
  qPageSize: number;
  qTotalPages: number;
  qSortField: string;
  setQSortField: (f: string) => void;
  qSortAsc: boolean;
  setQSortAsc: (a: boolean) => void;
  renderSortIcon: (field: string, currentField: string, isAsc: boolean) => React.ReactNode;
  setSelectedContext: (ctx: { title: string; content: string } | null) => void;
  openEditModal: (q: LearnerQuestion) => void;
  setIsEditing: (v: boolean) => void;
  handleSuggestAnswer: (q: LearnerQuestion) => void;
  handleToggleActive: (q: LearnerQuestion) => void;
  handleDelete: (id: string) => void;
  handleContributeToTenant: (q: LearnerQuestion) => void;
  isCleaningOrphans: boolean;
  handleCleanupOrphans: () => void;
  Pagination: React.ComponentType<{ page: number; totalPages: number; onChange: (p: number) => void }>;
}

export const QuestionTableSection: React.FC<QuestionTableSectionProps> = ({
  questions,
  topics,
  filteredQuestions,
  pagedQuestions,
  user,
  selectedQuestionIds,
  rangeFrom,
  rangeTo,
  setRangeFrom,
  setRangeTo,
  handleSelectRange,
  handleToggleSelect,
  handleToggleSelectAll,
  qSearch,
  setQSearch,
  qDomainFilter,
  setQDomainFilter,
  qTopicFilter,
  setQTopicFilter,
  qTypeFilter,
  setQTypeFilter,
  showMissingOnly,
  setShowMissingOnly,
  qTagFilter,
  setQTagFilter,
  qPage,
  setQPage,
  qPageSize,
  qTotalPages,
  qSortField,
  setQSortField,
  qSortAsc,
  setQSortAsc,
  renderSortIcon,
  setSelectedContext,
  openEditModal,
  setIsEditing,
  handleSuggestAnswer,
  handleToggleActive,
  handleDelete,
  handleContributeToTenant,
  isCleaningOrphans,
  handleCleanupOrphans,
  Pagination,
}) => {
  const { t } = useTranslation('admin_questions');

  const handleSort = (field: string) => {
    if (qSortField === field) {
      setQSortAsc(!qSortAsc);
    } else {
      setQSortField(field);
      setQSortAsc(true);
    }
  };

  const hasOrphanQuestions =
    questions.length > 0 &&
    (topics.length === 0 ||
      questions.some(
        q =>
          !topics.some(
            top =>
              top.code === q.topicCode ||
              top.categoryCode === q.topicCode ||
              (top.code &&
                q.topicCode &&
                top.code.replace(/[-_]/g, '').toUpperCase() === q.topicCode.replace(/[-_]/g, '').toUpperCase())
          )
      ));

  return (
    <div>
      {/* Filter bar */}
      <div className="p-6 border-b border-slate-850 bg-slate-900/10 space-y-3">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-7 gap-3">
          <div className="space-y-1">
            <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">
              {t('filters.searchQuestion')}
            </label>
            <input
              type="text"
              placeholder={t('filters.searchQuestionPlaceholder')}
              value={qSearch}
              onChange={e => setQSearch(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-indigo-500"
            />
          </div>
          <div className="space-y-1">
            <label className="text-[10px] font-black uppercase text-amber-400 tracking-wider flex items-center gap-1">
              {t('filters.industryDomain')}
            </label>
            <select
              value={qDomainFilter}
              onChange={e => setQDomainFilter(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-amber-300 focus:outline-none focus:border-amber-500 cursor-pointer font-bold"
            >
              <option value="ALL">{t('filters.allDomains')}</option>
              <option value="EDUCATION">{t('domains.EDUCATION')}</option>
              <option value="BANKING">{t('domains.BANKING')}</option>
              <option value="HEALTHCARE">{t('domains.HEALTHCARE')}</option>
              <option value="HSE">{t('domains.HSE')}</option>
              <option value="GOV_DRIVING">{t('domains.GOV_DRIVING')}</option>
              <option value="IT_SECURITY">{t('domains.IT_SECURITY')}</option>
              <option value="GENERAL">{t('domains.GENERAL')}</option>
            </select>
          </div>
          <div className="space-y-1">
            <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">
              {t('filters.topicFilter')}
            </label>
            <select
              value={qTopicFilter}
              onChange={e => setQTopicFilter(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer"
            >
              <option value="">{t('filters.allTopics')}</option>
              {topics.map(top => (
                <option key={top.id} value={top.code}>{getTopicFullPath(top, topics)}</option>
              ))}
            </select>
          </div>
          <div className="space-y-1">
            <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">
              {t('filters.questionType')}
            </label>
            <select
              value={qTypeFilter}
              onChange={e => setQTypeFilter(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer"
            >
              <option value="ALL">{t('filters.allTypes')}</option>
              <option value="SINGLE">{t('types.SINGLE')}</option>
              <option value="MULTI">{t('types.MULTI')}</option>
              <option value="TRUE_FALSE">{t('types.TRUE_FALSE')}</option>
              <option value="MULTI_TRUE_FALSE">{t('types.MULTI_TRUE_FALSE')}</option>
              <option value="SHORT_ANSWER">{t('types.SHORT_ANSWER')}</option>
              <option value="FILL_BLANK">{t('types.FILL_BLANK')}</option>
              <option value="ORDERING">{t('types.ORDERING')}</option>
              <option value="MATCHING">{t('types.MATCHING')}</option>
              <option value="ESSAY">{t('types.ESSAY')}</option>
            </select>
          </div>
          <div className="space-y-1">
            <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">
              {t('filters.answerStatus')}
            </label>
            <select
              value={showMissingOnly ? 'missing' : 'all'}
              onChange={e => setShowMissingOnly(e.target.value === 'missing')}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer"
            >
              <option value="all">{t('filters.allQuestions')}</option>
              <option value="missing">{t('filters.missingAnswersOnly')}</option>
            </select>
          </div>
          <div className="space-y-1 col-span-1 sm:col-span-2">
            <label className="text-[10px] font-black uppercase text-indigo-400 tracking-wider flex items-center gap-1">
              <BookOpen size={11} /> {t('filters.questionRange')}
            </label>
            <div className="flex items-center gap-1.5">
              <input
                type="number"
                min={1}
                placeholder={t('filters.rangeFrom')}
                value={rangeFrom}
                onChange={e => setRangeFrom(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 rounded-xl px-2 py-2 text-xs text-white text-center focus:outline-none focus:border-indigo-500 placeholder-slate-600"
              />
              <span className="text-xs text-slate-500">-</span>
              <input
                type="number"
                min={1}
                placeholder={t('filters.rangeTo')}
                value={rangeTo}
                onChange={e => setRangeTo(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 rounded-xl px-2 py-2 text-xs text-white text-center focus:outline-none focus:border-indigo-500 placeholder-slate-600"
              />
              <button
                type="button"
                onClick={handleSelectRange}
                className="px-3 py-2 bg-indigo-600/30 hover:bg-indigo-600/50 text-indigo-300 border border-indigo-500/40 rounded-xl font-bold text-xs cursor-pointer transition-colors active:scale-95"
                title={t('filters.rangeSelect')}
              >
                {t('filters.rangeSelect')}
              </button>
            </div>
          </div>
        </div>

        {qTagFilter && (
          <div className="flex items-center gap-2 pt-1">
            <span className="text-xs text-slate-400">{t('filters.filteringByTag')}</span>
            <span className="inline-flex items-center gap-1.5 px-3 py-0.5 rounded-full text-xs font-bold bg-indigo-600 text-white shadow-sm">
              #{qTagFilter}
              <button
                type="button"
                onClick={() => setQTagFilter(null)}
                className="hover:bg-white/20 rounded-full p-0.5 transition-colors cursor-pointer"
                title={t('filters.removeTagFilter')}
              >
                &times;
              </button>
            </span>
          </div>
        )}
      </div>

      <div className="w-full overflow-x-auto">
        {hasOrphanQuestions && (
          <div className="mx-4 mt-3 mb-3 p-4 bg-gradient-to-r from-amber-950/40 via-amber-900/20 to-slate-900/60 border border-amber-500/40 rounded-2xl flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-lg">
            <div className="flex items-start sm:items-center gap-3 text-amber-200">
              <span className="text-xl p-2 bg-amber-500/10 border border-amber-500/20 rounded-xl">⚠️</span>
              <div>
                <p className="font-bold text-xs sm:text-sm text-amber-300">
                  {topics.length === 0
                    ? `Đã xóa hết chủ đề nhưng vẫn còn ${questions.length} câu hỏi độc lập trong cơ sở dữ liệu!`
                    : `Phát hiện câu hỏi chưa liên kết với chủ đề hợp lệ nào (Câu hỏi mồ côi).`}
                </p>
                <p className="text-[11px] text-amber-400/80 mt-0.5">
                  Kiến trúc bảo toàn câu hỏi giữ lại dữ liệu an toàn. Bạn có thể chọn tick để xóa hoặc bấm nút bên cạnh để làm sạch hoàn toàn.
                </p>
              </div>
            </div>
            <button
              type="button"
              disabled={isCleaningOrphans}
              onClick={handleCleanupOrphans}
              className="px-4 py-2 bg-gradient-to-r from-red-600 to-rose-600 hover:from-red-500 hover:to-rose-500 text-white font-bold rounded-xl text-xs flex items-center gap-1.5 cursor-pointer shadow-md shadow-red-600/30 active:scale-95 transition-all disabled:opacity-50"
            >
              <Trash2 size={13} /> {isCleaningOrphans ? 'Đang dọn dẹp...' : '🧹 Dọn Sạch Câu Hỏi Mồ Côi'}
            </button>
          </div>
        )}

        <table className="w-full text-left border-collapse min-w-[750px]">
          <thead>
            <tr className="border-b border-slate-850 text-slate-400 text-xs font-black tracking-wider uppercase bg-slate-900/20 select-none">
              <th className="p-4 w-12 text-center">
                <input
                  type="checkbox"
                  checked={filteredQuestions.length > 0 && filteredQuestions.every(q => selectedQuestionIds.includes(q.id))}
                  onChange={handleToggleSelectAll}
                  className="w-4 h-4 rounded text-indigo-600 focus:ring-indigo-500 bg-slate-900 border-slate-750 cursor-pointer"
                />
              </th>
              <th className="p-4 w-16 text-center cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleSort('stt')}>
                <div className="flex items-center justify-center">{t('table.stt')} {renderSortIcon('stt', qSortField, qSortAsc)}</div>
              </th>
              <th className="p-4 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleSort('content')}>
                <div className="flex items-center">{t('table.content')} {renderSortIcon('content', qSortField, qSortAsc)}</div>
              </th>
              <th className="p-4 w-32 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleSort('questionType')}>
                <div className="flex items-center">{t('table.type')} {renderSortIcon('questionType', qSortField, qSortAsc)}</div>
              </th>
              <th className="p-4 w-44 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleSort('topicCode')}>
                <div className="flex items-center">{t('table.topic')} {renderSortIcon('topicCode', qSortField, qSortAsc)}</div>
              </th>
              <th className="p-4 w-32 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleSort('difficulty')}>
                <div className="flex items-center">{t('table.difficulty')} {renderSortIcon('difficulty', qSortField, qSortAsc)}</div>
              </th>
              <th className="p-4 w-24 text-right sticky right-0 bg-slate-900 z-10 border-l border-slate-800 shadow-[-4px_0_8px_rgba(0,0,0,0.15)]">
                {t('table.actions')}
              </th>
            </tr>
          </thead>
          <tbody>
            {pagedQuestions.map((q: LearnerQuestion, idx: number) => {
              const matchedTopic = topics.find(top => top.code === q.topicCode || top.categoryCode === q.topicCode);
              const correct = getCorrectAnswerDisplay(q);
              const isSelected = selectedQuestionIds.includes(q.id);
              const isRowManageable = canManageResource(user, q);

              return (
                <tr key={q.id || idx} className={`border-b border-slate-850/60 hover:bg-slate-800/10 transition-colors ${isSelected ? 'bg-indigo-950/20' : ''}`}>
                  <td className="p-4 text-center">
                    <input
                      type="checkbox"
                      checked={isSelected}
                      onChange={() => handleToggleSelect(q.id)}
                      className="w-4 h-4 rounded text-indigo-600 focus:ring-indigo-500 bg-slate-900 border-slate-750 cursor-pointer"
                    />
                  </td>
                  <td className="p-4 text-center text-xs text-slate-450 font-bold" title={`ID: ${q.id}`}>
                    {(qPage - 1) * qPageSize + idx + 1}
                  </td>
                  <td className="p-4 font-bold text-slate-200 text-sm leading-relaxed">
                    {q.contextContent && (
                      <div className="mb-2">
                        <button
                          type="button"
                          onClick={() => setSelectedContext({
                            title: q.contextTitle || t('table.sharedContext'),
                            content: q.contextContent || ''
                          })}
                          className="inline-flex items-center gap-1.5 px-3 py-1 rounded-xl text-xs font-bold bg-indigo-950/60 hover:bg-indigo-900/80 text-indigo-300 border border-indigo-500/40 transition-all cursor-pointer shadow-sm group"
                        >
                          <BookOpen size={13} className="text-indigo-400 group-hover:scale-110 transition-transform" />
                          <span>📖 {q.contextTitle || t('table.sharedContext')}</span>
                          <span className="text-[10px] bg-indigo-500/30 text-indigo-200 px-1.5 py-0.2 rounded font-mono ml-0.5">
                            {t('table.readContext')}
                          </span>
                        </button>
                      </div>
                    )}
                    <div className="max-w-2xl overflow-hidden text-slate-200">
                      <MathRenderer content={q.content} />
                    </div>
                    <div className="mt-2 flex items-center gap-2 flex-wrap text-xs font-semibold select-none">
                      {!correct ? (
                        <span className="text-amber-450 bg-amber-500/10 px-2.5 py-0.5 rounded-lg border border-amber-500/20 text-[10px] uppercase tracking-wider font-black">
                          {t('table.missingAnswerWarning')}
                        </span>
                      ) : (
                        <>
                          <span className="text-emerald-450 bg-emerald-500/10 px-2.5 py-0.5 rounded-lg border border-emerald-500/20 text-[10px] uppercase tracking-wider font-black">
                            {t('table.correctAnswerPrefix')} {correct.label}
                          </span>
                          {correct.text && (
                            <span className="text-slate-400 font-medium italic text-sm max-w-lg truncate" title={correct.text}>
                              (<MathRenderer content={correct.text} inline />)
                            </span>
                          )}
                        </>
                      )}
                    </div>
                    {/* Tags & Domain */}
                    {((q.tags && q.tags.length > 0) || (q.domainCode && q.domainCode !== 'EDUCATION') || q.targetLevel || q.issuingOrg) && (
                      <div className="mt-2 flex items-center gap-1.5 flex-wrap">
                        {q.domainCode && q.domainCode !== 'EDUCATION' && (
                          <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-purple-950/60 border border-purple-800/60 text-purple-300">
                            {t(`domains.${q.domainCode}`, { defaultValue: q.domainCode })}
                          </span>
                        )}
                        {q.targetLevel && (
                          <span className="px-2 py-0.5 rounded-md text-[10px] font-medium bg-slate-800/80 text-slate-300 border border-slate-700/60">
                            🎯 {q.targetLevel}
                          </span>
                        )}
                        {q.issuingOrg && (
                          <span className="px-2 py-0.5 rounded-md text-[10px] font-medium bg-blue-950/40 text-blue-300 border border-blue-800/40">
                            🏛️ {q.issuingOrg}
                          </span>
                        )}
                        {q.tags && q.tags.map(tagItem => (
                          <button
                            key={tagItem}
                            type="button"
                            onClick={() => setQTagFilter(tagItem === qTagFilter ? null : tagItem)}
                            className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[10px] font-bold transition-all cursor-pointer ${
                              tagItem === qTagFilter ? 'bg-indigo-600 text-white shadow-sm' : 'bg-indigo-950/40 hover:bg-indigo-900/60 border border-indigo-800/50 text-indigo-300'
                            }`}
                          >
                            #{tagItem}
                          </button>
                        ))}
                      </div>
                    )}
                    {/* Multi-tier Scope & Attribution */}
                    <div className="mt-2 flex items-center gap-1.5 flex-wrap">
                      <span className={`px-2 py-0.5 rounded-md text-[10px] font-bold flex items-center gap-1 border ${
                        q.scope === 'TEAM'
                          ? 'bg-amber-950/60 text-amber-300 border-amber-500/40'
                          : q.scope === 'PERSONAL'
                          ? 'bg-purple-950/60 text-purple-300 border-purple-500/40'
                          : 'bg-blue-950/50 text-blue-300 border-blue-500/30'
                      }`}>
                        {q.scope === 'TEAM' ? <Users size={10} /> : q.scope === 'PERSONAL' ? <User size={10} /> : <ShieldCheck size={10} />}
                        <span>{q.scope === 'TEAM' ? (q.orgUnitName ? t('scopes.teamPrefix', { name: q.orgUnitName }) : t('scopes.team')) : q.scope === 'PERSONAL' ? t('scopes.personal') : t('scopes.community')}</span>
                        {q.creatorName && <span className="opacity-80">• ✍️ {q.creatorName}</span>}
                      </span>
                      {q.contributionStatus === 'SUBMITTED_FOR_TENANT' && (
                        <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-amber-500/20 text-amber-300 border border-amber-500/40 flex items-center gap-1 animate-pulse">
                          {t('scopes.pendingTenant')}
                        </span>
                      )}
                      {q.contributionStatus === 'APPROVED_TENANT' && (
                        <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 flex items-center gap-1">
                          {t('scopes.approvedTenant')}
                        </span>
                      )}
                    </div>
                  </td>
                  <td className="p-4">
                    <span className="px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider bg-blue-500/10 text-blue-400 border border-blue-500/20">
                      {getQuestionTypeLabel(q.questionType, t)}
                    </span>
                  </td>
                  <td className="p-4 text-slate-355 text-xs">
                    {matchedTopic ? getTopicFullPath(matchedTopic, topics) : q.topicCode || t('table.unassignedTopic')}
                  </td>
                  <td className="p-4">
                    <div className="flex gap-0.5 text-amber-500">
                      {Array.from({ length: q.difficulty || 1 }).map((_, i) => (
                        <Star key={i} size={12} fill="currentColor" />
                      ))}
                    </div>
                  </td>
                  <td className="p-4 text-right sticky right-0 bg-slate-900/95 z-10 border-l border-slate-850/60 shadow-[-4px_0_8px_rgba(0,0,0,0.15)]">
                    <div className="flex items-center justify-end gap-1.5">
                      {q.scope === 'TEAM' && isRowManageable && q.contributionStatus !== 'SUBMITTED_FOR_TENANT' && q.contributionStatus !== 'APPROVED_TENANT' && (
                        <button
                          onClick={() => handleContributeToTenant(q)}
                          className="p-1.5 text-amber-400 hover:text-amber-300 hover:bg-amber-950/30 rounded-lg transition-colors cursor-pointer border border-amber-500/30"
                          title={t('tooltips.contributeTenant')}
                        >
                          <UploadCloud size={13} />
                        </button>
                      )}
                      <button
                        onClick={() => { openEditModal(q); setIsEditing(isRowManageable); }}
                        className={`p-1.5 rounded-lg transition-colors cursor-pointer ${
                          isRowManageable ? 'text-amber-400 hover:text-amber-300 hover:bg-amber-950/20' : 'text-slate-400 hover:text-white hover:bg-slate-800'
                        }`}
                        title={isRowManageable ? t('tooltips.editQuestion') : t('tooltips.viewDetails')}
                      >
                        {isRowManageable ? <Edit size={14} /> : <Eye size={14} />}
                      </button>
                      {(!q.answerRaw || !q.answerRaw.trim()) && isRowManageable && (
                        <button
                          onClick={() => handleSuggestAnswer(q)}
                          className="p-1.5 text-purple-400 hover:text-purple-300 hover:bg-purple-950/20 rounded-lg transition-colors cursor-pointer animate-pulse"
                          title={t('tooltips.aiSolve')}
                        >
                          <Sparkles size={14} />
                        </button>
                      )}
                      <button
                        onClick={() => handleToggleActive(q)}
                        disabled={!isRowManageable}
                        className={`p-1.5 rounded-lg transition-colors ${
                          isRowManageable
                            ? 'cursor-pointer ' + (q.enabled !== false ? 'text-emerald-450 hover:text-emerald-300 hover:bg-emerald-950/20' : 'text-slate-500 hover:text-slate-300 hover:bg-slate-850')
                            : 'opacity-40 cursor-not-allowed text-slate-600'
                        }`}
                        title={isRowManageable ? (q.enabled !== false ? t('tooltips.deactivate') : t('tooltips.activate')) : ''}
                      >
                        {q.enabled !== false ? <ToggleRight size={17} /> : <ToggleLeft size={17} />}
                      </button>
                      {isRowManageable && (
                        <button
                          onClick={() => handleDelete(q.id)}
                          className="p-1.5 text-red-400 hover:text-red-300 hover:bg-red-950/20 rounded-lg transition-colors cursor-pointer"
                          title={t('tooltips.deleteQuestion')}
                        >
                          <Trash2 size={13} />
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
            {pagedQuestions.length === 0 && (
              <tr>
                <td colSpan={7} className="text-center py-20 text-slate-500 text-xs font-bold">
                  {t('table.noQuestionsFound')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
        <div className="p-4 border-t border-slate-850 flex flex-col sm:flex-row items-center justify-between gap-4 bg-slate-900/10">
          <span className="text-xs text-slate-400 font-semibold">
            {t('table.paginationDisplay', {
              from: filteredQuestions.length > 0 ? (qPage - 1) * qPageSize + 1 : 0,
              to: Math.min(qPage * qPageSize, filteredQuestions.length),
              total: filteredQuestions.length,
            })}
          </span>
          <Pagination page={qPage} totalPages={qTotalPages} onChange={setQPage} />
        </div>
      </div>
    </div>
  );
};
