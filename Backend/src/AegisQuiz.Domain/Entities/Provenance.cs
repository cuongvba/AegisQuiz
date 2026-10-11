using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// Bản ghi xuất hiện của câu hỏi trong một tài liệu nguồn cụ thể
    /// </summary>
    [NotMapped]
    public class ProvenanceAppearance
    {
        public string SourceFile { get; set; } = string.Empty;
        public string? FileHash { get; set; }
        public string? ExamTitleInFile { get; set; }
        public string? SectionInFile { get; set; }
        public string? OriginalQuestionNo { get; set; }
        public int? RowIndex { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Cụm nhận diện trùng lặp đề thi (Exam Cluster Match)
    /// </summary>
    [NotMapped]
    public class ExamClusterMatch
    {
        public string ExistingTopicCode { get; set; } = string.Empty;
        public string ExistingTopicName { get; set; } = string.Empty;
        public int TotalQuestionsInCluster { get; set; }
        public int MatchedQuestionsCount { get; set; }
        public double JaccardSimilarityScore { get; set; }
        public string CanonicalExamUrn { get; set; } = string.Empty;
        public string Recommendation { get; set; } = "MERGE"; // MERGE | FORK | SKIP
    }
}
