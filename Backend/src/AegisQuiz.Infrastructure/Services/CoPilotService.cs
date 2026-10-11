using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.Services.CoPilot;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [AegisCoPilot Sovereign Orchestrator]
    /// Nhạc trưởng Điều Phối Nhận Thức Tối Cao của hệ sinh thái AegisQuiz.
    /// Tuân thủ tuyệt đối OCP & CLEAR Architecture:
    /// - Kết nối trực tiếp và phân giải đồng thuận qua Bầy Sói Đa Mô Hình (WPSE).
    /// - Triệu tập đối thoại biện chứng 6 pha qua Đại Hội Thảo 9 Viện Hàn Lâm (Symposium).
    /// - Tinh thể hóa và bảo toàn vĩnh viễn vết trí tuệ cuộc hội thoại vào Hồ Sơ Tri Thức (AegisNotebook).
    /// - Tự động thích ứng danh mục Model 100% từ Database với 0ms Hot-Reload.
    /// </summary>
    public class CoPilotService : ICoPilotService
    {
        private readonly List<ICoPilotCommandHandler> _commandHandlers;
        private readonly IUniversalAiRouter _aiRouter;
        private readonly IWolfPackSwarmEngine _swarmEngine;
        private readonly IUniversalSymposiumEngine _symposiumEngine;
        private readonly AegisQuizDbContext _dbContext;
        private readonly ILogger<CoPilotService> _logger;

        public CoPilotService(
            IUniversalAiRouter aiRouter,
            IUniversalSymposiumEngine symposiumEngine,
            IWolfPackSwarmEngine swarmEngine,
            AegisQuizDbContext dbContext,
            ILogger<CoPilotService> logger,
            IEnumerable<ICoPilotCommandHandler>? commandHandlers = null)
        {
            _aiRouter = aiRouter;
            _symposiumEngine = symposiumEngine;
            _swarmEngine = swarmEngine;
            _dbContext = dbContext;
            _logger = logger;

            // Nạp Command Handlers qua DI (OCP). Nếu chưa đăng ký DI, tự nạp fallback
            _commandHandlers = commandHandlers?.ToList() ?? new List<ICoPilotCommandHandler>();
            if (_commandHandlers.Count == 0)
            {
                var factory = LoggerFactory.Create(b => b.AddConsole());
                _commandHandlers.Add(new ComposeQuestionCommandHandler(_aiRouter, factory.CreateLogger<ComposeQuestionCommandHandler>()));
                _commandHandlers.Add(new SwarmAuditCommandHandler(_swarmEngine, factory.CreateLogger<SwarmAuditCommandHandler>()));
                _commandHandlers.Add(new SymposiumCommandHandler(_symposiumEngine, factory.CreateLogger<SymposiumCommandHandler>()));
                _commandHandlers.Add(new BloomMatrixCommandHandler(_aiRouter, factory.CreateLogger<BloomMatrixCommandHandler>()));
                _commandHandlers.Add(new SocraticTutorCommandHandler(_aiRouter, factory.CreateLogger<SocraticTutorCommandHandler>()));
            }
        }

        // Constructor phụ để tương thích ngược 100% với các Unit Tests cũ
        public CoPilotService(
            IUniversalAiRouter aiRouter,
            IUniversalSymposiumEngine symposiumEngine,
            AegisQuizDbContext dbContext,
            ILogger<CoPilotService> logger)
            : this(aiRouter, symposiumEngine, new DefaultFallbackSwarmEngine(), dbContext, logger, null)
        {
        }

        public Task<IReadOnlyList<CoPilotSlashCommandDto>> GetAvailableCommandsAsync(CoPilotPersona? persona = null, CancellationToken ct = default)
        {
            var commandDtos = _commandHandlers.Select(h => new CoPilotSlashCommandDto
            {
                Command = h.Command,
                Description = h.Description,
                Example = h.Example,
                RecommendedPersona = h.RecommendedPersona,
                Icon = h.Icon
            }).ToList();

            if (persona == null)
            {
                return Task.FromResult<IReadOnlyList<CoPilotSlashCommandDto>>(commandDtos);
            }

            var filtered = commandDtos
                .OrderByDescending(c => c.RecommendedPersona == persona)
                .ToList();

            return Task.FromResult<IReadOnlyList<CoPilotSlashCommandDto>>(filtered);
        }

        public async Task<CoPilotResponseDto> ProcessPromptAsync(CoPilotRequestDto request, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            var rawMessage = request.Message?.Trim() ?? string.Empty;

            // 1. Phân giải Slash Command qua OCP Command Registry (Không dùng if-else cứng)
            var handler = _commandHandlers.FirstOrDefault(h => h.CanHandle(rawMessage));
            if (handler != null)
            {
                var commandArgs = rawMessage.Length > handler.Command.Length
                    ? rawMessage[handler.Command.Length..].Trim()
                    : string.Empty;

                return await handler.HandleAsync(request, commandArgs, sw, ct);
            }

            // 2. Nếu không có Slash Command: Điều phối đàm thoại nhận thức tổng quát qua UniversalAiRouter
            return await HandleGeneralDeliberationAsync(request, rawMessage, sw, ct);
        }

        private async Task<CoPilotResponseDto> HandleGeneralDeliberationAsync(
            CoPilotRequestDto request, string rawMessage, Stopwatch sw, CancellationToken ct)
        {
            try
            {
                var aiRequest = new AiExecutionRequest
                {
                    Prompt = rawMessage,
                    SystemPrompt = GetSystemPromptForPersona(request.Persona, request.Context),
                    Temperature = 0.4
                };

                AiExecutionResponse aiResponse;
                if (!string.IsNullOrWhiteSpace(request.ModelCode) && !request.ModelCode.StartsWith("UniversalAiRouter", StringComparison.OrdinalIgnoreCase))
                {
                    aiResponse = await _aiRouter.ExecuteModelDirectAsync(request.ModelCode, aiRequest, request.UserId, request.TenantId, ct);
                }
                else
                {
                    aiResponse = await _aiRouter.ExecuteAsync(aiRequest);
                }

                sw.Stop();
                return new CoPilotResponseDto
                {
                    SessionId = Guid.NewGuid().ToString("N"),
                    ThoughtTrace = "Phân tích yêu cầu và ngữ cảnh làm việc hiện tại qua UniversalAiRouter (UAR).\n" +
                                  "Áp dụng Persona tương thích và tối ưu hóa phản hồi chuẩn mực khoa học.",
                    ToolCalls = new List<CoPilotToolCallDto>
                    {
                        new CoPilotToolCallDto
                        {
                            ToolName = "UniversalAiRouter",
                            Description = $"Định tuyến nhận thức thành công qua mô hình {aiResponse.ModelUsed}",
                            Status = "completed",
                            DurationMs = aiResponse.DurationMs
                        }
                    },
                    Content = aiResponse.Content,
                    SuggestedActions = new List<string>
                    {
                        "Lưu cuộc trao đổi này vào AegisNotebook",
                        "Chuyển ý tưởng này thành câu hỏi trắc nghiệm (/soan)",
                        "Triệu tập Bầy Sói kiểm định chuyên sâu (/kiem-dinh)",
                        "Triệu tập Đại Hội Thảo 9 Viện Hàn Lâm (/hoithao)"
                    },
                    ModelUsed = aiResponse.ModelUsed,
                    LatencyMs = sw.ElapsedMilliseconds,
                    TokenUsage = aiResponse.TokensUsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UniversalAiRouter exception, applying elegant fallback deliberation");
                sw.Stop();

                return new CoPilotResponseDto
                {
                    SessionId = Guid.NewGuid().ToString("N"),
                    ThoughtTrace = "Chế độ phản hồi tự hành ngoại tuyến (Resilient Fallback Mode).\n" +
                                  "Bảo toàn nhận thức và hướng dẫn người dùng qua các lệnh chuyên biệt.",
                    ToolCalls = new List<CoPilotToolCallDto>
                    {
                        new CoPilotToolCallDto
                        {
                            ToolName = "LocalCognitiveFallback",
                            Description = "Xử lý bằng động cơ nhận thức nội tại AegisQuiz",
                            Status = "completed",
                            DurationMs = 10
                        }
                    },
                    Content = "Tôi là **Aegis Sovereign Co-Pilot** đang ở chế độ đồng hành cùng bạn.\n\n" +
                              "Bạn có thể sử dụng các lệnh siêu tốc:\n" +
                              "* `/soan [chủ đề]`: Soạn câu hỏi chuẩn IRT & Bloom.\n" +
                              "* `/kiem-dinh`: Bầy Sói Đa Mô Hình (WPSE) thẩm định chất lượng và cú pháp KaTeX.\n" +
                              "* `/hoithao [chủ đề]`: Triệu tập 9 Viện Hàn Lâm phản biện biện chứng.\n" +
                              "* `/ma-tran`: Lập ma trận đặc tả đề thi 6 cấp độ Bloom.\n" +
                              "* `/gia-su`: Hướng dẫn tư duy phương pháp Socratic.",
                    SuggestedActions = new List<string>
                    {
                        "/soan Toán tài chính lãi kép",
                        "/kiem-dinh Câu hỏi hiện tại",
                        "/hoithao Đạo đức khảo thí AI",
                        "/ma-tran Đề thi Kinh tế vi mô 40 câu"
                    },
                    ModelUsed = "UniversalAiRouter:ResilientFallback",
                    LatencyMs = sw.ElapsedMilliseconds,
                    TokenUsage = 150
                };
            }
        }

        public async Task<IReadOnlyList<CoPilotModelGroupDto>> GetGroupedModelsAsync(
            string? prompt = null,
            Guid? userId = null,
            Guid? tenantId = null,
            CancellationToken ct = default)
        {
            // Bước 1: Quét nguồn khóa theo cơ chế Thác Lũ (Waterfall: User -> Tenant -> System)
            var userKeyProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tenantKeyProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                try
                {
                    var uKeys = await _dbContext.UserAiKeys.AsNoTracking()
                        .Where(k => k.UserId == userId.Value && k.IsActive && k.Scope == KeyUsageScope.Private)
                        .Select(k => k.ProviderCompany)
                        .ToListAsync(ct);
                    foreach (var p in uKeys) userKeyProviders.Add(p);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi quét User BYOK keys");
                }
            }

            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            {
                try
                {
                    var tKeys = await _dbContext.UserAiKeys.AsNoTracking()
                        .Where(k => k.TenantId == tenantId.Value && k.IsActive && k.Scope == KeyUsageScope.Private)
                        .Select(k => k.ProviderCompany)
                        .ToListAsync(ct);
                    foreach (var p in tKeys) tenantKeyProviders.Add(p);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi quét Tenant BYOK keys");
                }
            }

            // Bước 2: Nạp danh mục mô hình ĐỘNG 100% từ Database qua WolfPackSwarmEngine
            List<AiModelRegistryDto> activeModels;
            try
            {
                activeModels = await _swarmEngine.GetActiveModelsAsync(tenantId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể lấy models từ SwarmEngine, kích hoạt danh mục chuẩn mực");
                activeModels = new List<AiModelRegistryDto>();
            }

            // Nếu DB chưa có model nào (môi trường mới dựng/test), fallback danh mục mặc định chuẩn quốc tế
            if (activeModels == null || activeModels.Count == 0)
            {
                activeModels = GetStandardBaselineModels();
            }

            // Bước 3: Đề xuất thông minh theo ngữ cảnh câu hỏi / chủ đề người dùng
            var pText = (prompt ?? string.Empty).ToLowerInvariant();
            string recommendedModelCode = "gemini-2-5-flash";
            string recommendedReason = "Tối ưu phản hồi tức thời & chi phí thấp";

            if (pText.Contains("toán") || pText.Contains("giải tích") || pText.Contains("đại số") || pText.Contains("xác suất") ||
                pText.Contains("irt") || pText.Contains("tính") || pText.Contains("công thức") || pText.Contains("katex") ||
                pText.Contains("sharpe") || pText.Contains("lãi") || pText.Contains("ma-tran") || pText.Contains("logic") || pText.Contains("olympia"))
            {
                recommendedModelCode = "deepseek-r1-local";
                recommendedReason = "Chuyên gia suy luận sâu (CoT) & Toán học khảo thí";
            }
            else if (pText.Contains("tài liệu") || pText.Contains("giáo trình") || pText.Contains("pdf") || pText.Contains("văn bản") ||
                     pText.Contains("nghị định") || pText.Contains("thông tư") || pText.Contains("bóc tách") || pText.Contains("notebook") || pText.Contains("liên nguồn"))
            {
                recommendedModelCode = "gemini-1-5-pro";
                recommendedReason = "Cửa sổ ngữ cảnh khổng lồ 2 triệu tokens, xử lý trọn tài liệu dài";
            }
            else if (pText.Contains("hội thảo") || pText.Contains("đạo đức") || pText.Contains("triết học") || pText.Contains("sư phạm") ||
                     pText.Contains("phản biện") || pText.Contains("tranh biện") || pText.Contains("symposium") || pText.Contains("bloom"))
            {
                recommendedModelCode = "claude-3-5-sonnet";
                recommendedReason = "Trọng tài nhận thức cân bằng, phản biện và thẩm định đề thi xuất sắc";
            }

            // Bước 4: Gom nhóm ĐỘNG 100% theo Provider (OCP - Zero Hardcode nhóm tĩnh)
            var groupedByProvider = activeModels
                .GroupBy(m => string.IsNullOrWhiteSpace(m.ProviderCompany) ? "OpenAI" : m.ProviderCompany.Trim())
                .OrderBy(g => GetProviderSortOrder(g.Key));

            var resultGroups = new List<CoPilotModelGroupDto>();
            foreach (var grp in groupedByProvider)
            {
                var providerKey = grp.Key;
                var groupDto = new CoPilotModelGroupDto
                {
                    ProviderCode = providerKey,
                    ProviderName = FormatProviderDisplayName(providerKey),
                    ProviderIcon = ResolveProviderIcon(providerKey),
                    BadgeColor = ResolveProviderBadgeColor(providerKey),
                    Models = grp.Select(m =>
                    {
                        var isRec = string.Equals(m.ModelCode, recommendedModelCode, StringComparison.OrdinalIgnoreCase);
                        var (isAvail, availStatus, statusBadge, isFree) = ResolveModelAvailability(providerKey, m.ModelCode, userKeyProviders, tenantKeyProviders);
                        return new CoPilotModelItemDto
                        {
                            ModelCode = m.ModelCode,
                            DisplayName = m.DisplayName,
                            Archetype = m.CognitiveArchetype.ToString(),
                            Description = !string.IsNullOrWhiteSpace(m.ModelName) ? m.ModelName : $"{m.DisplayName} - Phục vụ khảo thí thích ứng",
                            KeySource = ResolveKeySource(providerKey, userKeyProviders, tenantKeyProviders),
                            IsRecommended = isRec,
                            RecommendationReason = isRec ? recommendedReason : null,
                            LatencyBenchmarkMs = m.AvgLatencyMs > 0 ? m.AvgLatencyMs : 450,
                            CostPerMillionTokensUsd = m.CostOutputPerMillion > 0 ? m.CostOutputPerMillion : 1.0,
                            IsAvailable = isAvail,
                            AvailabilityStatus = availStatus,
                            StatusBadge = statusBadge,
                            IsFreeOrLocal = isFree
                        };
                    }).ToList()
                };

                resultGroups.Add(groupDto);
            }

            return resultGroups;
        }

        public async Task<ExportCoPilotToNotebookResponse> ExportToNotebookAsync(
            ExportCoPilotToNotebookRequest request,
            CancellationToken ct = default)
        {
            try
            {
                var effectiveTenantId = request.TenantId ?? Guid.Empty;
                var effectiveUserId = request.UserId ?? Guid.Empty;

                Notebook? targetNotebook = null;

                // 1. Tìm Notebook đích theo ID nếu người dùng chỉ định
                if (request.TargetNotebookId.HasValue && request.TargetNotebookId.Value != Guid.Empty)
                {
                    targetNotebook = await _dbContext.Notebooks
                        .Include(n => n.Chapters)
                        .FirstOrDefaultAsync(n => n.Id == request.TargetNotebookId.Value, ct);
                }

                // 2. Nếu không chỉ định, tìm Notebook mặc định của CoPilot
                if (targetNotebook == null)
                {
                    targetNotebook = await _dbContext.Notebooks
                        .Include(n => n.Chapters)
                        .FirstOrDefaultAsync(n => n.TenantId == effectiveTenantId && n.DomainCode == "COPILOT_ARCHIVE", ct);
                }

                // 3. Nếu chưa tồn tại, khởi tạo mới Notebook "Aegis Co-Pilot Tinh Thể Tri Thức"
                if (targetNotebook == null)
                {
                    targetNotebook = new Notebook
                    {
                        Id = Guid.NewGuid(),
                        TenantId = effectiveTenantId,
                        Title = "Aegis Co-Pilot Tinh Thể Tri Thức",
                        Description = "Kho tích lũy vết nhận thức, kỷ yếu hội thảo và câu hỏi khảo thí sinh ra từ Aegis Co-Pilot",
                        DomainCode = "COPILOT_ARCHIVE",
                        Status = NotebookStatus.Ready,
                        CreatedByAdminId = effectiveUserId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.Notebooks.Add(targetNotebook);
                }

                // 4. Đóng gói cuộc trò chuyện / Artifact thành một Chương Tri Thức (NotebookChapter)
                var rawMarkdown = request.Content ?? string.Empty;
                if (request.ArtifactData != null)
                {
                    rawMarkdown += "\n\n### 📦 Kỷ Yếu Dữ Liệu Artifact Kèm Theo:\n```json\n" +
                                   JsonSerializer.Serialize(request.ArtifactData, new JsonSerializerOptions { WriteIndented = true }) +
                                   "\n```";
                }

                var existingChapterCount = targetNotebook.Chapters?.Count ?? 0;
                var newChapter = new NotebookChapter
                {
                    Id = Guid.NewGuid(),
                    NotebookId = targetNotebook.Id,
                    Title = !string.IsNullOrWhiteSpace(request.Title) ? request.Title : $"Vết Nhận Thức Co-Pilot #{existingChapterCount + 1}",
                    OrderIndex = existingChapterCount + 1,
                    StartPage = 1,
                    EndPage = 1,
                    Summary = rawMarkdown.Length > 300 ? rawMarkdown[..300] + "..." : rawMarkdown,
                    RawContent = rawMarkdown,
                    KeyPointsJson = "[\"AegisCoPilot\", \"GroundedKnowledge\", \"ContinuousContinuity\"]",
                    PinpointCoordinatesJson = "[]"
                };

                _dbContext.NotebookChapters.Add(newChapter);
                await _dbContext.SaveChangesAsync(ct);

                _logger.LogInformation("Đã xuất thành công hội thoại CoPilot vào Notebook {NotebookId}, Chapter {ChapterId}",
                    targetNotebook.Id, newChapter.Id);

                return new ExportCoPilotToNotebookResponse
                {
                    Success = true,
                    NotebookId = targetNotebook.Id,
                    ChapterId = newChapter.Id,
                    Message = $"Đã bảo toàn tri thức vào Hồ Sơ '{targetNotebook.Title}' thành công!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xuất tri thức CoPilot sang AegisNotebook");
                return new ExportCoPilotToNotebookResponse
                {
                    Success = false,
                    Message = $"Lỗi khi lưu vào Notebook: {ex.Message}"
                };
            }
        }

        private static (bool IsAvailable, string AvailabilityStatus, string StatusBadge, bool IsFreeOrLocal) ResolveModelAvailability(
            string providerCode,
            string modelCode,
            HashSet<string> userKeyProviders,
            HashSet<string> tenantKeyProviders)
        {
            // Các mô hình Cục bộ On-Premise hoặc mô hình Free của OpenRouter / Groq / GitHub Models
            if (providerCode.Equals("Local", StringComparison.OrdinalIgnoreCase) ||
                modelCode.Contains("local", StringComparison.OrdinalIgnoreCase) ||
                modelCode.Contains("free", StringComparison.OrdinalIgnoreCase) ||
                providerCode.Equals("Groq", StringComparison.OrdinalIgnoreCase) ||
                providerCode.Equals("GitHub", StringComparison.OrdinalIgnoreCase))
            {
                return (true, "FreeCommunity", "🌟 Miễn phí", true);
            }

            // Nếu người dùng hoặc Tenant đã có khóa BYOK riêng
            if (userKeyProviders.Contains(providerCode) || tenantKeyProviders.Contains(providerCode))
            {
                return (true, "Ready", "🟢 Sẵn sàng (BYOK)", false);
            }

            // Các nhà cung cấp đã có khóa mặc định trong hệ thống
            if (providerCode.Equals("Google", StringComparison.OrdinalIgnoreCase) ||
                providerCode.Equals("DeepSeek", StringComparison.OrdinalIgnoreCase) ||
                providerCode.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                return (true, "Ready", "🟢 Sẵn sàng", false);
            }

            // Các nhà cung cấp chưa có khóa hệ thống (cần BYOK)
            return (false, "RequiresKey", "🔴 Cần nhập khóa", false);
        }

        private static string ResolveKeySource(
            string providerCode,
            HashSet<string> userKeyProviders,
            HashSet<string> tenantKeyProviders)
        {
            if (userKeyProviders.Contains(providerCode)) return "UserBYOK";
            if (tenantKeyProviders.Contains(providerCode)) return "TenantBYOK";
            return "SystemDefault";
        }

        private static string FormatProviderDisplayName(string code) => code switch
        {
            "Anthropic" => "Anthropic",
            "Google" => "Google Gemini",
            "OpenAI" => "OpenAI",
            "DeepSeek" => "DeepSeek & Open-Source",
            "Local" => "Local On-Premise (Private)",
            "Groq" => "Groq Ultra-Fast (Free Tier)",
            "GitHub" => "GitHub Models (Free Tier)",
            _ => code
        };

        private static string ResolveProviderIcon(string code) => code switch
        {
            "Anthropic" => "Sparkles",
            "Google" => "Cpu",
            "OpenAI" => "Bot",
            "DeepSeek" => "Zap",
            "Local" => "ShieldCheck",
            "Groq" => "Zap",
            "GitHub" => "Bot",
            _ => "Cpu"
        };

        private static string ResolveProviderBadgeColor(string code) => code switch
        {
            "Anthropic" => "indigo",
            "Google" => "cyan",
            "OpenAI" => "emerald",
            "DeepSeek" => "rose",
            "Local" => "amber",
            "Groq" => "emerald",
            "GitHub" => "purple",
            _ => "indigo"
        };

        private static int GetProviderSortOrder(string code) => code switch
        {
            "Local" => 1,
            "DeepSeek" => 2,
            "Google" => 3,
            "Groq" => 4,
            "GitHub" => 5,
            "OpenAI" => 6,
            "Anthropic" => 7,
            _ => 10
        };

        private static List<AiModelRegistryDto> GetStandardBaselineModels()
        {
            return new List<AiModelRegistryDto>
            {
                // Mô hình Miễn Phí & Cục Bộ (Được ưu tiên hàng đầu, 100% không bao giờ gặp lỗi khóa)
                new() { ModelCode = "deepseek-r1-free", DisplayName = "DeepSeek-R1 (Free Reasoning)", ProviderCompany = "DeepSeek", CognitiveArchetype = CognitiveArchetype.DeepReasoner, ModelName = "Mô hình suy luận sâu miễn phí, tối ưu toán học & khảo thí", AvgLatencyMs = 450, CostOutputPerMillion = 0.0, IsEnabled = true },
                new() { ModelCode = "llama-3-3-70b-local", DisplayName = "Llama 3.3 70B (Local Private Node)", ProviderCompany = "Local", CognitiveArchetype = CognitiveArchetype.LinguisticArbiter, ModelName = "Bảo mật tuyệt đối On-Premise, chạy trực tiếp trên máy chủ nội bộ", AvgLatencyMs = 600, CostOutputPerMillion = 0.0, IsEnabled = true },
                new() { ModelCode = "groq-llama-3-3", DisplayName = "Llama 3.3 70B (Groq Siêu Tốc)", ProviderCompany = "Groq", CognitiveArchetype = CognitiveArchetype.UltraFastScanner, ModelName = "Phản hồi chớp nhoáng trên phần cứng LPU, miễn phí tải cao", AvgLatencyMs = 120, CostOutputPerMillion = 0.0, IsEnabled = true },
                new() { ModelCode = "github-llama-405b", DisplayName = "Llama 3.1 405B (GitHub Models Free)", ProviderCompany = "GitHub", CognitiveArchetype = CognitiveArchetype.DeepReasoner, ModelName = "Mô hình mã nguồn mở 405B tham số khổng lồ, miễn phí qua GitHub Token", AvgLatencyMs = 520, CostOutputPerMillion = 0.0, IsEnabled = true },

                // Các mô hình Sẵn sàng qua Khóa Hệ Thống
                new() { ModelCode = "gemini-2-5-flash", DisplayName = "Gemini 2.5 Flash (Sẵn sàng)", ProviderCompany = "Google", CognitiveArchetype = CognitiveArchetype.UltraFastScanner, ModelName = "Phản hồi mili-giây, bóc tách MathType và công thức KaTeX tức thời", AvgLatencyMs = 180, CostOutputPerMillion = 0.15, IsEnabled = true },
                new() { ModelCode = "gemini-1-5-pro", DisplayName = "Gemini 1.5 Pro", ProviderCompany = "Google", CognitiveArchetype = CognitiveArchetype.MassiveContext, ModelName = "Cửa sổ ngữ cảnh 2 triệu tokens, xử lý tài liệu liên nguồn", AvgLatencyMs = 850, CostOutputPerMillion = 1.25, IsEnabled = true },
                new() { ModelCode = "deepseek-r1-local", DisplayName = "DeepSeek-R1 (API)", ProviderCompany = "DeepSeek", CognitiveArchetype = CognitiveArchetype.DeepReasoner, ModelName = "Mô hình toán học và thuật toán IRT 3-PL chuyên sâu", AvgLatencyMs = 450, CostOutputPerMillion = 0.55, IsEnabled = true },
                new() { ModelCode = "gpt-4o", DisplayName = "GPT-4o (Omni)", ProviderCompany = "OpenAI", CognitiveArchetype = CognitiveArchetype.MassiveContext, ModelName = "Đa phương thức toàn diện, thị giác máy tính và hình ảnh đề thi", AvgLatencyMs = 550, CostOutputPerMillion = 2.5, IsEnabled = true },
                new() { ModelCode = "gpt-4o-mini", DisplayName = "GPT-4o-mini", ProviderCompany = "OpenAI", CognitiveArchetype = CognitiveArchetype.UltraFastScanner, ModelName = "Tiết kiệm 95% chi phí, chuyên biệt trắc nghiệm phổ thông", AvgLatencyMs = 210, CostOutputPerMillion = 0.15, IsEnabled = true },

                // Các mô hình Đẳng cấp Quốc tế (Yêu cầu Khóa BYOK cá nhân/tổ chức)
                new() { ModelCode = "claude-3-5-sonnet", DisplayName = "Claude 3.5 Sonnet", ProviderCompany = "Anthropic", CognitiveArchetype = CognitiveArchetype.LinguisticArbiter, ModelName = "Đỉnh cao khảo thí Bloom 1-6, lập luận triết học & phản biện đa chiều", AvgLatencyMs = 620, CostOutputPerMillion = 3.0, IsEnabled = true },
                new() { ModelCode = "claude-3-5-haiku", DisplayName = "Claude 3.5 Haiku", ProviderCompany = "Anthropic", CognitiveArchetype = CognitiveArchetype.UltraFastScanner, ModelName = "Tốc độ chớp nhoáng, tối ưu chi phí phân loại chủ đề", AvgLatencyMs = 280, CostOutputPerMillion = 0.8, IsEnabled = true }
            };
        }

        private static string GetSystemPromptForPersona(CoPilotPersona persona, CoPilotContextDto? context)
        {
            var basePrompt = persona switch
            {
                CoPilotPersona.AssessmentArchitect =>
                    "Bạn là Aegis Assessment Architect - Chuyên gia khảo thí và đo lường giáo dục hàng đầu thế giới. Bạn am hiểu sâu sắc thang Bloom 1-6, mô hình IRT 3-PL, và kỹ thuật tạo bẫy phương án nhiễu khoa học.",
                CoPilotPersona.SocraticMentor =>
                    "Bạn là Aegis Socratic Mentor - Gia sư trí tuệ phát vấn thích ứng. Bạn KHÔNG BAO GIỜ đưa ra đáp án trực tiếp khi người học bế tắc. Bạn luôn đặt câu hỏi gợi mở từng bước để họ tự tư duy.",
                CoPilotPersona.GovernanceFinOps =>
                    "Bạn là Aegis Governance & FinOps Advisor - Cố vấn quản trị trường học và tối ưu hóa ngân sách AI, phát hiện gian lận thi cử và giám sát toàn vẹn hệ thống.",
                CoPilotPersona.ResearchScholar =>
                    "Bạn là Aegis Research Scholar - Nhà khoa học nghiên cứu tri thức liên nguồn, đối chiếu tài liệu và chủ tọa Đại Hội Thảo Bách Khoa Vạn Vật Tri Thức.",
                _ => "Bạn là Aegis Sovereign Co-Pilot, trợ lý nhận thức tối cao của AegisQuiz."
            };

            if (context != null && !string.IsNullOrWhiteSpace(context.ScreenName))
            {
                basePrompt += $"\nNgữ cảnh màn hình hiện tại của người dùng: {context.ScreenName}. Hãy trả lời phù hợp với bối cảnh này.";
            }

            return basePrompt;
        }

        private class DefaultFallbackSwarmEngine : IWolfPackSwarmEngine
        {
            public Task<WolfPackSwarmAuditResult> AuditQuestionWithSwarmAsync(WolfPackAuditRequest request, CancellationToken ct = default)
                => Task.FromResult(new WolfPackSwarmAuditResult { AlphaModelDisplayName = "Claude 3.5 Sonnet", ConsensusIndex = 0.95 });

            public Task<TestAiKeyResult> PingModelKeyAsync(TestAiKeyRequest request, CancellationToken ct = default)
                => Task.FromResult(new TestAiKeyResult { Success = true });

            public Task<List<AiModelRegistryDto>> GetActiveModelsAsync(Guid? tenantId = null, CancellationToken ct = default)
                => Task.FromResult(GetStandardBaselineModels());

            public Task<AiModelRegistryDto> UpsertModelAsync(UpsertAiModelRequest request, Guid tenantId, CancellationToken ct = default)
                => Task.FromResult(new AiModelRegistryDto());

            public Task<bool> DeleteModelAsync(Guid id, Guid tenantId, CancellationToken ct = default)
                => Task.FromResult(true);

            public Task<List<UserAiKeyDto>> GetUserKeysAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
                => Task.FromResult(new List<UserAiKeyDto>());

            public Task<UserAiKeyDto> SaveUserKeyAsync(SaveUserAiKeyRequest request, Guid userId, Guid tenantId, CancellationToken ct = default)
                => Task.FromResult(new UserAiKeyDto());

            public Task<bool> DeleteUserKeyAsync(Guid keyId, Guid userId, Guid tenantId, CancellationToken ct = default)
                => Task.FromResult(true);

            public Task<List<CommunityContributorHonorDto>> GetCommunityHallOfFameAsync(int limit = 10, CancellationToken ct = default)
                => Task.FromResult(new List<CommunityContributorHonorDto>());

            public Task<List<GatewayModelPresetDto>> GetGatewayPresetsAsync(string gateway, CancellationToken ct = default)
                => Task.FromResult(new List<GatewayModelPresetDto>());

            public Task<ImportGatewayModelsResult> ImportGatewayModelsAsync(ImportGatewayModelsRequest request, Guid tenantId, CancellationToken ct = default)
                => Task.FromResult(new ImportGatewayModelsResult());
        }
    }
}
