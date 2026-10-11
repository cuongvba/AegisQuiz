using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Infrastructure.AI.Handoff
{
    /// <summary>
    /// [AegisAI Sovereign - OCP] Triển khai ICognitiveHandoffDispatcher
    /// Duyệt qua toàn bộ danh sách ICognitiveHandoffStrategy đã đăng ký qua DI container
    /// và áp dụng chiến lược có Priority cao nhất có thể xử lý cặp Archetype hiện tại.
    /// </summary>
    public class CognitiveHandoffDispatcher : ICognitiveHandoffDispatcher
    {
        private readonly List<ICognitiveHandoffStrategy> _strategies;
        private readonly ILogger<CognitiveHandoffDispatcher> _logger;

        public CognitiveHandoffDispatcher(
            IEnumerable<ICognitiveHandoffStrategy> strategies,
            ILogger<CognitiveHandoffDispatcher> logger)
        {
            _strategies = strategies.OrderByDescending(s => s.Priority).ToList();
            _logger = logger;
        }

        public (string AdaptedSystemPrompt, string AdaptedUserPrompt) DispatchHandoff(
            ThinkingTraceContext trace, 
            CognitiveArchetype targetArchetype, 
            string baseSystemPrompt, 
            string baseUserPrompt)
        {
            if (trace == null || string.IsNullOrWhiteSpace(trace.AccumulatedScratchpad))
            {
                return (baseSystemPrompt, baseUserPrompt);
            }

            foreach (var strategy in _strategies)
            {
                if (strategy.CanHandle(trace.OriginArchetype, targetArchetype))
                {
                    _logger.LogInformation("[CognitiveHandoff] Áp dụng chiến lược {Strategy} để bàn giao nhận thức từ {From} sang {To}",
                        strategy.GetType().Name, trace.OriginArchetype, targetArchetype);

                    return strategy.AdaptContext(trace, targetArchetype, baseSystemPrompt, baseUserPrompt);
                }
            }

            return (baseSystemPrompt, baseUserPrompt);
        }
    }
}
