using AuthNexus.Application.Persistence;
using AuthNexus.Infrastructure.Persistence;
using AuthNexus.Persistence.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthNexus.Persistence.Integration.Tests;

public sealed class UnitOfWorkTests : PostgreSqlTestFixture
{
    [Fact]
    public async Task Commit_persists_changes_staged_by_multiple_repositories()
    {
        var profile = PersistenceTestData.CreateApplicationProfile();
        var account = PersistenceTestData.CreateUserAccount();
        var securityEvent = PersistenceTestData.CreateSecurityEvent();
        await using var provider = Database.CreateServiceProvider();
        await using var writeScope = provider.CreateAsyncScope();
        var services = writeScope.ServiceProvider;
        var unitOfWork = services.GetRequiredService<IAuthNexusUnitOfWork>();

        Assert.Same(services.GetRequiredService<AuthNexusDbContext>(), unitOfWork);

        services.GetRequiredService<IApplicationProfileRepository>().Add(profile);
        services.GetRequiredService<IUserAccountRepository>().Add(account);
        services.GetRequiredService<ISecurityEventRepository>().Append(securityEvent);

        await using (var beforeCommitScope = provider.CreateAsyncScope())
        {
            Assert.Null(
                await beforeCommitScope.ServiceProvider
                    .GetRequiredService<IApplicationProfileRepository>()
                    .GetByIdAsync(profile.ApplicationId));
            Assert.Null(
                await beforeCommitScope.ServiceProvider
                    .GetRequiredService<IUserAccountRepository>()
                    .GetByIdAsync(account.UserId));
            Assert.Null(
                await beforeCommitScope.ServiceProvider
                    .GetRequiredService<ISecurityEventRepository>()
                    .GetByIdAsync(securityEvent.EventId));
        }

        Assert.Equal(5, await unitOfWork.CommitAsync());

        await using var verificationScope = provider.CreateAsyncScope();
        Assert.NotNull(
            await verificationScope.ServiceProvider
                .GetRequiredService<IApplicationProfileRepository>()
                .GetByIdAsync(profile.ApplicationId));
        Assert.NotNull(
            await verificationScope.ServiceProvider
                .GetRequiredService<IUserAccountRepository>()
                .GetByIdAsync(account.UserId));
        Assert.NotNull(
            await verificationScope.ServiceProvider
                .GetRequiredService<ISecurityEventRepository>()
                .GetByIdAsync(securityEvent.EventId));
    }

    [Fact]
    public async Task Explicit_transaction_commits_its_complete_staged_set()
    {
        var account = PersistenceTestData.CreateUserAccount();
        var transaction = PersistenceTestData.CreateAuthenticationTransaction();
        await using var provider = Database.CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;

        await services.GetRequiredService<IAuthNexusUnitOfWork>()
            .ExecuteInTransactionAsync(
                _ =>
                {
                    services.GetRequiredService<IUserAccountRepository>().Add(account);
                    services.GetRequiredService<IAuthenticationTransactionRepository>()
                        .Add(transaction);
                    return Task.CompletedTask;
                });

        await using var verificationScope = provider.CreateAsyncScope();
        Assert.NotNull(
            await verificationScope.ServiceProvider
                .GetRequiredService<IUserAccountRepository>()
                .GetByIdAsync(account.UserId));
        Assert.NotNull(
            await verificationScope.ServiceProvider
                .GetRequiredService<IAuthenticationTransactionRepository>()
                .GetByIdAsync(transaction.TransactionId));
    }

    [Fact]
    public async Task Failed_transaction_rolls_back_clears_tracking_and_allows_context_reuse()
    {
        var duplicateSecretHash = PersistenceTestData.NewSessionSecretHash();
        var firstSession = PersistenceTestData.CreateSession(secretHash: duplicateSecretHash);
        var secondSession = PersistenceTestData.CreateSession(secretHash: duplicateSecretHash);
        var laterAccount = PersistenceTestData.CreateUserAccount();
        await using var provider = Database.CreateServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<AuthNexusDbContext>();
        var unitOfWork = services.GetRequiredService<IAuthNexusUnitOfWork>();

        await Assert.ThrowsAsync<DbUpdateException>(
            () => unitOfWork.ExecuteInTransactionAsync(
                _ =>
                {
                    var sessions = services.GetRequiredService<ISessionRepository>();
                    sessions.Add(firstSession);
                    sessions.Add(secondSession);
                    return Task.CompletedTask;
                }));

        Assert.Empty(context.ChangeTracker.Entries());

        services.GetRequiredService<IUserAccountRepository>().Add(laterAccount);
        Assert.Equal(1, await unitOfWork.CommitAsync());

        await using var verificationScope = provider.CreateAsyncScope();
        var verificationServices = verificationScope.ServiceProvider;
        Assert.Null(
            await verificationServices.GetRequiredService<ISessionRepository>()
                .GetByIdAsync(firstSession.SessionId));
        Assert.Null(
            await verificationServices.GetRequiredService<ISessionRepository>()
                .GetByIdAsync(secondSession.SessionId));
        Assert.NotNull(
            await verificationServices.GetRequiredService<IUserAccountRepository>()
                .GetByIdAsync(laterAccount.UserId));
    }
}
