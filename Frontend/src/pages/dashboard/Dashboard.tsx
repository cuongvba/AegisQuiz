import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Target, TrendingUp, Flag, BookOpen, Zap, Sparkles,
  Loader2, Trophy, Users, ShieldCheck, ChevronRight,
  Flame, Clock, Brain, ArrowUpRight, Award, Compass,
  CheckCircle2, AlertCircle, Play
} from 'lucide-react';
import {
  Radar, RadarChart, PolarGrid, PolarAngleAxis,
  PolarRadiusAxis, ResponsiveContainer, Tooltip as RechartsTooltip,
} from 'recharts';
import { dashboardService } from '@/services/dashboard.service';
import { useAuthContext } from '@/app/providers/AuthProvider';

interface TopicData {
  subject: string;
  A: number;
  fullMark: number;
}

const FALLBACK_RADAR: TopicData[] = [
  { subject: 'Toán học & Logic', A: 68, fullMark: 100 },
  { subject: 'Công nghệ & IT',    A: 92, fullMark: 100 },
  { subject: 'Ngoại ngữ (Anh)',  A: 85, fullMark: 100 },
  { subject: 'Quy trình & Luật',  A: 64, fullMark: 100 },
  { subject: 'Kỹ năng Quản trị', A: 88, fullMark: 100 },
];

export function Dashboard() {
  const navigate = useNavigate();
  const { user, isAdmin } = useAuthContext();
  const userId = user?.id ?? 'anonymous';
  const isPremium = user?.isPremium ?? false;
  const isTeamLeader = user?.role === 'TeamLeader';

  const [activeTimeframe, setActiveTimeframe] = useState<'week' | 'month'>('week');

  // 1. Weak topics → RadarChart
  const { data: weakTopicsRaw, isLoading: loadingTopics } = useQuery({
    queryKey: ['weak-topics', userId],
    queryFn: () => dashboardService.getWeakTopics(userId),
    enabled: !!userId,
  });

  // 2. AI Mentor study plan
  const { data: studyPlanData, isLoading: loadingPlan } = useQuery({
    queryKey: ['study-plan', userId, isPremium],
    queryFn: () => dashboardService.getStudyPlan(userId, isPremium),
    staleTime: 5 * 60_000,
  });

  const radarData: TopicData[] = (Array.isArray(weakTopicsRaw) && weakTopicsRaw.length > 0)
    ? weakTopicsRaw
    : FALLBACK_RADAR;

  const focusAreas = [...radarData].sort((a, b) => a.A - b.A).slice(0, 3);
  const planText = studyPlanData?.plan ?? studyPlanData ?? '';

  // Determine greeting based on local time
  const currentHour = new Date().getHours();
  const greetingTime = currentHour < 12 ? 'Chào buổi sáng' : currentHour < 18 ? 'Chào buổi chiều' : 'Chào buổi tối';

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans pb-24">
      {/* ── Background Ambient Glows ────────────────────────────────────────── */}
      <div className="fixed inset-0 pointer-events-none overflow-hidden z-0">
        <div className="absolute top-[-10%] left-[15%] w-[600px] h-[600px] rounded-full bg-cyan-600/10 blur-[130px]" />
        <div className="absolute top-[20%] right-[10%] w-[500px] h-[500px] rounded-full bg-indigo-600/10 blur-[140px]" />
        <div className="absolute bottom-[10%] left-[30%] w-[700px] h-[700px] rounded-full bg-purple-600/10 blur-[160px]" />
      </div>

      <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-8 space-y-8">
        
        {/* ── HERO BANNER: World-Class Welcome Studio ────────────────────────── */}
        <div className="relative overflow-hidden rounded-3xl bg-gradient-to-r from-slate-900/90 via-indigo-950/60 to-slate-900/90 border border-slate-800/80 shadow-2xl backdrop-blur-xl p-6 sm:p-8">
          <div className="absolute -top-24 -right-24 w-96 h-96 rounded-full bg-gradient-to-br from-cyan-500/20 via-indigo-500/10 to-transparent blur-3xl pointer-events-none" />

          <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6 relative z-10">
            {/* Left Info */}
            <div className="space-y-2.5 max-w-2xl">
              <div className="flex flex-wrap items-center gap-2">
                <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-black bg-cyan-500/15 border border-cyan-500/30 text-cyan-300">
                  <Sparkles size={13} className="text-cyan-400 animate-pulse" />
                  <span>Aegis Enterprise AI Engine v4.0</span>
                </span>

                {isTeamLeader ? (
                  <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-black bg-amber-500/15 border border-amber-500/30 text-amber-300">
                    <ShieldCheck size={13} /> Trưởng Nhóm Khảo Thí
                  </span>
                ) : isAdmin ? (
                  <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-black bg-purple-500/15 border border-purple-500/30 text-purple-300">
                    <Award size={13} /> Quản Trị Hệ Thống
                  </span>
                ) : (
                  <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-black bg-emerald-500/15 border border-emerald-500/30 text-emerald-300">
                    <Flame size={13} /> Học Viên Ưu Tú
                  </span>
                )}

                <span className="inline-flex items-center gap-1 text-xs text-slate-400">
                  <Clock size={12} /> {greetingTime}
                </span>
              </div>

              <h1 className="text-2xl sm:text-4xl font-black text-white tracking-tight leading-tight">
                {greetingTime}, <span className="bg-gradient-to-r from-cyan-400 via-indigo-300 to-purple-400 bg-clip-text text-transparent">{user?.name ?? 'Chuyên Gia Khảo Thí'}</span>! 👋
              </h1>

              <p className="text-sm sm:text-base text-slate-300 font-medium leading-relaxed">
                Hôm nay là thời điểm lý tưởng để bứt phá năng lực nhận thức. Động cơ IRT đã cá nhân hóa ma trận câu hỏi mới nhất cho bạn!
              </p>
            </div>

            {/* Right Action Launchpads */}
            <div className="grid grid-cols-2 sm:grid-cols-2 gap-3 min-w-[300px]">
              <button
                onClick={() => navigate('/practice')}
                className="group relative flex flex-col items-start p-4 rounded-2xl bg-gradient-to-br from-cyan-600/30 via-slate-900/60 to-slate-900/90 border border-cyan-500/40 hover:border-cyan-400 transition-all duration-300 hover:shadow-lg hover:shadow-cyan-500/20 hover:-translate-y-0.5 cursor-pointer text-left"
              >
                <div className="w-9 h-9 rounded-xl bg-cyan-500/20 border border-cyan-500/40 flex items-center justify-center text-cyan-400 mb-2 group-hover:scale-110 transition-transform">
                  <Play size={18} />
                </div>
                <span className="text-xs font-black text-white group-hover:text-cyan-300 transition-colors flex items-center gap-1">
                  Luyện Thi AI <ArrowUpRight size={13} className="opacity-0 group-hover:opacity-100 transition-opacity" />
                </span>
                <span className="text-[10px] text-slate-400 font-medium mt-0.5">3,500+ Câu hỏi</span>
              </button>

              <button
                onClick={() => navigate('/arena')}
                className="group relative flex flex-col items-start p-4 rounded-2xl bg-gradient-to-br from-amber-600/30 via-slate-900/60 to-slate-900/90 border border-amber-500/40 hover:border-amber-400 transition-all duration-300 hover:shadow-lg hover:shadow-amber-500/20 hover:-translate-y-0.5 cursor-pointer text-left"
              >
                <div className="w-9 h-9 rounded-xl bg-amber-500/20 border border-amber-500/40 flex items-center justify-center text-amber-400 mb-2 group-hover:scale-110 transition-transform">
                  <Zap size={18} />
                </div>
                <span className="text-xs font-black text-white group-hover:text-amber-300 transition-colors flex items-center gap-1">
                  Đấu Trường <ArrowUpRight size={13} className="opacity-0 group-hover:opacity-100 transition-opacity" />
                </span>
                <span className="text-[10px] text-slate-400 font-medium mt-0.5">Chuông Vàng & PvP</span>
              </button>

              <button
                onClick={() => navigate('/team')}
                className="group relative flex flex-col items-start p-4 rounded-2xl bg-gradient-to-br from-emerald-600/30 via-slate-900/60 to-slate-900/90 border border-emerald-500/40 hover:border-emerald-400 transition-all duration-300 hover:shadow-lg hover:shadow-emerald-500/20 hover:-translate-y-0.5 cursor-pointer text-left"
              >
                <div className="w-9 h-9 rounded-xl bg-emerald-500/20 border border-emerald-500/40 flex items-center justify-center text-emerald-400 mb-2 group-hover:scale-110 transition-transform">
                  <Users size={18} />
                </div>
                <span className="text-xs font-black text-white group-hover:text-emerald-300 transition-colors flex items-center gap-1">
                  Đội Nhóm <ArrowUpRight size={13} className="opacity-0 group-hover:opacity-100 transition-opacity" />
                </span>
                <span className="text-[10px] text-slate-400 font-medium mt-0.5">Tiến độ & đề Team</span>
              </button>

              <button
                onClick={() => navigate('/quiz/attempt/adaptive')}
                className="group relative flex flex-col items-start p-4 rounded-2xl bg-gradient-to-br from-rose-600/30 via-slate-900/60 to-slate-900/90 border border-rose-500/40 hover:border-rose-400 transition-all duration-300 hover:shadow-lg hover:shadow-rose-500/20 hover:-translate-y-0.5 cursor-pointer text-left"
              >
                <div className="w-9 h-9 rounded-xl bg-rose-500/20 border border-rose-500/40 flex items-center justify-center text-rose-400 mb-2 group-hover:scale-110 transition-transform">
                  <Target size={18} />
                </div>
                <span className="text-xs font-black text-white group-hover:text-rose-300 transition-colors flex items-center gap-1">
                  Thi Đánh Giá <ArrowUpRight size={13} className="opacity-0 group-hover:opacity-100 transition-opacity" />
                </span>
                <span className="text-[10px] text-slate-400 font-medium mt-0.5">Chuẩn Năng Lực IRT</span>
              </button>
            </div>
          </div>
        </div>

        {/* ── METRIC CARDS: Executive Performance Strip ─────────────────────── */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {/* Card 1: Mục tiêu tuần */}
          <div className="rounded-2xl p-5 bg-gradient-to-br from-indigo-950/60 to-slate-900/80 border border-indigo-500/30 shadow-xl backdrop-blur-md relative overflow-hidden group hover:border-indigo-400/60 transition-colors">
            <div className="flex items-center justify-between mb-3">
              <span className="text-xs font-black text-indigo-300 tracking-wide uppercase flex items-center gap-1.5">
                <Target size={14} className="text-indigo-400" /> Mục tiêu tuần
              </span>
              <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">
                71% Đạt
              </span>
            </div>
            <div className="flex items-baseline gap-2">
              <span className="text-3xl font-black text-white">5/7</span>
              <span className="text-xs text-slate-400 font-semibold">ngày rèn luyện</span>
            </div>
            <div className="mt-3 w-full bg-slate-800 rounded-full h-2 overflow-hidden">
              <div className="bg-gradient-to-r from-indigo-500 to-cyan-400 h-full rounded-full transition-all duration-700" style={{ width: '71%' }} />
            </div>
            <p className="text-[11px] text-slate-400 mt-2 flex items-center gap-1">
              <CheckCircle2 size={12} className="text-emerald-400" /> Còn 2 ngày để hoàn thành mục tiêu
            </p>
          </div>

          {/* Card 2: Độ chính xác IRT */}
          <div className="rounded-2xl p-5 bg-gradient-to-br from-emerald-950/60 to-slate-900/80 border border-emerald-500/30 shadow-xl backdrop-blur-md relative overflow-hidden group hover:border-emerald-400/60 transition-colors">
            <div className="flex items-center justify-between mb-3">
              <span className="text-xs font-black text-emerald-300 tracking-wide uppercase flex items-center gap-1.5">
                <TrendingUp size={14} className="text-emerald-400" /> Độ chính xác TB
              </span>
              <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                +4.2% tuần này
              </span>
            </div>
            <div className="flex items-baseline gap-1">
              <span className="text-3xl font-black text-white">82.4</span>
              <span className="text-base font-black text-emerald-400">%</span>
            </div>
            <div className="mt-3 w-full bg-slate-800 rounded-full h-2 overflow-hidden">
              <div className="bg-gradient-to-r from-emerald-500 to-teal-400 h-full rounded-full transition-all duration-700" style={{ width: '82.4%' }} />
            </div>
            <p className="text-[11px] text-slate-400 mt-2 flex items-center gap-1">
              <Sparkles size={12} className="text-emerald-400" /> Top 5% học viên xuất sắc toàn viện
            </p>
          </div>

          {/* Card 3: Khối lượng câu hỏi */}
          <div className="rounded-2xl p-5 bg-gradient-to-br from-cyan-950/60 to-slate-900/80 border border-cyan-500/30 shadow-xl backdrop-blur-md relative overflow-hidden group hover:border-cyan-400/60 transition-colors">
            <div className="flex items-center justify-between mb-3">
              <span className="text-xs font-black text-cyan-300 tracking-wide uppercase flex items-center gap-1.5">
                <Flag size={14} className="text-cyan-400" /> Câu đã giải
              </span>
              <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-cyan-500/20 text-cyan-300 border border-cyan-500/30">
                48 Chuyên đề
              </span>
            </div>
            <div className="flex items-baseline gap-1.5">
              <span className="text-3xl font-black text-white">1,248</span>
              <span className="text-xs text-slate-400 font-semibold">câu hoàn tất</span>
            </div>
            <div className="mt-3 w-full bg-slate-800 rounded-full h-2 overflow-hidden">
              <div className="bg-gradient-to-r from-cyan-500 to-blue-500 h-full rounded-full transition-all duration-700" style={{ width: '65%' }} />
            </div>
            <p className="text-[11px] text-slate-400 mt-2 flex items-center gap-1">
              <Flame size={12} className="text-amber-400" /> Chuỗi trả lời đúng liên tiếp: 18 câu
            </p>
          </div>

          {/* Card 4: ELO Năng lực & Thứ hạng */}
          <div className="rounded-2xl p-5 bg-gradient-to-br from-purple-950/60 to-slate-900/80 border border-purple-500/30 shadow-xl backdrop-blur-md relative overflow-hidden group hover:border-purple-400/60 transition-colors">
            <div className="flex items-center justify-between mb-3">
              <span className="text-xs font-black text-purple-300 tracking-wide uppercase flex items-center gap-1.5">
                <Trophy size={14} className="text-amber-400" /> Bảng vàng thứ hạng
              </span>
              <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-purple-500/20 text-purple-300 border border-purple-500/30">
                Hạng #3
              </span>
            </div>
            <div className="flex items-baseline gap-2">
              <span className="text-3xl font-black text-white">1,850</span>
              <span className="text-xs text-purple-300 font-bold uppercase">Kim Cương III</span>
            </div>
            <div className="mt-3 w-full bg-slate-800 rounded-full h-2 overflow-hidden">
              <div className="bg-gradient-to-r from-purple-500 to-rose-400 h-full rounded-full transition-all duration-700" style={{ width: '85%' }} />
            </div>
            <p className="text-[11px] text-slate-400 mt-2 flex items-center gap-1">
              <ChevronRight size={12} className="text-purple-400" /> Còn 150 LP để lên Bậc Cao Thủ
            </p>
          </div>
        </div>

        {/* ── MAIN ANALYTICS GRID: Radar 360° & AI Diagnostic Studio ────────── */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          
          {/* Left 2 Cols: 360° Cognitive Radar Studio */}
          <div className="lg:col-span-2 rounded-3xl bg-slate-900/80 border border-slate-800/80 shadow-2xl p-6 sm:p-8 backdrop-blur-xl flex flex-col justify-between">
            <div>
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
                <div>
                  <h2 className="text-xl sm:text-2xl font-black text-white flex items-center gap-2.5">
                    <span>Bản Đồ Năng Lực Nhận Thức 360°</span>
                    <span className="text-xs font-black px-2.5 py-0.5 rounded-full bg-cyan-500/20 text-cyan-300 border border-cyan-500/40">
                      Chuẩn IRT CAT
                    </span>
                  </h2>
                  <p className="text-xs text-slate-400 mt-1">
                    Đánh giá tương quan giữa độ khó câu hỏi ($\theta$) và tỷ lệ phân hóa nhận thức thực tế
                  </p>
                </div>

                <div className="flex items-center gap-2">
                  <button
                    onClick={() => setActiveTimeframe('week')}
                    className={`px-3 py-1.5 rounded-xl text-xs font-black transition-all cursor-pointer ${activeTimeframe === 'week' ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40' : 'text-slate-400 hover:text-white'}`}
                  >
                    Tuần này
                  </button>
                  <button
                    onClick={() => setActiveTimeframe('month')}
                    className={`px-3 py-1.5 rounded-xl text-xs font-black transition-all cursor-pointer ${activeTimeframe === 'month' ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40' : 'text-slate-400 hover:text-white'}`}
                  >
                    Tháng này
                  </button>
                </div>
              </div>

              {/* Radar Chart Display */}
              {loadingTopics ? (
                <div className="h-80 flex flex-col items-center justify-center text-slate-500 gap-3">
                  <Loader2 className="animate-spin text-cyan-400" size={36} />
                  <span className="text-xs font-bold">Đang tổng hợp dữ liệu năng lực IRT...</span>
                </div>
              ) : (
                <div className="h-80 w-full relative">
                  <ResponsiveContainer width="100%" height="100%">
                    <RadarChart cx="50%" cy="50%" outerRadius="75%" data={radarData}>
                      <PolarGrid stroke="#334155" strokeDasharray="3 3" />
                      <PolarAngleAxis 
                        dataKey="subject" 
                        tick={{ fill: '#94a3b8', fontSize: 12, fontWeight: 700 }} 
                      />
                      <PolarRadiusAxis 
                        angle={30} 
                        domain={[0, 100]} 
                        tick={{ fill: '#475569', fontSize: 10 }}
                        axisLine={false} 
                      />
                      <RechartsTooltip 
                        contentStyle={{ 
                          backgroundColor: '#0f172a', 
                          borderColor: '#334155',
                          borderRadius: '16px',
                          color: '#f8fafc',
                          boxShadow: '0 20px 25px -5px rgb(0 0 0 / 0.5)'
                        }} 
                      />
                      <Radar 
                        name="Điểm thành thạo" 
                        dataKey="A" 
                        stroke="#06b6d4" 
                        strokeWidth={2.5} 
                        fill="#06b6d4" 
                        fillOpacity={0.45} 
                      />
                    </RadarChart>
                  </ResponsiveContainer>
                </div>
              )}
            </div>

            {/* Bottom summary bar */}
            <div className="mt-6 pt-5 border-t border-slate-800/80 flex flex-wrap items-center justify-between gap-4 text-xs">
              <div className="flex items-center gap-3">
                <span className="flex items-center gap-1.5 text-slate-400 font-medium">
                  <span className="w-2.5 h-2.5 rounded-full bg-cyan-400" /> Thế mạnh: <strong className="text-white">Công nghệ & IT (92%)</strong>
                </span>
                <span className="flex items-center gap-1.5 text-slate-400 font-medium">
                  <span className="w-2.5 h-2.5 rounded-full bg-rose-400" /> Cần chú ý: <strong className="text-white">Toán & Logic (68%)</strong>
                </span>
              </div>

              <button
                onClick={() => navigate('/practice')}
                className="text-cyan-400 hover:text-cyan-300 font-bold inline-flex items-center gap-1 hover:underline cursor-pointer"
              >
                Mở phòng luyện tập chi tiết <ChevronRight size={14} />
              </button>
            </div>
          </div>

          {/* Right 1 Col: Weak Topics + Gemini AI Cognitive Coach */}
          <div className="space-y-6">
            
            {/* ── Focus Areas: Khu vực cần cải thiện ── */}
            <div className="rounded-3xl bg-slate-900/80 border border-slate-800/80 p-6 shadow-xl backdrop-blur-xl">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-base font-black text-white flex items-center gap-2">
                  <Zap size={18} className="text-rose-400" /> Khu Vực Cần Cải Thiện
                </h3>
                <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-rose-500/20 text-rose-300 border border-rose-500/30">
                  Ưu tiên cao
                </span>
              </div>

              <div className="space-y-4">
                {focusAreas.map((topic, idx) => {
                  const percent = topic.A;
                  const isCritical = percent < 70;
                  return (
                    <div key={idx} className="p-3 rounded-2xl bg-slate-950/60 border border-slate-800/60 space-y-2">
                      <div className="flex justify-between text-xs font-bold">
                        <span className="text-slate-200">{topic.subject}</span>
                        <span className={isCritical ? 'text-rose-400' : 'text-amber-400'}>{percent}%</span>
                      </div>
                      <div className="w-full bg-slate-800 rounded-full h-2 overflow-hidden">
                        <div
                          className={`h-full rounded-full transition-all duration-700 ${
                            isCritical ? 'bg-gradient-to-r from-rose-500 to-orange-500' : 'bg-gradient-to-r from-amber-500 to-emerald-400'
                          }`}
                          style={{ width: `${percent}%` }}
                        />
                      </div>
                    </div>
                  );
                })}
              </div>

              <button
                onClick={() => navigate('/practice')}
                className="mt-5 w-full py-3 bg-gradient-to-r from-rose-600 to-orange-600 hover:brightness-110 text-white rounded-xl text-xs font-black shadow-lg shadow-rose-600/25 transition-all flex items-center justify-center gap-2 cursor-pointer"
              >
                <BookOpen size={16} />
                <span>Ôn Luyện Điểm Yếu Ngay!</span>
              </button>
            </div>

            {/* ── Gemini AI Cognitive Coach Card ── */}
            <div className="rounded-3xl bg-gradient-to-br from-indigo-950/80 via-slate-900/90 to-purple-950/70 border border-indigo-500/30 p-6 shadow-xl backdrop-blur-xl relative overflow-hidden">
              <div className="absolute top-0 right-0 w-32 h-32 bg-indigo-500/10 rounded-full blur-2xl pointer-events-none" />

              <div className="flex items-center gap-2.5 mb-3">
                <div className="w-8 h-8 rounded-xl bg-gradient-to-br from-purple-500 to-indigo-600 flex items-center justify-center text-white shadow-md shadow-indigo-500/30">
                  <Brain size={16} />
                </div>
                <div>
                  <h3 className="text-sm font-black text-white">AI Mentor • Gemini Coach</h3>
                  <p className="text-[10px] text-slate-400">Tư vấn lộ trình học tập cá nhân hóa</p>
                </div>
              </div>

              {loadingPlan ? (
                <div className="flex items-center gap-2 text-slate-400 text-xs py-4">
                  <Loader2 size={16} className="animate-spin text-indigo-400" />
                  <span>Đang tổng hợp lộ trình tối ưu...</span>
                </div>
              ) : (
                <div className="bg-slate-950/70 border border-indigo-500/20 rounded-2xl p-3.5 mt-2">
                  <p className="text-xs text-slate-300 font-medium leading-relaxed whitespace-pre-line line-clamp-5">
                    {planText ||
                      '💡 Lời khuyên hôm nay: Bạn đã làm chủ 92% mảng IT và 88% Kỹ năng quản trị. Hãy dành 15 phút ôn tập 20 câu hỏi Toán & Logic để cân bằng hồ sơ năng lực 360°.'}
                  </p>
                </div>
              )}

              <button
                onClick={() => navigate('/paths')}
                className="mt-4 w-full py-2.5 bg-indigo-600/20 hover:bg-indigo-600/30 text-indigo-300 border border-indigo-500/40 rounded-xl text-xs font-black transition-all flex items-center justify-center gap-1.5 cursor-pointer"
              >
                <Compass size={14} />
                <span>Khám phá Lộ trình Đầy đủ →</span>
              </button>
            </div>

          </div>

        </div>

      </div>
    </div>
  );
}

export default Dashboard;