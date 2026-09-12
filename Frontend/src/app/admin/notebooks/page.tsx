'use client';
import { useState } from 'react';

export default function AdminNotebooksPage() {
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
    const steps = ['Đang đọc PDF...', 'AI đang phân tích nội dung...', 'Đang sinh câu hỏi...', 'Hoàn tất!'];
    let step = 0;
    const interval = setInterval(() => {
      step++;
      setProgress(step * 25);
      if (step >= 4) { clearInterval(interval); setUploading(false); }
    }, 1500);
  };

  return (
    <div>
      <h1 className="text-3xl font-bold mb-8">Giáo trình từ PDF (AegisNotebook)</h1>

      {/* UPLOAD ZONE */}
      <div className="bg-gray-900 rounded-2xl border border-purple-600/30 p-8 mb-8">
        <div className="text-center">
          <p className="text-5xl mb-4">🧠</p>
          <h2 className="text-xl font-semibold mb-2">Tải lên PDF → AI tự động tạo Giáo trình + Quiz</h2>
          <p className="text-sm text-gray-400 mb-6">
            Gemini 1.5 Pro sẽ đọc toàn bộ tài liệu, tạo mục lục, tóm tắt và sinh hàng trăm câu hỏi tự động.
          </p>
          
          {uploading ? (
            <div className="max-w-md mx-auto">
              <div className="w-full bg-gray-800 rounded-full h-3 mb-3">
                <div className="bg-purple-600 h-3 rounded-full transition-all duration-1000" style={{ width: `${progress}%` }} />
              </div>
              <p className="text-sm text-purple-400">
                {progress < 25 ? 'Đang đọc PDF...' : progress < 50 ? 'AI đang phân tích nội dung...' : progress < 75 ? 'Đang sinh câu hỏi...' : '✅ Hoàn tất!'}
              </p>
            </div>
          ) : (
            <button onClick={handleUpload} className="bg-purple-600 hover:bg-purple-700 px-6 py-3 rounded-xl font-semibold transition-all hover:scale-105">
              📄 Chọn file PDF để Upload
            </button>
          )}
        </div>
      </div>

      {/* NOTEBOOKS LIST */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {mockNotebooks.map((nb) => (
          <div key={nb.id} className="bg-gray-900 rounded-2xl border border-gray-800 p-6 hover:border-purple-600/30 transition-all">
            <div className="flex justify-between items-start mb-4">
              <h3 className="font-semibold text-lg">{nb.title}</h3>
              <span className={`px-2 py-1 rounded-full text-xs ${nb.status === 'Ready' ? 'bg-green-600/20 text-green-400' : 'bg-yellow-600/20 text-yellow-400'}`}>
                {nb.status === 'Ready' ? '✅ Sẵn sàng' : '⏳ Đang xử lý'}
              </span>
            </div>
            <div className="grid grid-cols-3 gap-4 text-center">
              <div><p className="text-2xl font-bold text-blue-400">{nb.pages}</p><p className="text-xs text-gray-500">Trang</p></div>
              <div><p className="text-2xl font-bold text-purple-400">{nb.chapters}</p><p className="text-xs text-gray-500">Chương</p></div>
              <div><p className="text-2xl font-bold text-green-400">{nb.questions}</p><p className="text-xs text-gray-500">Câu hỏi</p></div>
            </div>
            <button className="w-full mt-4 py-2 rounded-lg border border-gray-700 text-sm text-gray-400 hover:bg-gray-800 transition-all">
              Xem chi tiết Mục lục
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
