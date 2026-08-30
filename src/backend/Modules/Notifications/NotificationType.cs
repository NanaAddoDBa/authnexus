namespace AuthNexus.Modules.Notifications;

public readonly record struct NotificationType
{
    public const int MaximumLength = 100;

    private readonly string? _value;

    public NotificationType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A notification type is required.", nameof(value));
        }

        var normalizedValue = value.Trim().ToLowerInvariant();

        if (normalizedValue.Length > MaximumLength ||
            normalizedValue[0] is < 'a' or > 'z' ||
            normalizedValue.Any(character =>
                character is not (>= 'a' and <= 'z') and
                not (>= '0' and <= '9') and
                not '_' and
                not '-' and
                not '.'))
        {
            throw new ArgumentException(
                "A notification type must start with a lowercase ASCII letter and contain only lowercase letters, digits, dots, hyphens, or underscores.",
                nameof(value));
        }

        _value = normalizedValue;
    }

    public string Value => _value ?? string.Empty;

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public override string ToString() => Value;
}
