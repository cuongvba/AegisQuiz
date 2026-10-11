using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [CLEAR Architecture Compliant]
    /// Bộ điều phối thực thi Bầy Sói Đa Mô Hình cho từng Vai Trò trong Hội Đồng (Role's Wolfpack Swarm Invoker)
    /// </summary>
    public interface IWolfpackRoleInvoker
    {
        Task<SeatInvocationResponse> InvokeRoleSwarmAsync(
            UniversalCouncilSeat seat,
            UniversalAiCouncil council,
            string systemPrompt,
            string userPrompt,
            List<string>? excludeModels = null,
            double temperature = 0.3,
            int maxTokens = 2000,
            Guid? userId = null,
            Guid? tenantId = null,
            CancellationToken ct = default);
    }
}
