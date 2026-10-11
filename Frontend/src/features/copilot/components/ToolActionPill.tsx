import React from 'react';
import { Activity, CheckCircle2, Clock, Zap } from 'lucide-react';
import type { ToolCall } from '../types/copilot.types';

interface ToolActionPillProps {
  tool: ToolCall;
}

export const ToolActionPill: React.FC<ToolActionPillProps> = ({ tool }) => {
  return (
    <div className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-mono bg-slate-900/90 border border-slate-700/80 text-slate-300 shadow-sm transition-all hover:border-cyan-500/50">
      <Zap className="w-3 h-3 text-amber-400" />
      <span className="font-medium text-cyan-300">{tool.toolName}</span>
      <span className="text-slate-500">•</span>
      <span className="text-slate-400 text-[10px] truncate max-w-[180px]">{tool.description}</span>
      {tool.durationMs > 0 && (
        <span className="inline-flex items-center gap-0.5 text-[9px] text-slate-500 font-normal">
          <Clock className="w-2.5 h-2.5" />
          {tool.durationMs}ms
        </span>
      )}
      {tool.status === 'completed' && (
        <CheckCircle2 className="w-3 h-3 text-emerald-400 ml-0.5" />
      )}
      {tool.status === 'running' && (
        <Activity className="w-3 h-3 text-cyan-400 animate-spin ml-0.5" />
      )}
    </div>
  );
};
