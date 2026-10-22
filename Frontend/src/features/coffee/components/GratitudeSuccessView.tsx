import React from 'react';
import { Heart, Gift, Copy, Check, Award, Sparkles } from 'lucide-react';

interface GratitudeSuccessViewProps {
  donorName: string;
  donorEmail: string;
  amount: number;
  rewardTier: string;
  voucherCode: string;
  onClose: () => void;
}

export const GratitudeSuccessView: React.FC<GratitudeSuccessViewProps> = ({
  donorName,
  donorEmail,
  amount,
  rewardTier,
  voucherCode,
  onClose,
}) => {
  const [copied, setCopied] = React.useState(false);

  const copyVoucher = () => {
    navigator.clipboard.writeText(voucherCode);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="text-center py-4 space-y-5 animate-fade-in">
      <div className="w-16 h-16 rounded-full bg-gradient-to-tr from-amber-500 to-rose-500 text-white flex items-center justify-center mx-auto shadow-lg shadow-amber-500/20 animate-bounce">
        <Heart className="w-8 h-8 fill-white" />
      </div>

      <div className="space-y-1">
        <span className="text-xs px-3 py-1 rounded-full bg-amber-500/20 border border-amber-500/30 text-amber-300 font-bold uppercase tracking-wider inline-flex items-center gap-1.5">
          <Award className="w-3.5 h-3.5 text-amber-400" />
          {rewardTier}
        </span>
        <h2 className="text-2xl font-black bg-gradient-to-r from-amber-200 via-amber-400 to-orange-400 bg-clip-text text-transparent">
          Cảm Ơn Tấm Lòng Vàng Của Bạn!
        </h2>
        <p className="text-xs text-slate-300 max-w-sm mx-auto leading-relaxed">
          Đóng góp <strong className="text-emerald-400 font-mono">{amount.toLocaleString('vi-VN')} VND</strong> từ{' '}
          <strong className="text-white">{donorName || 'Bạn'}</strong> đã được hệ thống ghi nhận vào Quỹ Phát Triển Giáo Dục AegisQuiz.
        </p>
      </div>

      {/* Thẻ Quà Tặng Tri Ân / Voucher */}
      <div className="p-4 rounded-2xl bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950 border border-amber-500/30 text-left space-y-3 relative overflow-hidden shadow-xl">
        <div className="absolute top-0 right-0 w-24 h-24 bg-amber-500/10 rounded-full blur-xl pointer-events-none" />
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Gift className="w-4 h-4 text-amber-400" />
            <h4 className="text-xs font-bold text-amber-300 uppercase tracking-wide">Mã Quà Tặng Tri Ân Đặc Quyền</h4>
          </div>
          <span className="text-[10px] text-emerald-400 font-mono font-bold bg-emerald-500/10 px-2 py-0.5 rounded-full">
            VIP Supporter
          </span>
        </div>

        <p className="text-[11px] text-slate-400">
          Tặng riêng cho <span className="text-slate-200 font-semibold">{donorEmail || 'nhà tài trợ'}</span> để kích hoạt đặc quyền VIP hoặc giảm 50% các khóa đào tạo & tài nguyên thi chuyên sâu:
        </p>

        <div className="flex items-center justify-between p-2.5 rounded-xl bg-slate-950 border border-slate-800">
          <span className="font-mono font-black text-amber-400 text-sm tracking-widest">{voucherCode}</span>
          <button
            onClick={copyVoucher}
            className="flex items-center gap-1 px-2.5 py-1 rounded-lg bg-amber-500/20 hover:bg-amber-500/30 text-amber-300 text-xs font-semibold transition"
          >
            {copied ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
            <span>{copied ? 'Đã sao chép' : 'Sao chép'}</span>
          </button>
        </div>
      </div>

      <p className="text-[11px] text-slate-400 flex items-center justify-center gap-1">
        <Sparkles className="w-3 h-3 text-amber-400" />
        Email của bạn đã được lưu vào Danh Sách Tri Ân Danh Dự của hệ thống.
      </p>

      <button
        onClick={onClose}
        className="w-full py-2.5 rounded-xl bg-gradient-to-r from-amber-500 to-orange-500 hover:from-amber-400 hover:to-orange-400 text-slate-950 font-bold text-xs uppercase tracking-wider shadow-lg transition"
      >
        Hoàn Tất & Tiếp Tục Trải Nghiệm
      </button>
    </div>
  );
};
