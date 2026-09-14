using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AegisQuiz.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHierarchyAndDynamicDomains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepthLevel",
                table: "BankTopics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "BankTopics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DomainCode",
                table: "BankTopics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaterializedPath",
                table: "BankTopics",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuestionCountCached",
                table: "BankTopics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "BankTopics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Urn",
                table: "BankTopics",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DynamicDomains",
                columns: table => new
                {
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    ColorBadge = table.Column<string>(type: "text", nullable: false),
                    IsSystemStandard = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentDomainCode = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicDomains", x => x.Code);
                    table.ForeignKey(
                        name: "FK_DynamicDomains_DynamicDomains_ParentDomainCode",
                        column: x => x.ParentDomainCode,
                        principalTable: "DynamicDomains",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UnitType = table.Column<int>(type: "integer", nullable: false),
                    HierarchyPath = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationUnits_OrganizationUnits_ParentId",
                        column: x => x.ParentId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantCoordinatePresets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainCode = table.Column<string>(type: "text", nullable: false),
                    CoordinateType = table.Column<string>(type: "text", nullable: false),
                    PresetCode = table.Column<string>(type: "text", nullable: false),
                    PresetLabel = table.Column<string>(type: "text", nullable: false),
                    SynonymsJson = table.Column<string>(type: "text", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantCoordinatePresets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    PrimaryColor = table.Column<string>(type: "text", nullable: true),
                    Domain = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Plan = table.Column<int>(type: "integer", nullable: false),
                    ScaleType = table.Column<int>(type: "integer", nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    KeycloakRealmId = table.Column<string>(type: "text", nullable: true),
                    ConfigJson = table.Column<string>(type: "text", nullable: true),
                    FeatureFlagsJson = table.Column<string>(type: "text", nullable: true),
                    CustomDomain = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantDomainConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DomainCode = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CustomDisplayName = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDomainConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantDomainConfigs_DynamicDomains_DomainCode",
                        column: x => x.DomainCode,
                        principalTable: "DynamicDomains",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    OrgUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_OrganizationUnits_OrgUnitId",
                        column: x => x.OrgUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "DynamicDomains",
                columns: new[] { "Code", "ColorBadge", "CreatedAt", "Description", "DisplayOrder", "Icon", "IsActive", "IsSystemStandard", "Name", "ParentDomainCode", "TenantId" },
                values: new object[,]
                {
                    { "BANKING", "#059669", new DateTime(2026, 9, 12, 11, 22, 33, 216, DateTimeKind.Utc).AddTicks(7615), "Nghiệp vụ tín dụng, thanh toán, ngân quỹ, quản trị rủi ro", 2, "Landmark", true, true, "Tài chính - Ngân hàng", null, null },
                    { "EDUCATION", "#2563eb", new DateTime(2026, 9, 12, 11, 22, 33, 215, DateTimeKind.Utc).AddTicks(7120), "Khảo thí đại học, phổ thông, học thuật tổng quát", 1, "GraduationCap", true, true, "Giáo dục & Học thuật", null, null },
                    { "GENERAL", "#4b5563", new DateTime(2026, 9, 12, 11, 22, 33, 216, DateTimeKind.Utc).AddTicks(7700), "Kiến thức đại cương, kỹ năng mềm, văn hóa doanh nghiệp", 7, "Layers", true, true, "Tổng hợp / Đại cương", null, null },
                    { "GOV_DRIVING", "#7c3aed", new DateTime(2026, 9, 12, 11, 22, 33, 216, DateTimeKind.Utc).AddTicks(7690), "Bộ 600 câu GPLX Bộ GTVT, 60 câu điểm liệt, sa hình AI", 5, "Car", true, true, "Sát hạch Lái xe Quốc gia", null, null },
                    { "HEALTHCARE", "#dc2626", new DateTime(2026, 9, 12, 11, 22, 33, 216, DateTimeKind.Utc).AddTicks(7680), "Y học, dược lâm sàng, quy trình điều dưỡng, kiểm soát nhiễm khuẩn", 3, "HeartPulse", true, true, "Y tế - Sức khỏe", null, null },
                    { "HSE", "#d97706", new DateTime(2026, 9, 12, 11, 22, 33, 216, DateTimeKind.Utc).AddTicks(7687), "An toàn vệ sinh lao động, PCCC, quy chuẩn ISO 45001", 4, "HardHat", true, true, "An toàn - Môi trường LĐ", null, null },
                    { "IT_SECURITY", "#0284c7", new DateTime(2026, 9, 12, 11, 22, 33, 216, DateTimeKind.Utc).AddTicks(7696), "Bảo mật an ninh mạng, kiến trúc hệ thống, chứng chỉ CISSP/CompTIA", 6, "ShieldCheck", true, true, "An toàn TT & CNTT", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankTopics_DomainCode",
                table: "BankTopics",
                column: "DomainCode");

            migrationBuilder.CreateIndex(
                name: "IX_BankTopics_MaterializedPath",
                table: "BankTopics",
                column: "MaterializedPath");

            migrationBuilder.CreateIndex(
                name: "IX_BankTopics_Scope",
                table: "BankTopics",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_BankTopics_TenantId",
                table: "BankTopics",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicDomains_IsSystemStandard",
                table: "DynamicDomains",
                column: "IsSystemStandard");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicDomains_ParentDomainCode",
                table: "DynamicDomains",
                column: "ParentDomainCode");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicDomains_TenantId",
                table: "DynamicDomains",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_ParentId",
                table: "OrganizationUnits",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_TenantId",
                table: "OrganizationUnits",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_TenantId_Code",
                table: "OrganizationUnits",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantCoordinatePresets_TenantId",
                table: "TenantCoordinatePresets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantCoordinatePresets_TenantId_DomainCode_CoordinateType",
                table: "TenantCoordinatePresets",
                columns: new[] { "TenantId", "DomainCode", "CoordinateType" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomainConfigs_DomainCode",
                table: "TenantDomainConfigs",
                column: "DomainCode");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomainConfigs_TenantId",
                table: "TenantDomainConfigs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomainConfigs_TenantId_DomainCode",
                table: "TenantDomainConfigs",
                columns: new[] { "TenantId", "DomainCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_OrgUnitId",
                table: "UserRoles",
                column: "OrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_Role",
                table: "UserRoles",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_TenantId_OrgUnitId",
                table: "UserRoles",
                columns: new[] { "TenantId", "OrgUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_TenantId_UserId",
                table: "UserRoles",
                columns: new[] { "TenantId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantCoordinatePresets");

            migrationBuilder.DropTable(
                name: "TenantDomainConfigs");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "DynamicDomains");

            migrationBuilder.DropTable(
                name: "OrganizationUnits");

            migrationBuilder.DropIndex(
                name: "IX_BankTopics_DomainCode",
                table: "BankTopics");

            migrationBuilder.DropIndex(
                name: "IX_BankTopics_MaterializedPath",
                table: "BankTopics");

            migrationBuilder.DropIndex(
                name: "IX_BankTopics_Scope",
                table: "BankTopics");

            migrationBuilder.DropIndex(
                name: "IX_BankTopics_TenantId",
                table: "BankTopics");

            migrationBuilder.DropColumn(
                name: "DepthLevel",
                table: "BankTopics");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "BankTopics");

            migrationBuilder.DropColumn(
                name: "DomainCode",
                table: "BankTopics");

            migrationBuilder.DropColumn(
                name: "MaterializedPath",
                table: "BankTopics");

            migrationBuilder.DropColumn(
                name: "QuestionCountCached",
                table: "BankTopics");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "BankTopics");

            migrationBuilder.DropColumn(
                name: "Urn",
                table: "BankTopics");
        }
    }
}
