import React, { useState, useEffect, useMemo, useCallback } from 'react';
import {
  X,
  Tag,
  Sparkles,
  Globe,
  Compass,
  Award,
  Building2,
  Calendar,
  Hash,
  CheckCheck,
  Plus,
  Save,
  Check,
  Loader2,
} from 'lucide-react';
import { DynamicDomainIcon } from '@/components/common/DynamicDomainIcon';
import { learnerQuizService } from '@/services/learner-quiz.service';
import { AddDomainModalSection } from './AddDomainModalSection';

export interface BulkTaxonomyApplyPayload {
  domainCode: string;
  targetLevel: string;
  assessmentPurpose: string;
  issuingOrg: string;
  benchmarkYear: number;
  benchmarkStandard: string;
  tags: string[];
  onlyUnassigned: boolean;
  savedToDatabase?: boolean;
}

export interface BulkTaxonomyModalProps {
  isOpen: boolean;
  onClose: () => void;
  onApply: (payload: BulkTaxonomyApplyPayload) => void;
  totalQuestions: number;
  processingFileName?: string;
  docxPreviewList?: any[];
  availableDomains?: any[];
  initialDomain?: string;
  showToast: (msg: string) => void;
}

export const FALLBACK_DOMAINS = [
  { code: 'EDUCATION', label: 'Giáo dục & Ngoại ngữ', icon: 'GraduationCap', colorBadge: '#8b5cf6' },
  { code: 'BANKING', label: 'Ngân hàng & Tài chính', icon: 'Landmark', colorBadge: '#0284c7' },
  { code: 'HEALTHCARE', label: 'Y tế & Dược phẩm', icon: 'HeartPulse', colorBadge: '#e11d48' },
  { code: 'HSE', label: 'An toàn lao động & PCCC', icon: 'HardHat', colorBadge: '#ea580c' },
  { code: 'GOV_DRIVING', label: 'Sát hạch Giao thông', icon: 'Car', colorBadge: '#10b981' },
  { code: 'IT_SECURITY', label: 'An ninh mạng & CNTT', icon: 'ShieldCheck', colorBadge: '#6366f1' },
  { code: 'GENERAL', label: 'Chung / Đa lĩnh vực', icon: 'Layers', colorBadge: '#64748b' },
];

export const BulkTaxonomyModal: React.FC<BulkTaxonomyModalProps> = ({
  isOpen,
  onClose,
  onApply,
  totalQuestions,
  processingFileName = '',
  docxPreviewList = [],
  availableDomains = [],
  initialDomain = 'EDUCATION',
  showToast,
}) => {
  // Danh sách domains động nạp từ DB hoặc fallback
  const [domainList, setDomainList] = useState<any[]>(() => {
    return availableDomains && availableDomains.length > 0 ? availableDomains : FALLBACK_DOMAINS;
  });

  // Tọa độ presets nạp động từ DB
  const [dynamicPresets, setDynamicPresets] = useState<any[]>([]);

  // Local Form States (Cô lập 100% để loại bỏ hiện tượng re-render giật lag)
  const [domain, setDomain] = useState<string>(initialDomain || 'EDUCATION');
  const [targetLevel, setTargetLevel] = useState<string>('');
  const [assessmentPurpose, setAssessmentPurpose] = useState<string>('');
  const [issuingOrg, setIssuingOrg] = useState<string>('');
  const [benchmarkYear, setBenchmarkYear] = useState<string>(() => String(new Date().getFullYear()));
  const [benchmarkStandard, setBenchmarkStandard] = useState<string>('');
  const [tags, setTags] = useState<string[]>([]);
  const [newTagInput, setNewTagInput] = useState<string>('');
  const [onlyUnassigned, setOnlyUnassigned] = useState<boolean>(false);
  const [saveToDbAsPreset, setSaveToDbAsPreset] = useState<boolean>(true);
  const [isSaving, setIsSaving] = useState<boolean>(false);

  // Modal phụ thêm ngành mới (OCP)
  const [showAddDomainModal, setShowAddDomainModal] = useState<boolean>(false);

  // Đồng bộ availableDomains khi prop thay đổi
  useEffect(() => {
    if (availableDomains && availableDomains.length > 0) {
      setDomainList(availableDomains);
    }
  }, [availableDomains]);

  // Nạp Tenant Coordinate Presets từ DB
  useEffect(() => {
    if (!isOpen) return;
    let isMounted = true;
    learnerQuizService
      .getCoordinatePresets()
      .then((data: any[]) => {
        if (isMounted && Array.isArray(data)) {
          setDynamicPresets(data);
        }
      })
      .catch(() => {});
    return () => {
      isMounted = false;
    };
  }, [isOpen]);

  /**
   * Thuật toán nhận diện Hệ 5 Trục Tọa Độ thông minh (Data-Driven + File Metadata)
   * Tuyệt đối không bắt rác công thức toán / Base64 / đơn vị đo lường
   */
  const executeAutoSniff = useCallback(() => {
    const fileNameLower = (processingFileName || '').toLowerCase();

    // 1. Quét văn bản tiêu đề / bối cảnh / đoạn đầu
    const headerSamples = docxPreviewList.slice(0, 15).map(q =>
      `${q.contextTitle || ''} ${q.contextContent || ''} ${q.content || ''}`
    ).join('\n');
    const fullSampleLower = `${fileNameLower}\n${headerSamples.toLowerCase()}`;

    let sniffedDomain = '';
    let sniffedLevel = '';
    let sniffedPurpose = '';
    let sniffedOrg = '';
    let sniffedYear = String(new Date().getFullYear());
    let sniffedStandard = '';
    const sniffedTags = new Set<string>();

    // 1. Nhận diện từ DB Presets trước (Ưu tiên số 1 theo OCP)
    if (dynamicPresets.length > 0) {
      for (const preset of dynamicPresets) {
        const label = preset.presetLabel?.toLowerCase() || '';
        const synonyms: string[] = Array.isArray(preset.synonyms) ? preset.synonyms : [];
        const isMatched = (label && fullSampleLower.includes(label)) ||
          synonyms.some(s => s && fullSampleLower.includes(s.toLowerCase()));

        if (isMatched) {
          if (preset.domainCode && preset.domainCode !== 'GENERAL' && !sniffedDomain) {
            sniffedDomain = preset.domainCode;
          }
          if (preset.coordinateType === 'TARGET_LEVEL' && !sniffedLevel) {
            sniffedLevel = preset.presetLabel;
          } else if (preset.coordinateType === 'ASSESSMENT_PURPOSE' && !sniffedPurpose) {
            sniffedPurpose = preset.presetLabel;
          } else if (preset.coordinateType === 'ISSUING_ORG' && !sniffedOrg) {
            sniffedOrg = preset.presetLabel;
          } else if (preset.coordinateType === 'BENCHMARK_STANDARD' && !sniffedStandard) {
            sniffedStandard = preset.presetLabel;
          }
        }
      }
    }

    // 2. Nhận diện từ Tên File & Ngữ cảnh Đề thi
    // Năm ban hành: tìm 2024, 2025, 2026...
    const yearMatch = fullSampleLower.match(/\b(202[4-9]|203[0-5])\b/);
    if (yearMatch) {
      sniffedYear = yearMatch[1];
      sniffedTags.add(`nam-${sniffedYear}`);
    }

    // Nhận diện Đề thi Giáo dục / THPT / Ngoại ngữ
    if (
      fileNameLower.includes('tnpt') ||
      fileNameLower.includes('thpt') ||
      fileNameLower.includes('toan') ||
      fileNameLower.includes('de_toan') ||
      fullSampleLower.includes('tốt nghiệp thpt') ||
      fullSampleLower.includes('lớp 12') ||
      fullSampleLower.includes('gdpt')
    ) {
      if (!sniffedDomain) sniffedDomain = 'EDUCATION';
      if (!sniffedLevel) sniffedLevel = 'Lớp 12 (THPT)';
      if (!sniffedPurpose) sniffedPurpose = 'Kỳ thi Tốt nghiệp THPT';
      if (!sniffedOrg) sniffedOrg = 'Bộ Giáo dục và Đào tạo';
      if (!sniffedStandard) sniffedStandard = 'Chương trình GDPT 2018';
      sniffedTags.add('tot-nghiep-thpt');
      sniffedTags.add('lop-12');
      sniffedTags.add('education');
      sniffedTags.add('gdpt-2018');
      if (fileNameLower.includes('toan')) sniffedTags.add('mon-toan');
    }
    // Nhận diện Ngân hàng / Tài chính
    else if (
      fileNameLower.includes('ngan_hang') ||
      fileNameLower.includes('tin_dung') ||
      fileNameLower.includes('aml') ||
      fullSampleLower.includes('chống rửa tiền') ||
      fullSampleLower.includes('thẩm định tín dụng') ||
      fullSampleLower.includes('khách hàng doanh nghiệp')
    ) {
      if (!sniffedDomain) sniffedDomain = 'BANKING';
      if (!sniffedLevel) sniffedLevel = 'Chuyên viên Tín dụng';
      if (!sniffedPurpose) sniffedPurpose = 'Kiểm tra nghiệp vụ định kỳ';
      if (!sniffedOrg) sniffedOrg = 'Ngân hàng Thương mại';
      if (!sniffedStandard) sniffedStandard = 'Thông tư 41 / Basel II';
      sniffedTags.add('banking');
      sniffedTags.add('tin-dung');
      sniffedTags.add('aml');
    }
    // Nhận diện Sát hạch Lái xe
    else if (
      fileNameLower.includes('gplx') ||
      fileNameLower.includes('lai_xe') ||
      fullSampleLower.includes('sát hạch lái xe') ||
      fullSampleLower.includes('giấy phép lái xe')
    ) {
      if (!sniffedDomain) sniffedDomain = 'GOV_DRIVING';
      if (!sniffedLevel) sniffedLevel = 'Hạng B2 / C';
      if (!sniffedPurpose) sniffedPurpose = 'Sát hạch Cấp GPLX Quốc gia';
      if (!sniffedOrg) sniffedOrg = 'Cục Đường bộ Việt Nam';
      if (!sniffedStandard) sniffedStandard = 'Quy chuẩn 600 câu hỏi 2023';
      sniffedTags.add('gplx');
      sniffedTags.add('lai-xe');
      sniffedTags.add('luat-giao-thong');
    }

    // 3. Quét các thẻ tags hiện có trong danh sách câu hỏi
    docxPreviewList.slice(0, 30).forEach(q => {
      if (Array.isArray(q.tags)) {
        q.tags.forEach((t: string) => {
          const clean = (t || '').trim().replace(/^#/, '');
          if (clean && clean.length <= 30 && !clean.includes('data:image')) {
            sniffedTags.add(clean);
          }
        });
      }
    });

    if (sniffedDomain) setDomain(sniffedDomain);
    if (sniffedLevel) setTargetLevel(sniffedLevel);
    if (sniffedPurpose) setAssessmentPurpose(sniffedPurpose);
    if (sniffedOrg) setIssuingOrg(sniffedOrg);
    if (sniffedYear) setBenchmarkYear(sniffedYear);
    if (sniffedStandard) setBenchmarkStandard(sniffedStandard);
    if (sniffedTags.size > 0) setTags(Array.from(sniffedTags));
  }, [processingFileName, docxPreviewList, dynamicPresets]);

  // Tự động nhận diện khi mở modal lần đầu
  useEffect(() => {
    if (isOpen) {
      executeAutoSniff();
    }
  }, [isOpen, executeAutoSniff]);

  // Sinh danh sách gợi ý Tags thông minh từ Presets của Domain đang chọn
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
      });

    // Thêm các tag mẫu chuẩn theo domain
    if (domain === 'EDUCATION') {
      ['tot-nghiep-thpt', 'lop-12', 'mon-toan', 'khao-thi-quoc-gia', 'gdpt-2018'].forEach(t => list.add(t));
    } else if (domain === 'BANKING') {
      ['tin-dung', 'aml-chong-rua-tien', 'kiem-tra-dinh-ky', 'ngan-hang', 'basel-ii'].forEach(t => list.add(t));
    } else if (domain === 'GOV_DRIVING') {
      ['gplx-b2', 'sat-hach-quoc-gia', '600-cau-hoi', 'diem-liet'].forEach(t => list.add(t));
    } else if (domain === 'HEALTHCARE') {
      ['y-te-duoc', 'phac-do-dieu-tri', 'bac-si-ck1', 'chuan-jci'].forEach(t => list.add(t));
    } else if (domain === 'HSE') {
      ['an-toan-lao-dong', 'pccc', 'iso-45001', 'the-an-toan'].forEach(t => list.add(t));
    }

    return Array.from(list);
  }, [dynamicPresets, domain]);

  const handleAddTag = (rawTag: string) => {
    const clean = rawTag.trim().replace(/^#/, '');
    if (clean && !tags.includes(clean)) {
      setTags(prev => [...prev, clean]);
    }
    setNewTagInput('');
  };

  const handleRemoveTag = (tagToRemove: string) => {
    setTags(prev => prev.filter(t => t !== tagToRemove));
  };

  // Xử lý sau khi tạo hoặc kích hoạt ngành mới thành công (OCP)
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

  // Áp dụng gán thẻ hàng loạt & Tự động lưu vào DB nếu chọn
  const handleApply = async () => {
    setIsSaving(true);
    try {
      // 1. Nếu người dùng chọn lưu vào DB để tái sử dụng
      if (saveToDbAsPreset && domain && domain !== 'GENERAL') {
        const presetsToSave = [
          { type: 'TARGET_LEVEL', val: targetLevel },
          { type: 'ASSESSMENT_PURPOSE', val: assessmentPurpose },
          { type: 'ISSUING_ORG', val: issuingOrg },
          { type: 'BENCHMARK_STANDARD', val: benchmarkStandard },
        ].filter(p => p.val && p.val.trim());

        for (const item of presetsToSave) {
          try {
            await learnerQuizService.createCoordinatePreset({
              domainCode: domain,
              coordinateType: item.type,
              presetLabel: item.val.trim(),
              synonymsJson: JSON.stringify(tags.slice(0, 5)),
              isDefault: false,
              displayOrder: 1,
            });
          } catch {}
        }
      }

      onApply({
        domainCode: domain,
        targetLevel: targetLevel.trim(),
        assessmentPurpose: assessmentPurpose.trim(),
        issuingOrg: issuingOrg.trim(),
        benchmarkYear: benchmarkYear ? parseInt(benchmarkYear) : new Date().getFullYear(),
        benchmarkStandard: benchmarkStandard.trim(),
        tags,
        onlyUnassigned,
        savedToDatabase: saveToDbAsPreset,
      });

      onClose();
    } catch (err: any) {
      showToast(`⚠️ Có lỗi khi áp dụng: ${err.message || 'Lỗi xử lý'}`);
    } finally {
      setIsSaving(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black/80 backdrop-blur-md z-[85] flex items-center justify-center p-3 sm:p-4">
      <div className="w-full max-w-3xl bg-slate-900 border border-purple-500/40 rounded-3xl p-5 sm:p-7 shadow-2xl relative text-left text-white animate-in fade-in zoom-in-95 duration-150 max-h-[92vh] overflow-y-auto">
        {/* Header Modal */}
        <div className="flex items-center justify-between pb-3 mb-4 border-b border-slate-800">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-gradient-to-br from-purple-500/20 to-blue-500/20 border border-purple-500/40 flex items-center justify-center text-purple-300">
              <Tag size={20} />
            </div>
            <div>
              <h3 className="text-base sm:text-lg font-black text-transparent bg-clip-text bg-gradient-to-r from-purple-300 via-indigo-300 to-blue-300">
                Gán Thẻ & Tọa Độ Đa Ngành Hàng Loạt
              </h3>
              <p className="text-[11px] text-slate-400">
                Cấu hình đồng loạt Hệ 5 Trục Tọa Độ & Thẻ thông minh cho <strong className="text-purple-300">{totalQuestions} câu hỏi</strong> đang duyệt
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={executeAutoSniff}
              className="px-3 py-1.5 rounded-xl bg-gradient-to-r from-purple-600 via-indigo-600 to-blue-600 hover:from-purple-500 hover:to-indigo-500 text-white text-xs font-bold flex items-center gap-1.5 shadow-lg shadow-purple-600/30 transition-all cursor-pointer active:scale-95 border border-purple-400/40"
              title="Tự động nhận diện 5 trục tọa độ & Thẻ thông minh từ tiêu đề và nội dung file"
            >
              <Sparkles size={13} className="text-amber-300 animate-pulse" />
              <span>⚡ Nhận diện lại bằng AI</span>
            </button>
            <button
              type="button"
              onClick={onClose}
              className="text-slate-400 hover:text-white p-1.5 rounded-lg hover:bg-slate-800 transition-colors cursor-pointer"
            >
              <X size={18} />
            </button>
          </div>
        </div>

        {/* Banner nhận diện thông minh */}
        {(targetLevel || assessmentPurpose || processingFileName) && (
          <div className="mb-4 p-3 bg-purple-950/40 border border-purple-500/40 rounded-2xl flex items-center justify-between text-xs text-purple-200">
            <div className="flex items-center gap-2 overflow-hidden">
              <Sparkles size={15} className="text-amber-300 shrink-0" />
              <span className="truncate">
                Nhận diện thông minh:{' '}
                <strong className="text-white">{targetLevel || processingFileName || 'Chưa định danh'}</strong>
                {assessmentPurpose ? ` • ${assessmentPurpose}` : ''} ({benchmarkYear})
              </span>
            </div>
            <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-purple-900/70 border border-purple-500/50 text-purple-300 shrink-0 ml-2">
              ✓ AI Verified
            </span>
          </div>
        )}

        <div className="space-y-4">
          {/* 1. Chọn Miền Ngành Nghề Đào Tạo (Domain) */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <label className="text-[11px] font-extrabold uppercase text-purple-400 flex items-center gap-1.5">
                <Globe size={13} /> 1. Miền Ngành Nghề Đào Tạo (Domain)
              </label>
              <button
                type="button"
                onClick={() => setShowAddDomainModal(true)}
                className="text-[10px] font-bold text-sky-400 hover:text-sky-300 flex items-center gap-1 cursor-pointer transition-colors"
              >
                <Plus size={11} /> Thêm ngành mới (OCP)
              </button>
            </div>

            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-2">
              {domainList.map(dom => {
                const isSelected = domain === dom.code;
                const domLabel = (dom.label || dom.name || dom.code).replace(/^.*? /, '');
                return (
                  <button
                    key={dom.code}
                    type="button"
                    onClick={() => setDomain(dom.code)}
                    className={`p-2.5 rounded-xl border text-xs font-bold flex items-center gap-2.5 transition-all cursor-pointer text-left ${
                      isSelected
                        ? 'bg-purple-950/80 border-purple-500 text-purple-200 shadow-md shadow-purple-950/60 ring-1 ring-purple-400/50'
                        : 'bg-slate-950/50 border-slate-800 text-slate-400 hover:text-slate-200 hover:border-slate-700'
                    }`}
                  >
                    <span className="shrink-0 text-purple-300">
                      <DynamicDomainIcon icon={dom.icon} size={16} />
                    </span>
                    <span className="truncate">{domLabel}</span>
                  </button>
                );
              })}
            </div>
          </div>

          {/* Form thêm / kích hoạt ngành mới thông minh (100% OCP) */}
          {showAddDomainModal && (
            <AddDomainModalSection
              onClose={() => setShowAddDomainModal(false)}
              onCreated={handleDomainCreated}
              showToast={showToast}
              existingCodes={domainList.map(d => d.code)}
            />
          )}

          {/* 2. Hệ Tọa Độ Tri Thức (Quy Chuẩn Định Danh) */}
          <div className="space-y-3 bg-slate-950/60 border border-slate-800/80 rounded-2xl p-4">
            <div className="text-[11px] font-extrabold uppercase text-slate-400 flex items-center gap-1.5">
              <Compass size={13} className="text-blue-400" /> 2. Hệ Tọa Độ Tri Thức (Quy chuẩn định danh)
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
              <div className="space-y-1">
                <label className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
                  <Award size={11} className="text-blue-400" /> Cấp bậc / Khối lớp
                </label>
                <input
                  type="text"
                  value={targetLevel}
                  onChange={e => setTargetLevel(e.target.value)}
                  placeholder="VD: Lớp 12, Senior RM, Bác sĩ CKI..."
                  className="w-full bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
                />
              </div>
              <div className="space-y-1">
                <label className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
                  🎯 Mục đích sát hạch / Đánh giá
                </label>
                <input
                  type="text"
                  value={assessmentPurpose}
                  onChange={e => setAssessmentPurpose(e.target.value)}
                  placeholder="VD: Thi tốt nghiệp THPT, Tuân thủ AML định kỳ..."
                  className="w-full bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
                />
              </div>
              <div className="space-y-1">
                <label className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
                  <Building2 size={11} className="text-emerald-400" /> Đơn vị / Cơ quan ban hành
                </label>
                <input
                  type="text"
                  value={issuingOrg}
                  onChange={e => setIssuingOrg(e.target.value)}
                  placeholder="VD: Bộ Giáo dục và Đào tạo, Vietcombank, Cục ĐBVN..."
                  className="w-full bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
                />
              </div>
              <div className="space-y-1">
                <label className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
                  <Calendar size={11} className="text-amber-400" /> Năm & Chuẩn mực quy chiếu
                </label>
                <div className="flex gap-2">
                  <input
                    type="number"
                    value={benchmarkYear}
                    onChange={e => setBenchmarkYear(e.target.value)}
                    placeholder="2026"
                    className="w-24 bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
                  />
                  <input
                    type="text"
                    value={benchmarkStandard}
                    onChange={e => setBenchmarkStandard(e.target.value)}
                    placeholder="Chương trình GDPT 2018 / Thông tư 41..."
                    className="flex-1 bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
                  />
                </div>
              </div>
            </div>
          </div>

          {/* 3. Thẻ Thông Minh (Smart Tags) */}
          <div className="space-y-2 bg-slate-950/60 border border-slate-800/80 rounded-2xl p-4">
            <div className="text-[11px] font-extrabold uppercase text-purple-400 flex items-center justify-between">
              <span className="flex items-center gap-1.5">
                <Hash size={13} /> 3. Danh Sách Thẻ Thông Minh (Smart Tags)
              </span>
              <span className="text-[10px] text-slate-500 font-normal">Gõ tag và nhấn Enter hoặc phẩy</span>
            </div>

            <div className="flex flex-wrap items-center gap-2 p-2.5 bg-slate-900 border border-slate-800 rounded-xl min-h-[44px]">
              {tags.map(tag => (
                <span
                  key={tag}
                  className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-purple-950/80 text-purple-300 border border-purple-700/60 shadow-sm"
                >
                  #{tag}
                  <button
                    type="button"
                    onClick={() => handleRemoveTag(tag)}
                    className="hover:text-red-400 cursor-pointer ml-1 text-xs"
                  >
                    ×
                  </button>
                </span>
              ))}
              <input
                type="text"
                value={newTagInput}
                onChange={e => setNewTagInput(e.target.value)}
                onKeyDown={e => {
                  if (e.key === 'Enter' || e.key === ',') {
                    e.preventDefault();
                    handleAddTag(newTagInput);
                  }
                }}
                placeholder="+ Nhập thẻ mới..."
                className="bg-transparent text-xs text-white placeholder:text-slate-500 focus:outline-none flex-1 min-w-[130px]"
              />
            </div>

            {/* Thẻ gợi ý động từ DB */}
            {suggestedTags.length > 0 && (
              <div className="space-y-1.5 pt-1">
                <div className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
                  <Sparkles size={11} className="text-amber-400" /> Thẻ gợi ý động từ Database ({domain}):
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {suggestedTags.map(tag => {
                    const isSelected = tags.includes(tag);
                    return (
                      <button
                        key={tag}
                        type="button"
                        onClick={() => {
                          if (isSelected) {
                            handleRemoveTag(tag);
                          } else {
                            handleAddTag(tag);
                          }
                        }}
                        className={`px-2.5 py-1 rounded-lg text-[10px] font-bold border transition-all cursor-pointer ${
                          isSelected
                            ? 'bg-purple-600 text-white border-purple-500 shadow-sm shadow-purple-600/30'
                            : 'bg-slate-900 text-slate-400 hover:text-slate-200 hover:border-slate-700 border-slate-800'
                        }`}
                      >
                        #{tag}
                      </button>
                    );
                  })}
                </div>
              </div>
            )}
          </div>

          {/* Tùy chọn phạm vi & Lưu vào DB tái sử dụng (100% OCP) */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
            <div className="p-3 bg-slate-950/60 border border-purple-500/20 rounded-2xl">
              <label className="flex items-center gap-2.5 cursor-pointer text-xs text-slate-200 select-none">
                <input
                  type="checkbox"
                  checked={onlyUnassigned}
                  onChange={e => setOnlyUnassigned(e.target.checked)}
                  className="rounded bg-slate-900 border-slate-750 text-purple-500 focus:ring-0 cursor-pointer"
                />
                <span className="font-medium text-slate-300">
                  Chỉ áp dụng cho các câu hỏi chưa được gán ngành riêng
                </span>
              </label>
            </div>

            <div className="p-3 bg-slate-950/60 border border-emerald-500/30 rounded-2xl">
              <label className="flex items-center gap-2.5 cursor-pointer text-xs text-slate-200 select-none">
                <input
                  type="checkbox"
                  checked={saveToDbAsPreset}
                  onChange={e => setSaveToDbAsPreset(e.target.checked)}
                  className="rounded bg-slate-900 border-slate-750 text-emerald-500 focus:ring-0 cursor-pointer"
                />
                <span className="font-bold text-emerald-300 flex items-center gap-1.5">
                  <Save size={12} /> Lưu vào Database để tự động nhận diện lần sau
                </span>
              </label>
            </div>
          </div>

          {/* Nút hành động */}
          <div className="flex items-center justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-750 text-slate-300 text-xs font-bold transition-colors cursor-pointer"
            >
              Hủy bỏ
            </button>
            <button
              type="button"
              onClick={handleApply}
              disabled={isSaving}
              className="px-6 py-2.5 rounded-xl bg-gradient-to-r from-purple-600 via-indigo-600 to-blue-600 hover:from-purple-500 hover:to-blue-500 text-white text-xs font-black transition-all shadow-lg shadow-purple-950/50 flex items-center gap-1.5 cursor-pointer active:scale-95 disabled:opacity-50"
            >
              {isSaving ? (
                <>
                  <Loader2 size={14} className="animate-spin" /> Đang lưu tri thức...
                </>
              ) : (
                <>
                  <CheckCheck size={14} /> Áp dụng cho {totalQuestions} câu hỏi
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default BulkTaxonomyModal;
