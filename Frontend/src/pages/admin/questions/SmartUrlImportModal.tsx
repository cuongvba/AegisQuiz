import React, { useState } from 'react';
import {
  Globe, FileText, FileSpreadsheet, Sparkles, AlertCircle,
  CheckCircle2, RefreshCw, X, ExternalLink, Copy, HelpCircle,
  Lock, ArrowRight, ShieldCheck, Download
} from 'lucide-react';
import { learnerQuizService } from '@/services/learner-quiz.service';

interface SmartUrlImportModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccessDocxOrPdf: (questions: any[], fileName: string) => void;
  onSuccessExcel: (excelData: any) => void;
}

export const SmartUrlImportModal: React.FC<SmartUrlImportModalProps> = ({
  isOpen,
  onClose,
  onSuccessDocxOrPdf,
  onSuccessExcel
}) => {
  const [url, setUrl] = useState('');
  const [useAi, setUseAi] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [loadingStep, setLoadingStep] = useState<string>('');
  const [errorInfo, setErrorInfo] = useState<{
    message: string;
    isRestricted?: boolean;
    suggestedAction?: string;
  } | null>(null);

  if (!isOpen) return null;

  // Real-time Provider & Format Detector
  const detectUrlType = (inputUrl: string) => {
    const trimmed = inputUrl.trim().toLowerCase();
    if (!trimmed) return null;

    if (trimmed.includes('docs.google.com/document')) {
      return {
        type: 'GDOCS',
        label: 'Google Docs',
        icon: FileText,
        color: 'bg-blue-500/10 text-blue-400 border-blue-500/30',
        desc: 'Tự động xuất nhị phân DOCX & trích xuất công thức Toán, hình ảnh nhúng'
      };
    }
    if (trimmed.includes('docs.google.com/spreadsheets')) {
      return {
        type: 'GSHEETS',
        label: 'Google Sheets',
        icon: FileSpreadsheet,
        color: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30',
        desc: 'Tự động xuất bảng tính XLSX & bóc tách các sheet đề thi DOT-2026'
      };
    }
    if (trimmed.includes('drive.google.com')) {
      return {
        type: 'GDRIVE',
        label: 'Google Drive File',
        icon: Globe,
        color: 'bg-amber-500/10 text-amber-400 border-amber-500/30',
        desc: 'Tệp lưu trữ đám mây Google Drive'
      };
    }
    if (trimmed.includes('1drv.ms') || trimmed.includes('sharepoint.com')) {
      return {
        type: 'ONEDRIVE',
        label: 'Microsoft OneDrive / SharePoint',
        icon: Globe,
        color: 'bg-cyan-500/10 text-cyan-400 border-cyan-500/30',
        desc: 'Tài liệu Office 365 / OneDrive trực tuyến'
      };
    }
    if (trimmed.endsWith('.docx')) {
      return {
        type: 'DOCX',
        label: 'Direct Word (.docx)',
        icon: FileText,
        color: 'bg-indigo-500/10 text-indigo-400 border-indigo-500/30',
        desc: 'Đường dẫn tệp Word trực tiếp'
      };
    }
    if (trimmed.endsWith('.xlsx') || trimmed.endsWith('.xls')) {
      return {
        type: 'EXCEL',
        label: 'Direct Excel (.xlsx)',
        icon: FileSpreadsheet,
        color: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30',
        desc: 'Đường dẫn bảng tính Excel trực tiếp'
      };
    }
    if (trimmed.endsWith('.pdf')) {
      return {
        type: 'PDF',
        label: 'Direct PDF (.pdf)',
        icon: FileText,
        color: 'bg-rose-500/10 text-rose-400 border-rose-500/30',
        desc: 'Đường dẫn tài liệu PDF trực tiếp'
      };
    }
    return {
      type: 'WEB',
      label: 'Web Link / Cloud Document',
      icon: Globe,
      color: 'bg-purple-500/10 text-purple-400 border-purple-500/30',
      desc: 'Tài liệu trực tuyến sẽ được phân tích tự động'
    };
  };

  const detectedInfo = detectUrlType(url);

  const handlePasteClipboard = async () => {
    try {
      const text = await navigator.clipboard.readText();
      if (text) {
        setUrl(text.trim());
        setErrorInfo(null);
      }
    } catch {
      // Fallback
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!url.trim()) return;

    setIsLoading(true);
    setErrorInfo(null);
    setLoadingStep('Đang kết nối & tải luồng tài liệu từ URL...');

    try {
      setLoadingStep('Đang chuyển đổi định dạng nhị phân & bóc tách cấu trúc câu hỏi...');
      const res = await learnerQuizService.importQuestionsFromUrl(url.trim(), useAi);

      // Phân luồng dữ liệu trả về theo loại
      if (res.sourceType === 'EXCEL') {
        onSuccessExcel(res);
        onClose();
      } else {
        const questionsList = res.questions || (Array.isArray(res) ? res : []);
        if (!questionsList || questionsList.length === 0) {
          setErrorInfo({
            message: 'Không tìm thấy câu hỏi hợp lệ trong tài liệu được tải về. Vui lòng kiểm tra định dạng đề thi.'
          });
          return;
        }
        onSuccessDocxOrPdf(questionsList, res.fileName || 'Google_Docs_Import.docx');
        onClose();
      }
    } catch (err: any) {
      console.error('[Import URL Error]', err);
      const data = err.response?.data;
      setErrorInfo({
        message: data?.message || err.message || 'Không thể nhập câu hỏi từ đường dẫn này.',
        isRestricted: data?.isRestricted || false,
        suggestedAction: data?.suggestedAction
      });
    } finally {
      setIsLoading(false);
      setLoadingStep('');
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/85 backdrop-blur-md animate-fadeIn">
      <div className="relative w-full max-w-2xl bg-slate-900 border border-slate-700/80 rounded-3xl shadow-2xl p-6 sm:p-8 text-white max-h-[90vh] overflow-y-auto">
        {/* Nút đóng */}
        <button
          type="button"
          onClick={onClose}
          disabled={isLoading}
          className="absolute top-6 right-6 text-slate-400 hover:text-white p-1 rounded-lg transition-colors cursor-pointer disabled:opacity-50"
        >
          <X size={20} />
        </button>

        <div className="space-y-6">
          {/* Header */}
          <div className="space-y-1.5">
            <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-indigo-500/10 text-indigo-400 border border-indigo-500/20">
              <Globe size={13} />
              UNIVERSAL SMART INGESTION
            </div>
            <h2 className="text-xl sm:text-2xl font-black text-white tracking-tight flex items-center gap-2.5">
              <span>Nhập Đề Thi Từ Liên Kết (URI / URL)</span>
            </h2>
            <p className="text-xs text-slate-400 leading-relaxed">
              Hỗ trợ dán đường link tài liệu trực tiếp từ <strong>Google Docs</strong>, <strong>Google Sheets</strong>, <strong>OneDrive</strong>, <strong>Dropbox</strong> hoặc bất kỳ liên kết tệp trực tuyến nào.
            </p>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-5">
            {/* Input Group */}
            <div className="space-y-2">
              <div className="flex items-center justify-between text-xs font-bold text-slate-300">
                <label htmlFor="documentUrl" className="uppercase tracking-wider">
                  Đường dẫn tài liệu (URI / URL)
                </label>
                <button
                  type="button"
                  onClick={handlePasteClipboard}
                  className="text-indigo-400 hover:text-indigo-300 flex items-center gap-1 font-medium transition-colors cursor-pointer"
                >
                  <Copy size={12} />
                  <span>Dán từ khay nhớ tạm</span>
                </button>
              </div>

              <div className="relative">
                <input
                  id="documentUrl"
                  type="url"
                  required
                  value={url}
                  onChange={(e) => { setUrl(e.target.value); setErrorInfo(null); }}
                  placeholder="https://docs.google.com/document/d/... hoặc https://.../DeThi.docx"
                  disabled={isLoading}
                  className="w-full bg-slate-950 border border-slate-700 focus:border-indigo-500 rounded-2xl px-4 py-3.5 text-sm text-white focus:outline-none focus:ring-2 focus:ring-indigo-500/30 font-mono transition-all pr-24 placeholder:text-slate-600"
                  autoFocus
                />
                {url && (
                  <button
                    type="button"
                    onClick={() => { setUrl(''); setErrorInfo(null); }}
                    className="absolute right-3 top-3.5 text-slate-500 hover:text-slate-300 text-xs px-2 py-1 rounded-md bg-slate-900 border border-slate-800"
                  >
                    Xóa
                  </button>
                )}
              </div>

              {/* Detected Provider Badge */}
              {detectedInfo && (
                <div className={`p-3 rounded-xl border text-xs flex items-center gap-3 transition-all ${detectedInfo.color}`}>
                  <detectedInfo.icon size={18} className="shrink-0" />
                  <div className="flex-1 min-w-0">
                    <span className="font-bold block">{detectedInfo.label}</span>
                    <span className="text-[11px] opacity-80 block truncate">{detectedInfo.desc}</span>
                  </div>
                  <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-black/20 uppercase font-bold shrink-0">
                    Sẵn sàng
                  </span>
                </div>
              )}
            </div>

            {/* AI Toggle */}
            <div className="p-4 rounded-2xl bg-slate-950/70 border border-slate-800 flex items-center justify-between gap-4">
              <div className="space-y-0.5">
                <div className="text-xs font-bold text-slate-200 flex items-center gap-1.5">
                  <Sparkles size={14} className="text-amber-400" />
                  <span>Kích hoạt Gemini AI tự động giải & lập barem chấm</span>
                </div>
                <p className="text-[11px] text-slate-400">
                  AI sẽ tự động phát hiện đáp án đúng nếu đề chưa tô đậm, tạo lời giải thích chi tiết và phân loại chuẩn kiến thức.
                </p>
              </div>
              <label className="relative inline-flex items-center cursor-pointer shrink-0">
                <input
                  type="checkbox"
                  checked={useAi}
                  onChange={(e) => setUseAi(e.target.checked)}
                  disabled={isLoading}
                  className="sr-only peer"
                />
                <div className="w-11 h-6 bg-slate-800 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-indigo-600"></div>
              </label>
            </div>

            {/* Quick Presets / Examples */}
            <div className="space-y-1.5">
              <span className="text-[11px] text-slate-400 font-semibold block">Hoặc thử nhanh với các mẫu:</span>
              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  onClick={() => {
                    setUrl('https://docs.google.com/document/d/1y1mnFE_Zq-vS4O3z6hcV0eDmzHy9_bYf/edit');
                    setErrorInfo(null);
                  }}
                  className="px-3 py-1.5 rounded-lg bg-slate-800/80 hover:bg-slate-800 text-slate-300 hover:text-white text-xs border border-slate-700/60 transition-colors flex items-center gap-1.5 cursor-pointer"
                >
                  <FileText size={13} className="text-blue-400" />
                  <span>Đề Vật Lý Thanh Hóa (Google Docs)</span>
                </button>
              </div>
            </div>

            {/* Error & Permission Notification Card */}
            {errorInfo && (
              <div className="p-4 rounded-2xl bg-rose-500/10 border border-rose-500/30 text-rose-300 space-y-3 animate-fadeIn">
                <div className="flex items-start gap-3">
                  {errorInfo.isRestricted ? (
                    <Lock size={20} className="text-amber-400 shrink-0 mt-0.5" />
                  ) : (
                    <AlertCircle size={20} className="text-rose-400 shrink-0 mt-0.5" />
                  )}
                  <div className="space-y-1 flex-1">
                    <h4 className="text-xs font-bold text-white uppercase tracking-wider">
                      {errorInfo.isRestricted ? 'Tài liệu Google Docs bị hạn chế quyền truy cập' : 'Lỗi khi nạp tài liệu'}
                    </h4>
                    <p className="text-xs leading-relaxed text-rose-200">{errorInfo.message}</p>
                  </div>
                </div>

                {/* Specific Guided Steps for Google Docs Permissions */}
                {errorInfo.isRestricted && (
                  <div className="p-3.5 rounded-xl bg-slate-950/80 border border-amber-500/20 text-xs text-slate-300 space-y-2">
                    <div className="font-bold text-amber-300 flex items-center gap-1.5">
                      <HelpCircle size={14} />
                      <span>Cách xử lý nhanh để nạp tự động:</span>
                    </div>
                    <ol className="list-decimal list-inside space-y-1 text-slate-400 text-[11px] leading-relaxed">
                      <li>Mở tài liệu trên Google Docs bằng tài khoản chủ sở hữu.</li>
                      <li>Bấm nút <strong>Chia sẻ (Share)</strong> ở góc phải phía trên màn hình.</li>
                      <li>Tại mục Quyền truy cập chung: Chọn <strong>"Bất kỳ ai có đường liên kết (Anyone with link) - Người xem"</strong>.</li>
                      <li>Bấm vào biểu tượng ⚙️ (Cài đặt) ở góc trên hộp thoại chia sẻ, bỏ chọn mục <em>"Người xem và người nhận xét có thể thấy tùy chọn tải xuống"</em>.</li>
                    </ol>
                    <div className="pt-2 flex flex-wrap items-center gap-3 border-t border-slate-800">
                      <a
                        href={url}
                        target="_blank"
                        rel="noreferrer"
                        className="inline-flex items-center gap-1.5 text-[11px] font-bold text-indigo-400 hover:text-indigo-300"
                      >
                        <ExternalLink size={12} />
                        Mở Google Docs để kiểm tra
                      </a>
                      <span className="text-slate-600">|</span>
                      <span className="text-[11px] text-slate-400">
                        Hoặc bấm <strong>Tệp → Tải xuống → Microsoft Word (.docx)</strong> rồi kéo thả vào đây.
                      </span>
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* Loading Progress */}
            {isLoading && (
              <div className="p-4 rounded-2xl bg-indigo-950/40 border border-indigo-500/30 text-xs text-indigo-200 flex items-center gap-3 animate-pulse">
                <RefreshCw size={18} className="animate-spin text-indigo-400 shrink-0" />
                <div className="space-y-0.5">
                  <span className="font-bold block">{loadingStep}</span>
                  <span className="text-[11px] text-indigo-300/80 block">Quá trình này có thể mất từ 3 đến 15 giây tùy kích thước đề thi...</span>
                </div>
              </div>
            )}

            {/* Submit Buttons */}
            <div className="pt-2 flex items-center justify-end gap-3">
              <button
                type="button"
                onClick={onClose}
                disabled={isLoading}
                className="px-5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-bold transition-colors cursor-pointer disabled:opacity-50"
              >
                Hủy bỏ
              </button>
              <button
                type="submit"
                disabled={isLoading || !url.trim()}
                className="px-6 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 via-indigo-600 to-purple-600 hover:from-blue-500 hover:to-purple-500 text-white text-xs font-bold shadow-lg shadow-indigo-600/30 transition-all flex items-center gap-2 cursor-pointer disabled:opacity-50"
              >
                {isLoading ? <RefreshCw size={15} className="animate-spin" /> : <ArrowRight size={15} />}
                <span>{isLoading ? 'Đang phân tích đề thi...' : 'Bắt Đầu Bóc Tách Đề Thi'}</span>
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};
