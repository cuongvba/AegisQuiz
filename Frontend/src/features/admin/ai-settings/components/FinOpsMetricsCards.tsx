import React from 'react';
import { Cpu, Database, DollarSign, Layers, Activity, CheckCircle2, AlertTriangle } from 'lucide-react';
import { useTranslation } from 'react-i18next';

interface FinOpsTelemetry {
  totalRequestsProcessed: number;
  cacheHitsCount: number;
  cacheHitRatio: number;
  estimatedCostSavedUsd: number;
  totalSpentUsd: number;
  requestCountByProvider: Record<string, number>;
}

interface FinOpsMetricsCardsProps {
  telemetry?: FinOpsTelemetry;
  activeProvidersCount: number;
  totalProvidersCount: number;
  testResults: Record<string, { success: boolean; latencyMs: number; message: string }> | null;
}

export const FinOpsMetricsCards: React.FC<FinOpsMetricsCardsProps> = ({
  telemetry,
  activeProvidersCount,
  totalProvidersCount,
  testResults,
}) => {
  const { t } = useTranslation('ai_settings');

  return (
    <div className="space-y-4">
      {/* 4 Cards Thống Kê FinOps */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Card 1: Tổng requests */}
        <div className="p-5 rounded-2xl bg-slate-900/90 border border-slate-800 shadow-sm flex items-center gap-4 hover:border-slate-700 transition-all">
          <div className="p-3 rounded-xl bg-blue-500/20 text-blue-400">
            <Cpu className="w-6 h-6" />
          </div>
          <div>
            <div className="text-2xl font-black text-white">
              {telemetry?.totalRequestsProcessed ?? 0}
            </div>
            <div className="text-xs text-slate-400 font-medium">
              {t('metrics.total_requests')}
            </div>
          </div>
        </div>

        {/* Card 2: Semantic Cache */}
        <div className="p-5 rounded-2xl bg-slate-900/90 border border-slate-800 shadow-sm flex items-center gap-4 hover:border-slate-700 transition-all">
          <div className="p-3 rounded-xl bg-emerald-500/20 text-emerald-400">
            <Database className="w-6 h-6" />
          </div>
          <div>
            <div className="text-2xl font-black text-emerald-400">
              {((telemetry?.cacheHitRatio ?? 0) * 100).toFixed(1)}%
            </div>
            <div className="text-xs text-slate-400 font-medium">
              {t('metrics.semantic_cache_ratio')}
            </div>
          </div>
        </div>

        {/* Card 3: Tiết kiệm ngân sách */}
        <div className="p-5 rounded-2xl bg-slate-900/90 border border-slate-800 shadow-sm flex items-center gap-4 hover:border-slate-700 transition-all">
          <div className="p-3 rounded-xl bg-amber-500/20 text-amber-400">
            <DollarSign className="w-6 h-6" />
          </div>
          <div>
            <div className="text-2xl font-black text-amber-400">
              ${(telemetry?.estimatedCostSavedUsd ?? 0).toFixed(2)}
            </div>
            <div className="text-xs text-slate-400 font-medium">
              {t('metrics.budget_saved')}
            </div>
          </div>
        </div>

        {/* Card 4: Providers hoạt động */}
        <div className="p-5 rounded-2xl bg-slate-900/90 border border-slate-800 shadow-sm flex items-center gap-4 hover:border-slate-700 transition-all">
          <div className="p-3 rounded-xl bg-purple-500/20 text-purple-400">
            <Layers className="w-6 h-6" />
          </div>
          <div>
            <div className="text-2xl font-black text-purple-300">
              {activeProvidersCount} / {totalProvidersCount}
            </div>
            <div className="text-xs text-slate-400 font-medium">
              {t('metrics.active_providers')}
            </div>
          </div>
        </div>
      </div>

      {/* Latency Test Results Banner */}
      {testResults && (
        <div className="p-5 rounded-2xl bg-slate-950/80 border border-indigo-500/30 space-y-3">
          <h3 className="text-sm font-bold text-indigo-300 flex items-center gap-2">
            <Activity className="w-4 h-4 text-emerald-400" />
            {t('latency_test.title')}
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-5 gap-3">
            {Object.entries(testResults).map(([provider, res]) => (
              <div 
                key={provider} 
                className="p-3 rounded-xl bg-slate-900/90 border border-slate-800 flex items-center justify-between text-xs"
              >
                <span className="font-semibold text-slate-200 truncate mr-2">{provider}</span>
                {res.success ? (
                  <span className="flex items-center gap-1 font-bold text-emerald-400 shrink-0 font-mono">
                    <CheckCircle2 className="w-3.5 h-3.5" /> 
                    {t('latency_test.status_ok', { latency: res.latencyMs })}
                  </span>
                ) : (
                  <span className="flex items-center gap-1 text-rose-400 shrink-0" title={res.message}>
                    <AlertTriangle className="w-3.5 h-3.5" /> 
                    {t('latency_test.status_error')}
                  </span>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
