START TRANSACTION;
ALTER TABLE "Questions" DROP COLUMN IF EXISTS "CreatedByUserId";

ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "ContextId" uuid;

ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "DomainCode" text NOT NULL DEFAULT 'EDUCATION';

ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "IsCritical" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "SubCategory" text;

ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "Tags" text[] NOT NULL DEFAULT ('{}'::text[]);

CREATE TABLE IF NOT EXISTS "QuestionContexts" (
    "Id" uuid NOT NULL,
    "Title" text NOT NULL,
    "Content" text NOT NULL,
    "ContentType" text NOT NULL,
    "MediaUrl" text,
    "ContentHash" text,
    "TenantId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_QuestionContexts" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_Questions_ContextId" ON "Questions" ("ContextId");

CREATE INDEX IF NOT EXISTS "IX_Questions_DomainCode" ON "Questions" ("DomainCode");

CREATE INDEX IF NOT EXISTS "IX_QuestionContexts_ContentHash" ON "QuestionContexts" ("ContentHash");

CREATE INDEX IF NOT EXISTS "IX_QuestionContexts_TenantId" ON "QuestionContexts" ("TenantId");

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_Questions_QuestionContexts_ContextId'
    ) THEN
        ALTER TABLE "Questions" ADD CONSTRAINT "FK_Questions_QuestionContexts_ContextId" 
        FOREIGN KEY ("ContextId") REFERENCES "QuestionContexts" ("Id") ON DELETE SET NULL;
    END IF;
END $$;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260905091540_AddQuestionContextAndMetadata', '10.0.5')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
