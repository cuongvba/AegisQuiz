import React from 'react';
import { FileSpreadsheet, FileUp, Loader2, Tag, X } from 'lucide-react';

interface ExcelPreviewModalProps {
  isOpen: boolean;
  excelPreviewData: any;
  excelSelectedSheets: Set<number>;
  setExcelSelectedSheets: React.Dispatch<React.SetStateAction<Set<number>>>;
  isExcelImporting: boolean;
  onClose: () => void;
  onConfirm: () => void;
}

export const ExcelPreviewModal: React.FC<ExcelPreviewModalProps> = ({
  isOpen,
  excelPreviewData,
  excelSelectedSheets,
  setExcelSelectedSheets,
  isExcelImporting,
  onClose,
  onConfirm,
}) => {
  if (!isOpen || !excelPreviewData) return null;

  const totalSelectedQuestions = excelPreviewData.sheets
    .filter((_: any, i: number) => excelSelectedSheets.has(i))
    .reduce((sum: number, sh: any) => sum + sh.questionCount, 0);

  const toggleSheet = (idx: number) => {
    setExcelSelectedSheets(prev => {
      const next = new Set(prev);
      if (next.has(idx)) next.delete(idx);
      else next.add(idx);
      return next;
    });
  };

  return (
    <div className="fixed inset-0 bg-black/70 backdrop-blur-sm flex items-center justify-center z-50 p-4">
      <div className="bg-slate-900 border border-slate-700/80 rounded-3xl max-w-2xl w-full max-h-[85vh] flex flex-col shadow-2xl animate-in fade-in zoom-in-95">
        {/* Header */}
        <div className="px-6 py-5 border-b border-slate-800 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center text-emerald-400">
              <FileSpreadsheet size={20} />
            </div>
            <div>
              <h3 className="text-base font-bold text-white">Xác nhận nhập từ Excel</h3>
              <p className="text-xs text-slate-400">
                Tìm thấy {excelPreviewData.totalSheets} sheet ({excelPreviewData.sheets.reduce((s: number, x: any) => s + x.questionCount, 0)} câu hỏi)
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="text-slate-400 hover:text-white p-1 rounded-lg cursor-pointer"
          >
            <X size={18} />
          </button>
        </div>

        {/* Global Taxonomy Metadata */}
        {(excelPreviewData.detectedDomainCode || excelPreviewData.detectedAssessmentPurpose) && (
          <div className="px-6 py-3 bg-slate-950/60 border-b border-slate-800/80 text-xs text-slate-300 space-y-1.5">
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
              {excelPreviewData.detectedDomainCode && (
                <div className="flex items-center gap-1.5 truncate">
                  <span className="text-slate-400">Ngành:</span>
                  <strong className="text-emerald-400 font-mono">{excelPreviewData.detectedDomainCode}</strong>
                </div>
              )}
              {excelPreviewData.detectedAssessmentPurpose && (
                <div className="flex items-center gap-1.5 truncate">
                  <span className="text-slate-400">Mục đích:</span>
                  <strong className="text-white">{excelPreviewData.detectedAssessmentPurpose}</strong>
                </div>
              )}
              {excelPreviewData.detectedIssuingOrg && (
                <div className="flex items-center gap-1.5 truncate">
                  <span className="text-slate-400">Đơn vị:</span>
                  <strong className="text-white">{excelPreviewData.detectedIssuingOrg}</strong>
                </div>
              )}
              {excelPreviewData.detectedBenchmarkYear && (
                <div className="flex items-center gap-1.5 truncate">
                  <span className="text-slate-400">Năm:</span>
                  <strong className="text-white">{excelPreviewData.detectedBenchmarkYear}</strong>
                </div>
              )}
            </div>
            {Array.isArray(excelPreviewData.detectedTags) && excelPreviewData.detectedTags.length > 0 && (
              <div className="flex flex-wrap items-center gap-1 pt-1 border-t border-slate-800/60">
                <Tag size={10} className="text-emerald-400 mr-1" />
                {excelPreviewData.detectedTags.map((t: string) => (
                  <span key={t} className="text-[10px] font-bold px-2 py-0.5 rounded-md bg-slate-900 text-emerald-300 border border-emerald-500/20">
                    #{t}
                  </span>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Sheet list */}
        <div className="flex-1 overflow-y-auto px-6 py-4 space-y-3">
          {excelPreviewData.sheets.map((sheet: any, idx: number) => {
            const isSelected = excelSelectedSheets.has(idx);
            return (
              <div
                key={idx}
                onClick={() => toggleSheet(idx)}
                className={`cursor-pointer rounded-2xl border p-4 transition-all duration-200 ${
                  isSelected
                    ? 'border-emerald-500/60 bg-emerald-950/30 shadow-lg shadow-emerald-950/30'
                    : 'border-slate-700/50 bg-slate-900/50 opacity-60 hover:opacity-80'
                }`}
              >
                <div className="flex items-start gap-3">
                  <div className={`mt-0.5 flex-shrink-0 w-5 h-5 rounded-md border-2 flex items-center justify-center ${
                    isSelected ? 'border-emerald-400 bg-emerald-500' : 'border-slate-600 bg-transparent'
                  }`}>
                    {isSelected && <span className="text-white text-[10px] font-black">✓</span>}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap mb-1.5">
                      <span className="text-sm font-bold text-slate-200 truncate">{sheet.sheetName}</span>
                      <span className="px-2 py-0.5 text-[10px] font-bold bg-slate-800 text-slate-400 rounded-full">
                        {sheet.questionCount} câu
                      </span>
                    </div>
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="px-2.5 py-0.5 text-[10px] font-bold bg-emerald-950/60 text-emerald-300 border border-emerald-500/30 rounded-full truncate max-w-[280px]">
                        🏷 {sheet.detectedTopicCode}
                      </span>
                      <span className="text-[11px] text-slate-400 truncate">{sheet.detectedTopicName}</span>
                    </div>
                    {sheet.questions.length > 0 && (
                      <div className="mt-2 space-y-1">
                        {sheet.questions.slice(0, 2).map((q: any, qi: number) => (
                          <p key={qi} className="text-[11px] text-slate-400 truncate pl-2 border-l-2 border-slate-700">
                            {qi + 1}. {q.content}
                          </p>
                        ))}
                        {sheet.questionCount > 2 && (
                          <p className="text-[10px] text-slate-500 pl-2">...và {sheet.questionCount - 2} câu hỏi khác</p>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>

        {/* Footer actions */}
        <div className="px-6 py-4 border-t border-slate-800 flex items-center justify-between gap-3">
          <div className="text-xs text-slate-400">
            Đã chọn: <strong className="text-emerald-400">{totalSelectedQuestions}</strong> câu hỏi
          </div>
          <div className="flex gap-2">
            <button
              onClick={onClose}
              className="px-4 py-2 text-sm font-medium text-slate-400 hover:text-white bg-slate-800 hover:bg-slate-700 rounded-xl transition-colors cursor-pointer"
            >
              Hủy
            </button>
            <button
              onClick={onConfirm}
              disabled={isExcelImporting || excelSelectedSheets.size === 0}
              className="px-5 py-2 text-sm font-bold text-slate-900 bg-gradient-to-r from-emerald-400 to-teal-400 hover:from-emerald-300 hover:to-teal-300 rounded-xl transition-all duration-200 disabled:opacity-50 cursor-pointer flex items-center gap-2"
            >
              {isExcelImporting ? (
                <><Loader2 size={14} className="animate-spin" /> Đang lưu...</>
              ) : (
                <><FileUp size={14} /> Xác nhận nhập ({excelSelectedSheets.size} sheet)</>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
