using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.AI.Protocols;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AegisQuiz.Infrastructure.AI.Invokers
{
    /// <summary>
    /// [AegisAI Grounded Symposium - Seat Model Invoker]
    /// Triệu gọi mô hình AI thật sự cho từng ghế/học viện trong Đại Hội Thảo.
    /// Đo độ trễ thực tế qua Stopwatch, giải quyết thác lũ khóa BYOK -> Community -> System,
    /// tự động xoay tua khóa khi lỗi, tuyệt đối trung thực về kết quả thực thi.
    /// </summary>
    public class SeatModelInvoker : ISeatModelInvoker
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Dictionary<ModelProtocolType, IModelProtocolDriver> _drivers;
        private readonly IConfiguration _config;
        private readonly ILogger<SeatModelInvoker> _logger;
        private readonly string _masterSecret;

        public SeatModelInvoker(
            IServiceScopeFactory scopeFactory,
            IEnumerable<IModelProtocolDriver> drivers,
            IConfiguration config,
            ILogger<SeatModelInvoker> logger)
        {
            _scopeFactory = scopeFactory;
            _drivers = drivers.ToDictionary(d => d.ProtocolType, d => d);
            _config = config;
            _logger = logger;
            _masterSecret = _config["Security:ApiKeyMasterSecret"] ?? "AegisQuiz-Master-Secret-Quantum-Resistant-2026-Vault";
        }

        public async Task<SeatInvocationResponse> InvokeSeatAsync(SeatInvocationRequest request, CancellationToken ct = default)
        {
            var swTotal = Stopwatch.StartNew();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            // 1. Phân giải Model Registry
            var model = await db.AiModelRegistries.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ModelCode.ToLower() == request.ModelCode.ToLower(), ct);

            if (model == null)
            {
                // Fallback metadata nếu chưa được cấu hình trong bảng AiModelRegistries
                model = InferModelMetadata(request.ModelCode);
            }

            // 2. Thác lũ phân giải danh sách khóa (Waterfall Key Resolution)
            var candidates = await ResolveKeyCandidatesAsync(db, model, request.UserId, request.TenantId);

            if (candidates.Count == 0)
            {
                swTotal.Stop();
                _logger.LogWarning("[SeatInvoker] No available API key found for model {ModelCode} (Provider: {Provider})",
                    request.ModelCode, model.ProviderCompany);

                return new SeatInvocationResponse
                {
                    Success = false,
                    Content = string.Empty,
                    ModelUsed = model.ModelName,
                    ProviderCompany = model.ProviderCompany,
                    LatencyMs = swTotal.ElapsedMilliseconds,
                    TokensUsed = 0,
                    EstimatedCostUsd = 0,
                    KeySource = "None",
                    ErrorMessage = $"Không tìm thấy API Key khả dụng cho nhà cung cấp {model.ProviderCompany} ({model.ModelCode}). Vui lòng cấu hình BYOK hoặc System Key."
                };
            }

            // 3. Chọn Protocol Driver
            if (!_drivers.TryGetValue(model.ProtocolType, out var driver))
            {
                if (!_drivers.TryGetValue(ModelProtocolType.OpenAiCompatible, out driver))
                {
                    swTotal.Stop();
                    return new SeatInvocationResponse
                    {
                        Success = false,
                        LatencyMs = swTotal.ElapsedMilliseconds,
                        ErrorMessage = $"Không tìm thấy giao thức driver phù hợp cho {model.ProtocolType}"
                    };
                }
            }

            // 4. Vòng lặp xoay tua khóa (Key Rotation Loop)
            string? lastError = null;

            foreach (var candidate in candidates)
            {
                var swAttempt = Stopwatch.StartNew();
                try
                {
                    var endpoint = !string.IsNullOrWhiteSpace(candidate.Endpoint) ? candidate.Endpoint : model.EndpointUrl;
                    var modelName = !string.IsNullOrWhiteSpace(candidate.ModelName) ? candidate.ModelName : model.ModelName;

                    var (success, content, error, duration, tokensUsed) = await driver.ExecuteAsync(
                        endpoint,
                        candidate.ApiKey,
                        modelName,
                        request.SystemPrompt,
                        request.UserPrompt,
                        temperature: request.Temperature,
                        maxTokens: request.MaxTokens,
                        ct: ct);

                    swAttempt.Stop();
                    var measuredLatency = duration > 0 ? duration : swAttempt.ElapsedMilliseconds;

                    if (success && !string.IsNullOrWhiteSpace(content))
                    {
                        // Cập nhật thống kê sử dụng khóa (chạy không đồng bộ nền để không chặn)
                        if (candidate.KeyEntityId.HasValue)
                        {
                            _ = UpdateKeyUsageTelemetryAsync(candidate.KeyEntityId.Value, tokensUsed, success: true);
                        }

                        var estimatedCost = (tokensUsed / 1_000_000.0) * (model.CostOutputPerMillion > 0 ? model.CostOutputPerMillion : 1.0);

                        return new SeatInvocationResponse
                        {
                            Success = true,
                            Content = content.Trim(),
                            ModelUsed = modelName,
                            ProviderCompany = model.ProviderCompany,
                            LatencyMs = measuredLatency,
                            TokensUsed = tokensUsed,
                            EstimatedCostUsd = estimatedCost,
                            KeySource = candidate.KeySource,
                            ErrorMessage = null
                        };
                    }

                    // Ghi nhận lỗi cho lần thử này và xoay khóa tiếp
                    lastError = error ?? "Mô hình trả về nội dung rỗng.";
                    _logger.LogWarning("[SeatInvoker] Attempt with key source {KeySource} failed for model {Model}: {Error}",
                        candidate.KeySource, modelName, lastError);

                    if (candidate.KeyEntityId.HasValue)
                    {
                        _ = UpdateKeyUsageTelemetryAsync(candidate.KeyEntityId.Value, 0, success: false);
                    }
                }
                catch (Exception ex)
                {
                    swAttempt.Stop();
                    lastError = ex.Message;
                    _logger.LogWarning(ex, "[SeatInvoker] Exception invoking key candidate {KeySource} for model {Model}",
                        candidate.KeySource, candidate.ModelName);

                    if (candidate.KeyEntityId.HasValue)
                    {
                        _ = UpdateKeyUsageTelemetryAsync(candidate.KeyEntityId.Value, 0, success: false);
                    }
                }
            }

            swTotal.Stop();
            return new SeatInvocationResponse
            {
                Success = false,
                Content = string.Empty,
                ModelUsed = model.ModelName,
                ProviderCompany = model.ProviderCompany,
                LatencyMs = swTotal.ElapsedMilliseconds,
                TokensUsed = 0,
                EstimatedCostUsd = 0,
                KeySource = candidates.FirstOrDefault()?.KeySource ?? "Unknown",
                ErrorMessage = $"Toàn bộ {candidates.Count} khóa khả dụng đều thất bại. Lỗi cuối: {lastError}"
            };
        }

        private async Task<List<ResolvedKeyCandidate>> ResolveKeyCandidatesAsync(
            AegisQuizDbContext db,
            AiModelRegistry model,
            Guid? userId,
            Guid? tenantId)
        {
            var candidates = new List<ResolvedKeyCandidate>();

            // Mức 1: Khóa cá nhân của người dùng (Private BYOK)
            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                var userKeys = await db.UserAiKeys.AsNoTracking()
                    .Where(k => k.UserId == userId.Value && k.IsActive &&
                               (k.ProviderCompany.ToLower() == model.ProviderCompany.ToLower() || (!string.IsNullOrWhiteSpace(k.CustomEndpoint) && k.ProtocolType == model.ProtocolType)))
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();

                foreach (var uk in userKeys)
                {
                    var decrypted = ApiKeyEncryptionHelper.Decrypt(uk.EncryptedApiKey, _masterSecret);
                    var endpoint = !string.IsNullOrWhiteSpace(uk.CustomEndpoint) ? uk.CustomEndpoint : model.EndpointUrl;
                    var mName = !string.IsNullOrWhiteSpace(uk.CustomModelName) ? uk.CustomModelName : model.ModelName;
                    candidates.Add(new ResolvedKeyCandidate
                    {
                        ApiKey = decrypted,
                        Endpoint = endpoint,
                        ModelName = mName,
                        KeySource = "UserBYOK",
                        KeyEntityId = uk.Id,
                        Priority = 1
                    });
                }
            }

            // Mức 2: Khóa của riêng Tenant (Tenant BYOK)
            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            {
                var tenantKeys = await db.UserAiKeys.AsNoTracking()
                    .Where(k => k.TenantId == tenantId.Value && k.Scope == KeyUsageScope.Private && k.IsActive &&
                               (k.ProviderCompany.ToLower() == model.ProviderCompany.ToLower() || (!string.IsNullOrWhiteSpace(k.CustomEndpoint) && k.ProtocolType == model.ProtocolType)))
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();

                foreach (var tk in tenantKeys)
                {
                    if (!candidates.Any(c => c.KeyEntityId == tk.Id))
                    {
                        var decrypted = ApiKeyEncryptionHelper.Decrypt(tk.EncryptedApiKey, _masterSecret);
                        var endpoint = !string.IsNullOrWhiteSpace(tk.CustomEndpoint) ? tk.CustomEndpoint : model.EndpointUrl;
                        var mName = !string.IsNullOrWhiteSpace(tk.CustomModelName) ? tk.CustomModelName : model.ModelName;
                        candidates.Add(new ResolvedKeyCandidate
                        {
                            ApiKey = decrypted,
                            Endpoint = endpoint,
                            ModelName = mName,
                            KeySource = "TenantBYOK",
                            KeyEntityId = tk.Id,
                            Priority = 2
                        });
                    }
                }
            }

            // Mức 3: Bể khóa đóng góp cộng đồng (Community Contributed Pool - Ưu tiên dùng ít nhất trước)
            var communityKeys = await db.UserAiKeys.AsNoTracking()
                .Where(k => k.Scope == KeyUsageScope.CommunityContributed && k.IsActive &&
                            (k.ProviderCompany.ToLower() == model.ProviderCompany.ToLower() || (!string.IsNullOrWhiteSpace(k.CustomEndpoint) && k.ProtocolType == model.ProtocolType)))
                .OrderBy(k => k.TokensUsedThisMonth)
                .ToListAsync();

            foreach (var ck in communityKeys)
            {
                if (!candidates.Any(c => c.KeyEntityId == ck.Id))
                {
                    var decrypted = ApiKeyEncryptionHelper.Decrypt(ck.EncryptedApiKey, _masterSecret);
                    var endpoint = !string.IsNullOrWhiteSpace(ck.CustomEndpoint) ? ck.CustomEndpoint : model.EndpointUrl;
                    var mName = !string.IsNullOrWhiteSpace(ck.CustomModelName) ? ck.CustomModelName : model.ModelName;
                    candidates.Add(new ResolvedKeyCandidate
                    {
                        ApiKey = decrypted,
                        Endpoint = endpoint,
                        ModelName = mName,
                        KeySource = "CommunityPool",
                        KeyEntityId = ck.Id,
                        Priority = 3
                    });
                }
            }

            // Mức 4: Nút Cục Bộ On-Premise (Ollama/Local) không cần API Key bên ngoài
            if (model.ProviderCompany.Equals("ollama", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(new ResolvedKeyCandidate
                {
                    ApiKey = "ollama-local",
                    Endpoint = !string.IsNullOrWhiteSpace(model.EndpointUrl) ? model.EndpointUrl : "http://host.docker.internal:11434/v1",
                    ModelName = model.ModelName,
                    KeySource = "LocalOnPremise",
                    KeyEntityId = null,
                    Priority = 1
                });
                return candidates;
            }

            // Mức 5: Khóa hệ thống từ cấu hình IConfiguration (System Default)
            string sysKey = model.ProviderCompany.ToLower() switch
            {
                "openrouter" => _config["OpenRouter:ApiKey"] ?? _config["OPENROUTER_API_KEY"] ?? "sk-or-free-tier",
                "deepseek" => _config["DeepSeek:ApiKey"] ?? string.Empty,
                "google" => _config["Gemini:ApiKey"] ?? string.Empty,
                "anthropic" => _config["Anthropic:ApiKey"] ?? string.Empty,
                "openai" => _config["OpenAI:ApiKey"] ?? string.Empty,
                "groq" => _config["Groq:ApiKey"] ?? string.Empty,
                "sambanova" => _config["SambaNova:ApiKey"] ?? string.Empty,
                "cerebras" => _config["Cerebras:ApiKey"] ?? string.Empty,
                "mistral" => _config["Mistral:ApiKey"] ?? string.Empty,
                "github" => _config["GitHub:Token"] ?? string.Empty,
                "replicate" => _config["Replicate:ApiKey"] ?? string.Empty,
                _ => string.Empty
            };

            // Nếu cấu hình chứa nhiều khóa phân tách bởi ';', lấy các khóa đó vào candidates để xoay vòng
            if (!string.IsNullOrWhiteSpace(sysKey))
            {
                var keys = sysKey.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var k in keys)
                {
                    candidates.Add(new ResolvedKeyCandidate
                    {
                        ApiKey = k,
                        Endpoint = model.EndpointUrl,
                        ModelName = model.ModelName,
                        KeySource = "SystemDefault",
                        KeyEntityId = null,
                        Priority = 4
                    });
                }
            }

            return candidates;
        }

        private async Task UpdateKeyUsageTelemetryAsync(Guid keyId, int tokens, bool success)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

                var key = await db.UserAiKeys.FindAsync(keyId);
                if (key != null)
                {
                    key.TotalCallsCount++;
                    if (success) key.SuccessCallsCount++; else key.FailedCallsCount++;
                    key.TokensUsedThisMonth += tokens;
                    key.LastUsedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SeatInvoker] Could not update key telemetry for key {KeyId}", keyId);
            }
        }

        private static AiModelRegistry InferModelMetadata(string modelCode)
        {
            var lower = modelCode.ToLower();

            if (lower.Contains("openrouter"))
            {
                return new AiModelRegistry
                {
                    ModelCode = modelCode,
                    DisplayName = "OpenRouter Free Community Gateway",
                    ProviderCompany = "OpenRouter",
                    CognitiveArchetype = CognitiveArchetype.DeepReasoner,
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    ModelName = lower.Contains("r1") ? "deepseek/deepseek-r1:free" : (lower.Contains("flash") ? "google/gemini-2.0-flash-exp:free" : "meta-llama/llama-3.3-70b-instruct:free"),
                    EndpointUrl = "https://openrouter.ai/api/v1",
                    CostInputPerMillion = 0,
                    CostOutputPerMillion = 0
                };
            }

            if (lower.Contains("ollama") || lower.Contains("local"))
            {
                return new AiModelRegistry
                {
                    ModelCode = modelCode,
                    DisplayName = "Local Sovereign Node (On-Premise)",
                    ProviderCompany = "Ollama",
                    CognitiveArchetype = CognitiveArchetype.SovereignLocal,
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    ModelName = "qwen2.5:32b",
                    EndpointUrl = "http://host.docker.internal:11434/v1",
                    CostInputPerMillion = 0,
                    CostOutputPerMillion = 0
                };
            }

            if (lower.Contains("deepseek"))
            {
                return new AiModelRegistry
                {
                    ModelCode = modelCode,
                    DisplayName = "DeepSeek Reasoning Engine",
                    ProviderCompany = "DeepSeek",
                    CognitiveArchetype = CognitiveArchetype.DeepReasoner,
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    ModelName = lower.Contains("r1") || lower.Contains("reasoner") ? "deepseek-reasoner" : "deepseek-chat",
                    EndpointUrl = "https://api.deepseek.com/v1",
                    CostInputPerMillion = 0.14,
                    CostOutputPerMillion = 0.28
                };
            }

            if (lower.Contains("gemini") || lower.Contains("google"))
            {
                return new AiModelRegistry
                {
                    ModelCode = modelCode,
                    DisplayName = "Google Gemini Epistemic Engine",
                    ProviderCompany = "Google",
                    CognitiveArchetype = CognitiveArchetype.MassiveContext,
                    ProtocolType = ModelProtocolType.GoogleGenerativeAi,
                    ModelName = lower.Contains("pro") ? "gemini-1.5-pro" : "gemini-2.0-flash",
                    EndpointUrl = "https://generativelanguage.googleapis.com/v1beta",
                    CostInputPerMillion = 0.075,
                    CostOutputPerMillion = 0.30
                };
            }

            if (lower.Contains("claude") || lower.Contains("anthropic"))
            {
                return new AiModelRegistry
                {
                    ModelCode = modelCode,
                    DisplayName = "Anthropic Claude Dialectical Engine",
                    ProviderCompany = "Anthropic",
                    CognitiveArchetype = CognitiveArchetype.LinguisticArbiter,
                    ProtocolType = ModelProtocolType.AnthropicMessages,
                    ModelName = "claude-3-5-sonnet-20241022",
                    EndpointUrl = "https://api.anthropic.com/v1",
                    CostInputPerMillion = 3.0,
                    CostOutputPerMillion = 15.0
                };
            }

            if (lower.Contains("llama") || lower.Contains("groq"))
            {
                return new AiModelRegistry
                {
                    ModelCode = modelCode,
                    DisplayName = "Groq LLaMA Ultra-Fast Engine",
                    ProviderCompany = "Groq",
                    CognitiveArchetype = CognitiveArchetype.UltraFastScanner,
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    ModelName = "llama-3.3-70b-versatile",
                    EndpointUrl = "https://api.groq.com/openai/v1",
                    CostInputPerMillion = 0.59,
                    CostOutputPerMillion = 0.79
                };
            }

            if (lower.Contains("sambanova"))
            {
                return new AiModelRegistry
                {
                    ModelCode = modelCode,
                    DisplayName = "SambaNova SN40L Frontier Engine",
                    ProviderCompany = "SambaNova",
                    CognitiveArchetype = CognitiveArchetype.DeepReasoner,
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    ModelName = "Meta-Llama-3.1-405B-Instruct",
                    EndpointUrl = "https://api.sambanova.ai/v1",
                    CostInputPerMillion = 5.0,
                    CostOutputPerMillion = 10.0
                };
            }

            if (lower.Contains("cerebras"))
            {
                return new AiModelRegistry
                {
                    ModelCode = modelCode,
                    DisplayName = "Cerebras Wafer-Scale Ultra-Fast Engine",
                    ProviderCompany = "Cerebras",
                    CognitiveArchetype = CognitiveArchetype.UltraFastScanner,
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    ModelName = "llama-3.3-70b",
                    EndpointUrl = "https://api.cerebras.ai/v1",
                    CostInputPerMillion = 0.60,
                    CostOutputPerMillion = 0.60
                };
            }

            if (lower.Contains("mistral") || lower.Contains("codestral"))
            {
                return new AiModelRegistry
                {
                    ModelCode = modelCode,
                    DisplayName = "Mistral AI Sovereign Frontier",
                    ProviderCompany = "Mistral",
                    CognitiveArchetype = CognitiveArchetype.LinguisticArbiter,
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    ModelName = "mistral-large-latest",
                    EndpointUrl = "https://api.mistral.ai/v1",
                    CostInputPerMillion = 2.0,
                    CostOutputPerMillion = 6.0
                };
            }

            if (lower.Contains("github") || lower.Contains("azure"))
            {
                return new AiModelRegistry
                {
                    ModelCode = modelCode,
                    DisplayName = "GitHub Models Gateway",
                    ProviderCompany = "GitHub",
                    CognitiveArchetype = CognitiveArchetype.LinguisticArbiter,
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    ModelName = "gpt-4o",
                    EndpointUrl = "https://models.inference.ai.azure.com",
                    CostInputPerMillion = 2.5,
                    CostOutputPerMillion = 10.0
                };
            }

            if (lower.Contains("replicate") || lower.Contains("musicgen"))
            {
                return new AiModelRegistry
                {
                    ModelCode = modelCode,
                    DisplayName = "Replicate Multimodal Audio Engine",
                    ProviderCompany = "Replicate",
                    CognitiveArchetype = CognitiveArchetype.MassiveContext,
                    ProtocolType = ModelProtocolType.OpenAiCompatible,
                    ModelName = "facebook/musicgen-small",
                    EndpointUrl = "https://api.replicate.com/v1",
                    CostInputPerMillion = 0.05,
                    CostOutputPerMillion = 0.10
                };
            }

            // Default fallback: Local Ollama / OpenAi-compatible
            return new AiModelRegistry
            {
                ModelCode = modelCode,
                DisplayName = modelCode,
                ProviderCompany = "GenericOpenAI",
                CognitiveArchetype = CognitiveArchetype.SovereignLocal,
                ProtocolType = ModelProtocolType.OpenAiCompatible,
                ModelName = modelCode,
                EndpointUrl = "http://localhost:11434/v1",
                CostInputPerMillion = 0.0,
                CostOutputPerMillion = 0.0
            };
        }
    }
}
