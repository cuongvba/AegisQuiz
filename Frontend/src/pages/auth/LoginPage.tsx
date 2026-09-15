import { useState, useEffect, useRef } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { ShieldCheck, ShieldAlert, Lock, Mail, LogIn, KeyRound, Loader2, ArrowRight, X, Sparkles, Building2, UserCheck, Smartphone, QrCode } from 'lucide-react';
import { useAgribankPKI } from '../../hooks/useAgribankPKI';
import api from '../../services/api';
import { normalizeRole, type UserProfile } from '@/types/auth';
import { resolveSmartRedirect } from '@/shared/lib/smart-router';
import { ForgotPasswordModal } from './ForgotPasswordModal';
import { TwoFactorChallengeModal } from './TwoFactorChallengeModal';
import { QrLoginCard } from './QrLoginCard';

export function LoginPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const returnTo = searchParams.get('returnTo');

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showSsoModal, setShowSsoModal] = useState(false);
  const [isForgotOpen, setIsForgotOpen] = useState(false);
  const [authMethod, setAuthMethod] = useState<'credentials' | 'qr'>('credentials');

  // 2FA Google Authenticator State
  const [show2FAModal, setShow2FAModal] = useState(false);
  const [mfaTempToken, setMfaTempToken] = useState('');
  const [mfaUserEmail, setMfaUserEmail] = useState('');

  // Google OAuth 2.0 Client ID
  const GOOGLE_CLIENT_ID = (import.meta as any).env?.VITE_GOOGLE_CLIENT_ID || '193004715260-v3bkisubivcfs8mj7efegf408dphc82i.apps.googleusercontent.com';
  const googleBtnRef = useRef<HTMLDivElement>(null);
  const [isGsiRendered, setIsGsiRendered] = useState(false);

  // Tự động tải thư viện Google Identity Services (GIS)
  useEffect(() => {
    const scriptId = 'google-jssdk';
    const initGsi = () => {
      if ((window as any).google?.accounts?.id) {
        try {
          (window as any).google.accounts.id.initialize({
            client_id: GOOGLE_CLIENT_ID,
            callback: handleGoogleCredentialResponse,
            auto_select: false,
          });

          // Render nút Google chính hãng nếu có container
          if (googleBtnRef.current) {
            (window as any).google.accounts.id.renderButton(googleBtnRef.current, {
              theme: 'filled_black',
              size: 'large',
              width: 380,
              text: 'continue_with',
              shape: 'rectangular',
              logo_alignment: 'center'
            });
            setIsGsiRendered(true);
          }
        } catch (e) {
          console.warn('Lỗi khởi tạo Google Identity Services', e);
        }
      }
    };

    if (!document.getElementById(scriptId)) {
      const script = document.createElement('script');
      script.id = scriptId;
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.onload = initGsi;
      document.body.appendChild(script);
    } else {
      initGsi();
    }
  }, []);

  const handleGoogleCredentialResponse = async (response: any) => {
    if (!response?.credential) return;
    setLoading(true);
    setError(null);
    try {
      const res = await api.post('/api/auth/google', {
        idToken: response.credential
      });

      // Nếu tài khoản Google này đã bật Google Authenticator (2FA)
      if (res.data?.requires2FA) {
        setMfaTempToken(res.data.tempToken || res.data.mfaTempToken);
        setMfaUserEmail(res.data.email || res.data.user?.email || '');
        setShow2FAModal(true);
        setLoading(false);
        return;
      }

      const { token, user } = res.data;
      handle2FASuccess(token, user);
    } catch (err: any) {
      console.error('[Google Auth Error]', err);
      setError(err.response?.data?.message || 'Đăng nhập bằng tài khoản Google thất bại.');
    } finally {
      setLoading(false);
    }
  };

  const handleGoogleLoginClick = () => {
    if ((window as any).google?.accounts?.id) {
      (window as any).google.accounts.id.prompt();
    } else {
      setError('Đang kết nối dịch vụ Google Identity... Vui lòng thử lại sau giây lát.');
    }
  };

  // PKI Hardware USB Token
  const { signData, loading: signLoading, error: signError } = useAgribankPKI();
  const [pkiLoading, setPkiLoading] = useState(false);
  const [pkiError, setPkiError] = useState<string | null>(null);

  // Xử lý đăng nhập thành công sau 2FA
  const handle2FASuccess = (token: string, user: any) => {
    const normalizedProfile: UserProfile = {
      id: user.id,
      name: user.name || user.email.split('@')[0],
      email: user.email,
      role: normalizeRole(user.role),
      isPremium: Boolean(user.isPremium),
      subscriptionTier: user.subscriptionTier || 'FREE',
      tenantId: user.tenantId,
      orgUnitId: user.orgUnitId,
      avatar: user.avatar
    };

    localStorage.setItem('token', token);
    localStorage.setItem('user', JSON.stringify(normalizedProfile));

    const targetUrl = resolveSmartRedirect(normalizedProfile, returnTo);
    navigate(targetUrl, { replace: true });
    window.location.reload();
  };

  // 1. Xử lý Đăng nhập qua Email + Mật khẩu (Server-Side Authentication)
  const handleCredentialsLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const response = await api.post('/api/auth/login', {
        email: email.trim(),
        password: password
      });

      // Nếu tài khoản đã bật 2FA Google Authenticator
      if (response.data?.requires2FA) {
        setMfaTempToken(response.data.tempToken || response.data.mfaTempToken);
        setMfaUserEmail(response.data.email || response.data.user?.email || email.trim());
        setShow2FAModal(true);
        setLoading(false);
        return;
      }

      const { token, user } = response.data;
      if (!token || !user) {
        throw new Error('Dữ liệu xác thực từ máy chủ không hợp lệ.');
      }

      const normalizedProfile: UserProfile = {
        id: user.id,
        name: user.name || user.email.split('@')[0],
        email: user.email,
        role: normalizeRole(user.role),
        isPremium: Boolean(user.isPremium),
        subscriptionTier: user.subscriptionTier || 'FREE',
        tenantId: user.tenantId,
        orgUnitId: user.orgUnitId,
        avatar: user.avatar
      };

      // Lưu phiên bảo mật
      localStorage.setItem('token', token);
      localStorage.setItem('user', JSON.stringify(normalizedProfile));

      // Phân luồng điều hướng thông minh theo vai trò và hạng thuê bao
      const targetUrl = resolveSmartRedirect(normalizedProfile, returnTo);
      navigate(targetUrl, { replace: true });
      window.location.reload();

    } catch (err: any) {
      console.error('[Login Error]', err);
      let serverMsg = 'Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin.';
      if (err.response?.status === 503 || err.response?.status === 502) {
        serverMsg = 'Dịch vụ máy chủ đang khởi động hoặc nâng cấp. Vui lòng thử lại sau 10-15 giây.';
      } else if (err.response?.data?.message) {
        serverMsg = err.response.data.message;
      } else if (typeof err.response?.data === 'string' && err.response.data.length < 150) {
        serverMsg = err.response.data;
      } else if (err.message) {
        serverMsg = err.message;
      }
      setError(serverMsg);
    } finally {
      setLoading(false);
    }
  };

  // 2. Xử lý Đăng nhập bằng Chữ ký số USB Token PKI
  const handlePkiLogin = async () => {
    setPkiLoading(true);
    setPkiError(null);
    setError(null);
    try {
      const challengeRes = await api.post('/api/auth/pki/challenge');
      const { challenge } = challengeRes.data;

      const signResult = await signData({
        contentB64: challenge,
        typeSign: 'text'
      });

      const signature = signResult.dataSigned || signResult.dataSigned2;
      const certificate = signResult.ctsInfo;

      if (!signature || !certificate) {
        throw new Error('Plugin không trả về chữ ký số hợp lệ từ thiết bị phần cứng.');
      }

      const loginRes = await api.post('/api/auth/pki/login', {
        challenge,
        signature,
        certificateBase64: certificate
      });

      const { token, user } = loginRes.data;
      const normalizedProfile: UserProfile = {
        id: user.id,
        name: user.name || 'Cán bộ PKI',
        email: user.email || 'pki.user@aegisquiz.com',
        role: normalizeRole(user.role),
        isPremium: Boolean(user.isPremium),
        subscriptionTier: user.subscriptionTier || 'ENTERPRISE',
        tenantId: user.tenantId,
        orgUnitId: user.orgUnitId
      };

      localStorage.setItem('token', token);
      localStorage.setItem('user', JSON.stringify(normalizedProfile));

      const targetUrl = resolveSmartRedirect(normalizedProfile, returnTo);
      navigate(targetUrl, { replace: true });
      window.location.reload();
    } catch (err: any) {
      console.error('[PKI Auth Error]', err);
      setPkiError(err.response?.data?.message || err.message || 'Lỗi giao tiếp với thiết bị USB Token PKI.');
    } finally {
      setPkiLoading(false);
    }
  };

  // 3. Xử lý Đăng nhập SSO Keycloak Tổ chức (OIDC Authorization Code Flow)
  const handleKeycloakOidcLogin = () => {
    const customKeycloakUrl = (import.meta as any).env?.VITE_KEYCLOAK_URL;
    const directSsoEnabled = (import.meta as any).env?.VITE_ENABLE_DIRECT_SSO === 'true';

    if (directSsoEnabled && customKeycloakUrl) {
      directRedirectToKeycloak(customKeycloakUrl);
      return;
    }

    // Mở Enterprise SSO Gateway Modal thông minh
    setShowSsoModal(true);
  };

  const directRedirectToKeycloak = (authorityOverride?: string) => {
    const authority = authorityOverride || (import.meta as any).env?.VITE_KEYCLOAK_URL || `${window.location.origin}/auth`;
    const clientId = 'quiz-service';
    const redirectUri = `${window.location.origin}/auth/callback`;
    const authUrl = `${authority}/realms/dehoc/protocol/openid-connect/auth?client_id=${clientId}&response_type=code&scope=openid%20profile%20email&redirect_uri=${encodeURIComponent(redirectUri)}`;
    window.location.href = authUrl;
  };

  return (
    <div className="min-h-screen bg-slate-950 text-white flex flex-col items-center justify-center p-6 relative overflow-hidden font-sans select-none">
      {/* Dynamic Background Glows */}
      <div className="absolute top-1/4 left-1/4 w-[480px] h-[480px] bg-blue-600/15 rounded-full blur-[140px] pointer-events-none" />
      <div className="absolute bottom-1/4 right-1/4 w-[480px] h-[480px] bg-purple-600/15 rounded-full blur-[140px] pointer-events-none" />

      {/* Main Login Card */}
      <div className="w-full max-w-md bg-slate-900/80 border border-slate-800/90 rounded-3xl p-8 shadow-2xl backdrop-blur-2xl relative z-10 space-y-6">

        {/* Brand Header */}
        <div className="text-center space-y-2">
          <div className="inline-flex items-center gap-1.5 px-3 py-1 bg-blue-500/10 border border-blue-500/20 text-blue-400 rounded-full text-[11px] font-bold tracking-wide">
            <ShieldCheck size={14} className="text-blue-400" /> Nền tảng Khảo thí & Đào tạo Doanh nghiệp
          </div>
          <h1 className="text-3xl font-black tracking-tight text-transparent bg-clip-text bg-gradient-to-r from-blue-400 via-indigo-300 to-purple-400">
            ĐĂNG NHẬP
          </h1>
          <p className="text-xs text-slate-400">
            Truy cập phân hệ đào tạo, ngân hàng tri thức và đấu trường khảo thí AI.
          </p>
        </div>

        {/* Auth Method Switcher Tabs */}
        <div className="flex p-1 bg-slate-950/80 border border-slate-800 rounded-2xl shadow-inner">
          <button
            type="button"
            onClick={() => setAuthMethod('credentials')}
            className={`flex-1 py-2.5 text-xs font-bold rounded-xl transition-all flex items-center justify-center gap-2 ${
              authMethod === 'credentials'
                ? 'bg-gradient-to-r from-blue-600 to-indigo-600 text-white shadow-md shadow-blue-500/25'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <Lock size={14} /> Mật khẩu & Google
          </button>
          <button
            type="button"
            onClick={() => setAuthMethod('qr')}
            className={`flex-1 py-2.5 text-xs font-bold rounded-xl transition-all flex items-center justify-center gap-2 ${
              authMethod === 'qr'
                ? 'bg-gradient-to-r from-indigo-600 to-purple-600 text-white shadow-md shadow-indigo-500/25'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <QrCode size={14} /> Quét mã QR 1-giây
          </button>
        </div>

        {/* Tab Content: QR Login vs Credentials & Google */}
        {authMethod === 'qr' ? (
          <QrLoginCard onSuccess={handle2FASuccess} />
        ) : (
          <>
            {/* Form Credentials Login */}
            <form onSubmit={handleCredentialsLogin} className="space-y-4">
              <div className="space-y-1.5">
                <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">
                  Email tài khoản
                </label>
                <div className="relative">
                  <span className="absolute left-3.5 top-3.5 text-slate-500">
                    <Mail size={16} />
                  </span>
                  <input
                    type="email"
                    required
                    autoComplete="email"
                    placeholder="name@dehoc.vn hoặc email cá nhân"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="w-full bg-slate-950/80 border border-slate-800 rounded-xl pl-10 pr-4 py-3 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors shadow-inner"
                  />
                </div>
              </div>

              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">
                    Mật khẩu
                  </label>
                  <button
                    type="button"
                    onClick={() => setIsForgotOpen(true)}
                    className="text-[11px] font-medium text-blue-400 hover:text-blue-300 transition-colors cursor-pointer"
                  >
                    Quên mật khẩu?
                  </button>
                </div>
                <div className="relative">
                  <span className="absolute left-3.5 top-3.5 text-slate-500">
                    <Lock size={16} />
                  </span>
                  <input
                    type="password"
                    required
                    autoComplete="current-password"
                    placeholder="••••••••"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full bg-slate-950/80 border border-slate-800 rounded-xl pl-10 pr-4 py-3 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors shadow-inner"
                  />
                </div>
              </div>

              {error && (
                <div className="bg-red-950/40 border border-red-500/25 p-3 rounded-xl text-red-400 text-xs flex items-start gap-2 animate-in fade-in duration-200">
                  <ShieldAlert className="shrink-0 mt-0.5" size={15} />
                  <p className="leading-relaxed">{error}</p>
                </div>
              )}

              <button
                type="submit"
                disabled={loading}
                className="w-full py-3.5 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white font-bold rounded-xl transition-all shadow-lg shadow-blue-500/20 active:scale-[0.98] text-xs flex items-center justify-center gap-2 cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {loading ? (
                  <>
                    <Loader2 size={16} className="animate-spin" /> Đang xác thực thông tin...
                  </>
                ) : (
                  <>
                    <LogIn size={16} /> Đăng nhập hệ thống <ArrowRight size={14} />
                  </>
                )}
              </button>
            </form>

            {/* Divider */}
            <div className="relative flex items-center justify-center my-4">
              <div className="absolute w-full border-t border-slate-800"></div>
              <span className="relative px-3 bg-slate-900 text-[10px] text-slate-500 font-bold uppercase tracking-widest">
                Hoặc phương thức bảo mật khác
              </span>
            </div>

            {/* Enterprise Hardware, Google & SSO Auth Buttons */}
            <div className="space-y-2.5">
              {/* Nút Đăng nhập với Google Duy Nhất */}
              <div className="w-full flex justify-center min-h-[40px] overflow-hidden rounded-xl">
                <div ref={googleBtnRef} className="w-full flex justify-center" />
              </div>

              {!isGsiRendered && (
                <button
                  type="button"
                  onClick={handleGoogleLoginClick}
                  disabled={loading}
                  className="w-full py-2.5 px-4 bg-white hover:bg-slate-100 text-slate-800 font-bold rounded-xl transition-all active:scale-[0.98] text-xs flex items-center justify-center gap-2.5 cursor-pointer shadow-md border border-slate-200/80"
                >
                  <svg className="w-4 h-4" viewBox="0 0 24 24">
                    <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
                    <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
                    <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z" />
                    <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z" />
                  </svg>
                  <span>Tiếp tục với tài khoản Google</span>
                </button>
              )}

              <button
                type="button"
                onClick={handlePkiLogin}
                disabled={pkiLoading || signLoading}
                className="w-full py-2.5 bg-emerald-950/30 hover:bg-emerald-900/40 border border-emerald-500/30 text-emerald-400 font-bold rounded-xl transition-all active:scale-[0.98] text-xs flex items-center justify-center gap-2 cursor-pointer disabled:opacity-50"
              >
                <Lock size={15} /> {pkiLoading || signLoading ? 'Đang giao tiếp thiết bị Token...' : 'Đăng nhập bằng USB Token (PKI)'}
              </button>

              <button
                type="button"
                onClick={handleKeycloakOidcLogin}
                className="w-full py-2.5 bg-slate-950 hover:bg-slate-800/80 border border-slate-800 text-slate-300 font-bold rounded-xl transition-all active:scale-[0.98] text-xs flex items-center justify-center gap-2 cursor-pointer"
              >
                <KeyRound size={15} className="text-blue-400" /> Đăng nhập qua SSO Doanh nghiệp (Keycloak OIDC)
              </button>

              {/* Badge Xác Thực 2 Bước Google Authenticator */}
              <div className="p-2.5 rounded-xl bg-indigo-950/40 border border-indigo-500/25 flex items-center justify-between text-xs text-indigo-300">
                <span className="flex items-center gap-1.5 font-semibold text-[11px]">
                  <Smartphone size={14} className="text-indigo-400" />
                  Bảo vệ 2 lớp Google Authenticator (TOTP)
                </span>
                <span className="text-[10px] bg-indigo-500/20 text-indigo-300 border border-indigo-500/30 px-2 py-0.5 rounded-md font-bold">
                  Đang hoạt động
                </span>
              </div>

              {(pkiError || signError) && (
                <div className="bg-red-950/40 border border-red-500/25 p-3 rounded-xl text-red-400 text-xs flex items-start gap-2">
                  <ShieldAlert className="shrink-0 mt-0.5" size={14} />
                  <p className="leading-relaxed">{pkiError || signError}</p>
                </div>
              )}
            </div>
          </>
        )}

        {/* Registration Link */}
        <div className="text-center pt-2 border-t border-slate-800/60">
          <p className="text-xs text-slate-400">
            Chưa có tài khoản học viên?{' '}
            <Link to="/register" className="text-blue-400 hover:text-blue-300 font-bold transition-colors">
              Đăng ký ngay
            </Link>
          </p>
        </div>
      </div>

      {/* Enterprise SSO Gateway Modal */}
      {showSsoModal && (
        <div className="fixed inset-0 z-50 bg-slate-950/80 backdrop-blur-md flex items-center justify-center p-4 animate-in fade-in duration-200">
          <div className="bg-slate-900/95 border border-slate-700/80 rounded-3xl p-6 sm:p-7 max-w-lg w-full shadow-2xl relative space-y-5 text-white">
            {/* Close Button */}
            <button
              onClick={() => setShowSsoModal(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-white p-2 rounded-xl hover:bg-slate-800 transition-colors"
            >
              <X size={18} />
            </button>

            {/* Header */}
            <div className="space-y-1.5">
              <div className="inline-flex items-center gap-1.5 px-3 py-1 bg-purple-500/15 border border-purple-500/30 text-purple-300 rounded-full text-[11px] font-bold">
                <Building2 size={13} /> Cổng Định Danh Doanh Nghiệp (SSO / IAM)
              </div>
              <h2 className="text-xl font-bold text-slate-100 flex items-center gap-2">
                Liên kết Định danh Hệ sinh thái dehoc.vn
              </h2>
              <p className="text-xs text-slate-400 leading-relaxed">
                Hạ tầng Đăng nhập Một lần (OpenID Connect / SAML 2.0 / Keycloak) đang trong lộ trình tích hợp đồng bộ bảo mật với Cổng thông tin tập trung <strong className="text-slate-200">dehoc.vn</strong>.
              </p>
            </div>

            {/* Official Authentication Options */}
            <div className="space-y-3 pt-1">
              <div className="text-[11px] font-bold uppercase tracking-wider text-slate-400">
                Phương thức xác thực chính thức:
              </div>

              {/* Option 1: Standard Credentials */}
              <button
                type="button"
                onClick={() => setShowSsoModal(false)}
                className="w-full p-3.5 bg-slate-800/80 hover:bg-slate-750 border border-slate-700/80 hover:border-blue-500/50 rounded-2xl flex items-center justify-between text-left transition-all group active:scale-[0.99]"
              >
                <div className="space-y-0.5">
                  <div className="text-xs font-bold text-blue-300 flex items-center gap-1.5">
                    <Mail size={14} className="text-blue-400" /> Đăng nhập bằng Email & Mật khẩu
                  </div>
                  <div className="text-[11px] text-slate-400">
                    Sử dụng tài khoản Quản trị, Giảng viên hoặc Học viên đã được cấp phát.
                  </div>
                </div>
                <ArrowRight size={16} className="text-blue-400 group-hover:translate-x-0.5 transition-transform" />
              </button>

              {/* Option 2: Hardware PKI */}
              <button
                type="button"
                onClick={() => {
                  setShowSsoModal(false);
                  handlePkiLogin();
                }}
                className="w-full p-3.5 bg-slate-800/80 hover:bg-slate-750 border border-slate-700/80 hover:border-emerald-500/50 rounded-2xl flex items-center justify-between text-left transition-all group active:scale-[0.99]"
              >
                <div className="space-y-0.5">
                  <div className="text-xs font-bold text-emerald-300 flex items-center gap-1.5">
                    <Lock size={14} className="text-emerald-400" /> Xác thực Chữ ký số USB Token PKI
                  </div>
                  <div className="text-[11px] text-slate-400">
                    Tiêu chuẩn bảo mật phần cứng cấp cao nhất cho Cán bộ & Trưởng ban khảo thí.
                  </div>
                </div>
                <ArrowRight size={16} className="text-emerald-400 group-hover:translate-x-0.5 transition-transform" />
              </button>
            </div>

            {/* Bottom info & Keycloak experimental redirect */}
            <div className="pt-2 border-t border-slate-800 flex items-center justify-between gap-3">
              <span className="text-[11px] text-slate-500">
                Cần cấp tài khoản? Liên hệ quản trị viên cơ quan.
              </span>

              <button
                type="button"
                onClick={() => {
                  directRedirectToKeycloak();
                }}
                className="text-[11px] text-slate-500 hover:text-purple-300 transition-colors underline"
              >
                Kết nối Keycloak IdP &rarr;
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Forgot Password Modal */}
      <ForgotPasswordModal
        isOpen={isForgotOpen}
        onClose={() => setIsForgotOpen(false)}
      />

      {/* 2FA Google Authenticator Challenge Modal */}
      <TwoFactorChallengeModal
        isOpen={show2FAModal}
        onClose={() => setShow2FAModal(false)}
        mfaTempToken={mfaTempToken}
        userEmail={mfaUserEmail}
        onSuccess={handle2FASuccess}
      />
    </div>
  );
}

export default LoginPage;
