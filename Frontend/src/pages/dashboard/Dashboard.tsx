import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { dashboardService } from '@/services/dashboard.service';
import { useAuthContext } from '@/app/providers/AuthProvider';
import { DashboardHero } from './components/DashboardHero';
import { DashboardKpiStrip } from './components/DashboardKpiStrip';
import { CognitiveRadarPanel, type TopicData } from './components/CognitiveRadarPanel';
import { FocusAreasList } from './components/FocusAreasList';
import { AiMentorCoachCard } from './components/AiMentorCoachCard';

export function Dashboard() {
  const { t } = useTranslation('dashboard');
  const { user, isAdmin } = useAuthContext();
  const userId = user?.id ?? 'anonymous';
  const isPremium = user?.isPremium ?? false;
  const isTeamLeader = user?.role === 'TeamLeader';

  const FALLBACK_RADAR: TopicData[] = [
    { subject: t('radar.topics.math'),       A: 68, fullMark: 100 },
    { subject: t('radar.topics.tech'),       A: 92, fullMark: 100 },
    { subject: t('radar.topics.language'),   A: 85, fullMark: 100 },
    { subject: t('radar.topics.process'),    A: 64, fullMark: 100 },
    { subject: t('radar.topics.management'), A: 88, fullMark: 100 },
  ];

  // 1. Weak topics & Cognitive Map (IRT)
  const { data: weakTopicsRaw, isLoading: loadingTopics } = useQuery({
    queryKey: ['weak-topics', userId],
    queryFn: () => dashboardService.getWeakTopics(userId),
    enabled: !!userId,
  });

  // 2. AI Mentor personalized study plan
  const { data: studyPlanData, isLoading: loadingPlan } = useQuery({
    queryKey: ['study-plan', userId, isPremium],
    queryFn: () => dashboardService.getStudyPlan(userId, isPremium),
    staleTime: 5 * 60_000,
  });

  const radarData: TopicData[] = (Array.isArray(weakTopicsRaw) && weakTopicsRaw.length > 0)
    ? weakTopicsRaw
    : FALLBACK_RADAR;

  // Sắp xếp các chuyên đề có % hoàn thành thấp nhất lên đầu
  const focusAreas = [...radarData].sort((a, b) => a.A - b.A).slice(0, 3);
  const planText = studyPlanData?.plan ?? studyPlanData ?? '';

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans pb-24">
      {/* ── Background Ambient Glows ────────────────────────────────────────── */}
      <div className="fixed inset-0 pointer-events-none overflow-hidden z-0">
        <div className="absolute top-[-10%] left-[15%] w-[600px] h-[600px] rounded-full bg-cyan-600/10 blur-[130px]" />
        <div className="absolute top-[20%] right-[10%] w-[500px] h-[500px] rounded-full bg-indigo-600/10 blur-[140px]" />
        <div className="absolute bottom-[10%] left-[30%] w-[700px] h-[700px] rounded-full bg-purple-600/10 blur-[160px]" />
      </div>

      <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-6 space-y-6">
        {/* ── 1. HERO BANNER: 70/30 Focus with 1-Click AI Daily Mission ── */}
        <DashboardHero
          user={user}
          isAdmin={isAdmin}
          isTeamLeader={isTeamLeader}
        />

        {/* ── 2. PERFORMANCE STRIP: 4 Gamified KPIs with Streak & Milestones ── */}
        <DashboardKpiStrip />

        {/* ── 3. ANALYTICS & REMEDIATION GRID: 360° Radar + Weak Focus + AI Coach ── */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Cột 1 & 2: Bản đồ Năng lực Nhận thức 360° IRT tương tác */}
          <div className="lg:col-span-2">
            <CognitiveRadarPanel
              data={radarData}
              isLoading={loadingTopics}
            />
          </div>

          {/* Cột 3: Top Chuyên đề Lỗ hổng & Cố vấn AI */}
          <div className="space-y-6">
            <FocusAreasList
              topics={focusAreas}
            />
            <AiMentorCoachCard
              planText={planText}
              isLoading={loadingPlan}
            />
          </div>
        </div>
      </div>
    </div>
  );
}

export default Dashboard;