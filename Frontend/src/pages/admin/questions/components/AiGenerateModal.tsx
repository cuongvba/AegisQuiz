import { useState, useRef } from 'react';
import { X, Sparkles, Upload, FileText, Wand2, BookOpen, Layers, CheckCircle2, AlertCircle, Sliders, Brain } from 'lucide-react';
import { learnerQuizService } from '@/services/learner-quiz.service';
import type { BankTopic } from '../types';

interface AiGenerateModalProps {
  isOpen: boolean;
  topics: BankTopic[];
  onClose: () => void;
  onSuccess: (questions: any[]) => void;
}

export function AiGenerateModal({ isOpen, topics, onClose, onSuccess }: AiGenerateModalProps) {
  const [activeTab, setActiveTab] = useState<'file' | 'text'>('file');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [rawText, setRawText] = useState('');
  const [questionCount, setQuestionCount] = useState(5);
  const [questionType, setQuestionType] = useState('ALL');
  const [difficulty, setDifficulty] = useState(3);
  const [topicCode, setTopicCode] = useState(topics.length > 0 ? topics[0].code : 'GENERAL');
  const [focusArea, setFocusArea] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [dragActive, setDragActive] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  if (!isOpen) return null;

  const handleFileDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragActive(false);
    if (e.dataTransfer.files?.[0]) {
      const file = e.dataTransfer.files[0];
      const ext = file.name.toLowerCase();
      if (ext.endsWith('.pdf') || ext.endsWith('.docx') || ext.endsWith('.txt')) {
        setSelectedFile(file);
        setErrorMessage(null);
      } else {
        setErrorMessage('Chỉ hỗ trợ tệp định dạng .PDF, .DOCX hoặc .TXT');
      }
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files?.[0]) {
      const file = e.target.files[0];
      const ext = file.name.toLowerCase();
      if (ext.endsWith('.pdf') || ext.endsWith('.docx') || ext.endsWith('.txt')) {
        setSelectedFile(file);
        setErrorMessage(null);
      } else {
        setErrorMessage('Chỉ hỗ trợ tệp định dạng .PDF, .DOCX hoặc .TXT');
      }
    }
  };

  const handleStartGeneration = async () => {
    setErrorMessage(null);

    if (activeTab === 'file' && !selectedFile) {
      setErrorMessage('Vui lòng chọn tệp tài liệu PDF hoặc Word để phân tích.');
      return;
    }

    if (activeTab === 'text' && (!rawText || rawText.trim().length < 30)) {
      setErrorMessage('Vui lòng nhập đoạn văn bản tài liệu tối thiểu 30 ký tự.');
      return;
    }

    setIsGenerating(true);

    try {
      let result: any[] = [];
      if (activeTab === 'file' && selectedFile) {
        result = await learnerQuizService.generateQuestionsFromDoc(selectedFile, {
          count: questionCount,
          type: questionType,
          difficulty: difficulty,
          topic: topicCode,
          focusArea: focusArea.trim() || undefined,
        });
      } else {
        result = await learnerQuizService.generateQuestionsFromText({
          documentText: rawText,
          questionCount: questionCount,
          questionType: questionType,
          difficulty: difficulty,
          topicCode: topicCode,
          focusArea: focusArea.trim() || undefined,
        });
      }

      if (!result || result.length === 0) {
        throw new Error('AI không tạo được câu hỏi từ nội dung này. Vui lòng kiểm tra lại tài liệu.');
      }

      onSuccess(result);
      onClose();
    } catch (err: any) {
      console.error(err);
      setErrorMessage(
        err.response?.data?.message || err.message || 'Lỗi trong quá trình sinh câu hỏi bằng AI. Vui lòng thử lại.'
      );
    } finally {
      setIsGenerating(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-slate-950/85 backdrop-blur-md z-50 flex items-center justify-center p-4">
      <div className="bg-slate-900 border border-purple-500/30 w-full max-w-3xl rounded-3xl shadow-2xl shadow-purple-950/40 overflow-hidden flex flex-col max-h-[90vh] animate-in fade-in zoom-in-95 duration-200">
        
        {/* MODAL HEADER */}
        <div className="px-6 py-5 border-b border-slate-800 bg-slate-950/50 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-gradient-to-tr from-purple-600 to-blue-500 flex items-center justify-center text-white shadow-lg shadow-purple-500/20">
              <Brain size={20} />
            </div>
            <div>
              <h2 className="text-lg font-black text-white flex items-center gap-2">
                Tự động tạo câu hỏi bằng AI từ tài liệu
                <span className="text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded-full bg-purple-950/80 text-purple-300 border border-purple-500/30">
                  Kịch bản B
                </span>
              </h2>
              <p className="text-xs text-slate-400">
                Phân tích sâu văn bản PDF/Word, nhận diện kiến thức trọng tâm & tạo câu hỏi kèm căn cứ trích dẫn
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            disabled={isGenerating}
            className="text-slate-400 hover:text-white p-1.5 hover:bg-slate-800 rounded-xl transition-all cursor-pointer"
          >
            <X size={20} />
          </button>
        </div>

        {/* MODAL BODY */}
        <div className="p-6 overflow-y-auto space-y-6 text-xs flex-1">
          {errorMessage && (
            <div className="p-3.5 rounded-2xl bg-red-950/40 border border-red-500/30 text-red-300 flex items-start gap-2.5">
              <AlertCircle size={16} className="shrink-0 mt-0.5 text-red-400" />
              <div>
                <p className="font-bold">Không thể thực hiện</p>
                <p className="text-[11px] text-red-400 mt-0.5 leading-relaxed">{errorMessage}</p>
              </div>
            </div>
          )}

          {/* TAB SWITCHER: FILE vs TEXT */}
          <div className="flex bg-slate-950 p-1 rounded-2xl border border-slate-800">
            <button
              type="button"
              onClick={() => { setActiveTab('file'); setErrorMessage(null); }}
              className={`flex-1 py-2.5 rounded-xl font-bold transition-all flex items-center justify-center gap-2 ${
                activeTab === 'file'
                  ? 'bg-purple-600 text-white shadow-md'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <Upload size={14} /> Tải tệp tài liệu (PDF / Word)
            </button>
            <button
              type="button"
              onClick={() => { setActiveTab('text'); setErrorMessage(null); }}
              className={`flex-1 py-2.5 rounded-xl font-bold transition-all flex items-center justify-center gap-2 ${
                activeTab === 'text'
                  ? 'bg-purple-600 text-white shadow-md'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <FileText size={14} /> Dán văn bản trực tiếp
            </button>
          </div>

          {/* TAB 1: FILE UPLOAD */}
          {activeTab === 'file' && (
            <div
              onDragOver={(e) => { e.preventDefault(); setDragActive(true); }}
              onDragLeave={() => setDragActive(false)}
              onDrop={handleFileDrop}
              onClick={() => fileInputRef.current?.click()}
              className={`border-2 border-dashed rounded-3xl p-6 text-center cursor-pointer transition-all ${
                dragActive
                  ? 'border-purple-500 bg-purple-950/20'
                  : selectedFile
                  ? 'border-emerald-500/50 bg-emerald-950/10'
                  : 'border-slate-800 hover:border-slate-700 bg-slate-950/40 hover:bg-slate-950/70'
              }`}
            >
              <input
                ref={fileInputRef}
                type="file"
                accept=".pdf,.docx,.txt"
                onChange={handleFileSelect}
                className="hidden"
              />
              {selectedFile ? (
                <div className="space-y-2">
                  <div className="w-12 h-12 rounded-2xl bg-emerald-950 border border-emerald-500/40 text-emerald-400 flex items-center justify-center mx-auto text-xl font-bold shadow-lg">
                    ✓
                  </div>
                  <p className="font-bold text-slate-200 text-sm">{selectedFile.name}</p>
                  <p className="text-[10px] text-slate-500">
                    {(selectedFile.size / 1024).toFixed(1)} KB · Nhấp vào để đổi tệp khác
                  </p>
                </div>
              ) : (
                <div className="space-y-2">
                  <div className="w-12 h-12 rounded-2xl bg-purple-950/60 border border-purple-500/30 text-purple-300 flex items-center justify-center mx-auto shadow-sm">
                    <Upload size={22} />
                  </div>
                  <p className="font-bold text-slate-300 text-sm">
                    Kéo thả tệp PDF hoặc Word vào đây, hoặc <span className="text-purple-400 underline">chọn tệp</span>
                  </p>
                  <p className="text-[11px] text-slate-500">
                    Hỗ trợ tài liệu giáo trình, thông tư quy chế, quy trình nghiệp vụ (.pdf, .docx, .txt)
                  </p>
                </div>
              )}
            </div>
          )}

          {/* TAB 2: RAW TEXT */}
          {activeTab === 'text' && (
            <div className="space-y-1.5">
              <label className="text-[11px] font-bold text-slate-400 uppercase tracking-wider">
                Nội dung tài liệu / quy trình nghiệp vụ:
              </label>
              <textarea
                value={rawText}
                onChange={(e) => setRawText(e.target.value)}
                placeholder="Dán đoạn văn bản quy định, chính sách, tài liệu đào tạo cần AI phân tích và sinh câu hỏi..."
                rows={6}
                className="w-full bg-slate-950 border border-slate-800 rounded-2xl p-3.5 text-slate-200 placeholder-slate-600 focus:outline-none focus:border-purple-500 transition-colors font-sans text-xs leading-relaxed"
              />
              <p className="text-[10px] text-slate-500 text-right">
                {rawText.length.toLocaleString('vi-VN')} ký tự
              </p>
            </div>
          )}

          {/* CONFIGURATION GRID */}
          <div className="bg-slate-950/60 p-4 rounded-3xl border border-slate-800/80 space-y-4">
            <h3 className="text-xs font-bold text-purple-300 flex items-center gap-1.5 uppercase tracking-wider">
              <Sliders size={13} /> Tham số cấu hình câu hỏi sinh ra
            </h3>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-3.5">
              {/* Số lượng câu hỏi */}
              <div className="space-y-1.5">
                <label className="text-[10px] font-bold text-slate-400 uppercase">
                  Số lượng câu hỏi: <span className="text-purple-400 font-bold">{questionCount} câu</span>
                </label>
                <div className="flex gap-1.5">
                  {[3, 5, 10, 15, 20].map((num) => (
                    <button
                      key={num}
                      type="button"
                      onClick={() => setQuestionCount(num)}
                      className={`flex-1 py-1.5 rounded-lg font-bold text-[11px] transition-all cursor-pointer ${
                        questionCount === num
                          ? 'bg-purple-600 text-white shadow-sm'
                          : 'bg-slate-900 border border-slate-800 text-slate-400 hover:text-white'
                      }`}
                    >
                      {num}
                    </button>
                  ))}
                </div>
              </div>

              {/* Loại câu hỏi */}
              <div className="space-y-1.5">
                <label className="text-[10px] font-bold text-slate-400 uppercase">Loại câu hỏi:</label>
                <select
                  value={questionType}
                  onChange={(e) => setQuestionType(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-slate-200 font-medium focus:outline-none focus:border-purple-500"
                >
                  <option value="ALL">Đa dạng kết hợp (Khuyên dùng)</option>
                  <option value="SINGLE">Trắc nghiệm 1 đáp án (SINGLE)</option>
                  <option value="MULTI">Nhiều đáp án đúng (MULTI)</option>
                  <option value="TRUE_FALSE">Đúng / Sai (TRUE_FALSE)</option>
                  <option value="ESSAY">Tự luận tình huống (ESSAY)</option>
                </select>
              </div>

              {/* Mức độ khó */}
              <div className="space-y-1.5">
                <label className="text-[10px] font-bold text-slate-400 uppercase">Độ khó mục tiêu:</label>
                <select
                  value={difficulty}
                  onChange={(e) => setDifficulty(Number(e.target.value))}
                  className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-slate-200 font-medium focus:outline-none focus:border-purple-500"
                >
                  <option value={1}>Cấp 1 — Nhận biết (Cơ bản)</option>
                  <option value={2}>Cấp 2 — Thông hiểu (Tiêu chuẩn)</option>
                  <option value={3}>Cấp 3 — Vận dụng (Nghiệp vụ)</option>
                  <option value={4}>Cấp 4 — Vận dụng cao (Xử lý tình huống)</option>
                  <option value={5}>Cấp 5 — Chuyên gia / Giám sát</option>
                </select>
              </div>
            </div>

            {/* Chủ đề & Trọng tâm */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5 pt-1">
              <div className="space-y-1.5">
                <label className="text-[10px] font-bold text-slate-400 uppercase">Chủ đề ngân hàng:</label>
                <select
                  value={topicCode}
                  onChange={(e) => setTopicCode(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-slate-200 font-medium focus:outline-none focus:border-purple-500"
                >
                  {topics.map((t) => (
                    <option key={t.id} value={t.code}>
                      [{t.code}] {t.name}
                    </option>
                  ))}
                  <option value="GENERAL">Chung / Tài liệu tổng hợp</option>
                </select>
              </div>

              <div className="space-y-1.5">
                <label className="text-[10px] font-bold text-slate-400 uppercase">
                  Trọng tâm bổ sung (Tuỳ chọn):
                </label>
                <input
                  type="text"
                  value={focusArea}
                  onChange={(e) => setFocusArea(e.target.value)}
                  placeholder="Ví dụ: Tập trung vào điều kiện giải ngân và xử phạt..."
                  className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-slate-200 placeholder-slate-600 focus:outline-none focus:border-purple-500"
                />
              </div>
            </div>
          </div>
        </div>

        {/* MODAL FOOTER */}
        <div className="px-6 py-4 border-t border-slate-800 bg-slate-950/70 flex items-center justify-between">
          <p className="text-[11px] text-slate-500 flex items-center gap-1.5">
            <Sparkles size={13} className="text-purple-400" />
            Sử dụng Google Gemini 1.5 Flash · Phân tích ngữ cảnh & trích dẫn điều khoản
          </p>

          <div className="flex items-center gap-2.5">
            <button
              type="button"
              onClick={onClose}
              disabled={isGenerating}
              className="px-4 py-2 rounded-xl text-slate-400 hover:text-white font-bold transition-all text-xs cursor-pointer"
            >
              Hủy
            </button>

            <button
              type="button"
              onClick={handleStartGeneration}
              disabled={isGenerating}
              className="px-5 py-2.5 bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-500 hover:to-blue-500 text-white font-bold rounded-xl shadow-lg shadow-purple-500/20 text-xs flex items-center gap-2 transition-all active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
            >
              {isGenerating ? (
                <>
                  <span className="animate-spin text-sm">🌀</span>
                  Đang phân tích & tạo câu hỏi...
                </>
              ) : (
                <>
                  <Wand2 size={14} />
                  Bắt đầu sinh câu hỏi bằng AI
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
export default AiGenerateModal;
