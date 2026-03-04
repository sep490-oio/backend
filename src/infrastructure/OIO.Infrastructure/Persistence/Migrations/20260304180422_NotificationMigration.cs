using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OIO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotificationMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actions = table.Column<string>(type: "jsonb", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    related_entities = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_notification_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    channels = table.Column<string>(type: "jsonb", nullable: false),
                    quiet_hours = table.Column<string>(type: "jsonb", nullable: true),
                    rate_limits = table.Column<string>(type: "jsonb", nullable: true),
                    type_preferences = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_notification_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_notification_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_delivery",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    error_details = table.Column<string>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    delivery_metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_delivery", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_delivery_notifications_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_notification_delivery_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_delivery_notification",
                table: "notification_delivery",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_retry",
                table: "notification_delivery",
                column: "next_retry_at");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_status_scheduled",
                table: "notification_delivery",
                columns: new[] { "status", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "idx_delivery_user_channel",
                table: "notification_delivery",
                columns: new[] { "user_id", "channel" });

            migrationBuilder.CreateIndex(
                name: "idx_notifications_entity",
                table: "notifications",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "idx_notifications_priority_created",
                table: "notifications",
                columns: new[] { "priority", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_notifications_type_event",
                table: "notifications",
                columns: new[] { "notification_type", "event_type" });

            migrationBuilder.CreateIndex(
                name: "idx_notifications_user_created",
                table: "notifications",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_notifications_user_status",
                table: "notifications",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_preferences_user",
                table: "user_notification_preferences",
                column: "user_id",
                unique: true);

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_email_active",
                table: "users");

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_user_name_active",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "idx_unique_users_normalized_email_active",
                table: "users",
                column: "normalized_email",
                unique: true,
                filter: "(deleted_at IS NULL)")
                .Annotation("MaxLength", 255)
                .Annotation("Relational:ColumnName", "normalized_email")
                .Annotation("Relational:ComputedColumnSql", "upper((email)::text)")
                .Annotation("Relational:IsStored", true);

            migrationBuilder.CreateIndex(
                name: "idx_unique_users_normalized_user_name_active",
                table: "users",
                column: "normalized_user_name",
                unique: true,
                filter: "(deleted_at IS NULL)")
                .Annotation("MaxLength", 50)
                .Annotation("Relational:ColumnName", "normalized_user_name")
                .Annotation("Relational:ComputedColumnSql", "upper((user_name)::text)")
                .Annotation("Relational:IsStored", true);
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS idx_notifications_metadata ON notifications USING gin (metadata);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_delivery");

            migrationBuilder.DropTable(
                name: "user_notification_preferences");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_email_active",
                table: "users");

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_user_name_active",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "idx_unique_users_normalized_email_active",
                table: "users",
                column: "normalized_email",
                unique: true,
                filter: "(deleted_at IS NULL)")
                .Annotation("MaxLength", 255)
                .Annotation("Relational:ColumnName", "normalized_email")
                .Annotation("Relational:ColumnType", "character varying(255)")
                .Annotation("Relational:ComputedColumnSql", "upper((email)::text)")
                .Annotation("Relational:IsStored", true);

            migrationBuilder.CreateIndex(
                name: "idx_unique_users_normalized_user_name_active",
                table: "users",
                column: "normalized_user_name",
                unique: true,
                filter: "(deleted_at IS NULL)")
                .Annotation("MaxLength", 50)
                .Annotation("Relational:ColumnName", "normalized_user_name")
                .Annotation("Relational:ColumnType", "character varying(50)")
                .Annotation("Relational:ComputedColumnSql", "upper((user_name)::text)")
                .Annotation("Relational:IsStored", true);
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS idx_notifications_metadata;");
        }
    }
}
