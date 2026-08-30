namespace AuthNexus.Modules.Sessions;

public sealed class SessionExpiredException : InvalidOperationException
{
    public SessionExpiredException(
        DateTimeOffset effectiveExpiresAt,
        DateTimeOffset attemptedAt)
        : base(
            $"The session expired at {effectiveExpiresAt:O} and cannot be used at {attemptedAt:O}.")
    {
        EffectiveExpiresAt = effectiveExpiresAt;
        AttemptedAt = attemptedAt;
    }

    public DateTimeOffset EffectiveExpiresAt { get; }

    public DateTimeOffset AttemptedAt { get; }
}
