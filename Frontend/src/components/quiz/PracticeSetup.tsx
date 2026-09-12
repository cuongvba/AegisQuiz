import { useMemo, useState } from 'react';
import { CognitiveTopicExplorer } from '@/components/quiz/CognitiveTopicExplorer';
import { PracticeLaunchPad } from '@/components/quiz/PracticeLaunchPad';
import type {
  CognitiveDomainGroupDto,
  ExamCategoryPreset,
  PracticeConfig,
  PracticeMode,
  PracticeTopicConfig,
  TopicSummary,
} from '@/types/quiz';

type Props = {
  topics: TopicSummary[];
  domainGroups?: CognitiveDomainGroupDto[];
  onStart: (config: PracticeConfig) => void;
  t: (key: string) => string;
};

function defaultTopicConfig(topicKey: string): PracticeTopicConfig {
  return {
    topicKey,
    numberOfQuestions: 10,
    isRandom: true,
    startIndex: 1,
  };
}

export function PracticeSetup({ topics, domainGroups, onStart, t }: Props) {
  const [mode, setMode] = useState<PracticeMode>('study');
  const [topicConfigs, setTopicConfigs] = useState<PracticeTopicConfig[]>([]);
  const [shuffleOptions, setShuffleOptions] = useState(true);
  const [shuffleQuestions, setShuffleQuestions] = useState(true);
  const [timeLimitMin, setTimeLimitMin] = useState(0);
  const [autoNextOnTimeout, setAutoNextOnTimeout] = useState(true);
  const [activePreset, setActivePreset] = useState<ExamCategoryPreset | null>(null);

  const handleSelectMultipleTopics = (keys: string[], select: boolean) => {
    if (select) {
      setTopicConfigs((prev) => {
        const existingKeys = new Set(prev.map((c) => c.topicKey));
        const toAdd = keys.filter((k) => !existingKeys.has(k)).map((k) => defaultTopicConfig(k));
        return [...prev, ...toAdd];
      });
    } else {
      const toRemove = new Set(keys);
      setTopicConfigs((prev) => prev.filter((c) => !toRemove.has(c.topicKey)));
    }
  };

  const applyDrivingPreset = (preset: ExamCategoryPreset, count: number, mins: number) => {
    setActivePreset(preset);
    setMode('exam');
    setTimeLimitMin(mins);
    setShuffleOptions(true);
    setShuffleQuestions(true);

    const gplxTopic = topics.find(
      (item) =>
        item.key.toLowerCase().includes('gplx') ||
        item.key.toLowerCase().includes('lai_xe') ||
        item.name.toLowerCase().includes('lái xe')
    );

    const targetKey =
      preset === 'gplx_fatal_only'
        ? gplxTopic?.key || 'GPLX_FATAL'
        : gplxTopic?.key || (topics.length > 0 ? topics[0].key : 'GPLX_B2');

    setTopicConfigs([
      {
        topicKey: targetKey,
        numberOfQuestions: count,
        isRandom: true,
        startIndex: 1,
        onlyCritical: preset === 'gplx_fatal_only',
      },
    ]);
  };

  const selectedMap = useMemo(() => {
    const map = new Map<string, PracticeTopicConfig>();
    for (const config of topicConfigs) {
      map.set(config.topicKey, config);
    }
    return map;
  }, [topicConfigs]);

  const toggleTopic = (topicKey: string, checked: boolean) => {
    if (checked) {
      setTopicConfigs((prev) => [...prev, defaultTopicConfig(topicKey)]);
      return;
    }

    setTopicConfigs((prev) => prev.filter((cfg) => cfg.topicKey !== topicKey));
  };

  const updateTopicConfig = (topicKey: string, patch: Partial<PracticeTopicConfig>) => {
    setTopicConfigs((prev) => prev.map((cfg) => (cfg.topicKey === topicKey ? { ...cfg, ...patch } : cfg)));
  };

  const handleModeChange = (nextMode: PracticeMode) => {
    setMode(nextMode);
    setActivePreset(null);
    if (nextMode === 'kids') {
      setShuffleOptions(true);
      setShuffleQuestions(true);
      setAutoNextOnTimeout(true);
    }
  };

  const resetTopics = () => {
    setTopicConfigs([]);
    setActivePreset(null);
  };

  const totalSelected = topicConfigs.reduce((sum, cfg) => sum + Math.max(1, cfg.numberOfQuestions || 1), 0);

  const handleStart = () => {
    if (topicConfigs.length === 0) {
      window.alert(t('selectTopicAlert') || 'Vui lòng chọn ít nhất một chuyên đề trước khi bắt đầu.');
      return;
    }

    onStart({
      mode,
      topicConfigs,
      shuffleOptions: mode === 'kids' ? true : shuffleOptions,
      shuffleQuestions: mode === 'kids' ? true : shuffleQuestions,
      timeLimitSec: mode === 'exam' ? Math.max(0, timeLimitMin) * 60 : 0,
      autoNextOnTimeout: mode === 'kids' ? true : autoNextOnTimeout,
      onlyCritical: activePreset === 'gplx_fatal_only',
      preset: activePreset ?? undefined,
    });
  };

  return (
    <div className="space-y-6">
      {/* ─── HERO HEADER ─── */}
      <section className="rounded-[2rem] border border-white/80 bg-white/90 p-5 shadow-[0_20px_50px_-30px_rgba(14,116,144,0.3)] backdrop-blur-md md:p-6 transition">
        <div className="flex flex-col gap-2 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-xs font-black uppercase tracking-[0.12em] text-cyan-600 flex items-center gap-1.5">
              <span className="inline-block h-2 w-2 rounded-full bg-cyan-500 animate-pulse" />
              {t('setupTitle') || 'THIẾT LẬP PHIÊN HỌC'}
            </p>
            <h2 className="mt-1 text-2xl font-black tracking-tight text-slate-900">
              {t('setupHeadline') || 'Bắt đầu ôn luyện ngay'}
            </h2>
            <p className="mt-1 text-xs sm:text-sm text-slate-500 max-w-2xl">
              {t('setupDescription') || 'Chọn chuyên đề kiến thức, cấu hình nhanh chế độ và bắt đầu luyện thi ngay tức thì.'}
            </p>
          </div>

          <div className="flex items-center gap-2">
            <div className="rounded-2xl border border-slate-200/80 bg-slate-50/80 px-4 py-2 text-xs font-bold text-slate-700 shadow-2xs">
              <span className="text-slate-400 font-normal">Tổng chọn:</span>{' '}
              <span className="text-cyan-700 font-black text-sm">{totalSelected}</span> câu hỏi
            </div>
          </div>
        </div>
      </section>

      {/* ─── TRỌNG TÂM: ĐỒ THỊ NHẬN THỨC CHUYÊN ĐỀ (COGNITIVE TOPIC EXPLORER) ─── */}
      <section>
        <CognitiveTopicExplorer
          domainGroups={domainGroups}
          legacyTopics={topics}
          topicConfigs={topicConfigs}
          selectedMap={selectedMap}
          activePreset={activePreset}
          onSelectDrivingPreset={applyDrivingPreset}
          onToggleTopic={toggleTopic}
          onUpdateTopicConfig={updateTopicConfig}
          onSelectMultipleTopics={handleSelectMultipleTopics}
          t={t}
        />
      </section>

      {/* ─── SMART LAUNCH PAD (COMBO CHỌN CHẾ ĐỘ + TÙY CHỌN + NÚT BẮT ĐẦU) ─── */}
      <PracticeLaunchPad
        mode={mode}
        onModeChange={handleModeChange}
        totalSelected={totalSelected}
        topicCount={topicConfigs.length}
        timeLimitMin={timeLimitMin}
        onTimeLimitChange={setTimeLimitMin}
        shuffleOptions={shuffleOptions}
        onShuffleOptionsChange={setShuffleOptions}
        shuffleQuestions={shuffleQuestions}
        onShuffleQuestionsChange={setShuffleQuestions}
        autoNextOnTimeout={autoNextOnTimeout}
        onAutoNextChange={setAutoNextOnTimeout}
        onStart={handleStart}
        onResetTopics={resetTopics}
        t={t}
      />
    </div>
  );
}
