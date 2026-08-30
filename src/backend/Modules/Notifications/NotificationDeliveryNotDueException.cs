namespace AuthNexus.Modules.Notifications;

public sealed class NotificationDeliveryNotDueException : InvalidOperationException
{
    public NotificationDeliveryNotDueException(
        DateTimeOffset nextAttemptAt,
        DateTimeOffset attemptedAt)
        : base(
            $"The notification is next eligible at {nextAttemptAt:O} and cannot record an attempt at {attemptedAt:O}.")
    {
        NextAttemptAt = nextAttemptAt;
        AttemptedAt = attemptedAt;
    }

    public DateTimeOffset NextAttemptAt { get; }

    public DateTimeOffset AttemptedAt { get; }
}
