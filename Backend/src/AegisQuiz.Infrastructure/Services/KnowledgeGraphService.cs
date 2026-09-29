using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Epoch 5: CognitiveOS] Triển khai Đồ thị Tri thức Nhận thức và thuật toán Truy vết Lỗ hổng Gốc (Root-Cause Gap Traversal).
    /// </summary>
    public class KnowledgeGraphService : IKnowledgeGraphService
    {
        private readonly AegisQuizDbContext _db;
        private readonly IDktEngine _dktEngine;
        private readonly ILogger<KnowledgeGraphService> _logger;
        private static bool _seeded = false;

        public KnowledgeGraphService(
            AegisQuizDbContext db,
            IDktEngine dktEngine,
            ILogger<KnowledgeGraphService> logger)
        {
            _db = db;
            _dktEngine = dktEngine;
            _logger = logger;
        }

        public async Task<List<KnowledgeGraphNodeDto>> GetDomainConceptsAsync(
            string domainCode = "EDUCATION",
            Guid? userId = null,
            CancellationToken ct = default)
        {
            await EnsureSeedConceptsAsync(ct);

            var concepts = await _db.KnowledgeConcepts
                .AsNoTracking()
                .Where(c => c.DomainCode == domainCode)
                .ToListAsync(ct);

            var relationships = await _db.ConceptRelationships
                .AsNoTracking()
                .ToListAsync(ct);

            DktStateDto? dktState = null;
            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                dktState = await _dktEngine.GetStudentKnowledgeStateAsync(userId.Value, ct);
            }

            var result = new List<KnowledgeGraphNodeDto>();
            foreach (var c in concepts)
            {
                var prereqs = relationships
                    .Where(r => r.TargetConceptCode == c.ConceptCode && r.RelationshipType == "PREREQUISITE")
                    .Select(r => r.SourceConceptCode)
                    .ToList();

                var subConcepts = relationships
                    .Where(r => r.SourceConceptCode == c.ConceptCode && r.RelationshipType == "SUBCONCEPT")
                    .Select(r => r.TargetConceptCode)
                    .ToList();

                double mastery = 0.50;
                if (dktState != null && dktState.ConceptMasteryMap.TryGetValue(c.ConceptCode, out var val))
                {
                    mastery = val;
                }

                result.Add(new KnowledgeGraphNodeDto
                {
                    ConceptCode = c.ConceptCode,
                    Name = c.Name,
                    DomainCode = c.DomainCode,
                    DifficultyRank = c.DifficultyRank,
                    StudentMasteryPercent = Math.Round(mastery * 100, 1),
                    CurriculumReference = c.CurriculumReference,
                    Prerequisites = prereqs,
                    SubConcepts = subConcepts
                });
            }

            return result;
        }

        public async Task<List<string>> TraceRootCauseGapsAsync(
            string failedConceptCode,
            Guid userId,
            CancellationToken ct = default)
        {
            await EnsureSeedConceptsAsync(ct);

            var dktState = await _dktEngine.GetStudentKnowledgeStateAsync(userId, ct);
            var rootGaps = new List<string>();
            var visited = new HashSet<string>();

            async Task TraverseAsync(string currentCode)
            {
                if (visited.Contains(currentCode)) return;
                visited.Add(currentCode);

                // Tìm các khái niệm tiên quyết (Prerequisites)
                var prereqs = await _db.ConceptRelationships
                    .AsNoTracking()
                    .Where(r => r.TargetConceptCode == currentCode && r.RelationshipType == "PREREQUISITE")
                    .Select(r => r.SourceConceptCode)
                    .ToListAsync(ct);

                if (prereqs.Count == 0)
                {
                    // Đã tới gốc rễ của tri thức
                    if (dktState.ConceptMasteryMap.GetValueOrDefault(currentCode, 0.5) < 0.65)
                    {
                        rootGaps.Add(currentCode);
                    }
                    return;
                }

                bool foundDeficitInPrereq = false;
                foreach (var p in prereqs)
                {
                    double pMastery = dktState.ConceptMasteryMap.GetValueOrDefault(p, 0.5);
                    if (pMastery < 0.65)
                    {
                        foundDeficitInPrereq = true;
                        await TraverseAsync(p);
                    }
                }

                if (!foundDeficitInPrereq)
                {
                    rootGaps.Add(currentCode);
                }
            }

            await TraverseAsync(failedConceptCode);
            _logger.LogInformation("[KnowledgeGraph] Truy vết lỗ hổng gốc cho User {User}, Concept {Concept}: Tìm ra {Count} điểm nghẽn",
                userId, failedConceptCode, rootGaps.Count);

            return rootGaps.Distinct().ToList();
        }

        public async Task<List<KnowledgeGraphNodeDto>> GetRemediationLearningPathAsync(
            string targetConceptCode,
            Guid userId,
            CancellationToken ct = default)
        {
            var rootGaps = await TraceRootCauseGapsAsync(targetConceptCode, userId, ct);
            var allConcepts = await GetDomainConceptsAsync("EDUCATION", userId, ct);

            // Sắp xếp thứ tự học từ lỗ hổng nền tảng sâu nhất đến khái niệm mục tiêu
            var path = new List<KnowledgeGraphNodeDto>();
            foreach (var gapCode in rootGaps)
            {
                var node = allConcepts.FirstOrDefault(c => c.ConceptCode == gapCode);
                if (node != null && !path.Any(p => p.ConceptCode == node.ConceptCode))
                {
                    path.Add(node);
                }
            }

            var targetNode = allConcepts.FirstOrDefault(c => c.ConceptCode == targetConceptCode);
            if (targetNode != null && !path.Any(p => p.ConceptCode == targetNode.ConceptCode))
            {
                path.Add(targetNode);
            }

            return path;
        }

        private async Task EnsureSeedConceptsAsync(CancellationToken ct)
        {
            if (_seeded) return;

            try
            {
                var sql = @"
                    CREATE TABLE IF NOT EXISTS ""KnowledgeConcepts"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""TenantId"" uuid NOT NULL,
                        ""ConceptCode"" text NOT NULL,
                        ""Name"" text NOT NULL,
                        ""DomainCode"" text NOT NULL DEFAULT 'EDUCATION',
                        ""DifficultyRank"" integer NOT NULL DEFAULT 3,
                        ""Description"" text NOT NULL,
                        ""CurriculumReference"" text,
                        ""PrerequisiteCodes"" text[],
                        ""CreatedAt"" timestamp with time zone NOT NULL
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS ""IX_KnowledgeConcepts_ConceptCode"" ON ""KnowledgeConcepts"" (""ConceptCode"");

                    CREATE TABLE IF NOT EXISTS ""ConceptRelationships"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""TenantId"" uuid NOT NULL,
                        ""SourceConceptCode"" text NOT NULL,
                        ""TargetConceptCode"" text NOT NULL,
                        ""RelationshipType"" text NOT NULL DEFAULT 'PREREQUISITE',
                        ""Weight"" double precision NOT NULL DEFAULT 1.0,
                        ""CreatedAt"" timestamp with time zone NOT NULL
                    );
                ";
                await _db.Database.ExecuteSqlRawAsync(sql, ct);

                if (!await _db.KnowledgeConcepts.AnyAsync(ct))
                {
                    var seedConcepts = new List<KnowledgeConcept>
                    {
                        new() { Id = Guid.NewGuid(), TenantId = Tenant.CommunityTenantId, ConceptCode = "MATH_CALC_DERIVATIVE_BASIC", Name = "Đạo Hàm Cơ Bản & Bảng Đạo Hàm", DomainCode = "EDUCATION", DifficultyRank = 2, Description = "Quy tắc tính đạo hàm đa thức, lượng giác", CurriculumReference = "GDPT 2018 - Toán 11 Tập 2 - Trang 120" },
                        new() { Id = Guid.NewGuid(), TenantId = Tenant.CommunityTenantId, ConceptCode = "MATH_CALC_DERIVATIVE_CHAIN_RULE", Name = "Đạo Hàm Của Hàm Hợp", DomainCode = "EDUCATION", DifficultyRank = 3, Description = "Công thức đạo hàm hàm hợp y' = u' * f'(u)", CurriculumReference = "GDPT 2018 - Toán 11 Tập 2 - Trang 125" },
                        new() { Id = Guid.NewGuid(), TenantId = Tenant.CommunityTenantId, ConceptCode = "MATH_CALC_INTEGRAL", Name = "Tích Phân & Ứng Dụng Hình Học", DomainCode = "EDUCATION", DifficultyRank = 4, Description = "Định lý Newton-Leibniz, diện tích hình phẳng", CurriculumReference = "GDPT 2018 - Toán 12 Tập 2 - Trang 10" },
                        new() { Id = Guid.NewGuid(), TenantId = Tenant.CommunityTenantId, ConceptCode = "MATH_CALC_INTEGRAL_BY_PARTS", Name = "Tích Phân Từng Phần", DomainCode = "EDUCATION", DifficultyRank = 5, Description = "Phương pháp tích phân từng phần: int(u dv) = uv - int(v du)", CurriculumReference = "GDPT 2018 - Toán 12 Tập 2 - Trang 18" },
                        new() { Id = Guid.NewGuid(), TenantId = Tenant.CommunityTenantId, ConceptCode = "MATH_GEOM_OXYZ_VECTOR", Name = "Tọa Độ Không Gian & Tích Vô Hướng", DomainCode = "EDUCATION", DifficultyRank = 3, Description = "Hệ tọa độ Oxyz, độ dài vectơ, góc giữa hai vectơ", CurriculumReference = "GDPT 2018 - Toán 12 Tập 1 - Trang 65" }
                    };
                    _db.KnowledgeConcepts.AddRange(seedConcepts);

                    var seedRels = new List<ConceptRelationship>
                    {
                        new() { Id = Guid.NewGuid(), TenantId = Tenant.CommunityTenantId, SourceConceptCode = "MATH_CALC_DERIVATIVE_BASIC", TargetConceptCode = "MATH_CALC_DERIVATIVE_CHAIN_RULE", RelationshipType = "PREREQUISITE" },
                        new() { Id = Guid.NewGuid(), TenantId = Tenant.CommunityTenantId, SourceConceptCode = "MATH_CALC_DERIVATIVE_CHAIN_RULE", TargetConceptCode = "MATH_CALC_INTEGRAL", RelationshipType = "PREREQUISITE" },
                        new() { Id = Guid.NewGuid(), TenantId = Tenant.CommunityTenantId, SourceConceptCode = "MATH_CALC_INTEGRAL", TargetConceptCode = "MATH_CALC_INTEGRAL_BY_PARTS", RelationshipType = "PREREQUISITE" }
                    };
                    _db.ConceptRelationships.AddRange(seedRels);

                    await _db.SaveChangesAsync(ct);
                }

                _seeded = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[KnowledgeGraph] Seed check skipped.");
                _seeded = true;
            }
        }
    }
}
