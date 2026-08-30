using System.Collections.ObjectModel;
using System.Globalization;

namespace AuthNexus.Modules.Audit;

public sealed class SecurityEventMetadata
{
    public const int MaximumEntryCount = 32;
    public const int MaximumKeyLength = 64;
    public const int MaximumValueLength = 512;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.Ordinal)
    {
        "password",
        "raw_password",
        "otp",
        "otp_value",
        "reset_token",
        "session_secret",
        "oauth_authorization_code",
        "authorization_code",
        "refresh_token",
        "totp_seed",
        "recovery_code",
    };

    private readonly ReadOnlyDictionary<string, string> _values;

    private SecurityEventMetadata(Dictionary<string, string> values)
    {
        _values = new ReadOnlyDictionary<string, string>(values);
    }

    public int Count => _values.Count;

    public IReadOnlyDictionary<string, string> Values => _values;

    public static SecurityEventMetadata Create(
        IEnumerable<KeyValuePair<string, string>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (key, value) in entries)
        {
            if (values.Count == MaximumEntryCount)
            {
                throw new ArgumentException(
                    $"Security event metadata cannot contain more than {MaximumEntryCount} entries.",
                    nameof(entries));
            }

            var normalizedKey = NormalizeKey(key, nameof(entries));
            var normalizedValue = NormalizeValue(value, nameof(entries));

            if (ContainsSensitiveKeyMaterial(normalizedKey))
            {
                throw new ArgumentException(
                    "Security event metadata contains a prohibited sensitive key.",
                    nameof(entries));
            }

            if (!values.TryAdd(normalizedKey, normalizedValue))
            {
                throw new ArgumentException(
                    "Security event metadata contains a duplicate normalized key.",
                    nameof(entries));
            }
        }

        return new SecurityEventMetadata(values);
    }

    public override string ToString() => $"[security-event-metadata:{Count}]";

    private static string NormalizeKey(string key, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                "Security event metadata keys are required.",
                parameterName);
        }

        var normalizedKey = key.Trim().ToLowerInvariant();

        if (normalizedKey.Length > MaximumKeyLength ||
            normalizedKey[0] is < 'a' or > 'z' ||
            normalizedKey.Any(character =>
                character is not (>= 'a' and <= 'z') and
                not (>= '0' and <= '9') and
                not '_' and
                not '-' and
                not '.'))
        {
            throw new ArgumentException(
                "Security event metadata keys must use lowercase ASCII letters, digits, dots, hyphens, or underscores and start with a letter.",
                parameterName);
        }

        return normalizedKey;
    }

    private static string NormalizeValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Security event metadata values are required.",
                parameterName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > MaximumValueLength ||
            normalizedValue.Any(IsUnsafeTextCharacter))
        {
            throw new ArgumentException(
                $"Security event metadata values must be at most {MaximumValueLength} characters and contain no control characters.",
                parameterName);
        }

        return normalizedValue;
    }

    private static bool ContainsSensitiveKeyMaterial(string key)
    {
        if (SensitiveKeys.Contains(key))
        {
            return true;
        }

        var segments = key.Split(['.', '_', '-'], StringSplitOptions.RemoveEmptyEntries);

        if (segments.Any(segment =>
                segment is "password" or "otp" or "secret" or "token" or "seed"))
        {
            return true;
        }

        var segmentSet = segments.ToHashSet(StringComparer.Ordinal);

        return segmentSet.Contains("recovery") && segmentSet.Contains("code") ||
               segmentSet.Contains("authorization") && segmentSet.Contains("code");
    }

    private static bool IsUnsafeTextCharacter(char character)
    {
        var category = char.GetUnicodeCategory(character);

        return category is UnicodeCategory.Control or
            UnicodeCategory.LineSeparator or
            UnicodeCategory.ParagraphSeparator or
            UnicodeCategory.Format;
    }
}
