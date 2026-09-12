import { useState } from 'react';
import { ShieldCheck, UserCheck, Trash2, ShieldAlert } from 'lucide-react';

export function AdminUsersPage() {
  const [users, setUsers] = useState([
    { id: '1', name: 'Nguyễn Văn A', email: 'vana@gmail.com', role: 'Student', vipStatus: 'VIP (Stripe)', joinedDate: '2026-05-12', attempts: 45 },
    { id: '2', name: 'Trần Thị B', email: 'thib@gmail.com', role: 'Student', vipStatus: 'Free', joinedDate: '2026-06-01', attempts: 12 },
    { id: '3', name: 'Lê Văn C', email: 'vanc@gmail.com', role: 'Student', vipStatus: 'VIP (Web3)', joinedDate: '2026-06-15', attempts: 88 },
    { id: '4', name: 'Phạm Minh Đức', email: 'minhduc@gmail.com', role: 'Admin', vipStatus: 'VIP (Lifetime)', joinedDate: '2026-01-10', attempts: 0 },
    { id: '5', name: 'Hoàng Lan Anh', email: 'lananh@gmail.com', role: 'Student', vipStatus: 'Free (Trial)', joinedDate: '2026-07-01', attempts: 3 },
  ]);

  const toggleVip = (userId: string) => {
    setUsers(users.map(u => {
      if (u.id === userId) {
        return { ...u, vipStatus: u.vipStatus === 'Free' ? 'VIP (Admin)' : 'Free' };
      }
      return u;
    }));
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row justify-between sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400 tracking-tight">
            Quản lý Người dùng
          </h1>
          <p className="text-sm text-slate-400 mt-1 font-medium">Quản lý và cấp quyền thành viên cho học viên trong hệ thống.</p>
        </div>
        <div className="text-sm text-slate-400 font-bold bg-slate-900 border border-slate-800 px-4 py-2 rounded-xl backdrop-blur-md self-start sm:self-auto">
          Tổng cộng: <strong className="text-white font-black">{users.length}</strong> tài khoản
        </div>
      </div>

      <div className="bg-slate-900/60 border border-slate-800 rounded-3xl overflow-hidden backdrop-blur-xl">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse min-w-[800px]">
            <thead>
              <tr className="border-b border-slate-850 text-slate-400 text-xs font-black tracking-wider uppercase bg-slate-900/20">
                <th className="p-4">Tên người dùng</th>
                <th className="p-4">Email</th>
                <th className="p-4 w-28">Vai trò</th>
                <th className="p-4 w-36">Trạng thái VIP</th>
                <th className="p-4 w-32">Ngày tham gia</th>
                <th className="p-4 w-28 text-center">Số lượt thi</th>
                <th className="p-4 w-28 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id} className="border-b border-slate-850/60 hover:bg-slate-800/10 transition-colors">
                  <td className="p-4">
                    <div className="flex items-center gap-3">
                      <div className="w-9 h-9 rounded-full bg-gradient-to-tr from-blue-500 to-purple-600 flex items-center justify-center font-black text-white text-xs shadow-md border border-white/10 uppercase">
                        {user.name.charAt(0)}
                      </div>
                      <div>
                        <div className="font-bold text-slate-200 text-sm">{user.name}</div>
                        <div className="text-[10px] text-slate-500 font-mono">ID: {user.id}</div>
                      </div>
                    </div>
                  </td>
                  <td className="p-4 text-slate-350 text-xs font-medium font-mono">{user.email}</td>
                  <td className="p-4">
                    <span className={`px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider flex items-center gap-1 w-fit
                      ${user.role === 'Admin' 
                        ? 'bg-red-500/10 text-red-400 border border-red-500/20' 
                        : 'bg-blue-500/10 text-blue-400 border border-blue-500/20'}`}>
                      {user.role === 'Admin' ? <ShieldCheck size={10} /> : <UserCheck size={10} />}
                      {user.role}
                    </span>
                  </td>
                  <td className="p-4">
                    <span className={`px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider flex items-center gap-1 w-fit
                      ${user.vipStatus.includes('VIP') 
                        ? 'bg-amber-500/10 text-amber-400 border border-amber-500/20 animate-pulse' 
                        : 'bg-slate-800 text-slate-400'}`}>
                      {user.vipStatus}
                    </span>
                  </td>
                  <td className="p-4 text-slate-400 text-xs font-mono">{user.joinedDate}</td>
                  <td className="p-4 text-center font-bold text-slate-200 text-sm">{user.attempts}</td>
                  <td className="p-4 text-right">
                    <button 
                      onClick={() => toggleVip(user.id)}
                      className="text-[10px] bg-slate-850 hover:bg-slate-800 text-slate-300 px-3 py-1.5 rounded-lg border border-slate-700 hover:border-amber-500 transition-all font-bold cursor-pointer"
                    >
                      Set VIP
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
export default AdminUsersPage;
