using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthNexus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PhaseEInitialPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "infrastructure");

            migrationBuilder.EnsureSchema(
                name: "applications");

            migrationBuilder.EnsureSchema(
                name: "authentication");

            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "sessions");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "application_profiles",
                schema: "applications",
                columns: table => new
                {
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    application_type = table.Column<short>(type: "smallint", nullable: false),
                    application_audience = table.Column<short>(type: "smallint", nullable: false),
                    application_mode = table.Column<short>(type: "smallint", nullable: false),
                    application_name = table.Column<string>(type: "text", nullable: false),
                    default_locale = table.Column<string>(type: "text", nullable: false),
                    authentication_policy_reference = table.Column<string>(type: "text", nullable: false),
                    registration_schema_reference = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_application_profiles", x => x.application_id);
                    table.CheckConstraint("ck_application_profiles_application_id_not_empty", "application_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_application_profiles_audience", "application_audience IN (1, 2, 3, 4, 5, 6)");
                    table.CheckConstraint("ck_application_profiles_locale_not_blank", "btrim(default_locale) <> ''");
                    table.CheckConstraint("ck_application_profiles_mode", "application_mode IN (1, 2, 3)");
                    table.CheckConstraint("ck_application_profiles_name_not_blank", "btrim(application_name) <> ''");
                    table.CheckConstraint("ck_application_profiles_policy_reference_not_blank", "btrim(authentication_policy_reference) <> ''");
                    table.CheckConstraint("ck_application_profiles_schema_reference_not_blank", "registration_schema_reference IS NULL OR btrim(registration_schema_reference) <> ''");
                    table.CheckConstraint("ck_application_profiles_tenant_id_not_empty", "tenant_id IS NULL OR tenant_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_application_profiles_type", "application_type IN (1, 2, 3, 4, 5, 6)");
                    table.CheckConstraint("ck_application_profiles_version_not_empty", "version <> '00000000-0000-0000-0000-000000000000'::uuid");
                });

            migrationBuilder.CreateTable(
                name: "authentication_transactions",
                schema: "authentication",
                columns: table => new
                {
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purpose = table.Column<short>(type: "smallint", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    state_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authentication_transactions", x => x.transaction_id);
                    table.CheckConstraint("ck_authentication_transactions_application_id", "application_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_authentication_transactions_correlation_id", "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_authentication_transactions_expiry_state", "(state = 7 AND state_changed_at >= expires_at) OR (state <> 7 AND state_changed_at < expires_at)");
                    table.CheckConstraint("ck_authentication_transactions_initial_state", "state <> 1 OR state_changed_at = created_at");
                    table.CheckConstraint("ck_authentication_transactions_lifetime", "expires_at > created_at AND state_changed_at >= created_at");
                    table.CheckConstraint("ck_authentication_transactions_purpose", "purpose BETWEEN 1 AND 14");
                    table.CheckConstraint("ck_authentication_transactions_state", "state BETWEEN 1 AND 8");
                    table.CheckConstraint("ck_authentication_transactions_tenant_id", "tenant_id IS NULL OR tenant_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_authentication_transactions_terminal_state", "(state = 5 AND completed_at IS NOT NULL AND completed_at = state_changed_at AND failed_at IS NULL) OR (state = 6 AND failed_at IS NOT NULL AND failed_at = state_changed_at AND completed_at IS NULL) OR (state NOT IN (5, 6) AND completed_at IS NULL AND failed_at IS NULL)");
                    table.CheckConstraint("ck_authentication_transactions_transaction_id", "transaction_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_authentication_transactions_user_id", "user_id IS NULL OR user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_authentication_transactions_version", "version <> '00000000-0000-0000-0000-000000000000'::uuid");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "notifications",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    application_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notification_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    destination_ciphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                    destination_protection_key_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    destination_format_version = table.Column<int>(type: "integer", nullable: false),
                    payload_ciphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                    payload_protection_key_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_format_version = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    available_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    state_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_attempted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    permanently_failed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_failure_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.message_id);
                    table.CheckConstraint("ck_outbox_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_outbox_channel", "channel BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_outbox_correlation_id", "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_outbox_delivery_shape", "(state = 1 AND state_changed_at = created_at AND attempt_count = 0 AND last_attempted_at IS NULL AND next_attempt_at IS NOT NULL AND next_attempt_at = available_at AND delivered_at IS NULL AND permanently_failed_at IS NULL AND last_failure_code IS NULL) OR (state = 2 AND state_changed_at >= available_at AND attempt_count > 0 AND last_attempted_at IS NOT NULL AND last_attempted_at = state_changed_at AND next_attempt_at IS NOT NULL AND next_attempt_at > last_attempted_at AND delivered_at IS NULL AND permanently_failed_at IS NULL AND last_failure_code IS NOT NULL) OR (state = 3 AND state_changed_at >= available_at AND attempt_count > 0 AND last_attempted_at IS NOT NULL AND last_attempted_at = state_changed_at AND delivered_at IS NOT NULL AND delivered_at = state_changed_at AND next_attempt_at IS NULL AND permanently_failed_at IS NULL AND last_failure_code IS NULL) OR (state = 4 AND state_changed_at >= available_at AND attempt_count > 0 AND last_attempted_at IS NOT NULL AND last_attempted_at = state_changed_at AND permanently_failed_at IS NOT NULL AND permanently_failed_at = state_changed_at AND next_attempt_at IS NULL AND delivered_at IS NULL AND last_failure_code IS NOT NULL)");
                    table.CheckConstraint("ck_outbox_destination_ciphertext", "octet_length(destination_ciphertext) BETWEEN 29 AND 1564");
                    table.CheckConstraint("ck_outbox_destination_format", "destination_format_version = 1");
                    table.CheckConstraint("ck_outbox_destination_key_id", "destination_protection_key_id ~ '^[A-Za-z0-9_.:-]{1,128}$'");
                    table.CheckConstraint("ck_outbox_failure_code", "last_failure_code IS NULL OR last_failure_code ~ '^[a-z][a-z0-9._-]{0,63}$'");
                    table.CheckConstraint("ck_outbox_message_id", "message_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_outbox_notification_type", "notification_type ~ '^[a-z][a-z0-9._-]{0,99}$'");
                    table.CheckConstraint("ck_outbox_optional_ids", "(target_user_id IS NULL OR target_user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (application_id IS NULL OR application_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (tenant_id IS NULL OR tenant_id <> '00000000-0000-0000-0000-000000000000'::uuid)");
                    table.CheckConstraint("ck_outbox_payload_ciphertext", "octet_length(payload_ciphertext) BETWEEN 1 AND 65536");
                    table.CheckConstraint("ck_outbox_payload_format", "payload_format_version > 0");
                    table.CheckConstraint("ck_outbox_payload_key_id", "payload_protection_key_id ~ '^[A-Za-z0-9_.:-]{1,128}$'");
                    table.CheckConstraint("ck_outbox_state", "state BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_outbox_times", "available_at >= created_at AND state_changed_at >= created_at");
                    table.CheckConstraint("ck_outbox_version", "version <> '00000000-0000-0000-0000-000000000000'::uuid");
                });

            migrationBuilder.CreateTable(
                name: "security_events",
                schema: "audit",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    result = table.Column<int>(type: "integer", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    application_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    network_summary = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    user_agent_summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_events", x => x.event_id);
                    table.CheckConstraint("ck_security_events_correlation_id", "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_security_events_event_id", "event_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_security_events_metadata_object", "jsonb_typeof(metadata) = 'object'");
                    table.CheckConstraint("ck_security_events_optional_ids", "(actor_user_id IS NULL OR actor_user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (target_user_id IS NULL OR target_user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (application_id IS NULL OR application_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (tenant_id IS NULL OR tenant_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id IS NULL OR session_id <> '00000000-0000-0000-0000-000000000000'::uuid)");
                    table.CheckConstraint("ck_security_events_result", "result BETWEEN 1 AND 6");
                    table.CheckConstraint("ck_security_events_type", "event_type IN ('registration_requested', 'registration_completed', 'email_verification_sent', 'email_verified', 'phone_verification_sent', 'phone_verified', 'login_succeeded', 'login_failed', 'login_throttled', 'mfa_required', 'mfa_succeeded', 'mfa_failed', 'password_reset_requested', 'password_reset_completed', 'password_changed', 'passkey_added', 'passkey_used', 'passkey_removed', 'totp_added', 'totp_removed', 'recovery_code_used', 'external_identity_linked', 'external_identity_unlinked', 'session_created', 'session_revoked', 'logout_all', 'security_step_up_required', 'security_step_up_completed', 'account_temporarily_protected', 'account_suspended', 'account_deletion_requested', 'account_deleted', 'authorization_denied', 'policy_draft_created', 'policy_changed', 'policy_approved', 'provider_unavailable')");
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "sessions",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_secret_hash = table.Column<string>(type: "character varying(43)", maxLength: 43, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<short>(type: "smallint", nullable: false),
                    authenticated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    idle_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    absolute_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    state_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    secret_rotated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    rotation_count = table.Column<int>(type: "integer", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<short>(type: "smallint", nullable: true),
                    expired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sessions", x => x.session_id);
                    table.CheckConstraint("ck_sessions_application_id", "application_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_sessions_lifetime", "authenticated_at <= created_at AND created_at < idle_expires_at AND idle_expires_at <= absolute_expires_at");
                    table.CheckConstraint("ck_sessions_operation_timestamps", "last_seen_at >= created_at AND last_seen_at <= updated_at AND last_seen_at < idle_expires_at AND updated_at >= created_at AND state_changed_at >= created_at AND state_changed_at <= updated_at AND secret_rotated_at >= created_at AND secret_rotated_at <= updated_at AND secret_rotated_at < idle_expires_at");
                    table.CheckConstraint("ck_sessions_revocation_reason", "revocation_reason IS NULL OR revocation_reason BETWEEN 1 AND 10");
                    table.CheckConstraint("ck_sessions_rotation_count", "rotation_count >= 0");
                    table.CheckConstraint("ck_sessions_secret_hash", "char_length(session_secret_hash) = 43 AND session_secret_hash ~ '^[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]$'");
                    table.CheckConstraint("ck_sessions_session_id", "session_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_sessions_state", "state BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_sessions_tenant_id", "tenant_id IS NULL OR tenant_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_sessions_terminal_state", "(state = 1 AND state_changed_at = created_at AND revoked_at IS NULL AND revocation_reason IS NULL AND expired_at IS NULL) OR (state = 2 AND revoked_at IS NOT NULL AND revoked_at = state_changed_at AND state_changed_at = updated_at AND revocation_reason IS NOT NULL AND expired_at IS NULL) OR (state = 3 AND expired_at IS NOT NULL AND expired_at = state_changed_at AND state_changed_at = updated_at AND expired_at >= idle_expires_at AND revoked_at IS NULL AND revocation_reason IS NULL)");
                    table.CheckConstraint("ck_sessions_user_id", "user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_sessions_version", "version <> '00000000-0000-0000-0000-000000000000'::uuid");
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    state_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_accounts", x => x.user_id);
                    table.CheckConstraint("ck_user_accounts_state", "state IN (1, 2, 3, 4, 5, 6)");
                    table.CheckConstraint("ck_user_accounts_state_changed_at", "(state = 1 AND state_changed_at = created_at) OR (state <> 1 AND state_changed_at >= created_at)");
                    table.CheckConstraint("ck_user_accounts_user_id_not_empty", "user_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_user_accounts_version_not_empty", "version <> '00000000-0000-0000-0000-000000000000'::uuid");
                });

            migrationBuilder.CreateTable(
                name: "application_redirect_uris",
                schema: "applications",
                columns: table => new
                {
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    redirect_uri = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_application_redirect_uris", x => new { x.application_id, x.redirect_uri });
                    table.CheckConstraint("ck_application_redirect_uris_application_id_not_empty", "application_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_application_redirect_uris_sort_order", "sort_order >= 0");
                    table.CheckConstraint("ck_application_redirect_uris_uri_not_blank", "btrim(redirect_uri) <> ''");
                    table.ForeignKey(
                        name: "fk_application_redirect_uris_application_profiles",
                        column: x => x.application_id,
                        principalSchema: "applications",
                        principalTable: "application_profiles",
                        principalColumn: "application_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                ALTER TABLE applications.application_redirect_uris
                    ALTER CONSTRAINT fk_application_redirect_uris_application_profiles
                    DEFERRABLE INITIALLY DEFERRED;
                """);

            migrationBuilder.CreateIndex(
                name: "ux_application_redirect_uris_application_sort_order",
                schema: "applications",
                table: "application_redirect_uris",
                columns: new[] { "application_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_authentication_transactions_application_id",
                schema: "authentication",
                table: "authentication_transactions",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_transactions_correlation_id",
                schema: "authentication",
                table: "authentication_transactions",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_transactions_expires_at",
                schema: "authentication",
                table: "authentication_transactions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_authentication_transactions_state_expires_at",
                schema: "authentication",
                table: "authentication_transactions",
                columns: new[] { "state", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_authentication_transactions_user_id",
                schema: "authentication",
                table: "authentication_transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_correlation_id",
                schema: "notifications",
                table: "outbox_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_due",
                schema: "notifications",
                table: "outbox_messages",
                columns: new[] { "state", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_target_user_created_at",
                schema: "notifications",
                table: "outbox_messages",
                columns: new[] { "target_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_security_events_application_occurred_at",
                schema: "audit",
                table: "security_events",
                columns: new[] { "application_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_security_events_correlation_id",
                schema: "audit",
                table: "security_events",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_events_occurred_at",
                schema: "audit",
                table: "security_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_security_events_target_user_occurred_at",
                schema: "audit",
                table: "security_events",
                columns: new[] { "target_user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_application_id_state",
                schema: "sessions",
                table: "sessions",
                columns: new[] { "application_id", "state" });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_state_absolute_expires_at",
                schema: "sessions",
                table: "sessions",
                columns: new[] { "state", "absolute_expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_state_idle_expires_at",
                schema: "sessions",
                table: "sessions",
                columns: new[] { "state", "idle_expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_user_id_state",
                schema: "sessions",
                table: "sessions",
                columns: new[] { "user_id", "state" });

            migrationBuilder.CreateIndex(
                name: "ux_sessions_session_secret_hash",
                schema: "sessions",
                table: "sessions",
                column: "session_secret_hash",
                unique: true);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION applications.enforce_application_profile_redirect_uri()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    profile_id uuid;
                BEGIN
                    IF TG_OP = 'TRUNCATE' THEN
                        RAISE EXCEPTION USING
                            ERRCODE = '23514',
                            MESSAGE = 'Application redirect URIs cannot be truncated while profile redirect invariants are active.',
                            SCHEMA = 'applications',
                            TABLE = 'application_redirect_uris',
                            CONSTRAINT = 'ck_application_profiles_has_redirect_uri';
                    END IF;

                    IF TG_TABLE_NAME = 'application_profiles' THEN
                        profile_id := NEW.application_id;
                    ELSIF TG_OP = 'DELETE' THEN
                        profile_id := OLD.application_id;
                    ELSE
                        profile_id := NEW.application_id;

                        IF TG_OP = 'UPDATE'
                           AND OLD.application_id IS DISTINCT FROM NEW.application_id
                           AND EXISTS (
                               SELECT 1
                               FROM applications.application_profiles AS profile
                               WHERE profile.application_id = OLD.application_id)
                           AND NOT EXISTS (
                               SELECT 1
                               FROM applications.application_redirect_uris AS redirect
                               WHERE redirect.application_id = OLD.application_id) THEN
                            RAISE EXCEPTION USING
                                ERRCODE = '23514',
                                MESSAGE = 'An application profile must retain at least one redirect URI.',
                                SCHEMA = 'applications',
                                TABLE = 'application_profiles',
                                CONSTRAINT = 'ck_application_profiles_has_redirect_uri';
                        END IF;
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM applications.application_profiles AS profile
                        WHERE profile.application_id = profile_id)
                       AND NOT EXISTS (
                           SELECT 1
                           FROM applications.application_redirect_uris AS redirect
                           WHERE redirect.application_id = profile_id) THEN
                        RAISE EXCEPTION USING
                            ERRCODE = '23514',
                            MESSAGE = 'An application profile must have at least one redirect URI.',
                            SCHEMA = 'applications',
                            TABLE = 'application_profiles',
                            CONSTRAINT = 'ck_application_profiles_has_redirect_uri';
                    END IF;

                    RETURN NULL;
                END;
                $function$;

                CREATE CONSTRAINT TRIGGER application_profiles_require_redirect_uri
                AFTER INSERT OR UPDATE ON applications.application_profiles
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW
                EXECUTE FUNCTION applications.enforce_application_profile_redirect_uri();

                CREATE CONSTRAINT TRIGGER application_redirect_uris_preserve_profile_redirect
                AFTER INSERT OR UPDATE OR DELETE ON applications.application_redirect_uris
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW
                EXECUTE FUNCTION applications.enforce_application_profile_redirect_uri();

                CREATE TRIGGER application_redirect_uris_reject_truncate
                BEFORE TRUNCATE ON applications.application_redirect_uris
                FOR EACH STATEMENT
                EXECUTE FUNCTION applications.enforce_application_profile_redirect_uri();
                """);

            migrationBuilder.Sql(
                """
                CREATE FUNCTION audit.reject_security_event_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $function$
                BEGIN
                    RAISE EXCEPTION USING
                        ERRCODE = '55000',
                        MESSAGE = 'Security events are append-only.',
                        SCHEMA = 'audit',
                        TABLE = 'security_events';
                END;
                $function$;

                CREATE TRIGGER security_events_reject_mutation
                BEFORE UPDATE OR DELETE ON audit.security_events
                FOR EACH ROW
                EXECUTE FUNCTION audit.reject_security_event_mutation();

                CREATE TRIGGER security_events_reject_truncate
                BEFORE TRUNCATE ON audit.security_events
                FOR EACH STATEMENT
                EXECUTE FUNCTION audit.reject_security_event_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS application_redirect_uris_reject_truncate
                    ON applications.application_redirect_uris;
                DROP TRIGGER IF EXISTS application_redirect_uris_preserve_profile_redirect
                    ON applications.application_redirect_uris;
                DROP TRIGGER IF EXISTS application_profiles_require_redirect_uri
                    ON applications.application_profiles;
                DROP FUNCTION IF EXISTS applications.enforce_application_profile_redirect_uri();
                """);

            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS security_events_reject_truncate
                    ON audit.security_events;
                DROP TRIGGER IF EXISTS security_events_reject_mutation
                    ON audit.security_events;
                DROP FUNCTION IF EXISTS audit.reject_security_event_mutation();
                """);

            migrationBuilder.DropTable(
                name: "application_redirect_uris",
                schema: "applications");

            migrationBuilder.DropTable(
                name: "authentication_transactions",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "security_events",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "sessions");

            migrationBuilder.DropTable(
                name: "user_accounts",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "application_profiles",
                schema: "applications");
        }
    }
}
