import React from 'react';
import { Award, CheckCircle2, FileCheck2, BookOpen, RefreshCw, AlertTriangle, ShieldAlert, ShieldCheck, Scale, Compass } from 'lucide-react';
import { SymposiumProceedingsResult } from '@/services/ai-swarm.service';

interface SymposiumResolutionCardProps {
  proceedings: SymposiumProceedingsResult;
  isExporting: boolean;
  exportedNotebookId: string | null;
  exportSuccessMessage: string | null;
  onExportToNotebook: () => void;
  onNavigateToNotebooks: () => void;
  onOpenNextRoundModal?: () => void;
  onConcludeSymposium?: () => void;
}

export function SymposiumResolutionCard({
  proceedings,
  isExporting,
  exportedNotebookId,
  exportSuccessMessage,
  onExportToNotebook,
  onNavigateToNotebooks,
  onOpenNextRoundModal,
  onConcludeSymposium
}: SymposiumResolutionCardProps) {
  const { evaluatedRubric } = proceedings;
  const isRubricEvaluated = Boolean(evaluatedRubric && evaluatedRubric.isEvaluated);

  const rubricDimensions = [
    { label: '1. Chặt Chẽ Logic (Rigor)', score: evaluatedRubric?.logicalRigor ?? 0, weight: '25%' },
    { label: '2. Phản Nghiệm Thực Địa [E#] (Falsifiability)', score: evaluatedRubric?.empiricalFalsifiability ?? 0, weight: '25%' },
    { label: '3. Độ Sâu Bản Thể Luận (Depth)', score: evaluatedRubric?.ontologicalDepth ?? 0, weight: '20%' },
    { label: '4. Giá Trị Thực Tiễn & Căn Chỉnh', score: evaluatedRubric?.pragmaticActionability ?? 0, weight: '15%' },
    { label: '5. Đề Kháng Bẫy Ngụy Biện (Fallacies)', score: evaluatedRubric?.trapResistance ?? 0, weight: '15%' }
  ];

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
      {/* BANNER MINH BẠCH DỮ LIỆU KHI LÀ PHIÊN MÔ PHỎNG */}
      {proceedings.isSimulated && (
        <div className="lg:col-span-3 p-4 rounded-2xl bg-amber-950/40 border border-amber-500/40 text-amber-200 text-xs flex items-start gap-3">
          <AlertTriangle className="text-amber-400 flex-shrink-0 mt-0.5" size={18} />
          <div>
            <strong className="font-bold text-amber-300">CHẾ ĐỘ MÔ PHỎNG (DATA INTEGRITY GUARANTEE):</strong>
            <p className="mt-0.5 text-amber-200/90 leading-relaxed">
              {proceedings.simulationReason || 'Các lượt phát biểu được sinh từ mẫu do chưa có API key khả dụng cho các mô hình của Hội đồng. Hệ thống không công bố điểm Rubric hay Nghị quyết đồng thuận ảo.'}
            </p>
          </div>
        </div>
      )}

      {/* CỘT 1: BẢNG ĐIỂM RUBRICS & TRỌNG TÀI ĐỘC LẬP */}
      <div className="lg:col-span-1 rounded-3xl p-5 sm:p-6 bg-slate-900/90 border border-cyan-500/30 shadow-2xl space-y-4">
        <div className="flex items-center justify-between border-b border-slate-800/80 pb-3">
          <div className="flex items-center gap-2">
            <Scale className="text-amber-400" size={18} />
            <h3 className="text-sm font-bold text-white">Thẩm Định Trọng Tài Độc Lập</h3>
          </div>
          {isRubricEvaluated ? (
            <span className="px-2.5 py-0.5 rounded-full text-xs font-mono font-black bg-cyan-500/20 text-cyan-300 border border-cyan-500/40">
              {evaluatedRubric.overallCompositeScore} / 10
            </span>
          ) : (
            <span className="px-2.5 py-0.5 rounded-full text-[10px] font-mono font-bold bg-slate-800 text-slate-400 border border-slate-700">
              Chưa chấm
            </span>
          )}
        </div>

        {/* THÔNG TIN TRỌNG TÀI */}
        {evaluatedRubric?.evaluatedByModel && (
          <div className="text-[11px] p-2.5 rounded-xl bg-slate-950/80 border border-slate-800 text-slate-300 flex items-center justify-between">
            <span className="text-slate-400">Trọng tài độc lập:</span>
            <span className="font-mono font-bold text-cyan-300 truncate max-w-[180px]">
              {evaluatedRubric.evaluatedByModel}
            </span>
          </div>
        )}

        {isRubricEvaluated ? (
          <div className="space-y-3">
            {rubricDimensions.map((item, idx) => (
              <div key={idx} className="space-y-1">
                <div className="flex justify-between text-xs font-medium">
                  <span className="text-slate-300 text-[11px]">{item.label}</span>
                  <span className="text-cyan-300 font-mono font-bold text-xs">
                    {item.score} <span className="text-slate-500 text-[10px]">({item.weight})</span>
                  </span>
                </div>
                <div className="w-full h-1.5 rounded-full bg-slate-800 overflow-hidden">
                  <div
                    className="h-full bg-gradient-to-r from-cyan-500 to-blue-500 rounded-full transition-all duration-1000"
                    style={{ width: `${(item.score / 10) * 100}%` }}
                  />
                </div>
              </div>
            ))}

            {evaluatedRubric.evaluationNote && (
              <div className="p-3 rounded-xl bg-slate-950/90 border border-slate-800 text-[11px] text-slate-300 leading-relaxed italic">
                "{evaluatedRubric.evaluationNote}"
              </div>
            )}
          </div>
        ) : (
          <div className="p-4 rounded-2xl bg-slate-950/80 border border-slate-800/80 text-xs text-slate-400 space-y-2">
            <div className="flex items-center gap-2 text-slate-300 font-bold">
              <ShieldAlert size={15} className="text-amber-400" />
              <span>Chưa có Trọng tài độc lập</span>
            </div>
            <p className="text-[11px] leading-relaxed text-slate-400">
              {evaluatedRubric?.evaluationNote || 'Điểm số Rubric chỉ được công bố khi có Trọng tài độc lập chấm mù và có API Key khả dụng cho mô hình thẩm định.'}
            </p>
          </div>
        )}

        {/* CHỈ SỐ GROUNDING & TIẾN HÓA */}
        <div className="p-3 rounded-2xl bg-slate-950/80 border border-slate-800 text-xs space-y-1.5">
          <div className="text-slate-400 flex items-center justify-between">
            <span>Độ Phủ Bằng Chứng (Grounding):</span>
            <span className="text-emerald-400 font-bold font-mono">
              {proceedings.groundingRate !== undefined ? `${proceedings.groundingRate}%` : 'Đang tính'}
            </span>
          </div>

          {proceedings.groundedEvidenceIds && proceedings.groundedEvidenceIds.length > 0 && (
            <div className="flex flex-wrap items-center gap-1 pt-1">
              <span className="text-[10px] text-slate-500">Chứng cứ trích dẫn:</span>
              {proceedings.groundedEvidenceIds.map((id) => (
                <span key={id} className="text-[10px] font-mono px-1.5 py-0.2 rounded bg-cyan-950/80 border border-cyan-500/30 text-cyan-300">
                  [{id}]
                </span>
              ))}
            </div>
          )}

          <div className="text-slate-400 flex items-center justify-between pt-1 border-t border-slate-800/60">
            <span>Chỉ Số Tiến Hóa Nhận Thức (EEV):</span>
            {proceedings.epistemicEvolutionIndex > 0 ? (
              <span className="text-emerald-400 font-bold font-mono">
                {(proceedings.epistemicEvolutionIndex * 100).toFixed(1)}%
              </span>
            ) : (
              <span className="text-slate-500 font-mono text-[11px]">Chưa xác định</span>
            )}
          </div>
          <div className="text-slate-400 flex items-center justify-between">
            <span>Thời Gian Xử Lý Thực Tế:</span>
            <span className="text-cyan-300 font-mono font-bold">{proceedings.totalSessionDurationMs} ms</span>
          </div>
        </div>
      </div>

      {/* CỘT 2 & 3: NGHỊ QUYẾT & XUẤT NOTEBOOK HUB */}
      <div className="lg:col-span-2 rounded-3xl p-5 sm:p-7 bg-gradient-to-br from-slate-900/90 to-cyan-950/20 border border-slate-800 shadow-2xl flex flex-col justify-between space-y-5">
        <div className="space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="flex items-center gap-2 text-cyan-400 text-xs font-black uppercase tracking-widest">
              <CheckCircle2 size={16} />
              <span>{proceedings.isFinalRound ? '🏆 Tuyên Ngôn Nghị Quyết Chung Cuộc' : proceedings.isSimulated ? 'Biên Bản Thu Thập Luận Điểm (Mô Phỏng)' : 'Nghị Quyết Đồng Thuận & Khung Nhận Thức Hợp Nhất'}</span>
            </div>
            
            <div className="flex items-center gap-2">
              <span className={`text-[10px] font-mono font-bold px-2.5 py-1 rounded-full border ${
                proceedings.isFinalRound
                  ? 'bg-amber-500/20 text-amber-300 border-amber-500/40 shadow-sm shadow-amber-500/20'
                  : 'bg-indigo-500/20 text-indigo-300 border-indigo-500/40'
              }`}>
                {proceedings.isFinalRound ? `🏆 Vòng Chung Cuộc (Đã hoàn tất)` : `⚡ Vòng ${proceedings.roundNumber || 1}`}
              </span>

              {proceedings.structuredOutcome && (
                <span className="text-[10px] font-bold px-2.5 py-1 rounded-full bg-cyan-950/80 border border-cyan-500/40 text-cyan-300">
                  {proceedings.teleologyMode === 1 ? '📊 Thực Nghiệm & Giải Pháp' :
                   proceedings.teleologyMode === 2 ? '💡 Khởi Sinh Tiên Đề & Nan Đề' :
                   '🛡️ Toàn Năng Lai Ghép'}
                </span>
              )}
            </div>
          </div>

          {/* CHỈ ĐẠO CỦA CHỦ TỌA PHIÊN (NẾU CÓ) */}
          {proceedings.socraticDirectives && (
            <div className="p-3.5 rounded-2xl bg-indigo-950/50 border border-indigo-500/30 text-xs text-indigo-200 space-y-1">
              <div className="flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-wider text-indigo-300">
                <Compass size={14} className="text-indigo-400" />
                <span>Ý Kiến Chỉ Đạo Của Chủ Tọa Cho Vòng Này:</span>
              </div>
              <p className="italic text-slate-200 leading-relaxed font-sans">
                "{proceedings.socraticDirectives}"
              </p>
            </div>
          )}

          {/* BANNER THÔNG BÁO TỰ ĐỘNG GHI VẾT VÀO NOTEBOOK HUB */}
          {proceedings.notebookArchiveMessage && (
            <div className="p-3 rounded-2xl bg-emerald-950/40 border border-emerald-500/30 flex items-center justify-between text-xs text-emerald-300">
              <div className="flex items-center gap-2">
                <FileCheck2 size={16} className="text-emerald-400 flex-shrink-0" />
                <span>{proceedings.notebookArchiveMessage}</span>
              </div>
              {onNavigateToNotebooks && (
                <button
                  onClick={onNavigateToNotebooks}
                  className="px-2.5 py-1 rounded-lg bg-emerald-900/60 hover:bg-emerald-800 text-emerald-200 text-[11px] font-bold flex items-center gap-1 transition cursor-pointer"
                >
                  <BookOpen size={12} />
                  <span>Mở Sổ</span>
                </button>
              )}
            </div>
          )}

          {/* KHỐI CẤU TRÚC 5 TRỤ CỘT TRI THỨC (NẾU CÓ) */}
          {proceedings.structuredOutcome && (
            <div className="space-y-4 pt-1">
              {/* 1. HẠT NHÂN TIÊN ĐỀ MỚI ĐÓNG GÓP CHO NHÂN LOẠI (NẾU CÓ) */}
              {proceedings.structuredOutcome.novelEpistemicAxiom && (
                <div className="p-4 rounded-2xl bg-gradient-to-r from-violet-950/60 via-slate-900/90 to-cyan-950/60 border border-violet-500/40 shadow-lg">
                  <div className="flex items-center gap-2 text-xs font-black text-violet-300 uppercase tracking-wider mb-1.5">
                    <Compass size={15} className="text-violet-400" />
                    <span>Hạt Nhân Tiên Đề Tri Thức Mới Cho Nhân Loại</span>
                  </div>
                  <p className="text-xs sm:text-sm text-slate-100 font-medium leading-relaxed italic">
                    "{proceedings.structuredOutcome.novelEpistemicAxiom}"
                  </p>
                </div>
              )}

              {/* 2. GRID 2 CỘT: THỰC TRẠNG & CĂN NGUYÊN VS TIẾN BỘ & NAN ĐỀ */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5">
                {/* Cột Trái: Thực trạng & Căn nguyên */}
                <div className="p-4 rounded-2xl bg-slate-950/90 border border-slate-800 space-y-3">
                  <div>
                    <h4 className="text-[11px] font-black uppercase tracking-wider text-amber-400 flex items-center gap-1.5">
                      <span>📉 Thực Trạng Khách Quan</span>
                    </h4>
                    <div className="mt-1.5 space-y-1 text-xs text-slate-300 leading-relaxed">
                      {proceedings.structuredOutcome.empiricalStatusQuo.map((st, i) => (
                        <p key={i} className="text-slate-300">{st}</p>
                      ))}
                    </div>
                  </div>

                  {proceedings.structuredOutcome.rootCauses.length > 0 && (
                    <div className="pt-2 border-t border-slate-800/80">
                      <h4 className="text-[11px] font-black uppercase tracking-wider text-rose-400">
                        Căn Nguyên Cốt Lõi:
                      </h4>
                      <ul className="mt-1 space-y-1 text-xs text-slate-400">
                        {proceedings.structuredOutcome.rootCauses.map((rc, i) => (
                          <li key={i} className="flex items-start gap-1.5">
                            <span className="text-rose-500 font-bold">•</span>
                            <span>{rc}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>

                {/* Cột Phải: Điểm tiến bộ & Nan đề cốt lõi */}
                <div className="p-4 rounded-2xl bg-slate-950/90 border border-slate-800 space-y-3">
                  {proceedings.structuredOutcome.validatedProgress.length > 0 && (
                    <div>
                      <h4 className="text-[11px] font-black uppercase tracking-wider text-emerald-400 flex items-center gap-1.5">
                        <span>✨ Điểm Tiến Bộ Đã Ghi Nhận</span>
                      </h4>
                      <ul className="mt-1.5 space-y-1 text-xs text-slate-300">
                        {proceedings.structuredOutcome.validatedProgress.map((vp, i) => (
                          <li key={i} className="flex items-start gap-1.5">
                            <span className="text-emerald-400 font-bold">✓</span>
                            <span>{vp}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}

                  {proceedings.structuredOutcome.coreAntinomy && (
                    <div className="pt-2 border-t border-slate-800/80">
                      <h4 className="text-[11px] font-black uppercase tracking-wider text-cyan-400">
                        Mâu Thuẫn / Nan Đề Trung Tâm:
                      </h4>
                      <p className="mt-1 text-xs text-slate-300 leading-relaxed italic">
                        {proceedings.structuredOutcome.coreAntinomy}
                      </p>
                    </div>
                  )}

                  {proceedings.structuredOutcome.openConjectures.length > 0 && (
                    <div className="pt-2 border-t border-slate-800/80">
                      <h4 className="text-[10px] font-bold uppercase tracking-wider text-slate-400">
                        Nan đề mở cho thế giới:
                      </h4>
                      <ul className="mt-1 space-y-0.5 text-[11px] text-slate-400">
                        {proceedings.structuredOutcome.openConjectures.map((oc, i) => (
                          <li key={i}>? {oc}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              </div>

              {/* 3. GÓI GIẢI PHÁP HÀNH ĐỘNG PHÂN KỲ LỘ TRÌNH (ACTIONABLE SOLUTIONS) */}
              {proceedings.structuredOutcome.actionableSolutions.length > 0 && (
                <div className="space-y-2.5 pt-1">
                  <div className="flex items-center justify-between">
                    <h4 className="text-xs font-black uppercase tracking-widest text-white flex items-center gap-2">
                      <Award size={14} className="text-cyan-400" />
                      <span>Gói Giải Pháp Hành Động Phân Kỳ Lộ Trình</span>
                    </h4>
                    <span className="text-[10px] text-slate-400 font-mono">
                      {proceedings.structuredOutcome.actionableSolutions.length} Giải pháp chiến lược
                    </span>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                    {proceedings.structuredOutcome.actionableSolutions.map((sol, idx) => (
                      <div
                        key={idx}
                        className="p-3.5 rounded-2xl bg-slate-950/90 border border-slate-800 hover:border-cyan-500/40 transition flex flex-col justify-between space-y-2.5"
                      >
                        <div className="space-y-1.5">
                          <div className="flex items-center justify-between gap-1">
                            <span className={`text-[10px] font-bold px-2 py-0.5 rounded-md ${
                              sol.horizon.includes('Ngắn') ? 'bg-amber-950/80 text-amber-300 border border-amber-800/40' :
                              sol.horizon.includes('Trung') ? 'bg-blue-950/80 text-blue-300 border border-blue-800/40' :
                              'bg-purple-950/80 text-purple-300 border border-purple-800/40'
                            }`}>
                              {sol.horizon}
                            </span>
                            <span className="text-[10px] font-mono font-bold text-emerald-400">
                              Khả thi: {sol.feasibilityScore}/10
                            </span>
                          </div>

                          <h5 className="text-xs font-bold text-slate-100 leading-snug">
                            {sol.solutionTitle}
                          </h5>

                          <p className="text-[11px] text-slate-400 leading-relaxed">
                            {sol.description}
                          </p>
                        </div>

                        <div className="pt-2 border-t border-slate-800/80 flex items-center justify-between text-[10px]">
                          <span className="text-slate-500 truncate max-w-[140px]" title={sol.responsibleDomain}>
                            🏛️ {sol.responsibleDomain}
                          </span>
                          {sol.groundedEvidenceIds && sol.groundedEvidenceIds.length > 0 && (
                            <div className="flex items-center gap-1">
                              {sol.groundedEvidenceIds.map(eid => (
                                <span key={eid} className="font-mono text-cyan-400 bg-cyan-950/80 px-1 py-0.2 rounded border border-cyan-800/40">
                                  [{eid}]
                                </span>
                              ))}
                            </div>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}

          {/* NGHỊ QUYẾT BẢN ĐẦY ĐỦ */}
          <div className="p-4 sm:p-5 rounded-2xl bg-slate-950/80 border border-cyan-500/20 text-xs sm:text-sm text-slate-200 leading-relaxed font-sans whitespace-pre-line max-h-96 overflow-y-auto scrollbar-thin scrollbar-thumb-slate-800">
            {proceedings.finalConsensusDeclaration}
          </div>

          {/* BẢN TỔNG HỢP SIÊU LÝ THUYẾT */}
          <div className="p-4 rounded-2xl bg-slate-950/60 border border-slate-800 text-xs text-slate-300 leading-relaxed font-mono whitespace-pre-line max-h-48 overflow-y-auto scrollbar-thin scrollbar-thumb-slate-800">
            {proceedings.unifiedMetatheory}
          </div>
        </div>

        {/* HÀNH ĐỘNG MULTI-ROUND & XUẤT NOTEBOOK HUB */}
        <div className="pt-4 border-t border-slate-800/80 flex flex-col gap-3">
          <div className="flex flex-col sm:flex-row items-center justify-between gap-3">
            {exportSuccessMessage ? (
              <div className="flex items-center gap-2.5 text-xs text-emerald-400 bg-emerald-950/40 px-3.5 py-2 rounded-xl border border-emerald-500/30">
                <FileCheck2 size={16} />
                <span>{exportSuccessMessage}</span>
              </div>
            ) : (
              <div className="text-xs text-slate-400">
                {proceedings.isFinalRound ? (
                  <span>🏆 Kỷ yếu đã được đúc kết thành <strong className="text-amber-300">Nghị Quyết Tối Hậu</strong> lưu vào Notebook Hub.</span>
                ) : (
                  <span>Phiên làm việc Vòng {proceedings.roundNumber || 1} đã hoàn thành. Chủ tọa có thể chỉ đạo để mở vòng tiếp theo hoặc chốt hạ.</span>
                )}
              </div>
            )}

            <div className="flex flex-wrap items-center gap-2.5 w-full sm:w-auto justify-end">
              {(exportedNotebookId || proceedings.targetNotebookId) && (
                <button
                  onClick={onNavigateToNotebooks}
                  className="px-3.5 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 font-bold text-xs flex items-center gap-1.5 border border-slate-700 transition cursor-pointer"
                >
                  <BookOpen size={13} className="text-cyan-400" />
                  <span>Vào Notebook Hub</span>
                </button>
              )}

              {/* Nút Xuất thủ công nếu chưa có */}
              {(!exportedNotebookId && !proceedings.targetNotebookId) && (
                <button
                  onClick={onExportToNotebook}
                  disabled={isExporting}
                  className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 font-bold text-xs flex items-center gap-1.5 border border-slate-700 transition cursor-pointer"
                >
                  <BookOpen size={13} />
                  <span>Xuất Vào Notebook</span>
                </button>
              )}
            </div>
          </div>

          {/* THANH ĐIỀU PHỐI ĐA VÒNG CỦA CHỦ TỌA */}
          {!proceedings.isFinalRound && (
            <div className="p-3.5 rounded-2xl bg-gradient-to-r from-indigo-950/80 via-slate-900 to-cyan-950/80 border border-indigo-500/40 flex flex-col sm:flex-row items-center justify-between gap-3 shadow-lg">
              <div className="flex items-center gap-2 text-xs text-indigo-200">
                <Compass size={16} className="text-cyan-400 flex-shrink-0" />
                <span>
                  Chủ tọa phiên họp: Tiếp tục đào sâu nan đề & bồi đắp tư liệu cho <strong>Vòng {(proceedings.roundNumber || 1) + 1}</strong>
                </span>
              </div>

              <div className="flex items-center gap-2.5 w-full sm:w-auto justify-end">
                {onConcludeSymposium && (
                  <button
                    onClick={onConcludeSymposium}
                    className="px-4 py-2 rounded-xl bg-amber-500/20 hover:bg-amber-500/30 text-amber-300 font-bold text-xs flex items-center gap-1.5 border border-amber-500/40 transition cursor-pointer"
                    title="Kết thúc hội thảo ngay và tuyên bố bản nghị quyết hiện tại là Chung cuộc"
                  >
                    <Award size={13} />
                    <span>Tuyên Bố Chung Cuộc</span>
                  </button>
                )}

                {onOpenNextRoundModal && (
                  <button
                    onClick={onOpenNextRoundModal}
                    className="px-5 py-2.5 rounded-xl bg-gradient-to-r from-cyan-500 to-indigo-500 hover:from-cyan-400 hover:to-indigo-400 text-slate-950 font-black text-xs flex items-center gap-2 shadow-lg shadow-cyan-500/25 transition cursor-pointer"
                  >
                    <span>Chỉ Đạo & Tiếp Tục Vòng {(proceedings.roundNumber || 1) + 1}</span>
                    <Award size={14} className="hidden sm:inline" />
                  </button>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
