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

            // [BƯỚC 2: AUTO-HEALING DDL] Đảm bảo 100% các cột mới (DepthLevel, MaterializedPath,...) và bảng mới luôn tồn tại
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    -- 1. BẢNG BankTopics: Đảm bảo đầy đủ các cột cây phân cấp 2026
                    ALTER TABLE IF EXISTS ""BankTopics"" ADD COLUMN IF NOT EXISTS ""DepthLevel"" integer NOT NULL DEFAULT 0;
                    ALTER TABLE IF EXISTS ""BankTopics"" ADD COLUMN IF NOT EXISTS ""MaterializedPath"" text NULL;
                    ALTER TABLE IF EXISTS ""BankTopics"" ADD COLUMN IF NOT EXISTS ""Scope"" text NOT NULL DEFAULT 'COMMUNITY';
                    ALTER TABLE IF EXISTS ""BankTopics"" ADD COLUMN IF NOT EXISTS ""DomainCode"" text NOT NULL DEFAULT 'GENERAL';
                    ALTER TABLE IF EXISTS ""BankTopics"" ADD COLUMN IF NOT EXISTS ""DisplayOrder"" integer NOT NULL DEFAULT 0;
                    ALTER TABLE IF EXISTS ""BankTopics"" ADD COLUMN IF NOT EXISTS ""QuestionCountCached"" integer NOT NULL DEFAULT 0;
                    ALTER TABLE IF EXISTS ""BankTopics"" ADD COLUMN IF NOT EXISTS ""Urn"" text NULL;
                    ALTER TABLE IF EXISTS ""BankTopics"" ADD COLUMN IF NOT EXISTS ""ParentId"" uuid NULL;
                    ALTER TABLE IF EXISTS ""BankTopics"" ADD COLUMN IF NOT EXISTS ""AllowedTenantIds"" text[] NOT NULL DEFAULT '{{}}';
                    ALTER TABLE IF EXISTS ""BankTopics"" ADD COLUMN IF NOT EXISTS ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                    
                    -- 2. BẢNG Questions: Đảm bảo đầy đủ toạ độ tri thức
                    ALTER TABLE IF EXISTS ""Questions"" ADD COLUMN IF NOT EXISTS ""ContextId"" uuid NULL;
                    ALTER TABLE IF EXISTS ""Questions"" ADD COLUMN IF NOT EXISTS ""DomainCode"" text NOT NULL DEFAULT 'EDUCATION';
                    ALTER TABLE IF EXISTS ""Questions"" ADD COLUMN IF NOT EXISTS ""IsCritical"" boolean NOT NULL DEFAULT false;
                    ALTER TABLE IF EXISTS ""Questions"" ADD COLUMN IF NOT EXISTS ""SubCategory"" text NULL;
                    ALTER TABLE IF EXISTS ""Questions"" ADD COLUMN IF NOT EXISTS ""Tags"" text[] NOT NULL DEFAULT '{{}}';
                    ALTER TABLE IF EXISTS ""Questions"" ADD COLUMN IF NOT EXISTS ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                    ALTER TABLE IF EXISTS ""Questions"" ADD COLUMN IF NOT EXISTS ""CreatedBy"" text NULL;
                    ALTER TABLE IF EXISTS ""Questions"" ADD COLUMN IF NOT EXISTS ""IsPublic"" boolean NOT NULL DEFAULT false;

                    -- 2a. BẢNG QuestionContexts: Ngữ cảnh bài đọc hiểu dùng chung
                    CREATE TABLE IF NOT EXISTS ""QuestionContexts"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""Title"" text NOT NULL DEFAULT '',
                        ""Content"" text NOT NULL DEFAULT '',
                        ""ContentType"" text NOT NULL DEFAULT 'text',
                        ""MediaUrl"" text NULL,
                        ""ContentHash"" text NULL,
                        ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        ""UpdatedAt"" timestamp with time zone NULL
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_QuestionContexts_ContentHash"" ON ""QuestionContexts"" (""ContentHash"");
                    CREATE INDEX IF NOT EXISTS ""IX_QuestionContexts_TenantId"" ON ""QuestionContexts"" (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_Questions_ContextId"" ON ""Questions"" (""ContextId"");

                    -- 2b. BẢNG Notebooks & NotebookChapters (AegisNotebook Sovereign 2026)
                    CREATE TABLE IF NOT EXISTS ""Notebooks"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""Title"" text NOT NULL,
                        ""SourcePdfUrl"" text NOT NULL DEFAULT '',
                        ""CreatedByAdminId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                        ""Status"" integer NOT NULL DEFAULT 0,
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'
                    );

                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""OrgUnitId"" uuid NULL;
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""DomainCode"" text NOT NULL DEFAULT 'GENERAL';
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""BankTopicId"" uuid NULL;
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""StoragePath"" text NOT NULL DEFAULT '';
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""OriginalFileName"" text NOT NULL DEFAULT '';
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""FileSizeBytes"" bigint NOT NULL DEFAULT 0;
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""TotalPages"" integer NOT NULL DEFAULT 0;
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""Description"" text NOT NULL DEFAULT '';
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""ErrorMessage"" text NULL;
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""CognitiveStatsJson"" text NOT NULL DEFAULT '{{}}';
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""ConflictReportJson"" text NOT NULL DEFAULT '[]';
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""PersonaName"" text NOT NULL DEFAULT 'Cố Vấn Tri Thức Trưởng';
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""PersonaRole"" integer NOT NULL DEFAULT 1;
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""CustomPersonaPrompt"" text NOT NULL DEFAULT '';
                    ALTER TABLE IF EXISTS ""Notebooks"" ADD COLUMN IF NOT EXISTS ""UpdatedAt"" timestamp with time zone NULL;

                    -- BẢNG NotebookSources: Quản lý đa tài liệu nguồn
                    CREATE TABLE IF NOT EXISTS ""NotebookSources"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""NotebookId"" uuid NOT NULL REFERENCES ""Notebooks""(""Id"") ON DELETE CASCADE,
                        ""Title"" text NOT NULL DEFAULT '',
                        ""OriginalFileName"" text NOT NULL DEFAULT '',
                        ""StoragePath"" text NOT NULL DEFAULT '',
                        ""SourceUrl"" text NOT NULL DEFAULT '',
                        ""FileSizeBytes"" bigint NOT NULL DEFAULT 0,
                        ""TotalPages"" integer NOT NULL DEFAULT 0,
                        ""Type"" integer NOT NULL DEFAULT 0,
                        ""Authority"" integer NOT NULL DEFAULT 2,
                        ""Status"" integer NOT NULL DEFAULT 2,
                        ""ExtractedText"" text NOT NULL DEFAULT '',
                        ""MetadataJson"" text NOT NULL DEFAULT '{{}}',
                        ""UploadedAt"" timestamp with time zone NOT NULL DEFAULT now()
                    );

                    CREATE TABLE IF NOT EXISTS ""NotebookChapters"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""NotebookId"" uuid NOT NULL REFERENCES ""Notebooks""(""Id"") ON DELETE CASCADE,
                        ""OrderIndex"" integer NOT NULL DEFAULT 1,
                        ""Title"" text NOT NULL,
                        ""Summary"" text NOT NULL DEFAULT '',
                        ""RawContent"" text NOT NULL DEFAULT '',
                        ""KeyPointsJson"" text NOT NULL DEFAULT '[]',
                        ""GeneratedQuestionIds"" uuid[] NOT NULL DEFAULT '{{}}'
                    );

                    ALTER TABLE IF EXISTS ""NotebookChapters"" ADD COLUMN IF NOT EXISTS ""StartPage"" integer NOT NULL DEFAULT 1;
                    ALTER TABLE IF EXISTS ""NotebookChapters"" ADD COLUMN IF NOT EXISTS ""EndPage"" integer NOT NULL DEFAULT 1;
                    ALTER TABLE IF EXISTS ""NotebookChapters"" ADD COLUMN IF NOT EXISTS ""SourceIds"" uuid[] NOT NULL DEFAULT '{{}}';
                    ALTER TABLE IF EXISTS ""NotebookChapters"" ADD COLUMN IF NOT EXISTS ""PinpointCoordinatesJson"" text NOT NULL DEFAULT '[]';
                    ALTER TABLE IF EXISTS ""NotebookChapters"" ADD COLUMN IF NOT EXISTS ""QuestionCount"" integer NOT NULL DEFAULT 0;

                    -- BẢNG NotebookChatMessages: Đối thoại nhận thức nhập vai
                    CREATE TABLE IF NOT EXISTS ""NotebookChatMessages"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""NotebookId"" uuid NOT NULL REFERENCES ""Notebooks""(""Id"") ON DELETE CASCADE,
                        ""Sender"" text NOT NULL DEFAULT 'User',
                        ""RoleName"" text NOT NULL DEFAULT '',
                        ""Message"" text NOT NULL DEFAULT '',
                        ""CitationsJson"" text NOT NULL DEFAULT '[]',
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now()
                    );

                    -- 3. BẢNG DynamicDomains (nếu chưa có)
                    CREATE TABLE IF NOT EXISTS ""DynamicDomains"" (
                        ""Code"" text NOT NULL,
                        ""Name"" text NOT NULL,
                        ""Description"" text,
                        ""Icon"" text NOT NULL DEFAULT 'Layers',
                        ""ColorBadge"" text NOT NULL DEFAULT '#0284c7',
                        ""IsSystemStandard"" boolean NOT NULL DEFAULT true,
                        ""TenantId"" uuid NULL,
                        ""ParentDomainCode"" text NULL,
                        ""IsActive"" boolean NOT NULL DEFAULT true,
                        ""DisplayOrder"" integer NOT NULL DEFAULT 0,
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        CONSTRAINT ""PK_DynamicDomains"" PRIMARY KEY (""Code"")
                    );

                    -- Nạp các ngành chuẩn
                    INSERT INTO ""DynamicDomains"" (""Code"", ""ColorBadge"", ""CreatedAt"", ""Description"", ""DisplayOrder"", ""Icon"", ""IsActive"", ""IsSystemStandard"", ""Name"", ""ParentDomainCode"", ""TenantId"")
                    VALUES 
                      ('EDUCATION', '#2563eb', now(), 'Khảo thí đại học, phổ thông, học thuật tổng quát', 1, 'GraduationCap', TRUE, TRUE, 'Giáo dục & Học thuật', NULL, NULL),
                      ('BANKING', '#059669', now(), 'Nghiệp vụ tín dụng, thanh toán, ngân quỹ, quản trị rủi ro', 2, 'Landmark', TRUE, TRUE, 'Tài chính - Ngân hàng', NULL, NULL),
                      ('HEALTHCARE', '#dc2626', now(), 'Y học, dược lâm sàng, quy trình điều dưỡng, kiểm soát nhiễm khuẩn', 3, 'HeartPulse', TRUE, TRUE, 'Y tế - Sức khỏe', NULL, NULL),
                      ('HSE', '#d97706', now(), 'An toàn vệ sinh lao động, PCCC, quy chuẩn ISO 45001', 4, 'HardHat', TRUE, TRUE, 'An toàn - Môi trường LĐ', NULL, NULL),
                      ('GOV_DRIVING', '#7c3aed', now(), 'Bộ 600 câu GPLX Bộ GTVT, 60 câu điểm liệt, sa hình AI', 5, 'Car', TRUE, TRUE, 'Sát hạch Lái xe Quốc gia', NULL, NULL),
                      ('IT_SECURITY', '#0284c7', now(), 'Bảo mật an ninh mạng, kiến trúc hệ thống, chứng chỉ CISSP/CompTIA', 6, 'ShieldCheck', TRUE, TRUE, 'An toàn TT & CNTT', NULL, NULL),
                      ('GENERAL', '#4b5563', now(), 'Kiến thức đại cương, kỹ năng mềm, văn hóa doanh nghiệp', 7, 'Layers', TRUE, TRUE, 'Tổng hợp / Đại cương', NULL, NULL)
                    ON CONFLICT (""Code"") DO NOTHING;

                    -- 4. BẢNG UserAccounts: Xác thực tài khoản người dùng chuẩn Enterprise
                    CREATE TABLE IF NOT EXISTS ""UserAccounts"" (
                        ""Id"" uuid NOT NULL,
                        ""Email"" text NOT NULL,
                        ""PasswordHash"" text NOT NULL,
                        ""FullName"" text NOT NULL,
                        ""PhoneNumber"" text,
                        ""AvatarUrl"" text,
                        ""Role"" text NOT NULL DEFAULT 'Learner',
                        ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                        ""OrgUnitId"" uuid,
                        ""IsPremium"" boolean NOT NULL DEFAULT false,
                        ""SubscriptionTier"" text NOT NULL DEFAULT 'FREE',
                        ""SubscriptionExpiresAt"" timestamp with time zone,
                        ""IsActive"" boolean NOT NULL DEFAULT true,
                        ""FailedLoginAttempts"" integer NOT NULL DEFAULT 0,
                        ""LockoutEnd"" timestamp with time zone,
                        ""LastLoginAt"" timestamp with time zone,
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        CONSTRAINT ""PK_UserAccounts"" PRIMARY KEY (""Id"")
                    );
                    ALTER TABLE IF EXISTS ""UserAccounts"" ADD COLUMN IF NOT EXISTS ""ActiveTenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';
                    ALTER TABLE IF EXISTS ""UserAccounts"" ADD COLUMN IF NOT EXISTS ""RefreshToken"" text;
                    ALTER TABLE IF EXISTS ""UserAccounts"" ADD COLUMN IF NOT EXISTS ""RefreshTokenExpiresAt"" timestamp with time zone;
                    ALTER TABLE IF EXISTS ""UserAccounts"" ADD COLUMN IF NOT EXISTS ""PasswordResetToken"" text;
                    ALTER TABLE IF EXISTS ""UserAccounts"" ADD COLUMN IF NOT EXISTS ""PasswordResetTokenExpiresAt"" timestamp with time zone;
                    ALTER TABLE IF EXISTS ""UserAccounts"" ADD COLUMN IF NOT EXISTS ""IsTwoFactorEnabled"" boolean NOT NULL DEFAULT false;
                    ALTER TABLE IF EXISTS ""UserAccounts"" ADD COLUMN IF NOT EXISTS ""TwoFactorSecret"" text;
                    ALTER TABLE IF EXISTS ""UserAccounts"" ADD COLUMN IF NOT EXISTS ""TwoFactorRecoveryCodes"" text;
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserAccounts_Email"" ON ""UserAccounts"" (""Email"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserAccounts_TenantId"" ON ""UserAccounts"" (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserAccounts_OrgUnitId"" ON ""UserAccounts"" (""OrgUnitId"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserAccounts_Role"" ON ""UserAccounts"" (""Role"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserAccounts_SubscriptionTier"" ON ""UserAccounts"" (""SubscriptionTier"");

                    -- 5. BẢNG AiModelRegistries (AegisAI Dynamic Registry 2026 - Mở rộng đa mô hình vô hạn)
                    CREATE TABLE IF NOT EXISTS ""AiModelRegistries"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""ModelCode"" text NOT NULL,
                        ""DisplayName"" text NOT NULL,
                        ""ProviderCompany"" text NOT NULL DEFAULT 'DeepSeek',
                        ""ProtocolType"" integer NOT NULL DEFAULT 1,
                        ""CognitiveArchetype"" integer NOT NULL DEFAULT 1,
                        ""EndpointUrl"" text NOT NULL DEFAULT '',
                        ""ModelName"" text NOT NULL DEFAULT '',
                        ""MaxContextTokens"" integer NOT NULL DEFAULT 128000,
                        ""CostInputPerMillion"" double precision NOT NULL DEFAULT 0.14,
                        ""CostOutputPerMillion"" double precision NOT NULL DEFAULT 0.28,
                        ""BenchmarkElo"" integer NOT NULL DEFAULT 1250,
                        ""AvgLatencyMs"" integer NOT NULL DEFAULT 400,
                        ""IsEnabled"" boolean NOT NULL DEFAULT true,
                        ""IsDefaultInArchetype"" boolean NOT NULL DEFAULT false,
                        ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        ""UpdatedAt"" timestamp with time zone NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_AiModelRegistries_ModelCode"" ON ""AiModelRegistries"" (""ModelCode"");
                    CREATE INDEX IF NOT EXISTS ""IX_AiModelRegistries_Archetype"" ON ""AiModelRegistries"" (""CognitiveArchetype"");

                    -- 6. BẢNG UserAiKeys (AegisAI BYOK Hub & Community Contributed Pool)
                    CREATE TABLE IF NOT EXISTS ""UserAiKeys"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""UserId"" uuid NOT NULL,
                        ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                        ""KeyLabel"" text NOT NULL DEFAULT '',
                        ""ProviderCompany"" text NOT NULL DEFAULT 'DeepSeek',
                        ""ProtocolType"" integer NOT NULL DEFAULT 1,
                        ""EncryptedApiKey"" text NOT NULL DEFAULT '',
                        ""KeyFingerprint"" text NOT NULL DEFAULT '',
                        ""CustomEndpoint"" text NULL,
                        ""CustomModelName"" text NULL,
                        ""Scope"" integer NOT NULL DEFAULT 1,
                        ""IsActive"" boolean NOT NULL DEFAULT true,
                        ""MonthlyTokenQuota"" bigint NOT NULL DEFAULT 0,
                        ""TokensUsedThisMonth"" bigint NOT NULL DEFAULT 0,
                        ""TotalCallsCount"" bigint NOT NULL DEFAULT 0,
                        ""SuccessCallsCount"" bigint NOT NULL DEFAULT 0,
                        ""FailedCallsCount"" bigint NOT NULL DEFAULT 0,
                        ""LastUsedAt"" timestamp with time zone NULL,
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        ""UpdatedAt"" timestamp with time zone NULL
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_UserAiKeys_UserId"" ON ""UserAiKeys"" (""UserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserAiKeys_TenantId"" ON ""UserAiKeys"" (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserAiKeys_Scope"" ON ""UserAiKeys"" (""Scope"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserAiKeys_IsActive"" ON ""UserAiKeys"" (""IsActive"");

                    -- Hạt giống mô hình AI ban đầu (Seed data)
                    INSERT INTO ""AiModelRegistries"" (""Id"", ""ModelCode"", ""DisplayName"", ""ProviderCompany"", ""ProtocolType"", ""CognitiveArchetype"", ""EndpointUrl"", ""ModelName"", ""MaxContextTokens"", ""CostInputPerMillion"", ""CostOutputPerMillion"", ""BenchmarkElo"", ""AvgLatencyMs"", ""IsEnabled"", ""IsDefaultInArchetype"", ""TenantId"", ""CreatedAt"")
                    VALUES 
                      ('a0000001-0000-0000-0000-000000000001', 'deepseek-r1', 'DeepSeek R1 Reasoning (CoT)', 'DeepSeek', 1, 1, 'https://api.deepseek.com/v1', 'deepseek-reasoner', 128000, 0.14, 0.28, 1340, 450, true, true, '00000000-0000-0000-0000-000000000000', now()),
                      ('a0000001-0000-0000-0000-000000000002', 'gemini-2.0-flash', 'Google Gemini 2.0 Flash (1M Tokens)', 'Google', 2, 2, 'https://generativelanguage.googleapis.com/v1beta', 'gemini-2.0-flash', 1048576, 0.10, 0.40, 1310, 220, true, true, '00000000-0000-0000-0000-000000000000', now()),
                      ('a0000001-0000-0000-0000-000000000003', 'claude-3-5-sonnet', 'Anthropic Claude 3.5 Sonnet (Pedagogy & Arbiter)', 'Anthropic', 3, 3, 'https://api.anthropic.com/v1', 'claude-3-5-sonnet-20241022', 200000, 3.00, 15.00, 1360, 600, true, true, '00000000-0000-0000-0000-000000000000', now()),
                      ('a0000001-0000-0000-0000-000000000004', 'groq-llama-3-3-70b', 'Groq LPU Llama 3.3 70B Versatile', 'Groq', 1, 4, 'https://api.groq.com/openai/v1', 'llama-3.3-70b-versatile', 128000, 0.59, 0.79, 1280, 180, true, true, '00000000-0000-0000-0000-000000000000', now()),
                      ('a0000001-0000-0000-0000-000000000005', 'ollama-qwen-local', 'Local Sovereign Qwen 2.5 32B (On-Premise)', 'Ollama', 1, 5, 'http://localhost:11434/v1', 'qwen2.5:32b', 32768, 0.0, 0.0, 1260, 300, true, true, '00000000-0000-0000-0000-000000000000', now()),
                      ('a0000001-0000-0000-0000-000000000006', 'sambanova-llama-3-1-405b', 'SambaNova SN40L Llama 3.1 405B Frontier', 'SambaNova', 1, 1, 'https://api.sambanova.ai/v1', 'Meta-Llama-3.1-405B-Instruct', 128000, 5.00, 10.00, 1370, 350, true, false, '00000000-0000-0000-0000-000000000000', now()),
                      ('a0000001-0000-0000-0000-000000000007', 'cerebras-llama-3-3-70b', 'Cerebras Wafer-Scale Llama 3.3 70B (2000+ t/s)', 'Cerebras', 1, 4, 'https://api.cerebras.ai/v1', 'llama-3.3-70b', 128000, 0.60, 0.60, 1290, 80, true, false, '00000000-0000-0000-0000-000000000000', now()),
                      ('a0000001-0000-0000-0000-000000000008', 'mistral-large-latest', 'Mistral Large 2411 Sovereign Frontier', 'Mistral', 1, 3, 'https://api.mistral.ai/v1', 'mistral-large-latest', 128000, 2.00, 6.00, 1330, 400, true, false, '00000000-0000-0000-0000-000000000000', now()),
                      ('a0000001-0000-0000-0000-000000000009', 'github-gpt-4o', 'GitHub Models GPT-4o Gateway', 'GitHub', 1, 3, 'https://models.inference.ai.azure.com', 'gpt-4o', 128000, 2.50, 10.00, 1350, 450, true, false, '00000000-0000-0000-0000-000000000000', now()),
                      ('a0000001-0000-0000-0000-000000000010', 'openai-gpt-4o-mini', 'OpenAI GPT-4o-mini High-Throughput', 'OpenAI', 1, 4, 'https://api.openai.com/v1', 'gpt-4o-mini', 128000, 0.15, 0.60, 1290, 250, true, false, '00000000-0000-0000-0000-000000000000', now()),
                      ('a0000001-0000-0000-0000-000000000011', 'replicate-musicgen', 'Replicate MusicGen Multimodal Audio', 'Replicate', 1, 2, 'https://api.replicate.com/v1', 'facebook/musicgen-small', 32000, 0.05, 0.10, 1200, 800, true, false, '00000000-0000-0000-0000-000000000000', now())
                    ON CONFLICT (""ModelCode"") DO NOTHING;

                    -- 7. BẢNG UniversalAiCouncils & UniversalCouncilSeats & SymposiumSessions (AegisAI Đại Hội Thảo Vạn Vật Tri Thức)
                    CREATE TABLE IF NOT EXISTS ""UniversalAiCouncils"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""CouncilCode"" text NOT NULL,
                        ""DisplayName"" text NOT NULL,
                        ""DisciplineArea"" text NOT NULL DEFAULT '',
                        ""MaterializedTaxonomyPath"" text NOT NULL DEFAULT '',
                        ""EpistemicManifesto"" text NOT NULL DEFAULT '',
                        ""BenchmarkPrestigeElo"" integer NOT NULL DEFAULT 2800,
                        ""IsActive"" boolean NOT NULL DEFAULT true,
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now()
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UniversalAiCouncils_CouncilCode"" ON ""UniversalAiCouncils"" (""CouncilCode"");

                    CREATE TABLE IF NOT EXISTS ""UniversalCouncilSeats"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""CouncilId"" uuid NOT NULL,
                        ""SeatTitle"" text NOT NULL DEFAULT '',
                        ""Role"" integer NOT NULL DEFAULT 1,
                        ""IdealArchetype"" integer NOT NULL DEFAULT 1,
                        ""PreferredModelCode"" text NOT NULL DEFAULT '',
                        ""DecisionWeight"" double precision NOT NULL DEFAULT 1.0,
                        ""PersonaPromptDirective"" text NOT NULL DEFAULT ''
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_UniversalCouncilSeats_CouncilId"" ON ""UniversalCouncilSeats"" (""CouncilId"");

                    CREATE TABLE IF NOT EXISTS ""SymposiumSessions"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""TopicTitle"" text NOT NULL DEFAULT '',
                        ""CoreProblemStatement"" text NOT NULL DEFAULT '',
                        ""ParticipatingCouncilCodesJson"" text NOT NULL DEFAULT '[]',
                        ""DialogueLogJson"" text NOT NULL DEFAULT '[]',
                        ""FinalConsensusResolution"" text NOT NULL DEFAULT '',
                        ""EpistemicConsensusIndex"" double precision NOT NULL DEFAULT 1.0,
                        ""RubricScoresJson"" text NOT NULL DEFAULT '{{}}',
                        ""ConvenedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        ""ConcludedAt"" timestamp with time zone NULL,
                        ""InitiatedByUserId"" uuid NULL,
                        ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_SymposiumSessions_ConvenedAt"" ON ""SymposiumSessions"" (""ConvenedAt"");

                    -- 10. BẢNG EducatorPartnerships (Creator Economy & 90 Ngày Trial Toàn Năng)
                    CREATE TABLE IF NOT EXISTS ""EducatorPartnerships"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""UserId"" uuid NOT NULL,
                        ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                        ""ReferralCode"" text NOT NULL DEFAULT '',
                        ""ReferredByUserId"" uuid NULL,
                        ""TrialExpiresAt"" timestamp with time zone NOT NULL DEFAULT (now() + interval '90 days'),
                        ""RevenueShareRate"" numeric(5,2) NOT NULL DEFAULT 0.80,
                        ""KarmaPoints"" integer NOT NULL DEFAULT 0,
                        ""PayoutBankAccount"" text NULL,
                        ""PayoutBankName"" text NULL,
                        ""PayoutAccountHolder"" text NULL,
                        ""TotalEarnings"" numeric(18,2) NOT NULL DEFAULT 0,
                        ""PendingPayoutBalance"" numeric(18,2) NOT NULL DEFAULT 0,
                        ""IsActive"" boolean NOT NULL DEFAULT true,
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        ""UpdatedAt"" timestamp with time zone NULL
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_EducatorPartnerships_UserId"" ON ""EducatorPartnerships"" (""UserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_EducatorPartnerships_ReferralCode"" ON ""EducatorPartnerships"" (""ReferralCode"");

                    -- Hạt giống 6 Viện Hàn Lâm Nhận Thức Vũ Trụ
                    INSERT INTO ""UniversalAiCouncils"" (""Id"", ""CouncilCode"", ""DisplayName"", ""DisciplineArea"", ""MaterializedTaxonomyPath"", ""EpistemicManifesto"", ""BenchmarkPrestigeElo"", ""IsActive"", ""CreatedAt"")
                    VALUES
                      ('c0000001-0000-0000-0000-000000000001', 'COSMOLOGY', 'Viện Vũ Trụ & Vật Lý Lượng Tử', 'Vật lý lý thuyết & Lượng tử', '/UNIVERSE/PHYSICS/QUANTUM', 'Thực tại vật lý vận hành bởi trường lượng tử và hình học không-thời gian phi tuyến.', 2950, true, now()),
                      ('c0000001-0000-0000-0000-000000000002', 'MATHEMATICS', 'Viện Toán Học & Logic Hình Thức', 'Toán học thuần túy & Độ phức tạp', '/UNIVERSE/MATHEMATICS/LOGIC', 'Chân lý tuyệt đối chỉ tồn tại qua phép suy diễn hình thức không mâu thuẫn.', 2980, true, now()),
                      ('c0000001-0000-0000-0000-000000000003', 'PHILOSOPHY', 'Viện Triết Học, Nhân Văn & Ý Thức', 'Bản thể luận, Đạo đức & Ý nghĩa', '/HUMANITY/PHILOSOPHY/EPISTEMOLOGY', 'Mọi tri thức khoa học phải được soi chiếu qua phẩm giá con người và ý nghĩa tồn tại.', 2900, true, now()),
                      ('c0000001-0000-0000-0000-000000000004', 'AGI_CYBERNETICS', 'Viện Siêu Trí Tuệ & Điều Khiển Học', 'Kiến trúc AGI & Căn chỉnh nhân loại', '/AI/SUPERINTELLIGENCE/ALIGNMENT', 'Sự xuất hiện của Siêu trí tuệ là bước tiến hóa tất yếu của thông tin vũ trụ.', 2990, true, now()),
                      ('c0000001-0000-0000-0000-000000000005', 'ECONOMICS', 'Viện Kinh Tế Chính Trị & Địa Chiến Lược', 'Kinh tế vĩ mô & Lý thuyết trò chơi', '/SOCIETY/ECONOMICS/GEOPOLITICS', 'Dòng chảy giá trị và phân bổ tài nguyên quyết định sự hưng thịnh của nền văn minh.', 2850, true, now()),
                      ('c0000001-0000-0000-0000-000000000006', 'LIFE_SCIENCES', 'Viện Khoa Học Sự Sống & Sinh Quyển', 'Sinh học phân tử & Sinh thái học', '/NATURE/BIOLOGY/EVOLUTION', 'Sự sống là cấu trúc tiêu tán vi mô chống lại entropy nhiệt động lực học.', 2880, true, now())
                    ON CONFLICT (""CouncilCode"") DO NOTHING;

                    -- 11. BẢNG PaymentTransactions (Hạ tầng thanh toán đa cổng và Quỹ tài trợ tri ân)
                    CREATE TABLE IF NOT EXISTS ""PaymentTransactions"" (
                        ""TransactionId"" uuid NOT NULL PRIMARY KEY,
                        ""UserId"" uuid NOT NULL,
                        ""TenantId"" uuid NULL,
                        ""OrderCode"" bigint NOT NULL DEFAULT 0,
                        ""PaymentCode"" text NOT NULL DEFAULT '',
                        ""AmountRequired"" numeric(18,2) NOT NULL DEFAULT 0,
                        ""Currency"" text NOT NULL DEFAULT 'VND',
                        ""Provider"" text NOT NULL DEFAULT 'PayOS',
                        ""Description"" text NOT NULL DEFAULT '',
                        ""CheckoutUrl"" text NOT NULL DEFAULT '',
                        ""QrCode"" text NOT NULL DEFAULT '',
                        ""PaymentLinkId"" text NOT NULL DEFAULT '',
                        ""TransactionHash"" text NOT NULL DEFAULT '',
                        ""MetadataJson"" text NOT NULL DEFAULT '{}',
                        ""Status"" integer NOT NULL DEFAULT 0,
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        ""CompletedAt"" timestamp with time zone NULL
                    );
                    ALTER TABLE ""PaymentTransactions"" ADD COLUMN IF NOT EXISTS ""CheckoutUrl"" text DEFAULT '';
                    ALTER TABLE ""PaymentTransactions"" ADD COLUMN IF NOT EXISTS ""TenantId"" uuid;
                    ALTER TABLE ""PaymentTransactions"" ADD COLUMN IF NOT EXISTS ""OrderCode"" bigint DEFAULT 0;
                    ALTER TABLE ""PaymentTransactions"" ADD COLUMN IF NOT EXISTS ""Provider"" text DEFAULT 'PayOS';
                    ALTER TABLE ""PaymentTransactions"" ADD COLUMN IF NOT EXISTS ""Description"" text DEFAULT '';
                    ALTER TABLE ""PaymentTransactions"" ADD COLUMN IF NOT EXISTS ""QrCode"" text DEFAULT '';
                    ALTER TABLE ""PaymentTransactions"" ADD COLUMN IF NOT EXISTS ""PaymentLinkId"" text DEFAULT '';
                    ALTER TABLE ""PaymentTransactions"" ADD COLUMN IF NOT EXISTS ""TransactionHash"" text DEFAULT '';
                    ALTER TABLE ""PaymentTransactions"" ADD COLUMN IF NOT EXISTS ""MetadataJson"" text DEFAULT '{}';
                    ALTER TABLE ""PaymentTransactions"" ADD COLUMN IF NOT EXISTS ""CompletedAt"" timestamp with time zone;
                    CREATE INDEX IF NOT EXISTS ""IX_PaymentTransactions_OrderCode"" ON ""PaymentTransactions"" (""OrderCode"");
                    CREATE INDEX IF NOT EXISTS ""IX_PaymentTransactions_Provider"" ON ""PaymentTransactions"" (""Provider"");

                    -- 12. BẢNG QuestionParsingRules (CIG Ingestion Grammar Engine 2026)
                    CREATE TABLE IF NOT EXISTS ""QuestionParsingRules"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""RuleCode"" text NOT NULL,
                        ""RuleName"" text NOT NULL,
                        ""Description"" text NULL,
                        ""Scope"" text NOT NULL DEFAULT 'GLOBAL',
                        ""Priority"" integer NOT NULL DEFAULT 100,
                        ""IsActive"" boolean NOT NULL DEFAULT true,
                        ""SpecJson"" text NOT NULL DEFAULT '{}',
                        ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                        ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                        ""UpdatedAt"" timestamp with time zone NULL
                    );
                    CREATE INDEX IF NOT EXISTS ""IX_QuestionParsingRules_IsActive"" ON ""QuestionParsingRules"" (""IsActive"");
                    CREATE INDEX IF NOT EXISTS ""IX_QuestionParsingRules_Priority"" ON ""QuestionParsingRules"" (""Priority"");
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_QuestionParsingRules_TenantId_RuleCode"" ON ""QuestionParsingRules"" (""TenantId"", ""RuleCode"");
                ");
                log.LogInformation("[AegisQuiz] Auto-healing DDL executed successfully. All columns and tables guaranteed.");
            }
            catch (Exception exDdl)
            {
                log.LogWarning(exDdl, "[AegisQuiz] Auto-healing DDL warning: {Message}", exDdl.Message);
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
