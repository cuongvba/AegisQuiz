import React, { useState } from 'react';
import { Eye, EyeOff, Copy, Check, Trash2, Plus, ShieldCheck, KeyRound, AlertCircle } from 'lucide-react';
import { useTranslation } from 'react-i18next';

interface SecureKeyVaultEditorProps {
  providerType: number;
  providerName: string;
  apiKeys: string[];
  onChange: (newKeys: string[]) => void;
}

export const SecureKeyVaultEditor: React.FC<SecureKeyVaultEditorProps> = ({
  providerName,
  apiKeys,
  onChange,
}) => {
  const { t } = useTranslation('ai_settings');
  const [visibleIndices, setVisibleIndices] = useState<Record<number, boolean>>({});
  const [copiedIndex, setCopiedIndex] = useState<number | null>(null);
  const [newKeyInput, setNewKeyInput] = useState('');
  const [isAdding, setIsAdding] = useState(false);

  // Che giấu key: Giữ lại 6 ký tự đầu và 6 ký tự cuối
  const maskKey = (rawKey: string): string => {
    const trimmed = rawKey.trim();
    if (trimmed.length <= 12) return '••••••••••••••••';
    const prefix = trimmed.slice(0, 6);
    const suffix = trimmed.slice(-6);
    return `${prefix}••••••••••••••••${suffix}`;
  };

  const toggleVisibility = (idx: number) => {
    setVisibleIndices(prev => {
      const nextState = !prev[idx];
      const updated = { ...prev, [idx]: nextState };
      
      // Tự động ẩn lại sau 10 giây để chống nhìn trộm
      if (nextState) {
        setTimeout(() => {
          setVisibleIndices(curr => ({ ...curr, [idx]: false }));
        }, 10000);
      }
      return updated;
    });
  };

  const handleCopy = async (key: string, idx: number) => {
    try {
      await navigator.clipboard.writeText(key.trim());
      setCopiedIndex(idx);
      setTimeout(() => setCopiedIndex(null), 2000);
    } catch {
      // Fallback
    }
  };

  const handleDelete = (indexToDelete: number) => {
    const updated = apiKeys.filter((_, idx) => idx !== indexToDelete);
    onChange(updated);
  };

  const handleAddNewKey = () => {
    const trimmed = newKeyInput.trim();
    if (!trimmed) return;
    
    // Nếu dán nhiều dòng (multi-line bulk import)
    const lines = trimmed.split(/[\r\n]+/).map(s => s.trim()).filter(Boolean);
    const combined = [...apiKeys, ...lines];
    // Loại bỏ trùng lặp
    const unique = Array.from(new Set(combined));
    onChange(unique);
    setNewKeyInput('');
    setIsAdding(false);
  };

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between text-xs">
        <span className="text-slate-400 flex items-center gap-1.5 font-medium">
          <KeyRound className="w-3.5 h-3.5 text-amber-400" />
          {t('key_vault.active_keys_count', { count: apiKeys.length })}
        </span>
        <span className="text-slate-500 text-[11px]">
          {t('key_vault.round_robin_hint')}
        </span>
      </div>

      {/* Danh sách các Keys đã được Masked */}
      <div className="space-y-2">
        {apiKeys.length === 0 ? (
          <div className="p-3 rounded-xl bg-slate-950/60 border border-dashed border-slate-800 text-center text-xs text-slate-500">
            {t('key_vault.paste_placeholder')}
          </div>
        ) : (
          apiKeys.map((key, idx) => {
            const isVisible = !!visibleIndices[idx];
            const isCopied = copiedIndex === idx;

            return (
              <div 
                key={idx}
                className="flex items-center justify-between px-3 py-2 rounded-xl bg-slate-950/90 border border-slate-800/80 hover:border-slate-700 transition-all text-xs font-mono group"
              >
                <div className="flex items-center gap-2 overflow-hidden flex-1 mr-2">
                  <ShieldCheck className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
                  <span className={`truncate select-all ${isVisible ? 'text-amber-200' : 'text-slate-300'}`}>
                    {isVisible ? key : maskKey(key)}
                  </span>
                </div>

                <div className="flex items-center gap-1 shrink-0">
                  {/* Nút Toggle Eye */}
                  <button
                    type="button"
                    onClick={() => toggleVisibility(idx)}
                    title={isVisible ? t('buttons.hide_keys') : t('buttons.show_keys')}
                    className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
                  >
                    {isVisible ? <EyeOff className="w-3.5 h-3.5 text-amber-400" /> : <Eye className="w-3.5 h-3.5" />}
                  </button>

                  {/* Nút Copy */}
                  <button
                    type="button"
                    onClick={() => handleCopy(key, idx)}
                    title={t('buttons.copy_key')}
                    className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
                  >
                    {isCopied ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
                  </button>

                  {/* Nút Xóa */}
                  <button
                    type="button"
                    onClick={() => handleDelete(idx)}
                    title="Xóa khóa"
                    className="p-1.5 rounded-lg text-slate-500 hover:text-rose-400 hover:bg-rose-500/10 transition-colors opacity-80 group-hover:opacity-100"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>
            );
          })
        )}
      </div>

      {/* Khu vực thêm Key an toàn */}
      {isAdding ? (
        <div className="p-3 rounded-xl bg-slate-950 border border-indigo-500/40 space-y-2">
          <div className="flex items-center justify-between text-xs text-indigo-300 font-semibold">
            <span>{t('buttons.add_key')} ({providerName})</span>
            <button 
              type="button" 
              onClick={() => { setIsAdding(false); setNewKeyInput(''); }}
              className="text-slate-400 hover:text-white text-[11px]"
            >
              Hủy
            </button>
          </div>

          <textarea
            rows={2}
            value={newKeyInput}
            onChange={(e) => setNewKeyInput(e.target.value)}
            placeholder={t('key_vault.paste_placeholder')}
            className="w-full px-3 py-2 text-xs font-mono bg-slate-900 border border-slate-700 rounded-lg text-slate-100 focus:outline-none focus:border-indigo-400 placeholder:text-slate-600"
          />

          <div className="flex items-center justify-between pt-1">
            <span className="text-[10px] text-slate-500 flex items-center gap-1">
              <AlertCircle className="w-3 h-3 text-amber-400" />
              {t('key_vault.warning_plaintext_risk')}
            </span>
            <button
              type="button"
              onClick={handleAddNewKey}
              disabled={!newKeyInput.trim()}
              className="px-3 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-white text-xs font-bold transition-all shadow-md shadow-indigo-600/20"
            >
              Lưu vào Bể Khóa
            </button>
          </div>
        </div>
      ) : (
        <button
          type="button"
          onClick={() => setIsAdding(true)}
          className="w-full py-2 px-3 rounded-xl border border-dashed border-slate-800 hover:border-slate-700 bg-slate-950/40 hover:bg-slate-900/50 text-slate-400 hover:text-slate-200 text-xs font-medium flex items-center justify-center gap-1.5 transition-all"
        >
          <Plus className="w-3.5 h-3.5 text-indigo-400" />
          {t('buttons.add_key')}
        </button>
      )}
    </div>
  );
};
