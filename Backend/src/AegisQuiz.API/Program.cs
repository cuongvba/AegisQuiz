using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.AI;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.Excel;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.API.Hubs;
using AegisQuiz.API.Services;
using AegisQuiz.Infrastructure.Arena;
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
builder.Services.AddScoped<IExcelParserService, ExcelParserService>();
builder.Services.AddScoped<IDocxParserService, DocxParserService>();
builder.Services.AddScoped<IPdfExtractorService, PdfParserService>();
builder.Services.AddScoped<IIrtClientService, IrtClientService>();

// [FIX P1] IMemoryCache — dùng cho GeminiSolverService (TTL eviction thay static dict)
builder.Services.AddMemoryCache();

// [FIX] GeminiGradingService dùng IHttpClientFactory (không còn new HttpClient)
builder.Services.AddScoped<IGeminiGradingService, GeminiGradingService>();
builder.Services.AddScoped<IGeminiMentorService, GeminiMentorService>();
builder.Services.AddScoped<IGeminiSolverService>(sp =>
    new GeminiSolverService(
        builder.Configuration["Gemini:ApiKey"] ?? "",
        sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>()));

// [Kịch bản B] AI Question Generator từ tài liệu PDF / Word / Text
builder.Services.AddScoped<IAiQuestionGeneratorService>(sp =>
    new AiQuestionGeneratorService(
        builder.Configuration["Gemini:ApiKey"] ?? "",
        sp.GetRequiredService<IHttpClientFactory>(),
        sp.GetRequiredService<IPdfExtractorService>(),
        sp.GetRequiredService<IDocxParserService>(),
        sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>()));

// [Universal Ingestion Engine Phase 4] AI Rule Autopilot (Gemini 1.5 Flash + Local Heuristic Sniffer)
builder.Services.AddScoped<IAiRuleAutopilotService, GeminiRuleAutopilotService>();

// [Phase B] Multi-Tenancy — Scoped per request
builder.Services.AddScoped<AegisQuiz.Application.Interfaces.ITenantContext,
                            AegisQuiz.Infrastructure.Services.TenantContext>();

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

// [World-Class Upgrade] Server-authoritative exam timer (học từ quiz-service)
builder.Services.AddHostedService<ExamTimerService>();

// Controllers + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// [CG2: Cảm biến Đe dọa] [CG6: Định danh Tuyệt đối]
// JWT Bearer — Dual-Issuer: PKI (Symmetric HS256) + Keycloak (Asymmetric RS256 via JWKS)
var isDevelopment = builder.Environment.IsDevelopment();
var oidcAuthority = builder.Configuration["Oidc:Authority"] ?? "http://localhost:8180/realms/dehoc";
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("[CG2] Jwt:Secret is not configured. Set via env var JWT_SECRET.");

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
builder.Services.AddAuthorization();

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
app.MapHealthChecks("/health").RequireRateLimiting("health-check");
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
}).RequireRateLimiting("health-check");

// [World-Class Upgrade] Database Migration — FAIL FAST pattern (học từ quiz-service)
using (var scope = app.Services.CreateScope())
{
    var db  = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        if (db.Database.IsNpgsql())
        {
            // [BƯỚC 1: AUTO-HEALING DDL CHẠY TRƯỚC TIÊN] Đảm bảo 100% các cột mới (DepthLevel, MaterializedPath,...) và bảng mới luôn tồn tại
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    -- 1. BẢNG BankTopics: Đảm bảo đầy đủ các cột cây phân cấp 2026
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""DepthLevel"" integer NOT NULL DEFAULT 0;
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""MaterializedPath"" text NULL;
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""Scope"" text NOT NULL DEFAULT 'COMMUNITY';
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""DomainCode"" text NOT NULL DEFAULT 'GENERAL';
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""DisplayOrder"" integer NOT NULL DEFAULT 0;
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""QuestionCountCached"" integer NOT NULL DEFAULT 0;
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""Urn"" text NULL;
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""ParentId"" uuid NULL;
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""AllowedTenantIds"" text[] NOT NULL DEFAULT '{}';
                    ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                    
                    -- 2. BẢNG Questions: Đảm bảo đầy đủ toạ độ tri thức
                    ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""ContextId"" uuid NULL;
                    ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""DomainCode"" text NOT NULL DEFAULT 'EDUCATION';
                    ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""IsCritical"" boolean NOT NULL DEFAULT false;
                    ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""SubCategory"" text NULL;
                    ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""Tags"" text[] NOT NULL DEFAULT '{}';
                    ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

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
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserAccounts_Email"" ON ""UserAccounts"" (""Email"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserAccounts_TenantId"" ON ""UserAccounts"" (""TenantId"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserAccounts_OrgUnitId"" ON ""UserAccounts"" (""OrgUnitId"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserAccounts_Role"" ON ""UserAccounts"" (""Role"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserAccounts_SubscriptionTier"" ON ""UserAccounts"" (""SubscriptionTier"");
                ");
                log.LogInformation("[AegisQuiz] Auto-healing DDL executed successfully. All columns and tables guaranteed.");
            }
            catch (Exception exDdl)
            {
                log.LogWarning(exDdl, "[AegisQuiz] Auto-healing DDL warning: {Message}", exDdl.Message);
            }

            // [BƯỚC 2: TIẾN HÀNH MIGRATIONS]
            try
            {
                db.Database.Migrate();
                log.LogInformation("[AegisQuiz] PostgreSQL database migrations applied successfully.");
            }
            catch (Exception exMigrate)
            {
                log.LogInformation("[AegisQuiz] EF migration notice: {Message}", exMigrate.Message);
            }
        }
        else
        {
            db.Database.EnsureCreated();
            log.LogInformation("[AegisQuiz] InMemory database created successfully for zero-friction local execution.");
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
// app.MapHub<PaymentHub>("/hubs/payment"); // Sẽ implement ở Sprint 2

Log.Information("[AegisQuiz] Application started successfully.");
app.Run();
