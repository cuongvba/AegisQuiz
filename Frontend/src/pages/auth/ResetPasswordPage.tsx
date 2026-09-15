import React, { useState, useEffect } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import {
  Key, Shield, CheckCircle2, AlertCircle, Eye, EyeOff,
  Lock, ArrowRight, Sparkles, RefreshCw, Check
} from 'lucide-react';
import api from '@/services/api';

export function ResetPasswordPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const tokenParam = searchParams.get('token') || '';
  const emailParam = searchParams.get('email') || '';

  const [email, setEmail] = useState(emailParam);
  const [token, setToken] = useState(tokenParam);
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    if (tokenParam) setToken(tokenParam);
    if (emailParam) setEmail(emailParam);
  }, [tokenParam, emailParam]);

  // NIST Password strength calculation
  const getPasswordStrength = (pass: string) => {
    let score = 0;
    if (pass.length >= 8) score += 1;
    if (pass.length >= 12) score += 1;
    if (/[A-Z]/.test(pass)) score += 1;
    if (/[0-9]/.test(pass)) score += 1;
    if (/[^A-Za-z0-9]/.test(pass)) score += 1;
    return score;
  };

  const passwordScore = getPasswordStrength(newPassword);

  const getStrengthLabel = (score: number) => {
    if (!newPassword) return { label: 'Chưa nhập', color: 'bg-slate-700', text: 'text-slate-500' };
    if (score <= 2) return { label: 'Yếu', color: 'bg-rose-500', text: 'text-rose-400' };
    if (score <= 3) return { label: 'Trung bình', color: 'bg-amber-500', text: 'text-amber-400' };
    if (score === 4) return { label: 'Tốt', color: 'bg-blue-500', text: 'text-blue-400' };
    return { label: 'Rất mạnh (Chuẩn NIST)', color: 'bg-emerald-500', text: 'text-emerald-400' };
  };

  const strengthInfo = getStrengthLabel(passwordScore);

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setSuccessMessage(null);

    if (!token.trim()) {
      setErrorMessage('Mã token xác thực một lần không được để trống.');
      return;
    }

    if (newPassword.length < 6) {
      setErrorMessage('Mật khẩu mới phải có tối thiểu 6 ký tự.');
      return;
    }

    if (newPassword !== confirmPassword) {
      setErrorMessage('Xác nhận mật khẩu mới không trùng khớp.');
      return;
    }

    setIsSubmitting(true);
    try {
      const res = await api.post('/api/auth/reset-password', {
        email: email.trim(),
        token: token.trim(),
        newPassword
      });

      setSuccessMessage(res.data?.message || 'Đặt lại mật khẩu và kích hoạt tài khoản thành công!');

      // Tự động lưu phiên và chuyển hướng nếu API trả về JWT
      if (res.data?.accessToken || res.data?.token) {
        const token = res.data.accessToken || res.data.token;
        localStorage.setItem('token', token);
        if (res.data.user) {
          localStorage.setItem('user', JSON.stringify(res.data.user));
        }

        setTimeout(() => {
          navigate('/practice');
        }, 2000);
      }
    } catch (err: any) {
      const msg = err.response?.data?.message || 'Không thể đặt lại mật khẩu. Mã xác thực có thể đã hết hạn hoặc không hợp lệ.';
      setErrorMessage(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex items-center justify-center p-4 sm:p-6 lg:p-8 relative overflow-hidden">
      {/* Glow Effects */}
      <div className="absolute top-1/4 left-1/2 -translate-x-1/2 w-[500px] h-[500px] bg-gradient-to-tr from-blue-600/20 via-purple-600/20 to-cyan-500/10 rounded-full blur-3xl pointer-events-none" />

      <div className="relative w-full max-w-md bg-slate-900/80 border border-slate-800 rounded-3xl p-6 sm:p-8 backdrop-blur-2xl shadow-2xl space-y-6">

        {/* Header */}
        <div className="text-center space-y-2">
          <div className="w-12 h-12 rounded-2xl bg-gradient-to-tr from-blue-500 to-purple-600 p-[2px] mx-auto shadow-lg shadow-blue-500/20">
            <div className="w-full h-full bg-slate-900 rounded-[14px] flex items-center justify-center">
              <Key className="w-6 h-6 text-blue-400" />
            </div>
          </div>
          <h1 className="text-2xl font-black text-white tracking-tight">
            Đặt Lại & Kích Hoạt Mật Khẩu
          </h1>
          <p className="text-xs text-slate-400">
            Khởi tạo mật khẩu an toàn theo tiêu chuẩn an ninh mạng NIST SP 800-63B.
          </p>
        </div>

        {errorMessage && (
          <div className="p-3.5 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-xs font-semibold flex items-center gap-2">
            <AlertCircle size={16} className="shrink-0" />
            <span>{errorMessage}</span>
          </div>
        )}

        {successMessage && (
          <div className="p-4 rounded-2xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-300 space-y-2 text-center">
            <div className="flex items-center justify-center gap-2 font-bold text-sm text-emerald-400">
              <CheckCircle2 size={18} />
              {successMessage}
            </div>
            <p className="text-xs text-emerald-400/80">
              Hệ thống đang tự động đăng nhập và đưa bạn vào không gian học tập...
            </p>
          </div>
        )}

        {!successMessage && (
          <form onSubmit={handleResetPassword} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                Email Tài Khoản
              </label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                placeholder="name@dehoc.vn"
                className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-4 py-3 text-white text-xs font-mono focus:outline-none transition-all"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                Mã Xác Thực One-Time Token (OTT)
              </label>
              <input
                type="text"
                value={token}
                onChange={(e) => setToken(e.target.value)}
                required
                placeholder="Dán mã token từ link email"
                className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-4 py-3 text-white text-xs font-mono focus:outline-none transition-all"
              />
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                Mật Khẩu Mới
              </label>
              <div className="relative">
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  required
                  placeholder="••••••••••••"
                  className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-4 py-3 text-white text-xs font-mono focus:outline-none transition-all pr-10"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3.5 top-3.5 text-slate-500 hover:text-slate-300"
                >
                  {showPassword ? <EyeOff size={15} /> : <Eye size={15} />}
                </button>
              </div>

              {/* Password Strength Indicator */}
              {newPassword && (
                <div className="mt-2 space-y-1">
                  <div className="flex items-center justify-between text-[11px]">
                    <span className="text-slate-400">Độ mạnh:</span>
                    <span className={`font-bold ${strengthInfo.text}`}>{strengthInfo.label}</span>
                  </div>
                  <div className="h-1.5 w-full bg-slate-800 rounded-full overflow-hidden flex gap-1">
                    {[1, 2, 3, 4, 5].map((lvl) => (
                      <div
                        key={lvl}
                        className={`h-full flex-1 transition-all ${
                          passwordScore >= lvl ? strengthInfo.color : 'bg-slate-800'
                        }`}
                      />
                    ))}
                  </div>
                </div>
              )}
            </div>

            <div>
              <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                Xác Nhận Mật Khẩu Mới
              </label>
              <input
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                required
                placeholder="••••••••••••"
                className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-4 py-3 text-white text-xs font-mono focus:outline-none transition-all"
              />
            </div>

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full py-3.5 px-4 rounded-xl bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-500 hover:to-purple-500 text-white font-bold text-xs uppercase tracking-wider flex items-center justify-center gap-2 shadow-lg shadow-blue-500/25 transition-all cursor-pointer disabled:opacity-50 mt-2"
            >
              {isSubmitting ? <RefreshCw size={16} className="animate-spin" /> : <CheckCircle2 size={16} />}
              {isSubmitting ? 'Đang kích hoạt...' : 'Kích Hoạt & Đổi Mật Khẩu'}
            </button>
          </form>
        )}

        <div className="text-center pt-2 border-t border-slate-800/80">
          <Link
            to="/login"
            className="text-xs text-blue-400 hover:text-blue-300 font-semibold transition-colors"
          >
            Quay lại trang Đăng nhập
          </Link>
        </div>

      </div>
    </div>
  );
}

export default ResetPasswordPage;
