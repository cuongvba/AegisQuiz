-- ============================================================
-- Migration: Add UserRoles Table (RBAC)
-- Version : 20260910_AddRBAC
-- Author  : Systems Architect — AegisQuiz Platform
-- ============================================================

-- ── 1. Bảng UserRoles ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "UserRoles" (
    "Id"          UUID         NOT NULL DEFAULT gen_random_uuid(),
    "UserId"      TEXT         NOT NULL,          -- Keycloak sub / PKI employeeId
    "TenantId"    UUID         NOT NULL,
    "Role"        TEXT         NOT NULL,          -- AppRoles.* constants
    "OrgUnitId"   UUID         NULL,              -- scope cho TeamLeader
    "IsActive"    BOOLEAN      NOT NULL DEFAULT TRUE,
    "AssignedAt"  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "AssignedBy"  TEXT         NULL,
    "ExpiresAt"   TIMESTAMPTZ  NULL,

    CONSTRAINT "PK_UserRoles" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserRoles_Tenants"
        FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserRoles_OrgUnits"
        FOREIGN KEY ("OrgUnitId") REFERENCES "OrganizationUnits"("Id") ON DELETE SET NULL,
    CONSTRAINT "CK_UserRoles_Role"
        CHECK ("Role" IN (
            'SystemAdmin', 'TenantAdmin', 'ContentManager',
            'Instructor', 'OrgUnitManager', 'TeamLeader',
            'Learner', 'GuestViewer'
        ))
);

-- ── 2. Indexes ───────────────────────────────────────────────────────────────
-- Tra cứu role của một user trong tenant (dùng nhiều nhất)
CREATE INDEX IF NOT EXISTS "IX_UserRoles_UserId_TenantId"
    ON "UserRoles" ("UserId", "TenantId")
    WHERE "IsActive" = TRUE;

-- TeamLeader cần tra cứu theo OrgUnit scope
CREATE INDEX IF NOT EXISTS "IX_UserRoles_OrgUnitId"
    ON "UserRoles" ("OrgUnitId")
    WHERE "OrgUnitId" IS NOT NULL AND "IsActive" = TRUE;

-- Admin: liệt kê toàn bộ user của tenant theo role
CREATE INDEX IF NOT EXISTS "IX_UserRoles_TenantId_Role"
    ON "UserRoles" ("TenantId", "Role")
    WHERE "IsActive" = TRUE;

-- ── 3. Seed: Default SystemAdmin role cho account bootstrap ──────────────────
-- NOTE: Thay UserId bằng Keycloak sub thực tế khi deploy production
INSERT INTO "UserRoles" ("Id", "UserId", "TenantId", "Role", "AssignedBy")
SELECT
    gen_random_uuid(),
    'a55850fa-1188-4f24-81e5-827c84a860b2',   -- sandbox admin userId
    t."Id",
    'TenantAdmin',
    'SYSTEM_SEED'
FROM "Tenants" t
WHERE t."Code" = 'default'
ON CONFLICT DO NOTHING;

-- ── 4. Comment documentation ─────────────────────────────────────────────────
COMMENT ON TABLE "UserRoles"            IS 'RBAC: User ↔ Role ↔ Tenant mapping với OrgUnit scope';
COMMENT ON COLUMN "UserRoles"."Role"    IS 'AppRoles constant: SystemAdmin|TenantAdmin|ContentManager|Instructor|OrgUnitManager|TeamLeader|Learner|GuestViewer';
COMMENT ON COLUMN "UserRoles"."OrgUnitId" IS 'Scope cho TeamLeader: chỉ quản lý learner thuộc OrgUnit này. NULL = toàn tenant.';
COMMENT ON COLUMN "UserRoles"."ExpiresAt" IS 'Hết hạn role tự động (NULL = vĩnh viễn). Dùng cho role tạm thời (e.g. Instructor contract).';
