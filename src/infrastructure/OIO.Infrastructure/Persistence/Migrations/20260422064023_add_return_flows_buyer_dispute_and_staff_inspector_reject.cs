using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OIO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class add_return_flows_buyer_dispute_and_staff_inspector_reject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_order_returns_order",
                table: "order_returns");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_reminder_sent_at",
                table: "order_returns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_fee_payer",
                table: "order_returns",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "warehouse_to_seller_shipments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_address_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    rejection_reason = table.Column<string>(type: "text", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tracking_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    shipped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    seller_confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivery_failure_reason = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouse_to_seller_shipments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "uq_order_returns_order",
                table: "order_returns",
                column: "order_id",
                unique: true,
                filter: "status NOT IN ('resolved','cancelled','rejected')");

            migrationBuilder.CreateIndex(
                name: "idx_warehouse_to_seller_shipments_seller_status",
                table: "warehouse_to_seller_shipments",
                columns: new[] { "seller_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_warehouse_to_seller_shipments_warehouse_item",
                table: "warehouse_to_seller_shipments",
                column: "warehouse_item_id");

            migrationBuilder.CreateIndex(
                name: "uq_warehouse_to_seller_shipments_inspection_active",
                table: "warehouse_to_seller_shipments",
                column: "warehouse_inspection_id",
                unique: true,
                filter: "status NOT IN ('closed','returned_to_warehouse')");

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
                name: "idx_terms_documents_status",
                table: "terms_documents");

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
                name: "idx_terms_documents_status",
                table: "terms_documents",
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
            // ======================================================================
            // LOW #2 SAFETY PRE-CHECK
            // ----------------------------------------------------------------------
            // Down() recreates the ORIGINAL full unique index on order_returns(order_id).
            // If any new dispute cycle created a second OrderReturn row for the same
            // order_id after this migration was applied (which the PARTIAL unique index
            // allows once the prior row is terminal), the full-unique recreation below
            // will FAIL. Abort early with a clear error so operators can resolve
            // duplicates manually before rolling back.
            // ======================================================================
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM order_returns GROUP BY order_id HAVING COUNT(*) > 1
    ) THEN
        RAISE EXCEPTION 'Down() unsafe: duplicate order_ids exist in order_returns. '
            'Resolve manually (consolidate or delete surplus terminal rows) before rolling back.';
    END IF;
END $$;
");

            migrationBuilder.DropTable(
                name: "warehouse_to_seller_shipments");

            migrationBuilder.DropIndex(
                name: "uq_order_returns_order",
                table: "order_returns");

            migrationBuilder.DropColumn(
                name: "last_reminder_sent_at",
                table: "order_returns");

            migrationBuilder.DropColumn(
                name: "shipping_fee_payer",
                table: "order_returns");

            migrationBuilder.CreateIndex(
                name: "uq_order_returns_order",
                table: "order_returns",
                column: "order_id",
                unique: true);

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
                name: "idx_terms_documents_status",
                table: "terms_documents");

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
                name: "idx_terms_documents_status",
                table: "terms_documents",
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
