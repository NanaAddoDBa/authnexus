using System.Globalization;
using System.Text;

namespace AuthNexus.Modules.Notifications;

public readonly record struct NotificationDestination
{
    public const int MaximumLength = 512;

    private readonly string? _value;

    public NotificationDestination(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A notification destination is required.",
                nameof(value));
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > MaximumLength ||
            HasUnpairedSurrogate(normalizedValue) ||
            normalizedValue.EnumerateRunes().Any(rune =>
                Rune.IsControl(rune) ||
                Rune.IsWhiteSpace(rune) ||
                Rune.GetUnicodeCategory(rune) is UnicodeCategory.Format))
        {
            throw new ArgumentException(
                $"A notification destination must be at most {MaximumLength} characters and contain no whitespace, control, or formatting characters.",
                nameof(value));
        }

        _value = normalizedValue;
    }

    public bool IsEmpty => string.IsNullOrEmpty(_value);

    public string RevealForDelivery() => _value ?? string.Empty;

    public override string ToString() => "[notification-destination]";

    private static bool HasUnpairedSurrogate(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (char.IsHighSurrogate(value[index]))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return true;
                }

                index++;
                continue;
            }

            if (char.IsLowSurrogate(value[index]))
            {
                return true;
            }
        }

        return false;
    }
}
