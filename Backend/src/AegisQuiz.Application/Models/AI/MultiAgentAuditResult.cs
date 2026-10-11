using System.Collections.Generic;

namespace AegisQuiz.Application.Models.AI
{
    /// <summary>
    /// Kết quả thẩm định câu hỏi đa tác tử (Multi-Agent Committee Audit)
    /// </summary>
    public class MultiAgentAuditResult
    {
        public bool IsApproved { get; set; } = true;
        public string CertifiedQuestion { get; set; } = string.Empty;
        public List<string> CertifiedOptions { get; set; } = new();
        public string CertifiedCorrectAnswer { get; set; } = string.Empty;
        public string AdversarialCritique { get; set; } = string.Empty;
        public int BloomLevel { get; set; } = 3;
        public double EstimatedIrtDifficulty { get; set; } = 0.5;
        public double EstimatedIrtDiscrimination { get; set; } = 1.2;
        public PedagogicalWalkthroughDto Walkthrough { get; set; } = new();
    }
}
