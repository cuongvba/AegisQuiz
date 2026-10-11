import React, { useState, useRef, useEffect } from 'react';
import {
  Brain,
  ChevronDown,
  Cpu,
  Sparkles,
  Zap,
  Bot,
  ShieldCheck,
  Check,
  Star,
  User,
  Building2,
  Globe
} from 'lucide-react';
import type { CoPilotModelGroup, CoPilotModelItem } from '../types/copilot.types';

interface ModelSelectorDropdownProps {
  groups: CoPilotModelGroup[];
  selectedModelCode: string; // 'auto' or specific modelCode
  onSelectModel: (modelCode: string) => void;
  currentPrompt?: string;
}

export const ModelSelectorDropdown: React.FC<ModelSelectorDropdownProps> = ({
  groups,
  selectedModelCode,
  onSelectModel,
  currentPrompt = ''
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  // Close when clicking outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Find currently selected model item
  const allModels: CoPilotModelItem[] = groups.flatMap(g => g.models || []);
  const currentModel = allModels.find(m => m.modelCode === selectedModelCode);
  const recommendedModel = allModels.find(m => m.isRecommended);

  // Render provider icon helper
  const renderProviderIcon = (code: string) => {
    switch (code?.toLowerCase()) {
      case 'anthropic':
        return <Sparkles className="w-3.5 h-3.5 text-indigo-400" />;
      case 'google':
        return <Cpu className="w-3.5 h-3.5 text-cyan-400" />;
      case 'openai':
        return <Bot className="w-3.5 h-3.5 text-emerald-400" />;
      case 'deepseek':
        return <Zap className="w-3.5 h-3.5 text-rose-400" />;
      case 'local':
        return <ShieldCheck className="w-3.5 h-3.5 text-amber-400" />;
      default:
        return <Brain className="w-3.5 h-3.5 text-slate-400" />;
    }
  };

  // Render key source badge helper
  const renderKeySourceBadge = (source: string) => {
    if (source === 'UserBYOK') {
      return (
        <span className="inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded text-[9px] font-mono bg-blue-950 text-blue-300 border border-blue-700/60" title="Đang sử dụng khóa cá nhân của bạn (0$ phụ phí)">
          <User className="w-2.5 h-2.5" />
          <span>User BYOK</span>
        </span>
      );
    }
    if (source === 'TenantBYOK') {
      return (
        <span className="inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded text-[9px] font-mono bg-amber-950 text-amber-300 border border-amber-700/60" title="Đang sử dụng khóa cấu hình của Trường/Đơn vị">
          <Building2 className="w-2.5 h-2.5" />
          <span>Tenant BYOK</span>
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded text-[9px] font-mono bg-slate-800 text-slate-400 border border-slate-700" title="Nguồn mặc định hệ thống AegisQuiz">
        <Globe className="w-2.5 h-2.5" />
        <span>System</span>
      </span>
    );
  };

  // Render model availability status badge
  const renderStatusBadge = (model: CoPilotModelItem) => {
    if (model.isFreeOrLocal || model.availabilityStatus === 'FreeCommunity') {
      return (
        <span className="inline-flex items-center gap-0.5 px-1.5 py-0.2 rounded text-[9px] font-mono bg-emerald-950 text-emerald-300 border border-emerald-700/60" title="Mô hình Miễn Phí / Cộng Đồng khả dụng 100%">
          <span>🌟 Miễn phí</span>
        </span>
      );
    }
    if (model.availabilityStatus === 'LocalNode') {
      return (
        <span className="inline-flex items-center gap-0.5 px-1.5 py-0.2 rounded text-[9px] font-mono bg-amber-950 text-amber-300 border border-amber-700/60" title="Nút máy chủ AI nội bộ On-Premise">
          <span>🛡️ Cục bộ</span>
        </span>
      );
    }
    if (model.isAvailable === false || model.availabilityStatus === 'RequiresKey') {
      return (
        <span className="inline-flex items-center gap-0.5 px-1.5 py-0.2 rounded text-[9px] font-mono bg-rose-950 text-rose-300 border border-rose-700/60" title="Chưa có khóa API hệ thống, cần nạp khóa BYOK">
          <span>🔴 Cần nhập khóa</span>
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-0.5 px-1.5 py-0.2 rounded text-[9px] font-mono bg-cyan-950 text-cyan-300 border border-cyan-700/60" title="Khả dụng sẵn sàng phục vụ">
        <span>🟢 Sẵn sàng</span>
      </span>
    );
  };

  return (
    <div className="relative inline-block text-left" ref={containerRef}>
      {/* ── Trigger Button ── */}
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-slate-900 hover:bg-slate-800 border border-slate-700/80 hover:border-cyan-500/60 transition-all text-xs font-medium text-slate-200 shadow-sm"
        title="Chọn mô hình AI và nguồn khóa"
      >
        <span className="text-[10px] text-slate-400 font-normal">Mô hình:</span>

        {selectedModelCode === 'auto' ? (
          <div className="flex items-center gap-1 text-cyan-300">
            <Sparkles className="w-3.5 h-3.5 text-cyan-400 animate-pulse" />
            <span className="font-semibold">Tự Động Tối Ưu</span>
            {recommendedModel && (
              <span className="text-[9px] font-mono text-cyan-400/80">({recommendedModel.displayName})</span>
            )}
          </div>
        ) : (
          <div className="flex items-center gap-1.5">
            <span className="text-cyan-300 font-semibold truncate max-w-[130px]">
              {currentModel?.displayName || selectedModelCode}
            </span>
            {currentModel && renderStatusBadge(currentModel)}
            {currentModel && renderKeySourceBadge(currentModel.keySource)}
          </div>
        )}

        <ChevronDown className={`w-3.5 h-3.5 text-slate-400 transition-transform ${isOpen ? 'rotate-180' : ''}`} />
      </button>

      {/* ── Dropdown Popover ── */}
      {isOpen && (
        <div className="absolute bottom-full left-0 mb-2 w-80 md:w-96 max-h-[460px] overflow-y-auto rounded-2xl bg-slate-950/98 border border-cyan-800/80 shadow-2xl backdrop-blur-2xl p-2.5 z-50 divide-y divide-slate-800/70 scrollbar-thin">
          {/* Header */}
          <div className="pb-2 px-1 flex items-center justify-between text-[11px] text-slate-400">
            <span className="font-bold uppercase tracking-wider text-slate-300 flex items-center gap-1">
              <Brain className="w-3.5 h-3.5 text-cyan-400" />
              Điều Phối Mô Hình AI
            </span>
            <span className="text-[9px] font-mono text-cyan-400">Ưu tiên: User → Tenant → System</span>
          </div>

          {/* Option: Auto Optimal */}
          <div className="py-2">
            <button
              type="button"
              onClick={() => {
                onSelectModel('auto');
                setIsOpen(false);
              }}
              className={`w-full text-left p-2.5 rounded-xl border transition-all flex items-start justify-between ${
                selectedModelCode === 'auto'
                  ? 'bg-gradient-to-r from-cyan-950/80 to-indigo-950/80 border-cyan-500/80 text-white shadow-md'
                  : 'bg-slate-900/60 border-slate-800 hover:border-cyan-600/50 text-slate-200'
              }`}
            >
              <div className="space-y-1">
                <div className="flex items-center gap-1.5">
                  <Sparkles className="w-4 h-4 text-cyan-400" />
                  <span className="font-bold text-xs text-cyan-200">🎯 Tự Động Tối Ưu (Auto-Optimal)</span>
                </div>
                <p className="text-[10px] text-slate-400 leading-tight">
                  Tự động phân tích câu hỏi/chủ đề và chọn mô hình có ELO/Cost cao nhất.
                </p>
                {recommendedModel && (
                  <div className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] bg-cyan-950/90 text-cyan-300 border border-cyan-800">
                    <Star className="w-3 h-3 text-amber-400 fill-amber-400" />
                    <span>Khuyên dùng hiện tại: <strong>{recommendedModel.displayName}</strong></span>
                  </div>
                )}
              </div>
              {selectedModelCode === 'auto' && (
                <Check className="w-4 h-4 text-cyan-400 mt-1 shrink-0" />
              )}
            </button>
          </div>

          {/* Grouped Models by Provider */}
          <div className="pt-2 space-y-3">
            {groups.map((group) => {
              const models: CoPilotModelItem[] = group.models || [];
              if (models.length === 0) return null;

              return (
                <div key={group.providerCode || group.providerName} className="space-y-1.5">
                  {/* Provider Header */}
                  <div className="flex items-center justify-between px-1 text-[10px] font-bold text-slate-400 uppercase tracking-wider">
                    <span className="flex items-center gap-1.5">
                      {renderProviderIcon(group.providerCode)}
                      {group.providerName}
                    </span>
                    <span className="text-[9px] text-slate-500 font-normal lowercase">
                      {models.length} mô hình
                    </span>
                  </div>

                  {/* Models list */}
                  <div className="space-y-1">
                    {models.map((m: CoPilotModelItem) => {
                      const isSelected = selectedModelCode === m.modelCode;

                      return (
                        <button
                          key={m.modelCode}
                          type="button"
                          onClick={() => {
                            onSelectModel(m.modelCode);
                            setIsOpen(false);
                          }}
                          className={`w-full text-left p-2 rounded-xl border transition-all flex items-start justify-between ${
                            isSelected
                              ? 'bg-slate-800/90 border-cyan-500/80 text-white shadow-sm'
                              : m.isRecommended
                              ? 'bg-indigo-950/40 border-indigo-700/60 hover:bg-slate-900 text-slate-200'
                              : 'bg-slate-900/50 border-slate-800/80 hover:bg-slate-800/60 text-slate-300'
                          }`}
                        >
                          <div className="space-y-1 flex-1 pr-2">
                            <div className="flex items-center gap-1.5 flex-wrap">
                              <span className="font-semibold text-xs text-slate-100">
                                {m.displayName}
                              </span>
                              {renderStatusBadge(m)}
                              {renderKeySourceBadge(m.keySource)}
                              {m.isRecommended && (
                                <span className="inline-flex items-center gap-0.5 px-1.5 py-0.2 rounded text-[9px] font-bold bg-amber-950 text-amber-300 border border-amber-600/70 animate-pulse">
                                  <Star className="w-2.5 h-2.5 fill-amber-400 text-amber-400" />
                                  <span>Khuyên dùng</span>
                                </span>
                              )}
                            </div>

                            <p className="text-[10px] text-slate-400 line-clamp-1 leading-normal">
                              {m.description}
                            </p>

                            {m.isRecommended && m.recommendationReason && (
                              <p className="text-[10px] text-amber-300/90 font-medium">
                                💡 {m.recommendationReason}
                              </p>
                            )}
                          </div>

                          {isSelected && (
                            <Check className="w-3.5 h-3.5 text-cyan-400 mt-1 shrink-0" />
                          )}
                        </button>
                      );
                    })}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
};
