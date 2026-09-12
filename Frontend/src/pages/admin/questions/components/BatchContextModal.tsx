import { useState } from 'react';
import { X, BookOpen, CheckCircle2, Loader2 } from 'lucide-react';

interface BatchContextModalProps {
  isOpen: boolean;
  onClose: () => void;
  selectedCount: number;
  contexts: any[];
  onConfirm: (data: { contextId?: string; contextTitle?: string; contextContent?: string }) => Promise<void>;
  isAssigning: boolean;
}

export function BatchContextModal({
  isOpen,
  onClose,
  selectedCount,
  contexts,
  onConfirm,
  isAssigning,
}: BatchContextModalProps) {
  const [mode, setMode] = useState<'new' | 'existing'>('new');
  const [selectedContextId, setSelectedContextId] = useState('');
  const [newTitle, setNewTitle] = useState('');
  const [newContent, setNewContent] = useState('');
  const [error, setError] = useState('');

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (mode === 'existing') {
      if (!selectedContextId) {
        setError('Vui lòng chọn một bài đọc hiểu từ danh sách.');
        return;
      }
      await onConfirm({ contextId: selectedContextId });
    } else {
      if (!newContent.trim()) {
        setError('Vui lòng nhập nội dung bài đọc hiểu.');
        return;
      }
      await onConfirm({
        contextTitle: newTitle.trim() || 'Bài đọc hiểu dùng chung',
        contextContent: newContent.trim(),
      });
    }
  };

  const currentSelected = contexts.find(c => c.id === selectedContextId);

  return (
    <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-md z-50 flex items-center justify-center p-4">
      <div className="w-full max-w-xl bg-slate-900 border border-slate-750 rounded-3xl p-6 sm:p-8 shadow-2xl relative space-y-5 text-white">
        <button
          onClick={onClose}
          className="absolute top-5 right-5 text-slate-400 hover:text-white p-1 hover:bg-slate-800 rounded-xl transition-colors cursor-pointer"
        >
          <X size={18} />
        </button>

        <div className="space-y-1">
          <div className="inline-flex items-center gap-1.5 px-3 py-0.5 bg-indigo-950/60 text-indigo-300 border border-indigo-500/30 rounded-full text-[10px] font-black uppercase tracking-wider">
            <BookOpen size={11} /> Universal Stimulus Batch Assign
          </div>
          <h2 className="text-xl font-black text-transparent bg-clip-text bg-gradient-to-r from-indigo-300 via-purple-300 to-pink-300">
            Gán Bài Đọc Hiểu Chung Cho {selectedCount} Câu Hỏi
          </h2>
          <p className="text-xs text-slate-400">
            Tất cả <span className="text-indigo-400 font-bold">{selectedCount}</span> câu hỏi đã chọn sẽ được liên kết chung bài đọc này một lần duy nhất, tránh trùng lặp dữ liệu.
          </p>
        </div>

        {error && (
          <div className="p-3 bg-red-950/50 border border-red-500/40 rounded-xl text-xs text-red-300 font-medium">
            ⚠️ {error}
          </div>
        )}

        {/* Chế độ: Viết mới hoặc Chọn từ ngân hàng */}
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => { setMode('new'); setError(''); }}
            className={`flex-1 py-2 rounded-xl text-xs font-bold transition-all cursor-pointer ${
              mode === 'new'
                ? 'bg-indigo-600 text-white shadow-md shadow-indigo-900/40'
                : 'bg-slate-950 text-slate-400 hover:text-white border border-slate-800'
            }`}
          >
            + Viết bài đọc mới
          </button>
          {contexts.length > 0 && (
            <button
              type="button"
              onClick={() => { setMode('existing'); setError(''); }}
              className={`flex-1 py-2 rounded-xl text-xs font-bold transition-all cursor-pointer ${
                mode === 'existing'
                  ? 'bg-indigo-600 text-white shadow-md shadow-indigo-900/40'
                  : 'bg-slate-950 text-slate-400 hover:text-white border border-slate-800'
              }`}
            >
              Chọn bài đọc có sẵn ({contexts.length})
            </button>
          )}
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          {mode === 'existing' && contexts.length > 0 ? (
            <div className="space-y-2.5">
              <label className="text-[11px] font-bold text-slate-400 uppercase tracking-wider block">
                Danh sách bài đọc có sẵn trong ngân hàng
              </label>
              <select
                value={selectedContextId}
                onChange={e => setSelectedContextId(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-xs text-slate-200 focus:outline-none focus:border-indigo-500 cursor-pointer"
              >
                <option value="">-- Bấm để chọn bài đọc hiểu --</option>
                {contexts.map(c => (
                  <option key={c.id} value={c.id}>
                    {c.title} ({c.questionCount || 0} câu đã liên kết)
                  </option>
                ))}
              </select>
              {currentSelected && (
                <div className="text-xs text-slate-300 max-h-36 overflow-y-auto p-3 bg-slate-950/60 rounded-xl border border-indigo-900/40 leading-relaxed whitespace-pre-wrap font-sans">
                  {currentSelected.content}
                </div>
              )}
            </div>
          ) : (
            <div className="space-y-3">
              <div className="space-y-1">
                <label className="text-[11px] font-bold text-slate-400 uppercase tracking-wider block">
                  Tiêu đề bài đọc
                </label>
                <input
                  type="text"
                  placeholder="Ví dụ: Bài đọc hiểu Tiếng Anh - Passage 1 (Câu 15 - 19)..."
                  value={newTitle}
                  onChange={e => setNewTitle(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none focus:border-indigo-500"
                />
              </div>

              <div className="space-y-1">
                <label className="text-[11px] font-bold text-slate-400 uppercase tracking-wider block">
                  Nội dung bài đọc hiểu / tình huống dùng chung *
                </label>
                <textarea
                  required
                  placeholder="Dán toàn văn nội dung bài đọc hiểu vào đây..."
                  value={newContent}
                  onChange={e => setNewContent(e.target.value)}
                  rows={5}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none focus:border-indigo-500 resize-none leading-relaxed"
                />
              </div>
            </div>
          )}

          <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-800">
            <button
              type="button"
              onClick={onClose}
              disabled={isAssigning}
              className="px-4 py-2.5 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white rounded-xl font-bold transition-all text-xs border border-slate-800 cursor-pointer disabled:opacity-50"
            >
              Hủy bỏ
            </button>
            <button
              type="submit"
              disabled={isAssigning}
              className="px-5 py-2.5 bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 text-white font-bold rounded-xl transition-all shadow-lg shadow-indigo-600/30 active:scale-95 text-xs flex items-center gap-1.5 cursor-pointer disabled:opacity-50"
            >
              {isAssigning ? (
                <>
                  <Loader2 size={14} className="animate-spin" /> Đang gán...
                </>
              ) : (
                <>
                  <CheckCircle2 size={14} /> Xác nhận gán ({selectedCount} câu)
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
