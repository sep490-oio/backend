using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OIO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class config_order : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "shipping_postal_code",
                table: "orders",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_street",
                table: "orders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

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
            migrationBuilder.DropColumn(
                name: "shipping_postal_code",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "shipping_street",
                table: "orders");

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
