using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisQuiz.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionContextAndMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Questions");

            migrationBuilder.AddColumn<Guid>(
                name: "ContextId",
                table: "Questions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DomainCode",
                table: "Questions",
                type: "text",
                nullable: false,
                defaultValue: "EDUCATION");

            migrationBuilder.AddColumn<bool>(
                name: "IsCritical",
                table: "Questions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SubCategory",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "Questions",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.CreateTable(
                name: "QuestionContexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    MediaUrl = table.Column<string>(type: "text", nullable: true),
                    ContentHash = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionContexts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ContextId",
                table: "Questions",
                column: "ContextId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_DomainCode",
                table: "Questions",
                column: "DomainCode");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionContexts_ContentHash",
                table: "QuestionContexts",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionContexts_TenantId",
                table: "QuestionContexts",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_QuestionContexts_ContextId",
                table: "Questions",
                column: "ContextId",
                principalTable: "QuestionContexts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_QuestionContexts_ContextId",
                table: "Questions");

            migrationBuilder.DropTable(
                name: "QuestionContexts");

            migrationBuilder.DropIndex(
                name: "IX_Questions_ContextId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_DomainCode",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "DomainCode",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "IsCritical",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "SubCategory",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ContextId",
                table: "Questions");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Questions",
                type: "uuid",
                nullable: true);
        }
    }
}
