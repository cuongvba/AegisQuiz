using System.Collections.Generic;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Multi-Agent Committee Engine - CLEAR Architecture Compliant]
    /// Hợp đồng ứng dụng cho Động cơ Thẩm định Câu hỏi Đa Tác Tử.
    /// Giúp Presentation Controller không phụ thuộc trực tiếp vào Infrastructure.
    /// </summary>
    public interface IMultiAgentCommitteeEngine
    {
        Task<MultiAgentAuditResult> AuditQuestionAsync(
            string rawContent,
            List<string> rawOptions,
            string rawCorrectAnswer,
            string domainCode);
    }
}
