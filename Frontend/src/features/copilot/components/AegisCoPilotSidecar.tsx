import React, { useState, useEffect, useRef } from 'react';
import {
  Brain,
  ChevronRight,
  Maximize2,
  Minimize2,
  RefreshCw,
  Send,
  Sparkles,
  Terminal,
  X,
  Zap,
  GraduationCap,
  ShieldAlert,
  BookOpen,
  ArrowUpRight
} from 'lucide-react';
import type {
  CoPilotPersona,
  CoPilotMessage,
  CoPilotSlashCommand,
  CoPilotModelGroup,
  CoPilotArtifact
} from '../types/copilot.types';
import { CoPilotService } from '../services/copilot.service';
import { CognitiveTraceViewer } from './CognitiveTraceViewer';
import { ToolActionPill } from './ToolActionPill';
import { ArtifactCardRenderer } from './ArtifactCardRenderer';
import { ModelSelectorDropdown } from './ModelSelectorDropdown';

interface AegisCoPilotSidecarProps {
  isOpen: boolean;
  onClose: () => void;
  currentScreen?: string;
  onApplyArtifact?: (artifact: CoPilotArtifact) => void;
}

export const AegisCoPilotSidecar: React.FC<AegisCoPilotSidecarProps> = ({
  isOpen,
  onClose,
  currentScreen = 'Dashboard',
  onApplyArtifact
}) => {
  const [persona, setPersona] = useState<CoPilotPersona>('AssessmentArchitect');
  const [selectedModel, setSelectedModel] = useState<string>('auto');
  const [modelGroups, setModelGroups] = useState<CoPilotModelGroup[]>([]);
  const [commands, setCommands] = useState<CoPilotSlashCommand[]>([]);
  const [inputMessage, setInputMessage] = useState<string>('');
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isExpanded, setIsExpanded] = useState<boolean>(false);
  const [showCommandsDropdown, setShowCommandsDropdown] = useState<boolean>(false);

  const [messages, setMessages] = useState<CoPilotMessage[]>([
    {
      id: 'welcome',
      role: 'assistant',
      content:
        'Xin chào! Tôi là **Aegis Sovereign Co-Pilot** — trợ lý nhận thức khảo thí & tri thức cá nhân hóa.\n\nBạn có thể ra lệnh siêu tốc bằng các Slash Command:\n* `/soan [chủ đề]`: Thiết kế câu hỏi chuẩn Bloom & IRT 3-PL kèm công thức KaTeX.\n* `/hoithao [chủ đề]`: Triệu tập 9 Viện Hàn Lâm tranh biện đa chiều.\n* `/ma-tran`: Lập ma trận đặc tả đề thi 6 cấp độ nhận thức.\n* `/gia-su`: Hướng dẫn tư duy Socratic không mớm lời giải.',
      suggestedActions: [
        '/soan Toán tài chính lãi suất kép',
        '/hoithao Đạo đức khảo thí AI',
        '/ma-tran Đề thi Kinh tế học 40 câu'
      ],
      modelUsed: 'UniversalAiRouter:Auto',
      keySourceUsed: 'SystemDefault',
      timestamp: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
    }
  ]);

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Load grouped models and slash commands on mount
  useEffect(() => {
    CoPilotService.getGroupedModels().then(data => {
      if (data && data.length > 0) setModelGroups(data);
    });
    CoPilotService.getCommands(persona).then(setCommands);
  }, []);

  // Update grouped models with dynamic recommendation when prompt changes
  useEffect(() => {
    const timer = setTimeout(() => {
      if (inputMessage.trim().length > 2) {
        CoPilotService.getGroupedModels(inputMessage).then(data => {
          if (data && data.length > 0) setModelGroups(data);
        });
      }
    }, 400);
    return () => clearTimeout(timer);
  }, [inputMessage]);

  // Update commands when persona changes
  useEffect(() => {
    CoPilotService.getCommands(persona).then(setCommands);
  }, [persona]);

  // Scroll to bottom on new message
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isLoading]);

  // Handle slash command suggestions
  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setInputMessage(val);
    if (val.startsWith('/') && !val.includes(' ')) {
      setShowCommandsDropdown(true);
    } else {
      setShowCommandsDropdown(false);
    }
  };

  const handleSelectCommand = (cmd: CoPilotSlashCommand) => {
    setInputMessage(cmd.command + ' ');
    setShowCommandsDropdown(false);
    inputRef.current?.focus();
  };

  const handleSend = async (overridePrompt?: string) => {
    const textToSend = overridePrompt ?? inputMessage;
    if (!textToSend.trim() || isLoading) return;

    const userMsg: CoPilotMessage = {
      id: Date.now().toString(),
      role: 'user',
      content: textToSend,
      timestamp: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
    };

    setMessages(prev => [...prev, userMsg]);
    setInputMessage('');
    setShowCommandsDropdown(false);
    setIsLoading(true);

    try {
      const response = await CoPilotService.sendMessage({
        message: textToSend,
        persona: persona,
        modelCode: selectedModel === 'auto' ? undefined : selectedModel,
        context: {
          screenName: currentScreen
        },
        history: messages.slice(-4).map(m => ({ role: m.role, content: m.content }))
      });

      const assistantMsg: CoPilotMessage = {
        id: response.sessionId,
        role: 'assistant',
        content: response.content,
        thoughtTrace: response.thoughtTrace,
        toolCalls: response.toolCalls,
        artifact: response.artifact,
        suggestedActions: response.suggestedActions,
        modelUsed: response.modelUsed,
        keySourceUsed: response.keySourceUsed,
        latencyMs: response.latencyMs,
        timestamp: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
      };

      setMessages(prev => [...prev, assistantMsg]);
    } catch (err: any) {
      const serverError = err?.response?.data?.message || err?.response?.data?.title || (typeof err?.response?.data === 'string' ? err?.response?.data : null) || err?.message;
      const errorMsg: CoPilotMessage = {
        id: Date.now().toString(),
        role: 'assistant',
        content: `⚠️ **Không thể kết nối tới mô hình AI đã chọn.**\n\n* **Chi tiết sự cố**: \`${serverError || 'Lỗi mạng hoặc không phản hồi từ máy chủ'}\`\n* **Giải pháp khuyến nghị**: Hãy chuyển bộ chọn mô hình sang **'🌟 Miễn phí / Cục bộ'** (như Gemini Flash hoặc DeepSeek-R1) hoặc chọn **'🎯 Tự Động Tối Ưu'** để hệ thống tự động định tuyến qua Bầy Sói WPSE!`,
        modelUsed: 'WPSE:ResilientFallback',
        suggestedActions: [
          '/soan Toán tài chính lãi suất kép',
          '/kiem-dinh Câu hỏi trên màn hình',
          '/hoithao Đạo đức khảo thí AI'
        ],
        timestamp: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
      };
      setMessages(prev => [...prev, errorMsg]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  if (!isOpen) return null;

  return (
    <aside
      className={`fixed top-0 right-0 h-screen z-50 flex flex-col bg-slate-950/95 text-slate-100 border-l border-cyan-800/50 shadow-2xl backdrop-blur-xl transition-all duration-300 ease-in-out ${
        isExpanded ? 'w-full md:w-[650px]' : 'w-full md:w-[420px]'
      }`}
      aria-label="Aegis Sovereign Co-Pilot Sidecar"
    >
      {/* ── Header ── */}
      <div className="flex flex-col border-b border-slate-800 bg-slate-900/80 px-4 py-3 shrink-0">
        <div className="flex items-center justify-between mb-2.5">
          <div className="flex items-center gap-2">
            <div className="relative">
              <Brain className="w-5 h-5 text-cyan-400 animate-pulse" />
              <span className="absolute -bottom-0.5 -right-0.5 w-2 h-2 rounded-full bg-emerald-400 ring-2 ring-slate-900" />
            </div>
            <div>
              <div className="flex items-center gap-1.5">
                <h3 className="font-bold text-sm tracking-wide bg-gradient-to-r from-cyan-300 via-indigo-300 to-purple-300 bg-clip-text text-transparent">
                  Aegis Co-Pilot Sovereign
                </h3>
                <span className="px-1.5 py-0.2 rounded text-[9px] font-mono bg-cyan-950 text-cyan-300 border border-cyan-800/50">
                  Sidecar
                </span>
              </div>
              <p className="text-[10px] text-slate-400 flex items-center gap-1">
                <span>Ngữ cảnh:</span>
                <span className="text-cyan-300 font-medium">{currentScreen}</span>
              </p>
            </div>
          </div>

          <div className="flex items-center gap-1">
            <button
              type="button"
              onClick={() => setIsExpanded(!isExpanded)}
              title={isExpanded ? 'Thu nhỏ cửa sổ' : 'Mở rộng toàn màn hình'}
              className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
            >
              {isExpanded ? <Minimize2 className="w-4 h-4" /> : <Maximize2 className="w-4 h-4" />}
            </button>
            <button
              type="button"
              onClick={() => setMessages([messages[0]])}
              title="Xóa phiên làm việc"
              className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
            >
              <RefreshCw className="w-4 h-4" />
            </button>
            <button
              type="button"
              onClick={onClose}
              title="Đóng Sidecar"
              className="p-1.5 rounded-lg text-slate-400 hover:text-rose-400 hover:bg-rose-950/30 transition-colors"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>

        {/* Persona Switcher Chips */}
        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 scrollbar-none text-[11px]">
          <button
            type="button"
            onClick={() => setPersona('AssessmentArchitect')}
            className={`px-2.5 py-1 rounded-full font-medium transition-all shrink-0 flex items-center gap-1 ${
              persona === 'AssessmentArchitect'
                ? 'bg-cyan-600 text-white shadow-sm shadow-cyan-500/20'
                : 'bg-slate-800/70 text-slate-400 hover:text-slate-200'
            }`}
          >
            <Sparkles className="w-3 h-3" />
            Khảo Thí
          </button>
          <button
            type="button"
            onClick={() => setPersona('SocraticMentor')}
            className={`px-2.5 py-1 rounded-full font-medium transition-all shrink-0 flex items-center gap-1 ${
              persona === 'SocraticMentor'
                ? 'bg-emerald-600 text-white shadow-sm shadow-emerald-500/20'
                : 'bg-slate-800/70 text-slate-400 hover:text-slate-200'
            }`}
          >
            <GraduationCap className="w-3 h-3" />
            Gia Sư
          </button>
          <button
            type="button"
            onClick={() => setPersona('GovernanceFinOps')}
            className={`px-2.5 py-1 rounded-full font-medium transition-all shrink-0 flex items-center gap-1 ${
              persona === 'GovernanceFinOps'
                ? 'bg-amber-600 text-white shadow-sm shadow-amber-500/20'
                : 'bg-slate-800/70 text-slate-400 hover:text-slate-200'
            }`}
          >
            <ShieldAlert className="w-3 h-3" />
            Quản Trị
          </button>
          <button
            type="button"
            onClick={() => setPersona('ResearchScholar')}
            className={`px-2.5 py-1 rounded-full font-medium transition-all shrink-0 flex items-center gap-1 ${
              persona === 'ResearchScholar'
                ? 'bg-purple-600 text-white shadow-sm shadow-purple-500/20'
                : 'bg-slate-800/70 text-slate-400 hover:text-slate-200'
            }`}
          >
            <BookOpen className="w-3 h-3" />
            Hội Thảo
          </button>
        </div>
      </div>

      {/* ── Messages Stream ── */}
      <div className="flex-1 overflow-y-auto px-4 py-4 space-y-4 scroll-smooth">
        {messages.map((msg) => (
          <div
            key={msg.id}
            className={`flex flex-col ${msg.role === 'user' ? 'items-end' : 'items-start'}`}
          >
            {/* Header info */}
            <div className="flex items-center gap-1.5 text-[10px] text-slate-500 mb-1 px-1">
              <span>{msg.role === 'user' ? 'Bạn' : 'Aegis Co-Pilot'}</span>
              <span>•</span>
              <span>{msg.timestamp}</span>
              {msg.modelUsed && (
                <>
                  <span>•</span>
                  <span className="font-mono text-cyan-400/80">{msg.modelUsed}</span>
                </>
              )}
              {msg.keySourceUsed && (
                <>
                  <span>•</span>
                  <span className={`px-1.5 py-0.2 rounded text-[9px] font-mono border ${
                    msg.keySourceUsed === 'UserBYOK'
                      ? 'bg-blue-950 text-blue-300 border-blue-700/60'
                      : msg.keySourceUsed === 'TenantBYOK'
                      ? 'bg-amber-950 text-amber-300 border-amber-700/60'
                      : 'bg-slate-800 text-slate-400 border-slate-700'
                  }`}>
                    {msg.keySourceUsed === 'UserBYOK' ? '👤 User BYOK' : msg.keySourceUsed === 'TenantBYOK' ? '🏢 Tenant BYOK' : '🌐 System'}
                  </span>
                </>
              )}
            </div>

            {/* Bubble */}
            <div
              className={`rounded-2xl px-4 py-3 text-xs leading-relaxed max-w-[92%] shadow-md ${
                msg.role === 'user'
                  ? 'bg-gradient-to-r from-cyan-600 to-blue-600 text-white rounded-tr-sm'
                  : 'bg-slate-900 border border-slate-800 text-slate-200 rounded-tl-sm'
              }`}
            >
              {/* Cognitive Trace (Thinking...) */}
              {msg.thoughtTrace && <CognitiveTraceViewer thoughtTrace={msg.thoughtTrace} />}

              {/* Action Pills */}
              {msg.toolCalls && msg.toolCalls.length > 0 && (
                <div className="flex flex-wrap gap-1.5 my-2">
                  {msg.toolCalls.map((t, idx) => (
                    <ToolActionPill key={idx} tool={t} />
                  ))}
                </div>
              )}

              {/* Content body */}
              <div className="whitespace-pre-wrap leading-relaxed select-text font-normal">
                {msg.content}
              </div>

              {/* Interactive Artifact Card */}
              {msg.artifact && (
                <ArtifactCardRenderer
                  artifact={msg.artifact}
                  onApply={onApplyArtifact}
                />
              )}
            </div>

            {/* Suggested actions chips */}
            {msg.suggestedActions && msg.suggestedActions.length > 0 && (
              <div className="flex flex-wrap gap-1.5 mt-2.5 max-w-[92%]">
                {msg.suggestedActions.map((action, i) => (
                  <button
                    key={i}
                    type="button"
                    onClick={() => handleSend(action)}
                    className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[10px] bg-slate-900 hover:bg-slate-800 text-cyan-300 border border-cyan-900/60 hover:border-cyan-500/50 transition-colors shadow-sm"
                  >
                    <span>{action}</span>
                    <ArrowUpRight className="w-2.5 h-2.5 opacity-70" />
                  </button>
                ))}
              </div>
            )}
          </div>
        ))}

        {isLoading && (
          <div className="flex items-center gap-2 p-3 rounded-xl bg-slate-900/80 border border-cyan-900/40 text-xs text-cyan-300">
            <Brain className="w-4 h-4 animate-spin text-cyan-400" />
            <span className="animate-pulse">Đang kích hoạt vết tư duy & điều phối nhận thức...</span>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* ── Slash Commands Autocomplete Popup ── */}
      {showCommandsDropdown && (
        <div className="mx-4 mb-2 p-2 rounded-xl bg-slate-900/95 border border-cyan-800/80 shadow-2xl backdrop-blur-xl">
          <div className="text-[10px] font-mono uppercase text-slate-400 px-2 py-1 flex items-center justify-between">
            <span>Slash Commands Khả Dụng</span>
            <Terminal className="w-3 h-3 text-cyan-400" />
          </div>
          <div className="space-y-1">
            {commands.map((cmd) => (
              <button
                key={cmd.command}
                type="button"
                onClick={() => handleSelectCommand(cmd)}
                className="w-full text-left p-2 rounded-lg hover:bg-slate-800/80 flex items-center justify-between text-xs transition-colors"
              >
                <div>
                  <div className="font-mono font-bold text-cyan-300">{cmd.command}</div>
                  <div className="text-[10px] text-slate-400">{cmd.description}</div>
                </div>
                <ChevronRight className="w-3.5 h-3.5 text-slate-500" />
              </button>
            ))}
          </div>
        </div>
      )}

      {/* ── Input Bar ── */}
      <div className="p-3 border-t border-slate-800 bg-slate-900/90 shrink-0">
        <div className="flex items-center justify-between text-[10px] text-slate-400 mb-1.5 px-1">
          <ModelSelectorDropdown
            groups={modelGroups}
            selectedModelCode={selectedModel}
            onSelectModel={setSelectedModel}
            currentPrompt={inputMessage}
          />
          <span className="font-mono text-[9px] text-slate-500">Phím tắt: Ctrl + J</span>
        </div>

        <div className="relative flex items-center">
          <input
            ref={inputRef}
            type="text"
            value={inputMessage}
            onChange={handleInputChange}
            onKeyDown={handleKeyDown}
            disabled={isLoading}
            placeholder="Hỏi Co-Pilot hoặc gõ '/' để xem lệnh soạn đề..."
            className="w-full pl-3 pr-10 py-2.5 rounded-xl bg-slate-950 border border-slate-800 text-xs text-slate-100 placeholder-slate-500 focus:outline-none focus:border-cyan-500 focus:ring-1 focus:ring-cyan-500 transition-all"
          />
          <button
            type="button"
            onClick={() => handleSend()}
            disabled={isLoading || !inputMessage.trim()}
            className="absolute right-1.5 p-2 rounded-lg bg-cyan-600 hover:bg-cyan-500 disabled:opacity-40 disabled:hover:bg-cyan-600 text-white transition-all shadow-sm"
          >
            <Send className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
    </aside>
  );
};
