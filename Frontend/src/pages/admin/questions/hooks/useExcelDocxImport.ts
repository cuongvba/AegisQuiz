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

  // Excel DOT-2026 import state
  const [excelPreviewData, setExcelPreviewData]         = useState<any | null>(null);
  const [showExcelPreviewModal, setShowExcelPreviewModal] = useState(false);
  const [showExcelStudioModal, setShowExcelStudioModal] = useState(false);
  const [excelSelectedSheets, setExcelSelectedSheets]   = useState<Set<number>>(new Set());
  const [isExcelImporting, setIsExcelImporting]         = useState(false);

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

  return {
    docxPreviewList, setDocxPreviewList,
    showDocxPreviewModal, setShowDocxPreviewModal,
    isDocxProcessing, setIsDocxProcessing,
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
