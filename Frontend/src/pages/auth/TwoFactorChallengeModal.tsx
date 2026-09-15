import React, { useState, useRef, useEffect } from 'react';
import { ShieldCheck, KeyRound, ArrowLeft, Loader2, AlertCircle, Smartphone } from 'lucide-react';
import api from '../../services/api';

interface TwoFactorChallengeModalProps {
  isOpen: boolean;
  onClose: () => void;
  mfaTempToken: string;
  userEmail: string;
  onSuccess: (token: string, user: any) => void;
}

export const TwoFactorChallengeModal: React.FC<TwoFactorChallengeModalProps> = ({
  isOpen,
  onClose,
  mfaTempToken,
  userEmail,
  onSuccess,
}) => {
  const [digits, setDigits] = useState<string[]>(['', '', '', '', '', '']);
  const [useBackupCode, setUseBackupCode] = useState(false);
  const [backupCode, setBackupCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

  useEffect(() => {
    if (isOpen) {
      setDigits(['', '', '', '', '', '']);
      setBackupCode('');
      setError(null);
      setUseBackupCode(false);
      setTimeout(() => {
        inputRefs.current[0]?.focus();
      }, 100);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const handleDigitChange = (index: number, value: string) => {
    // Chỉ nhận ký tự số
    const cleanVal = value.replace(/\D/g, '');
    if (!cleanVal) {
      const newDigits = [...digits];
      newDigits[index] = '';
      setDigits(newDigits);
      return;
    }

    const char = cleanVal.slice(-1);
    const newDigits = [...digits];
    newDigits[index] = char;
    setDigits(newDigits);

    // Tự động nhảy sang ô tiếp theo
    if (index < 5 && char) {
      inputRefs.current[index + 1]?.focus();
    }

    // Nếu đã nhập đủ 6 số, tự động submit
    if (index === 5 && char && newDigits.every(d => d !== '')) {
      submitVerification(newDigits.join(''), false);
    }
  };

  const handleKeyDown = (index: number, e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Backspace' && !digits[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
  };

  const handlePaste = (e: React.ClipboardEvent) => {
    e.preventDefault();
    const pasted = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, 6);
    if (!pasted) return;

    const newDigits = [...digits];
    for (let i = 0; i < 6; i++) {
      newDigits[i] = pasted[i] || '';
    }
    setDigits(newDigits);

    if (pasted.length === 6) {
      inputRefs.current[5]?.focus();
      submitVerification(pasted, false);
    } else {
      inputRefs.current[Math.min(pasted.length, 5)]?.focus();
    }
  };

  const submitVerification = async (codeToSubmit: string, isBackup: boolean) => {
    setError(null);
    setLoading(true);

    try {
      const res = await api.post('/api/auth/2fa/verify', {
        mfaTempToken,
        totpCode: isBackup ? '' : codeToSubmit,
        recoveryCode: isBackup ? codeToSubmit : ''
      });

      const { token, user } = res.data;
      if (!token || !user) {
        throw new Error('Dữ liệu xác thực trả về không hợp lệ.');
      }

      onSuccess(token, user);
    } catch (err: any) {
      console.error('[2FA Verify Error]', err);
      const msg = err.response?.data?.message || err.message || 'Mã xác thực không chính xác hoặc đã hết hạn.';
      setError(msg);
      // Reset ô nhập để người dùng gõ lại
      if (!isBackup) {
        setDigits(['', '', '', '', '', '']);
        inputRefs.current[0]?.focus();
      }
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (useBackupCode) {
      if (!backupCode.trim()) {
        setError('Vui lòng nhập mã khôi phục khẩn cấp.');
        return;
      }
      submitVerification(backupCode.trim(), true);
    } else {
      const code = digits.join('');
      if (code.length !== 6) {
        setError('Vui lòng nhập đủ 6 chữ số từ ứng dụng xác thực.');
        return;
      }
      submitVerification(code, false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md animate-fadeIn">
      <div className="relative w-full max-w-md bg-slate-900 border border-slate-700/80 rounded-2xl shadow-2xl p-6 sm:p-8 text-white">
        {/* Glow Header */}
        <div className="absolute -top-10 left-1/2 -translate-x-1/2 w-32 h-32 bg-indigo-500/20 rounded-full blur-3xl pointer-events-none" />

        {/* Icon & Title */}
        <div className="flex flex-col items-center text-center mb-6">
          <div className="w-14 h-14 rounded-2xl bg-indigo-600/20 border border-indigo-500/30 flex items-center justify-center text-indigo-400 mb-3 shadow-inner">
            {useBackupCode ? <KeyRound className="w-7 h-7" /> : <Smartphone className="w-7 h-7" />}
          </div>
          <h3 className="text-xl font-bold tracking-tight text-white">
            {useBackupCode ? 'Nhập mã khôi phục dự phòng' : 'Xác thực 2 bước (2FA)'}
          </h3>
          <p className="text-xs text-slate-400 mt-1 max-w-xs">
            {useBackupCode
              ? 'Sử dụng một trong 8 mã dự phòng khẩn cấp được cấp khi bật 2FA.'
              : `Nhập mã 6 chữ số từ ứng dụng Google Authenticator liên kết với tài khoản ${userEmail}.`}
          </p>
        </div>

        {/* Error Alert */}
        {error && (
          <div className="mb-5 p-3 rounded-xl bg-rose-500/10 border border-rose-500/20 flex items-center gap-2.5 text-rose-400 text-xs animate-shake">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span className="leading-snug">{error}</span>
          </div>
        )}

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="space-y-6">
          {!useBackupCode ? (
            <div>
              <div className="flex justify-between gap-2 sm:gap-2.5" onPaste={handlePaste}>
                {digits.map((digit, idx) => (
                  <input
                    key={idx}
                    ref={el => { inputRefs.current[idx] = el; }}
                    type="text"
                    inputMode="numeric"
                    maxLength={1}
                    value={digit}
                    onChange={e => handleDigitChange(idx, e.target.value)}
                    onKeyDown={e => handleKeyDown(idx, e)}
                    disabled={loading}
                    className="w-12 h-14 sm:w-13 sm:h-15 text-center text-2xl font-bold rounded-xl bg-slate-950/80 border border-slate-700/80 text-white focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/30 transition-all selection:bg-indigo-500"
                  />
                ))}
              </div>
              <p className="text-[11px] text-slate-500 text-center mt-3">
                Mã TOTP tự động làm mới sau mỗi 30 giây trong ứng dụng.
              </p>
            </div>
          ) : (
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1.5">
                Mã khôi phục (Dạng: XXXX-XXXX)
              </label>
              <input
                type="text"
                value={backupCode}
                onChange={e => setBackupCode(e.target.value.toUpperCase())}
                placeholder="VD: A1B2-C3D4"
                disabled={loading}
                autoFocus
                className="w-full px-4 py-3 rounded-xl bg-slate-950/80 border border-slate-700/80 text-white font-mono tracking-widest text-center text-lg focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/30"
              />
              <p className="text-[11px] text-slate-500 mt-2 text-center">
                Mỗi mã dự phòng chỉ có thể sử dụng đúng một lần duy nhất.
              </p>
            </div>
          )}

          {/* Action Buttons */}
          <div className="space-y-3">
            <button
              type="submit"
              disabled={loading || (!useBackupCode && digits.some(d => d === '')) || (useBackupCode && !backupCode.trim())}
              className="w-full py-3 px-4 rounded-xl bg-indigo-600 hover:bg-indigo-500 disabled:bg-indigo-600/40 disabled:cursor-not-allowed font-semibold text-sm shadow-lg shadow-indigo-600/25 flex items-center justify-center gap-2 transition-all active:scale-[0.98]"
            >
              {loading ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  <span>Đang xác thực bảo mật...</span>
                </>
              ) : (
                <>
                  <ShieldCheck className="w-4 h-4" />
                  <span>Xác nhận & Đăng nhập</span>
                </>
              )}
            </button>

            {/* Toggle Mode */}
            <div className="flex items-center justify-between text-xs pt-1">
              <button
                type="button"
                onClick={() => {
                  setUseBackupCode(!useBackupCode);
                  setError(null);
                }}
                className="text-indigo-400 hover:text-indigo-300 font-medium transition-colors"
              >
                {useBackupCode ? 'Quay lại nhập mã 6 số (TOTP)' : 'Mất điện thoại? Dùng mã dự phòng'}
              </button>

              <button
                type="button"
                onClick={onClose}
                className="text-slate-400 hover:text-white transition-colors flex items-center gap-1"
              >
                <ArrowLeft className="w-3.5 h-3.5" />
                <span>Hủy bỏ</span>
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
};
