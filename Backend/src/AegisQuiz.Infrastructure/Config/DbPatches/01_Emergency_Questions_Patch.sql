-- ============================================================================
-- 01_Emergency_Questions_Patch.sql
-- Mục đích: Nới rộng Discriminator QuestionType từ varchar(13) sang text
--           và đảm bảo toàn vẹn các cột tri thức của Questions & BankTopics.
-- Đặc tính: Idempotent 100%, an toàn chạy lặp lại nhiều lần.
-- ============================================================================

-- 1. Nới rộng kiểu dữ liệu sang TEXT vô hạn
ALTER TABLE IF EXISTS "Questions" ALTER COLUMN "QuestionType" TYPE text;
ALTER TABLE IF EXISTS "Questions" ALTER COLUMN "CategoryCode" TYPE text;
ALTER TABLE IF EXISTS "Questions" ALTER COLUMN "CorrectOption" TYPE text;
ALTER TABLE IF EXISTS "Questions" ALTER COLUMN "ContentType" TYPE text;
ALTER TABLE IF EXISTS "Questions" ALTER COLUMN "OptionType" TYPE text;
ALTER TABLE IF EXISTS "Questions" ALTER COLUMN "DomainCode" TYPE text;
ALTER TABLE IF EXISTS "Questions" ALTER COLUMN "TargetLevel" TYPE text;
ALTER TABLE IF EXISTS "Questions" ALTER COLUMN "AssessmentPurpose" TYPE text;
ALTER TABLE IF EXISTS "Questions" ALTER COLUMN "IssuingOrg" TYPE text;
ALTER TABLE IF EXISTS "Questions" ALTER COLUMN "BenchmarkStandard" TYPE text;
ALTER TABLE IF EXISTS "Questions" ALTER COLUMN "CreatedBy" TYPE text;
ALTER TABLE IF EXISTS "Questions" ALTER COLUMN "SubCategory" TYPE text;

-- 2. Đảm bảo các cột mới của Questions luôn tồn tại
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "DomainCode" text NOT NULL DEFAULT 'EDUCATION';
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "Tags" text[] NOT NULL DEFAULT '{}';
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "ContextId" uuid NULL;
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "IsCritical" boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "SubCategory" text NULL;
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "IsPublic" boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS "Questions" ADD COLUMN IF NOT EXISTS "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

-- 3. Đảm bảo cấu trúc phân cấp BankTopics
ALTER TABLE IF EXISTS "BankTopics" ALTER COLUMN "Code" TYPE text;
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "DepthLevel" integer NOT NULL DEFAULT 0;
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "MaterializedPath" text NULL;
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "Scope" text NOT NULL DEFAULT 'COMMUNITY';
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "DomainCode" text NOT NULL DEFAULT 'GENERAL';
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "AllowedTenantIds" text[] NOT NULL DEFAULT '{}';
ALTER TABLE IF EXISTS "BankTopics" ADD COLUMN IF NOT EXISTS "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
