import React from 'react';
import { Eye, ToggleLeft, ToggleRight, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import type { BankTopic } from '../types';
import { getTopicFullPath } from '../types';

interface TopicTableSectionProps {
  topics: BankTopic[];
  pagedTopics: BankTopic[];
  filteredTopics: BankTopic[];
  tSearch: string;
  setTSearch: (s: string) => void;
  tPage: number;
  setTPage: (p: number) => void;
  tPageSize: number;
  tTotalPages: number;
  tSortField: string;
  setTSortField: (f: string) => void;
  tSortAsc: boolean;
  setTSortAsc: (a: boolean) => void;
  hasWritePermission: boolean;
  openEditTopicModal: (topic: BankTopic) => void;
  handleToggleActiveTopic: (topic: BankTopic) => void;
  handleDeleteTopic: (id: string) => void;
  renderSortIcon: (field: string, currentField: string, isAsc: boolean) => React.ReactNode;
  Pagination: React.ComponentType<{ page: number; totalPages: number; onChange: (p: number) => void }>;
}

export const TopicTableSection: React.FC<TopicTableSectionProps> = ({
  topics,
  pagedTopics,
  filteredTopics,
  tSearch,
  setTSearch,
  tPage,
  setTPage,
  tPageSize,
  tTotalPages,
  tSortField,
  setTSortField,
  tSortAsc,
  setTSortAsc,
  hasWritePermission,
  openEditTopicModal,
  handleToggleActiveTopic,
  handleDeleteTopic,
  renderSortIcon,
  Pagination,
}) => {
  const { t } = useTranslation('admin_questions');

  const handleSort = (field: string) => {
    if (tSortField === field) {
      setTSortAsc(!tSortAsc);
    } else {
      setTSortField(field);
      setTSortAsc(true);
    }
  };

  return (
    <div>
      {/* Search Bar */}
      <div className="p-6 border-b border-slate-850 bg-slate-900/10">
        <div className="space-y-1">
          <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">
            {t('filters.searchTopic')}
          </label>
          <input
            type="text"
            placeholder={t('filters.searchTopicPlaceholder')}
            value={tSearch}
            onChange={e => setTSearch(e.target.value)}
            className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
          />
        </div>
      </div>

      <div className="w-full overflow-x-auto">
        <table className="w-full text-left border-collapse min-w-[700px]">
          <thead>
            <tr className="border-b border-slate-850 text-slate-400 text-xs font-black tracking-wider uppercase bg-slate-900/20 select-none">
              <th className="p-4 w-40 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleSort('code')}>
                <div className="flex items-center">{t('table.topicCode')} {renderSortIcon('code', tSortField, tSortAsc)}</div>
              </th>
              <th className="p-4 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleSort('name')}>
                <div className="flex items-center">{t('table.topicName')} {renderSortIcon('name', tSortField, tSortAsc)}</div>
              </th>
              <th className="p-4"><div className="flex items-center text-slate-400">{t('table.path')}</div></th>
              <th className="p-4 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleSort('description')}>
                <div className="flex items-center">{t('table.description')} {renderSortIcon('description', tSortField, tSortAsc)}</div>
              </th>
              <th className="p-4 w-40 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleSort('visibilityScope')}>
                <div className="flex items-center">{t('table.visibilityScope')} {renderSortIcon('visibilityScope', tSortField, tSortAsc)}</div>
              </th>
              <th className="p-4 w-24 text-right sticky right-0 bg-slate-900 z-10 border-l border-slate-800 shadow-[-4px_0_8px_rgba(0,0,0,0.15)]">{t('table.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {pagedTopics.map((topicItem: BankTopic, idx: number) => (
              <tr key={topicItem.id || idx} className="border-b border-slate-850/60 hover:bg-slate-800/10 transition-colors">
                <td className="p-4 font-mono text-xs font-black text-blue-400 select-all">{topicItem.code}</td>
                <td className="p-4 font-bold text-slate-200 text-sm">{topicItem.name}</td>
                <td className="p-4 text-xs text-slate-300">{getTopicFullPath(topicItem, topics).replace(/ → /g, '/')}</td>
                <td className="p-4 text-xs text-slate-400 leading-relaxed max-w-xs truncate">{topicItem.description || '_'}</td>
                <td className="p-4">
                  <span className={`px-2 py-0.5 rounded text-[10px] font-black tracking-wider uppercase border ${
                    topicItem.visibilityScope === 'PUBLIC'
                      ? 'bg-green-500/10 text-green-400 border-green-500/20'
                      : 'bg-purple-500/10 text-purple-400 border-purple-500/20'
                  }`}>
                    {topicItem.visibilityScope}
                  </span>
                </td>
                <td className="p-4 text-right sticky right-0 bg-slate-900/95 z-10 border-l border-slate-850/60 shadow-[-4px_0_8px_rgba(0,0,0,0.15)]">
                  <div className="flex items-center justify-end gap-2">
                    <button
                      onClick={() => openEditTopicModal(topicItem)}
                      className="p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition-colors cursor-pointer"
                      title={t('tooltips.viewDetails')}
                    >
                      <Eye size={14} />
                    </button>
                    <button
                      onClick={() => handleToggleActiveTopic(topicItem)}
                      className={`p-2 rounded-lg transition-colors cursor-pointer ${
                        topicItem.enabled !== false
                          ? 'text-emerald-450 hover:text-emerald-300 hover:bg-emerald-950/20'
                          : 'text-slate-500 hover:text-slate-300 hover:bg-slate-850'
                      } ${!hasWritePermission ? 'opacity-50 cursor-not-allowed' : ''}`}
                      title={topicItem.enabled !== false ? t('tooltips.deactivate') : t('tooltips.activate')}
                    >
                      {topicItem.enabled !== false ? <ToggleRight size={18} /> : <ToggleLeft size={18} />}
                    </button>
                    {hasWritePermission && (
                      <button
                        onClick={() => handleDeleteTopic(topicItem.id)}
                        className="p-2 text-red-400 hover:text-red-300 hover:bg-red-950/20 rounded-lg transition-colors cursor-pointer"
                        title={t('tooltips.deleteTopic')}
                      >
                        <Trash2 size={14} />
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
            {pagedTopics.length === 0 && (
              <tr>
                <td colSpan={6} className="text-center py-20 text-slate-500 text-xs font-bold">
                  {t('table.noTopicsFound')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
        <div className="p-4 border-t border-slate-850 flex flex-col sm:flex-row items-center justify-between gap-4 bg-slate-900/10">
          <span className="text-xs text-slate-400 font-semibold">
            {t('table.paginationDisplay', {
              from: filteredTopics.length > 0 ? (tPage - 1) * tPageSize + 1 : 0,
              to: Math.min(tPage * tPageSize, filteredTopics.length),
              total: filteredTopics.length,
            })}
          </span>
          <Pagination page={tPage} totalPages={tTotalPages} onChange={setTPage} />
        </div>
      </div>
    </div>
  );
};
