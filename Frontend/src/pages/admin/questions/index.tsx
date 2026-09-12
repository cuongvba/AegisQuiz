/**
 * AdminQuestionsPage — Refactored (Task A11)
 * =========================================
 * File gốc: 2024 dòng (107KB)
 * Sau refactor: ~250 dòng — tất cả logic tách ra thành modules:
 *
 *   questions/
 *   ├── types.ts                        — BankTopic, helper funcs
 *   ├── components/
 *   │   ├── QuestionModal.tsx            — Modal thêm/sửa câu hỏi
 *   │   ├── TopicModal.tsx               — Modal thêm/sửa chủ đề
 *   │   └── DocxPreviewModal.tsx         — Modal xem trước import DOCX + AI overlay
 *   └── index.tsx                        — File này (orchestrator)
 */
import { useState, useEffect, useRef, useMemo } from 'react';
import { FileUp, Plus, HelpCircle, Star, Sparkles, Layers, ArrowUpDown, Eye, Edit, ToggleLeft, ToggleRight, Loader2, FolderOpen, Brain, Wand2, Cpu, BookOpen, X, CheckCircle2, Unlink, Tag, Trash2, AlertTriangle, Network, FileSpreadsheet, Users, User, ShieldCheck, UploadCloud } from 'lucide-react';
import { learnerQuizService } from '@/services/learner-quiz.service';
import type { LearnerQuestion, QuestionType } from '@/types/quiz';
import { useAuthContext } from '@/app/providers/AuthProvider';
import { canManageResource } from '@/types/auth';
import { MathRenderer } from '@/components/common/MathRenderer';
import {
  type BankTopic,
  LETTERS,
  getQuestionTypeLabel,
  getTopicFullPath,
  isOptionSelectedAsCorrect,
} from './types';
import { QuestionModal }     from './components/QuestionModal';
import { TopicModal }        from './components/TopicModal';
import { DocxPreviewModal }  from './components/DocxPreviewModal';
import { AiGenerateModal }   from './components/AiGenerateModal';
import { RuleStudioTab }     from './components/RuleStudioTab';
import { BatchContextModal } from './components/BatchContextModal';
import { BatchTagModal }     from './components/BatchTagModal';
import { TenantGovernanceModal } from './components/TenantGovernanceModal';
import { ExcelIngestionStudioModal } from './components/ExcelIngestionStudioModal';
import { getSampleBankingUcisData } from './components/sampleBankingUcisData';

// ── Helper: correct answer display ────────────────────────────────────────────
function getCorrectAnswerDisplay(q: LearnerQuestion): { label: string; text: string } | null {
  if (!q.answerRaw || !q.answerRaw.trim()) return null;

  if (q.questionType === 'SINGLE' || q.questionType === 'TRUE_FALSE') {
    let idx = parseInt(q.answerRaw) - 1;
    // Polymorphic match if answerRaw is letter (e.g. "C") or option text (e.g. "Đáp án 1 và 2")
    if (isNaN(idx) || idx < 0 || !q.options || !q.options[idx]) {
      if (q.options && q.options.length > 0) {
        const found = q.options.findIndex((opt, i) => isOptionSelectedAsCorrect(opt, i, q.answerRaw, q.questionType, q.options));
        if (found !== -1) idx = found;
      }
    }
    if (q.options && q.options[idx] !== undefined) {
      const label = q.questionType === 'TRUE_FALSE'
        ? (idx === 0 ? 'Đúng (1)' : 'Sai (2)')
        : `${LETTERS[idx] || String(idx + 1)} (${idx + 1})`;
      return { label, text: q.options[idx] };
    }
    return { label: q.answerRaw, text: '' };
  }
  if (q.questionType === 'MULTI') {
    const labels: string[] = [];
    const texts: string[] = [];
    if (q.options && q.options.length > 0) {
      q.options.forEach((opt, idx) => {
        if (isOptionSelectedAsCorrect(opt, idx, q.answerRaw, 'MULTI', q.options)) {
          labels.push(`${LETTERS[idx] || String(idx + 1)} (${idx + 1})`);
          texts.push(`${LETTERS[idx] || String(idx + 1)}. ${opt}`);
        }
      });
    }
    if (labels.length > 0) {
      return { label: labels.join(', '), text: texts.join(' | ') };
    }
    return { label: q.answerRaw, text: '' };
  }
  return { label: q.answerRaw, text: '' };
}

// ── Pagination component ──────────────────────────────────────────────────────
function Pagination({ page, totalPages, onChange }: { page: number; totalPages: number; onChange: (p: number) => void }) {
  return (
    <div className="flex items-center gap-1.5">
      <button onClick={() => onChange(Math.max(1, page - 1))} disabled={page === 1} className="px-3 py-1.5 rounded-lg bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-855 disabled:opacity-40 disabled:cursor-not-allowed text-xs font-bold transition-all">Trước</button>
      {Array.from({ length: totalPages }).map((_, idx) => {
        const pNum = idx + 1;
        if (pNum === 1 || pNum === totalPages || Math.abs(pNum - page) <= 1) {
          return (
            <button key={pNum} onClick={() => onChange(pNum)} className={`px-3 py-1.5 rounded-lg border text-xs font-bold transition-all ${page === pNum ? 'bg-blue-650 border-blue-500 text-white shadow' : 'bg-slate-950 border-slate-850 text-slate-400 hover:bg-slate-850 hover:text-white'}`}>{pNum}</button>
          );
        }
        if (pNum === 2 || pNum === totalPages - 1) {
          return <span key={pNum} className="text-slate-600 text-xs px-1 select-none">...</span>;
        }
        return null;
      })}
      <button onClick={() => onChange(Math.min(totalPages, page + 1))} disabled={page === totalPages || totalPages === 0} className="px-3 py-1.5 rounded-lg bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-855 disabled:opacity-40 disabled:cursor-not-allowed text-xs font-bold transition-all">Sau</button>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// MAIN COMPONENT
// ─────────────────────────────────────────────────────────────────────────────
export function AdminQuestionsPage() {
  const { user } = useAuthContext();
  const isTeamLeader = user?.role === 'TeamLeader';

  const [activeTab, setActiveTab] = useState<'questions' | 'topics' | 'rules'>('questions');
  const [questions, setQuestions] = useState<LearnerQuestion[]>([]);
  const [topics, setTopics]       = useState<BankTopic[]>([]);
  const [loading, setLoading]     = useState(false);
  const [dragActive, setDragActive] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Role simulation
  const [userRoleOverride, setUserRoleOverride] = useState<'admin' | 'viewer'>('admin');
  const hasWritePermission = userRoleOverride === 'admin' || isTeamLeader;

  // Question modal state
  const [isModalOpen, setIsModalOpen]   = useState(false);
  const [isEditing, setIsEditing]       = useState(false);
  const [editingId, setEditingId]       = useState<string | null>(null);
  const [content, setContent]           = useState('');
  const [questionType, setQuestionType] = useState<QuestionType>('SINGLE');
  const [difficulty, setDifficulty]     = useState(1);
  const [categoryCode, setCategoryCode] = useState('');
  const [durationSeconds, setDurationSeconds] = useState(60);
  const [options, setOptions]           = useState<string[]>(['', '', '', '']);
  const [citation, setCitation]         = useState('');
  const [correctAnswer, setCorrectAnswer] = useState('');
  const [contentType, setContentType]   = useState('text');
  const [optionType, setOptionType]     = useState('text');

  // Enterprise Multi-Tier Scope & Attribution state
  const [formScope, setFormScope] = useState<'COMMUNITY' | 'TENANT' | 'TEAM' | 'PERSONAL'>('COMMUNITY');
  const [qScopeFilter, setQScopeFilter] = useState<'ALL' | 'TEAM' | 'MY_QUESTIONS'>(() => {
    const param = new URLSearchParams(window.location.search).get('scope');
    if (param === 'TEAM') return 'TEAM';
    if (param === 'MY_QUESTIONS') return 'MY_QUESTIONS';
    return 'ALL';
  });

  // Topic modal state
  const [isTopicModalOpen, setIsTopicModalOpen]   = useState(false);
  const [isEditingTopic, setIsEditingTopic]       = useState(false);
  const [editingTopicId, setEditingTopicId]       = useState<string | null>(null);
  const [topicCode, setTopicCode]   = useState('');
  const [topicName, setTopicName]   = useState('');
  const [topicDesc, setTopicDesc]   = useState('');
  const [topicCat, setTopicCat]     = useState('');
  const [topicVisibility, setTopicVisibility] = useState('PUBLIC');
  const [topicParentId, setTopicParentId]     = useState('');
  const [topicDomainCode, setTopicDomainCode] = useState('GENERAL');
  const [topicScope, setTopicScope]           = useState('COMMUNITY');

  // DOCX & PDF import state (Kịch bản A)
  const [docxPreviewList, setDocxPreviewList]       = useState<any[]>([]);
  const [showDocxPreviewModal, setShowDocxPreviewModal] = useState(false);
  const [isDocxProcessing, setIsDocxProcessing]     = useState(false);

  // Excel DOT-2026 import state
  const [excelPreviewData, setExcelPreviewData]         = useState<any | null>(null);
  const [showExcelPreviewModal, setShowExcelPreviewModal] = useState(false);
  const [showExcelStudioModal, setShowExcelStudioModal] = useState(false);
  const [excelSelectedSheets, setExcelSelectedSheets]   = useState<Set<number>>(new Set());
  const [isExcelImporting, setIsExcelImporting]         = useState(false);

  // AI Generate modal state (Kịch bản B)
  const [showAiGenerateModal, setShowAiGenerateModal] = useState(false);
  // Universal Context Modal state
  const [selectedContext, setSelectedContext] = useState<{ title: string; content: string } | null>(null);
  const [availableContexts, setAvailableContexts] = useState<any[]>([]);
  const [currentContextId, setCurrentContextId] = useState('');
  const [currentContextTitle, setCurrentContextTitle] = useState('');
  const [currentContextContent, setCurrentContextContent] = useState('');
  const [isStickyContext, setIsStickyContext] = useState(false);
  const [stickyCount, setStickyCount] = useState(0);

  // Range Selection & Batch Action state
  const [selectedQuestionIds, setSelectedQuestionIds] = useState<string[]>([]);
  const [rangeFrom, setRangeFrom] = useState('');
  const [rangeTo, setRangeTo] = useState('');
  const [showBatchContextModal, setShowBatchContextModal] = useState(false);
  const [isBatchAssigning, setIsBatchAssigning] = useState(false);

  // Multi-Industry & Smart Tags state
  const [qDomainFilter, setQDomainFilter] = useState('ALL');
  const [qTagFilter, setQTagFilter] = useState<string | null>(null);
  const [showBatchTagModal, setShowBatchTagModal] = useState(false);
  const [isBatchTagging, setIsBatchTagging] = useState(false);
  const [showGovernanceModal, setShowGovernanceModal] = useState(false);

  // Form states for Taxonomy & Tags
  const [formDomainCode, setFormDomainCode] = useState('EDUCATION');
  const [formTags, setFormTags] = useState<string[]>([]);
  const [formTargetLevel, setFormTargetLevel] = useState('');
  const [formAssessmentPurpose, setFormAssessmentPurpose] = useState('');
  const [formIssuingOrg, setFormIssuingOrg] = useState('');
  const [formBenchmarkYear, setFormBenchmarkYear] = useState<number | undefined>(2025);

  // Filter / pagination — questions
  const [showMissingOnly, setShowMissingOnly] = useState(false);
  const [qSearch, setQSearch]       = useState('');
  const [qTypeFilter, setQTypeFilter] = useState('');
  const [qTopicFilter, setQTopicFilter] = useState('');
  const [qPage, setQPage]           = useState(1);
  const [qSortField, setQSortField] = useState('stt');
  const [qSortAsc, setQSortAsc]     = useState(true);
  const qPageSize = 10;

  // Filter / pagination — topics
  const [tSearch, setTSearch]       = useState('');
  const [tPage, setTPage]           = useState(1);
  const [tSortField, setTSortField] = useState('code');
  const [tSortAsc, setTSortAsc]     = useState(true);
  const tPageSize = 10;

  const missingAnswersCount = useMemo(() =>
    questions.filter(q => !q.answerRaw || !q.answerRaw.trim()).length, [questions]);

  // Reset pages on filter change
  useEffect(() => setQPage(1), [qSearch, qTypeFilter, qTopicFilter, qDomainFilter, qTagFilter, qScopeFilter]);
  useEffect(() => setTPage(1), [tSearch]);

  // ── Data Fetch ──────────────────────────────────────────────────────────────
  const fetchData = async () => {
    setLoading(true);
    try {
      const [questionsData, topicsData, contextsData] = await Promise.all([
        learnerQuizService.getQuestions(),
        learnerQuizService.getTopics(),
        learnerQuizService.getContexts(),
      ]);
      setQuestions(questionsData);
      setTopics(topicsData);
      setAvailableContexts(contextsData);
      if (topicsData.length > 0) setCategoryCode(topicsData[0].code);
    } catch (err) {
      console.error(err);
    }
    setLoading(false);
  };

  useEffect(() => { fetchData(); }, []);

  // ── Sort helper ─────────────────────────────────────────────────────────────
  const renderSortIcon = (field: string, currentField: string, isAsc: boolean) => (
    <span className={`inline-flex ml-1.5 transition-colors ${field === currentField ? 'text-blue-400' : 'text-slate-600 group-hover:text-slate-400'}`}>
      <ArrowUpDown size={11} className={field === currentField && !isAsc ? 'rotate-180 transform transition-transform' : 'transition-transform'} />
    </span>
  );

  // ── Computed lists ──────────────────────────────────────────────────────────
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

  const pagedQuestions = useMemo(() => {
    const start = (qPage - 1) * qPageSize;
    return filteredQuestions.slice(start, start + qPageSize);
  }, [filteredQuestions, qPage, qPageSize]);

  const qTotalPages = Math.ceil(filteredQuestions.length / qPageSize);

  const pagedTopics = useMemo(() => {
    const start = (tPage - 1) * tPageSize;
    return filteredTopics.slice(start, start + tPageSize);
  }, [filteredTopics, tPage, tPageSize]);

  const tTotalPages = Math.ceil(filteredTopics.length / tPageSize);

  // ── Import handlers ─────────────────────────────────────────────────────────
  const handleFileImport = async (file: File) => {
    setIsDocxProcessing(true);
    try {
      const res = await learnerQuizService.importQuestions(file);
      // Kết quả mới trả về: { fileName, totalSheets, totalQuestions, warnings, sheets: [...] }
      if (res && res.sheets && Array.isArray(res.sheets)) {
        const allQuestions: any[] = [];
        const isBanking = /tin[\s_-]?dung|tham[\s_-]?dinh|ke[\s_-]?toan|kiem[\s_-]?ngan|thanh[\s_-]?toan|ngan[\s_-]?hang|bank|khdn|khcn|xu[\s_-]?ly[\s_-]?no|nstl/i.test(file.name);
        const defaultDomain = isBanking ? 'BANKING' : 'GENERAL';
        const globalDomain = res.detectedDomainCode || defaultDomain;
        const globalTargetLevel = res.detectedTargetLevel;
        const globalAssessmentPurpose = res.detectedAssessmentPurpose;
        const globalIssuingOrg = res.detectedIssuingOrg;
        const globalBenchmarkYear = res.detectedBenchmarkYear;
        const globalBenchmarkStandard = res.detectedBenchmarkStandard;
        const globalTags = Array.isArray(res.detectedTags) ? res.detectedTags : [];

        res.sheets.forEach((sheet: any) => {
          (sheet.questions || []).forEach((q: any) => {
            allQuestions.push({
              ...q,
              topicCode: q.topicCode || sheet.detectedTopicCode || (topics.length > 0 ? topics[0].code : 'GENERAL'),
              domainCode: q.domainCode || sheet.detectedDomainCode || globalDomain,
              targetLevel: q.targetLevel || sheet.detectedTargetLevel || globalTargetLevel,
              assessmentPurpose: q.assessmentPurpose || sheet.detectedAssessmentPurpose || globalAssessmentPurpose,
              issuingOrg: q.issuingOrg || sheet.detectedIssuingOrg || globalIssuingOrg,
              benchmarkYear: q.benchmarkYear || sheet.detectedBenchmarkYear || globalBenchmarkYear,
              benchmarkStandard: q.benchmarkStandard || sheet.detectedBenchmarkStandard || globalBenchmarkStandard,
              tags: (Array.isArray(q.tags) && q.tags.length > 0)
                ? q.tags
                : ((Array.isArray(sheet.detectedTags) && sheet.detectedTags.length > 0)
                    ? sheet.detectedTags
                    : globalTags),
              difficulty: q.difficulty || 3,
            });
          });
        });

        if (allQuestions.length === 0) {
          const warningMsg = res.warnings?.length > 0 ? `\n\nChi tiết cảnh báo:\n• ${res.warnings.join('\n• ')}` : '';
          alert(`Không tìm thấy câu hỏi hợp lệ trong file Excel.${warningMsg}`);
          return;
        }

        // Mở Universal Cognitive Excel Ingestion Studio (UCIS v3.0)
        setExcelPreviewData(res);
        setDocxPreviewList(allQuestions);
        setShowExcelStudioModal(true);
      } else {
        // Fallback: server trả về dạng cũ
        alert(`Đã nhập thành công ${res.count ?? 0} câu hỏi từ file Excel!`);
        fetchData();
      }
    } catch (err: any) {
      alert(err.response?.data?.message || err.message || 'Nhập câu hỏi từ file Excel thất bại. Vui lòng thử lại.');
    } finally {
      setIsDocxProcessing(false);
    }
  };

  const handleConfirmStudioImport = async (questionsToImport: any[]) => {
    setIsDocxProcessing(true);
    try {
      const tagged = questionsToImport.map(q => ({
        ...q,
        creatorId: user?.id,
        creatorName: user?.name || user?.email,
        orgUnitId: user?.orgUnitId,
        orgUnitName: user?.orgUnitName || (user?.orgUnitId ? 'Phòng GD/Chi Nhánh' : undefined),
        scope: isTeamLeader ? 'TEAM' : (q.scope || 'COMMUNITY'),
        visibilityScope: isTeamLeader ? 'TEAM' : (q.visibilityScope || 'PUBLIC'),
        contributionStatus: isTeamLeader ? 'TEAM_PUBLISHED' : undefined,
      }));
      await learnerQuizService.confirmImportQuestionsExcel(tagged);
      setShowExcelStudioModal(false);
      alert(`Đã nạp thành công ${questionsToImport.length} câu hỏi vào ngân hàng câu hỏi!`);
      fetchData();
    } catch (err: any) {
      alert(err.response?.data?.message || err.message || 'Nạp câu hỏi thất bại. Vui lòng thử lại.');
    } finally {
      setIsDocxProcessing(false);
    }
  };

  const handleOpenExcelStudio = () => {
    if (!excelPreviewData) {
      setExcelPreviewData(getSampleBankingUcisData());
    }
    setShowExcelStudioModal(true);
  };

  const handleLoadStudioSample = () => {
    setExcelPreviewData(getSampleBankingUcisData());
    setShowExcelStudioModal(true);
  };

  const handleConfirmExcelImport = async () => {
    if (!excelPreviewData) return;
    setIsExcelImporting(true);
    try {
      // Gom tất cả câu hỏi từ các sheet được chọn
      const allItems: any[] = [];
      excelPreviewData.sheets.forEach((sheet: any, idx: number) => {
        if (excelSelectedSheets.has(idx)) {
          allItems.push(...sheet.questions);
        }
      });
      if (allItems.length === 0) {
        alert('Vui lòng chọn ít nhất một sheet để nhập.');
        return;
      }
      const taggedItems = allItems.map(q => ({
        ...q,
        creatorId: user?.id,
        creatorName: user?.name || user?.email,
        orgUnitId: user?.orgUnitId,
        orgUnitName: user?.orgUnitName || (user?.orgUnitId ? 'Phòng GD/Chi Nhánh' : undefined),
        scope: isTeamLeader ? 'TEAM' : (q.scope || 'COMMUNITY'),
        visibilityScope: isTeamLeader ? 'TEAM' : (q.visibilityScope || 'PUBLIC'),
        contributionStatus: isTeamLeader ? 'TEAM_PUBLISHED' : undefined,
      }));
      const res = await learnerQuizService.confirmImportQuestionsExcel(taggedItems);
      setShowExcelPreviewModal(false);
      setExcelPreviewData(null);
      // Reset bộ lọc về trang đầu rồi tải lại dữ liệu
      setQTopicFilter('');
      setQDomainFilter('ALL');
      setQTagFilter(null);
      setQSearch('');
      setQPage(1);
      await fetchData();
      alert(`✅ Đã nhập thành công ${res?.count ?? allItems.length} câu hỏi từ Excel!`);
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lưu câu hỏi Excel thất bại. Vui lòng thử lại.');
    } finally {
      setIsExcelImporting(false);
    }
  };


  const handleFileImportDocxOrPdf = async (file: File) => {
    setIsDocxProcessing(true);
    try {
      const isPdf = file.name.toLowerCase().endsWith('.pdf');
      const data = isPdf
        ? await learnerQuizService.importQuestionsPdf(file)
        : await learnerQuizService.importQuestionsDocx(file);
      if (!Array.isArray(data) || data.length === 0) {
        alert('Không tìm thấy câu hỏi hợp lệ trong tệp. Vui lòng kiểm tra cấu trúc định dạng file Word/PDF.');
        return;
      }
      const mappedData = data.map((item: any) => ({
        ...item,
        topicCode: item.topicCode || (topics.length > 0 ? topics[0].code : 'PARTY_BUILDING'),
        difficulty: item.difficulty || 3,
      }));
      setDocxPreviewList(mappedData);
      setShowDocxPreviewModal(true);
    } catch (err: any) {
      alert(err.response?.data?.message || 'Nhập câu hỏi từ tệp thất bại. Vui lòng kiểm tra lại định dạng file hoặc liên hệ quản trị viên.');
    } finally {
      setIsDocxProcessing(false);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files?.[0]) {
      const file = e.target.files[0];
      const lower = file.name.toLowerCase();
      if (lower.endsWith('.docx') || lower.endsWith('.pdf')) {
        handleFileImportDocxOrPdf(file);
      } else {
        handleFileImport(file);
      }
    }
  };

  // ── Question CRUD ───────────────────────────────────────────────────────────
  const openAddModal = () => {
    setEditingId(null); setContent(''); setQuestionType('SINGLE'); setDifficulty(1);
    setCategoryCode(topics.length > 0 ? topics[0].code : ''); setDurationSeconds(60);
    setOptions(['', '', '', '']); setCitation(''); setCorrectAnswer('');
    setContentType('text'); setOptionType('text'); setIsEditing(true);
    setFormScope(isTeamLeader ? 'TEAM' : 'COMMUNITY');
    if (!isStickyContext) {
      setCurrentContextId('');
      setCurrentContextTitle('');
      setCurrentContextContent('');
      setStickyCount(0);
      setFormDomainCode('EDUCATION');
      setFormTags([]);
      setFormTargetLevel('');
      setFormAssessmentPurpose('');
      setFormIssuingOrg('');
      setFormBenchmarkYear(2025);
    }
    setIsModalOpen(true);
  };

  const openEditModal = (q: LearnerQuestion) => {
    setEditingId(q.id); setContent(q.content); setQuestionType(q.questionType);
    setDifficulty(q.difficulty); setCategoryCode(q.topicCode || (topics.length > 0 ? topics[0].code : ''));
    setDurationSeconds(q.durationSec || 60);
    const optList = q.options && q.options.length > 0 ? [...q.options] : [];
    while (optList.length < 4) optList.push('');
    setOptions(optList); setCitation(q.citation || '');
    let initialAnswer = q.answerRaw || '';
    if (q.questionType === 'SINGLE' && optList.length > 0 && initialAnswer) {
      const matchedIdx = optList.findIndex((opt, i) => isOptionSelectedAsCorrect(opt, i, initialAnswer, 'SINGLE', optList));
      if (matchedIdx !== -1) {
        initialAnswer = String(matchedIdx + 1);
      }
    }
    setCorrectAnswer(initialAnswer);
    setContentType(q.contentType || q.questionMediaType || 'text');
    setOptionType(q.optionType || q.optionMediaType || 'text');
    setCurrentContextId(q.contextId || '');
    setCurrentContextTitle(q.contextTitle || '');
    setCurrentContextContent(q.contextContent || '');
    setFormDomainCode(q.domainCode || 'EDUCATION');
    setFormTags(q.tags || []);
    setFormTargetLevel(q.targetLevel || '');
    setFormAssessmentPurpose(q.assessmentPurpose || '');
    setFormIssuingOrg(q.issuingOrg || '');
    setFormBenchmarkYear(q.benchmarkYear || 2025);
    setFormScope(q.scope || (isTeamLeader ? 'TEAM' : 'COMMUNITY'));
    setIsEditing(false); setIsModalOpen(true);
  };

  const handleToggleActive = async (q: LearnerQuestion) => {
    if (!canManageResource(user, q)) return alert('Bạn không có quyền thay đổi trạng thái câu hỏi này.');
    const nextEnabled = q.enabled === false ? true : false;
    try {
      const payload = { content: q.content, questionType: q.questionType, difficulty: q.difficulty, durationSeconds: q.durationSec, categoryCode: q.topicCode || '', options: q.options, correctOption: q.answerRaw, citation: q.citation, contentType: q.contentType, optionType: q.optionType, payload: { correctAnswer: q.answerRaw, options: q.options, enabled: nextEnabled } };
      await learnerQuizService.updateQuestion(q.id, payload);
      setQuestions(prev => prev.map(item => item.id === q.id ? { ...item, enabled: nextEnabled } : item));
    } catch { alert('Không thể cập nhật trạng thái câu hỏi.'); }
  };

  const handleDelete = async (id: string) => {
    const targetQ = questions.find(x => x.id === id);
    if (!canManageResource(user, targetQ)) return alert('Bạn không có quyền xoá câu hỏi này.');
    if (!window.confirm('Bạn có chắc chắn muốn xoá câu hỏi này?')) return;
    try { await learnerQuizService.deleteQuestion(id); fetchData(); }
    catch { alert('Không thể xoá câu hỏi.'); }
  };

  const handleContributeToTenant = async (q: LearnerQuestion) => {
    if (!confirm(`Bạn có muốn đề xuất câu hỏi này lên Ngân hàng Toàn Cơ Quan không?\nSau khi đề xuất, Hội đồng Chuyên môn / Quản trị viên sẽ phê duyệt.`)) return;
    try {
      await learnerQuizService.updateQuestion(q.id, {
        content: q.content,
        questionType: q.questionType,
        difficulty: q.difficulty,
        durationSeconds: q.durationSec,
        categoryCode: q.topicCode || '',
        options: q.options,
        correctOption: q.answerRaw,
        contributionStatus: 'SUBMITTED_FOR_TENANT',
      } as any);
      alert('🎉 Đã gửi đề xuất câu hỏi lên cấp Toàn Cơ Quan thành công!');
      fetchData();
    } catch {
      alert('Không thể gửi đề xuất. Vui lòng thử lại.');
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    const activeQ = editingId ? questions.find(q => q.id === editingId) : null;
    if (editingId && !canManageResource(user, activeQ)) {
      return alert('Bạn chỉ có thể cập nhật câu hỏi do chính bạn hoặc nhóm bạn tạo ra.');
    }

    const assignedScope = isTeamLeader ? 'TEAM' : formScope;
    const payload = {
      content, questionType, difficulty, durationSeconds, categoryCode,
      options: options.filter(x => x.trim() !== ''),
      correctOption: correctAnswer, citation, contentType, optionType,
      contextId: currentContextId || undefined,
      contextTitle: currentContextTitle || undefined,
      contextContent: currentContextContent || undefined,
      domainCode: formDomainCode,
      tags: formTags,
      targetLevel: formTargetLevel || undefined,
      assessmentPurpose: formAssessmentPurpose || undefined,
      issuingOrg: formIssuingOrg || undefined,
      benchmarkYear: formBenchmarkYear,
      // Multi-tier Ownership & Attribution
      creatorId: editingId && activeQ?.creatorId ? activeQ.creatorId : user?.id,
      creatorName: editingId && activeQ?.creatorName ? activeQ.creatorName : (user?.name || user?.email),
      orgUnitId: editingId && activeQ?.orgUnitId ? activeQ.orgUnitId : user?.orgUnitId,
      orgUnitName: editingId && activeQ?.orgUnitName ? activeQ.orgUnitName : (user?.orgUnitName || (user?.orgUnitId ? 'Phòng GD/Chi Nhánh' : undefined)),
      scope: assignedScope,
      visibilityScope: assignedScope === 'TEAM' ? 'TEAM' : 'PUBLIC',
      contributionStatus: isTeamLeader && !editingId ? 'TEAM_PUBLISHED' : (activeQ?.contributionStatus || undefined),
    };
    try {
      if (editingId) await learnerQuizService.updateQuestion(editingId, payload);
      else {
        await learnerQuizService.createQuestion(payload);
        if (isStickyContext) {
          setStickyCount(prev => prev + 1);
        } else {
          setCurrentContextId('');
          setCurrentContextTitle('');
          setCurrentContextContent('');
        }
      }
      setIsModalOpen(false); fetchData();
    } catch { alert('Lỗi lưu thông tin câu hỏi. Vui lòng thử lại.'); }
  };

  const handleSaveAndContinue = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim()) {
      alert('Vui lòng nhập nội dung câu hỏi.');
      return;
    }
    const assignedScope = isTeamLeader ? 'TEAM' : formScope;
    const payload = {
      content, questionType, difficulty, durationSeconds, categoryCode,
      options: options.filter(x => x.trim() !== ''),
      correctOption: correctAnswer, citation, contentType, optionType,
      contextId: currentContextId || undefined,
      contextTitle: currentContextTitle || undefined,
      contextContent: currentContextContent || undefined,
      domainCode: formDomainCode,
      tags: formTags,
      targetLevel: formTargetLevel || undefined,
      assessmentPurpose: formAssessmentPurpose || undefined,
      issuingOrg: formIssuingOrg || undefined,
      benchmarkYear: formBenchmarkYear,
      // Multi-tier Ownership & Attribution
      creatorId: user?.id,
      creatorName: user?.name || user?.email,
      orgUnitId: user?.orgUnitId,
      orgUnitName: user?.orgUnitName || (user?.orgUnitId ? 'Phòng GD/Chi Nhánh' : undefined),
      scope: assignedScope,
      visibilityScope: assignedScope === 'TEAM' ? 'TEAM' : 'PUBLIC',
      contributionStatus: isTeamLeader ? 'TEAM_PUBLISHED' : undefined,
    };
    try {
      await learnerQuizService.createQuestion(payload);
      setStickyCount(prev => prev + 1);
      setIsStickyContext(true);
      // Reset question-specific fields for the next question
      setContent('');
      setOptions(['', '', '', '']);
      setCorrectAnswer('');
      setCitation('');
      fetchData();
    } catch {
      alert('Lỗi lưu câu hỏi. Vui lòng thử lại.');
    }
  };

  // ── Range Selection & Batch Handlers ──────────────────────────────────────
  const handleToggleSelect = (id: string) => {
    setSelectedQuestionIds(prev =>
      prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]
    );
  };

  const handleToggleSelectAll = () => {
    const allFilteredIds = filteredQuestions.map(q => q.id);
    const isAllSelected = allFilteredIds.length > 0 && allFilteredIds.every(id => selectedQuestionIds.includes(id));
    if (isAllSelected) {
      setSelectedQuestionIds(prev => prev.filter(id => !allFilteredIds.includes(id)));
    } else {
      setSelectedQuestionIds(prev => Array.from(new Set([...prev, ...allFilteredIds])));
    }
  };

  const handleSelectRange = () => {
    const from = parseInt(rangeFrom);
    const to = parseInt(rangeTo);
    if (isNaN(from) || isNaN(to) || from < 1 || to < from) {
      alert('Vui lòng nhập dải số thứ tự hợp lệ (ví dụ: Từ câu 15 đến câu 20).');
      return;
    }
    const targetQuestions = filteredQuestions.slice(from - 1, to);
    if (targetQuestions.length === 0) {
      alert('Không tìm thấy câu hỏi nào trong dải STT này.');
      return;
    }
    const ids = targetQuestions.map(q => q.id);
    setSelectedQuestionIds(prev => Array.from(new Set([...prev, ...ids])));
  };

  const handleBatchAssignConfirm = async (data: { contextId?: string; contextTitle?: string; contextContent?: string }) => {
    if (selectedQuestionIds.length === 0) return;
    setIsBatchAssigning(true);
    try {
      const res = await learnerQuizService.batchAssignContext({
        questionIds: selectedQuestionIds,
        ...data,
      });
      alert(`🎉 Đã gán bài đọc hiểu dùng chung cho ${res.count || selectedQuestionIds.length} câu hỏi thành công!`);
      setSelectedQuestionIds([]);
      setShowBatchContextModal(false);
      await fetchData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi gán bài đọc hiểu.');
    } finally {
      setIsBatchAssigning(false);
    }
  };

  const handleBatchRemoveContext = async () => {
    if (selectedQuestionIds.length === 0) return;
    if (!window.confirm(`Bạn có chắc chắn muốn gỡ bài đọc hiểu khỏi ${selectedQuestionIds.length} câu hỏi đã chọn?`)) return;
    try {
      await learnerQuizService.batchRemoveContext(selectedQuestionIds);
      alert(`Đã gỡ bài đọc hiểu khỏi ${selectedQuestionIds.length} câu hỏi.`);
      setSelectedQuestionIds([]);
      await fetchData();
    } catch {
      alert('Không thể gỡ bài đọc hiểu.');
    }
  };

  const handleBatchAssignTagsConfirm = async (data: {
    tags: string[];
    domainCode: string;
    targetLevel?: string;
    assessmentPurpose?: string;
    issuingOrg?: string;
    benchmarkYear?: number;
    benchmarkStandard?: string;
  }) => {
    if (selectedQuestionIds.length === 0) return;
    setIsBatchTagging(true);
    try {
      const res = await learnerQuizService.batchAssignTags({
        questionIds: selectedQuestionIds,
        ...data,
      });
      alert(`🎉 Đã gán thẻ & miền ngành cho ${res.count || selectedQuestionIds.length} câu hỏi thành công!`);
      setSelectedQuestionIds([]);
      setShowBatchTagModal(false);
      await fetchData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi gán thẻ hàng loạt.');
    } finally {
      setIsBatchTagging(false);
    }
  };

  const handleSuggestAnswer = async (q: LearnerQuestion) => {
    setIsDocxProcessing(true);
    try {
      const res = await learnerQuizService.suggestQuestionAnswer(q.id);
      openEditModal(q); setIsEditing(true); setCorrectAnswer(res.suggestedAnswer || ''); setCitation(res.aiExplanation || '');
    } catch (err: any) { alert(err.response?.data?.message || 'Không thể lấy gợi ý từ AI.'); }
    finally { setIsDocxProcessing(false); }
  };

  // ── Topic CRUD ──────────────────────────────────────────────────────────────
  const openAddTopicModal = () => {
    setEditingTopicId(null); setTopicCode(''); setTopicName(''); setTopicDesc('');
    setTopicCat(''); setTopicVisibility('PUBLIC'); setTopicParentId('');
    setTopicDomainCode('GENERAL'); setTopicScope('COMMUNITY');
    setIsEditingTopic(true); setIsTopicModalOpen(true);
  };

  const openEditTopicModal = (topic: BankTopic) => {
    setEditingTopicId(topic.id); setTopicCode(topic.code); setTopicName(topic.name);
    setTopicDesc(topic.description || ''); setTopicCat(topic.categoryCode);
    setTopicVisibility(topic.visibilityScope); setTopicParentId(topic.parentId || '');
    setTopicDomainCode(topic.domainCode || 'GENERAL'); setTopicScope(topic.scope || 'COMMUNITY');
    setIsEditingTopic(false); setIsTopicModalOpen(true);
  };

  // State quản lý xóa chủ đề an toàn
  const [topicToDelete, setTopicToDelete] = useState<BankTopic | null>(null);
  const [isDeletingTopicLoading, setIsDeletingTopicLoading] = useState(false);

  const handleDeleteTopic = (id: string) => {
    const topic = topics.find(t => t.id === id);
    if (topic) {
      setTopicToDelete(topic);
    }
  };

  const confirmDeleteTopic = async (deleteQuestions: boolean) => {
    if (!topicToDelete) return;
    setIsDeletingTopicLoading(true);
    try { 
      const res: any = await learnerQuizService.deleteTopic(topicToDelete.id, deleteQuestions); 
      if (res?.message) alert(res.message);
      setTopicToDelete(null);
      await fetchData(); 
    }
    catch (err: any) { 
      alert(err.response?.data?.message || 'Không thể xoá chủ đề.'); 
    }
    finally {
      setIsDeletingTopicLoading(false);
    }
  };

  const [isAiSolving, setIsAiSolving] = useState(false);
  const [isConvertingLatex, setIsConvertingLatex] = useState(false);

  const handleAiSolveBatch = async () => {
    setIsAiSolving(true);
    try {
      const solved = await learnerQuizService.aiSolvePreview(docxPreviewList);
      setDocxPreviewList(solved);
      alert(`Gemini AI đã hoàn tất giải đáp án và trích dẫn quy định cho ${solved.length} câu hỏi!`);
    } catch (err: any) {
      alert(err.response?.data?.message || 'Không thể gọi AI giải tự động.');
    } finally {
      setIsAiSolving(false);
    }
  };

  const handleConvertLatex = async () => {
    setIsConvertingLatex(true);
    try {
      const converted = await learnerQuizService.convertMathImagesToLatex(docxPreviewList);
      setDocxPreviewList(converted);
      alert(`Đã hoàn tất nhận diện & chuyển đổi công thức toán sang mã LaTeX cho ${converted.length} câu hỏi!`);
    } catch (err: any) {
      alert(err.response?.data?.message || 'Không thể gọi AI chuyển đổi LaTeX.');
    } finally {
      setIsConvertingLatex(false);
    }
  };

  const handleToggleActiveTopic = async (topic: BankTopic) => {
    if (!hasWritePermission) return;
    const nextEnabled = topic.enabled !== false ? false : true;
    try {
      const payload = { code: topic.code, name: topic.name, description: topic.description || '', categoryCode: topic.categoryCode, parentId: topic.parentId || null, enabled: nextEnabled, visibilityScope: topic.visibilityScope };
      await learnerQuizService.updateTopic(topic.id, payload);
      setTopics(prev => prev.map(item => item.id === topic.id ? { ...item, enabled: nextEnabled } : item));
    } catch { alert('Không thể thay đổi trạng thái chủ đề.'); }
  };

  const handleSaveTopic = async (e: React.FormEvent) => {
    e.preventDefault();
    const existing = topics.find(t => t.id === editingTopicId);
    const payload = { 
      code: topicCode, 
      name: topicName, 
      description: topicDesc, 
      categoryCode: topicCat || topicCode, 
      parentId: topicParentId || null, 
      enabled: existing ? existing.enabled !== false : true, 
      visibilityScope: topicVisibility,
      domainCode: topicDomainCode,
      scope: topicScope
    };
    try {
      if (editingTopicId) await learnerQuizService.updateTopic(editingTopicId, payload);
      else await learnerQuizService.createTopic(payload);
      setIsTopicModalOpen(false); fetchData();
    } catch { alert('Lỗi lưu thông tin chủ đề.'); }
  };

  // ── DOCX confirm ─────────────────────────────────────────────────────────────
  const handleConfirmDocxImport = async () => {
    setLoading(true);
    try {
      const tagged = docxPreviewList.map(q => ({
        ...q,
        creatorId: user?.id,
        creatorName: user?.name || user?.email,
        orgUnitId: user?.orgUnitId,
        orgUnitName: user?.orgUnitName || (user?.orgUnitId ? 'Phòng GD/Chi Nhánh' : undefined),
        scope: isTeamLeader ? 'TEAM' : (q.scope || 'COMMUNITY'),
        visibilityScope: isTeamLeader ? 'TEAM' : (q.visibilityScope || 'PUBLIC'),
        contributionStatus: isTeamLeader ? 'TEAM_PUBLISHED' : undefined,
      }));
      const res = await learnerQuizService.confirmImportQuestionsDocx(tagged);
      alert(`Đã lưu thành công ${res.count} câu hỏi từ bản nháp vào hệ thống!`);
      setShowDocxPreviewModal(false);
      // Reset các bộ lọc và đưa về trang 1 để câu hỏi mới import xuất hiện ngay lập tức trên lưới
      setQTopicFilter('');
      setQDomainFilter('ALL');
      setQTagFilter(null);
      setQSearch('');
      setQPage(1);
      await fetchData();
    } catch (err: any) {
      console.error(err);
      alert(err.response?.data?.message || 'Lỗi lưu câu hỏi vào hệ thống.');
    }
    finally { setLoading(false); }
  };

  const handleLoadPresetSample = async (preset: 'MATH' | 'GPLX' | 'GPLX_F2023') => {
    setLoading(true);
    try {
      const res = await learnerQuizService.loadPresetSample(preset);
      alert(`⚡ Nạp thành công: ${res.message || res.count + ' câu hỏi'}`);
      await fetchData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi nạp bộ đề mẫu.');
    } finally {
      setLoading(false);
    }
  };

  // ── RENDER ───────────────────────────────────────────────────────────────────
  return (
    <div className="space-y-8 font-sans">
      {/* Missing answers banner */}
      {missingAnswersCount > 0 && (
        <div className="bg-gradient-to-r from-amber-500/10 to-orange-500/10 border border-amber-500/30 rounded-2xl p-4 flex items-center justify-between gap-4 backdrop-blur-md">
          <div className="flex items-center gap-3">
            <span className="w-10 h-10 bg-amber-500/20 text-amber-400 rounded-xl flex items-center justify-center font-bold text-lg">⚠️</span>
            <div className="text-left text-white">
              <h4 className="text-sm font-bold text-slate-200">Phát hiện câu hỏi chưa có đáp án</h4>
              <p className="text-xs text-slate-400 mt-0.5">Hệ thống quét thấy có {missingAnswersCount} câu hỏi chưa được cấu hình đáp án đúng.</p>
            </div>
          </div>
          <button onClick={() => setShowMissingOnly(!showMissingOnly)} className={`px-4 py-2 rounded-xl text-xs font-bold transition-all border cursor-pointer ${showMissingOnly ? 'bg-amber-500 text-slate-950 border-amber-500' : 'bg-slate-950 text-amber-400 border-amber-500/30'}`}>
            {showMissingOnly ? 'Hiển thị tất cả' : 'Lọc xử lý ngay'}
          </button>
        </div>
      )}

      {/* Page header */}
      <div className="flex flex-col sm:flex-row justify-between sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400 tracking-tight">Ngân hàng Câu hỏi</h1>
          <p className="text-sm text-slate-400 mt-1 font-medium">Bảng điều phối, nhập xuất Excel và quản trị chủ đề.</p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          {/* Role switcher */}
          <div className="flex items-center gap-1.5 bg-slate-950/60 border border-slate-850 rounded-xl px-2.5 py-1.5 text-xs select-none">
            <span className="text-[10px] text-slate-500 font-bold uppercase tracking-wider">Vai trò:</span>
            <button onClick={() => setUserRoleOverride('admin')} className={`px-2.5 py-0.5 rounded-lg text-[10px] font-black transition-colors cursor-pointer ${userRoleOverride === 'admin' ? 'bg-blue-600/20 text-blue-400 border border-blue-500/30' : 'text-slate-400 hover:text-white border border-transparent'}`}>Admin</button>
            <button onClick={() => setUserRoleOverride('viewer')} className={`px-2.5 py-0.5 rounded-lg text-[10px] font-black transition-colors cursor-pointer ${userRoleOverride === 'viewer' ? 'bg-amber-600/20 text-amber-400 border border-amber-500/30' : 'text-slate-400 hover:text-white border border-transparent'}`}>Viewer</button>
          </div>
          {hasWritePermission && (
            <>
              {/* Nút Nạp Nhanh Bộ Đề Mẫu */}
              <div className="relative group">
                <button
                  type="button"
                  className="flex items-center gap-2 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white px-3.5 py-2.5 rounded-xl font-bold transition-all shadow-lg shadow-emerald-500/25 active:scale-95 text-xs cursor-pointer border border-emerald-400/30"
                >
                  <Sparkles size={14} className="text-amber-300 animate-pulse" />
                  ⚡ Nạp Nhanh Đề Mẫu
                </button>
                <div className="absolute right-0 mt-1 w-72 bg-slate-900/95 border border-slate-700 rounded-xl shadow-2xl p-2 hidden group-hover:block z-50 backdrop-blur-xl">
                  <div className="text-[10px] font-bold text-slate-400 px-3 py-1 uppercase tracking-wider">Bộ câu hỏi thực tế sẵn có</div>
                  <button
                    onClick={() => handleLoadPresetSample('GPLX_F2023')}
                    className="w-full text-left px-3 py-2 rounded-lg text-xs font-semibold text-slate-200 hover:bg-slate-800 hover:text-sky-400 flex items-center justify-between transition-colors cursor-pointer"
                  >
                    <span>🚦 GPLX 2023 (Biển Báo & Sa Hình)</span>
                    <span className="text-[10px] bg-sky-500/20 text-sky-300 px-1.5 py-0.5 rounded border border-sky-500/30">GPLXf2023.pdf</span>
                  </button>
                  <button
                    onClick={() => handleLoadPresetSample('MATH')}
                    className="w-full text-left px-3 py-2 rounded-lg text-xs font-semibold text-slate-200 hover:bg-slate-800 hover:text-emerald-400 flex items-center justify-between transition-colors cursor-pointer mt-1"
                  >
                    <span>📐 Đề Toán GDPT 2025</span>
                    <span className="text-[10px] bg-emerald-500/20 text-emerald-300 px-1.5 py-0.5 rounded border border-emerald-500/30">DOCX 215 câu</span>
                  </button>
                  <button
                    onClick={() => handleLoadPresetSample('GPLX')}
                    className="w-full text-left px-3 py-2 rounded-lg text-xs font-semibold text-slate-200 hover:bg-slate-800 hover:text-amber-400 flex items-center justify-between transition-colors cursor-pointer mt-1"
                  >
                    <span>🚗 600 Câu Sát Hạch GPLX</span>
                    <span className="text-[10px] bg-amber-500/20 text-amber-300 px-1.5 py-0.5 rounded border border-amber-500/30">PDF Điểm liệt</span>
                  </button>
                  <button
                    onClick={handleLoadStudioSample}
                    className="w-full text-left px-3 py-2 rounded-lg text-xs font-semibold text-emerald-300 hover:bg-emerald-950/60 hover:text-emerald-200 flex items-center justify-between transition-colors cursor-pointer border border-emerald-500/30 mt-1"
                  >
                    <span className="flex items-center gap-1.5">
                      <FileSpreadsheet size={13} className="text-emerald-400" />
                      🏦 Mở Studio: Đề Tín Dụng (240 câu)
                    </span>
                    <span className="text-[10px] bg-emerald-500/20 text-emerald-300 px-1.5 py-0.5 rounded border border-emerald-500/30 font-mono">UCIS</span>
                  </button>
                </div>
              </div>

              <button
                type="button"
                onClick={handleOpenExcelStudio}
                className="flex items-center gap-2 bg-gradient-to-r from-emerald-600 via-teal-600 to-cyan-600 hover:from-emerald-500 hover:to-teal-500 text-white px-3.5 py-2.5 rounded-xl font-bold transition-all shadow-lg shadow-emerald-900/30 active:scale-95 text-xs cursor-pointer border border-emerald-400/30"
                title="Mở Universal Cognitive Ingestion Studio (UCIS v3.0) - Soi chiếu đa Sheet, Tự động phát hiện lược đồ, Radar nhận thức & AI Copilot"
              >
                <FileSpreadsheet size={15} className="text-emerald-200 animate-pulse" />
                <span>Cognitive Ingestion Studio</span>
                <span className="bg-emerald-400/20 text-emerald-300 text-[10px] px-1.5 py-0.2 rounded border border-emerald-400/40 font-mono">v3.0</span>
              </button>
              <button
                onClick={() => setShowAiGenerateModal(true)}
                className="flex items-center gap-2 bg-gradient-to-r from-purple-600 via-indigo-600 to-blue-600 hover:from-purple-500 hover:to-blue-500 text-white px-4 py-2.5 rounded-xl font-bold transition-all shadow-lg shadow-purple-500/25 active:scale-95 text-xs cursor-pointer border border-purple-400/30"
              >
                <Brain size={15} className="text-purple-200 animate-pulse" />
                Tạo câu hỏi AI từ tài liệu
              </button>
              <button
                onClick={() => setShowGovernanceModal(true)}
                className="flex items-center gap-2 bg-gradient-to-r from-blue-700 via-indigo-700 to-purple-800 hover:from-blue-600 hover:to-indigo-600 text-white px-3.5 py-2.5 rounded-xl font-bold transition-all shadow-lg shadow-indigo-900/30 active:scale-95 text-xs cursor-pointer border border-indigo-400/30"
                title="Quản trị Cây phân cấp 5 tầng (HQ -> Chi nhánh -> Phòng ban) & Danh mục Ngành động"
              >
                <Network size={14} className="text-cyan-300" />
                Phân Tầng & Ngành Động
              </button>
              <button onClick={openAddTopicModal} className="flex items-center gap-2 bg-slate-900 hover:bg-slate-850 text-slate-350 border border-slate-800 px-4 py-2.5 rounded-xl font-bold transition-all text-xs cursor-pointer active:scale-95"><FolderOpen size={14} /> Thêm Chủ đề mới</button>
              <button onClick={openAddModal} className="flex items-center gap-2 bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 text-white px-5 py-2.5 rounded-xl font-bold transition-all shadow-lg shadow-blue-500/10 active:scale-95 text-xs cursor-pointer"><Plus size={14} /> Thêm câu hỏi thủ công</button>
            </>
          )}
        </div>
      </div>

      {/* Upload zone */}
      <input type="file" ref={fileInputRef} onChange={handleFileChange} accept=".xlsx,.xls,.docx,.pdf" className="hidden" />
      {hasWritePermission && (
        <div
          onDragOver={e => { e.preventDefault(); setDragActive(true); }}
          onDragLeave={() => setDragActive(false)}
          onDrop={e => {
            e.preventDefault();
            setDragActive(false);
            if (e.dataTransfer.files?.[0]) {
              const f = e.dataTransfer.files[0];
              const lower = f.name.toLowerCase();
              if (lower.endsWith('.docx') || lower.endsWith('.pdf')) {
                handleFileImportDocxOrPdf(f);
              } else {
                handleFileImport(f);
              }
            }
          }}
          onClick={() => fileInputRef.current?.click()}
          className={`border-2 border-dashed rounded-3xl p-10 text-center transition-all cursor-pointer backdrop-blur-md relative overflow-hidden group ${dragActive ? 'border-blue-500 bg-blue-500/10' : 'border-slate-800 bg-slate-900/40 hover:border-slate-700 hover:bg-slate-900/60'}`}
        >
          <div className="absolute top-0 right-0 w-24 h-24 bg-blue-500/5 rounded-full blur-2xl group-hover:scale-150 transition-transform" />
          <div className="w-16 h-16 bg-slate-950/60 border border-slate-800 rounded-full flex items-center justify-center text-slate-400 mx-auto mb-4 group-hover:scale-110 transition-transform"><FileUp size={28} /></div>
          <h3 className="font-bold text-lg text-slate-200">Kéo thả file Excel (.xlsx) / Word (.docx) / PDF (.pdf) hoặc Nhấp vào đây để tải lên</h3>
          <p className="text-xs text-slate-500 mt-2 font-medium max-w-md mx-auto leading-relaxed">
            Nhập nhanh câu hỏi qua <span className="text-green-400 font-semibold">Excel</span>, hoặc tải tệp <span className="text-purple-400 font-semibold">Word (DOCX)</span> / <span className="text-red-400 font-semibold">PDF</span> có sẵn câu hỏi để Gemini AI tự động giải đề & lập barem.
          </p>
          <div className="mt-4 flex flex-wrap items-center justify-center gap-3">
            <button
              type="button"
              onClick={(e) => { e.stopPropagation(); handleOpenExcelStudio(); }}
              className="flex items-center gap-2 bg-slate-900/90 hover:bg-emerald-950/60 text-emerald-400 border border-emerald-500/40 px-4 py-2 rounded-xl font-bold text-xs cursor-pointer transition-all hover:scale-105 shadow-md shadow-emerald-900/20"
            >
              <FileSpreadsheet size={14} className="text-emerald-300" />
              Mở Universal Cognitive Ingestion Studio (UCIS)
              <span className="bg-emerald-500/20 text-emerald-300 text-[10px] px-1.5 py-0.2 rounded font-mono">v3.0</span>
            </button>
          </div>
        </div>
      )}

      {/* Tab selector */}
      <div className="flex border-b border-slate-800 gap-6">
        <button onClick={() => setActiveTab('questions')} className={`pb-3 text-sm font-black tracking-wide border-b-2 transition-all cursor-pointer uppercase ${activeTab === 'questions' ? 'border-blue-500 text-blue-400' : 'border-transparent text-slate-500 hover:text-slate-305'}`}>Ngân hàng Câu hỏi ({questions.length})</button>
        <button onClick={() => setActiveTab('topics')} className={`pb-3 text-sm font-black tracking-wide border-b-2 transition-all cursor-pointer uppercase ${activeTab === 'topics' ? 'border-blue-500 text-blue-400' : 'border-transparent text-slate-500 hover:text-slate-305'}`}>Quản lý Chủ đề ({topics.length})</button>
        <button onClick={() => setActiveTab('rules')} className={`pb-3 text-sm font-black tracking-wide border-b-2 transition-all cursor-pointer uppercase flex items-center gap-1.5 ${activeTab === 'rules' ? 'border-purple-500 text-purple-400' : 'border-transparent text-slate-500 hover:text-slate-305'}`}>
          <Cpu size={15} /> Quy Tắc CIG & Sandbox
        </button>
      </div>

      {activeTab === 'rules' ? (
        <RuleStudioTab />
      ) : (
        /* Tab body */
        <div className="bg-slate-900/60 rounded-3xl border border-slate-800 overflow-hidden backdrop-blur-xl">
        <div className="p-6 border-b border-slate-850 bg-slate-900/30 flex items-center justify-between">
          <h2 className="text-lg font-bold text-slate-200 flex items-center gap-2">
            {activeTab === 'questions' ? <><HelpCircle size={20} className="text-purple-400" /> Ngân hàng Câu hỏi Sát hạch</> : <><Layers size={20} className="text-blue-400" /> Danh mục Chủ đề hệ thống</>}
          </h2>
          <button onClick={fetchData} className="text-xs text-slate-400 hover:text-white font-bold tracking-wider uppercase bg-slate-950/60 border border-slate-850 px-3.5 py-1.5 rounded-full cursor-pointer transition-colors">Làm mới ↻</button>
        </div>

        {/* Filter bar */}
        <div className="p-6 border-b border-slate-850 bg-slate-900/10">
          {activeTab === 'questions' ? (
            <div className="space-y-3">
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-7 gap-3">
                <div className="space-y-1">
                  <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">Tìm kiếm (gõ #tag hoặc từ)</label>
                  <input type="text" placeholder="Gõ #tag hoặc từ khóa..." value={qSearch} onChange={e => setQSearch(e.target.value)} className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-indigo-500" />
                </div>
                <div className="space-y-1">
                  <label className="text-[10px] font-black uppercase text-amber-400 tracking-wider flex items-center gap-1">
                    <Users size={11} /> Phạm vi sở hữu
                  </label>
                  <select
                    value={qScopeFilter}
                    onChange={e => setQScopeFilter(e.target.value as any)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-amber-300 focus:outline-none focus:border-amber-500 cursor-pointer font-medium"
                  >
                    <option value="ALL">🌐 Mọi phạm vi</option>
                    <option value="TEAM">👥 Thuộc Nhóm của tôi</option>
                    <option value="MY_QUESTIONS">👤 Do tôi đóng góp</option>
                  </select>
                </div>
                <div className="space-y-1">
                  <label className="text-[10px] font-black uppercase text-indigo-400 tracking-wider">Miền Ngành Nghề</label>
                  <select value={qDomainFilter} onChange={e => setQDomainFilter(e.target.value)} className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-indigo-500 cursor-pointer font-medium">
                    <option value="ALL">🌐 Tất cả các ngành</option>
                    <option value="EDUCATION">🎓 Giáo dục & Ngoại ngữ</option>
                    <option value="BANKING">🏦 Ngân hàng & Tài chính</option>
                    <option value="HEALTHCARE">🏥 Y tế & Dược phẩm</option>
                    <option value="HSE">🛡️ An toàn lao động HSE</option>
                    <option value="GOV_DRIVING">🚗 Sát hạch Giao thông</option>
                    <option value="IT_SECURITY">💻 CNTT & An ninh mạng</option>
                    <option value="GENERAL">🌐 Chung / Đa lĩnh vực</option>
                  </select>
                </div>
                <div className="space-y-1">
                  <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">Loại câu hỏi</label>
                  <select value={qTypeFilter} onChange={e => setQTypeFilter(e.target.value)} className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer">
                    <option value="">Tất cả các loại</option>
                    {['SINGLE','MULTI','TRUE_FALSE','SHORT_ANSWER','FILL_BLANK','ORDERING','MATCHING','ESSAY'].map(t => <option key={t} value={t}>{getQuestionTypeLabel(t)}</option>)}
                  </select>
                </div>
                <div className="space-y-1">
                  <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">Theo chủ đề</label>
                  <select value={qTopicFilter} onChange={e => setQTopicFilter(e.target.value)} className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer">
                    <option value="">Tất cả chủ đề</option>
                    {topics.map(t => <option key={t.code} value={t.code}>{getTopicFullPath(t, topics)}</option>)}
                  </select>
                </div>
                <div className="space-y-1">
                  <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">Trạng thái đáp án</label>
                  <select value={showMissingOnly ? 'missing' : 'all'} onChange={e => setShowMissingOnly(e.target.value === 'missing')} className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer">
                    <option value="all">Tất cả câu hỏi</option>
                    <option value="missing">Chưa cấu hình đáp án</option>
                  </select>
                </div>

                {/* [RANGE SELECTOR] Chọn dải STT siêu tốc */}
                <div className="space-y-1">
                  <label className="text-[10px] font-black uppercase text-indigo-400 tracking-wider flex items-center gap-1">
                    <BookOpen size={11} /> Dải câu hỏi (STT)
                  </label>
                  <div className="flex items-center gap-1.5">
                    <input
                      type="number"
                      min={1}
                      placeholder="Từ"
                      value={rangeFrom}
                      onChange={e => setRangeFrom(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded-xl px-2 py-2 text-xs text-white text-center focus:outline-none focus:border-indigo-500 placeholder-slate-600"
                    />
                    <span className="text-xs text-slate-500">-</span>
                    <input
                      type="number"
                      min={1}
                      placeholder="Đến"
                      value={rangeTo}
                      onChange={e => setRangeTo(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded-xl px-2 py-2 text-xs text-white text-center focus:outline-none focus:border-indigo-500 placeholder-slate-600"
                    />
                    <button
                      type="button"
                      onClick={handleSelectRange}
                      className="px-3 py-2 bg-indigo-600/30 hover:bg-indigo-600/50 text-indigo-300 border border-indigo-500/40 rounded-xl font-bold text-xs cursor-pointer transition-colors active:scale-95"
                      title="Tự động tích chọn tất cả các câu trong dải STT này"
                    >
                      Chọn
                    </button>
                  </div>
                </div>
              </div>

              {/* Active Tag Filter Indicator */}
              {qTagFilter && (
                <div className="flex items-center gap-2 pt-1">
                  <span className="text-xs text-slate-400">Đang lọc theo thẻ:</span>
                  <span className="inline-flex items-center gap-1.5 px-3 py-0.5 rounded-full text-xs font-bold bg-indigo-600 text-white shadow-sm">
                    {qTagFilter}
                    <button
                      type="button"
                      onClick={() => setQTagFilter(null)}
                      className="hover:bg-white/20 rounded-full p-0.5 transition-colors cursor-pointer"
                      title="Bỏ lọc thẻ này"
                    >
                      &times;
                    </button>
                  </span>
                </div>
              )}
            </div>
          ) : (
            <div className="sm:col-span-3 space-y-1">
              <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">Tìm kiếm chủ đề</label>
              <input type="text" placeholder="Tìm theo tên chủ đề, mô tả hoặc mã chủ đề..." value={tSearch} onChange={e => setTSearch(e.target.value)} className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500" />
            </div>
          )}
        </div>

        <div className="overflow-x-auto">
          {loading ? (
            <div className="py-20 flex flex-col items-center justify-center gap-3">
              <Loader2 className="animate-spin text-blue-500" size={32} />
              <p className="text-xs text-slate-400 font-medium">Đang đồng bộ hóa dữ liệu...</p>
            </div>
          ) : activeTab === 'questions' ? (
            <div className="w-full overflow-x-auto">
              <table className="w-full text-left border-collapse min-w-[750px]">
                <thead>
                  <tr className="border-b border-slate-850 text-slate-400 text-xs font-black tracking-wider uppercase bg-slate-900/20 select-none">
                    <th className="p-4 w-12 text-center">
                      <input
                        type="checkbox"
                        checked={filteredQuestions.length > 0 && filteredQuestions.every(q => selectedQuestionIds.includes(q.id))}
                        onChange={handleToggleSelectAll}
                        className="w-4 h-4 rounded text-indigo-600 focus:ring-indigo-500 bg-slate-900 border-slate-750 cursor-pointer"
                        title="Chọn tất cả câu hỏi"
                      />
                    </th>
                    <th className="p-4 w-16 text-center cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => { if (qSortField === 'stt') { setQSortAsc(!qSortAsc); } else { setQSortField('stt'); setQSortAsc(true); } }}><div className="flex items-center justify-center">STT {renderSortIcon('stt', qSortField, qSortAsc)}</div></th>
                    <th className="p-4 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => { if (qSortField === 'content') { setQSortAsc(!qSortAsc); } else { setQSortField('content'); setQSortAsc(true); } }}><div className="flex items-center">Nội dung câu hỏi {renderSortIcon('content', qSortField, qSortAsc)}</div></th>
                    <th className="p-4 w-32 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => { if (qSortField === 'questionType') { setQSortAsc(!qSortAsc); } else { setQSortField('questionType'); setQSortAsc(true); } }}><div className="flex items-center">Loại {renderSortIcon('questionType', qSortField, qSortAsc)}</div></th>
                    <th className="p-4 w-44 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => { if (qSortField === 'topicCode') { setQSortAsc(!qSortAsc); } else { setQSortField('topicCode'); setQSortAsc(true); } }}><div className="flex items-center">Chủ đề {renderSortIcon('topicCode', qSortField, qSortAsc)}</div></th>
                    <th className="p-4 w-32 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => { if (qSortField === 'difficulty') { setQSortAsc(!qSortAsc); } else { setQSortField('difficulty'); setQSortAsc(true); } }}><div className="flex items-center">Độ khó {renderSortIcon('difficulty', qSortField, qSortAsc)}</div></th>
                    <th className="p-4 w-24 text-right sticky right-0 bg-slate-900 z-10 border-l border-slate-800 shadow-[-4px_0_8px_rgba(0,0,0,0.15)]">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {pagedQuestions.map((q: LearnerQuestion, idx: number) => {
                    const matchedTopic = topics.find(t => t.code === q.topicCode || t.categoryCode === q.topicCode);
                    const correct = getCorrectAnswerDisplay(q);
                    const isSelected = selectedQuestionIds.includes(q.id);
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
                        <td className="p-4 text-center text-xs text-slate-450 font-bold" title={`ID: ${q.id}`}>{(qPage - 1) * qPageSize + idx + 1}</td>
                        <td className="p-4 font-bold text-slate-200 text-sm leading-relaxed">
                          {q.contextContent && (
                            <div className="mb-2">
                              <button
                                type="button"
                                onClick={() => setSelectedContext({
                                  title: q.contextTitle || 'Bài đọc hiểu dùng chung',
                                  content: q.contextContent || ''
                                })}
                                className="inline-flex items-center gap-1.5 px-3 py-1 rounded-xl text-xs font-bold bg-indigo-950/60 hover:bg-indigo-900/80 text-indigo-300 border border-indigo-500/40 hover:border-indigo-400 transition-all cursor-pointer shadow-sm group active:scale-95"
                                title="Bấm để đọc toàn văn bài đọc hiểu dùng chung cho câu hỏi này"
                              >
                                <BookOpen size={13} className="text-indigo-400 group-hover:scale-110 transition-transform" />
                                <span>📖 {q.contextTitle || 'Bài đọc hiểu dùng chung'}</span>
                                <span className="text-[10px] bg-indigo-500/30 text-indigo-200 px-1.5 py-0.2 rounded font-mono ml-0.5 font-semibold">Xem bài đọc</span>
                              </button>
                            </div>
                          )}
                          <div className="max-w-2xl overflow-hidden text-slate-200">
                            <MathRenderer content={q.content} />
                          </div>
                          <div className="mt-2 flex items-center gap-2 flex-wrap text-xs font-semibold select-none">
                            {!correct ? (
                              <span className="text-amber-450 bg-amber-500/10 px-2.5 py-0.5 rounded-lg border border-amber-500/20 text-[10px] uppercase tracking-wider font-black">⚠️ Chưa cấu hình đáp án đúng!</span>
                            ) : (
                              <>
                                <span className="text-emerald-450 bg-emerald-500/10 px-2.5 py-0.5 rounded-lg border border-emerald-500/20 text-[10px] uppercase tracking-wider font-black">Đáp án đúng: {correct.label}</span>
                                {correct.text && (
                                  <span className="text-slate-400 font-medium italic text-sm max-w-lg truncate" title={correct.text}>
                                    (<MathRenderer content={correct.text} inline />)
                                  </span>
                                )}
                              </>
                            )}
                          </div>

                          {/* Domain & Smart Tags Badges */}
                          {((q.tags && q.tags.length > 0) || (q.domainCode && q.domainCode !== 'EDUCATION') || q.targetLevel || q.issuingOrg) && (
                            <div className="mt-2 flex items-center gap-1.5 flex-wrap">
                              {q.domainCode && q.domainCode !== 'EDUCATION' && (
                                <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-purple-950/60 border border-purple-800/60 text-purple-300">
                                  {q.domainCode === 'BANKING' ? '🏦 Ngân hàng' :
                                   q.domainCode === 'HEALTHCARE' ? '🏥 Y tế' :
                                   q.domainCode === 'HSE' ? '🛡️ HSE' :
                                   q.domainCode === 'GOV_DRIVING' ? '🚗 GPLX' :
                                   q.domainCode === 'IT_SECURITY' ? '💻 CNTT' : q.domainCode}
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
                              {q.tags && q.tags.map(t => (
                                <button
                                  key={t}
                                  type="button"
                                  onClick={() => setQTagFilter(t === qTagFilter ? null : t)}
                                  className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[10px] font-bold transition-all cursor-pointer ${
                                    t === qTagFilter
                                      ? 'bg-indigo-600 text-white shadow-sm'
                                      : 'bg-indigo-950/40 hover:bg-indigo-900/60 border border-indigo-800/50 text-indigo-300'
                                  }`}
                                  title={`Bấm để lọc tất cả câu hỏi có thẻ ${t}`}
                                >
                                  {t}
                                </button>
                              ))}
                            </div>
                          )}

                          {/* Multi-tier Scope & Attribution Badge */}
                          <div className="mt-2 flex items-center gap-1.5 flex-wrap">
                            <span className={`px-2 py-0.5 rounded-md text-[10px] font-bold flex items-center gap-1 border ${
                              q.scope === 'TEAM'
                                ? 'bg-amber-950/60 text-amber-300 border-amber-500/40'
                                : q.scope === 'PERSONAL'
                                ? 'bg-purple-950/60 text-purple-300 border-purple-500/40'
                                : 'bg-blue-950/50 text-blue-300 border-blue-500/30'
                            }`}>
                              {q.scope === 'TEAM' ? <Users size={10} /> : q.scope === 'PERSONAL' ? <User size={10} /> : <ShieldCheck size={10} />}
                              <span>{q.scope === 'TEAM' ? (q.orgUnitName ? `Nhóm: ${q.orgUnitName}` : 'Cấp Nhóm') : q.scope === 'PERSONAL' ? 'Cá nhân' : 'Toàn cơ quan'}</span>
                              {q.creatorName && <span className="opacity-80">• ✍️ {q.creatorName}</span>}
                            </span>

                            {q.contributionStatus === 'SUBMITTED_FOR_TENANT' && (
                              <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-amber-500/20 text-amber-300 border border-amber-500/40 flex items-center gap-1 animate-pulse">
                                ⚡ Đang chờ duyệt Toàn Cơ Quan
                              </span>
                            )}
                            {q.contributionStatus === 'APPROVED_TENANT' && (
                              <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 flex items-center gap-1">
                                ✅ Toàn Cơ Quan Đã Duyệt
                              </span>
                            )}
                          </div>
                        </td>
                        <td className="p-4"><span className="px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider bg-blue-500/10 text-blue-400 border border-blue-500/20">{getQuestionTypeLabel(q.questionType)}</span></td>
                        <td className="p-4 text-slate-355 text-xs">{matchedTopic ? getTopicFullPath(matchedTopic, topics) : q.topicCode || 'Chưa phân nhóm'}</td>
                        <td className="p-4"><div className="flex gap-0.5 text-amber-500">{Array.from({ length: q.difficulty || 1 }).map((_, i) => <Star key={i} size={12} fill="currentColor" />)}</div></td>
                        <td className="p-4 text-right sticky right-0 bg-slate-900/95 z-10 border-l border-slate-850/60 shadow-[-4px_0_8px_rgba(0,0,0,0.15)]">
                          {(() => {
                            const isRowManageable = canManageResource(user, q);
                            return (
                              <div className="flex items-center justify-end gap-1.5">
                                {q.scope === 'TEAM' && isRowManageable && q.contributionStatus !== 'SUBMITTED_FOR_TENANT' && q.contributionStatus !== 'APPROVED_TENANT' && (
                                  <button
                                    onClick={() => handleContributeToTenant(q)}
                                    className="p-1.5 text-amber-400 hover:text-amber-300 hover:bg-amber-950/30 rounded-lg transition-colors cursor-pointer border border-amber-500/30"
                                    title="⚡ Đóng góp câu hỏi này lên Ngân hàng Toàn Cơ Quan"
                                  >
                                    <UploadCloud size={13} />
                                  </button>
                                )}
                                <button
                                  onClick={() => { openEditModal(q); setIsEditing(isRowManageable); }}
                                  className={`p-1.5 rounded-lg transition-colors cursor-pointer ${
                                    isRowManageable
                                      ? 'text-amber-400 hover:text-amber-300 hover:bg-amber-950/20'
                                      : 'text-slate-400 hover:text-white hover:bg-slate-800'
                                  }`}
                                  title={isRowManageable ? "Chỉnh sửa câu hỏi" : "Xem chi tiết"}
                                >
                                  {isRowManageable ? <Edit size={14} /> : <Eye size={14} />}
                                </button>
                                {(!q.answerRaw || !q.answerRaw.trim()) && isRowManageable && (
                                  <button onClick={() => handleSuggestAnswer(q)} className="p-1.5 text-purple-400 hover:text-purple-300 hover:bg-purple-950/20 rounded-lg transition-colors cursor-pointer animate-pulse" title="Tự động giải bằng AI"><Sparkles size={14} /></button>
                                )}
                                <button
                                  onClick={() => handleToggleActive(q)}
                                  disabled={!isRowManageable}
                                  className={`p-1.5 rounded-lg transition-colors ${
                                    isRowManageable
                                      ? 'cursor-pointer ' + (q.enabled !== false ? 'text-emerald-450 hover:text-emerald-300 hover:bg-emerald-950/20' : 'text-slate-500 hover:text-slate-300 hover:bg-slate-850')
                                      : 'opacity-40 cursor-not-allowed text-slate-600'
                                  }`}
                                  title={isRowManageable ? (q.enabled !== false ? 'Vô hiệu hóa' : 'Kích hoạt') : 'Không có quyền thao tác'}
                                >
                                  {q.enabled !== false ? <ToggleRight size={17} /> : <ToggleLeft size={17} />}
                                </button>
                                {isRowManageable && (
                                  <button
                                    onClick={() => handleDelete(q.id)}
                                    className="p-1.5 text-red-400 hover:text-red-300 hover:bg-red-950/20 rounded-lg transition-colors cursor-pointer"
                                    title="Xóa câu hỏi"
                                  >
                                    <Trash2 size={13} />
                                  </button>
                                )}
                              </div>
                            );
                          })()}
                        </td>
                      </tr>
                    );
                  })}
                  {pagedQuestions.length === 0 && (
                    <tr><td colSpan={6} className="text-center py-20 text-slate-500 text-xs font-bold">Không tìm thấy câu hỏi nào phù hợp với bộ lọc.</td></tr>
                  )}
                </tbody>
              </table>
              <div className="p-4 border-t border-slate-850 flex flex-col sm:flex-row items-center justify-between gap-4 bg-slate-900/10">
                <span className="text-xs text-slate-400 font-semibold">Hiển thị từ {filteredQuestions.length > 0 ? (qPage - 1) * qPageSize + 1 : 0} đến {Math.min(qPage * qPageSize, filteredQuestions.length)} trong tổng số {filteredQuestions.length} câu hỏi</span>
                <Pagination page={qPage} totalPages={qTotalPages} onChange={setQPage} />
              </div>
            </div>
          ) : (
            <div className="w-full overflow-x-auto">
              <table className="w-full text-left border-collapse min-w-[700px]">
                <thead>
                  <tr className="border-b border-slate-850 text-slate-400 text-xs font-black tracking-wider uppercase bg-slate-900/20 select-none">
                    <th className="p-4 w-40 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => { if (tSortField === 'code') { setTSortAsc(!tSortAsc); } else { setTSortField('code'); setTSortAsc(true); } }}><div className="flex items-center">Mã chủ đề {renderSortIcon('code', tSortField, tSortAsc)}</div></th>
                    <th className="p-4 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => { if (tSortField === 'name') { setTSortAsc(!tSortAsc); } else { setTSortField('name'); setTSortAsc(true); } }}><div className="flex items-center">Tên chủ đề {renderSortIcon('name', tSortField, tSortAsc)}</div></th>
                    <th className="p-4"><div className="flex items-center text-slate-400">Đường dẫn (Path)</div></th>
                    <th className="p-4 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => { if (tSortField === 'description') { setTSortAsc(!tSortAsc); } else { setTSortField('description'); setTSortAsc(true); } }}><div className="flex items-center">Mô tả {renderSortIcon('description', tSortField, tSortAsc)}</div></th>
                    <th className="p-4 w-40 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => { if (tSortField === 'visibilityScope') { setTSortAsc(!tSortAsc); } else { setTSortField('visibilityScope'); setTSortAsc(true); } }}><div className="flex items-center">Phạm vi hiển thị {renderSortIcon('visibilityScope', tSortField, tSortAsc)}</div></th>
                    <th className="p-4 w-24 text-right sticky right-0 bg-slate-900 z-10 border-l border-slate-800 shadow-[-4px_0_8px_rgba(0,0,0,0.15)]">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {pagedTopics.map((t: BankTopic, idx: number) => (
                    <tr key={t.id || idx} className="border-b border-slate-850/60 hover:bg-slate-800/10 transition-colors">
                      <td className="p-4 font-mono text-xs font-black text-blue-400 select-all">{t.code}</td>
                      <td className="p-4 font-bold text-slate-200 text-sm">{t.name}</td>
                      <td className="p-4 text-xs text-slate-300">{getTopicFullPath(t, topics).replace(/ → /g, '/')}</td>
                      <td className="p-4 text-xs text-slate-400 leading-relaxed max-w-xs truncate">{t.description || '_'}</td>
                      <td className="p-4">
                        <span className={`px-2 py-0.5 rounded text-[10px] font-black tracking-wider uppercase border ${t.visibilityScope === 'PUBLIC' ? 'bg-green-500/10 text-green-400 border-green-500/20' : 'bg-purple-500/10 text-purple-400 border-purple-500/20'}`}>{t.visibilityScope}</span>
                      </td>
                      <td className="p-4 text-right sticky right-0 bg-slate-900/95 z-10 border-l border-slate-850/60 shadow-[-4px_0_8px_rgba(0,0,0,0.15)]">
                        <div className="flex items-center justify-end gap-2">
                          <button onClick={() => { openEditTopicModal(t); setIsEditingTopic(false); }} className="p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition-colors cursor-pointer" title="Xem chi tiết"><Eye size={14} /></button>
                          <button onClick={() => handleToggleActiveTopic(t)} className={`p-2 rounded-lg transition-colors cursor-pointer ${t.enabled !== false ? 'text-emerald-450 hover:text-emerald-300 hover:bg-emerald-950/20' : 'text-slate-500 hover:text-slate-300 hover:bg-slate-850'} ${!hasWritePermission ? 'opacity-50 cursor-not-allowed' : ''}`} title={t.enabled !== false ? 'Vô hiệu hóa' : 'Kích hoạt'}>
                            {t.enabled !== false ? <ToggleRight size={18} /> : <ToggleLeft size={18} />}
                          </button>
                          {hasWritePermission && (
                            <button onClick={() => handleDeleteTopic(t.id)} className="p-2 text-red-400 hover:text-red-300 hover:bg-red-950/20 rounded-lg transition-colors cursor-pointer" title="Xóa chủ đề"><Trash2 size={14} /></button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                  {pagedTopics.length === 0 && (
                    <tr><td colSpan={5} className="text-center py-20 text-slate-500 text-xs font-bold">Không tìm thấy chủ đề nào phù hợp với bộ lọc.</td></tr>
                  )}
                </tbody>
              </table>
              <div className="p-4 border-t border-slate-850 flex flex-col sm:flex-row items-center justify-between gap-4 bg-slate-900/10">
                <span className="text-xs text-slate-400 font-semibold">Hiển thị từ {filteredTopics.length > 0 ? (tPage - 1) * tPageSize + 1 : 0} đến {Math.min(tPage * tPageSize, filteredTopics.length)} trong tổng số {filteredTopics.length} chủ đề</span>
                <Pagination page={tPage} totalPages={tTotalPages} onChange={setTPage} />
              </div>
            </div>
          )}
        </div>
      </div>
      )}

      {/* Modals */}
      <QuestionModal
        isOpen={isModalOpen} isEditing={isEditing} editingId={editingId} hasWritePermission={hasWritePermission}
        currentUser={user} scope={formScope} setScope={setFormScope}
        questions={questions} topics={topics}
        content={content} questionType={questionType} difficulty={difficulty}
        categoryCode={categoryCode} durationSeconds={durationSeconds} options={options}
        citation={citation} correctAnswer={correctAnswer} contentType={contentType} optionType={optionType}
        setContent={setContent} setQuestionType={setQuestionType} setDifficulty={setDifficulty}
        setCategoryCode={setCategoryCode} setDurationSeconds={setDurationSeconds} setOptions={setOptions}
        setCitation={setCitation} setCorrectAnswer={setCorrectAnswer} setContentType={setContentType}
        setOptionType={setOptionType} setIsEditing={setIsEditing}
        contexts={availableContexts}
        contextId={currentContextId} contextTitle={currentContextTitle} contextContent={currentContextContent}
        setContextId={setCurrentContextId} setContextTitle={setCurrentContextTitle} setContextContent={setCurrentContextContent}
        isStickyContext={isStickyContext} setIsStickyContext={setIsStickyContext}
        stickyCount={stickyCount} setStickyCount={setStickyCount}
        onSaveAndContinue={handleSaveAndContinue}
        domainCode={formDomainCode} setDomainCode={setFormDomainCode}
        tags={formTags} setTags={setFormTags}
        targetLevel={formTargetLevel} setTargetLevel={setFormTargetLevel}
        assessmentPurpose={formAssessmentPurpose} setAssessmentPurpose={setFormAssessmentPurpose}
        issuingOrg={formIssuingOrg} setIssuingOrg={setFormIssuingOrg}
        benchmarkYear={formBenchmarkYear} setBenchmarkYear={setFormBenchmarkYear}
        onClose={() => setIsModalOpen(false)} onSave={handleSave}
        onOpenAdd={openAddModal} onOpenEdit={openEditModal}
        onDelete={handleDelete} onToggleActive={handleToggleActive}
      />

      <TopicModal
        isOpen={isTopicModalOpen} isEditingTopic={isEditingTopic} editingTopicId={editingTopicId} hasWritePermission={hasWritePermission}
        topics={topics} questions={questions}
        topicCode={topicCode} topicName={topicName} topicDesc={topicDesc}
        topicCat={topicCat} topicVisibility={topicVisibility} topicParentId={topicParentId}
        topicDomainCode={topicDomainCode} topicScope={topicScope}
        setTopicCode={setTopicCode} setTopicName={setTopicName} setTopicDesc={setTopicDesc}
        setTopicCat={setTopicCat} setTopicVisibility={setTopicVisibility} setTopicParentId={setTopicParentId}
        setTopicDomainCode={setTopicDomainCode} setTopicScope={setTopicScope}
        setIsEditingTopic={setIsEditingTopic}
        onClose={() => setIsTopicModalOpen(false)} onSave={handleSaveTopic}
        onOpenAdd={openAddTopicModal} onOpenEdit={openEditTopicModal}
        onDelete={handleDeleteTopic} onToggleActive={handleToggleActiveTopic}
      />

      {/* ── Modal Xác Nhận Xóa Chủ Đề An Toàn ── */}
      {topicToDelete && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-[70] flex items-center justify-center p-4">
          <div className="w-full max-w-lg bg-gradient-to-b from-slate-900 to-slate-950 border border-red-500/30 rounded-3xl p-6 md:p-8 shadow-2xl relative space-y-5 animate-in fade-in zoom-in-95">
            <div className="flex items-start gap-4">
              <div className="w-12 h-12 rounded-2xl bg-red-500/10 border border-red-500/20 text-red-400 flex items-center justify-center shrink-0">
                <Trash2 size={24} />
              </div>
              <div className="flex-1 min-w-0">
                <span className="text-[10px] font-black tracking-widest text-red-400 uppercase bg-red-950/60 border border-red-800/40 px-2.5 py-0.5 rounded-full inline-block">
                  Xác nhận xóa chủ đề
                </span>
                <h3 className="text-lg font-black text-white mt-1.5 truncate">
                  {topicToDelete.name}
                </h3>
                <p className="text-xs font-mono text-blue-400 mt-0.5">Mã chủ đề: {topicToDelete.code}</p>
              </div>
              <button
                onClick={() => !isDeletingTopicLoading && setTopicToDelete(null)}
                className="text-slate-400 hover:text-white p-1 rounded-lg cursor-pointer"
                disabled={isDeletingTopicLoading}
              >
                <X size={18} />
              </button>
            </div>

            <div className="bg-slate-950/70 border border-slate-800/80 rounded-2xl p-4 text-xs space-y-2 select-none">
              <div className="flex items-center justify-between text-slate-300">
                <span>Số câu hỏi trực thuộc:</span>
                <span className="font-black text-blue-400">
                  {questions.filter(q => q.topicCode === topicToDelete.code || q.categoryCode === topicToDelete.code).length} câu hỏi
                </span>
              </div>
              <div className="flex items-center justify-between text-slate-300">
                <span>Chủ đề con trực tiếp:</span>
                <span className="font-black text-purple-400">
                  {topics.filter(t => t.parentId === topicToDelete.id).length} chủ đề con
                </span>
              </div>
            </div>

            <p className="text-xs text-slate-300 leading-relaxed font-medium">
              Vui lòng chọn phương án xử lý câu hỏi và các chủ đề con khi xóa chủ đề này:
            </p>

            <div className="space-y-3">
              <button
                type="button"
                disabled={isDeletingTopicLoading}
                onClick={() => confirmDeleteTopic(false)}
                className="w-full text-left p-4 bg-slate-900/90 hover:bg-slate-850 border border-slate-750 hover:border-blue-500/50 rounded-2xl transition-all group cursor-pointer disabled:opacity-50"
              >
                <div className="flex items-center justify-between">
                  <span className="text-xs font-bold text-slate-200 group-hover:text-blue-400 flex items-center gap-1.5">
                    🛡️ Giữ lại câu hỏi (Khuyên dùng)
                  </span>
                  <span className="text-[10px] text-emerald-400 font-bold bg-emerald-950/60 border border-emerald-500/30 px-2 py-0.5 rounded-md">
                    An toàn
                  </span>
                </div>
                <p className="text-[11px] text-slate-400 mt-1.5 leading-relaxed">
                  Chỉ xóa chủ đề này. Câu hỏi được bảo lưu và chuyển sang chủ đề cấp trên (hoặc nhóm chung). Các chủ đề con được nâng lên cấp trên.
                </p>
              </button>

              <button
                type="button"
                disabled={isDeletingTopicLoading}
                onClick={() => confirmDeleteTopic(true)}
                className="w-full text-left p-4 bg-red-950/20 hover:bg-red-950/40 border border-red-500/30 hover:border-red-500/60 rounded-2xl transition-all group cursor-pointer disabled:opacity-50"
              >
                <div className="flex items-center justify-between">
                  <span className="text-xs font-bold text-red-300 group-hover:text-red-200 flex items-center gap-1.5">
                    💥 Xóa toàn bộ nhánh & câu hỏi
                  </span>
                  <span className="text-[10px] text-red-400 font-bold bg-red-950/80 border border-red-500/40 px-2 py-0.5 rounded-md">
                    Xóa triệt để
                  </span>
                </div>
                <p className="text-[11px] text-slate-400 mt-1.5 leading-relaxed">
                  Xóa triệt để chủ đề này, toàn bộ các chủ đề con/cháu và tất cả các câu hỏi thuộc nhánh khỏi hệ thống.
                </p>
              </button>
            </div>

            <div className="pt-2 flex justify-end gap-2 border-t border-slate-800/80">
              <button
                type="button"
                disabled={isDeletingTopicLoading}
                onClick={() => setTopicToDelete(null)}
                className="px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-slate-300 hover:text-white rounded-xl text-xs font-bold transition-all cursor-pointer border border-slate-800 disabled:opacity-50"
              >
                Hủy bỏ
              </button>
            </div>
          </div>
        </div>
      )}

      <DocxPreviewModal
        isOpen={showDocxPreviewModal} isProcessing={isDocxProcessing} isAiSolving={isAiSolving}
        isConvertingLatex={isConvertingLatex}
        docxPreviewList={docxPreviewList} topics={topics} loading={loading}
        setDocxPreviewList={setDocxPreviewList}
        onClose={() => setShowDocxPreviewModal(false)}
        onConfirmImport={handleConfirmDocxImport}
        onAiSolveBatch={handleAiSolveBatch}
        onConvertLatex={handleConvertLatex}
        onTopicCreated={(newTopic) => {
          setTopics(prev => prev.some(t => t.code === newTopic.code) ? prev : [...prev, newTopic]);
        }}
      />

      <AiGenerateModal
        isOpen={showAiGenerateModal}
        topics={topics}
        onClose={() => setShowAiGenerateModal(false)}
        onSuccess={(generatedQuestions) => {
          setDocxPreviewList(generatedQuestions);
          setShowDocxPreviewModal(true);
        }}
      />

      {/* ── Universal Cognitive Excel Ingestion Studio (UCIS v3.0) ── */}
      {showExcelStudioModal && (
        <ExcelIngestionStudioModal
          isOpen={showExcelStudioModal}
          isProcessing={isDocxProcessing}
          inspectionData={excelPreviewData}
          topics={topics}
          onClose={() => setShowExcelStudioModal(false)}
          onConfirmImport={handleConfirmStudioImport}
          onTopicCreated={(newTopic) => {
            setTopics(prev => prev.some(t => t.code === newTopic.code) ? prev : [...prev, newTopic]);
          }}
          onUploadNewFile={() => fileInputRef.current?.click()}
          onLoadSampleData={handleLoadStudioSample}
        />
      )}

      {/* ── Excel DOT-2026 Smart Import Preview Modal ── */}
      {showExcelPreviewModal && excelPreviewData && (
        <div className="fixed inset-0 bg-slate-950/90 backdrop-blur-md z-[70] flex items-center justify-center p-4">
          <div className="w-full max-w-3xl bg-gradient-to-b from-slate-900 to-slate-950 border border-emerald-500/30 rounded-3xl shadow-2xl flex flex-col max-h-[90vh]">
            {/* Header */}
            <div className="px-6 pt-6 pb-4 border-b border-slate-800 flex items-start justify-between gap-4">
              <div>
                <div className="flex items-center gap-2 mb-1">
                  <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 bg-emerald-950/60 text-emerald-300 border border-emerald-500/30 rounded-full text-[10px] font-black uppercase tracking-wider">
                    <FileUp size={11} /> Excel DOT-2026
                  </span>
                  <span className="text-xs text-slate-500">{excelPreviewData.fileName}</span>
                </div>
                <h3 className="text-xl font-black text-transparent bg-clip-text bg-gradient-to-r from-emerald-300 via-teal-300 to-cyan-300">
                  Xem trước & Xác nhận nhập Excel
                </h3>
                <p className="text-xs text-slate-400 mt-0.5">
                  Phát hiện <strong className="text-emerald-400">{excelPreviewData.totalQuestions}</strong> câu hỏi
                  từ <strong className="text-teal-400">{excelPreviewData.sheets.length}</strong> sheet hợp lệ.
                  Chọn các sheet muốn nhập.
                </p>
              </div>
              <button onClick={() => setShowExcelPreviewModal(false)}
                className="text-slate-400 hover:text-white p-1.5 hover:bg-slate-800 rounded-xl transition-colors cursor-pointer flex-shrink-0">
                <X size={18} />
              </button>
            </div>

            {/* Warnings */}
            {excelPreviewData.warnings?.length > 0 && (
              <div className="mx-6 mt-3 p-3 bg-yellow-950/30 border border-yellow-700/30 rounded-xl">
                <p className="text-xs font-bold text-yellow-400 mb-1">⚠ Cảnh báo:</p>
                {excelPreviewData.warnings.map((w: string, wi: number) => (
                  <p key={wi} className="text-xs text-yellow-300/80">• {w}</p>
                ))}
              </div>
            )}

            {/* Tọa độ nhận diện tự động từ File & Tiêu đề */}
            {(excelPreviewData.detectedTargetLevel || excelPreviewData.detectedAssessmentPurpose || excelPreviewData.detectedDomainCode) && (
              <div className="mx-6 mt-3 p-3.5 bg-gradient-to-r from-emerald-950/40 via-teal-950/40 to-slate-900/60 border border-emerald-500/30 rounded-2xl space-y-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2 text-xs font-bold text-emerald-300">
                    <Sparkles size={14} className="text-amber-300" />
                    <span>Hệ Tọa Độ Tri Thức Nhận Diện Tự Động Từ Tiêu Đề File</span>
                  </div>
                  <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-emerald-900/80 border border-emerald-400/40 text-emerald-200">
                    {excelPreviewData.detectedDomainCode || 'BANKING'}
                  </span>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-[11px] text-slate-300">
                  {excelPreviewData.detectedTargetLevel && (
                    <div className="flex items-center gap-1.5 truncate">
                      <span className="text-slate-400">Vị trí:</span>
                      <strong className="text-white">{excelPreviewData.detectedTargetLevel}</strong>
                    </div>
                  )}
                  {excelPreviewData.detectedAssessmentPurpose && (
                    <div className="flex items-center gap-1.5 truncate">
                      <span className="text-slate-400">Mục đích:</span>
                      <strong className="text-white">{excelPreviewData.detectedAssessmentPurpose}</strong>
                    </div>
                  )}
                  {excelPreviewData.detectedIssuingOrg && (
                    <div className="flex items-center gap-1.5 truncate">
                      <span className="text-slate-400">Đơn vị:</span>
                      <strong className="text-white">{excelPreviewData.detectedIssuingOrg}</strong>
                    </div>
                  )}
                  {excelPreviewData.detectedBenchmarkYear && (
                    <div className="flex items-center gap-1.5 truncate">
                      <span className="text-slate-400">Năm chuẩn mực:</span>
                      <strong className="text-white">{excelPreviewData.detectedBenchmarkYear}</strong>
                    </div>
                  )}
                </div>
                {Array.isArray(excelPreviewData.detectedTags) && excelPreviewData.detectedTags.length > 0 && (
                  <div className="flex flex-wrap items-center gap-1 pt-1 border-t border-slate-800/60">
                    <Tag size={10} className="text-emerald-400 mr-1" />
                    {excelPreviewData.detectedTags.map((t: string) => (
                      <span key={t} className="text-[10px] font-bold px-2 py-0.5 rounded-md bg-slate-900 text-emerald-300 border border-emerald-500/20">
                        #{t}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* Sheet list */}
            <div className="flex-1 overflow-y-auto px-6 py-4 space-y-3">
              {excelPreviewData.sheets.map((sheet: any, idx: number) => {
                const isSelected = excelSelectedSheets.has(idx);
                return (
                  <div key={idx}
                    onClick={() => {
                      setExcelSelectedSheets(prev => {
                        const next = new Set(prev);
                        if (next.has(idx)) next.delete(idx); else next.add(idx);
                        return next;
                      });
                    }}
                    className={`cursor-pointer rounded-2xl border p-4 transition-all duration-200 ${
                      isSelected
                        ? 'border-emerald-500/60 bg-emerald-950/30 shadow-lg shadow-emerald-950/30'
                        : 'border-slate-700/50 bg-slate-900/50 opacity-60 hover:opacity-80'
                    }`}
                  >
                    <div className="flex items-start gap-3">
                      <div className={`mt-0.5 flex-shrink-0 w-5 h-5 rounded-md border-2 flex items-center justify-center ${
                        isSelected ? 'border-emerald-400 bg-emerald-500' : 'border-slate-600 bg-transparent'
                      }`}>
                        {isSelected && <span className="text-white text-[10px] font-black">✓</span>}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap mb-1.5">
                          <span className="text-sm font-bold text-slate-200 truncate">{sheet.sheetName}</span>
                          <span className="px-2 py-0.5 text-[10px] font-bold bg-slate-800 text-slate-400 rounded-full">
                            {sheet.questionCount} câu
                          </span>
                        </div>
                        <div className="flex items-center gap-2 flex-wrap">
                          <span className="px-2.5 py-0.5 text-[10px] font-bold bg-emerald-950/60 text-emerald-300 border border-emerald-500/30 rounded-full truncate max-w-[280px]">
                            🏷 {sheet.detectedTopicCode}
                          </span>
                          <span className="text-[11px] text-slate-400 truncate">{sheet.detectedTopicName}</span>
                        </div>
                        {/* Sample questions preview */}
                        {sheet.questions.length > 0 && (
                          <div className="mt-2 space-y-1">
                            {sheet.questions.slice(0, 2).map((q: any, qi: number) => (
                              <p key={qi} className="text-[11px] text-slate-400 truncate pl-2 border-l-2 border-slate-700">
                                {qi + 1}. {q.content}
                              </p>
                            ))}
                            {sheet.questionCount > 2 && (
                              <p className="text-[10px] text-slate-500 pl-2">...và {sheet.questionCount - 2} câu hỏi khác</p>
                            )}
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Footer actions */}
            <div className="px-6 py-4 border-t border-slate-800 flex items-center justify-between gap-3">
              <div className="text-xs text-slate-400">
                Đã chọn: <strong className="text-emerald-400">{
                  excelPreviewData.sheets.filter((_: any, i: number) => excelSelectedSheets.has(i))
                    .reduce((sum: number, sh: any) => sum + sh.questionCount, 0)
                }</strong> câu hỏi
              </div>
              <div className="flex gap-2">
                <button onClick={() => setShowExcelPreviewModal(false)}
                  className="px-4 py-2 text-sm font-medium text-slate-400 hover:text-white bg-slate-800 hover:bg-slate-700 rounded-xl transition-colors cursor-pointer">
                  Hủy
                </button>
                <button
                  onClick={handleConfirmExcelImport}
                  disabled={isExcelImporting || excelSelectedSheets.size === 0}
                  className="px-5 py-2 text-sm font-bold text-slate-900 bg-gradient-to-r from-emerald-400 to-teal-400 hover:from-emerald-300 hover:to-teal-300 rounded-xl transition-all duration-200 disabled:opacity-50 cursor-pointer flex items-center gap-2"
                >
                  {isExcelImporting ? (
                    <><Loader2 size={14} className="animate-spin" /> Đang lưu...</>
                  ) : (
                    <><FileUp size={14} /> Xác nhận nhập ({excelSelectedSheets.size} sheet)</>
                  )}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Universal Reading Context View Modal */}
      {selectedContext && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-md z-50 flex items-center justify-center p-4">
          <div className="w-full max-w-3xl bg-slate-900 border border-slate-750 rounded-3xl p-6 sm:p-8 shadow-2xl relative space-y-4 max-h-[85vh] flex flex-col justify-between">
            <button
              onClick={() => setSelectedContext(null)}
              className="absolute top-5 right-5 text-slate-400 hover:text-white p-1.5 hover:bg-slate-800 rounded-xl transition-colors cursor-pointer"
            >
              <X size={18} />
            </button>
            <div className="space-y-1">
              <div className="inline-flex items-center gap-1.5 px-2.5 py-0.5 bg-indigo-950/60 text-indigo-300 border border-indigo-500/30 rounded-full text-[10px] font-black uppercase tracking-wider">
                <BookOpen size={11} /> Universal Shared Stimulus / Context
              </div>
              <h3 className="text-xl font-black text-transparent bg-clip-text bg-gradient-to-r from-indigo-300 via-purple-300 to-pink-300">
                {selectedContext.title}
              </h3>
              <p className="text-xs text-slate-400">
                Đoạn văn / Ngữ cảnh bài thi dùng chung cho các câu hỏi liên quan. Lưu trữ tối ưu một lần duy nhất trong cơ sở dữ liệu.
              </p>
            </div>
            <div className="flex-1 overflow-y-auto bg-slate-950/70 border border-indigo-900/40 rounded-2xl p-5 text-sm text-slate-200 leading-relaxed font-sans shadow-inner whitespace-pre-wrap">
              <MathRenderer content={selectedContext.content} />
            </div>
            <div className="flex items-center justify-between pt-2 border-t border-slate-800 text-xs text-slate-400">
              <span>Độ dài: {selectedContext.content.length} ký tự (~{selectedContext.content.split(/\s+/).filter(Boolean).length} từ)</span>
              <button
                onClick={() => setSelectedContext(null)}
                className="px-4 py-2 bg-slate-800 hover:bg-slate-750 text-white rounded-xl font-bold transition-all text-xs cursor-pointer"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}
      {/* Floating Action Bar khi chọn câu hỏi */}
      {selectedQuestionIds.length > 0 && (
        <div className="fixed bottom-6 left-1/2 -translate-x-1/2 z-40 bg-slate-900/95 border border-indigo-500/50 backdrop-blur-xl shadow-2xl rounded-2xl px-6 py-3 flex items-center gap-3 text-xs animate-in fade-in slide-in-from-bottom-5">
          <span className="font-bold text-white flex items-center gap-1.5 whitespace-nowrap">
            <CheckCircle2 className="text-emerald-400" size={16} /> Đã chọn <span className="text-indigo-400 font-mono font-black text-sm">{selectedQuestionIds.length}</span> câu
          </span>
          <div className="h-4 w-px bg-slate-750" />
          <button
            type="button"
            onClick={() => setShowBatchContextModal(true)}
            className="px-3.5 py-2 bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white font-bold rounded-xl flex items-center gap-1.5 cursor-pointer shadow-lg shadow-indigo-600/30 active:scale-95 transition-all whitespace-nowrap"
          >
            <BookOpen size={13} /> 🔗 Gán Bài Đọc
          </button>
          <button
            type="button"
            onClick={() => setShowBatchTagModal(true)}
            className="px-3.5 py-2 bg-gradient-to-r from-purple-600 to-pink-600 hover:from-purple-500 hover:to-pink-500 text-white font-bold rounded-xl flex items-center gap-1.5 cursor-pointer shadow-lg shadow-purple-600/30 active:scale-95 transition-all whitespace-nowrap"
          >
            <Tag size={13} /> 🏷️ Gán Thẻ & Ngành
          </button>
          <button
            type="button"
            onClick={handleBatchRemoveContext}
            className="px-3 py-2 bg-slate-800 hover:bg-slate-750 text-slate-300 hover:text-red-400 font-semibold rounded-xl flex items-center gap-1 cursor-pointer transition-colors whitespace-nowrap"
            title="Gỡ bài đọc hiểu chung khỏi các câu đã chọn"
          >
            <Unlink size={12} /> Gỡ bài đọc
          </button>
          <button
            type="button"
            onClick={() => setSelectedQuestionIds([])}
            className="text-slate-400 hover:text-white px-2 py-1 font-semibold cursor-pointer whitespace-nowrap"
          >
            Bỏ chọn
          </button>
        </div>
      )}

      {/* Batch Assign Context Modal */}
      <BatchContextModal
        isOpen={showBatchContextModal}
        onClose={() => setShowBatchContextModal(false)}
        selectedCount={selectedQuestionIds.length}
        contexts={availableContexts}
        onConfirm={handleBatchAssignConfirm}
        isAssigning={isBatchAssigning}
      />

      {/* Batch Assign Tags & Domain Modal */}
      <BatchTagModal
        isOpen={showBatchTagModal}
        onClose={() => setShowBatchTagModal(false)}
        selectedCount={selectedQuestionIds.length}
        selectedQuestionIds={selectedQuestionIds}
        onApply={handleBatchAssignTagsConfirm}
        isSubmitting={isBatchTagging}
      />

      {/* 5-Tier Organization & Dynamic Domain Governance Modal (Kỳ quan 16) */}
      <TenantGovernanceModal
        isOpen={showGovernanceModal}
        onClose={() => setShowGovernanceModal(false)}
      />
    </div>
  );
}

export default AdminQuestionsPage;
