using Microsoft.EntityFrameworkCore;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Data
{
    // [World-Class Upgrade] DbContext nâng cấp theo quiz-service
    public class AegisQuizDbContext : DbContext
    {
        private readonly ITenantContext? _tenantContext;

        public AegisQuizDbContext(
            DbContextOptions<AegisQuizDbContext> options,
            ITenantContext? tenantContext = null)
            : base(options)
        {
            _tenantContext = tenantContext;
        }

        // ── Core Quiz Engine ───────────────────────────────────────────────────
        public DbSet<QuestionBase> Questions { get; set; } = null!;
        public DbSet<QuestionContext> QuestionContexts { get; set; } = null!;
        public DbSet<QuestionAttempt> QuestionAttempts { get; set; } = null!;
        public DbSet<QuestionAttemptAnswer> QuestionAttemptAnswers { get; set; } = null!;

        // ── Multi-Tenancy, Hierarchy & Dynamic Taxonomy ──────────────
        public DbSet<Tenant> Tenants { get; set; } = null!;
        public DbSet<OrganizationUnit> OrganizationUnits { get; set; } = null!;
        public DbSet<DynamicDomain> DynamicDomains { get; set; } = null!;
        public DbSet<TenantDomainConfig> TenantDomainConfigs { get; set; } = null!;
        public DbSet<TenantCoordinatePreset> TenantCoordinatePresets { get; set; } = null!;

        // ── RBAC & Team Management ───────────────────────────────────────────
        public DbSet<UserRole> UserRoles { get; set; } = null!;

        // ── Payment ───────────────────────────────────────────────────────────
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;

        // ── AegisNotebook ─────────────────────────────────────────────────────
        public DbSet<Notebook> Notebooks { get; set; } = null!;
        public DbSet<NotebookChapter> NotebookChapters { get; set; } = null!;

        // ── Question Bank ─────────────────────────────────────────────────────
        public DbSet<BankTopic> BankTopics { get; set; } = null!;

        // ── Exam Session (Proctoring) ──────────────────────────────────────────
        public DbSet<ExamSession> ExamSessions { get; set; } = null!;

        // ── Gamification ──────────────────────────────────────────────────────
        public DbSet<LearnerLeaderboardEntry> LeaderboardEntries { get; set; } = null!;
        public DbSet<LearningAchievement> LearningAchievements { get; set; } = null!;
        public DbSet<PersonAchievement> PersonAchievements { get; set; } = null!;

        // ── Learning Path ─────────────────────────────────────────────────────
        public DbSet<LearningPath> LearningPaths { get; set; } = null!;
        public DbSet<LearningPathStep> LearningPathSteps { get; set; } = null!;
        public DbSet<LearningPathProgress> LearningPathProgresses { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // [Phase B — B3] Global Query Filters — Tự động cô lập dữ liệu theo Tenant
            var tenantId = _tenantContext?.CurrentTenantId ?? Guid.Empty;
            var isAdmin  = _tenantContext?.IsSystemAdmin  ?? true; // Nếu không có context (seed/migration) → không filter

            modelBuilder.Entity<OrganizationUnit>()
                .HasQueryFilter(o =>
                    isAdmin ||
                    o.TenantId == Guid.Empty ||
                    o.TenantId == tenantId);

            modelBuilder.Entity<TenantDomainConfig>()
                .HasQueryFilter(c =>
                    isAdmin ||
                    c.TenantId == Guid.Empty ||
                    c.TenantId == tenantId);

            modelBuilder.Entity<TenantCoordinatePreset>()
                .HasQueryFilter(p =>
                    isAdmin ||
                    p.TenantId == Guid.Empty ||
                    p.TenantId == tenantId);

            modelBuilder.Entity<QuestionBase>()
                .HasQueryFilter(q =>
                    isAdmin ||
                    q.TenantId == Guid.Empty ||
                    q.TenantId == tenantId);

            modelBuilder.Entity<QuestionContext>()
                .HasQueryFilter(c =>
                    isAdmin ||
                    c.TenantId == Guid.Empty ||
                    c.TenantId == tenantId);

            modelBuilder.Entity<BankTopic>()
                .HasQueryFilter(t =>
                    isAdmin ||
                    t.TenantId == Guid.Empty ||
                    t.TenantId == tenantId);

            // ── Questions (TPH) + JSONB Payload ──────────────────────────────
            modelBuilder.Entity<QuestionBase>()
                .HasDiscriminator(q => q.QuestionType)
                .HasValue<SingleChoiceQuestion>("SINGLE")
                .HasValue<EssayQuestion>("ESSAY")
                .HasValue<MultiChoiceQuestion>("MULTI")
                .HasValue<TrueFalseQuestion>("TRUE_FALSE")
                .HasValue<ShortAnswerQuestion>("SHORT_ANSWER")
                .HasValue<FillBlankQuestion>("FILL_BLANK")
                .HasValue<OrderingQuestion>("ORDERING")
                .HasValue<MatchingQuestion>("MATCHING");

            modelBuilder.Entity<QuestionBase>(entity =>
            {
                if (Database.IsNpgsql())
                {
                    entity.Property(q => q.Payload).HasColumnType("jsonb");
                    entity.Property(q => q.CorrectOption).HasColumnType("text");
                    entity.PrimitiveCollection(q => q.Options).HasColumnType("text[]");
                    entity.PrimitiveCollection(q => q.Tags).HasColumnType("text[]");
                    entity.Property(q => q.ContentType).HasColumnType("text").HasDefaultValue("text");
                    entity.Property(q => q.OptionType).HasColumnType("text").HasDefaultValue("text");
                    entity.Property(q => q.DomainCode).HasColumnType("text").HasDefaultValue("EDUCATION");
                }
                else
                {
                    entity.Property(q => q.Payload)
                        .HasConversion(
                            v => v == null ? null : v.RootElement.GetRawText(),
                            v => string.IsNullOrEmpty(v) ? null : System.Text.Json.JsonDocument.Parse(v, default));
                    entity.Property(q => q.Options)
                        .HasConversion(
                            v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => string.IsNullOrEmpty(v) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
                    entity.Property(q => q.Tags)
                        .HasConversion(
                            v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                            v => string.IsNullOrEmpty(v) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());
                    entity.Property(q => q.DomainCode).HasDefaultValue("EDUCATION");
                }
                entity.HasIndex(q => q.CategoryCode);
                entity.HasIndex(q => q.DomainCode);
                entity.HasIndex(q => q.ContextId);

                entity.HasOne(q => q.Context)
                      .WithMany(c => c.Questions)
                      .HasForeignKey(q => q.ContextId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ── QuestionContext (Shared Stimulus / Reading Passages) ────────
            modelBuilder.Entity<QuestionContext>(entity =>
            {
                entity.HasIndex(c => c.ContentHash);
                entity.HasIndex(c => c.TenantId);
            });

            // ── QuestionAttempt ────────────────────────────────────────────
            modelBuilder.Entity<QuestionAttempt>(entity =>
            {
                entity.HasIndex(q => new { q.UserId, q.AttemptDate });
                entity.HasIndex(q => q.AttemptDate);
            });

            // ── QuestionAttemptAnswer — per-question tracking ──────────────
            modelBuilder.Entity<QuestionAttemptAnswer>(entity =>
            {
                entity.HasIndex(a => a.AttemptId);
                entity.HasIndex(a => new { a.AttemptId, a.CategoryCode });
                entity.HasIndex(a => a.UserId);
            });

            // ── PaymentTransaction ─────────────────────────────────────────
            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(p => p.PaymentCode)
                .IsUnique();

            // ── BankTopic ─────────────────────────────────────────────────
            modelBuilder.Entity<BankTopic>(entity =>
            {
                entity.HasIndex(t => t.Code).IsUnique();
                entity.HasIndex(t => t.CategoryCode);
                entity.Property(t => t.VisibilityScope).HasDefaultValue("PRIVATE");

                entity.HasOne(t => t.Parent)
                    .WithMany(t => t.Children)
                    .HasForeignKey(t => t.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── ExamSession ────────────────────────────────────────────────
            modelBuilder.Entity<ExamSession>(entity =>
            {
                var finalAnswersComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<Dictionary<Guid, System.Text.Json.JsonElement>>(
                    (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any()),
                    c => c == null ? 0 : c.Aggregate(0, (a, p) => HashCode.Combine(a, p.Key.GetHashCode())),
                    c => c == null ? new Dictionary<Guid, System.Text.Json.JsonElement>() : c.ToDictionary(entry => entry.Key, entry => entry.Value.Clone()));

                var snapshotComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<Guid>?>(
                    (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                    c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c == null ? null : c.ToList());

                entity.Property(e => e.FinalAnswers)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<Guid, System.Text.Json.JsonElement>>(v, (System.Text.Json.JsonSerializerOptions?)null)
                             ?? new Dictionary<Guid, System.Text.Json.JsonElement>())
                    .Metadata.SetValueComparer(finalAnswersComparer);

                entity.Property(e => e.SnapshotQuestionIds)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null))
                    .Metadata.SetValueComparer(snapshotComparer);

                entity.HasIndex(e => e.ApplicantId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.ApplicantId, e.Status });
            });

            // ── Leaderboard ───────────────────────────────────────────────
            modelBuilder.Entity<LearnerLeaderboardEntry>(entity =>
            {
                entity.HasIndex(e => new { e.Period, e.PeriodStart, e.Rank });
                entity.HasIndex(e => new { e.UserId, e.Period, e.PeriodStart }).IsUnique();
            });

            // ── LearningAchievement ────────────────────────────────────────
            modelBuilder.Entity<LearningAchievement>(entity =>
            {
                entity.HasIndex(a => a.Code).IsUnique();
                if (Database.IsNpgsql())
                {
                    entity.Property(a => a.TriggerCondition).HasColumnType("jsonb");
                }
            });

            modelBuilder.Entity<PersonAchievement>(entity =>
            {
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => new { a.UserId, a.AchievementId });
                if (Database.IsNpgsql())
                {
                    entity.Property(a => a.ContextJson).HasColumnType("jsonb");
                }
            });

            // ── LearningPath ──────────────────────────────────────────────
            modelBuilder.Entity<LearningPath>(entity =>
            {
                entity.HasIndex(p => p.Code).IsUnique();
                entity.HasMany(p => p.Steps)
                    .WithOne(s => s.LearningPath)
                    .HasForeignKey(s => s.LearningPathId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LearningPathStep>(entity =>
            {
                entity.HasIndex(s => new { s.LearningPathId, s.StepOrder });
            });

            modelBuilder.Entity<LearningPathProgress>(entity =>
            {
                entity.HasIndex(p => new { p.UserId, p.LearningPathId }).IsUnique();
                entity.HasIndex(p => p.UserId);
            });

            // ── DynamicDomain & Seed Defaults ─────────────────────────────
            modelBuilder.Entity<DynamicDomain>(entity =>
            {
                entity.HasKey(d => d.Code);
                entity.HasIndex(d => d.IsSystemStandard);
                entity.HasIndex(d => d.TenantId);

                entity.HasOne(d => d.ParentDomain)
                    .WithMany(d => d.SubDomains)
                    .HasForeignKey(d => d.ParentDomainCode)
                    .OnDelete(DeleteBehavior.Restrict);

                // Seed 7 standard universal domains
                entity.HasData(
                    new DynamicDomain { Code = "EDUCATION", Name = "Giáo dục & Học thuật", Description = "Khảo thí đại học, phổ thông, học thuật tổng quát", Icon = "GraduationCap", ColorBadge = "#2563eb", IsSystemStandard = true, DisplayOrder = 1 },
                    new DynamicDomain { Code = "BANKING", Name = "Tài chính - Ngân hàng", Description = "Nghiệp vụ tín dụng, thanh toán, ngân quỹ, quản trị rủi ro", Icon = "Landmark", ColorBadge = "#059669", IsSystemStandard = true, DisplayOrder = 2 },
                    new DynamicDomain { Code = "HEALTHCARE", Name = "Y tế - Sức khỏe", Description = "Y học, dược lâm sàng, quy trình điều dưỡng, kiểm soát nhiễm khuẩn", Icon = "HeartPulse", ColorBadge = "#dc2626", IsSystemStandard = true, DisplayOrder = 3 },
                    new DynamicDomain { Code = "HSE", Name = "An toàn - Môi trường LĐ", Description = "An toàn vệ sinh lao động, PCCC, quy chuẩn ISO 45001", Icon = "HardHat", ColorBadge = "#d97706", IsSystemStandard = true, DisplayOrder = 4 },
                    new DynamicDomain { Code = "GOV_DRIVING", Name = "Sát hạch Lái xe Quốc gia", Description = "Bộ 600 câu GPLX Bộ GTVT, 60 câu điểm liệt, sa hình AI", Icon = "Car", ColorBadge = "#7c3aed", IsSystemStandard = true, DisplayOrder = 5 },
                    new DynamicDomain { Code = "IT_SECURITY", Name = "An toàn TT & CNTT", Description = "Bảo mật an ninh mạng, kiến trúc hệ thống, chứng chỉ CISSP/CompTIA", Icon = "ShieldCheck", ColorBadge = "#0284c7", IsSystemStandard = true, DisplayOrder = 6 },
                    new DynamicDomain { Code = "GENERAL", Name = "Tổng hợp / Đại cương", Description = "Kiến thức đại cương, kỹ năng mềm, văn hóa doanh nghiệp", Icon = "Layers", ColorBadge = "#4b5563", IsSystemStandard = true, DisplayOrder = 7 }
                );
            });

            // ── TenantDomainConfig ─────────────────────────────────────────
            modelBuilder.Entity<TenantDomainConfig>(entity =>
            {
                entity.HasIndex(c => c.TenantId);
                entity.HasIndex(c => new { c.TenantId, c.DomainCode }).IsUnique();

                entity.HasOne(c => c.Domain)
                    .WithMany()
                    .HasForeignKey(c => c.DomainCode)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ── TenantCoordinatePreset ─────────────────────────────────────
            modelBuilder.Entity<TenantCoordinatePreset>(entity =>
            {
                entity.HasIndex(p => p.TenantId);
                entity.HasIndex(p => new { p.TenantId, p.DomainCode, p.CoordinateType });
            });

            // ── OrganizationUnit (Self-referencing Tree) ───────────────────
            modelBuilder.Entity<OrganizationUnit>(entity =>
            {
                entity.HasIndex(o => o.TenantId);
                entity.HasIndex(o => o.ParentId);
                entity.HasIndex(o => new { o.TenantId, o.Code }).IsUnique();

                entity.HasOne(o => o.Parent)
                    .WithMany(o => o.Children)
                    .HasForeignKey(o => o.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── BankTopic (Materialized Path & Omni-Century Taxonomy) ──────
            modelBuilder.Entity<BankTopic>(entity =>
            {
                entity.HasIndex(t => t.Code);
                entity.HasIndex(t => t.TenantId);
                entity.HasIndex(t => t.Scope);
                entity.HasIndex(t => t.DomainCode);
                entity.HasIndex(t => t.MaterializedPath);
                entity.HasIndex(t => t.ParentId);

                entity.HasOne(t => t.Parent)
                    .WithMany(t => t.Children)
                    .HasForeignKey(t => t.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ── UserRole (RBAC & Multi-Scope) ──────────────────────────────
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasIndex(r => new { r.TenantId, r.UserId });
                entity.HasIndex(r => new { r.TenantId, r.OrgUnitId });
                entity.HasIndex(r => r.Role);
            });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyTenantAndAudit();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            ApplyTenantAndAudit();
            return base.SaveChanges();
        }

        private void ApplyTenantAndAudit()
        {
            var currentTenantId = _tenantContext?.CurrentTenantId ?? Guid.Empty;

            foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.TenantId == Guid.Empty && currentTenantId != Guid.Empty)
                    {
                        entry.Entity.TenantId = currentTenantId;
                    }
                }
            }
        }
    }
}
