import Link from 'next/link';

export default function Home() {
  return (
    <div className="min-h-screen bg-gray-950 flex flex-col items-center justify-center p-8 text-white">
      <div className="max-w-3xl text-center space-y-8">
        <h1 className="text-6xl font-extrabold text-transparent bg-clip-text bg-gradient-to-r from-cyan-400 to-blue-600">
          AegisQuiz
        </h1>
        <p className="text-xl text-gray-400">
          Nền tảng Đào tạo Enterprise tích hợp AI Gia sư và Web3 Paywall
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-12">
          {/* Admin Section */}
          <Link href="/admin" className="p-6 bg-gray-900 border border-gray-800 rounded-xl hover:border-cyan-500 transition-all hover:scale-105 group text-left">
            <h2 className="text-2xl font-bold text-cyan-400 mb-2 group-hover:text-cyan-300">Quản trị viên (Admin) &rarr;</h2>
            <p className="text-gray-500">Quản lý Ngân hàng câu hỏi, tải lên Excel và xử lý AI Giáo trình (NotebookLM).</p>
          </Link>

          {/* Student Dashboard */}
          <Link href="/dashboard" className="p-6 bg-gray-900 border border-gray-800 rounded-xl hover:border-purple-500 transition-all hover:scale-105 group text-left">
            <h2 className="text-2xl font-bold text-purple-400 mb-2 group-hover:text-purple-300">Góc Học tập (Student) &rarr;</h2>
            <p className="text-gray-500">Xem tiến độ học tập, điểm số và nhận phân tích chuyên sâu từ AI Gia sư.</p>
          </Link>

          {/* Paywall */}
          <Link href="/paywall" className="p-6 bg-gray-900 border border-gray-800 rounded-xl hover:border-green-500 transition-all hover:scale-105 group text-left">
            <h2 className="text-2xl font-bold text-green-400 mb-2 group-hover:text-green-300">Nâng cấp VIP &rarr;</h2>
            <p className="text-gray-500">Trải nghiệm thanh toán tự động qua VietQR và Web3 Smart Contract.</p>
          </Link>

          {/* Quiz Engine */}
          <div className="p-6 bg-gray-900 border border-gray-800 rounded-xl text-left flex flex-col justify-between">
            <div>
              <h2 className="text-2xl font-bold text-orange-400 mb-2">Phòng Thi (Quiz)</h2>
              <p className="text-gray-500 mb-4">Theme Engine, Luyện tập Thích ứng & Sát hạch:</p>
            </div>
            <div className="flex flex-wrap gap-2.5">
              <Link href="/quiz/kid" className="px-3 py-1.5 bg-pink-600 hover:bg-pink-500 text-white rounded-lg font-bold text-xs">👦 Kid Mode</Link>
              <Link href="/quiz/paper" className="px-3 py-1.5 bg-gray-100 hover:bg-white text-black border border-gray-300 rounded-lg font-serif text-xs">📄 Paper Mode</Link>
              <Link href="/quiz/custom" className="px-3 py-1.5 bg-purple-600 hover:bg-purple-500 text-white rounded-lg font-bold text-xs">⚙️ Custom Mode</Link>
              <Link href="/quiz/secure" className="px-3 py-1.5 bg-red-650 hover:bg-red-500 text-white rounded-lg font-bold text-xs">🔒 Secure Mode</Link>
            </div>
          </div>

          {/* Gamification Engine */}
          <div className="p-6 bg-gray-900 border border-gray-800 rounded-xl text-left flex flex-col justify-between">
            <div>
              <h2 className="text-2xl font-bold text-yellow-500 mb-2">Game Hóa & Thi Đua</h2>
              <p className="text-gray-500 mb-4">Khích lệ động lực và vinh danh cột mốc học tập:</p>
            </div>
            <div className="flex flex-wrap gap-2.5">
              <Link href="/leaderboard" className="px-3 py-1.5 bg-yellow-600 hover:bg-yellow-500 text-white rounded-lg font-bold text-xs">🏆 Bảng Xếp Hạng</Link>
              <Link href="/achievements" className="px-3 py-1.5 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg font-bold text-xs">🎖️ Trophy Room</Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
