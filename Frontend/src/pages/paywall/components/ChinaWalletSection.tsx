import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { QrCode, RefreshCw, Zap, ShieldCheck, CheckCircle2 } from 'lucide-react';
import api from '@/services/api';

interface ChinaWalletSectionProps {
  planTier: 'PRO' | 'ENTERPRISE';
  amountVnd: number;
  onPaymentSuccess: () => void;
}

export const ChinaWalletSection: React.FC<ChinaWalletSectionProps> = ({
  planTier,
  amountVnd,
  onPaymentSuccess,
}) => {
  const { t } = useTranslation('paywall');
  const [walletType, setWalletType] = useState<'alipay' | 'wechat'>('alipay');
  const [loading, setLoading] = useState(false);
  const [orderData, setOrderData] = useState<any>(null);
  const [simulating, setSimulating] = useState(false);

  // Tính số tiền CNY tương đương (1 CNY ≈ 3,500 VND)
  const amountCny = Math.max(1, Math.round(amountVnd / 3500));

  const initChinaOrder = async () => {
    setLoading(true);
    try {
      const res = await api.post('/api/payment/create-order', {
        provider: 'stripe_intl',
        planTier,
        amount: amountCny,
        currency: 'CNY',
        description: `AEGIS ${planTier} (${walletType.toUpperCase()})`,
        metadata: {
          subChannel: walletType,
          originalVnd: amountVnd,
        },
      });
      setOrderData(res.data);
    } catch (err) {
      console.error('Failed to create China wallet order:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    initChinaOrder();
  }, [walletType, planTier, amountVnd]);

  // Polling trạng thái
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
        // Silent catch
      }
    }, 3000);

    return () => clearInterval(interval);
  }, [orderData, onPaymentSuccess]);

  const handleSimulate = async () => {
    if (!orderData?.orderCode) return;
    setSimulating(true);
    try {
      await api.post(`/api/payment/simulate-success/${orderData.orderCode}`);
      onPaymentSuccess();
    } catch (err) {
      console.error(err);
    } finally {
      setSimulating(false);
    }
  };

  return (
    <div className="bg-slate-900/60 p-6 rounded-2xl border border-slate-800 backdrop-blur-md space-y-5">
      {/* Chọn Ví Alipay / WeChat */}
      <div className="flex items-center justify-between pb-3 border-b border-slate-800">
        <div className="flex items-center gap-2">
          <ShieldCheck className="w-5 h-5 text-emerald-400" />
          <h3 className="font-semibold text-slate-100 text-sm md:text-base">{t('china.title')}</h3>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setWalletType('alipay')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition ${
              walletType === 'alipay'
                ? 'bg-blue-600 text-white shadow'
                : 'bg-slate-800 text-slate-400 hover:text-slate-200'
            }`}
          >
            <span className="w-2 h-2 rounded-full bg-blue-300"></span>
            {t('china.alipay')}
          </button>
          <button
            onClick={() => setWalletType('wechat')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition ${
              walletType === 'wechat'
                ? 'bg-emerald-600 text-white shadow'
                : 'bg-slate-800 text-slate-400 hover:text-slate-200'
            }`}
          >
            <span className="w-2 h-2 rounded-full bg-emerald-300"></span>
            {t('china.wechat')}
          </button>
        </div>
      </div>

      <p className="text-xs text-slate-400">{t('china.desc')}</p>

      {/* Box hiển thị QR */}
      <div className="grid grid-cols-1 md:grid-cols-12 gap-6 items-center">
        <div className="md:col-span-5 flex flex-col items-center p-4 bg-slate-950/80 rounded-xl border border-slate-800">
          <div className="p-3 bg-white rounded-xl shadow-xl mb-3">
            {loading ? (
              <div className="w-44 h-44 flex items-center justify-center">
                <RefreshCw className="w-8 h-8 animate-spin text-blue-500" />
              </div>
            ) : orderData?.qrCode ? (
              <img src={orderData.qrCode} alt="China Wallet QR" className="w-44 h-44 object-contain rounded" />
            ) : (
              <div className="w-44 h-44 flex items-center justify-center bg-slate-100 rounded">
                <QrCode className="w-20 h-20 text-slate-400" />
              </div>
            )}
          </div>
          <span className="text-[11px] text-slate-400">{t('china.waiting_scan')}</span>
        </div>

        <div className="md:col-span-7 space-y-3">
          <div className="p-3 rounded-xl bg-slate-800/40 border border-slate-700/50 space-y-2">
            <div className="flex justify-between items-center text-xs">
              <span className="text-slate-400">应付金额 (Amount):</span>
              <span className="text-xl font-bold font-mono text-amber-400">¥ {amountCny} CNY</span>
            </div>
            <div className="flex justify-between items-center text-[11px] text-slate-500">
              <span>{t('china.rate_hint')}</span>
              <span>≈ {Number(amountVnd).toLocaleString('vi-VN')} VND</span>
            </div>
            <div className="flex justify-between items-center text-xs pt-2 border-t border-slate-700/50">
              <span className="text-slate-400">订单号 (Order Code):</span>
              <span className="font-mono text-slate-300 text-xs">#{orderData?.orderCode || '...'}</span>
            </div>
          </div>

          <div className="flex flex-col sm:flex-row gap-2">
            {orderData?.checkoutUrl && (
              <a
                href={orderData.checkoutUrl}
                target="_blank"
                rel="noreferrer"
                className={`flex-1 flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl font-medium text-xs text-white transition shadow ${
                  walletType === 'alipay' ? 'bg-blue-600 hover:bg-blue-500' : 'bg-emerald-600 hover:bg-emerald-500'
                }`}
              >
                <CheckCircle2 className="w-4 h-4" />
                {t('china.btn_open_wallet')}
              </a>
            )}
            <button
              onClick={handleSimulate}
              disabled={simulating}
              className="px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 border border-slate-700 text-slate-300 font-medium text-xs flex items-center justify-center gap-1.5 transition disabled:opacity-50"
            >
              {simulating ? <RefreshCw className="w-3.5 h-3.5 animate-spin" /> : <Zap className="w-3.5 h-3.5 text-amber-400" />}
              {t('vietqr.btn_simulate')}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
