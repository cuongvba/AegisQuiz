using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Epoch 5: CognitiveOS] Dịch vụ Đồ thị Tri thức Nhận thức & Truy vết Lỗ hổng gốc (Root-Cause Gap Traversal).
    /// </summary>
    public interface IKnowledgeGraphService
    {
        Task<List<KnowledgeGraphNodeDto>> GetDomainConceptsAsync(string domainCode = "EDUCATION", Guid? userId = null, CancellationToken ct = default);
        Task<List<string>> TraceRootCauseGapsAsync(string failedConceptCode, Guid userId, CancellationToken ct = default);
        Task<List<KnowledgeGraphNodeDto>> GetRemediationLearningPathAsync(string targetConceptCode, Guid userId, CancellationToken ct = default);
    }
}
