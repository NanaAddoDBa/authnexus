using AuthNexus.Application.Persistence;
using AuthNexus.Domain;
using AuthNexus.Modules.Audit;
using AuthNexus.Persistence.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AuthNexus.Persistence.Security.Tests;

public sealed class SecurityEventAppendOnlyFixture : PostgreSqlTestFixture;

public sealed class SecurityEventAppendOnlyTests :
    IClassFixture<SecurityEventAppendOnlyFixture>
{
    private readonly SecurityEventAppendOnlyFixture _fixture;

    public SecurityEventAppendOnlyTests(SecurityEventAppendOnlyFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Repository_appends_and_rehydrates_a_security_event()
    {
        var securityEvent = CreateSecurityEvent();
        using var provider = _fixture.Database.CreateServiceProvider();

        using (var writeScope = provider.CreateScope())
        {
            var repository = writeScope.ServiceProvider
                .GetRequiredService<ISecurityEventRepository>();
            repository.Append(securityEvent);
            await writeScope.ServiceProvider
                .GetRequiredService<IAuthNexusUnitOfWork>()
                .CommitAsync();
        }

        using var readScope = provider.CreateScope();
        var rehydrated = await readScope.ServiceProvider
            .GetRequiredService<ISecurityEventRepository>()
            .GetByIdAsync(securityEvent.EventId);

        Assert.NotNull(rehydrated);
        Assert.Equal(securityEvent.EventId, rehydrated.EventId);
        Assert.Equal(securityEvent.EventType, rehydrated.EventType);
        Assert.Equal(securityEvent.Result, rehydrated.Result);
        Assert.Equal(securityEvent.CorrelationId, rehydrated.CorrelationId);
        Assert.Equal("high", rehydrated.Metadata.Values["risk_level"]);
    }

    [Fact]
    public async Task Tracked_EF_update_is_rejected_before_database_execution()
    {
        var eventId = await AppendEventAsync();
        await using var context = _fixture.Database.CreateDbContext();
        var record = await FindTrackedSecurityEventAsync(context, eventId);

        context.Entry(record).Property("Metadata").CurrentValue = "{\"risk_level\":\"low\"}";

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());

        Assert.Equal("Append-only records cannot be updated or deleted.", exception.Message);
    }

    [Fact]
    public async Task Tracked_EF_delete_is_rejected_before_database_execution()
    {
        var eventId = await AppendEventAsync();
        await using var context = _fixture.Database.CreateDbContext();
        var record = await FindTrackedSecurityEventAsync(context, eventId);

        context.Remove(record);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());

        Assert.Equal("Append-only records cannot be updated or deleted.", exception.Message);
    }

    [Theory]
    [InlineData("UPDATE audit.security_events SET result = 2 WHERE event_id = @event_id;")]
    [InlineData("DELETE FROM audit.security_events WHERE event_id = @event_id;")]
    public async Task Direct_SQL_row_mutation_is_rejected_by_the_database(string sql)
    {
        var eventId = await AppendEventAsync();
        await using var connection = await _fixture.Database.OpenConnectionAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => ExecuteAsync(connection, sql, eventId));

        AssertAppendOnlyViolation(exception);
        Assert.Equal(1, await CountEventAsync(connection, eventId));
    }

    [Fact]
    public async Task Direct_SQL_truncate_is_rejected_by_the_database()
    {
        var eventId = await AppendEventAsync();
        await using var connection = await _fixture.Database.OpenConnectionAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => ExecuteAsync(
                connection,
                "TRUNCATE TABLE audit.security_events;",
                eventId));

        AssertAppendOnlyViolation(exception);
        Assert.Equal(1, await CountEventAsync(connection, eventId));
    }

    private async Task<Guid> AppendEventAsync()
    {
        var securityEvent = CreateSecurityEvent();
        using var provider = _fixture.Database.CreateServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider
            .GetRequiredService<ISecurityEventRepository>()
            .Append(securityEvent);
        await scope.ServiceProvider
            .GetRequiredService<IAuthNexusUnitOfWork>()
            .CommitAsync();

        return securityEvent.EventId.Value;
    }

    private static SecurityEvent CreateSecurityEvent() =>
        SecurityEvent.Create(
            new SecurityEventId(Guid.NewGuid()),
            new DateTimeOffset(2026, 8, 30, 18, 0, 0, TimeSpan.Zero),
            SecurityEventType.LoginSucceeded,
            SecurityEventResult.Succeeded,
            actorUserId: null,
            targetUserId: null,
            applicationId: null,
            tenantId: null,
            sessionId: null,
            new CorrelationId(Guid.NewGuid()),
            "loopback",
            "security-test-agent",
            SecurityEventMetadata.Create(
                [new KeyValuePair<string, string>("risk_level", "high")]));

    private static async Task<object> FindTrackedSecurityEventAsync(
        DbContext context,
        Guid eventId)
    {
        var entityType = context.Model.GetEntityTypes().Single(IsSecurityEventRecord);
        var record = await context.FindAsync(entityType.ClrType, [eventId]);

        Assert.NotNull(record);
        Assert.Equal(entityType.ClrType, record.GetType());
        return record;
    }

    private static bool IsSecurityEventRecord(IReadOnlyEntityType entityType) =>
        entityType.GetSchema() == "audit" &&
        entityType.GetTableName() == "security_events";

    private static async Task<int> ExecuteAsync(
        NpgsqlConnection connection,
        string sql,
        Guid eventId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("event_id", eventId);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountEventAsync(
        NpgsqlConnection connection,
        Guid eventId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT count(*) FROM audit.security_events WHERE event_id = @event_id;";
        command.Parameters.AddWithValue("event_id", eventId);
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private static void AssertAppendOnlyViolation(PostgresException exception)
    {
        Assert.Equal(PostgresErrorCodes.ObjectNotInPrerequisiteState, exception.SqlState);
        Assert.Equal("audit", exception.SchemaName);
        Assert.Equal("security_events", exception.TableName);
        Assert.Equal("Security events are append-only.", exception.MessageText);
    }
}
