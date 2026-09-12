import { useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Target, TrendingUp, Flag, BookOpen, Zap, Sparkles, Loader2 } from 'lucide-react';
import {
  Radar, RadarChart, PolarGrid, PolarAngleAxis,
  PolarRadiusAxis, ResponsiveContainer, Tooltip as RechartsTooltip,
} from 'recharts';
import { dashboardService } from '@/services/dashboard.service';

interface TopicData { subject: string; A: number; fullMark: number; }

export function Dashboard() {
  const navigate = useNavigate();

  const userRaw = localStorage.getItem('user');
  const user = userRaw ? JSON.parse(userRaw) as { id?: string; name?: string; role?: string; isPremium?: boolean } : null;
  const userId = user?.id ?? 'anonymous';
  const isPremium = user?.isPremium ?? false;



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

  const focusAreas = radarData.filter((r) => r.A <= 70).sort((a, b) => a.A - b.A);
  const planText = studyPlanData?.plan ?? studyPlanData ?? '';

  return (
    <div className="max-w-7xl mx-auto px-4 py-8 pb-24">
      {/* Header */}
      <h1 className="text-3xl font-extrabold text-slate-900 mb-2">
        Chào mừng trở lại, {user?.name ?? 'Học viên'}! 👋
      </h1>
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
        <p className="text-slate-500 font-medium">Hãy xem hôm nay chúng ta có gì nhé. Bạn đang làm rất tốt!</p>
        <div className="flex gap-3 flex-wrap">
          <button
            onClick={() => navigate('/quiz/attempt/adaptive')}
            className="flex items-center gap-2 px-5 py-2.5 bg-rose-500 hover:bg-rose-600 text-white rounded-xl font-bold shadow-lg shadow-rose-500/30 transition-transform active:scale-95 text-sm"
          >
            <Target size={18} /> Làm bài Đánh giá
          </button>
          <button
            onClick={() => navigate('/quiz/secure-attempt/exam-001')}
            className="flex items-center gap-2 px-5 py-2.5 bg-red-700 hover:bg-red-800 text-white rounded-xl font-bold shadow-lg shadow-red-700/30 transition-transform active:scale-95 text-sm"
          >
            <Target size={18} /> Thi Sát Hạch 🔒
          </button>
        </div>
      </div>

      {/* Quick Metrics */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
        <div className="bg-indigo-600 rounded-3xl p-6 text-white shadow-lg shadow-indigo-600/20">
          <div className="bg-white/20 w-12 h-12 rounded-full flex items-center justify-center mb-4"><Target size={24} /></div>
          <h3 className="text-indigo-100 font-semibold mb-1">Mục tiêu tuần</h3>
          <p className="text-3xl font-black">5/7 <span className="text-base font-normal text-indigo-200">ngày</span></p>
        </div>
        <div className="bg-white rounded-3xl p-6 border border-slate-200 shadow-sm flex flex-col justify-between">
          <div className="bg-emerald-100 text-emerald-600 w-12 h-12 rounded-full flex items-center justify-center mb-4"><TrendingUp size={24} /></div>
          <div>
            <h3 className="text-slate-500 font-semibold text-sm mb-1">Độ chính xác TB</h3>
            <p className="text-3xl font-black text-slate-800">82<span className="text-xl">%</span></p>
          </div>
        </div>
        <div className="bg-white rounded-3xl p-6 border border-slate-200 shadow-sm flex flex-col justify-between">
          <div className="bg-amber-100 text-amber-600 w-12 h-12 rounded-full flex items-center justify-center mb-4"><Flag size={24} /></div>
          <div>
            <h3 className="text-slate-500 font-semibold text-sm mb-1">Câu đã giải</h3>
            <p className="text-3xl font-black text-slate-800">1,248</p>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Radar Chart */}
        <div className="lg:col-span-2 bg-white rounded-3xl p-6 lg:p-8 border border-slate-200 shadow-sm">
          <div className="flex justify-between items-center mb-6">
            <h2 className="text-2xl font-bold text-slate-900">Bản đồ Năng Lực</h2>
            <span className="text-xs font-bold px-3 py-1.5 bg-slate-100 text-slate-600 rounded-full flex items-center gap-1">
              <Sparkles size={12} /> Phân tích bằng AI
            </span>
          </div>
          {loadingTopics ? (
            <div className="h-80 flex items-center justify-center"><Loader2 className="animate-spin text-indigo-500" size={40} /></div>
          ) : (
            <div className="h-80 w-full">
              <ResponsiveContainer width="100%" height="100%">
                <RadarChart cx="50%" cy="50%" outerRadius="80%" data={radarData}>
                  <PolarGrid stroke="#e2e8f0" />
                  <PolarAngleAxis dataKey="subject" tick={{ fill: '#64748b', fontSize: 13, fontWeight: 600 }} />
                  <PolarRadiusAxis angle={30} domain={[0, 100]} tick={false} axisLine={false} />
                  <RechartsTooltip contentStyle={{ borderRadius: '12px', border: 'none', boxShadow: '0 10px 15px -3px rgb(0 0 0/0.1)' }} />
                  <Radar name="Điểm thông thạo" dataKey="A" stroke="#4f46e5" strokeWidth={3} fill="#818cf8" fillOpacity={0.6} />
                </RadarChart>
              </ResponsiveContainer>
            </div>
          )}
        </div>

        {/* Focus Areas + AI Mentor */}
        <div className="space-y-6">
          {/* Focus areas */}
          <div className="bg-white rounded-3xl p-6 border border-slate-200 shadow-sm">
            <h2 className="text-xl font-bold text-slate-900 mb-4 flex items-center gap-2">
              <Zap size={22} className="text-rose-500" /> Khu Vực Cần Cải Thiện
            </h2>
            <div className="space-y-4">
              {focusAreas.length === 0 ? (
                <div className="text-center p-4 bg-slate-50 rounded-2xl">
                  <p className="text-slate-500 font-medium text-sm">Không có điểm yếu đáng kể 🎉</p>
                </div>
              ) : focusAreas.map((topic, idx) => (
                <div key={idx} className="flex flex-col gap-1.5">
                  <div className="flex justify-between text-sm font-bold">
                    <span className="text-slate-700">{topic.subject}</span>
                    <span className="text-rose-600">{topic.A}%</span>
                  </div>
                  <div className="w-full bg-slate-100 rounded-full h-2">
                    <div className="bg-rose-500 h-2 rounded-full transition-all" style={{ width: `${topic.A}%` }} />
                  </div>
                </div>
              ))}
            </div>
            <button
              onClick={() => navigate('/practice')}
              className="mt-6 w-full py-3.5 bg-slate-900 hover:bg-slate-800 text-white rounded-xl font-black tracking-wide shadow-lg transition-transform active:scale-95 flex justify-center items-center gap-2 text-sm"
            >
              <BookOpen size={18} /> Ôn luyện điểm yếu ngay!
            </button>
          </div>

          {/* AI Mentor (Freemium preview) */}
          <div className="bg-white rounded-3xl p-6 border border-slate-200 shadow-sm">
            <h2 className="text-lg font-bold text-orange-500 mb-3 flex items-center gap-2">
              🧠 AI Mentor (Gemini)
            </h2>
            {loadingPlan ? (
              <div className="flex items-center gap-2 text-slate-400 text-sm"><Loader2 size={16} className="animate-spin" /> Đang phân tích...</div>
            ) : (
              <p className="text-xs text-slate-600 font-mono bg-slate-50 p-3 rounded-xl border border-slate-100 whitespace-pre-line leading-relaxed line-clamp-6">
                {planText || 'Hãy làm bài thi trước để AI Mentor phân tích điểm yếu cho bạn.'}
              </p>
            )}
            <button
              onClick={() => navigate('/paths')}
              className="mt-4 w-full py-2.5 bg-white hover:bg-indigo-50 text-indigo-700 border-2 border-indigo-100 shadow-sm rounded-xl font-bold transition-colors text-sm"
            >
              Khám phá Lộ Trình →
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

const FALLBACK_RADAR: TopicData[] = [
  { subject: 'Toán học',   A: 40, fullMark: 100 },
  { subject: 'IT',         A: 90, fullMark: 100 },
  { subject: 'Anh văn',   A: 75, fullMark: 100 },
  { subject: 'Quy trình', A: 60, fullMark: 100 },
  { subject: 'Kỹ năng',  A: 85, fullMark: 100 },
];
