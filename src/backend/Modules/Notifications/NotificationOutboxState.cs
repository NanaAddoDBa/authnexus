namespace AuthNexus.Modules.Notifications;

public enum NotificationOutboxState
{
    Pending = 1,
    RetryScheduled = 2,
    Delivered = 3,
    PermanentlyFailed = 4,
}
