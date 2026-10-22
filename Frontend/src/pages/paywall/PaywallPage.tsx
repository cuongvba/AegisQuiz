import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Sparkles, CheckCircle2, ShieldCheck, Crown } from 'lucide-react';
import { PaymentMethodTabs, PaymentMethod } from './components/PaymentMethodTabs';
import { VietQRPayOSSection } from './components/VietQRPayOSSection';
import { ChinaWalletSection } from './components/ChinaWalletSection';
import { InternationalCardSection } from './components/InternationalCardSection';
import { Web3CryptoSection } from './components/Web3CryptoSection';
import { BuyMeCoffeeModal } from '@/features/coffee/BuyMeCoffeeModal';

export function PaywallPage() {
  const navigate = useNavigate();
  const { t, i18n } = useTranslation('paywall');
  const [isCoffeeOpen, setIsCoffeeOpen] = useState(false);

  // Đọc user từ localStorage
  const userRaw = localStorage.getItem('user');
  const user = userRaw ? JSON.parse(userRaw) : null;
  const [isVip, setIsVip] = useState<boolean>(!!user?.isPremium);

  // Chọn gói đăng ký
  const [planTier, setPlanTier] = useState<'PRO' | 'ENTERPRISE'>('PRO');
  const amountVnd = planTier === 'PRO' ? 299000 : 999000;

  // Tự động định tuyến Tab theo Locale
  const resolveDefaultTab = (): PaymentMethod => {
    const lang = (i18n.language || 'vi').toLowerCase();
    if (lang.startsWith('zh')) return 'china_wallet';
    if (lang.startsWith('vi')) return 'vietqr';
    return 'intl_card';
  };

  const [activeTab, setActiveTab] = useState<PaymentMethod>(resolveDefaultTab);

  useEffect(() => {
    setActiveTab(resolveDefaultTab());
  }, [i18n.language]);

  const handlePaymentSuccess = () => {
    setIsVip(true);
    if (user) {
      const updatedUser = { ...user, isPremium: true, subscriptionTier: 'VIP' };
      localStorage.setItem('user', JSON.stringify(updatedUser));
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col justify-between selection:bg-cyan-500/30">
      {/* Top Navigation */}
      <header className="border-b border-slate-800/80 bg-slate-900/40 backdrop-blur-md sticky top-0 z-30 px-6 py-4">
        <div className="max-w-5xl mx-auto flex items-center justify-between">
          <button
            onClick={() => navigate('/dashboard')}
            className="flex items-center gap-2 text-xs font-medium text-slate-400 hover:text-cyan-400 transition"
          >
            <ArrowLeft className="w-4 h-4" />
            {t('back_dashboard')}
          </button>
          <div className="flex items-center gap-2">
            <Crown className="w-5 h-5 text-amber-400" />
            <span className="font-bold text-sm tracking-wide bg-gradient-to-r from-amber-200 via-amber-400 to-yellow-500 bg-clip-text text-transparent">
              AEGIS ENTERPRISE PAYMENT HUB
            </span>
          </div>
        </div>
      </header>

      {/* Main Content Area */}
      <main className="flex-1 max-w-5xl mx-auto w-full px-4 py-8 space-y-8">
        {/* Banner Đã kích hoạt VIP */}
        {isVip ? (
          <div className="p-8 rounded-3xl bg-gradient-to-br from-emerald-950/40 via-slate-900/80 to-slate-950 border border-emerald-500/30 text-center space-y-4 shadow-2xl">
            <div className="w-16 h-16 rounded-2xl bg-emerald-500/20 text-emerald-400 flex items-center justify-center mx-auto border border-emerald-500/30">
              <CheckCircle2 className="w-8 h-8" />
            </div>
            <h2 className="text-2xl font-bold text-white">{t('vip_activated_title')}</h2>
            <p className="text-slate-300 text-sm max-w-xl mx-auto">{t('vip_activated_desc')}</p>
            <button
              onClick={() => navigate('/dashboard')}
              className="px-6 py-3 rounded-xl bg-emerald-500 hover:bg-emerald-400 text-slate-950 font-bold text-sm transition shadow-lg shadow-emerald-500/20"
            >
              {t('back_dashboard')}
            </button>
          </div>
        ) : (
          <>
            {/* Header Title */}
            <div className="text-center space-y-2">
              <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-cyan-500/10 border border-cyan-500/20 text-cyan-400 text-xs font-semibold">
                <Sparkles className="w-3.5 h-3.5" />
                <span>Next-Gen Omnichannel Payment Engine</span>
              </div>
              <h1 className="text-2xl md:text-3xl font-extrabold text-white tracking-tight">{t('title')}</h1>
              <p className="text-xs md:text-sm text-slate-400 max-w-2xl mx-auto">{t('subtitle')}</p>
            </div>

            {/* Chọn gói dịch vụ */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* Gói Pro */}
              <div
                onClick={() => setPlanTier('PRO')}
                className={`cursor-pointer p-5 rounded-2xl border transition-all ${
                  planTier === 'PRO'
                    ? 'bg-gradient-to-br from-cyan-950/40 to-slate-900 border-cyan-500 ring-1 ring-cyan-500/40 shadow-xl'
                    : 'bg-slate-900/40 border-slate-800 hover:border-slate-700'
                }`}
              >
                <div className="flex justify-between items-start mb-2">
                  <h3 className="font-bold text-slate-100">{t('plan_pro')}</h3>
                  <span className="text-xs font-bold font-mono text-cyan-400 px-2 py-0.5 rounded bg-cyan-500/10">
                    299,000 VND / tháng
                  </span>
                </div>
                <p className="text-xs text-slate-400">{t('plan_pro_desc')}</p>
              </div>

              {/* Gói Enterprise */}
              <div
                onClick={() => setPlanTier('ENTERPRISE')}
                className={`cursor-pointer p-5 rounded-2xl border transition-all ${
                  planTier === 'ENTERPRISE'
                    ? 'bg-gradient-to-br from-amber-950/40 to-slate-900 border-amber-500 ring-1 ring-amber-500/40 shadow-xl'
                    : 'bg-slate-900/40 border-slate-800 hover:border-slate-700'
                }`}
              >
                <div className="flex justify-between items-start mb-2">
                  <div className="flex items-center gap-1.5">
                    <Crown className="w-4 h-4 text-amber-400" />
                    <h3 className="font-bold text-slate-100">{t('plan_enterprise')}</h3>
                  </div>
                  <span className="text-xs font-bold font-mono text-amber-400 px-2 py-0.5 rounded bg-amber-500/10">
                    999,000 VND / năm
                  </span>
                </div>
                <p className="text-xs text-slate-400">{t('plan_enterprise_desc')}</p>
              </div>
            </div>

            {/* Điều hướng kênh thanh toán */}
            <PaymentMethodTabs activeTab={activeTab} onSelectTab={setActiveTab} />

            {/* Nội dung kênh thanh toán tương ứng */}
            {activeTab === 'vietqr' && (
              <VietQRPayOSSection
                planTier={planTier}
                amount={amountVnd}
                onPaymentSuccess={handlePaymentSuccess}
              />
            )}

            {activeTab === 'china_wallet' && (
              <ChinaWalletSection
                planTier={planTier}
                amountVnd={amountVnd}
                onPaymentSuccess={handlePaymentSuccess}
              />
            )}

            {activeTab === 'intl_card' && (
              <InternationalCardSection
                planTier={planTier}
                amountVnd={amountVnd}
                onPaymentSuccess={handlePaymentSuccess}
              />
            )}

            {activeTab === 'crypto' && (
              <Web3CryptoSection onPaymentSuccess={handlePaymentSuccess} />
            )}

            {/* Đặc quyền gói VIP */}
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3 pt-4 border-t border-slate-900">
              <div className="flex items-center gap-2 text-xs text-slate-300">
                <ShieldCheck className="w-4 h-4 text-cyan-400 shrink-0" />
                <span>{t('features.f1')}</span>
              </div>
              <div className="flex items-center gap-2 text-xs text-slate-300">
                <ShieldCheck className="w-4 h-4 text-cyan-400 shrink-0" />
                <span>{t('features.f2')}</span>
              </div>
              <div className="flex items-center gap-2 text-xs text-slate-300">
                <ShieldCheck className="w-4 h-4 text-cyan-400 shrink-0" />
                <span>{t('features.f3')}</span>
              </div>
            </div>

            {/* Banner Mời Cafe / Tài trợ cộng đồng */}
            <div className="p-4 rounded-2xl bg-gradient-to-r from-amber-500/10 via-orange-500/10 to-transparent border border-amber-500/20 flex flex-col sm:flex-row items-center justify-between gap-3">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-amber-500/20 text-amber-400 flex items-center justify-center shrink-0 border border-amber-500/30">
                  <span className="text-xl">☕</span>
                </div>
                <div>
                  <h4 className="text-xs md:text-sm font-bold text-amber-300">Phục Vụ Cộng Đồng & Tài Trợ Tùy Tâm</h4>
                  <p className="text-[11px] text-slate-400">AegisQuiz luôn mở cửa miễn phí cho giáo dục — Bạn có thể mời team 1 ly cafe để tiếp sức!</p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setIsCoffeeOpen(true)}
                className="px-4 py-2 rounded-xl bg-amber-500 hover:bg-amber-400 text-slate-950 font-bold text-xs whitespace-nowrap shadow-md shadow-amber-500/20 transition flex items-center gap-1.5"
              >
                <span>☕ Mời Cafe Ngay</span>
              </button>
            </div>
          </>
        )}
      </main>

      {/* Modal Mời Cafe */}
      <BuyMeCoffeeModal isOpen={isCoffeeOpen} onClose={() => setIsCoffeeOpen(false)} />

      {/* Footer */}
      <footer className="text-center py-6 text-xs text-slate-500 border-t border-slate-900">
        AegisQuiz Global Payment Sovereign Architecture &copy; 2026. All Rights Reserved.
      </footer>
    </div>
  );
}
export default PaywallPage;
