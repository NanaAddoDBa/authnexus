using AuthNexus.Domain.Identity;
using AuthNexus.Domain.Sessions;
using AuthNexus.Domain.Tenancy;
using ApplicationId = AuthNexus.Domain.Applications.ApplicationId;

namespace AuthNexus.Modules.Sessions;

public sealed class Session
{
    private Session(
        SessionId sessionId,
        SessionSecretHash secretHash,
        UserId userId,
        ApplicationId applicationId,
        TenantId? tenantId,
        DateTimeOffset authenticatedAt,
        DateTimeOffset createdAt,
        DateTimeOffset idleExpiresAt,
        DateTimeOffset absoluteExpiresAt)
    {
        SessionId = sessionId;
        SecretHash = secretHash;
        UserId = userId;
        ApplicationId = applicationId;
        TenantId = tenantId;
        State = SessionState.Active;
        AuthenticatedAt = authenticatedAt;
        CreatedAt = createdAt;
        LastSeenAt = createdAt;
        IdleExpiresAt = idleExpiresAt;
        AbsoluteExpiresAt = absoluteExpiresAt;
        UpdatedAt = createdAt;
        StateChangedAt = createdAt;
        SecretRotatedAt = createdAt;
    }

    public SessionId SessionId { get; }

    public SessionSecretHash SecretHash { get; private set; }

    public UserId UserId { get; }

    public ApplicationId ApplicationId { get; }

    public TenantId? TenantId { get; }

    public SessionState State { get; private set; }

    public DateTimeOffset AuthenticatedAt { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public DateTimeOffset IdleExpiresAt { get; private set; }

    public DateTimeOffset AbsoluteExpiresAt { get; }

    public DateTimeOffset EffectiveExpiresAt =>
        IdleExpiresAt <= AbsoluteExpiresAt ? IdleExpiresAt : AbsoluteExpiresAt;

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset StateChangedAt { get; private set; }

    public DateTimeOffset SecretRotatedAt { get; private set; }

    public int RotationCount { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public SessionRevocationReason? RevocationReason { get; private set; }

    public DateTimeOffset? ExpiredAt { get; private set; }

    public static Session Create(
        SessionId sessionId,
        SessionSecretHash secretHash,
        UserId userId,
        ApplicationId applicationId,
        TenantId? tenantId,
        DateTimeOffset authenticatedAt,
        DateTimeOffset createdAt,
        DateTimeOffset idleExpiresAt,
        DateTimeOffset absoluteExpiresAt)
    {
        if (sessionId.IsEmpty)
        {
            throw new ArgumentException("A session ID is required.", nameof(sessionId));
        }

        if (secretHash.IsEmpty)
        {
            throw new ArgumentException(
                "A session secret hash is required.",
                nameof(secretHash));
        }

        if (userId.IsEmpty)
        {
            throw new ArgumentException("A user ID is required.", nameof(userId));
        }

        if (applicationId.IsEmpty)
        {
            throw new ArgumentException("An application ID is required.", nameof(applicationId));
        }

        if (tenantId is { IsEmpty: true })
        {
            throw new ArgumentException(
                "A supplied tenant ID cannot be empty.",
                nameof(tenantId));
        }

        var normalizedAuthenticatedAt = NormalizeTimestamp(
            authenticatedAt,
            nameof(authenticatedAt));
        var normalizedCreatedAt = NormalizeTimestamp(createdAt, nameof(createdAt));
        var normalizedIdleExpiresAt = NormalizeTimestamp(idleExpiresAt, nameof(idleExpiresAt));
        var normalizedAbsoluteExpiresAt = NormalizeTimestamp(
            absoluteExpiresAt,
            nameof(absoluteExpiresAt));

        if (normalizedAuthenticatedAt > normalizedCreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(authenticatedAt),
                authenticatedAt,
                "Authentication cannot occur after the session is created.");
        }

        if (normalizedIdleExpiresAt <= normalizedCreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idleExpiresAt),
                idleExpiresAt,
                "Idle expiry must be later than session creation.");
        }

        if (normalizedAbsoluteExpiresAt <= normalizedCreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(absoluteExpiresAt),
                absoluteExpiresAt,
                "Absolute expiry must be later than session creation.");
        }

        if (normalizedIdleExpiresAt > normalizedAbsoluteExpiresAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idleExpiresAt),
                idleExpiresAt,
                "Idle expiry cannot be later than absolute expiry.");
        }

        return new Session(
            sessionId,
            secretHash,
            userId,
            applicationId,
            tenantId,
            normalizedAuthenticatedAt,
            normalizedCreatedAt,
            normalizedIdleExpiresAt,
            normalizedAbsoluteExpiresAt);
    }

    internal static Session Rehydrate(
        SessionId sessionId,
        SessionSecretHash secretHash,
        UserId userId,
        ApplicationId applicationId,
        TenantId? tenantId,
        SessionState state,
        DateTimeOffset authenticatedAt,
        DateTimeOffset createdAt,
        DateTimeOffset lastSeenAt,
        DateTimeOffset idleExpiresAt,
        DateTimeOffset absoluteExpiresAt,
        DateTimeOffset updatedAt,
        DateTimeOffset stateChangedAt,
        DateTimeOffset secretRotatedAt,
        int rotationCount,
        DateTimeOffset? revokedAt,
        SessionRevocationReason? revocationReason,
        DateTimeOffset? expiredAt)
    {
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state,
                "The session state is not defined.");
        }

        if (revocationReason is not null && !Enum.IsDefined(revocationReason.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(revocationReason),
                revocationReason,
                "The session revocation reason is not defined.");
        }

        if (rotationCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotationCount),
                rotationCount,
                "The session rotation count cannot be negative.");
        }

        var session = Create(
            sessionId,
            secretHash,
            userId,
            applicationId,
            tenantId,
            authenticatedAt,
            createdAt,
            idleExpiresAt,
            absoluteExpiresAt);
        var normalizedLastSeenAt = NormalizeTimestamp(lastSeenAt, nameof(lastSeenAt));
        var normalizedUpdatedAt = NormalizeTimestamp(updatedAt, nameof(updatedAt));
        var normalizedStateChangedAt = NormalizeTimestamp(
            stateChangedAt,
            nameof(stateChangedAt));
        var normalizedSecretRotatedAt = NormalizeTimestamp(
            secretRotatedAt,
            nameof(secretRotatedAt));
        DateTimeOffset? normalizedRevokedAt = revokedAt is null
            ? null
            : NormalizeTimestamp(revokedAt.Value, nameof(revokedAt));
        DateTimeOffset? normalizedExpiredAt = expiredAt is null
            ? null
            : NormalizeTimestamp(expiredAt.Value, nameof(expiredAt));

        if (normalizedLastSeenAt < session.CreatedAt ||
            normalizedLastSeenAt > normalizedUpdatedAt ||
            normalizedLastSeenAt >= session.IdleExpiresAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lastSeenAt),
                lastSeenAt,
                "The last-seen time is outside the stored session lifetime.");
        }

        if (normalizedUpdatedAt < session.CreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(updatedAt),
                updatedAt,
                "The session update time cannot precede creation.");
        }

        if (normalizedStateChangedAt < session.CreatedAt ||
            normalizedStateChangedAt > normalizedUpdatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stateChangedAt),
                stateChangedAt,
                "The session state-change time is inconsistent with its update time.");
        }

        if (normalizedSecretRotatedAt < session.CreatedAt ||
            normalizedSecretRotatedAt > normalizedUpdatedAt ||
            normalizedSecretRotatedAt >= session.IdleExpiresAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(secretRotatedAt),
                secretRotatedAt,
                "The secret-rotation time is outside the stored session lifetime.");
        }

        var hasValidStateDetails = state switch
        {
            SessionState.Active =>
                normalizedStateChangedAt == session.CreatedAt &&
                normalizedRevokedAt is null &&
                revocationReason is null &&
                normalizedExpiredAt is null,
            SessionState.Revoked =>
                normalizedRevokedAt == normalizedStateChangedAt &&
                normalizedStateChangedAt == normalizedUpdatedAt &&
                revocationReason is not null &&
                normalizedExpiredAt is null,
            SessionState.Expired =>
                normalizedExpiredAt == normalizedStateChangedAt &&
                normalizedStateChangedAt == normalizedUpdatedAt &&
                normalizedExpiredAt >= session.EffectiveExpiresAt &&
                normalizedRevokedAt is null &&
                revocationReason is null,
            _ => false,
        };

        if (!hasValidStateDetails)
        {
            throw new ArgumentException(
                "The stored session terminal details do not match its state.");
        }

        session.State = state;
        session.LastSeenAt = normalizedLastSeenAt;
        session.UpdatedAt = normalizedUpdatedAt;
        session.StateChangedAt = normalizedStateChangedAt;
        session.SecretRotatedAt = normalizedSecretRotatedAt;
        session.RotationCount = rotationCount;
        session.RevokedAt = normalizedRevokedAt;
        session.RevocationReason = revocationReason;
        session.ExpiredAt = normalizedExpiredAt;

        return session;
    }

    public bool CanBeUsedAt(DateTimeOffset observedAt)
    {
        var normalizedObservedAt = NormalizeTimestamp(observedAt, nameof(observedAt));

        return State == SessionState.Active &&
               normalizedObservedAt >= CreatedAt &&
               normalizedObservedAt < EffectiveExpiresAt;
    }

    public void RecordActivity(
        DateTimeOffset occurredAt,
        DateTimeOffset nextIdleExpiresAt)
    {
        EnsureStateCanTransitionTo(SessionState.Active, SessionState.Active);

        var normalizedOccurredAt = NormalizeOperationTimestamp(occurredAt);
        EnsureWithinUsableLifetime(normalizedOccurredAt);

        var normalizedNextIdleExpiresAt = NormalizeTimestamp(
            nextIdleExpiresAt,
            nameof(nextIdleExpiresAt));

        if (normalizedNextIdleExpiresAt <= normalizedOccurredAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextIdleExpiresAt),
                nextIdleExpiresAt,
                "The next idle expiry must be later than the activity time.");
        }

        if (normalizedNextIdleExpiresAt < IdleExpiresAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextIdleExpiresAt),
                nextIdleExpiresAt,
                "Activity cannot shorten the current idle lifetime.");
        }

        if (normalizedNextIdleExpiresAt > AbsoluteExpiresAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextIdleExpiresAt),
                nextIdleExpiresAt,
                "Idle expiry cannot extend beyond absolute expiry.");
        }

        LastSeenAt = normalizedOccurredAt;
        IdleExpiresAt = normalizedNextIdleExpiresAt;
        UpdatedAt = normalizedOccurredAt;
    }

    public void RotateSecretHash(
        SessionSecretHash replacementHash,
        DateTimeOffset occurredAt)
    {
        EnsureStateCanTransitionTo(SessionState.Active, SessionState.Active);

        if (replacementHash.IsEmpty)
        {
            throw new ArgumentException(
                "A replacement session secret hash is required.",
                nameof(replacementHash));
        }

        if (replacementHash == SecretHash)
        {
            throw new ArgumentException(
                "A session rotation must replace the current secret hash.",
                nameof(replacementHash));
        }

        var normalizedOccurredAt = NormalizeOperationTimestamp(occurredAt);
        EnsureWithinUsableLifetime(normalizedOccurredAt);
        var nextRotationCount = checked(RotationCount + 1);

        SecretHash = replacementHash;
        RotationCount = nextRotationCount;
        SecretRotatedAt = normalizedOccurredAt;
        UpdatedAt = normalizedOccurredAt;
    }

    public void Revoke(
        SessionRevocationReason reason,
        DateTimeOffset occurredAt)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "The session revocation reason is not defined.");
        }

        EnsureStateCanTransitionTo(
            SessionState.Revoked,
            SessionState.Active,
            SessionState.Revoked);

        var normalizedOccurredAt = NormalizeOperationTimestamp(occurredAt);

        if (State == SessionState.Revoked)
        {
            return;
        }

        State = SessionState.Revoked;
        StateChangedAt = normalizedOccurredAt;
        UpdatedAt = normalizedOccurredAt;
        RevokedAt = normalizedOccurredAt;
        RevocationReason = reason;
    }

    public void Expire(DateTimeOffset occurredAt)
    {
        EnsureStateCanTransitionTo(SessionState.Expired, SessionState.Active);

        var normalizedOccurredAt = NormalizeOperationTimestamp(occurredAt);

        if (normalizedOccurredAt < EffectiveExpiresAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(occurredAt),
                occurredAt,
                "A session cannot expire before its effective expiry.");
        }

        State = SessionState.Expired;
        StateChangedAt = normalizedOccurredAt;
        UpdatedAt = normalizedOccurredAt;
        ExpiredAt = normalizedOccurredAt;
    }

    private void EnsureStateCanTransitionTo(
        SessionState requestedState,
        params SessionState[] allowedCurrentStates)
    {
        if (!allowedCurrentStates.Contains(State))
        {
            throw new InvalidSessionStateTransitionException(State, requestedState);
        }
    }

    private DateTimeOffset NormalizeOperationTimestamp(DateTimeOffset occurredAt)
    {
        var normalizedOccurredAt = NormalizeTimestamp(occurredAt, nameof(occurredAt));

        if (normalizedOccurredAt < UpdatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(occurredAt),
                occurredAt,
                "A session operation cannot occur before the previous accepted operation.");
        }

        return normalizedOccurredAt;
    }

    private void EnsureWithinUsableLifetime(DateTimeOffset occurredAt)
    {
        if (occurredAt >= EffectiveExpiresAt)
        {
            throw new SessionExpiredException(EffectiveExpiresAt, occurredAt);
        }
    }

    private static DateTimeOffset NormalizeTimestamp(
        DateTimeOffset timestamp,
        string parameterName)
    {
        if (timestamp == default)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                timestamp,
                "A non-default timestamp is required.");
        }

        return timestamp.ToUniversalTime();
    }
}
