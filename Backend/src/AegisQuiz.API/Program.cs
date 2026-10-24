using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.AI;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.Excel;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.API.Hubs;
using AegisQuiz.API.Security;
using AegisQuiz.API.Services;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Arena;
using Microsoft.AspNetCore.Authorization;
using Serilog;

// ===== [World-Class Upgrade] STRUCTURED LOGGING (học từ quiz-service) =====
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .Enrich.FromLogContext());

// ===== DEPENDENCY INJECTION =====
// Database (PostgreSQL với InMemory Fallback thông minh)
var pgConn = builder.Configuration.GetConnectionString("DefaultConnection");
bool usePostgres = !string.IsNullOrWhiteSpace(pgConn) && !pgConn.Contains("CHANGE_ME");

builder.Services.AddDbContext<AegisQuizDbContext>(options =>
{
    if (usePostgres)
    {
        options.UseNpgsql(pgConn);
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }
    else
    {
        options.UseInMemoryDatabase("AegisQuizDb");
    }
});

// Redis Cache (với In-Memory Distributed Cache Fallback)
var redisConn = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConn) && redisConn != "localhost:6379")
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
        options.InstanceName = "AegisQuiz_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// [FIX CRITICAL] IHttpClientFactory — Tránh Socket Exhaustion khi gọi Gemini
// new HttpClient() trong mỗi request = memory leak trong production!
builder.Services.AddHttpClient("gemini", client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout     = TimeSpan.FromSeconds(45);
    client.DefaultRequestHeaders.Add("User-Agent", "AegisQuiz/2.0");
});

// IRT/CAT Engine client (Phase C)
builder.Services.AddHttpClient("irt-service", client =>
{
    var irtUrl = builder.Configuration["IrtService:BaseUrl"] ?? "http://localhost:8001";
    client.BaseAddress = new Uri(irtUrl);
    client.Timeout     = TimeSpan.FromSeconds(30);
});

// Services (S.O.L.I.D Interface bindings)
builder.Services.AddSingleton<IVietnameseTextCorrectionService, VietnameseTextCorrectionService>();
builder.Services.AddSingleton<ISmartOptionShufflerService, SmartOptionShufflerService>();
builder.Services.AddSingleton<ITaxonomyEngine, UniversalCoordinateTaxonomyEngine>();
builder.Services.AddScoped<IExcelParserService, ExcelParserService>();
// [Universal Excel Export Engine — DTaaS]
builder.Services.AddSingleton<IExcelExportStrategy, AegisQuiz.Infrastructure.Excel.Strategies.NativeIsomorphicExportStrategy>();
builder.Services.AddSingleton<IExcelExportStrategy, AegisQuiz.Infrastructure.Excel.Strategies.MoetGdpt2025ExportStrategy>();
builder.Services.AddSingleton<IExcelExportStrategy, AegisQuiz.Infrastructure.Excel.Strategies.AzotaCompatibleExportStrategy>();
builder.Services.AddSingleton<IExcelExportStrategy, AegisQuiz.Infrastructure.Excel.Strategies.CustomTemplateExportStrategy>();
builder.Services.AddScoped<IUniversalExcelExportService, AegisQuiz.Infrastructure.Excel.UniversalExcelExportService>();
// [Universal Exam Transpiler Engine — EdTech International Standards (Moodle, Canvas QTI, GIFT)]
builder.Services.AddSingleton<ITranspilerExportStrategy, AegisQuiz.Infrastructure.Transpiler.MoodleXmlExportStrategy>();
builder.Services.AddSingleton<ITranspilerExportStrategy, AegisQuiz.Infrastructure.Transpiler.MoodleGiftExportStrategy>();
builder.Services.AddSingleton<ITranspilerExportStrategy, AegisQuiz.Infrastructure.Transpiler.Qti30PackageExportStrategy>();
builder.Services.AddScoped<IUniversalExamTranspilerService, AegisQuiz.Infrastructure.Transpiler.UniversalExamTranspilerService>();
builder.Services.AddScoped<IDocxParserService, DocxParserService>();
builder.Services.AddScoped<IDocxStateMachineClassifier, AegisQuiz.Infrastructure.Services.Docx.DocxStateMachineClassifierService>();
builder.Services.AddScoped<IDynamicDomainContextService, DynamicDomainContextService>();
builder.Services.AddScoped<IPdfExtractorService, PdfParserService>();
builder.Services.AddScoped<IExamDeduplicationEngine, ExamDeduplicationEngine>();
builder.Services.AddScoped<IIrtClientService, IrtClientService>();
builder.Services.AddScoped<AegisQuiz.Application.Interfaces.IDatabasePatcher, AegisQuiz.Infrastructure.Services.DatabasePatcher>();

// [FIX P1] IMemoryCache — dùng cho GeminiSolverService (TTL eviction thay static dict)
builder.Services.AddMemoryCache();

// [FIX] GeminiGradingService dùng IHttpClientFactory (không còn new HttpClient)
builder.Services.AddScoped<IGeminiGradingService, GeminiGradingService>();
builder.Services.AddScoped<IGeminiMentorService, GeminiMentorService>();
builder.Services.AddScoped<IGeminiSolverService>(sp =>
    new GeminiSolverService(
        builder.Configuration["Gemini:ApiKey"] ?? "",
        sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
        sp.GetService<IVisionMathService>()));

// [Kịch bản B] AI Question Generator từ tài liệu PDF / Word / Text
builder.Services.AddScoped<IAiQuestionGeneratorService>(sp =>
    new AiQuestionGeneratorService(
        builder.Configuration["Gemini:ApiKey"] ?? "",
        sp.GetRequiredService<IHttpClientFactory>(),
        sp.GetRequiredService<IPdfExtractorService>(),
        sp.GetRequiredService<IDocxParserService>(),
        sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>()));

// [Universal Ingestion Engine Phase 4] AI Rule Autopilot (Gemini 1.5 Flash + Local Heuristic Sniffer) & Shadow Testing Pipeline
builder.Services.AddScoped<IAiRuleAutopilotService, GeminiRuleAutopilotService>();
builder.Services.AddScoped<IShadowTestingService, ShadowTestingService>();

// [Cognitive Ingestion Grammar] Động cơ bóc tách câu hỏi động — singleton dùng chung với DocxStateMachineClassifier,
// được gắn ScopeFactory để đồng bộ quy tắc với PostgreSQL (bảng QuestionParsingRules) khi khởi động.
builder.Services.AddSingleton<IDynamicQuestionIngestionEngine>(sp =>
    DynamicQuestionIngestionEngine.Instance.AttachInfrastructure(
        sp.GetRequiredService<IServiceScopeFactory>(),
        sp.GetRequiredService<ILogger<DynamicQuestionIngestionEngine>>()));

// [Universal Dynamic Math Dictionary & Locale Normalizer]
builder.Services.AddSingleton<IDynamicMathDictionaryService>(_ => DynamicMathDictionaryService.Instance);
builder.Services.AddSingleton<AegisQuiz.Application.Interfaces.IMathLocaleNormalizer>(_ => AegisQuiz.Infrastructure.Services.Docx.MathLocaleNormalizer.Default);

// [Hybrid L1+L2 Cache & Universal AI Router (OpenAdapter Engine Architecture)]
builder.Services.AddScoped<ICacheService, HybridCacheService>();
builder.Services.AddSingleton<AegisQuiz.Infrastructure.AI.Adapters.IAiProviderAdapter, AegisQuiz.Infrastructure.AI.Adapters.GoogleGeminiAdapter>();
builder.Services.AddSingleton<AegisQuiz.Infrastructure.AI.Adapters.IAiProviderAdapter, AegisQuiz.Infrastructure.AI.Adapters.AnthropicClaudeAdapter>();
builder.Services.AddSingleton<AegisQuiz.Infrastructure.AI.Adapters.IAiProviderAdapter, AegisQuiz.Infrastructure.AI.Adapters.LocalOllamaAdapter>();
builder.Services.AddSingleton<AegisQuiz.Infrastructure.AI.Adapters.IAiProviderAdapter, AegisQuiz.Infrastructure.AI.Adapters.ReplicateAdapter>();
builder.Services.AddSingleton<AegisQuiz.Infrastructure.AI.Adapters.OpenAiCompatibleAdapter>();
builder.Services.AddSingleton<IUniversalAiRouter, UniversalAiRouter>();
builder.Services.AddScoped<IVisionMathService, VisionMathService>();
builder.Services.AddScoped<IMultiAgentCommitteeEngine, MultiAgentCommitteeEngine>();

// [AegisCoPilot OCP Command Strategy Handlers & Orchestrator]
builder.Services.AddScoped<AegisQuiz.Infrastructure.Services.CoPilot.ICoPilotCommandHandler, AegisQuiz.Infrastructure.Services.CoPilot.ComposeQuestionCommandHandler>();
builder.Services.AddScoped<AegisQuiz.Infrastructure.Services.CoPilot.ICoPilotCommandHandler, AegisQuiz.Infrastructure.Services.CoPilot.SwarmAuditCommandHandler>();
builder.Services.AddScoped<AegisQuiz.Infrastructure.Services.CoPilot.ICoPilotCommandHandler, AegisQuiz.Infrastructure.Services.CoPilot.SymposiumCommandHandler>();
builder.Services.AddScoped<AegisQuiz.Infrastructure.Services.CoPilot.ICoPilotCommandHandler, AegisQuiz.Infrastructure.Services.CoPilot.BloomMatrixCommandHandler>();
builder.Services.AddScoped<AegisQuiz.Infrastructure.Services.CoPilot.ICoPilotCommandHandler, AegisQuiz.Infrastructure.Services.CoPilot.SocraticTutorCommandHandler>();
builder.Services.AddScoped<ICoPilotService, AegisQuiz.Infrastructure.Services.CoPilotService>();

// [AegisAI Dynamic Adaptive Wolf Pack Swarm & Protocols]
builder.Services.AddSingleton<ICognitiveWorkingMemory, AegisQuiz.Infrastructure.AI.Memory.InMemoryCognitiveWorkingMemory>();
builder.Services.AddSingleton<ICognitiveHandoffStrategy, AegisQuiz.Infrastructure.AI.Handoff.CoTToFastScannerHandoffStrategy>();
builder.Services.AddSingleton<ICognitiveHandoffStrategy, AegisQuiz.Infrastructure.AI.Handoff.UniversalCoTPreservationStrategy>();
builder.Services.AddSingleton<ICognitiveHandoffDispatcher, AegisQuiz.Infrastructure.AI.Handoff.CognitiveHandoffDispatcher>();
builder.Services.AddSingleton<AegisQuiz.Infrastructure.AI.Protocols.IModelProtocolDriver, AegisQuiz.Infrastructure.AI.Protocols.OpenAiCompatibleProtocolDriver>();
builder.Services.AddSingleton<AegisQuiz.Infrastructure.AI.Protocols.IModelProtocolDriver, AegisQuiz.Infrastructure.AI.Protocols.GoogleGenerativeAiProtocolDriver>();
builder.Services.AddSingleton<AegisQuiz.Infrastructure.AI.Protocols.IModelProtocolDriver, AegisQuiz.Infrastructure.AI.Protocols.AnthropicMessagesProtocolDriver>();
builder.Services.AddScoped<IWolfPackSwarmEngine, WolfPackSwarmEngine>();
builder.Services.AddScoped<ISeatModelInvoker, AegisQuiz.Infrastructure.AI.Invokers.SeatModelInvoker>();
builder.Services.AddScoped<IWolfpackRoleInvoker, AegisQuiz.Infrastructure.AI.Swarm.WolfpackRoleInvoker>();
builder.Services.AddSingleton<ISymposiumLiveBroadcaster, AegisQuiz.API.Services.SignalRSymposiumBroadcaster>();
builder.Services.AddSingleton<ISymposiumPromptComposer, AegisQuiz.Infrastructure.AI.Prompt.SymposiumPromptComposer>();
builder.Services.AddSingleton<ICognitiveLayerPlugin, AegisQuiz.Infrastructure.AI.Prompt.Plugins.BayesianBeliefRevisionPlugin>();
builder.Services.AddSingleton<ICognitiveLayerPlugin, AegisQuiz.Infrastructure.AI.Prompt.Plugins.PeirceanAbductiveGenesisPlugin>();
builder.Services.AddSingleton<ICognitiveLayerPlugin, AegisQuiz.Infrastructure.AI.Prompt.Plugins.FormalFalsifiabilityPlugin>();
builder.Services.AddScoped<IEpistemicGenesisEngine, AegisQuiz.Infrastructure.AI.Genesis.EpistemicGenesisEngine>();
builder.Services.AddScoped<IEpistemicIntentRouter, AegisQuiz.Infrastructure.AI.Selection.FastEpistemicIntentRouter>();
builder.Services.AddScoped<ICouncilSelectionStrategy, AegisQuiz.Infrastructure.AI.Selection.DiverseEpistemicCouncilSelectionStrategy>();
builder.Services.AddScoped<ISpatioTemporalRoleAllocator, AegisQuiz.Infrastructure.AI.Selection.SpatioTemporalRoleAllocator>();
builder.Services.AddScoped<IPreconfiguredCouncilProvider, AegisQuiz.Infrastructure.AI.Selection.PreconfiguredCouncilProvider>();
builder.Services.AddScoped<ISymposiumTeleologyResolver, AegisQuiz.Infrastructure.AI.Teleology.SymposiumTeleologyResolver>();
builder.Services.AddScoped<ICouncilContributionGenerator, AegisQuiz.Infrastructure.AI.Contributions.CouncilContributionGenerator>();
builder.Services.AddScoped<ISymposiumOutcomeBuilder, AegisQuiz.Infrastructure.AI.Outcomes.SymposiumOutcomeBuilder>();
builder.Services.AddScoped<ISymposiumResolutionComposer, AegisQuiz.Infrastructure.AI.Resolution.SymposiumResolutionComposer>();
builder.Services.AddScoped<ISymposiumSecretariat, AegisQuiz.Infrastructure.AI.Secretariat.SymposiumSecretariatCouncil>();
builder.Services.AddScoped<IUniversalSymposiumEngine, AegisQuiz.Infrastructure.AI.UniversalSymposiumEngine>();
builder.Services.AddScoped<ISymposiumJudge, AegisQuiz.Infrastructure.AI.Judges.IndependentSymposiumJudge>();
builder.Services.AddScoped<IPromptForgeAssistantService, AegisQuiz.Infrastructure.AI.Prompt.PromptForgeAssistantService>();
builder.Services.AddScoped<IEvidenceSource, AegisQuiz.Infrastructure.AI.Research.Sources.CuratedOfflineEvidenceSource>();
builder.Services.AddScoped<IEvidenceSource, AegisQuiz.Infrastructure.AI.Research.Sources.ScholarlyApiEvidenceSource>();
builder.Services.AddScoped<IEpistemicDeepResearchScout, AegisQuiz.Infrastructure.AI.Research.EpistemicDeepResearchScout>();

// [Epoch 2-5: Enterprise Fortress, CognitiveOS, FSRS & Community Economy]
builder.Services.AddScoped<IExamAuditService, ExamAuditService>();
builder.Services.AddScoped<IExamFortressPrewarmService, ExamFortressPrewarmService>();
builder.Services.AddScoped<IProctorService, ProctorService>();
builder.Services.AddScoped<IAegisCoinService, AegisCoinService>();
builder.Services.AddScoped<IPeerChallengeService, PeerChallengeService>();
builder.Services.AddSingleton<IFsrsEngine, AegisQuiz.Application.Services.FsrsEngine>();
builder.Services.AddScoped<ICognitiveScaffoldService, CognitiveScaffoldService>();
builder.Services.AddScoped<IDktEngine, DktEngine>();
builder.Services.AddScoped<IKnowledgeGraphService, KnowledgeGraphService>();
builder.Services.AddScoped<IPedagogicalRagService, PedagogicalRagService>();
builder.Services.AddScoped<IContentModerationService, ContentModerationService>();
builder.Services.AddScoped<INotebookService, NotebookProcessingService>();

// [P1 Supreme Scoring Sovereign] Đăng ký IQuestionEvaluatorStrategy OCP & Dispatcher
builder.Services.AddSingleton<IQuestionEvaluatorStrategy, AegisQuiz.Application.Services.Scoring.SingleChoiceEvaluatorStrategy>();
builder.Services.AddSingleton<IQuestionEvaluatorStrategy, AegisQuiz.Application.Services.Scoring.MultiChoiceEvaluatorStrategy>();
builder.Services.AddSingleton<IQuestionEvaluatorStrategy, AegisQuiz.Application.Services.Scoring.MultiTrueFalseEvaluatorStrategy>();
builder.Services.AddSingleton<IQuestionEvaluatorStrategy, AegisQuiz.Application.Services.Scoring.ShortAnswerEvaluatorStrategy>();
builder.Services.AddSingleton<IQuestionEvaluatorStrategy, AegisQuiz.Application.Services.Scoring.OrderingEvaluatorStrategy>();
builder.Services.AddSingleton<IQuestionEvaluatorStrategy, AegisQuiz.Application.Services.Scoring.MatchingEvaluatorStrategy>();
builder.Services.AddSingleton<IQuestionEvaluatorStrategy, AegisQuiz.Application.Services.Scoring.EssayEvaluatorStrategy>();
builder.Services.AddSingleton<IQuestionEvaluationDispatcher, AegisQuiz.Application.Services.Scoring.QuestionEvaluationDispatcher>();

// [Creator Zen Studio] Đăng ký ITaxonomySnifferStrategy OCP & Dispatcher
builder.Services.AddSingleton<ITaxonomySnifferStrategy, AegisQuiz.Application.Services.Taxonomy.K12MathTaxonomySnifferStrategy>();
builder.Services.AddSingleton<ITaxonomySnifferStrategy, AegisQuiz.Application.Services.Taxonomy.DrivingLicenseTaxonomySnifferStrategy>();
builder.Services.AddSingleton<ITaxonomySnifferStrategy, AegisQuiz.Application.Services.Taxonomy.BankingComplianceTaxonomySnifferStrategy>();
builder.Services.AddSingleton<ITaxonomySnifferStrategy, AegisQuiz.Application.Services.Taxonomy.GeneralFallbackTaxonomySnifferStrategy>();
builder.Services.AddSingleton<ITaxonomySnifferDispatcher, AegisQuiz.Application.Services.Taxonomy.TaxonomySnifferDispatcher>();

// [Phase B] Multi-Tenancy — Scoped per request
builder.Services.AddScoped<AegisQuiz.Application.Interfaces.ITenantContext,
                            AegisQuiz.Infrastructure.Services.TenantContext>();

// [Ownership Scoping] Kiểm tra quyền sở hữu tài nguyên (Question/Topic) cho TeamLeader
builder.Services.AddScoped<AegisQuiz.API.Security.IContentOwnershipGuard,
                            AegisQuiz.API.Security.ContentOwnershipGuard>();

// SignalR (WebSockets cho Proctoring + Webhook realtime + Gameshow Arena)
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// [Aegis Arena Plugin System] Gameshow Plugins & In-Memory Real-time Manager
builder.Services.AddSingleton<IArenaGamePlugin, OlympiaPlugin>();
builder.Services.AddSingleton<IArenaGamePlugin, GoldenBellPlugin>();
builder.Services.AddSingleton<IArenaGamePlugin, LuckyWheelPlugin>();
builder.Services.AddSingleton<IArenaGamePlugin, UniversityChallengePlugin>();
builder.Services.AddSingleton<IArenaGamePlugin, JeopardyPlugin>();
builder.Services.AddSingleton<IArenaGamePlugin, LightningQuizPlugin>();
builder.Services.AddSingleton<ArenaRoomManager>();
builder.Services.AddSingleton<INtpTimeSyncService, AegisQuiz.Infrastructure.Services.NtpTimeSyncService>();
builder.Services.AddSingleton<IOfflineSyncService, AegisQuiz.Infrastructure.Services.OfflineSyncService>();
builder.Services.AddSingleton<ILti13Service, AegisQuiz.Infrastructure.Services.Lti13Service>();
builder.Services.AddHttpClient("LtiAgsClient");

// [World-Class Upgrade] Server-authoritative exam timer (học từ quiz-service)
builder.Services.AddHostedService<ExamTimerService>();

// [Enterprise Omni-Channel Payment Sovereignty & OCP Gateway Strategies]
builder.Services.AddHttpClient<AegisQuiz.Infrastructure.Payments.PayOSGatewayService>();
builder.Services.AddHttpClient<AegisQuiz.Infrastructure.Payments.StripeInternationalGatewayService>();
builder.Services.AddScoped<AegisQuiz.Application.Payments.IPaymentGatewayStrategy, AegisQuiz.Infrastructure.Payments.PayOSGatewayService>();
builder.Services.AddScoped<AegisQuiz.Application.Payments.IPaymentGatewayStrategy, AegisQuiz.Infrastructure.Payments.StripeInternationalGatewayService>();
builder.Services.AddScoped<AegisQuiz.Application.Payments.IPaymentOrchestratorService, AegisQuiz.Infrastructure.Payments.PaymentOrchestratorService>();

// Controllers + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// [CG2: Cảm biến Đe dọa] [CG6: Định danh Tuyệt đối]
// JWT Bearer — Dual-Issuer: PKI (Symmetric HS256) + Keycloak (Asymmetric RS256 via JWKS)
var isDevelopment = builder.Environment.IsDevelopment();
var oidcAuthority = builder.Configuration["Oidc:Authority"] ?? "http://localhost:8180/realms/dehoc";
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("[CG2] Jwt:Secret is not configured. Set via env var JWT_SECRET.");

// [P0 Excellence] HS256 cần khóa >= 256-bit; placeholder "SET_VIA_*" không bao giờ được chạy ngoài Dev.
var isWeakJwtSecret = jwtSecret.StartsWith("SET_VIA", StringComparison.OrdinalIgnoreCase)
    || System.Text.Encoding.UTF8.GetByteCount(jwtSecret) < 32;
if (isWeakJwtSecret)
{
    if (!isDevelopment)
        throw new InvalidOperationException("[CG2] Jwt:Secret is a placeholder or shorter than 32 bytes. Set a strong JWT_SECRET.");
    Log.Warning("[CG2] Jwt:Secret is a placeholder/weak value — accepted only in Development.");
}

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // JWKS auto-discovery từ Keycloak (RS256 - không cần hardcode public key)
        options.Authority = oidcAuthority;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = false,
            // [CG2 FIX] ValidateIssuer = true trong Production — chống token giả mạo từ issuer khác
            ValidateIssuer = !isDevelopment,
            ValidIssuers = new[]
            {
                "AegisQuiz.PKI",                                // PKI USB Token
                oidcAuthority,                                  // Keycloak Realm URL
                oidcAuthority.TrimEnd('/'),                     // Trailing slash variant
            },
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30), // Cho phép lệch đồng hồ tối đa 30s
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                string? tokenIssuer = null;
                if (securityToken is Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jwtToken)
                    tokenIssuer = jwtToken.Issuer;
                else if (securityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken legacyToken)
                    tokenIssuer = legacyToken.Issuer;

                // Issuer 1: AegisQuiz.PKI (ký bằng HMAC-SHA256 symmetric key)
                if (tokenIssuer == "AegisQuiz.PKI")
                {
                    return new[]
                    {
                        new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                            System.Text.Encoding.UTF8.GetBytes(jwtSecret))
                    };
                }

                // Issuer 2: Keycloak (ký bằng RSA asymmetric, JWKS tự động fetch từ Authority)
                // Trả về empty → Framework sẽ dùng JWKS endpoint từ options.Authority
                return Enumerable.Empty<Microsoft.IdentityModel.Tokens.SecurityKey>();
            }
        };
        // HTTPS chỉ bắt buộc khi authority sử dụng HTTPS (Keycloak local / dev chạy HTTP)
        options.RequireHttpsMetadata = oidcAuthority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        // Sự kiện debug — chỉ log trong Development
        if (isDevelopment)
        {
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    Log.Warning("[JWT] Auth failed: {Error}", ctx.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        }
    });
// [RBAC — Privilege Ceiling Scoped RBAC] Role-Weight authorization handler
// (thay thế dần cho so khớp chuỗi role thô — Commandment #7 PRIVILEGE-CEIL).
builder.Services.AddSingleton<IAuthorizationHandler, RoleWeightAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    // [CRITICAL FIX] Mặc định TỪ CHỐI truy cập ẩn danh cho MỌI endpoint không gắn
    // [AllowAnonymous] tường minh. Trước đây chỉ 4/19 controller có [Authorize],
    // khiến phần lớn API (kể cả /api/system/recreate-tables) mở toang cho anonymous.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Policy theo Role Weight — W(Caller) ≥ W(NgưỡngYêuCầu). Vai trò cao hơn tự
    // động thỏa mãn policy của vai trò thấp hơn, không cần liệt kê thủ công.
    options.AddPolicy(RoleWeightPolicies.SystemAdmin,    p => p.Requirements.Add(MinimumRoleWeightRequirement.ForRole(AppRoles.SystemAdmin)));
    options.AddPolicy(RoleWeightPolicies.TenantAdmin,    p => p.Requirements.Add(MinimumRoleWeightRequirement.ForRole(AppRoles.TenantAdmin)));
    options.AddPolicy(RoleWeightPolicies.ContentManager, p => p.Requirements.Add(MinimumRoleWeightRequirement.ForRole(AppRoles.ContentManager)));
    options.AddPolicy(RoleWeightPolicies.OrgUnitManager, p => p.Requirements.Add(MinimumRoleWeightRequirement.ForRole(AppRoles.OrgUnitManager)));
    options.AddPolicy(RoleWeightPolicies.TeamLeader,     p => p.Requirements.Add(MinimumRoleWeightRequirement.ForRole(AppRoles.TeamLeader)));
    options.AddPolicy(RoleWeightPolicies.Learner,        p => p.Requirements.Add(MinimumRoleWeightRequirement.ForRole(AppRoles.Learner)));
});

// [World-Class Upgrade] Health Checks (học từ quiz-service)
var hcBuilder = builder.Services.AddHealthChecks();
if (usePostgres)
{
    hcBuilder
        .AddNpgSql(
            builder.Configuration.GetConnectionString("DefaultConnection") ?? "",
            name: "aegisquiz-db")
        .AddRedis(
            builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
            name: "aegisquiz-redis");
}

// [World-Class Upgrade] Rate Limiting — phân tầng AI vs Standard (học từ quiz-service)
builder.Services.AddRateLimiter(options =>
{
    // AI endpoints đắt tiền: giới hạn chặt
    options.AddFixedWindowLimiter("ai-ops", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
    // Webhook: bảo vệ khỏi DDoS
    options.AddFixedWindowLimiter("WebhookLimiter", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    // API thông thường
    options.AddFixedWindowLimiter("standard", opt =>
    {
        opt.PermitLimit = 200;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 10;
    });
    // Health check endpoints
    options.AddFixedWindowLimiter("health-check", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 5;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// [Upload Limits] Kestrel & FormOptions (Hỗ trợ file Word/PDF/Excel lớn lên tới 100MB)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 100 * 1024 * 1024;
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024;
    options.ValueLengthLimit = 100 * 1024 * 1024;
    options.MultipartHeadersLengthLimit = 100 * 1024 * 1024;
});

// CORS (Hỗ trợ đa miền: localhost, dehoc.vn, daotao.dehoc.vn)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (builder.Environment.IsDevelopment() || allowedOrigins.Length == 0 || allowedOrigins.Contains("*"))
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Cần cho SignalR
        }
        else
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Cần cho SignalR
        }
    });
});

// Problem Details (standardized error responses)
builder.Services.AddProblemDetails();

var app = builder.Build();

// ===== MIDDLEWARE PIPELINE =====
bool enableSwagger = app.Environment.IsDevelopment() ||
                     builder.Configuration.GetValue<bool>("EnableSwagger", false);
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// [World-Class Upgrade] Structured request logging (học từ quiz-service)
app.UseSerilogRequestLogging();

// [World-Class Upgrade] Health Checks (K8s/Docker readiness + liveness)
app.MapHealthChecks("/health/detail", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name        = e.Key,
                status      = e.Value.Status.ToString(),
                description = e.Value.Description,
                exception   = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
})
.RequireRateLimiting("health-check")
.AllowAnonymous(); // Health probe (K8s/Docker/Coolify) — nhất quán với /health, /health/live, /ping

// [World-Class Upgrade] Database Migration — FAIL FAST pattern (học từ quiz-service)
using (var scope = app.Services.CreateScope())
{
    var db  = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        if (db.Database.IsNpgsql())
        {
            // [BƯỚC 1: TIẾN HÀNH MIGRATIONS TRƯỚC CHO DB MỚI]
            try
            {
                db.Database.Migrate();
                log.LogInformation("[AegisQuiz] PostgreSQL database migrations applied successfully.");
            }
            catch (Exception exMigrate)
            {
                log.LogInformation("[AegisQuiz] EF migration notice: {Message}", exMigrate.Message);
            }

            // [BƯỚC 2: QUÉT & THI HÀNH CÁC BẢN VÁ DATABASE TỪ CONFIG / FILE SQL (Zero Hard-code Architecture)]
            try
            {
                var patcher = scope.ServiceProvider.GetRequiredService<AegisQuiz.Application.Interfaces.IDatabasePatcher>();
                int appliedCount = await patcher.ApplyPendingPatchesAsync();
                log.LogInformation("[AegisQuiz] Database Patcher completed: {Count} statements executed from external patches/resources.", appliedCount);
            }
            catch (Exception exPatcher)
            {
                log.LogWarning(exPatcher, "[AegisQuiz] Database Patcher notice: {Message}", exPatcher.Message);
            }
        }
        else
        {
            db.Database.EnsureCreated();
            log.LogInformation("[AegisQuiz] InMemory database created successfully for zero-friction local execution.");
        }

        // [BƯỚC 2b: CIG ENGINE] Đồng bộ quy tắc bóc tách câu hỏi từ CSDL vào bộ nhớ (0ms downtime, seed nếu bảng trống)
        try
        {
            var cigEngine = scope.ServiceProvider.GetRequiredService<IDynamicQuestionIngestionEngine>();
            var syncedRules = cigEngine.SyncRulesFromDatabaseAsync().GetAwaiter().GetResult();
            log.LogInformation("[CIG Engine] SyncRulesFromDatabase | Provider={Provider} -> Synced {Count} rules", db.Database.ProviderName, syncedRules);
        }
        catch (Exception exCig)
        {
            log.LogWarning(exCig, "[CIG Engine] SyncRulesFromDatabase | Warning -> {Message}", exCig.Message);
        }

        // [BƯỚC 3: KHỞI TẠO TÀI KHOẢN GỐC ROOT ADMIN NẾU CHƯA CÓ]
        try
        {
            if (!db.UserAccounts.Any())
            {
                var defaultTenant = db.Tenants.FirstOrDefault();
                if (defaultTenant == null)
                {
                    defaultTenant = new AegisQuiz.Domain.Entities.Tenant
                    {
                        Id = Guid.NewGuid(),
                        Code = "dehoc",
                        Name = "Hệ sinh thái Giáo dục Dehoc",
                        Plan = AegisQuiz.Domain.Entities.TenantPlan.Enterprise,
                        ScaleType = AegisQuiz.Domain.Entities.TenantScaleType.Enterprise,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.Tenants.Add(defaultTenant);
                    db.SaveChanges();
                }

                // Root Admin
                var adminEmail = builder.Configuration["ADMIN_INITIAL_EMAIL"] ?? "admin@dehoc.vn";
                var adminPassword = builder.Configuration["ADMIN_INITIAL_PASSWORD"] ?? "Admin@Dehoc2026!";
                var adminHash = AegisQuiz.Application.Common.Security.PasswordSecurityHelper.HashPassword(adminPassword);
                var adminUser = new AegisQuiz.Domain.Entities.UserAccount
                {
                    Id = Guid.NewGuid(),
                    Email = adminEmail.Trim().ToLowerInvariant(),
                    FullName = "Quản Trị Viên Hệ Thống",
                    PasswordHash = adminHash,
                    Role = AegisQuiz.Domain.Entities.AppRoles.TenantAdmin,
                    TenantId = defaultTenant.Id,
                    IsPremium = true,
                    SubscriptionTier = "ENTERPRISE",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.UserAccounts.Add(adminUser);
                db.UserRoles.Add(new AegisQuiz.Domain.Entities.UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUser.Id.ToString(),
                    TenantId = defaultTenant.Id,
                    Role = AegisQuiz.Domain.Entities.AppRoles.TenantAdmin,
                    IsActive = true
                });

                // TeamLeader
                var teamleadHash = AegisQuiz.Application.Common.Security.PasswordSecurityHelper.HashPassword("Lead@Dehoc2026!");
                var teamleadUser = new AegisQuiz.Domain.Entities.UserAccount
                {
                    Id = Guid.NewGuid(),
                    Email = "teamlead@dehoc.vn",
                    FullName = "Trưởng Nhóm Khảo Thí",
                    PasswordHash = teamleadHash,
                    Role = AegisQuiz.Domain.Entities.AppRoles.TeamLeader,
                    TenantId = defaultTenant.Id,
                    IsPremium = true,
                    SubscriptionTier = "VIP",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.UserAccounts.Add(teamleadUser);
                db.UserRoles.Add(new AegisQuiz.Domain.Entities.UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = teamleadUser.Id.ToString(),
                    TenantId = defaultTenant.Id,
                    Role = AegisQuiz.Domain.Entities.AppRoles.TeamLeader,
                    IsActive = true
                });

                // VIP Learner
                var vipHash = AegisQuiz.Application.Common.Security.PasswordSecurityHelper.HashPassword("Vip@Dehoc2026!");
                var vipUser = new AegisQuiz.Domain.Entities.UserAccount
                {
                    Id = Guid.NewGuid(),
                    Email = "student.vip@dehoc.vn",
                    FullName = "Học Viên VIP (AI Adaptive)",
                    PasswordHash = vipHash,
                    Role = AegisQuiz.Domain.Entities.AppRoles.Learner,
                    TenantId = defaultTenant.Id,
                    IsPremium = true,
                    SubscriptionTier = "VIP",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.UserAccounts.Add(vipUser);
                db.UserRoles.Add(new AegisQuiz.Domain.Entities.UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = vipUser.Id.ToString(),
                    TenantId = defaultTenant.Id,
                    Role = AegisQuiz.Domain.Entities.AppRoles.Learner,
                    IsActive = true
                });

                db.SaveChanges();
                log.LogInformation("[AegisQuiz] Seeded enterprise accounts: admin@dehoc.vn, teamlead@dehoc.vn, student.vip@dehoc.vn");
            }
        }
        catch (Exception exSeed)
        {
            log.LogWarning(exSeed, "[AegisQuiz] Initial accounts seeding warning: {Message}", exSeed.Message);
        }

        // Tự động nạp bộ câu hỏi toán thực tế từ DeToanGiaiChiTiet.docx nếu có
        string[] candidateDocxPaths =
        [
            Path.Combine(AppContext.BaseDirectory, "DeToanGiaiChiTiet.docx"),
            Path.Combine(Directory.GetCurrentDirectory(), "DeToanGiaiChiTiet.docx"),
            @"D:\Cuong\DuAn\mybank\AegisQuiz\DeToanGiaiChiTiet.docx"
        ];
        string? sampleDocx = candidateDocxPaths.FirstOrDefault(File.Exists);
        if (!string.IsNullOrEmpty(sampleDocx) && db.Questions.Count() <= 10)
        {
            try
            {
                var docxParser = scope.ServiceProvider.GetRequiredService<IDocxParserService>();
                using var fs = File.Open(sampleDocx, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var rawQuestions = docxParser.ParseDocxFile(fs);
                int imported = 0;
                foreach (var item in rawQuestions.Take(50))
                {
                    var q = new AegisQuiz.Domain.Entities.SingleChoiceQuestion
                    {
                        Id = Guid.NewGuid(),
                        Content = item.Content,
                        Difficulty = item.Difficulty,
                        CategoryCode = "TOAN_GDPT_2025",
                        ContentType = item.ContentType,
                        OptionType = item.OptionType,
                        DurationSeconds = 60,
                        Options = item.Options,
                        CorrectOption = item.SuggestedAnswer,
                        CreatedAt = DateTime.UtcNow,
                        Payload = System.Text.Json.JsonSerializer.SerializeToDocument(new
                        {
                            options = item.Options.Select((o, idx) => new { id = (idx + 1).ToString(), text = o }),
                            correctAnswer = item.SuggestedAnswer,
                            explanation = item.AiExplanation
                        })
                    };
                    db.Questions.Add(q);
                    imported++;
                }

                if (!db.BankTopics.Any(t => t.Code == "TOAN_GDPT_2025"))
                {
                    db.BankTopics.Add(new AegisQuiz.Domain.Entities.BankTopic
                    {
                        Id = Guid.NewGuid(),
                        Code = "TOAN_GDPT_2025",
                        Name = "Toán Học GDPT 2025 (Đề Có Giải Chi Tiết)",
                        CategoryCode = "TOAN",
                        Description = "Ngân hàng đề thi Toán học có giải chi tiết và công thức KaTeX",
                        Enabled = true,
                        VisibilityScope = "PUBLIC"
                    });
                }

                db.SaveChanges();
                log.LogInformation("[AegisQuiz] Tự động nạp thành công {Count} câu hỏi Toán từ DeToanGiaiChiTiet.docx vào Ngân hàng câu hỏi!", imported);
            }
            catch (Exception exDocx)
            {
                log.LogWarning(exDocx, "[AegisQuiz] Pre-seeding from DeToanGiaiChiTiet.docx skipped.");
            }
        }
    }
    catch (Exception ex)
    {
        log.LogWarning(ex, "[AegisQuiz] Database initialization warning. Continuing with in-memory execution.");
    }
}

// Phía trước đã có Nginx/Traefik làm Reverse Proxy SSL Termination
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication(); // [Cảnh giới 6: Đã tích hợp JWT SSO]
app.UseAuthorization();

// [Phase B] Multi-Tenant resolution — sau Authentication để có JWT claims
app.UseMiddleware<AegisQuiz.Infrastructure.Services.TenantMiddleware>();

app.MapControllers();

// [Health Checks & Liveness Probes cho Docker / Coolify / Traefik]
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResultStatusCodes =
    {
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = StatusCodes.Status200OK,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = StatusCodes.Status200OK,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
}).AllowAnonymous();

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy", service = "AegisQuiz.API", timestamp = DateTime.UtcNow })).AllowAnonymous();
app.MapGet("/ping", () => Results.Ok("pong")).AllowAnonymous();

// [World-Class Upgrade] SignalR Proctoring Hub (học từ quiz-service)
app.MapHub<ExamProctoringHub>("/hubs/proctoring");
// [Aegis Arena] Real-time Gameshow Arena Hub
app.MapHub<ArenaHub>("/hubs/arena");
// [Aegis Universal Epistemic Symposium] Real-time Dialectical Streaming Hub
app.MapHub<SymposiumHub>("/hubs/symposium");
// app.MapHub<PaymentHub>("/hubs/payment"); // Sẽ implement ở Sprint 2

Log.Information("[AegisQuiz] Application started successfully.");
app.Run();
