'use client';
import { useState, useEffect } from 'react';

interface Question {
  id: string;
  content: string;
  difficulty: number;
  durationSeconds: number;
  categoryCode: string;
  options: string[];
  correctOption: string;
}

export default function PaperQuizPage() {
  const [questions, setQuestions] = useState<Question[]>([]);
  const [loading, setLoading] = useState(true);

  const examInfo = {
    code: 'MĐ-2026-AEGIS',
    subject: 'Ngân hàng & An toàn Thông tin (Enterprise)',
    duration: '45 phút',
    date: '02/07/2026',
  };

  const fetchQuestions = async () => {
    try {
      const res = await fetch('http://localhost:8080/api/quiz/questions');
      const data = await res.json();
      setQuestions(data);
    } catch (e) {
      console.error("Lỗi lấy đề thi giấy:", e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchQuestions();
  }, []);

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-100 text-gray-700">
        <p className="text-xl font-bold animate-pulse">Đang nạp đề thi từ ngân hàng câu hỏi...</p>
      </div>
    );
  }

  return (
    <>
      {/* CSS ẨN KHI IN */}
      <style>{`
        @media print {
          .no-print { display: none !important; }
          body { background: white !important; -webkit-print-color-adjust: exact; }
          @page { margin: 1.5cm; size: A4; }
        }
      `}</style>

      {/* THANH CÔNG CỤ (Ẩn khi in) */}
      <div className="no-print bg-gray-900 text-white p-4 flex justify-between items-center">
        <p className="font-semibold">📄 Chế độ In Giấy Thi (PaperMode)</p>
        <div className="flex gap-3">
          <button onClick={() => window.print()} className="bg-blue-600 hover:bg-blue-700 px-4 py-2 rounded-lg text-sm transition-colors">
            🖨️ In đề thi
          </button>
          <button onClick={() => window.print()} className="bg-green-600 hover:bg-green-700 px-4 py-2 rounded-lg text-sm transition-colors">
            📥 Tải PDF
          </button>
        </div>
      </div>

      {/* NỘI DUNG ĐỀ THI (Tối ưu cho giấy A4) */}
      <div className="bg-white text-black font-serif max-w-4xl mx-auto p-10 leading-relaxed shadow-sm mt-4 border border-gray-200">
        {/* HEADER ĐỀ THI */}
        <div className="text-center border-b-2 border-black pb-4 mb-6">
          <p className="text-sm font-bold">TẬP ĐOÀN CÔNG NGHỆ AEGIS ENTERPRISE</p>
          <h1 className="text-2xl font-bold mt-2">ĐỀ KIỂM TRA ĐÁNH GIÁ NĂNG LỰC</h1>
          <p className="text-sm mt-2">
            Môn học: <strong>{examInfo.subject}</strong>
          </p>
          <p className="text-sm mt-1">
            Mã đề: <strong>{examInfo.code}</strong> | Thời gian: <strong>{examInfo.duration}</strong> | Ngày thi: <strong>{examInfo.date}</strong>
          </p>
        </div>

        {/* THÔNG TIN THÍ SINH */}
        <div className="grid grid-cols-2 gap-4 mb-8 text-sm border border-gray-300 p-4 rounded-lg bg-gray-50/50">
          <p>Họ và tên thí sinh: .............................................................</p>
          <p>Mã số học viên: .......................................................</p>
          <p>Lớp/Phòng ban: .................................................................</p>
          <p>Chữ ký thí sinh: ................................................................</p>
        </div>

        {/* CÂU HỎI */}
        <div className="space-y-6">
          {questions.map((q, idx) => (
            <div key={q.id} className="break-inside-avoid">
              <p className="font-bold text-gray-900">
                Câu {idx + 1}. {q.content}
                <span className="font-normal text-sm italic text-gray-500 ml-2">({q.categoryCode} - Độ khó: {q.difficulty})</span>
              </p>

              {q.options && q.options.length > 0 ? (
                <div className="mt-2 grid grid-cols-2 gap-x-8 gap-y-2 pl-4">
                  {q.options.map((opt, i) => (
                    <p key={i} className="flex items-center">
                      <span className="inline-block w-5 h-5 border border-black rounded-full mr-2 text-center text-xs font-bold leading-5">
                        {String.fromCharCode(65 + i)}
                      </span>
                      {opt}
                    </p>
                  ))}
                </div>
              ) : (
                <div className="mt-2 border-b border-dotted border-gray-400 pb-4">
                  <p className="text-xs text-gray-400 italic">Trả lời: ..........................................................................................................</p>
                </div>
              )}
            </div>
          ))}
        </div>

        {/* FOOTER */}
        <div className="mt-12 text-center text-sm border-t-2 border-black pt-4">
          <p className="font-bold">--- HẾT ---</p>
          <p className="text-gray-500 mt-2">Đề thi gồm {questions.length} câu | Được tạo bởi hệ thống tự động AegisQuiz</p>
        </div>
      </div>
    </>
  );
}
