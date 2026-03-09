using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OIO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WarehouseMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inbound_shipments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    client_order_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    carrier_tracking_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sender_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sender_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sender_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    sender_ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sender_district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sender_province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sender_carrier_address_data = table.Column<string>(type: "jsonb", nullable: true),
                    shipping_fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    insurance_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    extra_data = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    expected_arrival_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    arrived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    height_cm = table.Column<int>(type: "integer", nullable: true),
                    length_cm = table.Column<int>(type: "integer", nullable: true),
                    weight_grams = table.Column<int>(type: "integer", nullable: false),
                    width_cm = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbound_shipments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shipping_provider_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    environment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    api_base_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    credentials = table.Column<string>(type: "jsonb", nullable: false),
                    cached_token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cached_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    webhook_secret = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    pick_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pick_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pick_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    pick_ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pick_district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pick_province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pick_carrier_address_data = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipping_provider_configs", x => x.id);
                    table.CheckConstraint("chk_shipping_provider_configs_cached_token", "(cached_token IS NULL AND cached_token_expires_at IS NULL) OR (cached_token IS NOT NULL AND cached_token_expires_at IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "warehouse_storage_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    aisle = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    shelf = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    bin = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_occupied = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouse_storage_locations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "warehouse_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inbound_shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    condition_on_arrival = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    inspection_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    inspection_images = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    inspected_by = table.Column<Guid>(type: "uuid", nullable: true),
                    inspected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouse_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_warehouse_items_inbound_shipments_inbound_shipment_id",
                        column: x => x.inbound_shipment_id,
                        principalTable: "inbound_shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_warehouse_items_warehouse_storage_locations_storage_locatio",
                        column: x => x.storage_location_id,
                        principalTable: "warehouse_storage_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "outbound_shipments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    client_order_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    carrier_tracking_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    shipping_label_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    shipping_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    recipient_carrier_address_data = table.Column<string>(type: "jsonb", nullable: true),
                    shipping_fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    insurance_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    cod_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    ghn_payment_type = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    ghn_handling_note = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    extra_data = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    packed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    packed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dispatched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estimated_delivery_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    height_cm = table.Column<int>(type: "integer", nullable: true),
                    length_cm = table.Column<int>(type: "integer", nullable: true),
                    weight_grams = table.Column<int>(type: "integer", nullable: false),
                    width_cm = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbound_shipments", x => x.id);
                    table.CheckConstraint("chk_outbound_shipments_cod_amount", "cod_amount >= 0");
                    table.CheckConstraint("chk_outbound_shipments_insurance_value", "insurance_value >= 0");
                    table.CheckConstraint("chk_outbound_shipments_shipping_fee", "shipping_fee >= 0");
                    table.ForeignKey(
                        name: "fk_outbound_shipments_warehouse_items_warehouse_item_id",
                        column: x => x.warehouse_item_id,
                        principalTable: "warehouse_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipment_tracking_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    carrier_status_raw = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    carrier_status_desc = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    normalized_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    reason_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reason_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    event_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    raw_payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    inbound_shipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outbound_shipment_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipment_tracking_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_shipment_tracking_events_inbound_shipments_inbound_shipment",
                        column: x => x.inbound_shipment_id,
                        principalTable: "inbound_shipments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_shipment_tracking_events_outbound_shipments_outbound_shipme",
                        column: x => x.outbound_shipment_id,
                        principalTable: "outbound_shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_inbound_shipments_carrier_tracking_number",
                table: "inbound_shipments",
                column: "carrier_tracking_number",
                filter: "carrier_tracking_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_inbound_shipments_item_id",
                table: "inbound_shipments",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_inbound_shipments_seller_id",
                table: "inbound_shipments",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "idx_inbound_shipments_status",
                table: "inbound_shipments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_unique_inbound_shipments_client_order_code",
                table: "inbound_shipments",
                column: "client_order_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_outbound_shipments_carrier_tracking_number",
                table: "outbound_shipments",
                column: "carrier_tracking_number",
                filter: "carrier_tracking_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_outbound_shipments_order_id",
                table: "outbound_shipments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "idx_outbound_shipments_status",
                table: "outbound_shipments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_unique_outbound_shipments_client_order_code",
                table: "outbound_shipments",
                column: "client_order_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_unique_outbound_shipments_warehouse_item_id",
                table: "outbound_shipments",
                column: "warehouse_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_shipment_tracking_events_inbound_shipment_id",
                table: "shipment_tracking_events",
                columns: new[] { "inbound_shipment_id", "event_time" });

            migrationBuilder.CreateIndex(
                name: "idx_shipment_tracking_events_outbound_shipment_id",
                table: "shipment_tracking_events",
                columns: new[] { "outbound_shipment_id", "event_time" });

            migrationBuilder.CreateIndex(
                name: "idx_shipment_tracking_events_shipment_carrier_status",
                table: "shipment_tracking_events",
                columns: new[] { "shipment_id", "carrier_status_raw", "event_time" });

            migrationBuilder.CreateIndex(
                name: "idx_shipping_provider_configs_default_active",
                table: "shipping_provider_configs",
                column: "is_default",
                filter: "is_default = TRUE AND is_active = TRUE");

            migrationBuilder.CreateIndex(
                name: "idx_unique_shipping_provider_configs_provider_code",
                table: "shipping_provider_configs",
                column: "provider_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_unique_warehouse_items_inbound_shipment_id",
                table: "warehouse_items",
                column: "inbound_shipment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_warehouse_items_item_id",
                table: "warehouse_items",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_warehouse_items_status",
                table: "warehouse_items",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_warehouse_items_storage_location_id",
                table: "warehouse_items",
                column: "storage_location_id",
                filter: "storage_location_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_unique_warehouse_storage_locations_label",
                table: "warehouse_storage_locations",
                column: "label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_warehouse_storage_locations_vacant",
                table: "warehouse_storage_locations",
                column: "is_occupied",
                filter: "is_occupied = FALSE");

            migrationBuilder.DropIndex(
                name: "idx_auctions_end_time",
                table: "auctions");

            migrationBuilder.DropIndex(
                name: "idx_auctions_start_time",
                table: "auctions");

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_email_active",
                table: "users");

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_user_name_active",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "idx_auctions_end_time",
                table: "auctions",
                column: "end_time")
                .Annotation("Relational:ColumnName", "end_time");

            migrationBuilder.CreateIndex(
                name: "idx_auctions_start_time",
                table: "auctions",
                column: "start_time")
                .Annotation("Relational:ColumnName", "start_time");

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
                "ALTER TABLE shipment_tracking_events ADD CONSTRAINT " +
                "chk_tracking_events_exactly_one_shipment CHECK (" +
                "(inbound_shipment_id IS NOT NULL AND outbound_shipment_id IS NULL) OR " +
                "(inbound_shipment_id IS NULL AND outbound_shipment_id IS NOT NULL));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE shipment_tracking_events DROP CONSTRAINT IF EXISTS " +
                "chk_tracking_events_exactly_one_shipment;");
            migrationBuilder.DropTable(
                name: "shipment_tracking_events");

            migrationBuilder.DropTable(
                name: "shipping_provider_configs");

            migrationBuilder.DropTable(
                name: "outbound_shipments");

            migrationBuilder.DropTable(
                name: "warehouse_items");

            migrationBuilder.DropTable(
                name: "inbound_shipments");

            migrationBuilder.DropTable(
                name: "warehouse_storage_locations");

            migrationBuilder.DropIndex(
                name: "idx_auctions_end_time",
                table: "auctions");

            migrationBuilder.DropIndex(
                name: "idx_auctions_start_time",
                table: "auctions");

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_email_active",
                table: "users");

            migrationBuilder.DropIndex(
                name: "idx_unique_users_normalized_user_name_active",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "idx_auctions_end_time",
                table: "auctions",
                column: "end_time")
                .Annotation("Relational:ColumnName", "end_time")
                .Annotation("Relational:ColumnType", "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "idx_auctions_start_time",
                table: "auctions",
                column: "start_time")
                .Annotation("Relational:ColumnName", "start_time")
                .Annotation("Relational:ColumnType", "timestamp with time zone");

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
