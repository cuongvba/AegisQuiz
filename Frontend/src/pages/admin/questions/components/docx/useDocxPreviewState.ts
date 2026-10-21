import { useState, useEffect, useRef, useMemo } from 'react';
import type { BankTopic } from '../../types';
import { DOMAIN_OPTIONS } from './docx-constants';
import { useAdaptiveTenant } from '../../../../../hooks/useAdaptiveTenant';
import { useMathConfig } from '../../../../../shared/context/MathConfigContext';
import { learnerQuizService } from '../../../../../services/learner-quiz.service';
import { convertService } from '../../../../../services/convert.service';
import type { BulkTaxonomyApplyPayload } from '../BulkTaxonomyModal';
import type { DetectedTopicInfo } from './DocxTopicDecisionBanner';

interface UseDocxPreviewStateParams {
  docxPreviewList: any[];
  topics: BankTopic[];
  setDocxPreviewList: (v: any[]) => void;
  processingFileName?: string;
  onTopicCreated?: (topic: BankTopic) => void;
}

export function useDocxPreviewState({
  docxPreviewList,
  topics,
  setDocxPreviewList,
  processingFileName,
  onTopicCreated,
}: UseDocxPreviewStateParams) {
  const { domains } = useAdaptiveTenant();
  const activeDomains = domains && domains.length > 0
    ? domains.map((d: any) => ({
      code: d.code,
      label: d.label || d.name,
      short: (d.short || d.name || d.code).length > 15 ? (d.short || d.name || d.code).slice(0, 13) + '...' : (d.short || d.name || d.code),
      icon: d.icon || '🌐',
      color: 'from-purple-500 to-indigo-600',
      badge: 'bg-purple-950/70 text-purple-300 border-purple-800/60'
    }))
    : DOMAIN_OPTIONS;

  const [localTopics, setLocalTopics] = useState<BankTopic[]>(topics);
  const [bulkTopic, setBulkTopic] = useState<string>(topics.length > 0 ? topics[0].code : 'PARTY_BUILDING');
  const [bulkDifficulty, setBulkDifficulty] = useState<number>(3);
  const [showRawOptions, setShowRawOptions] = useState<Record<number, boolean>>({});
  const [selectedExamFilter, setSelectedExamFilter] = useState<string>('ALL');

  const [bulkDomain, setBulkDomain] = useState<string>('GENERAL');
  const [bulkTagInput, setBulkTagInput] = useState<string>('');
  const [showBulkTaxonomyModal, setShowBulkTaxonomyModal] = useState<boolean>(false);
  const [showAdvancedBulkTools, setShowAdvancedBulkTools] = useState<boolean>(false);
  const [expandedCoords, setExpandedCoords] = useState<Record<number, boolean>>({});
  const [tagInputPerQ, setTagInputPerQ] = useState<Record<number, string>>({});

  const [isExportingExcel, setIsExportingExcel] = useState<boolean>(false);
  const [showExportMenu, setShowExportMenu] = useState<boolean>(false);
  const [isTranspilingStandard, setIsTranspilingStandard] = useState<boolean>(false);
  const [showTranspileMenu, setShowTranspileMenu] = useState<boolean>(false);
  const customTemplateInputRef = useRef<HTMLInputElement>(null);

  const [dynamicPresets, setDynamicPresets] = useState<any[]>([]);
  const [showCreateTopicModal, setShowCreateTopicModal] = useState(false);
  const [targetQuestionIdx, setTargetQuestionIdx] = useState<number | null>(null);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const [isCorrectingVN, setIsCorrectingVN] = useState<boolean>(false);
  const [rawOriginalList, setRawOriginalList] = useState<any[] | null>(null);

  const mathContext = useMathConfig();
  const [showMathStudio, setShowMathStudio] = useState<boolean>(false);
  const [showCigRules, setShowCigRules] = useState<boolean>(false);

  const [isDecisionDismissed, setIsDecisionDismissed] = useState<boolean>(false);
  const [isCreatingVirtualTopic, setIsCreatingVirtualTopic] = useState<boolean>(false);

  useEffect(() => {
    let isMounted = true;
    learnerQuizService.getCoordinatePresets()
      .then((data: any[]) => {
        if (isMounted && Array.isArray(data)) {
          setDynamicPresets(data);
        }
      })
      .catch(() => { });
    return () => { isMounted = false; };
  }, []);

const KNOWN_TOPIC_NAMES: Record<string, string> = {
  'TIENG_ANH': 'Tiếng Anh',
  'ENGLISH': 'Tiếng Anh',
  'MATH': 'Toán học',
  'TOAN': 'Toán học',
  'HOA_HOC': 'Hóa học',
  'VAT_LY': 'Vật lý',
  'SINH_HOC': 'Sinh học',
  'LICH_SU': 'Lịch sử',
  'DIA_LY': 'Địa lý',
  'NGU_VAN': 'Ngữ văn',
  'CNTT': 'Tin học & CNTT',
  'GDKT_PL': 'Giáo dục KT & Pháp luật',
};

function inferQuestionTopic(q: any): { topicCode: string; topicName: string; domainCode: string } | null {
  const tags = Array.isArray(q.tags) ? q.tags : [];
  const text = `${q.contextTitle || ''} ${q.contextContent || ''} ${q.content || ''} ${q.options?.join(' ') || ''}`.toLowerCase();

  if (tags.some((t: string) => t === 'tieng_anh' || t === 'tieng-anh' || t === 'english')
    || /\b(?:reading|passage|pronunciation|closest in meaning|opposite in meaning|mark the letter|best arrangement)\b/i.test(text)
    || (text.match(/\b(?:the|and|is|in|of|to|that|it|for|with|was|on|are|by|an|at)\b/g)?.length || 0) >= 4) {
    return { topicCode: 'TIENG_ANH', topicName: 'Tiếng Anh', domainCode: 'EDUCATION' };
  }

  if (tags.some((t: string) => t === 'toan' || t === 'toan-hoc' || t === 'math')
    || text.includes('\\frac') || text.includes('\\sqrt') || text.includes('đạo hàm') || text.includes('tích phân') || text.includes('hàm số')) {
    return { topicCode: 'MATH', topicName: 'Toán học', domainCode: 'EDUCATION' };
  }

  if (tags.some((t: string) => t === 'hoa_hoc' || t === 'hoa-hoc' || t === 'chemistry')
    || text.includes('axit') || text.includes('acid') || text.includes('este') || text.includes('ancol') || text.includes('hcl')) {
    return { topicCode: 'HOA_HOC', topicName: 'Hóa học', domainCode: 'EDUCATION' };
  }

  if (tags.some((t: string) => t === 'vat_ly' || t === 'vat-ly' || t === 'physics')
    || text.includes('nhiệt lượng') || text.includes('từ trường') || text.includes('bước sóng')) {
    return { topicCode: 'VAT_LY', topicName: 'Vật lý', domainCode: 'EDUCATION' };
  }

  if (tags.some((t: string) => t === 'sinh_hoc' || t === 'sinh-hoc' || t === 'biology')
    || text.includes('nhiễm sắc thể') || text.includes('đột biến') || text.includes('gen') || text.includes('adn')) {
    return { topicCode: 'SINH_HOC', topicName: 'Sinh học', domainCode: 'EDUCATION' };
  }

  return null;
}

  useEffect(() => {
    // 1. Tự động suy luận môn học cho từng câu hỏi nếu đang bị gán GENERAL
    let hasChanges = false;
    const effectiveList = docxPreviewList.map(q => {
      if (!q.topicCode || q.topicCode === 'GENERAL' || q.topicCode === 'EDUCATION') {
        const inferred = inferQuestionTopic(q);
        if (inferred) {
          hasChanges = true;
          return {
            ...q,
            topicCode: inferred.topicCode,
            topicName: inferred.topicName,
            domainCode: inferred.domainCode,
          };
        }
      }
      return q;
    });

    if (hasChanges) {
      setDocxPreviewList(effectiveList);
    }

    // 2. Thu thập tất cả các TopicCode trong danh sách câu hỏi & tạo chủ đề ảo đưa vào localTopics
    const mergedTopics: BankTopic[] = [...topics];
    const existingCodes = new Set(topics.map(t => t.code.toUpperCase()));

    effectiveList.forEach(q => {
      const code = q.topicCode?.trim().toUpperCase();
      if (code && code !== 'GENERAL' && code !== 'EDUCATION' && code !== 'THPT_NATIONAL' && !existingCodes.has(code)) {
        existingCodes.add(code);
        const displayName = KNOWN_TOPIC_NAMES[code] || q.topicName?.trim() || code;
        mergedTopics.push({
          id: `virtual_${code}`,
          code: code,
          name: displayName,
          categoryCode: q.domainCode || 'EDUCATION',
          enabled: true,
          visibilityScope: 'PUBLIC',
          domainCode: q.domainCode || 'EDUCATION',
        });
      }
    });

    setLocalTopics(mergedTopics);

    // 3. Đồng bộ Chủ đề đã nhận diện lên thanh công cụ (bulkTopic)
    const detectedTopic = effectiveList.find(q => q.topicCode && q.topicCode !== 'GENERAL' && q.topicCode !== 'EDUCATION' && q.topicCode !== 'THPT_NATIONAL')?.topicCode;
    if (detectedTopic) {
      setBulkTopic(detectedTopic);
    } else if (mergedTopics.length > 0 && (!bulkTopic || !mergedTopics.some(t => t.code === bulkTopic))) {
      setBulkTopic(mergedTopics[0].code);
    }

    // 4. Đồng bộ Miền ngành (Domain) đã nhận diện lên thanh công cụ (bulkDomain)
    const detectedDomain = effectiveList.find(q => q.domainCode && q.domainCode !== 'GENERAL')?.domainCode;
    if (detectedDomain && activeDomains.some((d: any) => d.code === detectedDomain)) {
      setBulkDomain(detectedDomain);
    }
  }, [topics, docxPreviewList, activeDomains]);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3500);
  };

  const openCreateTopicModal = (qIdx: number | null = null) => {
    setTargetQuestionIdx(qIdx);
    setShowCreateTopicModal(true);
  };

  const updateItem = (idx: number, field: string, value: any) => {
    const updated = [...docxPreviewList];
    updated[idx] = { ...updated[idx], [field]: value };
    setDocxPreviewList(updated);
  };

  const handleOptionChange = (itemIdx: number, optIdx: number, val: string) => {
    const updated = [...docxPreviewList];
    updated[itemIdx].options[optIdx] = val;
    setDocxPreviewList(updated);
  };

  const handleDeleteItem = (targetIdx: number) => {
    const updated = docxPreviewList.filter((_, i) => i !== targetIdx);
    setDocxPreviewList(updated);
  };

  const handleApplyAllCore = () => {
    const updated = docxPreviewList.map(item => ({
      ...item,
      topicCode: bulkTopic || item.topicCode,
      difficulty: bulkDifficulty || item.difficulty,
      domainCode: bulkDomain || item.domainCode,
    }));
    setDocxPreviewList(updated);
    showToast(`Đã áp dụng Chủ đề, Độ khó & Ngành cho toàn bộ ${docxPreviewList.length} câu hỏi!`);
  };

  // Tính toán thông tin chủ đề nhận diện được từ bộ câu hỏi đã parse
  const detectedTopicInfo = useMemo<DetectedTopicInfo | null>(() => {
    const found = docxPreviewList.find(q =>
      q.topicCode &&
      q.topicCode !== 'GENERAL' &&
      q.topicCode !== 'EDUCATION' &&
      q.topicCode !== 'THPT_NATIONAL'
    );
    if (!found || !found.topicCode) return null;
    const code = found.topicCode.trim().toUpperCase();
    const name = KNOWN_TOPIC_NAMES[code] || found.topicName?.trim() || code;
    const domainCode = found.domainCode?.trim() || 'EDUCATION';
    const isVirtual = !topics.some(t => t.code.toUpperCase() === code);
    return { code, name, domainCode, isVirtual };
  }, [docxPreviewList, topics]);

  // Hiển thị banner quyết định nếu phát hiện chủ đề và chưa bị người dùng đóng
  const showTopicDecisionBanner = Boolean(
    detectedTopicInfo &&
    !isDecisionDismissed &&
    (detectedTopicInfo.isVirtual || docxPreviewList.some(q => q.topicCode !== detectedTopicInfo.code) || bulkTopic !== detectedTopicInfo.code)
  );

  // Lựa chọn 1: Áp dụng chủ đề nhận diện (tạo mới chủ đề vào DB nếu là ảo)
  const handleApplyDetectedTopic = async () => {
    if (!detectedTopicInfo) return;
    setIsCreatingVirtualTopic(true);
    try {
      const targetCode = detectedTopicInfo.code;
      const targetName = detectedTopicInfo.name;
      const targetDomain = detectedTopicInfo.domainCode || 'EDUCATION';

      if (detectedTopicInfo.isVirtual) {
        try {
          const res = await learnerQuizService.createTopic({
            code: targetCode,
            name: targetName,
            description: `Chủ đề tạo tự động từ bộ đề thi: ${processingFileName || targetName}`,
            categoryCode: targetDomain,
            domainCode: targetDomain,
            enabled: true,
            visibilityScope: 'PUBLIC',
            scope: 'COMMUNITY',
          });

          const newTopic: BankTopic = {
            id: res?.id || res?.data?.id || `topic_${Date.now()}`,
            code: targetCode,
            name: targetName,
            description: `Chủ đề tạo tự động từ bộ đề thi: ${processingFileName || targetName}`,
            categoryCode: targetDomain,
            domainCode: targetDomain,
            enabled: true,
            visibilityScope: 'PUBLIC',
          };

          setLocalTopics(prev => {
            const filtered = prev.filter(t => t.code.toUpperCase() !== targetCode);
            return [...filtered, newTopic];
          });
          onTopicCreated?.(newTopic);
        } catch (createErr: any) {
          console.warn('Backend createTopic call warning (có thể đã tồn tại hoặc fallback)', createErr);
        }
      }

      // Áp dụng chủ đề nhận diện cho toàn bộ câu hỏi
      const updated = docxPreviewList.map(item => ({
        ...item,
        topicCode: targetCode,
        topicName: targetName,
        domainCode: targetDomain,
      }));
      setDocxPreviewList(updated);
      setBulkTopic(targetCode);
      setBulkDomain(targetDomain);
      setIsDecisionDismissed(true);
      showToast(`✨ Đã áp dụng chủ đề "${targetName}" (${targetCode}) cho toàn bộ ${docxPreviewList.length} câu hỏi!`);
    } catch (err: any) {
      alert(err.response?.data?.message || 'Có lỗi khi áp dụng chủ đề. Vui lòng thử lại.');
    } finally {
      setIsCreatingVirtualTopic(false);
    }
  };

  // Lựa chọn 2: Đồng bộ theo chủ đề người dùng đang chọn
  const handleSyncWithSelectedTopic = () => {
    const selectedTopicObj = localTopics.find(t => t.code.toUpperCase() === bulkTopic.toUpperCase());
    const targetName = selectedTopicObj?.name || bulkTopic;
    const targetDomain = selectedTopicObj?.domainCode || bulkDomain;

    const updated = docxPreviewList.map(item => ({
      ...item,
      topicCode: bulkTopic,
      topicName: targetName,
      domainCode: targetDomain,
    }));
    setDocxPreviewList(updated);
    setIsDecisionDismissed(true);
    showToast(`🔄 Đã đồng bộ toàn bộ ${docxPreviewList.length} câu hỏi theo chủ đề đã chọn: "${targetName}"!`);
  };

  const handleDismissTopicDecision = () => {
    setIsDecisionDismissed(true);
  };

  const handleCorrectVietnamese = async () => {
    if (!docxPreviewList || docxPreviewList.length === 0) return;
    setIsCorrectingVN(true);
    try {
      if (!rawOriginalList) {
        setRawOriginalList([...docxPreviewList]);
      }
      const corrected = await learnerQuizService.correctVietnameseQuestions(docxPreviewList);
      if (corrected && Array.isArray(corrected)) {
        setDocxPreviewList(corrected);
        showToast(`✨ Đã chuẩn hóa chính tả tiếng Việt & Maprepl cho ${corrected.length} câu hỏi!`);
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi chuẩn hóa tiếng Việt. Vui lòng thử lại.');
    } finally {
      setIsCorrectingVN(false);
    }
  };

  const handleRevertRawText = () => {
    if (rawOriginalList) {
      setDocxPreviewList([...rawOriginalList]);
      setRawOriginalList(null);
      showToast('Đã hoàn tác về văn bản gốc ban đầu của file!');
    }
  };

  const handleAddBulkTag = () => {
    const raw = bulkTagInput.trim().replace(/^#/, '');
    if (!raw) return;
    const updated = docxPreviewList.map(item => {
      const currentTags = Array.isArray(item.tags) ? [...item.tags] : [];
      if (!currentTags.includes(raw)) currentTags.push(raw);
      return { ...item, tags: currentTags };
    });
    setDocxPreviewList(updated);
    setBulkTagInput('');
    showToast(`Đã gán thẻ '#${raw}' cho tất cả ${docxPreviewList.length} câu hỏi!`);
  };

  const handleAutoClassifyTopics = () => {
    const updated = docxPreviewList.map(item => {
      const text = `${item.content} ${item.options?.join(' ') || ''}`.toLowerCase();
      let bestCode = item.topicCode || (topics.length > 0 ? topics[0].code : 'GENERAL');

      for (const t of topics) {
        const tName = t.name.toLowerCase();
        const tCode = t.code.toLowerCase();
        if (text.includes(tCode) || text.includes(tName)) {
          bestCode = t.code;
          break;
        }
      }

      if (text.includes('kho tiền') || text.includes('chìa khóa') || text.includes('niêm phong') || text.includes('kiểm đếm')) {
        const found = topics.find(t => t.code === '381' || t.name.toLowerCase().includes('kế toán') || t.name.toLowerCase().includes('ngân quỹ'));
        if (found) bestCode = found.code;
      } else if (text.includes('tín dụng') || text.includes('cho vay') || text.includes('thế chấp') || text.includes('cic') || text.includes('nợ xấu')) {
        const found = topics.find(t => t.code === '382' || t.name.toLowerCase().includes('tín dụng') || t.name.toLowerCase().includes('cho vay'));
        if (found) bestCode = found.code;
      } else if (text.includes('đảng') || text.includes('chi bộ') || text.includes('đảng viên') || text.includes('quy định 69')) {
        const found = topics.find(t => t.code === 'PARTY_BUILDING' || t.name.toLowerCase().includes('đảng'));
        if (found) bestCode = found.code;
      } else if (text.includes('mật khẩu') || text.includes('giao thức') || text.includes('itsm') || text.includes('cntt') || text.includes('bảo mật')) {
        const found = topics.find(t => t.code === 'CNTT' || t.name.toLowerCase().includes('công nghệ') || t.name.toLowerCase().includes('cntt'));
        if (found) bestCode = found.code;
      } else if (/\b(?:the|and|is|in|of|to|that|it|for|with|was|on|are|by|reading|passage|pronunciation|closest in meaning)\b/i.test(text)) {
        const found = topics.find(t => t.code === 'TIENG_ANH' || t.code === 'ENGLISH' || t.name.toLowerCase().includes('tiếng anh'));
        if (found) bestCode = found.code;
      } else if (text.includes('đạo hàm') || text.includes('tích phân') || text.includes('hàm số') || text.includes('vectơ') || text.includes('phương trình') || text.includes('\\frac') || text.includes('\\sqrt')) {
        const found = topics.find(t => t.code === 'MATH' || t.code === 'TOAN' || t.name.toLowerCase().includes('toán'));
        if (found) bestCode = found.code;
      } else if (text.includes('axit') || text.includes('acid') || text.includes('bazo') || text.includes('este') || text.includes('ancol') || text.includes('mol') || text.includes('hcl') || text.includes('h2so4')) {
        const found = topics.find(t => t.code === 'HOA_HOC' || t.name.toLowerCase().includes('hóa'));
        if (found) bestCode = found.code;
      } else if (text.includes('nhiệt lượng') || text.includes('từ trường') || text.includes('bước sóng') || text.includes('hạt nhân') || text.includes('phóng xạ') || text.includes('quang điện')) {
        const found = topics.find(t => t.code === 'VAT_LY' || t.name.toLowerCase().includes('vật lý') || t.name.toLowerCase().includes('vật lí'));
        if (found) bestCode = found.code;
      } else if (text.includes('nhiễm sắc thể') || text.includes('đột biến') || text.includes('gen') || text.includes('adn') || text.includes('quần thể')) {
        const found = topics.find(t => t.code === 'SINH_HOC' || t.name.toLowerCase().includes('sinh'));
        if (found) bestCode = found.code;
      }

      return { ...item, topicCode: bestCode };
    });

    setDocxPreviewList(updated);
    showToast('Đã nhận diện chủ đề theo từ khóa!');
  };

  const sniffCoordinatesFromDocxList = () => {
    const sample = docxPreviewList.find(q => q.targetLevel || q.assessmentPurpose || q.issuingOrg || (q.domainCode && q.domainCode !== 'GENERAL')) || docxPreviewList[0];
    const allSampleText = docxPreviewList.slice(0, 30).map(q =>
      `${q.contextTitle || ''} ${q.contextContent || ''} ${q.content || ''} ${q.options?.join(' ') || ''}`
    ).join('\n');
    const lowerAll = allSampleText.toLowerCase();

    let targetLevel = sample?.targetLevel || '';
    let assessmentPurpose = sample?.assessmentPurpose || '';
    let issuingOrg = sample?.issuingOrg || '';
    let benchmarkYear = sample?.benchmarkYear ? String(sample.benchmarkYear) : (new Date().getFullYear().toString());
    let benchmarkStandard = sample?.benchmarkStandard || '';
    const sniffedTags = new Set<string>(sample?.tags || []);

    const posMatch = allSampleText.match(/(?:VỊ\s*TRÍ|CHỨC\s*DANH|CẤP\s*BẬC|KHỐI\s*LỚP|HẠNG\s*BẰNG|ĐỐI\s*TƯỢNG)\s*[:：]\s*([^\r\n,;]+)/i);
    if (posMatch && posMatch[1]) {
      targetLevel = posMatch[1].trim().replace(/\s+/g, ' ');
    } else {
      const matchedPreset = dynamicPresets.find((p: any) =>
        p.coordinateType === 'TARGET_LEVEL' && (
          lowerAll.includes(p.presetLabel?.toLowerCase()) ||
          (Array.isArray(p.synonyms) && p.synonyms.some((s: string) => lowerAll.includes(s.toLowerCase())))
        )
      );
      if (matchedPreset) targetLevel = matchedPreset.presetLabel;
    }

    const purposeMatch = allSampleText.match(/(?:BỘ\s*CÂU\s*HỎI[\s,]*(?:ĐÁP\s*ÁN\s*)?)?(?:ÔN\s*TẬP\s*)?(KỲ\s*KIỂM\s*TRA[^\r\n,;]+(?:NĂM\s*20\d\d)?)/i)
      || allSampleText.match(/(KIỂM\s*TRA\s*CHUYÊN\s*MÔN\s*NGHIỆP\s*VỤ[^\r\n,;]+)/i)
      || allSampleText.match(/(SÁT\s*HẠCH[^\r\n,;]+)/i)
      || allSampleText.match(/(THI\s*TỐT\s*NGHIỆP[^\r\n,;]+)/i)
      || allSampleText.match(/(ĐÁNH\s*GIÁ\s*NĂNG\s*LỰC[^\r\n,;]+)/i);
    if (purposeMatch && purposeMatch[1]) {
      assessmentPurpose = purposeMatch[1].trim().replace(/\s+/g, ' ');
    } else {
      const matchedPreset = dynamicPresets.find((p: any) =>
        p.coordinateType === 'ASSESSMENT_PURPOSE' && (
          lowerAll.includes(p.presetLabel?.toLowerCase()) ||
          (Array.isArray(p.synonyms) && p.synonyms.some((s: string) => lowerAll.includes(s.toLowerCase())))
        )
      );
      if (matchedPreset) assessmentPurpose = matchedPreset.presetLabel;
    }

    const yearMatch = allSampleText.match(/NĂM\s*(20\d\d)/i) || allSampleText.match(/\b(202[0-9]|201[8-9])\b/);
    if (yearMatch && yearMatch[1]) {
      benchmarkYear = yearMatch[1].trim();
    }

    const orgMatch = allSampleText.match(/(?:CƠ\s*QUAN|ĐƠN\s*VỊ|TỔ\s*CHỨC|BAN\s*HÀNH|NGÂN\s*HÀNG|BỘ\s+[A-ZÀ-Ỹ]+|CỤC\s+[A-ZÀ-Ỹ]+|CHI\s*NHÁNH)\s*[:：]\s*([^\r\n,;]+)/i);
    if (orgMatch && orgMatch[1]) {
      issuingOrg = orgMatch[1].trim().replace(/\s+/g, ' ');
    } else {
      const matchedPreset = dynamicPresets.find((p: any) =>
        p.coordinateType === 'ISSUING_ORG' && (
          lowerAll.includes(p.presetLabel?.toLowerCase()) ||
          (Array.isArray(p.synonyms) && p.synonyms.some((s: string) => lowerAll.includes(s.toLowerCase())))
        )
      );
      if (matchedPreset) issuingOrg = matchedPreset.presetLabel;
    }

    const stdMatch = allSampleText.match(/(Quy\s*định\s*(?:số\s*)?[\d/]+[^\r\n,;]*)/i)
      || allSampleText.match(/(Quy\s*chế[^\r\n,;]*)/i)
      || allSampleText.match(/(Thông\s*tư\s*(?:số\s*)?[\d/]+[^\r\n,;]*)/i)
      || allSampleText.match(/(Nghị\s*định\s*(?:số\s*)?[\d/]+[^\r\n,;]*)/i)
      || allSampleText.match(/(ISO[\s\d\:\-]+[^\r\n,;]*)/i)
      || allSampleText.match(/(GDPT\s*\d{4}[^\r\n,;]*)/i);
    if (stdMatch && stdMatch[1]) {
      benchmarkStandard = stdMatch[1].trim();
    } else {
      const matchedPreset = dynamicPresets.find((p: any) =>
        p.coordinateType === 'BENCHMARK_STANDARD' && (
          lowerAll.includes(p.presetLabel?.toLowerCase()) ||
          (Array.isArray(p.synonyms) && p.synonyms.some((s: string) => lowerAll.includes(s.toLowerCase())))
        )
      );
      if (matchedPreset) benchmarkStandard = matchedPreset.presetLabel;
    }

    let bestDomain = sample?.domainCode && sample.domainCode !== 'GENERAL' ? sample.domainCode : '';
    let maxScore = 0;

    activeDomains.forEach((dom: any) => {
      let score = 0;
      const domCode = dom.code?.toLowerCase();
      const domName = (dom.label || dom.name || '').toLowerCase();
      const domShort = (dom.short || '').toLowerCase();

      if (domCode && lowerAll.includes(domCode)) score += 3;
      if (domName && lowerAll.includes(domName)) score += 4;
      if (domShort && domShort.length >= 3 && lowerAll.includes(domShort)) score += 2;

      dynamicPresets
        .filter((p: any) => p.domainCode === dom.code)
        .forEach((p: any) => {
          if (p.presetLabel && lowerAll.includes(p.presetLabel.toLowerCase())) score += 5;
          if (Array.isArray(p.synonyms)) {
            p.synonyms.forEach((s: string) => {
              if (s && lowerAll.includes(s.toLowerCase())) score += 3;
            });
          }
        });

      if (score > maxScore) {
        maxScore = score;
        bestDomain = dom.code;
      }
    });

    const finalDomain = bestDomain || (activeDomains.length > 0 ? activeDomains[0].code : 'GENERAL');

    if (finalDomain && finalDomain !== 'GENERAL') {
      sniffedTags.add(finalDomain.toLowerCase().replace(/_/g, '-'));
    }
    if (targetLevel) {
      sniffedTags.add(targetLevel.toLowerCase().replace(/[^a-z0-9à-ỹ\s_-]/g, '').trim().replace(/\s+/g, '-'));
    }
    if (benchmarkYear) {
      sniffedTags.add(`nam-${benchmarkYear}`);
    }

    docxPreviewList.forEach(q => {
      if (Array.isArray(q.tags)) {
        q.tags.forEach((t: string) => {
          if (t && t.trim()) sniffedTags.add(t.trim().replace(/^#/, ''));
        });
      }
    });

    return {
      domain: finalDomain,
      targetLevel,
      assessmentPurpose,
      issuingOrg,
      benchmarkYear,
      benchmarkStandard,
      tags: Array.from(sniffedTags).filter(Boolean),
    };
  };

  const handleAutoClassifyTaxonomyAndTags = () => {
    const globalSniffed = sniffCoordinatesFromDocxList();
    const updated = docxPreviewList.map(item => {
      const text = `${item.content || ''} ${item.options?.join(' ') || ''} ${item.topicCode || ''} ${item.contextContent || ''}`.toLowerCase();
      let domain = item.domainCode && item.domainCode !== 'GENERAL' ? item.domainCode : '';
      let tags = Array.isArray(item.tags) ? [...item.tags] : [];
      let targetLevel = item.targetLevel || '';
      let assessmentPurpose = item.assessmentPurpose || '';
      let issuingOrg = item.issuingOrg || '';
      let benchmarkYear = item.benchmarkYear || (globalSniffed.benchmarkYear ? parseInt(globalSniffed.benchmarkYear) : 2026);
      let benchmarkStandard = item.benchmarkStandard || '';

      const addTag = (t: string) => {
        const clean = t.toLowerCase().replace(/[^a-z0-9à-ỹ\s_-]/g, '').trim().replace(/\s+/g, '-');
        if (clean && !tags.includes(clean)) tags.push(clean);
      };

      globalSniffed.tags.forEach(t => addTag(t));

      if (!domain) {
        let itemMaxScore = 0;
        let itemBestDomain = '';
        activeDomains.forEach((dom: any) => {
          let score = 0;
          if (dom.code && text.includes(dom.code.toLowerCase())) score += 3;
          if ((dom.label || dom.name) && text.includes((dom.label || dom.name).toLowerCase())) score += 4;
          dynamicPresets
            .filter((p: any) => p.domainCode === dom.code)
            .forEach((p: any) => {
              if (p.presetLabel && text.includes(p.presetLabel.toLowerCase())) score += 5;
              if (Array.isArray(p.synonyms) && p.synonyms.some((s: string) => text.includes(s.toLowerCase()))) score += 3;
            });
          if (score > itemMaxScore) {
            itemMaxScore = score;
            itemBestDomain = dom.code;
          }
        });
        domain = itemBestDomain || globalSniffed.domain;
      }

      const activePresetsForDomain = dynamicPresets.filter((p: any) => !domain || p.domainCode === domain || p.domainCode === 'GENERAL');

      if (!targetLevel) {
        const matchedLevel = activePresetsForDomain.find((p: any) =>
          p.coordinateType === 'TARGET_LEVEL' && (
            text.includes(p.presetLabel?.toLowerCase()) ||
            (Array.isArray(p.synonyms) && p.synonyms.some((s: string) => text.includes(s.toLowerCase())))
          )
        );
        targetLevel = matchedLevel?.presetLabel || globalSniffed.targetLevel;
      }

      if (!assessmentPurpose) {
        const matchedPurpose = activePresetsForDomain.find((p: any) =>
          p.coordinateType === 'ASSESSMENT_PURPOSE' && (
            text.includes(p.presetLabel?.toLowerCase()) ||
            (Array.isArray(p.synonyms) && p.synonyms.some((s: string) => text.includes(s.toLowerCase())))
          )
        );
        assessmentPurpose = matchedPurpose?.presetLabel || globalSniffed.assessmentPurpose;
      }

      if (!benchmarkStandard) {
        const matchedStd = activePresetsForDomain.find((p: any) =>
          p.coordinateType === 'BENCHMARK_STANDARD' && (
            text.includes(p.presetLabel?.toLowerCase()) ||
            (Array.isArray(p.synonyms) && p.synonyms.some((s: string) => text.includes(s.toLowerCase())))
          )
        );
        benchmarkStandard = matchedStd?.presetLabel || globalSniffed.benchmarkStandard;
      }

      if (!issuingOrg) {
        const matchedOrg = activePresetsForDomain.find((p: any) =>
          p.coordinateType === 'ISSUING_ORG' && (
            text.includes(p.presetLabel?.toLowerCase()) ||
            (Array.isArray(p.synonyms) && p.synonyms.some((s: string) => text.includes(s.toLowerCase())))
          )
        );
        issuingOrg = matchedOrg?.presetLabel || globalSniffed.issuingOrg;
      }

      if (domain && domain !== 'GENERAL') addTag(domain);
      if (targetLevel) addTag(targetLevel);
      if (item.isCritical) addTag('diem-liet');

      return {
        ...item,
        domainCode: domain,
        tags,
        targetLevel,
        assessmentPurpose,
        issuingOrg,
        benchmarkYear,
        benchmarkStandard
      };
    });

    setDocxPreviewList(updated);
    showToast(`✨ Đã tự động nhận diện và gán Ngành nghề & Smart Tags cho ${updated.length} câu hỏi theo cấu hình DB!`);
  };

  const handleApplyBulkTaxonomy = (payload: BulkTaxonomyApplyPayload) => {
    const updated = docxPreviewList.map(item => {
      if (payload.onlyUnassigned && item.domainCode && item.domainCode !== 'GENERAL') {
        return item;
      }

      const mergedTags = Array.isArray(item.tags) ? [...item.tags] : [];
      payload.tags.forEach(t => {
        if (!mergedTags.includes(t)) mergedTags.push(t);
      });

      return {
        ...item,
        domainCode: payload.domainCode,
        targetLevel: payload.targetLevel.trim() || item.targetLevel,
        assessmentPurpose: payload.assessmentPurpose.trim() || item.assessmentPurpose,
        issuingOrg: payload.issuingOrg.trim() || item.issuingOrg,
        benchmarkYear: payload.benchmarkYear || item.benchmarkYear,
        benchmarkStandard: payload.benchmarkStandard.trim() || item.benchmarkStandard,
        tags: mergedTags,
      };
    });

    setDocxPreviewList(updated);
    if (payload.savedToDatabase) {
      showToast(`✨ Đã áp dụng và lưu Hệ Tọa Độ Tri Thức & Smart Tags vào Database cho ${updated.length} câu hỏi!`);
    } else {
      showToast(`Đã thiết lập Đa Ngành & Smart Tags cho ${updated.length} câu hỏi!`);
    }
  };

  const handleAddTagToQuestion = (idx: number, rawTag: string) => {
    const cleanTag = rawTag.trim().replace(/^#/, '');
    if (!cleanTag) return;
    const current = docxPreviewList[idx];
    const currentTags = Array.isArray(current.tags) ? [...current.tags] : [];
    if (!currentTags.includes(cleanTag)) {
      currentTags.push(cleanTag);
      updateItem(idx, 'tags', currentTags);
    }
    setTagInputPerQ(prev => ({ ...prev, [idx]: '' }));
  };

  const handleRemoveTagFromQuestion = (idx: number, tagToRemove: string) => {
    const current = docxPreviewList[idx];
    const currentTags = Array.isArray(current.tags) ? [...current.tags] : [];
    updateItem(idx, 'tags', currentTags.filter(t => t !== tagToRemove));
  };

  const toggleExpandCoords = (idx: number) => {
    setExpandedCoords(prev => ({ ...prev, [idx]: !prev[idx] }));
  };

  const toggleShowRawOptions = (idx: number) => {
    setShowRawOptions(prev => ({ ...prev, [idx]: !prev[idx] }));
  };

  const handleExportExcel = async (templateCode: string) => {
    try {
      setIsExportingExcel(true);
      setShowExportMenu(false);
      const title = processingFileName?.replace(/\.[^/.]+$/, '') || 'DeThi_AegisQuiz';
      await convertService.exportPreviewToExcel(docxPreviewList, {
        templateCode,
        examTitle: title,
        includeExplanation: true,
        includeImages: true,
      });
      showToast(`✅ Đã xuất bản bảng tính Excel thành công (${templateCode})!`);
    } catch (err: any) {
      showToast(`❌ Lỗi xuất bản Excel: ${err?.message || 'Vui lòng thử lại.'}`);
    } finally {
      setIsExportingExcel(false);
    }
  };

  const handleExportCustomTemplate = async (file: File) => {
    try {
      setIsExportingExcel(true);
      setShowExportMenu(false);
      const title = processingFileName?.replace(/\.[^/.]+$/, '') || 'DeThi_AegisQuiz';
      await convertService.exportPreviewToCustomTemplate(docxPreviewList, file, {
        examTitle: title,
        includeExplanation: true,
        includeImages: true,
      });
      showToast(`✅ Đã xuất bản bảng tính theo template '${file.name}' thành công!`);
    } catch (err: any) {
      showToast(`❌ Lỗi xuất bản Excel theo template: ${err?.message || 'Vui lòng kiểm tra lại file mẫu.'}`);
    } finally {
      setIsExportingExcel(false);
      if (customTemplateInputRef.current) customTemplateInputRef.current.value = '';
    }
  };

  const handleTranspileStandard = async (formatCode: string) => {
    try {
      setIsTranspilingStandard(true);
      setShowTranspileMenu(false);
      const title = processingFileName?.replace(/\.[^/.]+$/, '') || 'DeThi_AegisQuiz';
      await convertService.exportPreviewToStandard(docxPreviewList, formatCode, {
        examTitle: title,
        categoryName: title,
        includeExplanation: true,
        includeImages: true,
      });
      const label = formatCode === 'MOODLE_XML' ? 'Moodle XML' : (formatCode === 'QTI_PACKAGE' ? 'Canvas QTI Zip' : 'Moodle GIFT');
      showToast(`✅ Đã xuất bản chuẩn quốc tế (${label}) thành công! Sẵn sàng nhập vào LMS.`);
    } catch (err: any) {
      showToast(`❌ Lỗi xuất bản chuẩn quốc tế: ${err?.message || 'Vui lòng thử lại.'}`);
    } finally {
      setIsTranspilingStandard(false);
    }
  };

  const hasFormulaImages = docxPreviewList.some(q =>
    q.optionType === 'image' ||
    (q.options && q.options.some((o: string) => o?.includes('data:image/'))) ||
    q.content?.includes('data:image/')
  );

  const examGroups = Array.from(new Set(docxPreviewList.map(q => q.topicCode || 'GENERAL'))).filter(Boolean).sort();
  const hasMultipleExams = examGroups.length > 1;

  const displayedList = selectedExamFilter === 'ALL'
    ? docxPreviewList
    : docxPreviewList.filter(q => (q.topicCode || 'GENERAL') === selectedExamFilter);

  return {
    activeDomains,
    localTopics,
    setLocalTopics,
    bulkTopic,
    setBulkTopic,
    bulkDifficulty,
    setBulkDifficulty,
    bulkDomain,
    setBulkDomain,
    bulkTagInput,
    setBulkTagInput,
    showBulkTaxonomyModal,
    setShowBulkTaxonomyModal,
    showAdvancedBulkTools,
    setShowAdvancedBulkTools,
    showMathStudio,
    setShowMathStudio,
    showCigRules,
    setShowCigRules,
    mathContext,
    showCreateTopicModal,
    setShowCreateTopicModal,
    targetQuestionIdx,
    toastMessage,
    showToast,
    isCorrectingVN,
    rawOriginalList,
    showRawOptions,
    selectedExamFilter,
    setSelectedExamFilter,
    expandedCoords,
    tagInputPerQ,
    setTagInputPerQ,
    isExportingExcel,
    showExportMenu,
    setShowExportMenu,
    isTranspilingStandard,
    showTranspileMenu,
    setShowTranspileMenu,
    handleTranspileStandard,
    customTemplateInputRef,
    hasFormulaImages,
    hasMultipleExams,
    examGroups,
    displayedList,
    openCreateTopicModal,
    updateItem,
    handleOptionChange,
    handleDeleteItem,
    handleApplyAllCore,
    handleCorrectVietnamese,
    handleRevertRawText,
    handleAddBulkTag,
    handleAutoClassifyTopics,
    handleAutoClassifyTaxonomyAndTags,
    handleApplyBulkTaxonomy,
    handleAddTagToQuestion,
    handleRemoveTagFromQuestion,
    toggleExpandCoords,
    toggleShowRawOptions,
    handleExportExcel,
    handleExportCustomTemplate,
    sniffCoordinatesFromDocxList,
    detectedTopicInfo,
    showTopicDecisionBanner,
    isCreatingVirtualTopic,
    handleApplyDetectedTopic,
    handleSyncWithSelectedTopic,
    handleDismissTopicDecision,
  };
}
