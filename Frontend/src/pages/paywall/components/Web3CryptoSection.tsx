import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Wallet, Loader2, CheckCircle2, ShieldAlert } from 'lucide-react';
import api from '@/services/api';

const TREASURY_WALLET = '0x16b607ED8d8a7c2937DF0EAc2Cd0B47a759600eE';
const MATIC_AMOUNT = '2.0';

const POLYGON_AMOY_PARAMS = {
  chainId: '0x13882',
  chainName: 'Polygon Amoy Testnet',
  nativeCurrency: { name: 'MATIC', symbol: 'MATIC', decimals: 18 },
  rpcUrls: ['https://rpc-amoy.polygon.technology'],
  blockExplorerUrls: ['https://amoy.polygonscan.com/'],
};

interface Web3CryptoSectionProps {
  onPaymentSuccess: () => void;
}

export const Web3CryptoSection: React.FC<Web3CryptoSectionProps> = ({ onPaymentSuccess }) => {
  const { t } = useTranslation('paywall');
  const [walletAddress, setWalletAddress] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [txState, setTxState] = useState<'idle' | 'connecting' | 'network_switching' | 'sending' | 'confirming' | 'completed'>('idle');
  const [txHash, setTxHash] = useState<string | null>(null);

  useEffect(() => {
    const ethereum = (window as any).ethereum;
    if (ethereum) {
      ethereum.request({ method: 'eth_accounts' })
        .then((accounts: any) => {
          if (accounts && accounts.length > 0) {
            setWalletAddress(accounts[0]);
          }
        })
        .catch(() => {});
    }
  }, []);

  const connectWallet = async () => {
    const ethereum = (window as any).ethereum;
    if (!ethereum) {
      setError('Không tìm thấy ví Web3 (MetaMask/OKX/Rabby). Vui lòng cài đặt tiện ích.');
      return;
    }
    setError(null);
    setTxState('connecting');
    try {
      const accounts = await ethereum.request({ method: 'eth_requestAccounts' });
      setWalletAddress(accounts[0]);
      setTxState('idle');
    } catch (err: any) {
      setError(err.message || 'Lỗi kết nối ví.');
      setTxState('idle');
    }
  };

  const switchNetworkToPolygon = async () => {
    const ethereum = (window as any).ethereum;
    if (!ethereum) return false;
    try {
      await ethereum.request({
        method: 'wallet_switchEthereumChain',
        params: [{ chainId: POLYGON_AMOY_PARAMS.chainId }],
      });
      return true;
    } catch (switchError: any) {
      if (switchError.code === 4902) {
        try {
          await ethereum.request({
            method: 'wallet_addEthereumChain',
            params: [POLYGON_AMOY_PARAMS],
          });
          return true;
        } catch (addError: any) {
          setError(addError.message || 'Lỗi thêm mạng Polygon.');
          return false;
        }
      }
      setError(switchError.message || 'Lỗi chuyển đổi mạng.');
      return false;
    }
  };

  const handleWeb3Payment = async () => {
    const ethereum = (window as any).ethereum;
    if (!ethereum || !walletAddress) {
      setError('Vui lòng kết nối ví trước.');
      return;
    }
    setError(null);
    setTxState('network_switching');

    const networkOk = await switchNetworkToPolygon();
    if (!networkOk) {
      setTxState('idle');
      return;
    }

    setTxState('sending');
    try {
      const valueHex = '0x1BC16D674EC80000'; // 2.0 MATIC in wei
      const hash = await ethereum.request({
        method: 'eth_sendTransaction',
        params: [
          {
            from: walletAddress,
            to: TREASURY_WALLET,
            value: valueHex,
          },
        ],
      });

      setTxHash(hash);
      setTxState('confirming');

      // Ghi nhận on-chain vào Backend
      await api.post('/api/paywall/verify-web3', {
        txHash: hash,
        senderAddress: walletAddress,
        amount: MATIC_AMOUNT,
      });

      setTxState('completed');
      onPaymentSuccess();
    } catch (err: any) {
      setError(err.message || 'Giao dịch bị từ chối hoặc thất bại.');
      setTxState('idle');
    }
  };

  return (
    <div className="bg-slate-900/60 p-6 rounded-2xl border border-slate-800 backdrop-blur-md space-y-4">
      <div className="flex items-center justify-between pb-3 border-b border-slate-800">
        <div className="flex items-center gap-2">
          <Wallet className="w-5 h-5 text-purple-400" />
          <h3 className="font-semibold text-slate-100 text-sm md:text-base">{t('crypto.title')}</h3>
        </div>
        <span className="text-xs px-2 py-0.5 rounded-full bg-purple-500/20 text-purple-300 font-mono">Polygon 2.0</span>
      </div>

      <p className="text-xs text-slate-400">{t('crypto.desc')}</p>

      {error && (
        <div className="p-3 rounded-xl bg-rose-500/10 border border-rose-500/20 flex items-center gap-2 text-rose-300 text-xs">
          <ShieldAlert className="w-4 h-4 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <div className="p-4 rounded-xl bg-slate-950/80 border border-slate-800 space-y-3">
        <div className="flex justify-between items-center text-xs">
          <span className="text-slate-400">{t('vietqr.amount')}:</span>
          <span className="font-mono font-bold text-purple-400 text-base">{MATIC_AMOUNT} MATIC</span>
        </div>
        <div className="flex justify-between items-center text-xs">
          <span className="text-slate-400">Treasury Contract:</span>
          <span className="font-mono text-slate-400 text-[11px] truncate max-w-[200px]">{TREASURY_WALLET}</span>
        </div>
        {walletAddress && (
          <div className="flex justify-between items-center text-xs pt-2 border-t border-slate-800">
            <span className="text-slate-400">Connected Wallet:</span>
            <span className="font-mono text-cyan-400 text-[11px]">{walletAddress.slice(0, 6)}...{walletAddress.slice(-4)}</span>
          </div>
        )}
      </div>

      {txState === 'completed' ? (
        <div className="p-4 rounded-xl bg-emerald-500/10 border border-emerald-500/20 flex items-center gap-2 text-emerald-300 text-xs">
          <CheckCircle2 className="w-5 h-5" />
          <span>{t('crypto.tx_completed')}</span>
        </div>
      ) : !walletAddress ? (
        <button
          onClick={connectWallet}
          disabled={txState === 'connecting'}
          className="w-full py-3 rounded-xl bg-purple-600 hover:bg-purple-500 text-white font-semibold text-xs transition shadow-lg shadow-purple-500/20 flex items-center justify-center gap-2"
        >
          {txState === 'connecting' ? <Loader2 className="w-4 h-4 animate-spin" /> : <Wallet className="w-4 h-4" />}
          {t('crypto.connect_wallet')}
        </button>
      ) : (
        <button
          onClick={handleWeb3Payment}
          disabled={txState !== 'idle'}
          className="w-full py-3 rounded-xl bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-500 hover:to-indigo-500 text-white font-semibold text-xs transition shadow-lg shadow-purple-500/20 flex items-center justify-center gap-2"
        >
          {txState !== 'idle' ? <Loader2 className="w-4 h-4 animate-spin" /> : <CheckCircle2 className="w-4 h-4" />}
          {t('crypto.send_payment')}
        </button>
      )}
    </div>
  );
};
