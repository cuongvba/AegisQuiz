import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { ShieldAlert, Sparkles, User, Lock, Mail, UserPlus } from 'lucide-react';

export function RegisterPage() {
  const navigate = useNavigate();
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handleRegister = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (password !== confirmPassword) {
      setError('Mật khẩu xác nhận không trùng khớp.');
      return;
    }

    // Mô phỏng tạo tài khoản thành công liên kết SSO
    setSuccess(true);
    setTimeout(() => {
      // Đăng nhập luôn cho người dùng sau khi đăng ký thành công
      const mockUser = {
        id: 'user_' + Math.random().toString(36).substring(2, 9),
        name: name,
        email: email,
        role: 'student',
        isPremium: false
      };
      
      const mockJwtToken = 'mock_jwt_token_' + btoa(JSON.stringify(mockUser));
      localStorage.setItem('token', mockJwtToken);
      localStorage.setItem('user', JSON.stringify(mockUser));
      
      navigate('/');
      window.location.reload(); // Làm mới MainLayout state
    }, 1500);
  };

  return (
    <div className="min-h-screen bg-slate-950 text-white flex flex-col items-center justify-center p-6 relative overflow-hidden font-sans">
      {/* Background Glow */}
      <div className="absolute top-1/4 left-1/3 w-96 h-96 bg-purple-650/20 rounded-full blur-[120px] pointer-events-none" />
      <div className="absolute bottom-1/4 right-1/3 w-96 h-96 bg-blue-650/20 rounded-full blur-[120px] pointer-events-none" />

      <div className="w-full max-w-md bg-slate-900/70 border border-slate-800 rounded-3xl p-8 shadow-2xl backdrop-blur-xl relative z-10 space-y-6">
        
        <div className="text-center space-y-2">
          <div className="inline-flex items-center gap-1.5 px-3 py-1 bg-purple-950/50 border border-purple-500/30 text-purple-300 rounded-full text-xs font-black tracking-wide uppercase">
            <Sparkles size={12} /> Đăng ký Thành viên
          </div>
          <h1 className="text-3xl font-black tracking-tight text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400">
            TẠO TÀI KHOẢN
          </h1>
          <p className="text-xs text-slate-400">Tạo tài khoản học viên để bắt đầu rèn luyện cùng AI.</p>
        </div>

        {success ? (
          <div className="text-center py-6 space-y-4 animate-in fade-in zoom-in duration-300">
            <div className="w-14 h-14 bg-emerald-950/50 border border-emerald-500/30 text-emerald-400 rounded-full flex items-center justify-center text-2xl mx-auto">✓</div>
            <h3 className="text-lg font-bold text-emerald-400">ĐĂNG KÝ THÀNH CÔNG!</h3>
            <p className="text-xs text-gray-300">Hệ thống đang tự động đăng nhập và dẫn bạn về Trang chủ...</p>
          </div>
        ) : (
          <form onSubmit={handleRegister} className="space-y-4">
            <div className="space-y-1.5">
              <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Họ và tên</label>
              <div className="relative">
                <span className="absolute left-3.5 top-3.5 text-slate-500"><User size={16} /></span>
                <input 
                  type="text" 
                  required
                  placeholder="Nguyễn Văn A" 
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="w-full bg-slate-950/70 border border-slate-800 rounded-xl pl-10 pr-4 py-3 text-sm text-white placeholder-slate-650 focus:outline-none focus:border-blue-500 transition-colors"
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Email học viên</label>
              <div className="relative">
                <span className="absolute left-3.5 top-3.5 text-slate-500"><Mail size={16} /></span>
                <input 
                  type="email" 
                  required
                  placeholder="student@example.com" 
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full bg-slate-950/70 border border-slate-800 rounded-xl pl-10 pr-4 py-3 text-sm text-white placeholder-slate-655 focus:outline-none focus:border-blue-500 transition-colors"
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

            <div className="space-y-1.5">
              <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Xác nhận Mật khẩu</label>
              <div className="relative">
                <span className="absolute left-3.5 top-3.5 text-slate-500"><Lock size={16} /></span>
                <input 
                  type="password" 
                  required
                  placeholder="••••••••" 
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
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
              className="w-full py-3.5 bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 text-white font-bold rounded-xl transition-all shadow-lg shadow-blue-500/10 active:scale-95 text-xs flex items-center justify-center gap-1.5 cursor-pointer"
            >
              <UserPlus size={14} /> Đăng ký tài khoản
            </button>
          </form>
        )}

        <div className="text-center">
          <p className="text-xs text-slate-500 font-medium">
            Đã có tài khoản?{' '}
            <Link to="/login" className="text-blue-400 hover:text-blue-300 font-bold transition-colors">
              Đăng nhập
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}
export default RegisterPage;
