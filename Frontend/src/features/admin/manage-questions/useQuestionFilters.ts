/**
 * useQuestionFilters — Filter, Sort & Pagination logic
 * ======================================================
 * Tách từ AdminQuestionsPage.tsx — tất cả useMemo + state filter.
 * FSD Layer: features/admin/manage-questions/
 */

import { useMemo, useState, useEffect } from 'react';
import type { LearnerQuestion } from '@/types/quiz';
import type { BankTopic } from '@/pages/admin/questions/types';
import { getTopicFullPath } from '@/pages/admin/questions/types';

const PAGE_SIZE = 10;

interface UseQuestionFiltersReturn {
  // State
  search:            string;
  typeFilter:        string;
  topicFilter:       string;
  domainFilter:      string;
  tagFilter:         string | null;
  scopeFilter:       string;
  showMissingOnly:   boolean;
  sortField:         string;
  sortAsc:           boolean;
  page:              number;
  // Computed
  filteredQuestions: LearnerQuestion[];
  pagedQuestions:    LearnerQuestion[];
  totalPages:        number;
  missingCount:      number;
  availableTags:     string[];
  // Actions
  setSearch:         (v: string) => void;
  setTypeFilter:     (v: string) => void;
  setTopicFilter:    (v: string) => void;
  setDomainFilter:   (v: string) => void;
  setTagFilter:      (v: string | null) => void;
  setScopeFilter:    (v: string) => void;
  setShowMissingOnly:(v: boolean) => void;
  handleSort:        (field: string) => void;
  setPage:           (p: number) => void;
}

export function useQuestionFilters(
  questions:         LearnerQuestion[],
  topics:            BankTopic[],
  currentUserId?:    string,
  currentOrgUnitId?: string,
): UseQuestionFiltersReturn {
  const [search,          setSearch]          = useState('');
  const [typeFilter,      setTypeFilter]      = useState('');
  const [topicFilter,     setTopicFilter]     = useState('');
  const [domainFilter,    setDomainFilter]    = useState('ALL');
  const [tagFilter,       setTagFilter]       = useState<string | null>(null);
  const [scopeFilter,     setScopeFilter]     = useState('ALL');
  const [showMissingOnly, setShowMissingOnly] = useState(false);
  const [sortField,       setSortField]       = useState('stt');
  const [sortAsc,         setSortAsc]         = useState(true);
  const [page,            setPage]            = useState(1);

  // Reset page khi filter thay đổi
  useEffect(() => setPage(1), [search, typeFilter, topicFilter, domainFilter, tagFilter, scopeFilter, showMissingOnly]);

  const missingCount = useMemo(
    () => questions.filter((q) => !q.answerRaw || !q.answerRaw.trim()).length,
    [questions],
  );

  const availableTags = useMemo(() => {
    const tagSet = new Set<string>();
    questions.forEach((q) => q.tags?.forEach((t) => tagSet.add(t)));
    return Array.from(tagSet).sort();
  }, [questions]);

  const filteredQuestions = useMemo(() => {
    const list = questions.filter((q) => {
      const searchLower = search.toLowerCase().trim();
      let matchSearch = true;
      if (searchLower) {
        if (searchLower.startsWith('#')) {
          const tagKw = searchLower.substring(1);
          matchSearch = (q.tags ?? []).some((t) => t.toLowerCase().includes(tagKw))
            || q.content.toLowerCase().includes(searchLower);
        } else {
          matchSearch =
            q.content.toLowerCase().includes(searchLower) ||
            q.id.toLowerCase().includes(searchLower) ||
            (q.tags ?? []).some((t) => t.toLowerCase().includes(searchLower)) ||
            (q.issuingOrg ?? '').toLowerCase().includes(searchLower) ||
            (q.targetLevel ?? '').toLowerCase().includes(searchLower);
        }
      }
      const matchType    = !typeFilter   || q.questionType === typeFilter;
      const matchTopic   = !topicFilter  || q.topicCode    === topicFilter;
      const matchDomain  = domainFilter  === 'ALL' || q.domainCode === domainFilter;
      const matchTag     = !tagFilter    || (q.tags ?? []).some((t) => t.toLowerCase() === tagFilter.toLowerCase());
      const matchMissing = !showMissingOnly || !q.answerRaw || !q.answerRaw.trim();
      const matchScope   =
        scopeFilter === 'ALL'
          ? true
          : scopeFilter === 'TEAM'
          ? q.scope === 'TEAM' || (!!currentOrgUnitId && q.orgUnitId === currentOrgUnitId)
          : scopeFilter === 'MY_QUESTIONS'
          ? !!currentUserId && q.creatorId === currentUserId
          : true;
      return matchSearch && matchType && matchTopic && matchDomain && matchTag && matchMissing && matchScope;
    });

    // Sort
    list.sort((a, b) => {
      let valA: string | number = '';
      let valB: string | number = '';
      switch (sortField) {
        case 'stt':          valA = questions.indexOf(a); valB = questions.indexOf(b); break;
        case 'content':      valA = a.content || '';      valB = b.content || '';      break;
        case 'questionType': valA = a.questionType || ''; valB = b.questionType || ''; break;
        case 'topicCode': {
          const tA = topics.find((t) => t.code === a.topicCode || t.categoryCode === a.topicCode);
          const tB = topics.find((t) => t.code === b.topicCode || t.categoryCode === b.topicCode);
          valA = tA ? getTopicFullPath(tA, topics) : a.topicCode || '';
          valB = tB ? getTopicFullPath(tB, topics) : b.topicCode || '';
          break;
        }
        case 'difficulty':   valA = a.difficulty || 0;   valB = b.difficulty || 0;   break;
      }
      if (typeof valA === 'string') {
        return sortAsc
          ? valA.localeCompare(valB as string, 'vi', { sensitivity: 'base' })
          : (valB as string).localeCompare(valA, 'vi', { sensitivity: 'base' });
      }
      return sortAsc ? valA - (valB as number) : (valB as number) - valA;
    });

    return list;
  }, [questions, search, typeFilter, topicFilter, domainFilter, tagFilter, showMissingOnly, sortField, sortAsc, topics]);

  const pagedQuestions = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return filteredQuestions.slice(start, start + PAGE_SIZE);
  }, [filteredQuestions, page]);

  const totalPages = Math.max(1, Math.ceil(filteredQuestions.length / PAGE_SIZE));

  const handleSort = (field: string) => {
    if (field === sortField) setSortAsc((v) => !v);
    else { setSortField(field); setSortAsc(true); }
  };

  return {
    search, typeFilter, topicFilter, domainFilter, tagFilter, scopeFilter, showMissingOnly,
    sortField, sortAsc, page,
    filteredQuestions, pagedQuestions, totalPages, missingCount, availableTags,
    setSearch, setTypeFilter, setTopicFilter, setDomainFilter, setTagFilter, setScopeFilter,
    setShowMissingOnly, handleSort, setPage,
  };
}
