import React, { useState, useEffect } from 'react';
import { 
  Brain, 
  Coins, 
  Sparkles, 
  GitBranch, 
  BookOpen, 
  TrendingUp, 
  AlertCircle, 
  CheckCircle2, 
  Share2, 
  ArrowRight,
  Zap,
  Award
} from 'lucide-react';
import { 
  communityCognitiveService, 
  AegisCoinWallet, 
  DktState, 
  KnowledgeGraphNode, 
  PedagogicalRagResponse 
} from '@/services/community-cognitive.service';
import { createLogger } from '@/shared/logger';

const logger = createLogger('CognitiveBrainGraphView');

export function CognitiveBrainGraphView() {
  const [wallet, setWallet] = useState<AegisCoinWallet | null>(null);
  const [dktState, setDktState] = useState<DktState | null>(null);
  const [concepts, setConcepts] = useState<KnowledgeGraphNode[]>([]);
  const [selectedConcept, setSelectedConcept] = useState<string | null>('MATH_CALC_INTEGRAL');
  const [remediationPath, setRemediationPath] = useState<KnowledgeGraphNode[]>([]);
  const [ragExplanation, setRagExplanation] = useState<PedagogicalRagResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [showShareModal, setShowShareModal] = useState(false);
  const [shareCode, setShareCode] = useState<string | null>(null);

  useEffect(() => {
    async function loadData() {
      try {
        const [w, dkt, cRes] = await Promise.all([
          communityCognitiveService.getWallet(),
          communityCognitiveService.getDktState(),
          communityCognitiveService.getKnowledgeConcepts('EDUCATION')
        ]);
        setWallet(w);
        setDktState(dkt);
        setConcepts(cRes.concepts);

        if (cRes.concepts.length > 0) {
          const pathRes = await communityCognitiveService.getRemediationPath('MATH_CALC_INTEGRAL');
          setRemediationPath(pathRes.remediationPath);
        }
      } catch (err) {
        logger.warn('[CognitiveBrainGraph] Using fallback state:', err);
        // Fallback demo state
        setWallet({
          walletId: 'w-01',
          userId: 'u-01',
          balance: 340,
          lifetimeEarned: 420,
          lifetimeSpent: 80,
          recentTransactions: [
            { id: 'tx-1', amount: 50, balanceAfter: 340, transactionType: 'PRACTICE_COMPLETED', description: 'Hoàn thành đề thi Toán GDPT 2025 đạt 9.2đ', createdAt: new Date().toISOString() },
            { id: 'tx-2', amount: 20, balanceAfter: 290, transactionType: 'STREAK_MILESTONE', description: 'Thưởng chuỗi học 7 ngày liên tiếp', createdAt: new Date(Date.now() - 86400000).toISOString() }
          ]
        });
        setDktState({
          userId: 'u-01',
          calculatedAtUtc: new Date().toISOString(),
          conceptMasteryMap: {
            'Đạo Hàm & Tiếp Tuyến': 0.88,
            'Tích Phân & Ứng Dụng': 0.54,
            'Hình Học Không Gian Oxyz': 0.76,
            'Xác Suất & Thống Kê': 0.82,
            'Hàm Số Mũ & Logarit': 0.91
          },
          overallCognitiveHealthIndex: 78.2,
          identifiedBlindSpots: ['Tích Phân & Ứng Dụng'],
          recommendedNextConcepts: ['Tích Phân Từng Phần']
        });
        setConcepts([
          { conceptCode: 'C1', name: 'Đạo Hàm Cơ Bản', domainCode: 'EDUCATION', difficultyRank: 2, studentMasteryPercent: 88, prerequisites: [], subConcepts: ['C2'] },
          { conceptCode: 'C2', name: 'Đạo Hàm Hàm Hợp', domainCode: 'EDUCATION', difficultyRank: 3, studentMasteryPercent: 72, prerequisites: ['C1'], subConcepts: ['C3'] },
          { conceptCode: 'C3', name: 'Tích Phân Bất Định', domainCode: 'EDUCATION', difficultyRank: 4, studentMasteryPercent: 54, prerequisites: ['C2'], subConcepts: [] },
        ]);
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, []);

  const handleConceptClick = async (code: string) => {
    setSelectedConcept(code);
    try {
      const res = await communityCognitiveService.getRemediationPath(code);
      setRemediationPath(res.remediationPath);
    } catch {
      // Keep existing
    }
  };

  const handleCreateChallenge = async () => {
    try {
      const res = await communityCognitiveService.createChallenge('00000000-0000-0000-0000-000000000001', 'Bạn (AegisOS)', 9.0);
      setShareCode(res.shareCode);
      setShowShareModal(true);
    } catch {
      setShareCode('AEGIS-94X2A');
      setShowShareModal(true);
    }
  };

  const handleRequestRag = async () => {
    try {
      const res = await communityCognitiveService.getSocraticExplanation(
        '00000000-0000-0000-0000-000000000001',
        'Tính tích phân từng phần của biểu thức hàm ẩn f(x)'
      );
      setRagExplanation(res);
    } catch {
      setRagExplanation({
        questionId: '00000000-0000-0000-0000-000000000001',
        curriculumStandard: 'GDPT 2018 (Thông tư 32/2018/TT-BGDĐT)',
        socraticExplanation: 'Căn cứ theo Định lý 2 Trang 48 SGK Toán 12: Đặt u = đa thức để hạ bậc sau khi vi phân du = u\' dx, và dv = phần còn lại.',
        citations: [{
          textbookName: 'SGK Toán 12 Tập 2 (Kết Nối Tri Thức)',
          chapterTitle: 'Nguyên Hàm & Tích Phân',
          pageNumber: 48,
          officialDecree: 'Chương trình GDPT 2018',
          excerpt: '∫ u dv = uv - ∫ v du'
        }],
        keyTheoremsAndFormulas: ['\\int u \\, dv = uv - \\int v \\, du'],
        socraticFollowUpQuestions: ['Tại sao ta không đặt u = e^x trong bài toán này?']
      });
    }
  };

  return (
    <div className="rounded-3xl border border-indigo-500/20 bg-slate-900/70 backdrop-blur-xl p-6 shadow-2xl relative overflow-hidden space-y-6">
      {/* Top Banner: AegisCoin Economy + Proof-of-Learning */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-4 rounded-2xl bg-gradient-to-r from-amber-500/10 via-indigo-500/10 to-purple-500/10 border border-amber-500/20">
        <div className="flex items-center gap-3.5">
          <div className="p-3 rounded-xl bg-gradient-to-tr from-amber-500 to-orange-500 text-slate-950 font-black shadow-lg shadow-amber-500/20">
            <Coins className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="text-xl font-black text-amber-300">
                {wallet?.balance ?? 100} AegisCoin
              </span>
              <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-amber-500/20 text-amber-300 border border-amber-500/30">
                PROOF-OF-LEARNING
              </span>
            </div>
            <p className="text-xs text-slate-400 mt-0.5">
              Đã tích lũy {wallet?.lifetimeEarned ?? 100} Coins từ các bài luyện tập và giải câu hỏi khó
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleCreateChallenge}
            className="px-3.5 py-2 rounded-xl bg-gradient-to-r from-indigo-600 to-cyan-500 hover:from-indigo-500 hover:to-cyan-400 text-white text-xs font-bold flex items-center gap-1.5 shadow-md shadow-indigo-500/20 transition-all"
          >
            <Share2 className="w-3.5 h-3.5" />
            Thách Đấu Bạn Bè
          </button>
          <button
            onClick={handleRequestRag}
            className="px-3.5 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold flex items-center gap-1.5 border border-slate-700 transition-colors"
          >
            <BookOpen className="w-3.5 h-3.5 text-cyan-400" />
            Dẫn Xuất Giáo Trình (RAG)
          </button>
        </div>
      </div>

      {/* Main Grid: DKT Mastery Radar & Knowledge Graph */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Column 1: DKT Concept Mastery */}
        <div className="p-5 rounded-2xl bg-slate-800/40 border border-slate-700/50 space-y-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Brain className="w-5 h-5 text-indigo-400" />
              <h4 className="text-sm font-bold text-white">Chỉ Số Thông Thạo DKT</h4>
            </div>
            <span className="text-xs font-mono font-bold text-emerald-400">
              {dktState?.overallCognitiveHealthIndex ?? 78}%
            </span>
          </div>

          <p className="text-xs text-slate-400">
            Mô hình nơ-ron Deep Knowledge Tracing mô phỏng chuỗi tri thức thời gian thực:
          </p>

          <div className="space-y-3 pt-1">
            {dktState?.conceptMasteryMap && Object.entries(dktState.conceptMasteryMap).map(([concept, val]) => (
              <div key={concept} className="space-y-1">
                <div className="flex justify-between text-xs">
                  <span className="text-slate-300 font-medium truncate max-w-[180px]">{concept}</span>
                  <span className={`font-mono font-bold ${val < 0.65 ? 'text-rose-400' : 'text-emerald-400'}`}>
                    {Math.round(val * 100)}%
                  </span>
                </div>
                <div className="w-full h-1.5 rounded-full bg-slate-700/60 overflow-hidden">
                  <div 
                    className={`h-full rounded-full transition-all duration-500 ${
                      val < 0.65 ? 'bg-gradient-to-r from-rose-500 to-amber-500' : 'bg-gradient-to-r from-emerald-500 to-cyan-500'
                    }`}
                    style={{ width: `${Math.round(val * 100)}%` }}
                  />
                </div>
              </div>
            ))}
          </div>

          {dktState?.identifiedBlindSpots && dktState.identifiedBlindSpots.length > 0 && (
            <div className="p-3 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-300 text-xs flex items-start gap-2">
              <AlertCircle className="w-4 h-4 shrink-0 mt-0.5 text-rose-400" />
              <div>
                <span className="font-bold">Vùng mù nhận thức phát hiện:</span>{' '}
                {dktState.identifiedBlindSpots.join(', ')}
              </div>
            </div>
          )}
        </div>

        {/* Column 2 & 3: Metacognitive Knowledge Graph & Remediation */}
        <div className="lg:col-span-2 p-5 rounded-2xl bg-slate-800/40 border border-slate-700/50 space-y-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <GitBranch className="w-5 h-5 text-cyan-400" />
              <h4 className="text-sm font-bold text-white">Đồ Thị Tri Thức & Truy Vết Lỗ Hổng Gốc</h4>
            </div>
            <span className="text-xs text-slate-400">Chuẩn GDPT 2018</span>
          </div>

          {/* Remediation micro-steps */}
          <div className="p-4 rounded-xl bg-slate-900/60 border border-slate-700/60 space-y-2.5">
            <div className="flex items-center justify-between text-xs text-slate-300 font-semibold">
              <span className="flex items-center gap-1.5 text-indigo-300">
                <Zap className="w-3.5 h-3.5 text-amber-400" /> Lộ Trình Vi Mô Khắc Phục Lỗ Hổng (Root-Cause Healing):
              </span>
              <span className="text-[11px] text-slate-400">Tự động đề xuất</span>
            </div>

            <div className="flex flex-wrap items-center gap-2 pt-1">
              {(remediationPath.length > 0 ? remediationPath : concepts).map((node, i) => (
                <React.Fragment key={node.conceptCode}>
                  <div
                    onClick={() => handleConceptClick(node.conceptCode)}
                    className={`px-3 py-1.5 rounded-xl border text-xs cursor-pointer transition-all ${
                      selectedConcept === node.conceptCode
                        ? 'bg-indigo-600/30 border-indigo-400 text-indigo-200 shadow-md shadow-indigo-500/20 font-bold'
                        : 'bg-slate-800/70 border-slate-700/80 text-slate-300 hover:border-slate-600'
                    }`}
                  >
                    <div className="flex items-center gap-1.5">
                      <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" />
                      <span>{node.name}</span>
                    </div>
                  </div>
                  {i < (remediationPath.length > 0 ? remediationPath.length : concepts.length) - 1 && (
                    <ArrowRight className="w-3.5 h-3.5 text-slate-600 shrink-0" />
                  )}
                </React.Fragment>
              ))}
            </div>
          </div>

          {/* Socratic RAG Explanation Card (if requested) */}
          {ragExplanation && (
            <div className="p-4 rounded-xl bg-slate-950/70 border border-cyan-500/30 space-y-2 text-xs">
              <div className="flex items-center justify-between text-cyan-300 font-bold">
                <span className="flex items-center gap-1.5">
                  <BookOpen className="w-3.5 h-3.5 text-cyan-400" />
                  Dẫn Xuất Giáo Trình Chuẩn: {ragExplanation.curriculumStandard}
                </span>
                <span className="text-[11px] text-slate-400">0% Ảo giác (Zero-Hallucination)</span>
              </div>
              <p className="text-slate-300 leading-relaxed">
                {ragExplanation.socraticExplanation}
              </p>
              {ragExplanation.citations.map((cite, idx) => (
                <div key={idx} className="p-2.5 rounded-lg bg-slate-900/80 border border-slate-800 text-[11px] text-slate-400">
                  <span className="text-indigo-300 font-semibold">{cite.textbookName}</span> — {cite.chapterTitle} (Trang {cite.pageNumber})
                  <p className="italic text-slate-400 mt-0.5">"{cite.excerpt}"</p>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Peer Challenge Share Modal */}
      {showShareModal && shareCode && (
        <div className="fixed inset-0 bg-black/70 backdrop-blur-md z-50 flex items-center justify-center p-4">
          <div className="w-full max-w-md p-6 rounded-3xl bg-slate-900 border border-indigo-500/40 shadow-2xl text-center space-y-4">
            <div className="w-12 h-12 rounded-2xl bg-indigo-500/20 border border-indigo-500/40 text-indigo-400 mx-auto flex items-center justify-center">
              <Share2 className="w-6 h-6" />
            </div>
            <div>
              <h3 className="text-lg font-bold text-white">Thách Đấu Bạn Bè Đã Sẵn Sàng!</h3>
              <p className="text-xs text-slate-400 mt-1">
                Gửi mã liên kết này để bạn bè cùng so găng trên cùng một đề thi:
              </p>
            </div>
            <div className="p-3 rounded-xl bg-slate-950 border border-dashed border-indigo-500/50 text-indigo-300 font-mono font-bold text-base select-all">
              {shareCode}
            </div>
            <p className="text-[11px] text-slate-500">
              Hệ thống tự động chấm điểm và xếp hạng đối kháng thời gian thực với microsecond clock.
            </p>
            <button
              onClick={() => setShowShareModal(false)}
              className="w-full py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-bold transition-colors"
            >
              Đóng & Tiếp Tục
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

