import { Sparkles } from 'lucide-react';
import type { SymposiumProceedingsResult, SymposiumDialogueTurn } from '@/services/ai-swarm.service';

interface SymposiumProceedingsCardProps {
  symposiumResult: SymposiumProceedingsResult | null;
  t: (key: string, opts?: any) => string;
}

export function SymposiumProceedingsCard({ symposiumResult, t }: SymposiumProceedingsCardProps) {
  if (!symposiumResult) return null;

  return (
    <div className="p-6 rounded-3xl bg-slate-900 border border-purple-500/40 space-y-4 shadow-xl">
      <div className="flex items-center justify-between pb-3 border-b border-slate-800">
        <div>
          <span className="text-[10px] font-bold px-2 py-0.5 rounded bg-purple-500/20 text-purple-300 font-mono">
            {t('symposium_admin.proceedings_badge')}
          </span>
          <h4 className="font-bold text-white text-base mt-1">{symposiumResult.topicTitle}</h4>
        </div>
        <div className="flex items-center gap-2 text-xs">
          <span className="text-slate-400 font-mono">
            Phiên #{symposiumResult.sessionId?.slice(0, 8)}
          </span>
          <span className="text-emerald-400 font-bold font-mono">
            {symposiumResult.totalSessionDurationMs}ms
          </span>
        </div>
      </div>

      {/* Epoch Synthesis Narrative */}
      {symposiumResult.unifiedMetatheory && (
        <div className="p-4 rounded-xl bg-purple-950/20 border border-purple-800/30 text-xs text-slate-200 leading-relaxed">
          <span className="font-bold text-purple-300 block mb-1">{t('symposium_admin.epoch_synthesis')}</span>
          {symposiumResult.unifiedMetatheory}
        </div>
      )}

      {/* Deliberation Turns */}
      {symposiumResult.dialogueLog && symposiumResult.dialogueLog.length > 0 && (
        <div className="space-y-3">
          <h5 className="text-xs font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
            <Sparkles className="w-3.5 h-3.5 text-amber-400" />
            {t('symposium_admin.deliberation_turns')} ({symposiumResult.dialogueLog.length})
          </h5>
          <div className="space-y-2 max-h-96 overflow-y-auto pr-1">
            {symposiumResult.dialogueLog.map((turn: SymposiumDialogueTurn, idx: number) => (
              <div key={idx} className="p-3.5 rounded-xl bg-slate-950/80 border border-slate-800 space-y-1.5">
                <div className="flex items-center justify-between text-xs">
                  <div className="flex items-center gap-2">
                    <span className="px-2 py-0.5 rounded bg-purple-500/20 text-purple-300 font-bold text-[10px]">
                      Phase {turn.phaseNumber}: {turn.phaseName}
                    </span>
                    <span className="font-bold text-white">{turn.speakerTitle}</span>
                  </div>
                  <span className="text-[10px] font-mono text-slate-400">{turn.latencyMs}ms</span>
                </div>
                <p className="text-xs text-slate-300 leading-relaxed">{turn.content}</p>
                {turn.exposedFallacies && turn.exposedFallacies.length > 0 && (
                  <div className="flex items-center gap-1.5 pt-1">
                    <span className="text-[10px] text-rose-400 font-bold">{t('symposium_admin.exposed_fallacies')}</span>
                    {turn.exposedFallacies.map((f: string, fIdx: number) => (
                      <span key={fIdx} className="px-1.5 py-0.5 rounded bg-rose-500/10 text-rose-300 text-[10px]">
                        {f}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Final Consensus Declaration */}
      {symposiumResult.finalConsensusDeclaration && (
        <div className="p-4 rounded-xl bg-emerald-950/40 border border-emerald-600/40">
          <span className="text-xs font-bold text-emerald-300 block mb-1">{t('symposium_admin.final_declaration')}</span>
          <p className="text-xs text-emerald-100 font-serif leading-relaxed italic">
            "{symposiumResult.finalConsensusDeclaration}"
          </p>
        </div>
      )}
    </div>
  );
}
