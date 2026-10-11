import React from 'react';
import { X, Check, Sparkles, Layers } from 'lucide-react';
import { DynamicDomainIcon } from '@/components/common/DynamicDomainIcon';
import { useMobileApp } from '../context/MobileAppContext';

export const MobileAppSwitcherModal: React.FC = () => {
  const { variant, setVariantId, allVariants, isSwitcherOpen, setIsSwitcherOpen, triggerHaptic } = useMobileApp();

  if (!isSwitcherOpen) return null;

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="mobile-app-switcher-title"
      className="fixed inset-0 z-50 flex flex-col justify-end bg-black/80 backdrop-blur-md animate-in fade-in duration-200"
    >
      <div
        className="w-full max-h-[85vh] overflow-y-auto rounded-t-3xl bg-slate-950 border-t border-slate-800 p-5 space-y-4 shadow-2xl animate-in slide-in-from-bottom duration-300"
        style={{ paddingBottom: 'calc(1.5rem + env(safe-area-inset-bottom, 16px))' }}
      >
        <div className="flex items-center justify-between border-b border-slate-800/80 pb-3">
          <div className="flex items-center gap-2">
            <div className="p-2 rounded-xl bg-purple-500/20 text-purple-400">
              <Sparkles size={18} />
            </div>
            <div>
              <h3 id="mobile-app-switcher-title" className="text-sm font-black text-white uppercase tracking-wider">
                Chọn Chuyên Ngành Khảo Thí
              </h3>
              <p className="text-[11px] text-slate-400">
                Giao diện & ngân hàng câu hỏi sẽ tự động biến hình tương ứng
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={() => setIsSwitcherOpen(false)}
            className="p-1.5 text-slate-400 hover:text-white rounded-xl bg-slate-900 border border-slate-800 cursor-pointer"
          >
            <X size={18} />
          </button>
        </div>

        <div className="space-y-2.5">
          {allVariants.map((item) => {
            const isSelected = variant.id === item.id;
            return (
              <button
                key={item.id}
                type="button"
                onClick={() => {
                  triggerHaptic(20);
                  setVariantId(item.id);
                }}
                className={`w-full text-left p-3.5 rounded-2xl border transition-all cursor-pointer flex items-center justify-between gap-3 ${
                  isSelected
                    ? 'bg-slate-900/90 border-slate-700 ring-2'
                    : 'bg-slate-950/60 border-slate-850 hover:bg-slate-900/60'
                }`}
                style={{
                  borderColor: isSelected ? item.colorScheme.primary : undefined,
                }}
              >
                <div className="flex items-center gap-3 min-w-0">
                  <div
                    className="p-3 rounded-2xl text-white shrink-0 shadow-lg"
                    style={{ backgroundColor: item.colorScheme.primary }}
                  >
                    <DynamicDomainIcon icon={item.icon} size={22} />
                  </div>
                  <div className="min-w-0">
                    <div className="flex items-center gap-1.5">
                      <span className="text-sm font-black text-white truncate">{item.name}</span>
                      <span className={`text-[9px] font-bold px-1.5 py-0.5 rounded-md border ${item.colorScheme.badgeBg} ${item.colorScheme.badgeText}`}>
                        {item.shortName}
                      </span>
                    </div>
                    <p className="text-[11px] text-slate-400 mt-0.5 line-clamp-1">{item.tagline}</p>
                  </div>
                </div>

                {isSelected ? (
                  <div
                    className="p-1 rounded-full text-slate-950 shrink-0"
                    style={{ backgroundColor: item.colorScheme.primary }}
                  >
                    <Check size={14} strokeWidth={3} />
                  </div>
                ) : (
                  <span className="text-xs text-slate-500 shrink-0">Chọn</span>
                )}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
};
