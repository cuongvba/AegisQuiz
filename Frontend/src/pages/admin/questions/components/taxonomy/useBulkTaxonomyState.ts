import { useState, useEffect, useMemo, useCallback } from 'react';
import { learnerQuizService } from '@/services/learner-quiz.service';

export const FALLBACK_DOMAINS = [
  { code: 'EDUCATION', label: 'Giáo dục & Ngoại ngữ', icon: 'GraduationCap', colorBadge: '#8b5cf6' },
  { code: 'BANKING', label: 'Ngân hàng & Tài chính', icon: 'Landmark', colorBadge: '#0284c7' },
  { code: 'HEALTHCARE', label: 'Y tế & Dược phẩm', icon: 'HeartPulse', colorBadge: '#e11d48' },
  { code: 'HSE', label: 'An toàn lao động & PCCC', icon: 'HardHat', colorBadge: '#ea580c' },
  { code: 'GOV_DRIVING', label: 'Sát hạch Giao thông', icon: 'Car', colorBadge: '#10b981' },
  { code: 'IT_SECURITY', label: 'An ninh mạng & CNTT', icon: 'ShieldCheck', colorBadge: '#6366f1' },
  { code: 'GENERAL', label: 'Chung / Đa lĩnh vực', icon: 'Layers', colorBadge: '#64748b' },
];

export interface UseBulkTaxonomyStateProps {
  isOpen: boolean;
  processingFileName?: string;
  docxPreviewList?: any[];
  availableDomains?: any[];
  initialDomain?: string;
  showToast: (msg: string) => void;
}

export function useBulkTaxonomyState({
  isOpen,
  processingFileName = '',
  docxPreviewList = [],
  availableDomains = [],
  initialDomain = 'EDUCATION',
  showToast,
}: UseBulkTaxonomyStateProps) {
  const [domainList, setDomainList] = useState<any[]>(() => {
    return availableDomains && availableDomains.length > 0 ? availableDomains : FALLBACK_DOMAINS;
  });

  const [dynamicPresets, setDynamicPresets] = useState<any[]>([]);
  const [domain, setDomain] = useState<string>(initialDomain || 'EDUCATION');
  const [targetLevel, setTargetLevel] = useState<string>('');
  const [assessmentPurpose, setAssessmentPurpose] = useState<string>('');
  const [issuingOrg, setIssuingOrg] = useState<string>('');
  const [benchmarkYear, setBenchmarkYear] = useState<string>(() => String(new Date().getFullYear()));
  const [benchmarkStandard, setBenchmarkStandard] = useState<string>('');
  const [tags, setTags] = useState<string[]>([]);
  const [onlyUnassigned, setOnlyUnassigned] = useState<boolean>(false);
  const [saveToDbAsPreset, setSaveToDbAsPreset] = useState<boolean>(true);
  const [isSaving, setIsSaving] = useState<boolean>(false);
  const [isSniffing, setIsSniffing] = useState<boolean>(false);
  const [showAddDomainModal, setShowAddDomainModal] = useState<boolean>(false);

  useEffect(() => {
    if (availableDomains && availableDomains.length > 0) {
      setDomainList(availableDomains);
    }
  }, [availableDomains]);

  // Nạp danh mục Presets từ DB của Tenant
  const reloadPresets = useCallback(async () => {
    try {
      const data = await learnerQuizService.getCoordinatePresets();
      if (Array.isArray(data)) {
        setDynamicPresets(data);
      }
    } catch {}
  }, []);

  useEffect(() => {
    if (isOpen) {
      reloadPresets();
    }
  }, [isOpen, reloadPresets]);

  // Nhận diện Hệ 5 Trục Tọa Độ Thông Minh (Unified Backend Cognitive Sniffing)
  const executeAutoSniff = useCallback(async () => {
    setIsSniffing(true);
    try {
      const headerSamples = docxPreviewList.slice(0, 15).map(q =>
        `${q.contextTitle || ''} ${q.contextContent || ''} ${q.content || ''}`
      );
      const sampleContents = docxPreviewList.slice(0, 15).map(q => q.content || '');

      const sniffRes = await learnerQuizService.sniffCoordinates({
        fileName: processingFileName,
        headerSamples,
        sampleContents,
      });

      if (sniffRes) {
        if (sniffRes.domainCode) setDomain(sniffRes.domainCode);
        if (sniffRes.targetLevel) setTargetLevel(sniffRes.targetLevel);
        if (sniffRes.assessmentPurpose) setAssessmentPurpose(sniffRes.assessmentPurpose);
        if (sniffRes.issuingOrg) setIssuingOrg(sniffRes.issuingOrg);
        if (sniffRes.benchmarkYear) setBenchmarkYear(String(sniffRes.benchmarkYear));
        if (sniffRes.benchmarkStandard) setBenchmarkStandard(sniffRes.benchmarkStandard);

        // Hợp nhất tags từ API và tags hiện tại trong file
        const mergedTags = new Set<string>(sniffRes.tags || []);
        docxPreviewList.slice(0, 30).forEach(q => {
          if (Array.isArray(q.tags)) {
            q.tags.forEach((t: string) => {
              const clean = (t || '').trim().replace(/^#/, '');
              if (clean && clean.length <= 30 && !clean.includes('data:image')) {
                mergedTags.add(clean);
              }
            });
          }
        });
        setTags(Array.from(mergedTags));
      }
    } catch {
      // Fallback cục bộ nếu mất mạng
      const yearMatch = (processingFileName || '').match(/\b(202[4-9]|203[0-5])\b/);
      if (yearMatch) setBenchmarkYear(yearMatch[1]);
    } finally {
      setIsSniffing(false);
    }
  }, [processingFileName, docxPreviewList]);

  // Tự động nhận diện khi mở modal lần đầu
  useEffect(() => {
    if (isOpen) {
      executeAutoSniff();
    }
  }, [isOpen, executeAutoSniff]);

  // Gợi ý tags động hoàn toàn từ Database Presets
  const suggestedTags = useMemo(() => {
    const list = new Set<string>();
    dynamicPresets
      .filter((p: any) => !domain || p.domainCode === domain || p.domainCode === 'GENERAL')
      .forEach((p: any) => {
        if (p.presetLabel) {
          const clean = p.presetLabel
            .toLowerCase()
            .replace(/[^a-z0-9à-ỹ\s_-]/g, '')
            .trim()
            .replace(/\s+/g, '-');
          if (clean && clean.length >= 2) list.add(clean);
        }
        if (p.synonymsJson) {
          try {
            const syns = JSON.parse(p.synonymsJson);
            if (Array.isArray(syns)) {
              syns.forEach((s: string) => {
                const c = s.trim().replace(/^#/, '');
                if (c) list.add(c);
              });
            }
          } catch {}
        }
      });
    return Array.from(list);
  }, [dynamicPresets, domain]);

  const handleAddTag = (rawTag: string) => {
    const clean = rawTag.trim().replace(/^#/, '');
    if (clean && !tags.includes(clean)) {
      setTags(prev => [...prev, clean]);
    }
  };

  const handleRemoveTag = (tagToRemove: string) => {
    setTags(prev => prev.filter(t => t !== tagToRemove));
  };

  const handleDomainCreated = (newDomain: any) => {
    const code = newDomain.code;
    setDomainList(prev => {
      const exists = prev.some(d => d.code === code);
      if (exists) {
        return prev.map(d => d.code === code ? { ...d, ...newDomain } : d);
      }
      return [...prev, newDomain];
    });
    setDomain(code);
    setShowAddDomainModal(false);
  };

  return {
    domainList,
    domain,
    setDomain,
    targetLevel,
    setTargetLevel,
    assessmentPurpose,
    setAssessmentPurpose,
    issuingOrg,
    setIssuingOrg,
    benchmarkYear,
    setBenchmarkYear,
    benchmarkStandard,
    setBenchmarkStandard,
    tags,
    handleAddTag,
    handleRemoveTag,
    suggestedTags,
    onlyUnassigned,
    setOnlyUnassigned,
    saveToDbAsPreset,
    setSaveToDbAsPreset,
    isSaving,
    setIsSaving,
    isSniffing,
    executeAutoSniff,
    showAddDomainModal,
    setShowAddDomainModal,
    handleDomainCreated,
    reloadPresets,
  };
}
