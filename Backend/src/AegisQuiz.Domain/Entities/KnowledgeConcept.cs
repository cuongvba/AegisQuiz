using System;
using System.Collections.Generic;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [Epoch 5: CognitiveOS] Nút khái niệm trong Đồ thị Tri thức Nhận thức (Metacognitive Knowledge Graph Node).
    /// </summary>
    public class KnowledgeConcept : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string ConceptCode { get; set; } = string.Empty; // e.g. "MATH_INTEGRAL_BY_PARTS", "BANK_RISK_BASEL_III"
        public string Name { get; set; } = string.Empty;
        public string DomainCode { get; set; } = "EDUCATION";
        public int DifficultyRank { get; set; } = 3;
        public string Description { get; set; } = string.Empty;
        public string? CurriculumReference { get; set; } // e.g. "GDPT 2018 - Toán 12 Tập 2 - Trang 14"
        public List<string> PrerequisiteCodes { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Mối quan hệ liên kết cạnh (Graph Edge) giữa 2 khái niệm tri thức.
    /// </summary>
    public class ConceptRelationship : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string SourceConceptCode { get; set; } = string.Empty;
        public string TargetConceptCode { get; set; } = string.Empty;
        
        /// <summary>
        /// PREREQUISITE (Tiên quyết), COREQUISITE (Song hành), SUBCONCEPT (Khái niệm con), ANALOGY (Tương đồng)
        /// </summary>
        public string RelationshipType { get; set; } = "PREREQUISITE";

        /// <summary>Trọng số ảnh hưởng (0.0 đến 1.0)</summary>
        public double Weight { get; set; } = 1.0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
