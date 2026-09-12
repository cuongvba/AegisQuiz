'use client';

export default function AdminDashboard() {
  // Giả lập dữ liệu thống kê
  const stats = [
    { label: 'Tổng Học sinh', value: '2,847', change: '+12%', color: 'text-blue-400', bg: 'bg-blue-600/10 border-blue-600/20' },
    { label: 'Doanh thu Tháng', value: '₫45.2M', change: '+28%', color: 'text-green-400', bg: 'bg-green-600/10 border-green-600/20' },
    { label: 'Câu hỏi trong Kho', value: '12,456', change: '+340', color: 'text-purple-400', bg: 'bg-purple-600/10 border-purple-600/20' },
    { label: 'Giáo trình PDF', value: '18', change: '+3', color: 'text-yellow-400', bg: 'bg-yellow-600/10 border-yellow-600/20' },
  ];

  const recentActivities = [
    { user: 'Nguyễn Văn A', action: 'Nạp VIP (Stripe)', time: '2 phút trước', amount: '₫99,000' },
    { user: 'Trần Thị B', action: 'Hoàn thành đề thi Toán', time: '5 phút trước', amount: '92/100' },
    { user: 'Admin', action: 'Upload giáo trình PDF mới', time: '1 giờ trước', amount: '156 trang' },
    { user: 'Lê Văn C', action: 'Nạp VIP (Polygon)', time: '3 giờ trước', amount: '2 MATIC' },
  ];

  return (
    <div>
      <h1 className="text-3xl font-bold mb-8">Tổng quan Hệ thống</h1>

      {/* STAT CARDS */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-10">
        {stats.map((stat) => (
          <div key={stat.label} className={`p-6 rounded-2xl border ${stat.bg}`}>
            <p className="text-sm text-gray-400 mb-1">{stat.label}</p>
            <p className={`text-3xl font-bold ${stat.color}`}>{stat.value}</p>
            <p className="text-xs text-green-500 mt-2">▲ {stat.change} so với tháng trước</p>
          </div>
        ))}
      </div>

      {/* RECENT ACTIVITIES */}
      <div className="bg-gray-900 rounded-2xl border border-gray-800 p-6">
        <h2 className="text-xl font-semibold mb-4">Hoạt động Gần đây</h2>
        <div className="space-y-4">
          {recentActivities.map((activity, i) => (
            <div key={i} className="flex items-center justify-between py-3 border-b border-gray-800 last:border-0">
              <div>
                <p className="font-medium">{activity.user}</p>
                <p className="text-sm text-gray-400">{activity.action}</p>
              </div>
              <div className="text-right">
                <p className="font-semibold text-blue-400">{activity.amount}</p>
                <p className="text-xs text-gray-500">{activity.time}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
