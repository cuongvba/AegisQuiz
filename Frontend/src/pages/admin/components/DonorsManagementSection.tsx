import React, { useState, useEffect } from 'react';
import { Coffee, Heart, Mail, Copy, Check, RefreshCw, Award, Download, MessageSquare } from 'lucide-react';
import api from '@/services/api';

interface DonorItem {
  orderCode: number;
  donorName: string;
  donorEmail: string;
  amount: number;
  message: string;
  rewardVoucherCode: string;
  rewardTier: string;
  status: string;
  createdAt: string;
}

export const DonorsManagementSection: React.FC = () => {
  const [donors, setDonors] = useState<DonorItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [copiedEmails, setCopiedEmails] = useState(false);

  const fetchDonors = async () => {
    setLoading(true);
    try {
      const res = await api.get('/api/payment/coffee-donations?limit=100');
      if (Array.isArray(res.data)) {
        setDonors(res.data);
      }
    } catch (err) {
      console.error('Failed to load donors:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDonors();
  }, []);

  const totalDonated = donors.reduce((sum, d) => sum + (d.amount || 0), 0);
  const emailsList = donors.map((d) => d.donorEmail).filter(Boolean).join(', ');

  const handleCopyEmails = () => {
    if (!emailsList) return;
    navigator.clipboard.writeText(emailsList);
    setCopiedEmails(true);
    setTimeout(() => setCopiedEmails(false), 2000);
  };

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="p-5 rounded-3xl bg-slate-900/60 border border-slate-800 backdrop-blur-xl">
          <span className="text-xs text-slate-400 font-bold uppercase tracking-wider">Tổng Quỹ Ủng Hộ Cafe</span>
          <p className="text-2xl font-black text-amber-400 mt-2 font-mono">{totalDonated.toLocaleString('vi-VN')} VND</p>
          <p className="text-[11px] text-slate-500 mt-1">Được cộng đồng chung tay tài trợ</p>
        </div>

        <div className="p-5 rounded-3xl bg-slate-900/60 border border-slate-800 backdrop-blur-xl">
          <span className="text-xs text-slate-400 font-bold uppercase tracking-wider">Tổng Lượt Mời Cafe</span>
          <p className="text-2xl font-black text-rose-400 mt-2 font-mono">{donors.length}</p>
          <p className="text-[11px] text-slate-500 mt-1">Người học, giáo viên & đối tác</p>
        </div>

        <div className="p-5 rounded-3xl bg-slate-900/60 border border-slate-800 backdrop-blur-xl flex flex-col justify-between">
          <div>
            <span className="text-xs text-slate-400 font-bold uppercase tracking-wider">Danh Sách Tri Ân & Thưởng</span>
            <p className="text-xs text-slate-300 mt-1">{donors.filter(d => d.donorEmail).length} Email đã lưu</p>
          </div>
          <button
            onClick={handleCopyEmails}
            disabled={!emailsList}
            className="px-3 py-1.5 rounded-xl bg-amber-500/20 hover:bg-amber-500/30 text-amber-300 border border-amber-500/30 text-xs font-semibold flex items-center justify-center gap-1.5 transition disabled:opacity-50"
          >
            {copiedEmails ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
            {copiedEmails ? 'Đã sao chép tất cả email!' : 'Sao chép email gửi Tri ân'}
          </button>
        </div>
      </div>

      {/* Table */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-3xl overflow-hidden backdrop-blur-xl">
        <div className="p-5 border-b border-slate-800 flex justify-between items-center bg-slate-900/40">
          <div className="flex items-center gap-2">
            <Heart className="w-5 h-5 text-rose-400 fill-rose-400/20" />
            <h3 className="font-bold text-slate-200 text-sm">Danh Sách Người Mời Cafe & Nhà Tài Trợ Cộng Đồng</h3>
          </div>
          <button
            onClick={fetchDonors}
            disabled={loading}
            className="px-3 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-medium flex items-center gap-1.5 border border-slate-700 transition"
          >
            <RefreshCw className={`w-3 h-3 ${loading ? 'animate-spin text-amber-400' : ''}`} />
            Làm mới
          </button>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse min-w-[750px]">
            <thead>
              <tr className="border-b border-slate-800 text-slate-400 text-xs font-bold tracking-wider uppercase bg-slate-950/30">
                <th className="p-4">Mã Đơn</th>
                <th className="p-4">Người Ủng Hộ</th>
                <th className="p-4">Email Tri Ân</th>
                <th className="p-4 w-32">Số tiền</th>
                <th className="p-4 w-44">Danh hiệu & Voucher</th>
                <th className="p-4">Lời chúc / Lời nhắn</th>
                <th className="p-4 w-36 text-right">Thời gian</th>
              </tr>
            </thead>
            <tbody>
              {donors.length === 0 ? (
                <tr>
                  <td colSpan={7} className="p-8 text-center text-slate-500 text-xs">
                    Chưa có lượt mời cafe nào được ghi nhận.
                  </td>
                </tr>
              ) : (
                donors.map((d) => (
                  <tr key={d.orderCode} className="border-b border-slate-800/60 hover:bg-slate-800/20 transition">
                    <td className="p-4 font-mono text-xs text-amber-400">#{d.orderCode}</td>
                    <td className="p-4 font-bold text-slate-200 text-xs">{d.donorName || 'Ẩn danh'}</td>
                    <td className="p-4 text-xs font-mono text-cyan-400">{d.donorEmail || '—'}</td>
                    <td className="p-4 font-bold text-emerald-400 text-xs font-mono">
                      {d.amount?.toLocaleString('vi-VN')} đ
                    </td>
                    <td className="p-4">
                      <div className="space-y-0.5">
                        <span className="text-[10px] font-bold text-amber-300 block">{d.rewardTier}</span>
                        <span className="text-[10px] font-mono text-slate-400 bg-slate-800 px-1.5 py-0.5 rounded">
                          {d.rewardVoucherCode}
                        </span>
                      </div>
                    </td>
                    <td className="p-4 text-xs text-slate-300 italic max-w-xs truncate" title={d.message}>
                      "{d.message || 'Mời team ly cafe!'}"
                    </td>
                    <td className="p-4 text-right text-slate-500 text-xs font-mono">
                      {d.createdAt ? d.createdAt.replace('T', ' ').slice(0, 16) : '—'}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
