namespace AuthNexus.Infrastructure.Persistence.Notifications;

public sealed class NotificationDestinationProtectionOptions
{
    public string CurrentKeyId { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Keys { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}
