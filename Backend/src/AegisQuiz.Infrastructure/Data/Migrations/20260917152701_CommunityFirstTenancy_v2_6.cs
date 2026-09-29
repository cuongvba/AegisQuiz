using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisQuiz.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CommunityFirstTenancy_v2_6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Tenants"" ADD COLUMN IF NOT EXISTS ""IsCommunityHub"" boolean NOT NULL DEFAULT false;
                ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""CreatedBy"" text;
                ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""IsPublic"" boolean NOT NULL DEFAULT false;
                ALTER TABLE ""OrganizationUnits"" ADD COLUMN IF NOT EXISTS ""DepthLevel"" integer NOT NULL DEFAULT 0;
                ALTER TABLE ""OrganizationUnits"" ADD COLUMN IF NOT EXISTS ""TaxonomyCategory"" text;

                CREATE TABLE IF NOT EXISTS ""UserAccounts"" (
                    ""Id"" uuid NOT NULL,
                    ""Email"" text NOT NULL,
                    ""PasswordHash"" text NOT NULL,
                    ""FullName"" text NOT NULL,
                    ""PhoneNumber"" text,
                    ""AvatarUrl"" text,
                    ""Role"" text NOT NULL DEFAULT 'Learner',
                    ""TenantId"" uuid NOT NULL,
                    ""OrgUnitId"" uuid,
                    ""IsPremium"" boolean NOT NULL DEFAULT false,
                    ""SubscriptionTier"" text NOT NULL DEFAULT 'FREE',
                    ""SubscriptionExpiresAt"" timestamp with time zone,
                    ""IsActive"" boolean NOT NULL DEFAULT true,
                    ""FailedLoginAttempts"" integer NOT NULL DEFAULT 0,
                    ""LockoutEnd"" timestamp with time zone,
                    ""LastLoginAt"" timestamp with time zone,
                    ""PasswordResetToken"" text,
                    ""PasswordResetTokenExpiresAt"" timestamp with time zone,
                    ""IsTwoFactorEnabled"" boolean NOT NULL DEFAULT false,
                    ""TwoFactorSecret"" text,
                    ""TwoFactorRecoveryCodes"" text,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                    ""ActiveTenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001',
                    ""RefreshToken"" text,
                    ""RefreshTokenExpiresAt"" timestamp with time zone,
                    CONSTRAINT ""PK_UserAccounts"" PRIMARY KEY (""Id"")
                );

                ALTER TABLE ""UserAccounts"" ADD COLUMN IF NOT EXISTS ""ActiveTenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';
                ALTER TABLE ""UserAccounts"" ADD COLUMN IF NOT EXISTS ""RefreshToken"" text;
                ALTER TABLE ""UserAccounts"" ADD COLUMN IF NOT EXISTS ""RefreshTokenExpiresAt"" timestamp with time zone;

                CREATE TABLE IF NOT EXISTS ""TenantMemberships"" (
                    ""Id"" uuid NOT NULL,
                    ""UserId"" uuid NOT NULL,
                    ""TenantId"" uuid NOT NULL,
                    ""RoleInTenant"" text NOT NULL DEFAULT 'Learner',
                    ""OrgUnitId"" uuid,
                    ""IsDefault"" boolean NOT NULL DEFAULT false,
                    ""IsActive"" boolean NOT NULL DEFAULT true,
                    ""JoinedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                    CONSTRAINT ""PK_TenantMemberships"" PRIMARY KEY (""Id"")
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserAccounts_Email"" ON ""UserAccounts"" (""Email"");
                CREATE INDEX IF NOT EXISTS ""IX_UserAccounts_TenantId"" ON ""UserAccounts"" (""TenantId"");
                CREATE INDEX IF NOT EXISTS ""IX_UserAccounts_OrgUnitId"" ON ""UserAccounts"" (""OrgUnitId"");
                CREATE INDEX IF NOT EXISTS ""IX_UserAccounts_Role"" ON ""UserAccounts"" (""Role"");
                CREATE INDEX IF NOT EXISTS ""IX_UserAccounts_SubscriptionTier"" ON ""UserAccounts"" (""SubscriptionTier"");

                CREATE INDEX IF NOT EXISTS ""IX_TenantMemberships_TenantId"" ON ""TenantMemberships"" (""TenantId"");
                CREATE INDEX IF NOT EXISTS ""IX_TenantMemberships_UserId"" ON ""TenantMemberships"" (""UserId"");
                CREATE INDEX IF NOT EXISTS ""IX_TenantMemberships_OrgUnitId"" ON ""TenantMemberships"" (""OrgUnitId"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_TenantMemberships_UserId_TenantId"" ON ""TenantMemberships"" (""UserId"", ""TenantId"");

                -- SEED Community Tenant
                INSERT INTO ""Tenants"" (
                    ""Id"", ""Code"", ""Name"", ""Plan"", ""ScaleType"",
                    ""IsCommunityHub"", ""MaxUsers"", ""IsActive"", ""CreatedAt"",
                    ""PrimaryColor""
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
                ) ON CONFLICT (""Id"") DO UPDATE SET ""IsCommunityHub"" = true;
            ");

            // ── INDEX: Nhanh chóng tra cứu Community Hub ─────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_Tenants_IsCommunityHub",
                table: "Tenants",
                column: "IsCommunityHub");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantMemberships");

            migrationBuilder.DropTable(
                name: "UserAccounts");

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
