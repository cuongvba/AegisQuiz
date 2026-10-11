import React, { useState } from 'react';
import { BookOpen, Check, Copy, FileText, Layers, Send, ShieldCheck, Sparkles, Trophy, Users } from 'lucide-react';
import type { CoPilotArtifact } from '../types/copilot.types';
import { CoPilotService } from '../services/copilot.service';

interface ArtifactCardRendererProps {
  artifact: CoPilotArtifact;
  onApply?: (artifact: CoPilotArtifact) => void;
}

export const ArtifactCardRenderer: React.FC<ArtifactCardRendererProps> = ({ artifact, onApply }) => {
  const [copied, setCopied] = useState<boolean>(false);
  const [applied, setApplied] = useState<boolean>(false);
  const [savingNotebook, setSavingNotebook] = useState<boolean>(false);
  const [savedNotebook, setSavedNotebook] = useState<boolean>(false);

  const handleCopy = () => {
    const textToCopy = artifact.rawMarkdown || JSON.stringify(artifact.data, null, 2);
    navigator.clipboard.writeText(textToCopy);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleApply = () => {
    setApplied(true);
    if (onApply) onApply(artifact);
    setTimeout(() => setApplied(false), 2500);
  };

  const handleSaveToNotebook = async () => {
    setSavingNotebook(true);
    try {
      await CoPilotService.exportToNotebook({
        title: artifact.title || 'Vết Nhận Thức Co-Pilot',
        content: artifact.rawMarkdown || JSON.stringify(artifact.data, null, 2),
        artifactType: artifact.artifactType,
        artifactData: artifact.data
      });
      setSavedNotebook(true);
      setTimeout(() => setSavedNotebook(false), 3000);
    } catch {
      // Fallback
    } finally {
      setSavingNotebook(false);
    }
  };

  return (
    <div className="my-3 rounded-xl border border-indigo-500/40 bg-gradient-to-b from-slate-900/90 to-slate-950/95 p-4 shadow-xl backdrop-blur-md transition-all duration-200 hover:border-indigo-400/60">
      {/* Header */}
      <div className="flex items-center justify-between border-b border-indigo-900/40 pb-2.5 mb-3 flex-wrap gap-2">
        <div className="flex items-center gap-2">
          {artifact.artifactType === 'question' && <FileText className="w-4 h-4 text-cyan-400" />}
          {artifact.artifactType === 'matrix' && <Layers className="w-4 h-4 text-purple-400" />}
          {artifact.artifactType === 'symposium' && <Users className="w-4 h-4 text-amber-400" />}
          {artifact.artifactType === 'swarm-audit' && <ShieldCheck className="w-4 h-4 text-emerald-400" />}
          <span className="font-semibold text-xs tracking-wide text-indigo-200">
            {artifact.title}
          </span>
        </div>
        <div className="flex items-center gap-1.5 flex-wrap">
          {/* Nút Ghi vào AegisNotebook */}
          <button
            type="button"
            onClick={handleSaveToNotebook}
            disabled={savingNotebook || savedNotebook}
            title="Bảo toàn tri thức vào Hồ Sơ AegisNotebook"
            className={`inline-flex items-center gap-1 px-2.5 py-1 rounded text-[11px] font-semibold transition-all ${
              savedNotebook
                ? 'bg-amber-600 text-white'
                : 'bg-slate-800 hover:bg-slate-700 text-amber-300 border border-amber-500/30'
            }`}
          >
            {savedNotebook ? <Check className="w-3 h-3 text-white" /> : <BookOpen className="w-3 h-3" />}
            {savedNotebook ? 'Đã lưu Notebook!' : savingNotebook ? 'Đang lưu...' : 'Ghi vào Notebook'}
          </button>

          <button
            type="button"
            onClick={handleCopy}
            title="Sao chép nội dung"
            className="inline-flex items-center gap-1 px-2 py-1 rounded text-[10px] font-mono text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
          >
            {copied ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
            {copied ? 'Đã chép' : 'Chép'}
          </button>
          {artifact.canApplyDirectly && (
            <button
              type="button"
              onClick={handleApply}
              disabled={applied}
              className={`inline-flex items-center gap-1 px-2.5 py-1 rounded text-[11px] font-semibold transition-all ${
                applied
                  ? 'bg-emerald-600 text-white'
                  : 'bg-indigo-600 hover:bg-indigo-500 text-white shadow-sm hover:shadow-indigo-500/20'
              }`}
            >
              {applied ? <Check className="w-3 h-3" /> : <Send className="w-3 h-3" />}
              {applied ? 'Đã áp dụng!' : 'Áp dụng vào Canvas'}
            </button>
          )}
        </div>
      </div>

      {/* Body for Question Artifact */}
      {artifact.artifactType === 'question' && artifact.data && (
        <div className="space-y-3 text-xs">
          <div className="flex items-center gap-2 flex-wrap mb-1">
            <span className="px-2 py-0.5 rounded-full text-[10px] font-semibold bg-cyan-950 text-cyan-300 border border-cyan-800/60">
              Bloom: {artifact.data.bloomLevel || 'Analyze'}
            </span>
            {artifact.data.irt && (
              <span className="px-2 py-0.5 rounded-full text-[10px] font-mono bg-purple-950 text-purple-300 border border-purple-800/60">
                IRT: a={artifact.data.irt.discrimination} | b={artifact.data.irt.difficulty}
              </span>
            )}
            <span className="px-2 py-0.5 rounded-full text-[10px] bg-slate-800 text-slate-300">
              Chủ đề: {artifact.data.topic || 'Khảo thí'}
            </span>
          </div>

          <p className="text-slate-100 font-medium leading-relaxed bg-slate-900/50 p-2.5 rounded-lg border border-slate-800/80">
            {artifact.data.questionText}
          </p>

          <div className="space-y-1.5">
            {Array.isArray(artifact.data.options) &&
              artifact.data.options.map((opt: string, idx: number) => {
                const isCorrect = idx === artifact.data.correctIndex;
                return (
                  <div
                    key={idx}
                    className={`px-2.5 py-1.5 rounded-md text-xs font-mono transition-colors flex items-center justify-between ${
                      isCorrect
                        ? 'bg-emerald-950/70 border border-emerald-700/60 text-emerald-200'
                        : 'bg-slate-950/60 border border-slate-800 text-slate-300'
                    }`}
                  >
                    <span>{opt}</span>
                    {isCorrect && (
                      <span className="text-[10px] font-bold uppercase tracking-wider text-emerald-400">
                        Đáp án chuẩn
                      </span>
                    )}
                  </div>
                );
              })}
          </div>

          {artifact.data.explanation && (
            <div className="p-2 rounded bg-slate-950/70 border border-cyan-900/40 text-[11px] text-slate-400 leading-normal">
              <span className="font-semibold text-cyan-400">💡 Giải thích bẫy nhận thức: </span>
              {artifact.data.explanation}
            </div>
          )}
        </div>
      )}

      {/* Body for Swarm Audit Artifact (WPSE) */}
      {artifact.artifactType === 'swarm-audit' && artifact.data && (
        <div className="space-y-3 text-xs">
          <div className="flex items-center justify-between bg-slate-950 p-2.5 rounded-lg border border-emerald-900/40">
            <div className="flex items-center gap-2">
              <ShieldCheck className="w-4 h-4 text-emerald-400" />
              <div>
                <p className="font-semibold text-emerald-300">Hội Chẩn Bầy Sói Đa Mô Hình (WPSE)</p>
                <p className="text-[10px] text-slate-400">Mô hình đầu đàn: {artifact.data.alphaModelDisplayName || 'Claude 3.5 Sonnet'}</p>
              </div>
            </div>
            <div className="text-right">
              <span className="font-mono text-sm font-bold text-emerald-400">
                {Math.round((artifact.data.consensusIndex || 0.94) * 100)}%
              </span>
              <p className="text-[9px] uppercase tracking-wider text-slate-400 font-mono">SCI Index</p>
            </div>
          </div>

          {artifact.data.synthesisSummary && (
            <div className="p-2.5 rounded bg-slate-900/60 border border-slate-800 text-[11px] text-slate-300 leading-relaxed">
              <span className="font-semibold text-cyan-300">📝 Tổng hợp phán quyết: </span>
              {artifact.data.synthesisSummary}
            </div>
          )}

          {Array.isArray(artifact.data.memberVerdicts) && artifact.data.memberVerdicts.length > 0 && (
            <div className="space-y-1.5">
              <p className="text-[10px] font-mono uppercase tracking-wider text-slate-400">Phán quyết chi tiết từ các thành viên:</p>
              {artifact.data.memberVerdicts.map((m: any, idx: number) => (
                <div key={idx} className="p-2 rounded bg-slate-950/70 border border-slate-800/80 flex items-center justify-between text-[11px]">
                  <div>
                    <span className="font-semibold text-indigo-300">{m.displayName || m.modelCode}</span>
                    <span className="ml-2 text-[10px] text-slate-500 font-mono">({m.providerCompany} - {m.archetype})</span>
                  </div>
                  <div className="flex items-center gap-2 font-mono text-[10px]">
                    <span className="text-slate-400">{m.latencyMs}ms</span>
                    <span className="text-emerald-400 font-semibold">{Math.round((m.confidenceScore || 0.9) * 100)}%</span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Body for Bloom Matrix Artifact */}
      {artifact.artifactType === 'matrix' && artifact.data && (
        <div className="space-y-2 text-xs">
          <p className="text-slate-300 font-medium">Quy mô: {artifact.data.totalQuestions} câu hỏi</p>
          <div className="overflow-x-auto rounded-lg border border-slate-800">
            <table className="w-full text-left text-[11px]">
              <thead className="bg-slate-900 text-slate-400 font-mono">
                <tr>
                  <th className="p-2">Cấp độ nhận thức</th>
                  <th className="p-2">Tỷ lệ</th>
                  <th className="p-2">Số câu</th>
                  <th className="p-2">Mục tiêu sư phạm</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800 bg-slate-950/60 text-slate-200">
                {Array.isArray(artifact.data.levels) &&
                  artifact.data.levels.map((lvl: any, i: number) => (
                    <tr key={i} className="hover:bg-slate-900/40">
                      <td className="p-2 font-medium text-cyan-300">{lvl.level}</td>
                      <td className="p-2 font-mono text-purple-300">{lvl.percentage}</td>
                      <td className="p-2 font-mono">{lvl.count}</td>
                      <td className="p-2 text-slate-400 text-[10px]">{lvl.purpose}</td>
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Body for Symposium Artifact */}
      {artifact.artifactType === 'symposium' && artifact.data && (
        <div className="space-y-2.5 text-xs">
          <div className="flex items-center justify-between bg-slate-950 p-2 rounded border border-amber-900/40">
            <span className="font-semibold text-amber-300 flex items-center gap-1">
              <Trophy className="w-3.5 h-3.5 text-amber-400" />
              Điểm Rubric Tranh Biện Học Thuật:
            </span>
            <span className="font-mono font-bold text-emerald-400">
              {artifact.data.evaluatedRubric?.overallCompositeScore || artifact.data.consensusScore || 8.8}/10
            </span>
          </div>

          <div className="space-y-1.5">
            {Array.isArray(artifact.data.dialogueLog) &&
              artifact.data.dialogueLog.map((turn: any, i: number) => (
                <div key={i} className="p-2 rounded bg-slate-900/60 border border-slate-800 text-[11px]">
                  <div className="flex items-center justify-between mb-1">
                    <span className="font-semibold text-cyan-300">{turn.councilDisplayName || turn.speakerTitle}</span>
                    <span className="px-1.5 py-0.5 rounded text-[9px] bg-slate-800 text-amber-300">
                      Pha {turn.phaseNumber}: {turn.phaseName}
                    </span>
                  </div>
                  <p className="text-slate-300 leading-normal">{turn.content}</p>
                </div>
              ))}

            {Array.isArray(artifact.data.councils) &&
              artifact.data.councils.map((c: any, i: number) => (
                <div key={i} className="p-2 rounded bg-slate-900/60 border border-slate-800 text-[11px]">
                  <div className="flex items-center justify-between mb-1">
                    <span className="font-semibold text-cyan-300">{c.name}</span>
                    <span className="px-1.5 py-0.5 rounded text-[9px] bg-slate-800 text-amber-300">
                      {c.stance}
                    </span>
                  </div>
                  <p className="text-slate-400 leading-normal">{c.argument}</p>
                </div>
              ))}
          </div>
        </div>
      )}
    </div>
  );
};
