import { useState } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router-dom';
import { ShieldCheck, ShieldAlert, Lock, Mail, LogIn, KeyRound, Loader2, ArrowRight, X, Sparkles, Building2, UserCheck } from 'lucide-react';
import { useAgribankPKI } from '../../hooks/useAgribankPKI';
import api from '../../services/api';
import { normalizeRole, type UserProfile } from '@/types/auth';
import { resolveSmartRedirect } from '@/shared/lib/smart-router';

export function LoginPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const returnTo = searchParams.get('returnTo');

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showSsoModal, setShowSsoModal] = useState(false);

  // PKI Hardware USB Token
  const { signData, loading: signLoading, error: signError } = useAgribankPKI();
  const [pkiLoading, setPkiLoading] = useState(false);
  const [pkiError, setPkiError] = useState<string | null>(null);

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

  const quickFillCredential = (fillEmail: string, fillPass: string) => {
    setEmail(fillEmail);
    setPassword(fillPass);
    setShowSsoModal(false);
    setError(null);
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

        {/* Enterprise Hardware & SSO Auth Buttons */}
        <div className="space-y-2.5">
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

          {(pkiError || signError) && (
            <div className="bg-red-950/40 border border-red-500/25 p-3 rounded-xl text-red-400 text-xs flex items-start gap-2">
              <ShieldAlert className="shrink-0 mt-0.5" size={14} />
              <p className="leading-relaxed">{pkiError || signError}</p>
            </div>
          )}
        </div>

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
                Máy chủ Single Sign-On (OIDC / SAML 2.0 / Keycloak) đang trong tiến trình đồng bộ bảo mật liên cơ quan. Quý Cán bộ và Học viên có thể sử dụng các tài khoản định danh chuẩn đã cấp bên dưới:
              </p>
            </div>

            {/* Pre-provisioned Enterprise Accounts */}
            <div className="space-y-2 pt-1">
              <div className="text-[11px] font-bold uppercase tracking-wider text-slate-400">
                Chọn vai trò để tự động điền & đăng nhập ngay:
              </div>

              {/* Admin */}
              <button
                type="button"
                onClick={() => quickFillCredential('admin@dehoc.vn', 'Admin@Dehoc2026!')}
                className="w-full p-3 bg-slate-800/80 hover:bg-slate-750 border border-slate-700/80 hover:border-purple-500/50 rounded-2xl flex items-center justify-between text-left transition-all group active:scale-[0.99]"
              >
                <div className="space-y-0.5">
                  <div className="text-xs font-bold text-purple-300 flex items-center gap-1.5">
                    👑 Quản Trị Viên Hệ Thống (Tenant Admin)
                  </div>
                  <div className="text-[11px] text-slate-400">admin@dehoc.vn (Toàn quyền quản trị & ngân hàng đề)</div>
                </div>
                <Sparkles size={16} className="text-purple-400 group-hover:scale-110 transition-transform" />
              </button>

              {/* Team Leader */}
              <button
                type="button"
                onClick={() => quickFillCredential('teamlead@dehoc.vn', 'Lead@Dehoc2026!')}
                className="w-full p-3 bg-slate-800/80 hover:bg-slate-750 border border-slate-700/80 hover:border-blue-500/50 rounded-2xl flex items-center justify-between text-left transition-all group active:scale-[0.99]"
              >
                <div className="space-y-0.5">
                  <div className="text-xs font-bold text-blue-300 flex items-center gap-1.5">
                    💼 Trưởng Nhóm Khảo Thí (TeamLeader)
                  </div>
                  <div className="text-[11px] text-slate-400">teamlead@dehoc.vn (Quản lý phòng thi & giám sát AI)</div>
                </div>
                <Sparkles size={16} className="text-blue-400 group-hover:scale-110 transition-transform" />
              </button>

              {/* VIP Learner */}
              <button
                type="button"
                onClick={() => quickFillCredential('student.vip@dehoc.vn', 'Vip@Dehoc2026!')}
                className="w-full p-3 bg-slate-800/80 hover:bg-slate-750 border border-slate-700/80 hover:border-amber-500/50 rounded-2xl flex items-center justify-between text-left transition-all group active:scale-[0.99]"
              >
                <div className="space-y-0.5">
                  <div className="text-xs font-bold text-amber-300 flex items-center gap-1.5">
                    🌟 Học Viên VIP (AI Adaptive CAT)
                  </div>
                  <div className="text-[11px] text-slate-400">student.vip@dehoc.vn (Mở khóa toàn bộ tính năng VIP)</div>
                </div>
                <Sparkles size={16} className="text-amber-400 group-hover:scale-110 transition-transform" />
              </button>
            </div>

            {/* Hardware PKI Option */}
            <div className="pt-2 border-t border-slate-800 flex items-center justify-between gap-3">
              <button
                type="button"
                onClick={() => {
                  setShowSsoModal(false);
                  handlePkiLogin();
                }}
                className="text-xs font-semibold text-emerald-400 hover:text-emerald-300 flex items-center gap-1.5 py-1.5 transition-colors"
              >
                <Lock size={13} /> Đăng nhập bằng USB Token PKI
              </button>

              <button
                type="button"
                onClick={() => {
                  directRedirectToKeycloak();
                }}
                className="text-[11px] text-slate-500 hover:text-slate-300 transition-colors underline"
              >
                Chuyển hướng Keycloak gốc &rarr;
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default LoginPage;
