import { Sparkles, X, Check } from 'lucide-react';
import type { UniversalAiCouncilDto } from '@/services/ai-swarm.service';

interface ConveneSymposiumModalProps {
  isOpen: boolean;
  onClose: () => void;
  conveneTopic: string;
  setConveneTopic: (val: string) => void;
  conveneProblem: string;
  setConveneProblem: (val: string) => void;
  selectedCouncilCodes: string[];
  setSelectedCouncilCodes: (codes: string[] | ((prev: string[]) => string[])) => void;
  councils: UniversalAiCouncilDto[];
  convening: boolean;
  handleConveneSymposium: () => void;
  t: (key: string, opts?: any) => string;
}

export function ConveneSymposiumModal({
  isOpen,
  onClose,
  conveneTopic,
  setConveneTopic,
  conveneProblem,
  setConveneProblem,
  selectedCouncilCodes,
  setSelectedCouncilCodes,
  councils,
  convening,
  handleConveneSymposium,
  t,
}: ConveneSymposiumModalProps) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4">
      <div className="bg-slate-900 border border-purple-800/60 rounded-3xl w-full max-w-2xl max-h-[90vh] overflow-y-auto p-6 space-y-5 shadow-2xl">
        <div className="flex items-center justify-between pb-3 border-b border-slate-800">
          <div className="flex items-center gap-2">
            <Sparkles className="w-5 h-5 text-amber-300" />
            <h3 className="font-bold text-white text-base">{t('symposium_admin.convene_modal_title')}</h3>
          </div>
          <button onClick={onClose} className="text-slate-400 hover:text-white cursor-pointer">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="space-y-4 text-xs">
          <div>
            <label className="text-slate-300 font-semibold block mb-1">{t('symposium_admin.topic_label')}</label>
            <input
              type="text"
              required
              placeholder={t('symposium_admin.topic_placeholder')}
              value={conveneTopic}
              onChange={(e) => setConveneTopic(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-white focus:outline-none focus:border-purple-500"
            />
          </div>

          <div>
            <label className="text-slate-300 font-semibold block mb-1">{t('symposium_admin.problem_label')}</label>
            <textarea
              rows={3}
              placeholder={t('symposium_admin.problem_placeholder')}
              value={conveneProblem}
              onChange={(e) => setConveneProblem(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-white focus:outline-none focus:border-purple-500"
            />
          </div>

          <div>
            <label className="text-slate-300 font-semibold block mb-2">{t('symposium_admin.select_councils_label')}</label>
            <div className="grid grid-cols-2 gap-2">
              {councils.map((c) => {
                const isSelected = selectedCouncilCodes.includes(c.councilCode);
                return (
                  <button
                    type="button"
                    key={c.councilCode}
                    onClick={() => {
                      if (isSelected) {
                        setSelectedCouncilCodes(prev => (Array.isArray(prev) ? prev.filter((code) => code !== c.councilCode) : []));
                      } else {
                        setSelectedCouncilCodes(prev => (Array.isArray(prev) ? [...prev, c.councilCode] : [c.councilCode]));
                      }
                    }}
                    className={`p-2.5 rounded-xl border text-left transition-all cursor-pointer ${
                      isSelected
                        ? 'bg-purple-900/40 border-purple-500 text-white font-bold'
                        : 'bg-slate-950/60 border-slate-800 text-slate-400 hover:text-slate-200'
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <span className="text-[11px]">{c.displayName}</span>
                      {isSelected && <Check className="w-3.5 h-3.5 text-purple-400" />}
                    </div>
                  </button>
                );
              })}
            </div>
          </div>
        </div>

        <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-800 text-xs">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 transition-colors cursor-pointer"
          >
            {t('symposium_admin.cancel_btn')}
          </button>
          <button
            type="button"
            disabled={convening || !conveneTopic.trim()}
            onClick={handleConveneSymposium}
            className="px-5 py-2.5 rounded-xl bg-gradient-to-r from-purple-600 via-pink-600 to-amber-500 hover:from-purple-500 hover:to-amber-400 text-white font-bold transition-all shadow-md shadow-purple-600/30 disabled:opacity-50 flex items-center gap-2 cursor-pointer"
          >
            <Sparkles className={`w-4 h-4 ${convening ? 'animate-spin' : ''}`} />
            {convening ? t('symposium_admin.convening_btn') : t('symposium_admin.convene_submit_btn')}
          </button>
        </div>
      </div>
    </div>
  );
}
