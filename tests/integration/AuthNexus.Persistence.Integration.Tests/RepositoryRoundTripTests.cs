using AuthNexus.Application.Persistence;
using AuthNexus.Modules.Authentication;
using AuthNexus.Modules.Identity;
using AuthNexus.Modules.Notifications;
using AuthNexus.Modules.Sessions;
using AuthNexus.Persistence.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace AuthNexus.Persistence.Integration.Tests;

public sealed class RepositoryRoundTripTests : PostgreSqlTestFixture
{
    [Fact]
    public async Task Application_profile_round_trips_with_ordered_redirects()
    {
        var expected = PersistenceTestData.CreateApplicationProfile();
        await using var provider = Database.CreateServiceProvider();

        await using (var writeScope = provider.CreateAsyncScope())
        {
            writeScope.ServiceProvider
                .GetRequiredService<IApplicationProfileRepository>()
                .Add(expected);

            Assert.Equal(
                3,
                await writeScope.ServiceProvider
                    .GetRequiredService<IAuthNexusUnitOfWork>()
                    .CommitAsync());
        }

        await using var readScope = provider.CreateAsyncScope();
        var persisted = await readScope.ServiceProvider
            .GetRequiredService<IApplicationProfileRepository>()
            .GetByIdAsync(expected.ApplicationId);

        Assert.NotNull(persisted);
        Assert.NotEqual(Guid.Empty, persisted.Version.Value);

        var actual = persisted.Entity;
        Assert.Equal(expected.ApplicationId, actual.ApplicationId);
        Assert.Equal(expected.TenantId, actual.TenantId);
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.Audience, actual.Audience);
        Assert.Equal(expected.Mode, actual.Mode);
        Assert.Equal(expected.ApplicationName, actual.ApplicationName);
        Assert.Equal(expected.DefaultLocale, actual.DefaultLocale);
        Assert.Equal(
            expected.AuthenticationPolicyReference,
            actual.AuthenticationPolicyReference);
        Assert.Equal(
            expected.RegistrationSchemaReference,
            actual.RegistrationSchemaReference);
        Assert.Equal(
            expected.AllowedRedirectUris.Select(redirect => redirect.Value),
            actual.AllowedRedirectUris.Select(redirect => redirect.Value));
    }

    [Fact]
    public async Task User_account_round_trips_an_accepted_state_transition()
    {
        var expected = PersistenceTestData.CreateUserAccount();
        expected.Activate(PersistenceTestData.BaseTime.AddMinutes(2));
        await using var provider = Database.CreateServiceProvider();

        await using (var writeScope = provider.CreateAsyncScope())
        {
            writeScope.ServiceProvider
                .GetRequiredService<IUserAccountRepository>()
                .Add(expected);
            Assert.Equal(
                1,
                await writeScope.ServiceProvider
                    .GetRequiredService<IAuthNexusUnitOfWork>()
                    .CommitAsync());
        }

        await using var readScope = provider.CreateAsyncScope();
        var persisted = await readScope.ServiceProvider
            .GetRequiredService<IUserAccountRepository>()
            .GetByIdAsync(expected.UserId);

        Assert.NotNull(persisted);
        Assert.NotEqual(Guid.Empty, persisted.Version.Value);
        Assert.Equal(expected.UserId, persisted.Entity.UserId);
        Assert.Equal(UserAccountState.Active, persisted.Entity.State);
        Assert.Equal(expected.CreatedAt, persisted.Entity.CreatedAt);
        Assert.Equal(expected.StateChangedAt, persisted.Entity.StateChangedAt);
    }

    [Fact]
    public async Task Authentication_transaction_round_trips_terminal_state_and_context()
    {
        var expected = PersistenceTestData.CreateAuthenticationTransaction();
        expected.IssueChallenge(PersistenceTestData.BaseTime.AddMinutes(1));
        expected.MarkPrimaryVerified(PersistenceTestData.BaseTime.AddMinutes(2));
        expected.Complete(PersistenceTestData.BaseTime.AddMinutes(3));
        await using var provider = Database.CreateServiceProvider();

        await using (var writeScope = provider.CreateAsyncScope())
        {
            writeScope.ServiceProvider
                .GetRequiredService<IAuthenticationTransactionRepository>()
                .Add(expected);
            Assert.Equal(
                1,
                await writeScope.ServiceProvider
                    .GetRequiredService<IAuthNexusUnitOfWork>()
                    .CommitAsync());
        }

        await using var readScope = provider.CreateAsyncScope();
        var persisted = await readScope.ServiceProvider
            .GetRequiredService<IAuthenticationTransactionRepository>()
            .GetByIdAsync(expected.TransactionId);

        Assert.NotNull(persisted);
        Assert.NotEqual(Guid.Empty, persisted.Version.Value);

        var actual = persisted.Entity;
        Assert.Equal(expected.TransactionId, actual.TransactionId);
        Assert.Equal(expected.ApplicationId, actual.ApplicationId);
        Assert.Equal(expected.TenantId, actual.TenantId);
        Assert.Equal(expected.UserId, actual.UserId);
        Assert.Equal(expected.Purpose, actual.Purpose);
        Assert.Equal(expected.CorrelationId, actual.CorrelationId);
        Assert.Equal(AuthenticationTransactionState.Completed, actual.State);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.ExpiresAt, actual.ExpiresAt);
        Assert.Equal(expected.StateChangedAt, actual.StateChangedAt);
        Assert.Equal(expected.CompletedAt, actual.CompletedAt);
        Assert.Null(actual.FailedAt);
    }

    [Fact]
    public async Task Session_round_trips_activity_rotation_and_revocation()
    {
        var expected = PersistenceTestData.CreateSession();
        expected.RecordActivity(
            PersistenceTestData.BaseTime.AddMinutes(5),
            PersistenceTestData.BaseTime.AddMinutes(45));
        expected.RotateSecretHash(
            PersistenceTestData.NewSessionSecretHash(),
            PersistenceTestData.BaseTime.AddMinutes(10));
        expected.Revoke(
            SessionRevocationReason.UserLogout,
            PersistenceTestData.BaseTime.AddMinutes(12));
        await using var provider = Database.CreateServiceProvider();

        await using (var writeScope = provider.CreateAsyncScope())
        {
            writeScope.ServiceProvider.GetRequiredService<ISessionRepository>().Add(expected);
            Assert.Equal(
                1,
                await writeScope.ServiceProvider
                    .GetRequiredService<IAuthNexusUnitOfWork>()
                    .CommitAsync());
        }

        await using var readScope = provider.CreateAsyncScope();
        var persisted = await readScope.ServiceProvider
            .GetRequiredService<ISessionRepository>()
            .GetByIdAsync(expected.SessionId);

        Assert.NotNull(persisted);
        Assert.NotEqual(Guid.Empty, persisted.Version.Value);

        var actual = persisted.Entity;
        Assert.Equal(expected.SessionId, actual.SessionId);
        Assert.Equal(expected.SecretHash, actual.SecretHash);
        Assert.Equal(expected.UserId, actual.UserId);
        Assert.Equal(expected.ApplicationId, actual.ApplicationId);
        Assert.Equal(expected.TenantId, actual.TenantId);
        Assert.Equal(SessionState.Revoked, actual.State);
        Assert.Equal(expected.AuthenticatedAt, actual.AuthenticatedAt);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.LastSeenAt, actual.LastSeenAt);
        Assert.Equal(expected.IdleExpiresAt, actual.IdleExpiresAt);
        Assert.Equal(expected.AbsoluteExpiresAt, actual.AbsoluteExpiresAt);
        Assert.Equal(expected.UpdatedAt, actual.UpdatedAt);
        Assert.Equal(expected.StateChangedAt, actual.StateChangedAt);
        Assert.Equal(expected.SecretRotatedAt, actual.SecretRotatedAt);
        Assert.Equal(expected.RotationCount, actual.RotationCount);
        Assert.Equal(expected.RevokedAt, actual.RevokedAt);
        Assert.Equal(expected.RevocationReason, actual.RevocationReason);
        Assert.Null(actual.ExpiredAt);
    }

    [Fact]
    public async Task Security_event_round_trips_immutable_context_and_metadata()
    {
        var expected = PersistenceTestData.CreateSecurityEvent();
        await using var provider = Database.CreateServiceProvider();

        await using (var writeScope = provider.CreateAsyncScope())
        {
            writeScope.ServiceProvider
                .GetRequiredService<ISecurityEventRepository>()
                .Append(expected);
            Assert.Equal(
                1,
                await writeScope.ServiceProvider
                    .GetRequiredService<IAuthNexusUnitOfWork>()
                    .CommitAsync());
        }

        await using var readScope = provider.CreateAsyncScope();
        var actual = await readScope.ServiceProvider
            .GetRequiredService<ISecurityEventRepository>()
            .GetByIdAsync(expected.EventId);

        Assert.NotNull(actual);
        Assert.Equal(expected.EventId, actual.EventId);
        Assert.Equal(expected.Timestamp, actual.Timestamp);
        Assert.Equal(expected.EventType, actual.EventType);
        Assert.Equal(expected.Result, actual.Result);
        Assert.Equal(expected.ActorUserId, actual.ActorUserId);
        Assert.Equal(expected.TargetUserId, actual.TargetUserId);
        Assert.Equal(expected.ApplicationId, actual.ApplicationId);
        Assert.Equal(expected.TenantId, actual.TenantId);
        Assert.Equal(expected.SessionId, actual.SessionId);
        Assert.Equal(expected.CorrelationId, actual.CorrelationId);
        Assert.Equal(expected.NetworkSummary, actual.NetworkSummary);
        Assert.Equal(expected.UserAgentSummary, actual.UserAgentSummary);
        Assert.Equal(expected.Metadata.Count, actual.Metadata.Count);
        Assert.Equal("aal2", actual.Metadata.Values["assurance_level"]);
        Assert.Equal("interactive", actual.Metadata.Values["flow"]);
    }

    [Fact]
    public async Task Notification_outbox_message_round_trips_protected_payload_and_retry_state()
    {
        var expected = PersistenceTestData.CreateOutboxMessage();
        expected.ScheduleRetry(
            expected.AvailableAt,
            expected.AvailableAt.AddMinutes(5),
            new NotificationDeliveryFailureCode("provider.timeout"));
        await using var provider = Database.CreateServiceProvider();

        await using (var writeScope = provider.CreateAsyncScope())
        {
            writeScope.ServiceProvider
                .GetRequiredService<INotificationOutboxRepository>()
                .Add(expected);
            Assert.Equal(
                1,
                await writeScope.ServiceProvider
                    .GetRequiredService<IAuthNexusUnitOfWork>()
                    .CommitAsync());
        }

        await using var readScope = provider.CreateAsyncScope();
        var persisted = await readScope.ServiceProvider
            .GetRequiredService<INotificationOutboxRepository>()
            .GetByIdAsync(expected.MessageId);

        Assert.NotNull(persisted);
        Assert.NotEqual(Guid.Empty, persisted.Version.Value);

        var actual = persisted.Entity;
        Assert.Equal(expected.MessageId, actual.MessageId);
        Assert.Equal(expected.CorrelationId, actual.CorrelationId);
        Assert.Equal(expected.TargetUserId, actual.TargetUserId);
        Assert.Equal(expected.ApplicationId, actual.ApplicationId);
        Assert.Equal(expected.TenantId, actual.TenantId);
        Assert.Equal(expected.NotificationType, actual.NotificationType);
        Assert.Equal(expected.Channel, actual.Channel);
        Assert.Equal(
            expected.Destination.RevealForDelivery(),
            actual.Destination.RevealForDelivery());
        Assert.Equal(
            expected.ProtectedPayload.CopyCiphertext(),
            actual.ProtectedPayload.CopyCiphertext());
        Assert.Equal(
            expected.ProtectedPayload.ProtectionKeyId,
            actual.ProtectedPayload.ProtectionKeyId);
        Assert.Equal(
            expected.ProtectedPayload.FormatVersion,
            actual.ProtectedPayload.FormatVersion);
        Assert.Equal(NotificationOutboxState.RetryScheduled, actual.State);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.AvailableAt, actual.AvailableAt);
        Assert.Equal(expected.StateChangedAt, actual.StateChangedAt);
        Assert.Equal(expected.AttemptCount, actual.AttemptCount);
        Assert.Equal(expected.LastAttemptedAt, actual.LastAttemptedAt);
        Assert.Equal(expected.NextAttemptAt, actual.NextAttemptAt);
        Assert.Null(actual.DeliveredAt);
        Assert.Null(actual.PermanentlyFailedAt);
        Assert.Equal(expected.LastFailureCode, actual.LastFailureCode);
    }
}
