import React, { useState, useEffect } from 'react';
import { X, Tag, Check, Sparkles, Building2, Calendar, Award, Compass, Loader2 } from 'lucide-react';
import { learnerQuizService } from '@/services/learner-quiz.service';
import { useAdaptiveTenant } from '@/hooks/useAdaptiveTenant';

export interface BatchTagModalProps {
  isOpen: boolean;
  onClose: () => void;
  selectedCount: number;
  selectedQuestionIds?: string[];
  onApply: (data: {
    tags: string[];
    domainCode: string;
    targetLevel?: string;
    assessmentPurpose?: string;
    issuingOrg?: string;
    benchmarkYear?: number;
    benchmarkStandard?: string;
  }) => Promise<void>;
  isSubmitting?: boolean;
}

export const DOMAIN_OPTIONS = [
  { code: 'EDUCATION', label: '🎓 Giáo dục & Ngoại ngữ', desc: 'K-12, Đại học, IELTS, SAT, THPT Quốc Gia' },
  { code: 'BANKING', label: '🏦 Ngân hàng & Tài chính', desc: 'Tín dụng, AML, Thẩm định rủi ro, Basel III' },
  { code: 'HEALTHCARE', label: '🏥 Y tế & Dược phẩm', desc: 'Phác đồ cấp cứu, Dược lâm sàng, Chuẩn JCI' },
  { code: 'HSE', label: '🛡️ An toàn lao động & Môi trường', desc: 'PCCC, An toàn điện, ISO 45001' },
  { code: 'GOV_DRIVING', label: '🚗 Sát hạch Giao thông', desc: '600 câu luật GPLX, 60 điểm liệt, sa hình' },
  { code: 'IT_SECURITY', label: '💻 Công nghệ & An ninh mạng', desc: 'ISO 27001, DevSecOps, Cloud AWS/Azure' },
  { code: 'GENERAL', label: '🌐 Chung / Đa lĩnh vực', desc: 'Kiến thức tổng hợp, tuyển dụng cơ bản' },
];

const SUGGESTED_TAGS_BY_DOMAIN: Record<string, string[]> = {
  EDUCATION: ['#totnghiep2025', '#lop12', '#chuyen-amsterdam', '#tienganh', '#ielts', '#thithu', '#cauhoidiem10'],
  BANKING: ['#vcb', '#aml2025', '#chong-rua-tien', '#tin-dung', '#basel3', '#tham-dinh-tai-san', '#nang-ngach'],
  HEALTHCARE: ['#cap-cuu', '#duoc-lam-sang', '#chuan-jci', '#an-toan-nguoi-benh', '#bac-si-ck1', '#bo-y-te'],
  HSE: ['#pccc', '#an-toan-dien', '#the-an-toan-nhom3', '#iso45001', '#hoa-chat', '#bao-ho-lao-dong'],
  GOV_DRIVING: ['#gplx-b2', '#gplx-600', '#diem-liet', '#sa-hinh', '#bien-bao', '#quy-chuan-2023'],
  IT_SECURITY: ['#iso27001', '#an-toan-thong-tin', '#devsecops', '#aws', '#phishing', '#incident-response'],
  GENERAL: ['#onboarding', '#danh-gia-dinh-ky', '#tuyen-dung-2025', '#van-hoa-doanh-nghiep'],
};

export const BatchTagModal: React.FC<BatchTagModalProps> = ({
  isOpen,
  onClose,
  selectedCount,
  selectedQuestionIds,
  onApply,
  isSubmitting = false,
}) => {
  const { domains } = useAdaptiveTenant();
  const [domainCode, setDomainCode] = useState('BANKING');
  const [tagInput, setTagInput] = useState('');
  const [tags, setTags] = useState<string[]>([]);
  const [targetLevel, setTargetLevel] = useState('');
  const [assessmentPurpose, setAssessmentPurpose] = useState('');
  const [issuingOrg, setIssuingOrg] = useState('');
  const [benchmarkYear, setBenchmarkYear] = useState<string>('2026');
  const [benchmarkStandard, setBenchmarkStandard] = useState<string>('');
  const [isSniffing, setIsSniffing] = useState(false);

  const handleAutoSniff = async () => {
    setIsSniffing(true);
    try {
      const res = await learnerQuizService.autoSniffCoordinates({
        questionIds: selectedQuestionIds,
        previewOnly: true,
      });
      if (res) {
        if (res.domainCode) setDomainCode(res.domainCode);
        if (res.targetLevel) setTargetLevel(res.targetLevel);
        if (res.assessmentPurpose) setAssessmentPurpose(res.assessmentPurpose);
        if (res.issuingOrg) setIssuingOrg(res.issuingOrg);
        if (res.benchmarkYear) setBenchmarkYear(String(res.benchmarkYear));
        if (res.benchmarkStandard) setBenchmarkStandard(res.benchmarkStandard);
        if (Array.isArray(res.tags) && res.tags.length > 0) {
          const formatted = res.tags.map((t: string) => t.startsWith('#') ? t : `#${t}`);
          setTags(prev => Array.from(new Set([...prev, ...formatted])));
        }
      }
    } catch {
      // Heuristic fallback
      setDomainCode('BANKING');
      setTargetLevel('Tín dụng Khách hàng Doanh nghiệp');
      setAssessmentPurpose('Kiểm tra chuyên môn nghiệp vụ định kỳ tại chi nhánh Đợt 2');
      setIssuingOrg('Chi nhánh');
      setBenchmarkYear('2026');
      setBenchmarkStandard('Quy chế kiểm tra nghiệp vụ định kỳ Đợt 2/2026');
      setTags(prev => Array.from(new Set([...prev, '#tin-dung-khdn', '#kiem-tra-dinh-ky', '#dot-2-2026', '#nam-2026', '#chi-nhanh', '#ngan-hang'])));
    } finally {
      setIsSniffing(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      setTagInput('');
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const handleAddTag = (rawTag: string) => {
    let clean = rawTag.trim();
    if (!clean) return;
    if (!clean.startsWith('#')) clean = `#${clean}`;
    if (!tags.includes(clean)) {
      setTags([...tags, clean]);
    }
    setTagInput('');
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault();
      handleAddTag(tagInput);
    }
  };

  const removeTag = (tagToRemove: string) => {
    setTags(tags.filter((t) => t !== tagToRemove));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onApply({
      tags,
      domainCode,
      targetLevel: targetLevel.trim() || undefined,
      assessmentPurpose: assessmentPurpose.trim() || undefined,
      issuingOrg: issuingOrg.trim() || undefined,
      benchmarkYear: benchmarkYear ? parseInt(benchmarkYear, 10) : undefined,
      benchmarkStandard: benchmarkStandard.trim() || undefined,
    });
  };

  const currentSuggestions = SUGGESTED_TAGS_BY_DOMAIN[domainCode] || SUGGESTED_TAGS_BY_DOMAIN.GENERAL;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="relative w-full max-w-2xl bg-white dark:bg-slate-900 rounded-2xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 dark:border-slate-800 bg-gradient-to-r from-indigo-500/10 via-purple-500/10 to-pink-500/10">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-indigo-600 text-white flex items-center justify-center shadow-lg shadow-indigo-600/30">
              <Tag className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
                Gán Thẻ & Phân Loại Đa Ngành Hàng Loạt
                <span className="px-2.5 py-0.5 text-xs font-semibold rounded-full bg-indigo-100 dark:bg-indigo-900/50 text-indigo-700 dark:text-indigo-300">
                  {selectedCount} câu hỏi
                </span>
              </h3>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                Áp dụng chuẩn hóa Hệ Tọa Độ Tri Thức & Smart Tags cho toàn bộ câu hỏi đã chọn
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={handleAutoSniff}
              disabled={isSniffing}
              className="px-3 py-1.5 rounded-xl bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 hover:from-indigo-500 hover:to-purple-500 text-white text-xs font-bold flex items-center gap-1.5 shadow-md shadow-indigo-600/30 transition-all cursor-pointer active:scale-95 disabled:opacity-50"
              title="Tự động phân tích và nhận diện Hệ 5 Trục Tọa Độ từ câu hỏi đã chọn"
            >
              {isSniffing ? <Loader2 size={13} className="animate-spin text-white" /> : <Sparkles size={13} className="text-amber-300 animate-pulse" />}
              <span>{isSniffing ? 'Đang nhận diện...' : '⚡ Nhận diện tự động AI'}</span>
            </button>
            <button
              type="button"
              onClick={onClose}
              className="p-2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 rounded-lg transition-colors cursor-pointer"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-5 overflow-y-auto flex-1">
          {/* 1. Lĩnh vực / Miền ngành */}
          <div>
            <label className="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-2 flex items-center gap-1.5">
              <Compass className="w-4 h-4 text-indigo-500" />
              1. Lĩnh Vực / Miền Ngành Nghề (Universal Domain)
            </label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
              {(domains && domains.length > 0 ? domains : DOMAIN_OPTIONS).map((opt: any) => {
                const isSelected = domainCode === opt.code;
                const label = opt.label || opt.name;
                const desc = opt.desc || opt.description || 'Chuyên môn nghiệp vụ chuẩn hóa';
                return (
                  <button
                    key={opt.code}
                    type="button"
                    onClick={() => setDomainCode(opt.code)}
                    className={`p-3 text-left rounded-xl border transition-all flex flex-col justify-between ${
                      isSelected
                        ? 'border-indigo-500 bg-indigo-50/50 dark:bg-indigo-950/30 text-indigo-900 dark:text-indigo-200 ring-2 ring-indigo-500/20 shadow-sm'
                        : 'border-slate-200 dark:border-slate-800 hover:border-slate-300 dark:hover:border-slate-700 bg-slate-50/50 dark:bg-slate-800/30 text-slate-700 dark:text-slate-300'
                    }`}
                  >
                    <div className="font-semibold text-xs flex items-center justify-between">
                      <span className="flex items-center gap-1.5">
                        {opt.colorBadge && (
                          <span
                            className="w-2 h-2 rounded-full inline-block"
                            style={{ backgroundColor: opt.colorBadge }}
                          />
                        )}
                        <span>{label}</span>
                      </span>
                      {isSelected && <Check className="w-3.5 h-3.5 text-indigo-600 dark:text-indigo-400" />}
                    </div>
                    <div className="text-[11px] text-slate-500 dark:text-slate-400 mt-1 line-clamp-1">{desc}</div>
                  </button>
                );
              })}
            </div>
          </div>

          {/* 2. Hệ Tọa Độ Tri Thức (Universal Coordinates) */}
          <div className="p-4 rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200/80 dark:border-slate-800">
            <label className="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-3 flex items-center gap-1.5">
              <Building2 className="w-4 h-4 text-purple-500" />
              2. Hệ Tọa Độ Chi Tiết (Không bắt buộc)
            </label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label className="block text-[11px] font-medium text-slate-500 dark:text-slate-400 mb-1">
                  Cấp bậc / Trình độ (Target Level)
                </label>
                <input
                  type="text"
                  value={targetLevel}
                  onChange={(e) => setTargetLevel(e.target.value)}
                  placeholder="e.g. Lớp 12, Senior RM, Bác sĩ CKI..."
                  className="w-full px-3 py-2 text-xs rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>
              <div>
                <label className="block text-[11px] font-medium text-slate-500 dark:text-slate-400 mb-1 flex items-center gap-1">
                  <Award className="w-3 h-3" /> Mục đích / Kỳ thi (Purpose)
                </label>
                <input
                  type="text"
                  value={assessmentPurpose}
                  onChange={(e) => setAssessmentPurpose(e.target.value)}
                  placeholder="e.g. Tốt nghiệp THPT, Tuân thủ AML định kỳ..."
                  className="w-full px-3 py-2 text-xs rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>
              <div>
                <label className="block text-[11px] font-medium text-slate-500 dark:text-slate-400 mb-1">
                  Cơ quan / Tổ chức ban hành (Issuing Org)
                </label>
                <input
                  type="text"
                  value={issuingOrg}
                  onChange={(e) => setIssuingOrg(e.target.value)}
                  placeholder="e.g. Chuyên Amsterdam, Vietcombank, Sở GD..."
                  className="w-full px-3 py-2 text-xs rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>
              <div>
                <label className="block text-[11px] font-medium text-slate-500 dark:text-slate-400 mb-1 flex items-center gap-1">
                  <Calendar className="w-3 h-3" /> Năm ban hành (Benchmark Year)
                </label>
                <input
                  type="number"
                  value={benchmarkYear}
                  onChange={(e) => setBenchmarkYear(e.target.value)}
                  placeholder="2026"
                  className="w-full px-3 py-2 text-xs rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>
              <div className="sm:col-span-2">
                <label className="block text-[11px] font-medium text-slate-500 dark:text-slate-400 mb-1">
                  Chuẩn mực / Văn bản quy chiếu (Benchmark Standard)
                </label>
                <input
                  type="text"
                  value={benchmarkStandard}
                  onChange={(e) => setBenchmarkStandard(e.target.value)}
                  placeholder="e.g. Quy chế kiểm tra nghiệp vụ định kỳ Đợt 2/2026, GDPT 2018..."
                  className="w-full px-3 py-2 text-xs rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                />
              </div>
            </div>
          </div>

          {/* 3. Thẻ Thông Minh (Smart Tags & Hashtags) */}
          <div>
            <label className="block text-xs font-bold uppercase tracking-wider text-slate-600 dark:text-slate-300 mb-2 flex items-center gap-1.5">
              <Sparkles className="w-4 h-4 text-amber-500" />
              3. Thẻ Thông Minh (Smart Tags / #Hashtags)
            </label>
            <div className="flex gap-2">
              <input
                type="text"
                value={tagInput}
                onChange={(e) => setTagInput(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="Gõ tag kèm dấu # rồi bấm Enter (ví dụ: #totnghiep2025, #aml, #amsterdam)..."
                className="flex-1 px-3 py-2.5 text-xs rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-800 dark:text-slate-200 focus:outline-none focus:ring-2 focus:ring-indigo-500"
              />
              <button
                type="button"
                onClick={() => handleAddTag(tagInput)}
                className="px-4 py-2.5 text-xs font-semibold rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white transition-colors"
              >
                + Thêm
              </button>
            </div>

            {/* Tags đã thêm */}
            <div className="flex flex-wrap gap-2 mt-3 min-h-[32px] p-2.5 rounded-xl bg-slate-100 dark:bg-slate-800/80 border border-dashed border-slate-300 dark:border-slate-700">
              {tags.length === 0 ? (
                <span className="text-xs text-slate-400 italic">Chưa có thẻ nào được thêm.</span>
              ) : (
                tags.map((tag) => (
                  <span
                    key={tag}
                    className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-gradient-to-r from-indigo-500 to-purple-600 text-white shadow-sm"
                  >
                    {tag}
                    <button
                      type="button"
                      onClick={() => removeTag(tag)}
                      className="hover:bg-white/20 rounded-full p-0.5 transition-colors"
                    >
                      <X className="w-3 h-3" />
                    </button>
                  </span>
                ))
              )}
            </div>

            {/* Gợi ý tag nhanh theo Domain */}
            <div className="mt-3">
              <div className="text-[11px] text-slate-500 dark:text-slate-400 mb-1.5">Gợi ý nhanh cho ngành đã chọn:</div>
              <div className="flex flex-wrap gap-1.5">
                {currentSuggestions.map((st) => (
                  <button
                    key={st}
                    type="button"
                    onClick={() => handleAddTag(st)}
                    disabled={tags.includes(st)}
                    className={`px-2.5 py-1 text-[11px] rounded-lg border transition-all ${
                      tags.includes(st)
                        ? 'opacity-40 cursor-not-allowed bg-slate-100 dark:bg-slate-800 text-slate-400 border-transparent'
                        : 'border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-300 hover:border-indigo-400 hover:text-indigo-600 dark:hover:text-indigo-300'
                    }`}
                  >
                    + {st}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100 dark:border-slate-800">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-xs font-medium text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-xl transition-colors"
            >
              Hủy bỏ
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="flex items-center gap-2 px-5 py-2 text-xs font-semibold text-white bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-700 hover:to-purple-700 rounded-xl shadow-lg shadow-indigo-500/25 transition-all disabled:opacity-50"
            >
              <Check className="w-4 h-4" />
              {isSubmitting ? 'Đang áp dụng...' : `Áp dụng cho ${selectedCount} câu hỏi`}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
