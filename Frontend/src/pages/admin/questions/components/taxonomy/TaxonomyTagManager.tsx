import React, { useState } from 'react';
import { Hash, Sparkles } from 'lucide-react';

export interface TaxonomyTagManagerProps {
  tags: string[];
  suggestedTags: string[];
  domain: string;
  onAddTag: (tag: string) => void;
  onRemoveTag: (tag: string) => void;
}

export const TaxonomyTagManager: React.FC<TaxonomyTagManagerProps> = ({
  tags,
  suggestedTags,
  domain,
  onAddTag,
  onRemoveTag,
}) => {
  const [newTagInput, setNewTagInput] = useState<string>('');

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' || e.key === ',') {
      e.preventDefault();
      const clean = newTagInput.trim().replace(/^#/, '');
      if (clean) {
        onAddTag(clean);
        setNewTagInput('');
      }
    }
  };

  return (
    <div className="space-y-2 bg-slate-950/60 border border-slate-800/80 rounded-2xl p-4">
      <div className="text-[11px] font-extrabold uppercase text-purple-400 flex items-center justify-between">
        <span className="flex items-center gap-1.5">
          <Hash size={13} /> 3. Danh Sách Thẻ Thông Minh (Smart Tags)
        </span>
        <span className="text-[10px] text-slate-500 font-normal">Gõ tag và nhấn Enter hoặc phẩy</span>
      </div>

      <div className="flex flex-wrap items-center gap-2 p-2.5 bg-slate-900 border border-slate-800 rounded-xl min-h-[44px]">
        {tags.map(tag => (
          <span
            key={tag}
            className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-purple-950/80 text-purple-300 border border-purple-700/60 shadow-sm"
          >
            #{tag}
            <button
              type="button"
              onClick={() => onRemoveTag(tag)}
              className="hover:text-red-400 cursor-pointer ml-1 text-xs"
            >
              ×
            </button>
          </span>
        ))}
        <input
          type="text"
          value={newTagInput}
          onChange={e => setNewTagInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="+ Nhập thẻ mới..."
          className="bg-transparent text-xs text-white placeholder:text-slate-500 focus:outline-none flex-1 min-w-[130px]"
        />
      </div>

      {suggestedTags.length > 0 && (
        <div className="space-y-1.5 pt-1">
          <div className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
            <Sparkles size={11} className="text-amber-400" /> Thẻ gợi ý động từ Database ({domain || 'Tất cả'}):
          </div>
          <div className="flex flex-wrap gap-1.5">
            {suggestedTags.map(tag => {
              const isSelected = tags.includes(tag);
              return (
                <button
                  key={tag}
                  type="button"
                  onClick={() => {
                    if (isSelected) {
                      onRemoveTag(tag);
                    } else {
                      onAddTag(tag);
                    }
                  }}
                  className={`px-2.5 py-1 rounded-lg text-[10px] font-bold border transition-all cursor-pointer ${
                    isSelected
                      ? 'bg-purple-600 text-white border-purple-500 shadow-sm shadow-purple-600/30'
                      : 'bg-slate-900 text-slate-400 hover:text-slate-200 hover:border-slate-700 border-slate-800'
                  }`}
                >
                  #{tag}
                </button>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
};
