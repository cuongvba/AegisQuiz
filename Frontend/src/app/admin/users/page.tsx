'use client';
import { useState } from 'react';

export default function AdminUsersPage() {
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
      <div className="flex justify-between items-center">
        <h1 className="text-3xl font-bold bg-gradient-to-r from-blue-400 to-purple-500 bg-clip-text text-transparent">
          Quản lý Người dùng
        </h1>
        <div className="text-sm text-gray-400">
          Tổng cộng: <strong className="text-white">{users.length}</strong> tài khoản
        </div>
      </div>

      <div className="bg-gray-900 border border-gray-800 rounded-2xl overflow-hidden">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-gray-800 text-gray-400 text-sm bg-gray-900/50">
              <th className="p-4 font-semibold">Tên người dùng</th>
              <th className="p-4 font-semibold">Email</th>
              <th className="p-4 font-semibold">Vai trò</th>
              <th className="p-4 font-semibold">Trạng thái VIP</th>
              <th className="p-4 font-semibold">Ngày tham gia</th>
              <th className="p-4 font-semibold text-center">Số lượt thi</th>
              <th className="p-4 font-semibold text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.id} className="border-b border-gray-800/50 hover:bg-gray-800/20 transition-all">
                <td className="p-4">
                  <div className="flex items-center gap-3">
                    <div className="w-10 h-10 rounded-full bg-gradient-to-tr from-blue-500 to-purple-600 flex items-center justify-center font-bold text-white text-sm">
                      {user.name.charAt(0)}
                    </div>
                    <div>
                      <div className="font-semibold text-gray-200">{user.name}</div>
                      <div className="text-xs text-gray-500">ID: {user.id}</div>
                    </div>
                  </div>
                </td>
                <td className="p-4 text-gray-300">{user.email}</td>
                <td className="p-4">
                  <span className={`px-2.5 py-1 rounded-full text-xs font-semibold ${user.role === 'Admin' ? 'bg-red-500/10 text-red-400 border border-red-500/20' : 'bg-blue-500/10 text-blue-400 border border-blue-500/20'}`}>
                    {user.role}
                  </span>
                </td>
                <td className="p-4">
                  <span className={`px-2.5 py-1 rounded-full text-xs font-semibold ${user.vipStatus.includes('VIP') ? 'bg-yellow-500/10 text-yellow-400 border border-yellow-500/20 animate-pulse' : 'bg-gray-800 text-gray-400'}`}>
                    {user.vipStatus}
                  </span>
                </td>
                <td className="p-4 text-gray-400 text-sm">{user.joinedDate}</td>
                <td className="p-4 text-center font-semibold text-gray-200">{user.attempts}</td>
                <td className="p-4 text-right">
                  <button 
                    onClick={() => toggleVip(user.id)}
                    className="text-xs bg-gray-800 hover:bg-gray-700 text-gray-300 px-3 py-1.5 rounded-lg border border-gray-700 hover:border-yellow-500 transition-all"
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
  );
}
