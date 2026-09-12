using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisQuiz.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CategoryCode = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    VisibilityScope = table.Column<string>(type: "text", nullable: false, defaultValue: "PRIVATE"),
                    AllowedTenantIds = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTopics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamContestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantIdentifier = table.Column<string>(type: "text", nullable: false),
                    TimeStarted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TimeSubmitted = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    NumberOfTabSwitches = table.Column<int>(type: "integer", nullable: false),
                    EndReason = table.Column<string>(type: "text", nullable: true),
                    SnapshotQuestionIds = table.Column<string>(type: "jsonb", nullable: true),
                    EarnedScore = table.Column<decimal>(type: "numeric", nullable: false),
                    CorrectItemCount = table.Column<int>(type: "integer", nullable: false),
                    TotalItemCount = table.Column<int>(type: "integer", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    FinalAnswers = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    AvatarInitial = table.Column<string>(type: "text", nullable: true),
                    Period = table.Column<string>(type: "text", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalScore = table.Column<double>(type: "double precision", nullable: false),
                    AverageScore = table.Column<double>(type: "double precision", nullable: false),
                    TotalAttempts = table.Column<int>(type: "integer", nullable: false),
                    CorrectCount = table.Column<int>(type: "integer", nullable: false),
                    XpTotal = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BadgeColor = table.Column<string>(type: "text", nullable: false),
                    IconEmoji = table.Column<string>(type: "text", nullable: true),
                    TriggerType = table.Column<string>(type: "text", nullable: false),
                    TriggerCondition = table.Column<string>(type: "jsonb", nullable: false),
                    XpReward = table.Column<int>(type: "integer", nullable: false),
                    TitleReward = table.Column<string>(type: "text", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningAchievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearningPaths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningPaths", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notebooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    SourcePdfUrl = table.Column<string>(type: "text", nullable: false),
                    CreatedByAdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notebooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentCode = table.Column<string>(type: "text", nullable: false),
                    AmountRequired = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.TransactionId);
                });

            migrationBuilder.CreateTable(
                name: "QuestionAttemptAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedAnswer = table.Column<string>(type: "text", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    PointsEarned = table.Column<double>(type: "double precision", nullable: false),
                    PointsMax = table.Column<double>(type: "double precision", nullable: false),
                    TimeTakenSec = table.Column<int>(type: "integer", nullable: true),
                    CategoryCode = table.Column<string>(type: "text", nullable: true),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    AttemptDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionAttemptAnswers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    SelectedAnswer = table.Column<string>(type: "text", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    TimeSpentSeconds = table.Column<int>(type: "integer", nullable: false),
                    AttemptDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    CategoryCode = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    QuestionType = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    GradingRubric = table.Column<string>(type: "text", nullable: true),
                    Options = table.Column<List<string>>(type: "text[]", nullable: true),
                    CorrectOption = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    XpEarned = table.Column<int>(type: "integer", nullable: false),
                    ContextJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonAchievements_LearningAchievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "LearningAchievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningPathProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningPathId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    CompletionPercent = table.Column<double>(type: "double precision", nullable: false),
                    AverageScore = table.Column<double>(type: "double precision", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningPathProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningPathProgresses_LearningPaths_LearningPathId",
                        column: x => x.LearningPathId,
                        principalTable: "LearningPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningPathSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningPathId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    CategoryCode = table.Column<string>(type: "text", nullable: false),
                    CategoryName = table.Column<string>(type: "text", nullable: false),
                    PassScore = table.Column<double>(type: "double precision", nullable: false),
                    LearningObjective = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningPathSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningPathSteps_LearningPaths_LearningPathId",
                        column: x => x.LearningPathId,
                        principalTable: "LearningPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotebookChapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotebookId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    RawContent = table.Column<string>(type: "text", nullable: false),
                    KeyPointsJson = table.Column<string>(type: "text", nullable: false),
                    GeneratedQuestionIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotebookChapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotebookChapters_Notebooks_NotebookId",
                        column: x => x.NotebookId,
                        principalTable: "Notebooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankTopics_CategoryCode",
                table: "BankTopics",
                column: "CategoryCode");

            migrationBuilder.CreateIndex(
                name: "IX_BankTopics_Code",
                table: "BankTopics",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_ApplicantId",
                table: "ExamSessions",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_ApplicantId_Status",
                table: "ExamSessions",
                columns: new[] { "ApplicantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSessions_Status",
                table: "ExamSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_Period_PeriodStart_Rank",
                table: "LeaderboardEntries",
                columns: new[] { "Period", "PeriodStart", "Rank" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_UserId_Period_PeriodStart",
                table: "LeaderboardEntries",
                columns: new[] { "UserId", "Period", "PeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningAchievements_Code",
                table: "LearningAchievements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathProgresses_LearningPathId",
                table: "LearningPathProgresses",
                column: "LearningPathId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathProgresses_UserId",
                table: "LearningPathProgresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathProgresses_UserId_LearningPathId",
                table: "LearningPathProgresses",
                columns: new[] { "UserId", "LearningPathId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningPaths_Code",
                table: "LearningPaths",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningPathSteps_LearningPathId_StepOrder",
                table: "LearningPathSteps",
                columns: new[] { "LearningPathId", "StepOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NotebookChapters_NotebookId",
                table: "NotebookChapters",
                column: "NotebookId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaymentCode",
                table: "PaymentTransactions",
                column: "PaymentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonAchievements_AchievementId",
                table: "PersonAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonAchievements_UserId",
                table: "PersonAchievements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonAchievements_UserId_AchievementId",
                table: "PersonAchievements",
                columns: new[] { "UserId", "AchievementId" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAttemptAnswers_AttemptId",
                table: "QuestionAttemptAnswers",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAttemptAnswers_AttemptId_CategoryCode",
                table: "QuestionAttemptAnswers",
                columns: new[] { "AttemptId", "CategoryCode" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAttemptAnswers_UserId",
                table: "QuestionAttemptAnswers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAttempts_AttemptDate",
                table: "QuestionAttempts",
                column: "AttemptDate");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAttempts_UserId_AttemptDate",
                table: "QuestionAttempts",
                columns: new[] { "UserId", "AttemptDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CategoryCode",
                table: "Questions",
                column: "CategoryCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankTopics");

            migrationBuilder.DropTable(
                name: "ExamSessions");

            migrationBuilder.DropTable(
                name: "LeaderboardEntries");

            migrationBuilder.DropTable(
                name: "LearningPathProgresses");

            migrationBuilder.DropTable(
                name: "LearningPathSteps");

            migrationBuilder.DropTable(
                name: "NotebookChapters");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "PersonAchievements");

            migrationBuilder.DropTable(
                name: "QuestionAttemptAnswers");

            migrationBuilder.DropTable(
                name: "QuestionAttempts");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "LearningPaths");

            migrationBuilder.DropTable(
                name: "Notebooks");

            migrationBuilder.DropTable(
                name: "LearningAchievements");
        }
    }
}
