using AuthNexus.Persistence.Tests.Support;
using Npgsql;

namespace AuthNexus.Persistence.Security.Tests;

public sealed class ApplicationRedirectInvariantFixture : PostgreSqlTestFixture;

public sealed class ApplicationRedirectInvariantTests :
    IClassFixture<ApplicationRedirectInvariantFixture>
{
    private const string RedirectConstraint = "ck_application_profiles_has_redirect_uri";

    private readonly ApplicationRedirectInvariantFixture _fixture;

    public ApplicationRedirectInvariantTests(ApplicationRedirectInvariantFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Profile_and_its_first_redirect_can_commit_in_one_transaction()
    {
        var applicationId = Guid.NewGuid();
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await InsertProfileAsync(connection, transaction, applicationId);
        await InsertRedirectAsync(
            connection,
            transaction,
            applicationId,
            "https://first-redirect.example.test/callback");

        await transaction.CommitAsync();

        Assert.Equal(1, await CountRedirectsAsync(connection, applicationId));
    }

    [Fact]
    public async Task First_redirect_and_its_profile_can_commit_in_child_first_order()
    {
        var applicationId = Guid.NewGuid();
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await InsertRedirectAsync(
            connection,
            transaction,
            applicationId,
            "https://child-first.example.test/callback");
        await InsertProfileAsync(connection, transaction, applicationId);

        await transaction.CommitAsync();

        Assert.Equal(1, await CountRedirectsAsync(connection, applicationId));
    }

    [Fact]
    public async Task Redirect_foreign_key_is_deferrable_and_initially_deferred()
    {
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT constraint_record.condeferrable, constraint_record.condeferred
            FROM pg_constraint AS constraint_record
            INNER JOIN pg_class AS relation
                ON relation.oid = constraint_record.conrelid
            INNER JOIN pg_namespace AS namespace
                ON namespace.oid = relation.relnamespace
            WHERE namespace.nspname = 'applications'
              AND relation.relname = 'application_redirect_uris'
              AND constraint_record.conname =
                  'fk_application_redirect_uris_application_profiles';
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.True(reader.GetBoolean(0));
        Assert.True(reader.GetBoolean(1));
        Assert.False(await reader.ReadAsync());
    }

    [Fact]
    public async Task Committing_a_profile_without_a_redirect_is_rejected_at_commit_time()
    {
        var applicationId = Guid.NewGuid();
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        await InsertProfileAsync(connection, transaction, applicationId);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => transaction.CommitAsync());

        AssertRedirectConstraint(exception);
    }

    [Fact]
    public async Task Deleting_the_last_redirect_is_rejected_at_commit_time()
    {
        var applicationId = Guid.NewGuid();
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await InsertCompleteProfileAsync(connection, applicationId);
        await using var transaction = await connection.BeginTransactionAsync();

        await ExecuteAsync(
            connection,
            transaction,
            "DELETE FROM applications.application_redirect_uris " +
            "WHERE application_id = @application_id;",
            new NpgsqlParameter("application_id", applicationId));

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => transaction.CommitAsync());

        AssertRedirectConstraint(exception);
    }

    [Fact]
    public async Task Replacing_the_last_redirect_in_the_same_transaction_is_allowed()
    {
        var applicationId = Guid.NewGuid();
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await InsertCompleteProfileAsync(connection, applicationId);
        await using var transaction = await connection.BeginTransactionAsync();

        await ExecuteAsync(
            connection,
            transaction,
            "DELETE FROM applications.application_redirect_uris " +
            "WHERE application_id = @application_id;",
            new NpgsqlParameter("application_id", applicationId));
        await InsertRedirectAsync(
            connection,
            transaction,
            applicationId,
            "https://replacement.example.test/callback");

        await transaction.CommitAsync();

        Assert.Equal(1, await CountRedirectsAsync(connection, applicationId));
    }

    [Fact]
    public async Task Moving_a_profiles_only_redirect_to_another_profile_is_rejected()
    {
        var sourceApplicationId = Guid.NewGuid();
        var destinationApplicationId = Guid.NewGuid();
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await InsertCompleteProfileAsync(connection, sourceApplicationId);
        await InsertCompleteProfileAsync(connection, destinationApplicationId);

        await using (var prepare = connection.CreateCommand())
        {
            prepare.CommandText =
                """
                UPDATE applications.application_redirect_uris
                SET sort_order = 1
                WHERE application_id = @application_id;
                """;
            prepare.Parameters.AddWithValue("application_id", sourceApplicationId);
            Assert.Equal(1, await prepare.ExecuteNonQueryAsync());
        }

        await using var transaction = await connection.BeginTransactionAsync();
        await ExecuteAsync(
            connection,
            transaction,
            """
            UPDATE applications.application_redirect_uris
            SET application_id = @destination_application_id
            WHERE application_id = @source_application_id;
            """,
            new NpgsqlParameter("destination_application_id", destinationApplicationId),
            new NpgsqlParameter("source_application_id", sourceApplicationId));

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => transaction.CommitAsync());

        AssertRedirectConstraint(exception);
        Assert.Equal(1, await CountRedirectsAsync(connection, sourceApplicationId));
    }

    [Fact]
    public async Task Deleting_a_profile_cascades_its_last_redirect_without_false_rejection()
    {
        var applicationId = Guid.NewGuid();
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await InsertCompleteProfileAsync(connection, applicationId);
        await using var transaction = await connection.BeginTransactionAsync();

        await ExecuteAsync(
            connection,
            transaction,
            "DELETE FROM applications.application_profiles " +
            "WHERE application_id = @application_id;",
            new NpgsqlParameter("application_id", applicationId));
        await transaction.CommitAsync();

        Assert.Equal(0, await CountRedirectsAsync(connection, applicationId));
    }

    [Fact]
    public async Task Truncating_redirects_is_rejected_before_data_can_be_removed()
    {
        var applicationId = Guid.NewGuid();
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await InsertCompleteProfileAsync(connection, applicationId);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => ExecuteAsync(
                connection,
                transaction: null,
                "TRUNCATE TABLE applications.application_redirect_uris;"));

        AssertRedirectConstraint(exception);
        Assert.Equal(1, await CountRedirectsAsync(connection, applicationId));
    }

    private static async Task InsertCompleteProfileAsync(
        NpgsqlConnection connection,
        Guid applicationId)
    {
        await using var transaction = await connection.BeginTransactionAsync();
        await InsertProfileAsync(connection, transaction, applicationId);
        await InsertRedirectAsync(
            connection,
            transaction,
            applicationId,
            $"https://{applicationId:N}.example.test/callback");
        await transaction.CommitAsync();
    }

    private static Task<int> InsertProfileAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid applicationId) =>
        ExecuteAsync(
            connection,
            transaction,
            """
            INSERT INTO applications.application_profiles (
                application_id, tenant_id, application_type, application_audience,
                application_mode, application_name, default_locale,
                authentication_policy_reference, registration_schema_reference, version)
            VALUES (
                @application_id, NULL, 1, 1, 1, 'Redirect invariant test', 'en-US',
                'policy:redirect-test', NULL, @version);
            """,
            new NpgsqlParameter("application_id", applicationId),
            new NpgsqlParameter("version", Guid.NewGuid()));

    private static Task<int> InsertRedirectAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid applicationId,
        string redirectUri) =>
        ExecuteAsync(
            connection,
            transaction,
            """
            INSERT INTO applications.application_redirect_uris (
                application_id, redirect_uri, sort_order)
            VALUES (@application_id, @redirect_uri, 0);
            """,
            new NpgsqlParameter("application_id", applicationId),
            new NpgsqlParameter("redirect_uri", redirectUri));

    private static async Task<long> CountRedirectsAsync(
        NpgsqlConnection connection,
        Guid applicationId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT count(*) FROM applications.application_redirect_uris " +
            "WHERE application_id = @application_id;";
        command.Parameters.AddWithValue("application_id", applicationId);

        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private static async Task<int> ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        return await command.ExecuteNonQueryAsync();
    }

    private static void AssertRedirectConstraint(PostgresException exception)
    {
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal(RedirectConstraint, exception.ConstraintName);
    }
}
