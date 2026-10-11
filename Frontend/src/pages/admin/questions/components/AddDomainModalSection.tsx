import React, { useState } from 'react';
import { Sparkles, Layers, Check, Loader2, X, Plus } from 'lucide-react';
import { DynamicDomainIcon } from '@/components/common/DynamicDomainIcon';
import { learnerQuizService } from '@/services/learner-quiz.service';

export interface AddDomainModalSectionProps {
  onClose: () => void;
  onCreated: (domain: any) => void;
  showToast: (msg: string) => void;
  existingCodes?: string[];
}

export const SYSTEM_CATALOG_PRESETS = [
  { code: 'DRIVING', name: 'Sát hạch Lái xe Quốc gia', icon: 'Car', colorBadge: '#10b981' },
  { code: 'BANKING', name: 'Ngân hàng & Tài chính Enterprise', icon: 'Landmark', colorBadge: '#0284c7' },
  { code: 'HEALTHCARE', name: 'Y tế & Dược phẩm', icon: 'HeartPulse', colorBadge: '#e11d48' },
  { code: 'HSE', name: 'An toàn Lao động & PCCC', icon: 'HardHat', colorBadge: '#ea580c' },
  { code: 'IT_SECURITY', name: 'An ninh mạng & CNTT', icon: 'ShieldCheck', colorBadge: '#6366f1' },
  { code: 'EDUCATION', name: 'Khảo thí & Giáo dục Quốc gia', icon: 'GraduationCap', colorBadge: '#8b5cf6' },
  { code: 'GENERAL', name: 'Chuyên đề Tổng hợp', icon: 'Layers', colorBadge: '#64748b' },
];

export const AddDomainModalSection: React.FC<AddDomainModalSectionProps> = ({
  onClose,
  onCreated,
  showToast,
  existingCodes = [],
}) => {
  const [domainName, setDomainName] = useState<string>('');
  const [domainCode, setDomainCode] = useState<string>('');
  const [domainIcon, setDomainIcon] = useState<string>('Layers');
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  // Lọc các ngành chuẩn chưa có trong danh sách hiện tại của Tenant để gợi ý
  const availableQuickPicks = SYSTEM_CATALOG_PRESETS.filter(
    p => !existingCodes.some(c => c.toUpperCase() === p.code.toUpperCase())
  );

  const handleSelectQuickPick = (preset: typeof SYSTEM_CATALOG_PRESETS[0]) => {
    setDomainName(preset.name);
    setDomainCode(preset.code);
    setDomainIcon(preset.icon);
  };

  const handleSubmit = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (!domainName.trim() || !domainCode.trim()) {
      showToast('⚠️ Vui lòng nhập đầy đủ Tên ngành và Mã ngành.');
      return;
    }

    setIsSubmitting(true);
    try {
      const cleanCode = domainCode.trim().toUpperCase().replace(/[^A-Z0-9_]/g, '_');
      const res = await learnerQuizService.createDomain({
        code: cleanCode,
        name: domainName.trim(),
        icon: domainIcon.trim() || 'Layers',
        colorBadge: '#8b5cf6',
      });

      const newDomain = res?.domain || {
        code: cleanCode,
        name: domainName.trim(),
        label: domainName.trim(),
        icon: domainIcon.trim() || 'Layers',
        colorBadge: '#8b5cf6',
      };

      showToast(res?.message || `✨ Đã kích hoạt ngành: ${newDomain.name} (${newDomain.code})`);
      onCreated(newDomain);
    } catch (err: any) {
      showToast(`⚠️ Không thể lưu ngành: ${err?.response?.data?.message || err.message || 'Lỗi kết nối'}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="p-4 bg-slate-950/95 border border-sky-500/50 rounded-2xl space-y-3.5 shadow-2xl shadow-sky-950/40 animate-in fade-in zoom-in-95 duration-150">
      <div className="flex items-center justify-between border-b border-slate-800/80 pb-2.5">
        <div className="flex items-center gap-2">
          <div className="p-1.5 rounded-lg bg-sky-500/20 text-sky-400">
            <Sparkles size={15} />
          </div>
          <div>
            <h4 className="text-xs font-black text-sky-300 uppercase tracking-wide">
              Bổ Sung / Kích Hoạt Miền Ngành (100% OCP)
            </h4>
            <p className="text-[10px] text-slate-400">
              Kích hoạt từ Catalog hệ sinh thái hoặc tự tạo ngành đặc thù mới
            </p>
          </div>
        </div>
        <button
          type="button"
          onClick={onClose}
          className="text-slate-400 hover:text-white p-1 rounded-lg hover:bg-slate-800 transition-colors cursor-pointer"
        >
          <X size={15} />
        </button>
      </div>

      {/* Gợi ý nhanh 1-chạm từ Hệ Sinh Thái */}
      {availableQuickPicks.length > 0 && (
        <div className="space-y-1.5">
          <span className="text-[10px] font-bold text-slate-400 flex items-center gap-1">
            ⚡ Gợi ý ngành chuẩn hệ sinh thái (1-chạm điền ngay):
          </span>
          <div className="flex flex-wrap gap-1.5">
            {availableQuickPicks.map(p => (
              <button
                key={p.code}
                type="button"
                onClick={() => handleSelectQuickPick(p)}
                className={`px-2.5 py-1 rounded-lg border text-[11px] font-bold flex items-center gap-1.5 transition-all cursor-pointer ${
                  domainCode === p.code
                    ? 'bg-sky-500/20 border-sky-400 text-sky-200 ring-1 ring-sky-400/60'
                    : 'bg-slate-900 border-slate-750 text-slate-300 hover:border-slate-600 hover:text-white'
                }`}
              >
                <DynamicDomainIcon icon={p.icon} size={13} />
                <span>{p.name}</span>
                <span className="text-[9px] opacity-60">({p.code})</span>
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Form tùy biến chi tiết */}
      <form onSubmit={handleSubmit} className="space-y-2.5">
        <div className="grid grid-cols-1 sm:grid-cols-12 gap-2">
          <div className="sm:col-span-5 space-y-1">
            <label className="text-[10px] font-bold text-slate-400">Tên ngành hiển thị *</label>
            <input
              type="text"
              value={domainName}
              onChange={e => setDomainName(e.target.value)}
              placeholder="VD: Sát hạch Lái xe, Hàng không..."
              className="w-full bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-sky-500 placeholder-slate-500 font-medium"
            />
          </div>

          <div className="sm:col-span-4 space-y-1">
            <label className="text-[10px] font-bold text-slate-400">Mã định danh (Code) *</label>
            <input
              type="text"
              value={domainCode}
              onChange={e => setDomainCode(e.target.value.toUpperCase())}
              placeholder="DRIVING, AVIATION..."
              className="w-full bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-sky-500 placeholder-slate-500 font-mono font-bold uppercase"
            />
          </div>

          <div className="sm:col-span-3 space-y-1">
            <label className="text-[10px] font-bold text-slate-400">Biểu tượng (Icon)</label>
            <div className="flex items-center gap-1.5">
              <input
                type="text"
                value={domainIcon}
                onChange={e => setDomainIcon(e.target.value)}
                placeholder="Car, Layers..."
                className="w-full bg-slate-900 border border-slate-750 rounded-xl px-2.5 py-2 text-xs text-white focus:outline-none focus:border-sky-500 placeholder-slate-500 font-medium"
              />
              <div className="p-2 rounded-xl bg-slate-900 border border-slate-750 text-sky-400 shrink-0">
                <DynamicDomainIcon icon={domainIcon} size={16} />
              </div>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-end gap-2 pt-1">
          <button
            type="button"
            onClick={onClose}
            className="px-3.5 py-2 rounded-xl border border-slate-750 text-slate-400 hover:text-white text-xs font-bold transition-colors cursor-pointer"
          >
            Hủy
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="px-4 py-2 rounded-xl bg-gradient-to-r from-sky-600 to-blue-600 hover:from-sky-500 hover:to-blue-500 text-white text-xs font-black shadow-lg shadow-sky-600/30 transition-all cursor-pointer flex items-center gap-1.5 active:scale-95 disabled:opacity-50"
          >
            {isSubmitting ? (
              <>
                <Loader2 size={13} className="animate-spin" />
                <span>Đang lưu...</span>
              </>
            ) : (
              <>
                <Check size={14} />
                <span>Lưu & Kích Hoạt Ngành</span>
              </>
            )}
          </button>
        </div>
      </form>
    </div>
  );
};
