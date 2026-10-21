import React, { useState, useEffect } from 'react';
import { BookOpen, Save, RotateCw, Plus, Trash2, CheckCircle2, Sliders, Layers } from 'lucide-react';
import { parsingRulesService, type DocumentStructureDictionary, type InlineAnswerExtractSpec, type TopicTaxonomyEntry, type DomainTaxonomyEntry } from '@/services/parsing-rules.service';
import { InlineAnswerSpecsPanel } from './InlineAnswerSpecsPanel';
import { TaxonomyDictionaryPanel } from './TaxonomyDictionaryPanel';

export const DocumentStructureDictionaryTab: React.FC = () => {
  const [dict, setDict] = useState<DocumentStructureDictionary | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [activeCategory, setActiveCategory] = useState<'scope' | 'explanation' | 'section' | 'inline' | 'taxonomy'>('scope');

  const fetchDictionary = async () => {
    setLoading(true);
    try {
      const data = await parsingRulesService.getStructureDictionary();
      setDict(data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDictionary();
  }, []);

  const handleSave = async () => {
    if (!dict) return;
    setSaving(true);
    try {
      const res = await parsingRulesService.updateStructureDictionary(dict);
      setStatusMessage(`✨ ${res.message}`);
      setTimeout(() => setStatusMessage(null), 4000);
    } catch {
      setStatusMessage('❌ Không thể lưu từ điển cấu trúc.');
    } finally {
      setSaving(false);
    }
  };

  const handleAddScopeMarker = (qType: string, pattern: string) => {
    if (!dict || !pattern.trim()) return;
    const current = dict.scopeDirectiveMarkers?.[qType] || [];
    setDict({
      ...dict,
      scopeDirectiveMarkers: {
        ...dict.scopeDirectiveMarkers,
        [qType]: [...current, pattern.trim()]
      }
    });
  };

  const handleRemoveScopeMarker = (qType: string, index: number) => {
    if (!dict) return;
    const current = [...(dict.scopeDirectiveMarkers?.[qType] || [])];
    current.splice(index, 1);
    setDict({
      ...dict,
      scopeDirectiveMarkers: {
        ...dict.scopeDirectiveMarkers,
        [qType]: current
      }
    });
  };

  if (loading || !dict) {
    return (
      <div className="p-12 text-center text-slate-500 flex flex-col items-center gap-2">
        <RotateCw className="animate-spin text-purple-400" size={24} />
        <span className="text-xs">Đang tải Từ Điển Cấu Trúc Toàn Diện...</span>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {statusMessage && (
        <div className="bg-emerald-500/10 border border-emerald-500/30 rounded-2xl p-4 text-emerald-400 text-sm font-bold flex items-center gap-2">
          <CheckCircle2 size={18} />
          {statusMessage}
        </div>
      )}

      {/* Header */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-3xl p-6 backdrop-blur-xl flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <h3 className="text-base font-black text-white flex items-center gap-2">
            <BookOpen size={20} className="text-teal-400" />
            Document Structure Taxonomy Dictionary (Từ Điển Cấu Trúc CIG)
          </h3>
          <p className="text-xs text-slate-400 mt-1 max-w-2xl">
            Quản trị các mẫu chỉ thị dải câu hỏi (Scope Directives), từ khóa nhận dạng Lời giải / Biểu điểm chấm, và Tiêu đề phân đoạn.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={fetchDictionary}
            className="flex items-center gap-1.5 bg-slate-950 border border-slate-800 px-3 py-2 rounded-xl text-xs font-bold text-slate-400 hover:text-white transition-all cursor-pointer"
          >
            <RotateCw size={14} /> Làm mới
          </button>
          <button
            onClick={handleSave}
            disabled={saving}
            className="flex items-center gap-1.5 bg-gradient-to-r from-teal-600 to-emerald-600 hover:from-teal-500 hover:to-emerald-500 text-white px-4 py-2 rounded-xl text-xs font-bold shadow-lg shadow-teal-500/20 active:scale-95 transition-all cursor-pointer"
          >
            <Save size={14} />
            {saving ? 'Đang lưu...' : 'Lưu & Hot-Reload 0ms'}
          </button>
        </div>
      </div>

      {/* Navigation Sub-Tabs */}
      <div className="flex border-b border-slate-800 gap-4">
        <button
          onClick={() => setActiveCategory('scope')}
          className={`pb-3 text-xs font-black uppercase tracking-wider border-b-2 transition-all cursor-pointer ${
            activeCategory === 'scope' ? 'border-teal-500 text-teal-400' : 'border-transparent text-slate-500 hover:text-slate-300'
          }`}
        >
          1. Dải Chỉ Thị Câu Hỏi (Scope Directives)
        </button>
        <button
          onClick={() => setActiveCategory('explanation')}
          className={`pb-3 text-xs font-black uppercase tracking-wider border-b-2 transition-all cursor-pointer ${
            activeCategory === 'explanation' ? 'border-purple-500 text-purple-400' : 'border-transparent text-slate-500 hover:text-slate-300'
          }`}
        >
          2. Từ Khóa Lời Giải / Hoạt Họa (Explanation Markers)
        </button>
        <button
          onClick={() => setActiveCategory('section')}
          className={`pb-3 text-xs font-black uppercase tracking-wider border-b-2 transition-all cursor-pointer ${
            activeCategory === 'section' ? 'border-blue-500 text-blue-400' : 'border-transparent text-slate-500 hover:text-slate-300'
          }`}
        >
          3. Tiêu Đề Phân Đoạn (Section Markers)
        </button>
        <button
          onClick={() => setActiveCategory('inline')}
          className={`pb-3 text-xs font-black uppercase tracking-wider border-b-2 transition-all cursor-pointer ${
            activeCategory === 'inline' ? 'border-amber-500 text-amber-400' : 'border-transparent text-slate-500 hover:text-slate-300'
          }`}
        >
          4. Trích Xuất Inline & Marker Vi Mô (Inline Specs)
        </button>
        <button
          onClick={() => setActiveCategory('taxonomy')}
          className={`pb-3 text-xs font-black uppercase tracking-wider border-b-2 transition-all cursor-pointer ${
            activeCategory === 'taxonomy' ? 'border-cyan-500 text-cyan-400' : 'border-transparent text-slate-500 hover:text-slate-300'
          }`}
        >
          5. Từ Điển Môn Học & Miền Ngành (Taxonomy)
        </button>
      </div>

      {/* Content */}
      {activeCategory === 'scope' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {Object.entries(dict.scopeDirectiveMarkers || {}).map(([qType, patterns]) => (
            <div key={qType} className="bg-slate-900/40 border border-slate-800 rounded-2xl p-4 space-y-3">
              <div className="flex items-center justify-between border-b border-slate-850 pb-2">
                <span className="text-xs font-black text-amber-400 tracking-wider uppercase">{qType}</span>
                <span className="text-[10px] text-slate-500 font-mono">({patterns.length} mẫu)</span>
              </div>
              <div className="space-y-1.5 font-mono text-[11px] max-h-40 overflow-y-auto">
                {patterns.map((p, idx) => (
                  <div key={idx} className="flex items-center justify-between bg-slate-950/70 p-2 rounded-xl border border-slate-850 text-slate-300">
                    <span className="truncate max-w-[280px]" title={p}>{p}</span>
                    <button
                      onClick={() => handleRemoveScopeMarker(qType, idx)}
                      className="text-red-400 hover:text-red-300 p-1 cursor-pointer"
                    >
                      <Trash2 size={12} />
                    </button>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {activeCategory === 'explanation' && (
        <div className="bg-slate-900/40 border border-slate-800 rounded-2xl p-5 space-y-4">
          <h4 className="text-xs font-black text-purple-400 uppercase tracking-wider">
            Mẫu Nhận Diện Lời Giải & Hướng Dẫn Ý Giải (Tư Liệu Tạo Hoạt Họa / Video)
          </h4>
          <div className="space-y-2 font-mono text-xs">
            {dict.explanationMarkers?.map((marker, idx) => (
              <div key={idx} className="bg-slate-950 p-3 rounded-xl border border-slate-850 text-purple-300/90 flex items-center justify-between">
                <span className="break-all">{marker}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {activeCategory === 'section' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {Object.entries(dict.sectionMarkers || {}).map(([secKey, patterns]) => (
            <div key={secKey} className="bg-slate-900/40 border border-slate-800 rounded-2xl p-4 space-y-3">
              <div className="flex items-center justify-between border-b border-slate-850 pb-2">
                <span className="text-xs font-black text-blue-400 uppercase tracking-wider">{secKey}</span>
                <span className="text-[10px] text-slate-500 font-mono">({patterns.length} tiêu đề)</span>
              </div>
              <div className="space-y-1.5 font-mono text-[11px] max-h-40 overflow-y-auto">
                {patterns.map((p, idx) => (
                  <div key={idx} className="bg-slate-950/70 p-2 rounded-xl border border-slate-850 text-slate-300">
                    <span className="truncate block" title={p}>{p}</span>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {activeCategory === 'inline' && (
        <InlineAnswerSpecsPanel
          specs={dict.inlineAnswerSpecs}
          onChange={updated => setDict({ ...dict, inlineAnswerSpecs: updated })}
        />
      )}

      {activeCategory === 'taxonomy' && (
        <TaxonomyDictionaryPanel
          topics={dict.topicTaxonomyEntries}
          domains={dict.domainTaxonomyEntries}
          onUpdateTopics={updated => setDict({ ...dict, topicTaxonomyEntries: updated })}
          onUpdateDomains={updated => setDict({ ...dict, domainTaxonomyEntries: updated })}
        />
      )}
    </div>
  );
};
