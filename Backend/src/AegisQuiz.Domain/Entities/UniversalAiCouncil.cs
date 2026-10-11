using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisQuiz.Domain.Entities
{
    public enum CouncilCategory
    {
        Academic = 1,     // Các Viện Hàn Lâm tham gia tranh biện Đại Hội Thảo
        Secretariat = 2,  // Ban Thư Ký Hợp Nhất (SYS_SECRETARIAT)
        Arbitration = 3,  // Tổ Trọng Tài Độc Lập (SYS_ARBITRATION)
        SystemCore = 4    // Hội Đồng Hệ Thống Cốt Lõi (SYS_CORE)
    }

    /// <summary>
    /// [AegisAI Universal Epistemic Domain]
    /// Hội Đồng Chuyên Gia / Viện Hàn Lâm Nhận Thức Vũ Trụ
    /// Đại diện cho một miền tri thức rộng lớn (Vũ trụ, Sự sống, Toán học, Triết học, Kinh tế, AI...)
    /// hoặc các Hội đồng chức năng cấp cao (Ban Thư Ký, Tổ Trọng Tài, Hội Đồng Hệ Thống Cốt Lõi).
    /// </summary>
    public class UniversalAiCouncil
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CouncilCode { get; set; } = string.Empty; // COSMOLOGY, LIFE_SCIENCES, SYS_SECRETARIAT, SYS_ARBITRATION, SYS_CORE...
        public string DisplayName { get; set; } = string.Empty;
        public CouncilCategory Category { get; set; } = CouncilCategory.Academic;
        public string DisciplineArea { get; set; } = string.Empty; // Miền ngành lớn
        public string MaterializedTaxonomyPath { get; set; } = string.Empty; // /UNIVERSE/PHYSICS/QUANTUM ($O(1)$)
        public string EpistemicManifesto { get; set; } = string.Empty; // Tuyên ngôn học thuật & lập trường nhận thức
        public string TriggerKeywordsJson { get; set; } = "[]"; // Danh sách từ khóa kích hoạt nhận thức
        public int BenchmarkPrestigeElo { get; set; } = 2800;
        public bool IsActive { get; set; } = true;
        public string SpatioTemporalAffinity { get; set; } = "Toàn Cầu Thực Chứng & Siêu Hình Học"; // Không gian văn hóa / chuyên môn sâu
        public string ReadinessStatus { get; set; } = "READY"; // READY, DEGRADED, OFFLINE
        public int ReadySeatsCount { get; set; } = 0;
        public int TotalSeatsCount { get; set; } = 0;
        public string ReadinessMessage { get; set; } = "Sẵn sàng hoạt động";
        public List<UniversalCouncilSeat> Seats { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Bản sắc Mô hình & Bể chứa Khóa (Keys Pool) của từng Sói trong Bầy
    /// Thể hiện thế mạnh đương thời và thác lũ ưu tiên: User BYOK -> Community -> System
    /// </summary>
    public class WolfModelKeyPoolSummary
    {
        public string ModelCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ProviderCompany { get; set; } = string.Empty;
        public CognitiveArchetype CognitiveArchetype { get; set; }
        public string ContemporaryStrength { get; set; } = string.Empty;
        public int UserKeysCount { get; set; }
        public bool HasSystemKey { get; set; }
        public bool HasCommunityKey { get; set; }
        public bool IsKeyPoolReady => UserKeysCount > 0 || HasSystemKey || HasCommunityKey;
        public string KeyPriorityFlow { get; set; } = "User BYOK (Ưu tiên 1) ➔ Community Pool ➔ System Pool";
    }

    /// <summary>
    /// Ghế học giả / Vai trò trong Hội đồng (Council Role)
    /// Mỗi Ghế được trao quyền năng cho một Bầy Sói Đa Mô Hình (WolfpackSwarm)
    /// </summary>
    public class UniversalCouncilSeat
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CouncilId { get; set; }
        public string SeatTitle { get; set; } = string.Empty; // Chủ tịch Hội đồng, Biện hộ viên đối kháng, Thẩm tra viên thực nghiệm...
        public CouncilSeatRole Role { get; set; } = CouncilSeatRole.President;
        public CognitiveArchetype IdealArchetype { get; set; }
        public string PreferredModelCode { get; set; } = string.Empty; // Model mặc định (tương thích ngược)
        public string AssignedModelCode { get; set; } = string.Empty; // Model được phân vai động theo không - thời gian
        public string AssignmentRationale { get; set; } = string.Empty; // Lý do phân vai theo không - thời gian hiện thời

        // 🐺 BẦY SÓI ĐẢM NHIỆM CHO VAI TRÒ NÀY (ROLE'S WOLFPACK SWARM)
        /// <summary>Sói Đầu Đàn (Alpha Model) phụ trách chính cho ghế này</summary>
        public string AlphaModelCode { get; set; } = string.Empty;

        /// <summary>Danh sách các mô hình trong Bầy Sói (Wolfpack Members / Fallbacks) dưới dạng JSON</summary>
        public string SwarmModelCodesJson { get; set; } = "[]";

        /// <summary>Chính sách vận hành Bầy Sói: CASCADE_FAILOVER hoặc SWARM_CONSENSUS</summary>
        public string SwarmPolicy { get; set; } = "CASCADE_FAILOVER";

        public bool IsReady { get; set; } = true;
        public double DecisionWeight { get; set; } = 1.0;
        public int SortOrder { get; set; } = 0;
        public string PersonaPromptDirective { get; set; } = string.Empty;

        /// <summary>Hồ sơ năng lực & Bể chứa Khóa (Keys Pool) của từng Sói trong Bầy</summary>
        [NotMapped]
        public List<WolfModelKeyPoolSummary> SwarmWolfProfiles { get; set; } = new();
    }

    public enum CouncilSeatRole
    {
        President = 1,          // Chủ tịch Viện / Báo cáo viên trưởng (Tổng hợp, ra phán quyết)
        AdversarialCritic = 2,  // Kẻ phản biện khắc nghiệt (Devil's Advocate, tìm lỗ hổng)
        EmpiricalAuditor = 3,   // Thư ký kiểm chứng dữ kiện & thực nghiệm (Fact-checker)
        Synthesizer = 4         // Học giả kiến tạo giải pháp siêu hình (Metatheorist)
    }

    /// <summary>
    /// Phiên Đại Hội Thảo Biện Chứng Vạn Vật Tri Thức
    /// </summary>
    public class SymposiumSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TopicTitle { get; set; } = string.Empty;
        public string CoreProblemStatement { get; set; } = string.Empty;
        public string ParticipatingCouncilCodesJson { get; set; } = "[]";
        public string DialogueLogJson { get; set; } = "[]";
        public string FinalConsensusResolution { get; set; } = string.Empty;
        public double EpistemicConsensusIndex { get; set; } = 1.0;
        public string RubricScoresJson { get; set; } = "{}";
        public DateTime ConvenedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ConcludedAt { get; set; }
        public Guid? InitiatedByUserId { get; set; }
        public Guid TenantId { get; set; } = Guid.Empty;
    }
}
