using AuthNexus.Domain;
using AuthNexus.Domain.Identity;
using AuthNexus.Domain.Tenancy;
using ApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Modules.Notifications;

public sealed class NotificationOutboxMessage
{
    private NotificationOutboxMessage(
        NotificationOutboxMessageId messageId,
        CorrelationId correlationId,
        UserId? targetUserId,
        ApplicationId? applicationId,
        TenantId? tenantId,
        NotificationType notificationType,
        NotificationChannel channel,
        NotificationDestination destination,
        ProtectedNotificationPayload protectedPayload,
        DateTimeOffset createdAt,
        DateTimeOffset availableAt)
    {
        MessageId = messageId;
        CorrelationId = correlationId;
        TargetUserId = targetUserId;
        ApplicationId = applicationId;
        TenantId = tenantId;
        NotificationType = notificationType;
        Channel = channel;
        Destination = destination;
        ProtectedPayload = protectedPayload;
        State = NotificationOutboxState.Pending;
        CreatedAt = createdAt;
        AvailableAt = availableAt;
        StateChangedAt = createdAt;
        NextAttemptAt = availableAt;
    }

    public NotificationOutboxMessageId MessageId { get; }

    public CorrelationId CorrelationId { get; }

    public UserId? TargetUserId { get; }

    public ApplicationId? ApplicationId { get; }

    public TenantId? TenantId { get; }

    public NotificationType NotificationType { get; }

    public NotificationChannel Channel { get; }

    public NotificationDestination Destination { get; }

    public ProtectedNotificationPayload ProtectedPayload { get; }

    public NotificationOutboxState State { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset AvailableAt { get; }

    public DateTimeOffset StateChangedAt { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset? LastAttemptedAt { get; private set; }

    public DateTimeOffset? NextAttemptAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public DateTimeOffset? PermanentlyFailedAt { get; private set; }

    public NotificationDeliveryFailureCode? LastFailureCode { get; private set; }

    public static NotificationOutboxMessage Create(
        NotificationOutboxMessageId messageId,
        CorrelationId correlationId,
        UserId? targetUserId,
        ApplicationId? applicationId,
        TenantId? tenantId,
        NotificationType notificationType,
        NotificationChannel channel,
        NotificationDestination destination,
        ProtectedNotificationPayload protectedPayload,
        DateTimeOffset createdAt,
        DateTimeOffset availableAt)
    {
        if (messageId.IsEmpty)
        {
            throw new ArgumentException(
                "A notification outbox message ID is required.",
                nameof(messageId));
        }

        if (correlationId.IsEmpty)
        {
            throw new ArgumentException("A correlation ID is required.", nameof(correlationId));
        }

        ValidateOptionalId(targetUserId, nameof(targetUserId));
        ValidateOptionalId(applicationId, nameof(applicationId));
        ValidateOptionalId(tenantId, nameof(tenantId));

        if (notificationType.IsEmpty)
        {
            throw new ArgumentException(
                "A notification type is required.",
                nameof(notificationType));
        }

        if (!Enum.IsDefined(channel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(channel),
                channel,
                "The notification channel is not defined.");
        }

        if (destination.IsEmpty)
        {
            throw new ArgumentException(
                "A notification destination is required.",
                nameof(destination));
        }

        ArgumentNullException.ThrowIfNull(protectedPayload);

        var normalizedCreatedAt = NormalizeTimestamp(createdAt, nameof(createdAt));
        var normalizedAvailableAt = NormalizeTimestamp(availableAt, nameof(availableAt));

        if (normalizedAvailableAt < normalizedCreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(availableAt),
                availableAt,
                "Notification availability cannot precede creation.");
        }

        return new NotificationOutboxMessage(
            messageId,
            correlationId,
            targetUserId,
            applicationId,
            tenantId,
            notificationType,
            channel,
            destination,
            protectedPayload,
            normalizedCreatedAt,
            normalizedAvailableAt);
    }

    public bool CanBeAttemptedAt(DateTimeOffset observedAt)
    {
        var normalizedObservedAt = NormalizeTimestamp(observedAt, nameof(observedAt));

        return State is NotificationOutboxState.Pending or NotificationOutboxState.RetryScheduled &&
               NextAttemptAt is { } nextAttemptAt &&
               normalizedObservedAt >= nextAttemptAt;
    }

    public void RecordDelivered(DateTimeOffset attemptedAt)
    {
        EnsureStateCanTransitionTo(
            NotificationOutboxState.Delivered,
            NotificationOutboxState.Pending,
            NotificationOutboxState.RetryScheduled);

        var normalizedAttemptedAt = NormalizeAttemptTimestamp(attemptedAt);
        EnsureAttemptIsDue(normalizedAttemptedAt);
        var nextAttemptCount = checked(AttemptCount + 1);

        State = NotificationOutboxState.Delivered;
        StateChangedAt = normalizedAttemptedAt;
        AttemptCount = nextAttemptCount;
        LastAttemptedAt = normalizedAttemptedAt;
        NextAttemptAt = null;
        DeliveredAt = normalizedAttemptedAt;
        LastFailureCode = null;
    }

    public void ScheduleRetry(
        DateTimeOffset attemptedAt,
        DateTimeOffset nextAttemptAt,
        NotificationDeliveryFailureCode failureCode)
    {
        EnsureStateCanTransitionTo(
            NotificationOutboxState.RetryScheduled,
            NotificationOutboxState.Pending,
            NotificationOutboxState.RetryScheduled);

        if (failureCode.IsEmpty)
        {
            throw new ArgumentException(
                "A delivery failure code is required.",
                nameof(failureCode));
        }

        var normalizedAttemptedAt = NormalizeAttemptTimestamp(attemptedAt);
        EnsureAttemptIsDue(normalizedAttemptedAt);
        var normalizedNextAttemptAt = NormalizeTimestamp(nextAttemptAt, nameof(nextAttemptAt));

        if (normalizedNextAttemptAt <= normalizedAttemptedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextAttemptAt),
                nextAttemptAt,
                "A retry must be scheduled after the failed attempt.");
        }

        var nextAttemptCount = checked(AttemptCount + 1);

        State = NotificationOutboxState.RetryScheduled;
        StateChangedAt = normalizedAttemptedAt;
        AttemptCount = nextAttemptCount;
        LastAttemptedAt = normalizedAttemptedAt;
        NextAttemptAt = normalizedNextAttemptAt;
        LastFailureCode = failureCode;
    }

    public void FailPermanently(
        DateTimeOffset attemptedAt,
        NotificationDeliveryFailureCode failureCode)
    {
        EnsureStateCanTransitionTo(
            NotificationOutboxState.PermanentlyFailed,
            NotificationOutboxState.Pending,
            NotificationOutboxState.RetryScheduled);

        if (failureCode.IsEmpty)
        {
            throw new ArgumentException(
                "A delivery failure code is required.",
                nameof(failureCode));
        }

        var normalizedAttemptedAt = NormalizeAttemptTimestamp(attemptedAt);
        EnsureAttemptIsDue(normalizedAttemptedAt);
        var nextAttemptCount = checked(AttemptCount + 1);

        State = NotificationOutboxState.PermanentlyFailed;
        StateChangedAt = normalizedAttemptedAt;
        AttemptCount = nextAttemptCount;
        LastAttemptedAt = normalizedAttemptedAt;
        NextAttemptAt = null;
        PermanentlyFailedAt = normalizedAttemptedAt;
        LastFailureCode = failureCode;
    }

    private void EnsureStateCanTransitionTo(
        NotificationOutboxState requestedState,
        params NotificationOutboxState[] allowedCurrentStates)
    {
        if (!allowedCurrentStates.Contains(State))
        {
            throw new InvalidNotificationOutboxStateTransitionException(State, requestedState);
        }
    }

    private DateTimeOffset NormalizeAttemptTimestamp(DateTimeOffset attemptedAt)
    {
        var normalizedAttemptedAt = NormalizeTimestamp(attemptedAt, nameof(attemptedAt));

        if (normalizedAttemptedAt < StateChangedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attemptedAt),
                attemptedAt,
                "A delivery result cannot precede the previous state change.");
        }

        return normalizedAttemptedAt;
    }

    private void EnsureAttemptIsDue(DateTimeOffset attemptedAt)
    {
        if (NextAttemptAt is not { } nextAttemptAt || attemptedAt < nextAttemptAt)
        {
            throw new NotificationDeliveryNotDueException(
                NextAttemptAt ?? StateChangedAt,
                attemptedAt);
        }
    }

    private static void ValidateOptionalId(UserId? value, string parameterName)
    {
        if (value is { IsEmpty: true })
        {
            throw new ArgumentException(
                "A supplied user ID cannot be empty.",
                parameterName);
        }
    }

    private static void ValidateOptionalId(ApplicationId? value, string parameterName)
    {
        if (value is { IsEmpty: true })
        {
            throw new ArgumentException(
                "A supplied application ID cannot be empty.",
                parameterName);
        }
    }

    private static void ValidateOptionalId(TenantId? value, string parameterName)
    {
        if (value is { IsEmpty: true })
        {
            throw new ArgumentException(
                "A supplied tenant ID cannot be empty.",
                parameterName);
        }
    }

    private static DateTimeOffset NormalizeTimestamp(
        DateTimeOffset timestamp,
        string parameterName)
    {
        if (timestamp == default)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                timestamp,
                "A non-default timestamp is required.");
        }

        return timestamp.ToUniversalTime();
    }
}
