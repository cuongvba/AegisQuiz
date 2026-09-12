using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisQuiz.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionMediaTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Questions",
                type: "text",
                nullable: false,
                defaultValue: "text");

            migrationBuilder.AddColumn<string>(
                name: "OptionType",
                table: "Questions",
                type: "text",
                nullable: false,
                defaultValue: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "OptionType",
                table: "Questions");
        }
    }
}
