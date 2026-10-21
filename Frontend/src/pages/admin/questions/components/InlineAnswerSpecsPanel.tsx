import React, { useState } from 'react';
import { Plus, Trash2, CheckCircle2 } from 'lucide-react';
import type { InlineAnswerExtractSpec } from '@/services/parsing-rules.service';

interface InlineAnswerSpecsPanelProps {
  specs?: InlineAnswerExtractSpec;
  onChange: (updated: InlineAnswerExtractSpec) => void;
}

const SPEC_CATEGORIES = [
  { key: 'singleChoiceExtractionPatterns', title: '1. Trắc Nghiệm Đơn (SINGLE / MULTI)', desc: 'Nhận diện Chọn A-G, là đáp án, Choose, 故选...' },
  { key: 'numericValueExtractionPatterns', title: '2. Điền Số Ngắn (SHORT_ANSWER)', desc: 'Nhận diện Đáp số bằng, Kết quả là, Result, Value...' },
  { key: 'trueFalseExtractionPatterns', title: '3. Mệnh Đề Đúng / Sai (TRUE_FALSE)', desc: 'Nhận diện ý a đúng/sai, statement b is true/false...' },
  { key: 'subItemMarkerPatterns', title: '4. Nhãn Ý Con Vi Mô (SUB-ITEMS)', desc: 'Nhận diện nhãn ý a, b, c, d hoặc bước 1, 2, 3...' },
  { key: 'explanationStartPatterns', title: '5. Mở Đầu Lời Giải (EXPLANATION)', desc: 'Nhận diện Lời giải:, Hướng dẫn chấm:, Solution:, 详细解析...' },
  { key: 'questionNumberPatterns', title: '6. Số Thứ Tự Câu Hỏi (QUESTION NO)', desc: 'Trích xuất số câu: Câu 1, Question 1, Aufgabe 1, 第1题...' },
] as const;

export const InlineAnswerSpecsPanel: React.FC<InlineAnswerSpecsPanelProps> = ({
  specs = {},
  onChange,
}) => {
  const [activeGroup, setActiveGroup] = useState<typeof SPEC_CATEGORIES[number]['key']>('singleChoiceExtractionPatterns');
  const [newPattern, setNewPattern] = useState('');

  const currentPatterns = (specs[activeGroup] as string[]) || [];

  const handleAdd = () => {
    if (!newPattern.trim()) return;
    const updated = {
      ...specs,
      [activeGroup]: [...currentPatterns, newPattern.trim()],
    };
    onChange(updated);
    setNewPattern('');
  };

  const handleRemove = (idx: number) => {
    const list = [...currentPatterns];
    list.splice(idx, 1);
    const updated = {
      ...specs,
      [activeGroup]: list,
    };
    onChange(updated);
  };

  return (
    <div className="space-y-4">
      {/* Category selector pills */}
      <div className="grid grid-cols-2 md:grid-cols-3 gap-2">
        {SPEC_CATEGORIES.map(cat => (
          <button
            key={cat.key}
            type="button"
            onClick={() => setActiveGroup(cat.key)}
            className={`p-3 rounded-2xl border text-left transition-all cursor-pointer ${
              activeGroup === cat.key
                ? 'bg-purple-950/60 border-purple-500/60 text-purple-200 shadow-md shadow-purple-950/50'
                : 'bg-slate-900/40 border-slate-800 text-slate-400 hover:text-slate-200 hover:border-slate-700'
            }`}
          >
            <div className="text-xs font-black truncate">{cat.title}</div>
            <div className="text-[10px] text-slate-500 truncate mt-0.5">{cat.desc}</div>
            <div className="text-[9px] font-mono text-purple-400 mt-1">
              ({((specs[cat.key] as string[]) || []).length} biểu thức)
            </div>
          </button>
        ))}
      </div>

      {/* Pattern list for active category */}
      <div className="bg-slate-900/50 border border-slate-800 rounded-2xl p-4 space-y-3">
        <div className="flex items-center justify-between border-b border-slate-850 pb-2">
          <span className="text-xs font-black text-purple-300 uppercase tracking-wide">
            Danh sách biểu thức nhận thức CIG ({currentPatterns.length} mẫu)
          </span>
          <span className="text-[10px] text-slate-500 font-mono">
            Độ ưu tiên cao hơn từ điển mặc định
          </span>
        </div>

        {/* Thêm pattern mới */}
        <div className="flex gap-2">
          <input
            type="text"
            value={newPattern}
            onChange={e => setNewPattern(e.target.value)}
            onKeyDown={e => {
              if (e.key === 'Enter') {
                e.preventDefault();
                handleAdd();
              }
            }}
            placeholder="Nhập Regex pattern mới (ví dụ: (?i)(?:Chọn|Choose)\s*([A-D]))..."
            className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs font-mono text-slate-200 focus:outline-none focus:border-purple-500"
          />
          <button
            type="button"
            onClick={handleAdd}
            className="flex items-center gap-1 bg-purple-600 hover:bg-purple-500 text-white px-3 py-2 rounded-xl text-xs font-bold transition-all cursor-pointer"
          >
            <Plus size={14} /> Thêm mẫu
          </button>
        </div>

        {/* Danh sách pattern */}
        <div className="space-y-1.5 font-mono text-[11px] max-h-56 overflow-y-auto">
          {currentPatterns.map((p, pIdx) => (
            <div
              key={pIdx}
              className="flex items-center justify-between bg-slate-950/80 p-2.5 rounded-xl border border-slate-850 text-slate-300 hover:border-slate-800 transition-all"
            >
              <div className="flex items-center gap-2 overflow-hidden">
                <span className="text-purple-400 font-bold shrink-0">{pIdx + 1}.</span>
                <span className="truncate break-all" title={p}>{p}</span>
              </div>
              <button
                type="button"
                onClick={() => handleRemove(pIdx)}
                className="text-red-400 hover:text-red-300 p-1 cursor-pointer shrink-0 ml-2"
                title="Xóa mẫu này"
              >
                <Trash2 size={13} />
              </button>
            </div>
          ))}
          {currentPatterns.length === 0 && (
            <div className="text-center py-6 text-xs text-slate-500 italic">
              Chưa có biểu thức tùy biến nào. Hệ thống đang sử dụng bộ mẫu mặc định chuẩn quốc tế.
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
