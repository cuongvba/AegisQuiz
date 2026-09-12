import { useState, useMemo, useEffect } from 'react';
import {
  X, Save, Sparkles, Layers, Wand2, CheckCheck, Eye,
  Tag, Globe, Building2, Calendar, Award, Hash, ChevronDown,
  ChevronUp, SlidersHorizontal, Trash2, RotateCcw, Loader2,
  FileSpreadsheet, Activity, AlertTriangle, CheckCircle2,
  HelpCircle, ArrowUpDown, Filter, Search, Edit3, Check, Play, BookOpen
} from 'lucide-react';
import type { BankTopic } from '../types';
import { LETTERS } from '../types';
import { MathRenderer } from '../../../../components/common/MathRenderer';
import { DOMAIN_OPTIONS } from './DocxPreviewModal';
import { CreateTopicModal } from './CreateTopicModal';
import { useAdaptiveTenant } from '../../../../hooks/useAdaptiveTenant';
import { learnerQuizService } from '../../../../services/learner-quiz.service';

interface ExcelIngestionStudioModalProps {
  isOpen: boolean;
  isProcessing: boolean;
  inspectionData: any; // { fileName, totalSheets, totalQuestions, warnings, sheets: [...], skippedSheets: [...], detectedDomainCode... }
  topics: BankTopic[];
  onClose: () => void;
  onConfirmImport: (questions: any[]) => Promise<void>;
  onTopicCreated?: (topic: BankTopic) => void;
  onUploadNewFile?: () => void;
  onLoadSampleData?: () => void;
}

export interface SheetTopicConfig {
  parentTopicCode: string;
  parentTopicName: string;
  childTopicCode: string;
  childTopicName: string;
  topicDescription: string;
}

export const COLUMN_ROLE_OPTIONS = [
  { value: 'stt', label: '🔢 STT (Thứ tự)', color: 'bg-slate-800 text-slate-300' },
  { value: 'content', label: '❓ Nội Dung Câu Hỏi', color: 'bg-blue-900/60 text-blue-300 border-blue-600' },
  { value: 'option_1', label: '🅰️ Phương Án A (1)', color: 'bg-emerald-950/60 text-emerald-300 border-emerald-700' },
  { value: 'option_2', label: '🅱️ Phương Án B (2)', color: 'bg-emerald-950/60 text-emerald-300 border-emerald-700' },
  { value: 'option_3', label: '🅲 Phương Án C (3)', color: 'bg-emerald-950/60 text-emerald-300 border-emerald-700' },
  { value: 'option_4', label: '🅳 Phương Án D (4)', color: 'bg-emerald-950/60 text-emerald-300 border-emerald-700' },
  { value: 'option_5', label: '🅴 Phương Án E (5)', color: 'bg-emerald-950/60 text-emerald-300 border-emerald-700' },
  { value: 'option_6', label: '🅵 Phương Án F (6)', color: 'bg-emerald-950/60 text-emerald-300 border-emerald-700' },
  { value: 'option_7', label: '🅶 Phương Án G (7)', color: 'bg-emerald-950/60 text-emerald-300 border-emerald-700' },
  { value: 'option_8', label: '🅷 Phương Án H (8)', color: 'bg-emerald-950/60 text-emerald-300 border-emerald-700' },
  { value: 'answer', label: '🎯 Đáp Án Đúng', color: 'bg-amber-950/60 text-amber-300 border-amber-600' },
  { value: 'reference', label: '📖 Trích Dẫn / Căn Cứ / Giải Thích', color: 'bg-indigo-950/60 text-indigo-300 border-indigo-700' },
  { value: 'context', label: '📜 Bài Đọc / Ngữ Cảnh Dùng Chung', color: 'bg-yellow-950/60 text-yellow-300 border-yellow-700' },
  { value: 'contextTitle', label: '🏷️ Tiêu Đề Bài Đọc / Tình Huống', color: 'bg-yellow-900/60 text-yellow-200 border-yellow-600' },
  { value: 'rubric', label: '📋 Tiêu Chí Chấm / Barem Điểm', color: 'bg-fuchsia-950/60 text-fuchsia-300 border-fuchsia-700' },
  { value: 'critical', label: '⚠️ Điểm Liệt Tử Thần', color: 'bg-red-950/60 text-red-300 border-red-700' },
  { value: 'difficulty', label: '⭐ Độ Khó Bloom', color: 'bg-purple-950/60 text-purple-300 border-purple-700' },
  { value: 'duration', label: '⏱️ Thời Gian Làm Bài (Giây)', color: 'bg-orange-950/60 text-orange-300 border-orange-700' },
  { value: 'subCategory', label: '📂 Chuyên Đề Con / Phân Loại', color: 'bg-sky-950/60 text-sky-300 border-sky-700' },
  { value: 'tags', label: '🏷️ Thẻ Phân Loại (Tags)', color: 'bg-rose-950/60 text-rose-300 border-rose-700' },
  { value: 'mediaUrl', label: '🖼️ Đa Phương Tiện (Ảnh/Media URL)', color: 'bg-pink-950/60 text-pink-300 border-pink-700' },
  { value: 'topicCode', label: '🔖 Mã Chủ Đề', color: 'bg-teal-950/60 text-teal-300 border-teal-700' },
  { value: 'topicName', label: '🏷️ Tên Chủ Đề', color: 'bg-teal-900/60 text-teal-200 border-teal-600' },
  { value: 'targetLevel', label: '🎓 Trình Độ / Khối Lớp', color: 'bg-lime-950/60 text-lime-300 border-lime-700' },
  { value: 'domainCode', label: '🌐 Lĩnh Vực / Khối Ngành', color: 'bg-emerald-900/60 text-emerald-200 border-emerald-600' },
  { value: 'type', label: '🔀 Loại Câu Hỏi (Single/Multi/Essay)', color: 'bg-cyan-950/60 text-cyan-300 border-cyan-700' },
  { value: 'ignore', label: '⛔ Bỏ Qua Cột Này', color: 'bg-gray-900 text-gray-500 border-gray-800' },
];

export const mapRoleToTargetField = (role: string): string => {
  if (role === 'answer') return 'ANSWER_KEY';
  if (role.startsWith('option_')) return 'OPTION';
  if (role === 'content') return 'QUESTION_CONTENT';
  if (role === 'reference') return 'EXPLANATION';
  if (role === 'context') return 'CONTEXT';
  if (role === 'contextTitle') return 'CONTEXT_TITLE';
  if (role === 'rubric') return 'GRADING_RUBRIC';
  if (role === 'duration') return 'DURATION';
  if (role === 'subCategory') return 'SUB_CATEGORY';
  if (role === 'tags') return 'TAGS';
  if (role === 'mediaUrl') return 'MEDIA_URL';
  if (role === 'targetLevel') return 'TARGET_LEVEL';
  if (role === 'domainCode') return 'DOMAIN_CODE';
  if (role === 'topicCode') return 'TOPIC_CODE';
  if (role === 'topicName') return 'TOPIC_NAME';
  if (role === 'stt') return 'STT';
  if (role === 'difficulty') return 'DIFFICULTY';
  if (role === 'critical') return 'CRITICAL';
  if (role === 'type') return 'TYPE';
  return 'UNKNOWN';
};

export function ExcelIngestionStudioModal({
  isOpen,
  isProcessing,
  inspectionData,
  topics,
  onClose,
  onConfirmImport,
  onTopicCreated,
  onUploadNewFile,
  onLoadSampleData,
}: ExcelIngestionStudioModalProps) {
  const { domains } = useAdaptiveTenant();
  const activeDomains = domains && domains.length > 0 ? domains : DOMAIN_OPTIONS;

  // Tab điều hướng Studio: 'sheets' | 'grid' | 'radar' | 'questions' | 'ai'
  const [activeTab, setActiveTab] = useState<'sheets' | 'grid' | 'radar' | 'questions' | 'ai'>('sheets');

  // Quản lý trạng thái các Sheet
  const [enabledSheets, setEnabledSheets] = useState<Record<string, boolean>>({});
  const [selectedSheetIndex, setSelectedSheetIndex] = useState<number>(0);

  // Danh sách câu hỏi có thể chỉnh sửa trực tiếp trong Studio
  const [editableQuestions, setEditableQuestions] = useState<any[]>([]);

  // Tùy biến schema mapping cho từng sheet
  const [sheetColumnMappings, setSheetColumnMappings] = useState<Record<string, Record<string, number>>>({});

  // Trạng thái tìm kiếm & lọc câu hỏi
  const [searchQuery, setSearchQuery] = useState('');
  const [filterType, setFilterType] = useState<string>('ALL');
  const [filterDifficulty, setFilterDifficulty] = useState<string>('ALL');
  const [onlyMissingAnswers, setOnlyMissingAnswers] = useState(false);
  const [onlyFatal, setOnlyFatal] = useState(false);

  // Trạng thái Modal Tạo Chủ Đề mới
  const [isCreateTopicOpen, setIsCreateTopicOpen] = useState(false);
  // Trạng thái thông báo Tự động làm giàu tri thức UCTE v4.0
  const [enrichToast, setEnrichToast] = useState<string | null>(null);

  // [UCIS v4.0 Hierarchical Topic Engine] Quản lý cấu hình chủ đề phân cấp động cho từng Sheet
  const [sheetTopics, setSheetTopics] = useState<Record<string, SheetTopicConfig>>({});

  // Khởi tạo dữ liệu từ inspectionData khi mở modal
  useEffect(() => {
    if (!inspectionData || !inspectionData.sheets) return;

    const initialEnabled: Record<string, boolean> = {};
    const allQ: any[] = [];
    const mappings: Record<string, Record<string, number>> = {};
    const initialSheetTopics: Record<string, SheetTopicConfig> = {};

    inspectionData.sheets.forEach((sh: any, idx: number) => {
      const sheetKey = sh.sheetName || `Sheet_${idx}`;
      initialEnabled[sheetKey] = sh.isEnabledForImport ?? true;
      mappings[sheetKey] = sh.columnMapping || {};

      const parentCode = sh.detectedParentTopicCode || '2026_DOT2';
      const parentName = sh.detectedParentTopicName || '2026-DOT2';
      const childCode = sh.detectedTopicCode || `${parentCode}_SHEET_${idx + 1}`;
      const childName = sh.detectedTopicName || sh.sheetName || 'Chủ đề chuyên môn';
      const qCount = (sh.questions || []).length;
      const desc = sh.detectedTopicDescription || `${childName} | Đường dẫn: ${parentName} | Số lượng: ${qCount} câu hỏi`;

      initialSheetTopics[sheetKey] = {
        parentTopicCode: parentCode,
        parentTopicName: parentName,
        childTopicCode: childCode,
        childTopicName: childName,
        topicDescription: desc,
      };

      (sh.questions || []).forEach((q: any) => {
        allQ.push({
          ...q,
          _sheetKey: sheetKey,
          _sheetName: sh.sheetName,
          topicCode: q.topicCode || childCode,
          topicName: q.topicName || childName,
          parentTopicCode: q.parentTopicCode || parentCode,
          parentTopicName: q.parentTopicName || parentName,
          topicDescription: q.topicDescription || desc,
          domainCode: q.domainCode || sh.detectedDomainCode || inspectionData.detectedDomainCode || 'GENERAL',
          targetLevel: q.targetLevel || sh.detectedTargetLevel || inspectionData.detectedTargetLevel,
          assessmentPurpose: q.assessmentPurpose || sh.detectedAssessmentPurpose || inspectionData.detectedAssessmentPurpose,
          issuingOrg: q.issuingOrg || sh.detectedIssuingOrg || inspectionData.detectedIssuingOrg,
          benchmarkYear: q.benchmarkYear || sh.detectedBenchmarkYear || inspectionData.detectedBenchmarkYear,
          benchmarkStandard: q.benchmarkStandard || sh.detectedBenchmarkStandard || inspectionData.detectedBenchmarkStandard,
          tags: Array.isArray(q.tags) && q.tags.length > 0 ? q.tags : (sh.detectedTags || inspectionData.detectedTags || []),
          difficulty: q.difficulty || 3,
        });
      });
    });

    setEnabledSheets(initialEnabled);
    setEditableQuestions(allQ);
    setSheetColumnMappings(mappings);
    setSheetTopics(initialSheetTopics);
    setSelectedSheetIndex(0);
  }, [inspectionData, topics]);

  // Cập nhật cấu hình chủ đề phân cấp cho Sheet và đồng bộ ngay vào các câu hỏi trực thuộc
  const handleUpdateSheetTopic = (sheetKey: string, updates: Partial<SheetTopicConfig>) => {
    setSheetTopics((prev) => {
      const current = prev[sheetKey] || {
        parentTopicCode: '2026_DOT2',
        parentTopicName: '2026-DOT2',
        childTopicCode: 'CREDIT',
        childTopicName: 'Chủ đề',
        topicDescription: '',
      };
      const updated = { ...current, ...updates };
      const next = { ...prev, [sheetKey]: updated };

      setEditableQuestions((allPrev) =>
        allPrev.map((q) => {
          if (q._sheetKey === sheetKey) {
            return {
              ...q,
              topicCode: updated.childTopicCode,
              topicName: updated.childTopicName,
              parentTopicCode: updated.parentTopicCode,
              parentTopicName: updated.parentTopicName,
              topicDescription: updated.topicDescription,
            };
          }
          return q;
        })
      );

      return next;
    });
  };

  const rawSheets: any[] = inspectionData?.sheets || [];
  const skippedSheets: any[] = inspectionData?.skippedSheets || [];
  const currentSheet = rawSheets[selectedSheetIndex] || rawSheets[0] || null;
  const currentSheetKey = currentSheet ? (currentSheet.sheetName || `Sheet_${selectedSheetIndex}`) : '';

  // Danh sách câu hỏi thuộc các sheet ĐƯỢC CHỌN NẠP
  const activeQuestions = useMemo(() => {
    return editableQuestions.filter((q) => enabledSheets[q._sheetKey] !== false);
  }, [editableQuestions, enabledSheets]);

  // Bộ lọc câu hỏi tại tab 'questions'
  const filteredQuestions = useMemo(() => {
    return activeQuestions.filter((q) => {
      if (onlyMissingAnswers && q.suggestedAnswer && q.suggestedAnswer.trim() !== '') return false;
      if (onlyFatal && !q.isCritical) return false;
      if (filterType !== 'ALL' && q.questionType !== filterType) return false;
      if (filterDifficulty !== 'ALL' && String(q.difficulty) !== filterDifficulty) return false;
      if (searchQuery.trim()) {
        const query = searchQuery.toLowerCase();
        const contentMatch = (q.content || '').toLowerCase().includes(query);
        const topicMatch = (q.topicCode || '').toLowerCase().includes(query);
        const optionMatch = (q.options || []).some((o: string) => (o || '').toLowerCase().includes(query));
        if (!contentMatch && !topicMatch && !optionMatch) return false;
      }
      return true;
    });
  }, [activeQuestions, onlyMissingAnswers, onlyFatal, filterType, filterDifficulty, searchQuery]);

  // Thống kê sức khỏe nhận thức tổng thể
  const overallHealth = useMemo(() => {
    const total = activeQuestions.length;
    const missingAns = activeQuestions.filter((q) => !q.suggestedAnswer || !q.suggestedAnswer.trim()).length;
    const fatalCount = activeQuestions.filter((q) => q.isCritical).length;
    const validCount = total - missingAns;
    const healthPercent = total > 0 ? Math.round((validCount / total) * 100) : 100;

    const typeBreakdown: Record<string, number> = {};
    const bloomBreakdown: Record<number, number> = { 1: 0, 2: 0, 3: 0, 4: 0 };

    activeQuestions.forEach((q) => {
      const t = q.questionType || 'SINGLE';
      typeBreakdown[t] = (typeBreakdown[t] || 0) + 1;
      const d = q.difficulty >= 1 && q.difficulty <= 4 ? q.difficulty : 3;
      bloomBreakdown[d] = (bloomBreakdown[d] || 0) + 1;
    });

    return { total, missingAns, fatalCount, validCount, healthPercent, typeBreakdown, bloomBreakdown };
  }, [activeQuestions]);

  // Toggle bật/tắt sheet
  const handleToggleSheet = (sheetKey: string) => {
    setEnabledSheets((prev) => ({
      ...prev,
      [sheetKey]: !prev[sheetKey],
    }));
  };

  // Cập nhật câu hỏi trực tiếp
  const handleUpdateQuestion = (indexInActive: number, patch: Partial<any>) => {
    setEditableQuestions((prev) => {
      const next = [...prev];
      const target = filteredQuestions[indexInActive];
      if (!target) return prev;
      const realIdx = next.findIndex((q) => q.tempId === target.tempId);
      if (realIdx !== -1) {
        next[realIdx] = { ...next[realIdx], ...patch };
      }
      return next;
    });
  };

  // 1-Click AI Làm sạch khoảng trắng và ký tự lạ
  const handleCleanSpacings = () => {
    setEditableQuestions((prev) =>
      prev.map((q) => ({
        ...q,
        content: (q.content || '')
          .replace(/_x000D_\n?/g, '')
          .replace(/\u00A0/g, ' ')
          .replace(/\s+/g, ' ')
          .trim(),
        options: (q.options || []).map((opt: string) =>
          (opt || '')
            .replace(/_x000D_\n?/g, '')
            .replace(/\u00A0/g, ' ')
            .replace(/^[A-Ja-j1-9][.):]\s*/i, (match) => match.trim() + ' ')
            .replace(/\s+/g, ' ')
            .trim()
        ),
      }))
    );
  };

  // 1-Click Xác nhận nạp
  const handleConfirm = async () => {
    if (activeQuestions.length === 0) {
      alert('Không có câu hỏi nào được chọn để nạp!');
      return;
    }
    await onConfirmImport(activeQuestions);
  };

  if (!isOpen) return null;

  // Nếu mở Studio nhưng chưa có dữ liệu nạp -> hiển thị màn hình Khởi Động (Launchpad) tiện dụng
  if (!inspectionData) {
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md animate-in fade-in duration-200">
        <div className="relative w-full max-w-2xl bg-gradient-to-b from-gray-950 via-slate-900 to-gray-950 border border-emerald-500/40 rounded-3xl shadow-2xl overflow-hidden text-gray-100 flex flex-col">
          {/* Header */}
          <div className="px-6 py-5 border-b border-gray-800 flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-2xl bg-emerald-500/20 border border-emerald-500/40 flex items-center justify-center text-emerald-400">
                <FileSpreadsheet className="w-5 h-5" />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <h2 className="text-base font-bold text-white tracking-wide">Universal Cognitive Ingestion Studio</h2>
                  <span className="px-2 py-0.5 rounded-full text-[10px] font-mono bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 font-bold">v3.0</span>
                </div>
                <p className="text-xs text-gray-400">Soi Chiếu Đa Sheet, Nhận Thức Lược Đồ & Tự Động Hóa Nhập Đề</p>
              </div>
            </div>
            <button
              onClick={onClose}
              className="p-2 rounded-xl bg-gray-800/60 hover:bg-gray-700 text-gray-400 hover:text-white transition-colors"
            >
              <X className="w-5 h-5" />
            </button>
          </div>

          {/* Body */}
          <div className="p-6 space-y-6">
            <div className="bg-emerald-950/30 border border-emerald-500/30 rounded-2xl p-4">
              <h3 className="text-sm font-bold text-emerald-300 flex items-center gap-2">
                <Sparkles className="w-4 h-4 text-amber-300" />
                Công nghệ Đột phá Nhập đề Excel Thông minh
              </h3>
              <p className="text-xs text-slate-300 mt-1.5 leading-relaxed">
                Hệ thống tự động phát hiện header từ bất kỳ dòng nào, phân tách sheet đề thi với sheet văn bản quy phạm, tự động ánh xạ số thứ tự đáp án (1, 2, 3, 4) sang nội dung phương án thực tế mà không nhầm loại câu hỏi.
              </p>
            </div>

            {/* Action Cards */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {onLoadSampleData && (
                <button
                  onClick={onLoadSampleData}
                  className="flex flex-col items-start p-4 rounded-2xl bg-gradient-to-br from-emerald-900/40 to-teal-950/60 border border-emerald-500/40 hover:border-emerald-400 transition-all hover:scale-[1.02] text-left group cursor-pointer shadow-lg shadow-emerald-950/50"
                >
                  <div className="w-9 h-9 rounded-xl bg-emerald-500/20 border border-emerald-500/30 flex items-center justify-center text-emerald-300 mb-3 group-hover:scale-110 transition-transform">
                    <Sparkles className="w-4 h-4 text-amber-300" />
                  </div>
                  <h4 className="text-sm font-bold text-white group-hover:text-emerald-300 transition-colors">
                    Khám phá Đề Tín Dụng 2026
                  </h4>
                  <p className="text-xs text-gray-400 mt-1">
                    Trải nghiệm ngay bộ đề chuẩn 240 câu Tín dụng KHDN, 2 Sheet, đầy đủ ánh xạ cột và radar nhận thức.
                  </p>
                  <span className="mt-3 text-[11px] font-bold text-emerald-400 flex items-center gap-1">
                    Trải nghiệm ngay →
                  </span>
                </button>
              )}

              {onUploadNewFile && (
                <button
                  onClick={onUploadNewFile}
                  className="flex flex-col items-start p-4 rounded-2xl bg-gray-900/70 border border-gray-800 hover:border-gray-700 transition-all hover:scale-[1.02] text-left group cursor-pointer"
                >
                  <div className="w-9 h-9 rounded-xl bg-blue-500/20 border border-blue-500/30 flex items-center justify-center text-blue-300 mb-3 group-hover:scale-110 transition-transform">
                    <FileSpreadsheet className="w-4 h-4 text-blue-400" />
                  </div>
                  <h4 className="text-sm font-bold text-white group-hover:text-blue-300 transition-colors">
                    Tải lên file Excel từ máy tính
                  </h4>
                  <p className="text-xs text-gray-400 mt-1">
                    Hỗ trợ định dạng .xlsx, .xls với đa dạng cấu trúc bảng biểu, tự động soi chiếu schema.
                  </p>
                  <span className="mt-3 text-[11px] font-bold text-blue-400 flex items-center gap-1">
                    Chọn tệp Excel →
                  </span>
                </button>
              )}
            </div>

            {/* Capabilities */}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 pt-2 border-t border-gray-800/80">
              <div className="p-2.5 rounded-xl bg-gray-900/40 border border-gray-800 text-center">
                <div className="text-xs font-bold text-purple-400">Sheet Matrix</div>
                <div className="text-[10px] text-gray-400 mt-0.5">Bảo vệ & lọc sheet rác</div>
              </div>
              <div className="p-2.5 rounded-xl bg-gray-900/40 border border-gray-800 text-center">
                <div className="text-xs font-bold text-indigo-400">Schema Mapper</div>
                <div className="text-[10px] text-gray-400 mt-0.5">Kéo thả cột linh hoạt</div>
              </div>
              <div className="p-2.5 rounded-xl bg-gray-900/40 border border-gray-800 text-center">
                <div className="text-xs font-bold text-blue-400">Answer Poly</div>
                <div className="text-[10px] text-gray-400 mt-0.5">Ánh xạ số [1..4] sang chữ</div>
              </div>
              <div className="p-2.5 rounded-xl bg-gray-900/40 border border-gray-800 text-center">
                <div className="text-xs font-bold text-emerald-400">Health Radar</div>
                <div className="text-[10px] text-gray-400 mt-0.5">Chẩn đoán câu điểm liệt</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-2 md:p-6 bg-black/85 backdrop-blur-md animate-in fade-in duration-200">
      <div className="bg-gradient-to-b from-gray-900 via-gray-950 to-black border border-gray-800/80 rounded-3xl w-full max-w-7xl h-[94vh] flex flex-col shadow-2xl shadow-purple-950/20 overflow-hidden text-gray-100">

        {/* ── TOP HEADER: METRIC STRIP & BRAND ────────────────────────── */}
        <header className="px-6 py-4 border-b border-gray-800/70 bg-gray-950/60 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="w-11 h-11 rounded-2xl bg-gradient-to-tr from-purple-600 via-indigo-600 to-blue-500 flex items-center justify-center shadow-lg shadow-indigo-600/30">
              <FileSpreadsheet className="w-6 h-6 text-white animate-pulse" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h2 className="text-lg font-black tracking-tight bg-gradient-to-r from-white via-gray-200 to-indigo-300 bg-clip-text text-transparent">
                  UNIVERSAL COGNITIVE INGESTION STUDIO
                </h2>
                <span className="px-2.5 py-0.5 rounded-full text-[10px] font-extrabold tracking-wider bg-purple-500/20 text-purple-300 border border-purple-500/30">
                  UCIS v3.0 SUPREMACY
                </span>
              </div>
              <p className="text-xs text-gray-400 flex items-center gap-2 mt-0.5">
                <span className="font-semibold text-gray-300">{inspectionData.fileName}</span>
                <span>•</span>
                <span>{rawSheets.length} Sheets phát hiện</span>
                <span>•</span>
                <span className="text-emerald-400 font-bold">{activeQuestions.length} câu hỏi chọn lọc</span>
                <span>•</span>
                <span className="px-1.5 py-0.2 rounded bg-indigo-950 text-indigo-300 border border-indigo-800/50 text-[10px]">
                  {inspectionData.detectedDomainCode || 'GENERAL'}
                </span>
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2.5">
            {/* Health Badge */}
            <div className="hidden sm:flex items-center gap-2 px-3 py-1.5 rounded-xl bg-gray-900 border border-gray-800 text-xs">
              <Activity className="w-4 h-4 text-emerald-400" />
              <span className="text-gray-400">Sức khỏe:</span>
              <span className={`font-bold ${overallHealth.healthPercent >= 90 ? 'text-emerald-400' : 'text-amber-400'}`}>
                {overallHealth.healthPercent}%
              </span>
            </div>

            {onUploadNewFile && (
              <button
                type="button"
                onClick={onUploadNewFile}
                disabled={isProcessing}
                className="hidden md:flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-gray-900 hover:bg-gray-800 border border-gray-700 text-xs font-semibold text-gray-300 hover:text-white transition-all cursor-pointer"
                title="Tải lên tệp Excel khác"
              >
                <FileSpreadsheet className="w-3.5 h-3.5 text-emerald-400" />
                <span>Đổi file</span>
              </button>
            )}

            {onLoadSampleData && (
              <button
                type="button"
                onClick={onLoadSampleData}
                disabled={isProcessing}
                className="hidden lg:flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-emerald-950/60 hover:bg-emerald-900/60 border border-emerald-500/40 text-xs font-semibold text-emerald-300 hover:text-emerald-200 transition-all cursor-pointer"
                title="Nạp lại bộ dữ liệu chuẩn mẫu 240 câu Tín dụng KHDN"
              >
                <Sparkles className="w-3.5 h-3.5 text-amber-300" />
                <span>Đề mẫu 240 câu</span>
              </button>
            )}

            <button
              onClick={onClose}
              disabled={isProcessing}
              className="p-2.5 rounded-xl bg-gray-800/60 hover:bg-gray-700/80 text-gray-400 hover:text-white transition-all cursor-pointer"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </header>

        {/* ── STUDIO TAB NAVIGATION ───────────────────────────────────── */}
        <div className="px-6 bg-gray-950/90 border-b border-gray-800/60 flex items-center justify-between gap-2 overflow-x-auto">
          <div className="flex items-center gap-1">
            <button
              onClick={() => setActiveTab('sheets')}
              className={`px-4 py-3 text-xs font-bold transition-all border-b-2 flex items-center gap-2 ${activeTab === 'sheets'
                  ? 'border-purple-500 text-purple-400 bg-purple-500/10'
                  : 'border-transparent text-gray-400 hover:text-gray-200'
                }`}
            >
              <Layers className="w-4 h-4" />
              Sheet Matrix Hub
              <span className="px-1.5 py-0.2 rounded-full bg-gray-800 text-[10px] text-gray-300">
                {rawSheets.length}
              </span>
            </button>

            <button
              onClick={() => setActiveTab('grid')}
              className={`px-4 py-3 text-xs font-bold transition-all border-b-2 flex items-center gap-2 ${activeTab === 'grid'
                  ? 'border-indigo-500 text-indigo-400 bg-indigo-500/10'
                  : 'border-transparent text-gray-400 hover:text-gray-200'
                }`}
            >
              <ArrowUpDown className="w-4 h-4" />
              Lưới Dữ Liệu & Ánh Xạ Cột
            </button>

            <button
              onClick={() => setActiveTab('questions')}
              className={`px-4 py-3 text-xs font-bold transition-all border-b-2 flex items-center gap-2 ${activeTab === 'questions'
                  ? 'border-blue-500 text-blue-400 bg-blue-500/10'
                  : 'border-transparent text-gray-400 hover:text-gray-200'
                }`}
            >
              <Eye className="w-4 h-4" />
              Duyệt & Sửa Câu Hỏi
              <span className="px-1.5 py-0.2 rounded-full bg-blue-900/60 text-blue-300 text-[10px]">
                {filteredQuestions.length}
              </span>
            </button>

            <button
              onClick={() => setActiveTab('radar')}
              className={`px-4 py-3 text-xs font-bold transition-all border-b-2 flex items-center gap-2 ${activeTab === 'radar'
                  ? 'border-emerald-500 text-emerald-400 bg-emerald-500/10'
                  : 'border-transparent text-gray-400 hover:text-gray-200'
                }`}
            >
              <Activity className="w-4 h-4" />
              Radar Sức Khỏe
              {overallHealth.missingAns > 0 && (
                <span className="px-1.5 py-0.2 rounded-full bg-amber-500/20 text-amber-400 text-[10px] border border-amber-500/30">
                  {overallHealth.missingAns} thiếu Đ/A
                </span>
              )}
            </button>

            <button
              onClick={() => setActiveTab('ai')}
              className={`px-4 py-3 text-xs font-bold transition-all border-b-2 flex items-center gap-2 ${activeTab === 'ai'
                  ? 'border-pink-500 text-pink-400 bg-pink-500/10'
                  : 'border-transparent text-gray-400 hover:text-gray-200'
                }`}
            >
              <Wand2 className="w-4 h-4 text-pink-400" />
              AI Copilot & Tự Chữa Lành
            </button>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => setIsCreateTopicOpen(true)}
              className="px-3 py-1.5 rounded-xl bg-gray-800 hover:bg-gray-700 text-xs font-bold text-gray-300 flex items-center gap-1.5 transition-all"
            >
              <Tag className="w-3.5 h-3.5 text-purple-400" />
              Tạo Chủ Đề Mới
            </button>
          </div>
        </div>

        {/* ── STUDIO WORKBENCH BODY ───────────────────────────────────── */}
        <div className="flex-1 overflow-y-auto p-6 bg-gray-950/40">

          {/* ════ TAB 1: SHEET MATRIX HUB ════ */}
          {activeTab === 'sheets' && (
            <div className="space-y-6 max-w-6xl mx-auto">
              <div>
                <h3 className="text-base font-extrabold text-white flex items-center gap-2">
                  <Layers className="w-5 h-5 text-purple-400" />
                  Ma Trận Sổ Tính Đa Sheet (Workbook Topology)
                </h3>
                <p className="text-xs text-gray-400 mt-1">
                  Hệ thống tự động phân loại từng sheet thành đề thi chính thức hoặc tài liệu tham khảo phi câu hỏi.
                  Bạn có thể bật hoặc tắt từng sheet để nạp chính xác những gì bạn mong muốn.
                </p>
              </div>

              {/* Danh sách các Sheet Đề Thi Được Nhận Diện */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {rawSheets.map((sh: any, sIdx: number) => {
                  const sheetKey = sh.sheetName || `Sheet_${sIdx}`;
                  const isEnabled = enabledSheets[sheetKey] !== false;
                  const qCount = (sh.questions || []).length;
                  const isCurrent = selectedSheetIndex === sIdx;

                  return (
                    <div
                      key={sheetKey}
                      className={`p-5 rounded-2xl border transition-all relative ${isEnabled
                          ? isCurrent
                            ? 'bg-gradient-to-br from-purple-950/40 via-gray-900 to-gray-900 border-purple-500/80 shadow-lg shadow-purple-950/20'
                            : 'bg-gray-900/80 border-gray-800 hover:border-gray-700'
                          : 'bg-gray-950/60 border-gray-900 opacity-60'
                        }`}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div className="flex items-center gap-3">
                          <button
                            onClick={() => handleToggleSheet(sheetKey)}
                            className={`w-6 h-6 rounded-lg flex items-center justify-center transition-all ${isEnabled
                                ? 'bg-purple-600 text-white shadow-md shadow-purple-600/30'
                                : 'bg-gray-800 text-gray-500'
                              }`}
                          >
                            <Check className="w-4 h-4" />
                          </button>
                          <div>
                            <div className="flex items-center gap-2">
                              <h4 className="font-extrabold text-sm text-white">{sh.sheetName}</h4>
                              <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-950 text-emerald-300 border border-emerald-800/60">
                                🎯 Đề Thi ({Math.round((sh.confidenceScore || 0.98) * 100)}% Tin cậy)
                              </span>
                            </div>
                            <p className="text-xs text-gray-400 mt-1">
                              {qCount} câu hỏi hợp lệ • Dòng header: #{sh.headerRowIndex ?? 0} • Chủ đề: {sh.detectedTopicName || sh.detectedTopicCode || 'Chung'}
                            </p>
                          </div>
                        </div>

                        <button
                          onClick={() => {
                            setSelectedSheetIndex(sIdx);
                            setActiveTab('grid');
                          }}
                          className="px-3 py-1.5 rounded-xl bg-gray-800 hover:bg-gray-700 text-xs font-bold text-indigo-300 flex items-center gap-1 transition-all"
                        >
                          <Eye className="w-3.5 h-3.5" />
                          Soi Lưới
                        </button>
                      </div>

                      {/* Mini Metric Footer */}
                      <div className="mt-4 pt-3 border-t border-gray-800/60 flex items-center justify-between text-[11px] text-gray-400">
                        <span>Độ khó Bloom: ⭐ {sh.questions?.[0]?.difficulty ?? 3}</span>
                        <span>Điểm liệt: {sh.questions?.filter((q: any) => q.isCritical).length ?? 0} câu</span>
                        <span>Định dạng: {sh.questions?.[0]?.questionType ?? 'SINGLE'}</span>
                      </div>

                      {/* [UCIS v4.0 Hierarchical Topic Assignment Box] */}
                      {sheetTopics[sheetKey] && (
                        <div className="mt-3 pt-3 border-t border-gray-800/80 space-y-2.5">
                          <div className="flex items-center justify-between">
                            <span className="text-[10px] font-black uppercase text-purple-400 tracking-wider flex items-center gap-1.5">
                              <Tag className="w-3 h-3 text-purple-400" />
                              Chủ Đề Phân Cấp (Hierarchical Topic)
                            </span>
                            <span className="text-[10px] px-2 py-0.5 rounded-md bg-purple-950/80 text-purple-300 font-mono border border-purple-800/40">
                              Mã: {sheetTopics[sheetKey].childTopicCode}
                            </span>
                          </div>

                          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                            {/* Chủ đề cha (Thư mục) */}
                            <div>
                              <label className="text-[10px] text-gray-400 font-bold block mb-1">
                                📁 Chủ đề cha (Thư mục gốc)
                              </label>
                              <input
                                type="text"
                                value={sheetTopics[sheetKey].parentTopicName}
                                onChange={(e) => {
                                  const pName = e.target.value;
                                  const pCode = pName.replace(/[^a-zA-Z0-9]/g, '_').toUpperCase();
                                  handleUpdateSheetTopic(sheetKey, { 
                                    parentTopicName: pName,
                                    parentTopicCode: pCode,
                                    topicDescription: `${sheetTopics[sheetKey].childTopicName} | Đường dẫn: ${pName} | Số lượng: ${qCount} câu hỏi`
                                  });
                                }}
                                className="w-full text-xs bg-gray-950 border border-gray-800 rounded-lg px-2.5 py-1.5 text-indigo-200 focus:border-indigo-500 focus:outline-none font-semibold"
                                placeholder="VD: 2026-DOT2"
                              />
                            </div>

                            {/* Tên chủ đề con (Tên file) */}
                            <div>
                              <label className="text-[10px] text-gray-400 font-bold block mb-1">
                                📄 Tên chủ đề con (Hiển thị học viên)
                              </label>
                              <input
                                type="text"
                                value={sheetTopics[sheetKey].childTopicName}
                                onChange={(e) => {
                                  const cName = e.target.value;
                                  handleUpdateSheetTopic(sheetKey, { 
                                    childTopicName: cName,
                                    topicDescription: `${cName} | Đường dẫn: ${sheetTopics[sheetKey].parentTopicName} | Số lượng: ${qCount} câu hỏi`
                                  });
                                }}
                                className="w-full text-xs bg-gray-950 border border-gray-800 rounded-lg px-2.5 py-1.5 text-white focus:border-purple-500 focus:outline-none font-semibold"
                                placeholder="VD: 1. Tín dụng KHDN - 240 câu"
                              />
                            </div>
                          </div>

                          {/* Chọn nhanh từ chủ đề có sẵn trong hệ thống */}
                          <div className="flex items-center gap-2">
                            <span className="text-[10px] text-gray-500 whitespace-nowrap">Hoặc gán chủ đề có sẵn:</span>
                            <select
                              value={sheetTopics[sheetKey].childTopicCode}
                              onChange={(e) => {
                                const selected = topics.find(t => t.code === e.target.value);
                                if (selected) {
                                  handleUpdateSheetTopic(sheetKey, {
                                    childTopicCode: selected.code,
                                    childTopicName: selected.name,
                                    topicDescription: selected.description || `${selected.name} | Số lượng: ${qCount} câu hỏi`
                                  });
                                }
                              }}
                              className="flex-1 text-[11px] bg-gray-950 border border-gray-800 rounded-lg px-2 py-1 text-gray-300 focus:border-purple-500 focus:outline-none"
                            >
                              <option value={sheetTopics[sheetKey].childTopicCode}>-- Giữ chủ đề tự nhận diện ({sheetTopics[sheetKey].childTopicName}) --</option>
                              {topics.map((t) => (
                                <option key={t.id || t.code} value={t.code}>
                                  {t.name} ({t.code})
                                </option>
                              ))}
                            </select>
                          </div>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>

              {/* Danh sách các Sheet Bị Lọc Bỏ (Skipped Sheets) */}
              {skippedSheets.length > 0 && (
                <div className="mt-8 pt-6 border-t border-gray-800">
                  <div className="flex items-center gap-2 mb-3">
                    <AlertTriangle className="w-4 h-4 text-amber-400" />
                    <h4 className="text-sm font-bold text-gray-300">
                      Các Sheet Phi Đề Thi Được Tự Động Bỏ Qua ({skippedSheets.length})
                    </h4>
                  </div>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                    {skippedSheets.map((sk: any, skIdx: number) => (
                      <div key={skIdx} className="p-4 rounded-xl bg-gray-900/40 border border-gray-800/60 flex items-center justify-between">
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="font-semibold text-xs text-gray-300">{sk.sheetName}</span>
                            <span className="px-2 py-0.2 rounded text-[10px] bg-amber-950/60 text-amber-300 border border-amber-800/50">
                              {sk.suggestedRole || 'Văn bản tham khảo'}
                            </span>
                          </div>
                          <p className="text-[11px] text-gray-500 mt-1">{sk.reason}</p>
                        </div>
                        <span className="text-xs text-gray-500 font-mono">{sk.rowCount} dòng</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}

          {/* ════ TAB 2: LIVE DATA GRID & SCHEMA MAPPER ════ */}
          {activeTab === 'grid' && currentSheet && (
            <div className="space-y-4">
              <div className="flex items-center justify-between bg-gray-900/60 p-4 rounded-2xl border border-gray-800">
                <div>
                  <h3 className="text-sm font-extrabold text-white flex items-center gap-2">
                    <ArrowUpDown className="w-4 h-4 text-indigo-400" />
                    Lưới Soi Chiếu Dữ Liệu & Ánh Xạ Cột — Sheet: <span className="text-purple-400">{currentSheet.sheetName}</span>
                  </h3>
                  <p className="text-xs text-gray-400 mt-0.5">
                    Thay đổi vai trò cột ở dropdown phía trên mỗi cột để tinh chỉnh ánh xạ nếu cần thiết.
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-xs text-gray-400">Xem trước 10 dòng đầu</span>
                </div>
              </div>

              {/* Thông báo Tự Động Làm Giàu Tri Thức UCTE v4.0 */}
              {enrichToast && (
                <div className="p-3 bg-gradient-to-r from-emerald-950/90 to-teal-950/90 border border-emerald-500/50 rounded-xl text-xs font-bold text-emerald-300 flex items-center gap-2 animate-pulse shadow-lg">
                  <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                  <span>{enrichToast}</span>
                </div>
              )}

              {/* Bảng Data Grid ảo */}
              <div className="overflow-x-auto border border-gray-800 rounded-2xl shadow-xl bg-gray-950">
                <table className="w-full text-left text-xs border-collapse">
                  <thead>
                    <tr className="bg-gray-900 border-b border-gray-800">
                      <th className="p-3 font-bold text-gray-400 border-r border-gray-800 w-12 text-center">#</th>
                      {(currentSheet.sampleRows?.[0] || []).map((_: any, colIdx: number) => {
                        // Xác định vai trò cột đang map
                        const currentRole = Object.entries(sheetColumnMappings[currentSheetKey] || {}).find(
                          ([_, c]) => c === colIdx
                        )?.[0] || 'ignore';

                        return (
                          <th key={colIdx} className="p-3 border-r border-gray-800 min-w-[180px] bg-gray-900/90">
                            <div className="space-y-1.5">
                              <span className="text-[10px] font-mono text-gray-400 uppercase tracking-wider">
                                Cột {String.fromCharCode(65 + colIdx)} (#{colIdx})
                              </span>
                              <select
                                value={currentRole}
                                onChange={async (e) => {
                                  const newRole = e.target.value;
                                  setSheetColumnMappings((prev) => {
                                    const next = { ...prev };
                                    const currentMap = { ...(next[currentSheetKey] || {}) };
                                    if (newRole === 'ignore') {
                                      delete currentMap[currentRole];
                                    } else {
                                      currentMap[newRole] = colIdx;
                                    }
                                    next[currentSheetKey] = currentMap;
                                    return next;
                                  });

                                  // Tự động kích hoạt cơ chế Làm Giàu Tri Thức UCTE v4.0
                                  const headerCell = currentSheet.sampleRows?.[currentSheet.headerRowIndex ?? 0]?.[colIdx];
                                  if (headerCell && newRole !== 'ignore') {
                                    const target = mapRoleToTargetField(newRole);
                                    if (target !== 'UNKNOWN') {
                                      try {
                                        await fetch('/api/quiz/questions/taxonomy/enrich', {
                                          method: 'POST',
                                          headers: { 'Content-Type': 'application/json' },
                                          body: JSON.stringify({
                                            targetField: target,
                                            rawHeader: headerCell,
                                            domainScope: currentSheet.detectedDomainCode || 'BANKING',
                                            languageCode: 'vi'
                                          })
                                        });
                                        setEnrichToast(`⚡ UCTE v4.0 đã tự làm giàu tri thức: "${headerCell}" ➔ ${newRole}`);
                                        setTimeout(() => setEnrichToast(null), 4000);
                                      } catch {
                                        // Ignore
                                      }
                                    }
                                  }
                                }}
                                className="w-full text-xs font-bold py-1 px-2 rounded-lg bg-gray-950 border border-indigo-500/50 text-indigo-200 focus:outline-none focus:ring-1 focus:ring-indigo-400"
                              >
                                {COLUMN_ROLE_OPTIONS.map((opt) => (
                                  <option key={opt.value} value={opt.value}>
                                    {opt.label}
                                  </option>
                                ))}
                              </select>
                            </div>
                          </th>
                        );
                      })}
                    </tr>
                  </thead>
                  <tbody>
                    {(currentSheet.sampleRows || []).slice(0, 10).map((row: string[], rIdx: number) => {
                      const isHeader = rIdx === (currentSheet.headerRowIndex ?? 0);
                      return (
                        <tr
                          key={rIdx}
                          className={`border-b border-gray-800/60 transition-colors ${isHeader ? 'bg-purple-950/30 font-bold text-purple-200' : 'hover:bg-gray-900/50'
                            }`}
                        >
                          <td className="p-3 text-center border-r border-gray-800/60 font-mono text-gray-500 text-[11px]">
                            {isHeader ? 'HDR' : rIdx + 1}
                          </td>
                          {row.map((cell: string, cIdx: number) => (
                            <td key={cIdx} className="p-3 border-r border-gray-800/60 max-w-xs truncate text-gray-300">
                              {cell || <span className="text-gray-600 italic">(trống)</span>}
                            </td>
                          ))}
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* ════ TAB 3: QUESTIONS INSPECTOR GRID ════ */}
          {activeTab === 'questions' && (
            <div className="space-y-4">
              {/* Filter Strip */}
              <div className="p-4 bg-gray-900/70 border border-gray-800 rounded-2xl flex flex-wrap items-center justify-between gap-3">
                <div className="flex items-center gap-2 flex-1 min-w-[240px]">
                  <Search className="w-4 h-4 text-gray-400" />
                  <input
                    type="text"
                    placeholder="Tìm kiếm nội dung câu hỏi, đáp án, mã chủ đề..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="w-full bg-transparent text-xs text-white placeholder-gray-500 focus:outline-none"
                  />
                </div>

                <div className="flex items-center gap-2">
                  <button
                    onClick={() => setOnlyMissingAnswers((v) => !v)}
                    className={`px-3 py-1.5 rounded-xl text-xs font-bold transition-all flex items-center gap-1.5 ${onlyMissingAnswers
                        ? 'bg-amber-600 text-white shadow-md shadow-amber-600/30'
                        : 'bg-gray-800 text-gray-400 hover:text-gray-200'
                      }`}
                  >
                    <AlertTriangle className="w-3.5 h-3.5" />
                    Chỉ câu thiếu đáp án ({overallHealth.missingAns})
                  </button>

                  <button
                    onClick={() => setOnlyFatal((v) => !v)}
                    className={`px-3 py-1.5 rounded-xl text-xs font-bold transition-all flex items-center gap-1.5 ${onlyFatal
                        ? 'bg-red-600 text-white shadow-md shadow-red-600/30'
                        : 'bg-gray-800 text-gray-400 hover:text-gray-200'
                      }`}
                  >
                    <AlertTriangle className="w-3.5 h-3.5" />
                    Điểm liệt ({overallHealth.fatalCount})
                  </button>
                </div>
              </div>

              {/* Danh sách câu hỏi */}
              <div className="space-y-3">
                {filteredQuestions.map((q, qIdx) => {
                  const isMissing = !q.suggestedAnswer || !q.suggestedAnswer.trim();

                  return (
                    <div
                      key={q.tempId || qIdx}
                      className={`p-5 rounded-2xl border transition-all ${isMissing
                          ? 'bg-amber-950/20 border-amber-600/60'
                          : q.isCritical
                            ? 'bg-red-950/20 border-red-600/60'
                            : 'bg-gray-900/60 border-gray-800/80 hover:border-gray-700'
                        }`}
                    >
                      <div className="flex items-start justify-between gap-4">
                        <div className="space-y-2 flex-1">
                          <div className="flex flex-wrap items-center gap-2">
                            <span className="px-2 py-0.5 rounded-md bg-purple-950 text-purple-300 font-bold text-xs border border-purple-800/60">
                              Câu #{qIdx + 1}
                            </span>
                            <span className="px-2 py-0.5 rounded-md bg-gray-800 text-gray-300 text-xs font-mono">
                              {q.questionType || 'SINGLE'}
                            </span>
                            <span className="px-2 py-0.5 rounded-md bg-gray-800 text-gray-300 text-xs font-bold">
                              Độ khó: ⭐ {q.difficulty}
                            </span>
                            <span className="px-2 py-0.5 rounded-md bg-indigo-950 text-indigo-300 text-xs border border-indigo-800/50">
                              Chủ đề: {q.topicName || q.topicCode}
                            </span>
                            {q.isCritical && (
                              <span className="px-2 py-0.5 rounded-md bg-red-950 text-red-300 font-bold text-xs border border-red-800 animate-pulse">
                                ⚠️ ĐIỂM LIỆT TỬ THẦN
                              </span>
                            )}
                          </div>

                          <div className="text-sm font-semibold text-gray-100 leading-relaxed pt-1">
                            <MathRenderer content={q.content} />
                          </div>

                          {/* Danh sách phương án */}
                          {q.options && q.options.length > 0 && (
                            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-2">
                              {q.options.map((opt: string, optIdx: number) => {
                                const cleanOpt = opt.replace(/^[A-Za-z0-9][\.\:\)\s\-]+/, '').trim();
                                const cleanAns = (q.suggestedAnswer || '').trim();
                                const isCorrect =
                                  cleanAns === String(optIdx + 1) ||
                                  cleanAns.toUpperCase() === LETTERS[optIdx] ||
                                  (cleanAns !== '' && (cleanAns.toLowerCase() === opt.trim().toLowerCase() || cleanAns.toLowerCase() === cleanOpt.toLowerCase()));

                                return (
                                  <div
                                    key={optIdx}
                                    onClick={() => handleUpdateQuestion(qIdx, { suggestedAnswer: String(optIdx + 1) })}
                                    title="Click để chọn phương án này làm đáp án đúng"
                                    className={`p-2.5 rounded-xl border text-xs flex items-start gap-2 cursor-pointer transition-all ${isCorrect
                                        ? 'bg-emerald-950/50 border-emerald-500 text-emerald-200 font-bold shadow-sm shadow-emerald-950/40 ring-1 ring-emerald-500/50'
                                        : 'bg-gray-950/50 border-gray-800/60 text-gray-300 hover:border-emerald-500/40 hover:bg-slate-900/60'
                                      }`}
                                  >
                                    <span
                                      className={`w-5 h-5 rounded-md flex items-center justify-center text-[10px] font-bold shrink-0 ${isCorrect ? 'bg-emerald-500 text-gray-950' : 'bg-gray-800 text-gray-400'
                                        }`}
                                    >
                                      {LETTERS[optIdx] || optIdx + 1}
                                    </span>
                                    <span className="flex-1">
                                      <MathRenderer content={opt} />
                                    </span>
                                  </div>
                                );
                              })}
                            </div>
                          )}

                          {/* Trích dẫn căn cứ */}
                          {q.aiExplanation && (
                            <p className="text-xs text-gray-400 italic pt-1 flex items-center gap-1.5">
                              <BookOpen className="w-3.5 h-3.5 text-indigo-400" />
                              Căn cứ: {q.aiExplanation}
                            </p>
                          )}
                        </div>

                        {/* Cột chỉnh sửa đáp án nhanh */}
                        <div className="w-36 flex flex-col items-end gap-2">
                          <label className="text-[10px] text-gray-400 font-bold uppercase tracking-wider">
                            Đáp án đúng:
                          </label>
                          <input
                            type="text"
                            value={q.suggestedAnswer || ''}
                            onChange={(e) => handleUpdateQuestion(qIdx, { suggestedAnswer: e.target.value })}
                            placeholder="VD: 1, 2, A, B"
                            className={`w-full py-1.5 px-2.5 rounded-xl text-center text-xs font-bold border focus:outline-none focus:ring-1 ${isMissing
                                ? 'bg-amber-950/60 border-amber-500 text-amber-200'
                                : 'bg-gray-950 border-gray-700 text-emerald-400'
                              }`}
                          />
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {/* ════ TAB 4: COGNITIVE HEALTH RADAR ════ */}
          {activeTab === 'radar' && (
            <div className="space-y-6 max-w-5xl mx-auto">
              <div>
                <h3 className="text-base font-extrabold text-white flex items-center gap-2">
                  <Activity className="w-5 h-5 text-emerald-400" />
                  Radar Sức Khỏe Nhận Thức & Thống Kê Bất Thường
                </h3>
                <p className="text-xs text-gray-400 mt-1">
                  Đánh giá toàn diện chất lượng dữ liệu khảo thí, đảm bảo không có câu hỏi khuyết thiếu trước khi nạp vào hệ thống.
                </p>
              </div>

              {/* 4 Thẻ chỉ số chính */}
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div className="p-5 rounded-2xl bg-gray-900 border border-gray-800 flex flex-col justify-between">
                  <span className="text-xs text-gray-400">Tổng Số Câu Hỏi</span>
                  <span className="text-3xl font-black text-white mt-2">{overallHealth.total}</span>
                  <span className="text-[10px] text-gray-500 mt-1">Thuộc các sheet đã chọn</span>
                </div>

                <div className="p-5 rounded-2xl bg-gray-900 border border-gray-800 flex flex-col justify-between">
                  <span className="text-xs text-emerald-400">Câu Hỏi Đạt Chuẩn</span>
                  <span className="text-3xl font-black text-emerald-400 mt-2">{overallHealth.validCount}</span>
                  <span className="text-[10px] text-emerald-600 mt-1">Đầy đủ nội dung & đáp án</span>
                </div>

                <div className="p-5 rounded-2xl bg-gray-900 border border-gray-800 flex flex-col justify-between">
                  <span className="text-xs text-amber-400">Câu Thiếu Đáp Án</span>
                  <span className="text-3xl font-black text-amber-400 mt-2">{overallHealth.missingAns}</span>
                  <span className="text-[10px] text-amber-600 mt-1">Cần bổ sung hoặc dùng AI giải</span>
                </div>

                <div className="p-5 rounded-2xl bg-gray-900 border border-gray-800 flex flex-col justify-between">
                  <span className="text-xs text-red-400">Câu Điểm Liệt Tử Thần</span>
                  <span className="text-3xl font-black text-red-400 mt-2">{overallHealth.fatalCount}</span>
                  <span className="text-[10px] text-red-600 mt-1">Khóa bài nếu làm sai</span>
                </div>
              </div>

              {/* Phân bổ Bậc nhận thức Bloom */}
              <div className="p-6 rounded-2xl bg-gray-900/80 border border-gray-800 space-y-4">
                <h4 className="font-extrabold text-sm text-white">Phân Bổ Độ Khó Nhận Thức (Thang Bloom 4 Bậc)</h4>
                <div className="grid grid-cols-4 gap-3 text-center">
                  {[
                    { level: 1, name: 'Nhận Biết (Bloom 1)', count: overallHealth.bloomBreakdown[1] || 0, color: 'text-blue-400 bg-blue-950/40 border-blue-800/60' },
                    { level: 2, name: 'Thông Hiểu (Bloom 2)', count: overallHealth.bloomBreakdown[2] || 0, color: 'text-emerald-400 bg-emerald-950/40 border-emerald-800/60' },
                    { level: 3, name: 'Vận Dụng (Bloom 3)', count: overallHealth.bloomBreakdown[3] || 0, color: 'text-amber-400 bg-amber-950/40 border-amber-800/60' },
                    { level: 4, name: 'Vận Dụng Cao (Bloom 4)', count: overallHealth.bloomBreakdown[4] || 0, color: 'text-purple-400 bg-purple-950/40 border-purple-800/60' },
                  ].map((b) => (
                    <div key={b.level} className={`p-4 rounded-xl border ${b.color}`}>
                      <span className="text-xs font-bold block">{b.name}</span>
                      <span className="text-2xl font-black block mt-2">{b.count} câu</span>
                      <span className="text-[10px] opacity-75 mt-1 block">
                        {overallHealth.total > 0 ? Math.round((b.count / overallHealth.total) * 100) : 0}%
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* ════ TAB 5: AI COPILOT & SELF-HEALING ════ */}
          {activeTab === 'ai' && (
            <div className="space-y-6 max-w-4xl mx-auto">
              <div>
                <h3 className="text-base font-extrabold text-white flex items-center gap-2">
                  <Wand2 className="w-5 h-5 text-pink-400" />
                  AI Self-Healing Copilot & Tự Động Khảo Thí
                </h3>
                <p className="text-xs text-gray-400 mt-1">
                  Bộ công cụ trí tuệ nhân tạo độc quyền của AegisQuiz giúp tự động làm sạch lỗi bảng tính, sửa chính tả và giải tự động các câu hỏi bị khuyết đáp án.
                </p>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="p-6 rounded-2xl bg-gray-900 border border-gray-800 space-y-3">
                  <div className="flex items-center gap-2">
                    <Sparkles className="w-5 h-5 text-indigo-400" />
                    <h4 className="font-bold text-sm text-white">Làm Sạch Khoảng Trắng & Rác Excel</h4>
                  </div>
                  <p className="text-xs text-gray-400">
                    Tự động loại bỏ các mã nhúng vô hình (`_x000D_`, non-breaking space `\u00A0`), dọn dẹp khoảng trắng kép và chèn khoảng cách chuẩn sau tiền tố phương án (ví dụ: `A.Đúng` ➔ `A. Đúng`).
                  </p>
                  <button
                    onClick={handleCleanSpacings}
                    className="w-full py-2.5 px-4 rounded-xl bg-indigo-600 hover:bg-indigo-500 font-bold text-xs text-white shadow-lg shadow-indigo-600/30 transition-all flex items-center justify-center gap-2"
                  >
                    <CheckCheck className="w-4 h-4" />
                    Thực Thi Làm Sạch Ngay
                  </button>
                </div>

                <div className="p-6 rounded-2xl bg-gray-900 border border-gray-800 space-y-3">
                  <div className="flex items-center gap-2">
                    <Wand2 className="w-5 h-5 text-pink-400" />
                    <h4 className="font-bold text-sm text-white">Gemini AI Auto-Solver (Giải Tự Động)</h4>
                  </div>
                  <p className="text-xs text-gray-400">
                    Kích hoạt mô hình AI Gemini 2.0 Pro phân tích câu hỏi và suy luận đáp án chuẩn xác nhất cho các câu hỏi đang bị khuyết đáp án, kèm trích dẫn giải thích.
                  </p>
                  <button
                    disabled={overallHealth.missingAns === 0}
                    onClick={() => alert('Đang gửi các câu hỏi khuyết đáp án lên Gemini AI Solver...')}
                    className={`w-full py-2.5 px-4 rounded-xl font-bold text-xs text-white shadow-lg transition-all flex items-center justify-center gap-2 ${overallHealth.missingAns > 0
                        ? 'bg-pink-600 hover:bg-pink-500 shadow-pink-600/30'
                        : 'bg-gray-800 text-gray-500 cursor-not-allowed'
                      }`}
                  >
                    <Play className="w-4 h-4" />
                    Giải {overallHealth.missingAns} Câu Khuyết Đáp Án Bằng AI
                  </button>
                </div>
              </div>
            </div>
          )}

        </div>

        {/* ── STUDIO FOOTER ACTION BAR ─────────────────────────────────── */}
        <footer className="px-6 py-4 border-t border-gray-800/80 bg-gray-950 flex items-center justify-between gap-4">
          <div className="flex items-center gap-4 text-xs">
            <span className="text-gray-400">
              Đã chọn: <span className="text-white font-extrabold">{activeQuestions.length}</span> câu hỏi
            </span>
            {overallHealth.missingAns > 0 && (
              <span className="text-amber-400 font-bold flex items-center gap-1">
                <AlertTriangle className="w-3.5 h-3.5" />
                Còn {overallHealth.missingAns} câu chưa có đáp án
              </span>
            )}
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={onClose}
              disabled={isProcessing}
              className="px-5 py-2.5 rounded-xl bg-gray-800 hover:bg-gray-700 font-bold text-xs text-gray-300 transition-all"
            >
              Hủy Bỏ
            </button>

            <button
              onClick={handleConfirm}
              disabled={isProcessing || activeQuestions.length === 0}
              className="px-6 py-2.5 rounded-xl bg-gradient-to-r from-purple-600 via-indigo-600 to-blue-600 hover:from-purple-500 hover:to-blue-500 text-white font-extrabold text-xs shadow-xl shadow-indigo-600/30 flex items-center gap-2 transition-all"
            >
              {isProcessing ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Đang Nạp Vào Ngân Hàng...
                </>
              ) : (
                <>
                  <Save className="w-4 h-4" />
                  ⚡ Xác Nhận Nạp Ngân Hàng Câu Hỏi ({activeQuestions.length})
                </>
              )}
            </button>
          </div>
        </footer>

      </div>

      {/* Modal tạo chủ đề mới nếu người dùng cần */}
      <CreateTopicModal
        isOpen={isCreateTopicOpen}
        topics={topics}
        targetQuestionIdx={null}
        totalQuestions={activeQuestions.length}
        onClose={() => setIsCreateTopicOpen(false)}
        onTopicCreated={(newTopic: BankTopic) => {
          setIsCreateTopicOpen(false);
          if (onTopicCreated) onTopicCreated(newTopic);
        }}
      />
    </div>
  );
}
