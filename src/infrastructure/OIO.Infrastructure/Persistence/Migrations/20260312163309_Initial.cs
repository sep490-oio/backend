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
            migrationBuilder.EnsureSchema(
                name: "quartz");

            migrationBuilder.CreateTable(
                name: "admin_review_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: true),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_review_tasks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    old_data = table.Column<string>(type: "jsonb", nullable: true),
                    new_data = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "buyer_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    communication_rating = table.Column<short>(type: "smallint", nullable: true),
                    overall_rating = table.Column<short>(type: "smallint", nullable: false),
                    payment_speed_rating = table.Column<short>(type: "smallint", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buyer_reviews", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    icon_bytes = table.Column<long>(type: "bigint", nullable: true),
                    icon_duration_seconds = table.Column<double>(type: "double precision", nullable: true),
                    icon_file_name = table.Column<string>(type: "text", nullable: true),
                    icon_format = table.Column<string>(type: "text", nullable: true),
                    icon_height = table.Column<int>(type: "integer", nullable: true),
                    icon_secure_url = table.Column<string>(type: "text", nullable: true),
                    icon_width = table.Column<int>(type: "integer", nullable: true),
                    icon_folder = table.Column<string>(type: "text", nullable: true),
                    icon_public_id = table.Column<string>(type: "text", nullable: true),
                    path = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dispute_response_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    body = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispute_response_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "disputes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    complainant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    respondent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    resolution_notes = table.Column<string>(type: "text", nullable: true),
                    resolution_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: true),
                    escalated_to = table.Column<Guid>(type: "uuid", nullable: true),
                    response_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    escalated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    desired_resolution = table.Column<string>(type: "text", nullable: true),
                    dispute_number = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<string>(type: "text", nullable: true),
                    resolution_type = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_disputes", x => x.id);
                });

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
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "VND"),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "media_uploads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    context = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_linked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    linked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    bytes = table.Column<long>(type: "bigint", nullable: true),
                    duration_seconds = table.Column<double>(type: "double precision", nullable: true),
                    file_name = table.Column<string>(type: "text", nullable: true),
                    format = table.Column<string>(type: "text", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    secure_url = table.Column<string>(type: "text", nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    folder = table.Column<string>(type: "text", nullable: false),
                    public_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_uploads", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "monitoring_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_monitoring_alerts", x => x.id);
                });

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
                    metadata = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'{}'::jsonb"),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    related_entities = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'[]'::jsonb"),
                    actions = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'[]'::jsonb"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipping_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    billing_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "VND"),
                    payment_due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_payment_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_failure_reason = table.Column<string>(type: "text", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    shipped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    platform_fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    shipping_fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    item_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    item_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    shipping_address = table.Column<string>(type: "text", nullable: false),
                    shipping_city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    shipping_district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    shipping_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    shipping_recipient_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    shipping_ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_message_consumers",
                columns: table => new
                {
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_message_consumers", x => new { x.outbox_message_id, x.name });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    token_reference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    expiry_month = table.Column<int>(type: "integer", nullable: true),
                    expiry_year = table.Column<int>(type: "integer", nullable: true),
                    holder_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_four = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    code = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "qrtz_calendars",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    calendar_name = table.Column<string>(type: "text", nullable: false),
                    calendar = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_calendars", x => new { x.sched_name, x.calendar_name });
                });

            migrationBuilder.CreateTable(
                name: "qrtz_fired_triggers",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    entry_id = table.Column<string>(type: "text", nullable: false),
                    trigger_name = table.Column<string>(type: "text", nullable: false),
                    trigger_group = table.Column<string>(type: "text", nullable: false),
                    instance_name = table.Column<string>(type: "text", nullable: false),
                    fired_time = table.Column<long>(type: "bigint", nullable: false),
                    sched_time = table.Column<long>(type: "bigint", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    job_name = table.Column<string>(type: "text", nullable: true),
                    job_group = table.Column<string>(type: "text", nullable: true),
                    is_nonconcurrent = table.Column<bool>(type: "bool", nullable: false),
                    requests_recovery = table.Column<bool>(type: "bool", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_fired_triggers", x => new { x.sched_name, x.entry_id });
                });

            migrationBuilder.CreateTable(
                name: "qrtz_job_details",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    job_name = table.Column<string>(type: "text", nullable: false),
                    job_group = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    job_class_name = table.Column<string>(type: "text", nullable: false),
                    is_durable = table.Column<bool>(type: "bool", nullable: false),
                    is_nonconcurrent = table.Column<bool>(type: "bool", nullable: false),
                    is_update_data = table.Column<bool>(type: "bool", nullable: false),
                    requests_recovery = table.Column<bool>(type: "bool", nullable: false),
                    job_data = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_job_details", x => new { x.sched_name, x.job_name, x.job_group });
                });

            migrationBuilder.CreateTable(
                name: "qrtz_locks",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    lock_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_locks", x => new { x.sched_name, x.lock_name });
                });

            migrationBuilder.CreateTable(
                name: "qrtz_paused_trigger_grps",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    trigger_group = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_paused_trigger_grps", x => new { x.sched_name, x.trigger_group });
                });

            migrationBuilder.CreateTable(
                name: "qrtz_scheduler_state",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    instance_name = table.Column<string>(type: "text", nullable: false),
                    last_checkin_time = table.Column<long>(type: "bigint", nullable: false),
                    checkin_interval = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_scheduler_state", x => new { x.sched_name, x.instance_name });
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "review_queue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority_score = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_queue", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "seller_rating_summary",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_reviews = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    average_rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    rating_5_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    rating_4_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    rating_3_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    rating_2_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    rating_1_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    avg_communication = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    avg_shipping_speed = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    avg_item_accuracy = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0m),
                    response_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seller_rating_summary", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seller_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    is_verified_purchase = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    moderation_reason = table.Column<string>(type: "text", nullable: true),
                    seller_response = table.Column<string>(type: "text", nullable: true),
                    seller_responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    helpful_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    not_helpful_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    communication_rating = table.Column<short>(type: "smallint", nullable: true),
                    item_accuracy_rating = table.Column<short>(type: "smallint", nullable: true),
                    overall_rating = table.Column<short>(type: "smallint", nullable: false),
                    shipping_speed_rating = table.Column<short>(type: "smallint", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seller_reviews", x => x.id);
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
                name: "system_settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    value_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "terms_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    duration_seconds = table.Column<double>(type: "double precision", nullable: true),
                    file_name = table.Column<string>(type: "text", nullable: true),
                    format = table.Column<string>(type: "text", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    content_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    storage_folder = table.Column<string>(type: "text", nullable: false),
                    storage_public_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_terms_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_identity_verifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, defaultValue: "Việt Nam"),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    rejection_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    auto_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    auto_verify_score = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    auto_verify_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    auto_verify_response = table.Column<string>(type: "jsonb", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    id_expired_date = table.Column<DateOnly>(type: "date", nullable: true),
                    id_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    id_issued_date = table.Column<DateOnly>(type: "date", nullable: true),
                    id_issued_place = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    id_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    permanent_address = table.Column<string>(type: "text", nullable: true),
                    province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    verification_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_identity_verifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_notification_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    type_preferences = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    channels = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{\"push\": true, \"email\": true, \"sms\": false}'::jsonb"),
                    quiet_hours = table.Column<string>(type: "jsonb", nullable: true),
                    rate_limits = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_notification_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_risk_flags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flag_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_risk_flags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    email_confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    phone_number_confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    lockout_reason = table.Column<string>(type: "text", nullable: true),
                    lockout_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    access_failed_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    normalized_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, computedColumnSql: "upper((email)::text)", stored: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone_number_country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "inactive"),
                    two_factor_provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "none"),
                    normalized_user_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, computedColumnSql: "upper((user_name)::text)", stored: true),
                    user_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
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
                name: "withdrawal_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    processed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    bank_account_holder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_withdrawal_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dispute_evidences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispute_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_by = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    evidence_bytes = table.Column<long>(type: "bigint", nullable: true),
                    evidence_duration_seconds = table.Column<double>(type: "double precision", nullable: true),
                    evidence_file_name = table.Column<string>(type: "text", nullable: true),
                    evidence_format = table.Column<string>(type: "text", nullable: true),
                    evidence_height = table.Column<int>(type: "integer", nullable: true),
                    evidence_secure_url = table.Column<string>(type: "text", nullable: true),
                    evidence_width = table.Column<int>(type: "integer", nullable: true),
                    evidence_folder = table.Column<string>(type: "text", nullable: true),
                    evidence_public_id = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispute_evidences", x => x.id);
                    table.ForeignKey(
                        name: "fk_dispute_evidences_disputes_dispute_id",
                        column: x => x.dispute_id,
                        principalTable: "disputes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dispute_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispute_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    is_internal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispute_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_dispute_messages_disputes_dispute_id",
                        column: x => x.dispute_id,
                        principalTable: "disputes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dispute_refunds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispute_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    refund_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispute_refunds", x => x.id);
                    table.ForeignKey(
                        name: "fk_dispute_refunds_disputes_dispute_id",
                        column: x => x.dispute_id,
                        principalTable: "disputes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dispute_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispute_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    new_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispute_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_dispute_status_history_dispute_dispute_id",
                        column: x => x.dispute_id,
                        principalTable: "disputes",
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
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "pending"),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    next_retry_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivery_metadata = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'{}'::jsonb"),
                    error_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    error_details = table.Column<string>(type: "jsonb", nullable: true),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_delivery", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_delivery_notification_notification_id",
                        column: x => x.notification_id,
                        principalTable: "notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "escrows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hold_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    release_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "VND"),
                    held_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    released_to = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_escrows", x => x.id);
                    table.ForeignKey(
                        name: "fk_escrows_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_returns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    seller_confirmed_received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    buyer_decision_due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_returns", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_returns_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fee = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "VND"),
                    description = table.Column<string>(type: "text", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    gateway_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    gateway_response = table.Column<string>(type: "jsonb", nullable: true),
                    gateway_transaction_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    net_amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    transaction_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_transactions_payment_methods_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "qrtz_triggers",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    trigger_name = table.Column<string>(type: "text", nullable: false),
                    trigger_group = table.Column<string>(type: "text", nullable: false),
                    job_name = table.Column<string>(type: "text", nullable: false),
                    job_group = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    next_fire_time = table.Column<long>(type: "bigint", nullable: true),
                    prev_fire_time = table.Column<long>(type: "bigint", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: true),
                    trigger_state = table.Column<string>(type: "text", nullable: false),
                    trigger_type = table.Column<string>(type: "text", nullable: false),
                    start_time = table.Column<long>(type: "bigint", nullable: false),
                    end_time = table.Column<long>(type: "bigint", nullable: true),
                    calendar_name = table.Column<string>(type: "text", nullable: true),
                    misfire_instr = table.Column<short>(type: "smallint", nullable: true),
                    job_data = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_triggers", x => new { x.sched_name, x.trigger_name, x.trigger_group });
                    table.ForeignKey(
                        name: "fk_qrtz_triggers_qrtz_job_details_sched_name_job_name_job_group",
                        columns: x => new { x.sched_name, x.job_name, x.job_group },
                        principalSchema: "quartz",
                        principalTable: "qrtz_job_details",
                        principalColumns: new[] { "sched_name", "job_name", "job_group" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_name = table.Column<string>(type: "text", nullable: false),
                    permission_code = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_name, x.permission_code });
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_code",
                        column: x => x.permission_code,
                        principalTable: "permissions",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_name",
                        column: x => x.role_name,
                        principalTable: "roles",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_images_seller_review_review_id",
                        column: x => x.review_id,
                        principalTable: "seller_reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_reports_seller_review_review_id",
                        column: x => x.review_id,
                        principalTable: "seller_reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_helpful = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_votes", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_votes_seller_review_review_id",
                        column: x => x.review_id,
                        principalTable: "seller_reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_terms_acceptances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    term_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_terms_acceptances", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_terms_acceptances_terms_document_term_document_id",
                        column: x => x.term_document_id,
                        principalTable: "terms_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_identity_verification_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    verification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "text", nullable: false),
                    file_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    mime_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    verification_notes = table.Column<string>(type: "text", nullable: true),
                    extracted_data = table.Column<string>(type: "jsonb", nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    document_type = table.Column<string>(type: "text", nullable: false),
                    bytes = table.Column<long>(type: "bigint", nullable: true),
                    duration_seconds = table.Column<double>(type: "double precision", nullable: true),
                    file_name = table.Column<string>(type: "text", nullable: true),
                    format = table.Column<string>(type: "text", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    secure_url = table.Column<string>(type: "text", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    folder = table.Column<string>(type: "text", nullable: false),
                    public_id = table.Column<string>(type: "text", nullable: false),
                    verification_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_identity_verification_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_identity_verification_documents_user_identity_verifica",
                        column: x => x.verification_id,
                        principalTable: "user_identity_verifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_identity_verification_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    verification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    new_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    changed_fields = table.Column<string>(type: "jsonb", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    performed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    performed_by_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_identity_verification_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_identity_verification_history_user_identity_verificati",
                        column: x => x.verification_id,
                        principalTable: "user_identity_verifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seller_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    store_description = table.Column<string>(type: "text", nullable: false, defaultValue: "There are no description for this store."),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_sales_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_sales_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seller_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_seller_profiles_users_id",
                        column: x => x.id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "other"),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    district = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number_country_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_addresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_addresses_user_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_login_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
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
                        name: "fk_user_login_history_user_user_id",
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
                    permission_code = table.Column<string>(type: "text", nullable: false),
                    is_allowed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_permissions", x => new { x.user_id, x.permission_code });
                    table.ForeignKey(
                        name: "fk_user_permissions_permissions_permission_code",
                        column: x => x.permission_code,
                        principalTable: "permissions",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Cascade);
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
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    gender = table.Column<string>(type: "text", nullable: true),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
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
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_name",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    absolute_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_rotated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                        name: "fk_warehouse_items_warehouse_storage_location_storage_location",
                        column: x => x.storage_location_id,
                        principalTable: "warehouse_storage_locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "escrow_release_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    escrow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    trigger_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    release_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_escrow_release_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_escrow_release_events_escrows_escrow_id",
                        column: x => x.escrow_id,
                        principalTable: "escrows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    pending_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallets", x => x.id);
                    table.CheckConstraint("chk_non_negative_balance", "balance >= 0");
                    table.CheckConstraint("chk_non_negative_pending", "pending_balance >= 0");
                    table.ForeignKey(
                        name: "fk_wallets_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_wallets_user_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qrtz_blob_triggers",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    trigger_name = table.Column<string>(type: "text", nullable: false),
                    trigger_group = table.Column<string>(type: "text", nullable: false),
                    blob_data = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_blob_triggers", x => new { x.sched_name, x.trigger_name, x.trigger_group });
                    table.ForeignKey(
                        name: "fk_qrtz_blob_triggers_qrtz_triggers_sched_name_trigger_name_tr",
                        columns: x => new { x.sched_name, x.trigger_name, x.trigger_group },
                        principalSchema: "quartz",
                        principalTable: "qrtz_triggers",
                        principalColumns: new[] { "sched_name", "trigger_name", "trigger_group" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qrtz_cron_triggers",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    trigger_name = table.Column<string>(type: "text", nullable: false),
                    trigger_group = table.Column<string>(type: "text", nullable: false),
                    cron_expression = table.Column<string>(type: "text", nullable: false),
                    time_zone_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_cron_triggers", x => new { x.sched_name, x.trigger_name, x.trigger_group });
                    table.ForeignKey(
                        name: "fk_qrtz_cron_triggers_qrtz_triggers_sched_name_trigger_name_tr",
                        columns: x => new { x.sched_name, x.trigger_name, x.trigger_group },
                        principalSchema: "quartz",
                        principalTable: "qrtz_triggers",
                        principalColumns: new[] { "sched_name", "trigger_name", "trigger_group" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qrtz_simple_triggers",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    trigger_name = table.Column<string>(type: "text", nullable: false),
                    trigger_group = table.Column<string>(type: "text", nullable: false),
                    repeat_count = table.Column<long>(type: "bigint", nullable: false),
                    repeat_interval = table.Column<long>(type: "bigint", nullable: false),
                    times_triggered = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_simple_triggers", x => new { x.sched_name, x.trigger_name, x.trigger_group });
                    table.ForeignKey(
                        name: "fk_qrtz_simple_triggers_qrtz_triggers_sched_name_trigger_name_",
                        columns: x => new { x.sched_name, x.trigger_name, x.trigger_group },
                        principalSchema: "quartz",
                        principalTable: "qrtz_triggers",
                        principalColumns: new[] { "sched_name", "trigger_name", "trigger_group" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qrtz_simprop_triggers",
                schema: "quartz",
                columns: table => new
                {
                    sched_name = table.Column<string>(type: "text", nullable: false),
                    trigger_name = table.Column<string>(type: "text", nullable: false),
                    trigger_group = table.Column<string>(type: "text", nullable: false),
                    str_prop_1 = table.Column<string>(type: "text", nullable: true),
                    str_prop_2 = table.Column<string>(type: "text", nullable: true),
                    str_prop_3 = table.Column<string>(type: "text", nullable: true),
                    int_prop_1 = table.Column<int>(type: "integer", nullable: true),
                    int_prop_2 = table.Column<int>(type: "integer", nullable: true),
                    long_prop_1 = table.Column<long>(type: "bigint", nullable: true),
                    long_prop_2 = table.Column<long>(type: "bigint", nullable: true),
                    dec_prop_1 = table.Column<decimal>(type: "numeric", nullable: true),
                    dec_prop_2 = table.Column<decimal>(type: "numeric", nullable: true),
                    bool_prop_1 = table.Column<bool>(type: "bool", nullable: true),
                    bool_prop_2 = table.Column<bool>(type: "bool", nullable: true),
                    time_zone_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qrtz_simprop_triggers", x => new { x.sched_name, x.trigger_name, x.trigger_group });
                    table.ForeignKey(
                        name: "fk_qrtz_simprop_triggers_qrtz_triggers_sched_name_trigger_name",
                        columns: x => new { x.sched_name, x.trigger_name, x.trigger_group },
                        principalSchema: "quartz",
                        principalTable: "qrtz_triggers",
                        principalColumns: new[] { "sched_name", "trigger_name", "trigger_group" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                        name: "fk_user_refresh_tokens_user_refresh_tokens_parent_token_id",
                        column: x => x.parent_token_id,
                        principalTable: "user_refresh_tokens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_user_refresh_tokens_user_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "user_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_refresh_tokens_user_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    attributes = table.Column<string>(type: "jsonb", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    resubmission_count = table.Column<int>(type: "integer", nullable: false),
                    assigned_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    warehouse_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    condition = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "draft"),
                    title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_items", x => x.id);
                    table.CheckConstraint("chk_items_resubmission_count", "resubmission_count >= 0");
                    table.ForeignKey(
                        name: "fk_items_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_items_warehouse_item_warehouse_item_id",
                        column: x => x.warehouse_item_id,
                        principalTable: "warehouse_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                        name: "fk_outbound_shipments_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_outbound_shipments_warehouse_item_warehouse_item_id",
                        column: x => x.warehouse_item_id,
                        principalTable: "warehouse_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wallet_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    balance_before = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_wallet_transactions_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_wallet_transactions_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auctions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actual_end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    winner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bid_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    watch_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    auction_type = table.Column<string>(type: "text", nullable: false, defaultValue: "regular"),
                    auto_extend = table.Column<bool>(type: "boolean", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    info_extension_count = table.Column<int>(type: "integer", nullable: false),
                    extension_minutes = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    qualification_end_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    qualification_start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    bid_increment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    buy_now_price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    currency = table.Column<string>(type: "text", nullable: false),
                    current_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    reserve_price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    starting_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    priority_reason = table.Column<string>(type: "jsonb", nullable: false),
                    priority = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "draft")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auctions", x => x.id);
                    table.CheckConstraint("chk_buy_now_gt_starting", "buy_now_price IS NULL OR buy_now_price > starting_price");
                    table.CheckConstraint("chk_current_gte_starting", "current_price >= starting_price");
                    table.CheckConstraint("chk_end_after_start", "end_time > start_time");
                    table.CheckConstraint("chk_positive_bid_increment", "bid_increment > 0");
                    table.CheckConstraint("chk_qualification_window", "qualification_start_at IS NULL OR qualification_end_at IS NULL OR qualification_end_at > qualification_start_at");
                    table.CheckConstraint("chk_reserve_gte_starting", "reserve_price IS NULL OR reserve_price >= starting_price");
                    table.ForeignKey(
                        name: "fk_auctions_item_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "text", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    bytes = table.Column<long>(type: "bigint", nullable: true),
                    duration_seconds = table.Column<double>(type: "double precision", nullable: true),
                    file_name = table.Column<string>(type: "text", nullable: true),
                    format = table.Column<string>(type: "text", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    secure_url = table.Column<string>(type: "text", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    folder = table.Column<string>(type: "text", nullable: false),
                    public_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_media", x => x.id);
                    table.ForeignKey(
                        name: "fk_item_media_item_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_moderation_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    old_status = table.Column<string>(type: "text", nullable: true),
                    new_status = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    action = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_moderation_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_item_moderation_reviews_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asker_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question = table.Column<string>(type: "text", nullable: false),
                    answer = table.Column<string>(type: "text", nullable: true),
                    answered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_questions", x => x.id);
                    table.ForeignKey(
                        name: "fk_item_questions_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateTable(
                name: "auction_auto_bids",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bidder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    total_auto_bids = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_auto_bid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    stop_reason = table.Column<string>(type: "text", nullable: true),
                    stopped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_validation_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    current_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    increment_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    max_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reserved_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_auto_bids", x => x.id);
                    table.ForeignKey(
                        name: "fk_auction_auto_bids_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auction_deposits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bidder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_deposits", x => x.id);
                    table.ForeignKey(
                        name: "auction_deposits_transaction_id_fkey",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "auction_deposits_user_id_fkey",
                        column: x => x.bidder_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_auction_deposits_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auction_emergencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggered_by = table.Column<Guid>(type: "uuid", nullable: true),
                    trigger_source = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    triggered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_emergencies", x => x.id);
                    table.ForeignKey(
                        name: "auction_emergencies_triggered_by_fkey",
                        column: x => x.triggered_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_auction_emergencies_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auction_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_in_auction = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    qualified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_reason = table.Column<string>(type: "text", nullable: true),
                    join_status = table.Column<string>(type: "text", nullable: false),
                    qualification_status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_auction_participants_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auction_relist_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relist_no = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_relist_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_auction_relist_history_auction_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auction_watchers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notify_on_bid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_on_end = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_watchers", x => x.id);
                    table.ForeignKey(
                        name: "fk_auction_watchers_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auction_winner_offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank_no = table.Column<int>(type: "integer", nullable: false),
                    offered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    offer_status = table.Column<string>(type: "text", nullable: false, defaultValue: "offered")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_winner_offers", x => x.id);
                    table.ForeignKey(
                        name: "fk_auction_winner_offers_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sealed_bids",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bidder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_encrypted = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    revealed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revealed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "submitted")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sealed_bids", x => x.id);
                    table.ForeignKey(
                        name: "fk_sealed_bids_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bids",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bidder_id = table.Column<Guid>(type: "uuid", nullable: false),
                    auto_bid_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_auto_bid = table.Column<bool>(type: "boolean", nullable: false, computedColumnSql: "(auto_bid_id IS NOT NULL)", stored: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bids", x => x.id);
                    table.ForeignKey(
                        name: "fk_bids_auction_auto_bids_auto_bid_id",
                        column: x => x.auto_bid_id,
                        principalTable: "auction_auto_bids",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_bids_auctions_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auction_emergency_actions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    emergency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_emergency_actions", x => x.id);
                    table.ForeignKey(
                        name: "fk_auction_emergency_actions_auction_emergencies_emergency_id",
                        column: x => x.emergency_id,
                        principalTable: "auction_emergencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auction_price_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bid_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auction_price_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_auction_price_history_auction_auction_id",
                        column: x => x.auction_id,
                        principalTable: "auctions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_auction_price_history_bid_bid_id",
                        column: x => x.bid_id,
                        principalTable: "bids",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "bid_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bid_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason_code = table.Column<string>(type: "text", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bid_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_bid_events_bids_bid_id",
                        column: x => x.bid_id,
                        principalTable: "bids",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_admin_review_tasks_assigned_to",
                table: "admin_review_tasks",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "idx_admin_review_tasks_due_at",
                table: "admin_review_tasks",
                column: "due_at",
                filter: "due_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_admin_review_tasks_entity",
                table: "admin_review_tasks",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "idx_auction_auto_bids_auction_id",
                table: "auction_auto_bids",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "idx_auction_auto_bids_bidder_id",
                table: "auction_auto_bids",
                column: "bidder_id");

            migrationBuilder.CreateIndex(
                name: "idx_auction_auto_bids_last_validation_at",
                table: "auction_auto_bids",
                column: "last_validation_at");

            migrationBuilder.CreateIndex(
                name: "idx_auction_auto_bids_stopped_at",
                table: "auction_auto_bids",
                column: "stopped_at",
                filter: "stopped_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_auction_auto_bids_auction_id_bidder_id",
                table: "auction_auto_bids",
                columns: new[] { "auction_id", "bidder_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_auction_deposits_auction_id_bidder_id",
                table: "auction_deposits",
                columns: new[] { "auction_id", "bidder_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_auction_deposits_bidder_id",
                table: "auction_deposits",
                column: "bidder_id");

            migrationBuilder.CreateIndex(
                name: "ix_auction_deposits_transaction_id",
                table: "auction_deposits",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "idx_auction_emergencies_auction_id",
                table: "auction_emergencies",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "ix_auction_emergencies_triggered_by",
                table: "auction_emergencies",
                column: "triggered_by");

            migrationBuilder.CreateIndex(
                name: "idx_auction_emergency_actions_emergency_id",
                table: "auction_emergency_actions",
                column: "emergency_id");

            migrationBuilder.CreateIndex(
                name: "idx_auction_participants_auction_id",
                table: "auction_participants",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "idx_auction_participants_auction_user",
                table: "auction_participants",
                columns: new[] { "auction_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_auction_participants_user_id",
                table: "auction_participants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_auction_price_history_auction_id",
                table: "auction_price_history",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "ix_auction_price_history_bid_id",
                table: "auction_price_history",
                column: "bid_id");

            migrationBuilder.CreateIndex(
                name: "idx_auction_relist_history_auction_id",
                table: "auction_relist_history",
                columns: new[] { "auction_id", "relist_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_auction_relist_history_created_at",
                table: "auction_relist_history",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_auction_watchers_auction_id_user_id",
                table: "auction_watchers",
                columns: new[] { "auction_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_auction_winner_offers_auction_rank",
                table: "auction_winner_offers",
                columns: new[] { "auction_id", "rank_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_auction_winner_offers_auction_user",
                table: "auction_winner_offers",
                columns: new[] { "auction_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_auctions_item_id",
                table: "auctions",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_action",
                table: "audit_logs",
                columns: new[] { "action", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_actor",
                table: "audit_logs",
                columns: new[] { "actor_user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_entity",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "idx_bid_events_bid",
                table: "bid_events",
                column: "bid_id");

            migrationBuilder.CreateIndex(
                name: "idx_bid_events_bid_created_at",
                table: "bid_events",
                columns: new[] { "event_type", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_bids_auction",
                table: "bids",
                column: "auction_id");

            migrationBuilder.CreateIndex(
                name: "idx_bids_auction_created_at",
                table: "bids",
                columns: new[] { "auction_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_bids_bidder",
                table: "bids",
                column: "bidder_id");

            migrationBuilder.CreateIndex(
                name: "ix_bids_auto_bid_id",
                table: "bids",
                column: "auto_bid_id");

            migrationBuilder.CreateIndex(
                name: "idx_buyer_reviews_buyer",
                table: "buyer_reviews",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "uq_buyer_reviews_order_reviewer",
                table: "buyer_reviews",
                columns: new[] { "order_id", "reviewer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_id",
                table: "categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispute_evidences_dispute_id",
                table: "dispute_evidences",
                column: "dispute_id");

            migrationBuilder.CreateIndex(
                name: "idx_dispute_messages_dispute",
                table: "dispute_messages",
                column: "dispute_id");

            migrationBuilder.CreateIndex(
                name: "ix_dispute_refunds_dispute_id",
                table: "dispute_refunds",
                column: "dispute_id");

            migrationBuilder.CreateIndex(
                name: "uq_dispute_refunds_transaction_id",
                table: "dispute_refunds",
                column: "transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dispute_status_history_dispute_id",
                table: "dispute_status_history",
                column: "dispute_id");

            migrationBuilder.CreateIndex(
                name: "idx_disputes_assigned_to",
                table: "disputes",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "idx_disputes_complainant_id",
                table: "disputes",
                column: "complainant_id");

            migrationBuilder.CreateIndex(
                name: "idx_disputes_order_id",
                table: "disputes",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "idx_disputes_respondent_id",
                table: "disputes",
                column: "respondent_id");

            migrationBuilder.CreateIndex(
                name: "idx_escrow_release_events_escrow",
                table: "escrow_release_events",
                column: "escrow_id");

            migrationBuilder.CreateIndex(
                name: "idx_escrow_release_events_trigger",
                table: "escrow_release_events",
                columns: new[] { "trigger_source_type", "trigger_source_id" });

            migrationBuilder.CreateIndex(
                name: "ix_escrows_order_id",
                table: "escrows",
                column: "order_id");

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
                name: "ix_item_media_item_id",
                table: "item_media",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_item_moderation_reviews_created_at",
                table: "item_moderation_reviews",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_item_moderation_reviews_item_id",
                table: "item_moderation_reviews",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_item_moderation_reviews_reviewer_id",
                table: "item_moderation_reviews",
                column: "reviewer_id");

            migrationBuilder.CreateIndex(
                name: "ix_item_questions_item_id",
                table: "item_questions",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_items_assigned_admin_id",
                table: "items",
                column: "assigned_admin_id");

            migrationBuilder.CreateIndex(
                name: "idx_items_category_id",
                table: "items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "idx_items_reviewed_by",
                table: "items",
                column: "reviewed_by");

            migrationBuilder.CreateIndex(
                name: "idx_items_seller_id",
                table: "items",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "idx_items_submitted_at",
                table: "items",
                column: "submitted_at");

            migrationBuilder.CreateIndex(
                name: "ix_items_warehouse_item_id",
                table: "items",
                column: "warehouse_item_id");

            migrationBuilder.CreateIndex(
                name: "idx_media_uploads_expired",
                table: "media_uploads",
                columns: new[] { "is_confirmed", "expires_at" },
                filter: "is_confirmed = false");

            migrationBuilder.CreateIndex(
                name: "idx_media_uploads_orphan",
                table: "media_uploads",
                columns: new[] { "is_confirmed", "is_linked", "confirmed_at" },
                filter: "is_confirmed = true AND is_linked = false");

            migrationBuilder.CreateIndex(
                name: "idx_media_uploads_user_id",
                table: "media_uploads",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_monitoring_alerts_entity",
                table: "monitoring_alerts",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "idx_delivery_notification",
                table: "notification_delivery",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_retry",
                table: "notification_delivery",
                column: "next_retry_at",
                filter: "status = 'failed' AND next_retry_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_status_scheduled",
                table: "notification_delivery",
                columns: new[] { "status", "scheduled_at" },
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "idx_delivery_user_channel",
                table: "notification_delivery",
                columns: new[] { "user_id", "channel" });

            migrationBuilder.CreateIndex(
                name: "idx_notifications_entity",
                table: "notifications",
                columns: new[] { "entity_type", "entity_id" },
                filter: "entity_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_notifications_type_event",
                table: "notifications",
                columns: new[] { "notification_type", "event_type" });

            migrationBuilder.CreateIndex(
                name: "idx_notifications_user_created",
                table: "notifications",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_order_returns_buyer",
                table: "order_returns",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "uq_order_returns_order",
                table: "order_returns",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_orders_buyer",
                table: "orders",
                column: "buyer_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_last_payment_attempt_at",
                table: "orders",
                column: "last_payment_attempt_at");

            migrationBuilder.CreateIndex(
                name: "idx_orders_payment_due_at",
                table: "orders",
                column: "payment_due_at",
                filter: "status = 'pending_payment'");

            migrationBuilder.CreateIndex(
                name: "idx_orders_seller",
                table: "orders",
                column: "seller_id");

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
                name: "idx_outbox_cleanup",
                table: "outbox_messages",
                columns: new[] { "occurred_at", "id" },
                filter: "processed_at IS NOT NULL AND error IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_outbox_messages_unprocessed",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_job_group",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "job_group");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_job_name",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "job_name");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_job_req_recovery",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "requests_recovery");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_trig_group",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "trigger_group");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_trig_inst_name",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "instance_name");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_trig_name",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                column: "trigger_name");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_ft_trig_nm_gp",
                schema: "quartz",
                table: "qrtz_fired_triggers",
                columns: new[] { "sched_name", "trigger_name", "trigger_group" });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_j_req_recovery",
                schema: "quartz",
                table: "qrtz_job_details",
                column: "requests_recovery");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_t_next_fire_time",
                schema: "quartz",
                table: "qrtz_triggers",
                column: "next_fire_time");

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_t_nft_st",
                schema: "quartz",
                table: "qrtz_triggers",
                columns: new[] { "next_fire_time", "trigger_state" });

            migrationBuilder.CreateIndex(
                name: "idx_qrtz_t_state",
                schema: "quartz",
                table: "qrtz_triggers",
                column: "trigger_state");

            migrationBuilder.CreateIndex(
                name: "ix_qrtz_triggers_sched_name_job_name_job_group",
                schema: "quartz",
                table: "qrtz_triggers",
                columns: new[] { "sched_name", "job_name", "job_group" });

            migrationBuilder.CreateIndex(
                name: "idx_reports_entity",
                table: "reports",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "idx_reports_reporter",
                table: "reports",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_images_review_id",
                table: "review_images",
                column: "review_id");

            migrationBuilder.CreateIndex(
                name: "idx_review_queue_assigned_to",
                table: "review_queue",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "idx_review_queue_entity",
                table: "review_queue",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_review_reports_review_id",
                table: "review_reports",
                column: "review_id");

            migrationBuilder.CreateIndex(
                name: "uq_review_votes_review_user",
                table: "review_votes",
                columns: new[] { "review_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_code",
                table: "role_permissions",
                column: "permission_code");

            migrationBuilder.CreateIndex(
                name: "idx_sealed_bids_auction_created_at",
                table: "sealed_bids",
                columns: new[] { "auction_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_sealed_bids_bidder",
                table: "sealed_bids",
                column: "bidder_id");

            migrationBuilder.CreateIndex(
                name: "ix_sealed_bids_auction_id_bidder_id",
                table: "sealed_bids",
                columns: new[] { "auction_id", "bidder_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_seller_rating_summary_seller",
                table: "seller_rating_summary",
                column: "seller_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_seller_reviews_order",
                table: "seller_reviews",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "idx_seller_reviews_reviewer",
                table: "seller_reviews",
                column: "reviewer_id");

            migrationBuilder.CreateIndex(
                name: "idx_seller_reviews_seller",
                table: "seller_reviews",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "uq_seller_reviews_order_reviewer",
                table: "seller_reviews",
                columns: new[] { "order_id", "reviewer_id" },
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
                name: "idx_terms_documents_published_at",
                table: "terms_documents",
                column: "published_at");

            migrationBuilder.CreateIndex(
                name: "idx_terms_documents_type",
                table: "terms_documents",
                column: "term_type");

            migrationBuilder.CreateIndex(
                name: "uq_terms_documents_type_version",
                table: "terms_documents",
                columns: new[] { "term_type", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_transactions_order",
                table: "transactions",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_user",
                table: "transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_payment_method_id",
                table: "transactions",
                column: "payment_method_id");

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
                name: "ix_user_identity_verification_documents_verification_id",
                table: "user_identity_verification_documents",
                column: "verification_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_identity_verification_history_created",
                table: "user_identity_verification_history",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_user_identity_verification_history_verification",
                table: "user_identity_verification_history",
                column: "verification_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_identity_verifications_submitted",
                table: "user_identity_verifications",
                column: "submitted_at",
                filter: "status = 'submitted'");

            migrationBuilder.CreateIndex(
                name: "idx_user_identity_verifications_user",
                table: "user_identity_verifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_login_history_user_id",
                table: "user_login_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_notification_preferences_user",
                table: "user_notification_preferences",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_permissions_permission_code",
                table: "user_permissions",
                column: "permission_code");

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_session_id",
                table: "user_refresh_tokens",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_expires_at",
                table: "user_refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_parent_token_id",
                table: "user_refresh_tokens",
                column: "parent_token_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_refresh_tokens_session_session_id_created_at",
                table: "user_refresh_tokens",
                columns: new[] { "session_id", "created_at" });

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
                name: "idx_user_risk_flags_user",
                table: "user_risk_flags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_absolute_expires_at",
                table: "user_sessions",
                column: "absolute_expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_expires_at",
                table: "user_sessions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_user",
                table: "user_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_user_active",
                table: "user_sessions",
                columns: new[] { "user_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "idx_user_terms_acceptances_accepted_at",
                table: "user_terms_acceptances",
                column: "accepted_at");

            migrationBuilder.CreateIndex(
                name: "idx_user_terms_acceptances_term_document",
                table: "user_terms_acceptances",
                column: "term_document_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_terms_acceptances_user",
                table: "user_terms_acceptances",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_terms_acceptances_user_term",
                table: "user_terms_acceptances",
                columns: new[] { "user_id", "term_document_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_wallet_transactions_wallet",
                table: "wallet_transactions",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_transactions_transaction_id",
                table: "wallet_transactions",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallets_transaction_id",
                table: "wallets",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "uq_wallets_user_id",
                table: "wallets",
                column: "user_id",
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
                name: "idx_auctions_active",
                table: "auctions",
                columns: new[] { "start_time", "end_time" },
                filter: "((status)::text = 'active'::text)");

            migrationBuilder.CreateIndex(
                name: "idx_auction_emergencies_status",
                table: "auction_emergencies",
                column: "status")
                .Annotation("Relational:ColumnName", "status");

            migrationBuilder.CreateIndex(
                name: "idx_auction_participants_qualification_status",
                table: "auction_participants",
                columns: new[] { "auction_id", "qualification_status" });

            migrationBuilder.CreateIndex(
                name: "idx_auction_winner_offers_status_expires",
                table: "auction_winner_offers",
                columns: new[] { "offer_status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "idx_auction_auto_bids_auction_id_status",
                table: "auction_auto_bids",
                columns: new[] { "auction_id", "status" },
                filter: "status = 'active'");

            migrationBuilder.CreateIndex(
                name: "IX_bids_auction_id_amount",
                table: "bids",
                columns: new[] { "auction_id", "amount" });

            migrationBuilder.CreateIndex(
                name: "idx_sealed_bids_auction_status",
                table: "sealed_bids",
                columns: new[] { "auction_id", "status" });

            migrationBuilder.CreateIndex(
                name: "idx_items_status",
                table: "items",
                column: "status")
                .Annotation("Relational:ColumnName", "status")
                .Annotation("Relational:DefaultValue", "draft");

            migrationBuilder.CreateIndex(
                name: "idx_items_status_submitted_at",
                table: "items",
                columns: new[] { "status", "submitted_at" },
                filter: "status IN ('submitted', 'under_review')");

            migrationBuilder.CreateIndex(
                name: "idx_admin_review_tasks_status_priority",
                table: "admin_review_tasks",
                columns: new[] { "status", "priority" });

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
                name: "idx_monitoring_alerts_status_severity",
                table: "monitoring_alerts",
                columns: new[] { "status", "severity", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_reports_status_assigned",
                table: "reports",
                columns: new[] { "status", "assigned_to" });

            migrationBuilder.CreateIndex(
                name: "idx_review_queue_status_priority",
                table: "review_queue",
                columns: new[] { "status", "priority_score", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_notifications_priority_created",
                table: "notifications",
                columns: new[] { "priority", "created_at" },
                filter: "status = 'unread'");

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
                name: "admin_review_tasks");

            migrationBuilder.DropTable(
                name: "auction_deposits");

            migrationBuilder.DropTable(
                name: "auction_emergency_actions");

            migrationBuilder.DropTable(
                name: "auction_participants");

            migrationBuilder.DropTable(
                name: "auction_price_history");

            migrationBuilder.DropTable(
                name: "auction_relist_history");

            migrationBuilder.DropTable(
                name: "auction_watchers");

            migrationBuilder.DropTable(
                name: "auction_winner_offers");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "bid_events");

            migrationBuilder.DropTable(
                name: "buyer_reviews");

            migrationBuilder.DropTable(
                name: "dispute_evidences");

            migrationBuilder.DropTable(
                name: "dispute_messages");

            migrationBuilder.DropTable(
                name: "dispute_refunds");

            migrationBuilder.DropTable(
                name: "dispute_response_templates");

            migrationBuilder.DropTable(
                name: "dispute_status_history");

            migrationBuilder.DropTable(
                name: "escrow_release_events");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "item_media");

            migrationBuilder.DropTable(
                name: "item_moderation_reviews");

            migrationBuilder.DropTable(
                name: "item_questions");

            migrationBuilder.DropTable(
                name: "media_uploads");

            migrationBuilder.DropTable(
                name: "monitoring_alerts");

            migrationBuilder.DropTable(
                name: "notification_delivery");

            migrationBuilder.DropTable(
                name: "order_returns");

            migrationBuilder.DropTable(
                name: "outbox_message_consumers");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "qrtz_blob_triggers",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_calendars",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_cron_triggers",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_fired_triggers",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_locks",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_paused_trigger_grps",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_scheduler_state",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_simple_triggers",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "qrtz_simprop_triggers",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "review_images");

            migrationBuilder.DropTable(
                name: "review_queue");

            migrationBuilder.DropTable(
                name: "review_reports");

            migrationBuilder.DropTable(
                name: "review_votes");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "sealed_bids");

            migrationBuilder.DropTable(
                name: "seller_profiles");

            migrationBuilder.DropTable(
                name: "seller_rating_summary");

            migrationBuilder.DropTable(
                name: "shipment_tracking_events");

            migrationBuilder.DropTable(
                name: "shipping_provider_configs");

            migrationBuilder.DropTable(
                name: "system_settings");

            migrationBuilder.DropTable(
                name: "user_addresses");

            migrationBuilder.DropTable(
                name: "user_identity_verification_documents");

            migrationBuilder.DropTable(
                name: "user_identity_verification_history");

            migrationBuilder.DropTable(
                name: "user_login_history");

            migrationBuilder.DropTable(
                name: "user_notification_preferences");

            migrationBuilder.DropTable(
                name: "user_permissions");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "user_refresh_tokens");

            migrationBuilder.DropTable(
                name: "user_risk_flags");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_terms_acceptances");

            migrationBuilder.DropTable(
                name: "wallet_transactions");

            migrationBuilder.DropTable(
                name: "withdrawal_requests");

            migrationBuilder.DropTable(
                name: "auction_emergencies");

            migrationBuilder.DropTable(
                name: "bids");

            migrationBuilder.DropTable(
                name: "disputes");

            migrationBuilder.DropTable(
                name: "escrows");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "qrtz_triggers",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "seller_reviews");

            migrationBuilder.DropTable(
                name: "outbound_shipments");

            migrationBuilder.DropTable(
                name: "user_identity_verifications");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "user_sessions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "terms_documents");

            migrationBuilder.DropTable(
                name: "wallets");

            migrationBuilder.DropTable(
                name: "auction_auto_bids");

            migrationBuilder.DropTable(
                name: "qrtz_job_details",
                schema: "quartz");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "auctions");

            migrationBuilder.DropTable(
                name: "payment_methods");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "warehouse_items");

            migrationBuilder.DropTable(
                name: "inbound_shipments");

            migrationBuilder.DropTable(
                name: "warehouse_storage_locations");
        }
    }
}
