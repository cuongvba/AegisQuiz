using System;
using System.Linq;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.AI.Teleology
{
    /// <summary>
    /// [AegisAI Teleology Engine] Bộ phân giải mục tiêu biện chứng linh động.
    /// Tự động suy luận mục đích thông qua NLP phân tích ngữ nghĩa đề tài và vấn đề khởi thảo.
    /// </summary>
    public class SymposiumTeleologyResolver : ISymposiumTeleologyResolver
    {
        private static readonly string[] ActionableKeywords =
        {
            "thực trạng", "nguyên nhân", "giải pháp", "lộ trình", "chính sách", "khắc phục",
            "hành động", "ngân hàng", "tài chính", "y tế", "triển khai", "ứng dụng", "tối ưu",
            "cải thiện", "quản trị", "kinh tế", "doanh nghiệp", "rủi ro", "xử lý", "biện pháp",
            "action", "solution", "roadmap", "policy", "reform", "implementation", "mitigation", "strategy", "governance"
        };

        private static readonly string[] EpistemicKeywords =
        {
            "bản chất", "tiên đề", "nghịch lý", "nan đề", "ý thức", "vũ trụ", "lượng tử",
            "nguồn gốc", "tương lai xa", "triết học", "bản thể", "nhận thức luận", "định lý",
            "paradox", "foundations", "epistemic", "ontology", "conjecture", "agi", "singularity", "axiom"
        };

        public SymposiumTeleologyMode ResolveTeleologyMode(SymposiumTeleologyMode requested, string topicTitle, string coreProblemStatement)
        {
            if (requested != SymposiumTeleologyMode.AutoDetect)
            {
                return requested;
            }

            var combined = $"{topicTitle} {coreProblemStatement}".ToLowerInvariant();

            int actionableHits = ActionableKeywords.Count(k => combined.Contains(k, StringComparison.OrdinalIgnoreCase));
            int epistemicHits = EpistemicKeywords.Count(k => combined.Contains(k, StringComparison.OrdinalIgnoreCase));

            if (actionableHits > 0 && epistemicHits == 0)
            {
                return SymposiumTeleologyMode.EmpiricalActionable;
            }

            if (epistemicHits > 0 && actionableHits == 0)
            {
                return SymposiumTeleologyMode.EpistemicGenesis;
            }

            // Mặc định hoặc khi hội tụ cả 2 yếu tố: UniversalHybrid (Toàn năng lai ghép)
            return SymposiumTeleologyMode.UniversalHybrid;
        }
    }
}
