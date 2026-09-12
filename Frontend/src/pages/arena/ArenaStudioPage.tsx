import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Sparkles,
  Zap,
  Trophy,
  Users,
  Flame,
  Check,
  Play,
  RotateCcw,
  Palette,
  Eye,
  Sliders,
  Plus,
  Trash2,
  HelpCircle,
  Clock,
  Layers,
  Award,
  ArrowRight,
} from 'lucide-react';
import type { GameshowManifest, LayoutShellType, ContentionMode } from '@/types/arena';
import { arenaService, playArenaSfx } from '@/services/arena.service';
import { DynamicUniversalArenaView } from './components/DynamicUniversalArenaView';

// ─── THƯ VIỆN PRESETS MẪU HOÀNG KIM (1-CLICK LOAD) ───────────────────────────
const STUDIO_PRESETS: Record<string, Partial<GameshowManifest>> = {
  MILLIONAIRE: {
    gameCode: 'TRIEU_PHU_VIP',
    displayName: 'Ai Là Triệu Phú (Millionaire Challenge)',
    description: 'Chinh phục 15 bậc thang tri thức đỉnh cao với 3 mốc an toàn và 4 quyền trợ giúp phao cứu sinh.',
    icon: 'Trophy',
    layoutShell: 'STEP_LADDER',
    contentionMode: 'TURN_ROUND_ROBIN',
    themeColor: '#1e3a8a',
    scoringRule: {
      basePointsPerCorrect: 1000,
      dropToZeroOnWrong: true,
      ladderPoints: [200, 400, 600, 1000, 2000, 3000, 6000, 10000, 14000, 22000, 30000, 40000, 60000, 85000, 150000],
    },
    survivalRule: {
      type: 'STRIKE_LIMIT',
      maxStrikes: 1,
      safeMilestones: [5, 10],
    },
    enabledLifelines: [
      { code: '50_50', name: 'Trợ giúp 50:50', icon: 'Zap' },
      { code: 'ASK_AI', name: 'Hỏi Gia Sư AI Socrates', icon: 'Brain' },
      { code: 'POLL_AUDIENCE', name: 'Hỏi Ý Kiến Khán Giả', icon: 'Users' },
      { code: 'CHANGE_QUESTION', name: 'Đổi Câu Hỏi', icon: 'RefreshCw' },
    ],
    rounds: [
      { roundIndex: 1, roundName: 'Khởi Động Mốc 1', durationSeconds: 30 },
      { roundIndex: 2, roundName: 'Vượt Dốc Mốc 2', durationSeconds: 45 },
      { roundIndex: 3, roundName: 'Đỉnh Cao Tri Thức', durationSeconds: 60 },
    ],
  },
  THE_CHASE: {
    gameCode: 'THE_CHASE_ARENA',
    displayName: 'Kẻ Săn Mồi (The Chase Vietnam)',
    description: 'Đấu trí nghẹt thở giữa Đội Người Chơi và Kẻ Săn Mồi AI trên thang dốc truy đuổi tử thần.',
    icon: 'Flame',
    layoutShell: 'STEP_LADDER',
    contentionMode: 'BUZZER_FASTEST',
    themeColor: '#dc2626',
    scoringRule: {
      basePointsPerCorrect: 100,
      penaltyPerWrong: 50,
      speedBonus: true,
    },
    survivalRule: { type: 'SUDDEN_DEATH' },
    enabledLifelines: [
      { code: 'TIME_FREEZE', name: 'Đóng Băng Kẻ Săn Mồi 10s', icon: 'Shield' },
      { code: '50_50', name: '50:50', icon: 'Zap' },
    ],
    rounds: [
      { roundIndex: 1, roundName: 'Tích Lũy Đối Đầu', durationSeconds: 60 },
      { roundIndex: 2, roundName: 'Đuổi Bắt 120 Giây', durationSeconds: 120 },
    ],
  },
  DUOI_HINH: {
    gameCode: 'DUOI_HINH_CUSTOM',
    displayName: 'Đuổi Hình Bắt Chữ Trực Quan',
    description: 'Bóc tách câu đố hình tượng dân gian qua bảng lật tranh ẩn số trung tâm 8 mảnh ghép.',
    icon: 'Sparkles',
    layoutShell: 'HIDDEN_TILES',
    contentionMode: 'BUZZER_FASTEST',
    themeColor: '#10b981',
    scoringRule: { basePointsPerCorrect: 50, speedBonus: true },
    survivalRule: { type: 'ACCUMULATIVE' },
    enabledLifelines: [
      { code: 'OPEN_ONE_TILE', name: 'Lật Mở 1 Ô Gợi Ý', icon: 'Eye' },
      { code: 'ASK_AI', name: 'Hỏi AI Socrates', icon: 'Brain' },
    ],
    rounds: [{ roundIndex: 1, roundName: 'Lật Tranh Đoán Chữ', durationSeconds: 45 }],
  },
  MOB_100: {
    gameCode: 'MOB_100_ARENA',
    displayName: 'Đấu Trí 1 vs 100 (The Mob)',
    description: '1 Thí sinh trung tâm đấu trí cùng sàn đấu 100 người chơi. Sai 1 câu là loại trực tiếp!',
    icon: 'Users',
    layoutShell: 'MEGA_GRID',
    contentionMode: 'SIMULTANEOUS_ALL',
    themeColor: '#8b5cf6',
    scoringRule: { basePointsPerCorrect: 20, streakMultiplier: true },
    survivalRule: { type: 'SUDDEN_DEATH' },
    enabledLifelines: [
      { code: 'ASK_THE_MOB', name: 'Thăm Dò 100 Người', icon: 'Users' },
      { code: 'REVIVAL_EVENT', name: 'Thầy Cô Cứu Trợ Hồi Sinh', icon: 'Sparkles' },
    ],
    rounds: [
      { roundIndex: 1, roundName: 'Sàng Lọc Quần Hùng', durationSeconds: 20 },
      { roundIndex: 2, roundName: 'Đơn Đấu Chung Cuộc', durationSeconds: 30 },
    ],
  },
};

export function ArenaStudioPage() {
  const navigate = useNavigate();

  // State cấu hình Gameshow Manifest
  const [manifest, setManifest] = useState<GameshowManifest>({
    gameCode: 'MY_GAMESHOW_2026',
    displayName: 'Đấu Trường Trí Tuệ Tự Thiết Kế',
    description: 'Gameshow thế hệ mới do cộng đồng sáng tạo trên Aegis Arena Studio.',
    icon: 'Trophy',
    minPlayers: 1,
    maxPlayers: 100,
    layoutShell: 'STEP_LADDER',
    contentionMode: 'BUZZER_FASTEST',
    themeColor: '#f59e0b',
    scoringRule: {
      basePointsPerCorrect: 20,
      penaltyPerWrong: 10,
      speedBonus: true,
      streakMultiplier: true,
      dropToZeroOnWrong: false,
    },
    survivalRule: {
      type: 'ACCUMULATIVE',
      maxStrikes: 3,
      safeMilestones: [5, 10],
    },
    enabledLifelines: [
      { code: '50_50', name: 'Trợ giúp 50:50', icon: 'Zap' },
      { code: 'ASK_AI', name: 'Gia Sư AI Socrates', icon: 'Brain' },
      { code: 'POLL_AUDIENCE', name: 'Ý Kiến Khán Giả', icon: 'Users' },
    ],
    rounds: [
      { roundIndex: 1, roundName: 'Vòng Khởi Động', durationSeconds: 30 },
      { roundIndex: 2, roundName: 'Vòng Tăng Tốc', durationSeconds: 45 },
      { roundIndex: 3, roundName: 'Vòng Về Đích', durationSeconds: 60 },
    ],
    isBuiltIn: false,
  });

  const [activeTab, setActiveTab] = useState<'DESIGN' | 'PREVIEW'>('DESIGN');
  const [isPublishing, setIsPublishing] = useState(false);
  const [publishSuccess, setPublishSuccess] = useState(false);

  // 1-Click Load Preset
  const handleLoadPreset = (key: string) => {
    playArenaSfx('wheel_tick');
    const preset = STUDIO_PRESETS[key];
    if (preset) {
      setManifest((prev) => ({
        ...prev,
        ...preset,
        gameCode: `${preset.gameCode}_${Math.floor(Math.random() * 1000)}`,
      }));
    }
  };

  // Toggle Lifelines
  const toggleLifeline = (code: string, name: string, icon: string) => {
    setManifest((prev) => {
      const exists = prev.enabledLifelines.some((l) => l.code === code);
      if (exists) {
        return { ...prev, enabledLifelines: prev.enabledLifelines.filter((l) => l.code !== code) };
      } else {
        return { ...prev, enabledLifelines: [...prev.enabledLifelines, { code, name, icon }] };
      }
    });
  };

  // Publish Gameshow
  const handlePublish = async () => {
    setIsPublishing(true);
    try {
      await arenaService.saveTemplate(manifest);
      playArenaSfx('victory');
      setPublishSuccess(true);
      setTimeout(() => {
        navigate('/arena');
      }, 1500);
    } catch (err: any) {
      alert(err.message || 'Lỗi xuất bản Gameshow');
    } finally {
      setIsPublishing(false);
    }
  };

  // Mock State dùng cho Live Preview Simulator
  const mockRoomState: any = {
    roomId: 'SIMULATOR',
    gameCode: manifest.gameCode,
    roomName: manifest.displayName,
    hostUserId: 'host-simulator',
    currentStage: 'ROUND_1',
    currentQuestionIndex: 0,
    questions: [
      {
        id: 'q-sim-1',
        content: 'Hành tinh nào gần Mặt Trời nhất trong Hệ Mặt Trời?',
        options: ['Sao Kim (Venus)', 'Sao Thủy (Mercury)', 'Sao Hỏa (Mars)', 'Trái Đất (Earth)'],
        answerRaw: 'Sao Thủy (Mercury)',
        points: manifest.scoringRule.basePointsPerCorrect,
        timeLimitSeconds: manifest.rounds[0]?.durationSeconds || 30,
        category: 'THIÊN VĂN HỌC',
      },
    ],
    players: {
      'host-simulator': {
        id: 'host-simulator',
        name: 'Bạn (Host Thử Nghiệm)',
        avatar: '👑',
        score: 120,
        streak: 3,
        stepPosition: 4,
        status: 'ACTIVE',
      },
    },
    buzzerWinnerPlayerId: null,
    buzzerTimestampMs: 0,
    stageStartTimeUtc: new Date().toISOString(),
    stageDurationSeconds: manifest.rounds[0]?.durationSeconds || 30,
    customData: {
      Manifest: manifest,
      LayoutShell: manifest.layoutShell,
      ContentionMode: manifest.contentionMode,
    },
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans pb-20">
      {/* ─── TOPBAR STUDIO HEADER (FIGMA STYLE) ─── */}
      <header className="sticky top-0 z-40 border-b border-slate-800 bg-slate-900/90 backdrop-blur px-6 py-4">
        <div className="max-w-7xl mx-auto flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <button
              onClick={() => navigate('/arena')}
              className="rounded-xl border border-slate-800 bg-slate-950 p-2 text-slate-400 hover:text-white transition cursor-pointer"
              title="Quay lại Đấu Trường"
            >
              ←
            </button>
            <div className="flex items-center gap-2">
              <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-tr from-amber-500 to-rose-600 text-lg text-slate-950 font-black shadow-lg">
                🎨
              </span>
              <div>
                <h1 className="text-base font-black text-white flex items-center gap-2">
                  Aegis Arena Studio
                  <span className="rounded-full bg-amber-500/10 border border-amber-500/30 px-2 py-0.5 text-[10px] font-black text-amber-400">
                    NO-CODE ENGINE
                  </span>
                </h1>
                <p className="text-xs text-slate-400">Figma / Canva của Đấu Trường Trí Tuệ Học Thuật</p>
              </div>
            </div>
          </div>

          {/* Mode Switcher: Design vs Preview */}
          <div className="flex items-center gap-2 rounded-2xl bg-slate-950 p-1 border border-slate-800">
            <button
              onClick={() => setActiveTab('DESIGN')}
              className={`flex items-center gap-1.5 rounded-xl px-4 py-1.5 text-xs font-black transition cursor-pointer ${
                activeTab === 'DESIGN'
                  ? 'bg-amber-500 text-slate-950 shadow-md'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <Sliders className="h-3.5 w-3.5" /> THIẾT KẾ
            </button>
            <button
              onClick={() => setActiveTab('PREVIEW')}
              className={`flex items-center gap-1.5 rounded-xl px-4 py-1.5 text-xs font-black transition cursor-pointer ${
                activeTab === 'PREVIEW'
                  ? 'bg-amber-500 text-slate-950 shadow-md'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <Eye className="h-3.5 w-3.5" /> LIVE SIMULATOR
            </button>
          </div>

          {/* Action Button: Publish */}
          <div className="flex items-center gap-3">
            <button
              onClick={handlePublish}
              disabled={isPublishing}
              className="flex items-center gap-2 rounded-2xl bg-gradient-to-r from-emerald-500 to-teal-600 px-6 py-2.5 text-xs font-black text-slate-950 shadow-xl shadow-emerald-500/20 hover:scale-105 active:scale-95 transition cursor-pointer disabled:opacity-50"
            >
              <Play className="h-4 w-4 fill-current" />
              {isPublishing ? 'ĐANG XUẤT BẢN...' : publishSuccess ? '✓ ĐÃ XUẤT BẢN!' : 'XUẤT BẢN GAMESHOW ➔'}
            </button>
          </div>
        </div>
      </header>

      {/* ─── PRESET GALLERY BAR (1-CLICK TEMPLATES) ─── */}
      <div className="border-b border-slate-800/80 bg-slate-900/40 px-6 py-3">
        <div className="max-w-7xl mx-auto flex flex-wrap items-center gap-3">
          <span className="text-xs font-black text-slate-400 uppercase tracking-widest flex items-center gap-1.5">
            <Sparkles className="h-3.5 w-3.5 text-amber-400" /> NẠP MẪU NHANH (PRESETS):
          </span>
          <button
            onClick={() => handleLoadPreset('MILLIONAIRE')}
            className="flex items-center gap-1.5 rounded-xl border border-slate-700 bg-slate-800/80 px-3 py-1 text-xs font-bold text-slate-200 hover:border-amber-400 hover:text-amber-300 transition cursor-pointer"
          >
            🏆 Ai Là Triệu Phú
          </button>
          <button
            onClick={() => handleLoadPreset('THE_CHASE')}
            className="flex items-center gap-1.5 rounded-xl border border-slate-700 bg-slate-800/80 px-3 py-1 text-xs font-bold text-slate-200 hover:border-rose-400 hover:text-rose-300 transition cursor-pointer"
          >
            🔥 Kẻ Săn Mồi (The Chase)
          </button>
          <button
            onClick={() => handleLoadPreset('DUOI_HINH')}
            className="flex items-center gap-1.5 rounded-xl border border-slate-700 bg-slate-800/80 px-3 py-1 text-xs font-bold text-slate-200 hover:border-emerald-400 hover:text-emerald-300 transition cursor-pointer"
          >
            🧩 Đuổi Hình Bắt Chữ
          </button>
          <button
            onClick={() => handleLoadPreset('MOB_100')}
            className="flex items-center gap-1.5 rounded-xl border border-slate-700 bg-slate-800/80 px-3 py-1 text-xs font-bold text-slate-200 hover:border-purple-400 hover:text-purple-300 transition cursor-pointer"
          >
            👥 Đấu Trí 1 vs 100
          </button>
        </div>
      </div>

      {/* ─── MAIN WORKSPACE: SPLIT SCREEN (TOOLBOX & LIVE PREVIEW CANVAS) ─── */}
      <main className="max-w-7xl mx-auto px-6 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8">
          {/* CỘT TRÁI: CONFIGURATION TOOLBOX (CANVA/FIGMA INSPECTOR) */}
          <div className={`lg:col-span-6 space-y-6 ${activeTab === 'PREVIEW' ? 'hidden lg:block' : ''}`}>
            {/* 1. THÔNG TIN CƠ BẢN */}
            <div className="rounded-3xl border border-slate-800 bg-slate-900/60 p-6 backdrop-blur">
              <h3 className="text-xs font-black uppercase text-amber-400 tracking-wider mb-4 flex items-center gap-2">
                <Palette className="h-4 w-4" /> 1. NHẬN DIỆN & THƯƠNG HIỆU GAMESHOW
              </h3>
              <div className="space-y-4">
                <div>
                  <label className="text-xs font-bold text-slate-300">Tên Hiển Thị Của Gameshow:</label>
                  <input
                    type="text"
                    value={manifest.displayName}
                    onChange={(e) => setManifest({ ...manifest, displayName: e.target.value })}
                    className="mt-1.5 w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2.5 text-sm font-bold text-white focus:border-amber-400 focus:outline-none"
                    placeholder="VD: Đấu Trường Siêu Trí Tuệ"
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="text-xs font-bold text-slate-300">Mã Định Danh (GameCode):</label>
                    <input
                      type="text"
                      value={manifest.gameCode}
                      onChange={(e) => setManifest({ ...manifest, gameCode: e.target.value.toUpperCase().replace(/\s+/g, '_') })}
                      className="mt-1.5 w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2 text-xs font-mono font-black text-amber-300 focus:border-amber-400 focus:outline-none"
                    />
                  </div>
                  <div>
                    <label className="text-xs font-bold text-slate-300">Biểu Tượng (Icon):</label>
                    <select
                      value={manifest.icon}
                      onChange={(e) => setManifest({ ...manifest, icon: e.target.value })}
                      className="mt-1.5 w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2 text-xs font-bold text-white focus:border-amber-400 focus:outline-none"
                    >
                      <option value="Trophy">🏆 Cúp Chiến Thắng (Trophy)</option>
                      <option value="Flame">🔥 Ngọn Lửa (Flame)</option>
                      <option value="Zap">⚡ Tia Chớp (Zap)</option>
                      <option value="Users">👥 Đấu Trường Quần Hùng (Users)</option>
                      <option value="Sparkles">✨ Ma Thuật (Sparkles)</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label className="text-xs font-bold text-slate-300">Mô Tả Luật Chơi Tóm Tắt:</label>
                  <textarea
                    value={manifest.description}
                    onChange={(e) => setManifest({ ...manifest, description: e.target.value })}
                    rows={2}
                    className="mt-1.5 w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2 text-xs text-slate-300 focus:border-amber-400 focus:outline-none"
                  />
                </div>
              </div>
            </div>

            {/* 2. BỐ CỤC SÂN KHẤU TRỰC QUAN (LAYOUT SHELL) */}
            <div className="rounded-3xl border border-slate-800 bg-slate-900/60 p-6 backdrop-blur">
              <h3 className="text-xs font-black uppercase text-amber-400 tracking-wider mb-4 flex items-center gap-2">
                <Layers className="h-4 w-4" /> 2. CHỌN BỐ CỤC SÂN KHẤU (STAGE LAYOUT SHELL)
              </h3>
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                {[
                  { code: 'STEP_LADDER', name: 'Thang Leo Dốc', icon: '⚡', desc: '10-15 Bậc leo đứng' },
                  { code: 'PODIUM', name: 'Bục Thí Sinh 3D', icon: '🏛️', desc: '1-8 người đối kháng' },
                  { code: 'MEGA_GRID', name: 'Ma Trận Ghế Số', icon: '🔔', desc: '100-1000 người' },
                  { code: 'RADIAL_WHEEL', name: 'Bánh Xe Vật Lý', icon: '🎡', desc: 'Quay nan quạt 60fps' },
                  { code: 'HIDDEN_TILES', name: 'Lật Mảnh Ghép', icon: '🧩', desc: 'Ô chữ & hình ảnh' },
                  { code: 'TOPIC_MATRIX', name: 'Ma Trận Chủ Đề', icon: '🇺🇸', desc: 'Ma trận điểm cược' },
                ].map((item) => (
                  <button
                    key={item.code}
                    onClick={() => setManifest({ ...manifest, layoutShell: item.code as LayoutShellType })}
                    className={`flex flex-col items-start p-4 rounded-2xl border text-left transition cursor-pointer ${
                      manifest.layoutShell === item.code
                        ? 'border-amber-400 bg-amber-500/20 text-amber-300 ring-2 ring-amber-400/40 shadow-lg'
                        : 'border-slate-800 bg-slate-950/60 text-slate-400 hover:border-slate-700 hover:text-white'
                    }`}
                  >
                    <span className="text-2xl mb-1">{item.icon}</span>
                    <span className="text-xs font-black text-white">{item.name}</span>
                    <span className="text-[10px] text-slate-500 mt-0.5">{item.desc}</span>
                  </button>
                ))}
              </div>
            </div>

            {/* 3. CƠ CHẾ TRANH LƯỢT & QUY CHẾ TÍNH ĐIỂM */}
            <div className="rounded-3xl border border-slate-800 bg-slate-900/60 p-6 backdrop-blur">
              <h3 className="text-xs font-black uppercase text-amber-400 tracking-wider mb-4 flex items-center gap-2">
                <Zap className="h-4 w-4" /> 3. TRANH CHẤP LƯỢT & LUẬT TÍNH ĐIỂM
              </h3>
              <div className="space-y-4">
                <div>
                  <label className="text-xs font-bold text-slate-300">Cơ Chế Tranh Quyền Trả Lời:</label>
                  <div className="grid grid-cols-2 gap-2 mt-1.5">
                    {[
                      { code: 'BUZZER_FASTEST', name: 'Bấm Chuông Microsecond', desc: 'Ai nhanh nhất được nói' },
                      { code: 'SIMULTANEOUS_ALL', name: 'Đồng Loạt Cùng Làm', desc: 'Hết giờ chốt kết quả' },
                      { code: 'TURN_ROUND_ROBIN', name: 'Lần Lượt Theo Lượt', desc: 'Xoay vòng từng người' },
                      { code: 'SELECTIVE_PICK', name: 'Tự Chọn Ô / Điểm', desc: 'Cược điểm trước khi mở' },
                    ].map((m) => (
                      <button
                        key={m.code}
                        onClick={() => setManifest({ ...manifest, contentionMode: m.code as ContentionMode })}
                        className={`p-3 rounded-xl border text-left transition cursor-pointer ${
                          manifest.contentionMode === m.code
                            ? 'border-amber-400 bg-amber-500/20 text-white font-bold'
                            : 'border-slate-800 bg-slate-950/60 text-slate-400'
                        }`}
                      >
                        <p className="text-xs font-black">{m.name}</p>
                        <p className="text-[10px] text-slate-500">{m.desc}</p>
                      </button>
                    ))}
                  </div>
                </div>

                <div className="pt-3 border-t border-slate-800 grid grid-cols-2 gap-4">
                  <div>
                    <label className="text-xs font-bold text-slate-300">Điểm Cơ Bản (+Đúng):</label>
                    <input
                      type="number"
                      value={manifest.scoringRule.basePointsPerCorrect}
                      onChange={(e) =>
                        setManifest({
                          ...manifest,
                          scoringRule: { ...manifest.scoringRule, basePointsPerCorrect: parseInt(e.target.value) || 10 },
                        })
                      }
                      className="mt-1.5 w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2 text-sm font-black text-amber-400"
                    />
                  </div>
                  <div>
                    <label className="text-xs font-bold text-slate-300">Điểm Phạt (-Sai):</label>
                    <input
                      type="number"
                      value={manifest.scoringRule.penaltyPerWrong || 0}
                      onChange={(e) =>
                        setManifest({
                          ...manifest,
                          scoringRule: { ...manifest.scoringRule, penaltyPerWrong: parseInt(e.target.value) || 0 },
                        })
                      }
                      className="mt-1.5 w-full rounded-xl border border-slate-700 bg-slate-950 px-4 py-2 text-sm font-black text-rose-400"
                    />
                  </div>
                </div>

                {/* Switch Tụt dốc về 0 & Đột tử */}
                <div className="space-y-2 pt-2">
                  <label className="flex items-center gap-3 cursor-pointer p-3 rounded-xl border border-slate-800 bg-slate-950/50">
                    <input
                      type="checkbox"
                      checked={manifest.scoringRule.dropToZeroOnWrong || false}
                      onChange={(e) =>
                        setManifest({
                          ...manifest,
                          scoringRule: { ...manifest.scoringRule, dropToZeroOnWrong: e.target.checked },
                        })
                      }
                      className="h-4 w-4 rounded accent-amber-500"
                    />
                    <div>
                      <span className="text-xs font-bold text-white block">Tụt Dốc Về 0 Khi Trả Lời Sai</span>
                      <span className="text-[10px] text-slate-500">Người chơi sai 1 câu sẽ bị rơi thẳng về vạch xuất phát số 0</span>
                    </div>
                  </label>

                  <label className="flex items-center gap-3 cursor-pointer p-3 rounded-xl border border-slate-800 bg-slate-950/50">
                    <input
                      type="checkbox"
                      checked={manifest.survivalRule.type === 'SUDDEN_DEATH'}
                      onChange={(e) =>
                        setManifest({
                          ...manifest,
                          survivalRule: { ...manifest.survivalRule, type: e.target.checked ? 'SUDDEN_DEATH' : 'ACCUMULATIVE' },
                        })
                      }
                      className="h-4 w-4 rounded accent-rose-500"
                    />
                    <div>
                      <span className="text-xs font-bold text-white block">Đột Tử (Sudden Death): Sai 1 Câu Là Rời Sàn Đấu</span>
                      <span className="text-[10px] text-slate-500">Dành cho các thể thức Battle Royale như Rung Chuông Vàng</span>
                    </div>
                  </label>
                </div>
              </div>
            </div>

            {/* 4. KHO PHAO CỨU SINH (LIFELINES) */}
            <div className="rounded-3xl border border-slate-800 bg-slate-900/60 p-6 backdrop-blur">
              <h3 className="text-xs font-black uppercase text-amber-400 tracking-wider mb-4 flex items-center gap-2">
                <Sparkles className="h-4 w-4" /> 4. KÍCH HOẠT PHAO CỨU SINH (LIFELINES)
              </h3>
              <div className="grid grid-cols-2 gap-2.5">
                {[
                  { code: '50_50', name: 'Trợ giúp 50:50', icon: 'Zap' },
                  { code: 'ASK_AI', name: 'Gia Sư AI Socrates', icon: 'Brain' },
                  { code: 'POLL_AUDIENCE', name: 'Ý Kiến Khán Giả', icon: 'Users' },
                  { code: 'CHANGE_QUESTION', name: 'Đổi Câu Hỏi', icon: 'RefreshCw' },
                  { code: 'REVIVAL_EVENT', name: 'Thầy Cô Ném Bóng Cứu Trợ', icon: 'Sparkles' },
                  { code: 'STAR_OF_HOPE', name: 'Ngôi Sao Hy Vọng (x2)', icon: 'Trophy' },
                ].map((life) => {
                  const isChecked = manifest.enabledLifelines.some((l) => l.code === life.code);
                  return (
                    <button
                      key={life.code}
                      onClick={() => toggleLifeline(life.code, life.name, life.icon)}
                      className={`flex items-center gap-2.5 p-3 rounded-xl border text-left transition cursor-pointer ${
                        isChecked
                          ? 'border-amber-400/60 bg-amber-500/15 text-amber-200'
                          : 'border-slate-800 bg-slate-950/60 text-slate-400 hover:border-slate-700'
                      }`}
                    >
                      <span className="flex h-5 w-5 items-center justify-center rounded-md border text-xs">
                        {isChecked ? '✓' : ''}
                      </span>
                      <span className="text-xs font-bold">{life.name}</span>
                    </button>
                  );
                })}
              </div>
            </div>
          </div>

          {/* CỘT PHẢI: LIVE INTERACTIVE STAGE PREVIEW (FIGMA CANVAS SIMULATOR) */}
          <div className={`lg:col-span-6 ${activeTab === 'DESIGN' ? 'hidden lg:block' : ''}`}>
            <div className="sticky top-24 space-y-4">
              <div className="flex items-center justify-between">
                <span className="text-xs font-black uppercase text-slate-400 tracking-wider flex items-center gap-2">
                  <Eye className="h-4 w-4 text-emerald-400" /> LIVE CANVAS PREVIEW (MÔ PHỎNG SÀN ĐẤU THỜI GIAN THỰC)
                </span>
                <span className="rounded-full bg-emerald-500/10 border border-emerald-500/30 px-3 py-0.5 text-[10px] font-black text-emerald-400">
                  REAL-TIME REACTIVE
                </span>
              </div>

              {/* KHUNG MÔ PHỎNG SÂN KHẤU CANVAS */}
              <div className="rounded-3xl border-2 border-slate-800 bg-slate-950 p-4 shadow-2xl overflow-hidden relative">
                <DynamicUniversalArenaView
                  state={mockRoomState}
                  onBuzzer={() => {
                    playArenaSfx('buzzer');
                    alert('🔔 [Live Simulator]: Bạn vừa bấm chuông cướp quyền trong 0.000001s!');
                  }}
                  onSubmitAnswer={(ans) => {
                    playArenaSfx('victory');
                    alert(`🎯 [Live Simulator]: Bạn đã chọn đáp án: "${ans}"!`);
                  }}
                  onAction={(type, payload) => {
                    playArenaSfx('wheel_tick');
                    alert(`⚡ [Live Simulator]: Đã kích hoạt hành động ${type}: ${payload}`);
                  }}
                  onNextStage={(s) => alert(`Tiến trình vòng thi: ${s}`)}
                />
              </div>

              <p className="text-center text-xs text-slate-500">
                💡 Bất kỳ thay đổi nào ở cột bên trái sẽ tự động phản ánh tức thì lên sàn đấu Live Preview phía trên!
              </p>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}

export default ArenaStudioPage;
