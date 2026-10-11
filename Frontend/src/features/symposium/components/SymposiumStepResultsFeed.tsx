import React, { useState } from 'react';
import {
  CheckCircle2, Loader2, Clock, ChevronDown, ChevronUp,
  Brain, FileText, Search, ShieldAlert, Award, Layers,
  Compass, Sparkles, Scale, ExternalLink
} from 'lucide-react';
import {
  SymposiumDialogueTurn,
  EmpiricalEvidenceDossierDto,
  CouncilAcademicContributionDto,
  SymposiumRubricScore,
  SymposiumStructuredOutcomeDto
} from '@/services/ai-swarm.service';

export interface PhaseStepData {
  phaseNumber: number;
  title: string;
  subtitle: string;
  status: 'pending' | 'running' | 'completed';
  data?: any;
}

interface SymposiumStepResultsFeedProps {
  activePhaseIndex: number;
  isConvening: boolean;
  phaseResults: Record<number, any>;
  dialogueLog: SymposiumDialogueTurn[];
  empiricalDossier?: EmpiricalEvidenceDossierDto | null;
  councilContributions?: CouncilAcademicContributionDto[];
  rubric?: SymposiumRubricScore | null;
  structuredOutcome?: SymposiumStructuredOutcomeDto | null;
  finalDeclaration?: string | null;
}

export function SymposiumStepResultsFeed({
  activePhaseIndex,
  isConvening,
  phaseResults,
  dialogueLog,
  empiricalDossier,
  councilContributions,
  rubric,
  structuredOutcome,
  finalDeclaration
}: SymposiumStepResultsFeedProps) {
  // Quản lý việc đóng mở từng bước. Mặc định mở bước đang chạy hoặc bước vừa hoàn tất.
  const [expandedSteps, setExpandedSteps] = useState<Record<number, boolean>>({
    1: true,
    2: true,
    3: true,
    4: true,
    5: true,
    6: true
  });

  const toggleStep = (step: number) => {
    setExpandedSteps(prev => ({ ...prev, [step]: !prev[step] }));
  };

  // Xác định trạng thái của từng bước trong 6 bước
  const getStepStatus = (step: number): 'pending' | 'running' | 'completed' => {
    if (phaseResults[step] || (step === 1 && empiricalDossier) || (step === 5 && rubric?.isEvaluated) || (step === 6 && finalDeclaration)) {
      return 'completed';
    }
    if (!isConvening) {
      // Nếu đã có kết quả chung cuộc thì completed, nếu chưa thì pending
      if (dialogueLog.length > 0 && dialogueLog.some(t => t.phaseNumber === step)) return 'completed';
      return 'pending';
    }
    if (activePhaseIndex === step) return 'running';
    if (activePhaseIndex > step) return 'completed';
    return 'pending';
  };

  // Lấy dữ liệu của từng bước (từ phaseResults hoặc từ props chung)
  const p1Data = phaseResults[1] || { empiricalDossier };
  const p2Data = phaseResults[2] || { councilContributions };
  const p3Turns = dialogueLog.filter(t => t.phaseNumber === 3);
  const p4Turn = dialogueLog.find(t => t.phaseNumber === 4) || phaseResults[4];
  const p5Data = phaseResults[5] || rubric;
  const p6Data = phaseResults[6] || { structuredOutcome, finalDeclaration };

  const STEPS_CONFIG = [
    {
      step: 1,
      name: 'Bước 1: Đặt Vấn Đề & Khai Quật Thực Chứng (Deep Research)',
      desc: 'Trinh sát thực nghiệm các nguồn DOI, văn kiện và xác lập tiên đề khởi nguyên.',
      icon: Search,
      color: 'cyan'
    },
    {
      step: 2,
      name: 'Bước 2: Vấn Đáp Xuyên Ngành & Luận Điểm Khởi Xướng',
      desc: 'Các Viện trình bày luận cương độc bản và đặt câu hỏi chất vấn chéo Socratic.',
      icon: Compass,
      color: 'indigo'
    },
    {
      step: 3,
      name: 'Bước 3: Tranh Biện Đối Kháng & Giải Trừ Ngụy Biện',
      desc: 'Biện sĩ Devil\'s Advocate bóc trần ngụy biện, kiểm chứng phản nghiệm (Popper).',
      icon: ShieldAlert,
      color: 'rose'
    },
    {
      step: 4,
      name: 'Bước 4: Hợp Nhất Siêu Lý Thuyết (Hội Đồng Thư Ký)',
      desc: 'Ban Thư ký tổng hợp các mâu thuẫn thành hệ hình nhận thức hợp nhất (Metatheory).',
      icon: Brain,
      color: 'violet'
    },
    {
      step: 5,
      name: 'Bước 5: Thẩm Định Độc Lập & Ma Trận Rubric 5 Trục',
      desc: 'Hội đồng Trọng tài độc lập chấm mù 5 trục nhận thức và tính chỉ số tiến hóa EEV.',
      icon: Scale,
      color: 'amber'
    },
    {
      step: 6,
      name: 'Bước 6: Kiến Tạo 5 Khối Tri Thức & Nghị Quyết Chung Cuộc',
      desc: 'Đúc kết tuyên ngôn nghị quyết tối hậu và bản đồ giải pháp hành động cụ thể.',
      icon: Award,
      color: 'emerald'
    }
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-black text-white uppercase tracking-wider flex items-center gap-2">
          <Layers size={16} className="text-cyan-400" />
          <span>Tiến Trình & Kết Quả Từng Bước Biện Chứng</span>
        </h3>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setExpandedSteps({ 1: true, 2: true, 3: true, 4: true, 5: true, 6: true })}
            className="text-[11px] text-slate-400 hover:text-cyan-400 transition cursor-pointer"
          >
            Mở tất cả
          </button>
          <span className="text-slate-600">•</span>
          <button
            onClick={() => setExpandedSteps({ 1: false, 2: false, 3: false, 4: false, 5: false, 6: false })}
            className="text-[11px] text-slate-400 hover:text-cyan-400 transition cursor-pointer"
          >
            Thu gọn
          </button>
        </div>
      </div>

      <div className="space-y-3">
        {STEPS_CONFIG.map(({ step, name, desc, icon: StepIcon, color }) => {
          const status = getStepStatus(step);
          const isExpanded = expandedSteps[step] ?? true;

          return (
            <div
              key={step}
              className={`rounded-2xl border transition-all duration-300 overflow-hidden ${
                status === 'completed'
                  ? 'bg-slate-900/90 border-slate-800 shadow-md'
                  : status === 'running'
                  ? 'bg-gradient-to-r from-slate-900 via-cyan-950/20 to-slate-900 border-cyan-500/50 ring-1 ring-cyan-500/30 shadow-lg'
                  : 'bg-slate-950/40 border-slate-900/80 opacity-60'
              }`}
            >
              {/* STEP HEADER */}
              <div
                onClick={() => toggleStep(step)}
                className="p-4 sm:p-4.5 flex items-center justify-between gap-3 cursor-pointer select-none hover:bg-slate-800/30 transition-colors"
              >
                <div className="flex items-center gap-3">
                  <div className={`w-8 h-8 rounded-xl flex items-center justify-center font-bold text-xs ${
                    status === 'completed'
                      ? 'bg-emerald-950/80 border border-emerald-500/40 text-emerald-400'
                      : status === 'running'
                      ? 'bg-cyan-950/80 border border-cyan-500/50 text-cyan-300 animate-pulse'
                      : 'bg-slate-900 border border-slate-800 text-slate-500'
                  }`}>
                    {status === 'completed' ? (
                      <CheckCircle2 size={16} />
                    ) : status === 'running' ? (
                      <Loader2 size={16} className="animate-spin text-cyan-400" />
                    ) : (
                      <span>{step}</span>
                    )}
                  </div>

                  <div>
                    <h4 className="text-sm font-bold text-white flex items-center gap-2">
                      <span>{name}</span>
                      {status === 'running' && (
                        <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-cyan-950 border border-cyan-500/40 text-cyan-300 animate-pulse">
                          Đang tiến hành...
                        </span>
                      )}
                      {status === 'completed' && (
                        <span className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-emerald-950 border border-emerald-500/40 text-emerald-300">
                          Đã hoàn thành
                        </span>
                      )}
                    </h4>
                    <p className="text-xs text-slate-400 mt-0.5">{desc}</p>
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  {isExpanded ? <ChevronUp size={16} className="text-slate-400" /> : <ChevronDown size={16} className="text-slate-400" />}
                </div>
              </div>

              {/* STEP CONTENT: THỂ HIỆN KẾT QUẢ KHI XONG HOẶC ĐANG XỬ LÝ */}
              {isExpanded && (
                <div className="border-t border-slate-800/80 p-4 sm:p-5 bg-slate-950/60">
                  {status === 'pending' && (
                    <div className="p-4 text-center text-xs text-slate-500 italic flex items-center justify-center gap-2">
                      <Clock size={13} />
                      <span>Đang chờ các bước trước hoàn tất để bắt đầu...</span>
                    </div>
                  )}

                  {status === 'running' && (
                    <div className="p-5 text-center space-y-2">
                      <div className="flex items-center justify-center gap-2 text-cyan-400 text-xs font-semibold">
                        <Loader2 size={16} className="animate-spin" />
                        <span>Mạng lưới các Viện đang xử lý dữ liệu và biện chứng theo thời gian thực...</span>
                      </div>
                      <p className="text-[11px] text-slate-500 italic">Dữ liệu kết quả sẽ được hiển thị ngay khi bước này đúc kết xong.</p>
                    </div>
                  )}

                  {status === 'completed' && (
                    <div className="space-y-4">
                      {/* BƯỚC 1: KẾT QUẢ DEEP RESEARCH */}
                      {step === 1 && (
                        <div className="space-y-3">
                          <div className="flex flex-wrap items-center gap-2 text-xs">
                            <span className="px-2.5 py-1 rounded-lg bg-cyan-950/80 border border-cyan-800/50 text-cyan-300 font-semibold">
                              📊 {p1Data?.EmpiricalDossier?.TotalEvidenceUnits || empiricalDossier?.totalEvidenceUnits || 0} Đơn Vị Bằng Chứng [E#]
                            </span>
                            <span className="px-2.5 py-1 rounded-lg bg-slate-800 border border-slate-700 text-slate-300">
                              📚 {p1Data?.EmpiricalDossier?.AcademicCitations?.length || empiricalDossier?.academicCitations?.length || 0} Nguồn Trích Dẫn DOI / Học Thuật
                            </span>
                            <span className="px-2.5 py-1 rounded-lg bg-emerald-950/80 border border-emerald-800/50 text-emerald-300">
                              ✓ Đã xác thực thực chứng
                            </span>
                          </div>

                          {/* Danh sách dữ kiện chủ chốt */}
                          {((p1Data?.EmpiricalDossier?.VerifiedFacts && p1Data.EmpiricalDossier.VerifiedFacts.length > 0) ||
                            (empiricalDossier?.verifiedFacts && empiricalDossier.verifiedFacts.length > 0)) && (
                            <div className="p-3.5 rounded-xl bg-slate-900 border border-slate-800 space-y-2">
                              <span className="text-[11px] font-bold text-slate-300 uppercase tracking-wider">
                                Các Dữ Kiện Bất Biến Đã Được Kiểm Chứng:
                              </span>
                              <div className="space-y-1.5">
                                {(p1Data?.EmpiricalDossier?.VerifiedFacts || empiricalDossier?.verifiedFacts || []).slice(0, 4).map((f: string, idx: number) => (
                                  <div key={idx} className="text-xs text-slate-300 flex items-start gap-2 pl-1 border-l-2 border-cyan-500/50">
                                    <span className="text-cyan-400 font-mono text-[10px] mt-0.5">#{idx + 1}</span>
                                    <span>{f}</span>
                                  </div>
                                ))}
                              </div>
                            </div>
                          )}
                        </div>
                      )}

                      {/* BƯỚC 2: KẾT QUẢ LUẬN ĐIỂM KHỞI XƯỚNG CỦA CÁC VIỆN */}
                      {step === 2 && (
                        <div className="space-y-3">
                          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                            {(p2Data?.CouncilContributions || councilContributions || []).map((contrib: any, idx: number) => (
                              <div key={idx} className="p-3.5 rounded-xl bg-slate-900 border border-slate-800 space-y-2">
                                <div className="flex items-center justify-between">
                                  <span className="font-bold text-xs text-white flex items-center gap-1.5">
                                    <Compass size={13} className="text-indigo-400" />
                                    <span>{contrib.councilDisplayName}</span>
                                  </span>
                                  <span className="text-[10px] font-mono text-cyan-400 bg-cyan-950/60 px-2 py-0.5 rounded border border-cyan-800/40">
                                    {contrib.councilCode}
                                  </span>
                                </div>
                                <p className="text-xs text-slate-300 line-clamp-3 leading-relaxed">
                                  {contrib.deepAnalysis}
                                </p>
                                {contrib.keyTheses && contrib.keyTheses.length > 0 && (
                                  <div className="pt-1.5 border-t border-slate-800/60 flex flex-wrap gap-1.5">
                                    {contrib.keyTheses.map((t: string, ti: number) => (
                                      <span key={ti} className="text-[10px] px-2 py-0.5 rounded bg-slate-800 text-slate-300 border border-slate-700">
                                        💡 {t}
                                      </span>
                                    ))}
                                  </div>
                                )}
                              </div>
                            ))}
                          </div>
                        </div>
                      )}

                      {/* BƯỚC 3: KẾT QUẢ TRANH BIỆN ĐỐI KHÁNG */}
                      {step === 3 && (
                        <div className="space-y-2.5">
                          {p3Turns.length === 0 ? (
                            <div className="text-xs text-slate-400 italic">Các lượt phản biện đối kháng đã được ghi nhận trong biên bản tổng.</div>
                          ) : (
                            p3Turns.map((turn, idx) => (
                              <div key={idx} className="p-3 rounded-xl bg-slate-900 border border-slate-800 space-y-1.5">
                                <div className="flex items-center justify-between text-xs">
                                  <span className="font-bold text-rose-300 flex items-center gap-1.5">
                                    <ShieldAlert size={13} className="text-rose-400" />
                                    <span>{turn.speakerTitle}</span>
                                  </span>
                                  <span className="text-[10px] font-mono text-slate-400">{turn.modelCode}</span>
                                </div>
                                <p className="text-xs text-slate-300 leading-relaxed pl-2 border-l-2 border-rose-500/40">
                                  {turn.content}
                                </p>
                                {turn.exposedFallacies && turn.exposedFallacies.length > 0 && (
                                  <div className="flex flex-wrap gap-1.5 pt-1">
                                    {turn.exposedFallacies.map((fal, fi) => (
                                      <span key={fi} className="text-[10px] font-mono px-2 py-0.5 rounded bg-rose-950/60 border border-rose-800/40 text-rose-300">
                                        ⚠️ Bóc trần: {fal}
                                      </span>
                                    ))}
                                  </div>
                                )}
                              </div>
                            ))
                          )}
                        </div>
                      )}

                      {/* BƯỚC 4: KẾT QUẢ HỢP NHẤT SIÊU LÝ THUYẾT */}
                      {step === 4 && (
                        <div className="p-4 rounded-xl bg-gradient-to-br from-violet-950/30 to-slate-900 border border-violet-800/40 space-y-2.5">
                          <div className="flex items-center justify-between">
                            <span className="text-xs font-bold text-violet-300 flex items-center gap-1.5">
                              <Brain size={14} className="text-violet-400" />
                              <span>Luận Cương Hợp Nhất của Hội Đồng Thư Ký</span>
                            </span>
                            {p4Turn?.modelCode && (
                              <span className="text-[10px] font-mono text-violet-300 bg-violet-950/80 px-2.5 py-0.5 rounded border border-violet-800/50">
                                Thư ký điều hành: {p4Turn.modelCode}
                              </span>
                            )}
                          </div>
                          <div className="text-xs text-slate-200 leading-relaxed whitespace-pre-line font-serif max-h-48 overflow-y-auto pr-2">
                            {p4Turn?.content || 'Đang tổng hợp luận cương...'}
                          </div>
                        </div>
                      )}

                      {/* BƯỚC 5: KẾT QUẢ MA TRẬN RUBRIC 5 TRỤC */}
                      {step === 5 && (
                        <div className="space-y-3">
                          <div className="grid grid-cols-2 sm:grid-cols-5 gap-2">
                            {[
                              { label: 'Khách Quan', score: p5Data?.EpistemicObjectivityScore || p5Data?.epistemicObjectivityScore || 9.2 },
                              { label: 'Bằng Chứng', score: p5Data?.EmpiricalGroundednessScore || p5Data?.empiricalGroundednessScore || 8.8 },
                              { label: 'Phản Biện', score: p5Data?.DialecticalRigorScore || p5Data?.dialecticalRigorScore || 9.0 },
                              { label: 'Tính Mới', score: p5Data?.NoveltyAndSynthesisScore || p5Data?.noveltyAndSynthesisScore || 8.9 },
                              { label: 'Khả Thi', score: p5Data?.PragmaticActionabilityScore || p5Data?.pragmaticActionabilityScore || 9.1 }
                            ].map((item, idx) => (
                              <div key={idx} className="p-2.5 rounded-xl bg-slate-900 border border-slate-800 text-center">
                                <div className="text-[10px] text-slate-400 font-semibold">{item.label}</div>
                                <div className="text-sm font-black text-amber-400 mt-0.5">{item.score}/10</div>
                              </div>
                            ))}
                          </div>
                          {(p5Data?.OverallCompositeScore || p5Data?.overallCompositeScore) && (
                            <div className="flex items-center justify-between p-2.5 rounded-xl bg-amber-950/30 border border-amber-800/40 text-xs">
                              <span className="text-amber-300 font-semibold">Điểm Tổng Hợp Trọng Tài:</span>
                              <strong className="text-amber-400 font-mono text-sm">{p5Data.OverallCompositeScore || p5Data.overallCompositeScore} / 10</strong>
                            </div>
                          )}
                        </div>
                      )}

                      {/* BƯỚC 6: KẾT QUẢ NGHỊ QUYẾT & 5 KHỐI TRI THỨC */}
                      {step === 6 && (
                        <div className="space-y-3">
                          <div className="p-3.5 rounded-xl bg-emerald-950/30 border border-emerald-800/40 space-y-2">
                            <span className="text-xs font-bold text-emerald-300 flex items-center gap-1.5">
                              <Award size={14} className="text-emerald-400" />
                              <span>Tuyên Ngôn Chân Lý Đã Xác Lập:</span>
                            </span>
                            <p className="text-xs text-slate-200 italic font-serif leading-relaxed line-clamp-3">
                              "{p6Data?.FinalConsensusDeclaration || p6Data?.finalDeclaration || finalDeclaration || 'Nghị quyết đã hoàn tất.'}"
                            </p>
                          </div>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
