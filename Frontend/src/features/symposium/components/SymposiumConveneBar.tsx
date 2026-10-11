import React from 'react';
import { Brain, Flame, Zap, RefreshCw, Layers, Sparkles, Target } from 'lucide-react';
import { SymposiumTeleologyMode } from '@/services/ai-swarm.service';

export interface PresetTopic {
  title: string;
  statement: string;
  councils: string[];
}

export const PRESET_TOPICS: PresetTopic[] = [
  {
    title: 'Thực Trạng Rủi Ro An Toàn Thanh Toán Số, Gian Lận AI & Gói Giải Pháp Quản Trị Hệ Thống',
    statement: 'Gian lận tài chính ứng dụng Deepfake và bẻ khóa xác thực sinh trắc học đang đe dọa các định chế ngân hàng toàn cầu. Đâu là thực trạng khách quan, căn nguyên cốt lõi, những tiến bộ phòng thủ đã đạt được và lộ trình giải pháp hành động phân kỳ cho các ngân hàng?',
    councils: ['ECONOMICS', 'AGI_CYBERNETICS', 'JURISPRUDENCE']
  },
  {
    title: 'Nghịch lý Fermi, Thuyết Rừng Rậm Đen & Bản Chất Văn Minh Vũ Trụ',
    statement: 'Nếu phương trình Drake chỉ ra hàng triệu nền văn minh trong Ngân Hà, tại sao bầu trời đêm lại tĩnh lặng đến rợn ngợp? Liệu các nền văn minh có tất yếu rơi vào trạng thái che giấu đối kháng do sự bùng nổ công nghệ và chuỗi nghi kỵ dây chuyền?',
    councils: ['COSMOLOGY', 'PHILOSOPHY', 'ECONOMICS', 'AGI_CYBERNETICS']
  },
  {
    title: 'Bản Thể Học Ý Thức, Sụp Đổ Hàm Sóng Lượng Tử & Siêu Trí Tuệ Nhân Tạo (AGI)',
    statement: 'Ý thức con người là hiện tượng nổi lên (Emergent Property) từ mạng thần kinh sinh học hay có nguồn gốc từ các quá trình khử kết hợp lượng tử vi mô? Khi AGI đạt ngưỡng siêu thông minh, liệu nó có thực sự trải nghiệm tính chủ quan (Qualia)?',
    councils: ['PHILOSOPHY', 'COSMOLOGY', 'LIFE_SCIENCES', 'AGI_CYBERNETICS']
  },
  {
    title: 'Định Lý Bất Toàn Gödel, Giới Hạn Tính Toán & Trật Tự Kinh Tế Hậu Khan Hiếm',
    statement: 'Liệu một hệ thống kinh tế dựa trên thuật toán tối ưu hóa tự động có thể vượt qua định lý bất toàn Gödel để triệt tiêu mọi khủng hoảng chu kỳ? Làm thế nào phân bổ nguồn lực công bằng trong một thế giới năng lượng và trí tuệ vô hạn?',
    councils: ['MATHEMATICS', 'ECONOMICS', 'AGI_CYBERNETICS', 'PHILOSOPHY']
  },
  {
    title: 'Cơ Chế Tiêu Tán Nhiệt Động Lực Học Của Sự Sống & Sự Tiến Hóa Tổng Hợp Sinh-Số',
    statement: 'Sự sống định nghĩa bản thân bằng cách đảo ngược cục bộ entropy theo lý thuyết của Schrödinger và Prigogine. Khi mã di truyền sinh học hợp nhất với các vi mạch bán dẫn, đâu là ranh giới tiếp theo của sự tiến hóa sinh quyển?',
    councils: ['LIFE_SCIENCES', 'COSMOLOGY', 'AGI_CYBERNETICS']
  }
];

interface SymposiumConveneBarProps {
  topicTitle: string;
  setTopicTitle: (title: string) => void;
  coreProblemStatement: string;
  setCoreProblemStatement: (stmt: string) => void;
  selectedCouncilCodes: string[];
  totalCouncilsCount: number;
  readyCouncilsCount?: number;
  onOpenCouncilSelector: () => void;
  enableAdversarialCritique: boolean;
  setEnableAdversarialCritique: (val: boolean) => void;
  teleologyMode?: SymposiumTeleologyMode;
  setTeleologyMode?: (mode: SymposiumTeleologyMode) => void;
  isConvening: boolean;
  activePhaseIndex: number;
  onConvene: () => void;
  onSelectPreset: (preset: PresetTopic) => void;
}

export function SymposiumConveneBar({
  topicTitle,
  setTopicTitle,
  coreProblemStatement,
  setCoreProblemStatement,
  selectedCouncilCodes,
  totalCouncilsCount,
  readyCouncilsCount,
  onOpenCouncilSelector,
  enableAdversarialCritique,
  setEnableAdversarialCritique,
  teleologyMode = SymposiumTeleologyMode.UniversalHybrid,
  setTeleologyMode,
  isConvening,
  activePhaseIndex,
  onConvene,
  onSelectPreset
}: SymposiumConveneBarProps) {
  return (
    <div className="rounded-3xl p-6 sm:p-7 bg-slate-900/90 border border-slate-800 shadow-2xl backdrop-blur-xl space-y-5">
      {/* Top Header & Presets */}
      <div className="space-y-3">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-xl bg-cyan-500/10 text-cyan-400 border border-cyan-500/20">
              <Brain size={20} />
            </div>
            <div>
              <h2 className="text-base sm:text-lg font-black text-white flex items-center gap-2">
                <span>Trung Tâm Triệu Tập Hội Đồng</span>
                <span className="text-[10px] font-mono px-2 py-0.5 rounded-full bg-cyan-950/80 border border-cyan-800/40 text-cyan-300">
                  Command Center
                </span>
              </h2>
              <p className="text-xs text-slate-400">
                Nhập tiên đề nan giải hoặc chọn chủ đề kinh điển để kích hoạt chuỗi biện chứng
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <label className="flex items-center gap-2 text-xs font-semibold text-slate-300 cursor-pointer select-none bg-slate-950/60 px-3 py-1.5 rounded-xl border border-slate-800">
              <input
                type="checkbox"
                checked={enableAdversarialCritique}
                onChange={(e) => setEnableAdversarialCritique(e.target.checked)}
                className="rounded border-slate-700 bg-slate-900 text-cyan-500 focus:ring-cyan-500 h-3.5 w-3.5"
              />
              <span className="flex items-center gap-1.5">
                <Flame size={13} className="text-rose-400" />
                <span>Phản Biện Đối Kháng</span>
              </span>
            </label>
          </div>
        </div>

        {/* Teleology Mode Selector (Mục tiêu biện chứng linh động) */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2.5 p-3 rounded-2xl bg-slate-950/80 border border-slate-800">
          <div className="flex items-center gap-2">
            <span className="text-[11px] font-black uppercase tracking-wider text-cyan-400 flex items-center gap-1.5">
              <Target size={13} />
              <span>Trọng Tâm Mục Tiêu Hội Thảo:</span>
            </span>
          </div>

          <div className="flex flex-wrap items-center gap-1.5">
            {[
              { mode: SymposiumTeleologyMode.UniversalHybrid, label: '🛡️ Toàn Năng Lai Ghép', desc: 'Thực trạng + Giải pháp + Tiên đề + Nan đề mở' },
              { mode: SymposiumTeleologyMode.EmpiricalActionable, label: '📊 Thực Nghiệm & Giải Pháp', desc: 'Thực trạng -> Nguyên nhân -> Điểm tiến bộ -> Lộ trình hành động' },
              { mode: SymposiumTeleologyMode.EpistemicGenesis, label: '💡 Tiên Đề & Nan Đề Mở', desc: 'Hạt nhân tiên đề mới & Các nghịch lý mở của thế giới' },
              { mode: SymposiumTeleologyMode.AutoDetect, label: '🧠 Tự Động (AI Auto)', desc: 'Tự suy luận trọng tâm theo đề tài' }
            ].map((item) => {
              const active = teleologyMode === item.mode;
              return (
                <button
                  key={item.mode}
                  type="button"
                  onClick={() => setTeleologyMode?.(item.mode)}
                  className={`px-3 py-1.5 rounded-xl text-xs font-bold transition flex items-center gap-1.5 cursor-pointer border ${
                    active
                      ? 'bg-cyan-500/20 text-cyan-300 border-cyan-500/50 shadow-sm shadow-cyan-500/20'
                      : 'bg-slate-900/90 text-slate-400 border-slate-800 hover:text-slate-200 hover:border-slate-700'
                  }`}
                  title={item.desc}
                >
                  <span>{item.label}</span>
                </button>
              );
            })}
          </div>
        </div>

        {/* Preset Topic Pills */}
        <div className="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-thin scrollbar-thumb-slate-800">
          <span className="text-[11px] font-bold uppercase tracking-wider text-slate-500 flex items-center gap-1 flex-shrink-0">
            <Sparkles size={11} className="text-cyan-400" /> Gợi ý:
          </span>
          {PRESET_TOPICS.map((preset, idx) => (
            <button
              key={idx}
              onClick={() => onSelectPreset(preset)}
              className="text-xs px-3 py-1.5 rounded-xl bg-slate-950 hover:bg-slate-800/90 border border-slate-800 hover:border-cyan-500/40 text-slate-300 hover:text-cyan-300 transition whitespace-nowrap cursor-pointer flex-shrink-0"
              title={preset.statement}
            >
              <span className="text-cyan-500 font-bold mr-1">#{idx + 1}</span>
              <span>{preset.title.split(',')[0]}</span>
            </button>
          ))}
        </div>
      </div>

      {/* Input Fields */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-3.5">
        <div className="lg:col-span-5 space-y-1.5">
          <label className="text-xs font-bold uppercase tracking-wider text-slate-300 block">
            Tiêu Đề Tranh Biện Học Thuật
          </label>
          <input
            type="text"
            value={topicTitle}
            onChange={(e) => setTopicTitle(e.target.value)}
            placeholder="Nhập đề tài hoặc giả thuyết cần hội đồng mổ xẻ..."
            className="w-full px-4 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-white placeholder-slate-500 text-xs sm:text-sm focus:outline-none focus:border-cyan-500 transition"
          />
        </div>

        <div className="lg:col-span-7 space-y-1.5">
          <label className="text-xs font-bold uppercase tracking-wider text-slate-300 block">
            Tiên Đề Cốt Lõi & Nghịch Lý Cần Khảo Nghiệm (Problem Statement)
          </label>
          <textarea
            rows={2}
            value={coreProblemStatement}
            onChange={(e) => setCoreProblemStatement(e.target.value)}
            placeholder="Mô tả chi tiết nghịch lý, ranh giới lý thuyết hoặc hiện tượng thực nghiệm cần liên viện hòa giải..."
            className="w-full px-4 py-2 rounded-xl bg-slate-950 border border-slate-800 text-white placeholder-slate-500 text-xs sm:text-sm focus:outline-none focus:border-cyan-500 transition resize-none"
          />
        </div>
      </div>

      {/* Actions Bar */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 pt-2 border-t border-slate-800/80">
        <div className="flex flex-wrap items-center gap-2">
          <button
            onClick={onOpenCouncilSelector}
            className="px-3.5 py-2 rounded-xl bg-slate-950 hover:bg-slate-850 border border-slate-800 hover:border-slate-700 text-xs font-medium text-slate-300 flex items-center gap-2 transition cursor-pointer"
          >
            <Layers size={14} className="text-cyan-400" />
            <span>
              Hội Đồng Tham Gia:{' '}
              {selectedCouncilCodes.length === 0 ? (
                <strong className="text-cyan-300 font-bold">✨ Tự Động (AI Dynamic)</strong>
              ) : (
                <strong className="text-cyan-300 font-bold">{selectedCouncilCodes.length} / {totalCouncilsCount} Viện</strong>
              )}
            </span>
            <span className="text-[10px] px-1.5 py-0.5 rounded bg-slate-800 text-slate-400 ml-1">
              Tùy chỉnh
            </span>
          </button>

          {/* Badge Sẵn Sàng & Phân Vai Động Theo Không - Thời Gian */}
          <div className="hidden md:flex items-center gap-2">
            <span className="text-[10px] font-semibold px-2.5 py-1 rounded-xl bg-emerald-950/60 border border-emerald-500/30 text-emerald-300 flex items-center gap-1">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
              <span>{readyCouncilsCount ?? totalCouncilsCount}/{totalCouncilsCount} Viện Sẵn Sàng</span>
            </span>
            <span className="text-[10px] font-mono px-2.5 py-1 rounded-xl bg-cyan-950/60 border border-cyan-800/40 text-cyan-300 flex items-center gap-1">
              <span>🧭 Phân vai Động học Không - Thời Gian</span>
            </span>
          </div>
        </div>

        <button
          onClick={onConvene}
          disabled={isConvening || !topicTitle.trim()}
          className="px-8 py-3 rounded-2xl bg-gradient-to-r from-cyan-500 via-blue-600 to-violet-600 hover:from-cyan-400 hover:to-violet-500 text-slate-950 font-black text-xs sm:text-sm tracking-wide shadow-lg shadow-cyan-500/20 flex items-center justify-center gap-2 transition-all transform hover:scale-[1.01] active:scale-[0.99] disabled:opacity-50 cursor-pointer"
        >
          {isConvening ? (
            <>
              <RefreshCw className="animate-spin text-slate-950" size={16} />
              <span>ĐANG BIỆN CHỨNG (PHASE {activePhaseIndex}/6)...</span>
            </>
          ) : (
            <>
              <Zap size={16} className="text-slate-950 fill-current" />
              <span>TRIỆU TẬP ĐẠI HỘI THẢO HÀN LÂM</span>
            </>
          )}
        </button>
      </div>
    </div>
  );
}
