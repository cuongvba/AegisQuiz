-- =========================================================================================
-- 🛡️ AEGIS GLOBAL — PRODUCTION DATABASE SYNC SCRIPT (2026)
-- Script đồng bộ cấu trúc CSDL môi trường Live (PostgreSQL) theo chuẩn Aegis Sovereign
-- An toàn 100%, có thể chạy lặp lại nhiều lần (Idempotent với IF NOT EXISTS)
-- =========================================================================================

BEGIN;

-- 1. BẢNG QuestionContexts (Ngữ cảnh bài đọc hiểu bóc tách từ Docx/PDF)
CREATE TABLE IF NOT EXISTS "QuestionContexts" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "Title" text NOT NULL DEFAULT '',
    "Content" text NOT NULL DEFAULT '',
    "ContentType" text NOT NULL DEFAULT 'text',
    "MediaUrl" text NULL,
    "ContentHash" text NULL,
    "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NULL
);
CREATE INDEX IF NOT EXISTS "IX_QuestionContexts_ContentHash" ON "QuestionContexts" ("ContentHash");
CREATE INDEX IF NOT EXISTS "IX_QuestionContexts_TenantId" ON "QuestionContexts" ("TenantId");

-- 2. CẬP NHẬT CỘT TRONG BẢNG Questions (Kho câu hỏi & Discriminator)
ALTER TABLE IF EXISTS "Questions" ALTER COLUMN "QuestionType" TYPE text;
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "ContextId" uuid NULL;
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "DomainCode" text NULL;
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "SubCategory" text NULL;
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "IsCritical" boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "Tags" text NULL;
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "CreatedBy" text NULL;
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "IsPublic" boolean NOT NULL DEFAULT false;
CREATE INDEX IF NOT EXISTS "IX_Questions_ContextId" ON "Questions" ("ContextId");
CREATE INDEX IF NOT EXISTS "IX_Questions_TenantId" ON "Questions" ("TenantId");

-- 3. CẬP NHẬT CỘT TRONG BẢNG BankTopics (Cây phân cấp chủ đề O(1))
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "DepthLevel" integer NOT NULL DEFAULT 1;
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "MaterializedPath" text NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "Scope" text NOT NULL DEFAULT 'GLOBAL';
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "DomainCode" text NOT NULL DEFAULT 'ALL';
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "Urn" text NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "ParentId" uuid NULL;
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "AllowedTenantIds" text NULL;
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
CREATE INDEX IF NOT EXISTS "IX_BankTopics_ParentId" ON "BankTopics" ("ParentId");
CREATE INDEX IF NOT EXISTS "IX_BankTopics_MaterializedPath" ON "BankTopics" ("MaterializedPath");
CREATE INDEX IF NOT EXISTS "IX_BankTopics_DomainCode" ON "BankTopics" ("DomainCode");

-- 4. BẢNG DynamicDomains (Phân hệ tri thức đa miền)
CREATE TABLE IF NOT EXISTS "DynamicDomains" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "Code" text NOT NULL,
    "DisplayName" text NOT NULL,
    "Description" text NULL,
    "Icon" text NOT NULL DEFAULT 'Layers',
    "ThemePrimary" text NOT NULL DEFAULT '#6366f1',
    "ThemeSecondary" text NOT NULL DEFAULT '#ec4899',
    "UrnPrefix" text NOT NULL DEFAULT 'urn:aegis:domain:',
    "IsActive" boolean NOT NULL DEFAULT true,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "MetadataJson" text NOT NULL DEFAULT '{}',
    "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_DynamicDomains_Code" ON "DynamicDomains" ("Code");

-- 5. BẢNG QuestionParsingRules (CIG Ingestion Grammar Engine 2026)
CREATE TABLE IF NOT EXISTS "QuestionParsingRules" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "RuleCode" text NOT NULL,
    "RuleName" text NOT NULL,
    "Description" text NULL,
    "Scope" text NOT NULL DEFAULT 'GLOBAL',
    "Priority" integer NOT NULL DEFAULT 100,
    "IsActive" boolean NOT NULL DEFAULT true,
    "SpecJson" text NOT NULL DEFAULT '{}',
    "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NULL
);
CREATE INDEX IF NOT EXISTS "IX_QuestionParsingRules_IsActive" ON "QuestionParsingRules" ("IsActive");
CREATE INDEX IF NOT EXISTS "IX_QuestionParsingRules_Priority" ON "QuestionParsingRules" ("Priority");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_QuestionParsingRules_TenantId_RuleCode" ON "QuestionParsingRules" ("TenantId", "RuleCode");

-- 6. BẢNG UniversalCoordinateTaxonomies & TaxonomyKeywords
CREATE TABLE IF NOT EXISTS "UniversalCoordinateTaxonomies" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "Urn" text NOT NULL,
    "Dimension" integer NOT NULL DEFAULT 1,
    "AxisCode" text NOT NULL,
    "DisplayName" text NOT NULL,
    "ParentUrn" text NULL,
    "MaterializedPath" text NOT NULL,
    "DepthLevel" integer NOT NULL DEFAULT 1,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "MetadataJson" text NOT NULL DEFAULT '{}',
    "IsSystem" boolean NOT NULL DEFAULT true,
    "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UniversalCoordinateTaxonomies_Urn" ON "UniversalCoordinateTaxonomies" ("Urn");

CREATE TABLE IF NOT EXISTS "TaxonomyKeywords" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "Urn" text NOT NULL,
    "Keyword" text NOT NULL,
    "Language" text NOT NULL DEFAULT 'vi',
    "Weight" double precision NOT NULL DEFAULT 1.0,
    "IsExactMatch" boolean NOT NULL DEFAULT false,
    "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'
);
CREATE INDEX IF NOT EXISTS "IX_TaxonomyKeywords_Keyword" ON "TaxonomyKeywords" ("Keyword");
CREATE INDEX IF NOT EXISTS "IX_TaxonomyKeywords_Urn" ON "TaxonomyKeywords" ("Urn");

-- 7. BẢNG UniversalAiCouncils & UniversalCouncilSeats & SymposiumSessions
CREATE TABLE IF NOT EXISTS "UniversalAiCouncils" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "CouncilCode" text NOT NULL,
    "DisplayName" text NOT NULL,
    "DisciplineArea" text NOT NULL DEFAULT '',
    "MaterializedTaxonomyPath" text NOT NULL DEFAULT '',
    "EpistemicManifesto" text NOT NULL DEFAULT '',
    "BenchmarkPrestigeElo" integer NOT NULL DEFAULT 2800,
    "IsActive" boolean NOT NULL DEFAULT true,
    "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UniversalAiCouncils_CouncilCode" ON "UniversalAiCouncils" ("CouncilCode");

CREATE TABLE IF NOT EXISTS "UniversalCouncilSeats" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "CouncilId" uuid NOT NULL,
    "PersonaCode" text NOT NULL,
    "SeatTitle" text NOT NULL,
    "ModelId" text NOT NULL,
    "Provider" text NOT NULL DEFAULT 'OpenAI',
    "SystemInstructionOverride" text NOT NULL DEFAULT '',
    "EpistemicWeight" double precision NOT NULL DEFAULT 1.0,
    "Temperature" double precision NOT NULL DEFAULT 0.7,
    "MaxTokens" integer NOT NULL DEFAULT 4096,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "IsActive" boolean NOT NULL DEFAULT true,
    "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NULL
);
CREATE INDEX IF NOT EXISTS "IX_UniversalCouncilSeats_CouncilId" ON "UniversalCouncilSeats" ("CouncilId");

CREATE TABLE IF NOT EXISTS "SymposiumSessions" (
    "Id" uuid NOT NULL PRIMARY KEY,
    "SessionCode" text NOT NULL,
    "CouncilCode" text NOT NULL,
    "CouncilDisplayName" text NOT NULL DEFAULT '',
    "TopicOrQuestion" text NOT NULL,
    "Status" integer NOT NULL DEFAULT 0,
    "RoundsCompleted" integer NOT NULL DEFAULT 0,
    "TranscriptJson" text NOT NULL DEFAULT '[]',
    "ConsensusThesis" text NOT NULL DEFAULT '',
    "ConsensusConfidence" double precision NOT NULL DEFAULT 0.0,
    "RubricScoresJson" text NOT NULL DEFAULT '{}',
    "ConvenedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "ConcludedAt" timestamp with time zone NULL,
    "InitiatedByUserId" uuid NULL,
    "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000'
);
CREATE INDEX IF NOT EXISTS "IX_SymposiumSessions_ConvenedAt" ON "SymposiumSessions" ("ConvenedAt");

-- Hạt giống 6 Viện Hàn Lâm Nhận Thức Vũ Trụ
INSERT INTO "UniversalAiCouncils" ("Id", "CouncilCode", "DisplayName", "DisciplineArea", "MaterializedTaxonomyPath", "EpistemicManifesto", "BenchmarkPrestigeElo", "IsActive", "CreatedAt")
VALUES
  ('c0000001-0000-0000-0000-000000000001', 'COSMOLOGY', 'Viện Vũ Trụ & Vật Lý Lượng Tử', 'Vật lý lý thuyết & Lượng tử', '/UNIVERSE/PHYSICS/QUANTUM', 'Thực tại vật lý vận hành bởi trường lượng tử và hình học không-thời gian phi tuyến.', 2950, true, now()),
  ('c0000001-0000-0000-0000-000000000002', 'MATHEMATICS', 'Viện Toán Học & Logic Hình Thức', 'Toán học thuần túy & Độ phức tạp', '/UNIVERSE/MATHEMATICS/LOGIC', 'Chân lý tuyệt đối chỉ tồn tại qua phép suy diễn hình thức không mâu thuẫn.', 2980, true, now()),
  ('c0000001-0000-0000-0000-000000000003', 'PHILOSOPHY', 'Viện Triết Học, Nhân Văn & Ý Thức', 'Bản thể luận, Đạo đức & Ý nghĩa', '/HUMANITY/PHILOSOPHY/EPISTEMOLOGY', 'Mọi tri thức khoa học phải được soi chiếu qua phẩm giá con người và ý nghĩa tồn tại.', 2900, true, now()),
  ('c0000001-0000-0000-0000-000000000004', 'AGI_CYBERNETICS', 'Viện Siêu Trí Tuệ & Điều Khiển Học', 'Kiến trúc AGI & Căn chỉnh nhân loại', '/AI/SUPERINTELLIGENCE/ALIGNMENT', 'Sự xuất hiện của Siêu trí tuệ là bước tiến hóa tất yếu của thông tin vũ trụ.', 2990, true, now()),
  ('c0000001-0000-0000-0000-000000000005', 'ECONOMICS', 'Viện Kinh Tế Chính Trị & Địa Chiến Lược', 'Kinh tế vĩ mô & Lý thuyết trò chơi', '/SOCIETY/ECONOMICS/GEOPOLITICS', 'Dòng chảy giá trị và phân bổ tài nguyên quyết định sự hưng thịnh của nền văn minh.', 2850, true, now()),
  ('c0000001-0000-0000-0000-000000000006', 'LIFE_SCIENCES', 'Viện Khoa Học Sự Sống & Sinh Quyển', 'Sinh học phân tử & Sinh thái học', '/NATURE/BIOLOGY/EVOLUTION', 'Sự sống là cấu trúc tiêu tán vi mô chống lại entropy nhiệt động lực học.', 2880, true, now())
ON CONFLICT ("CouncilCode") DO NOTHING;

-- 8. BẢNG PaymentTransactions (Hạ tầng thanh toán đa cổng và Quỹ tài trợ)
CREATE TABLE IF NOT EXISTS "PaymentTransactions" (
    "TransactionId" uuid NOT NULL PRIMARY KEY,
    "UserId" uuid NOT NULL,
    "TenantId" uuid NULL,
    "OrderCode" bigint NOT NULL DEFAULT 0,
    "PaymentCode" text NOT NULL DEFAULT '',
    "AmountRequired" numeric(18,2) NOT NULL DEFAULT 0,
    "Currency" text NOT NULL DEFAULT 'VND',
    "Provider" text NOT NULL DEFAULT 'PayOS',
    "Description" text NOT NULL DEFAULT '',
    "CheckoutUrl" text NOT NULL DEFAULT '',
    "QrCode" text NOT NULL DEFAULT '',
    "PaymentLinkId" text NOT NULL DEFAULT '',
    "TransactionHash" text NOT NULL DEFAULT '',
    "MetadataJson" text NOT NULL DEFAULT '{}',
    "Status" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "CompletedAt" timestamp with time zone NULL
);
ALTER TABLE IF EXISTS "PaymentTransactions" ADD COLUMN IF NOT EXISTS "CheckoutUrl" text DEFAULT '';
ALTER TABLE IF EXISTS "PaymentTransactions" ADD COLUMN IF NOT EXISTS "TenantId" uuid;
ALTER TABLE IF EXISTS "PaymentTransactions" ADD COLUMN IF NOT EXISTS "OrderCode" bigint DEFAULT 0;
ALTER TABLE IF EXISTS "PaymentTransactions" ADD COLUMN IF NOT EXISTS "Provider" text DEFAULT 'PayOS';
ALTER TABLE IF EXISTS "PaymentTransactions" ADD COLUMN IF NOT EXISTS "Description" text DEFAULT '';
ALTER TABLE IF EXISTS "PaymentTransactions" ADD COLUMN IF NOT EXISTS "QrCode" text DEFAULT '';
ALTER TABLE IF EXISTS "PaymentTransactions" ADD COLUMN IF NOT EXISTS "PaymentLinkId" text DEFAULT '';
ALTER TABLE IF EXISTS "PaymentTransactions" ADD COLUMN IF NOT EXISTS "TransactionHash" text DEFAULT '';
ALTER TABLE IF EXISTS "PaymentTransactions" ADD COLUMN IF NOT EXISTS "MetadataJson" text DEFAULT '{}';
ALTER TABLE IF EXISTS "PaymentTransactions" ADD COLUMN IF NOT EXISTS "CompletedAt" timestamp with time zone;
CREATE INDEX IF NOT EXISTS "IX_PaymentTransactions_OrderCode" ON "PaymentTransactions" ("OrderCode");
CREATE INDEX IF NOT EXISTS "IX_PaymentTransactions_Provider" ON "PaymentTransactions" ("Provider");

COMMIT;
