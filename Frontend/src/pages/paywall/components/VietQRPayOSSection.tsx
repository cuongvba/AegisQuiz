import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { QrCode, Copy, Check, ExternalLink, RefreshCw, Zap, ShieldCheck } from 'lucide-react';
import api from '@/services/api';

interface VietQRPayOSSectionProps {
  planTier: 'PRO' | 'ENTERPRISE';
  amount: number;
  onPaymentSuccess: () => void;
}

export const VietQRPayOSSection: React.FC<VietQRPayOSSectionProps> = ({
  planTier,
  amount,
  onPaymentSuccess,
}) => {
  const { t } = useTranslation('paywall');
  const [loading, setLoading] = useState(false);
  const [orderData, setOrderData] = useState<any>(null);
  const [copiedField, setCopiedField] = useState<string | null>(null);
  const [simulating, setSimulating] = useState(false);
  const [countdown, setCountdown] = useState(900); // 15 mins

  // Khởi tạo đơn hàng qua PayOS
  const initPaymentOrder = async () => {
    setLoading(true);
    try {
      const res = await api.post('/api/payment/create-order', {
        provider: 'payos',
        planTier,
        amount,
        currency: 'VND',
        description: `AEGIS ${planTier}`,
      });
      setOrderData(res.data);
      setCountdown(900);
    } catch (err) {
      console.error('Failed to create PayOS order:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    initPaymentOrder();
  }, [planTier, amount]);

  // Polling lắng nghe trạng thái thanh toán từ Webhook
  useEffect(() => {
    if (!orderData?.orderCode || orderData?.status === 'completed') return;

    const interval = setInterval(async () => {
      try {
        const res = await api.get(`/api/payment/status/${orderData.orderCode}`);
        if (res.data?.status === 'completed') {
          clearInterval(interval);
          onPaymentSuccess();
        }
      } catch {
        // Silent catch for network polling
      }
    }, 2500);

    return () => clearInterval(interval);
  }, [orderData, onPaymentSuccess]);

  // Đếm ngược QR
  useEffect(() => {
    if (countdown <= 0) return;
    const timer = setInterval(() => setCountdown((c) => c - 1), 1000);
    return () => clearInterval(timer);
  }, [countdown]);

  const copyToClipboard = (text: string, field: string) => {
    navigator.clipboard.writeText(text);
    setCopiedField(field);
    setTimeout(() => setCopiedField(null), 2000);
  };

  const handleSimulateSandbox = async () => {
    if (!orderData?.orderCode) return;
    setSimulating(true);
    try {
      await api.post(`/api/payment/simulate-success/${orderData.orderCode}`);
      onPaymentSuccess();
    } catch (err) {
      console.error('Simulate failed:', err);
    } finally {
      setSimulating(false);
    }
  };

  const formatCountdown = (seconds: number) => {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s < 10 ? '0' : ''}${s}`;
  };

  if (loading) {
    return (
      <div className="flex flex-col items-center justify-center p-12 text-slate-400">
        <RefreshCw className="w-8 h-8 animate-spin text-cyan-400 mb-3" />
        <p className="text-sm font-medium">{t('vietqr.checking_status', 'Đang kết nối cổng VietQR PayOS...')}</p>
      </div>
    );
  }

  const { qrCode, checkoutUrl, accountNumber, accountName, bankName, description, orderCode } =
    orderData || {};

  return (
    <div className="grid grid-cols-1 md:grid-cols-12 gap-6 bg-slate-900/60 p-6 rounded-2xl border border-slate-800 backdrop-blur-md">
      {/* Cột trái: QR Code & Quét mã */}
      <div className="md:col-span-5 flex flex-col items-center justify-center p-4 bg-slate-950/70 rounded-xl border border-slate-800 text-center">
        <div className="relative p-2 bg-white rounded-xl shadow-2xl shadow-cyan-500/10 mb-3">
          {qrCode ? (
            <img src={qrCode} alt="VietQR PayOS" className="w-52 h-52 object-contain rounded-lg" />
          ) : (
            <div className="w-52 h-52 flex items-center justify-center bg-slate-100 rounded-lg">
              <QrCode className="w-24 h-24 text-slate-400" />
            </div>
          )}
          <div className="absolute -bottom-2 left-1/2 -translate-x-1/2 bg-cyan-600 text-[10px] text-white px-2 py-0.5 rounded-full font-bold uppercase tracking-wider flex items-center gap-1 shadow">
            <Zap className="w-3 h-3" /> Napas 24/7
          </div>
        </div>

        <p className="text-xs text-slate-400 mt-2">{t('vietqr.scan_hint')}</p>
        <div className="text-[11px] text-cyan-400 font-semibold mt-1">
          {t('vietqr.time_remaining')} <span className="font-mono text-xs">{formatCountdown(countdown)}</span>
        </div>
      </div>

      {/* Cột phải: Chi tiết chuyển khoản & Nút thao tác */}
      <div className="md:col-span-7 flex flex-col justify-between space-y-4">
        <div className="space-y-2.5">
          <div className="flex items-center justify-between pb-2 border-b border-slate-800">
            <div className="flex items-center gap-2">
              <ShieldCheck className="w-5 h-5 text-emerald-400" />
              <h3 className="font-semibold text-slate-100 text-sm md:text-base">{t('vietqr.title')}</h3>
            </div>
            <span className="text-xs text-slate-400 font-mono">#{orderCode}</span>
          </div>

          <div className="grid grid-cols-1 gap-2 text-xs">
            {/* Ngân hàng */}
            <div className="flex justify-between items-center p-2 rounded-lg bg-slate-800/40">
              <span className="text-slate-400">{t('vietqr.bank_name')}:</span>
              <span className="font-bold text-slate-200">{bankName || 'MB Bank / Vietcombank'}</span>
            </div>

            {/* Số tài khoản */}
            <div className="flex justify-between items-center p-2 rounded-lg bg-slate-800/40">
              <span className="text-slate-400">{t('vietqr.account_number')}:</span>
              <div className="flex items-center gap-2">
                <span className="font-mono font-bold text-cyan-400 text-sm">{accountNumber || '0987654321'}</span>
                <button
                  onClick={() => copyToClipboard(accountNumber || '0987654321', 'acc')}
                  className="p-1 hover:bg-slate-700 rounded text-slate-400"
                >
                  {copiedField === 'acc' ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
                </button>
              </div>
            </div>

            {/* Chủ tài khoản */}
            <div className="flex justify-between items-center p-2 rounded-lg bg-slate-800/40">
              <span className="text-slate-400">{t('vietqr.account_name')}:</span>
              <span className="font-bold text-slate-200 uppercase">{accountName || 'AEGISQUIZ CORP'}</span>
            </div>

            {/* Số tiền */}
            <div className="flex justify-between items-center p-2 rounded-lg bg-slate-800/40">
              <span className="text-slate-400">{t('vietqr.amount')}:</span>
              <span className="font-mono font-bold text-emerald-400 text-base">
                {Number(amount).toLocaleString('vi-VN')} VND
              </span>
            </div>

            {/* Nội dung CK */}
            <div className="flex justify-between items-center p-2 rounded-lg bg-amber-500/10 border border-amber-500/20">
              <span className="text-amber-300 font-medium">{t('vietqr.transfer_content')}:</span>
              <div className="flex items-center gap-2">
                <span className="font-mono font-bold text-amber-300 text-xs">{description || `AEGIS ${orderCode}`}</span>
                <button
                  onClick={() => copyToClipboard(description || `AEGIS ${orderCode}`, 'desc')}
                  className="p-1 hover:bg-amber-500/20 rounded text-amber-300"
                >
                  {copiedField === 'desc' ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
                </button>
              </div>
            </div>
          </div>
        </div>

        {/* Nút hành động */}
        <div className="flex flex-col sm:flex-row gap-2 pt-2">
          {checkoutUrl && (
            <a
              href={checkoutUrl}
              target="_blank"
              rel="noreferrer"
              className="flex-1 flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl bg-cyan-600 hover:bg-cyan-500 text-white font-medium text-xs transition shadow"
            >
              <ExternalLink className="w-4 h-4" />
              {t('vietqr.btn_open_app')}
            </a>
          )}
          <button
            onClick={handleSimulateSandbox}
            disabled={simulating}
            className="flex-1 flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 border border-slate-700 text-slate-200 font-medium text-xs transition disabled:opacity-50"
          >
            {simulating ? <RefreshCw className="w-3.5 h-3.5 animate-spin" /> : <Zap className="w-3.5 h-3.5 text-amber-400" />}
            {t('vietqr.btn_simulate')}
          </button>
        </div>
      </div>
    </div>
  );
};
