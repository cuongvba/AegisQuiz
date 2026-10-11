import { Cpu, X } from 'lucide-react';

interface DocxCigRulesPanelProps {
  showCigRules: boolean;
  setShowCigRules: (val: boolean) => void;
}

export function DocxCigRulesPanel({ showCigRules, setShowCigRules }: DocxCigRulesPanelProps) {
  if (!showCigRules) return null;

  return (
    <div className="bg-slate-950/95 border border-purple-500/30 rounded-2xl p-4 shadow-xl shadow-black/40 space-y-3 animate-in fade-in duration-150 mb-2.5">
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-800/80 pb-2.5">
        <div className="flex items-center gap-2">
          <span className="p-1.5 rounded-lg bg-purple-500/20 text-purple-400">
            <Cpu size={16} />
          </span>
          <div>
            <h4 className="text-xs font-black text-slate-100 uppercase tracking-wider flex items-center gap-2">
              Universal Cognitive Ingestion Engine (CIG v6.0)
              <span className="text-[10px] font-semibold bg-purple-950 text-purple-400 border border-purple-800 px-2 py-0.5 rounded-full">
                ✨ Tự Động Nhận Diện Đề Thi
              </span>
            </h4>
            <p className="text-[11px] text-slate-400">
              Hệ thống tự động kích hoạt bộ quy tắc ngữ pháp nhận thức phù hợp nhất cho từng câu hỏi (GDPT 2025, THCS KNTT/CTM, Tiếng Anh Global, GPLX, SAT, Gaokao, IELTS...)
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setShowCigRules(false)}
            className="p-1 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 cursor-pointer"
          >
            <X size={14} />
          </button>
        </div>
      </div>

      {/* Rules Grid & AI Autopilot Quick Info */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 pt-1">
        <div className="bg-slate-900/80 border border-slate-800 p-2.5 rounded-xl space-y-1">
          <div className="text-[10px] font-bold text-teal-400 flex items-center gap-1">
            📐 THCS Toán KNTT/CTM
          </div>
          <p className="text-[10px] text-slate-400">Nhận diện câu trắc nghiệm & bài tự luận MathType</p>
          <span className="text-[9px] bg-teal-950 text-teal-300 px-1.5 py-0.5 rounded border border-teal-800/60 font-mono">VN_THCS_MATH</span>
        </div>
        <div className="bg-slate-900/80 border border-slate-800 p-2.5 rounded-xl space-y-1">
          <div className="text-[10px] font-bold text-indigo-400 flex items-center gap-1">
            🇬🇧 THCS Tiếng Anh Global
          </div>
          <p className="text-[10px] text-slate-400">Đề 4 phần Listening, Language, Reading, Writing</p>
          <span className="text-[9px] bg-indigo-950 text-indigo-300 px-1.5 py-0.5 rounded border border-indigo-800/60 font-mono">VN_THCS_ENG</span>
        </div>
        <div className="bg-slate-900/80 border border-slate-800 p-2.5 rounded-xl space-y-1">
          <div className="text-[10px] font-bold text-purple-400 flex items-center gap-1">
            🎯 GDPT 2025 Đa Mệnh Đề
          </div>
          <p className="text-[10px] text-slate-400">Trắc nghiệm Đúng/Sai 4 ý a, b, c, d & Điền số</p>
          <span className="text-[9px] bg-purple-950 text-purple-300 px-1.5 py-0.5 rounded border border-purple-800/60 font-mono">GDPT_2025</span>
        </div>
        <div className="bg-slate-900/80 border border-slate-800 p-2.5 rounded-xl space-y-1">
          <div className="text-[10px] font-bold text-amber-400 flex items-center gap-1">
            🤖 AI Rule Autopilot
          </div>
          <p className="text-[10px] text-slate-400">Học ngữ pháp đề dị biệt 0-code qua Gemini AI</p>
          <span className="text-[9px] bg-amber-950 text-amber-300 px-1.5 py-0.5 rounded border border-amber-800/60 font-mono">Zero-Downtime</span>
        </div>
      </div>
    </div>
  );
}
