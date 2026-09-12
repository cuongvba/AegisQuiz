'use client';
import { useState, useEffect } from 'react';
import { 
  Radar, 
  RadarChart, 
  PolarGrid, 
  PolarAngleAxis, 
  PolarRadiusAxis, 
  ResponsiveContainer,
  Tooltip
} from 'recharts';
import { ArrowLeft, Target, TrendingUp, Flag, Sparkles } from 'lucide-react';

interface TopicSkill {
  subject: string;
  score: number;
  fullMark: number;
}

export default function StudentDashboard() {
  const [isPremium, setIsPremium] = useState(false);
  const [planText, setPlanText] = useState("Đang phân tích kết quả học tập của bạn...");
  const [loading, setLoading] = useState(false);

  const radarData: TopicSkill[] = [
    { subject: 'Toán học', score: 40, fullMark: 100 },
    { subject: 'IT (Bảo mật)', score: 90, fullMark: 100 },
    { subject: 'Anh văn', score: 75, fullMark: 100 },
    { subject: 'Quy trình Enterprise', score: 60, fullMark: 100 },
    { subject: 'Kỹ năng mềm', score: 85, fullMark: 100 }
  ];

  const fetchPlan = async (premiumVal: boolean) => {
    setLoading(true);
    try {
      const response = await fetch('http://localhost:8080/api/mentor/plan', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          userId: '9b0a1a1c-a19b-4e12-8822-42171a82ea3c',
          isPremium: premiumVal
        })
      });
      const data = await response.json();
      setPlanText(data.plan);
    } catch (e) {
      setPlanText("⚠️ Không thể kết nối đến API Gia sư AI. Vui lòng đảm bảo Backend đang chạy tại http://localhost:8080.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPlan(isPremium);
  }, [isPremium]);

  return (
    <div className="min-h-screen bg-gray-950 text-white p-8 font-sans select-none">
      <div className="max-w-7xl mx-auto space-y-8">
        
        {/* Top Header Greetings */}
        <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
          <div className="space-y-1">
            <h1 className="text-3xl font-black bg-gradient-to-r from-red-400 to-orange-500 bg-clip-text text-transparent flex items-center gap-2">
              <Sparkles className="text-orange-500 animate-pulse" size={24} /> Báo Cáo Năng Lực (Aegis Analytics)
            </h1>
            <p className="text-sm text-gray-400">Xem tiến độ học tập và bản chẩn đoán chuyên sâu từ AI Mentor</p>
          </div>

          <div className="flex gap-4">
            {/* VIP simulation toggle */}
            <div className="flex items-center gap-3 bg-gray-900 border border-gray-850 px-4 py-2 rounded-xl">
              <span className="text-xs font-semibold text-gray-400">Mô phỏng VIP:</span>
              <button 
                onClick={() => setIsPremium(!isPremium)}
                className={`px-3 py-1 rounded-lg text-xs font-bold transition-all ${isPremium ? 'bg-orange-600 text-white' : 'bg-gray-800 text-gray-400'}`}
              >
                {isPremium ? '👑 VIP ACTIVE' : 'FREE'}
              </button>
            </div>
            
            <a 
              href="/"
              className="text-xs text-gray-400 hover:text-white px-4 py-2 border border-gray-850 rounded-xl flex items-center justify-center gap-1.5"
            >
              <ArrowLeft size={12} /> Trở về
            </a>
          </div>
        </div>

        {/* Dashboard Metrics Rows */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="bg-gradient-to-tr from-purple-900/35 to-indigo-900/35 border border-purple-500/20 rounded-3xl p-6 flex flex-col justify-between h-36">
            <div className="w-10 h-10 bg-purple-500/10 text-purple-400 rounded-full flex items-center justify-center">
              <Target size={20} />
            </div>
            <div>
              <p className="text-xs text-gray-400 font-semibold mb-1">Mục tiêu học tập tuần</p>
              <p className="text-2xl font-black">5 / 7 ngày làm bài</p>
            </div>
          </div>

          <div className="bg-gray-900 border border-gray-850 rounded-3xl p-6 flex flex-col justify-between h-36">
            <div className="w-10 h-10 bg-green-500/10 text-green-400 rounded-full flex items-center justify-center">
              <TrendingUp size={20} />
            </div>
            <div>
              <p className="text-xs text-gray-400 font-semibold mb-1">Độ chính xác trung bình</p>
              <p className="text-2xl font-black text-green-400">82%</p>
            </div>
          </div>

          <div className="bg-gray-900 border border-gray-850 rounded-3xl p-6 flex flex-col justify-between h-36">
            <div className="w-10 h-10 bg-amber-500/10 text-amber-400 rounded-full flex items-center justify-center">
              <Flag size={20} />
            </div>
            <div>
              <p className="text-xs text-gray-400 font-semibold mb-1">Số câu hỏi đã hoàn tất</p>
              <p className="text-2xl font-black text-amber-400">1,248 câu</p>
            </div>
          </div>
        </div>

        {/* Main Section Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Radar Skill Chart */}
          <div className="lg:col-span-2 bg-gray-900 border border-gray-850 rounded-3xl p-8 space-y-6 flex flex-col justify-between min-h-[450px]">
            <div>
              <h2 className="text-xl font-bold text-gray-200">Biểu đồ Mạng nhện Năng lực</h2>
              <p className="text-xs text-gray-500 mt-1">Trực quan hóa độ thông thạo của bạn theo từng môn học trong hệ thống.</p>
            </div>

            <div className="h-72 w-full flex items-center justify-center">
              <ResponsiveContainer width="100%" height="100%">
                <RadarChart cx="50%" cy="50%" outerRadius="80%" data={radarData}>
                  <PolarGrid stroke="#1f2937" />
                  <PolarAngleAxis dataKey="subject" tick={{ fill: '#9ca3af', fontSize: 11, fontWeight: 700 }} />
                  <PolarRadiusAxis angle={30} domain={[0, 100]} tick={false} axisLine={false} />
                  <Tooltip 
                    contentStyle={{ backgroundColor: '#111827', borderRadius: '12px', border: '1px solid #1f2937', color: '#fff' }}
                  />
                  <Radar name="Điểm năng lực" dataKey="score" stroke="#ef4444" strokeWidth={2.5} fill="#f97316" fillOpacity={0.4} />
                </RadarChart>
              </ResponsiveContainer>
            </div>
          </div>

          {/* AI Mentor Insights */}
          <div className="bg-gray-900 border border-gray-850 rounded-3xl p-8 flex flex-col justify-between relative overflow-hidden min-h-[450px]">
            <div className="space-y-4">
              <h2 className="text-xl font-bold text-orange-400 flex items-center gap-2">
                🧠 Cố vấn AI (Gemini Mentor)
              </h2>
              
              <div className="text-gray-300 text-xs font-mono bg-gray-950 p-4 border border-gray-850 rounded-2xl leading-relaxed whitespace-pre-line overflow-y-auto max-h-[300px]">
                {loading ? "Đang nhờ AI phân tích..." : planText}
              </div>
            </div>

            {!isPremium && (
              <div className="absolute inset-0 bg-gray-950/95 flex flex-col items-center justify-center p-6 text-center backdrop-blur-sm mt-16 space-y-4">
                <span className="text-3xl">🔒 VIP ONLY</span>
                <p className="text-xs text-gray-400 max-w-xs leading-relaxed">
                  Đăng nhập VIP để xem chi tiết mảng kiến thức rỗng và lời khuyên lộ trình cải thiện 3 bước từ Mentor.
                </p>
                <button 
                  onClick={() => setIsPremium(true)}
                  className="bg-gradient-to-r from-orange-500 to-amber-600 text-white text-xs font-bold py-2.5 px-6 rounded-xl hover:scale-103 transition-transform"
                >
                  Kích hoạt AI Mentor
                </button>
              </div>
            )}
          </div>
        </div>

      </div>
    </div>
  );
}
