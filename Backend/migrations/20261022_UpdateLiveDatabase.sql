CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "BankTopics" (
        "Id" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "CategoryCode" text NOT NULL,
        "Enabled" boolean NOT NULL,
        "VisibilityScope" text NOT NULL DEFAULT 'PRIVATE',
        "AllowedTenantIds" text[] NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_BankTopics" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "ExamSessions" (
        "Id" uuid NOT NULL,
        "ExamContestId" uuid NOT NULL,
        "ApplicantId" uuid NOT NULL,
        "ApplicantIdentifier" text NOT NULL,
        "TimeStarted" timestamp with time zone NOT NULL,
        "TimeSubmitted" timestamp with time zone,
        "Status" integer NOT NULL,
        "NumberOfTabSwitches" integer NOT NULL,
        "EndReason" text,
        "SnapshotQuestionIds" jsonb,
        "EarnedScore" numeric NOT NULL,
        "CorrectItemCount" integer NOT NULL,
        "TotalItemCount" integer NOT NULL,
        "Passed" boolean NOT NULL,
        "FinalAnswers" jsonb NOT NULL,
        CONSTRAINT "PK_ExamSessions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "LeaderboardEntries" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "DisplayName" text NOT NULL,
        "AvatarInitial" text,
        "Period" text NOT NULL,
        "PeriodStart" timestamp with time zone NOT NULL,
        "PeriodEnd" timestamp with time zone NOT NULL,
        "TotalScore" double precision NOT NULL,
        "AverageScore" double precision NOT NULL,
        "TotalAttempts" integer NOT NULL,
        "CorrectCount" integer NOT NULL,
        "XpTotal" integer NOT NULL,
        "Rank" integer NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_LeaderboardEntries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "LearningAchievements" (
        "Id" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "BadgeColor" text NOT NULL,
        "IconEmoji" text,
        "TriggerType" text NOT NULL,
        "TriggerCondition" jsonb NOT NULL,
        "XpReward" integer NOT NULL,
        "TitleReward" text,
        "Enabled" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_LearningAchievements" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "LearningPaths" (
        "Id" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "IsPublic" boolean NOT NULL,
        "Enabled" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_LearningPaths" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "Notebooks" (
        "Id" uuid NOT NULL,
        "Title" text NOT NULL,
        "SourcePdfUrl" text NOT NULL,
        "CreatedByAdminId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Notebooks" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "PaymentTransactions" (
        "TransactionId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "PaymentCode" text NOT NULL,
        "AmountRequired" numeric NOT NULL,
        "Currency" text NOT NULL,
        "Status" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PaymentTransactions" PRIMARY KEY ("TransactionId")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "QuestionAttemptAnswers" (
        "Id" uuid NOT NULL,
        "AttemptId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "QuestionId" uuid NOT NULL,
        "SubmittedAnswer" text NOT NULL,
        "IsCorrect" boolean NOT NULL,
        "PointsEarned" double precision NOT NULL,
        "PointsMax" double precision NOT NULL,
        "TimeTakenSec" integer,
        "CategoryCode" text,
        "Difficulty" integer NOT NULL,
        "AttemptDate" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_QuestionAttemptAnswers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "QuestionAttempts" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "QuestionId" uuid NOT NULL,
        "Category" text NOT NULL,
        "Difficulty" integer NOT NULL,
        "SelectedAnswer" text NOT NULL,
        "IsCorrect" boolean NOT NULL,
        "TimeSpentSeconds" integer NOT NULL,
        "AttemptDate" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_QuestionAttempts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "Questions" (
        "Id" uuid NOT NULL,
        "Content" text NOT NULL,
        "Difficulty" integer NOT NULL,
        "DurationSeconds" integer NOT NULL,
        "CategoryCode" text NOT NULL,
        "Payload" jsonb NOT NULL,
        "QuestionType" character varying(13) NOT NULL,
        "GradingRubric" text,
        "Options" text[],
        "CorrectOption" text,
        CONSTRAINT "PK_Questions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "PersonAchievements" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "AchievementId" uuid NOT NULL,
        "EarnedAt" timestamp with time zone NOT NULL,
        "XpEarned" integer NOT NULL,
        "ContextJson" jsonb,
        CONSTRAINT "PK_PersonAchievements" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PersonAchievements_LearningAchievements_AchievementId" FOREIGN KEY ("AchievementId") REFERENCES "LearningAchievements" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "LearningPathProgresses" (
        "Id" uuid NOT NULL,
        "LearningPathId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "CurrentStep" integer NOT NULL,
        "CompletionPercent" double precision NOT NULL,
        "AverageScore" double precision NOT NULL,
        "CompletedAt" timestamp with time zone,
        "Status" text NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_LearningPathProgresses" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LearningPathProgresses_LearningPaths_LearningPathId" FOREIGN KEY ("LearningPathId") REFERENCES "LearningPaths" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "LearningPathSteps" (
        "Id" uuid NOT NULL,
        "LearningPathId" uuid NOT NULL,
        "StepOrder" integer NOT NULL,
        "CategoryCode" text NOT NULL,
        "CategoryName" text NOT NULL,
        "PassScore" double precision NOT NULL,
        "LearningObjective" text,
        CONSTRAINT "PK_LearningPathSteps" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LearningPathSteps_LearningPaths_LearningPathId" FOREIGN KEY ("LearningPathId") REFERENCES "LearningPaths" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE TABLE "NotebookChapters" (
        "Id" uuid NOT NULL,
        "NotebookId" uuid NOT NULL,
        "OrderIndex" integer NOT NULL,
        "Title" text NOT NULL,
        "Summary" text NOT NULL,
        "RawContent" text NOT NULL,
        "KeyPointsJson" text NOT NULL,
        "GeneratedQuestionIds" uuid[] NOT NULL,
        CONSTRAINT "PK_NotebookChapters" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_NotebookChapters_Notebooks_NotebookId" FOREIGN KEY ("NotebookId") REFERENCES "Notebooks" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_BankTopics_CategoryCode" ON "BankTopics" ("CategoryCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_BankTopics_Code" ON "BankTopics" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_ExamSessions_ApplicantId" ON "ExamSessions" ("ApplicantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_ExamSessions_ApplicantId_Status" ON "ExamSessions" ("ApplicantId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_ExamSessions_Status" ON "ExamSessions" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_LeaderboardEntries_Period_PeriodStart_Rank" ON "LeaderboardEntries" ("Period", "PeriodStart", "Rank");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_LeaderboardEntries_UserId_Period_PeriodStart" ON "LeaderboardEntries" ("UserId", "Period", "PeriodStart");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_LearningAchievements_Code" ON "LearningAchievements" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_LearningPathProgresses_LearningPathId" ON "LearningPathProgresses" ("LearningPathId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_LearningPathProgresses_UserId" ON "LearningPathProgresses" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_LearningPathProgresses_UserId_LearningPathId" ON "LearningPathProgresses" ("UserId", "LearningPathId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_LearningPaths_Code" ON "LearningPaths" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_LearningPathSteps_LearningPathId_StepOrder" ON "LearningPathSteps" ("LearningPathId", "StepOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_NotebookChapters_NotebookId" ON "NotebookChapters" ("NotebookId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_PaymentTransactions_PaymentCode" ON "PaymentTransactions" ("PaymentCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_PersonAchievements_AchievementId" ON "PersonAchievements" ("AchievementId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_PersonAchievements_UserId" ON "PersonAchievements" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_PersonAchievements_UserId_AchievementId" ON "PersonAchievements" ("UserId", "AchievementId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_QuestionAttemptAnswers_AttemptId" ON "QuestionAttemptAnswers" ("AttemptId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_QuestionAttemptAnswers_AttemptId_CategoryCode" ON "QuestionAttemptAnswers" ("AttemptId", "CategoryCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_QuestionAttemptAnswers_UserId" ON "QuestionAttemptAnswers" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_QuestionAttempts_AttemptDate" ON "QuestionAttempts" ("AttemptDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_QuestionAttempts_UserId_AttemptDate" ON "QuestionAttempts" ("UserId", "AttemptDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    CREATE INDEX "IX_Questions_CategoryCode" ON "Questions" ("CategoryCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702105341_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260702105341_InitialCreate', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260703081819_AddTopicHierarchy') THEN
    ALTER TABLE "BankTopics" ADD "ParentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260703081819_AddTopicHierarchy') THEN
    CREATE INDEX "IX_BankTopics_ParentId" ON "BankTopics" ("ParentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260703081819_AddTopicHierarchy') THEN
    ALTER TABLE "BankTopics" ADD CONSTRAINT "FK_BankTopics_BankTopics_ParentId" FOREIGN KEY ("ParentId") REFERENCES "BankTopics" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260703081819_AddTopicHierarchy') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260703081819_AddTopicHierarchy', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260704025931_MoveOptionsToQuestionBase') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260704025931_MoveOptionsToQuestionBase', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260704103945_AddQuestionMediaTypes') THEN
    ALTER TABLE "Questions" ADD "ContentType" text NOT NULL DEFAULT 'text';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260704103945_AddQuestionMediaTypes') THEN
    ALTER TABLE "Questions" ADD "OptionType" text NOT NULL DEFAULT 'text';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260704103945_AddQuestionMediaTypes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260704103945_AddQuestionMediaTypes', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260828042215_AddTenantIdAndAuditTrail') THEN
    ALTER TABLE "Questions" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260828042215_AddTenantIdAndAuditTrail') THEN
    ALTER TABLE "Questions" ADD "CreatedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260828042215_AddTenantIdAndAuditTrail') THEN
    ALTER TABLE "Questions" ADD "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260828042215_AddTenantIdAndAuditTrail') THEN
    ALTER TABLE "Questions" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260828042215_AddTenantIdAndAuditTrail') THEN
    ALTER TABLE "QuestionAttemptAnswers" ADD "AiExplanation" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260828042215_AddTenantIdAndAuditTrail') THEN
    ALTER TABLE "QuestionAttemptAnswers" ADD "AiScore" double precision;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260828042215_AddTenantIdAndAuditTrail') THEN
    ALTER TABLE "QuestionAttemptAnswers" ADD "GradedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260828042215_AddTenantIdAndAuditTrail') THEN
    ALTER TABLE "BankTopics" ADD "CreatedByUserId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260828042215_AddTenantIdAndAuditTrail') THEN
    ALTER TABLE "BankTopics" ADD "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260828042215_AddTenantIdAndAuditTrail') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260828042215_AddTenantIdAndAuditTrail', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905091540_AddQuestionContextAndMetadata') THEN
    ALTER TABLE "Questions" DROP COLUMN "CreatedByUserId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905091540_AddQuestionContextAndMetadata') THEN
    ALTER TABLE "Questions" ADD "ContextId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905091540_AddQuestionContextAndMetadata') THEN
    ALTER TABLE "Questions" ADD "DomainCode" text NOT NULL DEFAULT 'EDUCATION';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905091540_AddQuestionContextAndMetadata') THEN
    ALTER TABLE "Questions" ADD "IsCritical" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905091540_AddQuestionContextAndMetadata') THEN
    ALTER TABLE "Questions" ADD "SubCategory" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905091540_AddQuestionContextAndMetadata') THEN
    ALTER TABLE "Questions" ADD "Tags" text[] NOT NULL DEFAULT ('{}'::text[]);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905091540_AddQuestionContextAndMetadata') THEN
    CREATE TABLE "QuestionContexts" (
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905091540_AddQuestionContextAndMetadata') THEN
    CREATE INDEX "IX_Questions_ContextId" ON "Questions" ("ContextId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905091540_AddQuestionContextAndMetadata') THEN
    CREATE INDEX "IX_Questions_DomainCode" ON "Questions" ("DomainCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905091540_AddQuestionContextAndMetadata') THEN
    CREATE INDEX "IX_QuestionContexts_ContentHash" ON "QuestionContexts" ("ContentHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905091540_AddQuestionContextAndMetadata') THEN
    CREATE INDEX "IX_QuestionContexts_TenantId" ON "QuestionContexts" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905091540_AddQuestionContextAndMetadata') THEN
    ALTER TABLE "Questions" ADD CONSTRAINT "FK_Questions_QuestionContexts_ContextId" FOREIGN KEY ("ContextId") REFERENCES "QuestionContexts" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905091540_AddQuestionContextAndMetadata') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260905091540_AddQuestionContextAndMetadata', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    ALTER TABLE "BankTopics" ADD "DepthLevel" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    ALTER TABLE "BankTopics" ADD "DisplayOrder" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    ALTER TABLE "BankTopics" ADD "DomainCode" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    ALTER TABLE "BankTopics" ADD "MaterializedPath" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    ALTER TABLE "BankTopics" ADD "QuestionCountCached" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    ALTER TABLE "BankTopics" ADD "Scope" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    ALTER TABLE "BankTopics" ADD "Urn" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE TABLE "DynamicDomains" (
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "Icon" text NOT NULL,
        "ColorBadge" text NOT NULL,
        "IsSystemStandard" boolean NOT NULL,
        "TenantId" uuid,
        "ParentDomainCode" text,
        "IsActive" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_DynamicDomains" PRIMARY KEY ("Code"),
        CONSTRAINT "FK_DynamicDomains_DynamicDomains_ParentDomainCode" FOREIGN KEY ("ParentDomainCode") REFERENCES "DynamicDomains" ("Code") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE TABLE "OrganizationUnits" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "ParentId" uuid,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "UnitType" integer NOT NULL,
        "HierarchyPath" text NOT NULL,
        "Email" text,
        "PhoneNumber" text,
        "Address" text,
        "IsActive" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_OrganizationUnits" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_OrganizationUnits_OrganizationUnits_ParentId" FOREIGN KEY ("ParentId") REFERENCES "OrganizationUnits" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE TABLE "TenantCoordinatePresets" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "DomainCode" text NOT NULL,
        "CoordinateType" text NOT NULL,
        "PresetCode" text NOT NULL,
        "PresetLabel" text NOT NULL,
        "SynonymsJson" text,
        "IsDefault" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TenantCoordinatePresets" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE TABLE "Tenants" (
        "Id" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "LogoUrl" text,
        "PrimaryColor" text,
        "Domain" text,
        "IsActive" boolean NOT NULL,
        "Plan" integer NOT NULL,
        "ScaleType" integer NOT NULL,
        "MaxUsers" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone,
        "KeycloakRealmId" text,
        "ConfigJson" text,
        "FeatureFlagsJson" text,
        "CustomDomain" text,
        CONSTRAINT "PK_Tenants" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE TABLE "TenantDomainConfigs" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "DomainCode" text NOT NULL,
        "IsEnabled" boolean NOT NULL,
        "CustomDisplayName" text,
        "DisplayOrder" integer NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TenantDomainConfigs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TenantDomainConfigs_DynamicDomains_DomainCode" FOREIGN KEY ("DomainCode") REFERENCES "DynamicDomains" ("Code") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE TABLE "UserRoles" (
        "Id" uuid NOT NULL,
        "UserId" text NOT NULL,
        "TenantId" uuid NOT NULL,
        "Role" text NOT NULL,
        "OrgUnitId" uuid,
        "IsActive" boolean NOT NULL,
        "AssignedAt" timestamp with time zone NOT NULL,
        "AssignedBy" text,
        "ExpiresAt" timestamp with time zone,
        CONSTRAINT "PK_UserRoles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserRoles_OrganizationUnits_OrgUnitId" FOREIGN KEY ("OrgUnitId") REFERENCES "OrganizationUnits" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    INSERT INTO "DynamicDomains" ("Code", "ColorBadge", "CreatedAt", "Description", "DisplayOrder", "Icon", "IsActive", "IsSystemStandard", "Name", "ParentDomainCode", "TenantId")
    VALUES ('BANKING', '#059669', TIMESTAMPTZ '2026-09-12T11:22:33.216761Z', 'Nghiệp vụ tín dụng, thanh toán, ngân quỹ, quản trị rủi ro', 2, 'Landmark', TRUE, TRUE, 'Tài chính - Ngân hàng', NULL, NULL);
    INSERT INTO "DynamicDomains" ("Code", "ColorBadge", "CreatedAt", "Description", "DisplayOrder", "Icon", "IsActive", "IsSystemStandard", "Name", "ParentDomainCode", "TenantId")
    VALUES ('EDUCATION', '#2563eb', TIMESTAMPTZ '2026-09-12T11:22:33.215712Z', 'Khảo thí đại học, phổ thông, học thuật tổng quát', 1, 'GraduationCap', TRUE, TRUE, 'Giáo dục & Học thuật', NULL, NULL);
    INSERT INTO "DynamicDomains" ("Code", "ColorBadge", "CreatedAt", "Description", "DisplayOrder", "Icon", "IsActive", "IsSystemStandard", "Name", "ParentDomainCode", "TenantId")
    VALUES ('GENERAL', '#4b5563', TIMESTAMPTZ '2026-09-12T11:22:33.21677Z', 'Kiến thức đại cương, kỹ năng mềm, văn hóa doanh nghiệp', 7, 'Layers', TRUE, TRUE, 'Tổng hợp / Đại cương', NULL, NULL);
    INSERT INTO "DynamicDomains" ("Code", "ColorBadge", "CreatedAt", "Description", "DisplayOrder", "Icon", "IsActive", "IsSystemStandard", "Name", "ParentDomainCode", "TenantId")
    VALUES ('GOV_DRIVING', '#7c3aed', TIMESTAMPTZ '2026-09-12T11:22:33.216769Z', 'Bộ 600 câu GPLX Bộ GTVT, 60 câu điểm liệt, sa hình AI', 5, 'Car', TRUE, TRUE, 'Sát hạch Lái xe Quốc gia', NULL, NULL);
    INSERT INTO "DynamicDomains" ("Code", "ColorBadge", "CreatedAt", "Description", "DisplayOrder", "Icon", "IsActive", "IsSystemStandard", "Name", "ParentDomainCode", "TenantId")
    VALUES ('HEALTHCARE', '#dc2626', TIMESTAMPTZ '2026-09-12T11:22:33.216768Z', 'Y học, dược lâm sàng, quy trình điều dưỡng, kiểm soát nhiễm khuẩn', 3, 'HeartPulse', TRUE, TRUE, 'Y tế - Sức khỏe', NULL, NULL);
    INSERT INTO "DynamicDomains" ("Code", "ColorBadge", "CreatedAt", "Description", "DisplayOrder", "Icon", "IsActive", "IsSystemStandard", "Name", "ParentDomainCode", "TenantId")
    VALUES ('HSE', '#d97706', TIMESTAMPTZ '2026-09-12T11:22:33.216768Z', 'An toàn vệ sinh lao động, PCCC, quy chuẩn ISO 45001', 4, 'HardHat', TRUE, TRUE, 'An toàn - Môi trường LĐ', NULL, NULL);
    INSERT INTO "DynamicDomains" ("Code", "ColorBadge", "CreatedAt", "Description", "DisplayOrder", "Icon", "IsActive", "IsSystemStandard", "Name", "ParentDomainCode", "TenantId")
    VALUES ('IT_SECURITY', '#0284c7', TIMESTAMPTZ '2026-09-12T11:22:33.216769Z', 'Bảo mật an ninh mạng, kiến trúc hệ thống, chứng chỉ CISSP/CompTIA', 6, 'ShieldCheck', TRUE, TRUE, 'An toàn TT & CNTT', NULL, NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_BankTopics_DomainCode" ON "BankTopics" ("DomainCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_BankTopics_MaterializedPath" ON "BankTopics" ("MaterializedPath");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_BankTopics_Scope" ON "BankTopics" ("Scope");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_BankTopics_TenantId" ON "BankTopics" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_DynamicDomains_IsSystemStandard" ON "DynamicDomains" ("IsSystemStandard");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_DynamicDomains_ParentDomainCode" ON "DynamicDomains" ("ParentDomainCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_DynamicDomains_TenantId" ON "DynamicDomains" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_OrganizationUnits_ParentId" ON "OrganizationUnits" ("ParentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_OrganizationUnits_TenantId" ON "OrganizationUnits" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE UNIQUE INDEX "IX_OrganizationUnits_TenantId_Code" ON "OrganizationUnits" ("TenantId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_TenantCoordinatePresets_TenantId" ON "TenantCoordinatePresets" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_TenantCoordinatePresets_TenantId_DomainCode_CoordinateType" ON "TenantCoordinatePresets" ("TenantId", "DomainCode", "CoordinateType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_TenantDomainConfigs_DomainCode" ON "TenantDomainConfigs" ("DomainCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_TenantDomainConfigs_TenantId" ON "TenantDomainConfigs" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE UNIQUE INDEX "IX_TenantDomainConfigs_TenantId_DomainCode" ON "TenantDomainConfigs" ("TenantId", "DomainCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_UserRoles_OrgUnitId" ON "UserRoles" ("OrgUnitId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_UserRoles_Role" ON "UserRoles" ("Role");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_UserRoles_TenantId_OrgUnitId" ON "UserRoles" ("TenantId", "OrgUnitId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    CREATE INDEX "IX_UserRoles_TenantId_UserId" ON "UserRoles" ("TenantId", "UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260912112239_AddHierarchyAndDynamicDomains') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260912112239_AddHierarchyAndDynamicDomains', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917152701_CommunityFirstTenancy_v2_6') THEN

                    ALTER TABLE "Tenants" ADD COLUMN IF NOT EXISTS "IsCommunityHub" boolean NOT NULL DEFAULT false;
                    ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "CreatedBy" text;
                    ALTER TABLE "Questions" ADD COLUMN IF NOT EXISTS "IsPublic" boolean NOT NULL DEFAULT false;
                    ALTER TABLE "OrganizationUnits" ADD COLUMN IF NOT EXISTS "DepthLevel" integer NOT NULL DEFAULT 0;
                    ALTER TABLE "OrganizationUnits" ADD COLUMN IF NOT EXISTS "TaxonomyCategory" text;

                    CREATE TABLE IF NOT EXISTS "UserAccounts" (
                        "Id" uuid NOT NULL,
                        "Email" text NOT NULL,
                        "PasswordHash" text NOT NULL,
                        "FullName" text NOT NULL,
                        "PhoneNumber" text,
                        "AvatarUrl" text,
                        "Role" text NOT NULL DEFAULT 'Learner',
                        "TenantId" uuid NOT NULL,
                        "OrgUnitId" uuid,
                        "IsPremium" boolean NOT NULL DEFAULT false,
                        "SubscriptionTier" text NOT NULL DEFAULT 'FREE',
                        "SubscriptionExpiresAt" timestamp with time zone,
                        "IsActive" boolean NOT NULL DEFAULT true,
                        "FailedLoginAttempts" integer NOT NULL DEFAULT 0,
                        "LockoutEnd" timestamp with time zone,
                        "LastLoginAt" timestamp with time zone,
                        "PasswordResetToken" text,
                        "PasswordResetTokenExpiresAt" timestamp with time zone,
                        "IsTwoFactorEnabled" boolean NOT NULL DEFAULT false,
                        "TwoFactorSecret" text,
                        "TwoFactorRecoveryCodes" text,
                        "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                        "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                        "ActiveTenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001',
                        "RefreshToken" text,
                        "RefreshTokenExpiresAt" timestamp with time zone,
                        CONSTRAINT "PK_UserAccounts" PRIMARY KEY ("Id")
                    );

                    ALTER TABLE "UserAccounts" ADD COLUMN IF NOT EXISTS "ActiveTenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';
                    ALTER TABLE "UserAccounts" ADD COLUMN IF NOT EXISTS "RefreshToken" text;
                    ALTER TABLE "UserAccounts" ADD COLUMN IF NOT EXISTS "RefreshTokenExpiresAt" timestamp with time zone;

                    CREATE TABLE IF NOT EXISTS "TenantMemberships" (
                        "Id" uuid NOT NULL,
                        "UserId" uuid NOT NULL,
                        "TenantId" uuid NOT NULL,
                        "RoleInTenant" text NOT NULL DEFAULT 'Learner',
                        "OrgUnitId" uuid,
                        "IsDefault" boolean NOT NULL DEFAULT false,
                        "IsActive" boolean NOT NULL DEFAULT true,
                        "JoinedAt" timestamp with time zone NOT NULL DEFAULT now(),
                        "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                        CONSTRAINT "PK_TenantMemberships" PRIMARY KEY ("Id")
                    );

                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserAccounts_Email" ON "UserAccounts" ("Email");
                    CREATE INDEX IF NOT EXISTS "IX_UserAccounts_TenantId" ON "UserAccounts" ("TenantId");
                    CREATE INDEX IF NOT EXISTS "IX_UserAccounts_OrgUnitId" ON "UserAccounts" ("OrgUnitId");
                    CREATE INDEX IF NOT EXISTS "IX_UserAccounts_Role" ON "UserAccounts" ("Role");
                    CREATE INDEX IF NOT EXISTS "IX_UserAccounts_SubscriptionTier" ON "UserAccounts" ("SubscriptionTier");

                    CREATE INDEX IF NOT EXISTS "IX_TenantMemberships_TenantId" ON "TenantMemberships" ("TenantId");
                    CREATE INDEX IF NOT EXISTS "IX_TenantMemberships_UserId" ON "TenantMemberships" ("UserId");
                    CREATE INDEX IF NOT EXISTS "IX_TenantMemberships_OrgUnitId" ON "TenantMemberships" ("OrgUnitId");
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_TenantMemberships_UserId_TenantId" ON "TenantMemberships" ("UserId", "TenantId");

                    -- SEED Community Tenant
                    INSERT INTO "Tenants" (
                        "Id", "Code", "Name", "Plan", "ScaleType",
                        "IsCommunityHub", "MaxUsers", "IsActive", "CreatedAt",
                        "PrimaryColor"
                    ) VALUES (
                        '00000000-0000-0000-0000-000000000001',
                        'community',
                        'AegisQuiz Community Commons',
                        4,
                        6,
                        true,
                        2147483647,
                        true,
                        NOW(),
                        '#3B82F6'
                    ) ON CONFLICT ("Id") DO UPDATE SET "IsCommunityHub" = true;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917152701_CommunityFirstTenancy_v2_6') THEN
    CREATE INDEX "IX_Tenants_IsCommunityHub" ON "Tenants" ("IsCommunityHub");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260917152701_CommunityFirstTenancy_v2_6') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260917152701_CommunityFirstTenancy_v2_6', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Tenants" ADD "IsCommunityHub" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Questions" ALTER COLUMN "QuestionType" TYPE character varying(21);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Questions" ADD "CreatedBy" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Questions" ADD "IsPublic" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "OrganizationUnits" ADD "DepthLevel" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "OrganizationUnits" ADD "TaxonomyCategory" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "BankTopicId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "CognitiveStatsJson" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "ConflictReportJson" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "CustomPersonaPrompt" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "Description" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "DomainCode" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "ErrorMessage" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "FileSizeBytes" bigint NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "OrgUnitId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "OriginalFileName" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "PersonaName" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "PersonaRole" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "StoragePath" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "TenantId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "TotalPages" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "Notebooks" ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "NotebookChapters" ADD "EndPage" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "NotebookChapters" ADD "PinpointCoordinatesJson" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "NotebookChapters" ADD "QuestionCount" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "NotebookChapters" ADD "SourceIds" uuid[] NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    ALTER TABLE "NotebookChapters" ADD "StartPage" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "AiModelRegistries" (
        "Id" uuid NOT NULL,
        "ModelCode" text NOT NULL,
        "DisplayName" text NOT NULL,
        "ProviderCompany" text NOT NULL,
        "ProtocolType" integer NOT NULL,
        "CognitiveArchetype" integer NOT NULL,
        "EndpointUrl" text NOT NULL,
        "ModelName" text NOT NULL,
        "MaxContextTokens" integer NOT NULL,
        "CostInputPerMillion" double precision NOT NULL,
        "CostOutputPerMillion" double precision NOT NULL,
        "BenchmarkElo" integer NOT NULL,
        "AvgLatencyMs" integer NOT NULL,
        "IsEnabled" boolean NOT NULL,
        "IsDefaultInArchetype" boolean NOT NULL,
        "TenantId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_AiModelRegistries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "ConceptRelationships" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "SourceConceptCode" text NOT NULL,
        "TargetConceptCode" text NOT NULL,
        "RelationshipType" text NOT NULL,
        "Weight" double precision NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ConceptRelationships" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "EducatorPartnerships" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "ReferralCode" text NOT NULL,
        "ReferredByUserId" uuid,
        "TrialExpiresAt" timestamp with time zone NOT NULL,
        "RevenueShareRate" numeric NOT NULL,
        "KarmaPoints" integer NOT NULL,
        "PayoutBankAccount" text,
        "PayoutBankName" text,
        "PayoutAccountHolder" text,
        "TotalEarnings" numeric NOT NULL,
        "PendingPayoutBalance" numeric NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_EducatorPartnerships" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "ExamAuditLogs" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "ExamId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Action" text NOT NULL,
        "Severity" text NOT NULL,
        "Details" text NOT NULL,
        "ClientIp" text NOT NULL,
        "UserAgent" text NOT NULL,
        "SequenceNumber" bigint NOT NULL,
        "PreviousLogHash" text NOT NULL,
        "CurrentHash" text NOT NULL,
        "TimestampUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ExamAuditLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "KnowledgeConcepts" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "ConceptCode" text NOT NULL,
        "Name" text NOT NULL,
        "DomainCode" text NOT NULL,
        "DifficultyRank" integer NOT NULL,
        "Description" text NOT NULL,
        "CurriculumReference" text,
        "PrerequisiteCodes" text[] NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_KnowledgeConcepts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "NotebookChatMessages" (
        "Id" uuid NOT NULL,
        "NotebookId" uuid NOT NULL,
        "Sender" text NOT NULL,
        "RoleName" text NOT NULL,
        "Message" text NOT NULL,
        "CitationsJson" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_NotebookChatMessages" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_NotebookChatMessages_Notebooks_NotebookId" FOREIGN KEY ("NotebookId") REFERENCES "Notebooks" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "NotebookSources" (
        "Id" uuid NOT NULL,
        "NotebookId" uuid NOT NULL,
        "Title" text NOT NULL,
        "OriginalFileName" text NOT NULL,
        "StoragePath" text NOT NULL,
        "SourceUrl" text NOT NULL,
        "FileSizeBytes" bigint NOT NULL,
        "TotalPages" integer NOT NULL,
        "Type" integer NOT NULL,
        "Authority" integer NOT NULL,
        "Status" integer NOT NULL,
        "ExtractedText" text NOT NULL,
        "MetadataJson" text NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_NotebookSources" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_NotebookSources_Notebooks_NotebookId" FOREIGN KEY ("NotebookId") REFERENCES "Notebooks" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "QuestionParsingRules" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "RuleCode" text NOT NULL,
        "RuleName" text NOT NULL,
        "Description" text NOT NULL,
        "Priority" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "Scope" text NOT NULL,
        "AnchorSpecJson" text NOT NULL,
        "InteractionSpecJson" text NOT NULL,
        "EvaluationSpecJson" text NOT NULL,
        "SemanticsSpecJson" text NOT NULL,
        "HitCount" bigint NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_QuestionParsingRules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "SymposiumPromptTemplates" (
        "Id" uuid NOT NULL,
        "TemplateCode" text NOT NULL,
        "Layer" integer NOT NULL,
        "TargetKey" text NOT NULL,
        "Locale" text NOT NULL,
        "Version" integer NOT NULL,
        "Body" text NOT NULL,
        "Description" text NOT NULL,
        "TenantId" uuid,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_SymposiumPromptTemplates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "SymposiumSessions" (
        "Id" uuid NOT NULL,
        "TopicTitle" text NOT NULL,
        "CoreProblemStatement" text NOT NULL,
        "ParticipatingCouncilCodesJson" text NOT NULL,
        "DialogueLogJson" text NOT NULL,
        "FinalConsensusResolution" text NOT NULL,
        "EpistemicConsensusIndex" double precision NOT NULL,
        "RubricScoresJson" text NOT NULL,
        "ConvenedAt" timestamp with time zone NOT NULL,
        "ConcludedAt" timestamp with time zone,
        "InitiatedByUserId" uuid,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SymposiumSessions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "UniversalAiCouncils" (
        "Id" uuid NOT NULL,
        "CouncilCode" text NOT NULL,
        "DisplayName" text NOT NULL,
        "Category" integer NOT NULL,
        "DisciplineArea" text NOT NULL,
        "MaterializedTaxonomyPath" text NOT NULL,
        "EpistemicManifesto" text NOT NULL,
        "TriggerKeywordsJson" text NOT NULL,
        "BenchmarkPrestigeElo" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "SpatioTemporalAffinity" text NOT NULL,
        "ReadinessStatus" text NOT NULL,
        "ReadySeatsCount" integer NOT NULL,
        "TotalSeatsCount" integer NOT NULL,
        "ReadinessMessage" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_UniversalAiCouncils" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "UserAccounts" (
        "Id" uuid NOT NULL,
        "Email" text NOT NULL,
        "PasswordHash" text NOT NULL,
        "FullName" text NOT NULL,
        "PhoneNumber" text,
        "AvatarUrl" text,
        "Role" text NOT NULL,
        "TenantId" uuid NOT NULL,
        "OrgUnitId" uuid,
        "IsPremium" boolean NOT NULL,
        "SubscriptionTier" text NOT NULL,
        "SubscriptionExpiresAt" timestamp with time zone,
        "IsActive" boolean NOT NULL,
        "FailedLoginAttempts" integer NOT NULL,
        "LockoutEnd" timestamp with time zone,
        "LastLoginAt" timestamp with time zone,
        "PasswordResetToken" text,
        "PasswordResetTokenExpiresAt" timestamp with time zone,
        "IsTwoFactorEnabled" boolean NOT NULL,
        "TwoFactorSecret" text,
        "TwoFactorRecoveryCodes" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "ActiveTenantId" uuid NOT NULL,
        "RefreshToken" text,
        "RefreshTokenExpiresAt" timestamp with time zone,
        CONSTRAINT "PK_UserAccounts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserAccounts_OrganizationUnits_OrgUnitId" FOREIGN KEY ("OrgUnitId") REFERENCES "OrganizationUnits" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "UserAiKeys" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "KeyLabel" text NOT NULL,
        "ProviderCompany" text NOT NULL,
        "ProtocolType" integer NOT NULL,
        "EncryptedApiKey" text NOT NULL,
        "KeyFingerprint" text NOT NULL,
        "CustomEndpoint" text,
        "CustomModelName" text,
        "Scope" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "MonthlyTokenQuota" bigint NOT NULL,
        "TokensUsedThisMonth" bigint NOT NULL,
        "TotalCallsCount" bigint NOT NULL,
        "SuccessCallsCount" bigint NOT NULL,
        "FailedCallsCount" bigint NOT NULL,
        "LastUsedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_UserAiKeys" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "UniversalCouncilSeats" (
        "Id" uuid NOT NULL,
        "CouncilId" uuid NOT NULL,
        "SeatTitle" text NOT NULL,
        "Role" integer NOT NULL,
        "IdealArchetype" integer NOT NULL,
        "PreferredModelCode" text NOT NULL,
        "AssignedModelCode" text NOT NULL,
        "AssignmentRationale" text NOT NULL,
        "AlphaModelCode" text NOT NULL,
        "SwarmModelCodesJson" text NOT NULL,
        "SwarmPolicy" text NOT NULL,
        "IsReady" boolean NOT NULL,
        "DecisionWeight" double precision NOT NULL,
        "SortOrder" integer NOT NULL,
        "PersonaPromptDirective" text NOT NULL,
        CONSTRAINT "PK_UniversalCouncilSeats" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UniversalCouncilSeats_UniversalAiCouncils_CouncilId" FOREIGN KEY ("CouncilId") REFERENCES "UniversalAiCouncils" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "AegisCoinWallets" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Balance" bigint NOT NULL,
        "LifetimeEarned" bigint NOT NULL,
        "LifetimeSpent" bigint NOT NULL,
        "LastEarnedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AegisCoinWallets" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AegisCoinWallets_UserAccounts_UserId" FOREIGN KEY ("UserId") REFERENCES "UserAccounts" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "QuestionNeuralStates" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "QuestionId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Difficulty" double precision NOT NULL,
        "Stability" double precision NOT NULL,
        "Retrievability" double precision NOT NULL,
        "ElapsedDays" integer NOT NULL,
        "ScheduledDays" integer NOT NULL,
        "Reps" integer NOT NULL,
        "Lapses" integer NOT NULL,
        "State" integer NOT NULL,
        "LastRating" integer NOT NULL,
        "LastReviewAt" timestamp with time zone,
        "DueAt" timestamp with time zone NOT NULL,
        "AverageResponseMs" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_QuestionNeuralStates" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_QuestionNeuralStates_Questions_QuestionId" FOREIGN KEY ("QuestionId") REFERENCES "Questions" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_QuestionNeuralStates_UserAccounts_UserId" FOREIGN KEY ("UserId") REFERENCES "UserAccounts" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "TenantMemberships" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "RoleInTenant" text NOT NULL,
        "OrgUnitId" uuid,
        "IsDefault" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "JoinedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TenantMemberships" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TenantMemberships_OrganizationUnits_OrgUnitId" FOREIGN KEY ("OrgUnitId") REFERENCES "OrganizationUnits" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_TenantMemberships_Tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES "Tenants" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_TenantMemberships_UserAccounts_UserId" FOREIGN KEY ("UserId") REFERENCES "UserAccounts" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE TABLE "AegisCoinTransactions" (
        "Id" uuid NOT NULL,
        "WalletId" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Amount" bigint NOT NULL,
        "BalanceAfter" bigint NOT NULL,
        "TransactionType" text NOT NULL,
        "Description" text NOT NULL,
        "ReferenceId" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AegisCoinTransactions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AegisCoinTransactions_AegisCoinWallets_WalletId" FOREIGN KEY ("WalletId") REFERENCES "AegisCoinWallets" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    UPDATE "DynamicDomains" SET "CreatedAt" = TIMESTAMPTZ '2026-10-22T00:12:24.37634Z'
    WHERE "Code" = 'BANKING';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    UPDATE "DynamicDomains" SET "CreatedAt" = TIMESTAMPTZ '2026-10-22T00:12:24.376119Z'
    WHERE "Code" = 'EDUCATION';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    UPDATE "DynamicDomains" SET "CreatedAt" = TIMESTAMPTZ '2026-10-22T00:12:24.376341Z'
    WHERE "Code" = 'GENERAL';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    UPDATE "DynamicDomains" SET "CreatedAt" = TIMESTAMPTZ '2026-10-22T00:12:24.376341Z'
    WHERE "Code" = 'GOV_DRIVING';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    UPDATE "DynamicDomains" SET "CreatedAt" = TIMESTAMPTZ '2026-10-22T00:12:24.376341Z'
    WHERE "Code" = 'HEALTHCARE';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    UPDATE "DynamicDomains" SET "CreatedAt" = TIMESTAMPTZ '2026-10-22T00:12:24.376341Z'
    WHERE "Code" = 'HSE';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    UPDATE "DynamicDomains" SET "CreatedAt" = TIMESTAMPTZ '2026-10-22T00:12:24.376341Z'
    WHERE "Code" = 'IT_SECURITY';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_Notebooks_CreatedAt" ON "Notebooks" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_Notebooks_DomainCode" ON "Notebooks" ("DomainCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_Notebooks_Status" ON "Notebooks" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_Notebooks_TenantId" ON "Notebooks" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_NotebookChapters_NotebookId_OrderIndex" ON "NotebookChapters" ("NotebookId", "OrderIndex");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_AegisCoinTransactions_CreatedAt" ON "AegisCoinTransactions" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_AegisCoinTransactions_UserId" ON "AegisCoinTransactions" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_AegisCoinTransactions_WalletId" ON "AegisCoinTransactions" ("WalletId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_AegisCoinWallets_TenantId" ON "AegisCoinWallets" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE UNIQUE INDEX "IX_AegisCoinWallets_UserId" ON "AegisCoinWallets" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_AiModelRegistries_CognitiveArchetype" ON "AiModelRegistries" ("CognitiveArchetype");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_AiModelRegistries_IsEnabled" ON "AiModelRegistries" ("IsEnabled");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE UNIQUE INDEX "IX_AiModelRegistries_ModelCode" ON "AiModelRegistries" ("ModelCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE UNIQUE INDEX "IX_ConceptRelationships_SourceConceptCode_TargetConceptCode" ON "ConceptRelationships" ("SourceConceptCode", "TargetConceptCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_ConceptRelationships_TenantId" ON "ConceptRelationships" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_ExamAuditLogs_ExamId" ON "ExamAuditLogs" ("ExamId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE UNIQUE INDEX "IX_ExamAuditLogs_ExamId_SequenceNumber" ON "ExamAuditLogs" ("ExamId", "SequenceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_ExamAuditLogs_ExamId_UserId" ON "ExamAuditLogs" ("ExamId", "UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_ExamAuditLogs_TenantId" ON "ExamAuditLogs" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_ExamAuditLogs_TimestampUtc" ON "ExamAuditLogs" ("TimestampUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE UNIQUE INDEX "IX_KnowledgeConcepts_ConceptCode" ON "KnowledgeConcepts" ("ConceptCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_KnowledgeConcepts_DomainCode" ON "KnowledgeConcepts" ("DomainCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_KnowledgeConcepts_TenantId" ON "KnowledgeConcepts" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_NotebookChatMessages_CreatedAt" ON "NotebookChatMessages" ("CreatedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_NotebookChatMessages_NotebookId" ON "NotebookChatMessages" ("NotebookId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_NotebookSources_Authority" ON "NotebookSources" ("Authority");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_NotebookSources_NotebookId" ON "NotebookSources" ("NotebookId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_NotebookSources_Type" ON "NotebookSources" ("Type");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_QuestionNeuralStates_QuestionId" ON "QuestionNeuralStates" ("QuestionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_QuestionNeuralStates_TenantId" ON "QuestionNeuralStates" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_QuestionNeuralStates_UserId_DueAt" ON "QuestionNeuralStates" ("UserId", "DueAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE UNIQUE INDEX "IX_QuestionNeuralStates_UserId_QuestionId" ON "QuestionNeuralStates" ("UserId", "QuestionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_QuestionParsingRules_IsActive" ON "QuestionParsingRules" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_QuestionParsingRules_Priority" ON "QuestionParsingRules" ("Priority");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE UNIQUE INDEX "IX_QuestionParsingRules_TenantId_RuleCode" ON "QuestionParsingRules" ("TenantId", "RuleCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_TenantMemberships_OrgUnitId" ON "TenantMemberships" ("OrgUnitId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_TenantMemberships_TenantId" ON "TenantMemberships" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_TenantMemberships_UserId" ON "TenantMemberships" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE UNIQUE INDEX "IX_TenantMemberships_UserId_TenantId" ON "TenantMemberships" ("UserId", "TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_UniversalCouncilSeats_CouncilId" ON "UniversalCouncilSeats" ("CouncilId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE UNIQUE INDEX "IX_UserAccounts_Email" ON "UserAccounts" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_UserAccounts_OrgUnitId" ON "UserAccounts" ("OrgUnitId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_UserAccounts_Role" ON "UserAccounts" ("Role");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_UserAccounts_SubscriptionTier" ON "UserAccounts" ("SubscriptionTier");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_UserAccounts_TenantId" ON "UserAccounts" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_UserAiKeys_IsActive" ON "UserAiKeys" ("IsActive");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_UserAiKeys_Scope" ON "UserAiKeys" ("Scope");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_UserAiKeys_TenantId" ON "UserAiKeys" ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    CREATE INDEX "IX_UserAiKeys_UserId" ON "UserAiKeys" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022001225_AddCreatorEconomyAndEnterprisePanopticon') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261022001225_AddCreatorEconomyAndEnterprisePanopticon', '10.0.5');
    END IF;
END $EF$;
COMMIT;

