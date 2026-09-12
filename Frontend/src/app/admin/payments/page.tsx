'use client';
import { useState } from 'react';

export default function AdminPaymentsPage() {
  const [transactions] = useState([
    { id: 'TX-9023', user: 'Nguyễn Văn A', method: 'Stripe', amount: '₫99,000', status: 'Completed', date: '2026-07-02 15:54' },
    { id: 'TX-9024', user: 'Lê Văn C', method: 'Web3 Polygon', amount: '2 MATIC', status: 'Completed', date: '2026-07-02 13:12' },
    { id: 'TX-9025', user: 'Hoàng Lan Anh', method: 'VietQR', amount: '₫99,000', status: 'Pending', date: '2026-07-02 12:45' },
    { id: 'TX-9026', user: 'Trần Thị B', method: 'VietQR', amount: '₫99,000', status: 'Expired', date: '2026-07-01 10:00' },
  ]);

  const summary = [
    { label: 'Tổng doanh thu', value: '₫45,280,000', detail: '+18% so với tuần trước', color: 'text-green-400' },
    { label: 'Doanh thu Web3', value: '420 MATIC', detail: 'Chiếm 15% tổng doanh thu', color: 'text-purple-400' },
    { label: 'Giao dịch thành công', value: '458', detail: 'Tỷ lệ thanh toán: 89.2%', color: 'text-blue-400' },
  ];

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold bg-gradient-to-r from-green-400 to-emerald-500 bg-clip-text text-transparent">
          Báo cáo Doanh thu
        </h1>
        <p className="text-sm text-gray-400 mt-1">Cập nhật thời gian thực từ VietQR và Web3 Gateway</p>
      </div>

      {/* SUMMARY */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {summary.map((item, i) => (
          <div key={i} className="p-6 bg-gray-900 border border-gray-800 rounded-2xl">
            <p className="text-sm text-gray-400 mb-1">{item.label}</p>
            <p className={`text-3xl font-bold ${item.color}`}>{item.value}</p>
            <p className="text-xs text-gray-500 mt-2">{item.detail}</p>
          </div>
        ))}
      </div>

      {/* TRANSACTION TABLE */}
      <div className="bg-gray-900 border border-gray-800 rounded-2xl overflow-hidden">
        <div className="p-6 border-b border-gray-800 flex justify-between items-center bg-gray-900/50">
          <h2 className="text-lg font-semibold text-gray-200">Giao dịch Gần đây</h2>
          <span className="text-xs text-gray-500">Mô phỏng tích hợp VietQR + Web3 Gateway</span>
        </div>
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-gray-800 text-gray-400 text-sm">
              <th className="p-4 font-semibold">Mã Giao dịch</th>
              <th className="p-4 font-semibold">Người nạp</th>
              <th className="p-4 font-semibold">Cổng thanh toán</th>
              <th className="p-4 font-semibold">Số tiền</th>
              <th className="p-4 font-semibold">Trạng thái</th>
              <th className="p-4 font-semibold text-right">Thời gian</th>
            </tr>
          </thead>
          <tbody>
            {transactions.map((tx) => (
              <tr key={tx.id} className="border-b border-gray-800/50 hover:bg-gray-800/20 transition-all">
                <td className="p-4 font-mono text-sm text-gray-400">{tx.id}</td>
                <td className="p-4 font-semibold text-gray-200">{tx.user}</td>
                <td className="p-4">
                  <span className={`px-2 py-1 rounded text-xs font-semibold ${tx.method.includes('Web3') ? 'bg-purple-600/10 text-purple-400 border border-purple-500/20' : tx.method.includes('VietQR') ? 'bg-orange-600/10 text-orange-400 border border-orange-500/20' : 'bg-blue-600/10 text-blue-400 border border-blue-500/20'}`}>
                    {tx.method}
                  </span>
                </td>
                <td className="p-4 font-semibold text-white">{tx.amount}</td>
                <td className="p-4">
                  <span className={`px-2.5 py-1 rounded-full text-xs font-semibold ${tx.status === 'Completed' ? 'bg-green-500/10 text-green-400 border border-green-500/20' : tx.status === 'Pending' ? 'bg-yellow-500/10 text-yellow-400 border border-yellow-500/20' : 'bg-red-500/10 text-red-400 border border-red-500/20'}`}>
                    {tx.status}
                  </span>
                </td>
                <td className="p-4 text-right text-gray-400 text-sm">{tx.date}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
