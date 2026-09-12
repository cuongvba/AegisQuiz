import { useState, useEffect } from 'react';
import { Users, CreditCard, FileQuestion, BookOpen, Clock, TrendingUp, Loader2, RefreshCw } from 'lucide-react';
import api from '@/services/api';

interface DashboardStats {
  questionCount: number;
  userCount: number;
  monthlyRevenue: string;
  notebookCount: number;
}

interface RecentActivity {
  user: string;
  action: string;
  time: string;
  amount: string;
}

function StatSkeleton() {
  return (
    <div className="p-6 rounded-2xl border bg-slate-800/30 border-slate-700/30 animate-pulse">
      <div className="flex justify-between items-start mb-4">
        <div className="h-3 w-24 bg-slate-700 rounded" />
        <div className="h-6 w-6 bg-slate-700 rounded-full" />
      </div>
      <div className="h-8 w-32 bg-slate-700 rounded mb-2" />
      <div className="h-2 w-20 bg-slate-800 rounded" />
    </div>
  );
}

export function AdminDashboard() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastRefresh, setLastRefresh] = useState(new Date());

  const fetchStats = async () => {
    setLoading(true);
    setError(null);
    try {
      // Gọi API lấy tổng số câu hỏi (pageSize=1 để tiết kiệm bandwidth)
      const qRes = await api.get('/api/quiz/questions?page=1&pageSize=1');
      const questionCount: number = qRes.data?.total ?? qRes.data?.Total ?? 0;

      setStats({
        questionCount,
        userCount: 2847,        // TODO: kết nối UserController khi có endpoint
        monthlyRevenue: '45.2M', // TODO: kết nối PaymentController
        notebookCount: 18,       // TODO: kết nối NotebookController
      });
    } catch (err: unknown) {
      console.warn('[AdminDashboard] API unavailable, using fallback stats:', err);
      // Graceful fallback khi backend chưa chạy
      setStats({ questionCount: 0, userCount: 2847, monthlyRevenue: '45.2M', notebookCount: 18 });
      setError('Backend API chưa sẵn sàng. Đang hiển thị dữ liệu mẫu.');
    } finally {
      setLoading(false);
      setLastRefresh(new Date());
    }
  };

  useEffect(() => {
    fetchStats();
  }, []);

  const recentActivities: RecentActivity[] = [
    { user: 'Nguyễn Văn A', action: 'Nạp VIP (Stripe)', time: '2 phút trước', amount: '₫99,000' },
    { user: 'Trần Thị B', action: 'Hoàn thành đề thi Toán', time: '5 phút trước', amount: '92/100' },
    { user: 'Admin', action: 'Upload giáo trình PDF mới', time: '1 giờ trước', amount: '156 trang' },
    { user: 'Lê Văn C', action: 'Nạp VIP (Polygon)', time: '3 giờ trước', amount: '2 MATIC' },
  ];

  const statCards = stats ? [
    { label: 'Tổng Học sinh',     value: stats.userCount.toLocaleString('vi-VN'), change: '+12%', color: 'text-blue-400',   bg: 'bg-blue-500/5 border-blue-500/10',   icon: <Users className="text-blue-400" /> },
    { label: 'Doanh thu Tháng',   value: `₫${stats.monthlyRevenue}`,             change: '+28%', color: 'text-emerald-400', bg: 'bg-emerald-500/5 border-emerald-500/10', icon: <CreditCard className="text-emerald-400" /> },
    { label: 'Câu hỏi trong Kho', value: stats.questionCount.toLocaleString('vi-VN') || '0', change: 'Cập nhật thực', color: 'text-purple-400', bg: 'bg-purple-500/5 border-purple-500/10', icon: <FileQuestion className="text-purple-400" /> },
    { label: 'Giáo trình PDF',    value: String(stats.notebookCount),            change: '+3', color: 'text-amber-400',   bg: 'bg-amber-500/5 border-amber-500/10',   icon: <BookOpen className="text-amber-400" /> },
  ] : [];

  return (
    <div className="space-y-8">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400 tracking-tight">
            Tổng quan Hệ thống
          </h1>
          <p className="text-sm text-slate-400 mt-1 font-medium">
            Báo cáo tổng quát và lịch sử vận hành của hệ thống AegisQuiz.
            {stats && (
              <span className="ml-2 text-slate-600 text-[10px] font-mono">
                Cập nhật: {lastRefresh.toLocaleTimeString('vi-VN')}
              </span>
            )}
          </p>
        </div>
        <button
          onClick={fetchStats}
          disabled={loading}
          className="flex items-center gap-1.5 px-3 py-2 text-xs font-bold text-slate-400 hover:text-white bg-slate-900 hover:bg-slate-800 border border-slate-800 rounded-xl transition-all disabled:opacity-50 cursor-pointer"
        >
          {loading ? <Loader2 size={14} className="animate-spin" /> : <RefreshCw size={14} />}
          Làm mới
        </button>
      </div>

      {/* Error banner */}
      {error && (
        <div className="bg-amber-950/30 border border-amber-500/20 rounded-2xl px-4 py-2.5 text-amber-400 text-xs font-medium flex items-center gap-2">
          <TrendingUp size={14} className="shrink-0" />
          {error}
        </div>
      )}

      {/* STAT CARDS */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {loading
          ? Array.from({ length: 4 }).map((_, i) => <StatSkeleton key={i} />)
          : statCards.map((stat) => (
            <div key={stat.label} className={`p-6 rounded-2xl border ${stat.bg} backdrop-blur-md shadow-sm relative overflow-hidden group hover:scale-[1.02] transition-transform`}>
              <div className="absolute top-0 right-0 w-16 h-16 bg-white/5 rounded-full blur-xl group-hover:scale-150 transition-transform" />
              <div className="flex justify-between items-start mb-4">
                <span className="text-sm text-slate-400 font-bold">{stat.label}</span>
                {stat.icon}
              </div>
              <p className={`text-3xl font-black ${stat.color}`}>{stat.value}</p>
              <p className="text-xs text-emerald-500 font-bold mt-2 flex items-center gap-1">
                ▲ {stat.change} <span className="text-slate-500 font-medium">so với tháng trước</span>
              </p>
            </div>
          ))
        }
      </div>

      {/* RECENT ACTIVITIES */}
      <div className="bg-slate-900/60 rounded-3xl border border-slate-800 p-6 md:p-8 backdrop-blur-xl">
        <h2 className="text-xl font-bold mb-6 flex items-center gap-2">
          <Clock size={20} className="text-blue-400" /> Hoạt động Gần đây
        </h2>
        <div className="space-y-4">
          {recentActivities.map((activity, i) => (
            <div key={i} className="flex items-center justify-between py-4 border-b border-slate-800 last:border-0">
              <div className="space-y-1">
                <p className="font-bold text-gray-200">{activity.user}</p>
                <p className="text-xs text-gray-400 font-medium">{activity.action}</p>
              </div>
              <div className="text-right space-y-1">
                <p className="font-black text-blue-400 text-sm">{activity.amount}</p>
                <p className="text-[10px] text-gray-500 font-mono">{activity.time}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
export default AdminDashboard;
