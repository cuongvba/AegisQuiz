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

// CORS (Cho phép Frontend Next.js gọi API)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Cần cho SignalR
    });
});

// Problem Details (standardized error responses)
builder.Services.AddProblemDetails();

var app = builder.Build();

// ===== MIDDLEWARE PIPELINE =====
if (app.Environment.IsDevelopment())
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
            db.Database.Migrate();
            log.LogInformation("[AegisQuiz] PostgreSQL database migrations applied successfully.");
        }
        else
        {
            db.Database.EnsureCreated();
            log.LogInformation("[AegisQuiz] InMemory database created successfully for zero-friction local execution.");
        }

        // Tự động nạp bộ câu hỏi toán thực tế từ DeToanGiaiChiTiet.docx nếu có
        string sampleDocx = @"D:\Cuong\DuAn\mybank\AegisQuiz\DeToanGiaiChiTiet.docx";
        if (File.Exists(sampleDocx) && db.Questions.Count() <= 10)
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

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication(); // [Cảnh giới 6: Đã tích hợp JWT SSO]
app.UseAuthorization();

// [Phase B] Multi-Tenant resolution — sau Authentication để có JWT claims
app.UseMiddleware<AegisQuiz.Infrastructure.Services.TenantMiddleware>();

app.MapControllers();

// [World-Class Upgrade] SignalR Proctoring Hub (học từ quiz-service)
app.MapHub<ExamProctoringHub>("/hubs/proctoring");
// [Aegis Arena] Real-time Gameshow Arena Hub
app.MapHub<ArenaHub>("/hubs/arena");
// app.MapHub<PaymentHub>("/hubs/payment"); // Sẽ implement ở Sprint 2

Log.Information("[AegisQuiz] Application started successfully.");
app.Run();
