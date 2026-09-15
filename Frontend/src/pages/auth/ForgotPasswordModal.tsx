import React, { useState } from 'react';
import { Mail, CheckCircle2, AlertCircle, X, RefreshCw, Copy, Check, ArrowRight } from 'lucide-react';
import api from '@/services/api';

interface ForgotPasswordModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function ForgotPasswordModal({ isOpen, onClose }: ForgotPasswordModalProps) {
  if (!isOpen) return null;

  const [email, setEmail] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [resetLink, setResetLink] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setSuccessMessage(null);
    setResetLink(null);

    setIsSubmitting(true);
    try {
      const res = await api.post('/api/auth/forgot-password', {
        email: email.trim()
      });

      setSuccessMessage(res.data?.message || 'Hướng dẫn đặt lại mật khẩu đã được gửi đến email của bạn.');
      if (res.data?.resetLink) {
        setResetLink(res.data.resetLink);
      }
    } catch (err: any) {
      const msg = err.response?.data?.message || 'Có lỗi xảy ra khi gửi yêu cầu.';
      setErrorMessage(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCopyLink = () => {
    if (resetLink) {
      navigator.clipboard.writeText(resetLink);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md">
      <div className="relative w-full max-w-md bg-slate-900 border border-slate-800 rounded-3xl p-6 sm:p-8 shadow-2xl space-y-6 text-slate-100">

        <div className="flex items-start justify-between pb-4 border-b border-slate-800">
          <div>
            <div className="flex items-center gap-2">
              <Mail size={20} className="text-blue-400" />
              <h3 className="text-lg font-black text-white">Quên Mật Khẩu?</h3>
            </div>
            <p className="text-xs text-slate-400 mt-1">
              Nhập email đăng ký để nhận link đặt lại mật khẩu bảo mật (One-Time Token).
            </p>
          </div>
          <button
            onClick={onClose}
            className="p-2 text-slate-400 hover:text-white rounded-xl cursor-pointer"
          >
            <X size={18} />
          </button>
        </div>

        {errorMessage && (
          <div className="p-3.5 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-xs font-semibold flex items-center gap-2">
            <AlertCircle size={16} className="shrink-0" />
            <span>{errorMessage}</span>
          </div>
        )}

        {successMessage ? (
          <div className="space-y-4">
            <div className="p-4 rounded-2xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-300 space-y-2">
              <div className="flex items-center gap-2 font-bold text-sm text-emerald-400">
                <CheckCircle2 size={18} />
                Yêu cầu đã được tiếp nhận!
              </div>
              <p className="text-xs text-emerald-400/80">{successMessage}</p>
            </div>

            {resetLink && (
              <div className="p-4 rounded-2xl bg-slate-950 border border-blue-500/30 space-y-2">
                <span className="text-[11px] font-bold text-blue-400 block">Đường link kích hoạt / đổi mật khẩu:</span>
                <div className="p-2.5 bg-slate-900 rounded-xl border border-slate-800 text-[11px] font-mono text-slate-300 break-all select-all">
                  {resetLink}
                </div>
                <div className="flex items-center justify-between gap-2 pt-1">
                  <button
                    type="button"
                    onClick={handleCopyLink}
                    className="px-3 py-1.5 rounded-lg bg-blue-600/20 hover:bg-blue-600 text-blue-400 hover:text-white text-xs font-bold flex items-center gap-1.5 transition-all cursor-pointer"
                  >
                    {copied ? <Check size={14} /> : <Copy size={14} />}
                    {copied ? 'Đã sao chép!' : 'Sao chép link'}
                  </button>

                  <a
                    href={resetLink}
                    className="px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold flex items-center gap-1.5 shadow-md shadow-blue-500/20 transition-all"
                  >
                    <span>Mở link ngay</span>
                    <ArrowRight size={14} />
                  </a>
                </div>
              </div>
            )}

            <button
              onClick={onClose}
              className="w-full py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-white text-xs font-bold cursor-pointer"
            >
              Đóng
            </button>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                Email Đăng Nhập Của Bạn
              </label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                placeholder="name@dehoc.vn hoặc email cá nhân"
                className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-4 py-3 text-white text-xs focus:outline-none transition-all font-mono"
              />
            </div>

            <div className="flex justify-end gap-3 pt-3 border-t border-slate-800">
              <button
                type="button"
                onClick={onClose}
                className="px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 font-bold text-xs cursor-pointer"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={isSubmitting}
                className="px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs flex items-center gap-2 shadow-lg shadow-blue-500/25 transition-all cursor-pointer disabled:opacity-50"
              >
                {isSubmitting ? <RefreshCw size={14} className="animate-spin" /> : <Mail size={14} />}
                {isSubmitting ? 'Đang gửi...' : 'Gửi Link Đặt Lại'}
              </button>
            </div>
          </form>
        )}

      </div>
    </div>
  );
}
