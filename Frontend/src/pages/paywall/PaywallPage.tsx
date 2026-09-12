import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, Wallet, ShieldAlert, Sparkles, Loader2, CheckCircle2 } from 'lucide-react';
import api from '@/services/api';

// Địa chỉ ví nhận tiền của hệ thống (Treasury Wallet Address)
const TREASURY_WALLET = '0x16b607ED8d8a7c2937DF0EAc2Cd0B47a759600eE';
const MATIC_AMOUNT = '2.0';

// Thông tin mạng Polygon Amoy Testnet (Dùng cho Sandbox Testing)
const POLYGON_AMOY_PARAMS = {
  chainId: '0x13882', // 80002 decimal
  chainName: 'Polygon Amoy Testnet',
  nativeCurrency: { name: 'MATIC', symbol: 'MATIC', decimals: 18 },
  rpcUrls: ['https://rpc-amoy.polygon.technology'],
  blockExplorerUrls: ['https://amoy.polygonscan.com/'],
};

export function PaywallPage() {
  const navigate = useNavigate();
  const [walletAddress, setWalletAddress] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [txState, setTxState] = useState<'idle' | 'connecting' | 'network_switching' | 'sending' | 'confirming' | 'completed'>('idle');
  const [txHash, setTxHash] = useState<string | null>(null);
  const [paymentCode, setPaymentCode] = useState<string | null>(null);

  // Đọc thông tin user từ localStorage
  const userRaw = localStorage.getItem('user');
  const user = userRaw ? JSON.parse(userRaw) as { id?: string; name?: string; email?: string; isPremium?: boolean } : null;
  const userId = user?.id ?? 'anonymous';

  useEffect(() => {
    // Kiểm tra xem đã kết nối ví trước đó chưa
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
      setError('Không tìm thấy ví MetaMask. Vui lòng cài đặt extension để thanh toán.');
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
      // Mạng chưa được thêm vào ví -> Thêm mạng mới
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

    try {
      // 1. Chuyển sang mạng Polygon
      const switched = await switchNetworkToPolygon();
      if (!switched) {
        setTxState('idle');
        return;
      }

      setTxState('sending');

      // 2. Chuyển đổi 2 MATIC sang đơn vị Wei (Hexadecimal)
      // 2 MATIC = 2 * 10^18 Wei = 2,000,000,000,000,000,000 Wei = 0x1bc16d674ec80000
      const amountInWeiHex = '0x1bc16d674ec80000'; 

      // 3. Gửi giao dịch thanh toán lên chuỗi
      const hash = await ethereum.request({
        method: 'eth_sendTransaction',
        params: [
          {
            from: walletAddress,
            to: TREASURY_WALLET,
            value: amountInWeiHex,
          },
        ],
      });

      setTxHash(hash);
      setTxState('confirming');

      // 4. Tạo giao dịch tạm trong Database Backend
      // Để API hoạt động, cần parse UserId thành Guid thật (nếu có, hoặc sinh Guid ngẫu nhiên nếu anonymous)
      const formattedUserId = (userId && userId !== 'anonymous' && userId.length === 36) 
        ? userId 
        : '00000000-0000-0000-0000-000000000000';

      const createTxResponse = await api.post('/api/webhook/create-transaction', {
        userId: formattedUserId,
        amount: parseFloat(MATIC_AMOUNT),
        currency: 'MATIC',
      });
      const registeredCode = createTxResponse.data.paymentCode;
      setPaymentCode(registeredCode);

      // [CG2+CG5 FIX] Server-side premium unlock
      // KHÔNG tự sửa isPremium trong localStorage — hải tấc có thể bypass!
      // Thay bằng: gọi API lấy JWT mới chứa claim isPremium=true do server cấp
      try {
        const refreshResponse = await api.post('/api/auth/refresh-premium', {
          userId: formattedUserId,
          paymentCode: registeredCode,
        });
        if (refreshResponse.data?.token) {
          // Cập nhật token mới (chứa isPremium=true trong JWT claim)
          localStorage.setItem('token', refreshResponse.data.token);
          // Cập nhật user profile từ server — không tự set isPremium
          if (refreshResponse.data.user) {
            localStorage.setItem('user', JSON.stringify(refreshResponse.data.user));
          }
        }
      } catch {
        // Nếu API refresh thất bại, vẫn để txState = completed
        // User sẽ thấy premium sau khi reload (JWT được xác thực server-side)
        console.warn('[CG5] Token refresh failed, user needs to re-login for premium features.');
      }

      setTxState('completed');

    } catch (err: any) {
      setError(err.message || 'Thanh toán thất bại.');
      setTxState('idle');
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-white flex flex-col items-center justify-center p-6 relative overflow-hidden font-sans">
      {/* Background Glow */}
      <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-purple-650/20 rounded-full blur-[120px] pointer-events-none" />
      <div className="absolute bottom-1/4 right-1/4 w-96 h-96 bg-cyan-650/20 rounded-full blur-[120px] pointer-events-none" />

      {/* Back Button */}
      <button 
        onClick={() => navigate('/')} 
        className="absolute top-8 left-8 flex items-center gap-2 text-gray-400 hover:text-white transition-colors font-bold text-sm bg-slate-900/40 border border-slate-800 px-4 py-2 rounded-xl backdrop-blur-md"
      >
        <ArrowLeft size={16} /> Quay về Trang chủ
      </button>

      <div className="w-full max-w-lg bg-slate-900/70 border border-slate-800 rounded-3xl p-8 md:p-10 shadow-2xl backdrop-blur-xl relative z-10">
        
        {txState === 'completed' ? (
          <div className="text-center space-y-6 animate-in fade-in zoom-in duration-300">
            <div className="w-20 h-20 bg-emerald-950/50 border border-emerald-500/30 text-emerald-400 rounded-full flex items-center justify-center text-4xl mx-auto">
              <CheckCircle2 size={44} />
            </div>
            <div className="space-y-2">
              <h1 className="text-3xl font-black text-emerald-400">NÂNG CẤP THÀNH CÔNG!</h1>
              <p className="text-gray-300 text-sm">Chào mừng bạn gia nhập thế giới Học viên VIP của AegisQuiz.</p>
            </div>

            <div className="bg-slate-950/60 p-5 rounded-2xl border border-slate-800/80 text-left text-xs font-mono text-gray-400 space-y-2">
              <p className="text-emerald-500 font-bold mb-1">Mã Giao dịch của bạn:</p>
              <p className="text-white select-all">{paymentCode}</p>
              <div className="h-px bg-slate-850 my-2" />
              <p className="text-emerald-500 font-bold mb-1">Tx Hash trên Blockchain:</p>
              <p className="break-all select-all">{txHash}</p>
            </div>

            <button
              onClick={() => navigate('/')}
              className="w-full py-4 bg-gradient-to-r from-emerald-500 to-teal-500 hover:from-emerald-600 hover:to-teal-600 text-white font-black rounded-xl transition-all shadow-lg shadow-emerald-500/20 active:scale-95"
            >
              Trải nghiệm AI Mentor ngay! 🧠
            </button>
          </div>
        ) : (
          <div className="space-y-8">
            <div className="text-center space-y-3">
              <div className="inline-flex items-center gap-1.5 px-3 py-1 bg-purple-950/50 border border-purple-500/30 text-purple-300 rounded-full text-xs font-black tracking-wide uppercase">
                <Sparkles size={12} /> Cảnh giới 5: Web3 Polygon
              </div>
              <h1 className="text-3xl font-black tracking-tight text-transparent bg-clip-text bg-gradient-to-r from-purple-400 to-cyan-400">
                NÂNG CẤP GÓI AEGIS VIP
              </h1>
              <p className="text-sm text-gray-400">Hết lượt miễn phí? Hãy kết nối ví Web3 để mở khóa không giới hạn Trí tuệ Nhân tạo chấm bài và Gia sư ảo Gemini.</p>
            </div>

            {/* Pricing Card */}
            <div className="bg-gradient-to-br from-purple-900/20 to-cyan-900/10 rounded-2xl p-6 border border-purple-500/20 text-center relative overflow-hidden">
              <div className="absolute top-0 right-0 w-24 h-24 bg-purple-500/10 rounded-full blur-2xl" />
              <h3 className="text-lg font-bold text-gray-300">Gói Trọn Đời (Lifetime AI access)</h3>
              <div className="my-4">
                <span className="text-5xl font-black text-transparent bg-clip-text bg-gradient-to-r from-purple-300 to-cyan-300">
                  {MATIC_AMOUNT}
                </span>
                <span className="text-xl font-bold ml-1.5 text-purple-400">MATIC</span>
              </div>
              <ul className="text-left text-xs text-gray-400 space-y-2 mt-4 border-t border-purple-950/40 pt-4">
                <li className="flex items-center gap-2">🔹 Chấm bài tự động không giới hạn bằng Gemini Pro</li>
                <li className="flex items-center gap-2">🔹 Gia sư ảo phân tích điểm yếu & lộ trình 7 ngày</li>
                <li className="flex items-center gap-2">🔹 Phí giao dịch rẻ & xử lý tức thời trên mạng Polygon</li>
              </ul>
            </div>

            {/* Wallet Address Status */}
            {walletAddress ? (
              <div className="bg-slate-950/60 p-4 rounded-xl border border-slate-800 text-sm flex items-center justify-between">
                <div className="space-y-0.5">
                  <p className="text-xs text-gray-500">Ví đang kết nối</p>
                  <p className="font-mono text-purple-300 font-semibold truncate max-w-[200px] md:max-w-[280px]">
                    {walletAddress}
                  </p>
                </div>
                <span className="h-2 w-2 rounded-full bg-emerald-500 animate-pulse" />
              </div>
            ) : (
              <button
                onClick={connectWallet}
                disabled={txState === 'connecting'}
                className="w-full py-3.5 bg-slate-850 hover:bg-slate-800 text-white font-bold rounded-xl border border-slate-700 transition-all flex items-center justify-center gap-2.5 active:scale-95 disabled:opacity-50"
              >
                {txState === 'connecting' ? <Loader2 className="animate-spin text-purple-400" /> : <Wallet size={18} />}
                Kết nối ví MetaMask
              </button>
            )}

            {/* Error Message */}
            {error && (
              <div className="bg-red-950/40 border border-red-500/25 p-4 rounded-xl text-red-400 text-xs flex items-start gap-2.5">
                <ShieldAlert className="shrink-0 mt-0.5" size={16} />
                <p className="leading-relaxed">{error}</p>
              </div>
            )}

            {/* Action Buttons */}
            {walletAddress && (
              <button
                onClick={handleWeb3Payment}
                disabled={txState !== 'idle'}
                className="w-full py-4 bg-gradient-to-r from-purple-600 to-cyan-600 hover:from-purple-700 hover:to-cyan-700 text-white font-black rounded-xl transition-all shadow-lg shadow-purple-500/20 active:scale-95 disabled:opacity-50 flex items-center justify-center gap-2"
              >
                {txState === 'network_switching' && (
                  <>
                    <Loader2 className="animate-spin" size={18} />
                    Đang yêu cầu chuyển mạng Polygon...
                  </>
                )}
                {txState === 'sending' && (
                  <>
                    <Loader2 className="animate-spin" size={18} />
                    Xác nhận giao dịch trên ví của bạn...
                  </>
                )}
                {txState === 'confirming' && (
                  <>
                    <Loader2 className="animate-spin" size={18} />
                    Đang xử lý kích hoạt tài khoản VIP...
                  </>
                )}
                {txState === 'idle' && (
                  <>
                    Mở khóa VIP ngay - {MATIC_AMOUNT} MATIC ⚡
                  </>
                )}
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
export default PaywallPage;
