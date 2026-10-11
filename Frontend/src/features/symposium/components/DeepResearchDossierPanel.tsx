import React, { useState } from 'react';
import { Compass, CheckCircle2, TrendingUp, BookOpen, AlertTriangle, ChevronDown, ChevronUp, ExternalLink, ShieldCheck } from 'lucide-react';
import { EmpiricalEvidenceDossierDto } from '@/services/ai-swarm.service';

interface DeepResearchDossierPanelProps {
  dossier: EmpiricalEvidenceDossierDto;
}

export function DeepResearchDossierPanel({ dossier }: DeepResearchDossierPanelProps) {
  const [isCollapsed, setIsCollapsed] = useState(false);

  return (
    <div className="rounded-3xl p-5 sm:p-6 bg-gradient-to-br from-slate-900/95 via-slate-900/80 to-blue-950/30 border border-blue-500/30 shadow-2xl space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between gap-3 border-b border-slate-800/80 pb-3.5">
        <div className="flex items-center gap-3">
          <div className="p-2 rounded-xl bg-blue-500/10 text-blue-400 border border-blue-500/20">
            <Compass size={20} className="animate-spin-slow" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="text-[10px] font-black uppercase tracking-wider text-blue-400">
                Epistemic Deep Research Scout
              </span>
              <span className="text-[10px] px-2 py-0.5 rounded-full bg-blue-950/80 border border-blue-500/40 text-blue-300 font-bold">
                {dossier.totalEvidenceUnits} Chứng Cứ Thực Địa
              </span>
              {dossier.isOnlineVerified ? (
                <span className="text-[10px] px-2 py-0.5 rounded-full bg-emerald-950/80 border border-emerald-500/40 text-emerald-300 font-bold flex items-center gap-1">
                  <ShieldCheck size={11} /> Đã kiểm chứng trực tuyến
                </span>
              ) : (
                <span className="text-[10px] px-2 py-0.5 rounded-full bg-slate-800/80 border border-slate-700 text-slate-300">
                  Thẩm định ngoại tuyến
                </span>
              )}
            </div>
            <h3 className="text-base font-black text-white">Sổ Cái Bằng Chứng Thực Nghiệm (Evidence Ledger)</h3>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <span className="hidden sm:inline text-xs text-slate-400 font-mono">
            Miền: <strong className="text-slate-200">{dossier.domainArea}</strong>
          </span>
          {dossier.provenanceNote && (
            <span className="text-[10px] px-2 py-0.5 rounded-full bg-slate-800 text-slate-300 border border-slate-700 hidden lg:inline">
              {dossier.provenanceNote}
            </span>
          )}
          <button
            onClick={() => setIsCollapsed(!isCollapsed)}
            className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition cursor-pointer"
            title={isCollapsed ? 'Mở rộng' : 'Thu gọn'}
          >
            {isCollapsed ? <ChevronDown size={16} /> : <ChevronUp size={16} />}
          </button>
        </div>
      </div>

      {!isCollapsed && (
        <div className="space-y-4 animate-in fade-in duration-200">
          {/* Bảng Sổ cái Bằng chứng cấu trúc [E1..En] */}
          {dossier.evidenceLedger && dossier.evidenceLedger.length > 0 && (
            <div className="rounded-2xl border border-blue-500/20 bg-slate-950/90 overflow-hidden">
              <div className="px-4 py-2.5 bg-blue-950/40 border-b border-blue-500/20 flex items-center justify-between text-xs font-bold text-blue-300">
                <span>📜 SỔ CÁI BẰNG CHỨNG HỘI ĐỒNG [E1..E{dossier.evidenceLedger.length}] (BẮT BUỘC TRÍCH DẪN)</span>
                <span className="text-[11px] text-slate-400 font-normal">Các ghế tranh biện bắt buộc phải viện dẫn mã [E#]</span>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead className="bg-slate-900/60 text-slate-400 border-b border-slate-800/80 font-mono text-[11px]">
                    <tr>
                      <th className="py-2.5 px-3 w-16">Mã</th>
                      <th className="py-2.5 px-3">Luận Điểm / Tiên Đề Thực Chứng</th>
                      <th className="py-2.5 px-3 w-48">Dữ Liệu Định Lượng</th>
                      <th className="py-2.5 px-3">Công Trình / Nguồn Học Thuật</th>
                      <th className="py-2.5 px-3 w-24 text-right">Độ Tin Cậy</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-800/50 text-slate-300">
                    {dossier.evidenceLedger.map((item) => (
                      <tr key={item.id} className="hover:bg-slate-800/30 transition">
                        <td className="py-2.5 px-3 font-mono font-black text-cyan-400">
                          <span className="px-1.5 py-0.5 rounded bg-cyan-950/80 border border-cyan-500/30">
                            [{item.id}]
                          </span>
                        </td>
                        <td className="py-2.5 px-3 leading-relaxed">
                          {item.claimOrFact}
                        </td>
                        <td className="py-2.5 px-3 text-emerald-400 font-mono text-[11px]">
                          {item.quantitativeValue ? (
                            <span className="flex items-center gap-1">
                              📊 {item.quantitativeValue}
                            </span>
                          ) : (
                            <span className="text-slate-500">Định tính</span>
                          )}
                        </td>
                        <td className="py-2.5 px-3 text-[11px] text-slate-400 font-mono">
                          <div>{item.sourceTitle}</div>
                          {item.doiOrUrl && (
                            <a
                              href={item.doiOrUrl.startsWith('http') ? item.doiOrUrl : `https://doi.org/${item.doiOrUrl}`}
                              target="_blank"
                              rel="noreferrer"
                              className="text-blue-400 hover:text-blue-300 hover:underline inline-flex items-center gap-1 mt-0.5 text-[10px]"
                            >
                              <span>{item.doiOrUrl}</span>
                              <ExternalLink size={10} />
                            </a>
                          )}
                        </td>
                        <td className="py-2.5 px-3 text-right font-mono font-bold text-blue-300">
                          {Math.round(item.confidenceWeight * 100)}%
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* 4 Nhóm Bằng Chứng Tổng Hợp */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5">
            {/* 1. Verified Facts */}
            <div className="p-4 rounded-2xl bg-slate-950/80 border border-slate-800/80 space-y-2">
              <div className="flex items-center gap-2 text-xs font-bold text-emerald-400">
                <CheckCircle2 size={14} />
                <span>Tiên Đề & Sự Kiện Đã Được Xác Thực ({dossier.verifiedFacts.length})</span>
              </div>
              <ul className="space-y-1.5 text-xs text-slate-300">
                {dossier.verifiedFacts.map((fact, idx) => (
                  <li key={idx} className="flex items-start gap-2 leading-relaxed">
                    <span className="text-emerald-500 mt-0.5">•</span>
                    <span>{fact}</span>
                  </li>
                ))}
              </ul>
            </div>

            {/* 2. Quantitative Data */}
            <div className="p-4 rounded-2xl bg-slate-950/80 border border-slate-800/80 space-y-2">
              <div className="flex items-center gap-2 text-xs font-bold text-cyan-400">
                <TrendingUp size={14} />
                <span>Chỉ Số Đo Đạc Định Lượng ({dossier.quantitativeDataPoints.length})</span>
              </div>
              <ul className="space-y-1.5 text-xs text-slate-300">
                {dossier.quantitativeDataPoints.map((item, idx) => (
                  <li key={idx} className="flex items-start gap-2 leading-relaxed">
                    <span className="text-cyan-500 mt-0.5">📊</span>
                    <span>{item}</span>
                  </li>
                ))}
              </ul>
            </div>

            {/* 3. Academic Citations */}
            <div className="p-4 rounded-2xl bg-slate-950/80 border border-slate-800/80 space-y-2">
              <div className="flex items-center gap-2 text-xs font-bold text-indigo-400">
                <BookOpen size={14} />
                <span>Nguồn Trích Dẫn Học Thuật Uy Tín ({dossier.academicCitations.length})</span>
              </div>
              <ul className="space-y-1.5 text-xs text-slate-300">
                {dossier.academicCitations.map((cite, idx) => (
                  <li key={idx} className="flex items-start gap-2 leading-relaxed font-mono text-[11px] text-slate-400">
                    <span className="text-indigo-400 mt-0.5">📖</span>
                    <span>{cite}</span>
                  </li>
                ))}
              </ul>
            </div>

            {/* 4. Controversies */}
            <div className="p-4 rounded-2xl bg-slate-950/80 border border-amber-500/20 space-y-2">
              <div className="flex items-center gap-2 text-xs font-bold text-amber-400">
                <AlertTriangle size={14} />
                <span>Mâu Thuẫn Thực Địa Chưa Đồng Thuận ({dossier.unresolvedControversies.length})</span>
              </div>
              <ul className="space-y-1.5 text-xs text-slate-300">
                {dossier.unresolvedControversies.map((cont, idx) => (
                  <li key={idx} className="flex items-start gap-2 leading-relaxed">
                    <span className="text-amber-500 mt-0.5">⚠️</span>
                    <span>{cont}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>

          <div className="p-2.5 rounded-xl bg-blue-950/30 border border-blue-500/20 text-[11px] text-blue-300 flex items-center justify-between">
            <span>💡 Toàn bộ Sổ cái Bằng chứng trên được nạp trực tiếp làm căn cứ tối cao cho các Viện Hàn Lâm trong Đại Hội Thảo.</span>
            <span className="font-bold text-emerald-400 flex items-center gap-1">✓ Evidence-Grounded</span>
          </div>
        </div>
      )}
    </div>
  );
}
