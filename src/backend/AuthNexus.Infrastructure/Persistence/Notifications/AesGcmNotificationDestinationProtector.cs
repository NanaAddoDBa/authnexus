using System.Security.Cryptography;
using System.Text;
using AuthNexus.Modules.Notifications;

namespace AuthNexus.Infrastructure.Persistence.Notifications;

internal sealed class AesGcmNotificationDestinationProtector :
    INotificationDestinationProtector,
    IDisposable
{
    private const int KeyLength = 32;
    private const int NonceLength = 12;
    private const int TagLength = 16;

    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly Dictionary<string, byte[]> _keys;
    private bool _disposed;

    public AesGcmNotificationDestinationProtector(
        NotificationDestinationProtectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.CurrentKeyId))
        {
            throw new ArgumentException(
                "A current destination protection key ID is required.",
                nameof(options));
        }

        CurrentKeyId = options.CurrentKeyId.Trim();
        _keys = DecodeKeys(options.Keys);

        if (!_keys.ContainsKey(CurrentKeyId))
        {
            Dispose();
            throw new ArgumentException(
                "The current destination protection key is not present in the key ring.",
                nameof(options));
        }
    }

    private string CurrentKeyId { get; }

    public ProtectedNotificationDestination Protect(
        NotificationOutboxMessageId messageId,
        NotificationDestination destination)
    {
        ThrowIfDisposed();

        if (messageId.IsEmpty)
        {
            throw new ArgumentException(
                "A notification message ID is required.",
                nameof(messageId));
        }

        if (destination.IsEmpty)
        {
            throw new ArgumentException("A notification destination is required.", nameof(destination));
        }

        var plaintext = StrictUtf8.GetBytes(destination.RevealForDelivery());
        var nonce = RandomNumberGenerator.GetBytes(NonceLength);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagLength];
        var associatedData = CreateAssociatedData(CurrentKeyId, messageId);

        try
        {
            using var aes = new AesGcm(_keys[CurrentKeyId], TagLength);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

            var envelope = new byte[NonceLength + TagLength + ciphertext.Length];
            nonce.CopyTo(envelope, 0);
            tag.CopyTo(envelope, NonceLength);
            ciphertext.CopyTo(envelope, NonceLength + TagLength);

            return new ProtectedNotificationDestination(
                envelope,
                CurrentKeyId,
                ProtectedNotificationDestination.CurrentFormatVersion);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(ciphertext);
            CryptographicOperations.ZeroMemory(tag);
            CryptographicOperations.ZeroMemory(associatedData);
        }
    }

    public NotificationDestination Unprotect(
        NotificationOutboxMessageId messageId,
        ProtectedNotificationDestination protectedDestination)
    {
        ThrowIfDisposed();

        if (messageId.IsEmpty)
        {
            throw new ArgumentException(
                "A notification message ID is required.",
                nameof(messageId));
        }

        ArgumentNullException.ThrowIfNull(protectedDestination);

        if (!_keys.TryGetValue(protectedDestination.KeyId, out var key))
        {
            throw new CryptographicException(
                "The notification destination cannot be opened with the configured key ring.");
        }

        var envelope = protectedDestination.CopyCiphertext();

        if (envelope.Length <= NonceLength + TagLength)
        {
            CryptographicOperations.ZeroMemory(envelope);
            throw new CryptographicException("The protected notification destination is invalid.");
        }

        var plaintext = new byte[envelope.Length - NonceLength - TagLength];
        var associatedData = CreateAssociatedData(protectedDestination.KeyId, messageId);

        try
        {
            using var aes = new AesGcm(key, TagLength);
            aes.Decrypt(
                envelope.AsSpan(0, NonceLength),
                envelope.AsSpan(NonceLength + TagLength),
                envelope.AsSpan(NonceLength, TagLength),
                plaintext,
                associatedData);

            return new NotificationDestination(StrictUtf8.GetString(plaintext));
        }
        catch (Exception exception) when (
            exception is CryptographicException or DecoderFallbackException or ArgumentException)
        {
            throw new CryptographicException(
                "The protected notification destination could not be opened.",
                exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(envelope);
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(associatedData);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var key in _keys.Values)
        {
            CryptographicOperations.ZeroMemory(key);
        }

        _keys.Clear();
        _disposed = true;
    }

    private static Dictionary<string, byte[]> DecodeKeys(IReadOnlyDictionary<string, string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var decodedKeys = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        try
        {
            foreach (var (rawKeyId, encodedKey) in keys)
            {
                var keyId = ProtectedNotificationDestination.NormalizeKeyId(
                    rawKeyId,
                    nameof(keys));

                byte[] key;

                try
                {
                    key = Convert.FromBase64String(encodedKey);
                }
                catch (FormatException exception)
                {
                    throw new ArgumentException(
                        "A destination protection key is not valid base64.",
                        nameof(keys),
                        exception);
                }

                if (key.Length != KeyLength)
                {
                    CryptographicOperations.ZeroMemory(key);
                    throw new ArgumentException(
                        "Every destination protection key must contain exactly 32 bytes.",
                        nameof(keys));
                }

                if (!decodedKeys.TryAdd(keyId, key))
                {
                    CryptographicOperations.ZeroMemory(key);
                    throw new ArgumentException(
                        "Destination protection key IDs must be unique.",
                        nameof(keys));
                }
            }

            return decodedKeys;
        }
        catch
        {
            foreach (var key in decodedKeys.Values)
            {
                CryptographicOperations.ZeroMemory(key);
            }

            throw;
        }
    }

    private static byte[] CreateAssociatedData(
        string keyId,
        NotificationOutboxMessageId messageId) =>
        StrictUtf8.GetBytes(
            $"AuthNexus:NotificationDestination:v1:{keyId}:{messageId.Value:D}");

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
