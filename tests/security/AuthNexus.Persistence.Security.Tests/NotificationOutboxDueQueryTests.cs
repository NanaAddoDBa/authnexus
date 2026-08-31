using AuthNexus.Application.Persistence;
using AuthNexus.Domain;
using AuthNexus.Modules.Notifications;
using AuthNexus.Persistence.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace AuthNexus.Persistence.Security.Tests;

public sealed class NotificationOutboxDueQueryFixture : PostgreSqlTestFixture;

public sealed class NotificationOutboxDueQueryTests :
    IClassFixture<NotificationOutboxDueQueryFixture>
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 8, 30, 9, 0, 0, TimeSpan.Zero);

    private readonly NotificationOutboxDueQueryFixture _fixture;

    public NotificationOutboxDueQueryTests(NotificationOutboxDueQueryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Due_query_filters_state_and_time_then_orders_by_schedule_and_message_id()
    {
        await ClearOutboxAsync();

        var firstTieId = new NotificationOutboxMessageId(
            Guid.Parse("70000000-0000-0000-0000-000000000001"));
        var secondTieId = new NotificationOutboxMessageId(
            Guid.Parse("70000000-0000-0000-0000-000000000002"));
        var earlierId = new NotificationOutboxMessageId(
            Guid.Parse("70000000-0000-0000-0000-000000000003"));
        var futureId = new NotificationOutboxMessageId(
            Guid.Parse("70000000-0000-0000-0000-000000000004"));
        var deliveredId = new NotificationOutboxMessageId(
            Guid.Parse("70000000-0000-0000-0000-000000000005"));
        var observationTime = CreatedAt.AddHours(1);

        var firstTie = CreateMessage(firstTieId, observationTime);
        var secondTie = CreateMessage(secondTieId, observationTime);
        var earlier = CreateMessage(earlierId, observationTime.AddMinutes(-30));
        var future = CreateMessage(futureId, observationTime.AddMinutes(1));
        var delivered = CreateMessage(deliveredId, observationTime.AddMinutes(-45));
        delivered.RecordDelivered(delivered.AvailableAt);

        using var provider = _fixture.Database.CreateServiceProvider();
        using (var writeScope = provider.CreateScope())
        {
            var repository = writeScope.ServiceProvider
                .GetRequiredService<INotificationOutboxRepository>();
            repository.Add(secondTie);
            repository.Add(future);
            repository.Add(delivered);
            repository.Add(firstTie);
            repository.Add(earlier);
            await writeScope.ServiceProvider
                .GetRequiredService<IAuthNexusUnitOfWork>()
                .CommitAsync();
        }

        using var readScope = provider.CreateScope();
        var due = await readScope.ServiceProvider
            .GetRequiredService<INotificationOutboxRepository>()
            .GetDueAsync(observationTime, maximumCount: 10);

        Assert.Equal(
            [earlierId, firstTieId, secondTieId],
            due.Select(candidate => candidate.Entity.MessageId));
    }

    [Fact]
    public async Task Due_query_applies_the_requested_batch_limit_after_ordering()
    {
        await ClearOutboxAsync();

        var laterId = new NotificationOutboxMessageId(Guid.NewGuid());
        var earlierId = new NotificationOutboxMessageId(Guid.NewGuid());
        var observationTime = CreatedAt.AddHours(1);

        using var provider = _fixture.Database.CreateServiceProvider();
        using (var writeScope = provider.CreateScope())
        {
            var repository = writeScope.ServiceProvider
                .GetRequiredService<INotificationOutboxRepository>();
            repository.Add(CreateMessage(laterId, observationTime));
            repository.Add(CreateMessage(earlierId, observationTime.AddMinutes(-1)));
            await writeScope.ServiceProvider
                .GetRequiredService<IAuthNexusUnitOfWork>()
                .CommitAsync();
        }

        using var readScope = provider.CreateScope();
        var due = await readScope.ServiceProvider
            .GetRequiredService<INotificationOutboxRepository>()
            .GetDueAsync(observationTime, maximumCount: 1);

        Assert.Single(due);
        Assert.Equal(earlierId, due[0].Entity.MessageId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task Due_query_rejects_an_out_of_range_batch_size(int maximumCount)
    {
        using var provider = _fixture.Database.CreateServiceProvider();
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<INotificationOutboxRepository>();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.GetDueAsync(CreatedAt, maximumCount));

        Assert.Equal("maximumCount", exception.ParamName);
    }

    private static NotificationOutboxMessage CreateMessage(
        NotificationOutboxMessageId messageId,
        DateTimeOffset availableAt) =>
        NotificationOutboxMessage.Create(
            messageId,
            new CorrelationId(Guid.NewGuid()),
            targetUserId: null,
            applicationId: null,
            tenantId: null,
            new NotificationType("security.due_query_test"),
            NotificationChannel.Email,
            new NotificationDestination($"{messageId.Value:N}@example.test"),
            ProtectedNotificationPayload.Create(
                [1, 2, 3],
                "test-payload:v1",
                formatVersion: 1),
            CreatedAt,
            availableAt);

    private async Task ClearOutboxAsync()
    {
        await using var connection = await _fixture.Database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM notifications.outbox_messages;";
        await command.ExecuteNonQueryAsync();
    }
}
