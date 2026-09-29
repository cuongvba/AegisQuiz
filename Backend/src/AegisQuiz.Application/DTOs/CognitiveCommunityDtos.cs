using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.DTOs
{
    // ══════════════════════════════════════════════════════════════════════════
    // EPOCH 4: COMMUNITY ECOSYSTEM DTOs
    // ══════════════════════════════════════════════════════════════════════════

    public class AegisCoinWalletDto
    {
        public Guid WalletId { get; set; }
        public Guid UserId { get; set; }
        public long Balance { get; set; }
        public long LifetimeEarned { get; set; }
        public long LifetimeSpent { get; set; }
        public DateTime? LastEarnedAt { get; set; }
        public List<AegisCoinTransactionItemDto> RecentTransactions { get; set; } = new();
    }

    public class AegisCoinTransactionItemDto
    {
        public Guid Id { get; set; }
        public long Amount { get; set; }
        public long BalanceAfter { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AegisCoinRewardRequestDto
    {
        public Guid UserId { get; set; }
        public long Amount { get; set; }
        public string TransactionType { get; set; } = "PRACTICE_COMPLETED";
        public string Description { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
    }

    public class AegisCoinSpendRequestDto
    {
        public Guid UserId { get; set; }
        public long Amount { get; set; }
        public string ItemCode { get; set; } = string.Empty; // e.g. "UNLOCK_MOCK_EXAM_2026", "AI_DEEP_SOCRATIC"
        public string Description { get; set; } = string.Empty;
    }

    public class PeerChallengeDto
    {
        public string ShareCode { get; set; } = string.Empty;
        public Guid ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public string CreatorName { get; set; } = string.Empty;
        public double CreatorScore { get; set; }
        public int TotalQuestions { get; set; }
        public int DurationMinutes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<PeerChallengeParticipantDto> Leaderboard { get; set; } = new();
    }

    public class PeerChallengeParticipantDto
    {
        public string CandidateIdentifier { get; set; } = string.Empty;
        public double Score { get; set; }
        public int TimeSpentSeconds { get; set; }
        public DateTime CompletedAtUtc { get; set; }
    }

    public class ClassLeaderboardEntryDto
    {
        public int Rank { get; set; }
        public Guid UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public double TotalScore { get; set; }
        public int StreakDays { get; set; }
        public long AegisCoins { get; set; }
        public string BadgeTitle { get; set; } = "Học Viên Tiên Phong";
    }

    public class ContentModerationResultDto
    {
        public bool IsApproved { get; set; }
        public double RiskScore { get; set; } // 0.0 (an toàn tuyệt đối) -> 1.0 (nguy hiểm)
        public List<string> FlaggedCategories { get; set; } = new();
        public string QualityAssessment { get; set; } = "ACCORDING_TO_CURRICULUM_STANDARDS";
        public List<string> RemediationSuggestions { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // EPOCH 5: COGNITIVE OS DTOs
    // ══════════════════════════════════════════════════════════════════════════

    public class DktStateDto
    {
        public Guid UserId { get; set; }
        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
        public Dictionary<string, double> ConceptMasteryMap { get; set; } = new(); // conceptCode -> 0.0 to 1.0
        public double OverallCognitiveHealthIndex { get; set; }
        public List<string> IdentifiedBlindSpots { get; set; } = new();
        public List<string> RecommendedNextConcepts { get; set; } = new();
    }

    public class KnowledgeGraphNodeDto
    {
        public string ConceptCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DomainCode { get; set; } = "EDUCATION";
        public int DifficultyRank { get; set; }
        public double StudentMasteryPercent { get; set; }
        public string? CurriculumReference { get; set; }
        public List<string> Prerequisites { get; set; } = new();
        public List<string> SubConcepts { get; set; } = new();
    }

    public class PedagogicalRagResponseDto
    {
        public Guid QuestionId { get; set; }
        public string SocraticExplanation { get; set; } = string.Empty;
        public string CurriculumStandard { get; set; } = "GDPT 2018";
        public List<CurriculumCitationDto> Citations { get; set; } = new();
        public List<string> KeyTheoremsAndFormulas { get; set; } = new();
        public List<string> SocraticFollowUpQuestions { get; set; } = new();
    }

    public class CurriculumCitationDto
    {
        public string TextbookName { get; set; } = string.Empty;
        public string ChapterTitle { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public string OfficialDecree { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
    }
}
