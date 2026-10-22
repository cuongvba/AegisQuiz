import React from 'react';
import { useEntitlement } from '@/shared/hooks/useEntitlement';
import { FeatureBadge } from './FeatureBadge';
import { FEATURE_METADATA_CATALOG, type FeatureKeyType } from '@/types/entitlement';

export const PaywallModal: React.FC = () => {
  const { activePaywallFeature, closePaywall, plan } = useEntitlement();

  if (!activePaywallFeature) return null;

  const meta = FEATURE_METADATA_CATALOG[activePaywallFeature] || {
    key: activePaywallFeature,
    name: 'Tính năng Mở Rộng',
    description: 'Tính năng này yêu cầu gói thuê bao cao hơn để kích hoạt.',
    requiredPlan: 'Professional',
    badge: 'PRO',
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/70 backdrop-blur-sm animate-fade-in">
      <div 
        className="relative w-full max-w-lg overflow-hidden rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-2xl transition-all"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Glow Header */}
        <div className="relative p-6 pb-4 bg-gradient-to-br from-indigo-900/20 via-purple-900/10 to-transparent border-b border-slate-100 dark:border-slate-800">
          <button
            onClick={closePaywall}
            className="absolute top-4 right-4 p-2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 transition"
          >
            ✕
          </button>
          
          <div className="flex items-center gap-3">
            <span className="p-2.5 rounded-xl bg-indigo-500/10 text-indigo-500 dark:bg-indigo-400/10 dark:text-indigo-400 text-2xl">
              💎
            </span>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-xl font-bold text-slate-900 dark:text-white">
                  {meta.name}
                </h3>
                <FeatureBadge badge={meta.badge} />
              </div>
              <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
                Yêu cầu gói <span className="font-semibold text-indigo-600 dark:text-indigo-400">{meta.requiredPlan}</span> trở lên
              </p>
            </div>
          </div>
        </div>

        {/* Content Body */}
        <div className="p-6 space-y-4">
          <div className="p-4 rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200/60 dark:border-slate-700/60 text-sm text-slate-700 dark:text-slate-300 leading-relaxed">
            {meta.description}
          </div>

          <div className="space-y-2.5">
            <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">
              Lợi thế khi nâng cấp gói
            </h4>
            <ul className="text-sm space-y-2 text-slate-600 dark:text-slate-300">
              <li className="flex items-center gap-2">
                <span className="text-emerald-500 font-bold">✓</span>
                Mở khóa không giới hạn tính năng <strong>{meta.name}</strong>
              </li>
              <li className="flex items-center gap-2">
                <span className="text-emerald-500 font-bold">✓</span>
                Tăng hạn mức câu hỏi, thí sinh và dung lượng lưu trữ đám mây
              </li>
              <li className="flex items-center gap-2">
                <span className="text-emerald-500 font-bold">✓</span>
                Hỗ trợ kỹ thuật ưu tiên 24/7 và cam kết SLA 99.9%
              </li>
            </ul>
          </div>

          <div className="pt-2 text-xs text-slate-500 dark:text-slate-400 bg-amber-500/5 border border-amber-500/20 p-3 rounded-lg">
            Gói hiện tại của bạn: <span className="font-bold text-slate-800 dark:text-slate-200">{plan}</span>. Hãy nâng cấp để tận hưởng toàn bộ sức mạnh của Aegis.
          </div>
        </div>

        {/* Action Buttons */}
        <div className="p-6 pt-3 bg-slate-50/60 dark:bg-slate-900/60 border-t border-slate-100 dark:border-slate-800 flex gap-3 justify-end">
          <button
            type="button"
            onClick={closePaywall}
            className="px-4 py-2 text-sm font-medium text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-lg transition"
          >
            Để sau
          </button>
          <a
            href="mailto:sales@aegis.vn?subject=Đăng ký nâng cấp gói dịch vụ Aegis"
            target="_blank"
            rel="noopener noreferrer"
            className="px-5 py-2 text-sm font-bold text-white bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 rounded-lg shadow-md shadow-indigo-500/20 transition flex items-center gap-1.5"
          >
            <span>Nâng Cấp Gói Ngay</span>
            <span>→</span>
          </a>
        </div>
      </div>
    </div>
  );
};
