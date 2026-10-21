import React from 'react';
import { FeatureGate } from '@/shared/components/entitlement';
import { FeatureKey } from '@/types/entitlement';
import { useStudioScrollSync } from './hooks/useStudioScrollSync';
import { useSplitStudioData } from './hooks/useSplitStudioData';
import { SplitStudioHeader } from './components/SplitStudioHeader';
import { SourceDocumentViewport } from './components/SourceDocumentViewport';
import { CognitiveExtractedViewport } from './components/CognitiveExtractedViewport';
import { StudioPaywallTeaser } from './components/StudioPaywallTeaser';

export const SplitScreenStudioPage: React.FC = () => {
  const {
    sourceRef,
    extractedRef,
    isSyncEnabled,
    toggleSync,
    handleSourceScroll,
    handleExtractedScroll,
  } = useStudioScrollSync();

  const {
    documentTitle,
    filteredQuestions,
    activeQuestionId,
    setActiveQuestionId,
    filterType,
    setFilterType,
    isSaving,
    isAiSolving,
    updateQuestionText,
    toggleOptionCorrect,
    deleteQuestion,
    triggerAiRubric,
  } = useSplitStudioData();

  return (
    <FeatureGate feature={FeatureKey.SplitScreenStudio} fallback={<StudioPaywallTeaser />}>
      <div className="flex flex-col h-screen w-screen overflow-hidden bg-slate-950 text-slate-100 select-none">
        {/* Top Header */}
        <SplitStudioHeader
          isSyncEnabled={isSyncEnabled}
          onToggleSync={toggleSync}
          isAiSolving={isAiSolving}
          onTriggerAiRubric={triggerAiRubric}
          isSaving={isSaving}
        />

        {/* Dual-Viewport Workspace (Responsive Split Screen) */}
        <div className="flex-1 grid grid-cols-1 lg:grid-cols-2 overflow-hidden">
          {/* Cột 1: Văn bản gốc (Source) */}
          <SourceDocumentViewport
            scrollRef={sourceRef}
            onScroll={handleSourceScroll}
            documentTitle={documentTitle}
            questions={filteredQuestions}
            activeQuestionId={activeQuestionId}
            onSelectQuestion={setActiveQuestionId}
          />

          {/* Cột 2: Dữ liệu bóc tách (Extracted) */}
          <CognitiveExtractedViewport
            scrollRef={extractedRef}
            onScroll={handleExtractedScroll}
            questions={filteredQuestions}
            activeQuestionId={activeQuestionId}
            onSelectQuestion={setActiveQuestionId}
            onUpdateText={updateQuestionText}
            onToggleCorrect={toggleOptionCorrect}
            onDeleteQuestion={deleteQuestion}
            filterType={filterType}
            onFilterChange={setFilterType}
          />
        </div>
      </div>
    </FeatureGate>
  );
};

export default SplitScreenStudioPage;
