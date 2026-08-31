using AuthNexus.Application.Persistence;
using AuthNexus.Modules.Notifications;
using AuthNexus.Persistence.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace AuthNexus.Persistence.Integration.Tests;

public sealed class NotificationOutboxDueQueryTests : PostgreSqlTestFixture
{
    [Fact]
    public async Task Due_query_filters_states_includes_the_boundary_and_orders_deterministically()
    {
        var observedAt = PersistenceTestData.BaseTime.AddHours(1);
        var earlyPending = PersistenceTestData.CreateOutboxMessage(
            observedAt.AddMinutes(-30),
            observedAt.AddMinutes(-10));
        var tiedPendingA = PersistenceTestData.CreateOutboxMessage(
            observedAt.AddMinutes(-30),
            observedAt.AddMinutes(-5));
        var tiedPendingB = PersistenceTestData.CreateOutboxMessage(
            observedAt.AddMinutes(-30),
            observedAt.AddMinutes(-5));
        var dueRetry = PersistenceTestData.CreateOutboxMessage(
            observedAt.AddMinutes(-30),
            observedAt.AddMinutes(-20));
        dueRetry.ScheduleRetry(
            observedAt.AddMinutes(-20),
            observedAt.AddMinutes(-5),
            new NotificationDeliveryFailureCode("provider.timeout"));
        var boundaryPending = PersistenceTestData.CreateOutboxMessage(
            observedAt.AddMinutes(-30),
            observedAt);
        var futurePending = PersistenceTestData.CreateOutboxMessage(
            observedAt.AddMinutes(-30),
            observedAt.AddMinutes(1));
        var delivered = PersistenceTestData.CreateOutboxMessage(
            observedAt.AddMinutes(-30),
            observedAt.AddMinutes(-15));
        delivered.RecordDelivered(delivered.AvailableAt);
        var permanentlyFailed = PersistenceTestData.CreateOutboxMessage(
            observedAt.AddMinutes(-30),
            observedAt.AddMinutes(-15));
        permanentlyFailed.FailPermanently(
            permanentlyFailed.AvailableAt,
            new NotificationDeliveryFailureCode("destination.rejected"));
        NotificationOutboxMessage[] messages =
        [
            earlyPending,
            tiedPendingA,
            tiedPendingB,
            dueRetry,
            boundaryPending,
            futurePending,
            delivered,
            permanentlyFailed,
        ];
        await using var provider = Database.CreateServiceProvider();

        await using (var writeScope = provider.CreateAsyncScope())
        {
            var writeRepository = writeScope.ServiceProvider
                .GetRequiredService<INotificationOutboxRepository>();

            foreach (var message in messages)
            {
                writeRepository.Add(message);
            }

            Assert.Equal(
                messages.Length,
                await writeScope.ServiceProvider
                    .GetRequiredService<IAuthNexusUnitOfWork>()
                    .CommitAsync());
        }

        var expectedDue = new[]
            {
                earlyPending,
                tiedPendingA,
                tiedPendingB,
                dueRetry,
                boundaryPending,
            }
            .OrderBy(message => message.NextAttemptAt)
            .ThenBy(
                message => message.MessageId.Value.ToString("N"),
                StringComparer.Ordinal)
            .Select(message => message.MessageId)
            .ToArray();

        await using var readScope = provider.CreateAsyncScope();
        var readRepository = readScope.ServiceProvider
            .GetRequiredService<INotificationOutboxRepository>();
        var due = await readRepository.GetDueAsync(observedAt, 500);

        Assert.Equal(expectedDue, due.Select(message => message.Entity.MessageId));
        Assert.All(
            due,
            message => Assert.True(message.Entity.CanBeAttemptedAt(observedAt)));
        Assert.Contains(
            due,
            message => message.Entity.State == NotificationOutboxState.RetryScheduled);
        Assert.DoesNotContain(due, message => message.Entity.MessageId == futurePending.MessageId);
        Assert.DoesNotContain(due, message => message.Entity.MessageId == delivered.MessageId);
        Assert.DoesNotContain(
            due,
            message => message.Entity.MessageId == permanentlyFailed.MessageId);

        var limited = await readRepository.GetDueAsync(observedAt, 3);
        Assert.Equal(expectedDue.Take(3), limited.Select(message => message.Entity.MessageId));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(501)]
    public async Task Due_query_rejects_an_out_of_range_batch_size(int maximumCount)
    {
        await using var provider = Database.CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<INotificationOutboxRepository>();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.GetDueAsync(PersistenceTestData.BaseTime, maximumCount));

        Assert.Equal("maximumCount", exception.ParamName);
    }

    [Fact]
    public async Task Due_query_rejects_a_default_observation_time()
    {
        await using var provider = Database.CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<INotificationOutboxRepository>();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.GetDueAsync(default, 1));

        Assert.Equal("observedAt", exception.ParamName);
    }
}
