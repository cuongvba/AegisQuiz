import { useMemo } from 'react';
import { X, Plus, Save, Edit, Lock, Unlock, Trash2, FolderTree, Network, Globe } from 'lucide-react';
import type { LearnerQuestion } from '@/types/quiz';
import type { BankTopic } from '../types';
import { getTopicFullPath } from '../types';

interface TopicModalProps {
  isOpen: boolean;
  isEditingTopic: boolean;
  editingTopicId: string | null;
  hasWritePermission: boolean;
  topics: BankTopic[];
  questions: LearnerQuestion[];

  topicCode: string;
  topicName: string;
  topicDesc: string;
  topicCat: string;
  topicVisibility: string;
  topicParentId: string;
  topicDomainCode?: string;
  topicScope?: string;

  setTopicCode: (v: string) => void;
  setTopicName: (v: string) => void;
  setTopicDesc: (v: string) => void;
  setTopicCat: (v: string) => void;
  setTopicVisibility: (v: string) => void;
  setTopicParentId: (v: string) => void;
  setTopicDomainCode?: (v: string) => void;
  setTopicScope?: (v: string) => void;
  setIsEditingTopic: (v: boolean) => void;

  onClose: () => void;
  onSave: (e: React.FormEvent) => void;
  onOpenAdd: () => void;
  onOpenEdit: (t: BankTopic) => void;
  onDelete: (id: string) => void;
  onToggleActive: (t: BankTopic) => void;
}

export function TopicModal({
  isOpen, isEditingTopic, editingTopicId, hasWritePermission,
  topics, questions,
  topicCode, topicName, topicDesc, topicCat, topicVisibility, topicParentId,
  topicDomainCode = 'GENERAL', topicScope = 'COMMUNITY',
  setTopicCode, setTopicName, setTopicDesc, setTopicCat, setTopicVisibility, setTopicParentId,
  setTopicDomainCode, setTopicScope, setIsEditingTopic,
  onClose, onSave, onOpenAdd, onOpenEdit, onDelete, onToggleActive,
}: TopicModalProps) {
  if (!isOpen) return null;

  const activeTopic = topics.find(t => t.id === editingTopicId);

  // Đếm câu hỏi trực tiếp và đệ quy qua các chuyên đề con
  const directCount = useMemo(() => {
    return questions.filter(
      q => q.topicCode === topicCode || q.categoryCode === topicCode
    ).length;
  }, [questions, topicCode]);

  const childTopics = useMemo(() => {
    if (!editingTopicId) return [];
    return topics.filter(t => t.parentId === editingTopicId);
  }, [editingTopicId, topics]);

  const { descendantCount, descendantTopicCount } = useMemo(() => {
    if (!editingTopicId) return { descendantCount: 0, descendantTopicCount: 0 };
    const childCodes = new Set<string>();
    const queue = [editingTopicId];
    while (queue.length > 0) {
      const pid = queue.shift();
      if (!pid) continue;
      topics.filter(t => t.parentId === pid).forEach(child => {
        childCodes.add(child.code);
        queue.push(child.id);
      });
    }
    const qCount = questions.filter(
      q => (q.topicCode && childCodes.has(q.topicCode)) || (q.categoryCode && childCodes.has(q.categoryCode))
    ).length;
    return { descendantCount: qCount, descendantTopicCount: childCodes.size };
  }, [editingTopicId, topics, questions]);

  const totalInBranch = directCount + descendantCount;

  return (
    <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div className="w-full max-w-md bg-slate-900 border border-slate-800 rounded-3xl p-6 md:p-8 shadow-2xl relative space-y-5 max-h-[90vh] overflow-y-auto">
        <button onClick={onClose} className="absolute top-4 right-4 text-slate-400 hover:text-white p-1.5 hover:bg-slate-800 rounded-xl transition-colors cursor-pointer">
          <X size={18} />
        </button>

        <div>
          <h2 className="text-xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400">
            {isEditingTopic ? (editingTopicId ? 'Cập nhật Chủ đề' : 'Thêm Chủ đề Mới') : 'Chi tiết Chủ đề'}
          </h2>
          <p className="text-[10px] text-slate-500 font-bold uppercase tracking-widest mt-1">Danh mục ngân hàng câu hỏi chuẩn đa ngành</p>
        </div>

        <form onSubmit={onSave} className="space-y-4">
          <div className="space-y-1">
            <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Mã Chủ đề (Ví dụ: IT, TOAN, ENGLISH)</label>
            <input
              type="text" required
              placeholder="Nhập mã viết hoa không dấu..."
              value={topicCode}
              onChange={e => setTopicCode(e.target.value.toUpperCase())}
              disabled={!isEditingTopic || !!editingTopicId}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed font-mono font-bold"
            />
          </div>

          <div className="space-y-1">
            <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Tên Chủ đề (Hiển thị học viên)</label>
            <input
              type="text" required
              placeholder="Ví dụ: Công nghệ thông tin, Toán cao cấp..."
              value={topicName}
              onChange={e => setTopicName(e.target.value)}
              disabled={!isEditingTopic}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 disabled:opacity-75 disabled:cursor-not-allowed font-bold"
            />
          </div>

          {/* Thống kê câu hỏi thông minh đa tầng */}
          {editingTopicId && (
            <div className="space-y-1.5 bg-slate-950/70 border border-slate-800/80 rounded-2xl p-3.5">
              <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider flex items-center justify-between">
                <span>Thống kê câu hỏi</span>
                {descendantTopicCount > 0 && (
                  <span className="text-[9px] font-black text-amber-400 bg-amber-500/10 px-2 py-0.5 rounded-full border border-amber-500/20">
                    Thư mục cha ({descendantTopicCount} chuyên đề con)
                  </span>
                )}
              </label>

              <div className="flex flex-col gap-1 text-xs">
                <div className="flex justify-between items-center text-slate-300">
                  <span>Trực thuộc mã này:</span>
                  <span className="font-mono font-black text-blue-400">{directCount} câu hỏi</span>
                </div>
                {descendantTopicCount > 0 && (
                  <div className="flex justify-between items-center text-slate-300 pt-1 border-t border-slate-800/60">
                    <span className="flex items-center gap-1 text-emerald-400 font-bold">
                      <FolderTree size={13} /> Tổng toàn bộ nhánh:
                    </span>
                    <span className="font-mono font-black text-emerald-400 bg-emerald-500/10 px-2 py-0.5 rounded border border-emerald-500/20">
                      {totalInBranch} câu hỏi
                    </span>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Lĩnh Vực / Miền Ngành & Phạm Vi Tri Thức */}
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider flex items-center gap-1">
                <Network size={11} className="text-cyan-400" /> Miền ngành
              </label>
              <select
                value={topicDomainCode}
                onChange={e => setTopicDomainCode && setTopicDomainCode(e.target.value)}
                disabled={!isEditingTopic}
                className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed font-medium"
              >
                <option value="BANKING">🏦 Tài chính Ngân hàng</option>
                <option value="EDUCATION">🎓 Giáo dục & Khảo thí</option>
                <option value="GOV_DRIVING">🚗 Sát hạch Giao thông</option>
                <option value="GENERAL">🌐 Tổng hợp / Khác</option>
                <option value="HEALTHCARE">⚕️ Y tế & Dược phẩm</option>
                <option value="IT_SECURITY">🛡️ CNTT & Bảo mật</option>
              </select>
            </div>

            <div className="space-y-1">
              <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider flex items-center gap-1">
                <Globe size={11} className="text-purple-400" /> Bản quyền
              </label>
              <select
                value={topicScope}
                onChange={e => setTopicScope && setTopicScope(e.target.value)}
                disabled={!isEditingTopic}
                className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed font-medium"
              >
                <option value="COMMUNITY">Cộng đồng (COMMUNITY)</option>
                <option value="TENANT">Nội bộ Tenant (TENANT)</option>
              </select>
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Mô tả chủ đề</label>
            <textarea
              placeholder="Mô tả nội dung học và kiểm tra của chủ đề này..."
              value={topicDesc}
              onChange={e => setTopicDesc(e.target.value)}
              disabled={!isEditingTopic}
              className="w-full h-20 bg-slate-950 border border-slate-800 rounded-xl px-4 py-3 text-xs text-white placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors resize-none disabled:opacity-75 disabled:cursor-not-allowed"
            />
          </div>

          <div className="space-y-1">
            <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Chủ đề cha (Phân cấp)</label>
            <select
              value={topicParentId}
              onChange={e => setTopicParentId(e.target.value)}
              disabled={!isEditingTopic}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed"
            >
              <option value="">(Không có chủ đề cha — Cấp gốc)</option>
              {topics.filter(t => t.id !== editingTopicId).map(t => (
                <option key={t.id} value={t.id}>{getTopicFullPath(t, topics)}</option>
              ))}
            </select>
          </div>

          <div className="space-y-1">
            <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Phạm vi hiển thị</label>
            <select
              value={topicVisibility}
              onChange={e => setTopicVisibility(e.target.value)}
              disabled={!isEditingTopic}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed"
            >
              <option value="PUBLIC">Công khai (PUBLIC)</option>
              <option value="PRIVATE">Nội bộ (PRIVATE)</option>
            </select>
          </div>

          {!isEditingTopic ? (
            <div className="space-y-3 pt-4 border-t border-slate-800/80">
              {hasWritePermission ? (
                <>
                  <div className="grid grid-cols-4 gap-2">
                    <button type="button" onClick={() => { onOpenAdd(); setIsEditingTopic(true); }} className="py-2.5 bg-slate-950 hover:bg-slate-850 text-blue-450 border border-slate-850 rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 cursor-pointer" title="Tạo chủ đề mới"><Plus size={14} /> Mới</button>
                    <button type="button" onClick={() => setIsEditingTopic(true)} className="py-2.5 bg-slate-950 hover:bg-slate-850 text-amber-450 border border-slate-850 rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 cursor-pointer" title="Chỉnh sửa"><Edit size={14} /> Sửa</button>
                    <button
                      type="button"
                      onClick={async () => { if (editingTopicId && activeTopic) await onToggleActive(activeTopic); }}
                      className={`py-2.5 border border-slate-850 rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 cursor-pointer bg-slate-950 hover:bg-slate-850 ${activeTopic?.enabled !== false ? 'text-orange-400' : 'text-emerald-450'}`}
                      title="Bật/Tắt Kích hoạt"
                    >
                      {activeTopic?.enabled !== false ? <Lock size={14} /> : <Unlock size={14} />}
                      {activeTopic?.enabled !== false ? 'Khóa' : 'Kích hoạt'}
                    </button>
                    <button type="button" onClick={() => { if (editingTopicId) { onDelete(editingTopicId); onClose(); } }} className="py-2.5 bg-slate-950 hover:bg-slate-850 text-red-405 border border-slate-850 rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 cursor-pointer" title="Xóa chủ đề"><Trash2 size={14} /> Xóa</button>
                  </div>
                  <button type="button" onClick={onClose} className="w-full py-2.5 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-800 rounded-xl font-bold transition-all text-xs cursor-pointer">Đóng</button>
                </>
              ) : (
                <button type="button" onClick={onClose} className="w-full py-3 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-800 rounded-xl font-bold transition-all text-xs cursor-pointer">Đóng</button>
              )}
            </div>
          ) : (
            <div className="flex gap-3 pt-4 border-t border-slate-800/80">
              <button
                type="button"
                onClick={() => {
                  if (editingTopicId) {
                    const original = topics.find(t => t.id === editingTopicId);
                    if (original) onOpenEdit(original);
                    setIsEditingTopic(false);
                  } else {
                    onClose();
                  }
                }}
                className="flex-1 py-3 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white rounded-xl font-bold transition-all text-xs border border-slate-800 cursor-pointer"
              >Hủy bỏ</button>
              <button type="submit" className="flex-1 py-3 bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 text-white font-bold rounded-xl transition-all shadow-md shadow-blue-500/10 active:scale-95 text-xs flex items-center justify-center gap-1.5 cursor-pointer">
                <Save size={14} /> Lưu Chủ đề
              </button>
            </div>
          )}
        </form>
      </div>
    </div>
  );
}

