using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OIO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_message",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_message", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    permission_code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    normalized_permission_code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, computedColumnSql: "UPPER(permission_code)", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    role_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    normalized_role_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, computedColumnSql: "UPPER(role_name)", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    normalized_user_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, computedColumnSql: "upper((user_name)::text)", stored: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, computedColumnSql: "upper((email)::text)", stored: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    email_confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    phone_number_country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    phone_number_confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    two_factor_provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "none"),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "inactive"),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    lockout_reason = table.Column<string>(type: "text", nullable: true),
                    lockout_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    access_failed_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "other"),
                    recipient_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    phone_number_country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_addresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_addresses_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_login_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: false),
                    login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_login_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_login_history_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_permissions",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_allowed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_permissions", x => new { x.user_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_user_permissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_profiles_users_id",
                        column: x => x.id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_refresh_token_families",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_rotated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_refresh_token_families", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_refresh_token_families_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "text", nullable: true),
                    created_by_ip = table.Column<IPAddress>(type: "inet", nullable: false),
                    revoked_by_ip = table.Column<IPAddress>(type: "inet", nullable: true),
                    rotation_counter = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_refresh_tokens", x => x.id);
                    table.CheckConstraint("chk_expires_after_created", "expires_at > created_at");
                    table.CheckConstraint("chk_revoked_after_created", "revoked_at IS NULL OR revoked_at >= created_at");
                    table.CheckConstraint("chk_used_after_created", "used_at IS NULL OR used_at >= created_at");
                    table.ForeignKey(
                        name: "fk_user_refresh_tokens_user_refresh_token_families_family_id",
                        column: x => x.family_id,
                        principalTable: "user_refresh_token_families",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_refresh_tokens_user_refresh_tokens_parent_token_id",
                        column: x => x.parent_token_id,
                        principalTable: "user_refresh_tokens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_outbox_cleanup",
                table: "outbox_message",
                columns: new[] { "occurred_at", "id" },
                filter: "processed_at IS NOT NULL AND error IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_messages_unprocessed",
                table: "outbox_message",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_normalized_permission_code",
                table: "permissions",
                column: "normalized_permission_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_normalized_role_name",
                table: "roles",
                column: "normalized_role_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_unique_default_address_per_user",
                table: "user_addresses",
                column: "user_id",
                unique: true,
                filter: "is_default = TRUE");

            migrationBuilder.CreateIndex(
                name: "idx_user_addresses_user_id_type",
                table: "user_addresses",
                columns: new[] { "user_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_user_login_history_user_id",
                table: "user_login_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_token_families_user",
                table: "user_refresh_token_families",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_token_families_user_active",
                table: "user_refresh_token_families",
                columns: new[] { "user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_expires_at",
                table: "user_refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_family_family_id_created_at",
                table: "user_refresh_tokens",
                columns: new[] { "family_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_family_id",
                table: "user_refresh_tokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_parent_token_id",
                table: "user_refresh_tokens",
                column: "parent_token_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_token_hash",
                table: "user_refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_user_id_is_used",
                table: "user_refresh_tokens",
                columns: new[] { "user_id", "is_used" });

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true,
                filter: "(deleted_at IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_users_normalized_user_name",
                table: "users",
                column: "normalized_user_name",
                unique: true,
                filter: "(deleted_at IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_message");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_addresses");

            migrationBuilder.DropTable(
                name: "user_login_history");

            migrationBuilder.DropTable(
                name: "user_permissions");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "user_refresh_tokens");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "user_refresh_token_families");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
