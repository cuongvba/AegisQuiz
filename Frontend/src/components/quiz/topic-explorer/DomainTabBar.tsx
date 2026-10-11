import React from 'react';
import type { CognitiveDomainGroupDto } from '@/types/quiz';

interface DomainTabBarProps {
  domainGroups: CognitiveDomainGroupDto[];
  selectedDomain: string;
  onSelectDomain: (domainCode: string) => void;
  isDomainLocked?: boolean;
}

const DOMAIN_ICONS: Record<string, string> = {
  BANKING: '🏦',
  DRIVING: '🚗',
  GOV_DRIVING: '🚗',
  EDUCATION: '🎓',
  IT: '💻',
  HEALTHCARE: '🏥',
  GENERAL: '🌐',
};

export const DomainTabBar: React.FC<DomainTabBarProps> = ({
  domainGroups,
  selectedDomain,
  onSelectDomain,
  isDomainLocked = false,
}) => {
  if (isDomainLocked) {
    return null; // Khi đã bị khóa vào một vertical app cụ thể, ẩn thanh tab để chống phân tán nhận thức
  }

  return (
    <div className="flex items-center gap-1.5 overflow-x-auto pb-1 scrollbar-none">
      <button
        type="button"
        onClick={() => onSelectDomain('ALL')}
        className={`px-3 py-1.5 rounded-xl text-xs font-bold transition-all flex items-center gap-1.5 whitespace-nowrap cursor-pointer ${
          selectedDomain === 'ALL'
            ? 'bg-blue-600 text-white shadow-md shadow-blue-600/30'
            : 'bg-slate-800/80 text-slate-300 hover:bg-slate-700 hover:text-white'
        }`}
      >
        <span>🌐</span>
        <span>Tất Cả Lĩnh Vực</span>
      </button>

      {domainGroups.map((group) => {
        const isSelected = selectedDomain.toUpperCase() === group.domainCode.toUpperCase();
        const icon = DOMAIN_ICONS[group.domainCode.toUpperCase()] || '📁';

        return (
          <button
            key={group.domainCode}
            type="button"
            onClick={() => onSelectDomain(group.domainCode)}
            className={`px-3 py-1.5 rounded-xl text-xs font-bold transition-all flex items-center gap-1.5 whitespace-nowrap cursor-pointer ${
              isSelected
                ? 'bg-blue-600 text-white shadow-md shadow-blue-600/30'
                : 'bg-slate-800/80 text-slate-300 hover:bg-slate-700 hover:text-white'
            }`}
          >
            <span>{icon}</span>
            <span>{group.domainName || group.domainCode}</span>
            <span className="text-[10px] px-1.5 py-0.2 rounded-full bg-slate-900/60 font-normal">
              {group.totalQuestions ?? 0}
            </span>
          </button>
        );
      })}
    </div>
  );
};
