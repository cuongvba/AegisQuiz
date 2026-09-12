'use client';
import { useState } from 'react';
import Link from 'next/link';

export default function AdminExamsPage() {
  const [exams, setExams] = useState([
    { id: 'EXAM-01', title: 'Kiểm tra Nghiệp vụ Giao dịch viên', duration: '45 phút', totalQuestions: 40, status: 'Active', category: 'Nghiệp vụ' },
    { id: 'EXAM-02', title: 'Quy trình Tín dụng Doanh nghiệp 2026', duration: '60 phút', totalQuestions: 50, status: 'Active', category: 'Tín dụng' },
    { id: 'EXAM-03', title: 'Kiểm tra Kiến thức ISO 27001 Cơ bản', duration: '30 phút', totalQuestions: 20, status: 'Draft', category: 'An toàn Thông tin' },
  ]);

  const [newTitle, setNewTitle] = useState('');
  const [newCategory, setNewCategory] = useState('Nghiệp vụ');

  const createExam = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newTitle) return;
    const newExam = {
      id: `EXAM-0${exams.length + 1}`,
      title: newTitle,
      duration: '45 phút',
      totalQuestions: 30,
      status: 'Draft',
      category: newCategory,
    };
    setExams([...exams, newExam]);
    setNewTitle('');
  };

  return (
    <div className="space-y-8">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold bg-gradient-to-r from-orange-400 to-amber-500 bg-clip-text text-transparent">
            Quản lý Đề thi
          </h1>
          <p className="text-sm text-gray-400 mt-1">Quản lý và tạo đề kiểm tra cho nhân viên/học viên</p>
        </div>
      </div>

      {/* CREATE FORM */}
      <form onSubmit={createExam} className="bg-gray-900 border border-gray-800 p-6 rounded-2xl space-y-4">
        <h2 className="text-lg font-semibold text-gray-200">Tạo Đề thi Mới (Từ Ngân hàng Câu hỏi)</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <input 
            type="text" 
            placeholder="Tên đề thi (ví dụ: Đề thi thử Toán học lớp 12)" 
            value={newTitle}
            onChange={(e) => setNewTitle(e.target.value)}
            className="md:col-span-2 bg-gray-950 border border-gray-800 rounded-xl px-4 py-3 text-white placeholder-gray-500 focus:outline-none focus:border-orange-500 transition-colors"
          />
          <select 
            value={newCategory} 
            onChange={(e) => setNewCategory(e.target.value)}
            className="bg-gray-950 border border-gray-800 rounded-xl px-4 py-3 text-white focus:outline-none focus:border-orange-500 transition-colors"
          >
            <option value="Nghiệp vụ">Nghiệp vụ</option>
            <option value="Tín dụng">Tín dụng</option>
            <option value="An toàn Thông tin">An toàn Thông tin</option>
            <option value="Khác">Khác</option>
          </select>
        </div>
        <button type="submit" className="bg-orange-600 hover:bg-orange-700 text-white font-semibold px-6 py-2.5 rounded-xl transition-all">
          Generate Đề thi
        </button>
      </form>

      {/* EXAMS LIST */}
      <div className="bg-gray-900 border border-gray-800 rounded-2xl overflow-hidden">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-gray-800 text-gray-400 text-sm">
              <th className="p-4 font-semibold">Mã đề</th>
              <th className="p-4 font-semibold">Tên đề thi</th>
              <th className="p-4 font-semibold">Chủ đề</th>
              <th className="p-4 font-semibold">Thời gian</th>
              <th className="p-4 font-semibold text-center">Số câu hỏi</th>
              <th className="p-4 font-semibold">Trạng thái</th>
              <th className="p-4 font-semibold text-right">In thi giấy</th>
            </tr>
          </thead>
          <tbody>
            {exams.map((exam) => (
              <tr key={exam.id} className="border-b border-gray-800/50 hover:bg-gray-800/20 transition-all">
                <td className="p-4 font-mono text-sm text-gray-400">{exam.id}</td>
                <td className="p-4 font-semibold text-gray-200">{exam.title}</td>
                <td className="p-4 text-gray-300">{exam.category}</td>
                <td className="p-4 text-gray-400">{exam.duration}</td>
                <td className="p-4 text-center font-semibold text-gray-200">{exam.totalQuestions}</td>
                <td className="p-4">
                  <span className={`px-2.5 py-1 rounded-full text-xs font-semibold ${exam.status === 'Active' ? 'bg-green-500/10 text-green-400 border border-green-500/20' : 'bg-gray-800 text-gray-400'}`}>
                    {exam.status}
                  </span>
                </td>
                <td className="p-4 text-right">
                  <Link 
                    href="/quiz/paper" 
                    className="text-xs bg-gray-800 hover:bg-gray-700 text-gray-300 px-3 py-1.5 rounded-lg border border-gray-700 hover:border-orange-500 transition-all inline-block"
                  >
                    📄 In PaperMode
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
