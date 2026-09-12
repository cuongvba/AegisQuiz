import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Zap, Trophy, Users, Play, Plus, Sparkles, Shield, ArrowRight } from 'lucide-react';
import { arenaService } from '@/services/arena.service';
import { ARENA_GAMES_METADATA, type ArenaGameCode } from '@/types/arena';

export function ArenaHubPage() {
  const navigate = useNavigate();
  const [selectedGame, setSelectedGame] = useState<ArenaGameCode>('OLYMPIA');
  const [roomNameInput, setRoomNameInput] = useState('');
  const [isCreating, setIsCreating] = useState(false);
  const [activeRooms, setActiveRooms] = useState<any[]>([]);
  const [dynamicTemplates, setDynamicTemplates] = useState<any[]>([]);

  useEffect(() => {
    arenaService.getRooms().then(setActiveRooms);
    arenaService.getTemplates().then(setDynamicTemplates);
  }, []);

  const handleQuickJoin = async (gameCode: ArenaGameCode) => {
    setIsCreating(true);
    try {
      const room = await arenaService.createRoom(gameCode, `${gameCode} Đấu Trường Tri Thức`, 'Kiện Tướng');
      navigate(`/arena/${gameCode}/${room.roomId}`);
    } finally {
      setIsCreating(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 selection:bg-amber-400 selection:text-slate-950 font-sans pb-20">
      {/* ─── HERO ESPORTS BANNER ─── */}
      <section className="relative overflow-hidden border-b border-slate-800 bg-gradient-to-b from-slate-900 via-slate-950 to-slate-950 py-16 px-4">
        {/* Ambient Glows */}
        <div className="absolute -top-24 left-1/4 h-96 w-96 rounded-full bg-amber-500/15 blur-3xl pointer-events-none" />
        <div className="absolute top-10 right-1/4 h-96 w-96 rounded-full bg-cyan-500/15 blur-3xl pointer-events-none" />

        <div className="max-w-6xl mx-auto text-center relative z-10">
          <span className="inline-flex items-center gap-2 rounded-full border border-amber-500/30 bg-amber-500/10 px-4 py-1.5 text-xs font-black uppercase tracking-widest text-amber-400 shadow-sm">
            <Zap className="h-3.5 w-3.5" /> AEGIS ARENA • ĐẤU TRƯỜNG GAMESHOW QUỐC GIA & THẾ GIỚI
          </span>
          <h1 className="mt-4 text-4xl sm:text-6xl font-black tracking-tight text-white">
            Vượt Trội Mọi Gameshow Truyền Hình
          </h1>
          <p className="mt-4 max-w-2xl mx-auto text-sm sm:text-base text-slate-400 leading-relaxed">
            Hệ thống đấu trường thời gian thực với công nghệ bấm chuông phân định mili-giây Microsecond Arbiter, AI Gemini dẫn chương trình và quy mô không giới hạn người tham gia.
          </p>

          <div className="mt-8 flex flex-wrap justify-center gap-4">
            <button
              onClick={() => handleQuickJoin('OLYMPIA')}
              className="flex items-center gap-2.5 rounded-2xl bg-gradient-to-r from-amber-500 via-orange-500 to-rose-600 px-8 py-4 text-base font-black text-slate-950 shadow-xl shadow-amber-500/25 hover:scale-105 active:scale-95 transition cursor-pointer"
            >
              <Play className="h-5 w-5 fill-current" />
              ĐẤU OLYMPIA NGAY ➔
            </button>
            <button
              onClick={() => handleQuickJoin('GOLDEN_BELL')}
              className="flex items-center gap-2.5 rounded-2xl border-2 border-yellow-500/40 bg-yellow-500/10 px-8 py-4 text-base font-black text-yellow-300 hover:bg-yellow-500/20 active:scale-95 transition cursor-pointer"
            >
              🔔 VÀO SÀN RUNG CHUÔNG VÀNG
            </button>
            <button
              onClick={() => navigate('/arena/studio')}
              className="flex items-center gap-2.5 rounded-2xl border-2 border-cyan-500/40 bg-cyan-500/10 px-8 py-4 text-base font-black text-cyan-300 hover:bg-cyan-500/20 active:scale-95 transition cursor-pointer shadow-lg shadow-cyan-500/10"
            >
              🎨 TỰ TẠO GAMESHOW (ARENA STUDIO)
            </button>
          </div>
        </div>
      </section>

      {/* ─── 6 GAMESHOW ARENA PLUGINS GRID ─── */}
      <section className="max-w-7xl mx-auto px-4 py-12">
        <div className="flex items-center justify-between mb-8">
          <div>
            <h2 className="text-2xl font-black text-white tracking-tight">
              Danh Sách Đấu Trường Gameshow Bản Quyền
            </h2>
            <p className="text-xs text-slate-400 mt-1">
              Chọn format đấu trường để bắt đầu thi đấu trực tiếp hoặc tạo phòng riêng
            </p>
          </div>
          <span className="rounded-full bg-slate-900 border border-slate-800 px-3.5 py-1 text-xs font-bold text-amber-400">
            6 Đấu Trường Đang Hoạt Động
          </span>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {ARENA_GAMES_METADATA.map((game) => (
            <div
              key={game.gameCode}
              className="group relative flex flex-col justify-between overflow-hidden rounded-3xl border-2 border-slate-800 bg-slate-900/80 p-6 transition-all duration-300 hover:border-slate-600 hover:bg-slate-900 hover:shadow-2xl hover:-translate-y-1"
            >
              {/* Top ambient color strip */}
              <div className={`absolute top-0 left-0 right-0 h-1.5 bg-gradient-to-r ${game.gradient}`} />

              <div>
                <div className="flex items-center justify-between">
                  <span className="flex h-14 w-14 items-center justify-center rounded-2xl bg-slate-800 text-3xl shadow-inner group-hover:scale-110 transition-transform">
                    {game.icon}
                  </span>
                  <span className={`rounded-full border px-3 py-1 text-xs font-black uppercase tracking-wider ${game.accentColor}`}>
                    {game.badge}
                  </span>
                </div>

                <h3 className="mt-4 text-xl font-black text-white tracking-tight group-hover:text-amber-400 transition-colors">
                  {game.displayName}
                </h3>
                <p className="mt-2 text-xs text-slate-400 leading-relaxed line-clamp-3">
                  {game.description}
                </p>

                <div className="mt-4 flex items-center gap-4 text-xs font-bold text-slate-400 border-t border-slate-800/80 pt-3">
                  <span className="flex items-center gap-1.5">
                    <Users className="h-3.5 w-3.5 text-slate-500" />
                    {game.minPlayers} - {game.maxPlayers.toLocaleString()} Người
                  </span>
                  <span className="flex items-center gap-1.5 text-emerald-400">
                    <span className="h-2 w-2 rounded-full bg-emerald-400 animate-ping" />
                    SignalR Live
                  </span>
                </div>
              </div>

              <div className="mt-6">
                <button
                  disabled={isCreating}
                  onClick={() => handleQuickJoin(game.gameCode)}
                  className={`w-full flex items-center justify-center gap-2 rounded-2xl py-3.5 font-black text-sm text-slate-950 shadow-lg bg-gradient-to-r ${game.gradient} hover:opacity-95 active:scale-98 transition cursor-pointer`}
                >
                  <Play className="h-4 w-4 fill-current" />
                  VÀO THI ĐẤU NGAY ➔
                </button>
              </div>
            </div>
          ))}
        </div>

        {/* ─── DYNAMIC & COMMUNITY GAMESHOW TEMPLATES (NO-CODE ARENA STUDIO) ─── */}
        {dynamicTemplates.length > 0 && (
          <div className="mt-16 pt-12 border-t border-slate-800/80">
            <div className="flex flex-wrap items-center justify-between gap-4 mb-8">
              <div>
                <div className="flex items-center gap-2">
                  <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-cyan-500/20 text-cyan-400 font-black text-sm">
                    🎨
                  </span>
                  <h2 className="text-2xl font-black text-white tracking-tight">
                    Đấu Trường Tự Thiết Kế (Arena Studio & Community Games)
                  </h2>
                </div>
                <p className="text-xs text-slate-400 mt-1">
                  Các format gameshow được cấu hình động 100% từ giao diện No-Code Arena Studio
                </p>
              </div>

              <button
                onClick={() => navigate('/arena/studio')}
                className="flex items-center gap-2 rounded-xl border border-cyan-500/40 bg-cyan-500/10 px-4 py-2 text-xs font-black text-cyan-300 hover:bg-cyan-500/20 transition cursor-pointer"
              >
                + TẠO THÊM GAMESHOW MỚI
              </button>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              {dynamicTemplates.map((template) => (
                <div
                  key={template.gameCode}
                  className="flex flex-col justify-between rounded-3xl border border-slate-800 bg-slate-900/60 p-6 backdrop-blur transition-all duration-300 hover:border-cyan-500/50 hover:bg-slate-900 hover:shadow-xl hover:-translate-y-1"
                >
                  <div>
                    <div className="flex items-center justify-between">
                      <span className="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-800 text-2xl shadow-inner">
                        {template.icon === 'Trophy' ? '🏆' : template.icon === 'Flame' ? '🔥' : template.icon === 'Users' ? '👥' : '✨'}
                      </span>
                      <span className="rounded-full bg-cyan-500/10 border border-cyan-500/30 px-2.5 py-0.5 text-[10px] font-black uppercase text-cyan-400">
                        {template.layoutShell}
                      </span>
                    </div>

                    <h3 className="mt-4 text-lg font-black text-white tracking-tight">
                      {template.displayName}
                    </h3>
                    <p className="mt-1.5 text-xs text-slate-400 line-clamp-2 leading-relaxed">
                      {template.description}
                    </p>

                    <div className="mt-4 flex items-center justify-between text-[11px] font-bold text-slate-500 border-t border-slate-800/80 pt-3">
                      <span>Cơ chế: {template.contentionMode}</span>
                      <span className="text-amber-400">★ {template.createdBy || 'Studio'}</span>
                    </div>
                  </div>

                  <div className="mt-6">
                    <button
                      disabled={isCreating}
                      onClick={() => handleQuickJoin(template.gameCode)}
                      className="w-full flex items-center justify-center gap-2 rounded-2xl py-3 font-black text-xs text-slate-950 shadow-md bg-gradient-to-r from-cyan-400 to-teal-500 hover:opacity-90 active:scale-95 transition cursor-pointer"
                    >
                      <Play className="h-3.5 w-3.5 fill-current" />
                      VÀO ĐẤU TRƯỜNG ➔
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </section>
    </div>
  );
}
export default ArenaHubPage;
