import React, { useState, useEffect, useRef } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import { QrCode, RefreshCw, Smartphone, CheckCircle2, ShieldCheck, Zap, AlertCircle, ArrowRight, Sparkles } from 'lucide-react';
import api from '../../services/api';

interface QrLoginCardProps {
  onSuccess: (token: string, user: any) => void;
}

export const QrLoginCard: React.FC<QrLoginCardProps> = ({ onSuccess }) => {
  const [ticket, setTicket] = useState<string | null>(null);
  const [qrUrl, setQrUrl] = useState<string>('');
  const [expiresIn, setExpiresIn] = useState<number>(120);
  const [status, setStatus] = useState<'loading' | 'pending' | 'scanned' | 'confirmed' | 'expired'>('loading');
  const [error, setError] = useState<string | null>(null);
  const [isDemoLoading, setIsDemoLoading] = useState(false);

  const timerRef = useRef<any>(null);
  const pollRef = useRef<any>(null);

  // Khởi tạo vé QR đăng nhập
  const initQr = async () => {
    setStatus('loading');
    setError(null);
    clearInterval(timerRef.current);
    clearInterval(pollRef.current);

    try {
      const res = await api.post('/api/auth/qr/generate', {
        deviceInfo: navigator.userAgent
      });

      const { ticket: newTicket, qrUrl: newQrUrl, expiresIn: newExpiresIn } = res.data;
      setTicket(newTicket);
      setQrUrl(newQrUrl);
      setExpiresIn(newExpiresIn || 120);
      setStatus('pending');

      // Đếm ngược thời gian
      let timeLeft = newExpiresIn || 120;
      timerRef.current = setInterval(() => {
        timeLeft -= 1;
        setExpiresIn(timeLeft);
        if (timeLeft <= 0) {
          clearInterval(timerRef.current);
          clearInterval(pollRef.current);
          setStatus('expired');
        }
      }, 1000);

      // Polling kiểm tra trạng thái vé mỗi 2 giây
      pollRef.current = setInterval(async () => {
        try {
          const statusRes = await api.get(`/api/auth/qr/status?ticket=${newTicket}`);
          const currentStatus = statusRes.data?.status;

          if (currentStatus === 'scanned') {
            setStatus('scanned');
          } else if (currentStatus === 'confirmed') {
            clearInterval(timerRef.current);
            clearInterval(pollRef.current);
            setStatus('confirmed');

            const { token, user } = statusRes.data;
            if (token && user) {
              setTimeout(() => {
                onSuccess(token, user);
              }, 800);
            }
          } else if (currentStatus === 'expired') {
            clearInterval(timerRef.current);
            clearInterval(pollRef.current);
            setStatus('expired');
          }
        } catch (e) {
          console.warn('Lỗi kiểm tra trạng thái QR', e);
        }
      }, 2000);
    } catch (err: any) {
      console.error('[QR Init Error]', err);
      setError('Không thể khởi tạo mã QR đăng nhập. Vui lòng thử lại.');
      setStatus('expired');
    }
  };

  useEffect(() => {
    initQr();
    return () => {
      clearInterval(timerRef.current);
      clearInterval(pollRef.current);
    };
  }, []);

  // Mô phỏng quét mã thử nghiệm ngay trên máy tính
  const handleDemoScan = async () => {
    if (!ticket) return;
    setIsDemoLoading(true);
    try {
      await api.post('/api/auth/qr/demo-confirm', {
        ticket,
        email: 'admin@dehoc.vn'
      });
      // Poll sẽ tự động bắt được trạng thái confirmed trong 1-2 giây tiếp theo
    } catch (e: any) {
      console.error('[Demo Scan Error]', e);
      setError(e.response?.data?.message || 'Lỗi khi kích hoạt mô phỏng.');
    } finally {
      setIsDemoLoading(false);
    }
  };

  // Định dạng thời gian mm:ss
  const formatTime = (secs: number) => {
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  return (
    <div className="flex flex-col items-center justify-center p-2 text-white animate-fadeIn">
      {/* Box QR Code */}
      <div className="relative p-4 rounded-3xl bg-white/5 border border-slate-700/80 shadow-2xl backdrop-blur-xl flex flex-col items-center">
        {status === 'loading' && (
          <div className="w-52 h-52 flex flex-col items-center justify-center gap-3">
            <RefreshCw className="w-8 h-8 text-indigo-400 animate-spin" />
            <span className="text-xs text-slate-400 font-medium">Đang tạo mã QR bảo mật...</span>
          </div>
        )}

        {status === 'pending' && qrUrl && (
          <div className="relative group">
            <div className="p-3 bg-white rounded-2xl shadow-xl transition-transform transform group-hover:scale-[1.02]">
              <QRCodeSVG value={qrUrl} size={200} level="H" includeMargin={true} />
            </div>

            {/* Glowing Corner Accents */}
            <div className="absolute -top-1 -left-1 w-4 h-4 border-t-2 border-l-2 border-indigo-500 rounded-tl-lg" />
            <div className="absolute -top-1 -right-1 w-4 h-4 border-t-2 border-r-2 border-indigo-500 rounded-tr-lg" />
            <div className="absolute -bottom-1 -left-1 w-4 h-4 border-b-2 border-l-2 border-indigo-500 rounded-bl-lg" />
            <div className="absolute -bottom-1 -right-1 w-4 h-4 border-b-2 border-r-2 border-indigo-500 rounded-br-lg" />
          </div>
        )}

        {status === 'scanned' && (
          <div className="w-52 h-52 flex flex-col items-center justify-center p-4 text-center gap-3 bg-indigo-950/40 rounded-2xl border border-indigo-500/30 animate-pulse">
            <Smartphone className="w-12 h-12 text-indigo-400 animate-bounce" />
            <div>
              <p className="text-xs font-bold text-white">Đã nhận diện thiết bị!</p>
              <p className="text-[11px] text-slate-300 mt-1 leading-snug">Vui lòng bấm "Xác nhận" trên màn hình điện thoại của bạn.</p>
            </div>
          </div>
        )}

        {status === 'confirmed' && (
          <div className="w-52 h-52 flex flex-col items-center justify-center p-4 text-center gap-3 bg-emerald-950/40 rounded-2xl border border-emerald-500/40 animate-scaleUp">
            <CheckCircle2 className="w-14 h-14 text-emerald-400" />
            <div>
              <p className="text-sm font-bold text-emerald-300">Xác thực thành công!</p>
              <p className="text-xs text-slate-300 mt-1">Đang chuyển hướng vào hệ thống...</p>
            </div>
          </div>
        )}

        {status === 'expired' && (
          <div className="w-52 h-52 flex flex-col items-center justify-center p-4 text-center gap-3 bg-slate-900/90 rounded-2xl border border-slate-700">
            <AlertCircle className="w-10 h-10 text-amber-400" />
            <div>
              <p className="text-xs font-bold text-slate-200">Mã QR đã hết hạn</p>
              <p className="text-[10px] text-slate-400 mt-0.5">Vì lý do an toàn, mã chỉ tồn tại 2 phút.</p>
            </div>
            <button
              type="button"
              onClick={initQr}
              className="mt-1 px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white font-bold text-xs shadow-lg shadow-indigo-600/30 flex items-center gap-1.5 transition-transform active:scale-95 cursor-pointer"
            >
              <RefreshCw size={13} /> Làm mới mã QR
            </button>
          </div>
        )}
      </div>

      {/* Timer & Instructions */}
      {status === 'pending' && (
        <div className="mt-4 text-center space-y-2 max-w-xs">
          <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-slate-800/80 border border-slate-700 text-xs font-mono font-bold text-indigo-300">
            <RefreshCw size={12} className="animate-spin text-indigo-400" />
            <span>Mã hết hạn sau: {formatTime(expiresIn)}</span>
          </div>

          <p className="text-xs text-slate-400 leading-relaxed">
            Mở <strong className="text-slate-200">Camera điện thoại</strong> hoặc ứng dụng AegisQuiz/Dehoc quét mã QR phía trên để đăng nhập tức thì.
          </p>
        </div>
      )}

      {/* Demo Simulator Quick Action */}
      <div className="mt-4 pt-3 border-t border-slate-800/80 w-full flex flex-col items-center gap-2">
        <button
          type="button"
          onClick={handleDemoScan}
          disabled={status !== 'pending' || isDemoLoading}
          className="text-[11px] text-indigo-400 hover:text-indigo-300 flex items-center gap-1 font-semibold py-1 px-3 rounded-lg bg-indigo-500/10 hover:bg-indigo-500/20 border border-indigo-500/20 transition-all cursor-pointer disabled:opacity-40"
          title="Bấm để trải nghiệm tính năng Quét QR ngay trên máy tính mà không cần điện thoại"
        >
          {isDemoLoading ? <RefreshCw size={12} className="animate-spin" /> : <Sparkles size={12} className="text-amber-400" />}
          <span>{isDemoLoading ? 'Đang mô phỏng quét...' : 'Thử nghiệm quét QR nhanh (Demo Scan)'}</span>
        </button>
        <span className="text-[10px] text-slate-500 text-center">
          Chuẩn an toàn Zero-Trust RFC 6749 • Tự động hủy sau 1 lần quét
        </span>
      </div>
    </div>
  );
};
