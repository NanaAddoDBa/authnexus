namespace AuthNexus.Modules.Sessions;

public readonly record struct SessionSecretHash
{
    public const int EncodedLength = 43;

    private const string CanonicalFinalCharacters = "AEIMQUYcgkosw048";

    private readonly string? _encodedValue;

    public SessionSecretHash(string encodedValue)
    {
        if (string.IsNullOrWhiteSpace(encodedValue))
        {
            throw new ArgumentException(
                "A session secret hash is required.",
                nameof(encodedValue));
        }

        var normalizedValue = encodedValue.Trim();

        if (normalizedValue.Length != EncodedLength ||
            normalizedValue.Any(character => !IsBase64UrlCharacter(character)) ||
            !CanonicalFinalCharacters.Contains(normalizedValue[^1]))
        {
            throw new ArgumentException(
                "A session secret hash must be an unpadded base64url-encoded 32-byte digest.",
                nameof(encodedValue));
        }

        _encodedValue = normalizedValue;
    }

    public string EncodedValue => _encodedValue ?? string.Empty;

    public bool IsEmpty => string.IsNullOrEmpty(_encodedValue);

    public override string ToString() => "[session-secret-hash]";

    private static bool IsBase64UrlCharacter(char character) =>
        character is >= 'A' and <= 'Z' or
        >= 'a' and <= 'z' or
        >= '0' and <= '9' or
        '-' or '_';
}
