'use client';
import { useState } from 'react';

export default function AdminQuestionsPage() {
  const [dragActive, setDragActive] = useState(false);

  const mockQuestions = [
    { id: 1, content: 'Đâu là TBTH?', type: 'Multi', category: 'IT', difficulty: 2, source: 'Excel' },
    { id: 2, content: '1+2=?', type: 'Single', category: 'Toán', difficulty: 1, source: 'Excel' },
    { id: 3, content: 'Thủ đô của Việt Nam', type: 'Short_Answer', category: 'Địa lý', difficulty: 1, source: 'AI (PDF)' },
    { id: 4, content: 'Cơ quan quản lý ngân hàng?', type: 'Single', category: 'Ngân hàng', difficulty: 1, source: 'AI (PDF)' },
  ];

  return (
    <div>
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-3xl font-bold">Ngân hàng Câu hỏi</h1>
        <div className="flex gap-3">
          <button className="bg-green-600 hover:bg-green-700 px-4 py-2 rounded-lg text-sm font-medium transition-all">
            + Thêm câu hỏi
          </button>
        </div>
      </div>

      {/* UPLOAD ZONE */}
      <div
        onDragOver={(e) => { e.preventDefault(); setDragActive(true); }}
        onDragLeave={() => setDragActive(false)}
        onDrop={(e) => { e.preventDefault(); setDragActive(false); alert('Đang upload file...'); }}
        className={`border-2 border-dashed rounded-2xl p-8 text-center mb-8 transition-all cursor-pointer
          ${dragActive ? 'border-blue-500 bg-blue-600/10' : 'border-gray-700 bg-gray-900 hover:border-gray-500'}`}
      >
        <p className="text-4xl mb-3">📁</p>
        <p className="font-semibold text-lg">Kéo thả file Excel (.xlsx) hoặc PDF vào đây</p>
        <p className="text-sm text-gray-400 mt-2">
          Excel → Import trực tiếp câu hỏi | PDF → AI tự động sinh câu hỏi (AegisNotebook)
        </p>
      </div>

      {/* QUESTIONS TABLE */}
      <div className="bg-gray-900 rounded-2xl border border-gray-800 overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="border-b border-gray-800 text-left text-sm text-gray-400">
              <th className="p-4">STT</th>
              <th className="p-4">Nội dung</th>
              <th className="p-4">Loại</th>
              <th className="p-4">Chủ đề</th>
              <th className="p-4">Độ khó</th>
              <th className="p-4">Nguồn</th>
              <th className="p-4">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {mockQuestions.map((q) => (
              <tr key={q.id} className="border-b border-gray-800/50 hover:bg-gray-800/30 transition-colors">
                <td className="p-4 text-gray-500">{q.id}</td>
                <td className="p-4 font-medium">{q.content}</td>
                <td className="p-4">
                  <span className="px-2 py-1 rounded-full text-xs bg-blue-600/20 text-blue-400">{q.type}</span>
                </td>
                <td className="p-4 text-gray-300">{q.category}</td>
                <td className="p-4">{'⭐'.repeat(q.difficulty)}</td>
                <td className="p-4">
                  <span className={`px-2 py-1 rounded-full text-xs ${q.source === 'AI (PDF)' ? 'bg-purple-600/20 text-purple-400' : 'bg-green-600/20 text-green-400'}`}>
                    {q.source}
                  </span>
                </td>
                <td className="p-4 flex gap-2">
                  <button className="text-blue-400 hover:text-blue-300 text-sm">Sửa</button>
                  <button className="text-red-400 hover:text-red-300 text-sm">Xóa</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
