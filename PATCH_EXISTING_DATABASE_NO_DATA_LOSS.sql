-- ==============================================================================
-- AegisQuiz — HOTFIX & CẬP NHẬT DATABASE KHÔNG MẤT DỮ LIỆU (Non-Destructive)
-- Khắc phục tức thì lỗi: 42703: column b.DepthLevel does not exist
-- Chạy trực tiếp tại PostgreSQL Query Tool (pgAdmin / DBeaver / Coolify psql)
-- ==============================================================================

START TRANSACTION;

-- 1. BẢNG BankTopics: BỔ SUNG ĐẦY ĐỦ CÁC CỘT HIERARCHY MỚI NHẤT
ALTER TABLE "BankTopics" ADD COLUMN IF NOT EXISTS "DepthLevel" integer NOT NULL DEFAULT 0;
ALTER TABLE "BankTopics" ADD COLUMN IF NOT EXISTS "MaterializedPath" text NULL;
ALTER TABLE "BankTopics" ADD COLUMN IF NOT EXISTS "Scope" text NOT NULL DEFAULT 'COMMUNITY';
ALTER TABLE "BankTopics" ADD COLUMN IF NOT EXISTS "DomainCode" text NOT NULL DEFAULT 'GENERAL';
ALTER TABLE "BankTopics" ADD COLUMN IF NOT EXISTS "DisplayOrder" integer NOT NULL DEFAULT 0;
ALTER TABLE "BankTopics" ADD COLUMN IF NOT EXISTS "QuestionCountCached" integer NOT NULL DEFAULT 0;
ALTER TABLE "BankTopics" ADD COLUMN IF NOT EXISTS "Urn" text NULL;
ALTER TABLE "BankTopics" ADD COLUMN IF NOT EXISTS "ParentId" uuid NULL;
ALTER TABLE "BankTopics" ADD COLUMN IF NOT EXISTS "AllowedTenantIds" text[] NOT NULL DEFAULT '{}';
ALTER TABLE "BankTopics" ADD COLUMN IF NOT EXISTS "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

CREATE INDEX IF NOT EXISTS "IX_BankTopics_DomainCode" ON "BankTopics" ("DomainCode");
CREATE INDEX IF NOT EXISTS "IX_BankTopics_MaterializedPath" ON "BankTopics" ("MaterializedPath");
CREATE INDEX IF NOT EXISTS "IX_BankTopics_Scope" ON "BankTopics" ("Scope");
CREATE INDEX IF NOT EXISTS "IX_BankTopics_TenantId" ON "BankTopics" ("TenantId");
CREATE INDEX IF NOT EXISTS "IX_BankTopics_ParentId" ON "BankTopics" ("ParentId");

-- 2. BẢNG Questions: BỔ SUNG CÁC CỘT TOẠ ĐỘ TRI THỨC VÀ AI
ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "ContextId" uuid NULL;
ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "DomainCode" text NOT NULL DEFAULT 'EDUCATION';
ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "IsCritical" boolean NOT NULL DEFAULT false;
ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "SubCategory" text NULL;
ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "Tags" text[] NOT NULL DEFAULT '{}';
ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "TargetLevel" text NULL;
ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "AssessmentPurpose" text NULL;
ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "IssuingOrg" text NULL;
ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "BenchmarkYear" integer NULL;
ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "BenchmarkStandard" text NULL;
ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "RubricJson" text NULL;
ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

CREATE INDEX IF NOT EXISTS "IX_Questions_DomainCode" ON "Questions" ("DomainCode");
CREATE INDEX IF NOT EXISTS "IX_Questions_ContextId" ON "Questions" ("ContextId");
CREATE INDEX IF NOT EXISTS "IX_Questions_TenantId" ON "Questions" ("TenantId");

-- 3. TẠO BẢNG DynamicDomains (NẾU CHƯA CÓ)
CREATE TABLE IF NOT EXISTS "DynamicDomains" (
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "Icon" text NOT NULL DEFAULT 'Layers',
    "ColorBadge" text NOT NULL DEFAULT '#0284c7',
    "IsSystemStandard" boolean NOT NULL DEFAULT true,
    "TenantId" uuid NULL,
    "ParentDomainCode" text NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "DisplayOrder" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_DynamicDomains" PRIMARY KEY ("Code")
);

-- 4. TẠO BẢNG OrganizationUnits (NẾU CHƯA CÓ)
CREATE TABLE IF NOT EXISTS "OrganizationUnits" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "ParentId" uuid NULL,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "UnitType" integer NOT NULL DEFAULT 0,
    "HierarchyPath" text NOT NULL DEFAULT '/',
    "Email" text,
    "PhoneNumber" text,
    "Address" text,
    "IsActive" boolean NOT NULL DEFAULT true,
    "DisplayOrder" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_OrganizationUnits" PRIMARY KEY ("Id")
);

-- 5. TẠO BẢNG Tenants (NẾU CHƯA CÓ)
CREATE TABLE IF NOT EXISTS "Tenants" (
    "Id" uuid NOT NULL,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "LogoUrl" text,
    "PrimaryColor" text,
    "Domain" text,
    "IsActive" boolean NOT NULL DEFAULT true,
    "Plan" integer NOT NULL DEFAULT 0,
    "ScaleType" integer NOT NULL DEFAULT 0,
    "MaxUsers" integer NOT NULL DEFAULT 100,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "ExpiresAt" timestamp with time zone,
    "KeycloakRealmId" text,
    "ConfigJson" text,
    "FeatureFlagsJson" text,
    "CustomDomain" text,
    CONSTRAINT "PK_Tenants" PRIMARY KEY ("Id")
);

-- 6. TẠO BẢNG TenantDomainConfigs (NẾU CHƯA CÓ)
CREATE TABLE IF NOT EXISTS "TenantDomainConfigs" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "DomainCode" text NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CustomLabel" text,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_TenantDomainConfigs" PRIMARY KEY ("Id")
);

-- 7. TẠO BẢNG TenantCoordinatePresets (NẾU CHƯA CÓ)
CREATE TABLE IF NOT EXISTS "TenantCoordinatePresets" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "DomainCode" text NOT NULL,
    "CoordinateType" text NOT NULL,
    "PresetCode" text NOT NULL,
    "PresetLabel" text NOT NULL,
    "SynonymsJson" text,
    "IsDefault" boolean NOT NULL DEFAULT false,
    "DisplayOrder" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_TenantCoordinatePresets" PRIMARY KEY ("Id")
);

-- 8. TẠO BẢNG UserRoles (NẾU CHƯA CÓ)
CREATE TABLE IF NOT EXISTS "UserRoles" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Role" integer NOT NULL,
    "OrgUnitId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "ExpiresAt" timestamp with time zone,
    CONSTRAINT "PK_UserRoles" PRIMARY KEY ("Id")
);

-- 9. NẠP CÁC NGÀNH TIÊU CHUẨN
INSERT INTO "DynamicDomains" ("Code", "ColorBadge", "CreatedAt", "Description", "DisplayOrder", "Icon", "IsActive", "IsSystemStandard", "Name", "ParentDomainCode", "TenantId")
VALUES 
  ('EDUCATION', '#2563eb', now(), 'Khảo thí đại học, phổ thông, học thuật tổng quát', 1, 'GraduationCap', TRUE, TRUE, 'Giáo dục & Học thuật', NULL, NULL),
  ('BANKING', '#059669', now(), 'Nghiệp vụ tín dụng, thanh toán, ngân quỹ, quản trị rủi ro', 2, 'Landmark', TRUE, TRUE, 'Tài chính - Ngân hàng', NULL, NULL),
  ('HEALTHCARE', '#dc2626', now(), 'Y học, dược lâm sàng, quy trình điều dưỡng, kiểm soát nhiễm khuẩn', 3, 'HeartPulse', TRUE, TRUE, 'Y tế - Sức khỏe', NULL, NULL),
  ('HSE', '#d97706', now(), 'An toàn vệ sinh lao động, PCCC, quy chuẩn ISO 45001', 4, 'HardHat', TRUE, TRUE, 'An toàn - Môi trường LĐ', NULL, NULL),
  ('GOV_DRIVING', '#7c3aed', now(), 'Bộ 600 câu GPLX Bộ GTVT, 60 câu điểm liệt, sa hình AI', 5, 'Car', TRUE, TRUE, 'Sát hạch Lái xe Quốc gia', NULL, NULL),
  ('IT_SECURITY', '#0284c7', now(), 'Bảo mật an ninh mạng, kiến trúc hệ thống, chứng chỉ CISSP/CompTIA', 6, 'ShieldCheck', TRUE, TRUE, 'An toàn TT & CNTT', NULL, NULL),
  ('GENERAL', '#4b5563', now(), 'Kiến thức đại cương, kỹ năng mềm, văn hóa doanh nghiệp', 7, 'Layers', TRUE, TRUE, 'Tổng hợp / Đại cương', NULL, NULL)
ON CONFLICT ("Code") DO NOTHING;

-- 10. ĐÁNH DẤU MIGRATION ĐÃ ÁP DỤNG TRONG __EFMigrationsHistory
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES
  ('20260702105341_InitialCreate', '10.0.5'),
  ('20260703081819_AddTopicHierarchy', '10.0.5'),
  ('20260704025931_MoveOptionsToQuestionBase', '10.0.5'),
  ('20260704103945_AddQuestionMediaTypes', '10.0.5'),
  ('20260828042215_AddTenantIdAndAuditTrail', '10.0.5'),
  ('20260905091540_AddQuestionContextAndMetadata', '10.0.5'),
  ('20260912112239_AddHierarchyAndDynamicDomains', '10.0.5')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

SELECT 'AegisQuiz 2026: ĐÃ CẬP NHẬT TẤT CẢ CÁC CỘT & BẢNG THÀNH CÔNG! KHÔNG MẤT DỮ LIỆU!' AS Result;
