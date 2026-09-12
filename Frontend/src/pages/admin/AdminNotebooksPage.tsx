import { useState } from 'react';
import { FileText, Brain, Sparkles, BookOpen, Layers, ListTodo, Loader2 } from 'lucide-react';

export function AdminNotebooksPage() {
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState(0);

  const mockNotebooks = [
    { id: 1, title: 'Nghiệp vụ Ngân hàng Cơ bản', pages: 156, chapters: 8, questions: 120, status: 'Ready' },
    { id: 2, title: 'Quy trình Tín dụng Agribank', pages: 89, chapters: 5, questions: 75, status: 'Ready' },
    { id: 3, title: 'An toàn Thông tin ISO 27001', pages: 234, chapters: 12, questions: 0, status: 'Processing' },
  ];

  const handleUpload = () => {
    setUploading(true);
    setProgress(0);
    let currentProgress = 0;
    const interval = setInterval(() => {
      currentProgress += 25;
      setProgress(currentProgress);
      if (currentProgress >= 100) { 
        clearInterval(interval); 
        setTimeout(() => setUploading(false), 500); 
      }
    }, 1200);
  };

  const getStatusText = (prog: number) => {
    if (prog < 25) return 'Đang đọc cấu trúc tập tin PDF...';
    if (prog < 50) return 'AI đang bóc tách phân tích nội dung học thuật...';
    if (prog < 75) return 'Đang phân nhỏ và cấu trúc hóa giáo trình...';
    if (prog < 100) return 'Đang tự động sinh câu hỏi thi trắc nghiệm...';
    return '✅ Hoàn tất bóc tách dữ liệu giáo án!';
  };

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400 tracking-tight">
          AegisNotebook (PDF to Study Plan & Quiz)
        </h1>
        <p className="text-sm text-slate-400 mt-1 font-medium">Biến các tài liệu định dạng PDF thô thành giáo án và đề thi trắc nghiệm trong tích tắc qua AI.</p>
      </div>

      {/* UPLOAD ZONE */}
      <div className="bg-gradient-to-br from-purple-900/10 to-indigo-900/5 rounded-3xl border border-purple-500/20 p-8 md:p-10 relative overflow-hidden backdrop-blur-md">
        <div className="absolute top-0 right-0 w-32 h-32 bg-purple-500/10 rounded-full blur-3xl pointer-events-none" />
        <div className="text-center space-y-4">
          <div className="w-16 h-16 bg-purple-950/40 border border-purple-500/20 text-purple-400 rounded-full flex items-center justify-center text-3xl mx-auto">
            <Brain />
          </div>
          <div className="space-y-2">
            <h2 className="text-xl font-bold text-slate-200">Tải giáo án PDF của bạn lên đây</h2>
            <p className="text-xs text-slate-400 max-w-md mx-auto leading-relaxed">
              Trí tuệ nhân tạo **Google Gemini 1.5 Pro** sẽ tự động trích xuất nội dung, phân tích cấu trúc, chia chương mục, tóm tắt và sinh bộ câu hỏi trắc nghiệm tương ứng.
            </p>
          </div>
          
          {uploading ? (
            <div className="max-w-md mx-auto space-y-3">
              <div className="w-full bg-slate-950/80 border border-slate-800 rounded-full h-3 overflow-hidden">
                <div className="bg-purple-500 h-3 rounded-full transition-all duration-1000" style={{ width: `${progress}%` }} />
              </div>
              <p className="text-xs text-purple-300 font-bold flex items-center justify-center gap-1.5 animate-pulse">
                <Loader2 size={12} className="animate-spin" />
                {getStatusText(progress)}
              </p>
            </div>
          ) : (
            <button 
              onClick={handleUpload} 
              className="inline-flex items-center gap-2 bg-purple-600 hover:bg-purple-700 text-white px-6 py-3.5 rounded-xl font-bold transition-all shadow-lg shadow-purple-500/15 active:scale-95 text-sm cursor-pointer"
            >
              <Sparkles size={16} /> Bắt đầu Tải lên PDF &rarr;
            </button>
          )}
        </div>
      </div>

      {/* NOTEBOOKS LIST */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {mockNotebooks.map((nb) => (
          <div key={nb.id} className="bg-slate-900/60 rounded-3xl border border-slate-800 p-6 flex flex-col justify-between hover:border-purple-500/30 transition-all backdrop-blur-xl relative group">
            <div className="absolute top-0 right-0 w-24 h-24 bg-white/5 rounded-full blur-2xl group-hover:bg-purple-500/5 transition-all" />
            <div className="space-y-4 relative z-10">
              <div className="flex justify-between items-start gap-4">
                <h3 className="font-bold text-slate-200 leading-snug line-clamp-2">{nb.title}</h3>
                <span className={`px-2.5 py-1 rounded-full text-[10px] font-black uppercase tracking-wider shrink-0
                  ${nb.status === 'Ready' 
                    ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' 
                    : 'bg-yellow-500/10 text-yellow-400 border border-yellow-500/20 animate-pulse'}`}>
                  {nb.status === 'Ready' ? 'Sẵn sàng' : 'Đang xử lý'}
                </span>
              </div>

              <div className="grid grid-cols-3 gap-3 text-center border-t border-slate-850 pt-4">
                <div className="space-y-0.5">
                  <p className="text-xl font-black text-blue-400 flex items-center justify-center gap-1"><FileText size={14} />{nb.pages}</p>
                  <p className="text-[10px] text-gray-500 font-bold uppercase tracking-wider">Trang</p>
                </div>
                <div className="space-y-0.5">
                  <p className="text-xl font-black text-purple-400 flex items-center justify-center gap-1"><Layers size={14} />{nb.chapters}</p>
                  <p className="text-[10px] text-gray-500 font-bold uppercase tracking-wider">Chương</p>
                </div>
                <div className="space-y-0.5">
                  <p className="text-xl font-black text-emerald-400 flex items-center justify-center gap-1"><ListTodo size={14} />{nb.questions}</p>
                  <p className="text-[10px] text-gray-500 font-bold uppercase tracking-wider">Câu hỏi</p>
                </div>
              </div>
            </div>

            <button className="w-full mt-6 py-2.5 rounded-xl border border-slate-800 hover:bg-slate-800 text-xs font-bold text-slate-400 hover:text-white transition-all cursor-pointer relative z-10">
              Khám phá Giáo án AI
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
export default AdminNotebooksPage;
