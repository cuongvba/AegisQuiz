using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Symposium Architecture] Bộ phân giải mục tiêu biện chứng linh động (Teleology Resolver).
    /// Xác định trọng tâm hội thảo (Thực nghiệm Giải pháp vs Tri thức Tiên đề vs Toàn năng lai ghép).
    /// </summary>
    public interface ISymposiumTeleologyResolver
    {
        /// <summary>
        /// Xác định mục tiêu biện chứng hiệu dụng dựa trên yêu cầu người dùng và ngữ nghĩa đề tài.
        /// </summary>
        SymposiumTeleologyMode ResolveTeleologyMode(SymposiumTeleologyMode requested, string topicTitle, string coreProblemStatement);
    }
}
