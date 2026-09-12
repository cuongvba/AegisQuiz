using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisQuiz.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "BankTopics",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankTopics_ParentId",
                table: "BankTopics",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankTopics_BankTopics_ParentId",
                table: "BankTopics",
                column: "ParentId",
                principalTable: "BankTopics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankTopics_BankTopics_ParentId",
                table: "BankTopics");

            migrationBuilder.DropIndex(
                name: "IX_BankTopics_ParentId",
                table: "BankTopics");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "BankTopics");
        }
    }
}
