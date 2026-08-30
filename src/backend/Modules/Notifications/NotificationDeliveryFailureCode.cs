namespace AuthNexus.Modules.Notifications;

public readonly record struct NotificationDeliveryFailureCode
{
    public const int MaximumLength = 64;

    private readonly string? _value;

    public NotificationDeliveryFailureCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A notification delivery failure code is required.",
                nameof(value));
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
                "A failure code must start with a lowercase ASCII letter and contain only lowercase letters, digits, dots, hyphens, or underscores.",
                nameof(value));
        }

        _value = normalizedValue;
    }

    public string Value => _value ?? string.Empty;

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public override string ToString() => Value;
}
