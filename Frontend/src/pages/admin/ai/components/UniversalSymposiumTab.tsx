import { 
  Sparkles, Scale, Info, Edit3, Trash2 
} from 'lucide-react';
import { 
  UniversalAiCouncilDto, 
  UniversalCouncilSeatDto, 
  CouncilCategory 
} from '@/services/ai-swarm.service';

interface UniversalSymposiumTabProps {
  councils: UniversalAiCouncilDto[];
  councilCategoryFilter: string;
  setCouncilCategoryFilter: (v: string) => void;
  setSelectedCouncilForDetail: (c: UniversalAiCouncilDto) => void;
  setEditingCouncil: (c: UniversalAiCouncilDto) => void;
  setSelectedSeatForSwarm: (v: { seat: UniversalCouncilSeatDto; councilName: string }) => void;
  handleDeleteSeat: (id: string, name: string) => void;
  onOpenConveneModal: () => void;
  t: (key: string, opts?: any) => string;
}

export function UniversalSymposiumTab({
  councils,
  councilCategoryFilter,
  setCouncilCategoryFilter,
  setSelectedCouncilForDetail,
  setEditingCouncil,
  setSelectedSeatForSwarm,
  handleDeleteSeat,
  onOpenConveneModal,
  t,
}: UniversalSymposiumTabProps) {
  return (
    <div className="space-y-6">
      {/* Header Action Bar */}
      <div className="p-6 rounded-3xl bg-gradient-to-br from-purple-950/60 via-slate-900/90 to-slate-900 border border-purple-800/40 backdrop-blur-md flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <span className="text-2xl">🌌</span>
            <h3 className="text-xl font-black text-white">{t('symposium_admin.title')}</h3>
            <span className="px-2 py-0.5 rounded-full bg-amber-500/20 text-amber-300 text-[10px] font-bold border border-amber-500/30">
              {t('symposium_admin.epoch_badge')}
            </span>
          </div>
          <p className="text-xs text-slate-300 mt-1 max-w-3xl leading-relaxed">
            {t('symposium_admin.desc')}
          </p>
        </div>

        <button
          onClick={onOpenConveneModal}
          className="flex items-center gap-2 px-5 py-3 rounded-2xl bg-gradient-to-r from-purple-600 via-pink-600 to-amber-500 hover:from-purple-500 hover:to-amber-400 text-white text-sm font-bold transition-all shadow-xl shadow-purple-600/30 whitespace-nowrap cursor-pointer"
        >
          <Sparkles className="w-4 h-4" />
          {t('symposium_admin.convene_btn')}
        </button>
      </div>

      {/* Sub-tabs & Isomorphic Council Architecture Hierarchy */}
      <div className="space-y-4">
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
          <div>
            <h4 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
              <Scale className="w-4 h-4 text-purple-400" />
              {t('councils.hierarchy_title')}
            </h4>
            <p className="text-[11px] text-slate-400 mt-0.5">
              {t('councils.hierarchy_subtitle')}
            </p>
          </div>

          {/* Category Filter Pills */}
          <div className="flex items-center gap-1.5 p-1 rounded-xl bg-slate-900 border border-slate-800 flex-wrap text-xs">
            <button
              onClick={() => setCouncilCategoryFilter('ALL')}
              className={`px-3 py-1.5 rounded-lg font-bold transition-all ${
                councilCategoryFilter === 'ALL'
                  ? 'bg-purple-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              {t('councils.filter_all')} ({councils.length})
            </button>
            <button
              onClick={() => setCouncilCategoryFilter('ACADEMIC')}
              className={`px-3 py-1.5 rounded-lg font-bold transition-all ${
                councilCategoryFilter === 'ACADEMIC'
                  ? 'bg-indigo-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              {t('councils.filter_academic')} ({councils.filter(c => c.category === CouncilCategory.Academic || (!c.category && !c.councilCode.startsWith('SYS_'))).length})
            </button>
            <button
              onClick={() => setCouncilCategoryFilter('SECRETARIAT')}
              className={`px-3 py-1.5 rounded-lg font-bold transition-all ${
                councilCategoryFilter === 'SECRETARIAT'
                  ? 'bg-cyan-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              {t('councils.filter_secretariat')} ({councils.filter(c => c.category === CouncilCategory.Secretariat || c.councilCode === 'SYS_SECRETARIAT').length})
            </button>
            <button
              onClick={() => setCouncilCategoryFilter('ARBITRATION')}
              className={`px-3 py-1.5 rounded-lg font-bold transition-all ${
                councilCategoryFilter === 'ARBITRATION'
                  ? 'bg-amber-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              {t('councils.filter_arbitration')} ({councils.filter(c => c.category === CouncilCategory.Arbitration || c.councilCode === 'SYS_ARBITRATION').length})
            </button>
            <button
              onClick={() => setCouncilCategoryFilter('SYSTEM_CORE')}
              className={`px-3 py-1.5 rounded-lg font-bold transition-all ${
                councilCategoryFilter === 'SYSTEM_CORE'
                  ? 'bg-emerald-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              {t('councils.filter_system_core')} ({councils.filter(c => c.category === CouncilCategory.SystemCore || c.councilCode === 'SYS_CORE').length})
            </button>
          </div>
        </div>

        {/* Councils Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {councils
            .filter(c => {
              if (councilCategoryFilter === 'ALL') return true;
              if (councilCategoryFilter === 'ACADEMIC') return c.category === CouncilCategory.Academic || (!c.category && !c.councilCode.startsWith('SYS_'));
              if (councilCategoryFilter === 'SECRETARIAT') return c.category === CouncilCategory.Secretariat || c.councilCode === 'SYS_SECRETARIAT';
              if (councilCategoryFilter === 'ARBITRATION') return c.category === CouncilCategory.Arbitration || c.councilCode === 'SYS_ARBITRATION';
              if (councilCategoryFilter === 'SYSTEM_CORE') return c.category === CouncilCategory.SystemCore || c.councilCode === 'SYS_CORE';
              return true;
            })
            .map((c) => {
              const isSys = c.councilCode.startsWith('SYS_');
              const categoryBadge = 
                c.category === CouncilCategory.Secretariat || c.councilCode === 'SYS_SECRETARIAT'
                  ? { label: t('councils.badge_secretariat'), color: 'bg-cyan-500/20 text-cyan-300 border-cyan-500/40' }
                  : c.category === CouncilCategory.Arbitration || c.councilCode === 'SYS_ARBITRATION'
                  ? { label: t('councils.badge_arbitration'), color: 'bg-amber-500/20 text-amber-300 border-amber-500/40' }
                  : c.category === CouncilCategory.SystemCore || c.councilCode === 'SYS_CORE'
                  ? { label: t('councils.badge_system_core'), color: 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40' }
                  : { label: t('councils.badge_academic'), color: 'bg-indigo-500/20 text-indigo-300 border-indigo-500/40' };

              return (
                <div 
                  key={c.id || c.councilCode} 
                  onDoubleClick={() => setSelectedCouncilForDetail(c)}
                  title="Nhấp đúp chuột để xem toàn diện Bản Tuyên Ngôn & Hồ Sơ Nhận Thức của Viện"
                  className={`p-5 rounded-2xl border transition-all space-y-4 cursor-pointer select-none group ${
                    isSys
                      ? 'bg-slate-900/95 border-purple-500/30 hover:border-purple-500/80 shadow-xl shadow-purple-950/20'
                      : 'bg-slate-900/85 border-slate-800 hover:border-indigo-500/50 shadow-md'
                  }`}
                >
                  {/* Council Header */}
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <div className="flex items-center gap-1.5 flex-wrap">
                        <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full border ${categoryBadge.color}`}>
                          {categoryBadge.label}
                        </span>
                        <span className="text-[10px] font-mono text-purple-400">{c.materializedTaxonomyPath}</span>
                      </div>
                      <h5 className="font-black text-white text-base mt-1 group-hover:text-purple-300 transition-colors">
                        {c.displayName}
                      </h5>
                    </div>
                    <div className="flex items-center gap-1.5 shrink-0">
                      <span className="px-2 py-0.5 rounded-full bg-slate-800 text-slate-300 text-[10px] font-bold font-mono">
                        ELO {c.benchmarkPrestigeElo}
                      </span>
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          setSelectedCouncilForDetail(c);
                        }}
                        className="p-1 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 transition-colors"
                        title="Xem chi tiết học thuật"
                      >
                        <Info className="w-4 h-4" />
                      </button>
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          setEditingCouncil(c);
                        }}
                        className="p-1 text-purple-400 hover:text-white rounded-lg hover:bg-purple-900/40 transition-colors"
                        title="Hiệu chỉnh Tuyên ngôn nhận thức luận & Thông tin Viện"
                      >
                        <Edit3 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>

                  {/* Epistemic Manifesto */}
                  <p className="text-[11px] text-slate-400 italic line-clamp-2 leading-relaxed">
                    "{c.epistemicManifesto}"
                  </p>

                  {/* Council Seats & Role's Wolfpack Swarm */}
                  <div className="pt-3 border-t border-slate-800/80 space-y-2">
                    <div className="flex items-center justify-between text-[10px] font-bold text-slate-400 uppercase tracking-wider">
                      <span>{t('councils.seats_header')}</span>
                      <span>{t('councils.roles_count', { count: c.seats.length })}</span>
                    </div>

                    <div className="space-y-2">
                      {c.seats.map((s, idx) => {
                        const alpha = s.alphaModelCode || s.assignedModelCode || s.preferredModelCode || t('councils.unassigned');
                        let backups: string[] = [];
                        if (s.swarmModelCodesJson) {
                          try {
                            const parsed = JSON.parse(s.swarmModelCodesJson);
                            if (Array.isArray(parsed)) {
                              backups = parsed.filter(m => m !== alpha);
                            }
                          } catch { /* empty */ }
                        }

                        return (
                          <div key={s.id || idx} className="p-2.5 rounded-xl bg-slate-950/70 border border-slate-800/60 space-y-2 hover:border-slate-700 transition-colors">
                            <div className="flex items-center justify-between gap-2">
                              <div className="flex items-center gap-1.5 min-w-0">
                                <span className="text-amber-400 text-xs">🐺</span>
                                <span className="text-slate-200 font-bold text-xs truncate">{s.seatTitle}</span>
                                {s.decisionWeight && s.decisionWeight !== 1.0 && (
                                  <span className="text-[9px] px-1 rounded bg-amber-500/20 text-amber-300 font-mono">
                                    ×{s.decisionWeight}
                                  </span>
                                )}
                              </div>
                              <div className="flex items-center gap-1 shrink-0">
                                <button
                                  type="button"
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    setSelectedSeatForSwarm({ seat: s, councilName: c.displayName });
                                  }}
                                  className="px-2 py-1 rounded-lg bg-indigo-500/20 hover:bg-indigo-500/30 text-indigo-300 text-[10px] font-bold border border-indigo-500/30 transition-all flex items-center gap-1 cursor-pointer"
                                  title="Cấu hình Chức danh, Prompt riêng & Bầy Sói"
                                >
                                  <span>{t('councils.edit_swarm_btn')}</span>
                                </button>
                                <button
                                  type="button"
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    handleDeleteSeat(s.id, s.seatTitle);
                                  }}
                                  className="p-1 text-slate-500 hover:text-rose-400 rounded-lg hover:bg-rose-950/40 transition-colors cursor-pointer"
                                  title="Xóa ghế này khỏi viện"
                                >
                                  <Trash2 className="w-3.5 h-3.5" />
                                </button>
                              </div>
                            </div>

                            {/* Sói Alpha & Backups */}
                            <div className="flex items-center gap-1.5 flex-wrap text-[10px]">
                              <span className="font-semibold text-slate-400">Alpha:</span>
                              <span className="px-1.5 py-0.5 rounded bg-amber-500/20 text-amber-300 font-mono font-bold border border-amber-500/30">
                                {alpha}
                              </span>

                              {backups.length > 0 && (
                                <>
                                  <span className="text-slate-500 ml-1">{t('councils.fallback_label', { count: backups.length })}</span>
                                  {backups.slice(0, 2).map((b, bIdx) => (
                                    <span key={bIdx} className="px-1.5 py-0.5 rounded bg-slate-800 text-slate-300 font-mono">
                                      {b}
                                    </span>
                                  ))}
                                  {backups.length > 2 && (
                                    <span className="text-slate-500 text-[9px]">+{backups.length - 2}</span>
                                  )}
                                </>
                              )}
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                </div>
              );
            })}
        </div>
      </div>
    </div>
  );
}
