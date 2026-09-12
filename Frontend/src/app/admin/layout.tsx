'use client';
import Link from 'next/link';
import { usePathname } from 'next/navigation';

const menuItems = [
  { href: '/admin', label: '📊 Tổng quan', icon: '📊' },
  { href: '/admin/questions', label: '❓ Ngân hàng Câu hỏi', icon: '❓' },
  { href: '/admin/notebooks', label: '📚 Giáo trình (PDF)', icon: '📚' },
  { href: '/admin/users', label: '👥 Người dùng', icon: '👥' },
  { href: '/admin/payments', label: '💰 Doanh thu', icon: '💰' },
  { href: '/admin/exams', label: '📝 Đề thi', icon: '📝' },
];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();

  return (
    <div className="flex min-h-screen bg-gray-950 text-white">
      {/* SIDEBAR */}
      <aside className="w-64 bg-gray-900 border-r border-gray-800 p-6 flex flex-col">
        <div className="mb-8">
          <h1 className="text-2xl font-bold bg-gradient-to-r from-blue-400 to-purple-500 bg-clip-text text-transparent">
            AegisQuiz
          </h1>
          <p className="text-xs text-gray-500 mt-1">Admin Portal</p>
        </div>

        <nav className="flex-1 space-y-1">
          {menuItems.map((item) => {
            const isActive = pathname === item.href;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={`flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-all
                  ${isActive 
                    ? 'bg-blue-600/20 text-blue-400 border border-blue-600/30' 
                    : 'text-gray-400 hover:bg-gray-800 hover:text-white'
                  }`}
              >
                <span>{item.icon}</span>
                {item.label.replace(/^[^\s]+\s/, '')}
              </Link>
            );
          })}
        </nav>

        <div className="border-t border-gray-800 pt-4 mt-4">
          <Link href="/" className="text-xs text-gray-500 hover:text-gray-300 transition-colors">
            ← Quay lại Trang chủ
          </Link>
        </div>
      </aside>

      {/* MAIN CONTENT */}
      <main className="flex-1 p-8 overflow-y-auto">
        {children}
      </main>
    </div>
  );
}
