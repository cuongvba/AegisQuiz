import React, { useState } from 'react';
import { 
  Brain, Shield, Sparkles, BookOpen, AlertTriangle, 
  Lightbulb, HelpCircle, CheckCircle2, Copy, Check, 
  Layers, Gauge, Zap, Database, X, RefreshCw
} from 'lucide-react';

export interface PedagogicalWalkthroughData {
  questionId?: string;
  questionContent: string;
  correctAnswer: string;
  domainCode: string;
  promptAnatomy: string;
  theoreticalGrounding: string;
  distractorAutopsy: string;
  mnemonicAndRecall: string;
  extrapolatedCase: string;
  bloomLevel: number;
  estimatedDifficultyIrt: number;
  resolvedByProvider?: string;
  resolvedByModel?: string;
  isFromSemanticCache?: boolean;
  executionTimeMs?: number;
}

interface AiCognitiveWalkthroughModalProps {
  isOpen: boolean;
  onClose: () => void;
  walkthrough: PedagogicalWalkthroughData | null;
  isLoading?: boolean;
  onRegenerateWithPersona?: (personaId: number) => void;
}

export const AiCognitiveWalkthroughModal: React.FC<AiCognitiveWalkthroughModalProps> = ({
  isOpen,
  onClose,
  walkthrough,
  isLoading = false,
  onRegenerateWithPersona
}) => {
  const [activeTab, setActiveTab] = useState<'all' | 'step1' | 'step2' | 'step3' | 'step4' | 'step5'>('all');
  const [copied, setCopied] = useState(false);
  const [selectedPersona, setSelectedPersona] = useState<number>(1);

  if (!isOpen) return null;

  const handleCopy = () => {
    if (!walkthrough) return;
    const text = `=== BẢN ĐỒ TƯ DUY SƯ PHẠM 5 BƯỚC (AEGISQUIZ COGNITIVE SCAFFOLD) ===
CÂU HỎI: ${walkthrough.questionContent}
ĐÁP ÁN ĐÚNG: ${walkthrough.correctAnswer} (Lĩnh vực: ${walkthrough.domainCode})

1. BÓC TÁCH DỮ KIỆN & TỪ KHÓA CỐT LÕI:
${walkthrough.promptAnatomy}

2. TỌA ĐỘ PHÁP LÝ & TRI THỨC ÁP DỤNG:
${walkthrough.theoreticalGrounding}

3. GIẢI PHẪU PHƯƠNG ÁN BẪY & LỖI SAI KINH ĐIỂN:
${walkthrough.distractorAutopsy}

4. MẸO NHỚ NHANH & PHẢN XẠ 1 NỐT NHẠC:
${walkthrough.mnemonicAndRecall}

5. BÀI TOÁN TÌNH HUỐNG MỞ RỘNG TƯ DUY:
${walkthrough.extrapolatedCase}

[Độ khó IRT: ${walkthrough.estimatedDifficultyIrt} | Bloom Level: ${walkthrough.bloomLevel} | Provider: ${walkthrough.resolvedByProvider}]`;

    navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const getBloomLabel = (level: number) => {
    switch (level) {
      case 1: return { text: 'Nhận biết (Remember)', color: 'bg-blue-500/20 text-blue-400 border-blue-500/30' };
      case 2: return { text: 'Thông hiểu (Understand)', color: 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30' };
      case 3: return { text: 'Vận dụng (Apply)', color: 'bg-amber-500/20 text-amber-400 border-amber-500/30' };
      case 4: return { text: 'Phân tích (Analyze)', color: 'bg-purple-500/20 text-purple-400 border-purple-500/30' };
      case 5: return { text: 'Đánh giá (Evaluate)', color: 'bg-rose-500/20 text-rose-400 border-rose-500/30' };
      case 6: return { text: 'Sáng tạo (Create)', color: 'bg-yellow-500/20 text-yellow-400 border-yellow-500/30' };
      default: return { text: 'Vận dụng', color: 'bg-amber-500/20 text-amber-400 border-amber-500/30' };
    }
  };

  const bloom = getBloomLabel(walkthrough?.bloomLevel || 3);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-md p-4 animate-in fade-in duration-200">
      <div className="relative w-full max-w-4xl max-h-[92vh] flex flex-col rounded-2xl bg-slate-900 border border-indigo-500/40 shadow-2xl shadow-indigo-500/20 text-slate-100 overflow-hidden">
        
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-800 bg-slate-950/70">
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 shadow-lg shadow-indigo-500/30">
              <Brain className="w-6 h-6 text-white" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-lg font-bold text-transparent bg-clip-text bg-gradient-to-r from-indigo-300 via-purple-300 to-pink-300">
                  Bản Đồ Tư Duy Sư Phạm 5 Bước (Cognitive Scaffold)
                </h3>
                {walkthrough?.isFromSemanticCache && (
                  <span className="flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                    <Database className="w-3 h-3" /> Semantic Cache (0$)
                  </span>
                )}
              </div>
              <p className="text-xs text-slate-400">
                Phân tích sâu kẽ hở, căn cứ pháp lý & bẫy nhận thức đa tác tử
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={handleCopy}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-xs font-medium text-slate-300 transition-colors border border-slate-700"
              title="Sao chép toàn bộ lời giải"
            >
              {copied ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
              {copied ? 'Đã sao chép' : 'Sao chép'}
            </button>
            <button
              onClick={onClose}
              className="p-1.5 rounded-lg hover:bg-slate-800 text-slate-400 hover:text-white transition-colors"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Telemetry Strip */}
        {walkthrough && (
          <div className="flex flex-wrap items-center justify-between px-6 py-2 bg-slate-950/40 border-b border-slate-800/80 text-xs">
            <div className="flex items-center gap-3">
              <span className={`px-2.5 py-0.5 rounded-full border text-[11px] font-medium ${bloom.color}`}>
                {bloom.text}
              </span>
              <span className="flex items-center gap-1 text-slate-400">
                <Gauge className="w-3.5 h-3.5 text-indigo-400" />
                Độ khó IRT: <strong className="text-slate-200">{walkthrough.estimatedDifficultyIrt.toFixed(2)}</strong>
              </span>
              <span className="flex items-center gap-1 text-slate-400">
                <Zap className="w-3.5 h-3.5 text-amber-400" />
                Provider: <strong className="text-amber-300">{walkthrough.resolvedByProvider || 'Universal Router'}</strong>
                {walkthrough.resolvedByModel && <span className="text-slate-500">({walkthrough.resolvedByModel})</span>}
              </span>
            </div>

            {walkthrough.executionTimeMs && (
              <span className="text-[11px] text-slate-500">
                Thời gian xử lý: {walkthrough.executionTimeMs}ms
              </span>
            )}
          </div>
        )}

        {/* Persona Selector Toolbar */}
        <div className="flex items-center justify-between px-6 py-2 bg-slate-900/90 border-b border-slate-800 text-xs">
          <span className="text-slate-400 font-medium flex items-center gap-1.5">
            <Sparkles className="w-3.5 h-3.5 text-purple-400" />
            Chọn Cốt Cách Tác Tử:
          </span>
          <div className="flex items-center gap-1.5 overflow-x-auto">
            {[
              { id: 1, name: '⚖️ Giám Khảo Tối Cao' },
              { id: 2, name: '🏛️ Pháp Lý Ngân Hàng' },
              { id: 3, name: '🧙 Socrates Mentor' },
              { id: 4, name: '🏹 Kẻ Đi Săn AI' },
              { id: 5, name: '🔬 Bác Sĩ Tâm Lý' }
            ].map(p => (
              <button
                key={p.id}
                onClick={() => {
                  setSelectedPersona(p.id);
                  onRegenerateWithPersona?.(p.id);
                }}
                className={`px-2.5 py-1 rounded-md transition-all font-medium ${
                  selectedPersona === p.id 
                    ? 'bg-indigo-600 text-white shadow-md shadow-indigo-500/20' 
                    : 'bg-slate-800/80 text-slate-400 hover:bg-slate-800 hover:text-slate-200'
                }`}
              >
                {p.name}
              </button>
            ))}
          </div>
        </div>

        {/* Content Body */}
        <div className="flex-1 overflow-y-auto p-6 space-y-5">
          {isLoading ? (
            <div className="py-20 flex flex-col items-center justify-center space-y-4">
              <RefreshCw className="w-10 h-10 text-indigo-400 animate-spin" />
              <p className="text-sm text-slate-300 font-medium animate-pulse">
                Đang triệu hồi Hội đồng Đa Đặc Vụ thẩm định & sinh bản đồ tư duy...
              </p>
              <p className="text-xs text-slate-500">
                (Định tuyến: DeepSeek-R1 / Gemini 2.0 Flash • Kiểm tra Semantic Cache)
              </p>
            </div>
          ) : !walkthrough ? (
            <div className="py-16 text-center text-slate-500">
              Chưa có dữ liệu bản đồ tư duy. Vui lòng bấm vào câu hỏi để kích hoạt phân tích.
            </div>
          ) : (
            <>
              {/* Question Context Card */}
              <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800/80 space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-semibold uppercase tracking-wider text-indigo-400">
                    Câu hỏi đang phân tích
                  </span>
                  <span className="px-2 py-0.5 rounded text-xs font-bold bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                    Đáp án đúng: {walkthrough.correctAnswer}
                  </span>
                </div>
                <p className="text-sm font-medium text-slate-200 leading-relaxed">
                  {walkthrough.questionContent}
                </p>
              </div>

              {/* 5 Cognitive Steps Grid */}
              <div className="grid grid-cols-1 gap-4">
                
                {/* Bước 1 */}
                <div className="p-4 rounded-xl bg-gradient-to-r from-blue-950/30 to-slate-900 border border-blue-500/30 hover:border-blue-500/50 transition-all space-y-1.5">
                  <div className="flex items-center gap-2 text-blue-400 font-bold text-xs">
                    <BookOpen className="w-4 h-4" />
                    <span>BƯỚC 1: BÓC TÁCH DỮ KIỆN & TỪ KHÓA CỐT LÕI (PROMPT ANATOMY)</span>
                  </div>
                  <p className="text-sm text-slate-300 leading-relaxed whitespace-pre-line pl-6">
                    {walkthrough.promptAnatomy || 'Đang bóc tách từ khóa then chốt...'}
                  </p>
                </div>

                {/* Bước 2 */}
                <div className="p-4 rounded-xl bg-gradient-to-r from-emerald-950/30 to-slate-900 border border-emerald-500/30 hover:border-emerald-500/50 transition-all space-y-1.5">
                  <div className="flex items-center gap-2 text-emerald-400 font-bold text-xs">
                    <Shield className="w-4 h-4" />
                    <span>BƯỚC 2: TỌA ĐỘ PHÁP LÝ & TRI THỨC ÁP DỤNG (THEORETICAL GROUNDING)</span>
                  </div>
                  <p className="text-sm text-slate-300 leading-relaxed whitespace-pre-line pl-6">
                    {walkthrough.theoreticalGrounding || 'Căn cứ quy định chuẩn mực ngành...'}
                  </p>
                </div>

                {/* Bước 3 */}
                <div className="p-4 rounded-xl bg-gradient-to-r from-rose-950/30 to-slate-900 border border-rose-500/30 hover:border-rose-500/50 transition-all space-y-1.5">
                  <div className="flex items-center gap-2 text-rose-400 font-bold text-xs">
                    <AlertTriangle className="w-4 h-4" />
                    <span>BƯỚC 3: GIẢI PHẪU PHƯƠNG ÁN BẪY & LỖI SAI KINH ĐIỂN (DISTRACTOR AUTOPSY)</span>
                  </div>
                  <p className="text-sm text-slate-300 leading-relaxed whitespace-pre-line pl-6">
                    {walkthrough.distractorAutopsy || 'Phân tích các cạm bẫy nhận thức...'}
                  </p>
                </div>

                {/* Bước 4 */}
                <div className="p-4 rounded-xl bg-gradient-to-r from-amber-950/30 to-slate-900 border border-amber-500/30 hover:border-amber-500/50 transition-all space-y-1.5">
                  <div className="flex items-center gap-2 text-amber-400 font-bold text-xs">
                    <Lightbulb className="w-4 h-4" />
                    <span>BƯỚC 4: MẸO NHỚ NHANH & PHẢN XẠ 1 NỐT NHẠC (MNEMONIC & QUICK RECALL)</span>
                  </div>
                  <p className="text-sm text-slate-300 leading-relaxed whitespace-pre-line pl-6">
                    {walkthrough.mnemonicAndRecall || 'Bí quyết ghi nhớ công thức ngắn gọn...'}
                  </p>
                </div>

                {/* Bước 5 */}
                <div className="p-4 rounded-xl bg-gradient-to-r from-purple-950/30 to-slate-900 border border-purple-500/30 hover:border-purple-500/50 transition-all space-y-1.5">
                  <div className="flex items-center gap-2 text-purple-400 font-bold text-xs">
                    <HelpCircle className="w-4 h-4" />
                    <span>BƯỚC 5: BÀI TOÁN TÌNH HUỐNG MỞ RỘNG TƯ DUY (EXTRAPOLATED CASE STUDY)</span>
                  </div>
                  <p className="text-sm text-slate-300 leading-relaxed whitespace-pre-line pl-6">
                    {walkthrough.extrapolatedCase || 'Bài toán tình huống thực tế chuyên sâu...'}
                  </p>
                </div>

              </div>
            </>
          )}
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between px-6 py-3 border-t border-slate-800 bg-slate-950/70 text-xs text-slate-400">
          <span>Aegis Cognitive Multi-Agent OS (ACM-OS) • 2026 Edition</span>
          <button
            onClick={onClose}
            className="px-4 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white font-semibold transition-colors"
          >
            Đóng Lời Giải
          </button>
        </div>

      </div>
    </div>
  );
};
export default AiCognitiveWalkthroughModal;
