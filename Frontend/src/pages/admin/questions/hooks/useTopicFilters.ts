/**
 * useTopicFilters — Tách từ AdminQuestionsPage (index.tsx)
 * ==========================================================
 * Gom state lọc/sắp xếp/phân trang cho tab "Chủ đề". Hành vi giữ nguyên 100%.
 *
 * FSD Layer: pages/admin/questions/hooks/
 */
import { useEffect, useMemo, useState } from 'react';
import type { BankTopic } from '../types';

export function useTopicFilters(topics: BankTopic[]) {
  const [tSearch, setTSearch]       = useState('');
  const [tPage, setTPage]           = useState(1);
  const [tSortField, setTSortField] = useState('code');
  const [tSortAsc, setTSortAsc]     = useState(true);
  const tPageSize = 10;

  useEffect(() => setTPage(1), [tSearch]);

  const filteredTopics = useMemo(() => {
    const list = topics.filter(t =>
      t.code.toLowerCase().includes(tSearch.toLowerCase()) ||
      t.name.toLowerCase().includes(tSearch.toLowerCase()) ||
      (t.description || '').toLowerCase().includes(tSearch.toLowerCase())
    );
    list.sort((a, b) => {
      let valA: any = '';
      let valB: any = '';
      if (tSortField === 'code') { valA = a.code; valB = b.code; }
      else if (tSortField === 'name') { valA = a.name; valB = b.name; }
      else if (tSortField === 'categoryCode') { valA = a.categoryCode; valB = b.categoryCode; }
      else if (tSortField === 'visibilityScope') { valA = a.visibilityScope; valB = b.visibilityScope; }
      return tSortAsc ? valA.localeCompare(valB, 'vi', { sensitivity: 'base' }) : valB.localeCompare(valA, 'vi', { sensitivity: 'base' });
    });
    return list;
  }, [topics, tSearch, tSortField, tSortAsc]);

  const pagedTopics = useMemo(() => {
    const start = (tPage - 1) * tPageSize;
    return filteredTopics.slice(start, start + tPageSize);
  }, [filteredTopics, tPage]);

  const tTotalPages = Math.ceil(filteredTopics.length / tPageSize);

  return {
    tSearch, setTSearch,
    tPage, setTPage,
    tSortField, setTSortField,
    tSortAsc, setTSortAsc,
    tPageSize,
    filteredTopics,
    pagedTopics,
    tTotalPages,
  };
}
