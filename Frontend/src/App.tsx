import { Suspense, lazy } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { MainLayout } from '@/layouts/MainLayout';
import { FullScreenSpinner } from '@/components/ui/FullScreenSpinner';
import { ProtectedRoute } from '@/components/auth/ProtectedRoute';
import { AppProviders } from '@/app/providers';
import { ErrorBoundary } from '@/shared/ui/ErrorBoundary';

const Dashboard = lazy(() => import('@/pages/dashboard/Dashboard').then(m => ({ default: m.Dashboard })));
const PracticePage = lazy(() => import('@/pages/practice/PracticePage').then(m => ({ default: m.PracticePage })));
const AttemptRoom = lazy(() => import('@/pages/quiz/AttemptRoom').then(m => ({ default: m.AttemptRoom })));
const AdaptiveExamRoom = lazy(() => import('@/pages/quiz/AdaptiveExamRoom'));
const SecureExamRoom = lazy(() => import('@/pages/quiz/SecureExamRoom'));
const AttemptReview = lazy(() => import('@/pages/quiz/AttemptReview').then(m => ({ default: m.AttemptReview })));
const LeaderboardPage = lazy(() => import('@/pages/gamification/LeaderboardPage').then(m => ({ default: m.LeaderboardPage })));
const AchievementsPage = lazy(() => import('@/pages/gamification/AchievementsPage').then(m => ({ default: m.AchievementsPage })));
const PathExplorer = lazy(() => import('@/pages/paths/PathExplorer').then(m => ({ default: m.PathExplorer })));
const PaywallPage = lazy(() => import('@/pages/paywall/PaywallPage').then(m => ({ default: m.PaywallPage })));
const ArenaHubPage = lazy(() => import('@/pages/arena/ArenaHubPage').then(m => ({ default: m.ArenaHubPage })));
const ArenaRoomPage = lazy(() => import('@/pages/arena/ArenaRoomPage').then(m => ({ default: m.ArenaRoomPage })));
const ArenaStudioPage = lazy(() => import('@/pages/arena/ArenaStudioPage').then(m => ({ default: m.ArenaStudioPage })));
const TeamDashboard = lazy(() => import('@/pages/team-leader/TeamDashboard').then(m => ({ default: m.TeamDashboard })));

// Admin Lazy Pages
const AdminLayout = lazy(() => import('@/pages/admin/AdminLayout').then(m => ({ default: m.AdminLayout })));
const AdminDashboard = lazy(() => import('@/pages/admin/AdminDashboard').then(m => ({ default: m.AdminDashboard })));
const AdminQuestionsPage = lazy(() => import('@/pages/admin/questions').then(m => ({ default: m.AdminQuestionsPage })));
const AdminNotebooksPage = lazy(() => import('@/pages/admin/AdminNotebooksPage').then(m => ({ default: m.AdminNotebooksPage })));
const AdminUsersPage = lazy(() => import('@/pages/admin/AdminUsersPage').then(m => ({ default: m.AdminUsersPage })));
const AdminPaymentsPage = lazy(() => import('@/pages/admin/AdminPaymentsPage').then(m => ({ default: m.AdminPaymentsPage })));
const AdminExamsPage = lazy(() => import('@/pages/admin/AdminExamsPage').then(m => ({ default: m.AdminExamsPage })));

// Auth Lazy Pages
const LoginPage = lazy(() => import('@/pages/auth/LoginPage').then(m => ({ default: m.LoginPage })));
const RegisterPage = lazy(() => import('@/pages/auth/RegisterPage').then(m => ({ default: m.RegisterPage })));
const ResetPasswordPage = lazy(() => import('@/pages/auth/ResetPasswordPage').then(m => ({ default: m.ResetPasswordPage })));
const CallbackPage = lazy(() => import('@/pages/auth/CallbackPage').then(m => ({ default: m.CallbackPage })));
const QrConfirmPage = lazy(() => import('@/pages/auth/QrConfirmPage').then(m => ({ default: m.QrConfirmPage })));
const ProfilePage = lazy(() => import('@/pages/profile/ProfilePage').then(m => ({ default: m.ProfilePage })));

export default function App() {
  return (
    <ErrorBoundary>
      <AppProviders>
        <BrowserRouter>
          <Suspense fallback={<FullScreenSpinner />}>
            <Routes>
              {/* Layout for browsing structure context */}
              <Route path="/" element={<MainLayout />}>
                <Route index element={<Dashboard />} />
                <Route path="leaderboard" element={<LeaderboardPage />} />
                <Route path="achievements" element={<AchievementsPage />} />
                <Route path="paths" element={<PathExplorer />} />
                <Route path="arena" element={<ArenaHubPage />} />
                <Route path="team" element={<TeamDashboard />} />
              </Route>

              {/* [P1 Security] Admin Nested Routes — chỉ Admin mới vào được */}
              <Route
                path="/admin"
                element={
                  <ProtectedRoute adminOnly>
                    <AdminLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<AdminDashboard />} />
                <Route path="questions" element={<AdminQuestionsPage />} />
                <Route path="notebooks" element={<AdminNotebooksPage />} />
                <Route path="users" element={<AdminUsersPage />} />
                <Route path="payments" element={<AdminPaymentsPage />} />
                <Route path="exams" element={<AdminExamsPage />} />
              </Route>

              {/* Auth Routes */}
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/reset-password" element={<ResetPasswordPage />} />
              <Route path="/auth/callback" element={<CallbackPage />} />
              <Route path="/auth/qr-confirm" element={<QrConfirmPage />} />
              <Route
                path="/profile"
                element={
                  <ProtectedRoute>
                    <ProfilePage />
                  </ProtectedRoute>
                }
              />

              {/* Practice/Exam is full screen so it stays separate from MainLayout */}
              <Route path="/practice" element={<PracticePage />} />
              <Route path="/paywall" element={<PaywallPage />} />
              <Route path="/arena/studio" element={<ArenaStudioPage />} />
              <Route path="/arena/:gameCode/:roomId" element={<ArenaRoomPage />} />
              <Route path="/quiz/attempt/adaptive" element={<AdaptiveExamRoom />} />
              <Route path="/quiz/attempt/:id" element={<AttemptRoom />} />
              <Route path="/quiz/secure-attempt/:id" element={<SecureExamRoom />} />
              <Route path="/quiz/:quizId/review/:attemptId" element={<AttemptReview />} />
            </Routes>
          </Suspense>
        </BrowserRouter>
      </AppProviders>
    </ErrorBoundary>
  );
}

