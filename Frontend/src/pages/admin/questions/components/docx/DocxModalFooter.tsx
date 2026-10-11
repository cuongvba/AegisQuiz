import { RefObject } from 'react';
import { FileSpreadsheet, Download, Upload, Sparkles, Save, Loader2, Globe } from 'lucide-react';
import { FeatureGate } from '@/shared/components/entitlement';
import { FeatureKey } from '@/types/entitlement';

interface DocxModalFooterProps {
  onClose: () => void;
  loading: boolean;
  isAiSolving?: boolean;
  isExportingExcel: boolean;
  totalQuestions: number;
  showExportMenu: boolean;
  setShowExportMenu: (val: boolean | ((prev: boolean) => boolean)) => void;
  onExportExcel: (templateCode: string) => void;
  customTemplateInputRef: RefObject<HTMLInputElement | null>;
  onExportCustomTemplate: (file: File) => void;
  isTranspilingStandard?: boolean;
  showTranspileMenu?: boolean;
  setShowTranspileMenu?: (val: boolean | ((prev: boolean) => boolean)) => void;
  onTranspileStandard?: (formatCode: string) => void;
  onAiSolveBatch?: () => Promise<void>;
  onConfirmImport: () => Promise<void>;
}

export function DocxModalFooter({
  onClose,
  loading,
  isAiSolving,
  isExportingExcel,
  totalQuestions,
  showExportMenu,
  setShowExportMenu,
  onExportExcel,
  customTemplateInputRef,
  onExportCustomTemplate,
  isTranspilingStandard = false,
  showTranspileMenu = false,
  setShowTranspileMenu,
  onTranspileStandard,
  onAiSolveBatch,
  onConfirmImport,
}: DocxModalFooterProps) {
  return (
    <div className="flex flex-wrap gap-4 pt-4 border-t border-slate-800 mt-2 shrink-0 items-center justify-between">
      <button
        type="button"
        onClick={onClose}
        className="px-6 py-3 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white rounded-xl font-bold transition-all text-xs border border-slate-800 cursor-pointer"
      >
        Hủy bỏ
      </button>

      <div className="flex items-center gap-3">
        {/* Nút Xuất Excel Đa Mẫu */}
        <div className="relative">
          <button
            type="button"
            onClick={() => setShowExportMenu(prev => !prev)}
            disabled={loading || isAiSolving || isExportingExcel || totalQuestions === 0}
            className="px-4 py-3 bg-emerald-950/60 hover:bg-emerald-900/80 text-emerald-300 hover:text-emerald-100 border border-emerald-500/40 font-bold rounded-xl transition-all shadow-md active:scale-95 text-xs flex items-center justify-center gap-1.5 cursor-pointer disabled:opacity-50"
            title="Xuất bản danh sách câu hỏi ra bảng tính Excel đa mẫu"
          >
            {isExportingExcel ? (
              <Loader2 size={14} className="animate-spin text-emerald-400" />
            ) : (
              <FileSpreadsheet size={14} className="text-emerald-400" />
            )}
            <span>{isExportingExcel ? 'Đang xuất bản...' : '📊 Xuất Excel'}</span>
          </button>

          {showExportMenu && (
            <div className="absolute bottom-full right-0 mb-2 w-64 bg-slate-900 border border-slate-800 rounded-2xl p-2 shadow-2xl z-50 text-xs space-y-1 animate-in fade-in zoom-in-95 duration-150">
              <div className="px-2.5 py-1 text-[10px] font-black uppercase text-slate-400 tracking-wider">
                Chọn Định Dạng Xuất Bản
              </div>
              <button
                type="button"
                onClick={() => onExportExcel('NATIVE')}
                className="w-full text-left px-2.5 py-2 hover:bg-slate-800 rounded-xl text-slate-200 hover:text-white font-medium flex items-center gap-2 cursor-pointer transition-colors"
              >
                <Download size={13} className="text-emerald-400" />
                <div>
                  <div className="font-bold">Mẫu Đẳng Cấu Aegis (Gốc)</div>
                  <div className="text-[10px] text-slate-400">Đầy đủ 100% cột, toạ độ & lời giải</div>
                </div>
              </button>
              <button
                type="button"
                onClick={() => onExportExcel('AZOTA')}
                className="w-full text-left px-2.5 py-2 hover:bg-slate-800 rounded-xl text-slate-200 hover:text-white font-medium flex items-center gap-2 cursor-pointer transition-colors"
              >
                <Download size={13} className="text-blue-400" />
                <div>
                  <div className="font-bold">Chuẩn Tương Thích Azota</div>
                  <div className="text-[10px] text-slate-400">Tương thích các phần mềm khảo thí</div>
                </div>
              </button>
              <button
                type="button"
                onClick={() => onExportExcel('VIETJACK')}
                className="w-full text-left px-2.5 py-2 hover:bg-slate-800 rounded-xl text-slate-200 hover:text-white font-medium flex items-center gap-2 cursor-pointer transition-colors"
              >
                <Download size={13} className="text-amber-400" />
                <div>
                  <div className="font-bold">Chuẩn Mẫu Ôn Thi Phổ Thông</div>
                  <div className="text-[10px] text-slate-400">Định dạng bảng cột truyền thống</div>
                </div>
              </button>
              <div className="border-t border-slate-800 my-1" />
              <label className="w-full text-left px-2.5 py-2 hover:bg-purple-950/40 rounded-xl text-purple-300 hover:text-purple-100 font-medium flex items-center gap-2 cursor-pointer transition-colors">
                <Upload size={13} className="text-purple-400" />
                <div>
                  <div className="font-bold">Nạp Template Excel Của Bạn</div>
                  <div className="text-[10px] text-slate-400">Điền tự động vào file mẫu tùy ý</div>
                </div>
                <input
                  type="file"
                  ref={customTemplateInputRef}
                  accept=".xlsx,.xls"
                  className="hidden"
                  onChange={e => {
                    const file = e.target.files?.[0];
                    if (file) onExportCustomTemplate(file);
                  }}
                />
              </label>
            </div>
          )}
        </div>

        {/* Nút Xuất Chuẩn Quốc Tế (Moodle XML, Canvas QTI, GIFT) */}
        {onTranspileStandard && (
          <div className="relative">
            <button
              type="button"
              onClick={() => setShowTranspileMenu?.(prev => !prev)}
              disabled={loading || isAiSolving || isTranspilingStandard || totalQuestions === 0}
              className="px-4 py-3 bg-cyan-950/60 hover:bg-cyan-900/80 text-cyan-300 hover:text-cyan-100 border border-cyan-500/40 font-bold rounded-xl transition-all shadow-md active:scale-95 text-xs flex items-center justify-center gap-1.5 cursor-pointer disabled:opacity-50"
              title="Xuất bản đề thi chuẩn quốc tế (Plug-and-play vào Moodle LMS, Canvas, TalentLMS)"
            >
              {isTranspilingStandard ? (
                <Loader2 size={14} className="animate-spin text-cyan-400" />
              ) : (
                <Globe size={14} className="text-cyan-400" />
              )}
              <span>{isTranspilingStandard ? 'Đang chuyển dịch...' : '🌐 Chuẩn Quốc Tế'}</span>
            </button>

            {showTranspileMenu && (
              <div className="absolute bottom-full right-0 mb-2 w-72 bg-slate-900 border border-slate-800 rounded-2xl p-2 shadow-2xl z-50 text-xs space-y-1 animate-in fade-in zoom-in-95 duration-150">
                <div className="px-2.5 py-1 text-[10px] font-black uppercase text-cyan-400 tracking-wider flex items-center gap-1.5">
                  <Globe size={12} />
                  <span>Xuất Bản Chuẩn EdTech Thế Giới</span>
                </div>
                
                <button
                  type="button"
                  onClick={() => onTranspileStandard('MOODLE_XML')}
                  className="w-full text-left px-2.5 py-2 hover:bg-slate-800 rounded-xl text-slate-200 hover:text-white font-medium flex items-center gap-2.5 cursor-pointer transition-colors"
                >
                  <Download size={14} className="text-orange-400 shrink-0" />
                  <div>
                    <div className="font-bold text-orange-300">🎓 Moodle XML (Khuyên dùng)</div>
                    <div className="text-[10px] text-slate-400">Kéo thả 1 chạm vào Moodle (nhúng ảnh Base64 & toán TeX)</div>
                  </div>
                </button>

                <FeatureGate feature={FeatureKey.TranspilerQti} className="w-full">
                  <button
                    type="button"
                    onClick={() => onTranspileStandard('QTI_PACKAGE')}
                    className="w-full text-left px-2.5 py-2 hover:bg-slate-800 rounded-xl text-slate-200 hover:text-white font-medium flex items-center gap-2.5 cursor-pointer transition-colors"
                  >
                    <Download size={14} className="text-blue-400 shrink-0" />
                    <div>
                      <div className="font-bold text-blue-300">📦 Canvas LMS / IMS QTI 2.1 (.zip)</div>
                      <div className="text-[10px] text-slate-400">Nhập vào Canvas LMS, Blackboard, Schoology</div>
                    </div>
                  </button>
                </FeatureGate>

                <FeatureGate feature={FeatureKey.TranspilerGift} className="w-full">
                  <button
                    type="button"
                    onClick={() => onTranspileStandard('MOODLE_GIFT')}
                    className="w-full text-left px-2.5 py-2 hover:bg-slate-800 rounded-xl text-slate-200 hover:text-white font-medium flex items-center gap-2.5 cursor-pointer transition-colors"
                  >
                    <Download size={14} className="text-emerald-400 shrink-0" />
                    <div>
                      <div className="font-bold text-emerald-300">📝 Moodle GIFT Plain Text (.gift.txt)</div>
                      <div className="text-[10px] text-slate-400">Định dạng văn bản gọn nhẹ cho Moodle & TalentLMS</div>
                    </div>
                  </button>
                </FeatureGate>
              </div>
            )}
          </div>
        )}

        {/* Nút Giải Bằng AI */}
        {onAiSolveBatch && (
          <FeatureGate feature={FeatureKey.AiSymposium}>
            <button
              type="button"
              onClick={onAiSolveBatch}
              disabled={loading || isAiSolving || totalQuestions === 0}
              className="px-5 py-3 bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-500 hover:to-indigo-500 text-white font-bold rounded-xl transition-all shadow-md active:scale-95 text-xs flex items-center justify-center gap-2 cursor-pointer disabled:opacity-50"
              title="Sử dụng Gemini AI để tự động suy luận đáp án đúng và trích dẫn quy định"
            >
              {isAiSolving ? (
                <Loader2 size={14} className="animate-spin text-purple-200" />
              ) : (
                <Sparkles size={14} className="text-purple-200" />
              )}
              <span>{isAiSolving ? 'AI đang suy luận...' : '⚡ Giải Đáp Án Bằng AI'}</span>
            </button>
          </FeatureGate>
        )}

        {/* Nút Lưu Chính Thức */}
        <button
          type="button"
          onClick={onConfirmImport}
          disabled={loading || isAiSolving || totalQuestions === 0}
          className="px-6 py-3 bg-gradient-to-r from-blue-600 to-teal-600 hover:from-blue-500 hover:to-teal-500 text-white font-black rounded-xl transition-all shadow-lg shadow-blue-950/60 active:scale-95 text-xs flex items-center justify-center gap-2 cursor-pointer disabled:opacity-50"
        >
          {loading ? (
            <Loader2 size={14} className="animate-spin text-white" />
          ) : (
            <Save size={14} />
          )}
          <span>{loading ? 'Đang lưu vào ngân hàng...' : `Lưu Vào Ngân Hàng (${totalQuestions} câu)`}</span>
        </button>
      </div>
    </div>
  );
}
