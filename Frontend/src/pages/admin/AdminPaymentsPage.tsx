import { useState } from 'react';
import { DollarSign, Landmark, Coins, TrendingUp } from 'lucide-react';

export function AdminPaymentsPage() {
  const [transactions] = useState([
    { id: 'TX-9023', user: 'Nguyễn Văn A', method: 'Stripe', amount: '₫99,000', status: 'Completed', date: '2026-07-02 15:54' },
    { id: 'TX-9024', user: 'Lê Văn C', method: 'Web3 Polygon', amount: '2 MATIC', status: 'Completed', date: '2026-07-02 13:12' },
    { id: 'TX-9025', user: 'Hoàng Lan Anh', method: 'VietQR', amount: '₫99,000', status: 'Pending', date: '2026-07-02 12:45' },
    { id: 'TX-9026', user: 'Trần Thị B', method: 'VietQR', amount: '₫99,000', status: 'Expired', date: '2026-07-01 10:00' },
  ]);

  const summary = [
    { label: 'Tổng doanh thu', value: '₫45,280,000', detail: '+18% so với tuần trước', color: 'text-emerald-400', icon: <Landmark size={20} className="text-emerald-400" /> },
    { label: 'Doanh thu Web3', value: '420 MATIC', detail: 'Chiếm 15% tổng doanh thu', color: 'text-purple-400', icon: <Coins size={20} className="text-purple-400" /> },
    { label: 'Giao dịch thành công', value: '458', detail: 'Tỷ lệ thanh toán: 89.2%', color: 'text-blue-400', icon: <TrendingUp size={20} className="text-blue-400" /> },
  ];

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400 tracking-tight">
          Báo cáo Doanh thu
        </h1>
        <p className="text-sm text-slate-400 mt-1 font-medium">Báo cáo tài chính trực quan tích hợp VietQR và Web3 Gateway.</p>
      </div>

      {/* SUMMARY */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {summary.map((item, i) => (
          <div key={i} className="p-6 bg-slate-900/60 border border-slate-800 rounded-3xl backdrop-blur-xl relative overflow-hidden group hover:scale-[1.02] transition-transform">
            <div className="absolute top-0 right-0 w-16 h-16 bg-white/5 rounded-full blur-xl group-hover:scale-150 transition-transform" />
            <div className="flex justify-between items-start mb-4">
              <span className="text-xs text-slate-450 font-bold uppercase tracking-wider">{item.label}</span>
              {item.icon}
            </div>
            <p className={`text-2xl font-black ${item.color}`}>{item.value}</p>
            <p className="text-[10px] text-gray-500 font-bold mt-2">{item.detail}</p>
          </div>
        ))}
      </div>

      {/* TRANSACTION TABLE */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-3xl overflow-hidden backdrop-blur-xl">
        <div className="p-6 border-b border-slate-850 flex justify-between items-center bg-slate-900/30">
          <h2 className="text-lg font-bold text-slate-200">Giao dịch Gần đây</h2>
          <span className="text-xs text-slate-500 font-bold tracking-wider bg-slate-950/60 border border-slate-850 px-3 py-1 rounded-full uppercase">Real-time Gateway Sync</span>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse min-w-[700px]">
            <thead>
              <tr className="border-b border-slate-850 text-slate-400 text-xs font-black tracking-wider uppercase bg-slate-900/20">
                <th className="p-4">Mã Giao dịch</th>
                <th className="p-4">Người nạp</th>
                <th className="p-4 w-36">Cổng thanh toán</th>
                <th className="p-4 w-32">Số tiền</th>
                <th className="p-4 w-32">Trạng thái</th>
                <th className="p-4 w-44 text-right">Thời gian</th>
              </tr>
            </thead>
            <tbody>
              {transactions.map((tx) => (
                <tr key={tx.id} className="border-b border-slate-850/60 hover:bg-slate-800/10 transition-colors">
                  <td className="p-4 font-mono text-xs text-slate-500">{tx.id}</td>
                  <td className="p-4 font-bold text-slate-200 text-sm">{tx.user}</td>
                  <td className="p-4">
                    <span className={`px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider flex items-center gap-1 w-fit
                      ${tx.method.includes('Web3') 
                        ? 'bg-purple-500/10 text-purple-400 border border-purple-500/20' 
                        : tx.method.includes('VietQR') 
                          ? 'bg-orange-500/10 text-orange-400 border border-orange-500/20' 
                          : 'bg-blue-500/10 text-blue-400 border border-blue-500/20'}`}>
                      {tx.method}
                    </span>
                  </td>
                  <td className="p-4 font-bold text-slate-200 text-sm">{tx.amount}</td>
                  <td className="p-4">
                    <span className={`px-2.5 py-1 rounded-full text-[10px] font-black uppercase tracking-wider flex items-center gap-1 w-fit
                      ${tx.status === 'Completed' 
                        ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' 
                        : tx.status === 'Pending' 
                          ? 'bg-yellow-500/10 text-yellow-400 border border-yellow-500/20 animate-pulse' 
                          : 'bg-red-500/10 text-red-400 border border-red-500/20'}`}>
                      {tx.status}
                    </span>
                  </td>
                  <td className="p-4 text-right text-slate-400 text-xs font-mono">{tx.date}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
export default AdminPaymentsPage;
