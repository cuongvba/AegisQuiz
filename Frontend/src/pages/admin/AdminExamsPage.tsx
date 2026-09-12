import { useState } from 'react';
import { Link } from 'react-router-dom';
import { FileText, Plus, CheckCircle, Eye, Printer, Settings } from 'lucide-react';

export function AdminExamsPage() {
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
      <div>
        <h1 className="text-3xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400 tracking-tight">
          Quản lý Đề thi
        </h1>
        <p className="text-sm text-slate-400 mt-1 font-medium">Tạo lập, xuất cấu hình đề thi giấy và quản trị các phòng thi sát hạch.</p>
      </div>

      {/* CREATE FORM */}
      <form onSubmit={createExam} className="bg-slate-900/60 border border-slate-800 p-6 md:p-8 rounded-3xl space-y-6 backdrop-blur-xl">
        <h2 className="text-lg font-bold text-slate-200 flex items-center gap-2">
          <Settings size={20} className="text-blue-400" /> Tạo Đề thi Mới (Từ Ngân hàng Câu hỏi)
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <input 
            type="text" 
            placeholder="Tên đề thi (ví dụ: Đề thi thử Nghiệp vụ Quỹ năm 2026)" 
            value={newTitle}
            onChange={(e) => setNewTitle(e.target.value)}
            className="md:col-span-2 bg-slate-950 border border-slate-800 rounded-xl px-4 py-3 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-blue-500 transition-colors"
          />
          <select 
            value={newCategory} 
            onChange={(e) => setNewCategory(e.target.value)}
            className="bg-slate-950 border border-slate-800 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-blue-500 transition-colors cursor-pointer"
          >
            <option value="Nghiệp vụ">Nghiệp vụ</option>
            <option value="Tín dụng">Tín dụng</option>
            <option value="An toàn Thông tin">An toàn Thông tin</option>
            <option value="Khác">Khác</option>
          </select>
        </div>
        <button type="submit" className="bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 text-white font-bold px-6 py-3 rounded-xl transition-all shadow-md shadow-blue-500/10 active:scale-95 text-sm cursor-pointer">
          Generate Đề thi
        </button>
      </form>

      {/* EXAMS LIST */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-3xl overflow-hidden backdrop-blur-xl">
        <div className="p-6 border-b border-slate-850 bg-slate-900/30">
          <h2 className="text-lg font-bold text-slate-200 flex items-center gap-2">
            <FileText size={20} className="text-blue-400" /> Danh sách Đề thi hiện có
          </h2>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse min-w-[700px]">
            <thead>
              <tr className="border-b border-slate-850 text-slate-400 text-xs font-black tracking-wider uppercase bg-slate-900/20">
                <th className="p-4 w-24">Mã đề</th>
                <th className="p-4">Tên đề thi</th>
                <th className="p-4 w-36">Chủ đề</th>
                <th className="p-4 w-28">Thời gian</th>
                <th className="p-4 w-28 text-center">Số câu hỏi</th>
                <th className="p-4 w-28">Trạng thái</th>
                <th className="p-4 w-32 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {exams.map((exam) => (
                <tr key={exam.id} className="border-b border-slate-850/60 hover:bg-slate-800/10 transition-colors">
                  <td className="p-4 font-mono text-xs text-slate-500">{exam.id}</td>
                  <td className="p-4 font-bold text-slate-200 text-sm leading-snug">{exam.title}</td>
                  <td className="p-4 text-slate-350 text-xs font-semibold">{exam.category}</td>
                  <td className="p-4 text-slate-400 text-xs">{exam.duration}</td>
                  <td className="p-4 text-center font-bold text-slate-200 text-sm">{exam.totalQuestions}</td>
                  <td className="p-4">
                    <span className={`px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider flex items-center gap-1 w-fit
                      ${exam.status === 'Active' 
                        ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' 
                        : 'bg-slate-800 text-slate-400'}`}>
                      {exam.status === 'Active' && <CheckCircle size={10} />}
                      {exam.status}
                    </span>
                  </td>
                  <td className="p-4 text-right">
                    <Link 
                      to="/quiz/attempt/paper-sample" 
                      className="inline-flex items-center gap-1 text-[10px] bg-slate-850 hover:bg-slate-800 text-slate-300 px-3 py-1.5 rounded-lg border border-slate-700 hover:border-blue-500 transition-all font-bold cursor-pointer"
                    >
                      <Printer size={10} /> In PaperMode
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
export default AdminExamsPage;
