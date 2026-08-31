using AuthNexus.Persistence.Tests.Support;
using Npgsql;

namespace AuthNexus.Persistence.Security.Tests;

public sealed class DatabaseCheckConstraintFixture : PostgreSqlTestFixture;

public sealed class DatabaseCheckConstraintTests :
    IClassFixture<DatabaseCheckConstraintFixture>
{
    private const string EmptyUuid = "00000000-0000-0000-0000-000000000000";
    private const string ApplicationId = "10000000-0000-0000-0000-000000000001";
    private const string AuthenticationTransactionId = "20000000-0000-0000-0000-000000000001";
    private const string UserId = "30000000-0000-0000-0000-000000000001";
    private const string OutboxMessageId = "40000000-0000-0000-0000-000000000001";
    private const string SecurityEventId = "50000000-0000-0000-0000-000000000001";
    private const string SessionId = "60000000-0000-0000-0000-000000000001";

    private static readonly ConstraintDefinition[] Definitions =
    [
        Profile("ck_application_profiles_application_id_not_empty", $"UPDATE applications.application_profiles SET application_id = '{EmptyUuid}' WHERE application_id = '{ApplicationId}';"),
        Profile("ck_application_profiles_audience", $"UPDATE applications.application_profiles SET application_audience = 0 WHERE application_id = '{ApplicationId}';"),
        Profile("ck_application_profiles_locale_not_blank", $"UPDATE applications.application_profiles SET default_locale = ' ' WHERE application_id = '{ApplicationId}';"),
        Profile("ck_application_profiles_mode", $"UPDATE applications.application_profiles SET application_mode = 0 WHERE application_id = '{ApplicationId}';"),
        Profile("ck_application_profiles_name_not_blank", $"UPDATE applications.application_profiles SET application_name = ' ' WHERE application_id = '{ApplicationId}';"),
        Profile("ck_application_profiles_policy_reference_not_blank", $"UPDATE applications.application_profiles SET authentication_policy_reference = ' ' WHERE application_id = '{ApplicationId}';"),
        Profile("ck_application_profiles_schema_reference_not_blank", $"UPDATE applications.application_profiles SET registration_schema_reference = ' ' WHERE application_id = '{ApplicationId}';"),
        Profile("ck_application_profiles_tenant_id_not_empty", $"UPDATE applications.application_profiles SET tenant_id = '{EmptyUuid}' WHERE application_id = '{ApplicationId}';"),
        Profile("ck_application_profiles_type", $"UPDATE applications.application_profiles SET application_type = 0 WHERE application_id = '{ApplicationId}';"),
        Profile("ck_application_profiles_version_not_empty", $"UPDATE applications.application_profiles SET version = '{EmptyUuid}' WHERE application_id = '{ApplicationId}';"),

        Redirect("ck_application_redirect_uris_application_id_not_empty", $"UPDATE applications.application_redirect_uris SET application_id = '{EmptyUuid}' WHERE application_id = '{ApplicationId}';"),
        Redirect("ck_application_redirect_uris_sort_order", $"UPDATE applications.application_redirect_uris SET sort_order = -1 WHERE application_id = '{ApplicationId}';"),
        Redirect("ck_application_redirect_uris_uri_not_blank", $"UPDATE applications.application_redirect_uris SET redirect_uri = ' ' WHERE application_id = '{ApplicationId}';"),

        Audit("ck_security_events_correlation_id", SecurityEventInsert(correlationId: EmptyUuid)),
        Audit("ck_security_events_event_id", SecurityEventInsert(eventId: EmptyUuid)),
        Audit("ck_security_events_metadata_object", SecurityEventInsert(metadataSql: "'[]'::jsonb")),
        Audit("ck_security_events_optional_ids", SecurityEventInsert(actorUserIdSql: $"'{EmptyUuid}'")),
        Audit("ck_security_events_result", SecurityEventInsert(result: 0)),
        Audit("ck_security_events_type", SecurityEventInsert(eventType: "not_a_security_event")),

        Authentication("ck_authentication_transactions_application_id", $"UPDATE authentication.authentication_transactions SET application_id = '{EmptyUuid}' WHERE transaction_id = '{AuthenticationTransactionId}';"),
        Authentication("ck_authentication_transactions_correlation_id", $"UPDATE authentication.authentication_transactions SET correlation_id = '{EmptyUuid}' WHERE transaction_id = '{AuthenticationTransactionId}';"),
        Authentication("ck_authentication_transactions_expiry_state", $"UPDATE authentication.authentication_transactions SET state = 7, state_changed_at = created_at WHERE transaction_id = '{AuthenticationTransactionId}';"),
        Authentication("ck_authentication_transactions_initial_state", $"UPDATE authentication.authentication_transactions SET state_changed_at = created_at + interval '1 minute' WHERE transaction_id = '{AuthenticationTransactionId}';"),
        Authentication("ck_authentication_transactions_lifetime", $"UPDATE authentication.authentication_transactions SET state = 2, created_at = state_changed_at + interval '1 minute' WHERE transaction_id = '{AuthenticationTransactionId}';"),
        Authentication("ck_authentication_transactions_purpose", $"UPDATE authentication.authentication_transactions SET purpose = 0 WHERE transaction_id = '{AuthenticationTransactionId}';"),
        Authentication("ck_authentication_transactions_state", $"UPDATE authentication.authentication_transactions SET state = 0 WHERE transaction_id = '{AuthenticationTransactionId}';"),
        Authentication("ck_authentication_transactions_tenant_id", $"UPDATE authentication.authentication_transactions SET tenant_id = '{EmptyUuid}' WHERE transaction_id = '{AuthenticationTransactionId}';"),
        Authentication("ck_authentication_transactions_terminal_state", $"UPDATE authentication.authentication_transactions SET state = 5, state_changed_at = created_at + interval '1 minute' WHERE transaction_id = '{AuthenticationTransactionId}';"),
        Authentication("ck_authentication_transactions_transaction_id", $"UPDATE authentication.authentication_transactions SET transaction_id = '{EmptyUuid}' WHERE transaction_id = '{AuthenticationTransactionId}';"),
        Authentication("ck_authentication_transactions_user_id", $"UPDATE authentication.authentication_transactions SET user_id = '{EmptyUuid}' WHERE transaction_id = '{AuthenticationTransactionId}';"),
        Authentication("ck_authentication_transactions_version", $"UPDATE authentication.authentication_transactions SET version = '{EmptyUuid}' WHERE transaction_id = '{AuthenticationTransactionId}';"),

        Identity("ck_user_accounts_state", $"UPDATE identity.user_accounts SET state = 0 WHERE user_id = '{UserId}';"),
        Identity("ck_user_accounts_state_changed_at", $"UPDATE identity.user_accounts SET state_changed_at = created_at + interval '1 minute' WHERE user_id = '{UserId}';"),
        Identity("ck_user_accounts_user_id_not_empty", $"UPDATE identity.user_accounts SET user_id = '{EmptyUuid}' WHERE user_id = '{UserId}';"),
        Identity("ck_user_accounts_version_not_empty", $"UPDATE identity.user_accounts SET version = '{EmptyUuid}' WHERE user_id = '{UserId}';"),

        Outbox("ck_outbox_attempt_count", $"UPDATE notifications.outbox_messages SET attempt_count = -1 WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_channel", $"UPDATE notifications.outbox_messages SET channel = 0 WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_correlation_id", $"UPDATE notifications.outbox_messages SET correlation_id = '{EmptyUuid}' WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_delivery_shape", $"UPDATE notifications.outbox_messages SET next_attempt_at = NULL WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_destination_ciphertext", $"UPDATE notifications.outbox_messages SET destination_ciphertext = decode('01', 'hex') WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_destination_format", $"UPDATE notifications.outbox_messages SET destination_format_version = 2 WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_destination_key_id", $"UPDATE notifications.outbox_messages SET destination_protection_key_id = 'bad key' WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_failure_code", $"UPDATE notifications.outbox_messages SET last_failure_code = 'bad code' WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_message_id", $"UPDATE notifications.outbox_messages SET message_id = '{EmptyUuid}' WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_notification_type", $"UPDATE notifications.outbox_messages SET notification_type = 'Bad Type' WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_optional_ids", $"UPDATE notifications.outbox_messages SET target_user_id = '{EmptyUuid}' WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_payload_ciphertext", $"UPDATE notifications.outbox_messages SET payload_ciphertext = ''::bytea WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_payload_format", $"UPDATE notifications.outbox_messages SET payload_format_version = 0 WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_payload_key_id", $"UPDATE notifications.outbox_messages SET payload_protection_key_id = 'bad key' WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_state", $"UPDATE notifications.outbox_messages SET state = 0 WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_times", $"UPDATE notifications.outbox_messages SET available_at = created_at - interval '1 minute' WHERE message_id = '{OutboxMessageId}';"),
        Outbox("ck_outbox_version", $"UPDATE notifications.outbox_messages SET version = '{EmptyUuid}' WHERE message_id = '{OutboxMessageId}';"),

        Session("ck_sessions_application_id", $"UPDATE sessions.sessions SET application_id = '{EmptyUuid}' WHERE session_id = '{SessionId}';"),
        Session("ck_sessions_lifetime", $"UPDATE sessions.sessions SET authenticated_at = created_at + interval '1 minute' WHERE session_id = '{SessionId}';"),
        Session("ck_sessions_operation_timestamps", $"UPDATE sessions.sessions SET last_seen_at = created_at - interval '1 minute' WHERE session_id = '{SessionId}';"),
        Session("ck_sessions_revocation_reason", $"UPDATE sessions.sessions SET state = 2, updated_at = created_at + interval '1 minute', state_changed_at = created_at + interval '1 minute', revoked_at = created_at + interval '1 minute', revocation_reason = 11 WHERE session_id = '{SessionId}';"),
        Session("ck_sessions_rotation_count", $"UPDATE sessions.sessions SET rotation_count = -1 WHERE session_id = '{SessionId}';"),
        Session("ck_sessions_secret_hash", $"UPDATE sessions.sessions SET session_secret_hash = 'not-a-valid-session-secret-hash' WHERE session_id = '{SessionId}';"),
        Session("ck_sessions_session_id", $"UPDATE sessions.sessions SET session_id = '{EmptyUuid}' WHERE session_id = '{SessionId}';"),
        Session("ck_sessions_state", $"UPDATE sessions.sessions SET state = 0 WHERE session_id = '{SessionId}';"),
        Session("ck_sessions_tenant_id", $"UPDATE sessions.sessions SET tenant_id = '{EmptyUuid}' WHERE session_id = '{SessionId}';"),
        Session("ck_sessions_terminal_state", $"UPDATE sessions.sessions SET state = 2 WHERE session_id = '{SessionId}';"),
        Session("ck_sessions_user_id", $"UPDATE sessions.sessions SET user_id = '{EmptyUuid}' WHERE session_id = '{SessionId}';"),
        Session("ck_sessions_version", $"UPDATE sessions.sessions SET version = '{EmptyUuid}' WHERE session_id = '{SessionId}';"),
    ];

    private readonly DatabaseCheckConstraintFixture _fixture;

    public DatabaseCheckConstraintTests(DatabaseCheckConstraintFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<string, string, string, string> ConstraintViolations
    {
        get
        {
            var cases = new TheoryData<string, string, string, string>();

            foreach (var definition in Definitions)
            {
                cases.Add(
                    definition.Schema,
                    definition.Table,
                    definition.Constraint,
                    definition.ViolationSql);
            }

            return cases;
        }
    }

    public static TheoryData<string, string, string, string> NullableLifecycleRegressions =>
        new()
        {
            {
                "authentication",
                "authentication_transactions",
                "ck_authentication_transactions_terminal_state",
                $"UPDATE authentication.authentication_transactions SET state = 6, state_changed_at = created_at + interval '1 minute', failed_at = NULL WHERE transaction_id = '{AuthenticationTransactionId}';"
            },
            {
                "sessions",
                "sessions",
                "ck_sessions_terminal_state",
                $"UPDATE sessions.sessions SET state = 3, updated_at = idle_expires_at, state_changed_at = idle_expires_at, expired_at = NULL WHERE session_id = '{SessionId}';"
            },
            {
                "notifications",
                "outbox_messages",
                "ck_outbox_delivery_shape",
                $"UPDATE notifications.outbox_messages SET state = 2, state_changed_at = available_at, attempt_count = 1, last_attempted_at = NULL, next_attempt_at = available_at + interval '5 minutes', last_failure_code = 'provider.temporary' WHERE message_id = '{OutboxMessageId}';"
            },
            {
                "notifications",
                "outbox_messages",
                "ck_outbox_delivery_shape",
                $"UPDATE notifications.outbox_messages SET state = 2, state_changed_at = available_at, attempt_count = 1, last_attempted_at = available_at, next_attempt_at = NULL, last_failure_code = 'provider.temporary' WHERE message_id = '{OutboxMessageId}';"
            },
            {
                "notifications",
                "outbox_messages",
                "ck_outbox_delivery_shape",
                $"UPDATE notifications.outbox_messages SET state = 3, state_changed_at = available_at, attempt_count = 1, last_attempted_at = NULL, next_attempt_at = NULL, delivered_at = available_at WHERE message_id = '{OutboxMessageId}';"
            },
            {
                "notifications",
                "outbox_messages",
                "ck_outbox_delivery_shape",
                $"UPDATE notifications.outbox_messages SET state = 3, state_changed_at = available_at, attempt_count = 1, last_attempted_at = available_at, next_attempt_at = NULL, delivered_at = NULL WHERE message_id = '{OutboxMessageId}';"
            },
            {
                "notifications",
                "outbox_messages",
                "ck_outbox_delivery_shape",
                $"UPDATE notifications.outbox_messages SET state = 4, state_changed_at = available_at, attempt_count = 1, last_attempted_at = NULL, next_attempt_at = NULL, permanently_failed_at = available_at, last_failure_code = 'destination.rejected' WHERE message_id = '{OutboxMessageId}';"
            },
            {
                "notifications",
                "outbox_messages",
                "ck_outbox_delivery_shape",
                $"UPDATE notifications.outbox_messages SET state = 4, state_changed_at = available_at, attempt_count = 1, last_attempted_at = available_at, next_attempt_at = NULL, permanently_failed_at = NULL, last_failure_code = 'destination.rejected' WHERE message_id = '{OutboxMessageId}';"
            },
        };

    [Fact]
    public async Task Migration_installs_the_exact_validated_check_constraint_catalog()
    {
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT namespace.nspname, relation.relname, constraint_record.conname,
                   constraint_record.convalidated
            FROM pg_constraint AS constraint_record
            INNER JOIN pg_class AS relation
                ON relation.oid = constraint_record.conrelid
            INNER JOIN pg_namespace AS namespace
                ON namespace.oid = relation.relnamespace
            WHERE constraint_record.contype = 'c'
              AND namespace.nspname = ANY (
                  ARRAY['applications', 'audit', 'authentication', 'identity',
                        'notifications', 'sessions'])
            ORDER BY namespace.nspname, relation.relname, constraint_record.conname;
            """;

        var actual = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            Assert.True(reader.GetBoolean(3));
            actual.Add($"{reader.GetString(0)}.{reader.GetString(1)}.{reader.GetString(2)}");
        }

        var expected = Definitions
            .Select(definition =>
                $"{definition.Schema}.{definition.Table}.{definition.Constraint}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(64, expected.Length);
        Assert.Equal(64, expected.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(ConstraintViolations))]
    public async Task Each_declared_check_constraint_rejects_its_own_violation(
        string schema,
        string table,
        string constraint,
        string violationSql)
    {
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await ExecuteAsync(connection, transaction, GetValidSeedSql(schema, table));

        if (schema == "applications")
        {
            await ExecuteAsync(connection, transaction, "SET CONSTRAINTS ALL IMMEDIATE;");
        }

        await DropSiblingChecksAsync(connection, transaction, schema, table, constraint);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => ExecuteAsync(connection, transaction, violationSql));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal(constraint, exception.ConstraintName);
    }

    [Theory]
    [MemberData(nameof(NullableLifecycleRegressions))]
    public async Task Nullable_lifecycle_fields_cannot_bypass_terminal_shape_checks(
        string schema,
        string table,
        string constraint,
        string violationSql)
    {
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await ExecuteAsync(connection, transaction, GetValidSeedSql(schema, table));
        await DropSiblingChecksAsync(connection, transaction, schema, table, constraint);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => ExecuteAsync(connection, transaction, violationSql));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal(constraint, exception.ConstraintName);
    }

    private static async Task DropSiblingChecksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string schema,
        string table,
        string retainedConstraint)
    {
        var siblingNames = new List<string>();
        await using (var query = connection.CreateCommand())
        {
            query.Transaction = transaction;
            query.CommandText =
                """
                SELECT constraint_record.conname
                FROM pg_constraint AS constraint_record
                INNER JOIN pg_class AS relation
                    ON relation.oid = constraint_record.conrelid
                INNER JOIN pg_namespace AS namespace
                    ON namespace.oid = relation.relnamespace
                WHERE constraint_record.contype = 'c'
                  AND namespace.nspname = @schema
                  AND relation.relname = @table
                  AND constraint_record.conname <> @retained_constraint;
                """;
            query.Parameters.AddWithValue("schema", schema);
            query.Parameters.AddWithValue("table", table);
            query.Parameters.AddWithValue("retained_constraint", retainedConstraint);

            await using var reader = await query.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                siblingNames.Add(reader.GetString(0));
            }
        }

        foreach (var siblingName in siblingNames)
        {
            await ExecuteAsync(
                connection,
                transaction,
                $"ALTER TABLE {QuoteIdentifier(schema)}.{QuoteIdentifier(table)} " +
                $"DROP CONSTRAINT {QuoteIdentifier(siblingName)};");
        }
    }

    private static async Task<int> ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return await command.ExecuteNonQueryAsync();
    }

    private static string GetValidSeedSql(string schema, string table) =>
        (schema, table) switch
        {
            ("applications", "application_profiles") or
            ("applications", "application_redirect_uris") =>
                $"""
                INSERT INTO applications.application_profiles (
                    application_id, tenant_id, application_type, application_audience,
                    application_mode, application_name, default_locale,
                    authentication_policy_reference, registration_schema_reference, version)
                VALUES (
                    '{ApplicationId}', NULL, 1, 1, 1, 'Constraint Test', 'en-US',
                    'policy:test', NULL, '90000000-0000-0000-0000-000000000001');
                INSERT INTO applications.application_redirect_uris (
                    application_id, redirect_uri, sort_order)
                VALUES ('{ApplicationId}', 'https://constraint.example.test/callback', 0);
                """,
            ("audit", "security_events") =>
                $"""
                INSERT INTO audit.security_events (
                    event_id, occurred_at, event_type, result, actor_user_id,
                    target_user_id, application_id, tenant_id, session_id,
                    correlation_id, network_summary, user_agent_summary, metadata)
                VALUES (
                    '{SecurityEventId}', '2026-08-30T10:00:00Z', 'login_succeeded', 1,
                    NULL, NULL, NULL, NULL, NULL,
                    '51000000-0000-0000-0000-000000000001', NULL, NULL,
                    jsonb_build_object());
                """,
            ("authentication", "authentication_transactions") =>
                $"""
                INSERT INTO authentication.authentication_transactions (
                    transaction_id, application_id, tenant_id, user_id, purpose,
                    correlation_id, state, created_at, expires_at, state_changed_at,
                    completed_at, failed_at, version)
                VALUES (
                    '{AuthenticationTransactionId}',
                    '21000000-0000-0000-0000-000000000001', NULL, NULL, 1,
                    '22000000-0000-0000-0000-000000000001', 1,
                    '2026-08-30T10:00:00Z', '2026-08-30T11:00:00Z',
                    '2026-08-30T10:00:00Z', NULL, NULL,
                    '29000000-0000-0000-0000-000000000001');
                """,
            ("identity", "user_accounts") =>
                $"""
                INSERT INTO identity.user_accounts (
                    user_id, state, created_at, state_changed_at, version)
                VALUES (
                    '{UserId}', 1, '2026-08-30T10:00:00Z',
                    '2026-08-30T10:00:00Z',
                    '39000000-0000-0000-0000-000000000001');
                """,
            ("notifications", "outbox_messages") =>
                $"""
                INSERT INTO notifications.outbox_messages (
                    message_id, correlation_id, target_user_id, application_id,
                    tenant_id, notification_type, channel, destination_ciphertext,
                    destination_protection_key_id, destination_format_version,
                    payload_ciphertext, payload_protection_key_id,
                    payload_format_version, state, created_at, available_at,
                    state_changed_at, attempt_count, last_attempted_at,
                    next_attempt_at, delivered_at, permanently_failed_at,
                    last_failure_code, version)
                VALUES (
                    '{OutboxMessageId}',
                    '41000000-0000-0000-0000-000000000001', NULL, NULL, NULL,
                    'security.constraint_test', 1, decode(repeat('01', 29), 'hex'),
                    'test-destination:v2', 1, decode('01', 'hex'), 'payload:test', 1,
                    1, '2026-08-30T10:00:00Z', '2026-08-30T10:05:00Z',
                    '2026-08-30T10:00:00Z', 0, NULL,
                    '2026-08-30T10:05:00Z', NULL, NULL, NULL,
                    '49000000-0000-0000-0000-000000000001');
                """,
            ("sessions", "sessions") =>
                $"""
                INSERT INTO sessions.sessions (
                    session_id, session_secret_hash, user_id, application_id,
                    tenant_id, state, authenticated_at, created_at, last_seen_at,
                    idle_expires_at, absolute_expires_at, updated_at,
                    state_changed_at, secret_rotated_at, rotation_count,
                    revoked_at, revocation_reason, expired_at, version)
                VALUES (
                    '{SessionId}', repeat('A', 43),
                    '61000000-0000-0000-0000-000000000001',
                    '62000000-0000-0000-0000-000000000001', NULL, 1,
                    '2026-08-30T09:59:00Z', '2026-08-30T10:00:00Z',
                    '2026-08-30T10:00:00Z', '2026-08-30T11:00:00Z',
                    '2026-08-30T12:00:00Z', '2026-08-30T10:00:00Z',
                    '2026-08-30T10:00:00Z', '2026-08-30T10:00:00Z', 0,
                    NULL, NULL, NULL,
                    '69000000-0000-0000-0000-000000000001');
                """,
            _ => throw new InvalidOperationException(
                $"No valid seed is defined for {schema}.{table}."),
        };

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private static string SecurityEventInsert(
        string eventId = "50000000-0000-0000-0000-000000000002",
        string eventType = "login_succeeded",
        int result = 1,
        string actorUserIdSql = "NULL",
        string correlationId = "51000000-0000-0000-0000-000000000002",
        string metadataSql = "jsonb_build_object()") =>
        $"""
        INSERT INTO audit.security_events (
            event_id, occurred_at, event_type, result, actor_user_id,
            target_user_id, application_id, tenant_id, session_id,
            correlation_id, network_summary, user_agent_summary, metadata)
        VALUES (
            '{eventId}', '2026-08-30T10:01:00Z', '{eventType}', {result},
            {actorUserIdSql}, NULL, NULL, NULL, NULL, '{correlationId}',
            NULL, NULL, {metadataSql});
        """;

    private static ConstraintDefinition Profile(string constraint, string sql) =>
        new("applications", "application_profiles", constraint, sql);

    private static ConstraintDefinition Redirect(string constraint, string sql) =>
        new("applications", "application_redirect_uris", constraint, sql);

    private static ConstraintDefinition Audit(string constraint, string sql) =>
        new("audit", "security_events", constraint, sql);

    private static ConstraintDefinition Authentication(string constraint, string sql) =>
        new("authentication", "authentication_transactions", constraint, sql);

    private static ConstraintDefinition Identity(string constraint, string sql) =>
        new("identity", "user_accounts", constraint, sql);

    private static ConstraintDefinition Outbox(string constraint, string sql) =>
        new("notifications", "outbox_messages", constraint, sql);

    private static ConstraintDefinition Session(string constraint, string sql) =>
        new("sessions", "sessions", constraint, sql);

    private sealed record ConstraintDefinition(
        string Schema,
        string Table,
        string Constraint,
        string ViolationSql);
}
