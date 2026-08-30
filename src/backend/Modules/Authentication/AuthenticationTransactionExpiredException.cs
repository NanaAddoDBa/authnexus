namespace AuthNexus.Modules.Authentication;

public sealed class AuthenticationTransactionExpiredException : InvalidOperationException
{
    public AuthenticationTransactionExpiredException(
        DateTimeOffset expiresAt,
        DateTimeOffset attemptedAt)
        : base(
            "The authentication transaction expired at " +
            $"{expiresAt:O}; the operation was attempted at {attemptedAt:O}.")
    {
        ExpiresAt = expiresAt;
        AttemptedAt = attemptedAt;
    }

    public DateTimeOffset ExpiresAt { get; }

    public DateTimeOffset AttemptedAt { get; }
}
