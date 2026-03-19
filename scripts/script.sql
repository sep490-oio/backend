CREATE TABLE IF NOT EXISTS __ef_migrations_history (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'quartz') THEN
            CREATE SCHEMA quartz;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE admin_review_tasks (
        id uuid NOT NULL,
        entity_type character varying(50) NOT NULL,
        entity_id uuid NOT NULL,
        assigned_to uuid,
        due_at timestamp with time zone,
        completed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        priority character varying(10) NOT NULL,
        status character varying(20) NOT NULL,
        CONSTRAINT pk_admin_review_tasks PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE audit_logs (
        id uuid NOT NULL,
        actor_user_id uuid,
        actor_role character varying(30),
        action character varying(100) NOT NULL,
        entity_type character varying(50) NOT NULL,
        entity_id uuid,
        old_data jsonb,
        new_data jsonb,
        ip_address inet,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT pk_audit_logs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE buyer_reviews (
        id uuid NOT NULL,
        order_id uuid NOT NULL,
        reviewer_id uuid NOT NULL,
        buyer_id uuid NOT NULL,
        comment text,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        communication_rating smallint,
        overall_rating smallint NOT NULL,
        payment_speed_rating smallint,
        status character varying(20),
        CONSTRAINT pk_buyer_reviews PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE categories (
        id uuid NOT NULL,
        parent_id uuid,
        name text NOT NULL,
        description text NOT NULL,
        is_active boolean NOT NULL,
        sort_order integer NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        icon_bytes bigint,
        icon_duration_seconds double precision,
        icon_file_name text,
        icon_format text,
        icon_height integer,
        icon_secure_url text,
        icon_width integer,
        icon_folder text,
        icon_public_id text,
        path text NOT NULL,
        slug text NOT NULL,
        CONSTRAINT pk_categories PRIMARY KEY (id),
        CONSTRAINT fk_categories_categories_parent_id FOREIGN KEY (parent_id) REFERENCES categories (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE dispute_response_templates (
        id uuid NOT NULL,
        name character varying(100) NOT NULL,
        category character varying(50),
        subject character varying(255),
        body text NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT pk_dispute_response_templates PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE disputes (
        id uuid NOT NULL,
        order_id uuid NOT NULL,
        auction_id uuid NOT NULL,
        complainant_id uuid NOT NULL,
        respondent_id uuid NOT NULL,
        title text NOT NULL,
        description text NOT NULL,
        resolution_notes text,
        resolution_amount numeric(18,2),
        assigned_to uuid,
        escalated_to uuid,
        response_deadline timestamp with time zone,
        escalated_at timestamp with time zone,
        resolved_at timestamp with time zone,
        closed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        desired_resolution text,
        dispute_number text NOT NULL,
        priority text,
        resolution_type text,
        status text,
        type text NOT NULL,
        CONSTRAINT pk_disputes PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE inbound_shipments (
        id uuid NOT NULL,
        item_id uuid NOT NULL,
        seller_id uuid NOT NULL,
        provider_code character varying(20) NOT NULL,
        client_order_code character varying(100) NOT NULL,
        carrier_tracking_number character varying(100),
        sender_name character varying(100) NOT NULL,
        sender_phone character varying(20) NOT NULL,
        sender_address character varying(255) NOT NULL,
        sender_ward character varying(100) NOT NULL,
        sender_district character varying(100) NOT NULL,
        sender_province character varying(100) NOT NULL,
        sender_carrier_address_data jsonb,
        shipping_fee numeric(18,2) NOT NULL DEFAULT 0.0,
        insurance_value numeric(18,2) NOT NULL DEFAULT 0.0,
        extra_data jsonb NOT NULL,
        status character varying(30) NOT NULL,
        notes character varying(500),
        expected_arrival_at timestamp with time zone,
        arrived_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        height_cm integer,
        length_cm integer,
        weight_grams integer NOT NULL,
        width_cm integer,
        CONSTRAINT pk_inbound_shipments PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE invoices (
        id uuid NOT NULL,
        order_id uuid NOT NULL,
        buyer_id uuid NOT NULL,
        seller_id uuid NOT NULL,
        subtotal numeric(18,2) NOT NULL,
        tax_amount numeric(18,2) NOT NULL DEFAULT 0.0,
        currency character varying(3) NOT NULL DEFAULT 'VND',
        issued_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        due_date date,
        paid_at timestamp with time zone,
        invoice_number character varying(50) NOT NULL,
        status character varying(20),
        total_amount numeric(18,2) NOT NULL,
        total_amount_currency character varying(3),
        CONSTRAINT pk_invoices PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE media_uploads (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        context character varying(50) NOT NULL,
        resource_type character varying(10) NOT NULL,
        entity_id uuid,
        is_confirmed boolean NOT NULL DEFAULT FALSE,
        is_linked boolean NOT NULL DEFAULT FALSE,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        expires_at timestamp with time zone NOT NULL,
        confirmed_at timestamp with time zone,
        linked_at timestamp with time zone,
        bytes bigint,
        duration_seconds double precision,
        file_name text,
        format text,
        height integer,
        secure_url text NOT NULL,
        width integer,
        folder text NOT NULL,
        public_id text NOT NULL,
        CONSTRAINT pk_media_uploads PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE monitoring_alerts (
        id uuid NOT NULL,
        entity_type character varying(50) NOT NULL,
        entity_id uuid NOT NULL,
        alert_type character varying(50) NOT NULL,
        payload jsonb NOT NULL DEFAULT ('{}'::jsonb),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        severity character varying(10) NOT NULL,
        status character varying(20) NOT NULL,
        CONSTRAINT pk_monitoring_alerts PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE notifications (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        notification_type character varying(50) NOT NULL,
        event_type character varying(50) NOT NULL,
        title character varying(500) NOT NULL,
        message text NOT NULL,
        metadata jsonb DEFAULT ('{}'::jsonb),
        entity_type character varying(50),
        entity_id uuid,
        related_entities jsonb DEFAULT ('[]'::jsonb),
        actions jsonb DEFAULT ('[]'::jsonb),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        read_at timestamp with time zone,
        expires_at timestamp with time zone,
        priority character varying(20) NOT NULL,
        status character varying(20) NOT NULL,
        CONSTRAINT pk_notifications PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE orders (
        id uuid NOT NULL,
        auction_id uuid NOT NULL,
        buyer_id uuid NOT NULL,
        seller_id uuid NOT NULL,
        shipping_address_id uuid,
        billing_address_id uuid,
        currency character varying(3) NOT NULL DEFAULT 'VND',
        payment_due_at timestamp with time zone,
        payment_attempt_count integer NOT NULL DEFAULT 0,
        last_payment_attempt_at timestamp with time zone,
        payment_failure_reason text,
        paid_at timestamp with time zone,
        shipped_at timestamp with time zone,
        delivered_at timestamp with time zone,
        completed_at timestamp with time zone,
        cancelled_at timestamp with time zone,
        version integer NOT NULL DEFAULT 0,
        notes text,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        order_number character varying(50) NOT NULL,
        platform_fee numeric(18,2) NOT NULL DEFAULT 0.0,
        shipping_fee numeric(18,2) NOT NULL DEFAULT 0.0,
        tax_amount numeric(18,2) NOT NULL DEFAULT 0.0,
        item_price numeric(18,2) NOT NULL,
        item_price_currency character varying(3),
        total_amount numeric(18,2) NOT NULL,
        total_amount_currency character varying(3),
        shipping_address text NOT NULL,
        shipping_city character varying(120),
        shipping_district character varying(100),
        shipping_phone character varying(20),
        shipping_recipient_name character varying(100),
        shipping_ward character varying(100),
        status character varying(30) NOT NULL,
        CONSTRAINT pk_orders PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE outbox_message_consumers (
        outbox_message_id uuid NOT NULL,
        name character varying(500) NOT NULL,
        CONSTRAINT pk_outbox_message_consumers PRIMARY KEY (outbox_message_id, name)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE outbox_messages (
        id uuid NOT NULL,
        type character varying(255) NOT NULL,
        content jsonb NOT NULL,
        occurred_at timestamp with time zone NOT NULL,
        processed_at timestamp with time zone,
        error text,
        attempt_count integer NOT NULL DEFAULT 0,
        CONSTRAINT pk_outbox_messages PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE payment_methods (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        provider character varying(50),
        is_default boolean NOT NULL DEFAULT FALSE,
        is_verified boolean NOT NULL DEFAULT FALSE,
        is_active boolean NOT NULL DEFAULT TRUE,
        token_reference character varying(255),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        expiry_month integer,
        expiry_year integer,
        holder_name character varying(200),
        last_four character varying(4),
        type character varying(30) NOT NULL,
        CONSTRAINT pk_payment_methods PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE permissions (
        code text NOT NULL,
        CONSTRAINT pk_permissions PRIMARY KEY (code)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE quartz.qrtz_calendars (
        sched_name text NOT NULL,
        calendar_name text NOT NULL,
        calendar bytea NOT NULL,
        CONSTRAINT pk_qrtz_calendars PRIMARY KEY (sched_name, calendar_name)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE quartz.qrtz_fired_triggers (
        sched_name text NOT NULL,
        entry_id text NOT NULL,
        trigger_name text NOT NULL,
        trigger_group text NOT NULL,
        instance_name text NOT NULL,
        fired_time bigint NOT NULL,
        sched_time bigint NOT NULL,
        priority integer NOT NULL,
        state text NOT NULL,
        job_name text,
        job_group text,
        is_nonconcurrent bool NOT NULL,
        requests_recovery bool,
        CONSTRAINT pk_qrtz_fired_triggers PRIMARY KEY (sched_name, entry_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE quartz.qrtz_job_details (
        sched_name text NOT NULL,
        job_name text NOT NULL,
        job_group text NOT NULL,
        description text,
        job_class_name text NOT NULL,
        is_durable bool NOT NULL,
        is_nonconcurrent bool NOT NULL,
        is_update_data bool NOT NULL,
        requests_recovery bool NOT NULL,
        job_data bytea,
        CONSTRAINT pk_qrtz_job_details PRIMARY KEY (sched_name, job_name, job_group)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE quartz.qrtz_locks (
        sched_name text NOT NULL,
        lock_name text NOT NULL,
        CONSTRAINT pk_qrtz_locks PRIMARY KEY (sched_name, lock_name)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE quartz.qrtz_paused_trigger_grps (
        sched_name text NOT NULL,
        trigger_group text NOT NULL,
        CONSTRAINT pk_qrtz_paused_trigger_grps PRIMARY KEY (sched_name, trigger_group)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE quartz.qrtz_scheduler_state (
        sched_name text NOT NULL,
        instance_name text NOT NULL,
        last_checkin_time bigint NOT NULL,
        checkin_interval bigint NOT NULL,
        CONSTRAINT pk_qrtz_scheduler_state PRIMARY KEY (sched_name, instance_name)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE reports (
        id uuid NOT NULL,
        reporter_id uuid NOT NULL,
        entity_type character varying(50) NOT NULL,
        entity_id uuid NOT NULL,
        reason_code character varying(50) NOT NULL,
        description text,
        assigned_to uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        status character varying(20) NOT NULL,
        CONSTRAINT pk_reports PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE review_queue (
        id uuid NOT NULL,
        entity_type character varying(50) NOT NULL,
        entity_id uuid NOT NULL,
        priority_score numeric(10,2) NOT NULL DEFAULT 0.0,
        assigned_to uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        status character varying(20) NOT NULL,
        CONSTRAINT pk_review_queue PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE roles (
        name text NOT NULL,
        level integer NOT NULL,
        modified_at timestamp with time zone,
        CONSTRAINT pk_roles PRIMARY KEY (name)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE seller_rating_summary (
        id uuid NOT NULL,
        seller_id uuid NOT NULL,
        total_reviews integer NOT NULL DEFAULT 0,
        average_rating numeric(3,2) NOT NULL DEFAULT 0.0,
        rating_5_count integer NOT NULL DEFAULT 0,
        rating_4_count integer NOT NULL DEFAULT 0,
        rating_3_count integer NOT NULL DEFAULT 0,
        rating_2_count integer NOT NULL DEFAULT 0,
        rating_1_count integer NOT NULL DEFAULT 0,
        avg_communication numeric(3,2) NOT NULL DEFAULT 0.0,
        avg_shipping_speed numeric(3,2) NOT NULL DEFAULT 0.0,
        avg_item_accuracy numeric(3,2) NOT NULL DEFAULT 0.0,
        response_count integer NOT NULL DEFAULT 0,
        last_updated_at timestamp with time zone,
        CONSTRAINT pk_seller_rating_summary PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE seller_reviews (
        id uuid NOT NULL,
        order_id uuid NOT NULL,
        auction_id uuid NOT NULL,
        reviewer_id uuid NOT NULL,
        seller_id uuid NOT NULL,
        title character varying(200),
        comment text,
        is_verified_purchase boolean NOT NULL DEFAULT TRUE,
        moderation_reason text,
        seller_response text,
        seller_responded_at timestamp with time zone,
        helpful_count integer NOT NULL DEFAULT 0,
        not_helpful_count integer NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        communication_rating smallint,
        item_accuracy_rating smallint,
        overall_rating smallint NOT NULL,
        shipping_speed_rating smallint,
        status character varying(20),
        CONSTRAINT pk_seller_reviews PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE shipping_provider_configs (
        id uuid NOT NULL,
        provider_code character varying(20) NOT NULL,
        display_name character varying(100) NOT NULL,
        environment character varying(20) NOT NULL,
        api_base_url character varying(255) NOT NULL,
        credentials jsonb NOT NULL,
        cached_token character varying(500),
        cached_token_expires_at timestamp with time zone,
        webhook_secret character varying(255),
        pick_name character varying(100) NOT NULL,
        pick_phone character varying(20) NOT NULL,
        pick_address character varying(255) NOT NULL,
        pick_ward character varying(100) NOT NULL,
        pick_district character varying(100) NOT NULL,
        pick_province character varying(100) NOT NULL,
        pick_carrier_address_data jsonb,
        is_active boolean NOT NULL DEFAULT TRUE,
        is_default boolean NOT NULL DEFAULT FALSE,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        CONSTRAINT pk_shipping_provider_configs PRIMARY KEY (id),
        CONSTRAINT chk_shipping_provider_configs_cached_token CHECK ((cached_token IS NULL AND cached_token_expires_at IS NULL) OR (cached_token IS NOT NULL AND cached_token_expires_at IS NOT NULL))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE system_settings (
        id text NOT NULL,
        value text NOT NULL,
        description character varying(500),
        value_type character varying(50) NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        modified_by character varying(100),
        CONSTRAINT pk_system_settings PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE terms_documents (
        id uuid NOT NULL,
        term_type character varying(50) NOT NULL,
        version integer NOT NULL,
        is_active boolean NOT NULL DEFAULT FALSE,
        published_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        file_size bigint,
        duration_seconds double precision,
        file_name text,
        format text,
        height integer,
        content_url character varying(500) NOT NULL,
        width integer,
        storage_folder text NOT NULL,
        storage_public_id text NOT NULL,
        CONSTRAINT pk_terms_documents PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE user_identity_verifications (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        full_name character varying(200) NOT NULL,
        date_of_birth date NOT NULL,
        nationality character varying(100) DEFAULT 'Việt Nam',
        verified_at timestamp with time zone,
        verified_by uuid,
        rejection_reason text,
        rejection_code character varying(50),
        auto_verified boolean NOT NULL DEFAULT FALSE,
        auto_verify_score numeric(5,2),
        auto_verify_provider character varying(50),
        auto_verify_response jsonb,
        submitted_at timestamp with time zone,
        expires_at timestamp with time zone,
        attempt_count integer NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        id_expired_date date,
        id_number character varying(50) NOT NULL,
        id_issued_date date,
        id_issued_place character varying(200),
        id_type character varying(20) NOT NULL,
        gender character varying(10),
        district character varying(100) NOT NULL,
        permanent_address text NOT NULL,
        province character varying(100) NOT NULL,
        ward character varying(100) NOT NULL,
        status character varying(20) NOT NULL,
        verification_type character varying(30) NOT NULL,
        CONSTRAINT pk_user_identity_verifications PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE user_notification_preferences (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        is_enabled boolean NOT NULL DEFAULT TRUE,
        type_preferences jsonb NOT NULL DEFAULT ('{}'::jsonb),
        channels jsonb NOT NULL DEFAULT ('{"push": true, "email": true, "sms": false}'::jsonb),
        quiet_hours jsonb,
        rate_limits jsonb,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        CONSTRAINT pk_user_notification_preferences PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE user_risk_flags (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        flag_type character varying(50) NOT NULL,
        reason text,
        created_by uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        severity character varying(10) NOT NULL,
        CONSTRAINT pk_user_risk_flags PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        email_confirmed boolean NOT NULL DEFAULT FALSE,
        email_confirmed_at timestamp with time zone,
        password_hash text,
        phone_number_confirmed boolean NOT NULL DEFAULT FALSE,
        phone_number_confirmed_at timestamp with time zone,
        two_factor_enabled boolean NOT NULL DEFAULT FALSE,
        lockout_enabled boolean NOT NULL DEFAULT FALSE,
        lockout_reason text,
        lockout_end timestamp with time zone,
        access_failed_count smallint NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        deleted_at timestamp with time zone,
        version integer NOT NULL DEFAULT 0,
        normalized_email character varying(255) GENERATED ALWAYS AS (upper((email)::text)) STORED NOT NULL,
        email character varying(255) NOT NULL,
        phone_number_country_code character varying(10),
        phone_number character varying(20),
        status character varying(30) NOT NULL DEFAULT 'inactive',
        two_factor_provider character varying(30) NOT NULL DEFAULT 'none',
        normalized_user_name character varying(50) GENERATED ALWAYS AS (upper((user_name)::text)) STORED NOT NULL,
        user_name character varying(50) NOT NULL,
        CONSTRAINT pk_users PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE verification_document_type (
        id text NOT NULL,
        CONSTRAINT pk_verification_document_type PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE warehouse_storage_locations (
        id uuid NOT NULL,
        zone character varying(10) NOT NULL,
        aisle character varying(10) NOT NULL,
        shelf character varying(10) NOT NULL,
        bin character varying(10) NOT NULL,
        label character varying(50) NOT NULL,
        is_occupied boolean NOT NULL DEFAULT FALSE,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT pk_warehouse_storage_locations PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE withdrawal_requests (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        wallet_id uuid NOT NULL,
        amount numeric(18,2) NOT NULL,
        fee numeric(18,2) NOT NULL DEFAULT 0.0,
        net_amount numeric(18,2) NOT NULL,
        processed_by uuid,
        processed_at timestamp with time zone,
        rejection_reason text,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        bank_account_holder character varying(200),
        bank_account_number character varying(50),
        bank_name character varying(100),
        status character varying(20) NOT NULL,
        CONSTRAINT pk_withdrawal_requests PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE dispute_evidences (
        id uuid NOT NULL,
        dispute_id uuid NOT NULL,
        submitted_by uuid NOT NULL,
        description text,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        evidence_bytes bigint,
        evidence_duration_seconds double precision,
        evidence_file_name text,
        evidence_format text,
        evidence_height integer,
        evidence_secure_url text,
        evidence_width integer,
        evidence_folder text,
        evidence_public_id text,
        type character varying(30) NOT NULL,
        CONSTRAINT pk_dispute_evidences PRIMARY KEY (id),
        CONSTRAINT fk_dispute_evidences_disputes_dispute_id FOREIGN KEY (dispute_id) REFERENCES disputes (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE dispute_messages (
        id uuid NOT NULL,
        dispute_id uuid NOT NULL,
        sender_id uuid NOT NULL,
        message text NOT NULL,
        is_internal boolean NOT NULL DEFAULT FALSE,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT pk_dispute_messages PRIMARY KEY (id),
        CONSTRAINT fk_dispute_messages_disputes_dispute_id FOREIGN KEY (dispute_id) REFERENCES disputes (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE dispute_refunds (
        id uuid NOT NULL,
        dispute_id uuid NOT NULL,
        transaction_id uuid NOT NULL,
        reason text NOT NULL,
        approved_by uuid,
        approved_at timestamp with time zone,
        notes text,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        refund_type character varying(30) NOT NULL,
        CONSTRAINT pk_dispute_refunds PRIMARY KEY (id),
        CONSTRAINT fk_dispute_refunds_disputes_dispute_id FOREIGN KEY (dispute_id) REFERENCES disputes (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE dispute_status_history (
        id uuid NOT NULL,
        dispute_id uuid NOT NULL,
        old_status character varying(30),
        new_status character varying(30) NOT NULL,
        changed_by uuid,
        reason text,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT pk_dispute_status_history PRIMARY KEY (id),
        CONSTRAINT fk_dispute_status_history_dispute_dispute_id FOREIGN KEY (dispute_id) REFERENCES disputes (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE notification_delivery (
        id uuid NOT NULL,
        notification_id uuid NOT NULL,
        user_id uuid NOT NULL,
        channel character varying(20) NOT NULL,
        status character varying(20) DEFAULT 'pending',
        attempt_count integer NOT NULL DEFAULT 0,
        max_attempts integer NOT NULL DEFAULT 3,
        next_retry_at timestamp with time zone,
        delivery_metadata jsonb DEFAULT ('{}'::jsonb),
        error_code character varying(50),
        error_message text,
        error_details jsonb,
        scheduled_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        sent_at timestamp with time zone,
        delivered_at timestamp with time zone,
        failed_at timestamp with time zone,
        CONSTRAINT pk_notification_delivery PRIMARY KEY (id),
        CONSTRAINT fk_notification_delivery_notification_notification_id FOREIGN KEY (notification_id) REFERENCES notifications (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE escrows (
        id uuid NOT NULL,
        order_id uuid NOT NULL,
        hold_transaction_id uuid,
        release_transaction_id uuid,
        currency character varying(3) NOT NULL DEFAULT 'VND',
        held_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        released_at timestamp with time zone,
        amount numeric(18,2) NOT NULL,
        amount_currency character varying(3),
        released_to character varying(20),
        status character varying(20) NOT NULL,
        CONSTRAINT pk_escrows PRIMARY KEY (id),
        CONSTRAINT fk_escrows_orders_order_id FOREIGN KEY (order_id) REFERENCES orders (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE order_returns (
        id uuid NOT NULL,
        order_id uuid NOT NULL,
        buyer_id uuid NOT NULL,
        reason_code character varying(50) NOT NULL,
        description text,
        requested_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        approved_at timestamp with time zone,
        rejected_at timestamp with time zone,
        seller_confirmed_received_at timestamp with time zone,
        buyer_decision_due_at timestamp with time zone,
        status character varying(30) NOT NULL,
        CONSTRAINT pk_order_returns PRIMARY KEY (id),
        CONSTRAINT fk_order_returns_orders_order_id FOREIGN KEY (order_id) REFERENCES orders (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE transactions (
        id uuid NOT NULL,
        order_id uuid,
        user_id uuid NOT NULL,
        payment_method_id uuid,
        fee numeric(18,2) NOT NULL DEFAULT 0.0,
        currency character varying(3) NOT NULL DEFAULT 'VND',
        description text,
        processed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        amount numeric(18,2) NOT NULL,
        amount_currency character varying(3),
        gateway_provider character varying(50),
        gateway_response jsonb,
        gateway_transaction_id character varying(255),
        net_amount numeric(18,2) NOT NULL,
        net_amount_currency character varying(3),
        status character varying(20) NOT NULL,
        transaction_number character varying(50) NOT NULL,
        type character varying(20) NOT NULL,
        CONSTRAINT pk_transactions PRIMARY KEY (id),
        CONSTRAINT fk_transactions_payment_methods_payment_method_id FOREIGN KEY (payment_method_id) REFERENCES payment_methods (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE quartz.qrtz_triggers (
        sched_name text NOT NULL,
        trigger_name text NOT NULL,
        trigger_group text NOT NULL,
        job_name text NOT NULL,
        job_group text NOT NULL,
        description text,
        next_fire_time bigint,
        prev_fire_time bigint,
        priority integer,
        trigger_state text NOT NULL,
        trigger_type text NOT NULL,
        start_time bigint NOT NULL,
        end_time bigint,
        calendar_name text,
        misfire_instr smallint,
        job_data bytea,
        CONSTRAINT pk_qrtz_triggers PRIMARY KEY (sched_name, trigger_name, trigger_group),
        CONSTRAINT fk_qrtz_triggers_qrtz_job_details_sched_name_job_name_job_group FOREIGN KEY (sched_name, job_name, job_group) REFERENCES quartz.qrtz_job_details (sched_name, job_name, job_group) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE role_permissions (
        role_name text NOT NULL,
        permission_code text NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        modified_at timestamp with time zone,
        CONSTRAINT pk_role_permissions PRIMARY KEY (role_name, permission_code),
        CONSTRAINT fk_role_permissions_permissions_permission_code FOREIGN KEY (permission_code) REFERENCES permissions (code) ON DELETE CASCADE,
        CONSTRAINT fk_role_permissions_roles_role_name FOREIGN KEY (role_name) REFERENCES roles (name) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE review_images (
        id uuid NOT NULL,
        review_id uuid NOT NULL,
        image_url character varying(500) NOT NULL,
        sort_order integer NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT pk_review_images PRIMARY KEY (id),
        CONSTRAINT fk_review_images_seller_review_review_id FOREIGN KEY (review_id) REFERENCES seller_reviews (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE review_reports (
        id uuid NOT NULL,
        review_id uuid NOT NULL,
        reporter_id uuid NOT NULL,
        description text,
        reviewed_by uuid,
        reviewed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        reason character varying(50) NOT NULL,
        status character varying(20),
        CONSTRAINT pk_review_reports PRIMARY KEY (id),
        CONSTRAINT fk_review_reports_seller_review_review_id FOREIGN KEY (review_id) REFERENCES seller_reviews (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE review_votes (
        id uuid NOT NULL,
        review_id uuid NOT NULL,
        user_id uuid NOT NULL,
        is_helpful boolean NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT pk_review_votes PRIMARY KEY (id),
        CONSTRAINT fk_review_votes_seller_review_review_id FOREIGN KEY (review_id) REFERENCES seller_reviews (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE user_terms_acceptances (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        term_document_id uuid NOT NULL,
        accepted_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        ip_address inet,
        user_agent text,
        CONSTRAINT pk_user_terms_acceptances PRIMARY KEY (id),
        CONSTRAINT fk_user_terms_acceptances_terms_document_term_document_id FOREIGN KEY (term_document_id) REFERENCES terms_documents (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE user_identity_verification_history (
        id uuid NOT NULL,
        verification_id uuid NOT NULL,
        old_status character varying(20),
        new_status character varying(20),
        changed_fields jsonb,
        notes text,
        performed_by uuid,
        ip_address inet,
        user_agent text,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        action character varying(30) NOT NULL,
        performed_by_type character varying(20),
        CONSTRAINT pk_user_identity_verification_history PRIMARY KEY (id),
        CONSTRAINT fk_user_identity_verification_history_user_identity_verificati FOREIGN KEY (verification_id) REFERENCES user_identity_verifications (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE seller_profiles (
        id uuid NOT NULL,
        store_name character varying(200) NOT NULL,
        store_description text NOT NULL DEFAULT 'There are no description for this store.',
        verified_at timestamp with time zone,
        total_sales_count integer NOT NULL DEFAULT 0,
        total_sales_amount numeric(18,2) NOT NULL DEFAULT 0.0,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        status character varying(30) NOT NULL,
        CONSTRAINT pk_seller_profiles PRIMARY KEY (id),
        CONSTRAINT fk_seller_profiles_users_id FOREIGN KEY (id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE user_addresses (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        type character varying(10) NOT NULL DEFAULT 'other',
        is_default boolean NOT NULL DEFAULT FALSE,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        city character varying(120) NOT NULL,
        district character varying(100) NOT NULL,
        postal_code character varying(10),
        address character varying(255) NOT NULL,
        ward character varying(100) NOT NULL,
        recipient_name character varying(100) NOT NULL,
        phone_number_country_code character varying(10) NOT NULL,
        phone_number character varying(20) NOT NULL,
        CONSTRAINT pk_user_addresses PRIMARY KEY (id),
        CONSTRAINT fk_user_addresses_user_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE user_login_history (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        ip_address inet NOT NULL,
        user_agent text NOT NULL,
        login_at timestamp with time zone NOT NULL,
        status character varying(30) NOT NULL,
        CONSTRAINT pk_user_login_history PRIMARY KEY (id),
        CONSTRAINT fk_user_login_history_user_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE user_permissions (
        user_id uuid NOT NULL,
        permission_code text NOT NULL,
        is_allowed boolean NOT NULL DEFAULT TRUE,
        CONSTRAINT pk_user_permissions PRIMARY KEY (user_id, permission_code),
        CONSTRAINT fk_user_permissions_permissions_permission_code FOREIGN KEY (permission_code) REFERENCES permissions (code) ON DELETE CASCADE,
        CONSTRAINT fk_user_permissions_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE user_profiles (
        id uuid NOT NULL,
        date_of_birth date,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        avatar_url text,
        gender text,
        display_name character varying(100),
        first_name character varying(50),
        last_name character varying(50),
        CONSTRAINT pk_user_profiles PRIMARY KEY (id),
        CONSTRAINT fk_user_profiles_users_id FOREIGN KEY (id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE user_roles (
        user_id uuid NOT NULL,
        role_id text NOT NULL,
        assigned_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id),
        CONSTRAINT fk_user_roles_roles_role_name FOREIGN KEY (role_id) REFERENCES roles (name) ON DELETE CASCADE,
        CONSTRAINT fk_user_roles_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE user_sessions (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        device_id uuid NOT NULL,
        user_agent text NOT NULL,
        ip_address inet NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        expires_at timestamp with time zone NOT NULL,
        absolute_expires_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        last_rotated_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        revoked_at timestamp with time zone,
        revoked_reason text,
        CONSTRAINT pk_user_sessions PRIMARY KEY (id),
        CONSTRAINT fk_user_sessions_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE user_identity_verification_documents (
        id uuid NOT NULL,
        verification_id uuid NOT NULL,
        document_type_id text,
        resource_type text NOT NULL,
        file_hash character varying(64),
        mime_type character varying(50),
        verification_notes text,
        extracted_data jsonb,
        uploaded_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        verified_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        bytes bigint,
        duration_seconds double precision,
        file_name text,
        format text,
        height integer,
        secure_url text NOT NULL,
        width integer,
        folder text NOT NULL,
        public_id text NOT NULL,
        verification_status character varying(20) NOT NULL DEFAULT 'pending',
        CONSTRAINT pk_user_identity_verification_documents PRIMARY KEY (id),
        CONSTRAINT fk_user_identity_verification_documents_user_identity_verifica FOREIGN KEY (verification_id) REFERENCES user_identity_verifications (id) ON DELETE CASCADE,
        CONSTRAINT fk_user_identity_verification_documents_verification_document_ FOREIGN KEY (document_type_id) REFERENCES verification_document_type (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE warehouse_items (
        id uuid NOT NULL,
        item_id uuid NOT NULL,
        inbound_shipment_id uuid NOT NULL,
        storage_location_id uuid,
        condition_on_arrival character varying(20) NOT NULL,
        inspection_notes character varying(1000),
        inspection_images jsonb NOT NULL,
        status character varying(20) NOT NULL,
        inspected_by uuid,
        inspected_at timestamp with time zone,
        received_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        CONSTRAINT pk_warehouse_items PRIMARY KEY (id),
        CONSTRAINT fk_warehouse_items_inbound_shipments_inbound_shipment_id FOREIGN KEY (inbound_shipment_id) REFERENCES inbound_shipments (id) ON DELETE RESTRICT,
        CONSTRAINT fk_warehouse_items_warehouse_storage_location_storage_location FOREIGN KEY (storage_location_id) REFERENCES warehouse_storage_locations (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE escrow_release_events (
        id uuid NOT NULL,
        escrow_id uuid NOT NULL,
        trigger_source_type character varying(50) NOT NULL,
        trigger_source_id uuid,
        amount numeric(18,2) NOT NULL,
        created_by uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        release_type character varying(30) NOT NULL,
        CONSTRAINT pk_escrow_release_events PRIMARY KEY (id),
        CONSTRAINT fk_escrow_release_events_escrows_escrow_id FOREIGN KEY (escrow_id) REFERENCES escrows (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE wallets (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        balance numeric(18,2) NOT NULL DEFAULT 0.0,
        pending_balance numeric(18,2) NOT NULL DEFAULT 0.0,
        currency character varying(3) NOT NULL DEFAULT 'VND',
        is_active boolean NOT NULL DEFAULT TRUE,
        version integer NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        transaction_id uuid,
        CONSTRAINT pk_wallets PRIMARY KEY (id),
        CONSTRAINT chk_non_negative_balance CHECK (balance >= 0),
        CONSTRAINT chk_non_negative_pending CHECK (pending_balance >= 0),
        CONSTRAINT fk_wallets_transactions_transaction_id FOREIGN KEY (transaction_id) REFERENCES transactions (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE quartz.qrtz_blob_triggers (
        sched_name text NOT NULL,
        trigger_name text NOT NULL,
        trigger_group text NOT NULL,
        blob_data bytea,
        CONSTRAINT pk_qrtz_blob_triggers PRIMARY KEY (sched_name, trigger_name, trigger_group),
        CONSTRAINT fk_qrtz_blob_triggers_qrtz_triggers_sched_name_trigger_name_tr FOREIGN KEY (sched_name, trigger_name, trigger_group) REFERENCES quartz.qrtz_triggers (sched_name, trigger_name, trigger_group) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE quartz.qrtz_cron_triggers (
        sched_name text NOT NULL,
        trigger_name text NOT NULL,
        trigger_group text NOT NULL,
        cron_expression text NOT NULL,
        time_zone_id text,
        CONSTRAINT pk_qrtz_cron_triggers PRIMARY KEY (sched_name, trigger_name, trigger_group),
        CONSTRAINT fk_qrtz_cron_triggers_qrtz_triggers_sched_name_trigger_name_tr FOREIGN KEY (sched_name, trigger_name, trigger_group) REFERENCES quartz.qrtz_triggers (sched_name, trigger_name, trigger_group) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE quartz.qrtz_simple_triggers (
        sched_name text NOT NULL,
        trigger_name text NOT NULL,
        trigger_group text NOT NULL,
        repeat_count bigint NOT NULL,
        repeat_interval bigint NOT NULL,
        times_triggered bigint NOT NULL,
        CONSTRAINT pk_qrtz_simple_triggers PRIMARY KEY (sched_name, trigger_name, trigger_group),
        CONSTRAINT fk_qrtz_simple_triggers_qrtz_triggers_sched_name_trigger_name_ FOREIGN KEY (sched_name, trigger_name, trigger_group) REFERENCES quartz.qrtz_triggers (sched_name, trigger_name, trigger_group) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE quartz.qrtz_simprop_triggers (
        sched_name text NOT NULL,
        trigger_name text NOT NULL,
        trigger_group text NOT NULL,
        str_prop_1 text,
        str_prop_2 text,
        str_prop_3 text,
        int_prop_1 integer,
        int_prop_2 integer,
        long_prop_1 bigint,
        long_prop_2 bigint,
        dec_prop_1 numeric,
        dec_prop_2 numeric,
        bool_prop_1 bool,
        bool_prop_2 bool,
        time_zone_id text,
        CONSTRAINT pk_qrtz_simprop_triggers PRIMARY KEY (sched_name, trigger_name, trigger_group),
        CONSTRAINT fk_qrtz_simprop_triggers_qrtz_triggers_sched_name_trigger_name FOREIGN KEY (sched_name, trigger_name, trigger_group) REFERENCES quartz.qrtz_triggers (sched_name, trigger_name, trigger_group) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE user_refresh_tokens (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        token_hash text NOT NULL,
        session_id uuid NOT NULL,
        parent_token_id uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        expires_at timestamp with time zone NOT NULL,
        revoked_at timestamp with time zone,
        revoked_reason text,
        created_by_ip inet NOT NULL,
        revoked_by_ip inet,
        rotation_counter integer NOT NULL DEFAULT 0,
        is_used boolean NOT NULL DEFAULT FALSE,
        used_at timestamp with time zone,
        CONSTRAINT pk_user_refresh_tokens PRIMARY KEY (id),
        CONSTRAINT chk_expires_after_created CHECK (expires_at > created_at),
        CONSTRAINT chk_revoked_after_created CHECK (revoked_at IS NULL OR revoked_at >= created_at),
        CONSTRAINT chk_used_after_created CHECK (used_at IS NULL OR used_at >= created_at),
        CONSTRAINT fk_user_refresh_tokens_user_refresh_tokens_parent_token_id FOREIGN KEY (parent_token_id) REFERENCES user_refresh_tokens (id) ON DELETE SET NULL,
        CONSTRAINT fk_user_refresh_tokens_user_sessions_session_id FOREIGN KEY (session_id) REFERENCES user_sessions (id) ON DELETE CASCADE,
        CONSTRAINT fk_user_refresh_tokens_user_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE items (
        id uuid NOT NULL,
        seller_id uuid NOT NULL,
        category_id uuid NOT NULL,
        description text,
        quantity integer NOT NULL,
        attributes jsonb,
        submitted_at timestamp with time zone,
        reviewed_at timestamp with time zone,
        reviewed_by uuid,
        rejection_reason text,
        resubmission_count integer NOT NULL,
        assigned_admin_id uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        warehouse_item_id uuid NOT NULL,
        condition text NOT NULL,
        status text NOT NULL DEFAULT 'draft',
        title text NOT NULL,
        CONSTRAINT pk_items PRIMARY KEY (id),
        CONSTRAINT chk_items_resubmission_count CHECK (resubmission_count >= 0),
        CONSTRAINT fk_items_categories_category_id FOREIGN KEY (category_id) REFERENCES categories (id) ON DELETE CASCADE,
        CONSTRAINT fk_items_warehouse_item_warehouse_item_id FOREIGN KEY (warehouse_item_id) REFERENCES warehouse_items (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE outbound_shipments (
        id uuid NOT NULL,
        order_id uuid NOT NULL,
        warehouse_item_id uuid NOT NULL,
        provider_code character varying(20) NOT NULL,
        client_order_code character varying(100) NOT NULL,
        carrier_tracking_number character varying(100),
        shipping_label_url character varying(500),
        shipping_method character varying(50),
        recipient_carrier_address_data jsonb,
        shipping_fee numeric(18,2) NOT NULL DEFAULT 0.0,
        insurance_value numeric(18,2) NOT NULL DEFAULT 0.0,
        cod_amount numeric(18,2) NOT NULL DEFAULT 0.0,
        ghn_payment_type character varying(5),
        ghn_handling_note character varying(30),
        extra_data jsonb NOT NULL,
        status character varying(30) NOT NULL,
        packed_by uuid,
        packed_at timestamp with time zone,
        dispatched_at timestamp with time zone,
        estimated_delivery_at timestamp with time zone,
        delivered_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        height_cm integer,
        length_cm integer,
        weight_grams integer NOT NULL,
        width_cm integer,
        CONSTRAINT pk_outbound_shipments PRIMARY KEY (id),
        CONSTRAINT chk_outbound_shipments_cod_amount CHECK (cod_amount >= 0),
        CONSTRAINT chk_outbound_shipments_insurance_value CHECK (insurance_value >= 0),
        CONSTRAINT chk_outbound_shipments_shipping_fee CHECK (shipping_fee >= 0),
        CONSTRAINT fk_outbound_shipments_orders_order_id FOREIGN KEY (order_id) REFERENCES orders (id) ON DELETE CASCADE,
        CONSTRAINT fk_outbound_shipments_warehouse_item_warehouse_item_id FOREIGN KEY (warehouse_item_id) REFERENCES warehouse_items (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE wallet_transactions (
        id uuid NOT NULL,
        wallet_id uuid NOT NULL,
        transaction_id uuid,
        amount numeric(18,2) NOT NULL,
        balance_before numeric(18,2) NOT NULL,
        balance_after numeric(18,2) NOT NULL,
        description text,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        type character varying(20) NOT NULL,
        CONSTRAINT pk_wallet_transactions PRIMARY KEY (id),
        CONSTRAINT fk_wallet_transactions_transactions_transaction_id FOREIGN KEY (transaction_id) REFERENCES transactions (id),
        CONSTRAINT fk_wallet_transactions_wallets_wallet_id FOREIGN KEY (wallet_id) REFERENCES wallets (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE auctions (
        id uuid NOT NULL,
        item_id uuid NOT NULL,
        actual_end_time timestamp with time zone,
        winner_id uuid,
        assigned_admin_id uuid,
        assigned_at timestamp with time zone,
        is_featured boolean NOT NULL DEFAULT FALSE,
        view_count integer NOT NULL DEFAULT 0,
        bid_count integer NOT NULL DEFAULT 0,
        watch_count integer NOT NULL DEFAULT 0,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        auction_type text NOT NULL DEFAULT 'regular',
        auto_extend boolean NOT NULL,
        end_time timestamp with time zone NOT NULL,
        info_extension_count integer NOT NULL,
        extension_minutes integer NOT NULL,
        start_time timestamp with time zone NOT NULL,
        qualification_end_at timestamp with time zone,
        qualification_start_at timestamp with time zone,
        bid_increment numeric(18,2) NOT NULL,
        buy_now_price numeric(18,2),
        currency character varying(3) NOT NULL,
        current_price numeric(18,2) NOT NULL,
        reserve_price numeric(18,2),
        starting_price numeric(18,2) NOT NULL,
        priority_reason jsonb NOT NULL,
        priority numeric NOT NULL,
        status character varying(20) NOT NULL DEFAULT 'draft',
        CONSTRAINT pk_auctions PRIMARY KEY (id),
        CONSTRAINT chk_buy_now_gt_starting CHECK (buy_now_price IS NULL OR buy_now_price > starting_price),
        CONSTRAINT chk_current_gte_starting CHECK (current_price >= starting_price),
        CONSTRAINT chk_end_after_start CHECK (end_time > start_time),
        CONSTRAINT chk_positive_bid_increment CHECK (bid_increment > 0),
        CONSTRAINT chk_qualification_window CHECK (qualification_start_at IS NULL OR qualification_end_at IS NULL OR qualification_end_at > qualification_start_at),
        CONSTRAINT chk_reserve_gte_starting CHECK (reserve_price IS NULL OR reserve_price >= starting_price),
        CONSTRAINT fk_auctions_item_item_id FOREIGN KEY (item_id) REFERENCES items (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE item_media (
        id uuid NOT NULL,
        item_id uuid NOT NULL,
        resource_type text NOT NULL,
        is_primary boolean NOT NULL,
        sort_order integer NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        bytes bigint,
        duration_seconds double precision,
        file_name text,
        format text,
        height integer,
        secure_url text NOT NULL,
        width integer,
        folder text NOT NULL,
        public_id text NOT NULL,
        CONSTRAINT pk_item_media PRIMARY KEY (id),
        CONSTRAINT fk_item_media_item_item_id FOREIGN KEY (item_id) REFERENCES items (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE item_moderation_reviews (
        id uuid NOT NULL,
        item_id uuid NOT NULL,
        reviewer_id uuid NOT NULL,
        reason text,
        old_status text,
        new_status text,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        action text NOT NULL,
        CONSTRAINT pk_item_moderation_reviews PRIMARY KEY (id),
        CONSTRAINT fk_item_moderation_reviews_items_item_id FOREIGN KEY (item_id) REFERENCES items (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE item_questions (
        id uuid NOT NULL,
        item_id uuid NOT NULL,
        asker_id uuid NOT NULL,
        question text NOT NULL,
        answer text,
        answered_at timestamp with time zone,
        is_public boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT pk_item_questions PRIMARY KEY (id),
        CONSTRAINT fk_item_questions_items_item_id FOREIGN KEY (item_id) REFERENCES items (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE shipment_tracking_events (
        id uuid NOT NULL,
        shipment_type character varying(10) NOT NULL,
        shipment_id uuid NOT NULL,
        provider_code character varying(20) NOT NULL,
        carrier_status_raw character varying(100) NOT NULL,
        carrier_status_desc character varying(255),
        normalized_status character varying(20) NOT NULL,
        location character varying(255),
        reason_code character varying(50),
        reason_description character varying(500),
        event_time timestamp with time zone NOT NULL,
        raw_payload jsonb NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        inbound_shipment_id uuid,
        outbound_shipment_id uuid,
        CONSTRAINT pk_shipment_tracking_events PRIMARY KEY (id),
        CONSTRAINT fk_shipment_tracking_events_inbound_shipments_inbound_shipment FOREIGN KEY (inbound_shipment_id) REFERENCES inbound_shipments (id),
        CONSTRAINT fk_shipment_tracking_events_outbound_shipments_outbound_shipme FOREIGN KEY (outbound_shipment_id) REFERENCES outbound_shipments (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE auction_auto_bids (
        id uuid NOT NULL,
        auction_id uuid NOT NULL,
        bidder_id uuid NOT NULL,
        is_enabled boolean NOT NULL DEFAULT TRUE,
        total_auto_bids integer NOT NULL DEFAULT 0,
        last_auto_bid_at timestamp with time zone,
        stop_reason text,
        stopped_at timestamp with time zone,
        last_validation_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        modified_at timestamp with time zone,
        currency character varying(3) NOT NULL,
        current_amount numeric(18,2) NOT NULL,
        increment_amount numeric(18,2),
        max_amount numeric(18,2) NOT NULL,
        reserved_amount numeric(18,2) NOT NULL,
        status character varying(20) NOT NULL DEFAULT 'active',
        CONSTRAINT pk_auction_auto_bids PRIMARY KEY (id),
        CONSTRAINT fk_auction_auto_bids_auctions_auction_id FOREIGN KEY (auction_id) REFERENCES auctions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE auction_deposits (
        id uuid NOT NULL,
        auction_id uuid NOT NULL,
        bidder_id uuid NOT NULL,
        transaction_id uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        released_at timestamp with time zone,
        amount numeric(18,2) NOT NULL,
        currency character varying(3) NOT NULL,
        status character varying(20) NOT NULL,
        CONSTRAINT pk_auction_deposits PRIMARY KEY (id),
        CONSTRAINT auction_deposits_transaction_id_fkey FOREIGN KEY (transaction_id) REFERENCES transactions (id),
        CONSTRAINT auction_deposits_user_id_fkey FOREIGN KEY (bidder_id) REFERENCES users (id),
        CONSTRAINT fk_auction_deposits_auctions_auction_id FOREIGN KEY (auction_id) REFERENCES auctions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE auction_emergencies (
        id uuid NOT NULL,
        auction_id uuid NOT NULL,
        triggered_by uuid,
        trigger_source character varying(255) NOT NULL,
        reason character varying(255) NOT NULL,
        triggered_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        resolved_at timestamp with time zone,
        status text NOT NULL,
        CONSTRAINT pk_auction_emergencies PRIMARY KEY (id),
        CONSTRAINT auction_emergencies_triggered_by_fkey FOREIGN KEY (triggered_by) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT fk_auction_emergencies_auctions_auction_id FOREIGN KEY (auction_id) REFERENCES auctions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE auction_participants (
        id uuid NOT NULL,
        auction_id uuid NOT NULL,
        user_id uuid NOT NULL,
        role_in_auction character varying(255) NOT NULL,
        joined_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        qualified_at timestamp with time zone,
        rejected_reason text,
        join_status text NOT NULL,
        qualification_status text NOT NULL,
        CONSTRAINT pk_auction_participants PRIMARY KEY (id),
        CONSTRAINT fk_auction_participants_auctions_auction_id FOREIGN KEY (auction_id) REFERENCES auctions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE auction_relist_history (
        id uuid NOT NULL,
        auction_id uuid NOT NULL,
        relist_no integer NOT NULL,
        reason text,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT pk_auction_relist_history PRIMARY KEY (id),
        CONSTRAINT fk_auction_relist_history_auction_auction_id FOREIGN KEY (auction_id) REFERENCES auctions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE auction_watchers (
        id uuid NOT NULL,
        auction_id uuid NOT NULL,
        user_id uuid NOT NULL,
        notify_on_bid boolean NOT NULL DEFAULT TRUE,
        notify_on_end boolean NOT NULL DEFAULT TRUE,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT pk_auction_watchers PRIMARY KEY (id),
        CONSTRAINT fk_auction_watchers_auctions_auction_id FOREIGN KEY (auction_id) REFERENCES auctions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE auction_winner_offers (
        id uuid NOT NULL,
        auction_id uuid NOT NULL,
        user_id uuid NOT NULL,
        rank_no integer NOT NULL,
        offered_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        expires_at timestamp with time zone,
        responded_at timestamp with time zone,
        offer_status text NOT NULL DEFAULT 'offered',
        CONSTRAINT pk_auction_winner_offers PRIMARY KEY (id),
        CONSTRAINT fk_auction_winner_offers_auctions_auction_id FOREIGN KEY (auction_id) REFERENCES auctions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE sealed_bids (
        id uuid NOT NULL,
        auction_id uuid NOT NULL,
        bidder_id uuid NOT NULL,
        amount_encrypted text NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        revealed_at timestamp with time zone,
        revealed_by uuid,
        status character varying(20) NOT NULL DEFAULT 'submitted',
        CONSTRAINT pk_sealed_bids PRIMARY KEY (id),
        CONSTRAINT fk_sealed_bids_auctions_auction_id FOREIGN KEY (auction_id) REFERENCES auctions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE bids (
        id uuid NOT NULL,
        auction_id uuid NOT NULL,
        bidder_id uuid NOT NULL,
        auto_bid_id uuid,
        is_auto_bid boolean GENERATED ALWAYS AS ((auto_bid_id IS NOT NULL)) STORED NOT NULL,
        ip_address inet,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        amount numeric(18,2) NOT NULL,
        currency character varying(3) NOT NULL,
        status character varying(20) NOT NULL DEFAULT 'active',
        CONSTRAINT pk_bids PRIMARY KEY (id),
        CONSTRAINT fk_bids_auction_auto_bids_auto_bid_id FOREIGN KEY (auto_bid_id) REFERENCES auction_auto_bids (id) ON DELETE SET NULL,
        CONSTRAINT fk_bids_auctions_auction_id FOREIGN KEY (auction_id) REFERENCES auctions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE auction_emergency_actions (
        id uuid NOT NULL,
        emergency_id uuid NOT NULL,
        action_type character varying(255) NOT NULL,
        payload jsonb NOT NULL DEFAULT ('{}'::jsonb),
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT pk_auction_emergency_actions PRIMARY KEY (id),
        CONSTRAINT fk_auction_emergency_actions_auction_emergencies_emergency_id FOREIGN KEY (emergency_id) REFERENCES auction_emergencies (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE auction_price_history (
        id uuid NOT NULL,
        auction_id uuid NOT NULL,
        bid_id uuid,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        price numeric(18,2) NOT NULL,
        currency character varying(3) NOT NULL,
        CONSTRAINT pk_auction_price_history PRIMARY KEY (id),
        CONSTRAINT fk_auction_price_history_auction_auction_id FOREIGN KEY (auction_id) REFERENCES auctions (id) ON DELETE CASCADE,
        CONSTRAINT fk_auction_price_history_bid_bid_id FOREIGN KEY (bid_id) REFERENCES bids (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE TABLE bid_events (
        id uuid NOT NULL,
        bid_id uuid NOT NULL,
        event_type character varying(50) NOT NULL,
        reason_code text,
        payload jsonb NOT NULL,
        created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
        CONSTRAINT pk_bid_events PRIMARY KEY (id),
        CONSTRAINT fk_bid_events_bids_bid_id FOREIGN KEY (bid_id) REFERENCES bids (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_admin_review_tasks_assigned_to ON admin_review_tasks (assigned_to);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_admin_review_tasks_due_at ON admin_review_tasks (due_at) WHERE due_at IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_admin_review_tasks_entity ON admin_review_tasks (entity_type, entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auction_auto_bids_auction_id ON auction_auto_bids (auction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auction_auto_bids_bidder_id ON auction_auto_bids (bidder_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auction_auto_bids_last_validation_at ON auction_auto_bids (last_validation_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auction_auto_bids_stopped_at ON auction_auto_bids (stopped_at) WHERE stopped_at IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX ix_auction_auto_bids_auction_id_bidder_id ON auction_auto_bids (auction_id, bidder_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX ix_auction_deposits_auction_id_bidder_id ON auction_deposits (auction_id, bidder_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_auction_deposits_bidder_id ON auction_deposits (bidder_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_auction_deposits_transaction_id ON auction_deposits (transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auction_emergencies_auction_id ON auction_emergencies (auction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_auction_emergencies_triggered_by ON auction_emergencies (triggered_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auction_emergency_actions_emergency_id ON auction_emergency_actions (emergency_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auction_participants_auction_id ON auction_participants (auction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX idx_auction_participants_auction_user ON auction_participants (auction_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auction_participants_user_id ON auction_participants (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_auction_price_history_auction_id ON auction_price_history (auction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_auction_price_history_bid_id ON auction_price_history (bid_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX idx_auction_relist_history_auction_id ON auction_relist_history (auction_id, relist_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auction_relist_history_created_at ON auction_relist_history (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX ix_auction_watchers_auction_id_user_id ON auction_watchers (auction_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX uq_auction_winner_offers_auction_rank ON auction_winner_offers (auction_id, rank_no);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX uq_auction_winner_offers_auction_user ON auction_winner_offers (auction_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_auctions_item_id ON auctions (item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_audit_logs_action ON audit_logs (action, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_audit_logs_actor ON audit_logs (actor_user_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_audit_logs_entity ON audit_logs (entity_type, entity_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_bid_events_bid ON bid_events (bid_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_bid_events_bid_created_at ON bid_events (event_type, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_bids_auction ON bids (auction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_bids_auction_created_at ON bids (auction_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_bids_bidder ON bids (bidder_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_bids_auto_bid_id ON bids (auto_bid_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_buyer_reviews_buyer ON buyer_reviews (buyer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX uq_buyer_reviews_order_reviewer ON buyer_reviews (order_id, reviewer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_categories_parent_id ON categories (parent_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_dispute_evidences_dispute_id ON dispute_evidences (dispute_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_dispute_messages_dispute ON dispute_messages (dispute_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_dispute_refunds_dispute_id ON dispute_refunds (dispute_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX uq_dispute_refunds_transaction_id ON dispute_refunds (transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_dispute_status_history_dispute_id ON dispute_status_history (dispute_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_disputes_assigned_to ON disputes (assigned_to);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_disputes_complainant_id ON disputes (complainant_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_disputes_order_id ON disputes (order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_disputes_respondent_id ON disputes (respondent_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_escrow_release_events_escrow ON escrow_release_events (escrow_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_escrow_release_events_trigger ON escrow_release_events (trigger_source_type, trigger_source_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_escrows_order_id ON escrows (order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_inbound_shipments_carrier_tracking_number ON inbound_shipments (carrier_tracking_number) WHERE carrier_tracking_number IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_inbound_shipments_item_id ON inbound_shipments (item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_inbound_shipments_seller_id ON inbound_shipments (seller_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_inbound_shipments_status ON inbound_shipments (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX idx_unique_inbound_shipments_client_order_code ON inbound_shipments (client_order_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_item_media_item_id ON item_media (item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_item_moderation_reviews_created_at ON item_moderation_reviews (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_item_moderation_reviews_item_id ON item_moderation_reviews (item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_item_moderation_reviews_reviewer_id ON item_moderation_reviews (reviewer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_item_questions_item_id ON item_questions (item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_items_assigned_admin_id ON items (assigned_admin_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_items_category_id ON items (category_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_items_reviewed_by ON items (reviewed_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_items_seller_id ON items (seller_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_items_submitted_at ON items (submitted_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_items_warehouse_item_id ON items (warehouse_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_media_uploads_expired ON media_uploads (is_confirmed, expires_at) WHERE is_confirmed = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_media_uploads_orphan ON media_uploads (is_confirmed, is_linked, confirmed_at) WHERE is_confirmed = true AND is_linked = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_media_uploads_user_id ON media_uploads (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_monitoring_alerts_entity ON monitoring_alerts (entity_type, entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_delivery_notification ON notification_delivery (notification_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_delivery_retry ON notification_delivery (next_retry_at) WHERE status = 'failed' AND next_retry_at IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_delivery_status_scheduled ON notification_delivery (status, scheduled_at) WHERE status = 'pending';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_delivery_user_channel ON notification_delivery (user_id, channel);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_notifications_entity ON notifications (entity_type, entity_id) WHERE entity_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_notifications_type_event ON notifications (notification_type, event_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_notifications_user_created ON notifications (user_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_order_returns_buyer ON order_returns (buyer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX uq_order_returns_order ON order_returns (order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_orders_buyer ON orders (buyer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_orders_last_payment_attempt_at ON orders (last_payment_attempt_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_orders_payment_due_at ON orders (payment_due_at) WHERE status = 'pending_payment';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_orders_seller ON orders (seller_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_outbound_shipments_carrier_tracking_number ON outbound_shipments (carrier_tracking_number) WHERE carrier_tracking_number IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_outbound_shipments_order_id ON outbound_shipments (order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_outbound_shipments_status ON outbound_shipments (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX idx_unique_outbound_shipments_client_order_code ON outbound_shipments (client_order_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX idx_unique_outbound_shipments_warehouse_item_id ON outbound_shipments (warehouse_item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_outbox_cleanup ON outbox_messages (occurred_at, id) WHERE processed_at IS NOT NULL AND error IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_outbox_messages_unprocessed ON outbox_messages (occurred_at) WHERE processed_at IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_qrtz_ft_job_group ON quartz.qrtz_fired_triggers (job_group);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_qrtz_ft_job_name ON quartz.qrtz_fired_triggers (job_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_qrtz_ft_job_req_recovery ON quartz.qrtz_fired_triggers (requests_recovery);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_qrtz_ft_trig_group ON quartz.qrtz_fired_triggers (trigger_group);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_qrtz_ft_trig_inst_name ON quartz.qrtz_fired_triggers (instance_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_qrtz_ft_trig_name ON quartz.qrtz_fired_triggers (trigger_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_qrtz_ft_trig_nm_gp ON quartz.qrtz_fired_triggers (sched_name, trigger_name, trigger_group);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_qrtz_j_req_recovery ON quartz.qrtz_job_details (requests_recovery);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_qrtz_t_next_fire_time ON quartz.qrtz_triggers (next_fire_time);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_qrtz_t_nft_st ON quartz.qrtz_triggers (next_fire_time, trigger_state);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_qrtz_t_state ON quartz.qrtz_triggers (trigger_state);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_qrtz_triggers_sched_name_job_name_job_group ON quartz.qrtz_triggers (sched_name, job_name, job_group);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_reports_entity ON reports (entity_type, entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_reports_reporter ON reports (reporter_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_review_images_review_id ON review_images (review_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_review_queue_assigned_to ON review_queue (assigned_to);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_review_queue_entity ON review_queue (entity_type, entity_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_review_reports_review_id ON review_reports (review_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX uq_review_votes_review_user ON review_votes (review_id, user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_role_permissions_permission_code ON role_permissions (permission_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_sealed_bids_auction_created_at ON sealed_bids (auction_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_sealed_bids_bidder ON sealed_bids (bidder_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX ix_sealed_bids_auction_id_bidder_id ON sealed_bids (auction_id, bidder_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX uq_seller_rating_summary_seller ON seller_rating_summary (seller_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_seller_reviews_order ON seller_reviews (order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_seller_reviews_reviewer ON seller_reviews (reviewer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_seller_reviews_seller ON seller_reviews (seller_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX uq_seller_reviews_order_reviewer ON seller_reviews (order_id, reviewer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_shipment_tracking_events_inbound_shipment_id ON shipment_tracking_events (inbound_shipment_id, event_time);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_shipment_tracking_events_outbound_shipment_id ON shipment_tracking_events (outbound_shipment_id, event_time);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_shipment_tracking_events_shipment_carrier_status ON shipment_tracking_events (shipment_id, carrier_status_raw, event_time);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_shipping_provider_configs_default_active ON shipping_provider_configs (is_default) WHERE is_default = TRUE AND is_active = TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX idx_unique_shipping_provider_configs_provider_code ON shipping_provider_configs (provider_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_terms_documents_published_at ON terms_documents (published_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_terms_documents_type ON terms_documents (term_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX uq_terms_documents_type_version ON terms_documents (term_type, version);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_transactions_order ON transactions (order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_transactions_user ON transactions (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_transactions_payment_method_id ON transactions (payment_method_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX idx_unique_default_address_per_user ON user_addresses (user_id) WHERE is_default = TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_user_addresses_user_id_type ON user_addresses (user_id, type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_identity_verification_documents_document_type_id ON user_identity_verification_documents (document_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_identity_verification_documents_verification_id ON user_identity_verification_documents (verification_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_user_identity_verification_history_created ON user_identity_verification_history (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_user_identity_verification_history_verification ON user_identity_verification_history (verification_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_user_identity_verifications_submitted ON user_identity_verifications (submitted_at) WHERE status = 'submitted';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_user_identity_verifications_user ON user_identity_verifications (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_login_history_user_id ON user_login_history (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX uq_user_notification_preferences_user ON user_notification_preferences (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_permissions_permission_code ON user_permissions (permission_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_refresh_session_id ON user_refresh_tokens (session_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_refresh_tokens_expires_at ON user_refresh_tokens (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_refresh_tokens_parent_token_id ON user_refresh_tokens (parent_token_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_refresh_tokens_session_session_id_created_at ON user_refresh_tokens (session_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX ix_user_refresh_tokens_token_hash ON user_refresh_tokens (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_refresh_tokens_user_id_is_used ON user_refresh_tokens (user_id, is_used);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_user_risk_flags_user ON user_risk_flags (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_roles_role_id ON user_roles (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_sessions_absolute_expires_at ON user_sessions (absolute_expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_sessions_expires_at ON user_sessions (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_sessions_user ON user_sessions (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_user_sessions_user_active ON user_sessions (user_id, is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_user_terms_acceptances_accepted_at ON user_terms_acceptances (accepted_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_user_terms_acceptances_term_document ON user_terms_acceptances (term_document_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_user_terms_acceptances_user ON user_terms_acceptances (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX uq_user_terms_acceptances_user_term ON user_terms_acceptances (user_id, term_document_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_wallet_transactions_wallet ON wallet_transactions (wallet_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_wallet_transactions_transaction_id ON wallet_transactions (transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX ix_wallets_transaction_id ON wallets (transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX uq_wallets_user_id ON wallets (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX idx_unique_warehouse_items_inbound_shipment_id ON warehouse_items (inbound_shipment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_warehouse_items_item_id ON warehouse_items (item_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_warehouse_items_status ON warehouse_items (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_warehouse_items_storage_location_id ON warehouse_items (storage_location_id) WHERE storage_location_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX idx_unique_warehouse_storage_locations_label ON warehouse_storage_locations (label);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_warehouse_storage_locations_vacant ON warehouse_storage_locations (is_occupied) WHERE is_occupied = FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auctions_priority ON auctions (priority);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auctions_status ON auctions (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auctions_active ON auctions (start_time, end_time) WHERE ((status)::text = 'active'::text);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auction_emergencies_status ON auction_emergencies (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auction_participants_qualification_status ON auction_participants (auction_id, qualification_status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auction_winner_offers_status_expires ON auction_winner_offers (offer_status, expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_auction_auto_bids_auction_id_status ON auction_auto_bids (auction_id, status) WHERE status = 'active';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX "IX_bids_auction_id_amount" ON bids (auction_id, amount);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_sealed_bids_auction_status ON sealed_bids (auction_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_items_status ON items (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_items_status_submitted_at ON items (status, submitted_at) WHERE status IN ('submitted', 'under_review');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_admin_review_tasks_status_priority ON admin_review_tasks (status, priority);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX "IX_disputes_dispute_number" ON disputes (dispute_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX "IX_disputes_status" ON disputes (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_monitoring_alerts_status_severity ON monitoring_alerts (status, severity, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_reports_status_assigned ON reports (status, assigned_to);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_review_queue_status_priority ON review_queue (status, priority_score, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_notifications_priority_created ON notifications (priority, created_at) WHERE status = 'unread';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX "IX_orders_order_number" ON orders (order_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_orders_status ON orders (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX "IX_invoices_invoice_number" ON invoices (invoice_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_transactions_status ON transactions (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX "IX_transactions_transaction_number" ON transactions (transaction_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_seller_reviews_rating ON seller_reviews (overall_rating);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_seller_reviews_status ON seller_reviews (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_media_uploads_public_id ON media_uploads (public_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_user_identity_verifications_status ON user_identity_verifications (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE INDEX idx_seller_profiles_status ON seller_profiles (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX idx_unique_users_normalized_email_active ON users (normalized_email) WHERE (deleted_at IS NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    CREATE UNIQUE INDEX idx_unique_users_normalized_user_name_active ON users (normalized_user_name) WHERE (deleted_at IS NULL);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM __ef_migrations_history WHERE "migration_id" = '20260312042726_Initial') THEN
    INSERT INTO __ef_migrations_history (migration_id, product_version)
    VALUES ('20260312042726_Initial', '10.0.1');
    END IF;
END $EF$;
COMMIT;

