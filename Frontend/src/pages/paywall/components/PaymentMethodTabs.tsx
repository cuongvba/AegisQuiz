import React from 'react';
import { useTranslation } from 'react-i18next';
import { QrCode, CreditCard, Wallet, Coins } from 'lucide-react';

export type PaymentMethod = 'vietqr' | 'intl_card' | 'china_wallet' | 'crypto';

interface PaymentMethodTabsProps {
  activeTab: PaymentMethod;
  onSelectTab: (tab: PaymentMethod) => void;
}

export const PaymentMethodTabs: React.FC<PaymentMethodTabsProps> = ({ activeTab, onSelectTab }) => {
  const { t } = useTranslation('paywall');

  const tabs: { id: PaymentMethod; label: string; icon: React.ReactNode; badge?: string }[] = [
    {
      id: 'vietqr',
      label: t('tabs.vietqr', 'VietQR PayOS (Việt Nam)'),
      icon: <QrCode className="w-5 h-5" />,
      badge: 'Napas 24/7',
    },
    {
      id: 'china_wallet',
      label: t('tabs.china_wallet', 'Alipay / WeChat (中国支付)'),
      icon: <span className="font-bold text-xs px-1 py-0.5 rounded bg-emerald-500/20 text-emerald-400">支/微</span>,
      badge: 'CNY / RMB',
    },
    {
      id: 'intl_card',
      label: t('tabs.intl_card', 'Thẻ Quốc Tế (Global Card)'),
      icon: <CreditCard className="w-5 h-5" />,
      badge: 'Visa / MC',
    },
    {
      id: 'crypto',
      label: t('tabs.crypto', 'Web3 Crypto (Polygon)'),
      icon: <Coins className="w-5 h-5" />,
      badge: 'MATIC',
    },
  ];

  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-2 p-1.5 bg-slate-900/80 backdrop-blur-md rounded-2xl border border-slate-800">
      {tabs.map((tab) => {
        const isActive = activeTab === tab.id;
        return (
          <button
            key={tab.id}
            onClick={() => onSelectTab(tab.id)}
            className={`flex flex-col md:flex-row items-center justify-center gap-2 px-3 py-3 rounded-xl font-medium text-xs md:text-sm transition-all duration-200 relative ${
              isActive
                ? 'bg-gradient-to-r from-cyan-500 to-blue-600 text-white shadow-lg shadow-cyan-500/20 font-semibold'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
            }`}
          >
            <div className="flex items-center gap-1.5">
              {tab.icon}
              <span className="truncate">{tab.label}</span>
            </div>
            {tab.badge && (
              <span
                className={`text-[10px] px-1.5 py-0.5 rounded-full uppercase tracking-wider font-semibold ${
                  isActive
                    ? 'bg-white/20 text-white'
                    : 'bg-slate-800 text-slate-400 border border-slate-700'
                }`}
              >
                {tab.badge}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
};
