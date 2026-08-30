namespace AuthNexus.Modules.Notifications;

public readonly record struct NotificationOutboxMessageId
{
    public NotificationOutboxMessageId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A notification outbox message ID cannot be empty.",
                nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString("D");
}
