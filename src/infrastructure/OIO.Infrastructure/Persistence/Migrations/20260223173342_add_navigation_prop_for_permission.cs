using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OIO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class add_navigation_prop_for_permission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "absolute_expires_at",
                table: "user_refresh_token_families",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "normalized_role_name",
                table: "roles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                computedColumnSql: "UPPER(role_name)",
                stored: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true,
                oldComputedColumnSql: "UPPER(role_name)",
                oldStored: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_token_families_absolute_expires_at",
                table: "user_refresh_token_families",
                column: "absolute_expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_token_families_expires_at",
                table: "user_refresh_token_families",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_permissions_permission_id",
                table: "user_permissions",
                column: "permission_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_permissions_permissions_permission_id",
                table: "user_permissions",
                column: "permission_id",
                principalTable: "permissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_roles_roles_role_id",
                table: "user_roles",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_permissions_permissions_permission_id",
                table: "user_permissions");

            migrationBuilder.DropForeignKey(
                name: "fk_user_roles_roles_role_id",
                table: "user_roles");

            migrationBuilder.DropIndex(
                name: "ix_user_refresh_token_families_absolute_expires_at",
                table: "user_refresh_token_families");

            migrationBuilder.DropIndex(
                name: "ix_user_refresh_token_families_expires_at",
                table: "user_refresh_token_families");

            migrationBuilder.DropIndex(
                name: "ix_user_permissions_permission_id",
                table: "user_permissions");

            migrationBuilder.DropColumn(
                name: "absolute_expires_at",
                table: "user_refresh_token_families");

            migrationBuilder.AlterColumn<string>(
                name: "normalized_role_name",
                table: "roles",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                computedColumnSql: "UPPER(role_name)",
                stored: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComputedColumnSql: "UPPER(role_name)",
                oldStored: true);
        }
    }
}
