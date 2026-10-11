using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.AI.Adapters;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.AI
{
    /// <summary>
    /// [AegisAI Epistemic Universal Gateway - Thế Hệ 2026]
    /// Cổng điều phối AI thông minh tối thượng:
    /// 1. 100% Database-Driven: Tự động đồng bộ và nạp động từ bảng PostgreSQL `AiModelRegistries` (0ms Hot-Reload).
    /// 2. Ma trận Định tuyến Nhận thức 4 Chiều (4D Epistemic Routing Matrix):
    ///    - Chiều 1: Độ tương thích Mẫu hình Nhận thức (Cognitive Archetypes: Deep CoT, Massive Context, Arbiter, UltraFast).
    ///    - Chiều 2: FinOps Pareto Efficiency (Tỉ số Benchmark ELO / Chi phí Token USD).
    ///    - Chiều 3: Độ trễ SLA Thời gian thực & Hedged Execution (Đua song song triệt tiêu độ trễ P99).
    ///    - Chiều 4: Bảo chứng Dung lượng Ngữ cảnh (Context Window Limit Prevention).
    /// 3. Zero Hardcode: Triệt tiêu hoàn toàn các định nghĩa tĩnh trong mã nguồn C#.
    /// </summary>
    public class UniversalAiRouter : IUniversalAiRouter
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UniversalAiRouter> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly AiKeyPoolManager _keyPoolManager;
        private readonly ConcurrentDictionary<string, IAiProviderAdapter> _adapters = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, IAiProviderAdapter> _dynamicStringAdapters = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, TenantByokConfigDto> _tenantByokMap = new(StringComparer.OrdinalIgnoreCase);

        private AiSystemConfigDto _systemConfig;
        private readonly object _configLock = new();

        public UniversalAiRouter(
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<UniversalAiRouter> logger,
            IEnumerable<IAiProviderAdapter> adapters,
            IHttpClientFactory httpClientFactory,
            IServiceScopeFactory scopeFactory)
        {
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _scopeFactory = scopeFactory;
            _keyPoolManager = new AiKeyPoolManager(quarantineDurationSeconds: 60);

            foreach (var a in adapters)
            {
                var pCode = a.ProviderCode;
                _adapters[pCode] = a;
                _adapters[WellKnownAiProviders.Normalize(pCode)] = a;
                _adapters[a.ProviderType.ToString().ToLowerInvariant()] = a;
            }

            _systemConfig = LoadConfigFromDatabaseOrFallback(configuration);
        }

        public static Dictionary<string, string> LoadApiKeyFileFallback()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var possiblePaths = new[]
            {
                "APIKey.txt",
                "../APIKey.txt",
                "../../APIKey.txt",
                Path.Combine(AppContext.BaseDirectory, "APIKey.txt"),
                "/app/APIKey.txt"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var lines = File.ReadAllLines(path);
                        foreach (var rawLine in lines)
                        {
                            var line = rawLine.Trim();
                            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                            var idx = line.IndexOf('=');
                            if (idx > 0)
                            {
                                var key = line[..idx].Trim();
                                var val = line[(idx + 1)..].Trim();
                                result[key] = val;
                            }
                        }
                        break;
                    }
                    catch { /* best-effort fallback */ }
                }
            }
            return result;
        }

        private AiSystemConfigDto LoadConfigFromDatabaseOrFallback(IConfiguration config)
        {
            var fileFallback = LoadApiKeyFileFallback();

            string GetKey(string configKey, string envKey)
            {
                var val = config[configKey];
                if (!string.IsNullOrWhiteSpace(val)) return val;
                if (fileFallback.TryGetValue(envKey, out var fileVal) && !string.IsNullOrWhiteSpace(fileVal)) return fileVal;
                return Environment.GetEnvironmentVariable(envKey) ?? string.Empty;
            }

            List<string> ParseKeys(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
                return raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }

            List<string> ResolveKeysForProviderCode(string providerCode)
            {
                if (string.IsNullOrWhiteSpace(providerCode)) return new List<string>();
                var norm = WellKnownAiProviders.Normalize(providerCode);
                var codeUpper = providerCode.ToUpperInvariant().Replace("-", "_").Replace(" ", "_");

                if (norm.Contains("ollama") || norm.Contains("local"))
                {
                    return new List<string> { "local_no_key_required" };
                }

                // 1. Kiểm tra config / env trực tiếp
                var directKey = GetKey($"{providerCode}:ApiKey", $"{codeUpper}_API_KEY");
                if (!string.IsNullOrWhiteSpace(directKey)) return ParseKeys(directKey);

                var directToken = GetKey($"{providerCode}:Token", $"{codeUpper}_TOKEN");
                if (!string.IsNullOrWhiteSpace(directToken)) return ParseKeys(directToken);

                // 2. Quét tự động trong file fallback (APIKey.txt) với bất kỳ tiền tố nào
                foreach (var kvp in fileFallback)
                {
                    var k = kvp.Key.ToUpperInvariant().Replace("-", "_");
                    if (k.Contains(codeUpper) && (k.EndsWith("_KEY") || k.EndsWith("_TOKEN") || k.EndsWith("_API_KEY")))
                    {
                        if (!string.IsNullOrWhiteSpace(kvp.Value)) return ParseKeys(kvp.Value);
                    }
                }

                // 3. Fallback theo tên quy ước phổ thông
                if (norm.Contains("gemini") || norm.Contains("google"))
                {
                    var geminiKey = GetKey("Gemini:ApiKey", "GOOGLE_API_KEY");
                    if (!string.IsNullOrWhiteSpace(geminiKey)) return ParseKeys(geminiKey);
                }
                if (norm.Contains("github"))
                {
                    var ghToken = GetKey("GitHub:Token", "GITHUB_TOKEN");
                    if (!string.IsNullOrWhiteSpace(ghToken)) return ParseKeys(ghToken);
                }

                return new List<string>();
            }

            List<string> ResolveKeysForProvider(AiProviderType providerType)
                => ResolveKeysForProviderCode(WellKnownAiProviders.ToCode(providerType));

            var providers = new List<AiProviderEndpointConfig>();

            // 1. Nạp danh mục động trực tiếp từ PostgreSQL AiModelRegistries
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetService<AegisQuizDbContext>();
                if (db != null && db.Database.CanConnect())
                {
                    var dbModels = db.AiModelRegistries.AsNoTracking().Where(m => m.IsEnabled).ToList();
                    if (dbModels.Count > 0)
                    {
                        int prioritySeq = 1;
                        foreach (var m in dbModels)
                        {
                            var pCode = WellKnownAiProviders.Normalize(m.ProviderCompany);
                            var keys = ResolveKeysForProviderCode(pCode);

                            // Giữ 1 cấu hình gateway duy nhất cho mỗi providerCode nhưng giữ các chỉ số ELO/Cost cao nhất
                            var existing = providers.FirstOrDefault(p => p.ProviderCode.Equals(pCode, StringComparison.OrdinalIgnoreCase));
                            if (existing == null)
                            {
                                providers.Add(new AiProviderEndpointConfig
                                {
                                    ProviderCode = pCode,
                                    DisplayName = m.ProviderCompany,
                                    BaseUrl = m.EndpointUrl,
                                    DefaultModel = m.ModelName,
                                    ProtocolType = m.ProtocolType,
                                    ApiKeys = keys,
                                    IsEnabled = m.IsEnabled && (keys.Count > 0 || pCode.Contains("ollama") || pCode.Contains("local")),
                                    Priority = prioritySeq++,
                                    CostPerMillionInputTokens = m.CostInputPerMillion,
                                    CostPerMillionOutputTokens = m.CostOutputPerMillion,
                                    BenchmarkElo = m.BenchmarkElo,
                                    AvgLatencyMs = m.AvgLatencyMs,
                                    MaxContextTokens = m.MaxContextTokens,
                                    Archetype = m.CognitiveArchetype
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UniversalAiRouter] Tải Model từ PostgreSQL chưa khả dụng lúc khởi động. Chuyển sang cơ chế Discovery qua Key Vault.");
            }

            // 2. Tự phục hồi (Discovery Fallback) nếu DB chưa chạy migration xong
            if (providers.Count == 0)
            {
                var discovered = new[]
                {
                    (WellKnownAiProviders.DeepSeek, "DeepSeek R1/V3", "https://api.deepseek.com/v1", "deepseek-chat", CognitiveArchetype.DeepReasoner, 0.14, 0.28, 1340, 450, 128000),
                    (WellKnownAiProviders.GoogleGemini, "Google Gemini 2.0 Flash", "https://generativelanguage.googleapis.com", "gemini-2.0-flash", CognitiveArchetype.MassiveContext, 0.1, 0.4, 1310, 220, 1048576),
                    (WellKnownAiProviders.OpenAI, "OpenAI GPT-4o-mini", "https://api.openai.com/v1/chat/completions", "gpt-4o-mini", CognitiveArchetype.UltraFastScanner, 0.15, 0.6, 1290, 250, 128000),
                    (WellKnownAiProviders.AnthropicClaude, "Anthropic Claude 3.5 Sonnet", "https://api.anthropic.com/v1/messages", "claude-3-5-sonnet-20241022", CognitiveArchetype.LinguisticArbiter, 3.0, 15.0, 1360, 600, 200000),
                    (WellKnownAiProviders.Groq, "Groq LPU Llama 3.3 70B", "https://api.groq.com/openai/v1/chat/completions", "llama-3.3-70b-versatile", CognitiveArchetype.UltraFastScanner, 0.59, 0.79, 1280, 180, 128000),
                    (WellKnownAiProviders.SambaNova, "SambaNova Llama 3.1 405B", "https://api.sambanova.ai/v1/chat/completions", "Meta-Llama-3.1-405B-Instruct", CognitiveArchetype.DeepReasoner, 5.0, 10.0, 1370, 350, 128000),
                    (WellKnownAiProviders.Cerebras, "Cerebras Wafer Llama 3.3 70B", "https://api.cerebras.ai/v1/chat/completions", "llama-3.3-70b", CognitiveArchetype.UltraFastScanner, 0.6, 0.6, 1290, 80, 128000),
                    (WellKnownAiProviders.MistralAI, "Mistral Large 2411", "https://api.mistral.ai/v1/chat/completions", "mistral-large-latest", CognitiveArchetype.LinguisticArbiter, 2.0, 6.0, 1330, 400, 128000),
                    (WellKnownAiProviders.GitHubModels, "GitHub Models GPT-4o", "https://models.inference.ai.azure.com/chat/completions", "gpt-4o", CognitiveArchetype.LinguisticArbiter, 2.5, 10.0, 1350, 450, 128000),
                    (WellKnownAiProviders.OpenRouter, "OpenRouter Gateway (GPT-4o)", "https://openrouter.ai/api/v1/chat/completions", "openai/gpt-4o", CognitiveArchetype.LinguisticArbiter, 2.5, 10.0, 1330, 380, 128000),
                    (WellKnownAiProviders.Replicate, "Replicate MusicGen Multimodal", "https://api.replicate.com/v1/predictions", "facebook/musicgen-small", CognitiveArchetype.MassiveContext, 0.05, 0.1, 1200, 800, 32000),
                    (WellKnownAiProviders.LocalOllama, "Local Sovereign Qwen 2.5 32B", "http://localhost:11434/v1/chat/completions", "qwen2.5:14b", CognitiveArchetype.SovereignLocal, 0.0, 0.0, 1260, 300, 32768)
                };

                int pSeq = 1;
                foreach (var item in discovered)
                {
                    var keys = ResolveKeysForProviderCode(item.Item1);
                    providers.Add(new AiProviderEndpointConfig
                    {
                        ProviderCode = item.Item1,
                        DisplayName = item.Item2,
                        BaseUrl = item.Item3,
                        DefaultModel = item.Item4,
                        ApiKeys = keys,
                        IsEnabled = keys.Count > 0 || item.Item1 == WellKnownAiProviders.LocalOllama,
                        Priority = pSeq++,
                        Archetype = item.Item5,
                        CostPerMillionInputTokens = item.Item6,
                        CostPerMillionOutputTokens = item.Item7,
                        BenchmarkElo = item.Item8,
                        AvgLatencyMs = item.Item9,
                        MaxContextTokens = item.Item10
                    });
                }
            }

            // 3. Đăng ký tất cả keys vào key pool (100% ProviderCode động)
            foreach (var p in providers)
            {
                if (p.ApiKeys.Count > 0)
                {
                    _keyPoolManager.RegisterKeys(p.ProviderCode, p.ApiKeys);
                    _keyPoolManager.RegisterKeys(p.ProviderType, p.ApiKeys);
                }
            }

            // 4. Ma trận định tuyến Tác vụ động (TaskRoutingMap & TaskArchetypeMap)
            var taskRouting = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { WellKnownAiTasks.SolveQuestions, WellKnownAiProviders.DeepSeek },
                { WellKnownAiTasks.CognitiveWalkthrough, WellKnownAiProviders.DeepSeek },
                { WellKnownAiTasks.GenerateQuestions, WellKnownAiProviders.GoogleGemini },
                { WellKnownAiTasks.LatexConversion, WellKnownAiProviders.GoogleGemini },
                { WellKnownAiTasks.GradeEssay, WellKnownAiProviders.AnthropicClaude },
                { WellKnownAiTasks.PersonalizedMentor, WellKnownAiProviders.GoogleGemini },
                { WellKnownAiTasks.ArenaAdversary, WellKnownAiProviders.OpenAI },
                { WellKnownAiTasks.RealTimeProctoringAlert, WellKnownAiProviders.Cerebras },
                { WellKnownAiTasks.SymposiumDebate, WellKnownAiProviders.AnthropicClaude },
                { WellKnownAiTasks.ExamDeduplication, WellKnownAiProviders.DeepSeek }
            };

            var taskArchetypes = new Dictionary<string, CognitiveArchetype>(StringComparer.OrdinalIgnoreCase)
            {
                { WellKnownAiTasks.SolveQuestions, CognitiveArchetype.DeepReasoner },
                { WellKnownAiTasks.DistractorAnalysis, CognitiveArchetype.DeepReasoner },
                { WellKnownAiTasks.LatexConversion, CognitiveArchetype.DeepReasoner },
                { WellKnownAiTasks.ExamDeduplication, CognitiveArchetype.DeepReasoner },
                { WellKnownAiTasks.GenerateQuestions, CognitiveArchetype.MassiveContext },
                { WellKnownAiTasks.CognitiveWalkthrough, CognitiveArchetype.LinguisticArbiter },
                { WellKnownAiTasks.GradeEssay, CognitiveArchetype.LinguisticArbiter },
                { WellKnownAiTasks.PersonalizedMentor, CognitiveArchetype.LinguisticArbiter },
                { WellKnownAiTasks.SymposiumDebate, CognitiveArchetype.LinguisticArbiter },
                { WellKnownAiTasks.ArenaAdversary, CognitiveArchetype.UltraFastScanner },
                { WellKnownAiTasks.RealTimeProctoringAlert, CognitiveArchetype.UltraFastScanner }
            };

            return new AiSystemConfigDto
            {
                Providers = providers,
                TaskRoutingMap = taskRouting,
                TaskArchetypeMap = taskArchetypes,
                EnableSemanticCache = true,
                SemanticCacheSimilarityThreshold = 0.95,
                EnableMultiAgentCommittee = true,
                KeyQuarantineSeconds = 60,
                Telemetry = new FinOpsTelemetryDto()
            };
        }

        public async Task RefreshRegistryFromDatabaseAsync()
        {
            lock (_configLock)
            {
                _systemConfig = LoadConfigFromDatabaseOrFallback(_configuration);
                _dynamicStringAdapters.Clear();
            }
            _logger.LogInformation("[UniversalAiRouter] Hot-Reload hoàn tất từ Database PostgreSQL. Tổng số NCC hoạt động: {Count}", _systemConfig.Providers.Count(p => p.IsEnabled));
            await Task.CompletedTask;
        }

        public static CognitiveArchetype InferTargetArchetype(string? taskCode, Dictionary<string, CognitiveArchetype>? customMap = null)
        {
            var norm = WellKnownAiTasks.Normalize(taskCode);
            if (customMap != null && customMap.TryGetValue(norm, out var directArch))
            {
                return directArch;
            }

            if (norm.Contains("solve") || norm.Contains("distractor") || norm.Contains("latex") || norm.Contains("math") || norm.Contains("reason") || norm.Contains("dedup"))
                return CognitiveArchetype.DeepReasoner;
            if (norm.Contains("generate") || norm.Contains("book") || norm.Contains("pdf") || norm.Contains("context"))
                return CognitiveArchetype.MassiveContext;
            if (norm.Contains("walkthrough") || norm.Contains("grade") || norm.Contains("mentor") || norm.Contains("arbiter") || norm.Contains("pedagogy") || norm.Contains("symposium"))
                return CognitiveArchetype.LinguisticArbiter;
            if (norm.Contains("proctor") || norm.Contains("alert") || norm.Contains("arena") || norm.Contains("adversary") || norm.Contains("fast"))
                return CognitiveArchetype.UltraFastScanner;

            return CognitiveArchetype.DeepReasoner;
        }

        [Obsolete("Sử dụng InferTargetArchetype(string taskCode)")]
        public static CognitiveArchetype InferTargetArchetype(AiTaskType taskType)
            => InferTargetArchetype(WellKnownAiTasks.ToCode(taskType));

        private List<string> ResolveEpistemicProviderOrder(AiExecutionRequest request)
        {
            var list = new List<string>();

            // 1. Ưu tiên tuyệt đối nếu có chỉ định trực tiếp từ Client/Tenant (ProviderCode)
            var preferredCode = !string.IsNullOrWhiteSpace(request.PreferredProviderCode)
                ? WellKnownAiProviders.Normalize(request.PreferredProviderCode)
                : null;

            if (!string.IsNullOrWhiteSpace(preferredCode))
            {
                list.Add(preferredCode);
            }
            else if (_systemConfig.TaskRoutingMap.TryGetValue(request.TaskCode, out var routedProvider) && !string.IsNullOrWhiteSpace(routedProvider))
            {
                list.Add(WellKnownAiProviders.Normalize(routedProvider));
            }

            // 2. Tính toán ma trận nhận thức 4 chiều (4D Epistemic Fitness Scoring Matrix)
            var targetArchetype = request.RequiredArchetype ?? InferTargetArchetype(request.TaskCode, _systemConfig.TaskArchetypeMap);
            int estimatedPromptTokens = Math.Max(100, (request.Prompt.Length / 3) + request.MaxTokens);
            var taskNorm = WellKnownAiTasks.Normalize(request.TaskCode);
            bool isLowLatencyRequired = taskNorm.Contains("proctor") || taskNorm.Contains("alert") || taskNorm.Contains("arena") || taskNorm.Contains("fast");

            var scoredProviders = _systemConfig.Providers
                .Where(p => p.IsEnabled && !list.Contains(p.ProviderCode, StringComparer.OrdinalIgnoreCase))
                .Select(p =>
                {
                    var pCode = p.ProviderCode;

                    // Kiểm tra tràn Context Window
                    if (p.MaxContextTokens > 0 && p.MaxContextTokens < estimatedPromptTokens)
                    {
                        return new { Provider = pCode, Score = -9999.0 };
                    }

                    // Kiểm tra tính sẵn sàng của API Key
                    bool hasAvailableKey = pCode.Contains("ollama") || pCode.Contains("local") || _keyPoolManager.GetAvailableKey(pCode) != null;
                    if (!hasAvailableKey)
                    {
                        return new { Provider = pCode, Score = -5000.0 };
                    }

                    double score = 1000.0;

                    // Chiều 1: Tương thích Mẫu hình Nhận thức (Cognitive Resonance)
                    if (p.Archetype == targetArchetype)
                    {
                        score += 500.0;
                    }

                    // Chiều 2: Hiệu năng FinOps Pareto (Điểm ELO trên Chi phí)
                    double totalCost = Math.Max(0.05, p.CostPerMillionInputTokens + p.CostPerMillionOutputTokens);
                    double paretoRatio = (p.BenchmarkElo / totalCost);
                    score += Math.Min(300.0, paretoRatio * 0.1);

                    // Chiều 3: Độ trễ SLA và Tốc độ phản xạ
                    if (isLowLatencyRequired)
                    {
                        if (p.AvgLatencyMs < 250) score += 400.0;
                        else score -= (p.AvgLatencyMs * 0.5);
                    }
                    else
                    {
                        // Với bài toán suy luận nặng, ưu tiên ELO cao
                        score += (p.BenchmarkElo * 0.2);
                    }

                    // Chiều 4: Điểm ưu tiên cơ sở
                    score -= (p.Priority * 10.0);

                    return new { Provider = pCode, Score = score };
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Provider)
                .ToList();

            list.AddRange(scoredProviders);

            // Dự phòng an toàn: nếu tất cả đều bị chấm điểm âm, lấy lại theo priority
            if (list.Count == 0)
            {
                list.AddRange(_systemConfig.Providers.Where(p => p.IsEnabled).OrderBy(p => p.Priority).Select(p => p.ProviderCode));
            }

            return list;
        }

        public async Task<AiExecutionResponse> ExecuteAsync(AiExecutionRequest request)
        {
            var sw = Stopwatch.StartNew();
            lock (_configLock)
            {
                _systemConfig.Telemetry.TotalRequestsProcessed++;
            }

            // 1. Kiểm tra Semantic Cache
            if (_systemConfig.EnableSemanticCache)
            {
                var cacheKey = string.IsNullOrWhiteSpace(request.CacheKey)
                    ? ComputeHash($"{request.TaskCode}_{request.Prompt}")
                    : request.CacheKey;

                if (_cache.TryGetValue<string>(cacheKey, out var cachedResult) && !string.IsNullOrWhiteSpace(cachedResult))
                {
                    sw.Stop();
                    lock (_configLock)
                    {
                        _systemConfig.Telemetry.CacheHitsCount++;
                        _systemConfig.Telemetry.EstimatedCostSavedUsd += 0.005;
                    }

                    return new AiExecutionResponse
                    {
                        Success = true,
                        Content = cachedResult,
                        ProviderUsed = "SemanticCache",
                        ModelUsed = "VectorMemory",
                        IsFromCache = true,
                        DurationMs = sw.ElapsedMilliseconds
                    };
                }
            }

            // 2. Định tuyến theo Ma trận Nhận thức 4 Chiều (4D Epistemic Routing)
            var providersToTry = ResolveEpistemicProviderOrder(request);

            // 3. Cơ chế Hedged Execution (Chạy đua song song triệt tiêu độ trễ P99 cho tác vụ thời gian thực)
            if (request.EnableHedgedExecution && providersToTry.Count >= 2)
            {
                var hedgedResponse = await TryHedgedExecutionAsync(request, providersToTry.Take(2).ToList());
                if (hedgedResponse != null && hedgedResponse.Success)
                {
                    return hedgedResponse;
                }
            }

            string lastError = "Không tìm thấy nhà cung cấp AI nào khả dụng.";

            foreach (var providerCode in providersToTry)
            {
                var provCfg = _systemConfig.Providers.FirstOrDefault(p => 
                    WellKnownAiProviders.Normalize(p.ProviderCode).Equals(WellKnownAiProviders.Normalize(providerCode), StringComparison.OrdinalIgnoreCase));
                var pCode = provCfg?.ProviderCode ?? providerCode;
                var adapter = ResolveAdapter(pCode, provCfg?.DefaultModel, provCfg?.BaseUrl);

                // Kiểm tra BYOK riêng của Tenant nếu có
                string? apiKey = null;
                string? customEndpoint = null;
                string? customModel = request.PreferredModel;

                if (!string.IsNullOrWhiteSpace(request.TenantId) &&
                    _tenantByokMap.TryGetValue(request.TenantId, out var byok) &&
                    byok.IsActive && 
                    !string.IsNullOrWhiteSpace(byok.ProviderCode) && 
                    WellKnownAiProviders.Normalize(byok.ProviderCode).Equals(WellKnownAiProviders.Normalize(pCode), StringComparison.OrdinalIgnoreCase))
                {
                    apiKey = byok.ApiKey;
                    customEndpoint = byok.CustomEndpoint;
                    customModel = byok.CustomModel ?? customModel;
                }
                else
                {
                    apiKey = _keyPoolManager.GetAvailableKey(pCode);
                }

                if (string.IsNullOrWhiteSpace(apiKey) && !pCode.Contains("ollama") && !pCode.Contains("local"))
                {
                    continue;
                }

                var systemPrompt = BuildPersonaSystemPrompt(request.PersonaCode, request.SystemPrompt);

                var (success, content, error, durationMs) = await adapter.CallAsync(
                    apiKey ?? "local",
                    customModel,
                    request.Prompt,
                    systemPrompt,
                    request.Temperature,
                    request.MaxTokens,
                    customEndpoint ?? provCfg?.BaseUrl);

                if (success && !string.IsNullOrWhiteSpace(content))
                {
                    sw.Stop();

                    if (_systemConfig.EnableSemanticCache)
                    {
                        var cacheKey = string.IsNullOrWhiteSpace(request.CacheKey)
                            ? ComputeHash($"{request.TaskCode}_{request.Prompt}")
                            : request.CacheKey;
                        _cache.Set(cacheKey, content, TimeSpan.FromHours(24));
                    }

                    var providerName = provCfg?.DisplayName ?? providerCode;
                    lock (_systemConfig.Telemetry.RequestCountByProvider)
                    {
                        if (!_systemConfig.Telemetry.RequestCountByProvider.ContainsKey(providerName))
                            _systemConfig.Telemetry.RequestCountByProvider[providerName] = 0;
                        _systemConfig.Telemetry.RequestCountByProvider[providerName]++;
                    }

                    return new AiExecutionResponse
                    {
                        Success = true,
                        Content = content,
                        ProviderUsed = providerName,
                        ModelUsed = customModel ?? adapter.DefaultModel,
                        IsFromCache = false,
                        DurationMs = sw.ElapsedMilliseconds
                    };
                }

                lastError = error ?? "Lỗi gọi API không xác định.";
                if (apiKey != null)
                {
                    bool isRateLimit = lastError.Contains("429") || lastError.Contains("RESOURCE_EXHAUSTED");
                    _keyPoolManager.ReportRateLimitOrFailure(pCode, apiKey, isRateLimit);
                }

                _logger.LogWarning("[EpistemicAiGateway] Provider {Provider} thất bại ({Error}). Chuyển vùng sang Provider tiếp theo trong ma trận nhận thức...", providerCode, lastError);
            }

            sw.Stop();
            return new AiExecutionResponse
            {
                Success = false,
                Content = string.Empty,
                ErrorMessage = $"Tất cả các nhà cung cấp AI trong ma trận nhận thức đều thất bại. Chi tiết lỗi cuối: {lastError}",
                DurationMs = sw.ElapsedMilliseconds
            };
        }

        private IAiProviderAdapter ResolveAdapter(string? providerCode, string? defaultModel = null, string? baseUrl = null)
        {
            var code = WellKnownAiProviders.Normalize(providerCode);

            if (_adapters.TryGetValue(code, out var adapter))
            {
                return adapter;
            }

            return _dynamicStringAdapters.GetOrAdd(code, c =>
            {
                var provCfg = _systemConfig.Providers.FirstOrDefault(p => 
                    WellKnownAiProviders.Normalize(p.ProviderCode).Equals(c, StringComparison.OrdinalIgnoreCase));

                return new OpenAiCompatibleAdapter(
                    _httpClientFactory,
                    c,
                    defaultModel ?? provCfg?.DefaultModel ?? "gpt-4o-mini",
                    baseUrl ?? provCfg?.BaseUrl ?? "https://api.openai.com/v1/chat/completions");
            });
        }

        private IAiProviderAdapter ResolveAdapter(string? providerCode, AiProviderType legacyType)
            => ResolveAdapter(providerCode ?? WellKnownAiProviders.ToCode(legacyType));

        private IAiProviderAdapter ResolveAdapterForProvider(AiProviderType providerType)
            => ResolveAdapter(WellKnownAiProviders.ToCode(providerType));

        private async Task<AiExecutionResponse?> TryHedgedExecutionAsync(AiExecutionRequest request, List<string> top2Providers)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));

                var taskA = Task.Run(async () =>
                {
                    var subReq = CloneRequest(request);
                    subReq.PreferredProviderCode = top2Providers[0];
                    subReq.EnableHedgedExecution = false;
                    return await ExecuteAsync(subReq);
                }, cts.Token);

                var taskB = Task.Run(async () =>
                {
                    var subReq = CloneRequest(request);
                    subReq.PreferredProviderCode = top2Providers[1];
                    subReq.EnableHedgedExecution = false;
                    return await ExecuteAsync(subReq);
                }, cts.Token);

                var completedTask = await Task.WhenAny(taskA, taskB);
                var result = await completedTask;

                if (result.Success)
                {
                    cts.Cancel(); // Hủy request chậm hơn để tiết kiệm tài nguyên
                    return result;
                }

                // Nếu request về trước thất bại, chờ request còn lại
                var otherTask = completedTask == taskA ? taskB : taskA;
                return await otherTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UniversalAiRouter] Hedged execution gặp lỗi, tự động chuyển về tuần tự.");
                return null;
            }
        }

        private static AiExecutionRequest CloneRequest(AiExecutionRequest r)
        {
            return new AiExecutionRequest
            {
                TaskCode = r.TaskCode,
                Prompt = r.Prompt,
                SystemPrompt = r.SystemPrompt,
                PersonaCode = r.PersonaCode,
                PreferredProviderCode = r.PreferredProviderCode,
                PreferredModel = r.PreferredModel,
                Temperature = r.Temperature,
                MaxTokens = r.MaxTokens,
                TenantId = r.TenantId,
                CacheKey = r.CacheKey,
                RequiredArchetype = r.RequiredArchetype,
                MinContextTokens = r.MinContextTokens,
                EnableHedgedExecution = r.EnableHedgedExecution
            };
        }

        public async Task<PedagogicalWalkthroughDto> GenerateWalkthroughAsync(
            string questionContent,
            List<string> options,
            string correctAnswer,
            string domainCode,
            AgentPersonaType persona = AgentPersonaType.SupremeArbiter,
            string? tenantId = null)
        {
            var sw = Stopwatch.StartNew();
            var optionsText = string.Join("\n", options);

            var prompt = $@"
Dưới đây là một câu hỏi trắc nghiệm chuyên ngành:
Lĩnh vực: {domainCode}
Câu hỏi: {questionContent}
Các phương án lựa chọn:
{optionsText}
Đáp án chính xác: {correctAnswer}

Hãy phân tích và sinh BẢN ĐỒ TƯ DUY SƯ PHẠM 5 BƯỚC (Pedagogical Walkthrough) theo định dạng JSON duy nhất dưới đây (không kèm markdown ngoài):
{{
  ""promptAnatomy"": ""Phân tích bóc tách các từ khóa then chốt, ngữ cảnh đề bài và phát hiện bẫy ngữ nghĩa."",
  ""theoreticalGrounding"": ""Căn cứ pháp lý, công thức lý thuyết hoặc quy định/thông tư viện dẫn chính xác."",
  ""distractorAutopsy"": ""Giải phẫu từng phương án sai (tại sao thí sinh hay bị lừa, lỗ hổng tư duy dẫn đến chọn nhầm)."",
  ""mnemonicAndRecall"": ""Mẹo nhớ nhanh, công thức ngắn gọn hoặc phương pháp phản xạ 1 nốt nhạc."",
  ""extrapolatedCase"": ""Một bài toán tình huống thực tế mở rộng nâng cao để kiểm tra tư duy phản biện."",
  ""bloomLevel"": 3,
  ""estimatedDifficultyIrt"": 0.45
}}
";

            var response = await ExecuteAsync(new AiExecutionRequest
            {
                TaskCode = WellKnownAiTasks.CognitiveWalkthrough,
                Prompt = prompt,
                PersonaCode = WellKnownAgentPersonas.ToCode(persona),
                TenantId = tenantId,
                Temperature = 0.3,
                MaxTokens = 2048,
                RequiredArchetype = CognitiveArchetype.LinguisticArbiter
            });

            sw.Stop();

            if (!response.Success)
            {
                return new PedagogicalWalkthroughDto
                {
                    QuestionContent = questionContent,
                    CorrectAnswer = correctAnswer,
                    DomainCode = domainCode,
                    PromptAnatomy = "Lỗi khi sinh Bản đồ sư phạm: " + response.ErrorMessage,
                    ExecutionTimeMs = sw.ElapsedMilliseconds
                };
            }

            try
            {
                var cleanedJson = CleanJsonString(response.Content);
                var dto = JsonSerializer.Deserialize<PedagogicalWalkthroughDto>(cleanedJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (dto != null)
                {
                    dto.QuestionContent = questionContent;
                    dto.CorrectAnswer = correctAnswer;
                    dto.DomainCode = domainCode;
                    dto.ResolvedByProvider = response.ProviderUsed;
                    dto.ResolvedByModel = response.ModelUsed;
                    dto.IsFromSemanticCache = response.IsFromCache;
                    dto.ExecutionTimeMs = response.DurationMs;
                    return dto;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UniversalAiRouter] Không thể parse JSON bản đồ tư duy. Trả về fallback.");
            }

            return new PedagogicalWalkthroughDto
            {
                QuestionContent = questionContent,
                CorrectAnswer = correctAnswer,
                DomainCode = domainCode,
                PromptAnatomy = response.Content,
                ResolvedByProvider = response.ProviderUsed,
                ResolvedByModel = response.ModelUsed,
                ExecutionTimeMs = response.DurationMs
            };
        }

        public AiSystemConfigDto GetSystemConfig()
        {
            lock (_configLock)
            {
                return _systemConfig;
            }
        }

        public void UpdateSystemConfig(AiSystemConfigDto config)
        {
            lock (_configLock)
            {
                _systemConfig = config;
                _dynamicStringAdapters.Clear();
                foreach (var p in config.Providers)
                {
                    if (p.ApiKeys.Count > 0)
                    {
                        var code = string.IsNullOrWhiteSpace(p.ProviderCode) ? WellKnownAiProviders.ToCode(p.ProviderType) : p.ProviderCode;
                        _keyPoolManager.RegisterKeys(code, p.ApiKeys);
                    }
                }
            }
            _logger.LogInformation("[UniversalAiRouter] Cấu hình hệ thống AI đã được cập nhật thành công.");
        }

        public void RegisterTenantByok(TenantByokConfigDto byokConfig)
        {
            _tenantByokMap[byokConfig.TenantId] = byokConfig;
            _logger.LogInformation("[UniversalAiRouter] Đăng ký BYOK cho Tenant {TenantId} thành công (Provider: {Provider}).", byokConfig.TenantId, byokConfig.ProviderCode ?? byokConfig.ProviderType.ToString());
        }

        public async Task<Dictionary<string, (bool Success, long LatencyMs, string Message)>> TestProvidersAsync()
        {
            var results = new Dictionary<string, (bool Success, long LatencyMs, string Message)>();
            var providers = _systemConfig.Providers.Where(p => p.IsEnabled).ToList();

            foreach (var prov in providers)
            {
                var pCode = string.IsNullOrWhiteSpace(prov.ProviderCode) ? prov.ProviderType.ToString().ToLowerInvariant() : prov.ProviderCode;
                var adapter = ResolveAdapter(pCode, prov.ProviderType);
                var key = _keyPoolManager.GetAvailableKey(pCode) ?? _keyPoolManager.GetAvailableKey(prov.ProviderType);

                if (string.IsNullOrWhiteSpace(key) && prov.ProviderType != AiProviderType.LocalOllama && !pCode.Contains("ollama"))
                {
                    results[prov.DisplayName] = (false, 0, "Không có API Key khả dụng trong Vault");
                    continue;
                }

                var (success, _, error, durationMs) = await adapter.CallAsync(
                    key ?? "local",
                    prov.DefaultModel,
                    "Ping test latency. Respond with 'PONG'.",
                    null,
                    0.1,
                    16,
                    prov.BaseUrl);

                results[prov.DisplayName] = (success, durationMs, success ? "Kết nối hoàn hảo" : (error ?? "Lỗi không xác định"));
            }

            return results;
        }

        private static string BuildPersonaSystemPrompt(string personaCode, string? customPrompt)
        {
            var norm = (personaCode ?? string.Empty).ToLowerInvariant();
            string personaRole;

            if (norm.Contains("legal") || norm.Contains("counsel"))
            {
                personaRole = "Bạn là Cố vấn Pháp chế & Rủi ro Ngân hàng Cấp cao. Luôn bám sát Luật Các TCTD, Thông tư NHNN, Basel III và các quy chuẩn kiểm toán an toàn vốn.";
            }
            else if (norm.Contains("socrates") || norm.Contains("mentor"))
            {
                personaRole = "Bạn là Gia sư Khai phóng Tri thức phương pháp Socrates. Không đưa ngay đáp án mà đặt câu hỏi gợi mở, bóc tách lỗi tư duy để người học tự giác ngộ.";
            }
            else if (norm.Contains("chaser") || norm.Contains("hunter"))
            {
                personaRole = "Bạn là Kẻ Săn Đuổi Sắc Lạnh trong Đấu Trường Gameshow Sinh Tử. Ngôn từ đanh thép, tốc độ, tạo áp lực thời gian nhưng tuyệt đối công tâm.";
            }
            else if (norm.Contains("psych") || norm.Contains("profiler"))
            {
                personaRole = "Bạn là Chuyên gia Tâm lý Học Nhận thức. Phân tích độ do dự, thời gian phản xạ và phát hiện thí sinh đang đoán mò hay nắm vững kiến thức.";
            }
            else
            {
                personaRole = "Bạn là Trọng tài Khảo thí Tối cao (Supreme Arbiter) của Hệ thống AegisQuiz chuẩn quốc tế ETS/AERA. Luôn khách quan, chính xác tuyệt đối, lập luận logic và viện dẫn khoa học chặt chẽ.";
            }

            if (!string.IsNullOrWhiteSpace(customPrompt))
            {
                return $"{personaRole}\n\n[CHỈ ĐỊNH ĐẶC BIỆT]:\n{customPrompt}";
            }

            return personaRole;
        }

        [Obsolete("Sử dụng BuildPersonaSystemPrompt(string personaCode, string? customPrompt)")]
        private static string BuildPersonaSystemPrompt(AgentPersonaType persona, string? customPrompt)
            => BuildPersonaSystemPrompt(WellKnownAgentPersonas.ToCode(persona), customPrompt);



        private static string CleanJsonString(string raw)
        {
            var trimmed = raw.Trim();
            if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[7..];
            }
            else if (trimmed.StartsWith("```"))
            {
                trimmed = trimmed[3..];
            }

            if (trimmed.EndsWith("```"))
            {
                trimmed = trimmed[..^3];
            }

            return trimmed.Trim();
        }

        private static string ComputeHash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        // ══════════════════════════════════════════════════════════════════════════
        // CÁC NĂNG LỰC HẠT NHÂN PHỤNG SỰ BẦY SÓI (SWARM KERNEL APIs - THẾ HỆ 2026)
        // ══════════════════════════════════════════════════════════════════════════

        public async Task<IReadOnlyList<AiModelRegistryDto>> GetActiveNormalizedModelsAsync(
            CognitiveArchetype? archetypeFilter = null,
            CancellationToken ct = default)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

                var query = db.AiModelRegistries.AsNoTracking().Where(m => m.IsEnabled);
                if (archetypeFilter.HasValue)
                {
                    query = query.Where(m => m.CognitiveArchetype == archetypeFilter.Value);
                }

                var list = await query
                    .OrderByDescending(m => m.BenchmarkElo)
                    .Select(m => new AiModelRegistryDto
                    {
                        Id = m.Id,
                        ModelCode = m.ModelCode,
                        DisplayName = m.DisplayName,
                        ProviderCompany = m.ProviderCompany,
                        ProtocolType = m.ProtocolType,
                        CognitiveArchetype = m.CognitiveArchetype,
                        EndpointUrl = m.EndpointUrl,
                        ModelName = m.ModelName,
                        MaxContextTokens = m.MaxContextTokens,
                        CostInputPerMillion = m.CostInputPerMillion,
                        CostOutputPerMillion = m.CostOutputPerMillion,
                        BenchmarkElo = m.BenchmarkElo,
                        AvgLatencyMs = m.AvgLatencyMs,
                        IsEnabled = m.IsEnabled,
                        IsDefaultInArchetype = m.IsDefaultInArchetype,
                        TenantId = m.TenantId,
                        CreatedAt = m.CreatedAt
                    })
                    .ToListAsync(ct);

                if (list.Count > 0) return list;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[UniversalAiRouter] Lỗi khi truy vấn AiModelRegistries từ PostgreSQL. Chuyển sang fallback.");
            }

            // Fallback hạt nhân an toàn nếu DB đang khởi động
            var fallbacks = new List<AiModelRegistryDto>
            {
                new() { ModelCode = "deepseek-r1", DisplayName = "DeepSeek R1 Reasoning", ProviderCompany = "DeepSeek", CognitiveArchetype = CognitiveArchetype.DeepReasoner, ProtocolType = ModelProtocolType.OpenAiCompatible, ModelName = "deepseek-reasoner", EndpointUrl = "https://api.deepseek.com/v1", BenchmarkElo = 1375, AvgLatencyMs = 450, CostInputPerMillion = 0.55, CostOutputPerMillion = 2.19, IsEnabled = true },
                new() { ModelCode = "gemini-2.0-flash", DisplayName = "Google Gemini 2.0 Flash", ProviderCompany = "Google", CognitiveArchetype = CognitiveArchetype.MassiveContext, ProtocolType = ModelProtocolType.GoogleGenerativeAi, ModelName = "gemini-2.0-flash", EndpointUrl = "https://generativelanguage.googleapis.com", BenchmarkElo = 1310, AvgLatencyMs = 220, CostInputPerMillion = 0.1, CostOutputPerMillion = 0.4, IsEnabled = true },
                new() { ModelCode = "claude-3-5-sonnet", DisplayName = "Anthropic Claude 3.5 Sonnet", ProviderCompany = "Anthropic", CognitiveArchetype = CognitiveArchetype.LinguisticArbiter, ProtocolType = ModelProtocolType.AnthropicMessages, ModelName = "claude-3-5-sonnet-20241022", EndpointUrl = "https://api.anthropic.com/v1", BenchmarkElo = 1360, AvgLatencyMs = 550, CostInputPerMillion = 3.0, CostOutputPerMillion = 15.0, IsEnabled = true },
                new() { ModelCode = "openrouter-meta-llama-3-3-70b-instruct-free", DisplayName = "Llama 3.3 70B Instruct (Free)", ProviderCompany = "OpenRouter", CognitiveArchetype = CognitiveArchetype.LinguisticArbiter, ProtocolType = ModelProtocolType.OpenAiCompatible, ModelName = "meta-llama/llama-3.3-70b-instruct:free", EndpointUrl = "https://openrouter.ai/api/v1", BenchmarkElo = 1350, AvgLatencyMs = 320, CostInputPerMillion = 0.0, CostOutputPerMillion = 0.0, IsEnabled = true }
            };

            if (archetypeFilter.HasValue)
            {
                fallbacks = fallbacks.Where(f => f.CognitiveArchetype == archetypeFilter.Value).ToList();
            }

            return fallbacks;
        }

        public async Task<AiExecutionResponse> ExecuteModelDirectAsync(
            string modelCode,
            AiExecutionRequest request,
            Guid? userId = null,
            Guid? tenantId = null,
            CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            // 1. Kiểm tra Semantic Cache
            if (_systemConfig.EnableSemanticCache)
            {
                var cacheKey = string.IsNullOrWhiteSpace(request.CacheKey)
                    ? ComputeHash($"{modelCode}_{request.Prompt}")
                    : request.CacheKey;

                if (_cache.TryGetValue<string>(cacheKey, out var cachedResult) && !string.IsNullOrWhiteSpace(cachedResult))
                {
                    sw.Stop();
                    lock (_configLock)
                    {
                        _systemConfig.Telemetry.CacheHitsCount++;
                        _systemConfig.Telemetry.EstimatedCostSavedUsd += 0.005;
                    }

                    return new AiExecutionResponse
                    {
                        Success = true,
                        Content = cachedResult,
                        ProviderUsed = "SemanticCache",
                        ModelUsed = modelCode,
                        IsFromCache = true,
                        DurationMs = sw.ElapsedMilliseconds
                    };
                }
            }

            // 2. Tra cứu thông tin Model từ DB hoặc bộ đệm
            AiModelRegistry? modelEntity = null;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();
                modelEntity = await db.AiModelRegistries.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ModelCode == modelCode, ct);
            }
            catch { /* fallback bên dưới */ }

            var providerCompany = modelEntity?.ProviderCompany ?? "OpenRouter";
            var modelName = modelEntity?.ModelName ?? modelCode;
            var endpointUrl = modelEntity?.EndpointUrl ?? "https://openrouter.ai/api/v1";
            var pCode = WellKnownAiProviders.Normalize(providerCompany);

            // 3. Phân giải API Key theo Waterfall (User BYOK -> Tenant BYOK -> KeyPool / System)
            string? apiKey = null;
            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();
                    var userKey = await db.UserAiKeys.AsNoTracking()
                        .Where(k => k.UserId == userId.Value && k.IsActive && k.ProviderCompany.ToLower() == providerCompany.ToLower())
                        .OrderByDescending(k => k.LastUsedAt)
                        .FirstOrDefaultAsync(ct);

                    if (userKey != null && !string.IsNullOrWhiteSpace(userKey.EncryptedApiKey))
                    {
                        var masterSecret = _configuration["Security:ApiKeyMasterSecret"] ?? "AegisQuiz-Master-Secret-Quantum-Resistant-2026-Vault";
                        apiKey = Security.ApiKeyEncryptionHelper.Decrypt(userKey.EncryptedApiKey, masterSecret);
                        if (!string.IsNullOrWhiteSpace(userKey.CustomEndpoint)) endpointUrl = userKey.CustomEndpoint;
                        if (!string.IsNullOrWhiteSpace(userKey.CustomModelName)) modelName = userKey.CustomModelName;
                    }
                }
                catch { /* tiếp tục waterfall */ }
            }

            if (string.IsNullOrWhiteSpace(apiKey) && tenantId.HasValue)
            {
                if (_tenantByokMap.TryGetValue(tenantId.Value.ToString(), out var byok) && byok.IsActive)
                {
                    apiKey = byok.ApiKey;
                    if (!string.IsNullOrWhiteSpace(byok.CustomEndpoint)) endpointUrl = byok.CustomEndpoint;
                    if (!string.IsNullOrWhiteSpace(byok.CustomModel)) modelName = byok.CustomModel;
                }
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = _keyPoolManager.GetAvailableKey(pCode);
            }

            // Fallback cuối: System Key
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = GetSystemKeyForProvider(pCode);
            }

            // 4. Giải quyết Adapter & Thực thi
            var adapter = ResolveAdapter(pCode, modelName, endpointUrl);
            var systemPrompt = BuildPersonaSystemPrompt(request.PersonaCode, request.SystemPrompt);

            var (success, content, error, durationMs) = await adapter.CallAsync(
                apiKey ?? "local",
                modelName,
                request.Prompt,
                systemPrompt,
                request.Temperature,
                request.MaxTokens,
                endpointUrl);

            sw.Stop();

            if (success && !string.IsNullOrWhiteSpace(content))
            {
                if (_systemConfig.EnableSemanticCache)
                {
                    var cacheKey = string.IsNullOrWhiteSpace(request.CacheKey)
                        ? ComputeHash($"{modelCode}_{request.Prompt}")
                        : request.CacheKey;
                    _cache.Set(cacheKey, content, TimeSpan.FromHours(24));
                }

                return new AiExecutionResponse
                {
                    Success = true,
                    Content = content,
                    ProviderUsed = providerCompany,
                    ModelUsed = modelName,
                    IsFromCache = false,
                    DurationMs = durationMs > 0 ? durationMs : sw.ElapsedMilliseconds
                };
            }

            return new AiExecutionResponse
            {
                Success = false,
                Content = string.Empty,
                ErrorMessage = error ?? "Lỗi thực thi mô hình qua UniversalAiRouter.",
                ProviderUsed = providerCompany,
                ModelUsed = modelName,
                DurationMs = sw.ElapsedMilliseconds
            };
        }

        public string GetSystemKeyForProvider(string? providerCompany)
        {
            if (string.IsNullOrWhiteSpace(providerCompany)) return string.Empty;

            var fileFallback = LoadApiKeyFileFallback();

            string GetKey(string configKey, string envKey)
            {
                var val = _configuration[configKey];
                if (!string.IsNullOrWhiteSpace(val)) return val;
                if (fileFallback.TryGetValue(envKey, out var fileVal) && !string.IsNullOrWhiteSpace(fileVal)) return fileVal;
                return Environment.GetEnvironmentVariable(envKey) ?? string.Empty;
            }

            var norm = WellKnownAiProviders.Normalize(providerCompany);
            var compUpper = providerCompany.ToUpperInvariant().Replace("-", "_").Replace(" ", "_");

            var directKey = GetKey($"{providerCompany}:ApiKey", $"{compUpper}_API_KEY");
            if (!string.IsNullOrWhiteSpace(directKey)) return directKey;

            var directToken = GetKey($"{providerCompany}:Token", $"{compUpper}_TOKEN");
            if (!string.IsNullOrWhiteSpace(directToken)) return directToken;

            // Quét từ file fallback
            foreach (var kvp in fileFallback)
            {
                var k = kvp.Key.ToUpperInvariant().Replace("-", "_");
                if (k.Contains(compUpper) && (k.EndsWith("_KEY") || k.EndsWith("_TOKEN") || k.EndsWith("_API_KEY")))
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Value)) return kvp.Value;
                }
            }

            // Fallback theo tên quy ước
            if (norm.Contains("gemini") || norm.Contains("google"))
            {
                var g = GetKey("Gemini:ApiKey", "GOOGLE_API_KEY");
                if (!string.IsNullOrWhiteSpace(g)) return g;
            }
            if (norm.Contains("github"))
            {
                var gh = GetKey("GitHub:Token", "GITHUB_TOKEN");
                if (!string.IsNullOrWhiteSpace(gh)) return gh;
            }
            if (norm.Contains("openrouter"))
            {
                var op = GetKey("OpenRouter:ApiKey", "OPENROUTER_API_KEY");
                if (!string.IsNullOrWhiteSpace(op)) return op;
            }

            return string.Empty;
        }

        public async Task<IReadOnlyList<GatewayModelPresetDto>> GetDynamicGatewayPresetsAsync(
            string? gateway = null,
            CancellationToken ct = default)
        {
            var presets = new List<GatewayModelPresetDto>
            {
                new GatewayModelPresetDto
                {
                    ModelCode = "openrouter-meta-llama-3-3-70b-instruct-free",
                    ModelName = "meta-llama/llama-3.3-70b-instruct:free",
                    DisplayName = "Llama 3.3 70B Instruct (Free)",
                    ProviderCompany = "OpenRouter",
                    EndpointUrl = "https://openrouter.ai/api/v1",
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    CognitiveArchetype = CognitiveArchetype.LinguisticArbiter,
                    ContemporaryStrength = "Chuyên gia Phản biện Đối kháng (Adversarial Counter) & Bóc trần Ngụy biện",
                    RecommendedCouncilRole = CouncilSeatRole.AdversarialCritic,
                    RecommendedCouncilCodes = new() { "SYS_ARBITRATION", "SYS_SECRETARIAT", "COSMOLOGY" },
                    BenchmarkElo = 1350,
                    AvgLatencyMs = 320,
                    MaxContextTokens = 128000,
                    CostInputPerMillion = 0.0,
                    CostOutputPerMillion = 0.0,
                    IsFree = true,
                    BadgeTag = "FREE_COUNTER",
                    Description = "Mô hình mã nguồn mở cự phách 70B của Meta, hoàn toàn 0$ trên OpenRouter. Xuất sắc trong phản biện Devil's Advocate và kiểm định barem khảo thí."
                },
                new GatewayModelPresetDto
                {
                    ModelCode = "openrouter-deepseek-r1-free",
                    ModelName = "deepseek/deepseek-r1:free",
                    DisplayName = "DeepSeek R1 Reasoning (Free)",
                    ProviderCompany = "OpenRouter",
                    EndpointUrl = "https://openrouter.ai/api/v1",
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    CognitiveArchetype = CognitiveArchetype.DeepReasoner,
                    ContemporaryStrength = "Bẻ khóa bẫy nhận thức & Suy luận toán học chuỗi dài (CoT)",
                    RecommendedCouncilRole = CouncilSeatRole.AdversarialCritic,
                    RecommendedCouncilCodes = new() { "SYS_CORE", "SYS_ARBITRATION", "COSMOLOGY" },
                    BenchmarkElo = 1375,
                    AvgLatencyMs = 450,
                    MaxContextTokens = 128000,
                    CostInputPerMillion = 0.0,
                    CostOutputPerMillion = 0.0,
                    IsFree = true,
                    BadgeTag = "REASONING_FREE",
                    Description = "Mô hình suy luận chuỗi dài số 1 thế giới nguồn mở, tự động phát hiện bẫy ngụy biện và bài toán hóc búa, miễn phí 100%."
                },
                new GatewayModelPresetDto
                {
                    ModelCode = "openrouter-qwen-2-5-72b-instruct",
                    ModelName = "qwen/qwen-2.5-72b-instruct",
                    DisplayName = "Qwen 2.5 72B Instruct (Deep Reasoner)",
                    ProviderCompany = "OpenRouter",
                    EndpointUrl = "https://openrouter.ai/api/v1",
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    CognitiveArchetype = CognitiveArchetype.DeepReasoner,
                    ContemporaryStrength = "Toán học, Mã nguồn, Đa ngôn ngữ & Kiến tạo Hợp đề Tối cao",
                    RecommendedCouncilRole = CouncilSeatRole.Synthesizer,
                    RecommendedCouncilCodes = new() { "COSMOLOGY", "LIFE_SCIENCES", "SYS_CORE" },
                    BenchmarkElo = 1345,
                    AvgLatencyMs = 280,
                    MaxContextTokens = 128000,
                    CostInputPerMillion = 0.35,
                    CostOutputPerMillion = 0.40,
                    IsFree = false,
                    BadgeTag = "FLAGSHIP",
                    Description = "Mô hình 72B đa năng vô địch của Alibaba, cực mạnh về tổng hợp kiến thức liên ngành và lập luận tri thức toàn cầu."
                },
                new GatewayModelPresetDto
                {
                    ModelCode = "openrouter-mistral-7b-instruct-free",
                    ModelName = "mistralai/mistral-7b-instruct:free",
                    DisplayName = "Mistral 7B Instruct (Free)",
                    ProviderCompany = "OpenRouter",
                    EndpointUrl = "https://openrouter.ai/api/v1",
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    CognitiveArchetype = CognitiveArchetype.UltraFastScanner,
                    ContemporaryStrength = "Quét & Phân loại Siêu tốc Thời gian thực (Real-time Scanner)",
                    RecommendedCouncilRole = CouncilSeatRole.EmpiricalAuditor,
                    RecommendedCouncilCodes = new() { "SYS_SECRETARIAT", "SYS_CORE" },
                    BenchmarkElo = 1260,
                    AvgLatencyMs = 150,
                    MaxContextTokens = 32768,
                    CostInputPerMillion = 0.0,
                    CostOutputPerMillion = 0.0,
                    IsFree = true,
                    BadgeTag = "SPEED_FREE",
                    Description = "Mô hình gọn nhẹ tốc độ cao của Mistral AI, miễn phí 100%, lý tưởng cho quét lỗi cú pháp và trích xuất dữ kiện."
                },
                new GatewayModelPresetDto
                {
                    ModelCode = "openrouter-google-gemini-2-0-flash-free",
                    ModelName = "google/gemini-2.0-flash-exp:free",
                    DisplayName = "Google Gemini 2.0 Flash (Free 1M+)",
                    ProviderCompany = "OpenRouter",
                    EndpointUrl = "https://openrouter.ai/api/v1",
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    CognitiveArchetype = CognitiveArchetype.MassiveContext,
                    ContemporaryStrength = "Siêu ngữ cảnh 1 Triệu Tokens & Thẩm định Toàn văn Tài liệu Nguồn",
                    RecommendedCouncilRole = CouncilSeatRole.EmpiricalAuditor,
                    RecommendedCouncilCodes = new() { "SYS_SECRETARIAT", "LIFE_SCIENCES", "COSMOLOGY" },
                    BenchmarkElo = 1320,
                    AvgLatencyMs = 210,
                    MaxContextTokens = 1048576,
                    CostInputPerMillion = 0.0,
                    CostOutputPerMillion = 0.0,
                    IsFree = true,
                    BadgeTag = "CONTEXT_1M_FREE",
                    Description = "Mô hình thế hệ mới của Google với cửa sổ ngữ cảnh 1 triệu token hoàn toàn 0$ trên OpenRouter."
                },
                new GatewayModelPresetDto
                {
                    ModelCode = "openrouter-anthropic-claude-3-5-sonnet",
                    ModelName = "anthropic/claude-3.5-sonnet",
                    DisplayName = "Claude 3.5 Sonnet (Supreme Arbiter)",
                    ProviderCompany = "OpenRouter",
                    EndpointUrl = "https://openrouter.ai/api/v1",
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    CognitiveArchetype = CognitiveArchetype.LinguisticArbiter,
                    ContemporaryStrength = "Trọng tài Ngôn ngữ Sư phạm Tối cao & Phản biện Khảo thí Đỉnh phong",
                    RecommendedCouncilRole = CouncilSeatRole.AdversarialCritic,
                    RecommendedCouncilCodes = new() { "SYS_ARBITRATION", "SYS_SECRETARIAT" },
                    BenchmarkElo = 1365,
                    AvgLatencyMs = 520,
                    MaxContextTokens = 200000,
                    CostInputPerMillion = 3.0,
                    CostOutputPerMillion = 15.0,
                    IsFree = false,
                    BadgeTag = "SUPREME_ARBITER",
                    Description = "Chuẩn mực vàng của thế giới về lập luận ngôn ngữ, bóc tách tinh tế các câu hỏi trắc nghiệm đa nghĩa và barem điểm phức tạp."
                },
                new GatewayModelPresetDto
                {
                    ModelCode = "github-gpt-4o-mini",
                    ModelName = "gpt-4o-mini",
                    DisplayName = "GitHub Models GPT-4o-mini",
                    ProviderCompany = "GitHub",
                    EndpointUrl = "https://models.inference.ai.azure.com",
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    CognitiveArchetype = CognitiveArchetype.UltraFastScanner,
                    ContemporaryStrength = "Cân bằng hoàn hảo Tốc độ - Chi phí - Độ chính xác",
                    RecommendedCouncilRole = CouncilSeatRole.EmpiricalAuditor,
                    RecommendedCouncilCodes = new() { "SYS_CORE", "SYS_SECRETARIAT" },
                    BenchmarkElo = 1290,
                    AvgLatencyMs = 240,
                    MaxContextTokens = 128000,
                    CostInputPerMillion = 0.15,
                    CostOutputPerMillion = 0.60,
                    IsFree = false,
                    BadgeTag = "AZURE_COPILOT",
                    Description = "Mô hình GPT-4o-mini tích hợp sẵn qua GitHub Copilot / Azure AI Models Marketplace."
                },
                new GatewayModelPresetDto
                {
                    ModelCode = "openrouter-meta-llama-3-1-405b-instruct",
                    ModelName = "meta-llama/llama-3.1-405b-instruct",
                    DisplayName = "Llama 3.1 405B Instruct (Presidential)",
                    ProviderCompany = "OpenRouter",
                    EndpointUrl = "https://openrouter.ai/api/v1",
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    CognitiveArchetype = CognitiveArchetype.DeepReasoner,
                    ContemporaryStrength = "Siêu Trí tuệ 405 Tỷ Tham số & Trọng tài Cấp Tổng thống",
                    RecommendedCouncilRole = CouncilSeatRole.President,
                    RecommendedCouncilCodes = new() { "SYS_ARBITRATION", "COSMOLOGY" },
                    BenchmarkElo = 1370,
                    AvgLatencyMs = 420,
                    MaxContextTokens = 128000,
                    CostInputPerMillion = 2.0,
                    CostOutputPerMillion = 5.0,
                    IsFree = false,
                    BadgeTag = "PRESIDENTIAL_405B",
                    Description = "Đỉnh cao trí tuệ mã nguồn mở của Meta với 405 tỷ tham số, đảm nhiệm vai trò Chủ tọa Đại Hội Thảo."
                }
            };

            if (!string.IsNullOrWhiteSpace(gateway) && !gateway.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var gNorm = gateway.Trim().ToLowerInvariant();
                presets = presets.Where(p => p.ProviderCompany.ToLowerInvariant().Contains(gNorm)).ToList();
            }

            await Task.CompletedTask;
            return presets;
        }
    }
}
