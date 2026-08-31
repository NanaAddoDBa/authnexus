using AuthNexus.Application.Persistence;
using AuthNexus.Infrastructure.Persistence;
using AuthNexus.Modules.Authentication;
using AuthNexus.Modules.Identity;
using AuthNexus.Modules.Notifications;
using AuthNexus.Persistence.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthNexus.Persistence.Integration.Tests;

public sealed class OptimisticConcurrencyTests : PostgreSqlTestFixture
{
    [Fact]
    public async Task User_account_rejects_a_stale_update_and_preserves_the_first_writer()
    {
        var account = PersistenceTestData.CreateUserAccount();
        await using var provider = Database.CreateServiceProvider();
        await SeedUserAccountAsync(provider, account);

        await using var firstScope = provider.CreateAsyncScope();
        await using var staleScope = provider.CreateAsyncScope();
        var firstRepository = firstScope.ServiceProvider
            .GetRequiredService<IUserAccountRepository>();
        var staleRepository = staleScope.ServiceProvider
            .GetRequiredService<IUserAccountRepository>();
        var first = AssertPersisted(
            await firstRepository.GetByIdAsync(account.UserId));
        var stale = AssertPersisted(
            await staleRepository.GetByIdAsync(account.UserId));

        first.Entity.Activate(PersistenceTestData.BaseTime.AddMinutes(1));
        stale.Entity.Activate(PersistenceTestData.BaseTime.AddMinutes(2));

        firstRepository.Update(first);
        Assert.Equal(
            1,
            await firstScope.ServiceProvider
                .GetRequiredService<IAuthNexusUnitOfWork>()
                .CommitAsync());

        staleRepository.Update(stale);
        await AssertPersistenceConflictAsync(staleScope.ServiceProvider);

        await using var verificationScope = provider.CreateAsyncScope();
        var stored = AssertPersisted(
            await verificationScope.ServiceProvider
                .GetRequiredService<IUserAccountRepository>()
                .GetByIdAsync(account.UserId));
        Assert.Equal(UserAccountState.Active, stored.Entity.State);
        Assert.Equal(PersistenceTestData.BaseTime.AddMinutes(1), stored.Entity.StateChangedAt);
        Assert.NotEqual(first.Version, stored.Version);
    }

    [Fact]
    public async Task Authentication_transaction_rejects_a_stale_update_and_preserves_the_first_writer()
    {
        var transaction = PersistenceTestData.CreateAuthenticationTransaction();
        await using var provider = Database.CreateServiceProvider();
        await SeedAuthenticationTransactionAsync(provider, transaction);

        await using var firstScope = provider.CreateAsyncScope();
        await using var staleScope = provider.CreateAsyncScope();
        var firstRepository = firstScope.ServiceProvider
            .GetRequiredService<IAuthenticationTransactionRepository>();
        var staleRepository = staleScope.ServiceProvider
            .GetRequiredService<IAuthenticationTransactionRepository>();
        var first = AssertPersisted(
            await firstRepository.GetByIdAsync(transaction.TransactionId));
        var stale = AssertPersisted(
            await staleRepository.GetByIdAsync(transaction.TransactionId));

        first.Entity.IssueChallenge(PersistenceTestData.BaseTime.AddMinutes(1));
        stale.Entity.MarkPrimaryVerified(PersistenceTestData.BaseTime.AddMinutes(2));

        firstRepository.Update(first);
        Assert.Equal(
            1,
            await firstScope.ServiceProvider
                .GetRequiredService<IAuthNexusUnitOfWork>()
                .CommitAsync());

        staleRepository.Update(stale);
        await AssertPersistenceConflictAsync(staleScope.ServiceProvider);

        await using var verificationScope = provider.CreateAsyncScope();
        var stored = AssertPersisted(
            await verificationScope.ServiceProvider
                .GetRequiredService<IAuthenticationTransactionRepository>()
                .GetByIdAsync(transaction.TransactionId));
        Assert.Equal(AuthenticationTransactionState.ChallengeIssued, stored.Entity.State);
        Assert.Equal(PersistenceTestData.BaseTime.AddMinutes(1), stored.Entity.StateChangedAt);
        Assert.NotEqual(first.Version, stored.Version);
    }

    [Fact]
    public async Task Session_rejects_a_stale_update_and_preserves_the_first_writer()
    {
        var session = PersistenceTestData.CreateSession();
        var originalSecretHash = session.SecretHash;
        await using var provider = Database.CreateServiceProvider();
        await SeedSessionAsync(provider, session);

        await using var firstScope = provider.CreateAsyncScope();
        await using var staleScope = provider.CreateAsyncScope();
        var firstRepository = firstScope.ServiceProvider.GetRequiredService<ISessionRepository>();
        var staleRepository = staleScope.ServiceProvider.GetRequiredService<ISessionRepository>();
        var first = AssertPersisted(
            await firstRepository.GetByIdAsync(session.SessionId));
        var stale = AssertPersisted(
            await staleRepository.GetByIdAsync(session.SessionId));

        first.Entity.RecordActivity(
            PersistenceTestData.BaseTime.AddMinutes(1),
            PersistenceTestData.BaseTime.AddMinutes(40));
        stale.Entity.RotateSecretHash(
            PersistenceTestData.NewSessionSecretHash(),
            PersistenceTestData.BaseTime.AddMinutes(2));

        firstRepository.Update(first);
        Assert.Equal(
            1,
            await firstScope.ServiceProvider
                .GetRequiredService<IAuthNexusUnitOfWork>()
                .CommitAsync());

        staleRepository.Update(stale);
        await AssertPersistenceConflictAsync(staleScope.ServiceProvider);

        await using var verificationScope = provider.CreateAsyncScope();
        var stored = AssertPersisted(
            await verificationScope.ServiceProvider
                .GetRequiredService<ISessionRepository>()
                .GetByIdAsync(session.SessionId));
        Assert.Equal(originalSecretHash, stored.Entity.SecretHash);
        Assert.Equal(PersistenceTestData.BaseTime.AddMinutes(1), stored.Entity.LastSeenAt);
        Assert.Equal(PersistenceTestData.BaseTime.AddMinutes(40), stored.Entity.IdleExpiresAt);
        Assert.Equal(0, stored.Entity.RotationCount);
        Assert.NotEqual(first.Version, stored.Version);
    }

    [Fact]
    public async Task Outbox_message_rejects_a_stale_update_and_preserves_the_first_writer()
    {
        var message = PersistenceTestData.CreateOutboxMessage();
        await using var provider = Database.CreateServiceProvider();
        await SeedOutboxMessageAsync(provider, message);

        await using var firstScope = provider.CreateAsyncScope();
        await using var staleScope = provider.CreateAsyncScope();
        var firstRepository = firstScope.ServiceProvider
            .GetRequiredService<INotificationOutboxRepository>();
        var staleRepository = staleScope.ServiceProvider
            .GetRequiredService<INotificationOutboxRepository>();
        var first = AssertPersisted(
            await firstRepository.GetByIdAsync(message.MessageId));
        var stale = AssertPersisted(
            await staleRepository.GetByIdAsync(message.MessageId));

        first.Entity.RecordDelivered(first.Entity.AvailableAt);
        stale.Entity.ScheduleRetry(
            stale.Entity.AvailableAt,
            stale.Entity.AvailableAt.AddMinutes(5),
            new NotificationDeliveryFailureCode("provider.timeout"));

        firstRepository.Update(first);
        Assert.Equal(
            1,
            await firstScope.ServiceProvider
                .GetRequiredService<IAuthNexusUnitOfWork>()
                .CommitAsync());

        staleRepository.Update(stale);
        await AssertPersistenceConflictAsync(staleScope.ServiceProvider);

        await using var verificationScope = provider.CreateAsyncScope();
        var stored = AssertPersisted(
            await verificationScope.ServiceProvider
                .GetRequiredService<INotificationOutboxRepository>()
                .GetByIdAsync(message.MessageId));
        Assert.Equal(NotificationOutboxState.Delivered, stored.Entity.State);
        Assert.Equal(message.AvailableAt, stored.Entity.DeliveredAt);
        Assert.Null(stored.Entity.NextAttemptAt);
        Assert.NotEqual(first.Version, stored.Version);
    }

    private static async Task SeedUserAccountAsync(
        ServiceProvider provider,
        AuthNexus.Modules.Identity.UserAccount account)
    {
        await using var scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IUserAccountRepository>().Add(account);
        await scope.ServiceProvider.GetRequiredService<IAuthNexusUnitOfWork>().CommitAsync();
    }

    private static async Task SeedAuthenticationTransactionAsync(
        ServiceProvider provider,
        AuthenticationTransaction transaction)
    {
        await using var scope = provider.CreateAsyncScope();
        scope.ServiceProvider
            .GetRequiredService<IAuthenticationTransactionRepository>()
            .Add(transaction);
        await scope.ServiceProvider.GetRequiredService<IAuthNexusUnitOfWork>().CommitAsync();
    }

    private static async Task SeedSessionAsync(
        ServiceProvider provider,
        AuthNexus.Modules.Sessions.Session session)
    {
        await using var scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ISessionRepository>().Add(session);
        await scope.ServiceProvider.GetRequiredService<IAuthNexusUnitOfWork>().CommitAsync();
    }

    private static async Task SeedOutboxMessageAsync(
        ServiceProvider provider,
        NotificationOutboxMessage message)
    {
        await using var scope = provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<INotificationOutboxRepository>().Add(message);
        await scope.ServiceProvider.GetRequiredService<IAuthNexusUnitOfWork>().CommitAsync();
    }

    private static async Task AssertPersistenceConflictAsync(IServiceProvider services)
    {
        var exception = await Assert.ThrowsAsync<PersistenceConflictException>(
            () => services.GetRequiredService<IAuthNexusUnitOfWork>().CommitAsync());

        Assert.IsType<DbUpdateConcurrencyException>(exception.InnerException);
        Assert.Empty(
            services.GetRequiredService<AuthNexusDbContext>().ChangeTracker.Entries());
    }

    private static Persisted<T> AssertPersisted<T>(Persisted<T>? persisted)
        where T : class
    {
        Assert.NotNull(persisted);
        return persisted;
    }
}
