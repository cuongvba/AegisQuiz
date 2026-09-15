using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Infrastructure.AI.Adapters;

namespace AegisQuiz.Infrastructure.AI
{
    public class UniversalAiRouter : IUniversalAiRouter
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<UniversalAiRouter> _logger;
        private readonly AiKeyPoolManager _keyPoolManager;
        private readonly Dictionary<AiProviderType, IAiProviderAdapter> _adapters;
        private readonly ConcurrentDictionary<string, TenantByokConfigDto> _tenantByokMap = new();

        private AiSystemConfigDto _systemConfig;
        private readonly object _configLock = new();

        public UniversalAiRouter(
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<UniversalAiRouter> logger,
            IEnumerable<IAiProviderAdapter> adapters)
        {
            _cache = cache;
            _logger = logger;
            _keyPoolManager = new AiKeyPoolManager(quarantineDurationSeconds: 60);

            _adapters = adapters.ToDictionary(a => a.ProviderType, a => a);

            _systemConfig = InitializeDefaultConfig(configuration);
        }

        private AiSystemConfigDto InitializeDefaultConfig(IConfiguration config)
        {
            var geminiKey = config["Gemini:ApiKey"] ?? string.Empty;
            var deepSeekKey = config["DeepSeek:ApiKey"] ?? string.Empty;
            var openAiKey = config["OpenAI:ApiKey"] ?? string.Empty;
            var claudeKey = config["Anthropic:ApiKey"] ?? string.Empty;
            var ollamaUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434/v1/chat/completions";

            var providers = new List<AiProviderEndpointConfig>
            {
                new()
                {
                    ProviderType = AiProviderType.DeepSeek,
                    DisplayName = "DeepSeek (Reasoner R1 / V3)",
                    BaseUrl = "https://api.deepseek.com/v1/chat/completions",
                    DefaultModel = "deepseek-chat",
                    ApiKeys = string.IsNullOrWhiteSpace(deepSeekKey) ? new() : new() { deepSeekKey },
                    IsEnabled = !string.IsNullOrWhiteSpace(deepSeekKey),
                    Priority = 1,
                    CostPerMillionInputTokens = 0.14,
                    CostPerMillionOutputTokens = 0.28
                },
                new()
                {
                    ProviderType = AiProviderType.GoogleGemini,
                    DisplayName = "Google Gemini 2.0 / 1.5 Flash",
                    BaseUrl = "https://generativelanguage.googleapis.com",
                    DefaultModel = "gemini-2.0-flash",
                    ApiKeys = string.IsNullOrWhiteSpace(geminiKey) ? new() : new() { geminiKey },
                    IsEnabled = !string.IsNullOrWhiteSpace(geminiKey),
                    Priority = 2,
                    CostPerMillionInputTokens = 0.075,
                    CostPerMillionOutputTokens = 0.30
                },
                new()
                {
                    ProviderType = AiProviderType.OpenAI,
                    DisplayName = "OpenAI (GPT-4o / GPT-4o-mini)",
                    BaseUrl = "https://api.openai.com/v1/chat/completions",
                    DefaultModel = "gpt-4o-mini",
                    ApiKeys = string.IsNullOrWhiteSpace(openAiKey) ? new() : new() { openAiKey },
                    IsEnabled = !string.IsNullOrWhiteSpace(openAiKey),
                    Priority = 3,
                    CostPerMillionInputTokens = 0.15,
                    CostPerMillionOutputTokens = 0.60
                },
                new()
                {
                    ProviderType = AiProviderType.AnthropicClaude,
                    DisplayName = "Anthropic Claude 3.5 Sonnet",
                    BaseUrl = "https://api.anthropic.com/v1/messages",
                    DefaultModel = "claude-3-5-sonnet-20241022",
                    ApiKeys = string.IsNullOrWhiteSpace(claudeKey) ? new() : new() { claudeKey },
                    IsEnabled = !string.IsNullOrWhiteSpace(claudeKey),
                    Priority = 4,
                    CostPerMillionInputTokens = 3.00,
                    CostPerMillionOutputTokens = 15.00
                },
                new()
                {
                    ProviderType = AiProviderType.LocalOllama,
                    DisplayName = "Local Private LLM (Ollama / vLLM)",
                    BaseUrl = ollamaUrl,
                    DefaultModel = "qwen2.5:14b",
                    ApiKeys = new() { "local_no_key_required" },
                    IsEnabled = false, // Kích hoạt khi có cấu hình On-Premise
                    Priority = 5,
                    CostPerMillionInputTokens = 0.0,
                    CostPerMillionOutputTokens = 0.0
                }
            };

            foreach (var p in providers)
            {
                if (p.ApiKeys.Count > 0)
                {
                    _keyPoolManager.RegisterKeys(p.ProviderType, p.ApiKeys);
                }
            }

            var taskRouting = new Dictionary<string, AiProviderType>
            {
                { AiTaskType.SolveQuestions.ToString(), AiProviderType.DeepSeek },
                { AiTaskType.CognitiveWalkthrough.ToString(), AiProviderType.DeepSeek },
                { AiTaskType.GenerateQuestions.ToString(), AiProviderType.GoogleGemini },
                { AiTaskType.LatexConversion.ToString(), AiProviderType.GoogleGemini },
                { AiTaskType.GradeEssay.ToString(), AiProviderType.AnthropicClaude },
                { AiTaskType.PersonalizedMentor.ToString(), AiProviderType.GoogleGemini },
                { AiTaskType.ArenaAdversary.ToString(), AiProviderType.OpenAI }
            };

            return new AiSystemConfigDto
            {
                Providers = providers,
                TaskRoutingMap = taskRouting,
                EnableSemanticCache = true,
                SemanticCacheSimilarityThreshold = 0.95,
                EnableMultiAgentCommittee = true,
                KeyQuarantineSeconds = 60,
                Telemetry = new FinOpsTelemetryDto()
            };
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
                    ? ComputeHash($"{request.TaskType}_{request.Prompt}")
                    : request.CacheKey;

                if (_cache.TryGetValue<string>(cacheKey, out var cachedResult) && !string.IsNullOrWhiteSpace(cachedResult))
                {
                    sw.Stop();
                    lock (_configLock)
                    {
                        _systemConfig.Telemetry.CacheHitsCount++;
                        _systemConfig.Telemetry.EstimatedCostSavedUsd += 0.005; // Ước tính tiết kiệm ~0.005$ mỗi hit
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

            // 2. Xác định danh sách Provider ưu tiên (Cascading Provider Pool)
            var providersToTry = ResolveProviderOrder(request);

            string lastError = "Không tìm thấy nhà cung cấp AI nào khả dụng.";

            foreach (var providerType in providersToTry)
            {
                if (!_adapters.TryGetValue(providerType, out var adapter))
                    continue;

                // Kiểm tra BYOK riêng của Tenant nếu có
                string? apiKey = null;
                string? customEndpoint = null;
                string? customModel = request.PreferredModel;

                if (!string.IsNullOrWhiteSpace(request.TenantId) &&
                    _tenantByokMap.TryGetValue(request.TenantId, out var byok) &&
                    byok.IsActive && byok.ProviderType == providerType)
                {
                    apiKey = byok.ApiKey;
                    customEndpoint = byok.CustomEndpoint;
                    customModel = byok.CustomModel ?? customModel;
                }
                else
                {
                    apiKey = _keyPoolManager.GetAvailableKey(providerType);
                }

                if (string.IsNullOrWhiteSpace(apiKey) && providerType != AiProviderType.LocalOllama)
                {
                    continue;
                }

                var systemPrompt = BuildPersonaSystemPrompt(request.Persona, request.SystemPrompt);

                var (success, content, error, durationMs) = await adapter.CallAsync(
                    apiKey ?? "local",
                    customModel,
                    request.Prompt,
                    systemPrompt,
                    request.Temperature,
                    request.MaxTokens,
                    customEndpoint);

                if (success && !string.IsNullOrWhiteSpace(content))
                {
                    sw.Stop();

                    // Lưu vào cache với TTL 24 giờ
                    if (_systemConfig.EnableSemanticCache)
                    {
                        var cacheKey = string.IsNullOrWhiteSpace(request.CacheKey)
                            ? ComputeHash($"{request.TaskType}_{request.Prompt}")
                            : request.CacheKey;
                        _cache.Set(cacheKey, content, TimeSpan.FromHours(24));
                    }

                    // Cập nhật telemetry
                    var providerName = providerType.ToString();
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

                // Nếu gặp lỗi rate limit 429 hoặc lỗi kết nối
                lastError = error ?? "Lỗi gọi API không xác định.";
                if (apiKey != null)
                {
                    bool isRateLimit = lastError.Contains("429") || lastError.Contains("RESOURCE_EXHAUSTED");
                    _keyPoolManager.ReportRateLimitOrFailure(providerType, apiKey, isRateLimit);
                }

                _logger.LogWarning("[UniversalAiRouter] Provider {Provider} thất bại ({Error}). Đang chuyển vùng sang Provider tiếp theo trong chuỗi Cascade...", providerType, lastError);
            }

            sw.Stop();
            return new AiExecutionResponse
            {
                Success = false,
                Content = string.Empty,
                ErrorMessage = $"Tất cả các nhà cung cấp AI trong chuỗi Cascade đều thất bại. Chi tiết lỗi cuối: {lastError}",
                DurationMs = sw.ElapsedMilliseconds
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
                TaskType = AiTaskType.CognitiveWalkthrough,
                Prompt = prompt,
                Persona = persona,
                Temperature = 0.2,
                MaxTokens = 2048,
                TenantId = tenantId
            });

            sw.Stop();

            if (response.Success && !string.IsNullOrWhiteSpace(response.Content))
            {
                try
                {
                    var cleanJson = ExtractJson(response.Content);
                    using var doc = JsonDocument.Parse(cleanJson);
                    var root = doc.RootElement;

                    return new PedagogicalWalkthroughDto
                    {
                        QuestionContent = questionContent,
                        CorrectAnswer = correctAnswer,
                        DomainCode = domainCode,
                        PromptAnatomy = root.TryGetProperty("promptAnatomy", out var pa) ? pa.GetString() ?? "" : "",
                        TheoreticalGrounding = root.TryGetProperty("theoreticalGrounding", out var tg) ? tg.GetString() ?? "" : "",
                        DistractorAutopsy = root.TryGetProperty("distractorAutopsy", out var da) ? da.GetString() ?? "" : "",
                        MnemonicAndRecall = root.TryGetProperty("mnemonicAndRecall", out var mr) ? mr.GetString() ?? "" : "",
                        ExtrapolatedCase = root.TryGetProperty("extrapolatedCase", out var ec) ? ec.GetString() ?? "" : "",
                        BloomLevel = root.TryGetProperty("bloomLevel", out var bl) && bl.TryGetInt32(out var bVal) ? bVal : 3,
                        EstimatedDifficultyIrt = root.TryGetProperty("estimatedDifficultyIrt", out var ed) && ed.TryGetDouble(out var dVal) ? dVal : 0.5,
                        ResolvedByProvider = response.ProviderUsed,
                        ResolvedByModel = response.ModelUsed,
                        IsFromSemanticCache = response.IsFromCache,
                        ExecutionTimeMs = sw.ElapsedMilliseconds
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[UniversalAiRouter] Lỗi giải mã JSON Walkthrough. Trả về định dạng bán cấu trúc.");
                }
            }

            // Fallback nếu JSON parse lỗi
            return new PedagogicalWalkthroughDto
            {
                QuestionContent = questionContent,
                CorrectAnswer = correctAnswer,
                DomainCode = domainCode,
                PromptAnatomy = "Bóc tách dữ kiện: Câu hỏi yêu cầu xác định căn cứ chuẩn mực và đáp án chính xác.",
                TheoreticalGrounding = string.IsNullOrWhiteSpace(response.Content) ? "Căn cứ quy định và chuẩn mực nghiệp vụ chuyên ngành." : response.Content,
                DistractorAutopsy = "Các phương án còn lại là phương án nhiễu phổ biến trong các kỳ thi nghiệp vụ.",
                MnemonicAndRecall = "Nhớ từ khóa định danh chính xác và đối chiếu với văn bản quy phạm.",
                ExtrapolatedCase = "Ứng dụng tình huống giải quyết xung đột thực tế tại đơn vị.",
                ResolvedByProvider = response.ProviderUsed,
                ResolvedByModel = response.ModelUsed,
                IsFromSemanticCache = response.IsFromCache,
                ExecutionTimeMs = sw.ElapsedMilliseconds
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
                foreach (var p in config.Providers)
                {
                    if (p.ApiKeys.Count > 0)
                    {
                        _keyPoolManager.RegisterKeys(p.ProviderType, p.ApiKeys);
                    }
                }
            }
        }

        public void RegisterTenantByok(TenantByokConfigDto byokConfig)
        {
            _tenantByokMap[byokConfig.TenantId] = byokConfig;
        }

        public async Task<Dictionary<string, (bool Success, long LatencyMs, string Message)>> TestProvidersAsync()
        {
            var results = new Dictionary<string, (bool Success, long LatencyMs, string Message)>();

            foreach (var (providerType, adapter) in _adapters)
            {
                var apiKey = _keyPoolManager.GetAvailableKey(providerType);
                if (string.IsNullOrWhiteSpace(apiKey) && providerType != AiProviderType.LocalOllama)
                {
                    results[providerType.ToString()] = (false, 0, "Chưa cấu hình API Key");
                    continue;
                }

                var (success, content, error, durationMs) = await adapter.CallAsync(
                    apiKey ?? "local",
                    null,
                    "Phản hồi 'OK' nếu kết nối thành công.",
                    "Hệ thống kiểm thử kết nối latency.",
                    0.1,
                    16);

                results[providerType.ToString()] = (success, durationMs, success ? "Kết nối hoàn hảo" : (error ?? "Lỗi không xác định"));
            }

            return results;
        }

        private List<AiProviderType> ResolveProviderOrder(AiExecutionRequest request)
        {
            var list = new List<AiProviderType>();

            if (request.PreferredProvider.HasValue)
            {
                list.Add(request.PreferredProvider.Value);
            }
            else if (_systemConfig.TaskRoutingMap.TryGetValue(request.TaskType.ToString(), out var routedProvider))
            {
                list.Add(routedProvider);
            }

            // Thứ tự Cascade dự phòng từ danh sách cấu hình
            var remaining = _systemConfig.Providers
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.Priority)
                .Select(p => p.ProviderType)
                .Where(p => !list.Contains(p));

            list.AddRange(remaining);
            return list;
        }

        private static string BuildPersonaSystemPrompt(AgentPersonaType persona, string? customPrompt)
        {
            var personaRole = persona switch
            {
                AgentPersonaType.SupremeArbiter =>
                    "Bạn là Supreme Arbiter — Giám khảo khảo thí tối cao chuẩn AERA/ETS. Tinh thần của bạn là nghiêm cẩn, khoa học tuyệt đối, soi xét từng từ ngữ để đảm bảo tính duy nhất và chuẩn xác của đề thi.",
                AgentPersonaType.BankingLegalCounsel =>
                    "Bạn là Senior Banking Legal Counsel — Cố vấn pháp lý và nghiệp vụ ngân hàng cấp cao. Phong thái uyên bác, dẫn chứng sắc bén theo Thông tư NHNN, Luật Các TCTD, chuẩn mực Basel và văn bản pháp luật hiện hành.",
                AgentPersonaType.SocratesMentor =>
                    "Bạn là Socrates Mentor — Gia sư khai phóng tri thức. Bạn không mớm đáp án mà đặt câu hỏi gợi mở, bóc tách bản chất để người học tự ngộ ra chân lý nhận thức.",
                AgentPersonaType.RelentlessChaser =>
                    "Bạn là The Chaser — Kẻ đi săn AI thông thái trong Đấu trường Gameshow. Phong thái sắc lạnh, phản xạ vi-giây, khơi dậy tinh thần thi đấu bùng nổ của thí sinh.",
                AgentPersonaType.PsychProfiler =>
                    "Bạn là Psychometric Profiler — Chuyên gia đo lường tâm lý khảo thí và chẩn đoán thói quen nhận thức, phân tích bẫy tâm lý và độ do dự của người học.",
                _ => "Bạn là Trợ lý AI Khảo thí Thông minh của AegisQuiz."
            };

            return string.IsNullOrWhiteSpace(customPrompt)
                ? personaRole
                : $"{personaRole}\n\n{customPrompt}";
        }

        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return $"ai_semcache_{Convert.ToHexString(bytes)[..16]}";
        }

        private static string ExtractJson(string text)
        {
            var clean = text.Trim();
            if (clean.StartsWith("```json")) clean = clean[7..];
            if (clean.StartsWith("```")) clean = clean[3..];
            if (clean.EndsWith("```")) clean = clean[..^3];
            return clean.Trim();
        }
    }
}
