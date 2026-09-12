import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Users, BarChart3, BookOpen, ChevronRight,
  TrendingUp, Clock, CheckCircle2, Trophy,
  Send, ArrowLeft, RefreshCw, AlertCircle,
  ShieldCheck, Target, FileQuestion, Play, Copy, Share2
} from 'lucide-react';
import { useAuth } from '@/hooks/useAuth';
import api from '@/services/api';

// ── Types ──────────────────────────────────────────────────────────────────────
interface TeamMember {
  userId:      string;
  name?:       string;
  assignedAt:  string;
}

interface MemberProgress {
  userId:         string;
  totalAttempts:  number;
  avgScore:       number;
  lastAttemptDate: string | null;
  correctAnswers:  number;
  totalQuestions:  number;
}

interface TeamInfo {
  orgUnit: { id: string; name: string; code: string; unitType: number };
  memberCount: number;
  members: TeamMember[];
}

// ── Mock data (dev sandbox — sẽ thay bằng API thật) ───────────────────────────
const MOCK_TEAM: TeamInfo = {
  orgUnit:     { id: 'team-demo-guid', name: 'Nhóm Tín dụng - Chi nhánh Hoàn Kiếm', code: 'CN_HK_TINDUNG', unitType: 5 },
  memberCount: 6,
  members: [
    { userId: 'learner-001', name: 'Nguyễn Văn An',    assignedAt: '2026-08-01' },
    { userId: 'learner-002', name: 'Trần Thị Bình',    assignedAt: '2026-08-01' },
    { userId: 'learner-003', name: 'Lê Minh Cường',    assignedAt: '2026-08-15' },
    { userId: 'learner-004', name: 'Phạm Thu Dung',    assignedAt: '2026-09-01' },
    { userId: 'learner-005', name: 'Hoàng Văn Em',     assignedAt: '2026-09-01' },
    { userId: 'learner-006', name: 'Vũ Thị Phương',    assignedAt: '2026-09-05' },
  ]
};

const MOCK_PROGRESS: Record<string, MemberProgress> = {
  'learner-001': { userId: 'learner-001', totalAttempts: 24, avgScore: 87.5, lastAttemptDate: '2026-09-09', correctAnswers: 210, totalQuestions: 240 },
  'learner-002': { userId: 'learner-002', totalAttempts: 18, avgScore: 92.1, lastAttemptDate: '2026-09-10', correctAnswers: 165, totalQuestions: 180 },
  'learner-003': { userId: 'learner-003', totalAttempts: 31, avgScore: 79.3, lastAttemptDate: '2026-09-08', correctAnswers: 246, totalQuestions: 310 },
  'learner-004': { userId: 'learner-004', totalAttempts: 12, avgScore: 68.0, lastAttemptDate: '2026-09-07', correctAnswers: 81,  totalQuestions: 120 },
  'learner-005': { userId: 'learner-005', totalAttempts: 7,  avgScore: 55.4, lastAttemptDate: '2026-09-05', correctAnswers: 38,  totalQuestions: 70 },
  'learner-006': { userId: 'learner-006', totalAttempts: 3,  avgScore: 43.2, lastAttemptDate: '2026-09-06', correctAnswers: 13,  totalQuestions: 30 },
};

// ── Helper: Score color ────────────────────────────────────────────────────────
function scoreColor(score: number): string {
  if (score >= 85) return 'text-emerald-400';
  if (score >= 70) return 'text-amber-400';
  if (score >= 50) return 'text-orange-400';
  return 'text-red-400';
}

function scoreBg(score: number): string {
  if (score >= 85) return 'bg-emerald-500/10 border-emerald-500/20';
  if (score >= 70) return 'bg-amber-500/10 border-amber-500/20';
  if (score >= 50) return 'bg-orange-500/10 border-orange-500/20';
  return 'bg-red-500/10 border-red-500/20';
}

// ── Component ──────────────────────────────────────────────────────────────────
export function TeamDashboard() {
  const navigate = useNavigate();
  const { user, isTeamLeader, isTenantAdmin, isSystemAdmin } = useAuth();
  const [teamInfo,  setTeamInfo]  = useState<TeamInfo | null>(null);
  const [progress,  setProgress]  = useState<Record<string, MemberProgress>>({});
  const [loading,   setLoading]   = useState(true);
  const [error,     setError]     = useState<string | null>(null);
  const [assigning, setAssigning] = useState(false);
  const [assignMsg, setAssignMsg] = useState<string | null>(null);

  const canManage = isTeamLeader || isTenantAdmin || isSystemAdmin;

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        // Dev sandbox: dùng mock data, production sẽ gọi API thật
        if (import.meta.env.DEV) {
          await new Promise(r => setTimeout(r, 600));
          setTeamInfo(MOCK_TEAM);
          setProgress(MOCK_PROGRESS);
        } else {
          const [teamRes, progressRes] = await Promise.all([
            api.get('/api/team-leader/my-team'),
            api.get('/api/team-leader/my-team/progress'),
          ]);
          setTeamInfo(teamRes.data);
          const prog: Record<string, MemberProgress> = {};
          (progressRes.data.memberProgress as MemberProgress[]).forEach(p => { prog[p.userId] = p; });
          setProgress(prog);
        }
      } catch (e: any) {
        setError(e.response?.data?.message || 'Không thể tải dữ liệu nhóm.');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const handleAssignExam = async () => {
    setAssigning(true);
    setAssignMsg(null);
    // Mock assign
    await new Promise(r => setTimeout(r, 800));
    setAssignMsg('✅ Đã giao đề luyện tập cho toàn nhóm!');
    setAssigning(false);
    setTimeout(() => setAssignMsg(null), 4000);
  };

  // ── Stats summary ────────────────────────────────────────────────────────────
  const members    = teamInfo?.members ?? [];
  const progList   = members.map(m => progress[m.userId]).filter(Boolean);
  const avgScore   = progList.length ? progList.reduce((s, p) => s + p.avgScore, 0) / progList.length : 0;
  const activeCount = progList.filter(p => p.totalAttempts > 0).length;
  const topMember  = progList.sort((a, b) => b.avgScore - a.avgScore)[0];

  return (
    <div className="min-h-screen bg-slate-950 text-white font-sans">
      {/* ── Header ────────────────────────────────────────────────────────────── */}
      <header className="sticky top-0 z-40 bg-slate-950/80 backdrop-blur-xl border-b border-slate-800/60">
        <div className="max-w-6xl mx-auto px-6 h-14 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Link to="/" className="text-slate-400 hover:text-white transition-colors p-1.5 hover:bg-slate-800 rounded-lg">
              <ArrowLeft size={18} />
            </Link>
            <div className="flex items-center gap-2">
              <div className="w-7 h-7 rounded-lg bg-emerald-500/15 border border-emerald-500/30 flex items-center justify-center">
                <Users size={14} className="text-emerald-400" />
              </div>
              <span className="font-black text-sm text-white tracking-tight">Team Dashboard</span>
            </div>
            {isTeamLeader && (
              <span className="hidden sm:inline-flex items-center gap-1 px-2 py-0.5 bg-emerald-950/50 border border-emerald-500/30 text-emerald-400 rounded-full text-[10px] font-black uppercase tracking-wide">
                <ShieldCheck size={10} /> Trưởng nhóm
              </span>
            )}
          </div>
          <div className="flex items-center gap-2">
            <span className="text-xs text-slate-500 hidden sm:block">{user?.name}</span>
            <button
              onClick={handleAssignExam}
              disabled={assigning || !canManage}
              className="flex items-center gap-1.5 px-3 py-1.5 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 disabled:cursor-not-allowed text-white rounded-lg text-xs font-black transition-all"
            >
              {assigning ? <RefreshCw size={12} className="animate-spin" /> : <Send size={12} />}
              Giao đề thi
            </button>
          </div>
        </div>
      </header>

      {assignMsg && (
        <div className="max-w-6xl mx-auto px-6 pt-4">
          <div className="flex items-center gap-2 px-4 py-3 bg-emerald-950/50 border border-emerald-500/30 text-emerald-300 rounded-xl text-sm font-medium">
            <CheckCircle2 size={16} /> {assignMsg}
          </div>
        </div>
      )}

      <main className="max-w-6xl mx-auto px-6 py-6 space-y-6">
        {/* ── Team Info & Quick Action Studio ───────────────────────────────── */}
        {teamInfo && (
          <div className="bg-gradient-to-r from-emerald-950/50 via-slate-900/80 to-cyan-950/40 border border-emerald-500/30 rounded-2xl p-6 shadow-xl backdrop-blur-md">
            <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
              <div>
                <p className="text-[10px] font-black uppercase text-emerald-400 tracking-widest mb-1.5 flex items-center gap-1.5">
                  <ShieldCheck size={12} /> Không Gian Đội Nhóm • Team Workspace
                </p>
                <h1 className="text-2xl font-black text-white flex items-center gap-2.5">
                  <span>{teamInfo.orgUnit.name}</span>
                  <span className="text-xs font-black px-2.5 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/40">
                    Đang hoạt động
                  </span>
                </h1>
                <p className="text-xs text-slate-400 mt-1">
                  Mã đơn vị: <code className="text-emerald-400 font-mono font-bold">{teamInfo.orgUnit.code}</code> • Quyền hạn: Trưởng nhóm & Quản lý ngân hàng câu hỏi
                </p>
              </div>

              <div className="flex flex-wrap items-center gap-3">
                <Link
                  to="/admin/questions"
                  className="inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-indigo-600 to-cyan-600 hover:brightness-110 text-white rounded-xl text-xs font-black shadow-lg shadow-indigo-500/20 transition-all cursor-pointer"
                >
                  <FileQuestion size={15} />
                  <span>Kho Câu Hỏi Team (Word/Excel)</span>
                </Link>

                <Link
                  to="/practice?scope=TEAM"
                  className="inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-emerald-600 to-teal-600 hover:brightness-110 text-white rounded-xl text-xs font-black shadow-lg shadow-emerald-500/20 transition-all cursor-pointer"
                >
                  <Play size={15} />
                  <span>Luyện Thi Cùng Team</span>
                </Link>
              </div>
            </div>
          </div>
        )}

        {/* ── Loading / Error ──────────────────────────────────────────────────── */}
        {loading && (
          <div className="flex items-center justify-center py-20 text-slate-500">
            <RefreshCw size={20} className="animate-spin mr-2" /> Đang tải dữ liệu nhóm...
          </div>
        )}
        {error && (
          <div className="flex items-center gap-2 px-4 py-3 bg-red-950/40 border border-red-500/20 text-red-400 rounded-xl text-sm">
            <AlertCircle size={16} /> {error}
          </div>
        )}

        {!loading && !error && (
          <>
            {/* ── Summary Stats ─────────────────────────────────────────────── */}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
              {[
                { label: 'Thành viên', value: members.length, icon: <Users size={16} />, color: 'text-blue-400', bg: 'bg-blue-500/10 border-blue-500/20' },
                { label: 'Đang hoạt động', value: activeCount, icon: <Target size={16} />, color: 'text-emerald-400', bg: 'bg-emerald-500/10 border-emerald-500/20' },
                { label: 'Điểm TB nhóm', value: avgScore > 0 ? `${avgScore.toFixed(1)}%` : '—', icon: <BarChart3 size={16} />, color: scoreColor(avgScore), bg: scoreBg(avgScore) },
                { label: 'Top thành viên', value: topMember ? `${topMember.avgScore.toFixed(0)}%` : '—', icon: <Trophy size={16} />, color: 'text-amber-400', bg: 'bg-amber-500/10 border-amber-500/20' },
              ].map((stat) => (
                <div key={stat.label} className={`rounded-2xl border p-4 ${stat.bg}`}>
                  <div className={`mb-2 ${stat.color}`}>{stat.icon}</div>
                  <p className="text-xl font-black text-white">{stat.value}</p>
                  <p className="text-[11px] text-slate-400 font-medium mt-0.5">{stat.label}</p>
                </div>
              ))}
            </div>

            {/* ── Member Progress Table ─────────────────────────────────────── */}
            <div className="bg-slate-900/60 border border-slate-800/60 rounded-2xl overflow-hidden">
              <div className="px-5 py-4 border-b border-slate-800/60 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <BarChart3 size={16} className="text-emerald-400" />
                  <span className="font-black text-sm text-white">Tiến độ học tập</span>
                </div>
                <span className="text-xs text-slate-500">{members.length} thành viên</span>
              </div>

              <div className="divide-y divide-slate-800/40">
                {members.map((member, idx) => {
                  const prog = progress[member.userId];
                  const completion = prog ? Math.round((prog.correctAnswers / Math.max(prog.totalQuestions, 1)) * 100) : 0;

                  return (
                    <div key={member.userId} className="px-5 py-3.5 flex items-center gap-4 hover:bg-slate-800/30 transition-colors group">
                      {/* Rank */}
                      <span className="text-xs text-slate-600 font-mono w-5 text-center">{idx + 1}</span>

                      {/* Avatar */}
                      <div className="w-8 h-8 rounded-full bg-gradient-to-br from-slate-700 to-slate-800 border border-slate-700 flex items-center justify-center text-xs font-black text-slate-300 flex-shrink-0">
                        {(member.name ?? member.userId).charAt(0).toUpperCase()}
                      </div>

                      {/* Name */}
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-semibold text-slate-200 truncate">{member.name ?? member.userId}</p>
                        <p className="text-[11px] text-slate-500">
                          {prog ? (
                            <>
                              <span className="text-slate-400">{prog.totalAttempts} lần thi</span>
                              {prog.lastAttemptDate && (
                                <> · Lần cuối: <span className="text-slate-400">{prog.lastAttemptDate}</span></>
                              )}
                            </>
                          ) : (
                            <span className="text-slate-600 italic">Chưa làm bài</span>
                          )}
                        </p>
                      </div>

                      {/* Progress bar */}
                      <div className="hidden sm:flex flex-col items-end gap-1 w-32">
                        <div className="w-full bg-slate-800 rounded-full h-1.5 overflow-hidden">
                          <div
                            className={`h-full rounded-full transition-all duration-700 ${
                              completion >= 85 ? 'bg-emerald-500' :
                              completion >= 70 ? 'bg-amber-500' :
                              completion >= 50 ? 'bg-orange-500' : 'bg-red-500/70'
                            }`}
                            style={{ width: `${completion}%` }}
                          />
                        </div>
                        <span className="text-[10px] text-slate-500">{completion}% đúng</span>
                      </div>

                      {/* Score badge */}
                      <div className={`flex-shrink-0 w-16 text-center`}>
                        {prog ? (
                          <span className={`text-sm font-black ${scoreColor(prog.avgScore)}`}>
                            {prog.avgScore.toFixed(1)}%
                          </span>
                        ) : (
                          <span className="text-xs text-slate-700">—</span>
                        )}
                      </div>

                      {/* Status icon */}
                      <div className="flex-shrink-0">
                        {!prog || prog.totalAttempts === 0 ? (
                          <Clock size={14} className="text-slate-700" />
                        ) : prog.avgScore >= 85 ? (
                          <CheckCircle2 size={14} className="text-emerald-500" />
                        ) : prog.avgScore >= 70 ? (
                          <TrendingUp size={14} className="text-amber-500" />
                        ) : (
                          <AlertCircle size={14} className="text-orange-400" />
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* ── Quick Actions ─────────────────────────────────────────────── */}
            {canManage && (
              <div className="bg-slate-900/40 border border-slate-800/50 rounded-2xl p-5">
                <p className="text-[10px] font-black uppercase text-slate-500 tracking-widest mb-3">Hành động nhanh</p>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
                  {[
                    { label: 'Giao đề luyện tập', icon: <Send size={14} />, color: 'text-emerald-400 hover:border-emerald-500/50', action: handleAssignExam },
                    { label: 'Luyện thi cùng Team', icon: <Play size={14} />, color: 'text-cyan-400 hover:border-cyan-500/50', action: () => navigate('/practice?scope=TEAM') },
                    { label: 'Xem ngân hàng câu hỏi', icon: <BookOpen size={14} />, color: 'text-amber-400 hover:border-amber-500/50', action: () => navigate('/admin/questions?scope=TEAM') },
                  ].map(action => (
                    <button
                      key={action.label}
                      onClick={action.action}
                      className={`flex items-center gap-2 px-4 py-2.5 bg-slate-900/60 border border-slate-800 ${action.color} rounded-xl text-xs font-semibold transition-all text-left hover:bg-slate-800/60`}
                    >
                      {action.icon} {action.label}
                      <ChevronRight size={12} className="ml-auto opacity-50" />
                    </button>
                  ))}
                </div>
              </div>
            )}
          </>
        )}
      </main>
    </div>
  );
}

export default TeamDashboard;
