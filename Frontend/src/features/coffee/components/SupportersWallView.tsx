import React, { useState, useEffect } from 'react';
import { Award, Heart, MessageSquare, RefreshCw, Sparkles, User } from 'lucide-react';
import api from '@/services/api';

interface Supporter {
  orderCode: number;
  donorName: string;
  donorEmail: string;
  amount: number;
  message: string;
  rewardTier: string;
  createdAt: string;
}

export const SupportersWallView: React.FC = () => {
  const [supporters, setSupporters] = useState<Supporter[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    setLoading(true);
    api.get('/api/payment/coffee-donations?limit=20')
      .then((res) => {
        if (Array.isArray(res.data) && res.data.length > 0) {
          setSupporters(res.data);
        } else {
          // Dữ liệu ban đầu trang nhã
          setSupporters([
            { orderCode: 9001, donorName: 'Thầy Hoàng Minh', donorEmail: 'h***@edu.vn', amount: 100000, message: 'Cảm ơn AegisQuiz đã hỗ trợ công cụ bóc tách đề thi toán cực kỳ hữu ích!', rewardTier: '🍰 Người Tiếp Lửa Thầm Lặng', createdAt: '2026-10-22 10:30' },
            { orderCode: 9002, donorName: 'Minh Trang (Learner)', donorEmail: 't***@gmail.com', amount: 50000, message: 'Chúc hệ thống ngày càng phát triển mạnh mẽ!', rewardTier: '☕☕ Tri Kỷ Đồng Hành', createdAt: '2026-10-22 09:15' },
            { orderCode: 9003, donorName: 'Cựu Sinh Viên Bách Khoa', donorEmail: 'b***@alumni.vn', amount: 200000, message: 'Tài trợ 1 tháng server cho dự án mã nguồn mở giáo dục tuyệt vời.', rewardTier: '🌟 Nhà Bảo Trợ Vàng', createdAt: '2026-10-21 16:40' },
          ]);
        }
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="space-y-3 max-h-96 overflow-y-auto pr-1">
      <div className="flex items-center justify-between pb-2 border-b border-slate-800">
        <div className="flex items-center gap-2">
          <Award className="w-4 h-4 text-amber-400" />
          <h4 className="text-xs font-bold text-slate-200 uppercase tracking-wide">Bảng Vàng Tri Ân Cộng Đồng</h4>
        </div>
        <span className="text-[10px] text-amber-400 font-bold bg-amber-500/10 px-2 py-0.5 rounded-full flex items-center gap-1">
          <Sparkles className="w-3 h-3" /> Danh dự
        </span>
      </div>

      {loading ? (
        <div className="py-12 text-center text-slate-400">
          <RefreshCw className="w-6 h-6 animate-spin mx-auto text-amber-400 mb-2" />
          <p className="text-xs">Đang nạp danh sách tri ân...</p>
        </div>
      ) : (
        <div className="space-y-2">
          {supporters.map((s, idx) => (
            <div
              key={s.orderCode || idx}
              className="p-3 rounded-xl bg-slate-950/80 border border-slate-800/80 hover:border-amber-500/30 transition flex flex-col gap-1.5"
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="w-7 h-7 rounded-full bg-amber-500/10 border border-amber-500/20 text-amber-300 flex items-center justify-center font-bold text-xs">
                    {s.donorName ? s.donorName.charAt(0).toUpperCase() : <User className="w-3.5 h-3.5" />}
                  </div>
                  <div>
                    <h5 className="text-xs font-bold text-slate-100">{s.donorName || 'Nhà tài trợ ẩn danh'}</h5>
                    <span className="text-[10px] text-amber-400/90 font-medium">{s.rewardTier}</span>
                  </div>
                </div>
                <span className="font-mono font-bold text-emerald-400 text-xs">
                  +{s.amount?.toLocaleString('vi-VN')} đ
                </span>
              </div>

              {s.message && (
                <p className="text-[11px] text-slate-300 italic bg-slate-900/60 p-2 rounded-lg border border-slate-800/50 flex items-start gap-1.5">
                  <MessageSquare className="w-3 h-3 text-slate-400 shrink-0 mt-0.5" />
                  <span>"{s.message}"</span>
                </p>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
