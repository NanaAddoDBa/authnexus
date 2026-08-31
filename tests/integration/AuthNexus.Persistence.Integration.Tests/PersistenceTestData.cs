using AuthNexus.Domain;
using AuthNexus.Domain.Authentication;
using AuthNexus.Domain.Identity;
using AuthNexus.Domain.Sessions;
using AuthNexus.Domain.Tenancy;
using AuthNexus.Modules.Applications;
using AuthNexus.Modules.Audit;
using AuthNexus.Modules.Authentication;
using AuthNexus.Modules.Identity;
using AuthNexus.Modules.Notifications;
using AuthNexus.Modules.Sessions;
using System.Security.Cryptography;
using DomainApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Persistence.Integration.Tests;

internal static class PersistenceTestData
{
    internal static readonly DateTimeOffset BaseTime = new(
        2026,
        8,
        30,
        10,
        0,
        0,
        TimeSpan.Zero);

    internal static ApplicationProfile CreateApplicationProfile(
        DomainApplicationId? applicationId = null) =>
        ApplicationProfile.Create(
            applicationId ?? NewApplicationId(),
            NewTenantId(),
            ApplicationType.Web,
            ApplicationAudience.Consumer,
            ApplicationMode.SignInOrRegister,
            "Customer Portal",
            "en-US",
            "consumer-default",
            "consumer-registration",
            [
                RedirectUri.Create("https://accounts.example.com/auth/callback"),
                RedirectUri.Create("https://accounts.example.com/auth/complete"),
            ]);

    internal static UserAccount CreateUserAccount(UserId? userId = null) =>
        UserAccount.Create(userId ?? NewUserId(), BaseTime);

    internal static AuthenticationTransaction CreateAuthenticationTransaction(
        AuthenticationTransactionId? transactionId = null) =>
        AuthenticationTransaction.Create(
            transactionId ?? NewAuthenticationTransactionId(),
            NewApplicationId(),
            NewTenantId(),
            NewUserId(),
            AuthenticationTransactionPurpose.SignIn,
            NewCorrelationId(),
            BaseTime,
            BaseTime.AddMinutes(15));

    internal static Session CreateSession(
        SessionId? sessionId = null,
        SessionSecretHash? secretHash = null) =>
        Session.Create(
            sessionId ?? NewSessionId(),
            secretHash ?? NewSessionSecretHash(),
            NewUserId(),
            NewApplicationId(),
            NewTenantId(),
            BaseTime.AddMinutes(-1),
            BaseTime,
            BaseTime.AddMinutes(30),
            BaseTime.AddHours(8));

    internal static SecurityEvent CreateSecurityEvent(SecurityEventId? eventId = null) =>
        SecurityEvent.Create(
            eventId ?? NewSecurityEventId(),
            BaseTime,
            SecurityEventType.LoginSucceeded,
            SecurityEventResult.Succeeded,
            NewUserId(),
            NewUserId(),
            NewApplicationId(),
            NewTenantId(),
            NewSessionId(),
            NewCorrelationId(),
            "203.0.113.0/24",
            "AuthNexus integration client",
            SecurityEventMetadata.Create(
            [
                new KeyValuePair<string, string>("assurance_level", "aal2"),
                new KeyValuePair<string, string>("flow", "interactive"),
            ]));

    internal static NotificationOutboxMessage CreateOutboxMessage(
        DateTimeOffset? createdAt = null,
        DateTimeOffset? availableAt = null,
        NotificationOutboxMessageId? messageId = null)
    {
        var created = createdAt ?? BaseTime;

        return NotificationOutboxMessage.Create(
            messageId ?? NewOutboxMessageId(),
            NewCorrelationId(),
            NewUserId(),
            NewApplicationId(),
            NewTenantId(),
            new NotificationType("security.login_alert"),
            NotificationChannel.Email,
            new NotificationDestination($"user-{Guid.NewGuid():N}@example.com"),
            ProtectedNotificationPayload.Create(
                [0x10, 0x20, 0x30, 0x40],
                "test-payload:v1",
                1),
            created,
            availableAt ?? created);
    }

    internal static DomainApplicationId NewApplicationId() => new(Guid.NewGuid());

    internal static TenantId NewTenantId() => new(Guid.NewGuid());

    internal static UserId NewUserId() => new(Guid.NewGuid());

    internal static AuthenticationTransactionId NewAuthenticationTransactionId() =>
        new(Guid.NewGuid());

    internal static SessionId NewSessionId() => new(Guid.NewGuid());

    internal static CorrelationId NewCorrelationId() => new(Guid.NewGuid());

    internal static SecurityEventId NewSecurityEventId() => new(Guid.NewGuid());

    internal static NotificationOutboxMessageId NewOutboxMessageId() => new(Guid.NewGuid());

    internal static SessionSecretHash NewSessionSecretHash() =>
        new(ToBase64Url(SHA256.HashData(Guid.NewGuid().ToByteArray())));

    private static string ToBase64Url(ReadOnlySpan<byte> value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
