using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.DTOs
{
    public class SniffTaxonomyRequest
    {
        public string? ContentPreview { get; set; }
    }

    public class LaunchZenRoomRequest
    {
        public string Title { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public string? SubjectCode { get; set; }
        public int DurationMinutes { get; set; } = 15;
    }

    public class UpdatePayoutConfigRequest
    {
        public string BankAccount { get; set; } = string.Empty;
        public string? BankName { get; set; }
    }

    public class CreatorPartnershipDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string ReferralCode { get; set; } = string.Empty;
        public DateTime TrialExpiresAt { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsActive { get; set; }
        public decimal RevenueShareRate { get; set; }
        public int KarmaPoints { get; set; }
        public string? PayoutBankAccount { get; set; }
    }

    public class ZenUploadResponseDto
    {
        public bool Success { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int DuplicateCount { get; set; }
        public int UniqueCount { get; set; }
        public double DeduplicationRatePercentage { get; set; }
        public object? SuggestedTaxonomy { get; set; }
        public List<ZenQuestionPreviewDto> Questions { get; set; } = new();
    }

    public class ZenQuestionPreviewDto
    {
        public int Order { get; set; }
        public string Content { get; set; } = string.Empty;
        public string CanonicalHash { get; set; } = string.Empty;
        public bool IsDuplicate { get; set; }
        public string QuestionType { get; set; } = "SINGLE";
        public List<string> Options { get; set; } = new();
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
    }
}
