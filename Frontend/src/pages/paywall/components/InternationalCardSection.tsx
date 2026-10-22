import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CreditCard, Lock, ShieldCheck, RefreshCw, CheckCircle2 } from 'lucide-react';
import api from '@/services/api';

interface InternationalCardSectionProps {
  planTier: 'PRO' | 'ENTERPRISE';
  amountVnd: number;
  onPaymentSuccess: () => void;
}

export const InternationalCardSection: React.FC<InternationalCardSectionProps> = ({
  planTier,
  amountVnd,
  onPaymentSuccess,
}) => {
  const { t } = useTranslation('paywall');
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [cardNumber, setCardNumber] = useState('');
  const [expiry, setExpiry] = useState('');
  const [cvc, setCvc] = useState('');
  const [cardHolder, setCardHolder] = useState('');

  // Quy đổi USD (1 USD ≈ 25,400 VND)
  const amountUsd = Math.max(1, Math.round(amountVnd / 25400));

  const handleCheckout = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      // 1. Tạo đơn thanh toán quốc tế
      const res = await api.post('/api/payment/create-order', {
        provider: 'stripe_intl',
        planTier,
        amount: amountUsd,
        currency: 'USD',
        description: `AEGIS ${planTier} (Card)`,
        metadata: {
          cardLast4: cardNumber.replace(/\s/g, '').slice(-4) || '4242',
          holder: cardHolder,
        },
      });

      const orderCode = res.data?.orderCode;

      // 2. Kích hoạt hoàn tất thanh toán
      if (orderCode) {
        await api.post(`/api/payment/simulate-success/${orderCode}`);
      }

      setSuccess(true);
      setTimeout(() => {
        onPaymentSuccess();
      }, 1200);
    } catch (err) {
      console.error('Card payment failed:', err);
    } finally {
      setLoading(false);
    }
  };

  const formatCardNumber = (value: string) => {
    const v = value.replace(/\s+/g, '').replace(/[^0-9]/gi, '');
    const matches = v.match(/\d{4,16}/g);
    const match = (matches && matches[0]) || '';
    const parts = [];
    for (let i = 0, len = match.length; i < len; i += 4) {
      parts.push(match.substring(i, i + 4));
    }
    return parts.length ? parts.join(' ') : value;
  };

  return (
    <div className="bg-slate-900/60 p-6 rounded-2xl border border-slate-800 backdrop-blur-md space-y-5">
      <div className="flex items-center justify-between pb-3 border-b border-slate-800">
        <div className="flex items-center gap-2">
          <CreditCard className="w-5 h-5 text-cyan-400" />
          <h3 className="font-semibold text-slate-100 text-sm md:text-base">{t('intl.title')}</h3>
        </div>
        <div className="flex items-center gap-1.5 text-slate-400 text-xs font-medium">
          <Lock className="w-3.5 h-3.5 text-emerald-400" />
          <span>Stripe Global PCI-DSS</span>
        </div>
      </div>

      <p className="text-xs text-slate-400">{t('intl.desc')}</p>

      <form onSubmit={handleCheckout} className="space-y-4 max-w-lg mx-auto">
        <div>
          <label className="block text-xs font-medium text-slate-300 mb-1">{t('intl.card_number')}</label>
          <div className="relative">
            <input
              type="text"
              required
              maxLength={19}
              placeholder="4242 •••• •••• 4242"
              value={cardNumber}
              onChange={(e) => setCardNumber(formatCardNumber(e.target.value))}
              className="w-full bg-slate-950/80 border border-slate-700 rounded-xl px-4 py-2.5 text-sm text-slate-100 placeholder-slate-500 font-mono focus:border-cyan-500 focus:outline-none"
            />
            <div className="absolute right-3 top-2.5 flex items-center gap-1 text-[10px] text-slate-400 font-bold">
              <span>VISA</span>
              <span>MC</span>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">{t('intl.expiry')}</label>
            <input
              type="text"
              required
              maxLength={5}
              placeholder="12/28"
              value={expiry}
              onChange={(e) => setExpiry(e.target.value)}
              className="w-full bg-slate-950/80 border border-slate-700 rounded-xl px-4 py-2.5 text-sm text-slate-100 placeholder-slate-500 font-mono focus:border-cyan-500 focus:outline-none"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">{t('intl.cvc')}</label>
            <input
              type="password"
              required
              maxLength={4}
              placeholder="•••"
              value={cvc}
              onChange={(e) => setCvc(e.target.value)}
              className="w-full bg-slate-950/80 border border-slate-700 rounded-xl px-4 py-2.5 text-sm text-slate-100 placeholder-slate-500 font-mono focus:border-cyan-500 focus:outline-none"
            />
          </div>
        </div>

        <div>
          <label className="block text-xs font-medium text-slate-300 mb-1">{t('intl.cardholder')}</label>
          <input
            type="text"
            required
            placeholder="JOHN DOE"
            value={cardHolder}
            onChange={(e) => setCardHolder(e.target.value.toUpperCase())}
            className="w-full bg-slate-950/80 border border-slate-700 rounded-xl px-4 py-2.5 text-sm text-slate-100 placeholder-slate-500 uppercase focus:border-cyan-500 focus:outline-none"
          />
        </div>

        <div className="p-3 rounded-xl bg-slate-800/40 border border-slate-800 flex justify-between items-center text-xs">
          <span className="text-slate-400">Total Charged:</span>
          <span className="font-mono font-bold text-emerald-400 text-base">${amountUsd} USD</span>
        </div>

        <button
          type="submit"
          disabled={loading || success}
          className="w-full py-3 rounded-xl bg-gradient-to-r from-blue-600 to-cyan-500 hover:from-blue-500 hover:to-cyan-400 text-white font-semibold text-sm flex items-center justify-center gap-2 transition shadow-lg shadow-cyan-500/20 disabled:opacity-50"
        >
          {loading ? (
            <RefreshCw className="w-4 h-4 animate-spin" />
          ) : success ? (
            <CheckCircle2 className="w-4 h-4 text-emerald-300" />
          ) : (
            <Lock className="w-4 h-4" />
          )}
          {loading ? 'Processing...' : success ? 'Settled!' : `${t('intl.btn_pay')} ($${amountUsd} USD)`}
        </button>

        <p className="text-center text-[11px] text-slate-500 flex items-center justify-center gap-1">
          <ShieldCheck className="w-3.5 h-3.5 text-emerald-400" />
          {t('intl.secured_by')}
        </p>
      </form>
    </div>
  );
};
