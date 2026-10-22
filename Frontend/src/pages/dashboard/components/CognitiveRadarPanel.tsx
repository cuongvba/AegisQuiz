import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Loader2, ChevronRight, Info } from 'lucide-react';
import {
  Radar, RadarChart, PolarGrid, PolarAngleAxis,
  PolarRadiusAxis, ResponsiveContainer, Tooltip as RechartsTooltip,
} from 'recharts';

export interface TopicData {
  subject: string;
  A: number;
  fullMark: number;
}

interface CognitiveRadarPanelProps {
  data: TopicData[];
  isLoading: boolean;
}

export function CognitiveRadarPanel({ data, isLoading }: CognitiveRadarPanelProps) {
  const { t } = useTranslation('dashboard');
  const navigate = useNavigate();
  const [activeTimeframe, setActiveTimeframe] = useState<'week' | 'month'>('week');

  const handleTopicClick = (subject: string) => {
    navigate(`/practice?topic=${encodeURIComponent(subject)}`);
  };

  return (
    <div className="rounded-3xl bg-slate-900/80 border border-slate-800/80 shadow-2xl p-5 sm:p-7 backdrop-blur-xl flex flex-col justify-between">
      <div>
        {/* Panel Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-4">
          <div>
            <h2 className="text-lg sm:text-xl font-black text-white flex items-center gap-2.5">
              <span>{t('radarPanel.title')}</span>
              <span className="text-[10px] font-black px-2 py-0.5 rounded-full bg-cyan-500/20 text-cyan-300 border border-cyan-500/40">
                {t('radarPanel.badge')}
              </span>
            </h2>
            <p className="text-xs text-slate-400 mt-1">
              {t('radarPanel.subtitle')}
            </p>
          </div>

          <div className="flex items-center gap-1.5 p-1 rounded-xl bg-slate-950/80 border border-slate-800 self-start sm:self-auto">
            <button
              onClick={() => setActiveTimeframe('week')}
              className={`px-3 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                activeTimeframe === 'week'
                  ? 'bg-cyan-500/25 text-cyan-300 border border-cyan-500/40 shadow-sm'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              {t('radarPanel.thisWeek')}
            </button>
            <button
              onClick={() => setActiveTimeframe('month')}
              className={`px-3 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                activeTimeframe === 'month'
                  ? 'bg-cyan-500/25 text-cyan-300 border border-cyan-500/40 shadow-sm'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              {t('radarPanel.thisMonth')}
            </button>
          </div>
        </div>

        {/* Radar Graphic Display */}
        {isLoading ? (
          <div className="h-72 flex flex-col items-center justify-center text-slate-500 gap-3">
            <Loader2 className="animate-spin text-cyan-400" size={32} />
            <span className="text-xs font-bold">{t('radar.loading')}</span>
          </div>
        ) : (
          <div className="h-72 w-full relative">
            <ResponsiveContainer width="100%" height="100%">
              <RadarChart cx="50%" cy="50%" outerRadius="75%" data={data}>
                <PolarGrid stroke="#334155" strokeDasharray="3 3" />
                <PolarAngleAxis 
                  dataKey="subject" 
                  tick={{ fill: '#94a3b8', fontSize: 11, fontWeight: 700 }}
                  onClick={(e: any) => e?.value && handleTopicClick(e.value)}
                  className="cursor-pointer hover:opacity-80"
                />
                <PolarRadiusAxis 
                  angle={30} 
                  domain={[0, 100]} 
                  tick={{ fill: '#475569', fontSize: 9 }}
                  axisLine={false} 
                />
                <RechartsTooltip 
                  contentStyle={{ 
                    backgroundColor: '#0f172a', 
                    borderColor: '#334155',
                    borderRadius: '14px',
                    color: '#f8fafc',
                    boxShadow: '0 20px 25px -5px rgb(0 0 0 / 0.5)'
                  }} 
                />
                <Radar 
                  name={t('radar.seriesName')} 
                  dataKey="A" 
                  stroke="#06b6d4" 
                  strokeWidth={2.5} 
                  fill="#06b6d4" 
                  fillOpacity={0.45} 
                />
              </RadarChart>
            </ResponsiveContainer>
          </div>
        )}

        {/* Gợi ý tương tác trực tiếp */}
        <div className="flex items-center gap-1.5 text-[11px] text-cyan-300/80 bg-cyan-950/30 border border-cyan-500/20 px-3 py-1.5 rounded-xl mt-1">
          <Info size={13} className="shrink-0 text-cyan-400" />
          <span>{t('radar.interactiveHint')}</span>
        </div>
      </div>

      {/* Tóm tắt Thế mạnh & Điểm yếu */}
      <div className="mt-5 pt-4 border-t border-slate-800/80 flex flex-wrap items-center justify-between gap-3 text-xs">
        <div className="flex items-center gap-3">
          <span className="flex items-center gap-1.5 text-slate-400 font-medium">
            <span className="w-2 h-2 rounded-full bg-cyan-400 shadow-sm shadow-cyan-400" /> {t('radar.strengthLabel')} <strong className="text-white">{t('radar.strengthValue')}</strong>
          </span>
          <span className="flex items-center gap-1.5 text-slate-400 font-medium">
            <span className="w-2 h-2 rounded-full bg-rose-400 shadow-sm shadow-rose-400" /> {t('radar.weaknessLabel')} <strong className="text-white">{t('radar.weaknessValue')}</strong>
          </span>
        </div>

        <button
          onClick={() => navigate('/practice')}
          className="text-cyan-400 hover:text-cyan-300 font-bold inline-flex items-center gap-1 hover:underline cursor-pointer"
        >
          {t('radar.openPractice')} <ChevronRight size={14} />
        </button>
      </div>
    </div>
  );
}
