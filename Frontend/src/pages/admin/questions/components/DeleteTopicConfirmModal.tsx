import React from 'react';
import { Trash2, X } from 'lucide-react';
import type { BankTopic } from '../types';
import type { LearnerQuestion } from '@/types/quiz';

interface DeleteTopicConfirmModalProps {
  topicToDelete: BankTopic | null;
  topics: BankTopic[];
  questions: LearnerQuestion[];
  isDeletingTopicLoading: boolean;
  onClose: () => void;
  onConfirm: (deleteQuestions: boolean) => void;
}

export const DeleteTopicConfirmModal: React.FC<DeleteTopicConfirmModalProps> = ({
  topicToDelete,
  topics,
  questions,
  isDeletingTopicLoading,
  onClose,
  onConfirm,
}) => {
  if (!topicToDelete) return null;

  const childQuestionsCount = questions.filter(
    q => q.topicCode === topicToDelete.code || q.categoryCode === topicToDelete.code
  ).length;

  const childTopicsCount = topics.filter(t => t.parentId === topicToDelete.id).length;

  return (
    <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-[70] flex items-center justify-center p-4">
      <div className="w-full max-w-lg bg-gradient-to-b from-slate-900 to-slate-950 border border-red-500/30 rounded-3xl p-6 md:p-8 shadow-2xl relative space-y-5 animate-in fade-in zoom-in-95">
        <div className="flex items-start gap-4">
          <div className="w-12 h-12 rounded-2xl bg-red-500/10 border border-red-500/20 text-red-400 flex items-center justify-center shrink-0">
            <Trash2 size={24} />
          </div>
          <div className="flex-1 min-w-0">
            <span className="text-[10px] font-black tracking-widest text-red-400 uppercase bg-red-950/60 border border-red-800/40 px-2.5 py-0.5 rounded-full inline-block">
              Xác nhận xóa chủ đề
            </span>
            <h3 className="text-lg font-black text-white mt-1.5 truncate">
              {topicToDelete.name}
            </h3>
            <p className="text-xs font-mono text-blue-400 mt-0.5">Mã chủ đề: {topicToDelete.code}</p>
          </div>
          <button
            onClick={() => !isDeletingTopicLoading && onClose()}
            className="text-slate-400 hover:text-white p-1 rounded-lg cursor-pointer"
            disabled={isDeletingTopicLoading}
          >
            <X size={18} />
          </button>
        </div>

        <div className="bg-slate-950/70 border border-slate-800/80 rounded-2xl p-4 text-xs space-y-2 select-none">
          <div className="flex items-center justify-between text-slate-300">
            <span>Số câu hỏi trực thuộc:</span>
            <span className="font-black text-blue-400">{childQuestionsCount} câu hỏi</span>
          </div>
          <div className="flex items-center justify-between text-slate-300">
            <span>Chủ đề con trực tiếp:</span>
            <span className="font-black text-purple-400">{childTopicsCount} chủ đề con</span>
          </div>
        </div>

        <p className="text-xs text-slate-300 leading-relaxed font-medium">
          Vui lòng chọn phương án xử lý câu hỏi và các chủ đề con khi xóa chủ đề này:
        </p>

        <div className="space-y-3">
          <button
            type="button"
            disabled={isDeletingTopicLoading}
            onClick={() => onConfirm(false)}
            className="w-full text-left p-4 bg-slate-900/90 hover:bg-slate-850 border border-slate-750 hover:border-blue-500/50 rounded-2xl transition-all group cursor-pointer disabled:opacity-50"
          >
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold text-slate-200 group-hover:text-blue-400 flex items-center gap-1.5">
                🛡️ Giữ lại câu hỏi (Khuyên dùng)
              </span>
              <span className="text-[10px] text-emerald-400 font-bold bg-emerald-950/60 border border-emerald-500/30 px-2 py-0.5 rounded-md">
                An toàn
              </span>
            </div>
            <p className="text-[11px] text-slate-400 mt-1.5 leading-relaxed">
              Chỉ xóa chủ đề này. Câu hỏi được bảo lưu và chuyển sang chủ đề cấp trên (hoặc nhóm chung). Các chủ đề con được nâng lên cấp trên.
            </p>
          </button>

          <button
            type="button"
            disabled={isDeletingTopicLoading}
            onClick={() => onConfirm(true)}
            className="w-full text-left p-4 bg-red-950/20 hover:bg-red-950/40 border border-red-500/30 hover:border-red-500/60 rounded-2xl transition-all group cursor-pointer disabled:opacity-50"
          >
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold text-red-300 group-hover:text-red-200 flex items-center gap-1.5">
                💥 Xóa toàn bộ nhánh & câu hỏi
              </span>
              <span className="text-[10px] text-red-400 font-bold bg-red-950/80 border border-red-500/40 px-2 py-0.5 rounded-md">
                Xóa triệt để
              </span>
            </div>
            <p className="text-[11px] text-slate-400 mt-1.5 leading-relaxed">
              Xóa triệt để chủ đề này, toàn bộ các chủ đề con/cháu và tất cả các câu hỏi thuộc nhánh khỏi hệ thống.
            </p>
          </button>
        </div>

        <div className="pt-2 flex justify-end gap-2 border-t border-slate-800/80">
          <button
            type="button"
            disabled={isDeletingTopicLoading}
            onClick={onClose}
            className="px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-slate-300 hover:text-white rounded-xl text-xs font-bold transition-all cursor-pointer border border-slate-800 disabled:opacity-50"
          >
            Hủy bỏ
          </button>
        </div>
      </div>
    </div>
  );
};
