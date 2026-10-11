using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.AI.Secretariat
{
    /// <summary>
    /// [Isomorphic Council Architecture]
    /// Hội Đồng Thư Ký Hợp Nhất Đại Hội Thảo (Secretariat Council).
    /// Đọc trực tiếp từ CSDL Council (Category == Secretariat) và trao quyền cho Bầy Sói (WolfpackSwarm)
    /// của Ghế Thư Ký Trưởng đảm nhiệm với cơ chế Cascade Failover tự phục hồi.
    /// </summary>
    public class SymposiumSecretariatCouncil : ISymposiumSecretariat
    {
        private readonly IWolfpackRoleInvoker _wolfpackInvoker;
        private readonly IPreconfiguredCouncilProvider _councilProvider;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISymposiumPromptComposer _promptComposer;
        private readonly ILogger<SymposiumSecretariatCouncil> _logger;

        public SymposiumSecretariatCouncil(
            IWolfpackRoleInvoker wolfpackInvoker,
            IPreconfiguredCouncilProvider councilProvider,
            IServiceScopeFactory scopeFactory,
            ISymposiumPromptComposer promptComposer,
            ILogger<SymposiumSecretariatCouncil> logger)
        {
            _wolfpackInvoker = wolfpackInvoker;
            _councilProvider = councilProvider;
            _scopeFactory = scopeFactory;
            _promptComposer = promptComposer;
            _logger = logger;
        }

        public async Task<SymposiumDialogueTurn> SynthesizeMetatheoryAsync(
            string topicTitle,
            string coreProblemStatement,
            SymposiumTeleologyMode teleologyMode,
            int roundNumber,
            string roundContextPrompt,
            EmpiricalEvidenceDossierDto? empiricalDossier,
            List<SymposiumDialogueTurn> dialogueLog,
            List<UniversalAiCouncil> participatingCouncils,
            List<CouncilAcademicContributionDto> councilContributions,
            Guid? userId = null,
            Guid? tenantId = null,
            string locale = "vi-VN",
            CancellationToken ct = default)
        {
            _logger.LogInformation("[SecretariatCouncil] 📜 Triệu tập Hội Đồng Thư Ký Hợp Nhất cho Vòng {Round}...", roundNumber);

            // 1. Lấy Hội Đồng Thư Ký (SYS_SECRETARIAT) từ CSDL
            UniversalAiCouncil? secCouncil = null;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();
                secCouncil = await db.UniversalAiCouncils
                    .Include(c => c.Seats)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Category == CouncilCategory.Secretariat && c.IsActive, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SecretariatCouncil] Lỗi nạp SYS_SECRETARIAT từ CSDL, fallback sang cấu hình hệ thống.");
            }

            if (secCouncil == null || secCouncil.Seats.Count == 0)
            {
                secCouncil = _councilProvider.GetPreconfiguredCouncils()
                    .FirstOrDefault(c => c.Category == CouncilCategory.Secretariat)
                    ?? new UniversalAiCouncil
                    {
                        CouncilCode = "SYS_SECRETARIAT",
                        DisplayName = "Ban Thư Ký Hợp Nhất Đại Hội Thảo",
                        Category = CouncilCategory.Secretariat,
                        Seats = new()
                        {
                            new()
                            {
                                SeatTitle = "Thư Ký Trưởng Hợp Nhất Siêu Lý Thuyết",
                                Role = CouncilSeatRole.Synthesizer,
                                AlphaModelCode = "claude-3-5-sonnet",
                                PreferredModelCode = "claude-3-5-sonnet",
                                SwarmModelCodesJson = @"[""claude-3-5-sonnet"", ""gemini-2.0-flash"", ""deepseek-r1""]"
                            }
                        }
                    };
            }

            // 2. Lấy Ghế Thư Ký Trưởng Hợp Nhất
            var secSeat = secCouncil.Seats.FirstOrDefault(s => s.Role == CouncilSeatRole.Synthesizer) ?? secCouncil.Seats.First();

            var promptCtx = new PromptCompositionContext
            {
                Council = secCouncil,
                Seat = secSeat,
                PhaseCode = "SYNTHESIS",
                TopicTitle = topicTitle,
                CoreProblemStatement = coreProblemStatement,
                ParticipatingCouncils = participatingCouncils.Select(c => c.DisplayName).ToList(),
                Locale = locale,
                TenantId = tenantId,
                ExtraVariables = new Dictionary<string, string>
                {
                    ["symposium.roundNumber"] = roundNumber.ToString(),
                    ["symposium.empiricalDossier"] = empiricalDossier?.FormattedDossierMarkdown ?? string.Empty
                }
            };

            var composedPrompt = await _promptComposer.ComposeSeatPromptAsync(promptCtx, ct);
            var systemPrompt = composedPrompt.Text;

            var userPrompt = PrepareSynthesisPrompt(topicTitle, teleologyMode, roundNumber, roundContextPrompt, empiricalDossier, dialogueLog);

            // 3. Triệu hồi Bầy Sói Đa Mô Hình của Ghế Thư Ký Trưởng
            var resp = await _wolfpackInvoker.InvokeRoleSwarmAsync(
                secSeat,
                secCouncil,
                systemPrompt,
                userPrompt,
                excludeModels: null,
                temperature: 0.25,
                maxTokens: 2500,
                userId: userId,
                tenantId: tenantId,
                ct: ct);

            if (resp.Success && !string.IsNullOrWhiteSpace(resp.Content))
            {
                return new SymposiumDialogueTurn
                {
                    RoundNumber = roundNumber,
                    PhaseNumber = 4,
                    PhaseName = $"Hợp Nhất Siêu Lý Thuyết (Metatheory Synthesis - Vòng {roundNumber})",
                    CouncilCode = secCouncil.CouncilCode,
                    CouncilDisplayName = secCouncil.DisplayName,
                    SpeakerTitle = $"{secSeat.SeatTitle} ({resp.ModelUsed})",
                    ModelCode = resp.ModelUsed,
                    Content = resp.Content,
                    KeyTheses = new List<string>
                    {
                        $"Dung hợp quan điểm {participatingCouncils.Count} viện học thuật",
                        "Kiến tạo khung siêu lý thuyết khả thi trên thực nghiệm [E#]",
                        "Giải trừ nghịch lý biện chứng"
                    },
                    LatencyMs = resp.LatencyMs,
                    IsSimulated = false
                };
            }

            // Fallback an toàn nếu toàn bộ Bầy Sói Thư Ký đều lỗi
            var fallbackContent = $"[Tổng hợp cấu trúc Vòng {roundNumber}]: Các Viện ({string.Join(", ", participatingCouncils.Select(c => c.DisplayName))}) đồng thuận trên nền tảng dữ liệu thực nghiệm [E1..En]. Luận cương siêu lý thuyết hướng tới dung hợp đa ngành.";

            return new SymposiumDialogueTurn
            {
                RoundNumber = roundNumber,
                PhaseNumber = 4,
                PhaseName = $"Hợp Nhất Siêu Lý Thuyết (Metatheory Synthesis - Vòng {roundNumber})",
                CouncilCode = secCouncil.CouncilCode,
                CouncilDisplayName = secCouncil.DisplayName,
                SpeakerTitle = $"{secSeat.SeatTitle} (Fallback)",
                ModelCode = secSeat.AlphaModelCode ?? "auto-secretariat",
                Content = fallbackContent,
                KeyTheses = councilContributions.SelectMany(c => c.KeyTheses).Distinct().Take(6).ToList(),
                LatencyMs = 0,
                IsSimulated = true,
                SimulationReason = $"Bầy Sói của Hội Đồng Thư Ký đều không phản hồi: {resp.ErrorMessage}"
            };
        }

        private string PrepareSynthesisPrompt(
            string topicTitle,
            SymposiumTeleologyMode teleologyMode,
            int roundNumber,
            string roundContextPrompt,
            EmpiricalEvidenceDossierDto? empiricalDossier,
            List<SymposiumDialogueTurn> dialogueLog)
        {
            var turnsSummary = string.Join("\n\n", dialogueLog.Select(d =>
                $"[{d.CouncilDisplayName} - {d.SpeakerTitle} ({d.ModelCode})]:\n{d.Content}"));

            return $@"=== YÊU CẦU TỔNG HỢP SIÊU LÝ THUYẾT (VÒNG {roundNumber}) ===
Đề tài: {topicTitle}
Chế độ mục đích luận: {teleologyMode}

{roundContextPrompt}

HỒ SƠ BẰNG CHỨNG THỰC NGHIỆM ĐÃ XÁC THỰC:
{empiricalDossier?.FormattedDossierMarkdown}

TOÀN VĂN TRANH LUẬN CỦA CÁC VIỆN TRONG VÒNG NÀY:
{turnsSummary}

HÃY SOẠN THẢO BẢN LUẬN CƯƠNG SIÊU LÝ THUYẾT VỚI CÁC MỤC:
1. ĐỒNG THUẬN CỐT LÕI (Core Consensus):
2. CÁC NGHỊCH LÝ ĐÃ ĐƯỢC GIẢI TRỪ:
3. MÔ HÌNH NHẬN THỨC MỚI ĐƯỢC KIẾN TẠO:
4. BẰNG CHỨNG THỰC CHỨNG NÒNG CỐT [E#]:";
        }
    }
}
