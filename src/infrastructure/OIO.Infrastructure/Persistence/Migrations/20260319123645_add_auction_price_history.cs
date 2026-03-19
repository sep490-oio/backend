using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OIO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class add_auction_price_history : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "last_used_totp_time_step",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_two_factor_secret",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "two_factor_secret",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_code",
                table: "payment_methods",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "masked_card_number",
                table: "payment_methods",
                type: "character varying(25)",
                maxLength: 25,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vnpay_card_type",
                table: "payment_methods",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vnpay_token",
                table: "payment_methods",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "auction_price_history",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "recovery_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recovery_codes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_payment_methods_user_token",
                table: "payment_methods",
                columns: new[] { "user_id", "vnpay_token" },
                filter: "vnpay_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_recovery_codes_user_unused",
                table: "recovery_codes",
                columns: new[] { "user_id", "is_used" },
                filter: "is_used = false");

            migrationBuilder.DropIndex(
                name: "idx_auctions_priority",
                table: "auctions");

            migrationBuilder.DropIndex(
                name: "idx_auctions_status",
                table: "auctions");

            migrationBuilder.DropIndex(
                name: "idx_auction_emergencies_status",
                table: "auction_emergencies");

            migrationBuilder.DropIndex(
                name: "idx_items_status",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_disputes_dispute_number",
                table: "disputes");

            migrationBuilder.DropIndex(
                name: "IX_disputes_status",
                table: "disputes");

            migrationBuilder.DropIndex(
                name: "IX_orders_order_number",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "idx_orders_status",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_invoices_invoice_number",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "idx_transactions_status",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_transaction_number",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "idx_seller_reviews_rating",
                table: "seller_reviews");

            migrationBuilder.DropIndex(
                name: "idx_seller_reviews_status",
                table: "seller_reviews");

            migrationBuilder.DropIndex(
                name: "idx_media_uploads_public_id",
                table: "media_uploads");

            migrationBuilder.DropIndex(
                name: "idx_user_identity_verifications_status",
                table: "user_identity_verifications");

            migrationBuilder.DropIndex(
                name: "idx_seller_profiles_status",
                table: "seller_profiles");

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_email_active",
                table: "users");

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_user_name_active",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "idx_auctions_priority",
                table: "auctions",
                column: "priority")
                .Annotation("Relational:ColumnName", "priority");

            migrationBuilder.CreateIndex(
                name: "idx_auctions_status",
                table: "auctions",
                column: "status")
                .Annotation("MaxLength", 20)
                .Annotation("Relational:ColumnName", "status")
                .Annotation("Relational:DefaultValue", "draft");

            migrationBuilder.CreateIndex(
                name: "idx_auction_emergencies_status",
                table: "auction_emergencies",
                column: "status")
                .Annotation("Relational:ColumnName", "status");

            migrationBuilder.CreateIndex(
                name: "idx_items_status",
                table: "items",
                column: "status")
                .Annotation("Relational:ColumnName", "status")
                .Annotation("Relational:DefaultValue", "draft");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_dispute_number",
                table: "disputes",
                column: "dispute_number",
                unique: true)
                .Annotation("Relational:ColumnName", "dispute_number");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_status",
                table: "disputes",
                column: "status")
                .Annotation("Relational:ColumnName", "status");

            migrationBuilder.CreateIndex(
                name: "IX_orders_order_number",
                table: "orders",
                column: "order_number",
                unique: true)
                .Annotation("MaxLength", 50)
                .Annotation("Relational:ColumnName", "order_number");

            migrationBuilder.CreateIndex(
                name: "idx_orders_status",
                table: "orders",
                column: "status")
                .Annotation("MaxLength", 30)
                .Annotation("Relational:ColumnName", "status");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_invoice_number",
                table: "invoices",
                column: "invoice_number",
                unique: true)
                .Annotation("MaxLength", 50)
                .Annotation("Relational:ColumnName", "invoice_number");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_status",
                table: "transactions",
                column: "status")
                .Annotation("MaxLength", 20)
                .Annotation("Relational:ColumnName", "status");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_transaction_number",
                table: "transactions",
                column: "transaction_number",
                unique: true)
                .Annotation("MaxLength", 50)
                .Annotation("Relational:ColumnName", "transaction_number");

            migrationBuilder.CreateIndex(
                name: "idx_seller_reviews_rating",
                table: "seller_reviews",
                column: "overall_rating")
                .Annotation("Relational:ColumnName", "overall_rating");

            migrationBuilder.CreateIndex(
                name: "idx_seller_reviews_status",
                table: "seller_reviews",
                column: "status")
                .Annotation("MaxLength", 20)
                .Annotation("Relational:ColumnName", "status");

            migrationBuilder.CreateIndex(
                name: "idx_media_uploads_public_id",
                table: "media_uploads",
                column: "public_id")
                .Annotation("Relational:ColumnName", "public_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_identity_verifications_status",
                table: "user_identity_verifications",
                column: "status")
                .Annotation("MaxLength", 20)
                .Annotation("Relational:ColumnName", "status");

            migrationBuilder.CreateIndex(
                name: "idx_seller_profiles_status",
                table: "seller_profiles",
                column: "status")
                .Annotation("MaxLength", 30)
                .Annotation("Relational:ColumnName", "status");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recovery_codes");

            migrationBuilder.DropIndex(
                name: "idx_payment_methods_user_token",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "last_used_totp_time_step",
                table: "users");

            migrationBuilder.DropColumn(
                name: "pending_two_factor_secret",
                table: "users");

            migrationBuilder.DropColumn(
                name: "two_factor_secret",
                table: "users");

            migrationBuilder.DropColumn(
                name: "bank_code",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "masked_card_number",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "vnpay_card_type",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "vnpay_token",
                table: "payment_methods");

            migrationBuilder.DropColumn(
                name: "type",
                table: "auction_price_history");

            migrationBuilder.DropIndex(
                name: "idx_auctions_priority",
                table: "auctions");

            migrationBuilder.DropIndex(
                name: "idx_auctions_status",
                table: "auctions");

            migrationBuilder.DropIndex(
                name: "idx_auction_emergencies_status",
                table: "auction_emergencies");

            migrationBuilder.DropIndex(
                name: "idx_items_status",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_disputes_dispute_number",
                table: "disputes");

            migrationBuilder.DropIndex(
                name: "IX_disputes_status",
                table: "disputes");

            migrationBuilder.DropIndex(
                name: "IX_orders_order_number",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "idx_orders_status",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_invoices_invoice_number",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "idx_transactions_status",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_transaction_number",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "idx_seller_reviews_rating",
                table: "seller_reviews");

            migrationBuilder.DropIndex(
                name: "idx_seller_reviews_status",
                table: "seller_reviews");

            migrationBuilder.DropIndex(
                name: "idx_media_uploads_public_id",
                table: "media_uploads");

            migrationBuilder.DropIndex(
                name: "idx_user_identity_verifications_status",
                table: "user_identity_verifications");

            migrationBuilder.DropIndex(
                name: "idx_seller_profiles_status",
                table: "seller_profiles");

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_email_active",
                table: "users");

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_user_name_active",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "idx_auctions_priority",
                table: "auctions",
                column: "priority")
                .Annotation("Relational:ColumnName", "priority")
                .Annotation("Relational:ColumnType", "numeric");

            migrationBuilder.CreateIndex(
                name: "idx_auctions_status",
                table: "auctions",
                column: "status")
                .Annotation("MaxLength", 20)
                .Annotation("Relational:ColumnName", "status")
                .Annotation("Relational:ColumnType", "character varying(20)")
                .Annotation("Relational:DefaultValue", "draft");

            migrationBuilder.CreateIndex(
                name: "idx_auction_emergencies_status",
                table: "auction_emergencies",
                column: "status")
                .Annotation("Relational:ColumnName", "status")
                .Annotation("Relational:ColumnType", "text");

            migrationBuilder.CreateIndex(
                name: "idx_items_status",
                table: "items",
                column: "status")
                .Annotation("Relational:ColumnName", "status")
                .Annotation("Relational:ColumnType", "text")
                .Annotation("Relational:DefaultValue", "draft");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_dispute_number",
                table: "disputes",
                column: "dispute_number",
                unique: true)
                .Annotation("Relational:ColumnName", "dispute_number")
                .Annotation("Relational:ColumnType", "text");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_status",
                table: "disputes",
                column: "status")
                .Annotation("Relational:ColumnName", "status")
                .Annotation("Relational:ColumnType", "text");

            migrationBuilder.CreateIndex(
                name: "IX_orders_order_number",
                table: "orders",
                column: "order_number",
                unique: true)
                .Annotation("MaxLength", 50)
                .Annotation("Relational:ColumnName", "order_number")
                .Annotation("Relational:ColumnType", "character varying(50)");

            migrationBuilder.CreateIndex(
                name: "idx_orders_status",
                table: "orders",
                column: "status")
                .Annotation("MaxLength", 30)
                .Annotation("Relational:ColumnName", "status")
                .Annotation("Relational:ColumnType", "character varying(30)");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_invoice_number",
                table: "invoices",
                column: "invoice_number",
                unique: true)
                .Annotation("MaxLength", 50)
                .Annotation("Relational:ColumnName", "invoice_number")
                .Annotation("Relational:ColumnType", "character varying(50)");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_status",
                table: "transactions",
                column: "status")
                .Annotation("MaxLength", 20)
                .Annotation("Relational:ColumnName", "status")
                .Annotation("Relational:ColumnType", "character varying(20)");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_transaction_number",
                table: "transactions",
                column: "transaction_number",
                unique: true)
                .Annotation("MaxLength", 50)
                .Annotation("Relational:ColumnName", "transaction_number")
                .Annotation("Relational:ColumnType", "character varying(50)");

            migrationBuilder.CreateIndex(
                name: "idx_seller_reviews_rating",
                table: "seller_reviews",
                column: "overall_rating")
                .Annotation("Relational:ColumnName", "overall_rating")
                .Annotation("Relational:ColumnType", "smallint");

            migrationBuilder.CreateIndex(
                name: "idx_seller_reviews_status",
                table: "seller_reviews",
                column: "status")
                .Annotation("MaxLength", 20)
                .Annotation("Relational:ColumnName", "status")
                .Annotation("Relational:ColumnType", "character varying(20)");

            migrationBuilder.CreateIndex(
                name: "idx_media_uploads_public_id",
                table: "media_uploads",
                column: "public_id")
                .Annotation("Relational:ColumnName", "public_id")
                .Annotation("Relational:ColumnType", "text");

            migrationBuilder.CreateIndex(
                name: "idx_user_identity_verifications_status",
                table: "user_identity_verifications",
                column: "status")
                .Annotation("MaxLength", 20)
                .Annotation("Relational:ColumnName", "status")
                .Annotation("Relational:ColumnType", "character varying(20)");

            migrationBuilder.CreateIndex(
                name: "idx_seller_profiles_status",
                table: "seller_profiles",
                column: "status")
                .Annotation("MaxLength", 30)
                .Annotation("Relational:ColumnName", "status")
                .Annotation("Relational:ColumnType", "character varying(30)");

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
        }
    }
}
