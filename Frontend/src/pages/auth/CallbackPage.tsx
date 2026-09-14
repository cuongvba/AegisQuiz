import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Loader2, ShieldCheck, ShieldAlert } from 'lucide-react';
import { normalizeRole, type UserProfile } from '@/types/auth';

export function CallbackPage() {
  const navigate = useNavigate();
  const [status, setStatus] = useState<'processing' | 'error'>('processing');
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    const handleAuthCallback = async () => {
      try {
        const url = new URL(window.location.href);
        
        // 1. Kiểm tra Token từ Query Params hoặc Hash fragment (OIDC Implicit flow gửi qua Hash)
        let token = url.searchParams.get('access_token') || url.searchParams.get('auth_token');
        let userRaw = url.searchParams.get('auth_user') || url.searchParams.get('user');
        
        // Bóc tách từ hash nếu không có trong query params (#access_token=xxx&id_token=yyy)
        if (!token && url.hash) {
          const hashParams = new URLSearchParams(url.hash.substring(1));
          token = hashParams.get('access_token') || hashParams.get('id_token') || hashParams.get('auth_token');
          userRaw = hashParams.get('auth_user') || hashParams.get('user');
        }

        if (!token) {
          // [CG6 FIX] Không sinh mock token — redirect đăng nhập thật
          // Trước đây: token = 'sso_mock_token_' + Math.random()... ← Lỗ hổng bảo mật!
          // PKCE Authorization Code Flow: phải có code từ IdP — không tự sinh
          const code = url.searchParams.get('code');
          if (code) {
            // Exchange authorization code → real token (PKCE flow)
            const codeVerifier = sessionStorage.getItem('pkce_code_verifier');
            if (!codeVerifier) throw new Error('[CG6] PKCE code_verifier không tìm thấy.');

            const tokenResponse = await fetch('/api/auth/exchange', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ code, codeVerifier }),
            });
            if (!tokenResponse.ok) throw new Error('Exchange token thất bại.');
            const tokenData = await tokenResponse.json();
            token = tokenData.accessToken;
            userRaw = userRaw ?? (tokenData.user ? JSON.stringify(tokenData.user) : null);
            sessionStorage.removeItem('pkce_code_verifier'); // Dọn dẹp
          } else {
            throw new Error('[CG6] Không nhận được token hoặc authorization code từ IdP.');
          }
        }

        if (!token) {
          throw new Error('[CG6] Token không hợp lệ.');
        }

        const validToken: string = token;

        // 2. Phân tích thông tin người dùng từ token (Stateless Jwt Decoding)
        // [RBAC FIX] Dùng normalizeRole() thay vì hardcode 'admin'/'student'
        let userProfile: UserProfile = {
          id:        'sso_user_id_123',
          name:      'SSO User',
          email:     'sso.user@aegisquiz.com',
          role:      normalizeRole('GuestViewer'),
          isPremium: false
        };

        if (userRaw) {
          try {
            userProfile = JSON.parse(decodeURIComponent(userRaw));
          } catch {}
        } else {
          // Bóc tách claims từ JWT payload nếu có cấu trúc phân mảnh (header.payload.signature)
          const parts = validToken.split('.');
          if (parts.length === 3) {
            try {
              const base64Url = parts[1].replace(/-/g, '+').replace(/_/g, '/');
              const utf8PayloadString = decodeURIComponent(escape(atob(base64Url)));
              const payload = JSON.parse(utf8PayloadString);
              userProfile = {
                id:        payload.sub || payload.id || 'sso_user_id_123',
                name:      payload.name || payload.preferred_username || 'SSO User',
                email:     payload.email || 'sso.user@aegisquiz.com',
                // [RBAC FIX] normalizeRole map Keycloak roles → AppRole constants
                // Keycloak realm roles: ['TenantAdmin', 'TeamLeader', ...] hoặc legacy ['admin']
                role: normalizeRole(
                  payload.roles?.find((r: string) => r in {
                    SystemAdmin: 1, TenantAdmin: 1, ContentManager: 1,
                    Instructor: 1, OrgUnitManager: 1, TeamLeader: 1,
                    Learner: 1, GuestViewer: 1, admin: 1, student: 1
                  }) ??
                  payload.resource_access?.account?.roles?.[0] ??
                  payload.role
                ),
                isPremium:  payload.isPremium || payload.roles?.includes('vip') || false,
                tenantId:   payload.tenant_id,
                orgUnitId:  payload.org_unit_id,
              };
            } catch {}
          }
        }

        // 3. Lưu phiên đăng nhập an toàn vào localStorage
        localStorage.setItem('token', validToken);
        localStorage.setItem('user', JSON.stringify(userProfile));

        // Tự động chuyển hướng về trang chủ daotao.dehoc.vn
        setTimeout(() => {
          if (typeof window !== 'undefined' && window.location.hostname.endsWith('dehoc.vn') && window.location.hostname !== 'daotao.dehoc.vn') {
            window.location.href = 'https://daotao.dehoc.vn/';
          } else {
            navigate('/');
            window.location.reload();
          }
        }, 1000);

      } catch (err: any) {
        setStatus('error');
        setErrorMsg(err.message || 'Lỗi bóc tách token xác thực SSO.');
      }
    };

    handleAuthCallback();
  }, [navigate]);

  return (
    <div className="min-h-screen bg-slate-950 text-white flex items-center justify-center p-6 font-sans">
      <div className="w-full max-w-sm bg-slate-900/60 border border-slate-800 rounded-3xl p-8 text-center space-y-6 backdrop-blur-xl">
        {status === 'processing' ? (
          <>
            <Loader2 className="animate-spin text-blue-500 mx-auto" size={44} />
            <div className="space-y-2">
              <h2 className="text-xl font-bold text-slate-200">Đang đồng bộ SSO...</h2>
              <p className="text-xs text-slate-400">Đang xác thực thông tin và bóc tách chữ ký số IdP.</p>
            </div>
          </>
        ) : (
          <>
            <div className="w-14 h-14 bg-red-950/40 border border-red-500/20 text-red-500 rounded-full flex items-center justify-center text-2xl mx-auto">
              <ShieldAlert size={28} />
            </div>
            <div className="space-y-2">
              <h2 className="text-xl font-bold text-red-400">Xác thực Thất bại</h2>
              <p className="text-xs text-slate-400">{errorMsg}</p>
            </div>
            <button 
              onClick={() => navigate('/login')}
              className="w-full py-2.5 bg-slate-800 hover:bg-slate-700 text-white font-bold rounded-xl transition-all text-xs cursor-pointer"
            >
              Quay lại Đăng nhập
            </button>
          </>
        )}
      </div>
    </div>
  );
}
export default CallbackPage;
