import { Sparkles, CheckCheck, X, Loader2, FolderPlus, RefreshCw } from 'lucide-react';
import type { BankTopic } from '../../types';

export interface DetectedTopicInfo {
  code: string;
  name: string;
  domainCode: string;
  isVirtual: boolean;
}

interface DocxTopicDecisionBannerProps {
  detectedTopicInfo: DetectedTopicInfo | null;
  bulkTopic: string;
  localTopics: BankTopic[];
  totalQuestions: number;
  isCreatingTopic: boolean;
  onApplyDetectedTopic: () => Promise<void>;
  onSyncWithSelectedTopic: () => void;
  onDismiss: () => void;
}

/**
 * DocxTopicDecisionBanner — Quyết định thông minh: Áp dụng chủ đề mới hay Đồng bộ chủ đề chọn.
 * Tuân thủ quy chuẩn Zero-Monolith (kích thước < 150 dòng) và hiển thị trực quan 100% theo OCP.
 */
export function DocxTopicDecisionBanner({
  detectedTopicInfo,
  bulkTopic,
  localTopics,
  totalQuestions,
  isCreatingTopic,
  onApplyDetectedTopic,
  onSyncWithSelectedTopic,
  onDismiss,
}: DocxTopicDecisionBannerProps) {
  if (!detectedTopicInfo) return null;

  const selectedTopic = localTopics.find(t => t.code.toUpperCase() === bulkTopic.toUpperCase());
  const selectedTopicName = selectedTopic ? `${selectedTopic.name} (${selectedTopic.code})` : bulkTopic;

  return (
    <div className="bg-gradient-to-r from-blue-950/80 via-slate-900/90 to-purple-950/80 border border-blue-500/40 rounded-2xl p-3 mb-2.5 shadow-xl shadow-blue-950/30 shrink-0 animate-in fade-in slide-in-from-top-2 duration-200">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 text-xs">
        {/* Left: Thông tin nhận diện */}
        <div className="flex items-center gap-2.5 min-w-0">
          <div className="w-8 h-8 rounded-xl bg-gradient-to-br from-blue-500/20 to-purple-500/20 border border-blue-400/40 flex items-center justify-center text-blue-300 shrink-0 shadow-inner">
            <Sparkles size={16} className="text-amber-300 animate-pulse" />
          </div>
          <div className="min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <span className="font-bold text-white text-[13px]">
                Nhận diện chủ đề từ file:
              </span>
              <span className="font-extrabold text-amber-300 bg-amber-950/70 px-2 py-0.5 rounded-lg border border-amber-500/40">
                {detectedTopicInfo.name} ({detectedTopicInfo.code})
              </span>
              {detectedTopicInfo.isVirtual ? (
                <span className="text-[10px] font-bold px-1.5 py-0.5 rounded-md bg-emerald-950/80 text-emerald-300 border border-emerald-500/50">
                  ✨ Chủ đề mới (Chưa có trong CSDL)
                </span>
              ) : (
                <span className="text-[10px] font-bold px-1.5 py-0.5 rounded-md bg-blue-950/80 text-blue-300 border border-blue-500/50">
                  🏷️ Đã có trong CSDL
                </span>
              )}
            </div>
            <p className="text-slate-400 text-[11px] mt-0.5 truncate">
              Bạn muốn áp dụng chủ đề này cho toàn bộ đề thi hay đồng bộ theo chủ đề đang chọn?
            </p>
          </div>
        </div>

        {/* Right: 2 Lựa chọn Hành động */}
        <div className="flex items-center gap-2 shrink-0 w-full sm:w-auto justify-end">
          {/* Lựa chọn 1: Áp dụng chủ đề nhận diện (tự động tạo mới nếu là ảo) */}
          <button
            type="button"
            disabled={isCreatingTopic}
            onClick={onApplyDetectedTopic}
            className="px-3 py-1.5 bg-gradient-to-r from-emerald-600 via-teal-600 to-emerald-500 hover:from-emerald-500 hover:to-teal-500 text-white rounded-xl font-bold transition-all text-xs flex items-center gap-1.5 cursor-pointer active:scale-95 shadow-md shadow-emerald-950/50 border border-emerald-400/40 disabled:opacity-50"
            title={detectedTopicInfo.isVirtual ? 'Tạo mới chủ đề này vào CSDL và áp dụng cho toàn bộ câu hỏi' : 'Áp dụng chủ đề nhận diện cho toàn bộ câu hỏi'}
          >
            {isCreatingTopic ? (
              <Loader2 size={13} className="animate-spin text-emerald-200" />
            ) : detectedTopicInfo.isVirtual ? (
              <FolderPlus size={13} className="text-emerald-200" />
            ) : (
              <CheckCheck size={13} className="text-emerald-200" />
            )}
            <span>
              {isCreatingTopic
                ? 'Đang khởi tạo...'
                : detectedTopicInfo.isVirtual
                ? `Tạo mới & Áp dụng (${totalQuestions})`
                : `Áp dụng chủ đề này (${totalQuestions})`}
            </span>
          </button>

          {/* Lựa chọn 2: Đồng bộ theo chủ đề người dùng đang chọn */}
          <button
            type="button"
            onClick={onSyncWithSelectedTopic}
            className="px-3 py-1.5 bg-slate-800 hover:bg-slate-750 text-slate-200 border border-slate-700 hover:border-slate-600 rounded-xl font-bold transition-all text-xs flex items-center gap-1.5 cursor-pointer active:scale-95 shadow-sm"
            title={`Đồng bộ toàn bộ câu hỏi theo chủ đề đang chọn: ${selectedTopicName}`}
          >
            <RefreshCw size={12} className="text-blue-400" />
            <span className="max-w-[140px] truncate">
              Đồng bộ: {selectedTopic ? selectedTopic.name : bulkTopic}
            </span>
          </button>

          {/* Đóng banner nếu muốn tùy biến từng câu */}
          <button
            type="button"
            onClick={onDismiss}
            className="p-1.5 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 transition-colors"
            title="Đóng thông báo (giữ nguyên phân loại hiện tại)"
          >
            <X size={14} />
          </button>
        </div>
      </div>
    </div>
  );
}
