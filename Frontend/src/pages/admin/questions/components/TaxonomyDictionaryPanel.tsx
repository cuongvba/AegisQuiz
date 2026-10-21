import React, { useState } from 'react';
import { Plus, Trash2, Tag, Compass, Layers } from 'lucide-react';
import type { TopicTaxonomyEntry, DomainTaxonomyEntry } from '@/services/parsing-rules.service';

interface TaxonomyDictionaryPanelProps {
  topics?: TopicTaxonomyEntry[];
  domains?: DomainTaxonomyEntry[];
  onUpdateTopics: (updated: TopicTaxonomyEntry[]) => void;
  onUpdateDomains: (updated: DomainTaxonomyEntry[]) => void;
}

export const TaxonomyDictionaryPanel: React.FC<TaxonomyDictionaryPanelProps> = ({
  topics = [],
  domains = [],
  onUpdateTopics,
  onUpdateDomains,
}) => {
  const [subTab, setSubTab] = useState<'topics' | 'domains'>('topics');
  const [newTopicCode, setNewTopicCode] = useState('');
  const [newTopicName, setNewTopicName] = useState('');
  const [newTopicDomain, setNewTopicDomain] = useState('EDUCATION');
  const [newTopicKeyword, setNewTopicKeyword] = useState<Record<number, string>>({});

  const handleAddTopic = () => {
    if (!newTopicCode.trim() || !newTopicName.trim()) return;
    const entry: TopicTaxonomyEntry = {
      topicCode: newTopicCode.trim().toUpperCase(),
      topicName: newTopicName.trim(),
      domainCode: newTopicDomain.trim().toUpperCase(),
      keywords: [newTopicName.trim().toLowerCase()],
    };
    onUpdateTopics([...topics, entry]);
    setNewTopicCode('');
    setNewTopicName('');
  };

  const handleRemoveTopic = (index: number) => {
    const list = [...topics];
    list.splice(index, 1);
    onUpdateTopics(list);
  };

  const handleAddKeywordToTopic = (tIdx: number) => {
    const kw = (newTopicKeyword[tIdx] || '').trim();
    if (!kw) return;
    const list = [...topics];
    const currentKws = list[tIdx].keywords || [];
    if (!currentKws.includes(kw.toLowerCase())) {
      list[tIdx] = { ...list[tIdx], keywords: [...currentKws, kw.toLowerCase()] };
      onUpdateTopics(list);
    }
    setNewTopicKeyword({ ...newTopicKeyword, [tIdx]: '' });
  };

  const handleRemoveKeywordFromTopic = (tIdx: number, kwIdx: number) => {
    const list = [...topics];
    const currentKws = [...(list[tIdx].keywords || [])];
    currentKws.splice(kwIdx, 1);
    list[tIdx] = { ...list[tIdx], keywords: currentKws };
    onUpdateTopics(list);
  };

  return (
    <div className="space-y-4">
      {/* Sub Tabs */}
      <div className="flex border-b border-slate-800 gap-4">
        <button
          type="button"
          onClick={() => setSubTab('topics')}
          className={`pb-2.5 text-xs font-black uppercase tracking-wider border-b-2 transition-all cursor-pointer flex items-center gap-1.5 ${
            subTab === 'topics' ? 'border-purple-500 text-purple-400' : 'border-transparent text-slate-500 hover:text-slate-300'
          }`}
        >
          <Layers size={14} /> 1. Từ Điển Môn Học / Chủ Đề ({topics.length} môn)
        </button>
        <button
          type="button"
          onClick={() => setSubTab('domains')}
          className={`pb-2.5 text-xs font-black uppercase tracking-wider border-b-2 transition-all cursor-pointer flex items-center gap-1.5 ${
            subTab === 'domains' ? 'border-cyan-500 text-cyan-400' : 'border-transparent text-slate-500 hover:text-slate-300'
          }`}
        >
          <Compass size={14} /> 2. Miền Ngành / Trục Tri Thức ({domains.length} miền)
        </button>
      </div>

      {subTab === 'topics' && (
        <div className="space-y-4">
          {/* Form thêm Môn học mới */}
          <div className="bg-slate-900/60 border border-slate-800 rounded-2xl p-4 flex flex-wrap items-center gap-2">
            <input
              type="text"
              placeholder="Mã môn (VD: TRIET_HOC)..."
              value={newTopicCode}
              onChange={e => setNewTopicCode(e.target.value)}
              className="bg-slate-950 border border-slate-800 rounded-xl px-3 py-1.5 text-xs font-mono text-purple-300 focus:outline-none focus:border-purple-500 w-36 uppercase"
            />
            <input
              type="text"
              placeholder="Tên môn hiển thị (VD: Triết học)..."
              value={newTopicName}
              onChange={e => setNewTopicName(e.target.value)}
              className="bg-slate-950 border border-slate-800 rounded-xl px-3 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-purple-500 flex-1 min-w-[180px]"
            />
            <input
              type="text"
              placeholder="Miền ngành (VD: EDUCATION)..."
              value={newTopicDomain}
              onChange={e => setNewTopicDomain(e.target.value)}
              className="bg-slate-950 border border-slate-800 rounded-xl px-3 py-1.5 text-xs font-mono text-cyan-300 focus:outline-none focus:border-cyan-500 w-32 uppercase"
            />
            <button
              type="button"
              onClick={handleAddTopic}
              className="flex items-center gap-1 bg-purple-600 hover:bg-purple-500 text-white px-3.5 py-1.5 rounded-xl text-xs font-bold transition-all cursor-pointer"
            >
              <Plus size={14} /> Thêm môn học
            </button>
          </div>

          {/* Danh sách Môn học & Từ khóa */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3 max-h-96 overflow-y-auto">
            {topics.map((t, tIdx) => (
              <div
                key={t.topicCode || tIdx}
                className="bg-slate-950/70 border border-slate-800 rounded-2xl p-3.5 space-y-2.5 hover:border-slate-700 transition-all"
              >
                <div className="flex items-center justify-between border-b border-slate-850 pb-2">
                  <div className="flex items-center gap-2">
                    <span className="text-xs font-black text-purple-300 font-mono">{t.topicCode}</span>
                    <span className="text-xs font-bold text-slate-200">({t.topicName})</span>
                    <span className="text-[9px] bg-cyan-950 text-cyan-400 px-1.5 py-0.2 rounded border border-cyan-800/40 font-mono">
                      {t.domainCode}
                    </span>
                  </div>
                  <button
                    type="button"
                    onClick={() => handleRemoveTopic(tIdx)}
                    className="text-red-400 hover:text-red-300 p-1 cursor-pointer"
                    title="Xóa môn học này"
                  >
                    <Trash2 size={13} />
                  </button>
                </div>

                {/* Từ khóa nhận dạng */}
                <div className="space-y-1.5">
                  <div className="flex items-center justify-between text-[10px] text-slate-400 font-bold">
                    <span className="flex items-center gap-1"><Tag size={10} /> Từ khóa nhận thức ({t.keywords?.length || 0}):</span>
                  </div>
                  <div className="flex flex-wrap gap-1">
                    {t.keywords?.map((kw, kwIdx) => (
                      <span
                        key={kwIdx}
                        className="inline-flex items-center gap-1 px-2 py-0.5 rounded-lg text-[10px] font-medium bg-slate-900 border border-slate-800 text-slate-300"
                      >
                        {kw}
                        <button
                          type="button"
                          onClick={() => handleRemoveKeywordFromTopic(tIdx, kwIdx)}
                          className="hover:text-red-400 cursor-pointer ml-0.5"
                        >
                          ×
                        </button>
                      </span>
                    ))}
                  </div>

                  {/* Input thêm từ khóa mới */}
                  <div className="flex gap-1 pt-1">
                    <input
                      type="text"
                      placeholder="+ Thêm từ khóa..."
                      value={newTopicKeyword[tIdx] || ''}
                      onChange={e => setNewTopicKeyword({ ...newTopicKeyword, [tIdx]: e.target.value })}
                      onKeyDown={e => {
                        if (e.key === 'Enter') {
                          e.preventDefault();
                          handleAddKeywordToTopic(tIdx);
                        }
                      }}
                      className="bg-slate-900 border border-slate-800 rounded-lg px-2 py-0.5 text-[11px] text-slate-300 flex-1 focus:outline-none focus:border-purple-500"
                    />
                    <button
                      type="button"
                      onClick={() => handleAddKeywordToTopic(tIdx)}
                      className="px-2 py-0.5 bg-slate-800 hover:bg-slate-700 text-slate-200 rounded-lg text-[10px] font-bold cursor-pointer"
                    >
                      Thêm
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {subTab === 'domains' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3 max-h-96 overflow-y-auto">
          {domains.map((d, dIdx) => (
            <div
              key={d.domainCode || dIdx}
              className="bg-slate-950/70 border border-slate-800 rounded-2xl p-4 space-y-2.5"
            >
              <div className="flex items-center justify-between border-b border-slate-850 pb-2">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-black text-cyan-300 font-mono">{d.domainCode}</span>
                  <span className="text-xs font-bold text-slate-200">({d.domainName})</span>
                </div>
                <span className="text-[10px] font-mono bg-purple-950 text-purple-300 px-2 py-0.5 rounded border border-purple-800/40">
                  Trọng số: x{d.weight}
                </span>
              </div>
              <div className="text-[11px] text-slate-400">
                <span className="font-bold text-slate-300">Từ khóa chuyên môn ({d.keywords?.length || 0}): </span>
                <span className="font-mono text-[10px] text-slate-400">
                  {d.keywords?.join(', ')}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
