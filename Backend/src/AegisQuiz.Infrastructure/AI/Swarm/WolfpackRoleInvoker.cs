using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Infrastructure.AI.Swarm
{
    /// <summary>
    /// [AegisAI Swarm Engine]
    /// Triển khai điều phối Bầy Sói Đa Mô Hình (Wolfpack Swarm Invoker) cho từng Vai Trò trong Hội Đồng.
    /// Bảo đảm 0ms Cascade Failover, khử bỏ hoàn toàn lỗi chết điểm đơn (Zero SPoF).
    /// </summary>
    public class WolfpackRoleInvoker : IWolfpackRoleInvoker
    {
        private readonly ISeatModelInvoker _seatInvoker;
        private readonly ILogger<WolfpackRoleInvoker> _logger;

        public WolfpackRoleInvoker(
            ISeatModelInvoker seatInvoker,
            ILogger<WolfpackRoleInvoker> logger)
        {
            _seatInvoker = seatInvoker;
            _logger = logger;
        }

        public async Task<SeatInvocationResponse> InvokeRoleSwarmAsync(
            UniversalCouncilSeat seat,
            UniversalAiCouncil council,
            string systemPrompt,
            string userPrompt,
            List<string>? excludeModels = null,
            double temperature = 0.3,
            int maxTokens = 2000,
            Guid? userId = null,
            Guid? tenantId = null,
            CancellationToken ct = default)
        {
            // 1. Tuyển quân Bầy Sói (Alpha Leader + Swarm Members)
            var candidateWolves = new List<string>();

            var alphaModel = !string.IsNullOrWhiteSpace(seat.AlphaModelCode)
                ? seat.AlphaModelCode
                : (!string.IsNullOrWhiteSpace(seat.AssignedModelCode) ? seat.AssignedModelCode : seat.PreferredModelCode);

            if (!string.IsNullOrWhiteSpace(alphaModel))
            {
                candidateWolves.Add(alphaModel);
            }

            if (!string.IsNullOrWhiteSpace(seat.SwarmModelCodesJson))
            {
                try
                {
                    var swarmMembers = JsonSerializer.Deserialize<List<string>>(seat.SwarmModelCodesJson);
                    if (swarmMembers != null)
                    {
                        foreach (var m in swarmMembers.Where(m => !string.IsNullOrWhiteSpace(m)))
                        {
                            if (!candidateWolves.Contains(m)) candidateWolves.Add(m);
                        }
                    }
                }
                catch (Exception exJson)
                {
                    _logger.LogWarning(exJson, "[Wolfpack] Lỗi giải mã SwarmModelCodesJson cho ghế {Seat}: {Message}", seat.SeatTitle, exJson.Message);
                }
            }

            // Loại trừ các models nếu có yêu cầu (ví dụ: Trọng tài chấm mù loại trừ các model đã phát biểu)
            if (excludeModels != null && excludeModels.Count > 0)
            {
                var excludeSet = new HashSet<string>(excludeModels, StringComparer.OrdinalIgnoreCase);
                candidateWolves = candidateWolves.Where(m => !excludeSet.Contains(m)).ToList();
            }

            // Fallback an toàn nếu bầy sói trống: Bảo đảm đủ chân tất cả các họ Sói trong hệ thống
            if (candidateWolves.Count == 0)
            {
                var fullFallback = new[] { "gemini-2.0-flash", "openrouter-deepseek-r1-free", "groq-llama-3-3", "ollama-qwen-local", "deepseek-r1", "claude-3-5-sonnet", "gpt-4o", "gpt-4o-mini" };
                if (excludeModels != null && excludeModels.Count > 0)
                {
                    var excludeSet = new HashSet<string>(excludeModels, StringComparer.OrdinalIgnoreCase);
                    var independentFallback = fullFallback.Where(m => !excludeSet.Contains(m)).ToList();
                    if (independentFallback.Count > 0)
                    {
                        candidateWolves.AddRange(independentFallback);
                    }
                    else
                    {
                        _logger.LogWarning("[Wolfpack] Toàn bộ Sói hệ thống đều nằm trong excludeModels, kích hoạt cơ chế Blind Neutrality Arbiter fallback.");
                        candidateWolves.AddRange(fullFallback);
                    }
                }
                else
                {
                    candidateWolves.AddRange(fullFallback);
                }
            }

            // [WPSE Resilient Sovereign Law] Bảo đảm mỗi Bầy Sói luôn có ít nhất 1 Sói Free và 1 Sói Local On-Premise ở hậu phương
            // để bảo đảm phiên tranh luận không bao giờ bị đứt đoạn khi các model thương mại hết quota (402/429)
            var resilientBackupWolves = new[] { "gemini-2.0-flash", "openrouter-deepseek-r1-free", "groq-llama-3-3", "ollama-qwen-local" };
            foreach (var backupWolf in resilientBackupWolves)
            {
                if (!candidateWolves.Contains(backupWolf, StringComparer.OrdinalIgnoreCase) &&
                    (excludeModels == null || !excludeModels.Contains(backupWolf, StringComparer.OrdinalIgnoreCase)))
                {
                    candidateWolves.Add(backupWolf);
                }
            }

            _logger.LogInformation(
                "[WolfpackSwarm] Kích hoạt Bầy Sói cho Ghế [{Seat}] thuộc Hội Đồng [{Council}]. Danh sách {Count} Sói: {Wolves}",
                seat.SeatTitle, council.DisplayName, candidateWolves.Count, string.Join(", ", candidateWolves));

            var failureLogs = new List<string>();

            // 2. Chu trình Cascade Failover tuần tự qua từng Sói trong Bầy
            for (int i = 0; i < candidateWolves.Count; i++)
            {
                var currentWolf = candidateWolves[i];
                var isAlpha = (i == 0);

                var req = new SeatInvocationRequest
                {
                    ModelCode = currentWolf,
                    SystemPrompt = systemPrompt,
                    UserPrompt = userPrompt,
                    Temperature = temperature,
                    MaxTokens = maxTokens,
                    UserId = userId,
                    TenantId = tenantId
                };

                try
                {
                    var resp = await _seatInvoker.InvokeSeatAsync(req, ct);
                    if (resp.Success && !string.IsNullOrWhiteSpace(resp.Content))
                    {
                        if (!isAlpha)
                        {
                            _logger.LogInformation(
                                "[WolfpackSwarm] Sói kế thừa [{Wolf}] đã thế chân thành công cho Ghế [{Seat}] sau khi Sói trước gặp sự cố.",
                                currentWolf, seat.SeatTitle);
                        }
                        return resp;
                    }

                    var reason = resp.ErrorMessage ?? "Phản hồi rỗng";
                    failureLogs.Add($"[{currentWolf}]: {reason}");
                    _logger.LogWarning("[WolfpackSwarm] Sói [{Wolf}] không phản hồi ({Reason}). Đang chuyển tiếp sang Sói tiếp theo...", currentWolf, reason);
                }
                catch (Exception ex)
                {
                    failureLogs.Add($"[{currentWolf}]: {ex.Message}");
                    _logger.LogWarning(ex, "[WolfpackSwarm] Sói [{Wolf}] gặp ngoại lệ khi thực thi. Tiếp tục chuyển tiếp...", currentWolf);
                }
            }

            _logger.LogError("[WolfpackSwarm] Toàn bộ {Count} Sói trong Bầy của Ghế [{Seat}] đều không thể phản hồi!", candidateWolves.Count, seat.SeatTitle);
            return new SeatInvocationResponse
            {
                Success = false,
                ErrorMessage = $"Toàn bộ Bầy Sói ({candidateWolves.Count} mô hình) đều không phản hồi: {string.Join("; ", failureLogs)}",
                ModelUsed = candidateWolves.FirstOrDefault() ?? "unknown",
                LatencyMs = 0
            };
        }
    }
}
