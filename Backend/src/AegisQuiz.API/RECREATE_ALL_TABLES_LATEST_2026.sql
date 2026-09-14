-- ==============================================================================
-- AegisQuiz — RECREATE ALL TABLES (Chuẩn Mới Nhất 2026 - Master DDL)
-- CẢNH BÁO: Script này sẽ DROP TOÀN BỘ BẢNG CŨ và TẠO LẠI TỪ ĐẦU theo chuẩn mới nhất.
-- Áp dụng trực tiếp tại: PostgreSQL Database (dehoc.vn / daotao.dehoc.vn)
-- ==============================================================================

-- BƯỚC 1: XÓA TOÀN BỘ CÁC BẢNG CŨ (NẾU TỒN TẠI) THEO THỨ TỰ CASCADE AN TOÀN
DROP TABLE IF EXISTS "UserRoles" CASCADE;
DROP TABLE IF EXISTS "TenantDomainConfigs" CASCADE;
DROP TABLE IF EXISTS "TenantCoordinatePresets" CASCADE;
DROP TABLE IF EXISTS "OrganizationUnits" CASCADE;
DROP TABLE IF EXISTS "Tenants" CASCADE;
DROP TABLE IF EXISTS "DynamicDomains" CASCADE;
DROP TABLE IF EXISTS "PaymentTransactions" CASCADE;
DROP TABLE IF EXISTS "Notebooks" CASCADE;
DROP TABLE IF EXISTS "LearningPaths" CASCADE;
DROP TABLE IF EXISTS "LearningAchievements" CASCADE;
DROP TABLE IF EXISTS "LeaderboardEntries" CASCADE;
DROP TABLE IF EXISTS "ExamSessions" CASCADE;
DROP TABLE IF EXISTS "Questions" CASCADE;
DROP TABLE IF EXISTS "BankTopics" CASCADE;
DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;

-- BƯỚC 2: TẠO BẢNG LỊCH SỬ MIGRATION EF CORE
CREATE TABLE "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- BƯỚC 3: TẠO BẢNG CHỦ ĐỀ NGÂN HÀNG CÂU HỎI (BankTopics) VỚI CÂY PHÂN CẤP 2026
CREATE TABLE "BankTopics" (
    "Id" uuid NOT NULL,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "CategoryCode" text NOT NULL,
    "Enabled" boolean NOT NULL DEFAULT true,
    "VisibilityScope" text NOT NULL DEFAULT 'PRIVATE',
    "AllowedTenantIds" text[] NOT NULL DEFAULT '{}',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone,
    "ParentId" uuid NULL,
    "CreatedByUserId" uuid NULL,
    "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    -- CÁC CỘT MỚI NHẤT 2026 (KHÔNG BAO GIỜ BỊ THIẾU DEPTHLEVEL NỮA):
    "DepthLevel" integer NOT NULL DEFAULT 0,
    "DisplayOrder" integer NOT NULL DEFAULT 0,
    "DomainCode" text NOT NULL DEFAULT 'GENERAL',
    "MaterializedPath" text NULL,
    "QuestionCountCached" integer NOT NULL DEFAULT 0,
    "Scope" text NOT NULL DEFAULT 'COMMUNITY',
    "Urn" text NULL,
    CONSTRAINT "PK_BankTopics" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BankTopics_BankTopics_ParentId" FOREIGN KEY ("ParentId") REFERENCES "BankTopics" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_BankTopics_DomainCode" ON "BankTopics" ("DomainCode");
CREATE INDEX "IX_BankTopics_MaterializedPath" ON "BankTopics" ("MaterializedPath");
CREATE INDEX "IX_BankTopics_Scope" ON "BankTopics" ("Scope");
CREATE INDEX "IX_BankTopics_TenantId" ON "BankTopics" ("TenantId");
CREATE INDEX "IX_BankTopics_ParentId" ON "BankTopics" ("ParentId");

-- BƯỚC 4: TẠO BẢNG NGÂN HÀNG CÂU HỎI (Questions)
CREATE TABLE "Questions" (
    "Id" uuid NOT NULL,
    "Content" text NOT NULL,
    "Difficulty" integer NOT NULL DEFAULT 1,
    "DurationSeconds" integer NOT NULL DEFAULT 60,
    "Explanation" text,
    "CategoryCode" text NOT NULL DEFAULT 'GENERAL',
    "QuestionType" text NOT NULL DEFAULT 'single_choice',
    "ContentType" text NOT NULL DEFAULT 'text',
    "OptionType" text NOT NULL DEFAULT 'text',
    "Payload" jsonb NOT NULL DEFAULT '{}',
    "Options" text[] NOT NULL DEFAULT '{}',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone,
    "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedByUserId" uuid NULL,
    -- TOẠ ĐỘ & PHÂN LOẠI MỚI NHẤT 2026:
    "ContextId" uuid NULL,
    "DomainCode" text NOT NULL DEFAULT 'EDUCATION',
    "IsCritical" boolean NOT NULL DEFAULT false,
    "SubCategory" text NULL,
    "Tags" text[] NOT NULL DEFAULT '{}',
    -- THUỘC TÍNH RIÊNG CHO TỪNG DẠNG CÂU HỎI:
    "CorrectOption" text NULL,
    "CorrectOptions" text[] NULL,
    "CorrectAnswer" boolean NULL,
    "CorrectPairs" jsonb NULL,
    "Blanks" jsonb NULL,
    "Tolerance" double precision NULL,
    "MaxScore" numeric NULL,
    "PassScore" numeric NULL,
    "RubricJson" text NULL,
    "TargetLevel" text NULL,
    "AssessmentPurpose" text NULL,
    "IssuingOrg" text NULL,
    "BenchmarkYear" integer NULL,
    "BenchmarkStandard" text NULL,
    CONSTRAINT "PK_Questions" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_Questions_CategoryCode" ON "Questions" ("CategoryCode");
CREATE INDEX "IX_Questions_TenantId" ON "Questions" ("TenantId");
CREATE INDEX "IX_Questions_DomainCode" ON "Questions" ("DomainCode");
CREATE INDEX "IX_Questions_ContextId" ON "Questions" ("ContextId");

-- BƯỚC 5: TẠO BẢNG NGÀNH DYNAMIC DOMAINS (ĐỘNG)
CREATE TABLE "DynamicDomains" (
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
    CONSTRAINT "PK_DynamicDomains" PRIMARY KEY ("Code"),
    CONSTRAINT "FK_DynamicDomains_DynamicDomains_ParentDomainCode" FOREIGN KEY ("ParentDomainCode") REFERENCES "DynamicDomains" ("Code") ON DELETE RESTRICT
);

CREATE INDEX "IX_DynamicDomains_IsSystemStandard" ON "DynamicDomains" ("IsSystemStandard");
CREATE INDEX "IX_DynamicDomains_ParentDomainCode" ON "DynamicDomains" ("ParentDomainCode");
CREATE INDEX "IX_DynamicDomains_TenantId" ON "DynamicDomains" ("TenantId");

-- BƯỚC 6: TẠO BẢNG TENANTS & DOANH NGHIỆP
CREATE TABLE "Tenants" (
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

-- BƯỚC 7: TẠO BẢNG CƠ CẤU TỔ CHỨC ĐA TẦNG (OrganizationUnits)
CREATE TABLE "OrganizationUnits" (
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
    CONSTRAINT "PK_OrganizationUnits" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_OrganizationUnits_OrganizationUnits_ParentId" FOREIGN KEY ("ParentId") REFERENCES "OrganizationUnits" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_OrganizationUnits_ParentId" ON "OrganizationUnits" ("ParentId");
CREATE INDEX "IX_OrganizationUnits_TenantId" ON "OrganizationUnits" ("TenantId");
CREATE UNIQUE INDEX "IX_OrganizationUnits_TenantId_Code" ON "OrganizationUnits" ("TenantId", "Code");

-- BƯỚC 8: TẠO BẢNG CẤU HÌNH TỌA ĐỘ VÀ NGÀNH THEO TENANT
CREATE TABLE "TenantDomainConfigs" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "DomainCode" text NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    "CustomLabel" text,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_TenantDomainConfigs" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_TenantDomainConfigs_TenantId_DomainCode" ON "TenantDomainConfigs" ("TenantId", "DomainCode");
CREATE INDEX "IX_TenantDomainConfigs_DomainCode" ON "TenantDomainConfigs" ("DomainCode");
CREATE INDEX "IX_TenantDomainConfigs_TenantId" ON "TenantDomainConfigs" ("TenantId");

CREATE TABLE "TenantCoordinatePresets" (
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

CREATE INDEX "IX_TenantCoordinatePresets_TenantId" ON "TenantCoordinatePresets" ("TenantId");
CREATE INDEX "IX_TenantCoordinatePresets_TenantId_DomainCode_CoordinateType" ON "TenantCoordinatePresets" ("TenantId", "DomainCode", "CoordinateType");

CREATE TABLE "UserRoles" (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Role" integer NOT NULL,
    "OrgUnitId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "ExpiresAt" timestamp with time zone,
    CONSTRAINT "PK_UserRoles" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserRoles_OrganizationUnits_OrgUnitId" FOREIGN KEY ("OrgUnitId") REFERENCES "OrganizationUnits" ("Id")
);

CREATE INDEX "IX_UserRoles_OrgUnitId" ON "UserRoles" ("OrgUnitId");
CREATE INDEX "IX_UserRoles_Role" ON "UserRoles" ("Role");
CREATE INDEX "IX_UserRoles_TenantId_OrgUnitId" ON "UserRoles" ("TenantId", "OrgUnitId");
CREATE INDEX "IX_UserRoles_TenantId_UserId" ON "UserRoles" ("TenantId", "UserId");

-- BƯỚC 9: CÁC BẢNG KHẢO THÍ, PHIÊN THI, ĐIỂM SỐ & THANH TOÁN
CREATE TABLE "ExamSessions" (
    "Id" uuid NOT NULL,
    "ExamContestId" uuid NOT NULL,
    "ApplicantId" uuid NOT NULL,
    "ApplicantIdentifier" text NOT NULL,
    "TimeStarted" timestamp with time zone NOT NULL DEFAULT now(),
    "TimeSubmitted" timestamp with time zone,
    "Status" integer NOT NULL DEFAULT 0,
    "NumberOfTabSwitches" integer NOT NULL DEFAULT 0,
    "EndReason" text,
    "SnapshotQuestionIds" jsonb,
    "EarnedScore" numeric NOT NULL DEFAULT 0,
    "CorrectItemCount" integer NOT NULL DEFAULT 0,
    "TotalItemCount" integer NOT NULL DEFAULT 0,
    "Passed" boolean NOT NULL DEFAULT false,
    "FinalAnswers" jsonb NOT NULL DEFAULT '{}',
    CONSTRAINT "PK_ExamSessions" PRIMARY KEY ("Id")
);

CREATE TABLE "LeaderboardEntries" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "DisplayName" text NOT NULL,
    "AvatarInitial" text,
    "Period" text NOT NULL,
    "PeriodStart" timestamp with time zone NOT NULL,
    "PeriodEnd" timestamp with time zone NOT NULL,
    "TotalScore" double precision NOT NULL DEFAULT 0,
    "AverageScore" double precision NOT NULL DEFAULT 0,
    "TotalAttempts" integer NOT NULL DEFAULT 0,
    "CorrectCount" integer NOT NULL DEFAULT 0,
    "XpTotal" integer NOT NULL DEFAULT 0,
    "Rank" integer NOT NULL DEFAULT 0,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_LeaderboardEntries" PRIMARY KEY ("Id")
);

CREATE TABLE "LearningAchievements" (
    "Id" uuid NOT NULL,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "BadgeColor" text NOT NULL DEFAULT '#eab308',
    "IconEmoji" text,
    "TriggerType" text NOT NULL DEFAULT 'COUNT',
    "TriggerCondition" jsonb NOT NULL DEFAULT '{}',
    "XpReward" integer NOT NULL DEFAULT 0,
    "TitleReward" text,
    "Enabled" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_LearningAchievements" PRIMARY KEY ("Id")
);

CREATE TABLE "LearningPaths" (
    "Id" uuid NOT NULL,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "IsPublic" boolean NOT NULL DEFAULT true,
    "Enabled" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_LearningPaths" PRIMARY KEY ("Id")
);

CREATE TABLE "Notebooks" (
    "Id" uuid NOT NULL,
    "Title" text NOT NULL,
    "SourcePdfUrl" text NOT NULL,
    "CreatedByAdminId" uuid NOT NULL,
    "Status" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_Notebooks" PRIMARY KEY ("Id")
);

CREATE TABLE "PaymentTransactions" (
    "TransactionId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "PaymentCode" text NOT NULL,
    "AmountRequired" numeric NOT NULL DEFAULT 0,
    "AmountPaid" numeric NOT NULL DEFAULT 0,
    "Status" integer NOT NULL DEFAULT 0,
    "Description" text,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "CompletedAt" timestamp with time zone,
    "RawWebhookPayload" text,
    CONSTRAINT "PK_PaymentTransactions" PRIMARY KEY ("TransactionId")
);

-- BƯỚC 10: NẠP SẴN CÁC NGÀNH ĐỘNG CHUẨN QUỐC GIA & DOANH NGHIỆP (DynamicDomains)
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

-- BƯỚC 11: NẠP TOÀN BỘ MIGRATION VÀO __EFMigrationsHistory ĐỂ EF CORE ĐỒNG BỘ 100%
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

-- HOÀN TẤT
SELECT 'AegisQuiz 2026: ĐÃ TÁI TẠO TOÀN BỘ BẢNG THEO CHUẨN MỚI NHẤT THÀNH CÔNG!' AS Status;
