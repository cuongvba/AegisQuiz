using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.DTOs
{
    /// <summary>
    /// Gói đề thi nạp sẵn (Pre-warmed Exam Package) tối ưu hóa cho độ trễ nano/micro-second (< 5ms dispatch).
    /// Toàn bộ dữ liệu câu hỏi, công thức toán LaTeX, ảnh SVG Vector và ma trận chấm điểm được nén & cache sẵn.
    /// </summary>
    public class ExamPrewarmPackageDto
    {
        public Guid ExamId { get; set; }
        public Guid TenantId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime PrewarmedAtUtc { get; set; }
        public string ChecksumSha256 { get; set; } = string.Empty;
        public List<PrewarmedQuestionItemDto> Questions { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class PrewarmedQuestionItemDto
    {
        public Guid Id { get; set; }
        public int OrderIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "SINGLE";
        public string DomainCode { get; set; } = "EDUCATION";
        public double Points { get; set; } = 1.0;
        public List<string> Options { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public string? SolutionGuide { get; set; }
        public bool HasMathSvg { get; set; }
    }

    /// <summary>
    /// Gói tín hiệu giám sát phòng thi thời gian thực (ProctorSignal Telemetry).
    /// </summary>
    public class ProctorSignalEventDto
    {
        public Guid ExamSessionId { get; set; }
        public Guid ExamId { get; set; }
        public Guid UserId { get; set; }
        public string CandidateIdentifier { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty; // TAB_SWITCH, WINDOW_BLUR, MULTI_MONITOR, CLIPBOARD, RAPID_ANSWER, HEARTBEAT
        public string Severity { get; set; } = "INFO"; // INFO, WARNING, CRITICAL, DISQUALIFIED
        public string Details { get; set; } = string.Empty;
        public string ClientIp { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Báo cáo xác thực chuỗi băm kiểm toán bất biến (Cryptographic Audit Verification Report).
    /// </summary>
    public class AuditTrailVerificationResultDto
    {
        public Guid ExamId { get; set; }
        public bool IsChainValid { get; set; }
        public int TotalBlocksChecked { get; set; }
        public DateTime VerifiedAtUtc { get; set; } = DateTime.UtcNow;
        public string? BrokenAtBlockId { get; set; }
        public long? BrokenSequenceNumber { get; set; }
        public string? AnomalyDescription { get; set; }
        public string LatestBlockHash { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bảng điều khiển thời gian thực cho Quản trị viên Khảo thí & Trung tâm chỉ huy (SysAdmin Command Center).
    /// </summary>
    public class SysAdminFortressStatusDto
    {
        public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
        public int ActiveExamSessionsCount { get; set; }
        public int ActiveProctoredParticipantsCount { get; set; }
        public int PrewarmedExamsCount { get; set; }
        public List<string> PrewarmedExamTitles { get; set; } = new();
        public double CacheHitRatioPercent { get; set; }
        public long MemoryUsageMb { get; set; }
        public int TotalViolationsToday { get; set; }
        public List<ProctorSignalEventDto> RecentViolationsFeed { get; set; } = new();
        public bool RedisConnected { get; set; }
        public string HealthStatus { get; set; } = "HEALTHY_OPTIMAL";
    }
}
