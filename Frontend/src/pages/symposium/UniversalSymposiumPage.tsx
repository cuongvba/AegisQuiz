import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  aiSwarmService,
  UniversalAiCouncilDto,
  SymposiumProceedingsResult,
  ConveneSymposiumRequest,
  SymposiumTeleologyMode,
  SymposiumDialogueTurn
} from '@/services/ai-swarm.service';
import { SymposiumHeader } from '@/features/symposium/components/SymposiumHeader';
import {
  SymposiumConveneBar,
  PRESET_TOPICS,
  PresetTopic
} from '@/features/symposium/components/SymposiumConveneBar';
import { CouncilSelectorDrawer } from '@/features/symposium/components/CouncilSelectorDrawer';
import { DeepResearchDossierPanel } from '@/features/symposium/components/DeepResearchDossierPanel';
import { CouncilContributionsPanel } from '@/features/symposium/components/CouncilContributionsPanel';
import { DialecticalTimelineFeed } from '@/features/symposium/components/DialecticalTimelineFeed';
import { SymposiumResolutionCard } from '@/features/symposium/components/SymposiumResolutionCard';
import { SymposiumPhaseProgress } from '@/features/symposium/components/SymposiumPhaseProgress';
import { SymposiumStepResultsFeed } from '@/features/symposium/components/SymposiumStepResultsFeed';
import { SocraticDirectiveModal } from '@/features/symposium/components/SocraticDirectiveModal';
import { Compass, Award, ArrowRight, Layers } from 'lucide-react';
import * as signalR from '@microsoft/signalr';

export function UniversalSymposiumPage() {
  const navigate = useNavigate();
  const { t, i18n } = useTranslation('symposium');

  // State: CSDL Viện Hàn Lâm
  const [councils, setCouncils] = useState<UniversalAiCouncilDto[]>([]);
  const [loadingCouncils, setLoadingCouncils] = useState<boolean>(true);
  const [isDrawerOpen, setIsDrawerOpen] = useState<boolean>(false);

  // State: Form Triệu Tập
  const [topicTitle, setTopicTitle] = useState<string>(PRESET_TOPICS[0].title);
  const [coreProblemStatement, setCoreProblemStatement] = useState<string>(PRESET_TOPICS[0].statement);
  const [selectedCouncilCodes, setSelectedCouncilCodes] = useState<string[]>(PRESET_TOPICS[0].councils);
  const [enableAdversarialCritique, setEnableAdversarialCritique] = useState<boolean>(true);
  const [teleologyMode, setTeleologyMode] = useState<SymposiumTeleologyMode>(SymposiumTeleologyMode.UniversalHybrid);

  // State: Tiến Trình Hội Thảo Đa Vòng (Multi-Round Dialectical Evolution)
  const [isConvening, setIsConvening] = useState<boolean>(false);
  const [activePhaseIndex, setActivePhaseIndex] = useState<number>(0);
  const [proceedings, setProceedings] = useState<SymposiumProceedingsResult | null>(null);
  const [currentRound, setCurrentRound] = useState<number>(1);
  const [roundsHistory, setRoundsHistory] = useState<SymposiumProceedingsResult[]>([]);
  const [isNextRoundModalOpen, setIsNextRoundModalOpen] = useState<boolean>(false);
  const [activeNotebookId, setActiveNotebookId] = useState<string | null>(null);

  // State: Xuất Kỷ Yếu sang Notebook Hub
  const [isExporting, setIsExporting] = useState<boolean>(false);
  const [exportedNotebookId, setExportedNotebookId] = useState<string | null>(null);
  const [exportSuccessMessage, setExportSuccessMessage] = useState<string | null>(null);

  // State: Kết quả từng bước theo thời gian thực (Incremental Step Results Streaming)
  const [phaseResults, setPhaseResults] = useState<Record<number, any>>({});
  const [liveDialogueTurns, setLiveDialogueTurns] = useState<SymposiumDialogueTurn[]>([]);

  useEffect(() => {
    loadCouncils();
  }, []);

  const loadCouncils = async () => {
    setLoadingCouncils(true);
    try {
      const data = await aiSwarmService.getSymposiumCouncils();
      setCouncils(data);
    } catch (err) {
      console.error('Lỗi nạp Viện Hàn Lâm từ API:', err);
    } finally {
      setLoadingCouncils(false);
    }
  };

  const handleToggleCouncil = (code: string) => {
    if (selectedCouncilCodes.includes(code)) {
      setSelectedCouncilCodes(prev => prev.filter(c => c !== code));
    } else {
      setSelectedCouncilCodes(prev => [...prev, code]);
    }
  };

  const handleSelectAllCouncils = () => {
    setSelectedCouncilCodes(councils.map(c => c.councilCode));
  };

  const handleClearToAutomatic = () => {
    setSelectedCouncilCodes([]);
  };

  const handleSelectPreset = (preset: PresetTopic) => {
    setTopicTitle(preset.title);
    setCoreProblemStatement(preset.statement);
    setSelectedCouncilCodes(preset.councils);
    setProceedings(null);
    setRoundsHistory([]);
    setCurrentRound(1);
    setActiveNotebookId(null);
    setExportedNotebookId(null);
    setExportSuccessMessage(null);
    setPhaseResults({});
    setLiveDialogueTurns([]);
  };

  // Khởi động Vòng 1 với SignalR Streaming Từng Bước
  const handleConvene = async () => {
    if (!topicTitle.trim() || !coreProblemStatement.trim()) return;

    setIsConvening(true);
    setActivePhaseIndex(1);
    setProceedings(null);
    setRoundsHistory([]);
    setCurrentRound(1);
    setActiveNotebookId(null);
    setExportedNotebookId(null);
    setExportSuccessMessage(null);
    setPhaseResults({});
    setLiveDialogueTurns([]);

    const clientSessionId = crypto.randomUUID();
    let hubConn: signalR.HubConnection | null = null;

    try {
      hubConn = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/symposium')
        .withAutomaticReconnect()
        .build();

      hubConn.on('OnPhaseChanged', (data: any) => {
        if (data?.PhaseNumber) {
          setActivePhaseIndex(data.PhaseNumber);
        }
      });

      hubConn.on('OnTurnSpoken', (turn: any) => {
        setLiveDialogueTurns(prev => [...prev, turn]);
      });

      hubConn.on('OnPhaseResult', (data: any) => {
        if (data?.PhaseNumber) {
          setPhaseResults(prev => ({ ...prev, [data.PhaseNumber]: data.Result }));
          setActivePhaseIndex(Math.min(data.PhaseNumber + 1, 6));
        }
      });

      hubConn.on('OnProceedingsCompleted', (proc: any) => {
        setProceedings(proc);
        setActivePhaseIndex(6);
      });

      await hubConn.start();
      await hubConn.invoke('JoinSymposiumSession', clientSessionId);
    } catch (connErr) {
      console.warn('Lỗi kết nối SignalR, fallback sang luồng tiêu chuẩn:', connErr);
    }

    try {
      const req: ConveneSymposiumRequest = {
        topicTitle,
        coreProblemStatement,
        specificCouncilCodes: selectedCouncilCodes,
        enableAdversarialCritique,
        teleologyMode,
        locale: i18n.language?.startsWith('en') ? 'en-US' : 'vi-VN',
        roundNumber: 1,
        clientSessionId
      };
      const result = await aiSwarmService.conveneSymposium(req as any);
      setProceedings(result);
      setRoundsHistory([result]);
      setCurrentRound(1);
      if (result.targetNotebookId) {
        setActiveNotebookId(result.targetNotebookId);
        setExportedNotebookId(result.targetNotebookId);
      }
      // Khóa chắc toàn bộ kết quả của 6 bước
      setPhaseResults({
        1: { EmpiricalDossier: result.empiricalDossier, Turns: result.dialogueLog.filter(t => t.phaseNumber === 1) },
        2: { CouncilContributions: result.councilContributions, Turns: result.dialogueLog.filter(t => t.phaseNumber === 2) },
        3: { Turns: result.dialogueLog.filter(t => t.phaseNumber === 3) },
        4: result.dialogueLog.find(t => t.phaseNumber === 4),
        5: result.evaluatedRubric,
        6: { structuredOutcome: result.structuredOutcome, finalDeclaration: result.finalConsensusDeclaration }
      });
      setActivePhaseIndex(6);
    } catch (err) {
      console.error('Lỗi triệu tập Đại Hội Thảo Vòng 1:', err);
    } finally {
      if (hubConn) {
        try {
          await hubConn.invoke('LeaveSymposiumSession', clientSessionId);
          await hubConn.stop();
        } catch { /* ignore */ }
      }
      setIsConvening(false);
    }
  };

  // Khởi động Vòng Tiếp Theo với SignalR Streaming Từng Bước
  const handleConveneNextRound = async (params: {
    socraticDirectives: string;
    additionalDocuments: string;
    additionalUris: string[];
    teleologyMode: SymposiumTeleologyMode;
    isFinalRound: boolean;
  }) => {
    if (!proceedings) return;
    const nextRound = currentRound + 1;

    setIsNextRoundModalOpen(false);
    setIsConvening(true);
    setActivePhaseIndex(1);
    setPhaseResults({});
    setLiveDialogueTurns([]);

    const clientSessionId = crypto.randomUUID();
    let hubConn: signalR.HubConnection | null = null;

    try {
      hubConn = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/symposium')
        .withAutomaticReconnect()
        .build();

      hubConn.on('OnPhaseChanged', (data: any) => {
        if (data?.PhaseNumber) {
          setActivePhaseIndex(data.PhaseNumber);
        }
      });

      hubConn.on('OnTurnSpoken', (turn: any) => {
        setLiveDialogueTurns(prev => [...prev, turn]);
      });

      hubConn.on('OnPhaseResult', (data: any) => {
        if (data?.PhaseNumber) {
          setPhaseResults(prev => ({ ...prev, [data.PhaseNumber]: data.Result }));
          setActivePhaseIndex(Math.min(data.PhaseNumber + 1, 6));
        }
      });

      await hubConn.start();
      await hubConn.invoke('JoinSymposiumSession', clientSessionId);
    } catch (connErr) {
      console.warn('Lỗi kết nối SignalR vòng tiếp theo:', connErr);
    }

    try {
      const req: ConveneSymposiumRequest = {
        topicTitle,
        coreProblemStatement,
        specificCouncilCodes: selectedCouncilCodes,
        enableAdversarialCritique,
        teleologyMode: params.teleologyMode,
        locale: i18n.language?.startsWith('en') ? 'en-US' : 'vi-VN',
        roundNumber: nextRound,
        parentSessionId: proceedings.sessionId,
        previousRoundResolution: proceedings.finalConsensusDeclaration,
        socraticDirectives: params.socraticDirectives,
        additionalDocuments: params.additionalDocuments,
        additionalUris: params.additionalUris,
        targetNotebookId: activeNotebookId || undefined,
        isFinalRound: params.isFinalRound,
        clientSessionId
      };

      const result = await aiSwarmService.conveneSymposium(req as any);
      setProceedings(result);
      setRoundsHistory(prev => [...prev, result]);
      setCurrentRound(nextRound);
      if (result.targetNotebookId) {
        setActiveNotebookId(result.targetNotebookId);
        setExportedNotebookId(result.targetNotebookId);
      }
      setPhaseResults({
        1: { EmpiricalDossier: result.empiricalDossier, Turns: result.dialogueLog.filter(t => t.phaseNumber === 1) },
        2: { CouncilContributions: result.councilContributions, Turns: result.dialogueLog.filter(t => t.phaseNumber === 2) },
        3: { Turns: result.dialogueLog.filter(t => t.phaseNumber === 3) },
        4: result.dialogueLog.find(t => t.phaseNumber === 4),
        5: result.evaluatedRubric,
        6: { structuredOutcome: result.structuredOutcome, finalDeclaration: result.finalConsensusDeclaration }
      });
      setActivePhaseIndex(6);
    } catch (err) {
      console.error(`Lỗi triệu tập Đại Hội Thảo Vòng ${nextRound}:`, err);
    } finally {
      if (hubConn) {
        try {
          await hubConn.invoke('LeaveSymposiumSession', clientSessionId);
          await hubConn.stop();
        } catch { /* ignore */ }
      }
      setIsConvening(false);
    }
  };

  // Tuyên bố Nghị quyết Chung cuộc ngay tại vòng hiện tại
  const handleConcludeSymposium = async () => {
    if (!proceedings) return;
    const confirm = window.confirm(
      `Chủ tọa xác nhận: Bạn có muốn chốt hạ hội thảo và tuyên bố Nghị quyết của Vòng ${currentRound} là Nghị Quyết Chung Cuộc Tối Hậu?`
    );
    if (!confirm) return;

    // Cập nhật trạng thái hiển thị
    const updated = {
      ...proceedings,
      isFinalRound: true
    };
    setProceedings(updated);
    setRoundsHistory(prev => prev.map((r, i) => (i === prev.length - 1 ? updated : r)));
  };

  const handleExportToNotebook = async () => {
    if (!proceedings) return;
    setIsExporting(true);
    try {
      const res = await aiSwarmService.exportSymposiumToNotebook(
        proceedings.sessionId,
        activeNotebookId || undefined
      );
      if (res.success) {
        setExportedNotebookId(res.notebookId);
        setActiveNotebookId(res.notebookId);
        setExportSuccessMessage(res.message);
      }
    } catch (err) {
      console.error('Lỗi xuất kỷ yếu sang notebook:', err);
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans pb-24 selection:bg-cyan-500 selection:text-black">
      {/* Background Glow */}
      <div className="fixed inset-0 pointer-events-none overflow-hidden z-0">
        <div className="absolute top-0 left-1/4 w-[500px] h-[500px] bg-cyan-600/10 rounded-full blur-[140px]" />
        <div className="absolute top-1/3 right-10 w-[450px] h-[450px] bg-violet-600/10 rounded-full blur-[150px]" />
      </div>

      <div className="relative z-10 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pt-6 space-y-6">
        {/* 1. Header Tinh Gọn */}
        <SymposiumHeader
          councilsCount={councils.length}
          loading={loadingCouncils}
          onRefresh={loadCouncils}
        />

        {/* 2. Bàn Điều Khiển Triệu Tập (Above the fold) */}
        <SymposiumConveneBar
          topicTitle={topicTitle}
          setTopicTitle={setTopicTitle}
          coreProblemStatement={coreProblemStatement}
          setCoreProblemStatement={setCoreProblemStatement}
          selectedCouncilCodes={selectedCouncilCodes}
          totalCouncilsCount={councils.length}
          readyCouncilsCount={councils.filter(c => c.readinessStatus !== 'OFFLINE').length}
          onOpenCouncilSelector={() => setIsDrawerOpen(true)}
          enableAdversarialCritique={enableAdversarialCritique}
          setEnableAdversarialCritique={setEnableAdversarialCritique}
          teleologyMode={teleologyMode}
          setTeleologyMode={setTeleologyMode}
          isConvening={isConvening}
          activePhaseIndex={activePhaseIndex}
          onConvene={handleConvene}
          onSelectPreset={handleSelectPreset}
        />

        {/* 3. Tiến Trình Biện Chứng (khi bắt đầu hoặc đã có kết quả) */}
        {(isConvening || proceedings) && (
          <div className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
            {/* THANH ĐIỀU HƯỚNG CÁC VÒNG (ROUND TABS STEPPER) */}
            {roundsHistory.length > 0 && (
              <div className="p-3 rounded-2xl bg-slate-900/80 border border-slate-800 flex items-center justify-between gap-4 overflow-x-auto">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5 px-2">
                    <Layers size={14} className="text-cyan-400" />
                    <span>Tiến Trình Đa Vòng:</span>
                  </span>
                  <div className="flex items-center gap-2">
                    {roundsHistory.map((r, idx) => {
                      const roundNum = r.roundNumber || idx + 1;
                      const isActive = proceedings?.sessionId === r.sessionId;
                      return (
                        <button
                          key={r.sessionId || idx}
                          onClick={() => setProceedings(r)}
                          className={`px-3.5 py-1.5 rounded-xl text-xs font-bold flex items-center gap-1.5 transition cursor-pointer ${
                            isActive
                              ? 'bg-cyan-500 text-slate-950 shadow-md shadow-cyan-500/20'
                              : 'bg-slate-800/80 hover:bg-slate-800 text-slate-300 border border-slate-700/60'
                          }`}
                        >
                          {r.isFinalRound ? (
                            <>
                              <Award size={13} className={isActive ? 'text-slate-950' : 'text-amber-400'} />
                              <span>{t('multiRound.round', { round: roundNum })} ({t('multiRound.finalRound')})</span>
                            </>
                          ) : (
                            <>
                              <Compass size={13} className={isActive ? 'text-slate-950' : 'text-cyan-400'} />
                              <span>{t('multiRound.round', { round: roundNum })}</span>
                            </>
                          )}
                        </button>
                      );
                    })}
                  </div>
                </div>

                {proceedings && !proceedings.isFinalRound && (
                  <button
                    onClick={() => setIsNextRoundModalOpen(true)}
                    disabled={isConvening}
                    className="px-4 py-1.5 rounded-xl bg-gradient-to-r from-indigo-600 to-cyan-600 hover:from-indigo-500 hover:to-cyan-500 text-white font-black text-xs flex items-center gap-1.5 shadow-md transition cursor-pointer flex-shrink-0"
                  >
                    <span>{t('multiRound.nextRoundButton')}</span>
                    <ArrowRight size={13} />
                  </button>
                )}
              </div>
            )}

            <SymposiumPhaseProgress
              activePhaseIndex={activePhaseIndex}
              isDone={!!proceedings}
              isConvening={isConvening}
            />

            {/* TIẾN TRÌNH & KẾT QUẢ TỨC THỜI TỪNG BƯỚC (LIVE DIALECTICAL STEPPER) */}
            <SymposiumStepResultsFeed
              activePhaseIndex={activePhaseIndex}
              isConvening={isConvening}
              phaseResults={phaseResults}
              dialogueLog={proceedings?.dialogueLog || liveDialogueTurns}
              empiricalDossier={proceedings?.empiricalDossier || phaseResults[1]?.EmpiricalDossier}
              councilContributions={proceedings?.councilContributions || phaseResults[2]?.CouncilContributions}
              rubric={proceedings?.evaluatedRubric || phaseResults[5]}
              structuredOutcome={proceedings?.structuredOutcome || phaseResults[6]?.structuredOutcome}
              finalDeclaration={proceedings?.finalConsensusDeclaration || phaseResults[6]?.finalDeclaration}
            />

            {/* 4. Kết Quả Kỷ Yếu & Bằng Chứng Deep Research */}
            {proceedings && (
              <>
                {/* Hồ Sơ Bằng Chứng Deep Research */}
                {proceedings.empiricalDossier && (
                  <DeepResearchDossierPanel dossier={proceedings.empiricalDossier} />
                )}

                {/* Hồ Sơ Luận Điểm & Đóng Góp Học Thuật Chi Tiết Từng Viện */}
                {((proceedings.councilContributions && proceedings.councilContributions.length > 0) ||
                  ((proceedings as any).CouncilContributions && (proceedings as any).CouncilContributions.length > 0)) && (
                  <CouncilContributionsPanel
                    contributions={proceedings.councilContributions || (proceedings as any).CouncilContributions || []}
                  />
                )}

                {/* Nghị Quyết & Bảng Điểm Rubric */}
                <SymposiumResolutionCard
                  proceedings={proceedings}
                  isExporting={isExporting}
                  exportedNotebookId={exportedNotebookId || activeNotebookId}
                  exportSuccessMessage={exportSuccessMessage}
                  onExportToNotebook={handleExportToNotebook}
                  onNavigateToNotebooks={() => navigate('/admin/notebooks')}
                  onOpenNextRoundModal={() => setIsNextRoundModalOpen(true)}
                  onConcludeSymposium={handleConcludeSymposium}
                />

                {/* Toàn Văn Biên Bản Tranh Biện Từng Giai Đoạn */}
                <DialecticalTimelineFeed dialogueLog={proceedings.dialogueLog} />
              </>
            )}
          </div>
        )}
      </div>

      {/* Modal Chỉ Đạo Của Chủ Tọa (Human-in-the-loop Ingestion) */}
      <SocraticDirectiveModal
        isOpen={isNextRoundModalOpen}
        onClose={() => setIsNextRoundModalOpen(false)}
        currentRound={currentRound}
        previousResolutionSummary={proceedings?.finalConsensusDeclaration ? proceedings.finalConsensusDeclaration.slice(0, 300) + '...' : ''}
        currentTeleologyMode={teleologyMode}
        onSubmit={handleConveneNextRound}
        isConvening={isConvening}
      />

      {/* Drawer Chọn Viện Hàn Lâm Linh Động (OCP) */}
      <CouncilSelectorDrawer
        isOpen={isDrawerOpen}
        onClose={() => setIsDrawerOpen(false)}
        councils={councils}
        selectedCouncilCodes={selectedCouncilCodes}
        onToggleCouncil={handleToggleCouncil}
        onSelectAll={handleSelectAllCouncils}
        onClearToAutomatic={handleClearToAutomatic}
      />
    </div>
  );
}
export default UniversalSymposiumPage;
