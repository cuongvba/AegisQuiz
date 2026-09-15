import { useState } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { LayoutDashboard, FileQuestion, BookOpen, Users, CreditCard, FileText, ArrowLeft, Bot, Menu, X, Shield, Sparkles } from 'lucide-react';
import { useAuthContext } from '@/app/providers/AuthProvider';

const allMenuItems = [
  { href: '/admin', label: 'Tổng quan', icon: <LayoutDashboard size={18} /> },
  { href: '/admin/questions', label: 'Ngân hàng Câu hỏi', icon: <FileQuestion size={18} /> },
  { href: '/admin/notebooks', label: 'Giáo trình (PDF)', icon: <BookOpen size={18} />, adminOnly: true },
  { href: '/admin/users', label: 'Người dùng', icon: <Users size={18} />, adminOnly: true },
  { href: '/admin/payments', label: 'Doanh thu', icon: <CreditCard size={18} />, adminOnly: true },
  { href: '/admin/exams', label: 'Đề thi', icon: <FileText size={18} /> },
];

export function AdminLayout() {
  const { user } = useAuthContext();
  const isTeamLeader = user?.role === 'TeamLeader';
  const location = useLocation();
  const navigate = useNavigate();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  const menuItems = allMenuItems.filter(item => !isTeamLeader || !item.adminOnly);

  return (
    <div className="flex flex-col min-h-screen bg-slate-950 text-white font-sans">
      {/* Top Header */}
      <div className="flex items-center justify-between bg-slate-900 border-b border-slate-800 px-6 py-4 sticky top-0 z-40 backdrop-blur-md bg-opacity-90">
        <div className="flex items-center gap-3 cursor-pointer" onClick={() => navigate('/')}>
          <div className="w-8 h-8 rounded-full bg-gradient-to-tr from-cyan-400 to-blue-500 flex items-center justify-center border border-white/20">
            <Bot className="h-5 w-5 text-white" />
          </div>
          <div>
            <span className="text-sm font-black bg-gradient-to-r from-cyan-400 to-blue-500 bg-clip-text text-transparent">
              AegisQuiz
            </span>
            <p className="text-[8px] text-gray-500 font-bold uppercase tracking-wider">
              {isTeamLeader ? 'Team Leader Workspace' : 'Admin Portal'}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          {isTeamLeader && (
            <span className="hidden sm:inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-amber-950/60 border border-amber-500/40 text-amber-300">
              <Sparkles size={13} className="text-amber-400" />
              <span>Trưởng nhóm: {user?.orgUnitName || 'Đơn vị nội bộ'}</span>
            </span>
          )}
          <button 
            onClick={() => setIsSidebarOpen(true)}
            className="md:hidden p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-xl transition-colors cursor-pointer"
          >
            <Menu size={20} />
          </button>
        </div>
      </div>

      {/* Drawer Backdrop */}
      {isSidebarOpen && (
        <div 
          className="fixed inset-0 bg-slate-950/20 z-40 md:hidden"
          onClick={() => setIsSidebarOpen(false)}
        />
      )}

      {/* SIDEBAR */}
      <aside className={`
        fixed inset-y-0 left-0 w-64 bg-slate-900 border-r border-slate-800 p-6 flex flex-col z-50 transition-transform duration-300 transform
        md:translate-x-0 ${isSidebarOpen ? 'translate-x-0' : '-translate-x-full'}
      `}>
        {/* Close Button (Mobile Only) */}
        <button 
          onClick={() => setIsSidebarOpen(false)}
          className="md:hidden absolute top-4 right-4 p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-xl transition-colors cursor-pointer"
        >
          <X size={18} />
        </button>

        <div className="mb-8 flex items-center gap-3 cursor-pointer" onClick={() => { navigate('/'); if (window.innerWidth < 768) setIsSidebarOpen(false); }}>
          <div className="w-10 h-10 rounded-full bg-gradient-to-tr from-cyan-400 to-blue-500 shadow-md flex items-center justify-center flex-shrink-0 border-2 border-white">
            <Bot className="h-6 w-6 text-white" />
          </div>
          <div>
            <h1 className="text-lg font-black bg-gradient-to-r from-cyan-400 to-blue-500 bg-clip-text text-transparent">
              AegisQuiz
            </h1>
            <p className="text-[10px] text-gray-500 font-bold uppercase tracking-wider">Admin Portal</p>
          </div>
        </div>

        <nav className="flex-1 space-y-1">
          {menuItems.map((item) => {
            const isActive = location.pathname === item.href;
            return (
              <Link
                key={item.href}
                to={item.href}
                onClick={() => { if (window.innerWidth < 768) setIsSidebarOpen(false); }}
                className={`flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-bold transition-all
                  ${isActive 
                    ? 'bg-blue-600/20 text-blue-400 border border-blue-500/20' 
                    : 'text-gray-400 hover:bg-slate-800 hover:text-white'
                  }`}
              >
                {item.icon}
                <span>{item.label}</span>
              </Link>
            );
          })}
        </nav>

        <div className="border-t border-slate-800 pt-4 mt-4">
          <Link to="/" onClick={() => { if (window.innerWidth < 768) setIsSidebarOpen(false); }} className="text-xs text-gray-500 hover:text-gray-300 transition-colors flex items-center gap-1 font-bold">
            <ArrowLeft size={12} /> Quay lại Học tập
          </Link>
        </div>
      </aside>

      {/* MAIN CONTENT */}
      <main className="flex-1 p-4 md:p-8 overflow-y-auto md:ml-64 transition-all duration-300">
        <div className="w-full">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
export default AdminLayout;
