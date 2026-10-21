/**
 * useExcelDocxImport — Tách từ AdminQuestionsPage (index.tsx)
 * =============================================================
 * Gom toàn bộ state + handler cho luồng nhập câu hỏi từ Excel (UCIS Studio) và
 * Word/PDF (preview modal). Hành vi giữ nguyên 100% so với bản gốc.
 *
 * FSD Layer: pages/admin/questions/hooks/
 */
import { useState } from 'react';
import { learnerQuizService } from '@/services/learner-quiz.service';
import type { UserProfile } from '@/types/auth';
import { type BankTopic } from '../types';
import { getSampleBankingUcisData } from '../components/sampleBankingUcisData';

export function useExcelDocxImport(
  topics: BankTopic[],
  user: UserProfile | null | undefined,
  isTeamLeader: boolean,
  fetchData: () => Promise<void>,
  resetQuestionFilters: () => void,
) {
  // DOCX & PDF import state (Kịch bản A)
  const [docxPreviewList, setDocxPreviewList]       = useState<any[]>([]);
  const [showDocxPreviewModal, setShowDocxPreviewModal] = useState(false);
  const [isDocxProcessing, setIsDocxProcessing]     = useState(false);
  const [processingFileName, setProcessingFileName] = useState<string>('');
  const [processingDurationMs, setProcessingDurationMs] = useState<number | null>(null);
  const [ingestionEngineSource, setIngestionEngineSource] = useState<'client_edge' | 'cloud_server' | 'cloud_fallback'>('client_edge');

  // Excel DOT-2026 import state
  const [excelPreviewData, setExcelPreviewData]         = useState<any | null>(null);
  const [showExcelPreviewModal, setShowExcelPreviewModal] = useState(false);
  const [showExcelStudioModal, setShowExcelStudioModal] = useState(false);
  const [excelSelectedSheets, setExcelSelectedSheets]   = useState<Set<number>>(new Set());
  const [isExcelImporting, setIsExcelImporting]         = useState(false);

  const handleFileImport = async (file: File) => {
    setIsDocxProcessing(true);
    setProcessingFileName(file.name);
    const startMs = performance.now();
    try {
      const res = await learnerQuizService.importQuestions(file);
      setProcessingDurationMs(Math.round(performance.now() - startMs));
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
      setProcessingFileName('');
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
      resetQuestionFilters();
      await fetchData();
      alert(`✅ Đã nhập thành công ${res?.count ?? allItems.length} câu hỏi từ Excel!`);
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lưu câu hỏi Excel thất bại. Vui lòng thử lại.');
    } finally {
      setIsExcelImporting(false);
    }
  };

  const handleFileImportDocxOrPdf = async (file: File) => {
    const isPdf = file.name.toLowerCase().endsWith('.pdf');
    setIsDocxProcessing(true);
    setProcessingFileName(file.name);
    const startMs = performance.now();
    try {
      let data: any[] = [];
      if (isPdf) {
        setIngestionEngineSource('cloud_server');
        data = await learnerQuizService.importQuestionsPdf(file);
      } else {
        // [Supreme Precision] Sử dụng Backend Sovereign Ingestion Engine (.NET 10)
        // để bảo toàn 100% công thức MathType LaTeX, bảng đáp án ma trận, Sentinel Boundaries và CIG Grammar
        setIngestionEngineSource('cloud_server');
        data = await learnerQuizService.importQuestionsDocx(file);
      }

      setProcessingDurationMs(Math.round(performance.now() - startMs));
      if (!Array.isArray(data) || data.length === 0) {
        alert(`Không tìm thấy câu hỏi hợp lệ trong tệp ${isPdf ? 'PDF' : 'Word'}. Vui lòng kiểm tra cấu trúc định dạng file.`);
        return;
      }
      const mappedData = data.map((item: any) => {
        // [Zero-Hardcode] Tôn trọng tuyệt đối TopicCode và DomainCode mà Backend Cognitive Ingestion Engine đã nhận diện
        const resolvedTopic = item.topicCode?.trim() || 'GENERAL';
        return {
          ...item,
          topicCode: resolvedTopic,
          domainCode: item.domainCode || 'GENERAL',
          difficulty: item.difficulty || 3,
        };
      });
      setDocxPreviewList(mappedData);
      setShowDocxPreviewModal(true);
    } catch (err: any) {
      if (err.response?.status === 401) {
        alert('⚠️ Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại để tiếp tục tải lên đề thi.');
        window.location.href = '/login';
        return;
      }
      const errMsg = err.response?.data?.message || err.message || 'Nhập câu hỏi từ tệp thất bại. Vui lòng kiểm tra lại định dạng file hoặc liên hệ quản trị viên.';
      alert(`❌ Lỗi xử lý tệp ${isPdf ? 'PDF' : 'Word'}: ${errMsg}`);
    } finally {
      setIsDocxProcessing(false);
      setProcessingFileName('');
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

  return {
    docxPreviewList, setDocxPreviewList,
    showDocxPreviewModal, setShowDocxPreviewModal,
    isDocxProcessing, setIsDocxProcessing,
    processingFileName, setProcessingFileName,
    processingDurationMs, setProcessingDurationMs,
    ingestionEngineSource,
    excelPreviewData, setExcelPreviewData,
    showExcelPreviewModal, setShowExcelPreviewModal,
    showExcelStudioModal, setShowExcelStudioModal,
    excelSelectedSheets, setExcelSelectedSheets,
    isExcelImporting, setIsExcelImporting,
    handleFileImport,
    handleConfirmStudioImport,
    handleOpenExcelStudio,
    handleLoadStudioSample,
    handleConfirmExcelImport,
    handleFileImportDocxOrPdf,
    handleFileChange,
  };
}
