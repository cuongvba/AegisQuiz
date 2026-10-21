import React from 'react';
import { Globe, Plus } from 'lucide-react';
import { DynamicDomainIcon } from '@/components/common/DynamicDomainIcon';

export interface TaxonomyDomainSelectorProps {
  domain: string;
  domainList: any[];
  onSelectDomain: (domainCode: string) => void;
  onOpenAddDomain: () => void;
}

export const TaxonomyDomainSelector: React.FC<TaxonomyDomainSelectorProps> = ({
  domain,
  domainList,
  onSelectDomain,
  onOpenAddDomain,
}) => {
  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <label className="text-[11px] font-extrabold uppercase text-purple-400 flex items-center gap-1.5">
          <Globe size={13} /> 1. Miền Ngành Nghề Đào Tạo (Domain)
        </label>
        <button
          type="button"
          onClick={onOpenAddDomain}
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
              onClick={() => onSelectDomain(dom.code)}
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
  );
};
