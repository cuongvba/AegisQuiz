import React from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, RefreshCw, Sparkles, CheckCircle2, Download, Layers } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

interface SplitStudioHeaderProps {
  isSyncEnabled: boolean;
  onToggleSync: () => void;
  isAiSolving: boolean;
  onTriggerAiRubric: () => void;
  isSaving: boolean;
}

export const SplitStudioHeader: React.FC<SplitStudioHeaderProps> = ({
  isSyncEnabled,
  onToggleSync,
  isAiSolving,
  onTriggerAiRubric,
  isSaving,
}) => {
  const { t } = useTranslation('split_studio');
  const navigate = useNavigate();

  return (
    <header className="h-16 border-b border-slate-800 bg-slate-900/90 backdrop-blur-md px-4 sm:px-6 flex items-center justify-between z-20 shrink-0">
      {/* Trái: Nút quay lại & Tiêu đề */}
      <div className="flex items-center gap-4">
        <button
          onClick={() => navigate('/admin/questions')}
          className="p-2 rounded-xl text-slate-400 hover:text-white hover:bg-slate-800/80 transition-all border border-transparent hover:border-slate-700"
          title={t('backToQuestions')}
        >
          <ArrowLeft className="w-5 h-5" />
        </button>

        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-base sm:text-lg font-bold text-white tracking-wide flex items-center gap-2">
              <Layers className="w-5 h-5 text-indigo-400" />
              {t('title')}
            </h1>
            <span className="hidden sm:inline-flex px-2 py-0.5 rounded-full text-[10px] font-bold bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">
              PRO & ENTERPRISE
            </span>
          </div>
          <p className="text-xs text-slate-400 hidden md:block">
            {t('subtitle')}
          </p>
        </div>
      </div>

      {/* Phải: Các thao tác chính */}
      <div className="flex items-center gap-2.5 sm:gap-3">
        {/* Toggle đồng bộ cuộn 60 FPS */}
        <button
          onClick={onToggleSync}
          className={`flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all ${
            isSyncEnabled
              ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-300 hover:bg-emerald-500/20'
              : 'bg-slate-800 border-slate-700 text-slate-400 hover:text-slate-200'
          }`}
          title={t('syncScroll')}
        >
          <span
            className={`w-2 h-2 rounded-full ${
              isSyncEnabled ? 'bg-emerald-400 animate-pulse' : 'bg-slate-500'
            }`}
          />
          <span className="hidden sm:inline">
            {isSyncEnabled ? t('syncScrollActive') : t('syncScrollInactive')}
          </span>
        </button>

        {/* Nút AI Rubric Generator */}
        <button
          onClick={onTriggerAiRubric}
          disabled={isAiSolving}
          className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl text-xs font-bold bg-gradient-to-r from-purple-600 to-indigo-600 text-white shadow-lg shadow-indigo-500/25 hover:from-purple-500 hover:to-indigo-500 active:scale-95 transition-all disabled:opacity-60"
        >
          {isAiSolving ? (
            <RefreshCw className="w-3.5 h-3.5 animate-spin" />
          ) : (
            <Sparkles className="w-3.5 h-3.5" />
          )}
          <span>{isAiSolving ? t('aiSolving') : t('aiSolveRubric')}</span>
        </button>

        {/* Trạng thái lưu đám mây */}
        <div className="hidden lg:flex items-center gap-1.5 text-xs text-slate-400 px-2 py-1 rounded-lg bg-slate-800/40 border border-slate-800">
          <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400" />
          <span>{isSaving ? t('saving') : t('saved')}</span>
        </div>

        {/* Nút Xuất đề thi */}
        <button
          onClick={() => alert('Xuất đề thi hoàn tất')}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-semibold bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition-all"
        >
          <Download className="w-3.5 h-3.5" />
          <span className="hidden sm:inline">{t('exportResults')}</span>
        </button>
      </div>
    </header>
  );
};
