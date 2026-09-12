'use client';
import { useState } from 'react';
// Mô phỏng thư viện wagmi kết nối Web3
// import { useConnect, useSendTransaction } from 'wagmi'; 

export default function PaywallPage() {
  const [loading, setLoading] = useState(false);
  const freeTokens = 0; // Giả lập đã hết 100 câu miễn phí

  const handleWeb3Payment = async () => {
    setLoading(true);
    // [Cảnh giới 5: Động cơ Kinh tế Web3 Polygon]
    console.log("Đang mở Metamask để thanh toán 2 MATIC mạng Polygon...");
    setTimeout(() => {
      alert("Thanh toán thành công! Bạn đã được mở khóa 1000 câu hỏi VIP.");
      setLoading(false);
    }, 2000);
  };

  return (
    <div className="flex flex-col items-center justify-center min-h-screen bg-gray-900 text-white p-8">
      <div className="bg-gray-800 p-10 rounded-2xl shadow-2xl max-w-md text-center border border-gray-700">
        <h1 className="text-3xl font-bold mb-4 text-red-500">Hết Lượt Miễn Phí!</h1>
        <p className="mb-6 text-gray-300">
          Bạn đã sử dụng hết 100 câu hỏi miễn phí của hệ thống AegisQuiz. 
          Để tiếp tục sử dụng Trí tuệ Nhân tạo chấm thi, vui lòng nâng cấp tài khoản.
        </p>
        
        <div className="space-y-4">
          <button className="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-3 px-6 rounded-lg transition-all">
            💳 Thanh toán bằng Thẻ (Stripe) - $5/tháng
          </button>
          
          <button 
            onClick={handleWeb3Payment}
            disabled={loading}
            className="w-full bg-purple-600 hover:bg-purple-700 text-white font-semibold py-3 px-6 rounded-lg transition-all flex items-center justify-center"
          >
            {loading ? "Đang kết nối Ví..." : "🦊 Mở khóa bằng Polygon Web3 - 2 MATIC"}
          </button>
        </div>
      </div>
    </div>
  );
}
