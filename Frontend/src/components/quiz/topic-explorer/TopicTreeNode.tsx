import React from 'react';
import {
  Folder,
  FolderOpen,
  ChevronRight,
  ChevronDown,
  Globe2,
  Building2,
  CheckSquare,
  Square,
} from 'lucide-react';
import type {
  CognitiveTopicNodeDto,
  PracticeTopicConfig,
} from '@/types/quiz';

interface TopicTreeNodeProps {
  node: CognitiveTopicNodeDto;
  isExpanded: boolean;
  onToggleExpand: (code: string) => void;
  selStatus: { all: boolean; some: boolean };
  selectedConfig?: PracticeTopicConfig;
  onSelectTree: (node: CognitiveTopicNodeDto, select: boolean) => void;
  onUpdateConfig: (topicKey: string, patch: Partial<PracticeTopicConfig>) => void;
  childrenNodes?: React.ReactNode;
}

export const TopicTreeNode: React.FC<TopicTreeNodeProps> = ({
  node,
  isExpanded,
  onToggleExpand,
  selStatus,
  selectedConfig,
  onSelectTree,
  onUpdateConfig,
  childrenNodes,
}) => {
  const hasChildren = Array.isArray(node.children) && node.children.length > 0;

  return (
    <div
      className={`rounded-xl border transition-all ${
        selStatus.all || selStatus.some
          ? 'border-cyan-400/60 bg-cyan-950/20 shadow-xs'
          : 'border-slate-800 bg-slate-900/40 hover:border-slate-700'
      }`}
    >
      {/* Node Header Row */}
      <div className="flex items-center justify-between p-3">
        <div className="flex items-center gap-2.5 min-w-0 flex-1">
          {hasChildren ? (
            <button
              type="button"
              onClick={() => onToggleExpand(node.code)}
              className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg border border-slate-800 bg-slate-800/80 text-slate-300 hover:bg-slate-700 transition"
              title={isExpanded ? 'Thu gọn' : 'Mở rộng'}
            >
              {isExpanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
            </button>
          ) : (
            <div className="w-7 shrink-0" />
          )}

          {/* Checkbox select */}
          <button
            type="button"
            onClick={() => onSelectTree(node, !selStatus.all)}
            className="flex items-center gap-2 text-left cursor-pointer group"
          >
            <span className="shrink-0 text-cyan-400">
              {selStatus.all ? (
                <CheckSquare className="h-5 w-5 text-cyan-400" />
              ) : selStatus.some ? (
                <span className="flex h-5 w-5 items-center justify-center rounded border-2 border-cyan-500 bg-cyan-600 text-white text-[10px] font-black">
                  -
                </span>
              ) : (
                <Square className="h-5 w-5 text-slate-600 group-hover:text-slate-500" />
              )}
            </span>

            {hasChildren ? (
              <FolderOpen className={`h-5 w-5 shrink-0 ${selStatus.all || selStatus.some ? 'text-amber-400' : 'text-slate-500'}`} />
            ) : (
              <Folder className={`h-5 w-5 shrink-0 ${selStatus.all ? 'text-cyan-400' : 'text-slate-500'}`} />
            )}
          </button>

          {/* Title & Metadata */}
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-2 flex-wrap">
              <span
                className="text-xs font-black text-slate-200 hover:text-cyan-400 cursor-pointer"
                onClick={() => (hasChildren ? onToggleExpand(node.code) : onSelectTree(node, !selStatus.all))}
              >
                {node.name}
              </span>

              {node.scope === 'COMMUNITY' ? (
                <span className="inline-flex items-center gap-0.5 rounded bg-emerald-950/60 px-1.5 py-0.2 text-[10px] font-bold text-emerald-300 border border-emerald-500/30">
                  <Globe2 className="h-2.5 w-2.5" /> Cộng đồng
                </span>
              ) : (
                <span className="inline-flex items-center gap-0.5 rounded bg-indigo-950/60 px-1.5 py-0.2 text-[10px] font-bold text-indigo-300 border border-indigo-500/30">
                  <Building2 className="h-2.5 w-2.5" /> Doanh nghiệp
                </span>
              )}

              {hasChildren && (
                <span className="rounded bg-sky-950/60 px-2 py-0.2 text-[10px] font-black text-sky-300 border border-sky-500/30">
                  {(node.children || []).length} mục con
                </span>
              )}
            </div>

            <p className="text-[11px] text-slate-400 truncate mt-0.5">
              Mã: <code className="font-mono text-cyan-300">{node.code}</code> • {(node.questionCount ?? 0).toLocaleString()} câu hỏi
            </p>
          </div>
        </div>

        {/* 1-touch select button for parent folders */}
        {hasChildren && (
          <button
            type="button"
            onClick={() => onSelectTree(node, !selStatus.all)}
            className={`rounded-xl px-2.5 py-1 text-[11px] font-bold transition border ${
              selStatus.all
                ? 'bg-rose-950/60 text-rose-300 border-rose-500/40 hover:bg-rose-900/80'
                : 'bg-slate-800 text-cyan-300 border-cyan-500/40 hover:bg-cyan-950/80'
            }`}
          >
            {selStatus.all ? 'Bỏ chọn cả cây' : '1-Chạm Chọn Hết'}
          </button>
        )}
      </div>

      {/* Inline Configuration for selected leaf */}
      {!hasChildren && selectedConfig && (
        <div className="border-t border-slate-800 bg-slate-950/60 p-3 rounded-b-xl flex items-center gap-4 text-xs">
          <label className="flex items-center gap-2 text-slate-300">
            <span>Số lượng câu:</span>
            <input
              type="number"
              min={1}
              value={selectedConfig.numberOfQuestions}
              onChange={(e) => onUpdateConfig(node.code, { numberOfQuestions: Math.max(1, Number(e.target.value) || 1) })}
              className="w-16 h-7 rounded border border-slate-700 bg-slate-900 px-2 text-white text-center font-bold"
            />
          </label>
          <label className="flex items-center gap-1.5 text-slate-300 cursor-pointer">
            <input
              type="checkbox"
              checked={selectedConfig.isRandom}
              onChange={(e) => onUpdateConfig(node.code, { isRandom: e.target.checked })}
              className="rounded border-slate-700 bg-slate-900 text-cyan-500"
            />
            <span>Xáo trộn</span>
          </label>
        </div>
      )}

      {/* Children Container */}
      {hasChildren && isExpanded && childrenNodes && (
        <div className="border-t border-slate-800/80 p-3 pl-6 space-y-2.5 bg-slate-950/30 rounded-b-xl">
          {childrenNodes}
        </div>
      )}
    </div>
  );
};
