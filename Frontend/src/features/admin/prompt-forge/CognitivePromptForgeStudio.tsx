import React, { useState, useEffect } from 'react';
import {
  Sparkles,
  Shield,
  Layers,
  Cpu,
  Brain,
  CheckCircle2,
  AlertTriangle,
  RefreshCw,
  Zap,
  ArrowRight,
  BookOpen,
  FileCode2,
  Activity,
  Sliders,
  Award,
  ChevronRight,
  Copy,
  Check,
} from 'lucide-react';
import {
  promptForgeService,
  DialecticalRigorLevel,
  RIGOR_LABELS,
  MetaPromptProposalDto,
  CascadingPromptStackDto,
  AxiomaticAuditReportDto,
  PromptTemplateSummaryDto,
} from '@/services/prompt-forge.service';

export function CognitivePromptForgeStudio() {
  // State Cây Phân Tầng hiện tại
  const [templates, setTemplates] = useState<PromptTemplateSummaryDto[]>([]);
  const [selectedLayer, setSelectedLayer] = useState<number>(0);
  const [loadingTemplates, setLoadingTemplates] = useState<boolean>(true);

  // Form Đề Xuất
  const [domainFocus, setDomainFocus] = useState<string>('Nông Nghiệp Sinh Thái & An Ninh Lương Thực');
  const [coreAxioms, setCoreAxioms] = useState<string>('Thực nghiệm lâm sàng, kiểm tra phản nghiệm Popper, loại bỏ ngụy biện');
  const [rigorLevel, setRigorLevel] = useState<DialecticalRigorLevel>(DialecticalRigorLevel.StrictPopperian);

  // Trạng thái AI Assistant
  const [isProposingMeta, setIsProposingMeta] = useState<boolean>(false);
  const [isGeneratingStack, setIsGeneratingStack] = useState<boolean>(false);
  const [proposal, setProposal] = useState<MetaPromptProposalDto | null>(null);
  const [cascadingStack, setCascadingStack] = useState<CascadingPromptStackDto | null>(null);

  // State Trình Soạn Thảo (Active Editor Text)
  const [editorText, setEditorText] = useState<string>('');
  const [isSaving, setIsSaving] = useState<boolean>(false);
  const [saveMessage, setSaveMessage] = useState<string | null>(null);

  // Audit Nhất Quán
  const [auditReport, setAuditReport] = useState<AxiomaticAuditReportDto | null>(null);
  const [isAuditing, setIsAuditing] = useState<boolean>(false);
  const [copied, setCopied] = useState<boolean>(false);

  useEffect(() => {
    loadTemplatesAndAudit();
  }, []);

  const loadTemplatesAndAudit = async () => {
    setLoadingTemplates(true);
    try {
      const [tpls, audit] = await Promise.all([
        promptForgeService.getAllTemplates().catch((err) => {
          console.warn('[PromptForge] Failed to load templates:', err);
          return [];
        }),
        promptForgeService.auditConsistency().catch((err) => {
          console.warn('[PromptForge] Failed to run audit:', err);
          return null;
        }),
      ]);
      const safeTpls = Array.isArray(tpls) ? tpls : [];
      setTemplates(safeTpls);
      if (audit && typeof audit === 'object' && typeof audit.consistencyScore === 'number') {
        setAuditReport(audit);
      } else {
        setAuditReport(null);
      }

      // Mặc định chọn Tầng 0
      const meta = safeTpls.find((t) => t.layer === 0);
      if (meta) {
        setEditorText(meta.body);
      }
    } catch (err) {
      console.error('Lỗi tải dữ liệu Prompt Forge:', err);
      setTemplates([]);
      setAuditReport(null);
    } finally {
      setLoadingTemplates(false);
    }
  };

  const handleSelectLayer = (layerNum: number) => {
    setSelectedLayer(layerNum);
    const target = Array.isArray(templates) ? templates.find((t) => t.layer === layerNum) : undefined;
    if (target) {
      setEditorText(target.body);
    } else if (layerNum === 0 && proposal) {
      setEditorText(proposal.body);
    } else if (cascadingStack) {
      if (layerNum === 1) setEditorText(cascadingStack.constitutionLayer1);
      if (layerNum === 4) setEditorText(cascadingStack.phaseDirectivesLayer4);
      if (layerNum === 5) setEditorText(cascadingStack.outputContractLayer5);
    }
  };

  const handleProposeMeta = async () => {
    setIsProposingMeta(true);
    setSaveMessage(null);
    try {
      const res = await promptForgeService.proposeMetaPrompt({
        organizationDomain: domainFocus,
        coreAxioms: coreAxioms,
        rigorLevel: rigorLevel,
      });
      setProposal(res);
      if (selectedLayer === 0) {
        setEditorText(res.body);
      }
    } catch (err) {
      console.error('Lỗi đề xuất prompt tầng 0:', err);
    } finally {
      setIsProposingMeta(false);
    }
  };

  const handleGenerateStack = async () => {
    setIsGeneratingStack(true);
    setSaveMessage(null);
    try {
      const metaBody = proposal?.body || editorText;
      const res = await promptForgeService.generateCascadingStack({
        metaPromptBody: metaBody,
        domainFocus: domainFocus,
      });
      setCascadingStack(res);
      setSaveMessage('✨ Đã sinh đồng bộ toàn bộ cây 6 tầng! Hãy xem từng tầng và bấm Phê Chuẩn.');
    } catch (err) {
      console.error('Lỗi sinh cây cascading:', err);
    } finally {
      setIsGeneratingStack(false);
    }
  };

  const handleRunAudit = async () => {
    setIsAuditing(true);
    try {
      const report = await promptForgeService.auditConsistency();
      setAuditReport(report);
    } catch (err) {
      console.error('Lỗi audit:', err);
    } finally {
      setIsAuditing(false);
    }
  };

  const handleEnact = async () => {
    setIsSaving(true);
    setSaveMessage(null);
    try {
      const meta = selectedLayer === 0 ? editorText : (proposal?.body || templates.find(t => t.layer === 0)?.body || '');
      const constText = selectedLayer === 1 ? editorText : (cascadingStack?.constitutionLayer1 || templates.find(t => t.layer === 1)?.body || '');

      await promptForgeService.enactPromptStack({
        metaPromptLayer0: meta,
        constitutionLayer1: constText,
        charters: cascadingStack?.councilChartersLayer2,
        outputContractLayer5: cascadingStack?.outputContractLayer5,
        changeSummary: `Phê chuẩn qua Cognitive Prompt Forge Studio cho lĩnh vực [${domainFocus}]`,
      });

      setSaveMessage('🚀 ĐÃ PHÊ CHUẨN & HOT-RELOAD THÀNH CÔNG VÀO POSTGRESQL (0ms GIÁN ĐOẠN)!');
      await loadTemplatesAndAudit();
    } catch (err) {
      console.error('Lỗi phê chuẩn:', err);
      setSaveMessage('❌ Có lỗi xảy ra khi phê chuẩn prompt vào CSDL.');
    } finally {
      setIsSaving(false);
    }
  };

  const handleCopy = () => {
    navigator.clipboard.writeText(editorText);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const layerItems = [
    { num: 0, label: 'Tầng 0: Meta Architect', code: 'ARCHITECT_META', desc: 'Ý chí kiến tạo tối cao' },
    { num: 1, label: 'Tầng 1: Constitution', code: 'CONSTITUTION', desc: 'Hiến pháp biện chứng chung' },
    { num: 2, label: 'Tầng 2: Council Charters', code: 'CHARTERS', desc: 'Hiến chương nhận thức Viện' },
    { num: 3, label: 'Tầng 3: Seat Roles', code: 'SEAT_ROLES', desc: 'Chỉ thị 4 ghế chuyên gia' },
    { num: 4, label: 'Tầng 4: Phase Directives', code: 'PHASES', desc: 'Chỉ thị 6 giai đoạn tranh biện' },
    { num: 5, label: 'Tầng 5: Output Contracts', code: 'OUTPUT_CONTRACT', desc: 'Chuẩn hóa cấu trúc kết quả' },
  ];

  return (
    <div className="space-y-6">
      {/* Studio Header */}
      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-r from-indigo-950/80 via-slate-900 to-slate-950 border border-indigo-500/30 p-6 shadow-2xl">
        <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
          <div className="space-y-1">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-indigo-500/20 border border-indigo-500/40 text-indigo-300 text-xs font-bold uppercase tracking-wider">
              <Zap size={14} className="text-indigo-400" />
              <span>Cognitive Prompt Forge Studio • OCP Sovereign Layer</span>
            </div>
            <h2 className="text-2xl font-black bg-gradient-to-r from-indigo-200 via-sky-200 to-white bg-clip-text text-transparent">
              Quản Trị & Khởi Sinh Cây Prompt 6 Tầng Nhận Thức
            </h2>
            <p className="text-slate-400 text-xs sm:text-sm max-w-3xl">
              Làm chủ hoàn toàn ý chí của Đại Kiến Trúc Sư Tối Cao Tầng 0. Tự động đề xuất, lan truyền thác đổ (Cascading),
              thanh tra nhất quán tiên đề và Hot-Reload 0ms vào PostgreSQL.
            </p>
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={handleRunAudit}
              disabled={isAuditing}
              className="flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs font-bold bg-slate-800 hover:bg-slate-700 border border-slate-700 text-slate-200 transition-all cursor-pointer"
            >
              <RefreshCw size={14} className={isAuditing ? 'animate-spin text-cyan-400' : ''} />
              <span>Kiểm Định Tiên Đề ({auditReport ? `${Math.round(auditReport.consistencyScore * 100)}%` : '...'})</span>
            </button>
            <button
              onClick={handleEnact}
              disabled={isSaving}
              className="flex items-center gap-2 px-5 py-2.5 rounded-xl text-xs font-black bg-gradient-to-r from-indigo-500 to-cyan-500 hover:from-indigo-600 hover:to-cyan-600 text-white shadow-lg shadow-indigo-500/25 transition-all cursor-pointer"
            >
              <Zap size={14} />
              <span>{isSaving ? 'Đang Lưu...' : 'Phê Chuẩn & Hot-Reload'}</span>
            </button>
          </div>
        </div>

        {saveMessage && (
          <div className="mt-4 p-3 rounded-xl bg-indigo-950/60 border border-indigo-500/40 text-indigo-200 text-xs font-bold flex items-center gap-2 animate-fadeIn">
            <CheckCircle2 size={16} className="text-emerald-400" />
            <span>{saveMessage}</span>
          </div>
        )}
      </div>

      {/* Main Studio Grid: 2 Cột */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* CỘT TRÁI (7 Cột): Cây Phân Tầng & Trình Soạn Thảo Prompt */}
        <div className="lg:col-span-7 space-y-4">
          {/* Layer Selector Bar */}
          <div className="grid grid-cols-3 sm:grid-cols-6 gap-2">
            {layerItems.map((item) => {
              const isSelected = selectedLayer === item.num;
              const hasTpl = Array.isArray(templates) && templates.some((t) => t.layer === item.num);
              return (
                <button
                  key={item.num}
                  onClick={() => handleSelectLayer(item.num)}
                  className={`p-2.5 rounded-xl border text-left transition-all cursor-pointer ${
                    isSelected
                      ? 'bg-indigo-600/30 border-indigo-500 text-white shadow-md shadow-indigo-500/10'
                      : 'bg-slate-900/60 border-slate-800 text-slate-400 hover:border-slate-700 hover:text-slate-300'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <span className="text-[10px] font-black uppercase text-indigo-400">L{item.num}</span>
                    {hasTpl ? (
                      <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" title="Đã có trong DB" />
                    ) : (
                      <span className="w-1.5 h-1.5 rounded-full bg-amber-400" title="Chưa seed" />
                    )}
                  </div>
                  <div className="text-xs font-bold truncate mt-0.5">{item.label.split(':')[1]}</div>
                </button>
              );
            })}
          </div>

          {/* Prompt Editor Container */}
          <div className="rounded-2xl bg-slate-900 border border-slate-800 overflow-hidden shadow-xl flex flex-col">
            <div className="px-4 py-3 bg-slate-950/80 border-b border-slate-800 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <FileCode2 size={16} className="text-indigo-400" />
                <span className="text-xs font-bold text-slate-200">
                  {layerItems.find((l) => l.num === selectedLayer)?.label}
                </span>
                <span className="text-[10px] px-2 py-0.5 rounded bg-slate-800 text-slate-400 font-mono">
                  {editorText.length} chars • ~{Math.ceil(editorText.length / 3.8)} tokens
                </span>
              </div>

              <div className="flex items-center gap-2">
                <button
                  onClick={handleCopy}
                  className="p-1.5 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 transition-colors cursor-pointer"
                  title="Sao chép"
                >
                  {copied ? <Check size={14} className="text-emerald-400" /> : <Copy size={14} />}
                </button>
              </div>
            </div>

            <textarea
              value={editorText}
              onChange={(e) => setEditorText(e.target.value)}
              rows={18}
              className="w-full p-4 bg-slate-950 font-mono text-xs text-slate-200 leading-relaxed focus:outline-none focus:ring-1 focus:ring-indigo-500/50 resize-y"
              placeholder="Nội dung system prompt của tầng này..."
            />

            <div className="px-4 py-2 bg-slate-950/90 border-t border-slate-800 flex items-center justify-between text-[11px] text-slate-400">
              <span>Hỗ trợ placeholder: &#123;&#123;council.*&#125;&#125;, &#123;&#123;seat.*&#125;&#125;, &#123;&#123;symposium.*&#125;&#125;</span>
              <span className="text-emerald-400 font-bold">● Nạp trực tiếp từ PostgreSQL</span>
            </div>
          </div>
        </div>

        {/* CỘT PHẢI (5 Cột): Trợ Lý AI Đề Xuất & Khởi Sinh Thác Đổ */}
        <div className="lg:col-span-5 space-y-4">
          <div className="rounded-2xl bg-slate-900 border border-indigo-500/20 p-5 space-y-4 shadow-xl">
            <div className="flex items-center gap-2">
              <Sparkles size={18} className="text-indigo-400" />
              <h3 className="text-sm font-black text-white">AI Metaprompt Assistant</h3>
            </div>

            <div className="space-y-3">
              <div>
                <label className="text-xs font-bold text-slate-300 block mb-1">
                  Miền Chuyên Ngành / Sứ Mệnh
                </label>
                <input
                  type="text"
                  value={domainFocus}
                  onChange={(e) => setDomainFocus(e.target.value)}
                  placeholder="VD: Y Khoa Lâm Sàng, Nông Nghiệp Thông Minh, Fintech..."
                  className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-800 text-xs text-slate-200 focus:border-indigo-500 focus:outline-none"
                />
              </div>

              <div>
                <label className="text-xs font-bold text-slate-300 block mb-1">
                  Chuẩn Mực Biện Chứng
                </label>
                <select
                  value={rigorLevel}
                  onChange={(e) => setRigorLevel(Number(e.target.value) as DialecticalRigorLevel)}
                  className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-800 text-xs text-slate-200 focus:border-indigo-500 focus:outline-none"
                >
                  {Object.entries(RIGOR_LABELS).map(([k, v]) => (
                    <option key={k} value={k}>
                      {v.name}
                    </option>
                  ))}
                </select>
                <p className="text-[11px] text-slate-400 mt-1 italic">
                  {RIGOR_LABELS[rigorLevel]?.desc}
                </p>
              </div>

              <div>
                <label className="text-xs font-bold text-slate-300 block mb-1">
                  Tiên Đề Cốt Lõi / Ràng Buộc
                </label>
                <textarea
                  value={coreAxioms}
                  onChange={(e) => setCoreAxioms(e.target.value)}
                  rows={2}
                  placeholder="Các nguyên tắc bất biến cần nhúng vào Tầng 0..."
                  className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-800 text-xs text-slate-200 focus:border-indigo-500 focus:outline-none"
                />
              </div>

              <div className="grid grid-cols-2 gap-2 pt-1">
                <button
                  onClick={handleProposeMeta}
                  disabled={isProposingMeta}
                  className="w-full py-2.5 rounded-xl text-xs font-bold bg-indigo-900/60 hover:bg-indigo-800/80 border border-indigo-500/40 text-indigo-200 flex items-center justify-center gap-1.5 transition-all cursor-pointer"
                >
                  <Brain size={14} className={isProposingMeta ? 'animate-pulse text-indigo-400' : ''} />
                  <span>{isProposingMeta ? 'Đang Tạo...' : 'Đề Xuất Tầng 0'}</span>
                </button>
                <button
                  onClick={handleGenerateStack}
                  disabled={isGeneratingStack}
                  className="w-full py-2.5 rounded-xl text-xs font-bold bg-cyan-900/60 hover:bg-cyan-800/80 border border-cyan-500/40 text-cyan-200 flex items-center justify-center gap-1.5 transition-all cursor-pointer"
                >
                  <Layers size={14} className={isGeneratingStack ? 'animate-spin text-cyan-400' : ''} />
                  <span>{isGeneratingStack ? 'Đang Khởi Sinh...' : 'Thác Đổ 6 Tầng'}</span>
                </button>
              </div>
            </div>

            {/* Rationale & Extracted Axioms */}
            {proposal && (
              <div className="p-3.5 rounded-xl bg-slate-950/80 border border-slate-800 space-y-2 text-xs">
                <div className="font-bold text-indigo-300 flex items-center gap-1.5">
                  <CheckCircle2 size={13} className="text-emerald-400" />
                  <span>Căn Cứ Nhận Thức Luận:</span>
                </div>
                <p className="text-slate-400 text-[11px] leading-relaxed">{proposal.rationale}</p>
                <div className="flex flex-wrap gap-1 pt-1">
                  {proposal.extractedAxioms.map((ax, idx) => (
                    <span
                      key={idx}
                      className="px-2 py-0.5 rounded-md bg-indigo-950/60 border border-indigo-500/30 text-[10px] text-indigo-300 font-semibold"
                    >
                      {ax}
                    </span>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Audit Consistency Panel */}
          {auditReport && (
            <div className="rounded-2xl bg-slate-900 border border-slate-800 p-5 space-y-3 shadow-xl">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Shield size={16} className="text-emerald-400" />
                  <h4 className="text-xs font-black text-white">Thanh Tra Nhất Quán Tiên Đề</h4>
                </div>
                <span
                  className={`text-xs font-black px-2.5 py-0.5 rounded-full ${
                    auditReport.isFullyConsistent
                      ? 'bg-emerald-950 text-emerald-300 border border-emerald-500/40'
                      : 'bg-amber-950 text-amber-300 border border-amber-500/40'
                  }`}
                >
                  {Math.round(auditReport.consistencyScore * 100)}% Nhất Quán
                </span>
              </div>

              {Array.isArray(auditReport.inconsistencies) && auditReport.inconsistencies.length > 0 ? (
                <div className="space-y-2">
                  {auditReport.inconsistencies.map((inc, i) => (
                    <div
                      key={i}
                      className="p-2.5 rounded-xl bg-amber-950/30 border border-amber-500/30 text-[11px] space-y-1"
                    >
                      <div className="flex items-center justify-between font-bold text-amber-300">
                        <span>{inc.layerName}</span>
                        <span className="text-[9px] uppercase px-1.5 py-0.2 rounded bg-amber-900/60">
                          {inc.severity}
                        </span>
                      </div>
                      <p className="text-slate-300">{inc.conflictSummary}</p>
                      <p className="text-amber-400/80 italic">💡 {inc.remediationSuggestion}</p>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="p-3 rounded-xl bg-emerald-950/30 border border-emerald-500/30 text-emerald-300 text-xs flex items-center gap-2">
                  <CheckCircle2 size={16} className="text-emerald-400" />
                  <span>Tuyệt hảo! Toàn bộ 6 tầng prompt đồng nhất 100%, 0 xung đột tiên đề.</span>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
