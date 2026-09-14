import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { KeyRound, ShieldAlert, Sparkles, User, Lock, Globe, LogIn, X, Mail } from 'lucide-react';
import { useAgribankPKI } from '../../hooks/useAgribankPKI';
import api from '../../services/api';
import { AppRole, normalizeRole } from '@/types/auth';

export function LoginPage() {
  const navigate = useNavigate();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);

  // State quản trị SSO Sandbox
  const [activeSsoModal, setActiveSsoModal] = useState<'keycloak' | 'google' | null>(null);
  const [keycloakUsername, setKeycloakUsername] = useState('');
  const [keycloakPassword, setKeycloakPassword] = useState('');
  const [ssoError, setSsoError] = useState<string | null>(null);

  // State quản trị PKI
  const { signData, loading: signLoading, error: signError } = useAgribankPKI();
  const [pkiLoading, setPkiLoading] = useState(false);
  const [pkiError, setPkiError] = useState<string | null>(null);

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
        throw new Error('Plugin không trả về chữ ký hoặc chứng thư số.');
      }

      const loginRes = await api.post('/api/auth/pki/login', {
        challenge: challenge,
        signature: signature,
        certificateBase64: certificate
      });

      const { token, user } = loginRes.data;

      localStorage.setItem('token', token);
      localStorage.setItem('user', JSON.stringify(user));

      if (typeof window !== 'undefined' && window.location.hostname.endsWith('dehoc.vn') && window.location.hostname !== 'daotao.dehoc.vn') {
        window.location.href = 'https://daotao.dehoc.vn/';
      } else {
        navigate('/');
        window.location.reload();
      }
    } catch (err: any) {
      console.error(err);
      setPkiError(err.response?.data?.message || err.message || 'Lỗi xác thực đăng nhập PKI.');
    } finally {
      setPkiLoading(false);
    }
  };

  const getRedirectUri = () => {
    const origin = typeof window !== 'undefined' && window.location.hostname.endsWith('dehoc.vn')
      ? 'https://daotao.dehoc.vn'
      : window.location.origin;
    return `${origin}/auth/callback`;
  };

  // 1. Mở Modal xác thực giả lập Keycloak SSO
  const handleKeycloakSSO = () => {
    setSsoError(null);
    setActiveSsoModal('keycloak');
  };

  // 2. Mở Modal xác thực giả lập Google SSO
  const handleGoogleSSO = () => {
    setSsoError(null);
    setActiveSsoModal('google');
  };

  // Hoàn tất luồng SSO giả lập
  const submitSsoLogin = (emailAddress: string) => {
    let ssoUser = null;

    const safeBtoa = (str: string) => btoa(unescape(encodeURIComponent(str)));

    if (emailAddress.includes('admin') || emailAddress === 'cuongvba@gmail.com') {
      ssoUser = { id: 'sso_admin_123', name: 'Quản trị viên (SSO)', email: emailAddress, role: AppRole.TenantAdmin, isPremium: true };
    } else if (emailAddress.includes('teamlead') || emailAddress.includes('leader')) {
      // ★ Sandbox TeamLeader SSO
      ssoUser = { id: 'sso_lead_123', name: 'Trưởng nhóm (SSO) 📊', email: emailAddress, role: AppRole.TeamLeader, isPremium: true, orgUnitId: 'team-demo-guid' };
    } else if (emailAddress.includes('vip')) {
      ssoUser = { id: 'sso_vip_123', name: 'Học viên VIP (SSO) 💎', email: emailAddress, role: AppRole.Learner, isPremium: true };
    } else {
      ssoUser = { id: 'sso_free_123', name: 'Học viên Free (SSO)', email: emailAddress, role: AppRole.Learner, isPremium: false };
    }

    // Sinh token chuẩn Jwt
    const mockJwtToken = 'sso_jwt_token_' + safeBtoa(JSON.stringify(ssoUser));

    // Thực hiện redirect đúng chuẩn OIDC Callback
    const callbackPath = `/auth/callback?auth_token=${mockJwtToken}&auth_user=${encodeURIComponent(JSON.stringify(ssoUser))}`;

    setActiveSsoModal(null);
    navigate(callbackPath);
  };

  const handleKeycloakSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!keycloakUsername) {
      setSsoError('Vui long dien email tai khoan.');
      return;
    }
    submitSsoLogin(keycloakUsername);
  };

  // 3. Credentials Auth
  const handleCredentialsLogin = (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        const safeBtoa = (str: string) => btoa(unescape(encodeURIComponent(str)));
        let loggedInUser = null;

        if (username === 'admin@aegisquiz.com') {
          // [RBAC] TenantAdmin — vào được /admin
          loggedInUser = { id: 'a55850fa-1188-4f24-81e5-827c84a860b2', name: 'Quản trị viên', email: username, role: AppRole.TenantAdmin, isPremium: true };
        } else if (username === 'teamlead@aegisquiz.com') {
          // [RBAC] ★ TeamLeader — vào /team, không vào được /admin
          loggedInUser = { id: 'e1234567-abcd-4f00-b000-000000000001', name: 'Trưởng nhóm 📊', email: username, role: AppRole.TeamLeader, isPremium: true, orgUnitId: 'team-demo-guid' };
        } else if (username === 'student.vip@aegisquiz.com') {
          loggedInUser = { id: 'b8cd0927-4c4f-4d94-9b59-7b3b9b47e5b6', name: 'Học viên VIP 💎', email: username, role: AppRole.Learner, isPremium: true };
        } else if (username === 'student.free@aegisquiz.com') {
          loggedInUser = { id: 'd7454848-d3e9-4e6f-9721-a3f16a04874c', name: 'Học viên Free', email: username, role: AppRole.Learner, isPremium: false };
        } else {
          setError('Tài khoản hoặc mật khẩu không chính xác. Hãy nhấp thử các tài khoản mẫu bên dưới.');
          return;
        }

        const mockJwtToken = 'mock_jwt_token_' + safeBtoa(JSON.stringify(loggedInUser));

        localStorage.setItem('token', mockJwtToken);
        localStorage.setItem('user', JSON.stringify(loggedInUser));

        if (typeof window !== 'undefined' && window.location.hostname.endsWith('dehoc.vn') && window.location.hostname !== 'daotao.dehoc.vn') {
          window.location.href = 'https://daotao.dehoc.vn/';
        } else {
          navigate('/');
          window.location.reload();
        }
      };

      const autoFill = (userType: 'admin' | 'teamlead' | 'vip' | 'free') => {
        setError(null);
        if (userType === 'admin') {
          setUsername('admin@aegisquiz.com');
          setPassword('AdminPass2026');
        } else if (userType === 'teamlead') {
          setUsername('teamlead@aegisquiz.com');
          setPassword('LeadPass2026');
        } else if (userType === 'vip') {
          setUsername('student.vip@aegisquiz.com');
          setPassword('VipPass2026');
        } else {
          setUsername('student.free@aegisquiz.com');
          setPassword('FreePass2026');
        }
      };

      return (
        <div className="min-h-screen bg-slate-950 text-white flex flex-col items-center justify-center p-6 relative overflow-hidden font-sans">
          {/* Background Glow */}
          <div className="absolute top-1/4 left-1/3 w-96 h-96 bg-purple-650/20 rounded-full blur-[120px] pointer-events-none" />
          <div className="absolute bottom-1/4 right-1/3 w-96 h-96 bg-blue-650/20 rounded-full blur-[120px] pointer-events-none" />

          <div className="w-full max-w-md bg-slate-900/70 border border-slate-800 rounded-3xl p-8 shadow-2xl backdrop-blur-xl relative z-10 space-y-6">

            <div className="text-center space-y-2">
              <div className="inline-flex items-center gap-1.5 px-3 py-1 bg-blue-950/50 border border-blue-500/30 text-blue-300 rounded-full text-xs font-black tracking-wide uppercase">
                <Sparkles size={12} /> Cảnh giới 6: Identity SSO
              </div>
              <h1 className="text-3xl font-black tracking-tight text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400">
                ĐĂNG NHẬP
              </h1>
              <p className="text-xs text-slate-400">Đăng nhập vào AegisQuiz để lưu tiến trình học tập.</p>
            </div>

            {/* SSO AUTH BUTTONS */}
            <div className="space-y-2.5">
              <button
                type="button"
                onClick={handleKeycloakSSO}
                className="w-full py-3 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-xl transition-all active:scale-95 text-xs flex items-center justify-center gap-2 cursor-pointer shadow-md shadow-blue-500/10"
              >
                <KeyRound size={16} /> Đăng nhập qua Keycloak SSO Hub
              </button>

              <button
                type="button"
                onClick={handleGoogleSSO}
                className="w-full py-3 bg-white hover:bg-gray-105 text-slate-800 font-bold rounded-xl transition-all active:scale-95 text-xs flex items-center justify-center gap-2 cursor-pointer border border-slate-200"
              >
                <Globe size={16} className="text-red-500" /> Đăng nhập bằng Google Account
              </button>

              <button
                type="button"
                onClick={handlePkiLogin}
                disabled={pkiLoading || signLoading}
                className="w-full py-3 bg-emerald-600 hover:bg-emerald-700 text-white font-bold rounded-xl transition-all active:scale-95 text-xs flex items-center justify-center gap-2 cursor-pointer shadow-md shadow-emerald-500/10 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <Lock size={16} /> {pkiLoading || signLoading ? 'Đang giao tiếp Token...' : 'Đăng nhập bằng USB Token (PKI)'}
              </button>

              {(pkiError || signError) && (
                <div className="bg-red-950/40 border border-red-500/25 p-3 rounded-xl text-red-400 text-[10px] flex items-start gap-1.5 mt-1.5">
                  <ShieldAlert className="shrink-0 mt-0.5" size={12} />
                  <p className="leading-relaxed">{pkiError || signError}</p>
                </div>
              )}
            </div>

            <div className="relative flex items-center justify-center my-4">
              <div className="absolute w-full border-t border-slate-800"></div>
              <span className="relative px-3 bg-slate-900 text-xs text-slate-500 font-bold uppercase tracking-widest">Hoặc dùng tài khoản</span>
            </div>

            {/* CREDENTIALS FORM */}
            <form onSubmit={handleCredentialsLogin} className="space-y-4">
              <div className="space-y-1.5">
                <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Email tài khoản</label>
                <div className="relative">
                  <span className="absolute left-3.5 top-3.5 text-slate-500"><User size={16} /></span>
                  <input
                    type="email"
                    required
                    placeholder="email@example.com"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    className="w-full bg-slate-950/70 border border-slate-800 rounded-xl pl-10 pr-4 py-3 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors"
                  />
                </div>
              </div>

              <div className="space-y-1.5">
                <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Mật khẩu</label>
                <div className="relative">
                  <span className="absolute left-3.5 top-3.5 text-slate-500"><Lock size={16} /></span>
                  <input
                    type="password"
                    required
                    placeholder="••••••••"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full bg-slate-950/70 border border-slate-800 rounded-xl pl-10 pr-4 py-3 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors"
                  />
                </div>
              </div>

              {error && (
                <div className="bg-red-950/40 border border-red-500/25 p-3.5 rounded-xl text-red-400 text-xs flex items-start gap-2">
                  <ShieldAlert className="shrink-0 mt-0.5" size={14} />
                  <p className="leading-relaxed">{error}</p>
                </div>
              )}

              <button
                type="submit"
                className="w-full py-3 bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 text-white font-bold rounded-xl transition-all shadow-lg shadow-blue-500/10 active:scale-95 text-xs flex items-center justify-center gap-1.5 cursor-pointer"
              >
                <LogIn size={14} /> Đăng nhập hệ thống
              </button>
            </form>

            {/* DEMO ACCOUNTS ASSISTANT */}
            <div className="bg-slate-950/60 p-4 rounded-2xl border border-slate-850 space-y-3">
              <p className="text-[10px] font-black uppercase text-slate-500 tracking-wider text-center">Tài khoản chạy thử nghiệm Local</p>
              <div className="grid grid-cols-2 gap-2">
                            <button
              onClick={() => autoFill('admin')}
              className="py-1.5 bg-slate-900 hover:bg-slate-800 border border-slate-800 hover:border-blue-500 rounded-lg text-[10px] font-bold text-blue-400 transition-colors cursor-pointer"
            >
              🛡️ TenantAdmin
            </button>
            <button
              onClick={() => autoFill('teamlead')}
              className="py-1.5 bg-slate-900 hover:bg-slate-800 border border-slate-800 hover:border-emerald-500 rounded-lg text-[10px] font-bold text-emerald-400 transition-colors cursor-pointer"
            >
              📊 TeamLeader ★
            </button>
            <button
              onClick={() => autoFill('vip')}
              className="py-1.5 bg-slate-900 hover:bg-slate-800 border border-slate-800 hover:border-amber-500 rounded-lg text-[10px] font-bold text-amber-400 transition-colors cursor-pointer"
            >
              💎 Learner VIP
            </button>
            <button
              onClick={() => autoFill('free')}
              className="py-1.5 bg-slate-900 hover:bg-slate-800 border border-slate-800 hover:border-slate-500 rounded-lg text-[10px] font-bold text-slate-300 transition-colors cursor-pointer"
            >
              👤 Learner Free
            </button>
              </div>
            </div>

            <div className="text-center">
              <p className="text-xs text-slate-500 font-medium">
                Chưa có tài khoản?{' '}
                <Link to="/register" className="text-blue-400 hover:text-blue-300 font-bold transition-colors">
                  Đăng ký ngay
                </Link>
              </p>
            </div>
          </div>

          {/* ======================================================== */}
          {/* 🛡️ KEYCLOAK SSO SANDBOX MODAL OVERLAY */}
          {/* ======================================================== */}
          {activeSsoModal === 'keycloak' && (
            <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
              <div className="w-full max-w-sm bg-slate-900 border border-blue-500/30 rounded-3xl p-6 shadow-2xl relative space-y-5">
                <button
                  onClick={() => setActiveSsoModal(null)}
                  className="absolute top-4 right-4 text-slate-400 hover:text-white p-1 hover:bg-slate-800 rounded-lg transition-colors cursor-pointer"
                >
                  <X size={18} />
                </button>

                <div className="text-center space-y-2">
                  <div className="w-12 h-12 rounded-full bg-blue-950/50 border border-blue-500/30 text-blue-400 flex items-center justify-center text-xl mx-auto">
                    🔑
                  </div>
                  <h2 className="text-xl font-black text-blue-400">Keycloak IdP Server</h2>
                  <p className="text-[10px] text-slate-400 leading-relaxed max-w-xs mx-auto">
                    Cổng xác thực SSO mô phỏng của Realm <span className="font-mono text-white">dehoc</span>.<br />
                    Đang lắng nghe redirect của client <span className="font-mono text-white">quiz-frontend</span>.
                  </p>
                </div>

                <form onSubmit={handleKeycloakSubmit} className="space-y-4">
                  <div className="space-y-1">
                    <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Email Đăng nhập</label>
                    <div className="relative">
                      <span className="absolute left-3.5 top-3 text-slate-500"><Mail size={14} /></span>
                      <input
                        type="email"
                        required
                        placeholder="sso.admin@dehoc.org"
                        value={keycloakUsername}
                        onChange={(e) => setKeycloakUsername(e.target.value)}
                        className="w-full bg-slate-950/70 border border-slate-800 rounded-xl pl-9 pr-4 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors"
                      />
                    </div>
                  </div>

                  <div className="space-y-1">
                    <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Mật khẩu SSO</label>
                    <div className="relative">
                      <span className="absolute left-3.5 top-3 text-slate-500"><Lock size={14} /></span>
                      <input
                        type="password"
                        placeholder="••••••••"
                        value={keycloakPassword}
                        onChange={(e) => setKeycloakPassword(e.target.value)}
                        className="w-full bg-slate-950/70 border border-slate-800 rounded-xl pl-9 pr-4 py-2.5 text-xs text-white placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors"
                      />
                    </div>
                  </div>

                  {ssoError && (
                    <div className="bg-red-950/40 border border-red-500/25 p-3.5 rounded-xl text-red-400 text-xs flex items-start gap-2">
                      <ShieldAlert className="shrink-0 mt-0.5" size={14} />
                      <p className="leading-relaxed">{ssoError}</p>
                    </div>
                  )}

                  <button
                    type="submit"
                    className="w-full py-3 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-xl text-xs cursor-pointer active:scale-95 transition-all shadow-md shadow-blue-500/20"
                  >
                    Xác thực & Trả lại Token (OIDC)
                  </button>
                </form>

                <div className="bg-slate-950/60 p-3.5 rounded-2xl border border-slate-850 text-[10px] space-y-1.5">
                  <p className="font-bold text-slate-400 text-center uppercase tracking-wider">Tùy chọn đăng nhập nhanh:</p>
                  <div className="flex gap-2">
                    <button
                      onClick={() => { setKeycloakUsername('admin@aegisquiz.com'); submitSsoLogin('admin@aegisquiz.com'); }}
                      className="flex-1 py-1.5 bg-slate-900 border border-slate-800 hover:border-blue-500 rounded-lg text-blue-400 text-[9px] font-bold cursor-pointer"
                    >
                      Admin SSO
                    </button>
                    <button
                      onClick={() => { setKeycloakUsername('student.vip@aegisquiz.com'); submitSsoLogin('student.vip@aegisquiz.com'); }}
                      className="flex-1 py-1.5 bg-slate-900 border border-slate-800 hover:border-amber-500 rounded-lg text-amber-400 text-[9px] font-bold cursor-pointer"
                    >
                      VIP SSO
                    </button>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* ======================================================== */}
          {/* 🌐 GOOGLE ACCOUNTS CHOOSER MODAL OVERLAY */}
          {/* ======================================================== */}
          {activeSsoModal === 'google' && (
            <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
              <div className="w-full max-w-sm bg-white text-slate-850 rounded-3xl p-6 shadow-2xl relative space-y-5">
                <button
                  onClick={() => setActiveSsoModal(null)}
                  className="absolute top-4 right-4 text-slate-400 hover:text-slate-900 p-1 hover:bg-slate-100 rounded-lg transition-colors cursor-pointer"
                >
                  <X size={18} />
                </button>

                <div className="text-center space-y-1.5">
                  <div className="w-12 h-12 rounded-full bg-slate-100 flex items-center justify-center text-xl mx-auto shadow-sm">
                    <Globe className="text-red-500" size={24} />
                  </div>
                  <h2 className="text-lg font-black text-slate-900 font-sans">Đăng nhập bằng Google</h2>
                  <p className="text-xs text-slate-500">Chọn tài khoản Google để tiếp tục đến AegisQuiz</p>
                </div>

                <div className="space-y-2 max-h-64 overflow-y-auto">
                  <button
                    onClick={() => submitSsoLogin('cuongvba@gmail.com')}
                    className="w-full flex items-center gap-3 p-3 hover:bg-slate-50 rounded-xl transition-all border border-slate-150 text-left cursor-pointer group"
                  >
                    <div className="w-8 h-8 rounded-full bg-gradient-to-tr from-cyan-400 to-blue-500 text-white flex items-center justify-center font-black text-xs">
                      C
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-xs font-bold text-slate-800">Cường Nguyễn (Học viên VIP)</p>
                      <p className="text-[10px] text-slate-500 font-mono truncate">cuongvba@gmail.com</p>
                    </div>
                    <span className="text-[10px] font-bold text-amber-600 bg-amber-50 px-2 py-0.5 rounded border border-amber-200">VIP</span>
                  </button>

                  <button
                    onClick={() => submitSsoLogin('admin@aegisquiz.com')}
                    className="w-full flex items-center gap-3 p-3 hover:bg-slate-50 rounded-xl transition-all border border-slate-150 text-left cursor-pointer group"
                  >
                    <div className="w-8 h-8 rounded-full bg-gradient-to-tr from-purple-500 to-indigo-600 text-white flex items-center justify-center font-black text-xs">
                      A
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-xs font-bold text-slate-800">Quản trị viên (Admin Portal)</p>
                      <p className="text-[10px] text-slate-500 font-mono truncate">admin@aegisquiz.com</p>
                    </div>
                    <span className="text-[10px] font-bold text-blue-600 bg-blue-50 px-2 py-0.5 rounded border border-blue-200">Admin</span>
                  </button>

                  <button
                    onClick={() => submitSsoLogin('student.free@aegisquiz.com')}
                    className="w-full flex items-center gap-3 p-3 hover:bg-slate-50 rounded-xl transition-all border border-slate-150 text-left cursor-pointer group"
                  >
                    <div className="w-8 h-8 rounded-full bg-slate-200 text-slate-650 flex items-center justify-center font-black text-xs">
                      S
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-xs font-bold text-slate-800">Học viên Free (Thường)</p>
                      <p className="text-[10px] text-slate-500 font-mono truncate">student.free@aegisquiz.com</p>
                    </div>
                    <span className="text-[10px] font-bold text-slate-500 bg-slate-100 px-2 py-0.5 rounded border border-slate-200">Free</span>
                  </button>
                </div>

                <div className="text-center pt-2 border-t border-slate-100">
                  <p className="text-[10px] text-slate-400">
                    SSO Sandbox - Redirects back to `/auth/callback` to simulate production OIDC protocol.
                  </p>
                </div>
              </div>
            </div>
          )}
        </div>
      );
    }
    export default LoginPage;
