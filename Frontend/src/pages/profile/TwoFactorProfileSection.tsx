import React, { useState } from 'react';
import { 
  ShieldCheck, ShieldAlert, Smartphone, KeyRound, Copy, Check, 
  Download, RefreshCw, AlertCircle, CheckCircle2, Lock, X, Eye, EyeOff
} from 'lucide-react';
import { QRCodeSVG } from 'qrcode.react';
import api from '../../services/api';

interface TwoFactorProfileSectionProps {
  user: any;
  onProfileUpdated?: () => void;
}

export const TwoFactorProfileSection: React.FC<TwoFactorProfileSectionProps> = ({ 
  user, 
  onProfileUpdated 
}) => {
  const isEnabled = Boolean(user?.isTwoFactorEnabled);

  // Setup Modal State
  const [showSetupModal, setShowSetupModal] = useState(false);
  const [setupLoading, setSetupLoading] = useState(false);
  const [setupData, setSetupData] = useState<{
    manualKey: string;
    qrCodeUri: string;
    recoveryCodes: string[];
  } | null>(null);
  const [verifyCode, setVerifyCode] = useState('');
  const [verifyLoading, setVerifyLoading] = useState(false);
  const [setupError, setSetupError] = useState<string | null>(null);
  const [copiedKey, setCopiedKey] = useState(false);
  const [copiedCodes, setCopiedCodes] = useState(false);

  // Disable Modal State
  const [showDisableModal, setShowDisableModal] = useState(false);
  const [disablePassword, setDisablePassword] = useState('');
  const [disableTotp, setDisableTotp] = useState('');
  const [showDisablePassword, setShowDisablePassword] = useState(false);
  const [disableLoading, setDisableLoading] = useState(false);
  const [disableError, setDisableError] = useState<string | null>(null);

  // Regenerate Codes State
  const [showRegenModal, setShowRegenModal] = useState(false);
  const [regenTotp, setRegenTotp] = useState('');
  const [regenCodes, setRegenCodes] = useState<string[] | null>(null);
  const [regenLoading, setRegenLoading] = useState(false);
  const [regenError, setRegenError] = useState<string | null>(null);

  // Toast / Status Message
  const [statusMessage, setStatusMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const showToast = (type: 'success' | 'error', text: string) => {
    setStatusMessage({ type, text });
    setTimeout(() => setStatusMessage(null), 5000);
  };

  // 1. Khởi động quy trình Setup 2FA
  const handleStartSetup = async () => {
    setSetupLoading(true);
    setSetupError(null);
    setVerifyCode('');
    try {
      const res = await api.post('/api/auth/2fa/setup');
      setSetupData(res.data);
      setShowSetupModal(true);
    } catch (err: any) {
      console.error('[2FA Setup Error]', err);
      showToast('error', err.response?.data?.message || 'Không thể khởi tạo mã thiết lập 2FA.');
    } finally {
      setSetupLoading(false);
    }
  };

  // 2. Xác nhận kích hoạt 2FA
  const handleConfirmEnable = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!verifyCode || verifyCode.trim().length !== 6) {
      setSetupError('Vui lòng nhập đủ 6 chữ số từ ứng dụng Google Authenticator.');
      return;
    }

    setVerifyLoading(true);
    setSetupError(null);
    try {
      const res = await api.post('/api/auth/2fa/enable', {
        totpCode: verifyCode.trim()
      });

      setShowSetupModal(false);
      showToast('success', res.data.message || 'Đã kích hoạt bảo mật 2 bước Google Authenticator thành công!');
      if (onProfileUpdated) onProfileUpdated();
    } catch (err: any) {
      console.error('[2FA Enable Error]', err);
      setSetupError(err.response?.data?.message || 'Mã xác thực không đúng hoặc đã hết hạn.');
    } finally {
      setVerifyLoading(false);
    }
  };

  // 3. Tắt 2FA
  const handleConfirmDisable = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!disablePassword && !disableTotp) {
      setDisableError('Vui lòng nhập mật khẩu tài khoản hoặc mã TOTP để xác thực.');
      return;
    }

    setDisableLoading(true);
    setDisableError(null);
    try {
      const res = await api.post('/api/auth/2fa/disable', {
        password: disablePassword,
        totpCode: disableTotp.trim()
      });

      setShowDisableModal(false);
      setDisablePassword('');
      setDisableTotp('');
      showToast('success', res.data.message || 'Đã hủy kích hoạt xác thực 2 bước 2FA thành công.');
      if (onProfileUpdated) onProfileUpdated();
    } catch (err: any) {
      console.error('[2FA Disable Error]', err);
      setDisableError(err.response?.data?.message || 'Thông tin xác thực không chính xác.');
    } finally {
      setDisableLoading(false);
    }
  };

  // 4. Tạo lại mã dự phòng
  const handleRegenerateCodes = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!regenTotp || regenTotp.trim().length !== 6) {
      setRegenError('Vui lòng nhập mã 6 số từ Google Authenticator để xác nhận.');
      return;
    }

    setRegenLoading(true);
    setRegenError(null);
    try {
      const res = await api.post('/api/auth/2fa/regenerate-backup-codes', {
        totpCode: regenTotp.trim()
      });
      setRegenCodes(res.data.recoveryCodes);
      showToast('success', 'Đã tạo mới thành công 8 mã khôi phục dự phòng!');
    } catch (err: any) {
      console.error('[2FA Regen Codes Error]', err);
      setRegenError(err.response?.data?.message || 'Mã xác thực không hợp lệ.');
    } finally {
      setRegenLoading(false);
    }
  };

  // Copy helpers
  const copyToClipboard = (text: string, setCopied: (v: boolean) => void) => {
    navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const downloadBackupCodesFile = (codes: string[]) => {
    const textContent = `=== AEGISQUIZ 2FA RECOVERY CODES ===\nUser: ${user?.email}\nGenerated: ${new Date().toISOString()}\n\nMỗi mã dưới đây chỉ dùng được 1 lần:\n${codes.map((c, i) => `${i + 1}. ${c}`).join('\n')}\n\nLưu ý: Bảo mật tập tin này cẩn thận, không chia sẻ cho bất kỳ ai.`;
    const element = document.createElement('a');
    const file = new Blob([textContent], { type: 'text/plain;charset=utf-8' });
    element.href = URL.createObjectURL(file);
    element.download = `AegisQuiz_2FA_Backup_Codes_${user?.email || 'user'}.txt`;
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
  };

  return (
    <div className="bg-slate-900/60 border border-slate-800 rounded-3xl p-6 sm:p-8 backdrop-blur-xl shadow-xl space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-xl font-black text-white flex items-center gap-2">
            <Smartphone size={22} className="text-indigo-400" />
            Xác Thực 2 Bước (Google Authenticator)
          </h2>
          <p className="text-xs text-slate-400 mt-1">
            Bảo vệ tài khoản theo chuẩn quốc tế RFC 6238 TOTP & NIST SP 800-63B AAL2. Ngăn chặn 99.9% nguy cơ bị đánh cắp mật khẩu.
          </p>
        </div>

        <div>
          {isEnabled ? (
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
              <ShieldCheck size={14} />
              ĐANG BẢO VỆ (ACTIVE)
            </span>
          ) : (
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-amber-500/10 text-amber-400 border border-amber-500/20">
              <ShieldAlert size={14} />
              CHƯA KÍCH HOẠT
            </span>
          )}
        </div>
      </div>

      {/* Toast Alert */}
      {statusMessage && (
        <div className={`p-4 rounded-xl flex items-center gap-3 text-sm font-medium border ${
          statusMessage.type === 'success'
            ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400'
            : 'bg-rose-500/10 border-rose-500/20 text-rose-400'
        }`}>
          {statusMessage.type === 'success' ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />}
          {statusMessage.text}
        </div>
      )}

      {/* Main Info Card */}
      <div className="p-5 rounded-2xl bg-slate-950/60 border border-slate-800/80 flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div className="space-y-1.5">
          <h3 className="text-sm font-bold text-slate-200">
            {isEnabled ? 'Tài khoản của bạn được bảo vệ bằng ứng dụng xác thực' : 'Kích hoạt xác thực 2 bước ngay hôm nay'}
          </h3>
          <p className="text-xs text-slate-400 max-w-xl">
            {isEnabled
              ? 'Mỗi khi đăng nhập bằng Mật khẩu, hệ thống sẽ yêu cầu thêm mã xác nhận 6 chữ số từ ứng dụng Google Authenticator hoặc Microsoft Authenticator trên điện thoại của bạn.'
              : 'Tăng cường phòng vệ chủ động: Bạn sẽ cần quét mã QR bằng ứng dụng Google Authenticator để liên kết thiết bị trước khi có thể đăng nhập.'}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-3 shrink-0">
          {isEnabled ? (
            <>
              <button
                type="button"
                onClick={() => {
                  setShowRegenModal(true);
                  setRegenCodes(null);
                  setRegenTotp('');
                  setRegenError(null);
                }}
                className="px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-bold transition-colors flex items-center gap-2"
              >
                <KeyRound size={15} className="text-indigo-400" />
                Mã dự phòng
              </button>

              <button
                type="button"
                onClick={() => {
                  setShowDisableModal(true);
                  setDisablePassword('');
                  setDisableTotp('');
                  setDisableError(null);
                }}
                className="px-4 py-2.5 rounded-xl bg-rose-500/10 hover:bg-rose-500/20 text-rose-400 border border-rose-500/20 text-xs font-bold transition-colors flex items-center gap-2"
              >
                <Lock size={15} />
                Tắt 2FA
              </button>
            </>
          ) : (
            <button
              type="button"
              onClick={handleStartSetup}
              disabled={setupLoading}
              className="px-5 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-bold shadow-lg shadow-indigo-600/25 transition-all flex items-center gap-2 disabled:opacity-50 cursor-pointer"
            >
              {setupLoading ? <RefreshCw size={15} className="animate-spin" /> : <Smartphone size={15} />}
              {setupLoading ? 'Đang chuẩn bị...' : 'Bật Google Authenticator'}
            </button>
          )}
        </div>
      </div>

      {/* ================= MODAL THIẾT LẬP 2FA ================= */}
      {showSetupModal && setupData && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/85 backdrop-blur-md animate-fadeIn">
          <div className="relative w-full max-w-xl bg-slate-900 border border-slate-700/80 rounded-3xl shadow-2xl p-6 sm:p-8 text-white max-h-[90vh] overflow-y-auto">
            {/* Close Button */}
            <button
              type="button"
              onClick={() => setShowSetupModal(false)}
              className="absolute top-6 right-6 text-slate-400 hover:text-white p-1 rounded-lg"
            >
              <X size={20} />
            </button>

            <div className="space-y-6">
              <div>
                <h3 className="text-xl font-bold text-white flex items-center gap-2">
                  <Smartphone size={22} className="text-indigo-400" />
                  Kích hoạt Google Authenticator
                </h3>
                <p className="text-xs text-slate-400 mt-1">
                  Làm theo 3 bước bên dưới để hoàn tất bảo vệ 2 lớp cho tài khoản của bạn.
                </p>
              </div>

              {setupError && (
                <div className="p-3.5 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-xs flex items-center gap-2.5">
                  <AlertCircle size={16} className="shrink-0" />
                  <span>{setupError}</span>
                </div>
              )}

              {/* Bước 1: Quét mã QR */}
              <div className="p-4 rounded-2xl bg-slate-950/80 border border-slate-800 space-y-3">
                <div className="text-xs font-bold text-slate-200 flex items-center gap-2">
                  <span className="w-5 h-5 rounded-full bg-indigo-600 text-white flex items-center justify-center text-[10px]">1</span>
                  Quét mã QR bằng ứng dụng Authenticator
                </div>
                <div className="flex flex-col sm:flex-row items-center gap-4 pt-1">
                  <div className="p-3 bg-white rounded-2xl shadow-md shrink-0">
                    <QRCodeSVG value={setupData.qrCodeUri} size={150} level="M" />
                  </div>
                  <div className="space-y-2 text-xs text-slate-400">
                    <p>
                      Mở ứng dụng <strong className="text-slate-200">Google Authenticator</strong> hoặc <strong className="text-slate-200">Microsoft Authenticator</strong> trên điện thoại, chọn <em>"Quét mã QR"</em>.
                    </p>
                    <div>
                      <span className="text-[11px] text-slate-500 block mb-1">Hoặc nhập mã khóa bí mật thủ công:</span>
                      <div className="flex items-center gap-2">
                        <code className="px-2.5 py-1 rounded bg-slate-900 border border-slate-700 text-indigo-300 font-mono text-[11px] tracking-wider select-all">
                          {setupData.manualKey}
                        </code>
                        <button
                          type="button"
                          onClick={() => copyToClipboard(setupData.manualKey, setCopiedKey)}
                          className="p-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 transition-colors"
                          title="Sao chép khóa"
                        >
                          {copiedKey ? <Check size={14} className="text-emerald-400" /> : <Copy size={14} />}
                        </button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              {/* Bước 2: Lưu mã dự phòng */}
              <div className="p-4 rounded-2xl bg-slate-950/80 border border-slate-800 space-y-3">
                <div className="flex items-center justify-between">
                  <div className="text-xs font-bold text-slate-200 flex items-center gap-2">
                    <span className="w-5 h-5 rounded-full bg-indigo-600 text-white flex items-center justify-center text-[10px]">2</span>
                    Lưu 8 mã khôi phục dự phòng khẩn cấp
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => copyToClipboard(setupData.recoveryCodes.join('\n'), setCopiedCodes)}
                      className="text-[11px] text-indigo-400 hover:text-indigo-300 flex items-center gap-1 font-medium"
                    >
                      {copiedCodes ? <Check size={12} className="text-emerald-400" /> : <Copy size={12} />}
                      {copiedCodes ? 'Đã sao chép' : 'Sao chép'}
                    </button>
                    <button
                      type="button"
                      onClick={() => downloadBackupCodesFile(setupData.recoveryCodes)}
                      className="text-[11px] text-slate-400 hover:text-slate-200 flex items-center gap-1"
                    >
                      <Download size={12} />
                      Tải file .txt
                    </button>
                  </div>
                </div>
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
                  {setupData.recoveryCodes.map((code, idx) => (
                    <div key={idx} className="p-2 rounded-lg bg-slate-900 border border-slate-800 text-center font-mono text-xs font-bold text-slate-200 tracking-wider">
                      {code}
                    </div>
                  ))}
                </div>
                <p className="text-[11px] text-amber-400/90 flex items-center gap-1">
                  <AlertCircle size={12} />
                  Nếu bị mất điện thoại, bạn có thể dùng các mã này để đăng nhập. Mỗi mã chỉ dùng được 1 lần.
                </p>
              </div>

              {/* Bước 3: Xác nhận mã 6 số */}
              <form onSubmit={handleConfirmEnable} className="space-y-4">
                <div className="text-xs font-bold text-slate-200 flex items-center gap-2">
                  <span className="w-5 h-5 rounded-full bg-indigo-600 text-white flex items-center justify-center text-[10px]">3</span>
                  Nhập mã 6 chữ số hiển thị trên điện thoại để kích hoạt
                </div>
                <div className="flex items-center gap-3">
                  <input
                    type="text"
                    inputMode="numeric"
                    maxLength={6}
                    placeholder="000000"
                    value={verifyCode}
                    onChange={(e) => setVerifyCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                    className="w-44 px-4 py-3 rounded-xl bg-slate-950 border border-slate-700 text-center text-2xl font-mono tracking-widest text-white focus:outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/30"
                    autoFocus
                  />
                  <button
                    type="submit"
                    disabled={verifyLoading || verifyCode.length !== 6}
                    className="flex-1 py-3 px-5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white font-bold text-xs shadow-lg shadow-indigo-600/25 transition-all flex items-center justify-center gap-2 disabled:opacity-50 cursor-pointer"
                  >
                    {verifyLoading ? <RefreshCw size={16} className="animate-spin" /> : <ShieldCheck size={16} />}
                    {verifyLoading ? 'Đang xác minh...' : 'Xác Nhận & Kích Hoạt'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}

      {/* ================= MODAL TẮT 2FA ================= */}
      {showDisableModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/85 backdrop-blur-md animate-fadeIn">
          <div className="relative w-full max-w-md bg-slate-900 border border-slate-700/80 rounded-3xl shadow-2xl p-6 sm:p-8 text-white">
            <button
              type="button"
              onClick={() => setShowDisableModal(false)}
              className="absolute top-6 right-6 text-slate-400 hover:text-white p-1 rounded-lg"
            >
              <X size={20} />
            </button>

            <form onSubmit={handleConfirmDisable} className="space-y-5">
              <div className="text-center space-y-2">
                <div className="w-12 h-12 rounded-2xl bg-rose-500/20 border border-rose-500/30 flex items-center justify-center text-rose-400 mx-auto">
                  <Lock size={24} />
                </div>
                <h3 className="text-lg font-bold text-white">Hủy kích hoạt xác thực 2 bước</h3>
                <p className="text-xs text-slate-400">
                  Hành động này sẽ làm giảm mức độ bảo vệ của tài khoản. Vui lòng xác thực mật khẩu hoặc mã TOTP để tiếp tục.
                </p>
              </div>

              {disableError && (
                <div className="p-3 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-xs flex items-center gap-2">
                  <AlertCircle size={15} className="shrink-0" />
                  <span>{disableError}</span>
                </div>
              )}

              <div>
                <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                  Mật khẩu hiện tại
                </label>
                <div className="relative">
                  <input
                    type={showDisablePassword ? 'text' : 'password'}
                    value={disablePassword}
                    onChange={(e) => setDisablePassword(e.target.value)}
                    placeholder="••••••••••••"
                    className="w-full bg-slate-950 border border-slate-800 focus:border-rose-500 rounded-xl px-4 py-2.5 text-white text-sm pr-11 font-mono focus:outline-none"
                  />
                  <button
                    type="button"
                    onClick={() => setShowDisablePassword(!showDisablePassword)}
                    className="absolute right-3 top-3 text-slate-500 hover:text-slate-300"
                  >
                    {showDisablePassword ? <EyeOff size={15} /> : <Eye size={15} />}
                  </button>
                </div>
              </div>

              <div className="relative flex py-1 items-center">
                <div className="flex-grow border-t border-slate-800"></div>
                <span className="flex-shrink mx-3 text-slate-500 text-[11px] uppercase tracking-wider">Hoặc</span>
                <div className="flex-grow border-t border-slate-800"></div>
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                  Mã 6 số từ Google Authenticator
                </label>
                <input
                  type="text"
                  inputMode="numeric"
                  maxLength={6}
                  value={disableTotp}
                  onChange={(e) => setDisableTotp(e.target.value.replace(/\D/g, '').slice(0, 6))}
                  placeholder="000000"
                  className="w-full bg-slate-950 border border-slate-800 focus:border-rose-500 rounded-xl px-4 py-2.5 text-center text-xl font-mono tracking-widest text-white focus:outline-none"
                />
              </div>

              <div className="pt-2 flex items-center gap-3">
                <button
                  type="button"
                  onClick={() => setShowDisableModal(false)}
                  className="flex-1 py-2.5 px-4 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-bold transition-colors"
                >
                  Hủy bỏ
                </button>
                <button
                  type="submit"
                  disabled={disableLoading || (!disablePassword && disableTotp.length !== 6)}
                  className="flex-1 py-2.5 px-4 rounded-xl bg-rose-600 hover:bg-rose-500 text-white text-xs font-bold shadow-lg shadow-rose-600/25 transition-all flex items-center justify-center gap-2 disabled:opacity-50 cursor-pointer"
                >
                  {disableLoading ? <RefreshCw size={14} className="animate-spin" /> : <Lock size={14} />}
                  {disableLoading ? 'Đang xử lý...' : 'Xác nhận tắt 2FA'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ================= MODAL MÃ DỰ PHÒNG ================= */}
      {showRegenModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/85 backdrop-blur-md animate-fadeIn">
          <div className="relative w-full max-w-md bg-slate-900 border border-slate-700/80 rounded-3xl shadow-2xl p-6 sm:p-8 text-white">
            <button
              type="button"
              onClick={() => setShowRegenModal(false)}
              className="absolute top-6 right-6 text-slate-400 hover:text-white p-1 rounded-lg"
            >
              <X size={20} />
            </button>

            <div className="space-y-5">
              <div className="text-center space-y-2">
                <div className="w-12 h-12 rounded-2xl bg-indigo-500/20 border border-indigo-500/30 flex items-center justify-center text-indigo-400 mx-auto">
                  <KeyRound size={24} />
                </div>
                <h3 className="text-lg font-bold text-white">Quản lý mã khôi phục khẩn cấp</h3>
                <p className="text-xs text-slate-400">
                  Tạo mới 8 mã dự phòng khẩn cấp sẽ vô hiệu hóa toàn bộ các mã dự phòng đã tạo trước đây.
                </p>
              </div>

              {regenError && (
                <div className="p-3 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-xs flex items-center gap-2">
                  <AlertCircle size={15} className="shrink-0" />
                  <span>{regenError}</span>
                </div>
              )}

              {regenCodes ? (
                <div className="space-y-4">
                  <div className="p-4 rounded-2xl bg-slate-950 border border-slate-800 space-y-3">
                    <div className="flex items-center justify-between text-xs">
                      <span className="font-bold text-slate-300">8 Mã dự phòng mới:</span>
                      <div className="flex items-center gap-2">
                        <button
                          type="button"
                          onClick={() => copyToClipboard(regenCodes.join('\n'), setCopiedCodes)}
                          className="text-indigo-400 hover:text-indigo-300 font-medium text-[11px] flex items-center gap-1"
                        >
                          {copiedCodes ? <Check size={12} className="text-emerald-400" /> : <Copy size={12} />}
                          {copiedCodes ? 'Đã chép' : 'Sao chép'}
                        </button>
                        <button
                          type="button"
                          onClick={() => downloadBackupCodesFile(regenCodes)}
                          className="text-slate-400 hover:text-slate-200 text-[11px] flex items-center gap-1"
                        >
                          <Download size={12} />
                          Lưu file
                        </button>
                      </div>
                    </div>
                    <div className="grid grid-cols-2 gap-2">
                      {regenCodes.map((code, idx) => (
                        <div key={idx} className="p-2 rounded-lg bg-slate-900 border border-slate-800 text-center font-mono text-xs font-bold text-emerald-300">
                          {code}
                        </div>
                      ))}
                    </div>
                  </div>

                  <button
                    type="button"
                    onClick={() => setShowRegenModal(false)}
                    className="w-full py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-bold"
                  >
                    Đã lưu xong
                  </button>
                </div>
              ) : (
                <form onSubmit={handleRegenerateCodes} className="space-y-4">
                  <div>
                    <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                      Nhập mã 6 số từ Google Authenticator
                    </label>
                    <input
                      type="text"
                      inputMode="numeric"
                      maxLength={6}
                      value={regenTotp}
                      onChange={(e) => setRegenTotp(e.target.value.replace(/\D/g, '').slice(0, 6))}
                      placeholder="000000"
                      className="w-full bg-slate-950 border border-slate-800 focus:border-indigo-500 rounded-xl px-4 py-2.5 text-center text-xl font-mono tracking-widest text-white focus:outline-none"
                      autoFocus
                    />
                  </div>

                  <div className="pt-2 flex items-center gap-3">
                    <button
                      type="button"
                      onClick={() => setShowRegenModal(false)}
                      className="flex-1 py-2.5 px-4 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-bold transition-colors"
                    >
                      Hủy bỏ
                    </button>
                    <button
                      type="submit"
                      disabled={regenLoading || regenTotp.length !== 6}
                      className="flex-1 py-2.5 px-4 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-bold shadow-lg shadow-indigo-600/25 transition-all flex items-center justify-center gap-2 disabled:opacity-50 cursor-pointer"
                    >
                      {regenLoading ? <RefreshCw size={14} className="animate-spin" /> : <KeyRound size={14} />}
                      {regenLoading ? 'Đang tạo...' : 'Tạo 8 mã mới'}
                    </button>
                  </div>
                </form>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
