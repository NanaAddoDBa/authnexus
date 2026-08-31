using AuthNexus.Modules.Notifications;

namespace AuthNexus.Infrastructure.Persistence.Notifications;

internal sealed class ProtectedNotificationDestination
{
    public const int CurrentFormatVersion = 1;
    public const int EncryptionEnvelopeLength = 28;
    public const int MinimumCiphertextLength = EncryptionEnvelopeLength + 1;
    public const int MaximumCiphertextLength =
        (NotificationDestination.MaximumLength * 3) + EncryptionEnvelopeLength;
    public const int MaximumKeyIdLength = 128;

    private readonly byte[] _ciphertext;

    public ProtectedNotificationDestination(
        ReadOnlySpan<byte> ciphertext,
        string keyId,
        int formatVersion)
    {
        if (ciphertext.Length < MinimumCiphertextLength ||
            ciphertext.Length > MaximumCiphertextLength)
        {
            throw new ArgumentException(
                $"Protected destination ciphertext must contain between {MinimumCiphertextLength} and {MaximumCiphertextLength} bytes.",
                nameof(ciphertext));
        }

        if (formatVersion != CurrentFormatVersion)
        {
            throw new ArgumentOutOfRangeException(
                nameof(formatVersion),
                formatVersion,
                "The destination protection format version is not supported.");
        }

        _ciphertext = ciphertext.ToArray();
        KeyId = NormalizeKeyId(keyId, nameof(keyId));
        FormatVersion = formatVersion;
    }

    public string KeyId { get; }

    public int FormatVersion { get; }

    public byte[] CopyCiphertext() => _ciphertext.ToArray();

    public override string ToString() => $"[protected-notification-destination:{_ciphertext.Length}]";

    internal static string NormalizeKeyId(string keyId, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(keyId))
        {
            throw new ArgumentException(
                "A destination protection key ID is required.",
                parameterName);
        }

        var normalizedKeyId = keyId.Trim();

        if (normalizedKeyId.Length > MaximumKeyIdLength ||
            normalizedKeyId.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '_' and not '-' and not '.' and not ':'))
        {
            throw new ArgumentException(
                "The destination protection key ID contains unsupported characters or is too long.",
                parameterName);
        }

        return normalizedKeyId;
    }
}
