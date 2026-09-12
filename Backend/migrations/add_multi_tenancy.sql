-- ============================================================
-- [Phase B] Multi-Tenancy Migration
-- Thêm TenantId vào tất cả bảng + Row-Level Security (PostgreSQL)
-- Chạy: psql -d aegisquiz -f add_multi_tenancy.sql
-- ============================================================

-- Bước 1: Tạo bảng Tenants
CREATE TABLE IF NOT EXISTS "Tenants" (
    "Id"               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "Code"             VARCHAR(50) NOT NULL UNIQUE,
    "Name"             VARCHAR(200) NOT NULL,
    "LogoUrl"          TEXT,
    "PrimaryColor"     VARCHAR(20) DEFAULT '#1a73e8',
    "Domain"           VARCHAR(100),
    "IsActive"         BOOLEAN NOT NULL DEFAULT TRUE,
    "Plan"             INTEGER NOT NULL DEFAULT 2,  -- Professional
    "MaxUsers"         INTEGER NOT NULL DEFAULT 1000,
    "KeycloakRealmId"  VARCHAR(100),
    "ConfigJson"       JSONB,
    "CreatedAt"        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ExpiresAt"        TIMESTAMPTZ
);

-- Seed: Tenant mặc định (Agribank pilot)
INSERT INTO "Tenants" ("Id", "Code", "Name", "Plan", "MaxUsers")
VALUES
    ('00000000-0000-0000-0000-000000000001', 'agribank', 'Ngân hàng Nông nghiệp Agribank', 3, 5000),
    ('00000000-0000-0000-0000-000000000002', 'system',   'AegisQuiz System Admin',          4, 999999)
ON CONFLICT ("Code") DO NOTHING;

-- Bước 2: Thêm TenantId vào các bảng core
-- Default: Agribank tenant (để không break data hiện có)
ALTER TABLE "Questions"
    ADD COLUMN IF NOT EXISTS "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

ALTER TABLE "QuestionAttempts"
    ADD COLUMN IF NOT EXISTS "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

ALTER TABLE "QuestionAttemptAnswers"
    ADD COLUMN IF NOT EXISTS "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

ALTER TABLE "BankTopics"
    ADD COLUMN IF NOT EXISTS "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

ALTER TABLE "ExamSessions"
    ADD COLUMN IF NOT EXISTS "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

ALTER TABLE "Notebooks"
    ADD COLUMN IF NOT EXISTS "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

ALTER TABLE "NotebookChapters"
    ADD COLUMN IF NOT EXISTS "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

ALTER TABLE "LearningPaths"
    ADD COLUMN IF NOT EXISTS "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

ALTER TABLE "LearningPathProgresses"
    ADD COLUMN IF NOT EXISTS "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

ALTER TABLE "LearnerLeaderboardEntries"
    ADD COLUMN IF NOT EXISTS "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

-- Bước 3: Indexes cho TenantId (query performance)
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Questions_TenantId"
    ON "Questions" ("TenantId");

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_QuestionAttempts_TenantId_UserId"
    ON "QuestionAttempts" ("TenantId", "UserId");

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_BankTopics_TenantId"
    ON "BankTopics" ("TenantId");

CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_ExamSessions_TenantId_ApplicantId"
    ON "ExamSessions" ("TenantId", "ApplicantId");

-- Bước 4: Enable Row Level Security
ALTER TABLE "Questions"                ENABLE ROW LEVEL SECURITY;
ALTER TABLE "QuestionAttempts"         ENABLE ROW LEVEL SECURITY;
ALTER TABLE "BankTopics"               ENABLE ROW LEVEL SECURITY;
ALTER TABLE "ExamSessions"             ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Notebooks"                ENABLE ROW LEVEL SECURITY;
ALTER TABLE "LearningPaths"            ENABLE ROW LEVEL SECURITY;

-- Bước 5: Tạo RLS Policies
-- Mỗi tenant chỉ thấy data của mình (set qua session variable app.tenant_id)

CREATE POLICY IF NOT EXISTS tenant_isolation_questions ON "Questions"
    USING ("TenantId" = current_setting('app.tenant_id', TRUE)::UUID
        OR current_setting('app.is_system_admin', TRUE) = 'true');

CREATE POLICY IF NOT EXISTS tenant_isolation_attempts ON "QuestionAttempts"
    USING ("TenantId" = current_setting('app.tenant_id', TRUE)::UUID
        OR current_setting('app.is_system_admin', TRUE) = 'true');

CREATE POLICY IF NOT EXISTS tenant_isolation_topics ON "BankTopics"
    USING ("TenantId" = current_setting('app.tenant_id', TRUE)::UUID
        OR current_setting('app.is_system_admin', TRUE) = 'true'
        OR "VisibilityScope" = 'PUBLIC');

CREATE POLICY IF NOT EXISTS tenant_isolation_sessions ON "ExamSessions"
    USING ("TenantId" = current_setting('app.tenant_id', TRUE)::UUID
        OR current_setting('app.is_system_admin', TRUE) = 'true');

CREATE POLICY IF NOT EXISTS tenant_isolation_notebooks ON "Notebooks"
    USING ("TenantId" = current_setting('app.tenant_id', TRUE)::UUID
        OR current_setting('app.is_system_admin', TRUE) = 'true');

CREATE POLICY IF NOT EXISTS tenant_isolation_paths ON "LearningPaths"
    USING ("TenantId" = current_setting('app.tenant_id', TRUE)::UUID
        OR current_setting('app.is_system_admin', TRUE) = 'true'
        OR "IsPublic" = TRUE);

-- Bước 6: Helper function để set tenant context
CREATE OR REPLACE FUNCTION set_tenant_context(tenant_id UUID, is_admin BOOLEAN DEFAULT FALSE)
RETURNS void AS $$
BEGIN
    PERFORM set_config('app.tenant_id', tenant_id::TEXT, TRUE);
    PERFORM set_config('app.is_system_admin', is_admin::TEXT, TRUE);
END;
$$ LANGUAGE plpgsql;

-- Sử dụng: SELECT set_tenant_context('agribank-uuid', false);

-- ── Verification ────────────────────────────────────────────────────────────
SELECT
    tablename,
    rowsecurity
FROM pg_tables
WHERE schemaname = 'public'
  AND tablename IN ('Questions', 'QuestionAttempts', 'BankTopics', 'ExamSessions')
ORDER BY tablename;

-- Expected output: rowsecurity = true cho tất cả 4 bảng
