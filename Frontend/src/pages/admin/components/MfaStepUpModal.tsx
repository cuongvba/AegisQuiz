import React, { useState } from 'react';
import { ShieldAlert, KeyRound, Smartphone, Lock, X, Check, RefreshCw } from 'lucide-react';

interface MfaStepUpModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (authData: { sudoPassword?: string; mfaVerificationCode?: string }) => void;
  isLoading?: boolean;
}

export const MfaStepUpModal: React.FC<MfaStepUpModalProps> = ({
  isOpen,
  onClose,
  onConfirm,
  isLoading = false,
}) => {
  const [method, setMethod] = useState<'password' | 'otp'>('password');
  const [password, setPassword] = useState('');
  const [otpCode, setOtpCode] = useState('');
  const [error, setError] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (method === 'password') {
      if (!password || password.trim().length === 0) {
        setError('Vui lòng nhập mật khẩu Quản trị viên Tối cao (SysAdmin).');
        return;
      }
      onConfirm({ sudoPassword: password.trim() });
    } else {
      const cleanOtp = otpCode.trim();
      if (cleanOtp.length !== 6 || !/^\d{6}$/.test(cleanOtp)) {
        setError('Mã xác thực bảo mật OTP phải là 6 chữ số nguyên.');
        return;
      }
      onConfirm({ mfaVerificationCode: cleanOtp });
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-md animate-fade-in">
      <div className="bg-slate-900 border border-slate-800 rounded-3xl w-full max-w-md overflow-hidden shadow-2xl shadow-cyan-500/10">
        {/* Header */}
        <div className="p-5 border-b border-slate-800 flex items-center justify-between bg-gradient-to-r from-rose-950/30 via-slate-900 to-cyan-950/30">
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 rounded-xl bg-rose-500/10 border border-rose-500/30 flex items-center justify-center text-rose-400">
              <ShieldAlert className="w-5 h-5" />
            </div>
            <div>
              <h3 className="font-bold text-slate-100 text-sm">Xác Thực Sudo / MFA Cấp Độ 3</h3>
              <p className="text-[11px] text-slate-400">Yêu cầu bảo vệ thay đổi cấu hình tài chính</p>
            </div>
          </div>
          <button
            onClick={onClose}
            disabled={isLoading}
            className="p-1.5 rounded-lg text-slate-400 hover:text-slate-200 hover:bg-slate-800 transition"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Content */}
        <form onSubmit={handleSubmit} className="p-5 space-y-4 text-xs">
          <div className="bg-slate-950/60 p-3 rounded-2xl border border-slate-800/80 space-y-1">
            <p className="text-slate-300 font-medium">
              Bạn đang thực hiện thay đổi <strong className="text-cyan-400">Tài khoản thụ hưởng thanh toán</strong> toàn hệ thống.
            </p>
            <p className="text-[11px] text-slate-400">
              Để ngăn chặn hành vi chiếm đoạt tài chính, chỉ Quản trị viên Tối cao (SysAdmin) với thẩm quyền xác thực mới được phép lưu.
            </p>
          </div>

          {/* Method selector */}
          <div className="flex rounded-xl bg-slate-950 p-1 border border-slate-800">
            <button
              type="button"
              onClick={() => {
                setMethod('password');
                setError(null);
              }}
              className={`flex-1 py-1.5 rounded-lg font-semibold flex items-center justify-center gap-1.5 transition ${
                method === 'password'
                  ? 'bg-slate-800 text-cyan-300 shadow'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <KeyRound className="w-3.5 h-3.5" />
              <span>Mật khẩu SysAdmin</span>
            </button>
            <button
              type="button"
              onClick={() => {
                setMethod('otp');
                setError(null);
              }}
              className={`flex-1 py-1.5 rounded-lg font-semibold flex items-center justify-center gap-1.5 transition ${
                method === 'otp'
                  ? 'bg-slate-800 text-cyan-300 shadow'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <Smartphone className="w-3.5 h-3.5" />
              <span>Mã OTP 6 số</span>
            </button>
          </div>

          {method === 'password' ? (
            <div className="space-y-1.5">
              <label className="block text-slate-300 font-semibold">Mật khẩu SysAdmin hiện tại</label>
              <div className="relative">
                <input
                  type="password"
                  required
                  autoFocus
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Nhập mật khẩu tài khoản SysAdmin..."
                  className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3.5 py-2.5 text-slate-100 placeholder:text-slate-600 focus:border-cyan-500 focus:outline-none"
                />
                <Lock className="w-4 h-4 text-slate-500 absolute right-3.5 top-3" />
              </div>
            </div>
          ) : (
            <div className="space-y-1.5">
              <label className="block text-slate-300 font-semibold">Mã bảo mật OTP (6 chữ số)</label>
              <input
                type="text"
                required
                maxLength={6}
                autoFocus
                value={otpCode}
                onChange={(e) => setOtpCode(e.target.value.replace(/\D/g, ''))}
                placeholder="000000"
                className="w-full text-center tracking-[0.5em] font-mono text-base font-bold bg-slate-950 border border-slate-700 rounded-xl px-3.5 py-2.5 text-cyan-400 placeholder:text-slate-600 focus:border-cyan-500 focus:outline-none"
              />
            </div>
          )}

          {error && (
            <div className="p-2.5 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-300 text-[11px] font-medium">
              {error}
            </div>
          )}

          {/* Action buttons */}
          <div className="flex justify-end gap-2.5 pt-2">
            <button
              type="button"
              disabled={isLoading}
              onClick={onClose}
              className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 font-semibold transition"
            >
              Hủy bỏ
            </button>
            <button
              type="submit"
              disabled={isLoading}
              className="px-5 py-2 rounded-xl bg-gradient-to-r from-rose-600 via-rose-500 to-cyan-500 hover:opacity-90 text-white font-bold flex items-center gap-1.5 shadow-lg shadow-rose-500/20 transition disabled:opacity-50"
            >
              {isLoading ? (
                <RefreshCw className="w-3.5 h-3.5 animate-spin" />
              ) : (
                <Check className="w-3.5 h-3.5" />
              )}
              <span>Phê Chuẩn & Lưu</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
