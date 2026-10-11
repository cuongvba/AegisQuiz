using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

namespace AegisQuiz.Infrastructure.AI
{
    /// <summary>
    /// [AegisAI Universal Epistemic Symposium Engine]
    /// Nhạc trưởng Đại Hội Thảo Bách Khoa Vạn Vật Tri Thức (Pure Orchestrator).
    /// Tuân thủ nghiêm ngặt CLEAR Architecture & OCP: Điều phối 6 pha biện chứng liên viện,
    /// ủy quyền hoàn toàn cho các phân hệ chuyên trách (Scout, Allocator, Invoker, Arbiter, Teleology, OutcomeBuilder, ResolutionComposer).
    /// </summary>
    public class UniversalSymposiumEngine : IUniversalSymposiumEngine
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ICouncilSelectionStrategy _councilSelector;
        private readonly IEpistemicDeepResearchScout _deepResearchScout;
        private readonly ISpatioTemporalRoleAllocator _roleAllocator;
        private readonly ISeatModelInvoker _seatInvoker;
        private readonly ISymposiumJudge _symposiumJudge;
        private readonly ISymposiumTeleologyResolver _teleologyResolver;
        private readonly ICouncilContributionGenerator _contributionGenerator;
        private readonly ISymposiumOutcomeBuilder _outcomeBuilder;
        private readonly ISymposiumResolutionComposer _resolutionComposer;
        private readonly ISymposiumSecretariat _secretariat;
        private readonly IPreconfiguredCouncilProvider _councilProvider;
        private readonly IWolfpackRoleInvoker _wolfpackInvoker;
        private readonly ISymposiumPromptComposer _promptComposer;
        private readonly ILogger<UniversalSymposiumEngine> _logger;

        public UniversalSymposiumEngine(
            IServiceScopeFactory scopeFactory,
            ICouncilSelectionStrategy councilSelector,
            IEpistemicDeepResearchScout deepResearchScout,
            ISpatioTemporalRoleAllocator roleAllocator,
            ISeatModelInvoker seatInvoker,
            ISymposiumJudge symposiumJudge,
            ISymposiumTeleologyResolver teleologyResolver,
            ICouncilContributionGenerator contributionGenerator,
            ISymposiumOutcomeBuilder outcomeBuilder,
            ISymposiumResolutionComposer resolutionComposer,
            ISymposiumSecretariat secretariat,
            IPreconfiguredCouncilProvider councilProvider,
            IWolfpackRoleInvoker wolfpackInvoker,
            ISymposiumPromptComposer promptComposer,
            ILogger<UniversalSymposiumEngine> logger)
        {
            _scopeFactory = scopeFactory;
            _councilSelector = councilSelector;
            _deepResearchScout = deepResearchScout;
            _roleAllocator = roleAllocator;
            _seatInvoker = seatInvoker;
            _symposiumJudge = symposiumJudge;
            _teleologyResolver = teleologyResolver;
            _contributionGenerator = contributionGenerator;
            _outcomeBuilder = outcomeBuilder;
            _resolutionComposer = resolutionComposer;
            _secretariat = secretariat;
            _councilProvider = councilProvider;
            _wolfpackInvoker = wolfpackInvoker;
            _promptComposer = promptComposer;
            _logger = logger;
        }

        public async Task<List<UniversalAiCouncil>> GetActiveCouncilsAsync(CancellationToken ct = default)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();
                var dbCouncils = await db.UniversalAiCouncils.AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.BenchmarkPrestigeElo)
                    .ToListAsync(ct);

                if (dbCouncils.Any())
                {
                    var seats = await db.UniversalCouncilSeats.AsNoTracking().ToListAsync(ct);
                    foreach (var c in dbCouncils)
                    {
                        c.Seats = seats.Where(s => s.CouncilId == c.Id).OrderBy(s => s.SortOrder).ToList();
                    }

                    // [Resilience Auto-Heal]: Bảo đảm Tổ Trọng Tài Độc Lập SYS_ARBITRATION luôn có đủ 4 Ghế và đầy đủ chân các Sói
                    var arbCouncil = dbCouncils.FirstOrDefault(c => c.Category == CouncilCategory.Arbitration || c.CouncilCode == "SYS_ARBITRATION");
                    if (arbCouncil != null && (arbCouncil.Seats.Count < 4 || !arbCouncil.Seats.Any(s => s.Role == CouncilSeatRole.EmpiricalAuditor)))
                    {
                        try
                        {
                            var preconfiguredArb = _councilProvider.GetPreconfiguredCouncils()
                                .FirstOrDefault(c => c.Category == CouncilCategory.Arbitration || c.CouncilCode == "SYS_ARBITRATION");

                            if (preconfiguredArb != null && preconfiguredArb.Seats.Count >= 4)
                            {
                                using var healScope = _scopeFactory.CreateScope();
                                var healDb = healScope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

                                var oldSeats = await healDb.UniversalCouncilSeats.Where(s => s.CouncilId == arbCouncil.Id).ToListAsync(ct);
                                healDb.UniversalCouncilSeats.RemoveRange(oldSeats);

                                var healedSeats = new List<UniversalCouncilSeat>();
                                foreach (var preSeat in preconfiguredArb.Seats)
                                {
                                    var newSeat = new UniversalCouncilSeat
                                    {
                                        Id = Guid.NewGuid(),
                                        CouncilId = arbCouncil.Id,
                                        SeatTitle = preSeat.SeatTitle,
                                        Role = preSeat.Role,
                                        IdealArchetype = preSeat.IdealArchetype,
                                        PreferredModelCode = preSeat.PreferredModelCode,
                                        AlphaModelCode = preSeat.AlphaModelCode,
                                        SwarmModelCodesJson = preSeat.SwarmModelCodesJson,
                                        SwarmPolicy = preSeat.SwarmPolicy,
                                        IsReady = true,
                                        DecisionWeight = preSeat.DecisionWeight,
                                        SortOrder = preSeat.SortOrder,
                                        PersonaPromptDirective = preSeat.PersonaPromptDirective
                                    };
                                    healedSeats.Add(newSeat);
                                    healDb.UniversalCouncilSeats.Add(newSeat);
                                }

                                await healDb.SaveChangesAsync(ct);
                                arbCouncil.Seats = healedSeats;
                                _logger.LogInformation("[AutoHeal] ✅ Đã tự động phục hồi 4 Ghế chuyên môn hóa đầy đủ chân các Sói cho Viện Trọng Tài SYS_ARBITRATION.");
                            }
                        }
                        catch (Exception exHeal)
                        {
                            _logger.LogWarning(exHeal, "[AutoHeal] Lỗi khi tự động phục hồi ghế cho SYS_ARBITRATION: {Message}", exHeal.Message);
                        }
                    }

                    await _roleAllocator.EvaluateCouncilsReadinessAsync(dbCouncils, Guid.Empty, ct);
                    return dbCouncils;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Symposium] Lỗi nạp hội đồng từ DB, fallback sang preconfigured provider.");
            }

            var preconfigured = _councilProvider.GetPreconfiguredCouncils();
            await _roleAllocator.EvaluateCouncilsReadinessAsync(preconfigured, Guid.Empty, ct);
            return preconfigured;
        }

        public async Task<SymposiumProceedingsResult> ConveneSymposiumAsync(ConveneSymposiumRequest request, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            var sessionId = request.ClientSessionId.HasValue && request.ClientSessionId.Value != Guid.Empty
                ? request.ClientSessionId.Value
                : Guid.NewGuid();
            int roundNumber = request.RoundNumber > 0 ? request.RoundNumber : 1;

            var effectiveTeleology = _teleologyResolver.ResolveTeleologyMode(request.TeleologyMode, request.TopicTitle, request.CoreProblemStatement);
            _logger.LogInformation("[Symposium] Bắt đầu triệu tập Đại Hội Thảo: {Title} | Vòng: {Round} | Mode: {Mode}", request.TopicTitle, roundNumber, effectiveTeleology);

            ISymposiumLiveBroadcaster? broadcaster = null;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                broadcaster = scope.ServiceProvider.GetService<ISymposiumLiveBroadcaster>();
            }
            catch { /* optional */ }

            // 1. Tuyển chọn các Viện Hàn Lâm tham gia qua CouncilSelectionStrategy
            var allCouncils = await GetActiveCouncilsAsync(ct);
            var selectionContext = new CouncilSelectionContext
            {
                TopicTitle = request.TopicTitle,
                CoreProblemStatement = request.CoreProblemStatement,
                RequestedCouncilCodes = request.SpecificCouncilCodes,
                IncludeContrarianPerspective = request.EnableAdversarialCritique,
                AllowDynamicGenesis = request.AllowDynamicGenesis,
                TenantId = request.TenantId,
                MinCouncils = 2,
                MaxCouncils = 4
            };
            var participating = await _councilSelector.SelectCouncilsAsync(selectionContext, allCouncils, ct);
            if (participating.Count < 2)
            {
                participating = allCouncils
                    .OrderByDescending(c => c.ReadinessStatus == "READY" ? 2 : c.ReadinessStatus == "DEGRADED" ? 1 : 0)
                    .ThenByDescending(c => c.BenchmarkPrestigeElo)
                    .Take(3).ToList();
            }

            var dialogueLog = new List<SymposiumDialogueTurn>();

            // ── BƯỚC 0: TRINH SÁT BẰNG CHỨNG THỰC NGHIỆM (Deep Research Scout) ──────
            EmpiricalEvidenceDossierDto? empiricalDossier = null;
            try
            {
                if (broadcaster != null)
                    await broadcaster.BroadcastPhaseProgressAsync(sessionId.ToString(), 0, FormatPhaseName(0, roundNumber, request.Locale ?? "vi-VN"), 5, ct);

                empiricalDossier = await _deepResearchScout.ConductPreSymposiumInvestigationAsync(request.TopicTitle, request.CoreProblemStatement, ct);
            }
            catch (Exception exDR)
            {
                _logger.LogWarning(exDR, "[Symposium] Deep Research Scout trinh sát dữ liệu thất bại, tiếp tục luồng chuẩn.");
            }

            if (empiricalDossier == null)
            {
                empiricalDossier = new EmpiricalEvidenceDossierDto
                {
                    TopicTitle = request.TopicTitle,
                    DomainArea = "Đa ngành",
                    EvidenceLedger = new List<EvidenceLedgerItemDto>(),
                    VerifiedFacts = new List<string>(),
                    QuantitativeDataPoints = new List<string>(),
                    AcademicCitations = new List<string>(),
                    UnresolvedControversies = new List<string>(),
                    FormattedDossierMarkdown = "",
                    GatheredAt = DateTime.UtcNow,
                    TotalEvidenceUnits = 0
                };
            }

            // [NẠP DỮ LIỆU & TƯ LIỆU BỔ SUNG TỪ CHỦ TỌA]
            if (!string.IsNullOrWhiteSpace(request.AdditionalDocuments))
            {
                int nextId = (empiricalDossier.EvidenceLedger?.Count ?? 0) + 1;
                var docItem = new EvidenceLedgerItemDto
                {
                    Id = $"E{nextId}",
                    ClaimOrFact = request.AdditionalDocuments.Length > 200 ? request.AdditionalDocuments[..200] + "..." : request.AdditionalDocuments,
                    SourceTitle = "Văn kiện/Tư liệu bổ sung do Chủ tọa nạp vào",
                    Provenance = "Chủ tọa phiên họp (Socratic Human Directive)",
                    IsOnlineVerified = true,
                    ConfidenceWeight = 0.98
                };
                empiricalDossier.EvidenceLedger ??= new List<EvidenceLedgerItemDto>();
                empiricalDossier.EvidenceLedger.Add(docItem);
                empiricalDossier.VerifiedFacts.Add($"[Chủ tọa nạp] {docItem.ClaimOrFact}");
                empiricalDossier.FormattedDossierMarkdown += $"\n\n### 📜 TƯ LIỆU BỔ SUNG DO CHỦ TỌA NẠP VÀO (VÒNG {roundNumber})\n{request.AdditionalDocuments}";
            }

            if (request.AdditionalUris != null && request.AdditionalUris.Count > 0)
            {
                foreach (var uri in request.AdditionalUris.Where(u => !string.IsNullOrWhiteSpace(u)))
                {
                    int nextId = (empiricalDossier.EvidenceLedger?.Count ?? 0) + 1;
                    var uriItem = new EvidenceLedgerItemDto
                    {
                        Id = $"E{nextId}",
                        ClaimOrFact = $"Tài liệu trích xuất từ liên kết tham chiếu: {uri}",
                        SourceTitle = uri,
                        DoiOrUrl = uri,
                        Provenance = "Chủ tọa chỉ định liên kết tham chiếu trực tiếp",
                        IsOnlineVerified = true,
                        ConfidenceWeight = 0.95
                    };
                    empiricalDossier.EvidenceLedger ??= new List<EvidenceLedgerItemDto>();
                    empiricalDossier.EvidenceLedger.Add(uriItem);
                    empiricalDossier.AcademicCitations.Add($"[{uriItem.Id}] {uri}");
                }
            }

            // Xây dựng bối cảnh Đa vòng & Chỉ đạo Socratic
            var roundContextSb = new System.Text.StringBuilder();
            if (roundNumber > 1)
            {
                roundContextSb.AppendLine($"\n=== NGỮ CẢNH VÒNG {roundNumber} (DIALECTICAL EVOLUTION ROUND {roundNumber}) ===");
                if (!string.IsNullOrWhiteSpace(request.PreviousRoundResolution))
                {
                    roundContextSb.AppendLine($"• NGHỊ QUYẾT/KẾT LUẬN CỦA VÒNG {roundNumber - 1}:");
                    roundContextSb.AppendLine(request.PreviousRoundResolution);
                }
                if (!string.IsNullOrWhiteSpace(request.SocraticDirectives))
                {
                    roundContextSb.AppendLine($"• Ý KIẾN CHỈ ĐẠO & ĐỊNH HƯỚNG CỦA CHỦ TỌA CHO VÒNG NÀY:");
                    roundContextSb.AppendLine(request.SocraticDirectives);
                }
                if (!string.IsNullOrWhiteSpace(request.AdditionalDocuments))
                {
                    roundContextSb.AppendLine($"• TƯ LIỆU VĂN KIỆN NẠP THÊM:");
                    roundContextSb.AppendLine(request.AdditionalDocuments);
                }
                roundContextSb.AppendLine("• YÊU CẦU: Đào sâu, giải quyết các nghịch lý vòng trước và tuân thủ định hướng chỉ đạo của Chủ tọa.\n=======================================================\n");
            }
            string roundContextPrompt = roundContextSb.ToString();

            // ── BƯỚC 0.5: PHÂN VAI ĐỘNG HỌC KHÔNG - THỜI GIAN & LẬP TRƯỜNG NHẬN THỨC ────
            await _roleAllocator.AllocateCouncilRolesAsync(request.TopicTitle, request.CoreProblemStatement, participating, request.TenantId ?? Guid.Empty, ct);
            var councilContributions = _contributionGenerator.GenerateContributions(request.TopicTitle, request.CoreProblemStatement, participating, empiricalDossier);

            string currentLocale = request.Locale ?? "vi-VN";

            // ── BƯỚC 1: ĐẶT VẤN ĐỀ KHỞI NGUYÊN (Problem Formulation) ───────────────────
            var p1PhaseName = FormatPhaseName(1, roundNumber, currentLocale);
            if (broadcaster != null)
                await broadcaster.BroadcastPhaseProgressAsync(sessionId.ToString(), 1, p1PhaseName, 15, ct);

            foreach (var council in participating)
            {
                var presSeat = council.Seats.FirstOrDefault(s => s.Role == CouncilSeatRole.President) ?? council.Seats.FirstOrDefault()
                    ?? new UniversalCouncilSeat { SeatTitle = "Viện Trưởng", PreferredModelCode = "deepseek-r1", AlphaModelCode = "deepseek-r1" };
                var contrib = councilContributions.FirstOrDefault(c => c.CouncilCode == council.CouncilCode);

                // Ghép System Prompt 6 Tầng chuẩn OCP & Zero Hard-code qua ISymposiumPromptComposer
                var promptCtx = new PromptCompositionContext
                {
                    Council = council,
                    Seat = presSeat,
                    PhaseCode = "FORMULATION",
                    TopicTitle = request.TopicTitle,
                    CoreProblemStatement = request.CoreProblemStatement,
                    ParticipatingCouncils = participating.Select(c => c.DisplayName).ToList(),
                    Locale = currentLocale,
                    TenantId = request.TenantId,
                    ExtraVariables = new Dictionary<string, string>
                    {
                        ["symposium.roundNumber"] = roundNumber.ToString(),
                        ["symposium.affinity"] = council.SpatioTemporalAffinity ?? string.Empty,
                        ["symposium.empiricalDossier"] = empiricalDossier?.FormattedDossierMarkdown ?? string.Empty
                    }
                };

                var composedPrompt = await _promptComposer.ComposeSeatPromptAsync(promptCtx, ct);
                var systemPrompt = composedPrompt.Text;

                var userPrompt = $"Đề tài: {request.TopicTitle}\nBối cảnh: {request.CoreProblemStatement}\n{roundContextPrompt}\n{empiricalDossier?.FormattedDossierMarkdown}\n\nPhát biểu của quý viện:";

                var seatResp = await _wolfpackInvoker.InvokeRoleSwarmAsync(
                    presSeat,
                    council,
                    systemPrompt,
                    userPrompt,
                    temperature: 0.2,
                    maxTokens: 1500,
                    userId: request.UserId,
                    tenantId: request.TenantId,
                    ct: ct);

                var actualModel = seatResp.ModelUsed;
                var turn = new SymposiumDialogueTurn
                {
                    RoundNumber = roundNumber,
                    PhaseNumber = 1,
                    PhaseName = p1PhaseName,
                    CouncilCode = council.CouncilCode,
                    CouncilDisplayName = council.DisplayName,
                    SpeakerTitle = $"{presSeat.SeatTitle} ({actualModel})",
                    ModelCode = actualModel,
                    SpatioTemporalPerspective = council.SpatioTemporalAffinity,
                    AssignmentRationale = presSeat.AssignmentRationale,
                    Content = seatResp.Success && !string.IsNullOrWhiteSpace(seatResp.Content)
                        ? seatResp.Content
                        : (contrib?.DeepAnalysis ?? $"Dưới góc độ {council.DisciplineArea}, vấn đề [{request.TopicTitle}] đặt ra tiên đề: \"{council.EpistemicManifesto}\"."),
                    KeyTheses = contrib?.KeyTheses ?? new List<string> { $"Xác lập khung tham chiếu {council.DisciplineArea}", "Định vị tiên đề bất biến" },
                    LatencyMs = seatResp.LatencyMs,
                    IsSimulated = !seatResp.Success,
                    SimulationReason = seatResp.Success ? null : seatResp.ErrorMessage,
                    KeySource = seatResp.KeySource
                };
                dialogueLog.Add(turn);
                if (broadcaster != null) await broadcaster.BroadcastTurnSpokenAsync(sessionId.ToString(), turn, ct);
            }

            if (broadcaster != null)
            {
                var p1Turns = dialogueLog.Where(t => t.PhaseNumber == 1).ToList();
                await broadcaster.BroadcastPhaseResultAsync(sessionId.ToString(), 1, p1PhaseName, new
                {
                    EmpiricalDossier = empiricalDossier,
                    Turns = p1Turns
                }, ct);
            }

            // ── BƯỚC 2: VẤN ĐÁP XUYÊN NGÀNH (Socratic Cross-Examination) ─────────────
            var p2PhaseName = FormatPhaseName(2, roundNumber, currentLocale);
            if (broadcaster != null)
                await broadcaster.BroadcastPhaseProgressAsync(sessionId.ToString(), 2, p2PhaseName, 35, ct);

            for (int i = 0; i < participating.Count; i++)
            {
                var cFrom = participating[i];
                var cTo = participating[(i + 1) % participating.Count];
                var seat = cFrom.Seats.FirstOrDefault(s => s.Role == CouncilSeatRole.President) 
                    ?? cFrom.Seats.FirstOrDefault() 
                    ?? new UniversalCouncilSeat { SeatTitle = "Viện Trưởng", PreferredModelCode = "gemini-2.0-flash", AlphaModelCode = "gemini-2.0-flash" };

                var promptCtx = new PromptCompositionContext
                {
                    Council = cFrom,
                    Seat = seat,
                    PhaseCode = "CROSS_EXAMINATION",
                    TopicTitle = request.TopicTitle,
                    CoreProblemStatement = request.CoreProblemStatement,
                    ParticipatingCouncils = participating.Select(c => c.DisplayName).ToList(),
                    Locale = currentLocale,
                    TenantId = request.TenantId,
                    ExtraVariables = new Dictionary<string, string>
                    {
                        ["symposium.roundNumber"] = roundNumber.ToString(),
                        ["symposium.targetCouncil"] = cTo.DisplayName,
                        ["symposium.targetCouncilCode"] = cTo.CouncilCode,
                        ["symposium.empiricalDossier"] = empiricalDossier?.FormattedDossierMarkdown ?? string.Empty
                    }
                };

                var composedPrompt = await _promptComposer.ComposeSeatPromptAsync(promptCtx, ct);
                var systemPrompt = composedPrompt.Text;

                var userPrompt = $"Đề tài: {request.TopicTitle}\n{roundContextPrompt}\n{empiricalDossier?.FormattedDossierMarkdown}\nCâu hỏi chất vấn của quý viện:";

                var seatResp = await _wolfpackInvoker.InvokeRoleSwarmAsync(
                    seat,
                    cFrom,
                    systemPrompt,
                    userPrompt,
                    temperature: 0.3,
                    maxTokens: 800,
                    userId: request.UserId,
                    tenantId: request.TenantId,
                    ct: ct);

                var actualModel = seatResp.ModelUsed;
                var turn = new SymposiumDialogueTurn
                {
                    RoundNumber = roundNumber,
                    PhaseNumber = 2,
                    PhaseName = p2PhaseName,
                    CouncilCode = cFrom.CouncilCode,
                    CouncilDisplayName = cFrom.DisplayName,
                    SpeakerTitle = $"{seat.SeatTitle} ({actualModel})",
                    ModelCode = actualModel,
                    Content = seatResp.Success && !string.IsNullOrWhiteSpace(seatResp.Content)
                        ? seatResp.Content
                        : $"Chất vấn gửi tới {cTo.DisplayName}: Giả định của quý viện xử lý như thế nào trước các dữ kiện thực nghiệm và giới hạn biên?",
                    LatencyMs = seatResp.LatencyMs,
                    IsSimulated = !seatResp.Success,
                    SimulationReason = seatResp.Success ? null : seatResp.ErrorMessage,
                    KeySource = seatResp.KeySource
                };
                dialogueLog.Add(turn);
                if (broadcaster != null) await broadcaster.BroadcastTurnSpokenAsync(sessionId.ToString(), turn, ct);
            }

            if (broadcaster != null)
            {
                var p2Turns = dialogueLog.Where(t => t.PhaseNumber == 2).ToList();
                await broadcaster.BroadcastPhaseResultAsync(sessionId.ToString(), 2, p2PhaseName, new
                {
                    CouncilContributions = councilContributions,
                    Turns = p2Turns
                }, ct);
            }

            // ── BƯỚC 3: PHẢN BIỆN ĐỐI KHÁNG (Adversarial Critique) ────────────────────
            if (request.EnableAdversarialCritique)
            {
                var p3PhaseName = FormatPhaseName(3, roundNumber, currentLocale);
                if (broadcaster != null)
                    await broadcaster.BroadcastPhaseProgressAsync(sessionId.ToString(), 3, p3PhaseName, 55, ct);

                foreach (var council in participating)
                {
                    var devilSeat = council.Seats.FirstOrDefault(s => s.Role == CouncilSeatRole.AdversarialCritic) 
                        ?? council.Seats.LastOrDefault() 
                        ?? new UniversalCouncilSeat { SeatTitle = "Phản Biện Sắc Bén", PreferredModelCode = "gemini-2.0-flash", AlphaModelCode = "gemini-2.0-flash" };

                    var promptCtx = new PromptCompositionContext
                    {
                        Council = council,
                        Seat = devilSeat,
                        PhaseCode = "CRITIQUE",
                        TopicTitle = request.TopicTitle,
                        CoreProblemStatement = request.CoreProblemStatement,
                        ParticipatingCouncils = participating.Select(c => c.DisplayName).ToList(),
                        Locale = currentLocale,
                        TenantId = request.TenantId,
                        ExtraVariables = new Dictionary<string, string>
                        {
                            ["symposium.roundNumber"] = roundNumber.ToString(),
                            ["symposium.empiricalDossier"] = empiricalDossier?.FormattedDossierMarkdown ?? string.Empty
                        }
                    };

                    var composedPrompt = await _promptComposer.ComposeSeatPromptAsync(promptCtx, ct);
                    var systemPrompt = composedPrompt.Text;

                    var userPrompt = $"Đề tài: {request.TopicTitle}\n{roundContextPrompt}\n{empiricalDossier?.FormattedDossierMarkdown}\nLuận điểm phản biện sắc bén nhất:";

                    var seatResp = await _wolfpackInvoker.InvokeRoleSwarmAsync(
                        devilSeat,
                        council,
                        systemPrompt,
                        userPrompt,
                        temperature: 0.4,
                        maxTokens: 1200,
                        userId: request.UserId,
                        tenantId: request.TenantId,
                        ct: ct);

                    var actualModel = seatResp.ModelUsed;
                    var turn = new SymposiumDialogueTurn
                    {
                        RoundNumber = roundNumber,
                        PhaseNumber = 3,
                        PhaseName = p3PhaseName,
                        CouncilCode = council.CouncilCode,
                        CouncilDisplayName = council.DisplayName,
                        SpeakerTitle = $"{devilSeat.SeatTitle} ({actualModel})",
                        ModelCode = actualModel,
                        Content = seatResp.Success && !string.IsNullOrWhiteSpace(seatResp.Content)
                            ? seatResp.Content
                            : (councilContributions.FirstOrDefault(c => c.CouncilCode == council.CouncilCode)?.PrimaryObjection ?? "Cảnh báo ngụy biện ngoại suy tuyến tính và ảo tưởng vị nhân tâm."),
                        ExposedFallacies = new List<string> { "Ngụy biện ngoại suy tuyến tính", "Lỗ hổng kiểm chứng Popper" },
                        LatencyMs = seatResp.LatencyMs,
                        IsSimulated = !seatResp.Success,
                        SimulationReason = seatResp.Success ? null : seatResp.ErrorMessage,
                        KeySource = seatResp.KeySource
                    };
                    dialogueLog.Add(turn);
                    if (broadcaster != null) await broadcaster.BroadcastTurnSpokenAsync(sessionId.ToString(), turn, ct);
                }
            }

            if (broadcaster != null)
            {
                var p3Turns = dialogueLog.Where(t => t.PhaseNumber == 3).ToList();
                await broadcaster.BroadcastPhaseResultAsync(sessionId.ToString(), 3, FormatPhaseName(3, roundNumber, currentLocale), new
                {
                    Turns = p3Turns
                }, ct);
            }

            // ── BƯỚC 4: HỢP NHẤT SIÊU LÝ THUYẾT QUA HỘI ĐỒNG THƯ KÝ ĐA MÔ HÌNH (Secretariat Council Cascade) ──
            var p4PhaseName = FormatPhaseName(4, roundNumber, currentLocale);
            if (broadcaster != null)
                await broadcaster.BroadcastPhaseProgressAsync(sessionId.ToString(), 4, p4PhaseName, 75, ct);

            var synthTurn = await _secretariat.SynthesizeMetatheoryAsync(
                request.TopicTitle,
                request.CoreProblemStatement,
                effectiveTeleology,
                roundNumber,
                roundContextPrompt,
                empiricalDossier,
                dialogueLog,
                participating,
                councilContributions,
                request.UserId,
                request.TenantId,
                currentLocale,
                ct);

            synthTurn.PhaseName = p4PhaseName;
            dialogueLog.Add(synthTurn);
            if (broadcaster != null)
            {
                await broadcaster.BroadcastTurnSpokenAsync(sessionId.ToString(), synthTurn, ct);
                await broadcaster.BroadcastPhaseResultAsync(sessionId.ToString(), 4, p4PhaseName, synthTurn, ct);
            }

            // ── BƯỚC 5: CHẤM ĐIỂM MA TRẬN ĐA TRỤC QUA TRỌNG TÀI ĐỘC LẬP ───────────────
            var p5PhaseName = FormatPhaseName(5, roundNumber, currentLocale);
            if (broadcaster != null)
                await broadcaster.BroadcastPhaseProgressAsync(sessionId.ToString(), 5, p5PhaseName, 90, ct);

            var rubric = await _symposiumJudge.EvaluateSessionAsync(
                request.TopicTitle, request.CoreProblemStatement, empiricalDossier, dialogueLog, participating, request.UserId, request.TenantId, currentLocale, ct);

            if (broadcaster != null)
            {
                await broadcaster.BroadcastPhaseResultAsync(sessionId.ToString(), 5, p5PhaseName, rubric, ct);
            }

            // ── BƯỚC 6: XÂY DỰNG 5 KHỐI TRI THỨC & TUYÊN NGÔN NGHỊ QUYẾT ─────────────
            bool hasRealTurns = dialogueLog.Any(t => !t.IsSimulated);
            var allDialogueText = string.Join(" ", dialogueLog.Select(d => d.Content));
            var citedEvidenceIds = Regex.Matches(allDialogueText, @"\[(E\d+)\]", RegexOptions.IgnoreCase)
                .Select(m => m.Groups[1].Value.ToUpperInvariant()).Distinct().OrderBy(x => x).ToList();

            var structuredOutcome = await _outcomeBuilder.BuildStructuredOutcomeAsync(
                effectiveTeleology, request.TopicTitle, request.CoreProblemStatement, empiricalDossier, dialogueLog, councilContributions, citedEvidenceIds, hasRealTurns, request.UserId, request.TenantId, ct);

            var epistemicEvolutionIndex = rubric.IsEvaluated && rubric.OverallCompositeScore > 0
                ? Math.Round((rubric.OverallCompositeScore / 10.0) * (rubric.GroundingRate > 0 ? rubric.GroundingRate / 100.0 : 0.5), 3)
                : 0.0;

            // ĐIỀU KIỆN KẾT THÚC HỘI THẢO (TERMINATION CRITERIA)
            // 1. Chủ tọa chủ động yêu cầu kết thúc (IsFinalRound = true)
            // 2. Hội thảo đạt được chân lý như yêu cầu (Chỉ số EEV >= 0.82 và Rubric >= 8.8)
            bool reachedConsensus = rubric.IsEvaluated && rubric.OverallCompositeScore >= 8.8 && epistemicEvolutionIndex >= 0.82;
            bool isFinalRound = request.IsFinalRound || reachedConsensus;

            var finalDeclaration = _resolutionComposer.ComposeFinalDeclaration(
                sessionId, request.TopicTitle, effectiveTeleology, citedEvidenceIds, rubric, structuredOutcome, participating, hasRealTurns,
                roundNumber, isFinalRound, request.SocraticDirectives);

            if (broadcaster != null)
            {
                await broadcaster.BroadcastPhaseResultAsync(sessionId.ToString(), 6, FormatPhaseName(6, roundNumber, currentLocale), new
                {
                    StructuredOutcome = structuredOutcome,
                    FinalConsensusDeclaration = finalDeclaration,
                    EpistemicEvolutionIndex = epistemicEvolutionIndex,
                    ReachedConsensus = reachedConsensus,
                    IsFinalRound = isFinalRound
                }, ct);
            }

            // Lưu phiên vào CSDL
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();
                db.SymposiumSessions.Add(new SymposiumSession
                {
                    Id = sessionId,
                    TopicTitle = request.TopicTitle,
                    CoreProblemStatement = request.CoreProblemStatement,
                    ParticipatingCouncilCodesJson = JsonSerializer.Serialize(participating.Select(c => c.CouncilCode)),
                    DialogueLogJson = JsonSerializer.Serialize(dialogueLog),
                    FinalConsensusResolution = finalDeclaration,
                    EpistemicConsensusIndex = rubric.IsEvaluated ? (int)(rubric.OverallCompositeScore * 10) : 0,
                    RubricScoresJson = JsonSerializer.Serialize(new
                    {
                        Rubric = rubric,
                        RoundNumber = roundNumber,
                        ParentSessionId = request.ParentSessionId,
                        IsFinalRound = isFinalRound,
                        ReachedEpistemicConsensus = reachedConsensus,
                        SocraticDirectives = request.SocraticDirectives,
                        TargetNotebookId = request.TargetNotebookId
                    }),
                    ConvenedAt = DateTime.UtcNow,
                    ConcludedAt = DateTime.UtcNow,
                    InitiatedByUserId = request.UserId,
                    TenantId = request.TenantId ?? Guid.Empty
                });
                await db.SaveChangesAsync(ct);
            }
            catch (Exception exSave)
            {
                _logger.LogWarning(exSave, "[Symposium] Lỗi lưu vết Kỷ yếu vào CSDL: {Message}", exSave.Message);
            }

            // TỰ ĐỘNG GHI VẾT TÍCH LŨY TOÀN BỘ CÁC PHA VÀO NOTEBOOK HUB (LIVING ACADEMIC CHRONICLE)
            Guid? savedNotebookId = request.TargetNotebookId;
            string notebookMsg = "Đã tự động lưu vết kỷ yếu vào Notebook Hub.";
            try
            {
                savedNotebookId = await ExportSymposiumToNotebookAsync(
                    sessionId,
                    request.TargetNotebookId,
                    request.UserId ?? Guid.Empty,
                    request.TenantId ?? Guid.Empty,
                    ct);

                var nbShort = savedNotebookId.HasValue ? savedNotebookId.Value.ToString()[..8].ToUpperInvariant() : "HUB";
                notebookMsg = isFinalRound 
                    ? $"Đã lưu vết trọn vẹn Kỷ yếu Chung cuộc vào Notebook #{nbShort}."
                    : $"Đã ghi chép nối tiếp diễn biến Vòng {roundNumber} vào Notebook #{nbShort}.";
            }
            catch (Exception exNb)
            {
                _logger.LogWarning(exNb, "[Symposium] Lỗi tự động lưu vết Kỷ yếu vào Notebook Hub: {Message}", exNb.Message);
            }

            sw.Stop();

            var result = new SymposiumProceedingsResult
            {
                SessionId = sessionId,
                TopicTitle = request.TopicTitle,
                CoreProblemStatement = request.CoreProblemStatement,
                TeleologyMode = effectiveTeleology,
                StructuredOutcome = structuredOutcome,
                ParticipatingCouncils = participating.Select(c => c.DisplayName).ToList(),
                CouncilContributions = councilContributions,
                DialogueLog = dialogueLog,
                UnifiedMetatheory = synthTurn.Content,
                FinalConsensusDeclaration = finalDeclaration,
                EvaluatedRubric = rubric,
                EpistemicEvolutionIndex = epistemicEvolutionIndex,
                EmpiricalDossier = empiricalDossier,
                GroundingRate = rubric.GroundingRate,
                GroundedEvidenceIds = citedEvidenceIds,
                IsSimulated = !hasRealTurns,
                SimulationReason = hasRealTurns ? null : "Chưa có mô hình AI nào của Hội đồng kết nối thành công qua API Key khả dụng.",
                TotalSessionDurationMs = sw.ElapsedMilliseconds,
                ConcludedAt = DateTime.UtcNow,
                RoundNumber = roundNumber,
                ParentSessionId = request.ParentSessionId,
                IsFinalRound = isFinalRound,
                ReachedEpistemicConsensus = reachedConsensus,
                SocraticDirectives = request.SocraticDirectives,
                TargetNotebookId = savedNotebookId,
                NotebookArchiveMessage = notebookMsg
            };

            if (broadcaster != null)
            {
                var finalPhaseTitle = isFinalRound
                    ? "Tuyên Bố Nghị Quyết Chung Cuộc & Kỷ Yếu Hoàn Tất"
                    : $"Kết Thúc Vòng {roundNumber} — Sẵn Sàng Cho Vòng Tiếp Theo";
                await broadcaster.BroadcastPhaseProgressAsync(sessionId.ToString(), 6, finalPhaseTitle, 100, ct);
                await broadcaster.BroadcastProceedingsCompletedAsync(sessionId.ToString(), result, ct);
            }

            return result;
        }

        public async Task<Guid> ExportSymposiumToNotebookAsync(
            Guid sessionId,
            Guid? targetNotebookId,
            Guid userId,
            Guid tenantId,
            CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            var session = await db.SymposiumSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId, ct);
            if (session == null) throw new InvalidOperationException($"Không tìm thấy phiên hội thảo {sessionId} để xuất kỷ yếu");

            SymposiumRubricScore? rubric = null;
            int roundNumber = 1;
            bool isFinalRound = false;
            string? socraticDirectives = null;

            if (!string.IsNullOrEmpty(session.RubricScoresJson))
            {
                try 
                { 
                    rubric = JsonSerializer.Deserialize<SymposiumRubricScore>(session.RubricScoresJson); 
                } 
                catch 
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(session.RubricScoresJson);
                        if (doc.RootElement.TryGetProperty("Rubric", out var rProp))
                            rubric = JsonSerializer.Deserialize<SymposiumRubricScore>(rProp.GetRawText());
                        if (doc.RootElement.TryGetProperty("RoundNumber", out var rnProp))
                            roundNumber = rnProp.GetInt32();
                        if (doc.RootElement.TryGetProperty("IsFinalRound", out var ifrProp))
                            isFinalRound = ifrProp.GetBoolean();
                        if (doc.RootElement.TryGetProperty("SocraticDirectives", out var sdProp))
                            socraticDirectives = sdProp.GetString();
                    }
                    catch { }
                }
            }

            if (session.FinalConsensusResolution != null && session.FinalConsensusResolution.Contains("NGHỊ QUYẾT CHUNG CUỘC"))
            {
                isFinalRound = true;
            }

            bool isSimulatedSession = session.EpistemicConsensusIndex <= 0 || 
                (session.FinalConsensusResolution != null && session.FinalConsensusResolution.Contains("CHẾ ĐỘ MÔ PHỎNG"));

            var markdownContent = _resolutionComposer.ComposeNotebookMarkdown(
                session, rubric, roundNumber, isFinalRound, socraticDirectives);

            Notebook targetNotebook;
            if (targetNotebookId.HasValue && targetNotebookId.Value != Guid.Empty)
            {
                targetNotebook = await db.Notebooks.FirstOrDefaultAsync(n => n.Id == targetNotebookId.Value, ct)
                    ?? throw new InvalidOperationException($"Không tìm thấy Notebook đích {targetNotebookId}");
            }
            else
            {
                targetNotebook = new Notebook
                {
                    Id = Guid.NewGuid(),
                    Title = $"🌌 Kỷ Yếu Hàn Lâm: {session.TopicTitle}",
                    Description = $"Hồ sơ tri thức biện chứng đa vòng từ Đại Hội Thảo Bách Khoa Vạn Vật Tri Thức. Mã phiên: {session.Id}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedByAdminId = userId,
                    TenantId = tenantId
                };
                db.Notebooks.Add(targetNotebook);
            }

            string sourceTitle = isFinalRound
                ? $"🏆 [CHUNG CUỘC] Tuyên Ngôn Nghị Quyết Tối Hậu — {session.TopicTitle}"
                : $"⚡ [Vòng {roundNumber}] Biên Bản Biện Chứng & Nghị Quyết — {session.TopicTitle}";

            var noteSource = new NotebookSource
            {
                Id = Guid.NewGuid(),
                NotebookId = targetNotebook.Id,
                Title = sourceTitle,
                Type = SourceType.PlainText,
                Authority = isSimulatedSession ? AuthorityLevel.ReferenceCase : AuthorityLevel.SupremeLegal,
                ExtractedText = markdownContent,
                Status = SourceStatus.Ready,
                UploadedAt = DateTime.UtcNow,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    SymposiumSessionId = session.Id,
                    ConcludedAt = session.ConcludedAt,
                    EpistemicConsensusIndex = session.EpistemicConsensusIndex,
                    ParticipatingCouncils = session.ParticipatingCouncilCodesJson
                })
            };

            db.NotebookSources.Add(noteSource);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("[Symposium] Xuất thành công Kỷ yếu phiên {SessionId} vào Notebook {NotebookId}", sessionId, targetNotebook.Id);
            return targetNotebook.Id;
        }

        #region Council & Seat Administration (CRUD)

        public async Task<UniversalAiCouncil> CreateOrUpdateCouncilAsync(UniversalAiCouncil council, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            var existing = await db.UniversalAiCouncils.FirstOrDefaultAsync(c => c.Id == council.Id, ct);
            if (existing == null)
            {
                if (council.Id == Guid.Empty) council.Id = Guid.NewGuid();
                council.CreatedAt = DateTime.UtcNow;
                council.UpdatedAt = DateTime.UtcNow;
                db.UniversalAiCouncils.Add(council);
            }
            else
            {
                existing.CouncilCode = council.CouncilCode;
                existing.DisplayName = council.DisplayName;
                existing.Category = council.Category;
                existing.DisciplineArea = council.DisciplineArea;
                existing.MaterializedTaxonomyPath = council.MaterializedTaxonomyPath;
                existing.TriggerKeywordsJson = council.TriggerKeywordsJson;
                existing.EpistemicManifesto = council.EpistemicManifesto;
                existing.BenchmarkPrestigeElo = council.BenchmarkPrestigeElo;
                existing.IsActive = council.IsActive;
                existing.SpatioTemporalAffinity = council.SpatioTemporalAffinity;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            return council;
        }

        public async Task<bool> DeleteCouncilAsync(Guid councilId, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            var council = await db.UniversalAiCouncils.FirstOrDefaultAsync(c => c.Id == councilId, ct);
            if (council == null) return false;

            council.IsActive = false;
            council.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<UniversalCouncilSeat> AddCouncilSeatAsync(Guid councilId, UniversalCouncilSeat seat, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            if (seat.Id == Guid.Empty) seat.Id = Guid.NewGuid();
            seat.CouncilId = councilId;

            db.UniversalCouncilSeats.Add(seat);
            await db.SaveChangesAsync(ct);
            return seat;
        }

        public async Task<bool> RemoveCouncilSeatAsync(Guid seatId, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            var seat = await db.UniversalCouncilSeats.FirstOrDefaultAsync(s => s.Id == seatId, ct);
            if (seat == null) return false;

            db.UniversalCouncilSeats.Remove(seat);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<UniversalCouncilSeat?> UpdateCouncilSeatSwarmAsync(
            Guid seatId,
            string alphaModelCode,
            string swarmModelCodesJson,
            string swarmPolicy,
            string? personaPromptDirective = null,
            string? seatTitle = null,
            CouncilSeatRole? role = null,
            double? decisionWeight = null,
            CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            var seat = await db.UniversalCouncilSeats.FirstOrDefaultAsync(s => s.Id == seatId, ct);
            if (seat == null) return null;

            seat.AlphaModelCode = alphaModelCode;
            seat.SwarmModelCodesJson = swarmModelCodesJson;
            if (!string.IsNullOrWhiteSpace(swarmPolicy)) seat.SwarmPolicy = swarmPolicy;
            if (personaPromptDirective != null) seat.PersonaPromptDirective = personaPromptDirective;
            if (!string.IsNullOrWhiteSpace(seatTitle)) seat.SeatTitle = seatTitle;
            if (role.HasValue) seat.Role = role.Value;
            if (decisionWeight.HasValue) seat.DecisionWeight = decisionWeight.Value;

            await db.SaveChangesAsync(ct);
            return seat;
        }

        private static string FormatPhaseName(int phase, int round, string locale)
        {
            bool isEn = locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;
            return phase switch
            {
                0 => isEn ? $"Deep Research Grounding (Round {round})" : $"Khai Quật Bằng Chứng Thực Nghiệm (Deep Research Grounding - Vòng {round})",
                1 => isEn ? $"Problem Formulation (Round {round})" : $"Đặt Vấn Đề Khởi Nguyên (Problem Formulation - Vòng {round})",
                2 => isEn ? $"Socratic Cross-Examination (Round {round})" : $"Vấn Đáp Xuyên Ngành (Socratic Cross-Examination - Vòng {round})",
                3 => isEn ? $"Adversarial Critique (Round {round})" : $"Phản Biện Đối Kháng (Adversarial Critique - Vòng {round})",
                4 => isEn ? $"Metatheory Synthesis (Round {round})" : $"Hợp Nhất Siêu Lý Thuyết (Metatheory Synthesis - Vòng {round})",
                5 => isEn ? $"Independent 5-Axis Rubric Arbitration (Round {round})" : $"Đánh Giá Ma Trận Rubric 5 Trục (Independent Arbiter - Vòng {round})",
                6 => isEn ? $"Knowledge Genesis & Consensus Declaration (Round {round})" : $"Kiến Tạo 5 Khối Tri Thức & Nghị Quyết Chung Cuộc (Vòng {round})",
                _ => $"Phase {phase} (Round {round})"
            };
        }

        #endregion
    }
}
