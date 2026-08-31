using AuthNexus.Application.Persistence;
using AuthNexus.Domain;
using AuthNexus.Domain.Applications;
using AuthNexus.Domain.Identity;
using AuthNexus.Domain.Tenancy;
using AuthNexus.Modules.Notifications;

namespace AuthNexus.Infrastructure.Persistence.Notifications;

internal sealed class NotificationOutboxMessageRecord : IConcurrencyTrackedRecord
{
    public Guid MessageId { get; set; }

    public Guid CorrelationId { get; set; }

    public Guid? TargetUserId { get; set; }

    public Guid? ApplicationId { get; set; }

    public Guid? TenantId { get; set; }

    public string NotificationType { get; set; } = string.Empty;

    public int Channel { get; set; }

    public byte[] DestinationCiphertext { get; set; } = [];

    public string DestinationProtectionKeyId { get; set; } = string.Empty;

    public int DestinationFormatVersion { get; set; }

    public byte[] PayloadCiphertext { get; set; } = [];

    public string PayloadProtectionKeyId { get; set; } = string.Empty;

    public int PayloadFormatVersion { get; set; }

    public int State { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset AvailableAt { get; set; }

    public DateTimeOffset StateChangedAt { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? LastAttemptedAt { get; set; }

    public DateTimeOffset? NextAttemptAt { get; set; }

    public DateTimeOffset? DeliveredAt { get; set; }

    public DateTimeOffset? PermanentlyFailedAt { get; set; }

    public string? LastFailureCode { get; set; }

    public Guid Version { get; set; }

    public static NotificationOutboxMessageRecord FromDomain(
        NotificationOutboxMessage message,
        INotificationDestinationProtector destinationProtector,
        PersistenceVersion? persistenceVersion = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(destinationProtector);

        var protectedDestination = destinationProtector.Protect(
            message.MessageId,
            message.Destination);

        return new NotificationOutboxMessageRecord
        {
            MessageId = message.MessageId.Value,
            CorrelationId = message.CorrelationId.Value,
            TargetUserId = message.TargetUserId?.Value,
            ApplicationId = message.ApplicationId?.Value,
            TenantId = message.TenantId?.Value,
            NotificationType = message.NotificationType.Value,
            Channel = (int)message.Channel,
            DestinationCiphertext = protectedDestination.CopyCiphertext(),
            DestinationProtectionKeyId = protectedDestination.KeyId,
            DestinationFormatVersion = protectedDestination.FormatVersion,
            PayloadCiphertext = message.ProtectedPayload.CopyCiphertext(),
            PayloadProtectionKeyId = message.ProtectedPayload.ProtectionKeyId,
            PayloadFormatVersion = message.ProtectedPayload.FormatVersion,
            State = (int)message.State,
            CreatedAt = message.CreatedAt,
            AvailableAt = message.AvailableAt,
            StateChangedAt = message.StateChangedAt,
            AttemptCount = message.AttemptCount,
            LastAttemptedAt = message.LastAttemptedAt,
            NextAttemptAt = message.NextAttemptAt,
            DeliveredAt = message.DeliveredAt,
            PermanentlyFailedAt = message.PermanentlyFailedAt,
            LastFailureCode = message.LastFailureCode?.Value,
            Version = persistenceVersion?.Value ?? Guid.Empty,
        };
    }

    public Persisted<NotificationOutboxMessage> ToDomain(
        INotificationDestinationProtector destinationProtector)
    {
        ArgumentNullException.ThrowIfNull(destinationProtector);

        var destination = destinationProtector.Unprotect(
            new NotificationOutboxMessageId(MessageId),
            new ProtectedNotificationDestination(
                DestinationCiphertext,
                DestinationProtectionKeyId,
                DestinationFormatVersion));

        var message = NotificationOutboxMessage.Restore(
            new NotificationOutboxMessageId(MessageId),
            new CorrelationId(CorrelationId),
            TargetUserId is { } targetUserId ? new UserId(targetUserId) : null,
            ApplicationId is { } applicationId
                ? new AuthNexus.Domain.Applications.ApplicationId(applicationId)
                : null,
            TenantId is { } tenantId ? new TenantId(tenantId) : null,
            new NotificationType(NotificationType),
            (NotificationChannel)Channel,
            destination,
            ProtectedNotificationPayload.Create(
                PayloadCiphertext,
                PayloadProtectionKeyId,
                PayloadFormatVersion),
            CreatedAt,
            AvailableAt,
            (NotificationOutboxState)State,
            StateChangedAt,
            AttemptCount,
            LastAttemptedAt,
            NextAttemptAt,
            DeliveredAt,
            PermanentlyFailedAt,
            LastFailureCode is { } failureCode
                ? new NotificationDeliveryFailureCode(failureCode)
                : null);

        return new Persisted<NotificationOutboxMessage>(
            message,
            new PersistenceVersion(Version));
    }
}
