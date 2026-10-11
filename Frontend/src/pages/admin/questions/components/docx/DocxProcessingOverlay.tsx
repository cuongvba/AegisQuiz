import { ProcessingStopwatch } from './ProcessingStopwatch';

interface DocxProcessingOverlayProps {
  isProcessing: boolean;
  isAiSolving?: boolean;
  isConvertingLatex?: boolean;
  processingFileName?: string;
}

export function DocxProcessingOverlay({
  isProcessing,
  isAiSolving,
  isConvertingLatex,
  processingFileName,
}: DocxProcessingOverlayProps) {
  const isPdf = processingFileName?.toLowerCase().endsWith('.pdf');
  const isDocx = processingFileName?.toLowerCase().endsWith('.docx');
  const fileIcon = isPdf ? '📄' : isDocx ? '📝' : '📂';
  const fileTypeLabel = isPdf ? 'PDF' : isDocx ? 'Word (DOCX)' : 'tài liệu';

  return (
    <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-md z-50 flex flex-col items-center justify-center space-y-4 text-white">
      <div className="animate-spin text-5xl mb-2 text-purple-400">🌀</div>
      <h3 className="text-lg font-bold text-slate-200 text-center flex items-center justify-center gap-2 flex-wrap">
        <span>
          {isConvertingLatex
            ? 'Gemini Vision đang OCR & chuyển đổi công thức toán sang mã LaTeX ($...$)...'
            : isAiSolving
            ? 'Gemini AI đang giải đáp án & trích dẫn quy định...'
            : `Đang đọc và phân tích tệp ${fileTypeLabel}...`}
        </span>
        <span className="text-slate-400">:</span>
        <ProcessingStopwatch />
      </h3>
      {isProcessing && processingFileName && (
        <div className="flex items-center gap-2 bg-slate-800/80 border border-slate-700 rounded-xl px-4 py-2 text-sm">
          <span className="text-2xl">{fileIcon}</span>
          <div className="text-left">
            <p className="text-slate-300 font-semibold">{processingFileName}</p>
            <p className="text-slate-500 text-xs">
              {isPdf
                ? 'Đang giải mã cấu trúc PDF, trích xuất nội dung văn bản...'
                : isDocx
                ? 'Đang phân tích cấu trúc DOCX, nhận dạng câu hỏi...'
                : 'Đang xử lý tài liệu...'}
            </p>
          </div>
        </div>
      )}
      <p className="text-xs text-slate-500 max-w-sm text-center leading-relaxed">
        Quá trình có thể mất từ 5-15 giây tùy số lượng câu hỏi và độ phức tạp của công thức. Vui lòng giữ trình duyệt mở!
      </p>
    </div>
  );
}
