using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.Models.AI
{
    /// <summary>
    /// [AegisAI Epistemic Routing Intent]
    /// Kết quả giải mã ý định nhận thức và phân luồng bản thể học chuyên sâu.
    /// Giúp hệ thống vượt qua các bẫy đa nghĩa (polysemy), đồng nghĩa (synonym gap),
    /// tự động xác định Viện Chủ Trì và Viện Đối Trọng Biện Chứng động 100%.
    /// </summary>
    public class EpistemicRoutingIntent
    {
        /// <summary>Miền tri thức bản thể học cốt lõi (VD: QUANTUM_PHYSICS, MOLECULAR_BIOLOGY, ARCHAEOLOGY, BEHAVIORAL_ECONOMICS)</summary>
        public string PrimaryDomain { get; set; } = string.Empty;

        /// <summary>Các khái niệm tri thức then chốt đã khử đa nghĩa (VD: 'vốn gen', 'đa dạng sinh thái' thay vì 'vốn tài chính')</summary>
        public List<string> CoreEpistemicConcepts { get; set; } = new List<string>();

        /// <summary>Mã Viện Hàn Lâm hiện hữu phù hợp nhất để làm Viện Chủ Trì (Lead Council)</summary>
        public string? RecommendedLeadCouncilCode { get; set; }

        /// <summary>Mã Viện Hàn Lâm hiện hữu phù hợp nhất để làm Viện Đối Trọng Biện Chứng (Contrarian Council)</summary>
        public string? RecommendedContrarianCouncilCode { get; set; }

        /// <summary>Luận giải khoa học vì sao Viện Đối Trọng này tạo ra góc nhìn biện chứng sâu sắc cho đề tài</summary>
        public string DialecticalReason { get; set; } = string.Empty;

        /// <summary>
        /// Điểm khoảng trống tri thức (0.0: Đã có Viện chuyên sâu hoàn hảo, 1.0: Hoàn toàn là miền mới chưa từng có).
        /// Khi KnowledgeGapScore >= 0.70 -> Tự động kích hoạt Genesis Engine sinh Viện mới.
        /// </summary>
        public double KnowledgeGapScore { get; set; }

        /// <summary>Cờ đánh dấu đây là miền tri thức mới, hệ thống hiện tại chưa có Viện chuyên ngành tương ứng</summary>
        public bool IsNovelDomain { get; set; }

        /// <summary>Gợi ý cấu trúc Viện mới nếu IsNovelDomain = true</summary>
        public SuggestedGenesisMetadata? SuggestedGenesis { get; set; }
    }

    /// <summary>
    /// Gợi ý cấu trúc khởi sinh Viện mới khi phát hiện Miền Tri Thức Mới
    /// </summary>
    public class SuggestedGenesisMetadata
    {
        public string ProposedCouncilCode { get; set; } = string.Empty;
        public string ProposedDisplayName { get; set; } = string.Empty;
        public string DisciplineArea { get; set; } = string.Empty;
        public string MaterializedTaxonomyPath { get; set; } = string.Empty;
        public string EpistemicRationale { get; set; } = string.Empty;
    }
}
