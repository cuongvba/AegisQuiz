using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Infrastructure.AI.Resolution
{
    /// <summary>
    /// [AegisAI Resolution Composer] Bộ soạn thảo Nghị quyết Hàn Lâm và Kỷ yếu Living Academic Document.
    /// Render văn bản nghị quyết thích ứng linh hoạt theo TeleologyMode và Markdown xuất sang Notebook Hub.
    /// </summary>
    public class SymposiumResolutionComposer : ISymposiumResolutionComposer
    {
        public string ComposeFinalDeclaration(
            Guid sessionId,
            string topicTitle,
            SymposiumTeleologyMode mode,
            List<string> citedEvidenceIds,
            SymposiumRubricScore rubric,
            SymposiumStructuredOutcomeDto structuredOutcome,
            List<UniversalAiCouncil> participating,
            bool hasRealTurns,
            int roundNumber = 1,
            bool isFinalRound = false,
            string? socraticDirectives = null)
        {
            var declBuilder = new StringBuilder();
            if (isFinalRound)
            {
                declBuilder.AppendLine($"🏆 TUYÊN NGÔN NGHỊ QUYẾT CHUNG CUỘC — ĐẠI HỘI THẢO HÀN LÂM AEGISQUIZ");
                declBuilder.AppendLine($"[XÁC LẬP CHÂN LÝ TIẾN HÓA TOÀN DIỆN — SAU {roundNumber} VÒNG BIỆN CHỨNG HỘI ĐỒNG]");
            }
            else
            {
                declBuilder.AppendLine($"⚡ BẢN KẾT LUẬN NGHỊ QUYẾT VÒNG {roundNumber} — ĐẠI HỘI THẢO HÀN LÂM AEGISQUIZ");
                declBuilder.AppendLine($"[PHIÊN BIỆN CHỨNG TIẾP DIỄN — SẴN SÀNG CHO VÒNG {roundNumber + 1}]");
            }
            declBuilder.AppendLine($"Đề tài: {topicTitle}");
            declBuilder.AppendLine($"Mã phiên triệu tập: #{sessionId.ToString()[..8].ToUpperInvariant()} | Vòng: {roundNumber}");
            declBuilder.AppendLine($"Mục tiêu biện chứng: {mode}");
            declBuilder.AppendLine($"Thời điểm thông qua: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            declBuilder.AppendLine();

            if (!string.IsNullOrWhiteSpace(socraticDirectives))
            {
                declBuilder.AppendLine("🏛️ Ý KIẾN CHỈ ĐẠO CỦA CHỦ TỌA PHIÊN TOÀN THỂ (SOCRATIC DIRECTIVE):");
                declBuilder.AppendLine($"\"{socraticDirectives.Trim()}\"");
                declBuilder.AppendLine();
            }

            declBuilder.AppendLine("I. CĂN CỨ THỰC CHỨNG & SỔ CÁI BẰNG CHỨNG:");
            declBuilder.AppendLine($"Phiên hội thảo đã trích dẫn và đối chiếu với {citedEvidenceIds.Count} bằng chứng thực nghiệm: {(citedEvidenceIds.Count > 0 ? string.Join(", ", citedEvidenceIds.Select(id => $"[{id}]")) : "Dữ liệu quan trắc chuyên ngành")}.");
            declBuilder.AppendLine($"Độ phủ trích dẫn thực nghiệm (Grounding Rate): {rubric.GroundingRate}%.");
            declBuilder.AppendLine();

            if (mode == SymposiumTeleologyMode.EmpiricalActionable || mode == SymposiumTeleologyMode.UniversalHybrid)
            {
                declBuilder.AppendLine("II. THỰC TRẠNG & CĂN NGUYÊN HỆ THỐNG:");
                if (structuredOutcome.EmpiricalStatusQuo.Count > 0)
                {
                    declBuilder.AppendLine("• Thực trạng hiện hữu:");
                    foreach (var sq in structuredOutcome.EmpiricalStatusQuo)
                        declBuilder.AppendLine($"  - {sq}");
                }
                if (structuredOutcome.RootCauses.Count > 0)
                {
                    declBuilder.AppendLine("• Căn nguyên cốt lõi:");
                    foreach (var rc in structuredOutcome.RootCauses)
                        declBuilder.AppendLine($"  - {rc}");
                }
                if (structuredOutcome.ValidatedProgress.Count > 0)
                {
                    declBuilder.AppendLine("• Điểm tiến bộ đã ghi nhận:");
                    foreach (var vp in structuredOutcome.ValidatedProgress)
                        declBuilder.AppendLine($"  - {vp}");
                }
                declBuilder.AppendLine();

                declBuilder.AppendLine("III. GÓI GIẢI PHÁP HÀNH ĐỘNG PHÂN KỲ LỘ TRÌNH:");
                foreach (var sol in structuredOutcome.ActionableSolutions)
                {
                    var evStr = sol.GroundedEvidenceIds.Count > 0 ? $" (Căn cứ: {string.Join(", ", sol.GroundedEvidenceIds.Select(e => $"[{e}]"))})" : "";
                    declBuilder.AppendLine($"• [{sol.Horizon}] {sol.SolutionTitle}{evStr}");
                    declBuilder.AppendLine($"  ↳ Nội dung: {sol.Description}");
                    declBuilder.AppendLine($"  ↳ Chủ thể: {sol.ResponsibleDomain} | Khả thi: {sol.FeasibilityScore}/10");
                }
                declBuilder.AppendLine();
            }

            if (mode == SymposiumTeleologyMode.EpistemicGenesis || mode == SymposiumTeleologyMode.UniversalHybrid)
            {
                var romanNum = mode == SymposiumTeleologyMode.UniversalHybrid ? "IV" : "II";
                declBuilder.AppendLine($"{romanNum}. HẠT NHÂN TIÊN ĐỀ MỚI & NAN ĐỀ MỞ CHO THẾ GIỚI:");
                declBuilder.AppendLine($"• Tiên đề mới đúc kết: {structuredOutcome.NovelEpistemicAxiom}");
                declBuilder.AppendLine($"• Mâu thuẫn/Nghịch lý trung tâm (Antinomy): {structuredOutcome.CoreAntinomy}");
                if (structuredOutcome.OpenConjectures.Count > 0)
                {
                    declBuilder.AppendLine("• Các nan đề mở bỏ ngỏ cho nhân loại:");
                    foreach (var conj in structuredOutcome.OpenConjectures)
                        declBuilder.AppendLine($"  - {conj}");
                }
                declBuilder.AppendLine();
            }

            var judgeRoman = mode == SymposiumTeleologyMode.UniversalHybrid ? "V" : "III";
            declBuilder.AppendLine($"{judgeRoman}. THẨM ĐỊNH TRỌNG TÀI ĐỘC LẬP:");
            declBuilder.AppendLine($"- Đơn vị thẩm định: {rubric.EvaluatedByModel}");
            declBuilder.AppendLine($"- Điểm tổng hợp Rubric 5 trục: {(rubric.IsEvaluated ? rubric.OverallCompositeScore.ToString("F2") + "/10" : "Chưa chấm")}");
            declBuilder.AppendLine($"- Nhận xét trọng tài: {rubric.EvaluationNote}");

            return declBuilder.ToString().TrimEnd();
        }

        public string ComposeNotebookMarkdown(
            SymposiumSession session, 
            SymposiumRubricScore? rubric,
            int roundNumber = 1,
            bool isFinalRound = false,
            string? socraticDirectives = null,
            string? additionalDocuments = null)
        {
            bool isSimulatedSession = session.EpistemicConsensusIndex <= 0 || 
                (session.FinalConsensusResolution != null && session.FinalConsensusResolution.Contains("CHẾ ĐỘ MÔ PHỎNG"));

            var mdBuilder = new StringBuilder();
            if (isFinalRound)
            {
                mdBuilder.AppendLine($"# 🏆 KỶ YẾU CHUNG CUỘC — ĐẠI HỘI THẢO BÁCH KHOA VẠN VẬT TRI THỨC");
                mdBuilder.AppendLine($"**Chủ đề:** {session.TopicTitle} *(Nghị Quyết Tối Hậu sau {roundNumber} Vòng Biện Chứng)*");
            }
            else
            {
                mdBuilder.AppendLine($"# 🌌 KỶ YẾU VÒNG {roundNumber} — ĐẠI HỘI THẢO BÁCH KHOA VẠN VẬT TRI THỨC");
                mdBuilder.AppendLine($"**Chủ đề:** {session.TopicTitle}");
            }
            mdBuilder.AppendLine($"**Mã phiên triệu tập:** `{session.Id}` | **Vòng số:** `{roundNumber}`");
            mdBuilder.AppendLine($"**Thời gian xác lập:** {session.ConcludedAt:yyyy-MM-dd HH:mm:ss} UTC");
            mdBuilder.AppendLine($"**Cấp thẩm quyền tri thức:** {(isSimulatedSession ? "Cấp 1 (ReferenceCase / Tài liệu mô phỏng tham khảo)" : "Cấp 4 (SupremeLegal / UniversalTruth)")}");
            mdBuilder.AppendLine();

            if (!string.IsNullOrWhiteSpace(socraticDirectives))
            {
                mdBuilder.AppendLine("---");
                mdBuilder.AppendLine("## 🏛️ CHỈ ĐẠO BIỆN CHỨNG CỦA CHỦ TỌA PHIÊN TOÀN THỂ (SOCRATIC DIRECTIVE)");
                mdBuilder.AppendLine(socraticDirectives.Trim());
                mdBuilder.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(additionalDocuments))
            {
                mdBuilder.AppendLine("---");
                mdBuilder.AppendLine("## 📜 VĂN KIỆN & TƯ LIỆU BỔ SUNG DO CHỦ TỌA NẠP VÀO");
                mdBuilder.AppendLine(additionalDocuments.Trim());
                mdBuilder.AppendLine();
            }

            mdBuilder.AppendLine("---");
            mdBuilder.AppendLine("## I. TIÊN ĐỀ & VẤN ĐỀ KHỞI THẢO");
            mdBuilder.AppendLine(session.CoreProblemStatement);
            mdBuilder.AppendLine();
            mdBuilder.AppendLine("---");
            mdBuilder.AppendLine("## II. MA TRẬN ĐÁNH GIÁ 5 TRỤC RUBRIC HÀN LÂM");

            if (rubric != null)
            {
                if (!rubric.IsEvaluated)
                {
                    mdBuilder.AppendLine($"*Ghi chú kiểm toán:* {rubric.EvaluationNote ?? "Chưa qua Trọng tài độc lập thẩm định — không công bố điểm số."}");
                }
                else
                {
                    mdBuilder.AppendLine("| Trục Tiêu Chí | Điểm Số / 10 | Trọng Số |");
                    mdBuilder.AppendLine("| :--- | :---: | :---: |");
                    mdBuilder.AppendLine($"| **1. Tính Chặt Chẽ Logic (Logical Rigor)** | {rubric.LogicalRigor:F1} | 25% |");
                    mdBuilder.AppendLine($"| **2. Khả Năng Phản Nghiệm Popper (Empirical Falsifiability)** | {rubric.EmpiricalFalsifiability:F1} | 20% |");
                    mdBuilder.AppendLine($"| **3. Độ Sâu Bản Thể Luận (Ontological Depth)** | {rubric.OntologicalDepth:F1} | 20% |");
                    mdBuilder.AppendLine($"| **4. Giá Trị Thực Tiễn & Căn Chỉnh Nhân Loại** | {rubric.PragmaticActionability:F1} | 20% |");
                    mdBuilder.AppendLine($"| **5. Khả Năng Đề Kháng Bẫy Ngụy Biện (Trap Resistance)** | {rubric.TrapResistance:F1} | 15% |");
                    mdBuilder.AppendLine($"| **TỔNG ĐIỂM CHỈ SỐ TIẾN HÓA TOÀN DIỆN** | **{rubric.OverallCompositeScore:F2}** | **100%** |");
                }
            }
            mdBuilder.AppendLine();
            mdBuilder.AppendLine("---");
            mdBuilder.AppendLine("## III. SIÊU KHUNG NHẬN THỨC HỢP NHẤT & NGHỊ QUYẾT TỐI HẬU");
            mdBuilder.AppendLine(session.FinalConsensusResolution);
            mdBuilder.AppendLine();
            mdBuilder.AppendLine("---");
            mdBuilder.AppendLine("## IV. DIỄN BIẾN TRANH BIỆN BIỆN CHỨNG CHI TIẾT");

            if (!string.IsNullOrEmpty(session.DialogueLogJson))
            {
                try
                {
                    var dialogue = JsonSerializer.Deserialize<List<SymposiumDialogueTurn>>(session.DialogueLogJson);
                    if (dialogue != null)
                    {
                        foreach (var turn in dialogue)
                        {
                            mdBuilder.AppendLine($"### [Pha {turn.PhaseNumber}] {turn.PhaseName} — {turn.SpeakerTitle}");
                            mdBuilder.AppendLine($"*Viện Hàn Lâm:* **{turn.CouncilDisplayName}** | *Hệ quy chiếu:* `{turn.SpatioTemporalPerspective ?? "Biện chứng toàn cầu"}` | *Mô hình:* `{turn.ModelCode}`");
                            if (turn.LatencyMs > 0)
                            {
                                mdBuilder.AppendLine($"*Thời gian đo thật:* `{turn.LatencyMs}ms`");
                            }
                            mdBuilder.AppendLine();
                            mdBuilder.AppendLine(turn.Content);
                            if (turn.KeyTheses != null && turn.KeyTheses.Count > 0)
                            {
                                mdBuilder.AppendLine();
                                mdBuilder.AppendLine("**Luận điểm trọng tâm:**");
                                foreach (var thesis in turn.KeyTheses)
                                {
                                    mdBuilder.AppendLine($"- {thesis}");
                                }
                            }
                            mdBuilder.AppendLine();
                        }
                    }
                }
                catch { /* continue */ }
            }

            return mdBuilder.ToString();
        }
    }
}
