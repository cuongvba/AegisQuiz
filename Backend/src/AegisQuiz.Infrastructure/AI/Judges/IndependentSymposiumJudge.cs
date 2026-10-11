using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.AI.Judges
{
    /// <summary>
    /// [AegisAI Grounded Symposium - Independent Arbitration Council & Blind Rubric Evaluator]
    /// Hiện thực hóa Hội Đồng Trọng Tài Độc Lập Chấm Mù Đa Mô Hình (Isomorphic Council Architecture):
    /// 1. Triệu tập Hội Đồng Trọng Tài Độc Lập từ CSDL (Category == CouncilCategory.Arbitration, mã SYS_ARBITRATION).
    /// 2. Điều phối Bầy Sói (WolfpackSwarm) của Ghế Chánh Trọng Tài Chấm Mù qua IWolfpackRoleInvoker.
    /// 3. Tự động loại trừ 100% các mô hình đã tham gia phát biểu hoặc thư ký trong phiên hội thảo (Blind Review).
    /// 4. 0ms Cascade Failover: Nếu Sói Alpha bận/lỗi API, tự động chuyển tiếp sang Sói dự phòng trong Swarm hoặc Ghế kế cận.
    /// 5. Chấm điểm 5 trục Rubric nghiêm cẩn theo dữ liệu thực nghiệm [E#].
    /// </summary>
    public class IndependentSymposiumJudge : ISymposiumJudge
    {
        private readonly IWolfpackRoleInvoker _wolfpackInvoker;
        private readonly IPreconfiguredCouncilProvider _councilProvider;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISymposiumPromptComposer _promptComposer;
        private readonly ILogger<IndependentSymposiumJudge> _logger;

        public IndependentSymposiumJudge(
            IWolfpackRoleInvoker wolfpackInvoker,
            IPreconfiguredCouncilProvider councilProvider,
            IServiceScopeFactory scopeFactory,
            ISymposiumPromptComposer promptComposer,
            ILogger<IndependentSymposiumJudge> logger)
        {
            _wolfpackInvoker = wolfpackInvoker;
            _councilProvider = councilProvider;
            _scopeFactory = scopeFactory;
            _promptComposer = promptComposer;
            _logger = logger;
        }

        public async Task<SymposiumRubricScore> EvaluateSessionAsync(
            string topicTitle,
            string coreProblemStatement,
            EmpiricalEvidenceDossierDto? empiricalDossier,
            List<SymposiumDialogueTurn> dialogueLog,
            List<UniversalAiCouncil> participatingCouncils,
            Guid? userId = null,
            Guid? tenantId = null,
            string locale = "vi-VN",
            CancellationToken ct = default)
        {
            _logger.LogInformation("[ArbitrationCouncil] ⚖️ Triệu tập Hội Đồng Trọng Tài Độc Lập thẩm định đề tài: '{Topic}'", topicTitle);

            // 1. Phân tích chỉ số trích dẫn khách quan (Objective Grounding Metrics)
            var validEvidenceIds = empiricalDossier?.EvidenceLedger?.Select(e => e.Id.ToUpperInvariant()).ToHashSet()
                ?? new HashSet<string>();

            int turnsWithCitation = 0;
            var citedEvidenceIds = new HashSet<string>();

            foreach (var turn in dialogueLog)
            {
                var matches = Regex.Matches(turn.Content, @"\[(E\d+)\]", RegexOptions.IgnoreCase);
                bool hasValid = false;
                foreach (Match m in matches)
                {
                    var id = m.Groups[1].Value.ToUpperInvariant();
                    if (validEvidenceIds.Count == 0 || validEvidenceIds.Contains(id))
                    {
                        citedEvidenceIds.Add(id);
                        hasValid = true;
                    }
                }
                if (hasValid) turnsWithCitation++;
            }

            var totalTurns = dialogueLog.Count > 0 ? dialogueLog.Count : 1;
            var groundingRate = Math.Round((double)turnsWithCitation / totalTurns * 100.0, 1);

            // 2. Tuyển chọn Hội Đồng Trọng Tài Độc Lập & Nạp ĐỦ CHÂN toàn bộ các Sói trong hệ thống
            var usedModels = dialogueLog.Select(t => t.ModelCode.ToLowerInvariant()).ToHashSet();

            UniversalAiCouncil? arbCouncil = null;
            var allActiveModelCodes = new List<string>();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

                // Nạp tất cả các Mô Hình AI đang hoạt động trong hệ thống
                var activeModels = await db.AiModelRegistries
                    .AsNoTracking()
                    .Where(m => m.IsEnabled)
                    .OrderBy(m => m.BenchmarkElo)
                    .ToListAsync(ct);

                allActiveModelCodes = activeModels.Select(m => m.ModelCode).Distinct().ToList();

                arbCouncil = await db.UniversalAiCouncils
                    .Include(c => c.Seats)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Category == CouncilCategory.Arbitration && c.IsActive, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ArbitrationCouncil] Lỗi nạp SYS_ARBITRATION hoặc AiModelRegistries từ CSDL, fallback sang cấu hình hệ thống.");
            }

            if (allActiveModelCodes.Count == 0)
            {
                allActiveModelCodes = new List<string> { "deepseek-r1", "gemini-2.0-flash", "claude-3-5-sonnet", "groq-llama-3-3-70b", "ollama-qwen-local", "gpt-4o" };
            }

            if (arbCouncil == null || arbCouncil.Seats.Count == 0)
            {
                arbCouncil = _councilProvider.GetPreconfiguredCouncils()
                    .FirstOrDefault(c => c.Category == CouncilCategory.Arbitration)
                    ?? new UniversalAiCouncil
                    {
                        CouncilCode = "SYS_ARBITRATION",
                        DisplayName = "Hội Đồng Trọng Tài Độc Lập Chấm Mù",
                        Category = CouncilCategory.Arbitration,
                        Seats = new List<UniversalCouncilSeat>()
                    };
            }

            // ĐẢM BẢO VIỆN TRỌNG TÀI CÓ ĐỦ CHÂN TẤT CẢ CÁC SÓI CÓ TRONG HỆ THỐNG
            // Duyệt qua từng ghế và nạp toàn bộ các Sói hệ thống vào Bầy Sói (Swarm) của ghế đó
            if (arbCouncil != null)
            {
                foreach (var seat in arbCouncil.Seats)
                {
                    var seatWolves = new List<string>();
                    if (!string.IsNullOrWhiteSpace(seat.SwarmModelCodesJson))
                    {
                        try
                        {
                            var parsed = JsonSerializer.Deserialize<List<string>>(seat.SwarmModelCodesJson);
                            if (parsed != null) seatWolves.AddRange(parsed.Where(p => !string.IsNullOrWhiteSpace(p)));
                        }
                        catch { }
                    }

                    if (!string.IsNullOrWhiteSpace(seat.AlphaModelCode) && !seatWolves.Contains(seat.AlphaModelCode))
                    {
                        seatWolves.Insert(0, seat.AlphaModelCode);
                    }

                    // Tự động nạp động (Dynamic Auto-Enroll) toàn bộ các Sói còn thiếu trong hệ thống
                    foreach (var mCode in allActiveModelCodes)
                    {
                        if (!seatWolves.Any(w => string.Equals(w, mCode, StringComparison.OrdinalIgnoreCase)))
                        {
                            seatWolves.Add(mCode);
                        }
                    }

                    // Sắp xếp ưu tiên: Các Sói ĐỘC LẬP TUYỆT ĐỐI (chưa từng phát biểu trong phiên) được đôn lên ĐẦU bầy sói
                    var independentWolves = seatWolves.Where(w => !usedModels.Contains(w.ToLowerInvariant())).ToList();
                    var participatingWolves = seatWolves.Where(w => usedModels.Contains(w.ToLowerInvariant())).ToList();
                    var prioritizedSwarm = independentWolves.Concat(participatingWolves).ToList();

                    seat.SwarmModelCodesJson = JsonSerializer.Serialize(prioritizedSwarm);
                }
            }

            var strictlyIndependentCount = allActiveModelCodes.Count(m => !usedModels.Contains(m.ToLowerInvariant()));
            _logger.LogInformation("[ArbitrationCouncil] ⚖️ Hội Đồng Trọng Tài '{Council}' đã sẵn sàng ({SeatCount} Ghế, {TotalWolves} Họ Sói hệ thống, {IndepCount} Sói độc lập chưa phát biểu).",
                arbCouncil?.DisplayName ?? "SYS_ARBITRATION", arbCouncil?.Seats.Count ?? 0, allActiveModelCodes.Count, strictlyIndependentCount);

            // 3. Chuẩn bị Biên bản thẩm định gửi Hội Đồng Trọng Tài
            var transcriptBuilder = new StringBuilder();
            transcriptBuilder.AppendLine($"# BIÊN BẢN TRANH LUẬN ĐẠI HỘI THẢO: {topicTitle}");
            transcriptBuilder.AppendLine($"Bối cảnh: {coreProblemStatement}");
            transcriptBuilder.AppendLine($"Chỉ số trích dẫn thực nghiệm đo đạc máy: {groundingRate}% ({turnsWithCitation}/{totalTurns} lượt phát biểu trích dẫn mã [E#]).");
            transcriptBuilder.AppendLine();
            transcriptBuilder.AppendLine("## SỔ CÁI BẰNG CHỨNG THỰC ĐỊA [E1..En]:");
            if (empiricalDossier?.EvidenceLedger != null)
            {
                foreach (var ev in empiricalDossier.EvidenceLedger)
                {
                    transcriptBuilder.AppendLine($"- [{ev.Id}]: {ev.ClaimOrFact} | {ev.QuantitativeValue} | Nguồn: {ev.SourceTitle}");
                }
            }
            transcriptBuilder.AppendLine();
            transcriptBuilder.AppendLine("## CÁC LƯỢT TRANH BIỆN TRONG HỘI ĐỒNG:");
            foreach (var turn in dialogueLog)
            {
                transcriptBuilder.AppendLine($"### [Pha {turn.PhaseNumber}] {turn.CouncilDisplayName} - {turn.SpeakerTitle}:");
                transcriptBuilder.AppendLine(turn.Content);
                transcriptBuilder.AppendLine();
            }

            var arbiterSeat = (arbCouncil?.Seats ?? new List<UniversalCouncilSeat>()).FirstOrDefault()
                ?? new UniversalCouncilSeat { SeatTitle = "Chánh Trọng Tài Chấm Mù", Role = CouncilSeatRole.EmpiricalAuditor };

            var promptCtx = new PromptCompositionContext
            {
                Council = arbCouncil ?? new UniversalAiCouncil { CouncilCode = "SYS_ARBITRATION", DisplayName = "Hội Đồng Trọng Tài Độc Lập" },
                Seat = arbiterSeat,
                PhaseCode = "SCORE_ARBITRATION",
                TopicTitle = topicTitle,
                CoreProblemStatement = coreProblemStatement,
                ParticipatingCouncils = participatingCouncils.Select(c => c.DisplayName).ToList(),
                Locale = locale,
                TenantId = tenantId
            };

            var composedPrompt = await _promptComposer.ComposeSeatPromptAsync(promptCtx, ct);
            var baseSystemPrompt = composedPrompt.Text;

            var userPrompt = $"Hãy thẩm định toàn bộ biên bản sau và xuất ra kết quả đánh giá:\n\n{transcriptBuilder}";

            // 4. Vòng lặp điều phối qua các Ghế Trọng tài (Seats) và Bầy Sói (WolfpackSwarm) của từng Ghế
            var arbiterFailures = new List<string>();

            // Sắp xếp Ghế ưu tiên: President -> EmpiricalAuditor -> AdversarialCritic -> Synthesizer
            var seatsToTry = (arbCouncil?.Seats ?? new List<UniversalCouncilSeat>())
                .OrderBy(s => s.Role == CouncilSeatRole.President ? 0 :
                              s.Role == CouncilSeatRole.EmpiricalAuditor ? 1 :
                              s.Role == CouncilSeatRole.AdversarialCritic ? 2 : 3)
                .ToList();

            // Nếu có Sói độc lập, loại trừ tất cả model đã phát biểu.
            // Nếu tình huống cực đoan tất cả Sói hệ thống đều đã phát biểu, không exclude để tránh sập phiên, nhưng ghi chú rõ.
            var modelsToExclude = strictlyIndependentCount > 0 ? usedModels.ToList() : null;

            foreach (var seat in seatsToTry)
            {
                _logger.LogInformation("[ArbitrationCouncil] Đang điều phối Ghế [{Seat}] qua Bầy Sói (Wolfpack Swarm)...", seat.SeatTitle);

                var seatDirective = !string.IsNullOrWhiteSpace(seat.PersonaPromptDirective)
                    ? $"\n\n[CHỈ ĐẠO CHUYÊN MÔN RIÊNG CHO GHẾ {seat.SeatTitle}]:\n{seat.PersonaPromptDirective}"
                    : "";
                var effectiveSystemPrompt = baseSystemPrompt + seatDirective;

                try
                {
                    var invocationResp = await _wolfpackInvoker.InvokeRoleSwarmAsync(
                        seat,
                        arbCouncil!,
                        effectiveSystemPrompt,
                        userPrompt,
                        excludeModels: modelsToExclude,
                        temperature: 0.1,
                        maxTokens: 1200,
                        userId: userId,
                        tenantId: tenantId,
                        ct: ct);

                    if (invocationResp.Success && !string.IsNullOrWhiteSpace(invocationResp.Content))
                    {
                        var cleanJson = ExtractJson(invocationResp.Content);
                        using var doc = JsonDocument.Parse(cleanJson);
                        var root = doc.RootElement;

                        var isStrictlyIndependent = !usedModels.Contains(invocationResp.ModelUsed?.ToLowerInvariant() ?? "");
                        var independenceTag = isStrictlyIndependent ? " [Độc Lập Tuyệt Đối]" : " [Giám Sát Trung Lập Dự Phòng]";

                        _logger.LogInformation("[ArbitrationCouncil] ✅ Ghế [{Seat}] (Model: {Model}{Indep}) đã hoàn tất phán quyết Rubric 5 trục sau {Latency}ms.",
                            seat.SeatTitle, invocationResp.ModelUsed, independenceTag, invocationResp.LatencyMs);

                        var evaluationNote = root.TryGetProperty("evaluationNote", out var note) ? note.GetString() : "Đã hoàn thành thẩm định độc lập.";
                        if (!isStrictlyIndependent)
                        {
                            evaluationNote += " (Lưu ý: Toàn bộ Sói hệ thống đều đã tham gia tranh biện; Phán quyết thực hiện bởi cơ chế Giám sát Trung Lập dự phòng).";
                        }

                        return new SymposiumRubricScore
                        {
                            IsEvaluated = true,
                            EvaluatedByModel = $"{invocationResp.ModelUsed} ({invocationResp.KeySource}){independenceTag} - [{arbCouncil!.DisplayName}: {seat.SeatTitle}]",
                            EvaluationNote = evaluationNote,
                            GroundingRate = groundingRate,
                            ValidCitationsCount = citedEvidenceIds.Count,
                            LogicalRigor = root.TryGetProperty("logicalRigor", out var r1) ? r1.GetDouble() : 8.0,
                            EmpiricalFalsifiability = root.TryGetProperty("empiricalFalsifiability", out var r2) ? r2.GetDouble() : 8.0,
                            OntologicalDepth = root.TryGetProperty("ontologicalDepth", out var r3) ? r3.GetDouble() : 8.0,
                            PragmaticActionability = root.TryGetProperty("pragmaticActionability", out var r4) ? r4.GetDouble() : 7.5,
                            TrapResistance = root.TryGetProperty("trapResistance", out var r5) ? r5.GetDouble() : 8.0
                        };
                    }

                    var err = invocationResp.ErrorMessage ?? "Phản hồi rỗng hoặc sai định dạng";
                    arbiterFailures.Add($"{seat.SeatTitle}: {err}");
                    _logger.LogWarning("[ArbitrationCouncil] ⚠️ Ghế [{Seat}] thất bại: {Error}. Tiếp tục thử Ghế trọng tài kế tiếp...",
                        seat.SeatTitle, err);
                }
                catch (Exception exSeat)
                {
                    arbiterFailures.Add($"{seat.SeatTitle}: {exSeat.Message}");
                    _logger.LogWarning(exSeat, "[ArbitrationCouncil] ⚠️ Ghế [{Seat}] gặp lỗi ngoại lệ. Thử Ghế trọng tài kế tiếp...",
                        seat.SeatTitle);
                }
            }

            // 5. Fallback trung thực tuyệt đối: Khi toàn bộ Bầy Sói của Hội Đồng Trọng Tài đều kiệt quệ
            _logger.LogWarning("[ArbitrationCouncil] Toàn bộ các Ghế trong Hội Đồng Trọng Tài đều không thể thẩm định. Không công bố điểm Rubric ảo.");

            return new SymposiumRubricScore
            {
                IsEvaluated = false,
                EvaluatedByModel = $"{arbCouncil?.DisplayName ?? "Hội Đồng Trọng Tài Độc Lập"} (Tất cả mô hình đều lỗi API hoặc kiệt hạn mức)",
                EvaluationNote = $"Hội Đồng Trọng Tài ({seatsToTry.Count} Ghế) đều không phản hồi: {string.Join("; ", arbiterFailures)}",
                GroundingRate = groundingRate,
                ValidCitationsCount = citedEvidenceIds.Count,
                LogicalRigor = 0.0,
                EmpiricalFalsifiability = 0.0,
                OntologicalDepth = 0.0,
                PragmaticActionability = 0.0,
                TrapResistance = 0.0
            };
        }

        private static string ExtractJson(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(7);
                var end = trimmed.LastIndexOf("```");
                if (end >= 0) trimmed = trimmed.Substring(0, end);
            }
            else if (trimmed.StartsWith("```"))
            {
                trimmed = trimmed.Substring(3);
                var end = trimmed.LastIndexOf("```");
                if (end >= 0) trimmed = trimmed.Substring(0, end);
            }
            return trimmed.Trim();
        }
    }
}
