import React, { useState, useEffect, useRef } from 'react';
import { 
  X, Brain, Sparkles, BookOpen, Layers, ListTodo, FileText, CheckCircle2, 
  Send, ExternalLink, RefreshCw, BarChart2, ShieldCheck, ChevronRight,
  Loader2, ArrowRight, Bookmark, Compass, Upload, Link2, Trash2, 
  AlertTriangle, MessageSquare, Bot, User, Sliders, Shield, Scale, HelpCircle
} from 'lucide-react';
import { 
  notebookService, 
  NotebookDetailDto, 
  NotebookChapterDto,
  NotebookSourceDto,
  NotebookChatMessageDto
} from '../../../services/notebook.service';
import { learnerQuizService } from '../../../services/learner-quiz.service';
import { aiSwarmService, WolfPackSwarmAuditResult } from '../../../services/ai-swarm.service';
import type { BankTopic } from '../questions/types';
import { MathRenderer } from '../../../components/common/MathRenderer';

interface NotebookDualPaneStudioProps {
  notebookId: string;
  isOpen: boolean;
  onClose: () => void;
  onRefreshList: () => void;
}

export function NotebookDualPaneStudio({
  notebookId,
  isOpen,
  onClose,
  onRefreshList,
}: NotebookDualPaneStudioProps) {
  const [loading, setLoading] = useState(true);
  const [detail, setDetail] = useState<NotebookDetailDto | null>(null);
  const [selectedChapter, setSelectedChapter] = useState<NotebookChapterDto | null>(null);
  const [selectedSource, setSelectedSource] = useState<NotebookSourceDto | null>(null);
  
  // 5 Cognitive Tabs: 'topics' | 'sources' | 'conflicts' | 'persona' | 'exam'
  const [activeTab, setActiveTab] = useState<'topics' | 'sources' | 'conflicts' | 'persona' | 'exam'>('topics');
  
  // Sources Hub state
  const [uploadingSource, setUploadingSource] = useState(false);
  const [sourceAuthLevel, setSourceAuthLevel] = useState<number>(2);
  const [urlInput, setUrlInput] = useState('');
  const [urlTitle, setUrlTitle] = useState('');
  const [showUrlModal, setShowUrlModal] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Persona Chat state
  const [chatMessages, setChatMessages] = useState<NotebookChatMessageDto[]>([]);
  const [chatInput, setChatInput] = useState('');
  const [sendingChat, setSendingChat] = useState(false);
  const [selectedPersonaRole, setSelectedPersonaRole] = useState<number>(1);
  const [personaNameInput, setPersonaNameInput] = useState('');
  const [customPromptInput, setCustomPromptInput] = useState('');
  const [updatingPersona, setUpdatingPersona] = useState(false);
  const chatBottomRef = useRef<HTMLDivElement>(null);

  // Exam Studio state
  const [singlePercent, setSinglePercent] = useState(40);
  const [crossPercent, setCrossPercent] = useState(60);
  const [totalQuestionsCount, setTotalQuestionsCount] = useState(10);
  const [targetBloomLevel, setTargetBloomLevel] = useState(5);
  const [generatingQuiz, setGeneratingQuiz] = useState(false);
  const [generatedQuestions, setGeneratedQuestions] = useState<any[]>([]);
  
  // Bank publishing state
  const [topics, setTopics] = useState<BankTopic[]>([]);
  const [selectedTopicId, setSelectedTopicId] = useState<string>('');
  const [publishing, setPublishing] = useState(false);
  const [publishMessage, setPublishMessage] = useState<string | null>(null);

  // Swarm Audit State
  const [auditingQuestionIdx, setAuditingQuestionIdx] = useState<number | null>(null);
  const [swarmAuditResult, setSwarmAuditResult] = useState<WolfPackSwarmAuditResult | null>(null);
  const [showSwarmModal, setShowSwarmModal] = useState(false);

  useEffect(() => {
    if (isOpen && notebookId) {
      loadNotebookDetail();
      loadTopics();
    }
  }, [isOpen, notebookId]);

  useEffect(() => {
    chatBottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [chatMessages]);

  const loadNotebookDetail = async () => {
    setLoading(true);
    try {
      const data = await notebookService.getNotebookById(notebookId);
      setDetail(data);
      if (data.chapters && data.chapters.length > 0) {
        setSelectedChapter(data.chapters[0]);
      }
      if (data.sources && data.sources.length > 0) {
        setSelectedSource(data.sources[0]);
      }
      if (data.chatMessages) {
        setChatMessages(data.chatMessages);
      }
      setSelectedPersonaRole(data.personaRole || 1);
      setPersonaNameInput(data.personaName || 'Cố Vấn Tri Thức Trưởng');
      setCustomPromptInput(data.customPersonaPrompt || '');
    } catch (err) {
      console.error('Lỗi tải chi tiết notebook:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadTopics = async () => {
    try {
      const data = await learnerQuizService.getTopics();
      setTopics(data);
      if (data.length > 0) {
        setSelectedTopicId(data[0].id);
      }
    } catch (err) {
      console.error('Lỗi tải danh mục topics:', err);
    }
  };

  const handleAddSourceFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setUploadingSource(true);
    try {
      await notebookService.addSourceFile(notebookId, file, sourceAuthLevel);
      await loadNotebookDetail();
      onRefreshList();
    } catch (err) {
      console.error('Lỗi tải thêm tài liệu nguồn:', err);
    } finally {
      setUploadingSource(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleAddSourceUrl = async () => {
    if (!urlInput.trim()) return;
    setUploadingSource(true);
    try {
      await notebookService.addSourceUrl(notebookId, {
        url: urlInput.trim(),
        title: urlTitle.trim() || undefined,
        authorityLevel: sourceAuthLevel
      });
      setShowUrlModal(false);
      setUrlInput('');
      setUrlTitle('');
      await loadNotebookDetail();
      onRefreshList();
    } catch (err) {
      console.error('Lỗi nạp Web URL:', err);
    } finally {
      setUploadingSource(false);
    }
  };

  const handleRemoveSource = async (sourceId: string) => {
    if (!window.confirm('Bạn có chắc chắn muốn gỡ bỏ tài liệu nguồn này?')) return;
    try {
      await notebookService.removeSource(notebookId, sourceId);
      await loadNotebookDetail();
      onRefreshList();
    } catch (err) {
      console.error('Lỗi gỡ bỏ tài liệu nguồn:', err);
    }
  };

  const handleUpdatePersona = async () => {
    setUpdatingPersona(true);
    try {
      await notebookService.updatePersona(notebookId, {
        personaName: personaNameInput,
        personaRole: selectedPersonaRole,
        customPersonaPrompt: customPromptInput
      });
      await loadNotebookDetail();
    } catch (err) {
      console.error('Lỗi cập nhật vai trò:', err);
    } finally {
      setUpdatingPersona(false);
    }
  };

  const handleSendChat = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (!chatInput.trim() || sendingChat) return;

    const userText = chatInput.trim();
    setChatInput('');
    setSendingChat(true);

    const tempUserMsg: NotebookChatMessageDto = {
      id: 'temp-' + Date.now(),
      notebookId,
      sender: 'User',
      roleName: 'Bạn',
      message: userText,
      citationsJson: '[]',
      createdAt: new Date().toISOString()
    };
    setChatMessages(prev => [...prev, tempUserMsg]);

    try {
      const response = await notebookService.sendChatMessage(notebookId, userText);
      setChatMessages(prev => [...prev.filter(m => m.id !== tempUserMsg.id), tempUserMsg, response]);
    } catch (err) {
      console.error('Lỗi gửi tin nhắn chat:', err);
    } finally {
      setSendingChat(false);
    }
  };

  const handleGenerateCrossQuiz = async () => {
    setGeneratingQuiz(true);
    try {
      const questions = await notebookService.generateCrossQuiz(notebookId, {
        totalQuestionCount: totalQuestionsCount,
        singleSourcePercent: singlePercent,
        crossSourcePercent: crossPercent,
        targetBloomLevel: targetBloomLevel
      });
      setGeneratedQuestions(questions);
      setActiveTab('exam');
      onRefreshList();
    } catch (err) {
      console.error('Lỗi sinh câu hỏi tình huống liên nguồn:', err);
    } finally {
      setGeneratingQuiz(false);
    }
  };

  const handlePublishToBank = async () => {
    if (!selectedTopicId) return;
    setPublishing(true);
    setPublishMessage(null);
    try {
      const res = await notebookService.publishToBank(notebookId, {
        targetBankTopicId: selectedTopicId,
        chapterId: selectedChapter?.id,
      });
      setPublishMessage(res.message);
      onRefreshList();
    } catch (err) {
      console.error('Lỗi xuất bản ngân hàng đề:', err);
    } finally {
      setPublishing(false);
    }
  };

  const handleAuditQuestion = async (q: any, idx: number) => {
    try {
      setAuditingQuestionIdx(idx);
      const optionsText = q.options?.map((opt: string, i: number) => `${String.fromCharCode(65 + i)}. ${opt}`).join('\n') || '';
      const fullPrompt = `Câu hỏi:\n${q.content}\n\nCác phương án:\n${optionsText}`;
      const result = await aiSwarmService.auditWithSwarm({
        prompt: fullPrompt,
        context: selectedChapter?.rawContent || detail?.description,
        proposedAnswer: q.options?.[0] || 'A',
        questionContent: q.content,
      });
      setSwarmAuditResult(result);
      setShowSwarmModal(true);
    } catch (err) {
      console.error('Lỗi thẩm định bầy sói:', err);
      alert('Thẩm định bầy sói gặp lỗi kết nối.');
    } finally {
      setAuditingQuestionIdx(null);
    }
  };

  const getAuthorityBadge = (level: number) => {
    switch (level) {
      case 4:
        return <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-red-950/60 text-red-400 border border-red-800"><Shield className="w-3 h-3" /> Tối Cao (Luật)</span>;
      case 3:
        return <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-950/60 text-amber-400 border border-amber-800"><Scale className="w-3 h-3" /> Quy Chuẩn (Thông tư)</span>;
      case 2:
        return <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-blue-950/60 text-blue-400 border border-blue-800"><FileText className="w-3 h-3" /> Nội Bộ (SOP)</span>;
      default:
        return <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-emerald-950/60 text-emerald-400 border border-emerald-800"><Bookmark className="w-3 h-3" /> Tham Khảo</span>;
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 bg-black/85 backdrop-blur-md flex flex-col overflow-hidden animate-in fade-in duration-200">
      {/* ── HEADER CỐT LÕI ────────────────────────────────────────── */}
      <header className="h-16 px-6 bg-slate-900/90 border-b border-slate-800 flex items-center justify-between flex-shrink-0">
        <div className="flex items-center gap-3">
          <div className="p-2 rounded-xl bg-gradient-to-tr from-sky-500/20 to-indigo-500/20 border border-sky-500/30 text-sky-400">
            <Brain className="w-5 h-5 animate-pulse" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-base font-bold text-white tracking-wide truncate max-w-md">
                {detail?.title || 'Không Gian Tri Thức Hub'}
              </h2>
              <span className="px-2 py-0.5 text-xs font-medium rounded-full bg-sky-950 text-sky-400 border border-sky-800">
                {detail?.domainCode || 'GENERAL'}
              </span>
            </div>
            <p className="text-xs text-slate-400">
              {detail?.sources?.length || 0} Tài liệu nguồn • {detail?.chapters?.length || 0} Chủ đề nhận thức • {detail?.totalQuestions || 0} Câu hỏi
            </p>
          </div>
        </div>

        {/* 5 COGNITIVE TABS NAV */}
        <div className="flex items-center gap-1 bg-slate-950 p-1 rounded-xl border border-slate-800">
          <button
            onClick={() => setActiveTab('topics')}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-all flex items-center gap-1.5 ${
              activeTab === 'topics' ? 'bg-sky-600 text-white shadow-sm' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <Layers className="w-3.5 h-3.5" />
            Chủ Đề ({detail?.chapters?.length || 0})
          </button>
          <button
            onClick={() => setActiveTab('sources')}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-all flex items-center gap-1.5 ${
              activeTab === 'sources' ? 'bg-sky-600 text-white shadow-sm' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <BookOpen className="w-3.5 h-3.5" />
            Nguồn ({detail?.sources?.length || 0})
          </button>
          <button
            onClick={() => setActiveTab('conflicts')}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-all flex items-center gap-1.5 ${
              activeTab === 'conflicts' ? 'bg-amber-600 text-white shadow-sm' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <AlertTriangle className="w-3.5 h-3.5" />
            Xung Đột
          </button>
          <button
            onClick={() => setActiveTab('persona')}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-all flex items-center gap-1.5 ${
              activeTab === 'persona' ? 'bg-purple-600 text-white shadow-sm' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <Bot className="w-3.5 h-3.5" />
            Hội Thoại Nhập Vai
          </button>
          <button
            onClick={() => setActiveTab('exam')}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-all flex items-center gap-1.5 ${
              activeTab === 'exam' ? 'bg-emerald-600 text-white shadow-sm' : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <Sparkles className="w-3.5 h-3.5" />
            Đề Thi Tự Động
          </button>
        </div>

        <button
          onClick={onClose}
          className="p-2 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 transition-colors"
        >
          <X className="w-5 h-5" />
        </button>
      </header>

      {/* ── NỘI DUNG 2 NỬA MÀN HÌNH (DUAL-PANE LAYOUT) ────────────── */}
      <div className="flex-1 flex overflow-hidden">
        {/* NỬA TRÁI: TRÌNH XEM NGUỒN TÀI LIỆU TRỰC TIẾP */}
        <div className="w-1/2 border-r border-slate-800 flex flex-col bg-slate-950">
          <div className="h-10 px-4 bg-slate-900/60 border-b border-slate-800 flex items-center justify-between text-xs text-slate-300">
            <div className="flex items-center gap-2 truncate">
              <span className="font-semibold text-slate-400">Đang đọc:</span>
              <span className="text-white font-medium truncate max-w-xs">
                {selectedSource?.title || detail?.originalFileName || 'Tài liệu nguồn'}
              </span>
              {selectedSource && getAuthorityBadge(selectedSource.authority)}
            </div>

            {/* Chọn nguồn khác để đọc */}
            {detail?.sources && detail.sources.length > 1 && (
              <select
                value={selectedSource?.id || ''}
                onChange={(e) => {
                  const s = detail.sources.find(src => src.id === e.target.value);
                  if (s) setSelectedSource(s);
                }}
                className="bg-slate-800 text-slate-200 border border-slate-700 rounded px-2 py-0.5 text-xs focus:outline-none"
              >
                {detail.sources.map(s => (
                  <option key={s.id} value={s.id}>{s.title} ({s.type})</option>
                ))}
              </select>
            )}
          </div>

          <div className="flex-1 overflow-auto bg-slate-900/40 p-4">
            {selectedSource?.type === 'Pdf' || (!selectedSource && detail?.sourcePdfUrl) ? (
              <iframe
                src={selectedSource?.sourceUrl || detail?.sourcePdfUrl}
                title="Tài liệu PDF"
                className="w-full h-full rounded-xl border border-slate-800 shadow-inner bg-slate-950"
              />
            ) : (
              <div className="max-w-2xl mx-auto bg-slate-900/80 border border-slate-800 rounded-xl p-6 shadow-xl">
                <div className="flex items-center justify-between pb-4 border-b border-slate-800 mb-4">
                  <h3 className="text-base font-bold text-white">{selectedSource?.title}</h3>
                  <span className="text-xs text-slate-400">Định dạng: {selectedSource?.type}</span>
                </div>
                <div className="text-sm text-slate-300 whitespace-pre-wrap font-sans leading-relaxed">
                  {selectedChapter?.rawContent || 'Chọn một chương mục hoặc tài liệu để đối soát toàn văn.'}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* NỬA PHẢI: TRUNG TÂM NHẬN THỨC & KHẢO THÍ SỐNG (5 TABS) */}
        <div className="w-1/2 flex flex-col bg-slate-900/50 overflow-hidden">
          {loading ? (
            <div className="flex-1 flex flex-col items-center justify-center gap-3 text-slate-400">
              <Loader2 className="w-8 h-8 animate-spin text-sky-500" />
              <p className="text-sm">Đang tải trung tâm nhận thức AegisNotebook...</p>
            </div>
          ) : (
            <div className="flex-1 flex flex-col overflow-hidden">
              {/* TAB 1: CÂY CHỦ ĐỀ NHẬN THỨC */}
              {activeTab === 'topics' && (
                <div className="flex-1 flex overflow-hidden">
                  <div className="w-64 border-r border-slate-800 overflow-y-auto p-3 space-y-1.5 bg-slate-950/60">
                    <p className="text-xs font-semibold text-slate-400 px-2 py-1 uppercase tracking-wider">
                      Cây Phân Đoạn Chương Mục
                    </p>
                    {detail?.chapters?.map((ch) => (
                      <button
                        key={ch.id}
                        onClick={() => setSelectedChapter(ch)}
                        className={`w-full text-left p-3 rounded-xl transition-all border ${
                          selectedChapter?.id === ch.id
                            ? 'bg-sky-950/70 border-sky-600/60 text-white shadow-sm'
                            : 'bg-slate-900/40 border-slate-800/60 text-slate-300 hover:bg-slate-800/50'
                        }`}
                      >
                        <div className="flex items-center justify-between text-xs mb-1">
                          <span className="font-bold text-sky-400">Phần #{ch.orderIndex}</span>
                          <span className="text-slate-500">Tr. {ch.startPage}-{ch.endPage}</span>
                        </div>
                        <h4 className="text-xs font-medium line-clamp-2 leading-snug">{ch.title}</h4>
                      </button>
                    ))}
                  </div>

                  <div className="flex-1 overflow-y-auto p-6 space-y-6">
                    {selectedChapter ? (
                      <>
                        <div className="border-b border-slate-800 pb-4">
                          <div className="flex items-center gap-2 mb-2">
                            <span className="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-sky-950 text-sky-400 border border-sky-800">
                              Phần {selectedChapter.orderIndex}
                            </span>
                            <span className="text-xs text-slate-400">
                              Trang {selectedChapter.startPage} – {selectedChapter.endPage}
                            </span>
                          </div>
                          <h3 className="text-xl font-bold text-white leading-tight">
                            {selectedChapter.title}
                          </h3>
                        </div>

                        <div>
                          <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2 flex items-center gap-2">
                            <Brain className="w-4 h-4 text-sky-400" />
                            Tóm Lược Nhận Thức Chuyên Sâu
                          </h4>
                          <div className="p-4 rounded-xl bg-slate-950 border border-slate-800 text-sm text-slate-300 leading-relaxed">
                            <MathRenderer content={selectedChapter.summary} />
                          </div>
                        </div>

                        {selectedChapter.keyPoints && selectedChapter.keyPoints.length > 0 && (
                          <div>
                            <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-3 flex items-center gap-2">
                              <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                              Ý Chính & Quy Chuẩn Cốt Tử
                            </h4>
                            <div className="space-y-2">
                              {selectedChapter.keyPoints.map((pt, idx) => (
                                <div key={idx} className="flex items-start gap-3 p-3 rounded-lg bg-slate-950/70 border border-slate-800/80 text-xs text-slate-300">
                                  <span className="flex-shrink-0 w-5 h-5 rounded-full bg-emerald-950 border border-emerald-700 text-emerald-400 flex items-center justify-center font-bold text-xs">
                                    {idx + 1}
                                  </span>
                                  <div className="flex-1">
                                    <MathRenderer content={pt} />
                                  </div>
                                </div>
                              ))}
                            </div>
                          </div>
                        )}
                      </>
                    ) : (
                      <div className="h-full flex items-center justify-center text-slate-500 text-sm">
                        Chọn một chương mục từ cây phân đoạn bên trái để bắt đầu.
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* TAB 2: TRẠM NGUỒN TÀI LIỆU (SOURCES HUB) */}
              {activeTab === 'sources' && (
                <div className="flex-1 overflow-y-auto p-6 space-y-6">
                  <div className="flex items-center justify-between pb-4 border-b border-slate-800">
                    <div>
                      <h3 className="text-lg font-bold text-white flex items-center gap-2">
                        <BookOpen className="w-5 h-5 text-sky-400" />
                        Trạm Nguồn Tài Liệu (Universal Sources Hub)
                      </h3>
                      <p className="text-xs text-slate-400">
                        Quản lý toàn bộ tài liệu nguồn trong không gian tri thức kèm cấp bậc thẩm quyền.
                      </p>
                    </div>

                    <div className="flex items-center gap-2">
                      <select
                        value={sourceAuthLevel}
                        onChange={(e) => setSourceAuthLevel(Number(e.target.value))}
                        className="bg-slate-950 border border-slate-800 rounded-lg px-2.5 py-1.5 text-xs text-slate-300 focus:outline-none"
                      >
                        <option value={4}>🔴 Cấp 4: Tối Cao (Luật)</option>
                        <option value={3}>🟠 Cấp 3: Quy Chuẩn (Thông tư)</option>
                        <option value={2}>🟡 Cấp 2: Nội Bộ (SOP)</option>
                        <option value={1}>🟢 Cấp 1: Tham Khảo</option>
                      </select>

                      <input
                        type="file"
                        ref={fileInputRef}
                        onChange={handleAddSourceFile}
                        accept=".pdf,.docx,.xlsx,.xls,.txt"
                        className="hidden"
                      />

                      <button
                        onClick={() => fileInputRef.current?.click()}
                        disabled={uploadingSource}
                        className="px-3 py-1.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-semibold flex items-center gap-1.5 transition-all disabled:opacity-50"
                      >
                        {uploadingSource ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Upload className="w-3.5 h-3.5" />}
                        Tải Tệp Lên
                      </button>

                      <button
                        onClick={() => setShowUrlModal(true)}
                        className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-medium flex items-center gap-1.5 border border-slate-700 transition-all"
                      >
                        <Link2 className="w-3.5 h-3.5" />
                        Nạp URL
                      </button>
                    </div>
                  </div>

                  {/* DANH SÁCH TÀI LIỆU NGUỒN */}
                  <div className="space-y-3">
                    {detail?.sources?.map((s) => (
                      <div
                        key={s.id}
                        className="p-4 rounded-xl bg-slate-950 border border-slate-800 hover:border-slate-700 transition-all flex items-center justify-between"
                      >
                        <div className="flex items-center gap-3">
                          <div className="p-2.5 rounded-xl bg-slate-900 border border-slate-800 text-sky-400">
                            <FileText className="w-5 h-5" />
                          </div>
                          <div>
                            <div className="flex items-center gap-2 mb-1">
                              <h4 className="text-sm font-bold text-white">{s.title}</h4>
                              {getAuthorityBadge(s.authority)}
                              <span className="px-2 py-0.5 rounded text-xs bg-slate-900 text-slate-400 border border-slate-800">
                                {s.type}
                              </span>
                            </div>
                            <p className="text-xs text-slate-400">
                              {s.originalFileName} • {(s.fileSizeBytes / 1024).toFixed(0)} KB • {s.totalPages} trang • Nạp lúc {new Date(s.uploadedAt).toLocaleDateString()}
                            </p>
                          </div>
                        </div>

                        <div className="flex items-center gap-2">
                          <button
                            onClick={() => setSelectedSource(s)}
                            className="px-3 py-1.5 rounded-lg bg-slate-900 hover:bg-slate-800 text-sky-400 text-xs font-medium border border-slate-800"
                          >
                            Đọc Tài Liệu
                          </button>
                          <button
                            onClick={() => handleRemoveSource(s.id)}
                            className="p-2 rounded-lg bg-slate-900 hover:bg-red-950/60 text-slate-400 hover:text-red-400 border border-slate-800 hover:border-red-800 transition-all"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* TAB 3: MA TRẬN XUNG ĐỘT & ĐỐI CHIẾU */}
              {activeTab === 'conflicts' && (
                <div className="flex-1 overflow-y-auto p-6 space-y-6">
                  <div className="pb-4 border-b border-slate-800">
                    <h3 className="text-lg font-bold text-white flex items-center gap-2">
                      <AlertTriangle className="w-5 h-5 text-amber-400" />
                      Cảm Biến Xung Đột & Đối Soát Thẩm Quyền (Conflict Matrix)
                    </h3>
                    <p className="text-xs text-slate-400">
                      Tự động phân giải khác biệt dữ liệu giữa văn bản Pháp lý cấp cao và Sổ tay tác nghiệp nội bộ.
                    </p>
                  </div>

                  {(() => {
                    let conflicts: any[] = [];
                    try {
                      conflicts = JSON.parse(detail?.conflictReportJson || '[]');
                    } catch { }

                    if (conflicts.length === 0) {
                      return (
                        <div className="p-8 rounded-xl bg-slate-950 border border-slate-800 text-center space-y-3">
                          <CheckCircle2 className="w-10 h-10 text-emerald-400 mx-auto" />
                          <h4 className="text-base font-bold text-white">Chưa ghi nhận xung đột thẩm quyền</h4>
                          <p className="text-xs text-slate-400 max-w-md mx-auto">
                            Khi nạp từ 2 tài liệu nguồn trở lên với các cấp thẩm quyền khác nhau (Luật vs SOP), AI sẽ tự động phân tích và đối soát tại đây.
                          </p>
                        </div>
                      );
                    }

                    return (
                      <div className="space-y-4">
                        {conflicts.map((c, idx) => (
                          <div key={idx} className="p-4 rounded-xl bg-slate-950 border border-amber-900/60 space-y-3">
                            <div className="flex items-center justify-between">
                              <span className="px-2.5 py-0.5 rounded-full text-xs font-bold bg-amber-950 text-amber-400 border border-amber-800">
                                Mức độ: {c.severity}
                              </span>
                              <span className="text-xs text-slate-500">Đối chiếu tự động OCP</span>
                            </div>
                            <p className="text-sm font-medium text-white">{c.summary}</p>
                            <div className="p-3 rounded-lg bg-slate-900/80 border border-slate-800 text-xs text-slate-300">
                              <span className="font-semibold text-emerald-400">Khuyến nghị giải quyết: </span>
                              {c.recommendation}
                            </div>
                          </div>
                        ))}
                      </div>
                    );
                  })()}
                </div>
              )}

              {/* TAB 4: HỘI THOẠI NHẬP VAI & VẤN ĐÁP SOCRATIC */}
              {activeTab === 'persona' && (
                <div className="flex-1 flex flex-col overflow-hidden bg-slate-950">
                  {/* PERSONA CONFIG HEADER */}
                  <div className="p-3 bg-slate-900/80 border-b border-slate-800 flex items-center justify-between flex-shrink-0">
                    <div className="flex items-center gap-2">
                      <Bot className="w-4 h-4 text-purple-400" />
                      <select
                        value={selectedPersonaRole}
                        onChange={(e) => {
                          const r = Number(e.target.value);
                          setSelectedPersonaRole(r);
                          if (r === 1) setPersonaNameInput('Gia sư Socratic Tri Thức');
                          if (r === 2) setPersonaNameInput('Thanh Tra Pháp Chế & Rủi Ro');
                          if (r === 3) setPersonaNameInput('Cố Vấn Chiến Lược Điều Hành');
                          if (r === 4) setPersonaNameInput('Trưởng Ban Giám Khảo Khảo Thí');
                          if (r === 5) setPersonaNameInput('Chuyên Gia Kỹ Thuật Đầu Ngành');
                        }}
                        className="bg-slate-950 border border-slate-700 rounded-lg px-2.5 py-1 text-xs text-white focus:outline-none"
                      >
                        <option value={1}>🎓 Gia Sư Socratic (Gợi mở phản biện)</option>
                        <option value={2}>⚖️ Thanh Tra Pháp Chế (Soi xét rủi ro)</option>
                        <option value={3}>🏛️ Cố Vấn Điều Hành (Chiến lược vĩ mô)</option>
                        <option value={4}>🏆 Trưởng Ban Giám Khảo (Thi vấn đáp)</option>
                        <option value={5}>🛠️ Chuyên Gia Kỹ Thuật (Chuyên sâu)</option>
                        <option value={99}>⚙️ Tùy Biến Tự Do</option>
                      </select>
                      <button
                        onClick={handleUpdatePersona}
                        disabled={updatingPersona}
                        className="px-2.5 py-1 rounded bg-purple-600 hover:bg-purple-500 text-white text-xs font-medium disabled:opacity-50"
                      >
                        {updatingPersona ? 'Lưu...' : 'Nạp Vai Trò'}
                      </button>
                    </div>

                    <button
                      onClick={async () => {
                        if (window.confirm('Đặt lại toàn bộ cuộc hội thoại?')) {
                          await notebookService.clearChatHistory(notebookId);
                          setChatMessages([]);
                        }
                      }}
                      className="text-xs text-slate-400 hover:text-red-400 transition-colors"
                    >
                      Xóa lịch sử
                    </button>
                  </div>

                  {/* CHAT MESSAGES BODY */}
                  <div className="flex-1 overflow-y-auto p-4 space-y-4">
                    {chatMessages.length === 0 && (
                      <div className="h-full flex flex-col items-center justify-center text-center p-6 space-y-3 text-slate-400">
                        <Bot className="w-12 h-12 text-purple-400 animate-bounce" />
                        <h4 className="text-base font-bold text-white">
                          Sẵn sàng đàm đạo cùng {personaNameInput || 'Persona'}
                        </h4>
                        <p className="text-xs max-w-sm">
                          Hãy đặt một câu hỏi, bài toán hoặc tình huống thực tế. Persona sẽ căn cứ vào toàn bộ tài liệu nguồn để đối đáp và phản biện cùng bạn!
                        </p>
                      </div>
                    )}

                    {chatMessages.map((msg) => (
                      <div
                        key={msg.id}
                        className={`flex gap-3 ${msg.sender === 'User' ? 'justify-end' : 'justify-start'}`}
                      >
                        {msg.sender === 'Assistant' && (
                          <div className="w-8 h-8 rounded-full bg-purple-950 border border-purple-800 text-purple-400 flex items-center justify-center flex-shrink-0 text-xs font-bold">
                            <Bot className="w-4 h-4" />
                          </div>
                        )}

                        <div className={`max-w-lg rounded-2xl p-4 text-xs leading-relaxed ${
                          msg.sender === 'User'
                            ? 'bg-sky-600 text-white'
                            : 'bg-slate-900 border border-slate-800 text-slate-200'
                        }`}>
                          <div className="flex items-center justify-between pb-1.5 mb-1.5 border-b border-white/10 text-xs opacity-75">
                            <span className="font-semibold">{msg.roleName}</span>
                            <span>{new Date(msg.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                          </div>
                          
                          <div className="whitespace-pre-wrap font-sans text-xs">
                            <MathRenderer content={msg.message} />
                          </div>

                          {/* Grounding Citations */}
                          {msg.citationsJson && msg.citationsJson !== '[]' && (
                            <div className="mt-3 pt-2 border-t border-slate-800 flex flex-wrap gap-1.5">
                              {(() => {
                                try {
                                  const cits = JSON.parse(msg.citationsJson);
                                  return cits.map((cit: any, cIdx: number) => (
                                    <span
                                      key={cIdx}
                                      className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs bg-slate-950 text-sky-400 border border-sky-800 cursor-pointer hover:bg-sky-950"
                                    >
                                      <Bookmark className="w-2.5 h-2.5" />
                                      {cit.sourceName}
                                    </span>
                                  ));
                                } catch { return null; }
                              })()}
                            </div>
                          )}
                        </div>

                        {msg.sender === 'User' && (
                          <div className="w-8 h-8 rounded-full bg-sky-900 text-sky-200 flex items-center justify-center flex-shrink-0 text-xs font-bold">
                            <User className="w-4 h-4" />
                          </div>
                        )}
                      </div>
                    ))}
                    <div ref={chatBottomRef} />
                  </div>

                  {/* CHAT INPUT FORM */}
                  <form onSubmit={handleSendChat} className="p-3 bg-slate-900 border-t border-slate-800 flex gap-2">
                    <input
                      type="text"
                      value={chatInput}
                      onChange={(e) => setChatInput(e.target.value)}
                      placeholder={`Chất vấn hoặc yêu cầu ${personaNameInput} thi vấn đáp...`}
                      className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-4 py-2 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-purple-500"
                    />
                    <button
                      type="submit"
                      disabled={sendingChat || !chatInput.trim()}
                      className="px-4 py-2 rounded-xl bg-purple-600 hover:bg-purple-500 text-white font-medium text-xs flex items-center gap-1.5 transition-all disabled:opacity-50"
                    >
                      {sendingChat ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Send className="w-3.5 h-3.5" />}
                      Gửi
                    </button>
                  </form>
                </div>
              )}

              {/* TAB 5: NHÀ MÁY ĐỀ THI TÌNH HUỐNG & XUẤT BẢN */}
              {activeTab === 'exam' && (
                <div className="flex-1 overflow-y-auto p-6 space-y-6">
                  <div className="pb-4 border-b border-slate-800">
                    <h3 className="text-lg font-bold text-white flex items-center gap-2">
                      <Sparkles className="w-5 h-5 text-emerald-400" />
                      Nhà Máy Khảo Thí Tình Huống Liên Nguồn (Cross-Exam Studio)
                    </h3>
                    <p className="text-xs text-slate-400">
                      Tự do điều phối tỷ lệ câu hỏi đơn nguồn và câu hỏi tình huống phức hợp đa nguồn (Bloom 5-6).
                    </p>
                  </div>

                  {/* THANH TRƯỢT TỶ LỆ KHẢO THÍ */}
                  <div className="p-5 rounded-2xl bg-slate-950 border border-slate-800 space-y-4">
                    <div className="flex items-center justify-between">
                      <span className="text-xs font-bold text-slate-300 flex items-center gap-1.5">
                        <Sliders className="w-4 h-4 text-emerald-400" />
                        Cân Bằng Tỷ Lệ Đề Thi:
                      </span>
                      <div className="flex items-center gap-3 text-xs font-bold">
                        <span className="text-sky-400">{singlePercent}% Đơn Nguồn</span>
                        <span className="text-slate-600">•</span>
                        <span className="text-emerald-400">{crossPercent}% Tình Huống Liên Nguồn (Bloom 5-6)</span>
                      </div>
                    </div>

                    <input
                      type="range"
                      min={0}
                      max={100}
                      value={crossPercent}
                      onChange={(e) => {
                        const cross = Number(e.target.value);
                        setCrossPercent(cross);
                        setSinglePercent(100 - cross);
                      }}
                      className="w-full h-2 bg-slate-800 rounded-lg appearance-none cursor-pointer accent-emerald-500"
                    />

                    <div className="grid grid-cols-2 gap-4 pt-2">
                      <div>
                        <label className="text-xs text-slate-400 block mb-1">Tổng Số Câu Hỏi:</label>
                        <input
                          type="number"
                          min={1}
                          max={50}
                          value={totalQuestionsCount}
                          onChange={(e) => setTotalQuestionsCount(Number(e.target.value))}
                          className="w-full bg-slate-900 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white focus:outline-none"
                        />
                      </div>
                      <div>
                        <label className="text-xs text-slate-400 block mb-1">Thang Đo Bloom Mục Tiêu:</label>
                        <select
                          value={targetBloomLevel}
                          onChange={(e) => setTargetBloomLevel(Number(e.target.value))}
                          className="w-full bg-slate-900 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white focus:outline-none"
                        >
                          <option value={3}>Cấp 3: Vận dụng cơ bản</option>
                          <option value={4}>Cấp 4: Phân tích đa chiều</option>
                          <option value={5}>Cấp 5: Đánh giá & Phân giải mâu thuẫn</option>
                          <option value={6}>Cấp 6: Sáng tạo & Quyết sách thực chiến</option>
                        </select>
                      </div>
                    </div>

                    <button
                      onClick={handleGenerateCrossQuiz}
                      disabled={generatingQuiz}
                      className="w-full py-2.5 rounded-xl bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white font-bold text-xs flex items-center justify-center gap-2 shadow-lg shadow-emerald-950/50 transition-all disabled:opacity-50"
                    >
                      {generatingQuiz ? <Loader2 className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4" />}
                      Kích Hoạt Sinh Đề Khảo Thí Đa Chiều
                    </button>
                  </div>

                  {/* KẾT QUẢ CÂU HỎI VÀ XUẤT BẢN */}
                  {generatedQuestions.length > 0 && (
                    <div className="space-y-4 pt-4 border-t border-slate-800">
                      <div className="flex items-center justify-between">
                        <h4 className="text-sm font-bold text-white flex items-center gap-2">
                          <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                          Đã Sinh {generatedQuestions.length} Câu Hỏi Khảo Thí
                        </h4>

                        <div className="flex items-center gap-2">
                          <select
                            value={selectedTopicId}
                            onChange={(e) => setSelectedTopicId(e.target.value)}
                            className="bg-slate-950 border border-slate-800 rounded-lg px-3 py-1 text-xs text-slate-200 focus:outline-none"
                          >
                            {topics.map(t => (
                              <option key={t.id} value={t.id}>{t.name}</option>
                            ))}
                          </select>

                          <button
                            onClick={handlePublishToBank}
                            disabled={publishing}
                            className="px-3 py-1 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-semibold flex items-center gap-1.5 transition-all disabled:opacity-50"
                          >
                            {publishing ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <ArrowRight className="w-3.5 h-3.5" />}
                            Xuất Bản Vào Ngân Hàng
                          </button>
                        </div>
                      </div>

                      {publishMessage && (
                        <div className="p-3 rounded-lg bg-emerald-950/60 border border-emerald-800 text-xs text-emerald-400 flex items-center gap-2">
                          <CheckCircle2 className="w-4 h-4 flex-shrink-0" />
                          {publishMessage}
                        </div>
                      )}

                      <div className="space-y-3">
                        {generatedQuestions.map((q, idx) => (
                          <div key={idx} className="p-4 rounded-xl bg-slate-950 border border-slate-800 text-xs space-y-2">
                            <div className="flex items-center justify-between text-slate-400">
                              <div className="flex items-center gap-2">
                                <span className="font-bold text-sky-400">Câu hỏi #{idx + 1}</span>
                                <span className="px-2 py-0.5 rounded bg-slate-900 border border-slate-800 text-slate-400">
                                  Bloom {q.difficulty || 4}
                                </span>
                              </div>

                              <button
                                onClick={() => handleAuditQuestion(q, idx)}
                                disabled={auditingQuestionIdx === idx}
                                className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-indigo-950/60 hover:bg-indigo-900/80 text-indigo-300 border border-indigo-700/50 text-[11px] font-bold transition-all shadow-sm disabled:opacity-50 cursor-pointer"
                              >
                                {auditingQuestionIdx === idx ? (
                                  <Loader2 className="w-3.5 h-3.5 animate-spin text-indigo-400" />
                                ) : (
                                  <Brain className="w-3.5 h-3.5 text-indigo-400" />
                                )}
                                <span>🐺 Thẩm Định Bầy Sói (SCI)</span>
                              </button>
                            </div>
                            <p className="text-white font-medium">{q.content}</p>
                            <div className="grid grid-cols-2 gap-2 pt-1">
                              {q.options?.map((opt: string, optIdx: number) => (
                                <div
                                  key={optIdx}
                                  className={`p-2 rounded border text-xs ${
                                    optIdx === 0
                                      ? 'bg-emerald-950/40 border-emerald-800/80 text-emerald-300 font-semibold'
                                      : 'bg-slate-900 border-slate-800 text-slate-300'
                                  }`}
                                >
                                  {String.fromCharCode(65 + optIdx)}. {opt}
                                </div>
                              ))}
                            </div>
                            {q.aiExplanation && (
                              <p className="text-xs text-slate-400 italic pt-1 border-t border-slate-900">
                                <span className="font-semibold text-slate-300">Căn cứ:</span> {q.aiExplanation}
                              </p>
                            )}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {/* ── MODAL NẠP WEB URL ────────────────────────────────────────── */}
      {showUrlModal && (
        <div className="fixed inset-0 z-50 bg-black/75 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl w-full max-w-md p-6 space-y-4 shadow-2xl">
            <div className="flex items-center justify-between">
              <h3 className="text-base font-bold text-white flex items-center gap-2">
                <Link2 className="w-5 h-5 text-sky-400" />
                Nạp Tài Liệu Trực Tuyến (Web URL)
              </h3>
              <button onClick={() => setShowUrlModal(false)} className="text-slate-400 hover:text-white">
                <X className="w-5 h-5" />
              </button>
            </div>

            <div>
              <label className="text-xs text-slate-400 block mb-1">Đường Dẫn URL:</label>
              <input
                type="url"
                value={urlInput}
                onChange={(e) => setUrlInput(e.target.value)}
                placeholder="https://thuvienphapluat.vn/van-ban/..."
                className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-xs text-white focus:outline-none focus:border-sky-500"
              />
            </div>

            <div>
              <label className="text-xs text-slate-400 block mb-1">Tiêu Đề Tài Liệu (Tùy chọn):</label>
              <input
                type="text"
                value={urlTitle}
                onChange={(e) => setUrlTitle(e.target.value)}
                placeholder="Thông tư 03/2026/TT-NHNN..."
                className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-xs text-white focus:outline-none focus:border-sky-500"
              />
            </div>

            <div className="flex justify-end gap-2 pt-2">
              <button
                onClick={() => setShowUrlModal(false)}
                className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-medium"
              >
                Hủy
              </button>
              <button
                onClick={handleAddSourceUrl}
                disabled={uploadingSource || !urlInput.trim()}
                className="px-4 py-1.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold disabled:opacity-50"
              >
                {uploadingSource ? 'Đang nạp...' : 'Xác Nhận Nạp'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── MODAL THẨM ĐỊNH BẦY SÓI (SWARM AUDIT) ────────────────── */}
      {showSwarmModal && swarmAuditResult && (
        <div className="fixed inset-0 z-50 bg-black/85 backdrop-blur-md flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-indigo-500/40 rounded-3xl w-full max-w-4xl max-h-[90vh] overflow-y-auto p-6 space-y-6 shadow-2xl">
            {/* Header */}
            <div className="flex items-start justify-between pb-4 border-b border-slate-800">
              <div className="flex items-center gap-3">
                <div className="p-3 rounded-2xl bg-gradient-to-tr from-indigo-500 via-purple-500 to-pink-500 text-white shadow-lg shadow-indigo-500/30">
                  <Brain className="w-6 h-6 animate-pulse" />
                </div>
                <div>
                  <div className="flex items-center gap-2">
                    <h3 className="text-lg font-bold text-white">
                      Hội Chẩn Bầy Sói Đa Mô Hình (Wolf Pack Swarm Audit)
                    </h3>
                    <span className={`px-2.5 py-0.5 rounded-full text-[11px] font-bold border ${
                      swarmAuditResult.consensusLevel === 'UNANIMOUS'
                        ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40'
                        : swarmAuditResult.consensusLevel === 'HIGH_CONSENSUS'
                        ? 'bg-sky-500/20 text-sky-300 border-sky-500/40'
                        : 'bg-amber-500/20 text-amber-300 border-amber-500/40'
                    }`}>
                      {swarmAuditResult.consensusLevel}
                    </span>
                  </div>
                  <p className="text-xs text-slate-400">
                    Phản biện song song phi tập trung • Đo lường chỉ số đồng thuận SCI • Phân giải Sói Đầu Đàn
                  </p>
                </div>
              </div>

              <button
                onClick={() => setShowSwarmModal(false)}
                className="text-slate-400 hover:text-white p-1 rounded-lg hover:bg-slate-800 transition-colors"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Score & Alpha Metrics Bar */}
            <div className="grid grid-cols-1 sm:grid-cols-4 gap-3">
              <div className="p-4 rounded-2xl bg-slate-950/80 border border-slate-800">
                <div className="text-[11px] text-slate-400 font-medium">Chỉ Số Đồng Thuận (SCI)</div>
                <div className="text-2xl font-black text-transparent bg-clip-text bg-gradient-to-r from-emerald-400 to-teal-300 font-mono">
                  {(swarmAuditResult.consensusIndex * 100).toFixed(0)}%
                </div>
              </div>

              <div className="p-4 rounded-2xl bg-slate-950/80 border border-slate-800">
                <div className="text-[11px] text-slate-400 font-medium">Sói Đầu Đàn (Alpha Leader)</div>
                <div className="text-sm font-bold text-indigo-300 truncate">
                  {swarmAuditResult.alphaModelDisplayName}
                </div>
              </div>

              <div className="p-4 rounded-2xl bg-slate-950/80 border border-slate-800">
                <div className="text-[11px] text-slate-400 font-medium">Đáp Án Phê Chuẩn</div>
                <div className="text-2xl font-black text-amber-400 font-mono">
                  {swarmAuditResult.winningAnswer}
                </div>
              </div>

              <div className="p-4 rounded-2xl bg-slate-950/80 border border-slate-800">
                <div className="text-[11px] text-slate-400 font-medium">Thời Gian & Chi Phí</div>
                <div className="text-xs font-mono text-slate-300 pt-1">
                  <span className="text-emerald-400 font-bold">{swarmAuditResult.totalLatencyMs}ms</span> • ${swarmAuditResult.totalCostUsd}
                </div>
              </div>
            </div>

            {/* Synthesis Summary */}
            <div className="p-4 rounded-2xl bg-indigo-950/30 border border-indigo-500/30 text-xs text-indigo-200 leading-relaxed">
              <span className="font-bold text-white">Kết luận hội nghị thẩm định: </span>
              {swarmAuditResult.synthesisSummary}
            </div>

            {/* Member Verdicts Breakdown */}
            <div className="space-y-3">
              <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400 flex items-center gap-2">
                <Layers className="w-4 h-4 text-sky-400" />
                Chi Tiết Phán Quyết Từng Mô Hình ({swarmAuditResult.memberVerdicts.length})
              </h4>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {swarmAuditResult.memberVerdicts.map((v, idx) => (
                  <div 
                    key={idx}
                    className="p-4 rounded-2xl bg-slate-950/80 border border-slate-800 text-xs space-y-2"
                  >
                    <div className="flex items-center justify-between pb-2 border-b border-slate-800/80">
                      <div>
                        <div className="font-bold text-white text-xs">{v.displayName}</div>
                        <span className="text-[10px] text-slate-500 font-mono">{v.modelCode}</span>
                      </div>

                      <div className="flex items-center gap-2">
                        <span className="text-[10px] px-2 py-0.5 rounded bg-slate-900 border border-slate-800 font-mono text-slate-300">
                          {v.keySource}
                        </span>
                        <span className="font-mono font-bold text-emerald-400 text-xs">{v.latencyMs}ms</span>
                      </div>
                    </div>

                    <div className="flex items-center justify-between pt-1">
                      <span className="text-slate-400">Đáp án chọn:</span>
                      <span className="font-mono font-bold text-amber-400 text-sm px-2 py-0.5 rounded bg-amber-500/10 border border-amber-500/20">
                        {v.normalizedAnswer}
                      </span>
                    </div>

                    <div className="text-slate-300 leading-relaxed">
                      <span className="text-slate-500">Căn cứ sư phạm: </span>
                      {v.pedagogicalRationale}
                    </div>

                    {v.identifiedFlaws && v.identifiedFlaws.length > 0 && (
                      <div className="p-2 rounded-lg bg-rose-950/30 border border-rose-900/40 text-rose-300 text-[11px]">
                        <span className="font-bold">Cảnh báo bẫy: </span>
                        {v.identifiedFlaws.join('; ')}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>

            <div className="flex justify-end pt-2">
              <button
                onClick={() => setShowSwarmModal(false)}
                className="px-5 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-white text-xs font-bold transition-colors cursor-pointer"
              >
                Đóng Hội Chẩn
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
