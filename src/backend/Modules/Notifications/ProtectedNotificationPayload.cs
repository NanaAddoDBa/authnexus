namespace AuthNexus.Modules.Notifications;

public sealed class ProtectedNotificationPayload
{
    public const int MaximumCiphertextLength = 65_536;
    public const int MaximumProtectionKeyIdLength = 128;

    private readonly byte[] _ciphertext;

    private ProtectedNotificationPayload(
        byte[] ciphertext,
        string protectionKeyId,
        int formatVersion)
    {
        _ciphertext = ciphertext;
        ProtectionKeyId = protectionKeyId;
        FormatVersion = formatVersion;
    }

    public int CiphertextLength => _ciphertext.Length;

    public string ProtectionKeyId { get; }

    public int FormatVersion { get; }

    public static ProtectedNotificationPayload Create(
        ReadOnlySpan<byte> ciphertext,
        string protectionKeyId,
        int formatVersion)
    {
        if (ciphertext.IsEmpty)
        {
            throw new ArgumentException(
                "A protected notification payload is required.",
                nameof(ciphertext));
        }

        if (ciphertext.Length > MaximumCiphertextLength)
        {
            throw new ArgumentException(
                $"A protected notification payload cannot exceed {MaximumCiphertextLength} bytes.",
                nameof(ciphertext));
        }

        if (string.IsNullOrWhiteSpace(protectionKeyId))
        {
            throw new ArgumentException(
                "A payload protection key ID is required.",
                nameof(protectionKeyId));
        }

        var normalizedProtectionKeyId = protectionKeyId.Trim();

        if (normalizedProtectionKeyId.Length > MaximumProtectionKeyIdLength ||
            normalizedProtectionKeyId.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-' and not '.' and not ':'))
        {
            throw new ArgumentException(
                "A payload protection key ID contains invalid characters or exceeds its length limit.",
                nameof(protectionKeyId));
        }

        if (formatVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(formatVersion),
                formatVersion,
                "A positive payload format version is required.");
        }

        return new ProtectedNotificationPayload(
            ciphertext.ToArray(),
            normalizedProtectionKeyId,
            formatVersion);
    }

    public byte[] CopyCiphertext() => _ciphertext.ToArray();

    public override string ToString() => $"[protected-notification-payload:{CiphertextLength}]";
}
