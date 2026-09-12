using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    public interface IIrtClientService
    {
        Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
        
        Task<CatStartResponse?> StartCatSessionAsync(
            string userId,
            List<QuestionIrtDto> questionBank,
            CatConfigDto config,
            CancellationToken cancellationToken = default);

        Task<CatStepResponse?> SubmitAnswerAndGetNextAsync(
            string sessionId,
            bool isCorrect,
            int timeSpentSecs,
            CancellationToken cancellationToken = default);

        Task<object?> GetSessionResultAsync(
            string sessionId,
            CancellationToken cancellationToken = default);
    }
}
