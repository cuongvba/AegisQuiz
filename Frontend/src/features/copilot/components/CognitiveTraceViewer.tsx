import React, { useState } from 'react';
import { Brain, ChevronDown, ChevronRight, Sparkles } from 'lucide-react';

interface CognitiveTraceViewerProps {
  thoughtTrace: string;
}

export const CognitiveTraceViewer: React.FC<CognitiveTraceViewerProps> = ({ thoughtTrace }) => {
  const [isExpanded, setIsExpanded] = useState<boolean>(false);

  if (!thoughtTrace || thoughtTrace.trim() === '') return null;

  return (
    <div className="my-2 rounded-lg border border-cyan-800/40 bg-slate-950/80 backdrop-blur-md overflow-hidden transition-all duration-200">
      <button
        type="button"
        onClick={() => setIsExpanded(!isExpanded)}
        className="w-full flex items-center justify-between px-3 py-2 text-xs font-mono text-cyan-400 hover:bg-cyan-950/30 transition-colors"
      >
        <div className="flex items-center gap-2">
          <Brain className="w-3.5 h-3.5 text-cyan-400 animate-pulse" />
          <span className="font-semibold tracking-wide flex items-center gap-1.5">
            Cognitive Deliberation (Suy Luận Ngầm)
            <Sparkles className="w-3 h-3 text-cyan-300" />
          </span>
        </div>
        <div className="flex items-center gap-1 text-slate-400">
          <span className="text-[10px] opacity-75">{isExpanded ? 'Thu gọn' : 'Xem vết tư duy'}</span>
          {isExpanded ? <ChevronDown className="w-3.5 h-3.5" /> : <ChevronRight className="w-3.5 h-3.5" />}
        </div>
      </button>

      {isExpanded && (
        <div className="px-3.5 py-2.5 border-t border-cyan-900/30 bg-slate-900/60 font-mono text-[11px] text-slate-300 leading-relaxed whitespace-pre-wrap selection:bg-cyan-500/30 selection:text-cyan-200">
          {thoughtTrace}
        </div>
      )}
    </div>
  );
};
