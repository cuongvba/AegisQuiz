import React, { useState, useEffect } from 'react';
import { Coffee, Heart, X, Copy, Check, Sparkles, RefreshCw, Award, Landmark, ExternalLink } from 'lucide-react';
import { QRCodeSVG } from 'qrcode.react';
import confetti from 'canvas-confetti';
import api from '@/services/api';
import { GratitudeSuccessView } from './components/GratitudeSuccessView';
import { SupportersWallView } from './components/SupportersWallView';

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
  const [activeTab, setActiveTab] = useState<'donate' | 'wall'>('donate');
  const [amount, setAmount] = useState<number>(25000);
  const [customAmount, setCustomAmount] = useState<string>('');
  const [donorName, setDonorName] = useState<string>('');
  const [donorEmail, setDonorEmail] = useState<string>('');
  const [message, setMessage] = useState<string>('Chúc AegisQuiz ngày càng phát triển!');
  const [copiedField, setCopiedField] = useState<string | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [isSuccess, setIsSuccess] = useState<boolean>(false);
  const [donationResult, setDonationResult] = useState<any>(null);
  const [useSvgQr, setUseSvgQr] = useState<boolean>(true);

  const [submitting, setSubmitting] = useState<boolean>(false);

  // Tạo đơn đóng góp và lấy chính xác thông tin tài khoản thụ hưởng từ hệ thống
  const initDonationOrder = async (targetAmount: number) => {
    setLoading(true);
    try {
      const res = await api.post('/api/payment/coffee-donation', {
        donorName: donorName.trim() || 'Bạn Đọc Yêu Quý',
        donorEmail: donorEmail.trim() || 'supporter@aegisquiz.com',
        amount: targetAmount,
        message: message.trim() || 'Mời team ly cafe!',
      });
      setDonationResult(res.data);
    } catch (err) {
      console.error('Failed to init donation:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      setIsSuccess(false);
      setActiveTab('donate');
      initDonationOrder(amount);
    }
  }, [isOpen]);

  const copyToClipboard = (text: string, field: string) => {
    if (!text) return;
    navigator.clipboard.writeText(text);
    setCopiedField(field);
    setTimeout(() => setCopiedField(null), 2000);
  };

  const handleAmountChange = (newAmount: number) => {
    setAmount(newAmount);
    setCustomAmount('');
    initDonationOrder(newAmount);
  };

  const handleConfirmSent = async () => {
    setSubmitting(true);
    try {
      const res = await api.post('/api/payment/coffee-donation', {
        donorName: donorName.trim() || 'Người Bạn Quý Giả',
        donorEmail: donorEmail.trim(),
        amount: currentAmount,
        message: message.trim() || 'Mời team ly cafe!',
      });
      if (res.data) {
        setDonationResult(res.data);
      }
      setIsSuccess(true);
      confetti({
        particleCount: 150,
        spread: 80,
        origin: { y: 0.6 },
        colors: ['#f59e0b', '#ec4899', '#3b82f6', '#10b981'],
      });
    } catch (err) {
      console.error('Failed to record donation:', err);
      setIsSuccess(true);
    } finally {
      setSubmitting(false);
    }
  };

  const currentAmount = customAmount ? Math.max(10000, Number(customAmount.replace(/\D/g, '')) || 0) : amount;

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-md animate-fade-in">
      <div className="relative w-full max-w-lg bg-slate-900 border border-amber-500/30 rounded-3xl p-6 shadow-2xl text-slate-100 overflow-hidden">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 p-2 text-slate-400 hover:text-white rounded-full hover:bg-slate-800 transition"
        >
          <X className="w-5 h-5" />
        </button>

        {isSuccess ? (
          <GratitudeSuccessView
            donorName={donorName || 'Bạn'}
            donorEmail={donorEmail || 'Nhà tài trợ'}
            amount={currentAmount}
            rewardTier={donationResult?.rewardTier || '☕ Người Gieo Mầm Tri Thức'}
            voucherCode={donationResult?.rewardVoucherCode || 'AEGIS-THANKS-8899'}
            onClose={onClose}
          />
        ) : (
          <div className="space-y-4">
            {/* Header Tabs: Mời Cafe / Bảng Vàng */}
            <div className="flex items-center justify-between pb-2 border-b border-slate-800">
              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => setActiveTab('donate')}
                  className={`px-3 py-1.5 rounded-xl text-xs font-bold transition flex items-center gap-1.5 ${
                    activeTab === 'donate'
                      ? 'bg-amber-500 text-slate-950 shadow'
                      : 'text-slate-400 hover:text-slate-200'
                  }`}
                >
                  <Coffee className="w-3.5 h-3.5" />
                  <span>Mời Cafe & Tài Trợ</span>
                </button>
                <button
                  type="button"
                  onClick={() => setActiveTab('wall')}
                  className={`px-3 py-1.5 rounded-xl text-xs font-bold transition flex items-center gap-1.5 ${
                    activeTab === 'wall'
                      ? 'bg-amber-500 text-slate-950 shadow'
                      : 'text-slate-400 hover:text-slate-200'
                  }`}
                >
                  <Award className="w-3.5 h-3.5" />
                  <span>Bảng Vàng Tri Ân</span>
                </button>
              </div>
            </div>

            {activeTab === 'wall' ? (
              <SupportersWallView />
            ) : (
              <div className="space-y-4">
                {/* Presets */}
                <div className="grid grid-cols-4 gap-2">
                  {PRESET_AMOUNTS.map((p) => (
                    <button
                      key={p.value}
                      type="button"
                      onClick={() => handleAmountChange(p.value)}
                      className={`p-2 rounded-2xl border text-center transition flex flex-col items-center gap-0.5 ${
                        !customAmount && amount === p.value
                          ? 'bg-amber-500/20 border-amber-400 text-amber-300 shadow-md'
                          : 'bg-slate-950/60 border-slate-800 text-slate-400 hover:border-slate-700'
                      }`}
                    >
                      <span className="text-base">{p.icon}</span>
                      <span className="text-[11px] font-bold">{p.label}</span>
                      <span className="text-[10px] font-mono text-slate-400">{(p.value / 1000).toLocaleString()}k</span>
                    </button>
                  ))}
                </div>

                {/* Tên & Email người ủng hộ */}
                <div className="grid grid-cols-2 gap-2.5 text-xs">
                  <div>
                    <label className="block text-slate-400 mb-1">Tên bạn (hoặc Biệt danh)</label>
                    <input
                      type="text"
                      placeholder="VD: Cường Nguyễn..."
                      value={donorName}
                      onChange={(e) => setDonorName(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-slate-200 focus:border-amber-500 focus:outline-none"
                    />
                  </div>
                  <div>
                    <label className="block text-slate-400 mb-1">Email (nhận Voucher tri ân)</label>
                    <input
                      type="email"
                      placeholder="email@example.com"
                      value={donorEmail}
                      onChange={(e) => setDonorEmail(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-cyan-400 focus:border-amber-500 focus:outline-none"
                    />
                  </div>
                </div>

                {/* QR Code Napas VietQR Chuẩn 100% Theo Tài Khoản Thụ Hưởng */}
                <div className="p-3.5 bg-slate-950/90 rounded-2xl border border-slate-800 flex flex-col sm:flex-row items-center gap-4">
                  <div className="p-2.5 bg-white rounded-2xl shadow-xl shrink-0 flex flex-col items-center justify-center">
                    {loading ? (
                      <div className="w-44 h-44 flex flex-col items-center justify-center text-slate-700">
                        <RefreshCw className="w-6 h-6 animate-spin text-amber-600 mb-1" />
                        <span className="text-[10px] font-medium">Đang nạp mã...</span>
                      </div>
                    ) : useSvgQr && donationResult?.emvCoPayload ? (
                      <div className="p-1 bg-white flex flex-col items-center">
                        <QRCodeSVG
                          value={donationResult.emvCoPayload}
                          size={160}
                          level="M"
                          includeMargin={false}
                          className="rounded"
                        />
                      </div>
                    ) : donationResult?.qrImageUrl ? (
                      <img
                        src={donationResult.qrImageUrl}
                        alt="VietQR Napas 24/7"
                        className="w-44 h-44 object-contain rounded-lg"
                        onError={() => setUseSvgQr(true)}
                      />
                    ) : (
                      <div className="w-44 h-44 flex items-center justify-center text-slate-400">
                        <Landmark className="w-10 h-10" />
                      </div>
                    )}
                    <div className="flex items-center gap-2 mt-1.5">
                      <span className="text-[9px] text-slate-900 font-extrabold uppercase tracking-wider">
                        Napas VietQR 24/7
                      </span>
                      {donationResult?.qrImageUrl && (
                        <button
                          type="button"
                          onClick={() => setUseSvgQr(!useSvgQr)}
                          className="text-[9px] text-blue-600 underline font-semibold hover:text-blue-800"
                        >
                          {useSvgQr ? 'Phiếu VietQR' : 'QR Vector'}
                        </button>
                      )}
                    </div>
                  </div>

                  <div className="flex-1 space-y-1.5 text-xs w-full">
                    <div className="flex justify-between items-center pb-1 border-b border-slate-800">
                      <span className="text-slate-400">Ngân hàng:</span>
                      <span className="font-bold text-slate-200">{donationResult?.bankName || 'Đang tải...'}</span>
                    </div>
                    <div className="flex justify-between items-center pb-1 border-b border-slate-800">
                      <span className="text-slate-400">Số tài khoản:</span>
                      <div className="flex items-center gap-1.5">
                        <span className="font-mono font-bold text-amber-400 text-sm">{donationResult?.accountNumber || '...'}</span>
                        {donationResult?.accountNumber && (
                          <button
                            type="button"
                            onClick={() => copyToClipboard(donationResult.accountNumber, 'acc')}
                            className="p-1 hover:bg-slate-800 rounded text-slate-400"
                          >
                            {copiedField === 'acc' ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                          </button>
                        )}
                      </div>
                    </div>
                    <div className="flex justify-between items-center pb-1 border-b border-slate-800">
                      <span className="text-slate-400">Chủ tài khoản:</span>
                      <span className="font-bold text-slate-200 uppercase">{donationResult?.accountName || '...'}</span>
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
                        <span className="font-mono font-bold text-amber-300">{donationResult?.transferContent || '...'}</span>
                        {donationResult?.transferContent && (
                          <button
                            type="button"
                            onClick={() => copyToClipboard(donationResult.transferContent, 'memo')}
                            className="p-1 hover:bg-slate-800 rounded text-amber-400"
                          >
                            {copiedField === 'memo' ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                </div>

                {/* Action Button */}
                <button
                  type="button"
                  disabled={loading || submitting || !donationResult}
                  onClick={handleConfirmSent}
                  className="w-full py-3 rounded-2xl bg-gradient-to-r from-amber-500 via-orange-500 to-amber-600 hover:from-amber-400 hover:to-orange-400 text-slate-950 font-bold text-xs uppercase tracking-wider flex items-center justify-center gap-2 shadow-lg shadow-amber-500/20 transition disabled:opacity-50"
                >
                  {submitting ? (
                    <>
                      <RefreshCw className="w-4 h-4 animate-spin text-slate-950" />
                      <span>Đang lưu thông tin tri ân...</span>
                    </>
                  ) : (
                    <>
                      <Heart className="w-4 h-4 fill-slate-950" />
                      <span>Xác Nhận Đã Chuyển Khoản & Nhận Quà Tri Ân</span>
                    </>
                  )}
                </button>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};
