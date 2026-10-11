using System;
using System.Collections.Generic;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Models.AI
{
    /// <summary>
    /// [AegisAI Symposium Teleology Mode]
    /// Phổ Mục Tiêu Linh Hoạt của Đại Hội Thảo Chuyên Gia:
    /// Cho phép định hướng hội thảo theo mục đích thực tế của nhân loại.
    /// </summary>
    public enum SymposiumTeleologyMode
    {
        /// <summary>🧠 Tự động phân tích ngữ nghĩa đề tài để chọn trọng tâm phù hợp nhất</summary>
        AutoDetect = 0,

        /// <summary>📊 Thực nghiệm & Hành động: Thực trạng -> Căn nguyên -> Điểm tiến bộ -> Gói giải pháp thi hành</summary>
        EmpiricalActionable = 1,

        /// <summary>💡 Khởi sinh Tri thức & Nan đề: Khái niệm mới -> Tiên đề nền tảng -> Nan đề mở cho nhân loại</summary>
        EpistemicGenesis = 2,

        /// <summary>🛡️ Toàn năng Lai ghép (Master): Tích hợp trọn vẹn cả Thực trạng/Giải pháp và Tiên đề/Nan đề tầm vóc</summary>
        UniversalHybrid = 3
    }

    /// <summary>
    /// Yêu cầu triệu tập Đại Hội Thảo Bách Khoa Vạn Vật Tri Thức
    /// </summary>
    public class ConveneSymposiumRequest
    {
        public string TopicTitle { get; set; } = string.Empty;
        public string CoreProblemStatement { get; set; } = string.Empty;
        public List<string>? SpecificCouncilCodes { get; set; }
        public bool EnableAdversarialCritique { get; set; } = true;
        public bool AllowDynamicGenesis { get; set; } = true;
        public SymposiumTeleologyMode TeleologyMode { get; set; } = SymposiumTeleologyMode.UniversalHybrid;

        /// <summary>Ngôn ngữ mục tiêu của phiên hội thảo (vi-VN, en-US, ja-JP...)</summary>
        public string Locale { get; set; } = "vi-VN";

        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }

        /// <summary>Mã phiên hội thảo do Client khởi tạo để kết nối SignalR Real-time Stream</summary>
        public Guid? ClientSessionId { get; set; }

        /// <summary>Số thứ tự vòng hội thảo biện chứng (1, 2, 3...)</summary>
        public int RoundNumber { get; set; } = 1;

        /// <summary>Mã phiên hội thảo vòng trước (nếu là vòng 2 trở đi)</summary>
        public Guid? ParentSessionId { get; set; }

        /// <summary>Nghị quyết/kết luận của vòng trước làm đầu vào biện chứng cho vòng này</summary>
        public string? PreviousRoundResolution { get; set; }

        /// <summary>Ý kiến chỉ đạo, định hướng và chất vấn Socratic từ Chủ tọa (Human-in-the-loop)</summary>
        public string? SocraticDirectives { get; set; }

        /// <summary>Văn kiện, tài liệu hoặc dữ liệu thực chứng bổ sung do người dùng nạp vào</summary>
        public string? AdditionalDocuments { get; set; }

        /// <summary>Danh sách URI, DOI hoặc nguồn web tham chiếu bổ sung do người dùng cung cấp</summary>
        public List<string>? AdditionalUris { get; set; }

        /// <summary>Mã Notebook Hub dùng để ghi vết tích lũy liên tục qua mọi vòng</summary>
        public Guid? TargetNotebookId { get; set; }

        /// <summary>Chủ tọa quyết định đây là vòng chung cuộc để chốt hạ nghị quyết tối hậu</summary>
        public bool IsFinalRound { get; set; }
    }

    /// <summary>
    /// Bước phát biểu hoặc phản biện trong Đại Hội Thảo
    /// </summary>
    public class SymposiumDialogueTurn
    {
        public int RoundNumber { get; set; } = 1;
        public int PhaseNumber { get; set; } // 1: Formulation, 2: Inquiries, 3: Critique, 4: Synthesis, 5: Scoring, 6: Resolution
        public string PhaseName { get; set; } = string.Empty;
        public string CouncilCode { get; set; } = string.Empty;
        public string CouncilDisplayName { get; set; } = string.Empty;
        public string SpeakerTitle { get; set; } = string.Empty;
        public string ModelCode { get; set; } = string.Empty;
        public string SpatioTemporalPerspective { get; set; } = string.Empty; // Trường phái không - thời gian (Đông Phương Luận, Tây Phương Thực Chứng...)
        public string AssignmentRationale { get; set; } = string.Empty; // Lý do mô hình này được giao phó vai trò
        public string Content { get; set; } = string.Empty;
        public List<string> KeyTheses { get; set; } = new();
        public List<string> ExposedFallacies { get; set; } = new();
        /// <summary>Độ trễ ĐO THẬT từ lời gọi mô hình. 0 = lượt không gọi mô hình (không hiển thị).</summary>
        public long LatencyMs { get; set; }
        /// <summary>true = nội dung sinh từ mẫu, KHÔNG phải mô hình AI phát biểu.</summary>
        public bool IsSimulated { get; set; }
        public string? SimulationReason { get; set; }
        /// <summary>Nguồn gốc khóa API đã giải quyết: UserBYOK, TenantBYOK, CommunityPool, SystemDefault</summary>
        public string? KeySource { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Điểm số ma trận Rubric đa trục
    /// </summary>
    public class SymposiumRubricScore
    {
        /// <summary>true chỉ khi Trọng tài độc lập đã thực sự chấm. false = mọi điểm số vô nghĩa, UI không được hiển thị.</summary>
        public bool IsEvaluated { get; set; }
        public string? EvaluationNote { get; set; }
        public string EvaluatedByModel { get; set; } = string.Empty;
        public double GroundingRate { get; set; } // Tỷ lệ phát biểu có trích dẫn [E#] hợp lệ (0-100%)
        public int ValidCitationsCount { get; set; }
        public double LogicalRigor { get; set; }           // R1: Chặt chẽ logic & toán học (0-10)
        public double EmpiricalFalsifiability { get; set; } // R2: Khả năng kiểm chứng thực nghiệm (0-10)
        public double OntologicalDepth { get; set; }        // R3: Chiều sâu bản thể & nhân văn (0-10)
        public double PragmaticActionability { get; set; }  // R4: Tính khả thi ứng dụng (0-10)
        public double TrapResistance { get; set; }          // R5: Khả năng kháng ngụy biện & bẫy (0-10)
        public double OverallCompositeScore => Math.Round(
            (LogicalRigor * 0.25) +
            (EmpiricalFalsifiability * 0.25) +
            (OntologicalDepth * 0.20) +
            (PragmaticActionability * 0.15) +
            (TrapResistance * 0.15), 2);
    }

    /// <summary>
    /// Giải pháp hành động cụ thể có phân kỳ lộ trình thực thi
    /// </summary>
    public class ActionableSolutionItemDto
    {
        public string Horizon { get; set; } = "Ngắn hạn"; // Ngắn hạn (0-1 năm), Trung hạn (1-5 năm), Dài hạn (chiến lược)
        public string SolutionTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ResponsibleDomain { get; set; } = string.Empty;
        public double FeasibilityScore { get; set; } = 8.5; // Điểm khả thi 0-10
        public List<string> GroundedEvidenceIds { get; set; } = new();
    }

    /// <summary>
    /// [AegisAI Symposium - Structured Outcome Contract]
    /// Kết tinh 5 Khối Tri Thức Toàn Diện từ Đại Hội Thảo Chuyên Gia
    /// </summary>
    public class SymposiumStructuredOutcomeDto
    {
        public SymposiumTeleologyMode AppliedMode { get; set; } = SymposiumTeleologyMode.UniversalHybrid;

        /// <summary>1. Bằng chứng & Thực trạng hiện hữu (bóc tách từ Sổ cái [E#])</summary>
        public List<string> EmpiricalStatusQuo { get; set; } = new();

        /// <summary>2. Căn nguyên cốt lõi (Root Cause Analysis)</summary>
        public List<string> RootCauses { get; set; } = new();

        /// <summary>3. Điểm tiến bộ & Mô hình sáng đã được kiểm chứng (Progress & Milestones)</summary>
        public List<string> ValidatedProgress { get; set; } = new();

        /// <summary>4. Nan đề / Nghịch lý chưa đồng thuận (Core Antinomy / Paradox / Trilemma)</summary>
        public string? CoreAntinomy { get; set; }

        /// <summary>5. Gói giải pháp hành động phân kỳ lộ trình (Actionable Solutions & Execution Roadmap)</summary>
        public List<ActionableSolutionItemDto> ActionableSolutions { get; set; } = new();

        /// <summary>6. Tiên đề / Tri thức mới đóng góp cho nhân loại (Novel Axiomatic Insight)</summary>
        public string? NovelEpistemicAxiom { get; set; }

        /// <summary>7. Các phỏng đoán khoa học mở (Open Research Conjectures)</summary>
        public List<string> OpenConjectures { get; set; } = new();
    }

    /// <summary>
    /// Đóng góp học thuật chi tiết của từng Viện Hàn Lâm tham gia
    /// </summary>
    public class CouncilAcademicContributionDto
    {
        public string CouncilCode { get; set; } = string.Empty;
        public string CouncilDisplayName { get; set; } = string.Empty;
        public string SpatioTemporalAffinity { get; set; } = string.Empty;
        public string RepresentativeModel { get; set; } = string.Empty;
        public string ScholarTitle { get; set; } = string.Empty;
        public string CoreEpistemicStance { get; set; } = string.Empty; // Lập trường nhận thức then chốt
        public string DeepAnalysis { get; set; } = string.Empty;        // Luận giải chi tiết chuyên sâu của Viện
        public string PrimaryObjection { get; set; } = string.Empty;    // Điểm phản biện sắc bén nhất
        public string ProposedRemedy { get; set; } = string.Empty;      // Giải pháp hòa giải mang tính đột phá
        public List<string> KeyTheses { get; set; } = new();            // Hạt nhân luận điểm
    }

    /// <summary>
    /// Kết quả Kỷ yếu Đại Hội Thảo & Tuyên ngôn Tiến hóa
    /// </summary>
    public class SymposiumProceedingsResult
    {
        public Guid SessionId { get; set; }
        public string TopicTitle { get; set; } = string.Empty;
        public string CoreProblemStatement { get; set; } = string.Empty;
        public SymposiumTeleologyMode TeleologyMode { get; set; } = SymposiumTeleologyMode.UniversalHybrid;
        public List<string> ParticipatingCouncils { get; set; } = new();
        public List<CouncilAcademicContributionDto> CouncilContributions { get; set; } = new();
        public List<SymposiumDialogueTurn> DialogueLog { get; set; } = new();
        public string UnifiedMetatheory { get; set; } = string.Empty;
        public string FinalConsensusDeclaration { get; set; } = string.Empty;
        public SymposiumRubricScore EvaluatedRubric { get; set; } = new();
        /// <summary>EEV ∈ [0,1]. Chỉ có nghĩa khi đã được tính từ dữ liệu thật; 0 = chưa tính.</summary>
        public double EpistemicEvolutionIndex { get; set; }
        public EmpiricalEvidenceDossierDto? EmpiricalDossier { get; set; }
        public double GroundingRate { get; set; }
        public List<string> GroundedEvidenceIds { get; set; } = new();
        public SymposiumStructuredOutcomeDto? StructuredOutcome { get; set; }
        /// <summary>true = phiên chưa có lời gọi mô hình thật (nội dung mẫu).</summary>
        public bool IsSimulated { get; set; }
        public string? SimulationReason { get; set; }
        public long TotalSessionDurationMs { get; set; }
        public DateTime ConcludedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Số thứ tự vòng hội thảo biện chứng (1, 2, 3...)</summary>
        public int RoundNumber { get; set; } = 1;

        /// <summary>Mã phiên hội thảo tiền nhiệm (nếu có)</summary>
        public Guid? ParentSessionId { get; set; }

        /// <summary>Đã đạt ngưỡng đồng thuận chân lý tối cao hoặc do Chủ tọa tuyên bố kết thúc</summary>
        public bool IsFinalRound { get; set; }

        /// <summary>Chỉ số đồng thuận / chân lý (EEV) đã đạt ngưỡng mục tiêu</summary>
        public bool ReachedEpistemicConsensus { get; set; }

        /// <summary>Ý kiến chỉ đạo Socratic từ Chủ tọa đã áp dụng cho vòng này</summary>
        public string? SocraticDirectives { get; set; }

        /// <summary>Mã Notebook Hub lưu vết toàn bộ các pha của các vòng hội thảo</summary>
        public Guid? TargetNotebookId { get; set; }

        /// <summary>Thông báo trạng thái lưu vết vào Notebook</summary>
        public string? NotebookArchiveMessage { get; set; }
    }
}
