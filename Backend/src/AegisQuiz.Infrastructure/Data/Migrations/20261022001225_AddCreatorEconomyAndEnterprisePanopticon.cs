using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisQuiz.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatorEconomyAndEnterprisePanopticon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCommunityHub",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "QuestionType",
                table: "Questions",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Questions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DepthLevel",
                table: "OrganizationUnits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TaxonomyCategory",
                table: "OrganizationUnits",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BankTopicId",
                table: "Notebooks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CognitiveStatsJson",
                table: "Notebooks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConflictReportJson",
                table: "Notebooks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomPersonaPrompt",
                table: "Notebooks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Notebooks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DomainCode",
                table: "Notebooks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Notebooks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "Notebooks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "OrgUnitId",
                table: "Notebooks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "Notebooks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PersonaName",
                table: "Notebooks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PersonaRole",
                table: "Notebooks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StoragePath",
                table: "Notebooks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Notebooks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "TotalPages",
                table: "Notebooks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Notebooks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndPage",
                table: "NotebookChapters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PinpointCoordinatesJson",
                table: "NotebookChapters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "QuestionCount",
                table: "NotebookChapters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "SourceIds",
                table: "NotebookChapters",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "StartPage",
                table: "NotebookChapters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AiModelRegistries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelCode = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    ProviderCompany = table.Column<string>(type: "text", nullable: false),
                    ProtocolType = table.Column<int>(type: "integer", nullable: false),
                    CognitiveArchetype = table.Column<int>(type: "integer", nullable: false),
                    EndpointUrl = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    MaxContextTokens = table.Column<int>(type: "integer", nullable: false),
                    CostInputPerMillion = table.Column<double>(type: "double precision", nullable: false),
                    CostOutputPerMillion = table.Column<double>(type: "double precision", nullable: false),
                    BenchmarkElo = table.Column<int>(type: "integer", nullable: false),
                    AvgLatencyMs = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefaultInArchetype = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiModelRegistries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConceptRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceConceptCode = table.Column<string>(type: "text", nullable: false),
                    TargetConceptCode = table.Column<string>(type: "text", nullable: false),
                    RelationshipType = table.Column<string>(type: "text", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptRelationships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EducatorPartnerships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferralCode = table.Column<string>(type: "text", nullable: false),
                    ReferredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TrialExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevenueShareRate = table.Column<decimal>(type: "numeric", nullable: false),
                    KarmaPoints = table.Column<int>(type: "integer", nullable: false),
                    PayoutBankAccount = table.Column<string>(type: "text", nullable: true),
                    PayoutBankName = table.Column<string>(type: "text", nullable: true),
                    PayoutAccountHolder = table.Column<string>(type: "text", nullable: true),
                    TotalEarnings = table.Column<decimal>(type: "numeric", nullable: false),
                    PendingPayoutBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducatorPartnerships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    ClientIp = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    PreviousLogHash = table.Column<string>(type: "text", nullable: false),
                    CurrentHash = table.Column<string>(type: "text", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeConcepts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConceptCode = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DomainCode = table.Column<string>(type: "text", nullable: false),
                    DifficultyRank = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CurriculumReference = table.Column<string>(type: "text", nullable: true),
                    PrerequisiteCodes = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeConcepts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotebookChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotebookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sender = table.Column<string>(type: "text", nullable: false),
                    RoleName = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CitationsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotebookChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotebookChatMessages_Notebooks_NotebookId",
                        column: x => x.NotebookId,
                        principalTable: "Notebooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotebookSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotebookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    TotalPages = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Authority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExtractedText = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotebookSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotebookSources_Notebooks_NotebookId",
                        column: x => x.NotebookId,
                        principalTable: "Notebooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionParsingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleCode = table.Column<string>(type: "text", nullable: false),
                    RuleName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Scope = table.Column<string>(type: "text", nullable: false),
                    AnchorSpecJson = table.Column<string>(type: "text", nullable: false),
                    InteractionSpecJson = table.Column<string>(type: "text", nullable: false),
                    EvaluationSpecJson = table.Column<string>(type: "text", nullable: false),
                    SemanticsSpecJson = table.Column<string>(type: "text", nullable: false),
                    HitCount = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionParsingRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SymposiumPromptTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateCode = table.Column<string>(type: "text", nullable: false),
                    Layer = table.Column<int>(type: "integer", nullable: false),
                    TargetKey = table.Column<string>(type: "text", nullable: false),
                    Locale = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymposiumPromptTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SymposiumSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicTitle = table.Column<string>(type: "text", nullable: false),
                    CoreProblemStatement = table.Column<string>(type: "text", nullable: false),
                    ParticipatingCouncilCodesJson = table.Column<string>(type: "text", nullable: false),
                    DialogueLogJson = table.Column<string>(type: "text", nullable: false),
                    FinalConsensusResolution = table.Column<string>(type: "text", nullable: false),
                    EpistemicConsensusIndex = table.Column<double>(type: "double precision", nullable: false),
                    RubricScoresJson = table.Column<string>(type: "text", nullable: false),
                    ConvenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConcludedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InitiatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymposiumSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UniversalAiCouncils",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CouncilCode = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    DisciplineArea = table.Column<string>(type: "text", nullable: false),
                    MaterializedTaxonomyPath = table.Column<string>(type: "text", nullable: false),
                    EpistemicManifesto = table.Column<string>(type: "text", nullable: false),
                    TriggerKeywordsJson = table.Column<string>(type: "text", nullable: false),
                    BenchmarkPrestigeElo = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SpatioTemporalAffinity = table.Column<string>(type: "text", nullable: false),
                    ReadinessStatus = table.Column<string>(type: "text", nullable: false),
                    ReadySeatsCount = table.Column<int>(type: "integer", nullable: false),
                    TotalSeatsCount = table.Column<int>(type: "integer", nullable: false),
                    ReadinessMessage = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniversalAiCouncils", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrgUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPremium = table.Column<bool>(type: "boolean", nullable: false),
                    SubscriptionTier = table.Column<string>(type: "text", nullable: false),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "text", nullable: true),
                    PasswordResetTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsTwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorSecret = table.Column<string>(type: "text", nullable: true),
                    TwoFactorRecoveryCodes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActiveTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccounts_OrganizationUnits_OrgUnitId",
                        column: x => x.OrgUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserAiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyLabel = table.Column<string>(type: "text", nullable: false),
                    ProviderCompany = table.Column<string>(type: "text", nullable: false),
                    ProtocolType = table.Column<int>(type: "integer", nullable: false),
                    EncryptedApiKey = table.Column<string>(type: "text", nullable: false),
                    KeyFingerprint = table.Column<string>(type: "text", nullable: false),
                    CustomEndpoint = table.Column<string>(type: "text", nullable: true),
                    CustomModelName = table.Column<string>(type: "text", nullable: true),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MonthlyTokenQuota = table.Column<long>(type: "bigint", nullable: false),
                    TokensUsedThisMonth = table.Column<long>(type: "bigint", nullable: false),
                    TotalCallsCount = table.Column<long>(type: "bigint", nullable: false),
                    SuccessCallsCount = table.Column<long>(type: "bigint", nullable: false),
                    FailedCallsCount = table.Column<long>(type: "bigint", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UniversalCouncilSeats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CouncilId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatTitle = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IdealArchetype = table.Column<int>(type: "integer", nullable: false),
                    PreferredModelCode = table.Column<string>(type: "text", nullable: false),
                    AssignedModelCode = table.Column<string>(type: "text", nullable: false),
                    AssignmentRationale = table.Column<string>(type: "text", nullable: false),
                    AlphaModelCode = table.Column<string>(type: "text", nullable: false),
                    SwarmModelCodesJson = table.Column<string>(type: "text", nullable: false),
                    SwarmPolicy = table.Column<string>(type: "text", nullable: false),
                    IsReady = table.Column<bool>(type: "boolean", nullable: false),
                    DecisionWeight = table.Column<double>(type: "double precision", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    PersonaPromptDirective = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniversalCouncilSeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UniversalCouncilSeats_UniversalAiCouncils_CouncilId",
                        column: x => x.CouncilId,
                        principalTable: "UniversalAiCouncils",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AegisCoinWallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<long>(type: "bigint", nullable: false),
                    LifetimeEarned = table.Column<long>(type: "bigint", nullable: false),
                    LifetimeSpent = table.Column<long>(type: "bigint", nullable: false),
                    LastEarnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AegisCoinWallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AegisCoinWallets_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionNeuralStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Difficulty = table.Column<double>(type: "double precision", nullable: false),
                    Stability = table.Column<double>(type: "double precision", nullable: false),
                    Retrievability = table.Column<double>(type: "double precision", nullable: false),
                    ElapsedDays = table.Column<int>(type: "integer", nullable: false),
                    ScheduledDays = table.Column<int>(type: "integer", nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: false),
                    Lapses = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    LastRating = table.Column<int>(type: "integer", nullable: false),
                    LastReviewAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AverageResponseMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionNeuralStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionNeuralStates_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestionNeuralStates_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleInTenant = table.Column<string>(type: "text", nullable: false),
                    OrgUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantMemberships_OrganizationUnits_OrgUnitId",
                        column: x => x.OrgUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TenantMemberships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantMemberships_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AegisCoinTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    BalanceAfter = table.Column<long>(type: "bigint", nullable: false),
                    TransactionType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ReferenceId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AegisCoinTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AegisCoinTransactions_AegisCoinWallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "AegisCoinWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "BANKING",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 22, 0, 12, 24, 376, DateTimeKind.Utc).AddTicks(3407));

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "EDUCATION",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 22, 0, 12, 24, 376, DateTimeKind.Utc).AddTicks(1196));

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "GENERAL",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 22, 0, 12, 24, 376, DateTimeKind.Utc).AddTicks(3418));

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "GOV_DRIVING",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 22, 0, 12, 24, 376, DateTimeKind.Utc).AddTicks(3416));

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "HEALTHCARE",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 22, 0, 12, 24, 376, DateTimeKind.Utc).AddTicks(3413));

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "HSE",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 22, 0, 12, 24, 376, DateTimeKind.Utc).AddTicks(3415));

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "IT_SECURITY",
                column: "CreatedAt",
                value: new DateTime(2026, 10, 22, 0, 12, 24, 376, DateTimeKind.Utc).AddTicks(3417));

            migrationBuilder.CreateIndex(
                name: "IX_Notebooks_CreatedAt",
                table: "Notebooks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notebooks_DomainCode",
                table: "Notebooks",
                column: "DomainCode");

            migrationBuilder.CreateIndex(
                name: "IX_Notebooks_Status",
                table: "Notebooks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notebooks_TenantId",
                table: "Notebooks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NotebookChapters_NotebookId_OrderIndex",
                table: "NotebookChapters",
                columns: new[] { "NotebookId", "OrderIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_AegisCoinTransactions_CreatedAt",
                table: "AegisCoinTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AegisCoinTransactions_UserId",
                table: "AegisCoinTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AegisCoinTransactions_WalletId",
                table: "AegisCoinTransactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_AegisCoinWallets_TenantId",
                table: "AegisCoinWallets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AegisCoinWallets_UserId",
                table: "AegisCoinWallets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiModelRegistries_CognitiveArchetype",
                table: "AiModelRegistries",
                column: "CognitiveArchetype");

            migrationBuilder.CreateIndex(
                name: "IX_AiModelRegistries_IsEnabled",
                table: "AiModelRegistries",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_AiModelRegistries_ModelCode",
                table: "AiModelRegistries",
                column: "ModelCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConceptRelationships_SourceConceptCode_TargetConceptCode",
                table: "ConceptRelationships",
                columns: new[] { "SourceConceptCode", "TargetConceptCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConceptRelationships_TenantId",
                table: "ConceptRelationships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAuditLogs_ExamId",
                table: "ExamAuditLogs",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAuditLogs_ExamId_SequenceNumber",
                table: "ExamAuditLogs",
                columns: new[] { "ExamId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamAuditLogs_ExamId_UserId",
                table: "ExamAuditLogs",
                columns: new[] { "ExamId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAuditLogs_TenantId",
                table: "ExamAuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAuditLogs_TimestampUtc",
                table: "ExamAuditLogs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeConcepts_ConceptCode",
                table: "KnowledgeConcepts",
                column: "ConceptCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeConcepts_DomainCode",
                table: "KnowledgeConcepts",
                column: "DomainCode");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeConcepts_TenantId",
                table: "KnowledgeConcepts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NotebookChatMessages_CreatedAt",
                table: "NotebookChatMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotebookChatMessages_NotebookId",
                table: "NotebookChatMessages",
                column: "NotebookId");

            migrationBuilder.CreateIndex(
                name: "IX_NotebookSources_Authority",
                table: "NotebookSources",
                column: "Authority");

            migrationBuilder.CreateIndex(
                name: "IX_NotebookSources_NotebookId",
                table: "NotebookSources",
                column: "NotebookId");

            migrationBuilder.CreateIndex(
                name: "IX_NotebookSources_Type",
                table: "NotebookSources",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionNeuralStates_QuestionId",
                table: "QuestionNeuralStates",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionNeuralStates_TenantId",
                table: "QuestionNeuralStates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionNeuralStates_UserId_DueAt",
                table: "QuestionNeuralStates",
                columns: new[] { "UserId", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionNeuralStates_UserId_QuestionId",
                table: "QuestionNeuralStates",
                columns: new[] { "UserId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionParsingRules_IsActive",
                table: "QuestionParsingRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionParsingRules_Priority",
                table: "QuestionParsingRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionParsingRules_TenantId_RuleCode",
                table: "QuestionParsingRules",
                columns: new[] { "TenantId", "RuleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_OrgUnitId",
                table: "TenantMemberships",
                column: "OrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_TenantId",
                table: "TenantMemberships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_UserId",
                table: "TenantMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMemberships_UserId_TenantId",
                table: "TenantMemberships",
                columns: new[] { "UserId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UniversalCouncilSeats_CouncilId",
                table: "UniversalCouncilSeats",
                column: "CouncilId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Email",
                table: "UserAccounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_OrgUnitId",
                table: "UserAccounts",
                column: "OrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Role",
                table: "UserAccounts",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_SubscriptionTier",
                table: "UserAccounts",
                column: "SubscriptionTier");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_TenantId",
                table: "UserAccounts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAiKeys_IsActive",
                table: "UserAiKeys",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserAiKeys_Scope",
                table: "UserAiKeys",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_UserAiKeys_TenantId",
                table: "UserAiKeys",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAiKeys_UserId",
                table: "UserAiKeys",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AegisCoinTransactions");

            migrationBuilder.DropTable(
                name: "AiModelRegistries");

            migrationBuilder.DropTable(
                name: "ConceptRelationships");

            migrationBuilder.DropTable(
                name: "EducatorPartnerships");

            migrationBuilder.DropTable(
                name: "ExamAuditLogs");

            migrationBuilder.DropTable(
                name: "KnowledgeConcepts");

            migrationBuilder.DropTable(
                name: "NotebookChatMessages");

            migrationBuilder.DropTable(
                name: "NotebookSources");

            migrationBuilder.DropTable(
                name: "QuestionNeuralStates");

            migrationBuilder.DropTable(
                name: "QuestionParsingRules");

            migrationBuilder.DropTable(
                name: "SymposiumPromptTemplates");

            migrationBuilder.DropTable(
                name: "SymposiumSessions");

            migrationBuilder.DropTable(
                name: "TenantMemberships");

            migrationBuilder.DropTable(
                name: "UniversalCouncilSeats");

            migrationBuilder.DropTable(
                name: "UserAiKeys");

            migrationBuilder.DropTable(
                name: "AegisCoinWallets");

            migrationBuilder.DropTable(
                name: "UniversalAiCouncils");

            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Notebooks_CreatedAt",
                table: "Notebooks");

            migrationBuilder.DropIndex(
                name: "IX_Notebooks_DomainCode",
                table: "Notebooks");

            migrationBuilder.DropIndex(
                name: "IX_Notebooks_Status",
                table: "Notebooks");

            migrationBuilder.DropIndex(
                name: "IX_Notebooks_TenantId",
                table: "Notebooks");

            migrationBuilder.DropIndex(
                name: "IX_NotebookChapters_NotebookId_OrderIndex",
                table: "NotebookChapters");

            migrationBuilder.DropColumn(
                name: "IsCommunityHub",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "DepthLevel",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "TaxonomyCategory",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "BankTopicId",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "CognitiveStatsJson",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "ConflictReportJson",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "CustomPersonaPrompt",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "DomainCode",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "OrgUnitId",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "PersonaName",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "PersonaRole",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "StoragePath",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "TotalPages",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Notebooks");

            migrationBuilder.DropColumn(
                name: "EndPage",
                table: "NotebookChapters");

            migrationBuilder.DropColumn(
                name: "PinpointCoordinatesJson",
                table: "NotebookChapters");

            migrationBuilder.DropColumn(
                name: "QuestionCount",
                table: "NotebookChapters");

            migrationBuilder.DropColumn(
                name: "SourceIds",
                table: "NotebookChapters");

            migrationBuilder.DropColumn(
                name: "StartPage",
                table: "NotebookChapters");

            migrationBuilder.AlterColumn<string>(
                name: "QuestionType",
                table: "Questions",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(21)",
                oldMaxLength: 21);

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "BANKING",
                column: "CreatedAt",
                value: new DateTime(2026, 9, 12, 11, 22, 33, 216, DateTimeKind.Utc).AddTicks(7615));

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "EDUCATION",
                column: "CreatedAt",
                value: new DateTime(2026, 9, 12, 11, 22, 33, 215, DateTimeKind.Utc).AddTicks(7120));

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "GENERAL",
                column: "CreatedAt",
                value: new DateTime(2026, 9, 12, 11, 22, 33, 216, DateTimeKind.Utc).AddTicks(7700));

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "GOV_DRIVING",
                column: "CreatedAt",
                value: new DateTime(2026, 9, 12, 11, 22, 33, 216, DateTimeKind.Utc).AddTicks(7690));

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "HEALTHCARE",
                column: "CreatedAt",
                value: new DateTime(2026, 9, 12, 11, 22, 33, 216, DateTimeKind.Utc).AddTicks(7680));

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "HSE",
                column: "CreatedAt",
                value: new DateTime(2026, 9, 12, 11, 22, 33, 216, DateTimeKind.Utc).AddTicks(7687));

            migrationBuilder.UpdateData(
                table: "DynamicDomains",
                keyColumn: "Code",
                keyValue: "IT_SECURITY",
                column: "CreatedAt",
                value: new DateTime(2026, 9, 12, 11, 22, 33, 216, DateTimeKind.Utc).AddTicks(7696));
        }
    }
}
