import React, { useState, useEffect } from 'react';
import { Coffee, Heart, X, Copy, Check, Sparkles, QrCode, ShieldCheck, RefreshCw } from 'lucide-react';
import confetti from 'canvas-confetti';
import api from '@/services/api';

interface BuyMeCoffeeModalProps {
  isOpen: boolean;
  onClose: () => void;
}

const PRESET_AMOUNTS = [
  { value: 25000, label: '1 Ly Cafe', icon: '☕' },
  { value: 50000, label: '2 Ly Cafe', icon: '☕☕' },
  { value: 100000, label: 'Cafe & Bánh', icon: '🍰' },
  { value: 200000, label: 'Nuôi Server', icon: '🚀' },
];

export const BuyMeCoffeeModal: React.FC<BuyMeCoffeeModalProps> = ({ isOpen, onClose }) => {
  const [amount, setAmount] = useState<number>(25000);
  const [customAmount, setCustomAmount] = useState<string>('');
  const [senderName, setSenderName] = useState<string>('');
  const [message, setMessage] = useState<string>('Chúc AegisQuiz ngày càng phát triển!');
  const [copiedField, setCopiedField] = useState<string | null>(null);
  const [isSent, setIsSent] = useState<boolean>(false);
  const [loading, setLoading] = useState<boolean>(false);

  const [bankInfo, setBankInfo] = useState({
    bin: '970422',
    bankName: 'MB Bank (Ngân hàng Quân Đội)',
    accountNumber: '0868888999',
    accountName: 'AEGISQUIZ CORP',
  });

  useEffect(() => {
    if (isOpen) {
      setIsSent(false);
      setLoading(true);
      api.get('/api/payment/config')
        .then((res) => {
          if (res.data) {
            setBankInfo({
              bin: res.data.vietQrBankBin || '970422',
              bankName: res.data.vietQrBankName || 'MB Bank',
              accountNumber: res.data.vietQrAccountNumber || '0868888999',
              accountName: res.data.vietQrAccountName || 'AEGISQUIZ CORP',
            });
          }
        })
        .catch(() => {})
        .finally(() => setLoading(false));
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const currentAmount = customAmount ? Math.max(10000, Number(customAmount.replace(/\D/g, '')) || 0) : amount;
  const cleanSender = senderName.trim().replace(/[^a-zA-Z0-9 ]/g, '') || 'AN DANH';
  const transferContent = `CAFE ${cleanSender}`.slice(0, 25).toUpperCase();

  // URL sinh mã VietQR Napas 24/7 trực tiếp cho tài khoản thụ hưởng
  const qrUrl = `https://img.vietqr.io/image/${bankInfo.bin}-${bankInfo.accountNumber}-compact2.png?amount=${currentAmount}&addInfo=${encodeURIComponent(
    transferContent
  )}&accountName=${encodeURIComponent(bankInfo.accountName)}`;

  const copyToClipboard = (text: string, field: string) => {
    navigator.clipboard.writeText(text);
    setCopiedField(field);
    setTimeout(() => setCopiedField(null), 2000);
  };

  const handleConfirmSent = () => {
    setIsSent(true);
    confetti({
      particleCount: 120,
      spread: 70,
      origin: { y: 0.6 },
      colors: ['#f59e0b', '#ec4899', '#3b82f6', '#10b981'],
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-md animate-fade-in">
      <div className="relative w-full max-w-lg bg-slate-900 border border-amber-500/30 rounded-3xl p-6 shadow-2xl shadow-amber-500/10 text-slate-100 overflow-hidden">
        {/* Glow Header */}
        <div className="absolute top-0 right-0 w-36 h-36 bg-amber-500/10 rounded-full blur-2xl pointer-events-none" />

        {/* Close Button */}
        <button
          onClick={onClose}
          className="absolute top-4 right-4 p-2 text-slate-400 hover:text-white rounded-full hover:bg-slate-800 transition"
        >
          <X className="w-5 h-5" />
        </button>

        {isSent ? (
          <div className="text-center py-8 space-y-4">
            <div className="w-16 h-16 rounded-full bg-amber-500/20 text-amber-400 flex items-center justify-center mx-auto border border-amber-500/40 animate-bounce">
              <Heart className="w-8 h-8 fill-amber-400" />
            </div>
            <h2 className="text-2xl font-black bg-gradient-to-r from-amber-200 via-amber-400 to-orange-400 bg-clip-text text-transparent">
              Cảm Ơn Tấm Lòng Của Bạn!
            </h2>
            <p className="text-sm text-slate-300 max-w-sm mx-auto leading-relaxed">
              Mỗi ly cà phê và sự ủng hộ của bạn là nguồn động lực vô giá giúp đội ngũ duy trì nền tảng AegisQuiz hoàn toàn miễn phí vì cộng đồng.
            </p>
            <button
              onClick={onClose}
              className="px-6 py-2.5 rounded-xl bg-amber-500 hover:bg-amber-400 text-slate-950 font-bold text-xs shadow-lg transition"
            >
              Tiếp tục trải nghiệm
            </button>
          </div>
        ) : (
          <div className="space-y-5">
            {/* Title */}
            <div className="text-center space-y-1">
              <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-amber-500/10 border border-amber-500/20 text-amber-400 text-xs font-bold">
                <Coffee className="w-3.5 h-3.5" />
                <span>Mời Cafe & Tiếp Sức Dự Án Cộng Đồng</span>
              </div>
              <h2 className="text-xl font-black text-white">Ủng Hộ Đội Ngũ Phát Triển</h2>
              <p className="text-xs text-slate-400">AegisQuiz phục vụ miễn phí — tài trợ tùy tâm qua VietQR 24/7</p>
            </div>

            {/* Presets */}
            <div className="grid grid-cols-4 gap-2">
              {PRESET_AMOUNTS.map((p) => {
                const isSelected = !customAmount && amount === p.value;
                return (
                  <button
                    key={p.value}
                    type="button"
                    onClick={() => {
                      setAmount(p.value);
                      setCustomAmount('');
                    }}
                    className={`p-2.5 rounded-2xl border text-center transition flex flex-col items-center gap-1 ${
                      isSelected
                        ? 'bg-amber-500/20 border-amber-400 text-amber-300 shadow-md shadow-amber-500/10'
                        : 'bg-slate-950/60 border-slate-800 text-slate-400 hover:border-slate-700'
                    }`}
                  >
                    <span className="text-lg">{p.icon}</span>
                    <span className="text-[11px] font-bold">{p.label}</span>
                    <span className="text-[10px] font-mono text-slate-400">{(p.value / 1000).toLocaleString()}k</span>
                  </button>
                );
              })}
            </div>

            {/* Custom Amount & Name */}
            <div className="grid grid-cols-2 gap-3 text-xs">
              <div>
                <label className="block text-slate-400 mb-1">Số tiền tùy tâm (VND)</label>
                <input
                  type="text"
                  placeholder="Nhập số tiền khác..."
                  value={customAmount}
                  onChange={(e) => setCustomAmount(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-amber-400 font-mono font-bold focus:border-amber-500 focus:outline-none"
                />
              </div>
              <div>
                <label className="block text-slate-400 mb-1">Tên bạn (hoặc Biệt danh)</label>
                <input
                  type="text"
                  placeholder="VD: Minh Triết..."
                  value={senderName}
                  onChange={(e) => setSenderName(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-slate-200 focus:border-amber-500 focus:outline-none"
                />
              </div>
            </div>

            {/* QR Box & Bank Details */}
            <div className="p-3 bg-slate-950/90 rounded-2xl border border-slate-800/80 flex flex-col sm:flex-row items-center gap-4">
              <div className="relative p-1.5 bg-white rounded-xl shadow-lg shrink-0">
                {loading ? (
                  <div className="w-36 h-36 flex items-center justify-center">
                    <RefreshCw className="w-6 h-6 animate-spin text-amber-500" />
                  </div>
                ) : (
                  <img src={qrUrl} alt="VietQR Cafe" className="w-36 h-36 object-contain rounded" />
                )}
                <div className="absolute -bottom-1.5 left-1/2 -translate-x-1/2 bg-amber-600 text-[9px] text-white px-2 py-0.2 rounded-full font-bold uppercase tracking-wider shadow">
                  VietQR
                </div>
              </div>

              <div className="flex-1 space-y-1.5 text-xs w-full">
                <div className="flex justify-between items-center pb-1 border-b border-slate-800">
                  <span className="text-slate-400">Ngân hàng:</span>
                  <span className="font-bold text-slate-200">{bankInfo.bankName}</span>
                </div>
                <div className="flex justify-between items-center pb-1 border-b border-slate-800">
                  <span className="text-slate-400">Số tài khoản:</span>
                  <div className="flex items-center gap-1.5">
                    <span className="font-mono font-bold text-amber-400">{bankInfo.accountNumber}</span>
                    <button
                      type="button"
                      onClick={() => copyToClipboard(bankInfo.accountNumber, 'acc')}
                      className="p-1 hover:bg-slate-800 rounded text-slate-400"
                    >
                      {copiedField === 'acc' ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                    </button>
                  </div>
                </div>
                <div className="flex justify-between items-center pb-1 border-b border-slate-800">
                  <span className="text-slate-400">Chủ tài khoản:</span>
                  <span className="font-bold text-slate-200 uppercase">{bankInfo.accountName}</span>
                </div>
                <div className="flex justify-between items-center pb-1 border-b border-slate-800">
                  <span className="text-slate-400">Số tiền ủng hộ:</span>
                  <span className="font-mono font-bold text-emerald-400 text-sm">
                    {currentAmount.toLocaleString('vi-VN')} VND
                  </span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-amber-400 font-medium">Nội dung CK:</span>
                  <div className="flex items-center gap-1.5">
                    <span className="font-mono font-bold text-amber-300">{transferContent}</span>
                    <button
                      type="button"
                      onClick={() => copyToClipboard(transferContent, 'memo')}
                      className="p-1 hover:bg-slate-800 rounded text-amber-400"
                    >
                      {copiedField === 'memo' ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                    </button>
                  </div>
                </div>
              </div>
            </div>

            {/* Action Button */}
            <button
              type="button"
              onClick={handleConfirmSent}
              className="w-full py-3 rounded-2xl bg-gradient-to-r from-amber-500 via-orange-500 to-amber-600 hover:from-amber-400 hover:to-orange-400 text-slate-950 font-bold text-xs uppercase tracking-wider flex items-center justify-center gap-2 shadow-lg shadow-amber-500/20 transition"
            >
              <Heart className="w-4 h-4 fill-slate-950" />
              Tôi Đã Quét Mã Chuyển Khoản Mời Cafe
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
