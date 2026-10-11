import { 
  Layers, Plus, Globe, CheckCheck, Loader2, RotateCcw, Tag, Sparkles, 
  SlidersHorizontal, ChevronDown, ChevronUp, Hash, Wand2, FileCode, Cpu 
} from 'lucide-react';
import type { BankTopic } from '../../types';

interface DocxTaxonomyBarProps {
  bulkTopic: string;
  setBulkTopic: (val: string) => void;
  localTopics: BankTopic[];
  openCreateTopicModal: (qIdx: number | null) => void;
  bulkDifficulty: number;
  setBulkDifficulty: (val: number) => void;
  bulkDomain: string;
  setBulkDomain: (val: string) => void;
  activeDomains: any[];
  onApplyAllCore: () => void;
  totalCount: number;
  onCorrectVietnamese: () => void;
  isCorrectingVN: boolean;
  rawOriginalList: any[] | null;
  onRevertRawText: () => void;
  onOpenBulkTaxonomyModal: () => void;
  onAutoClassifyTaxonomyAndTags: () => void;
  showAdvancedBulkTools: boolean;
  setShowAdvancedBulkTools: (val: boolean | ((prev: boolean) => boolean)) => void;
  bulkTagInput: string;
  setBulkTagInput: (val: string) => void;
  onAddBulkTag: () => void;
  onAutoClassifyTopics: () => void;
  onConvertLatex?: () => Promise<void>;
  hasFormulaImages: boolean;
  showMathStudio: boolean;
  setShowMathStudio: (val: boolean | ((prev: boolean) => boolean)) => void;
  mathSettings: any;
  showCigRules: boolean;
  setShowCigRules: (val: boolean | ((prev: boolean) => boolean)) => void;
  hasMultipleExams: boolean;
  examGroups: string[];
  selectedExamFilter: string;
  setSelectedExamFilter: (val: string) => void;
  docxPreviewList: any[];
}

export function DocxTaxonomyBar({
  bulkTopic,
  setBulkTopic,
  localTopics,
  openCreateTopicModal,
  bulkDifficulty,
  setBulkDifficulty,
  bulkDomain,
  setBulkDomain,
  activeDomains,
  onApplyAllCore,
  totalCount,
  onCorrectVietnamese,
  isCorrectingVN,
  rawOriginalList,
  onRevertRawText,
  onOpenBulkTaxonomyModal,
  onAutoClassifyTaxonomyAndTags,
  showAdvancedBulkTools,
  setShowAdvancedBulkTools,
  bulkTagInput,
  setBulkTagInput,
  onAddBulkTag,
  onAutoClassifyTopics,
  onConvertLatex,
  hasFormulaImages,
  showMathStudio,
  setShowMathStudio,
  mathSettings,
  showCigRules,
  setShowCigRules,
  hasMultipleExams,
  examGroups,
  selectedExamFilter,
  setSelectedExamFilter,
  docxPreviewList,
}: DocxTaxonomyBarProps) {
  return (
    <div className="bg-slate-950/80 border border-slate-800/90 rounded-2xl p-2.5 mb-2.5 shadow-inner shrink-0">
      <div className="flex flex-wrap items-center justify-between gap-2 text-xs">
        {/* Core Bulk Selectors */}
        <div className="flex flex-wrap items-center gap-1.5 sm:gap-2">
          {/* Chủ đề */}
          <div className="flex items-center gap-1 bg-slate-900 border border-slate-800 rounded-xl px-2 py-1">
            <Layers size={13} className="text-blue-400 shrink-0" />
            <select
              value={bulkTopic}
              onChange={e => {
                if (e.target.value === '__CREATE_NEW__') {
                  openCreateTopicModal(null);
                } else {
                  setBulkTopic(e.target.value);
                }
              }}
              className="bg-transparent text-slate-200 text-xs font-medium focus:outline-none cursor-pointer max-w-[150px] sm:max-w-[180px] truncate"
            >
              {localTopics.map(t => <option key={t.id || t.code} value={t.code} className="bg-slate-900">{t.name} ({t.code})</option>)}
              <option value="__CREATE_NEW__" className="text-amber-400 font-bold bg-slate-950">➕ Tạo chủ đề mới...</option>
            </select>
            <button
              type="button"
              onClick={() => openCreateTopicModal(null)}
              className="p-0.5 text-slate-400 hover:text-white rounded hover:bg-slate-800 transition-colors"
              title="Tạo chủ đề mới"
            >
              <Plus size={12} />
            </button>
          </div>

          {/* Độ khó */}
          <div className="flex items-center gap-1 bg-slate-900 border border-slate-800 rounded-xl px-2 py-1">
            <span className="text-[11px] text-slate-400 font-semibold">Độ khó:</span>
            <select
              value={bulkDifficulty}
              onChange={e => setBulkDifficulty(parseInt(e.target.value))}
              className="bg-transparent text-slate-200 text-xs font-medium focus:outline-none cursor-pointer"
            >
              {[1, 2, 3, 4, 5].map(d => <option key={d} value={d} className="bg-slate-900">Mức {d}</option>)}
            </select>
          </div>

          {/* Ngành */}
          <div className="flex items-center gap-1 bg-slate-900 border border-purple-500/30 rounded-xl px-2 py-1">
            <Globe size={13} className="text-purple-400 shrink-0" />
            <select
              value={bulkDomain}
              onChange={e => setBulkDomain(e.target.value)}
              className="bg-transparent text-purple-200 text-xs font-bold focus:outline-none cursor-pointer max-w-[130px] sm:max-w-[150px] truncate"
            >
              {activeDomains.map(d => (
                <option key={d.code} value={d.code} className="bg-slate-900">{d.icon} {d.short}</option>
              ))}
            </select>
          </div>

          {/* Nút Áp dụng chung cho bộ 3 */}
          <button
            type="button"
            onClick={onApplyAllCore}
            className="px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white rounded-xl font-bold transition-all text-xs flex items-center gap-1 cursor-pointer active:scale-95 shadow-md shadow-blue-950/50 whitespace-nowrap"
            title="Áp dụng đồng loạt Chủ đề, Độ khó và Ngành cho cả bộ đề"
          >
            <CheckCheck size={13} /> Áp dụng ({totalCount})
          </button>
        </div>

        {/* Right Action Tools */}
        <div className="flex items-center gap-1.5">
          {/* VNCorrect Chuẩn hóa Tiếng Việt On-Demand */}
          <button
            type="button"
            onClick={onCorrectVietnamese}
            disabled={isCorrectingVN}
            className="px-3 py-1.5 bg-gradient-to-r from-emerald-600 via-teal-600 to-emerald-500 hover:from-emerald-500 hover:to-teal-500 text-white rounded-xl font-bold transition-all text-xs flex items-center gap-1.5 cursor-pointer active:scale-95 shadow-md shadow-emerald-950/40 border border-emerald-400/40 whitespace-nowrap disabled:opacity-50"
            title="Chuẩn hóa chính tả tiếng Việt & Maprepl theo 670+ quy tắc thực chiến"
          >
            {isCorrectingVN ? (
              <Loader2 size={13} className="animate-spin text-emerald-200" />
            ) : (
              <CheckCheck size={13} className="text-emerald-200" />
            )}
            <span>{isCorrectingVN ? 'Đang chuẩn hóa...' : '✨ VNCorrect'}</span>
          </button>

          {rawOriginalList && (
            <button
              type="button"
              onClick={onRevertRawText}
              className="px-2.5 py-1.5 bg-slate-850 hover:bg-slate-800 text-amber-300 border border-amber-500/30 rounded-xl font-bold transition-all text-xs flex items-center gap-1 cursor-pointer active:scale-95 whitespace-nowrap"
              title="Khôi phục văn bản gốc ban đầu của file"
            >
              <RotateCcw size={12} className="text-amber-400" />
              <span>Bản gốc</span>
            </button>
          )}

          <button
            type="button"
            onClick={onOpenBulkTaxonomyModal}
            className="px-3 py-1.5 bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-500 hover:to-indigo-500 text-white rounded-xl font-bold transition-all text-xs flex items-center gap-1.5 cursor-pointer active:scale-95 shadow-md shadow-purple-950/40 border border-purple-400/30 whitespace-nowrap"
          >
            <Tag size={13} /> 🏷️ Thẻ & Tọa độ
          </button>

          <button
            type="button"
            onClick={onAutoClassifyTaxonomyAndTags}
            className="px-2.5 py-1.5 bg-purple-950/50 hover:bg-purple-900/60 text-purple-300 border border-purple-800/60 rounded-xl font-bold transition-all text-xs flex items-center gap-1 cursor-pointer active:scale-95 whitespace-nowrap"
            title="Tự động nhận diện Ngành và Thẻ bằng AI"
          >
            <Sparkles size={12} className="text-pink-400" /> Auto AI
          </button>

          <button
            type="button"
            onClick={() => setShowAdvancedBulkTools(prev => !prev)}
            className={`px-2.5 py-1.5 rounded-xl font-bold transition-all text-xs flex items-center gap-1 border cursor-pointer active:scale-95 ${
              showAdvancedBulkTools
                ? 'bg-slate-800 text-white border-slate-700'
                : 'bg-slate-900 hover:bg-slate-850 text-slate-400 hover:text-slate-200 border-slate-800'
            }`}
            title="Mở rộng / Thu gọn công cụ bổ trợ (Thẻ chung, OCR LaTeX, Lọc đề)"
          >
            <SlidersHorizontal size={12} />
            <span>Tiện ích</span>
            {showAdvancedBulkTools ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
          </button>
        </div>
      </div>

      {/* Drawer Công cụ Mở rộng */}
      {showAdvancedBulkTools && (
        <div className="pt-2 mt-2 border-t border-slate-800/80 flex flex-wrap items-center justify-between gap-2 text-xs animate-in fade-in slide-in-from-top-1 duration-150">
          <div className="flex flex-wrap items-center gap-2">
            {/* Thêm thẻ chung */}
            <div className="flex items-center gap-1 bg-slate-900 border border-slate-800 rounded-xl px-2 py-0.5">
              <Hash size={12} className="text-purple-400" />
              <input
                type="text"
                value={bulkTagInput}
                onChange={e => setBulkTagInput(e.target.value)}
                onKeyDown={e => {
                  if (e.key === 'Enter') {
                    e.preventDefault();
                    onAddBulkTag();
                  }
                }}
                placeholder="Thẻ chung (VD: tot-nghiep-2025)..."
                className="bg-transparent text-xs text-slate-200 w-40 focus:outline-none"
              />
              <button
                type="button"
                onClick={onAddBulkTag}
                className="px-2 py-0.5 bg-purple-600 hover:bg-purple-500 text-white rounded-lg text-[11px] font-bold transition-all"
              >
                + Gán
              </button>
            </div>

            {/* Nhận diện từ khóa */}
            <button
              onClick={onAutoClassifyTopics}
              className="px-2.5 py-1 bg-slate-900 hover:bg-slate-850 text-slate-300 border border-slate-800 rounded-xl font-bold transition-all text-xs flex items-center gap-1 cursor-pointer active:scale-95"
              title="Phân tích từ khóa nội dung để gán Chủ đề"
            >
              <Wand2 size={12} /> Nhận diện từ khóa
            </button>

            {/* AI OCR LaTeX */}
            {onConvertLatex && hasFormulaImages && (
              <button
                onClick={onConvertLatex}
                className="px-2.5 py-1 bg-teal-950/40 hover:bg-teal-900/60 text-teal-300 border border-teal-800/60 rounded-xl font-bold transition-all text-xs flex items-center gap-1 cursor-pointer active:scale-95"
                title="Chuyển đổi ảnh công thức MathType sang mã LaTeX ($...$)"
              >
                <FileCode size={12} className="text-teal-400" /> AI OCR LaTeX
              </button>
            )}

            {/* Math Typography Studio Button */}
            <button
              type="button"
              onClick={() => setShowMathStudio(prev => !prev)}
              className={`px-2.5 py-1 rounded-xl font-bold transition-all text-xs flex items-center gap-1.5 cursor-pointer active:scale-95 ${
                showMathStudio
                  ? 'bg-gradient-to-r from-blue-600 to-indigo-600 text-white shadow-md shadow-blue-500/20 border border-blue-400/50'
                  : 'bg-slate-900 hover:bg-slate-850 text-blue-300 border border-blue-900/50'
              }`}
              title="Tùy chỉnh cỡ chữ, giãn cách và tỷ lệ công thức toán học động theo chuẩn 2026"
            >
              <SlidersHorizontal size={12} className={showMathStudio ? 'text-white' : 'text-blue-400'} />
              <span>Math Studio</span>
              <span className="text-[10px] bg-blue-950/80 px-1.5 py-0.2 rounded border border-blue-800/40 text-blue-200">
                {mathSettings.baseFontUnit}x
              </span>
            </button>

            {/* CIG Engine Rules Studio Button */}
            <button
              type="button"
              onClick={() => setShowCigRules(prev => !prev)}
              className={`px-2.5 py-1 rounded-xl font-bold transition-all text-xs flex items-center gap-1.5 cursor-pointer active:scale-95 ${
                showCigRules
                  ? 'bg-gradient-to-r from-purple-600 to-pink-600 text-white shadow-md shadow-purple-500/20 border border-purple-400/50'
                  : 'bg-slate-900 hover:bg-slate-850 text-purple-300 border border-purple-900/50'
              }`}
              title="Quy Tắc Nhận Thức Toàn Cầu (CIG Engine) & AI Rule Autopilot"
            >
              <Cpu size={12} className={showCigRules ? 'text-white' : 'text-purple-400'} />
              <span>CIG Rules</span>
            </button>
          </div>

          {/* Tabs lọc theo đề thi (nếu có nhiều đề) */}
          {hasMultipleExams && (
            <div className="flex items-center gap-1.5 overflow-x-auto">
              <span className="text-[10px] font-bold uppercase text-slate-400">Đề:</span>
              <button
                type="button"
                onClick={() => setSelectedExamFilter('ALL')}
                className={`px-2 py-0.5 rounded-lg text-xs font-bold transition-all ${
                  selectedExamFilter === 'ALL'
                    ? 'bg-blue-600 text-white'
                    : 'bg-slate-900 text-slate-400 hover:text-white border border-slate-800'
                }`}
              >
                Tất cả ({docxPreviewList.length})
              </button>
              {examGroups.map(eg => {
                const count = docxPreviewList.filter(q => (q.topicCode || 'GENERAL') === eg).length;
                const label = eg.replace('TOAN_DE_', 'Đề ');
                const isSelected = selectedExamFilter === eg;
                return (
                  <button
                    key={eg}
                    type="button"
                    onClick={() => setSelectedExamFilter(eg)}
                    className={`px-2 py-0.5 rounded-lg text-xs font-bold transition-all ${
                      isSelected
                        ? 'bg-purple-600 text-white'
                        : 'bg-slate-900 text-slate-400 hover:text-white border border-slate-800'
                    }`}
                  >
                    {label} ({count})
                  </button>
                );
              })}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
