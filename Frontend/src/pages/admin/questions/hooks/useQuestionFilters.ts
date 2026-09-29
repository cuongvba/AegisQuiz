/**
 * useQuestionFilters — Tách từ AdminQuestionsPage (index.tsx)
 * =============================================================
 * Gom toàn bộ state lọc/sắp xếp/phân trang cho tab "Câu hỏi" (trước đây nằm rải rác
 * trong 1 component 1971 dòng — vi phạm chuẩn nội bộ "NO God Components > 300 lines").
 * Hành vi giữ nguyên 100% so với bản gốc — chỉ di chuyển vị trí khai báo state/logic.
 *
 * FSD Layer: pages/admin/questions/hooks/
 */
import { useEffect, useMemo, useState } from 'react';
import type { LearnerQuestion } from '@/types/quiz';
import type { UserProfile } from '@/types/auth';
import { type BankTopic, getTopicFullPath } from '../types';

export type QuestionScopeFilter = 'ALL' | 'TEAM' | 'MY_QUESTIONS';

function readInitialScopeFilter(): QuestionScopeFilter {
  const param = new URLSearchParams(window.location.search).get('scope');
  if (param === 'TEAM') return 'TEAM';
  if (param === 'MY_QUESTIONS') return 'MY_QUESTIONS';
  return 'ALL';
}

export function useQuestionFilters(
  questions: LearnerQuestion[],
  topics: BankTopic[],
  user: UserProfile | null | undefined,
) {
  const [showMissingOnly, setShowMissingOnly] = useState(false);
  const [qSearch, setQSearch]           = useState('');
  const [qTypeFilter, setQTypeFilter]   = useState('');
  const [qTopicFilter, setQTopicFilter] = useState('');
  const [qDomainFilter, setQDomainFilter] = useState('ALL');
  const [qTagFilter, setQTagFilter]     = useState<string | null>(null);
  const [qScopeFilter, setQScopeFilter] = useState<QuestionScopeFilter>(readInitialScopeFilter);
  const [qPage, setQPage]               = useState(1);
  const [qSortField, setQSortField]     = useState('stt');
  const [qSortAsc, setQSortAsc]         = useState(true);
  const qPageSize = 10;

  // Reset về trang đầu mỗi khi bộ lọc thay đổi (giữ nguyên hành vi gốc)
  useEffect(() => setQPage(1), [qSearch, qTypeFilter, qTopicFilter, qDomainFilter, qTagFilter, qScopeFilter]);

  const missingAnswersCount = useMemo(() =>
    questions.filter(q => !q.answerRaw || !q.answerRaw.trim()).length, [questions]);

  const filteredQuestions = useMemo(() => {
    const list = questions.filter(q => {
      const searchLower = qSearch.toLowerCase().trim();
      let matchSearch = true;
      if (searchLower) {
        if (searchLower.startsWith('#')) {
          const tagKeyword = searchLower.substring(1);
          matchSearch = (q.tags || []).some(t => t.toLowerCase().includes(tagKeyword)) || q.content.toLowerCase().includes(searchLower);
        } else {
          matchSearch =
            q.content.toLowerCase().includes(searchLower) ||
            q.id.toLowerCase().includes(searchLower) ||
            (q.tags || []).some(t => t.toLowerCase().includes(searchLower)) ||
            (q.issuingOrg || '').toLowerCase().includes(searchLower) ||
            (q.targetLevel || '').toLowerCase().includes(searchLower);
        }
      }

      const matchType    = !qTypeFilter || q.questionType === qTypeFilter;
      const matchTopic   = !qTopicFilter || q.topicCode === qTopicFilter;
      const matchDomain  = !qDomainFilter || qDomainFilter === 'ALL' || q.domainCode === qDomainFilter;
      const matchTag     = !qTagFilter || (q.tags || []).some(t => t.toLowerCase() === qTagFilter.toLowerCase());
      const matchMissing = !showMissingOnly || !q.answerRaw || !q.answerRaw.trim();
      const matchScope =
        qScopeFilter === 'ALL'
          ? true
          : qScopeFilter === 'TEAM'
          ? (q.scope === 'TEAM' || (user?.orgUnitId && q.orgUnitId === user.orgUnitId))
          : qScopeFilter === 'MY_QUESTIONS'
          ? (user?.id && q.creatorId === user.id)
          : true;

      return matchSearch && matchType && matchTopic && matchDomain && matchTag && matchMissing && matchScope;
    });
    list.sort((a, b) => {
      let valA: any = '';
      let valB: any = '';
      if (qSortField === 'stt') { valA = questions.indexOf(a); valB = questions.indexOf(b); }
      else if (qSortField === 'content') { valA = a.content || ''; valB = b.content || ''; }
      else if (qSortField === 'questionType') { valA = a.questionType || ''; valB = b.questionType || ''; }
      else if (qSortField === 'topicCode') {
        const tA = topics.find(t => t.code === a.topicCode || t.categoryCode === a.topicCode);
        const tB = topics.find(t => t.code === b.topicCode || t.categoryCode === b.topicCode);
        valA = tA ? getTopicFullPath(tA, topics) : a.topicCode || '';
        valB = tB ? getTopicFullPath(tB, topics) : b.topicCode || '';
      }
      else if (qSortField === 'difficulty') { valA = a.difficulty || 0; valB = b.difficulty || 0; }
      if (typeof valA === 'string') return qSortAsc ? valA.localeCompare(valB, 'vi', { sensitivity: 'base' }) : valB.localeCompare(valA, 'vi', { sensitivity: 'base' });
      return qSortAsc ? valA - valB : valB - valA;
    });
    return list;
  }, [questions, qSearch, qTypeFilter, qTopicFilter, qDomainFilter, qTagFilter, qScopeFilter, qSortField, qSortAsc, showMissingOnly, topics, user]);

  const pagedQuestions = useMemo(() => {
    const start = (qPage - 1) * qPageSize;
    return filteredQuestions.slice(start, start + qPageSize);
  }, [filteredQuestions, qPage]);

  const qTotalPages = Math.ceil(filteredQuestions.length / qPageSize);

  // Reset về mặc định (dùng sau khi import hàng loạt câu hỏi mới)
  const resetFilters = () => {
    setQTopicFilter('');
    setQDomainFilter('ALL');
    setQTagFilter(null);
    setQSearch('');
    setQPage(1);
  };

  return {
    // filter state
    showMissingOnly, setShowMissingOnly,
    qSearch, setQSearch,
    qTypeFilter, setQTypeFilter,
    qTopicFilter, setQTopicFilter,
    qDomainFilter, setQDomainFilter,
    qTagFilter, setQTagFilter,
    qScopeFilter, setQScopeFilter,
    qPage, setQPage,
    qSortField, setQSortField,
    qSortAsc, setQSortAsc,
    qPageSize,
    // derived
    missingAnswersCount,
    filteredQuestions,
    pagedQuestions,
    qTotalPages,
    resetFilters,
  };
}
